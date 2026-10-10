using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.VerticalSliceFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class CampaignCombatAcceptanceTests
{
    private static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private static SimulationSession Awaiting(ScenarioDto scenario)
    {
        var s = Session(scenario); var result = s.AdvanceTurn(1, 0);
        Assert.Equal(TurnStatus.AwaitingCombat, result.Status); Assert.Equal(1, result.TurnNumber); Assert.Empty(result.Events);
        return s;
    }
    private static void Submit(SimulationSession s, CombatResultDto result)
    {
        var stamp = s.CaptureSnapshot(); Assert.Equal(CombatSubmissionStatus.Buffered, s.SubmitCombatResult(result, stamp.TurnNumber, stamp.StateRevision).Status);
    }
    private static void Finish(SimulationSession s)
    {
        var stamp = s.CaptureSnapshot(); var result = s.FinalizeCombat(stamp.TurnNumber, stamp.StateRevision);
        Assert.Equal(TurnStatus.Completed, result.Status); Assert.Equal(1, result.CompletedTurnNumber); Assert.Equal(2, result.TurnNumber);
        Assert.Empty(result.Events); Assert.Empty(s.GetPendingCombatRequests()); Assert.Empty(s.GetResolvedCombats());
    }
    private static CombatResultDto Scripted(CombatRequestDto request)
    {
        var r = new NoLossCombatResolver().Resolve(request); var first = request.Participants[0]; var second = request.Participants[1];
        // Deliberately sparse effects and several local RNG draws; presentation events remain ordered data.
        var rng = new EncounterRandomStream(request.RandomState); for (var i = 0; i < request.LocationSystemId.Length; i++) rng.NextUInt64(); r.FinalRandomState = rng.State;
        if (request.LocationSystemId == "system.bajor")
        {
            r.ShipChanges.Add(new() { ShipId = second.Ships[0].ShipId, RemainingHull = 7 });
            r.FleetChanges.Add(new() { FleetId = first.FleetId, Disposition = FleetCombatDisposition.Retreated, RetreatSystemId = "system.sol" });
            r.Events.Insert(0, new() { Kind = CombatEventKind.FleetRetreated, FleetId = first.FleetId });
            r.Events.Insert(1, new() { Kind = CombatEventKind.ShipDamaged, ShipId = second.Ships[0].ShipId });
        }
        else if (request.LocationSystemId == "system.bajoran-wormhole")
            r.FleetChanges.Add(new() { FleetId = first.FleetId, Disposition = FleetCombatDisposition.Destroyed });
        else
        {
            r.ShipChanges.Add(new() { ShipId = second.Ships[0].ShipId, RemainingHull = 6 });
            r.ShipChanges.Add(new() { ShipId = first.Ships[0].ShipId, RemainingHull = 0 });
            r.FleetChanges.Add(new() { FleetId = second.FleetId, Disposition = FleetCombatDisposition.Held });
            r.Events.Insert(0, new() { Kind = CombatEventKind.ShipDestroyed, ShipId = first.Ships[0].ShipId });
        }
        for (var i = 0; i < r.Events.Count; i++) r.Events[i].Sequence = i;
        return r;
    }
    private static SimulationSession Reload(SimulationSession s, ScenarioDto scenario)
    {
        var before = Json(s); var save = Clone(s.ExportSave());
        save.GameState.PendingCombats!.Reverse(); save.GameState.ResolvedCombats!.Reverse();
        foreach (var request in save.GameState.PendingCombats.Select(c => c.Request).Concat(save.GameState.ResolvedCombats.Select(c => c.Request)))
        { request.Participants.Reverse(); foreach (var participant in request.Participants) participant.Ships.Reverse(); }
        foreach (var combat in save.GameState.ResolvedCombats) { combat.Result.ShipChanges.Reverse(); combat.Result.FleetChanges.Reverse(); }
        var reloaded = new SimulationSession(save, Clone(scenario.Definitions), Compatibility(scenario));
        Assert.Equal(before, Json(reloaded)); return reloaded;
    }
    [Theory]
    [InlineData(0, 1, 2)] [InlineData(0, 2, 1)] [InlineData(1, 0, 2)]
    [InlineData(1, 2, 0)] [InlineData(2, 0, 1)] [InlineData(2, 1, 0)]
    public void EveryThreeBattlePermutationAndCheckpointReloadHasIdenticalCanonicalEffects(int a, int b, int c)
    {
        var scenario = CampaignCombatFixture.Scenario(3); var baseline = Awaiting(scenario); var frozen = baseline.GetPendingCombatRequests();
        foreach (var request in frozen) Submit(baseline, Scripted(request));
        var buffered = Json(baseline); Finish(baseline); var completed = Json(baseline);
        // Reload each of the three persisted combat points separately, plus all points together.
        for (var mode = 0; mode < 4; mode++)
        {
            var s = Awaiting(Clone(scenario)); var stamp = s.CaptureSnapshot(); var frozenJson = JsonSerializer.Serialize(s.GetPendingCombatRequests());
            if (mode == 0 || mode == 3) s = Reload(s, scenario);
            Assert.Equal(frozenJson, JsonSerializer.Serialize(s.GetPendingCombatRequests()));
            var order = new[] { a, b, c };
            for (var index = 0; index < order.Length; index++)
            {
                var result = Scripted(frozen[order[index]]); result.ShipChanges.Reverse(); result.FleetChanges.Reverse(); Submit(s, result);
                if ((mode == 1 && index == 0) || (mode == 2 && index == 2) || mode == 3) s = Reload(s, scenario);
                Assert.Equal(index + 1, s.GetResolvedCombats().Count); Assert.Equal(2 - index, s.GetPendingCombatRequests().Count);
                Assert.Equal(stamp.NextEntitySequence, s.CaptureSnapshot().NextEntitySequence); Assert.Equal(stamp.RngState.State, s.CaptureSnapshot().RngState.State);
                Assert.DoesNotContain(s.GetPendingCombatRequests(), r => r.EncounterId == result.EncounterId);
                var before = Json(s); var p = s.CaptureSnapshot(); Assert.Equal(CombatSubmissionError.AlreadyResolved, s.SubmitCombatResult(result, p.TurnNumber, p.StateRevision).ErrorCode); Assert.Equal(before, Json(s));
            }
            Assert.Equal(buffered, Json(s)); Finish(s); Assert.Equal(completed, Json(s));
        }
        var final = baseline.CaptureSnapshot(); Assert.Equal(5, final.StateRevision); Assert.Equal(4, final.Ships.Count); Assert.Equal(4, final.Fleets.Count);
        Assert.Contains(final.Ships, s => s.CurrentHull == 7); Assert.Contains(final.Ships, s => s.CurrentHull == 6);
        Assert.Contains(final.Fleets, f => f.LocationSystemId.Value == "system.sol");
        Assert.Equal((ulong)frozen.Count + 1UL, final.NextEntitySequence);
    }
    [Fact]
    public void ExtraDrawsInOneBattleCannotChangeAnotherRequestOrCampaignRandomState()
    {
        var scenario = CampaignCombatFixture.Scenario(3); var normal = Awaiting(scenario); var extra = Awaiting(Clone(scenario));
        var requests = normal.GetPendingCombatRequests(); var beforeExtra = JsonSerializer.Serialize(extra.GetPendingCombatRequests()); var stamp = normal.CaptureSnapshot();
        foreach (var request in requests) Submit(normal, CampaignCombatFixture.NoChanges(request));
        foreach (var request in extra.GetPendingCombatRequests().Reverse())
        {
            var result = CampaignCombatFixture.NoChanges(request);
            if (request.EncounterId == requests[0].EncounterId) { var rng = new EncounterRandomStream(request.RandomState); for (var i = 0; i < 97; i++) rng.NextUInt64(); result.FinalRandomState = rng.State; }
            Submit(extra, result);
        }
        Assert.Equal(beforeExtra, JsonSerializer.Serialize(extra.GetResolvedCombats().Select(c => c.Request)));
        Assert.Equal(stamp.RngState.State, extra.CaptureSnapshot().RngState.State); Assert.Equal(stamp.NextEntitySequence, extra.CaptureSnapshot().NextEntitySequence);
        Assert.Equal(JsonSerializer.Serialize(normal.GetResolvedCombats().Skip(1)), JsonSerializer.Serialize(extra.GetResolvedCombats().Skip(1)));
        Finish(normal); Finish(extra); Assert.Equal(Json(normal), Json(extra));
    }
    private sealed class ThrowingResolver : ICombatResolver
    {
        public CombatResultDto Resolve(CombatRequestDto request) { request.Participants.Clear(); throw new ApplicationException("Resolver failure before submission."); }
    }
    [Fact]
    public void ResolverExceptionCannotMutateTheCheckpointAndRetryUsesFrozenRequest()
    {
        var scenario = CampaignCombatFixture.Scenario(3); var s = Awaiting(scenario); var before = Json(s); var request = s.GetPendingCombatRequests()[0];
        Assert.Throws<ApplicationException>(() => new ThrowingResolver().Resolve(request)); Assert.Equal(before, Json(s));
        foreach (var pending in s.GetPendingCombatRequests()) Submit(s, CampaignCombatFixture.NoChanges(pending)); Finish(s);
        Assert.Equal(SessionPhase.Planning, s.CaptureSnapshot().Phase);
    }
    [Fact]
    public void NoLossOpponentsCanMeetAgainOnlyInTheNextStrategicTurn()
    {
        var s = Awaiting(CampaignCombatFixture.Scenario()); var first = s.GetPendingCombatRequests()[0]; var stamp = s.CaptureSnapshot();
        Submit(s, CampaignCombatFixture.NoChanges(first)); Finish(s);
        Assert.Equal(stamp.NextEntitySequence, s.CaptureSnapshot().NextEntitySequence); Assert.Equal(stamp.RngState.State, s.CaptureSnapshot().RngState.State);
        var next = s.CaptureSnapshot(); Assert.Equal(TurnStatus.AwaitingCombat, s.AdvanceTurn(next.TurnNumber, next.StateRevision).Status);
        var second = Assert.Single(s.GetPendingCombatRequests()); Assert.Equal(2, second.TurnNumber); Assert.NotEqual(first.EncounterId, second.EncounterId); Assert.NotEqual(first.RandomState, second.RandomState);
    }
    [Fact]
    public void MultiFleetMultiShipFreezeIsCanonicalAndDestroyedFleetRemovesAllSurvivors()
    {
        var scenario = CampaignCombatFixture.Scenario(1, true); var state = scenario.InitialState;
        var fleet = state.Fleets!.Single(f => f.OwnerCivilizationId == "civ.cardassian");
        state.Ships!.Add(new() { ShipId = "ship.cardassian-extra", FleetId = fleet.FleetId, OwnerCivilizationId = fleet.OwnerCivilizationId, ShipDesignDefinitionId = "ship-design.scout", CurrentHull = 8 });
        fleet.ShipIds!.Add("ship.cardassian-extra"); state.EntityIdentities.Add(new() { EntityId = "ship.cardassian-extra", DefinitionId = "ship-design.scout" });
        state.Fleets!.Add(new() { FleetId = "fleet.cardassian-extra", OwnerCivilizationId = fleet.OwnerCivilizationId, LocationSystemId = fleet.LocationSystemId, ShipIds = new() { "ship.cardassian-other" }, RouteSystemIds = new() });
        state.Ships.Add(new() { ShipId = "ship.cardassian-other", FleetId = "fleet.cardassian-extra", OwnerCivilizationId = fleet.OwnerCivilizationId, ShipDesignDefinitionId = "ship-design.scout", CurrentHull = 10 });
        state.EntityIdentities.Add(new() { EntityId = "fleet.cardassian-extra" }); state.EntityIdentities.Add(new() { EntityId = "ship.cardassian-other", DefinitionId = "ship-design.scout" });
        var s = Awaiting(scenario); var request = Assert.Single(s.GetPendingCombatRequests()); Assert.Equal(4, request.Participants.Count);
        Assert.Equal(request.Participants.Select(p => (p.CivilizationId, p.FleetId)).OrderBy(p => p.CivilizationId, StringComparer.Ordinal).ThenBy(p => p.FleetId, StringComparer.Ordinal), request.Participants.Select(p => (p.CivilizationId, p.FleetId)));
        var participant = request.Participants.Single(p => p.FleetId == fleet.FleetId); Assert.Equal(2, participant.Ships.Count);
        Assert.Equal(participant.Ships.Select(ship => ship.ShipId).OrderBy(id => id, StringComparer.Ordinal), participant.Ships.Select(ship => ship.ShipId));
        var result = CampaignCombatFixture.NoChanges(request); result.ShipChanges.Add(new() { ShipId = participant.Ships[0].ShipId, RemainingHull = 2 });
        result.FleetChanges.Add(new() { FleetId = fleet.FleetId, Disposition = FleetCombatDisposition.Destroyed }); Submit(s, result); Finish(s);
        Assert.DoesNotContain(s.CaptureSnapshot().Fleets, f => f.FleetId.Value == fleet.FleetId);
        Assert.All(participant.Ships, ship => { Assert.DoesNotContain(s.CaptureSnapshot().Ships, survivor => survivor.ShipId.Value == ship.ShipId); Assert.DoesNotContain(s.CaptureSnapshot().Entities, e => e.EntityId.Value == ship.ShipId); });
        Assert.Contains(s.CaptureSnapshot().Fleets, f => f.FleetId.Value == "fleet.cardassian-extra");
    }
    [Fact]
    public void FrozenForeignSingleFleetOccupancyRejectsRetreatWithoutAnotherBattle()
    {
        var scenario = CampaignCombatFixture.Scenario(); var state = scenario.InitialState;
        state.Fleets!.Add(new() { FleetId = "fleet.foreign", OwnerCivilizationId = "civ.dominion", LocationSystemId = "system.sol", ShipIds = new() { "ship.foreign" }, RouteSystemIds = new() });
        state.Ships!.Add(new() { ShipId = "ship.foreign", FleetId = "fleet.foreign", OwnerCivilizationId = "civ.dominion", ShipDesignDefinitionId = "ship-design.scout", CurrentHull = 10 });
        state.EntityIdentities.Add(new() { EntityId = "fleet.foreign" }); state.EntityIdentities.Add(new() { EntityId = "ship.foreign", DefinitionId = "ship-design.scout" });
        var s = Awaiting(scenario); var request = Assert.Single(s.GetPendingCombatRequests()); var result = CampaignCombatFixture.NoChanges(request);
        result.FleetChanges.Add(new() { FleetId = request.Participants[0].FleetId, Disposition = FleetCombatDisposition.Retreated, RetreatSystemId = "system.sol" });
        var before = Json(s); var stamp = s.CaptureSnapshot(); Assert.Equal(CombatSubmissionError.InvalidRetreat, s.SubmitCombatResult(result, stamp.TurnNumber, stamp.StateRevision).ErrorCode); Assert.Equal(before, Json(s));
    }

}
