using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Simulation.Tests;

internal static class CampaignCombatFixture
{
    internal static ScenarioDto Scenario(int battles = 1, bool threeCivilizations = false)
    {
        var scenario = VerticalSliceFixture.Scenario(); var state = scenario.InitialState;
        var ids = state.Ships!.Select(s => s.ShipId).Concat(state.Fleets!.Select(f => f.FleetId)).ToHashSet(StringComparer.Ordinal);
        state.EntityIdentities.RemoveAll(e => ids.Contains(e.EntityId)); state.Ships!.Clear(); state.Fleets!.Clear();
        var locations = new[] { "system.bajor", "system.bajoran-wormhole", "system.gamma-exit" };
        for (var i = 0; i < battles; i++)
        foreach (var actor in threeCivilizations ? new[] { "civ.federation", "civ.cardassian", "civ.dominion" } : new[] { "civ.federation", "civ.cardassian" })
        {
            var suffix = actor.Substring(4) + "-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture); var shipId = "ship." + suffix; var fleetId = "fleet." + suffix;
            state.Ships.Add(new() { ShipId = shipId, FleetId = fleetId, OwnerCivilizationId = actor, ShipDesignDefinitionId = "ship-design.scout", CurrentHull = 10 });
            state.Fleets.Add(new() { FleetId = fleetId, OwnerCivilizationId = actor, LocationSystemId = locations[i], ShipIds = new() { shipId }, RouteSystemIds = new() });
            state.EntityIdentities.Add(new() { EntityId = shipId, DefinitionId = "ship-design.scout" }); state.EntityIdentities.Add(new() { EntityId = fleetId });
        }
        return scenario;
    }
    // Scripted generic result for lifecycle isolation; this is not a production resolver.
    internal static CombatResultDto NoChanges(CombatRequestDto request) => PhasedCombatFixture.EmptyResult(request);
    internal static TurnResult Complete(SimulationSession s, TurnResult result)
    {
        if (result.Status != TurnStatus.AwaitingCombat) return result;
        foreach (var request in s.GetPendingCombatRequests())
        { var p = s.CaptureSnapshot(); var submitted = s.SubmitCombatResult(NoChanges(request), p.TurnNumber, p.StateRevision); if (submitted.Status != CombatSubmissionStatus.Buffered) throw new InvalidOperationException("Test host failed to buffer."); }
        var stamp = s.CaptureSnapshot(); var final = s.FinalizeCombat(stamp.TurnNumber, stamp.StateRevision);
        return new TurnResult(final.Status, final.ErrorCode, final.AttemptedTurnNumber, final.TurnNumber, final.StateRevision, final.Phase, final.Events, result.CommandResults);
    }
}
