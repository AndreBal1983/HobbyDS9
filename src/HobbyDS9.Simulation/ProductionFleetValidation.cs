using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public static partial class FoundationValidator
    {
        private static bool SameResources(List<ResourceAmountDto>? a, List<ResourceAmountDto>? b) => a != null && b != null && a.Count == b.Count
            && a.All(r => r != null && b.Any(v => v != null && v.ResourceDefinitionId == r.ResourceDefinitionId && v.Amount == r.Amount));
        private static void ValidateShipDesigns(DefinitionCatalogDto catalog, HashSet<DefinitionId> ids, HashSet<string> typed,
            string path, List<ValidationIssue> issues)
        {
            if (catalog.ShipDesigns == null) { Add(issues, ValidationCode.MissingField, path + ".ShipDesigns"); return; }
            var research = new HashSet<string>((catalog.ResearchNodes ?? new List<ResearchNodeDefinitionDto>()).Where(n => n != null).Select(n => n.DefinitionId), StringComparer.Ordinal);
            for (var i = 0; i < catalog.ShipDesigns.Count; i++)
            {
                var d = catalog.ShipDesigns[i]; var p = path + ".ShipDesigns[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
                if (d == null) { Add(issues, ValidationCode.MissingField, p); continue; }
                if (!DefinitionId.TryParse(d.DefinitionId, out var id)) Add(issues, ValidationCode.InvalidId, p + ".DefinitionId");
                else if (!ids.Contains(id)) Add(issues, ValidationCode.MissingDefinition, p + ".DefinitionId");
                if (d.DefinitionId != null && !typed.Add(d.DefinitionId)) Add(issues, ValidationCode.DuplicateId, p + ".DefinitionId");
                if (string.IsNullOrWhiteSpace(d.DisplayName)) Add(issues, ValidationCode.MissingField, p + ".DisplayName");
                if (d.MovementBudget<=0 || d.IndustryCost <= 0 || d.MaxHull <= 0 || d.CreditCost < 0) Add(issues, ValidationCode.InvalidProductionValue, p);
                ValidateResources(d.ResourceCosts, catalog, p + ".ResourceCosts", issues);
                if (!CombatProfiles.IsValid(d.CombatProfile)) Add(issues, ValidationCode.InvalidCombatProfile, p + ".CombatProfile");
                if (d.RequiredResearchDefinitionIds == null) { Add(issues, ValidationCode.MissingField, p + ".RequiredResearchDefinitionIds"); continue; }
                var unique = new HashSet<string>(StringComparer.Ordinal);
                foreach (var prerequisite in d.RequiredResearchDefinitionIds)
                {
                    if (!DefinitionId.TryParse(prerequisite, out _)) Add(issues, ValidationCode.InvalidId, p + ".RequiredResearchDefinitionIds");
                    else if (!research.Contains(prerequisite)) Add(issues, ValidationCode.MissingDefinition, p + ".RequiredResearchDefinitionIds");
                    if (!unique.Add(prerequisite)) Add(issues, ValidationCode.DuplicateId, p + ".RequiredResearchDefinitionIds");
                }
            }
        }

        private static void ValidateProductionFleets(GameStateDto state, DefinitionCatalogDto? catalog, Dictionary<string, EntityIdentityDto> headers,
            HashSet<string> typed, HashSet<string> civilizations, string path, List<ValidationIssue> issues)
        {
            var designs = (catalog?.ShipDesigns ?? new List<ShipDesignDefinitionDto>()).Where(d => d != null && d.DefinitionId != null)
                .GroupBy(d => d.DefinitionId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            void Identity(string? id, string? definition, string p)
            {
                if (!EntityId.TryParse(id, out _)) { Add(issues, ValidationCode.InvalidId, p); return; }
                if (!typed.Add(id!)) Add(issues, ValidationCode.DuplicateId, p);
                if (!headers.TryGetValue(id!, out var header)) Add(issues, ValidationCode.MissingEntity, p);
                else if (header.DefinitionId != definition) Add(issues, ValidationCode.IdentityMismatch, p);
            }
            void Owner(string? id, string p)
            {
                if (!EntityId.TryParse(id, out _)) Add(issues, ValidationCode.InvalidId, p);
                else if (!civilizations.Contains(id!)) Add(issues, ValidationCode.MissingCivilization, p);
            }
            ShipDesignDefinitionDto? Design(string? id, string p)
            {
                if (!DefinitionId.TryParse(id, out _)) { Add(issues, ValidationCode.InvalidId, p); return null; }
                if (!designs.TryGetValue(id!, out var design)) Add(issues, ValidationCode.MissingDefinition, p);
                return design;
            }
            foreach (var colony in (state.Colonies ?? new List<ColonyStateDto>()).Where(c => c != null))
            {
                var p = path + ".Colonies." + colony.ColonyId + ".ProductionQueue";
                if (colony.ProductionQueue == null) { Add(issues, ValidationCode.MissingField, p); continue; }
                if (colony.OwnerCivilizationId == null && colony.ProductionQueue.Count != 0) Add(issues, ValidationCode.OwnershipMismatch, p);
                for (var i = 0; i < colony.ProductionQueue.Count; i++)
                {
                    var item = colony.ProductionQueue[i];
                    if (item == null) { Add(issues, ValidationCode.MissingField, p); continue; }
                    Identity(item.QueueItemId, item.ShipDesignDefinitionId, p);
                    var design = Design(item.ShipDesignDefinitionId, p);
                    if (item.RequiredIndustry <= 0 || item.CompletedIndustry < 0 || item.CompletedIndustry >= item.RequiredIndustry
                        || (i != 0 && item.CompletedIndustry != 0) || item.PaidCredits < 0
                        || (design != null && (item.RequiredIndustry != design.IndustryCost || !SameResources(item.PaidResources, design.ResourceCosts) || item.PaidCredits != design.CreditCost)))
                        Add(issues, ValidationCode.InvalidProductionValue, p);
                    ValidateResources(item.PaidResources, catalog, p + ".PaidResources", issues);
                    var owner = state.Civilizations?.FirstOrDefault(c => c != null && c.CivilizationId == colony.OwnerCivilizationId);
                    if (design?.RequiredResearchDefinitionIds != null && owner?.UnlockedResearchDefinitionIds != null
                        && design.RequiredResearchDefinitionIds.Any(id => !owner.UnlockedResearchDefinitionIds.Contains(id, StringComparer.Ordinal)))
                        Add(issues, ValidationCode.ResearchPrerequisiteMissing, p);
                }
            }
            if (state.Ships == null) Add(issues, ValidationCode.MissingField, path + ".Ships");
            if (state.Fleets == null) Add(issues, ValidationCode.MissingField, path + ".Fleets");
            var ships = (state.Ships ?? new List<ShipStateDto>()).Where(s => s != null && s.ShipId != null)
                .GroupBy(s => s.ShipId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            var fleets = (state.Fleets ?? new List<FleetStateDto>()).Where(f => f != null && f.FleetId != null)
                .GroupBy(f => f.FleetId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            var nodeIds = (catalog?.NavigationRegions ?? new List<NavigationRegionDefinitionDto>()).Where(n => n != null).Select(n => n.NodeId).ToList();
            var memberships = new HashSet<string>(StringComparer.Ordinal);
            foreach (var fleet in state.Fleets ?? new List<FleetStateDto>())
            {
                var p = path + ".Fleets";
                if (fleet == null) { Add(issues, ValidationCode.MissingField, p); continue; }
                Identity(fleet.FleetId, null, p); Owner(fleet.OwnerCivilizationId, p);
                if (!EntityId.TryParse(fleet.LocationNodeId, out var location)) Add(issues, ValidationCode.InvalidId, p);
                else if (!nodeIds.Contains(fleet.LocationNodeId, StringComparer.Ordinal)) Add(issues, ValidationCode.MissingSystem, p);
                if (fleet.ShipIds == null) Add(issues, ValidationCode.MissingField, p + ".ShipIds");
                else
                {
                    if (fleet.ShipIds.Count == 0) Add(issues, ValidationCode.InvalidFleetMembership, p);
                    foreach (var id in fleet.ShipIds)
                    {
                        if (!EntityId.TryParse(id, out _)) Add(issues, ValidationCode.InvalidId, p + ".ShipIds");
                        if (!memberships.Add(id)) Add(issues, ValidationCode.InvalidFleetMembership, p);
                        if (id == null || !ships.TryGetValue(id, out var ship)) Add(issues, ValidationCode.MissingEntity, p + ".ShipIds");
                        else if (ship.FleetId != fleet.FleetId || ship.OwnerCivilizationId != fleet.OwnerCivilizationId) Add(issues, ValidationCode.InvalidFleetMembership, p);
                    }
                }
                if (fleet.RouteNodeIds == null) Add(issues, ValidationCode.MissingField, p + ".RouteNodeIds");
                else if (!location.IsValid || fleet.RouteNodeIds.Any(id => !EntityId.TryParse(id, out _))
)
                    Add(issues, ValidationCode.InvalidFleetRoute, p + ".RouteNodeIds");
            }
            foreach (var ship in state.Ships ?? new List<ShipStateDto>())
            {
                var p = path + ".Ships";
                if (ship == null) { Add(issues, ValidationCode.MissingField, p); continue; }
                Identity(ship.ShipId, ship.ShipDesignDefinitionId, p); Owner(ship.OwnerCivilizationId, p); var design = Design(ship.ShipDesignDefinitionId, p);
                if (ship.CurrentHull <= 0 || (design != null && ship.CurrentHull > design.MaxHull)) Add(issues, ValidationCode.InvalidShipValue, p);
                if (!EntityId.TryParse(ship.FleetId, out _)) Add(issues, ValidationCode.InvalidId, p + ".FleetId");
                else if (!fleets.TryGetValue(ship.FleetId, out var fleet) || !memberships.Contains(ship.ShipId)
                    || fleet.OwnerCivilizationId != ship.OwnerCivilizationId || fleet.ShipIds == null || !fleet.ShipIds.Contains(ship.ShipId, StringComparer.Ordinal))
                    Add(issues, ValidationCode.InvalidFleetMembership, p);
            }
        }
    }
}
