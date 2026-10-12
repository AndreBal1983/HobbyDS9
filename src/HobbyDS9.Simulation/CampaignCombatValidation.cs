using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public static partial class FoundationValidator
    {
        private static void ValidateCampaignCombat(GameStateDto? state, DefinitionCatalogDto? definitions, string? rulesVersion, string? contentHash, string path, List<ValidationIssue> issues)
        {
            if (state == null) return;
            foreach (var blueprint in (state.Blueprints ?? new List<BlueprintRevisionDto>()).Where(b => b != null))
                if ((rulesVersion != null && blueprint.RulesVersion != rulesVersion) || (contentHash != null && blueprint.SourceContentHash != contentHash))
                    Add(issues, ValidationCode.BlueprintIdentityMismatch, path + ".Blueprints");
            if (state.PendingCombats == null) Add(issues, ValidationCode.MissingField, path + ".PendingCombats");
            if (state.ResolvedCombats == null) Add(issues, ValidationCode.MissingField, path + ".ResolvedCombats");
            if (state.PendingCombats == null || state.ResolvedCombats == null || !state.Phase.HasValue || state.RngState?.State == null
                || state.TurnNumber <= 0 || state.StateRevision < 0 || state.NextEntitySequence == 0
                || (state.Phase != SessionPhase.Planning && state.Phase != SessionPhase.AwaitingCombat)) return;
            try { CombatEncounterScaffold.ValidateCheckpoint(CampaignCombatState.ToCheckpoint(state)); }
            catch (ArgumentException) { Add(issues, ValidationCode.InvalidCombatCheckpoint, path + ".Combats"); return; }
            var requests = CampaignCombatState.AllRequests(state).ToList();
            var fleets = (state.Fleets ?? new List<FleetStateDto>()).Where(f => f != null && f.FleetId != null).GroupBy(f => f.FleetId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            var ships = (state.Ships ?? new List<ShipStateDto>()).Where(s => s != null && s.ShipId != null).GroupBy(s => s.ShipId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            var sequences = new HashSet<ulong>();
            foreach (var header in state.EntityIdentities ?? new List<EntityIdentityDto>())
                if (header != null && EntityId.TryParse(header.EntityId, out var id) && id.TryGetAllocationSequence(out var sequence)) sequences.Add(sequence);
            foreach (var request in requests)
            {
                if ((rulesVersion != null && request.RulesVersion != rulesVersion) || (contentHash != null && request.ContentHash != contentHash)
                    || request.ResolverId != CampaignCombatRules.ResolverId || request.ResolverVersion != CampaignCombatRules.ResolverVersion)
                    Add(issues, ValidationCode.CombatIdentityMismatch, path + ".Combats");
                new EntityId(request.EncounterId).TryGetAllocationSequence(out var sequence);
                if (!sequences.Add(sequence)) Add(issues, ValidationCode.DuplicateId, path + ".Combats");
                var atLocation = fleets.Values.Where(f => f.TransitTotalCost==0 && f.LocationNodeId == request.LocationNodeId).Select(f => f.FleetId).OrderBy(id => id, StringComparer.Ordinal);
                if (!atLocation.SequenceEqual(request.Participants.Select(p => p.FleetId).OrderBy(id => id, StringComparer.Ordinal))) Add(issues, ValidationCode.InvalidCombatReference, path + ".Combats");
                foreach (var participant in request.Participants)
                {
                    if (!fleets.TryGetValue(participant.FleetId, out var fleet) || fleet.LocationNodeId != request.LocationNodeId
                        || fleet.OwnerCivilizationId != participant.CivilizationId || fleet.ShipIds == null
                        || !fleet.ShipIds.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(participant.Ships.Select(s => s.ShipId).OrderBy(id => id, StringComparer.Ordinal)))
                        Add(issues, ValidationCode.InvalidCombatReference, path + ".Combats");
                    foreach (var snapshot in participant.Ships)
                        if (!ships.TryGetValue(snapshot.ShipId, out var ship) || ship.FleetId != participant.FleetId || ship.OwnerCivilizationId != participant.CivilizationId
                            || CampaignShipDesignResolver.Resolve(state, definitions, ship.ShipDesignDefinitionId, ship.Blueprint)?.DefinitionId != snapshot.DesignId || ship.CurrentHull != snapshot.Hull
                            || !MatchesResolvedDesign(state, definitions, ship, snapshot))
                            Add(issues, ValidationCode.InvalidCombatReference, path + ".Combats");
                }
            }
            if (state.Phase == SessionPhase.AwaitingCombat)
            {
                var contested = fleets.Values.Where(f=>f.TransitTotalCost==0).GroupBy(f => f.LocationNodeId, StringComparer.Ordinal).Where(g => g.Select(f => f.OwnerCivilizationId).Distinct(StringComparer.Ordinal).Count() >= 2).Select(g => g.Key).OrderBy(id => id, StringComparer.Ordinal);
                if (!contested.SequenceEqual(requests.Select(r => r.LocationNodeId).OrderBy(id => id, StringComparer.Ordinal))) Add(issues, ValidationCode.InvalidCombatReference, path + ".Combats");
            }
        }
        private static bool MatchesResolvedDesign(GameStateDto state, DefinitionCatalogDto? definitions, ShipStateDto ship, CombatShipDto snapshot)
        {
            var design = CampaignShipDesignResolver.Resolve(state, definitions, ship.ShipDesignDefinitionId, ship.Blueprint);
            return design != null && design.MaxHull == snapshot.MaxHull && CombatProfiles.Equal(design.CombatProfile, snapshot.CombatProfile);
        }
        private static void ValidateCombatTopology(GameStateDto? state, DefinitionCatalogDto? definitions, string path, List<ValidationIssue> issues)
        {
            if (state?.ResolvedCombats == null || state.PendingCombats == null || state.ResolvedCombats.Count == 0 || state.RngState?.State == null || !state.Phase.HasValue) return;
            try { CombatEncounterScaffold.ValidateCheckpoint(CampaignCombatState.ToCheckpoint(state)); }
            catch (ArgumentException) { return; }
            if(definitions==null||!WeightedNavigationGraph.Validate(definitions).IsValid||state.Fleets==null||state.Fleets.Any(f=>f==null))return;
            var graph=new WeightedNavigationGraph(definitions);
            foreach (var resolved in state.ResolvedCombats)
                if (CampaignCombatConsequences.Validate(state, graph, resolved.Request, resolved.Result) != CombatSubmissionError.None)
                    Add(issues, ValidationCode.InvalidCombatCheckpoint, path + ".ResolvedCombats");
        }
    }
}
