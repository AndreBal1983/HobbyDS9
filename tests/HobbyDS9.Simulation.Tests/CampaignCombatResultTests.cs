using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.VerticalSliceFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class CampaignCombatResultTests
{
    private static SimulationSession Awaiting(int battles = 1)
    { var s = Session(CampaignCombatFixture.Scenario(battles)); Assert.Equal(TurnStatus.AwaitingCombat, s.AdvanceTurn(1, 0).Status); return s; }
    private static CombatSubmissionResult Submit(SimulationSession s, CombatResultDto? r)
    { var p = s.CaptureSnapshot(); return s.SubmitCombatResult(r, p.TurnNumber, p.StateRevision); }
    private static TurnResult Finalize(SimulationSession s)
    { var p = s.CaptureSnapshot(); return s.FinalizeCombat(p.TurnNumber, p.StateRevision); }

    [Fact]
    public void NoLossIsPureDeterministicAndConsumesNoLocalOrCampaignRandomness()
    {
        var s = Awaiting(); var request = s.GetPendingCombatRequests()[0]; request.ResolverId = NoLossCombatResolver.ResolverId; var before = JsonSerializer.Serialize(request); var campaign = Json(s); var resolver = new NoLossCombatResolver();
        var a = resolver.Resolve(request); var b = resolver.Resolve(JsonSerializer.Deserialize<CombatRequestDto>(before)!);
        Assert.Equal(before, JsonSerializer.Serialize(request)); Assert.Equal(JsonSerializer.Serialize(a), JsonSerializer.Serialize(b)); Assert.Equal(campaign, Json(s));
        Assert.Empty(a.ShipChanges); Assert.Empty(a.FleetChanges); Assert.Equal(request.RandomState, a.FinalRandomState); Assert.Equal(CombatResolutionStatus.Resolved, a.Status);
        var e = Assert.Single(a.Events); Assert.Equal(CombatEventKind.EncounterResolved, e.Kind); Assert.Equal(0, e.Sequence);
    }
    [Fact]
    public void BufferAndFinalizeHaveExactStampsKeepLedgerAndLeaveStrategicPhasesUntouched()
    {
        var s = Session(CampaignCombatFixture.Scenario()); Assert.True(Enqueue(s).Accepted); var advanced = s.AdvanceTurn(1, 1); Assert.Equal(TurnStatus.AwaitingCombat, advanced.Status);
        var checkpoint = s.ExportSave(); var result = CampaignCombatFixture.NoChanges(s.GetPendingCombatRequests()[0]); var buffered = Submit(s, result);
        Assert.Equal(CombatSubmissionStatus.Buffered, buffered.Status); Assert.Equal(3, buffered.StateRevision); Assert.Equal(1, buffered.TurnNumber);
        Assert.Empty(s.GetPendingCombatRequests()); Assert.Single(s.GetResolvedCombats()); Assert.Single(s.CaptureSnapshot().AcceptedCommandIds); Assert.Equal(SessionPhase.AwaitingCombat, s.CaptureSnapshot().Phase);
        var before = Json(s); Assert.Equal(CombatSubmissionError.AlreadyResolved, Submit(s, result).ErrorCode); Assert.Equal(before, Json(s));
        result.Events.Clear(); var detached = s.GetResolvedCombats(); detached[0].Result.Events.Clear(); detached[0].Request.RandomState = 999; Assert.Equal(before, Json(s));
        Assert.Throws<NotSupportedException>(() => ((IList<ResolvedCombatDto>)detached).Clear());
        var completed = Finalize(s); Assert.Equal(TurnStatus.Completed, completed.Status); Assert.Equal(1, completed.CompletedTurnNumber); Assert.Equal(2, completed.TurnNumber); Assert.Equal(4, completed.StateRevision);
        Assert.Empty(s.CaptureSnapshot().AcceptedCommandIds); Assert.Empty(s.CaptureSnapshot().Combats); Assert.Empty(completed.Events); Assert.Empty(completed.CommandResults);
        Assert.Equal(checkpoint.GameState.NextEntitySequence, s.CaptureSnapshot().NextEntitySequence); Assert.Equal(checkpoint.GameState.RngState.State, s.CaptureSnapshot().RngState.State);
        Assert.Equal(8, Colony(s).ProductionQueue![0].CompletedIndustry); Assert.Equal(1005, PopulationRules.Population(Colony(s))); Assert.Equal(TurnErrorCode.NotAwaitingCombat, Finalize(s).ErrorCode);
    }
    [Theory]
    [InlineData("null", CombatSubmissionError.InvalidEncounterId)]
    [InlineData("bad-id", CombatSubmissionError.InvalidEncounterId)]
    [InlineData("unknown", CombatSubmissionError.UnknownEncounter)]
    [InlineData("resolver", CombatSubmissionError.ResolverMismatch)]
    [InlineData("version", CombatSubmissionError.ResolverMismatch)]
    [InlineData("status", CombatSubmissionError.MalformedResult)]
    [InlineData("ship-list", CombatSubmissionError.MalformedResult)]
    [InlineData("fleet-list", CombatSubmissionError.MalformedResult)]
    [InlineData("event-list", CombatSubmissionError.MalformedResult)]
    [InlineData("ship-null", CombatSubmissionError.MalformedResult)]
    [InlineData("fleet-null", CombatSubmissionError.MalformedResult)]
    [InlineData("nonparticipant-ship", CombatSubmissionError.Nonparticipant)]
    [InlineData("nonparticipant-fleet", CombatSubmissionError.Nonparticipant)]
    [InlineData("hull-negative", CombatSubmissionError.InvalidHull)]
    [InlineData("hull-above-frozen", CombatSubmissionError.InvalidHull)]
    [InlineData("duplicate-change", CombatSubmissionError.MalformedResult)]
    [InlineData("fleet-disposition", CombatSubmissionError.MalformedResult)]
    [InlineData("event-sequence", CombatSubmissionError.InvalidEvents)]
    [InlineData("event-kind", CombatSubmissionError.InvalidEvents)]
    [InlineData("event-reference", CombatSubmissionError.Nonparticipant)]
    [InlineData("event-null", CombatSubmissionError.InvalidEvents)]
    [InlineData("event-inconsistent", CombatSubmissionError.InvalidEvents)]
    [InlineData("terminal-missing", CombatSubmissionError.InvalidEvents)]
    [InlineData("terminal-duplicate", CombatSubmissionError.InvalidEvents)]
    [InlineData("retreat-unknown", CombatSubmissionError.InvalidRetreat)]
    [InlineData("retreat-self", CombatSubmissionError.InvalidRetreat)]
    [InlineData("retreat-not-adjacent", CombatSubmissionError.InvalidRetreat)]
    [InlineData("held-destination", CombatSubmissionError.InvalidRetreat)]
    [InlineData("empty-held", CombatSubmissionError.MalformedResult)]
    public void InvalidResultsRejectWithStableCodesAndByteIdenticalState(string error, CombatSubmissionError expected)
    {
        var s = Awaiting(); var request = s.GetPendingCombatRequests()[0]; CombatResultDto? result = CampaignCombatFixture.NoChanges(request); var ship = request.Participants[0].Ships[0].ShipId; var fleet = request.Participants[0].FleetId;
        switch (error)
        {
            case "null": result = null; break;
            case "bad-id": result.EncounterId = "Bad"; break;
            case "unknown": result.EncounterId = "encounter.unknown"; break;
            case "resolver": result.ResolverId = "resolver.other"; break;
            case "version": result.ResolverVersion++; break;
            case "status": result.Status = (CombatResolutionStatus)99; break;
            case "ship-list": result.ShipChanges = null!; break;
            case "fleet-list": result.FleetChanges = null!; break;
            case "event-list": result.Events = null!; break;
            case "ship-null": result.ShipChanges.Add(null!); break;
            case "fleet-null": result.FleetChanges.Add(null!); break;
            case "nonparticipant-ship": result.ShipChanges.Add(new() { ShipId = "ship.absent", RemainingHull = 1 }); break;
            case "nonparticipant-fleet": result.FleetChanges.Add(new() { FleetId = "fleet.absent" }); break;
            case "hull-negative": result.ShipChanges.Add(new() { ShipId = ship, RemainingHull = -1 }); break;
            case "hull-above-frozen": result.ShipChanges.Add(new() { ShipId = ship, RemainingHull = 11 }); break;
            case "duplicate-change": result.ShipChanges.Add(new() { ShipId = ship, RemainingHull = 1 }); result.ShipChanges.Add(result.ShipChanges[0]); break;
            case "fleet-disposition": result.FleetChanges.Add(new() { FleetId = fleet, Disposition = (FleetCombatDisposition)99 }); break;
            case "event-sequence": result.Events[0].Sequence = 1; break;
            case "event-kind": result.Events[0].Kind = (CombatEventKind)99; break;
            case "event-reference": result.Events[0].ShipId = "ship.absent"; break;
            case "event-null": result.Events.Add(null!); break;
            case "event-inconsistent": result.Events.Insert(0, new() { Sequence = 0, Kind = CombatEventKind.ShipDestroyed, ShipId = ship }); result.Events[1].Sequence = 1; break;
            case "terminal-missing": result.Events.Clear(); break;
            case "terminal-duplicate": result.Events.Add(new() { Sequence = 1, Kind = CombatEventKind.EncounterResolved }); break;
            case "retreat-unknown": result.FleetChanges.Add(new() { FleetId = fleet, Disposition = FleetCombatDisposition.Retreated, RetreatNodeId = "system.absent" }); break;
            case "retreat-self": result.FleetChanges.Add(new() { FleetId = fleet, Disposition = FleetCombatDisposition.Retreated, RetreatNodeId = request.LocationNodeId }); break;
            case "retreat-not-adjacent": result.FleetChanges.Add(new() { FleetId = fleet, Disposition = FleetCombatDisposition.Retreated, RetreatNodeId = "system.gamma-exit" }); break;
            case "held-destination": result.FleetChanges.Add(new() { FleetId = fleet, Disposition = FleetCombatDisposition.Held, RetreatNodeId = "system.sol" }); break;
            case "empty-held": result.ShipChanges.Add(new() { ShipId = ship, RemainingHull = 0 }); result.FleetChanges.Add(new() { FleetId = fleet, Disposition = FleetCombatDisposition.Held }); break;
        }
        var before = Json(s); Assert.Equal(expected, Submit(s, result).ErrorCode); Assert.Equal(before, Json(s));
    }
    [Fact]
    public void DeferredAndConcurrencyErrorsNeverMutateTheBatch()
    {
        var s = Awaiting(); var request = s.GetPendingCombatRequests()[0]; var result = CampaignCombatFixture.NoChanges(request); result.Status = CombatResolutionStatus.Deferred; result.Events.Clear(); var before = Json(s);
        Assert.Equal(CombatSubmissionStatus.Deferred, Submit(s, result).Status); Assert.Equal(before, Json(s)); result.FinalRandomState++; Assert.Equal(CombatSubmissionError.MalformedResult, Submit(s, result).ErrorCode); Assert.Equal(before, Json(s));
        var p = s.CaptureSnapshot(); Assert.Equal(CombatSubmissionError.StaleTurn, s.SubmitCombatResult(result, 0, p.StateRevision).ErrorCode); Assert.Equal(CombatSubmissionError.StaleRevision, s.SubmitCombatResult(result, p.TurnNumber, 0).ErrorCode);
        Assert.Equal(TurnErrorCode.StaleTurn, s.FinalizeCombat(0, p.StateRevision).ErrorCode); Assert.Equal(TurnErrorCode.StaleRevision, s.FinalizeCombat(p.TurnNumber, 0).ErrorCode); Assert.Equal(before, Json(s));
        Assert.Equal(CombatSubmissionError.WrongPhase, Submit(Session(), result).ErrorCode);
    }
    [Theory]
    [InlineData("damage")]
    [InlineData("ship-destroyed")]
    [InlineData("fleet-destroyed")]
    public void ScriptedConsequencesApplyOnlyAtFinalizationAndRemoveEmptyMembership(string effect)
    {
        var s = Awaiting(); var request = s.GetPendingCombatRequests()[0]; var result = CampaignCombatFixture.NoChanges(request); var participant = request.Participants[0]; var ship = participant.Ships[0];
        if (effect == "fleet-destroyed") result.FleetChanges.Add(new() { FleetId = participant.FleetId, Disposition = FleetCombatDisposition.Destroyed });
        else result.ShipChanges.Add(new() { ShipId = ship.ShipId, RemainingHull = effect == "damage" ? 7 : 0 });
        var frozen = s.CaptureSnapshot(); Assert.Equal(CombatSubmissionStatus.Buffered, Submit(s, result).Status); Assert.Equal(10, s.CaptureSnapshot().Ships.Single(x => x.ShipId.Value == ship.ShipId).CurrentHull);
        Assert.Equal(TurnStatus.Completed, Finalize(s).Status);
        if (effect == "damage") Assert.Equal(7, s.CaptureSnapshot().Ships.Single(x => x.ShipId.Value == ship.ShipId).CurrentHull);
        else { Assert.DoesNotContain(s.CaptureSnapshot().Ships, x => x.ShipId.Value == ship.ShipId); Assert.DoesNotContain(s.CaptureSnapshot().Fleets, f => f.FleetId.Value == participant.FleetId); Assert.DoesNotContain(s.CaptureSnapshot().Entities, e => e.EntityId.Value == ship.ShipId || e.EntityId.Value == participant.FleetId); }
        Assert.Equal(frozen.RngState.State, s.CaptureSnapshot().RngState.State); Assert.Equal(frozen.NextEntitySequence, s.CaptureSnapshot().NextEntitySequence);
    }
    [Fact]
    public void RetreatClearsRouteHeldKeepsRouteAndFinalizationDoesNotCreateAnotherEncounter()
    {
        var scenario = CampaignCombatFixture.Scenario(); foreach (var f in scenario.InitialState.Fleets!) f.RouteNodeIds = new() { "system.bajoran-wormhole", "system.gamma-exit" };
        var s = Session(scenario); s.AdvanceTurn(1, 0); var request = s.GetPendingCombatRequests()[0]; Assert.Equal("system.bajor", request.LocationNodeId);
        var result = CampaignCombatFixture.NoChanges(request); result.FleetChanges.Add(new() { FleetId = request.Participants[0].FleetId, Disposition = FleetCombatDisposition.Retreated, RetreatNodeId = "system.sol" });
        result.FleetChanges.Add(new() { FleetId = request.Participants[1].FleetId, Disposition = FleetCombatDisposition.Held }); Assert.Equal(CombatSubmissionStatus.Buffered, Submit(s, result).Status); Finalize(s);
        var fleets = s.CaptureSnapshot().Fleets; var retreat = fleets.Single(f => f.FleetId.Value == request.Participants[0].FleetId); Assert.Equal("system.sol", retreat.LocationNodeId.Value); Assert.Empty(retreat.RouteNodeIds);
        var held = fleets.Single(f => f.FleetId.Value == request.Participants[1].FleetId); Assert.Equal("system.bajor", held.LocationNodeId.Value); Assert.Equal(new[] { new EntityId("system.bajoran-wormhole"), new EntityId("system.gamma-exit") }, held.RouteNodeIds);
        Assert.Empty(s.GetPendingCombatRequests()); Assert.Equal(SessionPhase.Planning, s.CaptureSnapshot().Phase);
    }
    [Theory]
    [InlineData("partial-throw")]
    [InlineData("final-invariant")]
    [InlineData("valid-probe-mutation")]
    public void FinalizationFaultAfterDamageRollsBackTheEntireResolvedCheckpoint(string fault)
    {
        var scenario = CampaignCombatFixture.Scenario(3); var failed = true;
        var s = new SimulationSession(Save(scenario), scenario.Definitions, Compatibility(scenario), new Dictionary<CommandKind, ICommandHandler>(), combatApplicationProbe: (index, staging) =>
        {
            if (failed && fault == "partial-throw" && index == 1) { Assert.Equal(2, staging.Ships!.Count(ship => ship.CurrentHull == 7)); throw new ApplicationException("After second encounter."); }
        }, combatFinalizationProbe: staging => { if (failed && fault == "final-invariant") staging.Ships![0].CurrentHull = 0; if (failed && fault == "valid-probe-mutation") staging.Colonies![0].Food++; });
        s.AdvanceTurn(1, 0);
        foreach (var request in s.GetPendingCombatRequests()) { var r = CampaignCombatFixture.NoChanges(request); r.ShipChanges.Add(new() { ShipId = request.Participants[0].Ships[0].ShipId, RemainingHull = 7 }); Assert.Equal(CombatSubmissionStatus.Buffered, Submit(s, r).Status); }
        var before = Json(s);
        if (fault == "partial-throw") Assert.Throws<ApplicationException>(() => Finalize(s)); else if (fault == "final-invariant") Assert.Throws<ArgumentException>(() => Finalize(s)); else Assert.Throws<InvalidOperationException>(() => Finalize(s));
        Assert.Equal(before, Json(s)); failed = false; Assert.Equal(TurnStatus.Completed, Finalize(s).Status); Assert.Equal(3, s.CaptureSnapshot().Ships.Count(ship => ship.CurrentHull == 7));
    }
    [Fact]
    public void RetreatIntoAnotherEncounterOrForeignOccupiedSnapshotIsRejected()
    {
        var s = Awaiting(3); var request = s.GetPendingCombatRequests()[0]; var result = CampaignCombatFixture.NoChanges(request);
        result.FleetChanges.Add(new() { FleetId = request.Participants[0].FleetId, Disposition = FleetCombatDisposition.Retreated, RetreatNodeId = "system.bajoran-wormhole" });
        var before = Json(s); Assert.Equal(CombatSubmissionError.InvalidRetreat, Submit(s, result).ErrorCode); Assert.Equal(before, Json(s));
        var scenario = CampaignCombatFixture.Scenario(2); scenario.InitialState.Fleets!.Where(f => f.LocationNodeId == "system.bajoran-wormhole").ToList().ForEach(f => f.LocationNodeId = "system.sol");
        // Both foreign fleets at Sol form another frozen battle; the retreat is still invalid.
        s = Session(scenario); s.AdvanceTurn(1, 0); request = s.GetPendingCombatRequests().Single(r => r.LocationNodeId == "system.bajor"); result = CampaignCombatFixture.NoChanges(request);
        result.FleetChanges.Add(new() { FleetId = request.Participants[0].FleetId, Disposition = FleetCombatDisposition.Retreated, RetreatNodeId = "system.sol" });
        before = Json(s); Assert.Equal(CombatSubmissionError.InvalidRetreat, Submit(s, result).ErrorCode); Assert.Equal(before, Json(s));
    }
    [Fact]
    public void ExhaustedRevisionBlocksBufferingAndFinalizationButNotPureDeferral()
    {
        var original = Awaiting(); var scenario = CampaignCombatFixture.Scenario(); var save = original.ExportSave(); save.GameState.StateRevision = long.MaxValue;
        var s = new SimulationSession(save, scenario.Definitions, Compatibility(scenario)); var request = s.GetPendingCombatRequests()[0]; var result = CampaignCombatFixture.NoChanges(request); var before = Json(s);
        Assert.Equal(CombatSubmissionError.RevisionExhausted, Submit(s, result).ErrorCode); Assert.Equal(before, Json(s));
        result.Status = CombatResolutionStatus.Deferred; result.Events.Clear(); Assert.Equal(CombatSubmissionStatus.Deferred, Submit(s, result).Status); Assert.Equal(before, Json(s));
        Assert.Equal(CombatSubmissionStatus.Buffered, Submit(original, CampaignCombatFixture.NoChanges(request)).Status);
        save = original.ExportSave(); save.GameState.StateRevision = long.MaxValue; s = new SimulationSession(save, scenario.Definitions, Compatibility(scenario)); before = Json(s);
        Assert.Equal(TurnErrorCode.RevisionExhausted, Finalize(s).ErrorCode); Assert.Equal(before, Json(s));
    }
    [Fact]
    public void ReloadRejectsBufferedRetreatThatDoesNotMatchGalaxyTopology()
    {
        var s = Awaiting(); var r = CampaignCombatFixture.NoChanges(s.GetPendingCombatRequests()[0]); Assert.Equal(CombatSubmissionStatus.Buffered, Submit(s, r).Status);
        var save = s.ExportSave(); save.GameState.ResolvedCombats![0].Result.FleetChanges.Add(new() { FleetId = save.GameState.ResolvedCombats[0].Request.Participants[0].FleetId, Disposition = FleetCombatDisposition.Retreated, RetreatNodeId = "system.founders-homeworld" });
        var scenario = CampaignCombatFixture.Scenario(); Assert.Throws<ArgumentException>(() => new SimulationSession(save, scenario.Definitions, Compatibility(scenario)));
    }

    [Fact]
    public void OpposingFleetsCannotRetreatIntoTheSameDestinationWithinOneResult()
    {
        var s = Awaiting(); var request = s.GetPendingCombatRequests()[0]; var result = CampaignCombatFixture.NoChanges(request);
        foreach (var participant in request.Participants) result.FleetChanges.Add(new() { FleetId = participant.FleetId, Disposition = FleetCombatDisposition.Retreated, RetreatNodeId = "system.sol" });
        var before = Json(s); Assert.Equal(CombatSubmissionError.InvalidRetreat, Submit(s, result).ErrorCode); Assert.Equal(before, Json(s));
    }

}
