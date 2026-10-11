using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.PhasedCombatFixture;
using V = HobbyDS9.Simulation.Tests.VerticalSliceFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class PhasedCampaignIntegrationTests
{
    private static SimulationSession Awaiting(ScenarioDto scenario)
    {
        var s = V.Session(scenario); Assert.Equal(TurnStatus.AwaitingCombat, s.AdvanceTurn(1, 0).Status); return s;
    }
    private static void Submit(SimulationSession s, CombatResultDto result)
    {
        var p = s.CaptureSnapshot(); Assert.Equal(CombatSubmissionStatus.Buffered, s.SubmitCombatResult(result, p.TurnNumber, p.StateRevision).Status);
    }
    private static void Finish(SimulationSession s)
    {
        var p = s.CaptureSnapshot(); Assert.Equal(TurnStatus.Completed, s.FinalizeCombat(p.TurnNumber, p.StateRevision).Status); Assert.Empty(s.CaptureSnapshot().Combats);
    }
    private static SimulationSession Reload(SimulationSession s, ScenarioDto scenario)
    {
        var before = V.Json(s); var save = Clone(s.ExportSave()); save.GameState.PendingCombats!.Reverse(); save.GameState.ResolvedCombats!.Reverse();
        foreach (var request in save.GameState.PendingCombats.Select(c => c.Request).Concat(save.GameState.ResolvedCombats.Select(c => c.Request)))
        { request.Participants.Reverse(); foreach (var participant in request.Participants) participant.Ships.Reverse(); }
        var loaded = new SimulationSession(save, Clone(scenario.Definitions), V.Compatibility(scenario)); Assert.Equal(before, V.Json(loaded)); return loaded;
    }
    [Theory]
    [InlineData(0, 1, 2)] [InlineData(0, 2, 1)] [InlineData(1, 0, 2)] [InlineData(1, 2, 0)] [InlineData(2, 0, 1)] [InlineData(2, 1, 0)]
    public void RealResultsMatchAcrossEveryPermutationAndEveryPersistedCombatPoint(int a, int b, int c)
    {
        var scenario = CampaignCombatFixture.Scenario(3, true); var baseline = Awaiting(scenario); var checkpoint = baseline.ExportSave(); var resolver = new DeterministicPhasedCombatResolver();
        var requests = baseline.GetPendingCombatRequests(); var results = requests.Select(resolver.Resolve).ToArray();
        Assert.Contains(results.SelectMany(r => r.ShipChanges), change => change.RemainingHull < 10);
        Assert.Contains(results.SelectMany(r => r.Events), e => e.Kind == CombatEventKind.PhaseStarted);
        foreach (var result in results) Submit(baseline, result); var ready = V.Json(baseline); Finish(baseline); var completed = V.Json(baseline);
        var resumed = Reload(Awaiting(scenario), scenario); Assert.Equal(Json(requests), Json(resumed.GetPendingCombatRequests()));
        foreach (var index in new[] { a, b, c })
        {
            // Resolve only pending requests. Already-buffered results survive reload without replay.
            var pending = resumed.GetPendingCombatRequests().Single(r => r.EncounterId == requests[index].EncounterId);
            var before = V.Json(resumed); var result = resolver.Resolve(pending); Assert.Equal(before, V.Json(resumed)); Assert.Equal(Json(results[index]), Json(result)); Submit(resumed, result);
            Assert.Equal(Json(checkpoint.GameState.Ships), Json(resumed.ExportSave().GameState.Ships));
            Assert.Equal(checkpoint.GameState.RngState.State, resumed.CaptureSnapshot().RngState.State); Assert.Equal(checkpoint.GameState.NextEntitySequence, resumed.CaptureSnapshot().NextEntitySequence);
            resumed = Reload(resumed, scenario); var saved = resumed.GetResolvedCombats().Single(r => r.Request.EncounterId == result.EncounterId);
            Assert.Equal(Json(result), Json(saved.Result)); Assert.Equal(Json(requests[index]), Json(saved.Request)); Assert.DoesNotContain(resumed.GetPendingCombatRequests(), r => r.EncounterId == result.EncounterId);
        }
        Assert.Equal(ready, V.Json(resumed)); Finish(resumed); Assert.Equal(completed, V.Json(resumed));
    }
    [Theory]
    [InlineData("system.sol", true)] [InlineData("system.gamma-exit", false)] [InlineData("system.bajor", false)] [InlineData("system.missing", false)]
    public void ActualPlannedRetreatStillPassesThroughCampaignTopologyValidation(string destination, bool legal)
    {
        var s = Awaiting(CampaignCombatFixture.Scenario()); var r = s.GetPendingCombatRequests()[0]; var plans = Plans(r); plans[0].RetreatHullPercent = 100; plans[0].RetreatNodeId = destination;
        var result = new DeterministicPhasedCombatResolver().Resolve(r, plans); var retreat = Assert.Single(result.FleetChanges); Assert.Equal(FleetCombatDisposition.Retreated, retreat.Disposition);
        var before = V.Json(s); var p = s.CaptureSnapshot(); var submitted = s.SubmitCombatResult(result, p.TurnNumber, p.StateRevision);
        if (!legal) { Assert.Equal(CombatSubmissionError.InvalidRetreat, submitted.ErrorCode); Assert.Equal(before, V.Json(s)); }
        else { Assert.Equal(CombatSubmissionStatus.Buffered, submitted.Status); Finish(s); var fleet = s.CaptureSnapshot().Fleets.Single(f => f.FleetId.Value == retreat.FleetId); Assert.Equal(destination, fleet.LocationNodeId.Value); Assert.Empty(fleet.RouteNodeIds); }
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RealDamageFinalizationFailurePreservesAllDetailedBufferedResults(bool invariantFailure)
    {
        var scenario = CampaignCombatFixture.Scenario(3, true); var fail = true;
        var s = new SimulationSession(V.Save(scenario), scenario.Definitions, V.Compatibility(scenario), new Dictionary<CommandKind, ICommandHandler>(),
            combatApplicationProbe: (index, state) => { if (fail && !invariantFailure && index == 1) throw new ApplicationException("Second battle failed."); },
            combatFinalizationProbe: state => { if (fail && invariantFailure) state.NextEntitySequence = 0; });
        s.AdvanceTurn(1, 0); foreach (var r in s.GetPendingCombatRequests()) Submit(s, new DeterministicPhasedCombatResolver().Resolve(r)); var before = V.Json(s);
        if (invariantFailure) Assert.Throws<ArgumentException>(() => Finish(s)); else Assert.Throws<ApplicationException>(() => Finish(s));
        Assert.Equal(before, V.Json(s)); fail = false; var resumed = Reload(s, scenario); Finish(s); Finish(resumed); Assert.Equal(V.Json(s), V.Json(resumed));
    }
    [Fact]
    public void ResolverFailureOrNoLossIdentityCannotMutatePhasedCampaign()
    {
        var s = Awaiting(CampaignCombatFixture.Scenario()); var r = s.GetPendingCombatRequests()[0]; var before = V.Json(s); var plans = Plans(r); plans[0].LongRangeTactic = (CombatTactic)99;
        Assert.Throws<ArgumentException>(() => new DeterministicPhasedCombatResolver().Resolve(r, plans)); Assert.Equal(before, V.Json(s));
        r.ResolverId = NoLossCombatResolver.ResolverId; var result = new NoLossCombatResolver().Resolve(r); var p = s.CaptureSnapshot();
        Assert.Equal(CombatSubmissionError.ResolverMismatch, s.SubmitCombatResult(result, p.TurnNumber, p.StateRevision).ErrorCode); Assert.Equal(before, V.Json(s));
    }
    [Fact]
    public void AiCanRebuildFromNoFleetsAndHandleDamagedSurvivorsWithoutPolicyChanges()
    {
        var scenario = V.Scenario(false); var emptyActor = "civ.cardassian";
        var removed = scenario.InitialState.Ships!.Where(s => s.OwnerCivilizationId == emptyActor).Select(s => s.ShipId).Concat(scenario.InitialState.Fleets!.Where(f => f.OwnerCivilizationId == emptyActor).Select(f => f.FleetId)).ToHashSet(StringComparer.Ordinal);
        scenario.InitialState.Ships!.RemoveAll(s => s.OwnerCivilizationId == emptyActor); scenario.InitialState.Fleets!.RemoveAll(f => f.OwnerCivilizationId == emptyActor); scenario.InitialState.EntityIdentities.RemoveAll(e => removed.Contains(e.EntityId));
        scenario.InitialState.Ships!.Single(s => s.OwnerCivilizationId == "civ.dominion").CurrentHull = 1;
        var s = V.Session(scenario); var built = false;
        for (var i = 0; i < 6; i++)
        {
            var stamp = s.CaptureSnapshot(); var turn = s.AdvanceTurn(stamp.TurnNumber, stamp.StateRevision); Assert.All(turn.CommandResults, c => Assert.True(c.Accepted));
            if (turn.Status == TurnStatus.AwaitingCombat) { foreach (var r in s.GetPendingCombatRequests()) Submit(s, new DeterministicPhasedCombatResolver().Resolve(r)); Finish(s); }
            else Assert.Equal(TurnStatus.Completed, turn.Status);
            built |= s.CaptureSnapshot().Ships.Any(ship => ship.OwnerCivilizationId.Value == emptyActor);
        }
        Assert.True(built); Assert.True(FoundationValidator.ValidateSave(s.ExportSave(), scenario.Definitions, V.Compatibility(scenario)).IsValid);
    }
}
