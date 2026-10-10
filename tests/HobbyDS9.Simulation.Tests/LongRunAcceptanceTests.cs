using System.Text.Json;
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
        var scenario = Scenario(false); var baseline = Session(scenario); var repeated = Session(Clone(scenario)); var resumed = Session(Clone(scenario));
        var reorderedScenario = Clone(scenario); var state = reorderedScenario.InitialState; var defs = reorderedScenario.Definitions;
        state.Civilizations!.Reverse(); state.StarSystems!.Reverse(); state.Colonies!.Reverse(); state.Ships!.Reverse(); state.Fleets!.Reverse(); state.EntityIdentities.Reverse();
        defs.DefinitionIds.Reverse(); defs.Civilizations!.Reverse(); defs.StarSystems!.Reverse(); defs.GalaxyEdges!.Reverse(); defs.ResearchNodes!.Reverse(); defs.ShipDesigns!.Reverse(); defs.EconomicProfiles!.Reverse();
        foreach (var d in defs.ShipDesigns) d.RequiredResearchDefinitionIds!.Reverse(); foreach (var r in defs.ResearchNodes) r.PrerequisiteDefinitionIds!.Reverse();
        var reordered = Session(reorderedScenario); var reloads = new HashSet<int> { 1, 2, 4, 7, 13, 18, 24 }; var partial = false; var orders = new List<string>(); var commandCount = 0; var generated = 0; var checkpoints = 0;
        for (var i = 1; i <= 25; i++)
        {
            var result = Advance(baseline); Assert.Equal(TurnStatus.Completed, result.Status); Assert.Empty(result.Events); Assert.All(result.CommandResults, c => Assert.True(c.Accepted));
            var resultJson = JsonSerializer.Serialize(result); orders.Add(resultJson); commandCount += result.CommandResults.Count;
            Assert.Equal(resultJson, JsonSerializer.Serialize(Advance(repeated))); Assert.Equal(resultJson, JsonSerializer.Serialize(AdvanceWithCombatReload(ref resumed, scenario, ref generated, ref checkpoints))); Assert.Equal(resultJson, JsonSerializer.Serialize(Advance(reordered)));
            Assert.Equal(Json(baseline), Json(repeated)); Assert.Equal(Json(baseline), Json(resumed)); Assert.Equal(Json(baseline), Json(reordered));
            var current = baseline.ExportSave().GameState; partial |= current.Colonies!.Any(c => c.ProductionQueue!.Any(q => q.CompletedIndustry > 0));
            Assert.All(current.Civilizations!.Where(c => c.ControllerKind == ControllerKind.AI), c => Assert.Contains(c.CivilizationId, new[] { "civ.cardassian", "civ.dominion" }));
            if (reloads.Contains(i)) resumed = new SimulationSession(Clone(resumed.ExportSave()), Clone(scenario.Definitions), Compatibility(scenario));
        }
        Assert.True(partial); Assert.Equal(44, generated); Assert.Equal(23, checkpoints); Assert.Empty(baseline.CaptureSnapshot().Combats); Assert.Equal(25, orders.Count); var final = baseline.CaptureSnapshot();
        Assert.Equal(26, final.TurnNumber); Assert.Equal(SessionPhase.Planning, final.Phase);
        Assert.Equal(374, final.StateRevision); Assert.Equal(282, commandCount);
        Assert.Equal(21, final.Ships.Count); Assert.Equal(21, final.Fleets.Count); Assert.Equal(101UL, final.NextEntitySequence); Assert.Equal(0x82f866ceb4d760e1UL, final.RngState.State);
        foreach (var actor in new[] { "civ.cardassian", "civ.dominion" })
        {
            Assert.Contains(new DefinitionId("research.advanced-engineering"), final.Civilizations.Single(c => c.CivilizationId.Value == actor).UnlockedResearchDefinitionIds);
            Assert.True(final.Ships.Count(s => s.OwnerCivilizationId.Value == actor) > 1);
            Assert.True(final.Fleets.Count(f => f.OwnerCivilizationId.Value == actor) > 1);
            Assert.Contains(final.Fleets, f => f.OwnerCivilizationId.Value == actor && f.LocationSystemId.Value != (actor == "civ.cardassian" ? "system.cardassia" : "system.founders-homeworld"));
        }
        var human = final.Civilizations.Single(c => c.ControllerKind == ControllerKind.Human);
        Assert.Equal("civ.federation", human.CivilizationId.Value); Assert.Null(human.ResearchTargetDefinitionId); Assert.Empty(human.UnlockedResearchDefinitionIds);
        Assert.Single(final.Ships, s => s.OwnerCivilizationId == human.CivilizationId); Assert.Single(final.Fleets, f => f.OwnerCivilizationId == human.CivilizationId);
        Assert.Equal("system.sol", final.Fleets.Single(f => f.OwnerCivilizationId == human.CivilizationId).LocationSystemId.Value);
        Assert.Empty(final.Colonies.Single(c => c.OwnerCivilizationId == human.CivilizationId).ProductionQueue);
        // Full canonical save equality includes RNG, allocation, every queue/ship/fleet ID and every ordered route.
        Assert.Equal(Json(baseline), Json(resumed)); Assert.Equal(JsonSerializer.Serialize(final), JsonSerializer.Serialize(reordered.CaptureSnapshot()));
    }
    [Fact]
    public void HumanOrdersQueueProgressAndRoutedFleetSurviveRepeatedReloadAlongsideAutonomousAi()
    {
        var scenario = Scenario(false); var continuous = Session(scenario); var resumed = Session(Clone(scenario));
        foreach (var s in new[] { continuous, resumed })
        { Assert.True(Enqueue(s).Accepted); Assert.True(FleetMovementTests.Route(s, "cmd.route", "system.bajor", "system.bajoran-wormhole", "system.gamma-exit").Accepted); }
        for (var turn = 1; turn <= 25; turn++)
        {
            Assert.Equal(JsonSerializer.Serialize(Advance(continuous)), JsonSerializer.Serialize(Advance(resumed))); Assert.Equal(Json(continuous), Json(resumed));
            if (turn == 1)
            { Assert.Equal(8, Colony(resumed).ProductionQueue![0].CompletedIndustry); Assert.Equal(2, resumed.CaptureSnapshot().Fleets.Single(f => f.FleetId.Value == "fleet.federation-scout").RouteSystemIds.Count); }
            resumed = new SimulationSession(Clone(resumed.ExportSave()), Clone(scenario.Definitions), Compatibility(scenario));
        }
        Assert.Equal(2, resumed.CaptureSnapshot().Ships.Count(s => s.OwnerCivilizationId.Value == "civ.federation"));
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
            stamp = session.CaptureSnapshot(); Assert.Equal(CombatSubmissionStatus.Buffered, session.SubmitCombatResult(new NoLossCombatResolver().Resolve(request), stamp.TurnNumber, stamp.StateRevision).Status);
            session = Reload(session); // Partial batch and fully resolved but not finalized.
            Assert.DoesNotContain(session.GetPendingCombatRequests(), r => r.EncounterId == request.EncounterId);
            Assert.Single(session.GetResolvedCombats().Single(r => r.Request.EncounterId == request.EncounterId).Result.Events);
        }
        stamp = session.CaptureSnapshot(); var final = session.FinalizeCombat(stamp.TurnNumber, stamp.StateRevision);
        return new TurnResult(final.Status, final.ErrorCode, final.AttemptedTurnNumber, final.TurnNumber, final.StateRevision, final.Phase, final.Events, advanced.CommandResults);
    }

}
