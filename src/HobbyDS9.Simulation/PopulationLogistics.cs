using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static class PopulationLogistics
    {
        internal const long TransitCapacity = 100;
        internal static GalaxyGraph Graph(GameStateDto s, DefinitionCatalogDto d) => new GalaxyGraph(s.StarSystems!.Select(n => new EntityId(n.SystemId)), d.GalaxyEdges!);
        internal static (List<string> path, int integrity) Path(GameStateDto s, GalaxyGraph graph, string from, string to, string? owner)
        {
            var route = graph.FindRoute(new EntityId(from), new EntityId(to));
            if (!route.Success) return (new List<string>(), 0);
            var path = new[] { from }.Concat(route.Steps.Select(n => n.Value)).ToList();
            var hostile = s.Fleets!.Where(f => f.OwnerCivilizationId != owner).Select(f => f.LocationSystemId).ToHashSet(StringComparer.Ordinal);
            if (path.Any(hostile.Contains)) return (path, 0);
            return (path, path.Any(n => graph.GetNeighbors(new EntityId(n)).Any(x => hostile.Contains(x.Value))) ? 5000 : 10000);
        }
        private static string Edge(string a, string b) => StringComparer.Ordinal.Compare(a, b) < 0 ? a + "|" + b : b + "|" + a;
        internal static void Execute(GameStateDto s, DefinitionCatalogDto d)
        {
            s.LastDeliveries!.Clear(); var graph = Graph(s, d);
            var colonies = s.Colonies!.OrderBy(c => c.ColonyId, StringComparer.Ordinal).ToList();
            var facts = colonies.ToDictionary(c => c.ColonyId, c => WorldEconomy.Project(c, s, d), StringComparer.Ordinal);
            var endpoints = colonies.ToDictionary(c => c.ColonyId, c => facts[c.ColonyId].LogisticsCapacity, StringComparer.Ordinal);
            var edges = graph.Edges.ToDictionary(e => Edge(e.EndpointA.Value, e.EndpointB.Value), e => TransitCapacity, StringComparer.Ordinal);
            foreach (var resource in new[] { "resource.food" }.Concat(d.Resources!.Select(r => r.DefinitionId).Where(id => id != "resource.food").OrderBy(id => id, StringComparer.Ordinal)))
            {
                long Stock(ColonyStateDto c) => resource == "resource.food" ? c.Food : PopulationRules.Stock(c.Stockpiles!, resource);
                long Desired(ColonyStateDto c) => resource == "resource.food" ? facts[c.ColonyId].FoodDemand : PopulationRules.Stock(c.DesiredStockpiles!, resource);
                void Change(ColonyStateDto c, long value) { if (resource == "resource.food") c.Food = checked(c.Food + value); else PopulationRules.AddStock(c.Stockpiles!, resource, value); }
                foreach (var destination in colonies.Where(c => c.OwnerCivilizationId != null))
                    foreach (var origin in colonies.Where(c => c.ColonyId != destination.ColonyId && c.OwnerCivilizationId == destination.OwnerCivilizationId))
                    {
                        var need = Math.Max(0, Desired(destination) - Stock(destination)); var surplus = Math.Max(0, Stock(origin) - Desired(origin));
                        if (need == 0 || surplus == 0) continue;
                        var route = Path(s, graph, origin.SystemId, destination.SystemId, destination.OwnerCivilizationId);
                        var segments = Enumerable.Range(1, Math.Max(0, route.path.Count - 1)).Select(i => Edge(route.path[i - 1], route.path[i])).ToList();
                        var capacity = Math.Min(endpoints[origin.ColonyId], endpoints[destination.ColonyId]);
                        if (route.path.Count == 0) capacity = 0;
                        foreach (var edge in segments) capacity = Math.Min(capacity, edges[edge]);
                        var reachable = route.integrity == 0 ? 0 : surplus;
                        var delivered = PopulationRules.ScaleBy(PopulationRules.ScaleBy(Math.Min(need, Math.Min(reachable, capacity)), destination.Stability), route.integrity);
                        s.LastDeliveries.Add(new LogisticsDeliveryDto
                        {
                            OriginPlanetId = origin.PlanetId,
                            DestinationPlanetId = destination.PlanetId,
                            ResourceDefinitionId = resource,
                            RouteSystemIds = route.path,
                            ImportNeed = need,
                            ReachableSurplus = reachable,
                            RouteCapacity = capacity,
                            StabilityFactor = destination.Stability,
                            RouteIntegrity = route.integrity,
                            Delivered = delivered
                        });
                        Change(origin, -delivered); Change(destination, delivered); endpoints[origin.ColonyId] -= delivered; endpoints[destination.ColonyId] -= delivered;
                        foreach (var edge in segments) edges[edge] -= delivered;
                        if (resource == "resource.food") s.PopulationStatistics!.FoodDelivered = checked(s.PopulationStatistics.FoodDelivered + delivered);
                        else s.PopulationStatistics!.ResourceDelivered = checked(s.PopulationStatistics.ResourceDelivered + delivered);
                    }
            }
        }
    }
}
