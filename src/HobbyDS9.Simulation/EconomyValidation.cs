using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    public static partial class FoundationValidator
    {
        private static void ValidatePopulationDefinitions(DefinitionCatalogDto d, HashSet<DefinitionId> ids, HashSet<string> typed, string path, List<ValidationIssue> issues)
        {
            void Check(bool ok, string p) { if (!ok) Add(issues, ValidationCode.InvalidEconomicValue, path + "." + p); }
            void Definition(string id, string label) { Check(DefinitionId.TryParse(id, out var parsed) && ids.Contains(parsed) && typed.Add(id) && !string.IsNullOrWhiteSpace(label), id); }
            if (d.Species == null || d.Planets == null || d.Infrastructure == null || d.Resources == null) { Add(issues, ValidationCode.MissingField, path + ".PopulationDefinitions"); return; }
            foreach (var r in d.Resources) { if (r == null) { Check(false, "Resources"); continue; } Definition(r.DefinitionId, r.DisplayName); }
            foreach (var s in d.Species)
            {
                if (s == null) { Check(false, "Species"); continue; }
                Definition(s.DefinitionId, s.DisplayName);
                Check(s.Suitabilities != null && s.Suitabilities.Count == 9 && s.Suitabilities.All(h => h != null && Enum.IsDefined(typeof(HabitatKind), h.Kind) && h.SuitabilityBps >= 0 && h.SuitabilityBps <= 10000) && s.Suitabilities.Select(h => h.Kind).Distinct().Count() == 9, s.DefinitionId);
                Check(s.MinimumGravity >= 0 && s.MaximumGravity >= s.MinimumGravity && s.MinimumPressure >= 0 && s.MaximumPressure >= s.MinimumPressure && s.MaximumRadiation >= 0, s.DefinitionId);
                Check(s.Atmospheres != null && s.Atmospheres.Count > 0 && s.Atmospheres.All(a => DefinitionId.TryParse(a, out _)) && s.Atmospheres.Distinct().Count() == s.Atmospheres.Count, s.DefinitionId);
                Check(s.TraitTags != null && s.TraitTags.All(a => DefinitionId.TryParse(a, out _)) && s.TraitTags.Distinct().Count() == s.TraitTags.Count, s.DefinitionId);
            }
            foreach (var p in d.Planets)
            {
                if (p == null) { Check(false, "Planets"); continue; }
                Definition(p.DefinitionId, p.DisplayName);
                Check(d.StarSystems != null && d.StarSystems.Any(s => s != null && s.DefinitionId == p.SystemDefinitionId) && !string.IsNullOrWhiteSpace(p.PlanetClass) && p.Gravity >= 0 && p.Pressure >= 0 && p.Radiation >= 0 && DefinitionId.TryParse(p.Atmosphere, out _), p.DefinitionId);
                Check(p.Habitats != null && p.Habitats.Count == 9 && p.Habitats.All(h => h != null && Enum.IsDefined(typeof(HabitatKind), h.Kind) && h.ShareBps >= 0 && h.ShareBps <= 10000 && h.NaturalCapacity >= 0)
                    && p.Habitats.Select(h => h.Kind).Distinct().Count() == 9 && p.Habitats.Sum(h => (long)h.ShareBps) == 10000 && p.Habitats.Aggregate(BigInteger.Zero, (a, h) => a + h.NaturalCapacity) <= long.MaxValue, p.DefinitionId);
                ValidateResources(p.Deposits, d, p.DefinitionId + ".Deposits", issues);
            }
            foreach (var i in d.Infrastructure)
            {
                if (i == null) { Check(false, "Infrastructure"); continue; }
                Definition(i.DefinitionId, i.DisplayName);
                Check(i.FoodOutput >= 0 && i.Industry >= 0 && i.PowerSupply >= 0 && i.PowerDemand >= 0 && i.Research >= 0 && i.LogisticsCapacity >= 0 && i.ArtificialCapacity >= 0 && i.ArtificialHabitability >= 0 && i.ArtificialHabitability <= 10000, i.DefinitionId);
                Check(i.AccessibleHabitats != null && i.AccessibleHabitats.All(h => Enum.IsDefined(typeof(HabitatKind), h)) && i.AccessibleHabitats.Distinct().Count() == i.AccessibleHabitats.Count, i.DefinitionId);
                ValidateResources(i.Extraction, d, i.DefinitionId + ".Extraction", issues);
            }
        }
        private static void ValidateResources(List<ResourceAmountDto>? values, DefinitionCatalogDto? d, string path, List<ValidationIssue> issues)
        {
            if (values == null) { Add(issues, ValidationCode.MissingField, path); return; }
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in values) if (r == null || r.Amount < 0 || !DefinitionId.TryParse(r.ResourceDefinitionId, out _) || !unique.Add(r.ResourceDefinitionId) || d?.Resources == null || !d.Resources.Any(v => v != null && v.DefinitionId == r.ResourceDefinitionId)) Add(issues, ValidationCode.InvalidEconomicValue, path);
        }
        private static void ValidateColonies(GameStateDto s, DefinitionCatalogDto? d, Dictionary<string, EntityIdentityDto> headers, HashSet<string> typed, HashSet<string> civs, string path, List<ValidationIssue> issues)
        {
            void Check(bool ok, string p) { if (!ok) Add(issues, ValidationCode.InvalidEconomicValue, path + "." + p); }
            void Identity(string id, string? definition)
            {
                if (!EntityId.TryParse(id, out _) || !typed.Add(id)) { Add(issues, ValidationCode.DuplicateId, path + "." + id); return; }
                if (!headers.TryGetValue(id, out var header)) Add(issues, ValidationCode.MissingEntity, path + "." + id);
                else if (header.DefinitionId != definition) Add(issues, ValidationCode.IdentityMismatch, path + "." + id);
            }
            if (s.Planets == null || s.Colonies == null || s.Displacements == null || s.LastDeliveries == null || s.PopulationStatistics == null) { Add(issues, ValidationCode.MissingField, path + ".PopulationState"); return; }
            foreach (var p in s.Planets)
            {
                if (p == null) { Check(false, "Planets"); continue; }
                Identity(p.PlanetId, p.DefinitionId);
                var definition = d?.Planets?.FirstOrDefault(v => v != null && v.DefinitionId == p.DefinitionId);
                Check(definition != null && s.StarSystems != null && s.StarSystems.Any(n => n != null && n.SystemId == p.SystemId && n.DefinitionId == definition.SystemDefinitionId), p.PlanetId);
                ValidateResources(p.RemainingDeposits, d, p.PlanetId, issues);
                if (p.RemainingDeposits != null && definition?.Deposits != null) Check(p.RemainingDeposits.All(r => r != null && r.Amount <= PopulationRules.Stock(definition.Deposits.Where(x => x != null), r.ResourceDefinitionId)), p.PlanetId);
            }
            var occupied = new HashSet<string>(StringComparer.Ordinal);
            foreach (var c in s.Colonies)
            {
                if (c == null) { Check(false, "Colonies"); continue; }
                Identity(c.ColonyId, null); ValidatePolitics(c,s,d,path+"."+c.ColonyId,issues);
                Check(occupied.Add(c.PlanetId) && s.Planets.Any(p => p != null && p.PlanetId == c.PlanetId && p.SystemId == c.SystemId), c.ColonyId + ".Planet");
                Check(s.StarSystems != null && s.StarSystems.Any(n => n != null && n.SystemId == c.SystemId && n.OwnerCivilizationId == c.OwnerCivilizationId), c.ColonyId + ".Ownership");
                Check(c.OwnerCivilizationId == null || civs.Contains(c.OwnerCivilizationId), c.ColonyId + ".Owner");
                Check(c.Stability >= 0 && c.Stability <= 10000 && c.Prosperity >= 0 && c.Prosperity <= 10000 && c.Automation >= 0 && c.Automation <= 10000 && c.Food >= 0 && c.FoodShortageTurns >= 0, c.ColonyId);
                ValidateResources(c.Stockpiles, d, c.ColonyId + ".Stockpiles", issues); ValidateResources(c.DesiredStockpiles, d, c.ColonyId + ".DesiredStockpiles", issues);
                if (c.Infrastructure == null || c.Cohorts == null) { Add(issues, ValidationCode.MissingField, path + "." + c.ColonyId); continue; }
                Check(c.Infrastructure.All(i => i != null && i.Count > 0 && d?.Infrastructure != null && d.Infrastructure.Any(v => v != null && v.DefinitionId == i.DefinitionId)) && c.Infrastructure.Where(i => i != null).Select(i => i.DefinitionId).Distinct().Count() == c.Infrastructure.Count, c.ColonyId + ".Infrastructure");
                Check(c.Cohorts.Count <= 32 && c.Cohorts.Where(p => p != null).Aggregate(BigInteger.Zero, (a, p) => a + p.Population) <= long.MaxValue, c.ColonyId + ".Cohorts");
                foreach (var p in c.Cohorts)
                {
                    if (p == null) { Check(false, c.ColonyId + ".Cohort"); continue; }
                    Identity(p.CohortId, p.SpeciesDefinitionId);
                    Check(d?.Species != null && d.Species.Any(x => x != null && x.DefinitionId == p.SpeciesDefinitionId) && p.Population >= 0 && Enum.IsDefined(typeof(LegalStatus), p.LegalStatus)
                        && (p.OriginCivilizationId == null || civs.Contains(p.OriginCivilizationId)), p.CohortId);
                    Check(new[] { p.Satisfaction, p.Integration, p.CivicAlignment, p.Radicalisation }.All(v => v >= 0 && v <= 10000), p.CohortId + ".Ratings");
                    if (p.ExternalAffinities == null || p.Memories == null) { Add(issues, ValidationCode.MissingField, path + "." + p.CohortId); continue; }
                    Check(p.ExternalAffinities.All(a => a != null && civs.Contains(a.CivilizationId) && a.Rating >= 0 && a.Rating <= 10000) && p.ExternalAffinities.Where(a => a != null).Select(a => a.CivilizationId).Distinct().Count() == p.ExternalAffinities.Count, p.CohortId + ".Affinities");
                    Check(p.Memories.All(m => m != null && DefinitionId.TryParse(m.Kind, out _) && m.Magnitude >= 0 && m.Magnitude <= 10000 && m.StartTurn > 0 && m.StartTurn <= s.TurnNumber && m.ExpiryTurn >= m.StartTurn && m.DecayPerTurn >= 0 && m.DecayPerTurn <= 10000 && (m.SourceEntityId == null || EntityId.TryParse(m.SourceEntityId, out _)))
                        && p.Memories.Where(m => m != null).Select(m => m.Kind + "|" + m.SourceEntityId).Distinct().Count() == p.Memories.Count, p.CohortId + ".Memories");
                }
            }
            foreach (var r in s.Displacements)
            {
                if (r == null) { Check(false, "Displacement"); continue; }
                Identity(r.DisplacementId, null);
                var c = s.Colonies.FirstOrDefault(c => c != null && c.PlanetId == r.SourcePlanetId); var cohort = c?.Cohorts?.FirstOrDefault(p => p != null && p.CohortId == r.CohortId);
                Check(cohort != null && r.Population > 0 && r.Population <= cohort.Population && DefinitionId.TryParse(r.Cause, out _) && (r.ResponsibleEntityId == null || EntityId.TryParse(r.ResponsibleEntityId, out _)), r.DisplacementId);
            }
            Check(s.Displacements.Where(r => r != null).Select(r => r.CohortId).Distinct().Count() == s.Displacements.Count, "Displacements");
            foreach (var r in s.LastDeliveries)
            {
                if (r == null) { Check(false, "Delivery"); continue; }
                Check(s.Planets.Any(p => p != null && p.PlanetId == r.OriginPlanetId) && s.Planets.Any(p => p != null && p.PlanetId == r.DestinationPlanetId)
                    && (r.ResourceDefinitionId == "resource.food" || d?.Resources?.Any(x => x != null && x.DefinitionId == r.ResourceDefinitionId) == true)
                    && r.ImportNeed >= 0 && r.ReachableSurplus >= 0 && r.RouteCapacity >= 0 && r.StabilityFactor >= 0 && r.StabilityFactor <= 10000 && r.RouteIntegrity >= 0 && r.RouteIntegrity <= 10000
                    && r.Delivered >= 0 && r.Delivered <= Math.Min(r.ImportNeed, Math.Min(r.ReachableSurplus, r.RouteCapacity)) && r.RouteNodeIds != null && r.RouteNodeIds.All(id => d?.NavigationRegions?.Any(n => n != null && n.NodeId == id) == true), "LastDeliveries");
            }
            Check(s.LastDeliveries.Where(r => r != null).Select(r => r.OriginPlanetId + "|" + r.DestinationPlanetId + "|" + r.ResourceDefinitionId).Distinct(StringComparer.Ordinal).Count() == s.LastDeliveries.Count, "DuplicateDeliveries");
            foreach (var receipt in s.LastDeliveries.Where(r => r != null))
            {
                var origin = s.Planets.FirstOrDefault(p => p != null && p.PlanetId == receipt.OriginPlanetId);
                var destination = s.Planets.FirstOrDefault(p => p != null && p.PlanetId == receipt.DestinationPlanetId);
                var route = receipt.RouteNodeIds;
                Check(origin != null && destination != null && origin.PlanetId != destination.PlanetId, "DeliveryEndpoints");
                if (route != null && route.Count > 0)
                {
                    Check(d?.NavigationRegions?.Any(n => n != null && n.NodeId == route[0] && n.AssociatedSystemId == origin?.SystemId) == true && d?.NavigationRegions?.Any(n => n != null && n.NodeId == route[route.Count - 1] && n.AssociatedSystemId == destination?.SystemId) == true && route.Distinct(StringComparer.Ordinal).Count() == route.Count, "DeliveryRoute");
                    for (var index = 1; index < route.Count; index++)
                        Check(d?.NavigationConnections?.Any(e => e != null && ((e.EndpointA == route[index - 1] && e.EndpointB == route[index]) || (e.EndpointB == route[index - 1] && e.EndpointA == route[index]))) == true, "DeliveryRoute");
                }
                else Check(receipt.RouteIntegrity == 0 && receipt.RouteCapacity == 0 && receipt.Delivered == 0, "DisconnectedDelivery");
                if (receipt.ImportNeed >= 0 && receipt.ReachableSurplus >= 0 && receipt.RouteCapacity >= 0 && receipt.StabilityFactor >= 0 && receipt.StabilityFactor <= 10000 && receipt.RouteIntegrity >= 0 && receipt.RouteIntegrity <= 10000)
                    Check(receipt.Delivered == PopulationRules.ScaleBy(PopulationRules.ScaleBy(Math.Min(receipt.ImportNeed, Math.Min(receipt.ReachableSurplus, receipt.RouteCapacity)), receipt.StabilityFactor), receipt.RouteIntegrity), "DeliveryAmount");
            }
            var stats = s.PopulationStatistics; Check(new[] { stats.Births, stats.Deaths, stats.Migrated, stats.RefugeesMoved, stats.FoodDelivered, stats.ResourceExtracted, stats.ResourceDelivered, stats.ResourceConsumed, stats.ProductionCompleted }.All(v => v >= 0), "Statistics");
        }
    }
}
