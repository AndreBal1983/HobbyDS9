using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class CampaignCombatState
    {
        internal static CombatCheckpointDto ToCheckpoint(GameStateDto state) => new CombatCheckpointDto
        {
            Phase = state.Phase!.Value, TurnNumber = state.TurnNumber, StateRevision = state.StateRevision,
            CampaignRandomState = state.RngState.State!.Value, NextEntitySequence = state.NextEntitySequence,
            PendingCombats = state.PendingCombats!, ResolvedCombats = state.ResolvedCombats!
        };
        internal static void SetCheckpoint(GameStateDto state, CombatCheckpointDto checkpoint)
        {
            state.Phase = checkpoint.Phase; state.RngState.State = checkpoint.CampaignRandomState;
            state.NextEntitySequence = checkpoint.NextEntitySequence; state.StateRevision = checkpoint.StateRevision;
            state.PendingCombats = checkpoint.PendingCombats; state.ResolvedCombats = checkpoint.ResolvedCombats;
        }
        internal static IEnumerable<CombatRequestDto> AllRequests(GameStateDto state) => state.PendingCombats!.Select(c => c.Request)
            .Concat(state.ResolvedCombats!.Select(c => c.Request)).OrderBy(r => r.LocationSystemId, StringComparer.Ordinal).ThenBy(r => r.EncounterId, StringComparer.Ordinal);
        internal static void Detect(GameStateDto state, SaveCompatibility compatibility)
        {
            var ships = state.Ships!.ToDictionary(s => s.ShipId, StringComparer.Ordinal);
            var templates = state.Fleets!.GroupBy(f => f.LocationSystemId, StringComparer.Ordinal).Where(g => g.Select(f => f.OwnerCivilizationId).Distinct(StringComparer.Ordinal).Count() >= 2)
                .OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => new CombatRequestDto
                {
                    LocationSystemId = g.Key, RulesVersion = compatibility.RulesVersion, ContentHash = compatibility.ContentHash,
                    ResolverId = CampaignCombatRules.ResolverId, ResolverVersion = CampaignCombatRules.ResolverVersion,
                    Participants = g.OrderBy(f => f.OwnerCivilizationId, StringComparer.Ordinal).ThenBy(f => f.FleetId, StringComparer.Ordinal).Select(f => new CombatParticipantDto
                    {
                        CivilizationId = f.OwnerCivilizationId, FleetId = f.FleetId,
                        Ships = f.ShipIds!.OrderBy(id => id, StringComparer.Ordinal).Select(id => new CombatShipDto { ShipId = id, DesignId = ships[id].ShipDesignDefinitionId, Hull = ships[id].CurrentHull }).ToList()
                    }).ToList()
                }).ToList();
            SetCheckpoint(state, CombatEncounterScaffold.CreateCheckpoint(state.TurnNumber, state.StateRevision, state.RngState.State!.Value, state.NextEntitySequence, templates));
        }
        internal static void RequireDetectionOnly(GameStateDto before, GameStateDto after)
        {
            var protectedCopy = SimulationSession.CopyState(after);
            protectedCopy.Phase = before.Phase; protectedCopy.NextEntitySequence = before.NextEntitySequence; protectedCopy.RngState = before.RngState;
            protectedCopy.PendingCombats = before.PendingCombats; protectedCopy.ResolvedCombats = before.ResolvedCombats;
            SimulationSession.RequirePhaseChanges(before, protectedCopy, allowEconomy: false);
        }
        internal static bool SameBatch(GameStateDto a, GameStateDto b)
        {
            if (a.PendingCombats!.Count != b.PendingCombats!.Count || a.ResolvedCombats!.Count != b.ResolvedCombats!.Count) return false;
            return a.PendingCombats.Zip(b.PendingCombats, (x, y) => SameRequest(x.Request, y.Request)).All(v => v)
                && a.ResolvedCombats.Zip(b.ResolvedCombats, (x, y) => SameRequest(x.Request, y.Request) && SameResult(x.Result, y.Result)).All(v => v);
        }
        private static bool SameRequest(CombatRequestDto a, CombatRequestDto b) =>
            (a.EncounterId, a.TurnNumber, a.LocationSystemId, a.RulesVersion, a.ContentHash, a.ResolverId, a.ResolverVersion, a.RandomAlgorithmId, a.RandomState)
                == (b.EncounterId, b.TurnNumber, b.LocationSystemId, b.RulesVersion, b.ContentHash, b.ResolverId, b.ResolverVersion, b.RandomAlgorithmId, b.RandomState)
            && a.Participants.Select(p => (p.CivilizationId, p.FleetId)).SequenceEqual(b.Participants.Select(p => (p.CivilizationId, p.FleetId)))
            && a.Participants.SelectMany(p => p.Ships).Select(s => (s.ShipId, s.DesignId, s.Hull)).SequenceEqual(b.Participants.SelectMany(p => p.Ships).Select(s => (s.ShipId, s.DesignId, s.Hull)));
        private static bool SameResult(CombatResultDto a, CombatResultDto b) =>
            (a.EncounterId, a.ResolverId, a.ResolverVersion, a.Status, a.FinalRandomState) == (b.EncounterId, b.ResolverId, b.ResolverVersion, b.Status, b.FinalRandomState)
            && a.ShipChanges.Select(c => (c.ShipId, c.RemainingHull)).SequenceEqual(b.ShipChanges.Select(c => (c.ShipId, c.RemainingHull)))
            && a.FleetChanges.Select(c => (c.FleetId, c.Disposition, c.RetreatSystemId)).SequenceEqual(b.FleetChanges.Select(c => (c.FleetId, c.Disposition, c.RetreatSystemId)))
            && a.Events.Select(e => (e.Sequence, e.Kind, e.ShipId, e.FleetId)).SequenceEqual(b.Events.Select(e => (e.Sequence, e.Kind, e.ShipId, e.FleetId)));
    }
}
