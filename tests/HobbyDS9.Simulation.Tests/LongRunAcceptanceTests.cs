using System.Text.Json;
using System.Security.Cryptography;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.VerticalSliceFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class LongRunAcceptanceTests
{
    private static T Clone<T>(T input) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(input))!;
    [Fact]
    public void TwentyFiveAutonomousTurnsRepeatExactlyAcrossReloadsAndReorderedContentAndState()
    {
        var totals = new CombatTotals(); var builtBy = new HashSet<string>(); var travelledBy = new HashSet<string>();
        var scenario = Scenario(false); var baseline = Session(scenario); var repeated = Session(Clone(scenario)); var resumed = Session(Clone(scenario));
        var reorderedScenario = Clone(scenario); var state = reorderedScenario.InitialState; var defs = reorderedScenario.Definitions;
        state.Civilizations!.Reverse(); state.StarSystems!.Reverse(); state.Colonies!.Reverse(); state.Ships!.Reverse(); state.Fleets!.Reverse(); state.EntityIdentities.Reverse();
        defs.DefinitionIds.Reverse(); defs.Civilizations!.Reverse(); defs.StarSystems!.Reverse(); defs.GalaxyEdges!.Reverse(); defs.ResearchNodes!.Reverse(); defs.ShipDesigns!.Reverse(); defs.EconomicProfiles!.Reverse();
        foreach (var d in defs.ShipDesigns) d.RequiredResearchDefinitionIds!.Reverse(); foreach (var r in defs.ResearchNodes) r.PrerequisiteDefinitionIds!.Reverse();
        var reordered = Session(reorderedScenario); var reloads = new HashSet<int> { 1, 2, 4, 7, 13, 18, 24 }; var partial = false; var orders = new List<string>(); var commandCount = 0; var generated = 0; var checkpoints = 0;
        for (var i = 1; i <= 25; i++)
        {
            var result = Advance(baseline, totals); Assert.Equal(TurnStatus.Completed, result.Status); Assert.Empty(result.Events); Assert.All(result.CommandResults, c => Assert.True(c.Accepted));
            var resultJson = JsonSerializer.Serialize(result); orders.Add(resultJson); commandCount += result.CommandResults.Count;
            Assert.Equal(resultJson, JsonSerializer.Serialize(Advance(repeated))); Assert.Equal(resultJson, JsonSerializer.Serialize(AdvanceWithCombatReload(ref resumed, scenario, ref generated, ref checkpoints))); Assert.Equal(resultJson, JsonSerializer.Serialize(Advance(reordered)));
            Assert.Equal(Json(baseline), Json(repeated)); Assert.Equal(Json(baseline), Json(resumed)); Assert.Equal(Json(baseline), Json(reordered));
            foreach (var ship in baseline.CaptureSnapshot().Ships.Where(s => s.ShipId.TryGetAllocationSequence(out _))) builtBy.Add(ship.OwnerCivilizationId.Value);
            foreach (var fleet in baseline.CaptureSnapshot().Fleets.Where(f => f.LocationSystemId.Value != (f.OwnerCivilizationId.Value == "civ.cardassian" ? "system.cardassia" : "system.founders-homeworld"))) travelledBy.Add(fleet.OwnerCivilizationId.Value);
            Assert.All(baseline.CaptureSnapshot().Fleets.Where(f => f.OwnerCivilizationId.Value == "civ.federation"), f => { Assert.Equal("system.sol", f.LocationSystemId.Value); Assert.Empty(f.RouteSystemIds); });
            var current = baseline.ExportSave().GameState; partial |= current.Colonies!.Any(c => c.ProductionQueue!.Any(q => q.CompletedIndustry > 0));
            Assert.All(current.Civilizations!.Where(c => c.ControllerKind == ControllerKind.AI), c => Assert.Contains(c.CivilizationId, new[] { "civ.cardassian", "civ.dominion" }));
            if (reloads.Contains(i)) resumed = new SimulationSession(Clone(resumed.ExportSave()), Clone(scenario.Definitions), Compatibility(scenario));
        }
        Assert.True(partial); Assert.Equal(15, generated); Assert.Equal(14, checkpoints); Assert.Empty(baseline.CaptureSnapshot().Combats); Assert.Equal(25, orders.Count); var final = baseline.CaptureSnapshot();
        Assert.Equal(26, final.TurnNumber); Assert.Equal(SessionPhase.Planning, final.Phase);
        Assert.Equal(228, final.StateRevision); Assert.Equal(174, commandCount);
        Assert.Equal(8, final.Ships.Count); Assert.Equal(8, final.Fleets.Count); Assert.Equal(72UL, final.NextEntitySequence); Assert.Equal(0x850d3ff45bd6b85eUL, final.RngState.State);
        Assert.Equal((37, 118, 81, 108L, 139L, 13, 13, 0), (totals.Phases, totals.Shots, totals.Hits, totals.ShieldDamage, totals.HullDamage, totals.ShipsDestroyed, totals.FleetsDestroyed, totals.Retreated));
        Assert.True(totals.Hits > 0 && totals.HullDamage > 0 && totals.ShipsDestroyed > 0);
        Assert.Equal(2, final.Colonies.Sum(c => c.ProductionQueue.Count)); Assert.Equal(2, final.Civilizations.Sum(c => c.UnlockedResearchDefinitionIds.Count));
        Assert.Equal("3a310fad801dfc231d7639c909a0958fd03363097946944677a029fa7c914fee", Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(baseline.ExportSave()))).ToLowerInvariant());
        foreach (var actor in new[] { "civ.cardassian", "civ.dominion" })
        {
            Assert.Contains(new DefinitionId("research.advanced-engineering"), final.Civilizations.Single(c => c.CivilizationId.Value == actor).UnlockedResearchDefinitionIds);
            Assert.Contains(actor, builtBy); Assert.Contains(actor, travelledBy);
        }
        var human = final.Civilizations.Single(c => c.ControllerKind == ControllerKind.Human);
        Assert.Equal("civ.federation", human.CivilizationId.Value); Assert.Null(human.ResearchTargetDefinitionId); Assert.Empty(human.UnlockedResearchDefinitionIds);
        Assert.DoesNotContain(final.Ships, ship => ship.OwnerCivilizationId == human.CivilizationId && ship.ShipId.Value != "ship.federation-scout");
        Assert.DoesNotContain(final.Fleets, fleet => fleet.OwnerCivilizationId == human.CivilizationId && fleet.FleetId.Value != "fleet.federation-scout");
        Assert.Empty(final.Colonies.Single(c => c.OwnerCivilizationId == human.CivilizationId).ProductionQueue);
        // Full canonical save equality includes RNG, allocation, every queue/ship/fleet ID and every ordered route.
        Assert.Equal(Json(baseline), Json(resumed)); Assert.Equal(JsonSerializer.Serialize(final), JsonSerializer.Serialize(reordered.CaptureSnapshot()));
    }
    [Fact]
    public void HumanOrdersQueueProgressAndRoutedFleetSurviveRepeatedReloadAlongsideAutonomousAi()
    {
        var humanProduced = false; var scenario = Scenario(false); var continuous = Session(scenario); var resumed = Session(Clone(scenario));
        foreach (var s in new[] { continuous, resumed })
        { Assert.True(Enqueue(s).Accepted); Assert.True(FleetMovementTests.Route(s, "cmd.route", "system.bajor", "system.bajoran-wormhole", "system.gamma-exit").Accepted); }
        for (var turn = 1; turn <= 25; turn++)
        {
            Assert.Equal(JsonSerializer.Serialize(Advance(continuous)), JsonSerializer.Serialize(Advance(resumed))); Assert.Equal(Json(continuous), Json(resumed));
            humanProduced |= resumed.CaptureSnapshot().Ships.Any(s => s.OwnerCivilizationId.Value == "civ.federation" && s.ShipId.TryGetAllocationSequence(out _));
            if (turn == 1)
            { Assert.Equal(8, Colony(resumed).ProductionQueue![0].CompletedIndustry); Assert.Equal(2, resumed.CaptureSnapshot().Fleets.Single(f => f.FleetId.Value == "fleet.federation-scout").RouteSystemIds.Count); }
            resumed = new SimulationSession(Clone(resumed.ExportSave()), Clone(scenario.Definitions), Compatibility(scenario));
        }
        Assert.True(humanProduced); Assert.Empty(Colony(resumed).ProductionQueue!);
    }
    private static TurnResult AdvanceWithCombatReload(ref SimulationSession session, ScenarioDto scenario, ref int generated, ref int checkpoints)
    {
        SimulationSession Reload(SimulationSession s)
        {
            var before = Json(s); var reloaded = new SimulationSession(Clone(s.ExportSave()), Clone(scenario.Definitions), Compatibility(scenario));
            Assert.Equal(before, Json(reloaded)); return reloaded;
        }
        var stamp = session.CaptureSnapshot(); var advanced = session.AdvanceTurn(stamp.TurnNumber, stamp.StateRevision);
        if (advanced.Status != TurnStatus.AwaitingCombat) return advanced;
        checkpoints++; generated += session.GetPendingCombatRequests().Count;
        session = Reload(session); // Pending only; no strategic phase or seed/ID allocation repeats.
        foreach (var request in session.GetPendingCombatRequests().Reverse())
        {
            stamp = session.CaptureSnapshot(); var result = new DeterministicPhasedCombatResolver().Resolve(request); Assert.Equal(CombatSubmissionStatus.Buffered, session.SubmitCombatResult(result, stamp.TurnNumber, stamp.StateRevision).Status);
            session = Reload(session); // Partial batch and fully resolved but not finalized.
            Assert.DoesNotContain(session.GetPendingCombatRequests(), r => r.EncounterId == request.EncounterId);
            Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(session.GetResolvedCombats().Single(r => r.Request.EncounterId == request.EncounterId).Result));
        }
        stamp = session.CaptureSnapshot(); var final = session.FinalizeCombat(stamp.TurnNumber, stamp.StateRevision);
        return new TurnResult(final.Status, final.ErrorCode, final.AttemptedTurnNumber, final.TurnNumber, final.StateRevision, final.Phase, final.Events, advanced.CommandResults);
    }

    private static TurnResult Advance(SimulationSession s, CombatTotals? totals = null)
    {
        var stamp = s.CaptureSnapshot(); var advanced = s.AdvanceTurn(stamp.TurnNumber, stamp.StateRevision);
        if (advanced.Status != TurnStatus.AwaitingCombat) return advanced;
        foreach (var request in s.GetPendingCombatRequests())
        {
            stamp = s.CaptureSnapshot(); var combat = new DeterministicPhasedCombatResolver().Resolve(request); totals?.Add(combat); Assert.Equal(CombatSubmissionStatus.Buffered, s.SubmitCombatResult(combat, stamp.TurnNumber, stamp.StateRevision).Status);
        }
        stamp = s.CaptureSnapshot(); var final = s.FinalizeCombat(stamp.TurnNumber, stamp.StateRevision);
        return new TurnResult(final.Status, final.ErrorCode, final.AttemptedTurnNumber, final.TurnNumber, final.StateRevision, final.Phase, final.Events, advanced.CommandResults);
    }

    private sealed class CombatTotals
    {
        internal int Phases, Shots, Hits, ShipsDestroyed, FleetsDestroyed, Retreated;
        internal long ShieldDamage, HullDamage;
        internal void Add(CombatResultDto r)
        {
            Phases += r.Events.Count(e => e.Kind == CombatEventKind.PhaseStarted); Hits += r.Events.Count(e => e.Kind == CombatEventKind.AttackHit);
            Shots += r.Events.Count(e => e.Kind == CombatEventKind.AttackHit || e.Kind == CombatEventKind.AttackMissed);
            ShieldDamage += r.Events.Where(e => e.Kind == CombatEventKind.ShieldDamaged).Sum(e => e.Amount!.Value); HullDamage += r.Events.Where(e => e.Kind == CombatEventKind.ShipDamaged).Sum(e => e.Amount!.Value);
            ShipsDestroyed += r.Events.Count(e => e.Kind == CombatEventKind.ShipDestroyed); FleetsDestroyed += r.FleetChanges.Count(f => f.Disposition == FleetCombatDisposition.Destroyed); Retreated += r.FleetChanges.Count(f => f.Disposition == FleetCombatDisposition.Retreated);
        }
    }

}
