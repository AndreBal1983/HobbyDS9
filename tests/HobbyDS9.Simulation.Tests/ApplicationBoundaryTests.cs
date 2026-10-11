using System.Collections;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.PhasedCombatFixture;
using V = HobbyDS9.Simulation.Tests.VerticalSliceFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class ApplicationBoundaryTests
{
    private static string Save(ICampaignSession app) => Json(app.ExportSave());
    private static SessionSnapshot State(ICampaignSession app) => app.CaptureSnapshot().Campaign;
    private static CommandEnvelope Command(ICampaignSession app, string id = "cmd.test", CommandKind kind = CommandKind.SetResearchTarget)
    {
        var stamp = State(app);
        return new CommandEnvelope
        {
            CommandId = id,
            ActorCivilizationId = "civ.federation",
            ExpectedTurn = stamp.TurnNumber,
            ExpectedRevision = stamp.StateRevision,
            Kind = kind,
            SetResearchTarget = new() { TargetDefinitionId = "research.advanced-engineering" }
        };
    }
    private static ICampaignSession Reload(ICampaignSession app, ScenarioDto scenario)
    {
        var json = Save(app); var loaded = CampaignApplication.LoadSave(Clone(scenario), JsonSerializer.Deserialize<SaveEnvelopeDto>(json)!);
        Assert.Equal(json, Save(loaded)); return loaded;
    }

    [Fact]
    public void PortableFacadeHasNoPublicSimulationTypesOrHostDependencies()
    {
        var assembly = typeof(CampaignApplication).Assembly;
        Assert.Equal(".NETStandard,Version=v2.1", assembly.GetCustomAttribute<TargetFrameworkAttribute>()!.FrameworkName);
        Assert.All(assembly.GetReferencedAssemblies(), r => Assert.Contains(r.Name, new[] { "netstandard", "HobbyDS9.Contracts", "HobbyDS9.Simulation" }));
        void Check(Type type)
        {
            Assert.NotEqual(typeof(SimulationSession).Assembly, type.Assembly);
            foreach (var argument in type.GetGenericArguments()) Check(argument);
            if (type.HasElementType) Check(type.GetElementType()!);
        }
        foreach (var type in assembly.GetExportedTypes())
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            { Check(method.ReturnType); foreach (var parameter in method.GetParameters()) Check(parameter.ParameterType); }
        Assert.Empty(typeof(CampaignApplication).GetConstructors());
    }

    [Fact]
    public void ScenarioSaveContentAndOldSnapshotsCannotMutateTheSession()
    {
        var scenario = V.Scenario(); var app = CampaignApplication.StartScenario(scenario); var old = app.CaptureSnapshot();
        var initial = Save(app); var view = Json(old);
        scenario.InitialState.Civilizations![0].Credits = 0;
        scenario.InitialState.Fleets![0].RouteNodeIds!.Add("system.sol");
        scenario.Definitions.Civilizations![0].DisplayName = "changed";
        scenario.Definitions.StarSystems!.Clear(); scenario.Definitions.ResearchNodes![0].PrerequisiteDefinitionIds!.Add("research.missing");
        scenario.Definitions.ShipDesigns![0].CombatProfile!.AccuracyBps = 0;
        var save = app.ExportSave(); save.GameState.Civilizations!.Clear(); save.GameState.RngState.State = 0; save.GameState.AcceptedCommandIds!.Add("cmd.fake");
        Assert.Equal(initial, Save(app)); Assert.Equal(view, Json(old));
        Assert.Throws<NotSupportedException>(() => ((IList)old.Campaign.Fleets).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList)old.Campaign.Fleets[0].ShipIds).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList)old.Content.Research[0].Prerequisites).Add(new DefinitionId("research.other")));
        Assert.Throws<NotSupportedException>(() => ((IList)old.Navigation.Nodes).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList)old.AcceptedPlayerCommandIds).Add(new EntityId("cmd.fake")));
        Assert.True(app.SubmitCommand(Command(app)).Accepted);
        Assert.Equal(view, Json(old)); Assert.Single(app.CaptureSnapshot().AcceptedPlayerCommandIds);
        Assert.Equal(0, old.Campaign.StateRevision);
    }

    [Fact]
    public void AllFourPlayerCommandsUseTheExistingHandlersAndLedger()
    {
        var app = CampaignApplication.StartScenario(V.Scenario(false));
        Assert.True(app.SubmitCommand(Command(app, "cmd.research")).Accepted);
        var enqueue = Command(app, "cmd.build", CommandKind.EnqueueProduction); enqueue.SetResearchTarget = null;
        enqueue.EnqueueProduction = new() { ColonyId = "colony.sol", DefinitionId = "ship-design.scout" };
        Assert.True(app.SubmitCommand(enqueue).Accepted);
        enqueue.EnqueueProduction.ColonyId = "colony.invalid"; // Not retained.
        var queue = State(app).Colonies.Single(c => c.ColonyId.Value == "colony.sol").ProductionQueue;
        var cancel = Command(app, "cmd.cancel", CommandKind.CancelProduction); cancel.SetResearchTarget = null;
        cancel.CancelProduction = new() { ColonyId = "colony.sol", QueueItemId = Assert.Single(queue).QueueItemId.Value };
        Assert.True(app.SubmitCommand(cancel).Accepted);
        var nav = app.CaptureSnapshot().Navigation;
        var from = nav.Nodes.Single(n => n.AssociatedSystemId!.Value.Value == "system.sol").NodeId;
        var to = nav.Nodes.Single(n => n.AssociatedSystemId!.Value.Value == "system.gamma-exit").NodeId;
        var route = app.FindRoute(from, to); var stamp = State(app);
        Assert.True(app.SubmitFleetRoute(new("cmd.route"), new("civ.federation"), stamp.TurnNumber, stamp.StateRevision, new("fleet.federation-scout"), route.Steps).Accepted);
        Assert.Equal(4, State(app).StateRevision); Assert.Equal(4, app.CaptureSnapshot().AcceptedPlayerCommandIds.Count);
        Assert.Equal(new[] { "system.bajor", "system.bajoran-wormhole", "system.gamma-exit" }, State(app).Fleets.Single(f => f.FleetId.Value == "fleet.federation-scout").RouteNodeIds.Select(i => i.Value));
        Assert.Empty(State(app).Colonies.Single(c => c.ColonyId.Value == "colony.sol").ProductionQueue);
    }

    [Theory]
    [InlineData("stale-revision", CommandErrorCode.StaleRevision)]
    [InlineData("stale-turn", CommandErrorCode.StaleTurn)]
    [InlineData("ai", CommandErrorCode.ActorNotControlled)]
    [InlineData("reserved", CommandErrorCode.ReservedCommandId)]
    [InlineData("payload", CommandErrorCode.InvalidPayload)]
    [InlineData("unknown", CommandErrorCode.UnknownCommandKind)]
    public void RejectedPlayerActionsPreserveTheEntireState(string reason, CommandErrorCode expected)
    {
        var app = CampaignApplication.StartScenario(V.Scenario(false)); var cmd = Command(app);
        if (reason == "stale-revision") cmd.ExpectedRevision = 10;
        if (reason == "stale-turn") cmd.ExpectedTurn = 2;
        if (reason == "ai") cmd.ActorCivilizationId = "civ.cardassian";
        if (reason == "reserved") cmd.CommandId = "ai-command.player";
        if (reason == "payload") cmd.SetResearchTarget = null;
        if (reason == "unknown") cmd.Kind = (CommandKind)999;
        var before = Save(app); Assert.Equal(expected, app.SubmitCommand(cmd).ErrorCode); Assert.Equal(before, Save(app));
        Assert.Empty(app.CaptureSnapshot().AcceptedPlayerCommandIds);
    }

    [Fact]
    public void StampsAreNeverRefreshedAndDuplicatesSurviveReloadUntilCompletion()
    {
        var scenario = V.Scenario(); var app = CampaignApplication.StartScenario(scenario); var command = Command(app);
        Assert.True(app.SubmitCommand(command).Accepted); app = Reload(app, scenario);
        var before = Save(app); Assert.Equal(CommandErrorCode.DuplicateCommand, app.SubmitCommand(command).ErrorCode);
        Assert.Equal(TurnErrorCode.StaleRevision, app.AdvanceTurn(1, 0).ErrorCode); Assert.Equal(before, Save(app));
        Assert.Equal(TurnStatus.Completed, app.AdvanceTurn(1, 1).Status);
        Assert.Equal(2, State(app).TurnNumber); Assert.Equal(2, State(app).StateRevision); Assert.Empty(app.CaptureSnapshot().AcceptedPlayerCommandIds);
        Assert.Equal(CommandErrorCode.StaleTurn, app.SubmitCommand(command).ErrorCode);
        Reload(app, scenario);
    }

    [Theory]
    [InlineData("schema", ValidationCode.UnsupportedSaveFormat)]
    [InlineData("content", ValidationCode.ContentMismatch)]
    [InlineData("rules", ValidationCode.RulesMismatch)]
    [InlineData("scenario", ValidationCode.ScenarioMismatch)]
    public void FailedLoadsReportStableIssuesAndLeaveTheExistingSessionAlone(string reason, ValidationCode code)
    {
        var scenario = V.Scenario(); var app = CampaignApplication.StartScenario(scenario); var save = app.ExportSave(); var before = Save(app);
        if (reason == "schema") save.SaveFormatVersion--;
        if (reason == "content") save.ContentHash = new string('a', 64);
        if (reason == "rules") save.RulesVersion = "different";
        if (reason == "scenario") save.ScenarioId = "different";
        Assert.Contains(CampaignApplication.ValidateSave(scenario, save), i => i.Code == code);
        Assert.Throws<ArgumentException>(() => CampaignApplication.LoadSave(scenario, save)); Assert.Equal(before, Save(app));
    }

    [Fact]
    public void LoadedSaveAndCatalogAreOwnedAndInvalidScenariosHaveNoSession()
    {
        var scenario = V.Scenario(); var save = CampaignApplication.StartScenario(scenario).ExportSave();
        var app = CampaignApplication.LoadSave(scenario, save); var before = Save(app); var view = Json(app.CaptureSnapshot());
        save.GameState.Fleets!.Clear(); scenario.Definitions.ShipDesigns!.Clear();
        Assert.Equal(before, Save(app)); Assert.Equal(view, Json(app.CaptureSnapshot()));
        Assert.NotEmpty(CampaignApplication.ValidateScenario(scenario));
        Assert.Throws<ArgumentException>(() => CampaignApplication.StartScenario(scenario));
        Assert.NotEmpty(CampaignApplication.ValidateScenario(null));
    }

    [Fact]
    public void NavigationQueriesAreDetachedDeterministicAndDoNotPromiseMovementTiming()
    {
        var scenario = V.Scenario(); var app = CampaignApplication.StartScenario(scenario);
        var reordered = Clone(scenario); reordered.InitialState.StarSystems!.Reverse(); reordered.Definitions.GalaxyEdges!.Reverse();
        foreach (var edge in reordered.Definitions.GalaxyEdges) (edge.EndpointA, edge.EndpointB) = (edge.EndpointB, edge.EndpointA);
        var other = CampaignApplication.StartScenario(reordered);
        Assert.Equal(Json(app.CaptureSnapshot().Navigation), Json(other.CaptureSnapshot().Navigation));
        var before = Save(app); var route = app.FindRoute(new("system.sol"), new("system.founders-homeworld"));
        Assert.True(route.Success); Assert.Equal(4L, route.TraversalCost);
        Assert.Equal(Json(route), Json(other.FindRoute(new("system.sol"), new("system.founders-homeworld"))));
        Assert.Equal(0L, app.FindRoute(new("system.sol"), new("system.sol")).TraversalCost);
        Assert.Equal(NavigationRouteError.InvalidNode, app.FindRoute(default, new("system.sol")).ErrorCode);
        var missing = app.FindRoute(new("node.unknown"), new("system.sol")); Assert.Equal(NavigationRouteError.UnknownNode, missing.ErrorCode); Assert.Null(missing.TraversalCost);
        Assert.All(app.CaptureSnapshot().Navigation.Links, e => Assert.Equal(1L, e.TraversalCost));
        Assert.Single(app.CaptureSnapshot().Navigation.Links, e => e.Kind == GalaxyEdgeKind.Wormhole);
        Assert.Equal(before, Save(app));
        // Client model represents non-system nodes even though this adapter does not create them yet.
        Assert.Null(new NavigationNodeSnapshot(new("node.interstellar"), null).AssociatedSystemId);
        Assert.Equal(CommandErrorCode.InvalidRoute, app.SubmitFleetRoute(new("cmd.bad-route"), new("civ.federation"), 1, 0, new("fleet.federation-scout"), new[] { new NavigationNodeId("node.unknown") }).ErrorCode);
        Assert.Equal(CommandErrorCode.InvalidRoute, app.SubmitFleetRoute(new("cmd.disconnected"), new("civ.federation"), 1, 0, new("fleet.federation-scout"), new[] { new NavigationNodeId("system.gamma-exit") }).ErrorCode);
        Assert.Equal(before, Save(app));
    }

    [Theory]
    [InlineData(1, 99, "civ.federation", CommandErrorCode.StaleRevision)]
    [InlineData(2, 0, "civ.federation", CommandErrorCode.StaleTurn)]
    [InlineData(1, 0, "civ.cardassian", CommandErrorCode.ActorNotControlled)]
    public void NavigationSubmissionPreservesCallerStampsAndHumanControl(int turn, long revision, string actor, CommandErrorCode error)
    {
        var app = CampaignApplication.StartScenario(V.Scenario(false)); var before = Save(app);
        Assert.Equal(error, app.SubmitFleetRoute(new("cmd.route"), new(actor), turn, revision, new("fleet.federation-scout"), new[] { new NavigationNodeId("system.bajor") }).ErrorCode);
        Assert.Equal(before, Save(app));
    }

    [Fact]
    public void DisconnectedNavigationHasNoRouteCostAndNoMutation()
    {
        var scenario = V.Scenario(); scenario.Definitions.NavigationConnections!.Clear();
        foreach(var k in scenario.InitialState.NavigationKnowledge!) k.Connections!.Clear();
        var app = CampaignApplication.StartScenario(scenario); var before = Save(app);
        var route = app.FindRoute(new("system.sol"), new("system.bajor"));
        Assert.Equal(NavigationRouteError.Unreachable, route.ErrorCode); Assert.Empty(route.Steps); Assert.Null(route.TraversalCost);
        Assert.Equal(before, Save(app));
    }

    [Fact]
    public void CombatCheckpointLifecycleReloadAndReturnedDtoIsolationStayAuthoritative()
    {
        var scenario = CampaignCombatFixture.Scenario(3, true); var app = CampaignApplication.StartScenario(scenario);
        Assert.True(app.SubmitCommand(Command(app)).Accepted);
        Assert.Equal(TurnStatus.AwaitingCombat, app.AdvanceTurn(1, 1).Status);
        var original = Save(app); var stamp = State(app); Assert.Equal(1, stamp.TurnNumber); Assert.Equal(SessionPhase.AwaitingCombat, stamp.Phase);
        Assert.Equal(CommandErrorCode.CombatPending, app.SubmitCommand(Command(app, "cmd.blocked")).ErrorCode);
        Assert.Equal(TurnErrorCode.CombatPending, app.AdvanceTurn(stamp.TurnNumber, stamp.StateRevision).ErrorCode);
        Assert.Equal(TurnErrorCode.UnresolvedCombats, app.FinalizeCombat(stamp.TurnNumber, stamp.StateRevision).ErrorCode);
        Assert.Equal(original, Save(app)); app = Reload(app, scenario);
        var requests = app.GetPendingCombatRequests(); requests[0].Participants[0].Ships[0].CombatProfile!.LongRangeAttack = 999;
        requests[0].Participants.Clear(); Assert.Equal(original, Save(app));
        var oldSnapshot = app.CaptureSnapshot(); var oldJson = Json(oldSnapshot);
        foreach (var request in app.GetPendingCombatRequests().Reverse())
        {
            var before = Save(app); var plans = Plans(request); var requestJson = Json(request); var planJson = Json(plans);
            var result = app.ResolveCombat(request, plans);
            Assert.Equal(before, Save(app)); Assert.Equal(requestJson, Json(request)); Assert.Equal(planJson, Json(plans));
            stamp = State(app);
            Assert.Equal(CombatSubmissionError.StaleRevision, app.SubmitCombatResult(result, stamp.TurnNumber, stamp.StateRevision - 1).ErrorCode);
            Assert.Equal(before, Save(app)); Assert.Equal(CombatSubmissionStatus.Buffered, app.SubmitCombatResult(result, stamp.TurnNumber, stamp.StateRevision).Status);
            var buffered = Save(app); result.Events.Clear(); result.ShipChanges.Clear();
            var returned = app.GetResolvedCombats()[0]; returned.Request.Participants.Clear(); returned.Result.Events.Clear();
            Assert.Equal(buffered, Save(app)); app = Reload(app, scenario);
            Assert.Single(app.CaptureSnapshot().AcceptedPlayerCommandIds); Assert.Equal(SessionPhase.AwaitingCombat, State(app).Phase);
        }
        Assert.Equal(oldJson, Json(oldSnapshot)); Assert.Empty(app.GetPendingCombatRequests()); Assert.Equal(3, app.GetResolvedCombats().Count);
        stamp = State(app); var ready = Save(app);
        Assert.Equal(TurnErrorCode.StaleRevision, app.FinalizeCombat(1, stamp.StateRevision - 1).ErrorCode); Assert.Equal(ready, Save(app));
        Assert.Equal(TurnStatus.Completed, app.FinalizeCombat(1, stamp.StateRevision).Status);
        Assert.Equal(stamp.StateRevision + 1, State(app).StateRevision); Assert.Equal(2, State(app).TurnNumber);
        Assert.Empty(app.CaptureSnapshot().AcceptedPlayerCommandIds); Assert.Empty(State(app).Combats); Reload(app, scenario);
    }

    [Fact]
    public void InvalidPlansAndRejectedConsequencesCannotChangeCheckpoint()
    {
        var app = CampaignApplication.StartScenario(CampaignCombatFixture.Scenario()); app.AdvanceTurn(1, 0);
        var request = app.GetPendingCombatRequests()[0]; var before = Save(app); var plans = Plans(request); plans[0].LongRangeTactic = (CombatTactic)999;
        Assert.Throws<ArgumentException>(() => app.ResolveCombat(request, plans)); Assert.Equal(before, Save(app));
        var result = app.ResolveCombat(request); result.ResolverVersion++; var stamp = State(app);
        Assert.Equal(CombatSubmissionError.ResolverMismatch, app.SubmitCombatResult(result, stamp.TurnNumber, stamp.StateRevision).ErrorCode); Assert.Equal(before, Save(app));
        plans = Plans(request); plans[0].RetreatHullPercent = 100; plans[0].RetreatNodeId = "system.sol";
        result = app.ResolveCombat(request, plans); Assert.Contains(result.FleetChanges, c => c.Disposition == FleetCombatDisposition.Retreated);
        Assert.Equal(CombatSubmissionStatus.Buffered, app.SubmitCombatResult(result, stamp.TurnNumber, stamp.StateRevision).Status);
        stamp = State(app); Assert.Equal(TurnStatus.Completed, app.FinalizeCombat(stamp.TurnNumber, stamp.StateRevision).Status);
        Assert.Contains(State(app).Fleets, f => f.FleetId.Value == plans[0].FleetId && f.LocationNodeId.Value == "system.sol");
    }

    [Fact]
    public void ApplicationPropagatesTurnFailureWithoutChangingAnyAuthoritativeFacts()
    {
        var scenario = V.Scenario(false); PopulationFixture.SetDilithium(scenario.InitialState.Colonies!.Single(c => c.ColonyId == "colony.sol"), long.MaxValue);
        var app = CampaignApplication.StartScenario(scenario); Assert.True(app.SubmitCommand(Command(app)).Accepted); var before = Save(app);
        Assert.Throws<OverflowException>(() => app.AdvanceTurn(1, 1)); Assert.Equal(before, Save(app));
        Assert.Single(app.CaptureSnapshot().AcceptedPlayerCommandIds); Assert.Equal(SessionPhase.Planning, State(app).Phase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwentyFiveTurnsMatchDirectSimulationIncludingEveryCombatReload(bool reverse)
    {
        var scenario = V.Scenario(false); var direct = V.Session(scenario); var app = CampaignApplication.StartScenario(Clone(scenario));
        var resolver = new DeterministicPhasedCombatResolver(); var encounters = 0; var destroyed = 0;
        for (var i = 0; i < 25; i++)
        {
            var p = State(app); var directTurn = direct.AdvanceTurn(p.TurnNumber, p.StateRevision); var appTurn = app.AdvanceTurn(p.TurnNumber, p.StateRevision);
            Assert.Equal(Json(directTurn), Json(appTurn)); Assert.Equal(V.Json(direct), Save(app));
            Assert.Empty(app.CaptureSnapshot().AcceptedPlayerCommandIds); // Trusted AI does not enter the player ledger projection.
            if (appTurn.Status == TurnStatus.AwaitingCombat)
            {
                foreach (var request in direct.GetPendingCombatRequests())
                { var stamp = direct.CaptureSnapshot(); Assert.Equal(CombatSubmissionStatus.Buffered, direct.SubmitCombatResult(resolver.Resolve(request), stamp.TurnNumber, stamp.StateRevision).Status); }
                app = Reload(app, scenario); var requests = app.GetPendingCombatRequests();
                foreach (var request in reverse ? requests.Reverse() : requests)
                {
                    encounters++; var result = app.ResolveCombat(request); destroyed += result.Events.Count(e => e.Kind == CombatEventKind.ShipDestroyed);
                    var stamp = State(app); Assert.Equal(CombatSubmissionStatus.Buffered, app.SubmitCombatResult(result, stamp.TurnNumber, stamp.StateRevision).Status);
                    app = Reload(app, scenario);
                }
                Assert.Equal(V.Json(direct), Save(app)); p = State(app);
                Assert.Equal(Json(direct.FinalizeCombat(p.TurnNumber, p.StateRevision)), Json(app.FinalizeCombat(p.TurnNumber, p.StateRevision)));
            }
            Assert.Equal(V.Json(direct), Save(app)); app = Reload(app, scenario);
        }
        Assert.Equal(15, encounters); Assert.Equal(12, destroyed); Assert.Equal(26, State(app).TurnNumber);
        Assert.Equal(7, State(app).Ships.Count); Assert.Equal(173, State(app).StateRevision);
    }
}
