using System.Numerics;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using HobbyDS9.UnityClient;
using Xunit;
using static HobbyDS9.Simulation.Tests.PhasedCombatFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class CombatControlReportTests
{
    private static readonly EntityId Human = new("civ.federation");
    private static (ScenarioDto Scenario, ICampaignSession App) Pending(int battles = 1)
    {
        var scenario = CampaignCombatFixture.Scenario(battles, true);
        var staging = CampaignApplication.StartScenario(scenario);
        Assert.Equal(TurnStatus.AwaitingCombat, staging.AdvanceTurn(1, 0).Status);
        // Controllers are persisted authority, not host-supplied UI permissions.
        var save = staging.ExportSave();
        foreach (var actor in save.GameState.Civilizations!) actor.ControllerKind = actor.CivilizationId == Human.Value ? ControllerKind.Human : ControllerKind.AI;
        return (scenario, CampaignApplication.LoadSave(scenario, save));
    }
    [Fact]
    public void WeightedScenarioFixtureSupportsTwoThreePartyCheckpointsThroughRealApplication()
    {
        var scenario = VerticalSliceFixture.Scenario(true, true); var app = CampaignApplication.StartScenario(scenario);
        var save = app.ExportSave();
        foreach (var fleet in save.GameState.Fleets!)
        { fleet.LocationNodeId = "nav.bajor"; fleet.RouteNodeIds!.Clear(); fleet.TransitTotalCost = 0; fleet.TransitProgress = 0; }
        foreach (var original in save.GameState.Fleets.ToArray())
        {
            var source = save.GameState.Ships!.Single(s => s.FleetId == original.FleetId);
            var fleetId = original.FleetId + "-second"; var shipId = source.ShipId + "-second";
            save.GameState.Fleets.Add(new() { FleetId = fleetId, OwnerCivilizationId = original.OwnerCivilizationId,
                LocationNodeId = "nav.gamma-exit", ShipIds = new() { shipId }, RouteNodeIds = new() });
            save.GameState.Ships!.Add(new() { ShipId = shipId, FleetId = fleetId, OwnerCivilizationId = original.OwnerCivilizationId,
                ShipDesignDefinitionId = source.ShipDesignDefinitionId, CurrentHull = source.CurrentHull });
            save.GameState.EntityIdentities.Add(new() { EntityId = fleetId });
            save.GameState.EntityIdentities.Add(new() { EntityId = shipId, DefinitionId = source.ShipDesignDefinitionId });
        }
        app = CampaignApplication.LoadSave(scenario, save);
        Assert.Equal(TurnStatus.AwaitingCombat, app.AdvanceTurn(1, 0).Status);
        save = app.ExportSave();
        foreach (var actor in save.GameState.Civilizations!) actor.ControllerKind = actor.CivilizationId == Human.Value ? ControllerKind.Human : ControllerKind.AI;
        app = CampaignApplication.LoadSave(scenario, save);
        Assert.Equal(2, app.GetPendingCombatRequests().Count);
        Assert.All(app.GetPendingCombatRequests(), r => Assert.Equal(3, r.Participants.Select(p => p.CivilizationId).Distinct().Count()));
        foreach (var request in app.GetPendingCombatRequests())
        {
            var stamp = app.CaptureSnapshot().Campaign;
            Assert.Equal(CombatSubmissionStatus.Buffered, app.SubmitCombatResult(app.ResolvePlayerCombat(request.EncounterId, Human), stamp.TurnNumber, stamp.StateRevision).Status);
        }
        Assert.Equal(2, app.GetBattleReports().Count);
    }
    [Fact]
    public void PlayerResolutionRetainsObservedStampsAndRejectedSubmissionIsAtomic()
    {
        var (scenario, app) = Pending(2); var client = new StrategicClient(app, save => CampaignApplication.LoadSave(scenario, save));
        var observed = client.Snapshot; var requests = client.Pending();
        Assert.Equal(CombatSubmissionStatus.Buffered, client.Resolve(observed, requests[0]).Status);
        var before = Json(client.ExportSave());
        Assert.Equal(CombatSubmissionError.StaleRevision, client.Resolve(observed, requests[1]).ErrorCode);
        Assert.Equal(before, Json(client.ExportSave())); Assert.Single(client.Reports()); Assert.Single(client.Pending());
        Assert.Equal(CombatSubmissionStatus.Buffered, client.Resolve(client.Snapshot, requests[1]).Status);
        before = Json(client.ExportSave());
        Assert.Equal(TurnErrorCode.StaleRevision, client.FinalizeCombats(observed).ErrorCode);
        Assert.Equal(before, Json(client.ExportSave())); Assert.Equal(2, client.Reports().Count);
        Assert.Equal(TurnStatus.Completed, client.FinalizeCombats(client.Snapshot).Status);
    }
    [Fact]
    public void PlayerCannotOverrideAiUnknownOrDuplicateFleetPlansOrActAsAi()
    {
        var (_, app) = Pending(); var request = app.GetPendingCombatRequests().Single(); var before = Json(app.ExportSave());
        var own = Assert.Single(app.GetPlayerCombatPlans(request.EncounterId, Human));
        var foreign = DeterministicPhasedCombatResolver.CreateDefaultPlans(request).First(p => p.FleetId != own.FleetId);
        foreign.CloseRangeTactic = CombatTactic.Evasive;
        Assert.Throws<ArgumentException>(() => app.ResolvePlayerCombat(request.EncounterId, Human, new[] { foreign }));
        foreign.FleetId = "fleet.unknown";
        Assert.Throws<ArgumentException>(() => app.ResolvePlayerCombat(request.EncounterId, Human, new[] { foreign }));
        Assert.Throws<ArgumentException>(() => app.ResolvePlayerCombat(request.EncounterId, Human, new[] { own, own }));
        Assert.Throws<ArgumentException>(() => app.ResolvePlayerCombat(request.EncounterId, new EntityId("civ.dominion")));
        Assert.Throws<ArgumentException>(() => app.GetPlayerCombatPlans(request.EncounterId, new EntityId("civ.dominion")));
        own.LongRangeTactic = (CombatTactic)999;
        Assert.Throws<ArgumentException>(() => app.ResolvePlayerCombat(request.EncounterId, Human, new[] { own }));
        Assert.Equal(before, Json(app.ExportSave()));
    }
    [Fact]
    public void HumanOverridesComposeWithExistingV1AiDefaultsWithoutConsumingCampaignState()
    {
        var (_, app) = Pending(); var request = app.GetPendingCombatRequests().Single(); var before = Json(app.ExportSave());
        var defaults = app.ResolveCombat(request);
        Assert.Equal(Json(defaults), Json(app.ResolvePlayerCombat(request.EncounterId, Human)));
        var own = Assert.Single(app.GetPlayerCombatPlans(request.EncounterId, Human)); own.LongRangeTactic = CombatTactic.Evasive;
        var composed = DeterministicPhasedCombatResolver.CreateDefaultPlans(request);
        composed.Single(p => p.FleetId == own.FleetId).LongRangeTactic = CombatTactic.Evasive;
        Assert.Equal(Json(app.ResolveCombat(request, composed)), Json(app.ResolvePlayerCombat(request.EncounterId, Human, new[] { own })));
        Assert.Equal(CombatTactic.FocusFire, Assert.Single(app.GetPlayerCombatPlans(request.EncounterId, Human)).LongRangeTactic);
        Assert.Equal(before, Json(app.ExportSave()));
    }
    [Fact]
    public void StrategicClientResolvesFrozenRequestInsteadOfCallerMutatedShipData()
    {
        var (scenario, app) = Pending(); var request = app.GetPendingCombatRequests().Single();
        var expected = app.ResolveCombat(request); var client = new StrategicClient(app, save => CampaignApplication.LoadSave(scenario, save));
        request.Participants[0].Ships[0].Hull = 1; request.RandomState = 0;
        Assert.Equal(CombatSubmissionStatus.Buffered, client.Resolve(client.Snapshot, request).Status);
        Assert.Equal(Json(expected), Json(app.GetResolvedCombats().Single().Result));
    }
    [Fact]
    public void ThreeCivilizationsRemainDeterministicFreeForAllRegardlessOfInputOrder()
    {
        var request = Request(3); var plans = Plans(request); var resolver = new DeterministicPhasedCombatResolver();
        var result = resolver.Resolve(request, plans);
        var shots = result.Events.Where(e => e.Kind == CombatEventKind.AttackHit).ToArray();
        Assert.Equal(9, shots.Length);
        var ownership = request.Participants.ToDictionary(p => p.FleetId, p => p.CivilizationId);
        Assert.All(shots, e => Assert.NotEqual(ownership[e.SourceFleetId!], ownership[e.FleetId!]));
        Assert.Equal(3, shots.Select(e => ownership[e.SourceFleetId!]).Distinct().Count());
        Assert.Equal(new long[] { 40, 70 }, result.ShipChanges.Select(s => s.RemainingHull)); // third ship unchanged, not allied
        Assert.Contains(shots, e => e.SourceFleetId == request.Participants[2].FleetId && e.FleetId == request.Participants[0].FleetId);
        request.Participants.Reverse(); plans.Reverse();
        Assert.Equal(Json(result), Json(resolver.Resolve(request, plans)));
        Assert.Equal(CampaignCombatRules.ResolverVersion, result.ResolverVersion);
    }
    [Fact]
    public void ReportProjectsHullDispositionDamageAndOrderedPhasesWithoutDtoReferences()
    {
        var request = Request(3); var p = request.Participants;
        var result = new CombatResultDto
        {
            ShipChanges = new() { new() { ShipId = p[0].Ships[0].ShipId, RemainingHull = 0 }, new() { ShipId = p[1].Ships[0].ShipId, RemainingHull = 80 } },
            FleetChanges = new() { new() { FleetId = p[0].FleetId, Disposition = FleetCombatDisposition.Destroyed }, new() { FleetId = p[1].FleetId, Disposition = FleetCombatDisposition.Retreated, RetreatNodeId = "node.safe" } },
            Events = new()
            {
                new() { Sequence = 0, Kind = CombatEventKind.PhaseStarted, Phase = CombatPhaseKind.LongRange },
                new() { Sequence = 1, Kind = CombatEventKind.AttackHit, Phase = CombatPhaseKind.LongRange, SourceFleetId = p[2].FleetId, SourceShipId = p[2].Ships[0].ShipId, FleetId = p[0].FleetId, ShipId = p[0].Ships[0].ShipId },
                new() { Sequence = 2, Kind = CombatEventKind.ShieldDamaged, Phase = CombatPhaseKind.LongRange, Amount = 5 },
                new() { Sequence = 3, Kind = CombatEventKind.ShipDamaged, Phase = CombatPhaseKind.LongRange, Amount = 100 },
                new() { Sequence = 4, Kind = CombatEventKind.ShipDestroyed, Phase = CombatPhaseKind.LongRange, ShipId = p[0].Ships[0].ShipId },
                new() { Sequence = 5, Kind = CombatEventKind.AttackMissed, Phase = CombatPhaseKind.MediumRange },
                new() { Sequence = 6, Kind = CombatEventKind.ShipDamaged, Phase = CombatPhaseKind.CloseRange, Amount = 20 },
                new() { Sequence = 7, Kind = CombatEventKind.FleetRetreated, Phase = CombatPhaseKind.CloseRange, FleetId = p[1].FleetId }
            }
        };
        var report = new BattleReportSnapshot(new() { Request = request, Result = result });
        Assert.Equal(new BigInteger(5), report.ShieldDamage); Assert.Equal(new BigInteger(120), report.HullDamage); Assert.Equal(1, report.DestroyedShips);
        Assert.Equal(FleetCombatDisposition.Destroyed, report.Fleets[0].Disposition);
        Assert.Equal("node.safe", report.Fleets[1].RetreatNodeId); Assert.Equal(FleetCombatDisposition.Held, report.Fleets[2].Disposition);
        Assert.Equal(100, report.Fleets[2].Ships[0].RemainingHull); Assert.Equal(100, report.Fleets[0].Ships[0].StartingHull);
        Assert.Equal(new[] { CombatPhaseKind.LongRange, CombatPhaseKind.MediumRange, CombatPhaseKind.CloseRange }, report.Phases.Select(p => p.Phase));
        Assert.Equal(2, report.Shots); Assert.Equal(1, report.Hits); Assert.Equal(1, report.Misses); Assert.Equal(1, report.PhasesFought);
        Assert.Equal(1, report.Phases[0].Hits); Assert.Equal(1, report.Phases[1].Misses); Assert.Equal(CombatEventKind.FleetRetreated, report.Phases[2].Events.Last().Kind);
        var ordered = report.Phases.SelectMany(p => p.Events).Select(e => e.Sequence).ToArray();
        result.Events.Reverse(); request.Participants.Reverse();
        Assert.Equal(ordered, new BattleReportSnapshot(new() { Request = request, Result = result }).Phases.SelectMany(p => p.Events).Select(e => e.Sequence));
        request.Participants.Clear(); result.Events[0].Amount = 99; result.ShipChanges[0].RemainingHull = 50; result.FleetChanges.Clear();
        Assert.Equal(0, report.Fleets[0].Ships[0].RemainingHull); Assert.Equal(120, report.HullDamage); Assert.Equal(FleetCombatDisposition.Destroyed, report.Fleets[0].Disposition);
        Assert.Throws<NotSupportedException>(() => ((IList<BattleFleetReport>)report.Fleets).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<BattleEventSnapshot>)report.Phases[0].Events).Clear());
    }
    [Fact]
    public void ReportDamageTotalsDoNotOverflowWhenMultipleShipsHaveInt64Damage()
    {
        var data = new ResolvedCombatDto { Request = Request() };
        data.Result.Events = new() { new() { Kind = CombatEventKind.ShipDamaged, Amount = long.MaxValue }, new() { Kind = CombatEventKind.ShipDamaged, Amount = long.MaxValue } };
        Assert.Equal((BigInteger)long.MaxValue * 2, new BattleReportSnapshot(data).HullDamage);
    }
    [Fact]
    public void BufferedReportsSurviveReloadAndOtherPendingEncountersUntilExplicitFinalization()
    {
        var (scenario, app) = Pending(2); var requests = app.GetPendingCombatRequests();
        var result = app.ResolvePlayerCombat(requests[0].EncounterId, Human); var stamp = app.CaptureSnapshot().Campaign;
        Assert.Equal(CombatSubmissionStatus.Buffered, app.SubmitCombatResult(result, stamp.TurnNumber, stamp.StateRevision).Status);
        var before = Json(app.ExportSave()); var report = Assert.Single(app.GetBattleReports());
        Assert.Single(app.GetPendingCombatRequests()); Assert.Equal(before, Json(app.ExportSave()));
        var copy = app.GetResolvedCombats().Single(); copy.Result.Events.Clear(); copy.Request.Participants.Clear();
        Assert.NotEmpty(app.GetBattleReports().Single().Phases.SelectMany(p => p.Events));
        var reloaded = CampaignApplication.LoadSave(scenario, app.ExportSave());
        Assert.Equal(Json(report), Json(Assert.Single(reloaded.GetBattleReports())));
        var pending = reloaded.GetPendingCombatRequests().Single(); stamp = reloaded.CaptureSnapshot().Campaign;
        Assert.Equal(CombatSubmissionStatus.Buffered, reloaded.SubmitCombatResult(reloaded.ResolvePlayerCombat(pending.EncounterId, Human), stamp.TurnNumber, stamp.StateRevision).Status);
        Assert.Equal(2, reloaded.GetBattleReports().Count); Assert.Empty(reloaded.GetPendingCombatRequests());
        Assert.Equal(SessionPhase.AwaitingCombat, reloaded.CaptureSnapshot().Campaign.Phase);
        Assert.Throws<ArgumentException>(() => reloaded.ResolvePlayerCombat(requests[0].EncounterId, Human));
        stamp = reloaded.CaptureSnapshot().Campaign;
        Assert.Equal(TurnStatus.Completed, reloaded.FinalizeCombat(stamp.TurnNumber, stamp.StateRevision).Status);
        Assert.Empty(reloaded.GetBattleReports()); Assert.NotEmpty(report.Fleets);
    }
    [Fact]
    public void HeadingNamesAllCivilizationsWithoutImplicitSidesOrAlliances()
    {
        var (_, app) = Pending(); var snapshot = app.CaptureSnapshot();
        var actors = app.GetPendingCombatRequests().Single().Participants.Select(p => p.CivilizationId);
        Assert.Equal("3-way engagement: Federation vs Cardassian Union vs Dominion", CombatPresentation.Heading(snapshot, actors.Concat(actors).Reverse()));
    }
}
