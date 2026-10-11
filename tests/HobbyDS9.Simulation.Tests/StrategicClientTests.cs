using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityClient;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class StrategicClientTests
{
    private static readonly EntityId Human = new("civ.federation"), Colony = new("colony.sol"), Fleet = new("fleet.federation-scout");
    private static readonly DefinitionId Research = new("research.advanced-engineering"), Scout = new("ship-design.scout");
    private static StrategicClient Start(ScenarioDto? scenario = null)
    {
        var content = scenario ?? VerticalSliceFixture.Scenario(false);
        return new StrategicClient(CampaignApplication.StartScenario(content), save => CampaignApplication.LoadSave(content, save));
    }
    private static string Save(StrategicClient client) => JsonSerializer.Serialize(client.ExportSave());
    private static CivilizationSnapshot Actor(StrategicClient client) => client.Snapshot.Campaign.Civilizations.Single(c => c.CivilizationId == Human);
    private static ColonySnapshot Sol(StrategicClient client) => client.Snapshot.Campaign.Colonies.Single(c => c.ColonyId == Colony);

    [Fact]
    public void ResearchProjectionDistinguishesAvailableCurrentCompletedAndMissingPrerequisite()
    {
        var scenario = VerticalSliceFixture.Scenario();
        scenario.Definitions.DefinitionIds.Add("research.follow-up");
        scenario.Definitions.ResearchNodes!.Add(new()
        {
            DefinitionId = "research.follow-up",
            DisplayName = "Follow up",
            ResearchCost = 20,
            PrerequisiteDefinitionIds = new() { Research.Value }
        });
        var client = Start(scenario);
        var initial = StrategicOptions.Research(client.Snapshot, Human);
        Assert.Equal(ResearchDisplayState.Available, initial.Single(r => r.Definition.DefinitionId == Research).State);
        Assert.Equal(CommandErrorCode.ResearchPrerequisiteMissing, initial.Single(r => r.Definition.DefinitionId.Value == "research.follow-up").UnavailableReason);
        Assert.True(client.Research(client.Snapshot, Human, Research).Accepted);
        Assert.Equal(ResearchDisplayState.Current, StrategicOptions.Research(client.Snapshot, Human).Single(r => r.Definition.DefinitionId == Research).State);
        for (var i = 0; i < 3; i++) Assert.Equal(TurnStatus.Completed, client.EndTurn(client.Snapshot).Status);
        var complete = StrategicOptions.Research(client.Snapshot, Human);
        Assert.Equal(ResearchDisplayState.Completed, complete.Single(r => r.Definition.DefinitionId == Research).State);
        Assert.Equal(ResearchDisplayState.Available, complete.Single(r => r.Definition.DefinitionId.Value == "research.follow-up").State);
        Assert.Equal(ResearchDisplayState.Available, initial.Single(r => r.Definition.DefinitionId == Research).State);
        Assert.Throws<NotSupportedException>(() => ((IList<ResearchOption>)initial).Clear());
    }
    [Fact]
    public void ResearchCommandsRefreshOnAcceptanceAndRejectUnchangedWithoutMutation()
    {
        var client = Start(); var before = client.Snapshot;
        var accepted = client.Research(before, Human, Research);
        Assert.True(accepted.Accepted); Assert.Equal(1, client.Snapshot.Campaign.StateRevision);
        Assert.Null(before.Campaign.Civilizations.Single(c => c.CivilizationId == Human).ResearchTargetDefinitionId);
        Assert.Equal(Research, Actor(client).ResearchTargetDefinitionId);
        var save = Save(client); var rejected = client.Research(client.Snapshot, Human, Research);
        Assert.Equal(CommandErrorCode.ResearchTargetUnchanged, rejected.ErrorCode); Assert.Equal(save, Save(client));
        Assert.Contains("ResearchTargetUnchanged", CommandFeedback.Describe(rejected));
        Assert.NotEqual(accepted.CommandId, rejected.CommandId);
    }
    [Fact]
    public void ProductionUsesAuthoritativeCostsQueueAndCancellationRefunds()
    {
        var client = Start(); var old = client.Snapshot; var credits = Actor(client).Credits; var resource = PopulationFixture.Dilithium(Sol(client));
        var design = old.Campaign.ShipDesigns.Single(d => d.DefinitionId == Scout);
        Assert.True(client.Enqueue(old, Human, Colony, Scout).Accepted);
        Assert.Equal(credits - design.CreditCost, Actor(client).Credits);
        Assert.Equal(resource - design.ResourceCosts.Single(r => r.ResourceDefinitionId == "resource.dilithium").Amount, PopulationFixture.Dilithium(Sol(client)));
        var queued = Assert.Single(Sol(client).ProductionQueue);
        Assert.Empty(old.Campaign.Colonies.Single(c => c.ColonyId == Colony).ProductionQueue);
        Assert.True(client.Cancel(client.Snapshot, Human, Colony, queued.QueueItemId).Accepted);
        Assert.Empty(Sol(client).ProductionQueue); Assert.Equal(credits, Actor(client).Credits); Assert.Equal(resource, PopulationFixture.Dilithium(Sol(client)));
        Assert.Equal(2, client.Snapshot.Campaign.StateRevision);
    }
    [Theory]
    [InlineData("credits", CommandErrorCode.InsufficientCredits)]
    [InlineData("resource", CommandErrorCode.InsufficientStrategicResource)]
    [InlineData("research", CommandErrorCode.DesignResearchPrerequisiteMissing)]
    [InlineData("owner", CommandErrorCode.ColonyNotOwned)]
    public void ProductionProjectionMatchesCommandRejectionAndDoesNotMutate(string problem, CommandErrorCode expected)
    {
        var scenario = VerticalSliceFixture.Scenario(false);
        if (problem == "credits") scenario.InitialState.Civilizations!.Single(c => c.CivilizationId == Human.Value).Credits = 0;
        if (problem == "resource") PopulationFixture.SetDilithium(scenario.InitialState.Colonies!.Single(c => c.ColonyId == Colony.Value), 0);
        var client = Start(scenario);
        var design = problem == "research" ? new DefinitionId("ship-design.advanced-scout") : Scout;
        var colony = problem == "owner" ? new EntityId("colony.cardassia") : Colony;
        var option = StrategicOptions.Production(client.Snapshot, Human, colony).Single(o => o.Design.DefinitionId == design);
        Assert.False(option.CanEnqueue); Assert.Equal(expected, option.UnavailableReason);
        var before = Save(client); var rejected = client.Enqueue(client.Snapshot, Human, colony, design);
        Assert.Equal(expected, rejected.ErrorCode); Assert.Equal(before, Save(client));
        Assert.Contains(expected.ToString(), CommandFeedback.Describe(rejected));
    }
    [Fact]
    public void OldObservedStampIsSubmittedUnchangedAndNeverRetried()
    {
        var client = Start(); var observed = client.Snapshot;
        Assert.True(client.Research(observed, Human, Research).Accepted);
        var before = Save(client);
        var rejected = client.Enqueue(observed, Human, Colony, Scout);
        Assert.Equal(CommandErrorCode.StaleRevision, rejected.ErrorCode); Assert.Equal(before, Save(client));
        Assert.Equal(1, client.Snapshot.Campaign.StateRevision); Assert.Single(client.Snapshot.AcceptedPlayerCommandIds);
        Assert.Contains("Review", CommandFeedback.Describe(rejected));
    }
    [Fact]
    public void StaleTurnAndForeignActorAreNotSilentlyRetriedOrAutomated()
    {
        var client = Start(); var old = client.Snapshot;
        client.EndTurn(old); var before = Save(client);
        Assert.Equal(CommandErrorCode.StaleTurn, client.Research(old, Human, Research).ErrorCode);
        Assert.Equal(before, Save(client));
        var ai = new EntityId("civ.cardassian");
        Assert.All(StrategicOptions.Research(client.Snapshot, ai), option => Assert.False(option.CanSelect));
        Assert.Equal(CommandErrorCode.ActorNotControlled, client.Research(client.Snapshot, ai, Research).ErrorCode);
        Assert.Equal(before, Save(client));
    }
    [Fact]
    public void RoutePreviewIsDetachedAndDoesNotIssueAnOrderUntilConfirmed()
    {
        var client = Start(); var before = Save(client);
        var preview = client.Preview(client.Snapshot, Fleet, new("system.founders-homeworld"));
        Assert.True(preview.Route.Success); Assert.Equal(4, preview.Route.TraversalCost);
        Assert.Equal(new[] { "system.bajor", "system.bajoran-wormhole", "system.gamma-exit", "system.founders-homeworld" }, preview.Route.Steps.Select(n => n.Value));
        Assert.Equal(before, Save(client));
        Assert.Throws<NotSupportedException>(() => ((IList<NavigationNodeId>)preview.Route.Steps).Clear());
        Assert.True(client.SubmitRoute(preview).Accepted);
        Assert.Equal(preview.Route.Steps.Select(n => n.Value), client.Snapshot.Campaign.Fleets.Single(f => f.FleetId == Fleet).RouteNodeIds.Select(n => n.Value));
        Assert.Empty(preview.Observed.Campaign.Fleets.Single(f => f.FleetId == Fleet).RouteNodeIds);
    }
    [Fact]
    public void RouteCanBeReplacedAndClearedAtTheCurrentNode()
    {
        var client = Start();
        Assert.True(client.SubmitRoute(client.Preview(client.Snapshot, Fleet, new("system.bajor"))).Accepted);
        Assert.True(client.SubmitRoute(client.Preview(client.Snapshot, Fleet, new("system.cardassia"))).Accepted);
        var cleared = client.Preview(client.Snapshot, Fleet, new("system.sol"));
        Assert.Empty(cleared.Route.Steps); Assert.Equal(0, cleared.Route.TraversalCost);
        Assert.True(client.SubmitRoute(cleared).Accepted);
        Assert.Empty(client.Snapshot.Campaign.Fleets.Single(f => f.FleetId == Fleet).RouteNodeIds);
    }
    [Fact]
    public void StalePreviewCannotOverwriteAnOrder()
    {
        var client = Start(); var preview = client.Preview(client.Snapshot, Fleet, new("system.bajor"));
        client.Research(client.Snapshot, Human, Research); var before = Save(client);
        Assert.Equal(CommandErrorCode.StaleRevision, client.SubmitRoute(preview).ErrorCode);
        Assert.Equal(before, Save(client));
    }
    [Fact]
    public void InvalidDestinationDoesNotIssueAnEmptyRoute()
    {
        var client = Start(); var before = Save(client);
        var preview = client.Preview(client.Snapshot, Fleet, new("node.unknown"));
        Assert.Equal(NavigationRouteError.UnknownNode, preview.Route.ErrorCode);
        Assert.False(client.SubmitRoute(preview).Accepted); Assert.Equal(before, Save(client));
    }
    [Fact]
    public void CommandIdsSkipAcceptedIdsAfterReloadAndDoNotUseAllocationOrRng()
    {
        var client = Start(); Assert.True(client.Research(client.Snapshot, Human, Research).Accepted);
        var save = client.ExportSave();
        var ids = new ClientCommandIds(); var next = ids.Next(client.Snapshot);
        Assert.Equal("player-ui.1", next.Value); Assert.True(next.IsValid);
        Assert.DoesNotContain(next, client.Snapshot.AcceptedPlayerCommandIds);
        var restored = Start(); restored.Load(save);
        var result = restored.Enqueue(restored.Snapshot, Human, Colony, Scout);
        Assert.True(result.Accepted); Assert.Equal("player-ui.1", result.CommandId);
        var pre = Save(restored); var a = ids.Next(restored.Snapshot); var b = ids.Next(restored.Snapshot);
        Assert.NotEqual(a, b); Assert.DoesNotContain("ai-command.", a.Value); Assert.Equal(pre, Save(restored));
    }
    [Fact]
    public void SerializationReloadReplacesSessionAndDetachedSnapshotsRemainUnchanged()
    {
        var client = Start(); client.Research(client.Snapshot, Human, Research);
        var json = Save(client); var old = client.Snapshot;
        client.Enqueue(client.Snapshot, Human, Colony, Scout);
        client.Load(JsonSerializer.Deserialize<SaveEnvelopeDto>(json)!);
        Assert.Equal(json, Save(client)); Assert.Equal(1, client.Snapshot.Campaign.StateRevision); Assert.Empty(Sol(client).ProductionQueue);
        Assert.NotSame(old, client.Snapshot); Assert.Equal(1, old.Campaign.StateRevision);
        Assert.True(client.Enqueue(client.Snapshot, Human, Colony, Scout).Accepted);
    }
    [Fact]
    public void IncompatibleLoadLeavesLiveSessionAndObservedSnapshotIntact()
    {
        var client = Start(); client.Research(client.Snapshot, Human, Research); var before = Save(client); var old = client.Snapshot;
        var bad = client.ExportSave(); bad.ContentHash = "wrong";
        Assert.Throws<ArgumentException>(() => client.Load(bad));
        Assert.Same(old, client.Snapshot); Assert.Equal(before, Save(client));
        Assert.True(client.Enqueue(client.Snapshot, Human, Colony, Scout).Accepted);
    }
    [Fact]
    public void ExportedDtosCannotMutateClientState()
    {
        var client = Start(); var before = Save(client); var dto = client.ExportSave();
        dto.GameState.Civilizations!.Clear(); dto.GameState.Fleets!.Clear(); dto.GameState.StateRevision = 999;
        Assert.Equal(before, Save(client)); Assert.Equal(3, client.Snapshot.Campaign.Civilizations.Count);
    }
    [Fact]
    public void SaveSlotsRequireOverwriteAndReplaceWholeFileWithoutTempLeaks()
    {
        var path = Path.Combine(Path.GetTempPath(), "hobby-save-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SaveSlotStore(path); var client = Start(); var first = Save(client);
            store.Write(1, first, false); Assert.True(store.Exists(1)); Assert.Equal(first, store.Read(1));
            client.Research(client.Snapshot, Human, Research); var second = Save(client);
            Assert.Throws<IOException>(() => store.Write(1, second, false)); Assert.Equal(first, store.Read(1));
            store.Write(1, second, true); Assert.Equal(second, store.Read(1)); Assert.Single(Directory.GetFiles(path));
            var restored = Start(); restored.Load(JsonSerializer.Deserialize<SaveEnvelopeDto>(store.Read(1))!); Assert.Equal(second, Save(restored));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Write(0, second, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Read(4));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }
    [Fact]
    public void CombatCheckpointSaveAndReloadPreservesBufferAndBlocksStrategicActions()
    {
        var client = Start();
        while (client.Snapshot.Campaign.Phase == SessionPhase.Planning && client.Snapshot.Campaign.TurnNumber < 10) client.EndTurn(client.Snapshot);
        Assert.Equal(SessionPhase.AwaitingCombat, client.Snapshot.Campaign.Phase);
        var before = Save(client);
        Assert.Equal(CommandErrorCode.CombatPending, client.Research(client.Snapshot, Human, Research).ErrorCode);
        Assert.All(StrategicOptions.Research(client.Snapshot, Human), o => Assert.Equal(CommandErrorCode.CombatPending, o.UnavailableReason));
        Assert.All(StrategicOptions.Production(client.Snapshot, Human, Colony), o => Assert.Equal(CommandErrorCode.CombatPending, o.UnavailableReason));
        Assert.Equal(before, Save(client));
        var restored = Start(); restored.Load(JsonSerializer.Deserialize<SaveEnvelopeDto>(before)!);
        foreach (var request in restored.Pending()) Assert.Equal(CombatSubmissionStatus.Buffered, restored.Resolve(restored.Snapshot, request).Status);
        var buffered = Save(restored); restored.Load(JsonSerializer.Deserialize<SaveEnvelopeDto>(buffered)!);
        Assert.Empty(restored.Pending()); Assert.Equal(buffered, Save(restored));
        Assert.Equal(TurnStatus.Completed, restored.FinalizeCombats(restored.Snapshot).Status);
    }
    [Fact]
    public void CancellingAMissingItemRejectsWithoutChangingResourcesOrLedger()
    {
        var client = Start(); var before = Save(client);
        var result = client.Cancel(client.Snapshot, Human, Colony, new("queue-item.missing"));
        Assert.Equal(CommandErrorCode.UnknownQueueItem, result.ErrorCode);
        Assert.Equal(before, Save(client)); Assert.Empty(client.Snapshot.AcceptedPlayerCommandIds);
    }
    [Fact]
    public void InvalidResearchDoesNotBlockTheNextValidCommandOrReuseItsId()
    {
        var client = Start(); var before = Save(client);
        var invalid = client.Research(client.Snapshot, Human, new("research.unknown"));
        Assert.Equal(CommandErrorCode.UnknownResearchNode, invalid.ErrorCode); Assert.Equal(before, Save(client));
        var valid = client.Research(client.Snapshot, Human, Research);
        Assert.True(valid.Accepted); Assert.NotEqual(valid.CommandId, invalid.CommandId);
    }
    [Fact]
    public void DisconnectedRouteReportsUnreachableWithoutStateChanges()
    {
        var scenario = VerticalSliceFixture.Scenario();
        scenario.Definitions.NavigationConnections!.RemoveAll(e => e.EndpointA == "system.gamma-exit" || e.EndpointB == "system.gamma-exit");
        foreach(var k in scenario.InitialState.NavigationKnowledge!) k.Connections!.RemoveAll(e=>!scenario.Definitions.NavigationConnections.Any(c=>c.ConnectionId==e.Id));
        var client = Start(scenario); var before = Save(client);
        var preview = client.Preview(client.Snapshot, Fleet, new("system.founders-homeworld"));
        Assert.Equal(NavigationRouteError.Unreachable, preview.Route.ErrorCode);
        Assert.Null(preview.Route.TraversalCost); Assert.False(client.SubmitRoute(preview).Accepted);
        Assert.Equal(before, Save(client));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClientTwentyFiveTurnPathMatchesApplicationWithRepeatedSaveReload(bool reverse)
    {
        var scenario = VerticalSliceFixture.Scenario(false);
        var client = Start(scenario); var direct = CampaignApplication.StartScenario(scenario);
        for (var i = 0; i < 25; i++)
        {
            var p = direct.CaptureSnapshot().Campaign;
            var expected = direct.AdvanceTurn(p.TurnNumber, p.StateRevision);
            Assert.Equal(expected.Status, client.EndTurn(client.Snapshot).Status);
            client.Load(JsonSerializer.Deserialize<SaveEnvelopeDto>(Save(client))!);
            if (expected.Status == TurnStatus.AwaitingCombat)
            {
                var pending = direct.GetPendingCombatRequests();
                foreach (var request in reverse ? pending.Reverse() : pending)
                {
                    var observed = direct.CaptureSnapshot().Campaign;
                    direct.SubmitCombatResult(direct.ResolveCombat(request), observed.TurnNumber, observed.StateRevision);
                }
                var requests = client.Pending();
                foreach (var request in reverse ? requests.Reverse() : requests)
                {
                    Assert.Equal(CombatSubmissionStatus.Buffered, client.Resolve(client.Snapshot, request).Status);
                    client.Load(JsonSerializer.Deserialize<SaveEnvelopeDto>(Save(client))!);
                }
                var finish = direct.CaptureSnapshot().Campaign;
                direct.FinalizeCombat(finish.TurnNumber, finish.StateRevision);
                Assert.Equal(TurnStatus.Completed, client.FinalizeCombats(client.Snapshot).Status);
            }
            Assert.Equal(JsonSerializer.Serialize(direct.ExportSave()), Save(client));
        }
        Assert.Equal(26, client.Snapshot.Campaign.TurnNumber);
        Assert.Equal(173, client.Snapshot.Campaign.StateRevision);
    }

}
