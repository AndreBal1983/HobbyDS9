using System;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class CampaignCombatConsequences
    {
        internal static CombatSubmissionError Validate(GameStateDto state, GalaxyGraph graph, CombatRequestDto request, CombatResultDto result)
        {
            var error = CombatEncounterScaffold.GetResultError(CampaignCombatState.ToCheckpoint(state), request, result);
            if (error != CombatSubmissionError.None || result.Status == CombatResolutionStatus.Deferred) return error;
            var hulls = request.Participants.SelectMany(p => p.Ships).ToDictionary(s => s.ShipId, s => s.Hull, StringComparer.Ordinal);
            foreach (var change in result.ShipChanges) hulls[change.ShipId] = change.RemainingHull;
            foreach (var change in result.FleetChanges)
            {
                var participant = request.Participants.Single(p => p.FleetId == change.FleetId);
                if (change.Disposition != FleetCombatDisposition.Destroyed && participant.Ships.All(s => hulls[s.ShipId] == 0)) return CombatSubmissionError.MalformedResult;
                if (change.Disposition != FleetCombatDisposition.Retreated) continue;
                if (!graph.GetNeighbors(new EntityId(request.LocationSystemId)).Contains(new EntityId(change.RetreatSystemId!))) return CombatSubmissionError.InvalidRetreat;
                if (state.Fleets!.Any(f => f.LocationSystemId == change.RetreatSystemId && f.OwnerCivilizationId != participant.CivilizationId)) return CombatSubmissionError.InvalidRetreat;
                if (result.FleetChanges.Any(other => other.FleetId != change.FleetId && other.Disposition == FleetCombatDisposition.Retreated && other.RetreatSystemId == change.RetreatSystemId
                    && request.Participants.Single(p => p.FleetId == other.FleetId).CivilizationId != participant.CivilizationId)) return CombatSubmissionError.InvalidRetreat;
                foreach (var buffered in state.ResolvedCombats!)
                foreach (var retreat in buffered.Result.FleetChanges.Where(c => c.Disposition == FleetCombatDisposition.Retreated && c.RetreatSystemId == change.RetreatSystemId))
                    if (buffered.Request.Participants.Single(p => p.FleetId == retreat.FleetId).CivilizationId != participant.CivilizationId) return CombatSubmissionError.InvalidRetreat;
            }
            // Events may omit effects, but any supplied effect event must agree
            // with explicit consequences; presentation never drives rules.
            foreach (var e in result.Events)
            {
                if (e.Kind == CombatEventKind.EncounterResolved && (e.ShipId != null || e.FleetId != null)) return CombatSubmissionError.InvalidEvents;
                if (e.Kind == CombatEventKind.ShipDamaged && (e.ShipId == null || !result.ShipChanges.Any(c => c.ShipId == e.ShipId && c.RemainingHull > 0 && c.RemainingHull < request.Participants.SelectMany(p => p.Ships).Single(s => s.ShipId == c.ShipId).Hull))) return CombatSubmissionError.InvalidEvents;
                if (e.Kind == CombatEventKind.ShipDestroyed && (e.ShipId == null || !result.ShipChanges.Any(c => c.ShipId == e.ShipId && c.RemainingHull == 0))) return CombatSubmissionError.InvalidEvents;
                if (e.Kind == CombatEventKind.FleetRetreated && (e.FleetId == null || !result.FleetChanges.Any(c => c.FleetId == e.FleetId && c.Disposition == FleetCombatDisposition.Retreated))) return CombatSubmissionError.InvalidEvents;
            }
            return CombatSubmissionError.None;
        }
        internal static void Apply(GameStateDto staging, CombatResultDto result)
        {
            foreach (var change in result.ShipChanges.OrderBy(c => c.ShipId, StringComparer.Ordinal))
            {
                var ship = staging.Ships!.Single(s => s.ShipId == change.ShipId);
                if (change.RemainingHull > 0) ship.CurrentHull = change.RemainingHull;
                else RemoveShip(staging, ship);
            }
            foreach (var change in result.FleetChanges.OrderBy(c => c.FleetId, StringComparer.Ordinal))
            {
                var fleet = staging.Fleets!.Single(f => f.FleetId == change.FleetId);
                if (change.Disposition == FleetCombatDisposition.Destroyed)
                {
                    foreach (var ship in staging.Ships!.Where(s => s.FleetId == fleet.FleetId).ToList()) RemoveShip(staging, ship);
                }
                else if (change.Disposition == FleetCombatDisposition.Retreated) { fleet.LocationSystemId = change.RetreatSystemId!; fleet.RouteSystemIds!.Clear(); }
                // Held preserves the remaining strategic route.
            }
            // Sparse results implicitly remove a fleet when its last ship died.
            foreach (var fleet in staging.Fleets!.Where(f => f.ShipIds!.Count == 0).ToList())
            { staging.Fleets!.Remove(fleet); staging.EntityIdentities.RemoveAll(e => e.EntityId == fleet.FleetId); }
        }
        private static void RemoveShip(GameStateDto state, ShipStateDto ship)
        {
            state.Ships!.Remove(ship); state.Fleets!.Single(f => f.FleetId == ship.FleetId).ShipIds!.Remove(ship.ShipId);
            state.EntityIdentities.RemoveAll(e => e.EntityId == ship.ShipId);
        }
        internal static void RequireOnlyConsequences(GameStateDto before, GameStateDto after)
        {
            var protectedCopy = SimulationSession.CopyState(after);
            protectedCopy.Ships = before.Ships; protectedCopy.Fleets = before.Fleets; protectedCopy.EntityIdentities = before.EntityIdentities;
            SimulationSession.RequirePhaseChanges(before, protectedCopy, allowEconomy: false);
            if (after.EntityIdentities.Any(e => !before.EntityIdentities.Any(old => old.EntityId == e.EntityId && old.DefinitionId == e.DefinitionId)))
                throw new InvalidOperationException("Combat finalization cannot create entities.");
        }
    }
}
