using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public sealed class ScaledGalaxyReviewSession
    {
        private readonly ScaledGalaxyDocumentDto document;
        private readonly Dictionary<string, List<string>> neighbors;
        private ScaledGalaxyReviewSession(ScaledGalaxyDocumentDto owned)
        { document = owned; neighbors = GeneratedGalaxyData.Neighbors(owned.Layout.Regions, owned.Layout.Connections); }
        public static ScaledGalaxyReviewSession Generate(ScaledGalaxySettingsDto settings, IEnumerable<CivilizationDefinitionDto> catalog)
            => new ScaledGalaxyReviewSession(ScaledGalaxyGenerator.Generate(settings, catalog));
        public static ScaledGalaxyReviewSession Open(ScaledGalaxyDocumentDto document, IEnumerable<CivilizationDefinitionDto> catalog)
            => new ScaledGalaxyReviewSession(ScaledGalaxyData.Open(document, catalog));
        public ScaledGalaxyDocumentDto Export() => ScaledGalaxyData.Copy(document);

        public StrategicRouteSnapshot FindRoute(string originNodeId, string destinationNodeId)
        {
            StrategicRouteSnapshot Error(RouteErrorCode code) => new StrategicRouteSnapshot(code, Array.Empty<string>(), Array.Empty<NavigationCostSnapshot>(), null);
            if (!EntityId.TryParse(originNodeId, out _) || !EntityId.TryParse(destinationNodeId, out _)) return Error(RouteErrorCode.InvalidSystemId);
            if (!neighbors.ContainsKey(originNodeId) || !neighbors.ContainsKey(destinationNodeId)) return Error(RouteErrorCode.UnknownSystem);
            var previous = new Dictionary<string, string?>(StringComparer.Ordinal) { [originNodeId] = null };
            var queue = new Queue<string>(); queue.Enqueue(originNodeId);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue(); if (node == destinationNodeId) break;
                foreach (var next in neighbors[node])
                    if (!previous.ContainsKey(next)) { previous[next] = node; queue.Enqueue(next); }
            }
            if (!previous.ContainsKey(destinationNodeId)) return Error(RouteErrorCode.Unreachable);
            var steps = new List<string>();
            for (string? node = destinationNodeId; node != originNodeId; node = previous[node!]) steps.Add(node!);
            steps.Reverse();
            var costs = new List<NavigationCostSnapshot>(); var from = originNodeId;
            foreach (var to in steps)
            {
                var edge = document.Layout.Connections.Single(e => e.EndpointA == from && e.EndpointB == to || e.EndpointB == from && e.EndpointA == to);
                costs.Add(new NavigationCostSnapshot(edge.ConnectionId, from, to, edge.Kind, edge.BaseTraversalCost, 0, 0)); from = to;
            }
            return new StrategicRouteSnapshot(RouteErrorCode.None, steps, costs, costs.Sum(c => c.EffectiveCost));
        }
    }
}
