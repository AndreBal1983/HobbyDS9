using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class GalaxyReviewRouteSearch
    {
        private sealed class Arc
        {
            internal string To = "";
            internal NavigationConnectionDto Edge = null!;
        }

        internal static GalaxyReviewRoutePlanSnapshot Query(string mapHash, IReadOnlyList<GeneratedGalaxyRegionDto> regions,
            IReadOnlyList<NavigationConnectionDto> connections, GalaxyReviewRouteQueryDto query)
        {
            string origin = query != null && query.OriginNodeId != null && query.OriginNodeId.Length <= 128 ? query.OriginNodeId : "";
            GalaxyReviewRoutePlanSnapshot Error(GalaxyReviewPlanError code, int? index = null) => new GalaxyReviewRoutePlanSnapshot(
                GalaxyReviewRoutePolicy.MinimumCrossingsDirectV1, mapHash, origin, code, index,
                Array.Empty<GalaxyReviewLegSnapshot>(), Array.Empty<string>(), null, null);
            if (query == null || query.Targets == null || query.Targets.Count == 0 || query.Targets.Count > GalaxyReviewRoutePolicy.MaximumTargets)
                return Error(GalaxyReviewPlanError.InvalidRequest);
            if (query.PolicyVersion != GalaxyReviewRoutePolicy.MinimumCrossingsDirectV1) return Error(GalaxyReviewPlanError.UnsupportedPolicy);
            if (query.MapContentHash != mapHash) return Error(GalaxyReviewPlanError.StaleMap);
            var filter = query.Filter;
            if (filter == null || filter.PermittedNodeIds == null || filter.PermittedConnectionIds == null
                || filter.PermittedNodeIds.Count > ScaledGalaxyIdentity.MaximumRegions || filter.PermittedConnectionIds.Count > ScaledGalaxyIdentity.MaximumConnections)
                return Error(GalaxyReviewPlanError.InvalidFilter);
            var byNode = regions.ToDictionary(r => r.NodeId, StringComparer.Ordinal);
            var byEdge = connections.ToDictionary(e => e.ConnectionId, StringComparer.Ordinal);
            var nodes = new HashSet<string>(filter.PermittedNodeIds, StringComparer.Ordinal);
            var edges = new HashSet<string>(filter.PermittedConnectionIds, StringComparer.Ordinal);
            if (nodes.Count != filter.PermittedNodeIds.Count || edges.Count != filter.PermittedConnectionIds.Count
                || nodes.Any(n => n == null || !byNode.ContainsKey(n)) || edges.Any(e => e == null || !byEdge.ContainsKey(e)))
                return Error(GalaxyReviewPlanError.InvalidFilter);
            if (!nodes.Contains(origin)) return Error(GalaxyReviewPlanError.UnavailableOrigin);
            // This explicitly versioned query is for validated generated unit-cost maps only.
            if (connections.Any(e => e.BaseTraversalCost != 1)) return Error(GalaxyReviewPlanError.InvalidRequest);
            var targets = new List<GalaxyReviewTargetSnapshot>();
            for (int index = 0; index < query.Targets.Count; index++)
            {
                var t = query.Targets[index];
                if (t == null) return Error(GalaxyReviewPlanError.InvalidRequest, index);
                if (t.NodeId == null || !nodes.Contains(t.NodeId)) return Error(GalaxyReviewPlanError.UnavailableTarget, index);
                if (t.Intent != GalaxyReviewTargetIntent.RegionTransit && t.Intent != GalaxyReviewTargetIntent.SystemLocation)
                    return Error(GalaxyReviewPlanError.InvalidRequest, index);
                if (t.Intent == GalaxyReviewTargetIntent.RegionTransit && t.SystemId != null
                    || t.Intent == GalaxyReviewTargetIntent.SystemLocation && (t.SystemId == null || byNode[t.NodeId].SystemId != t.SystemId))
                    return Error(GalaxyReviewPlanError.InvalidSystemTarget, index);
                targets.Add(new GalaxyReviewTargetSnapshot(t.NodeId, t.Intent, t.SystemId));
            }
            var neighbors = nodes.ToDictionary(n => n, _ => new List<Arc>(), StringComparer.Ordinal);
            foreach (var e in connections.Where(e => edges.Contains(e.ConnectionId) && nodes.Contains(e.EndpointA) && nodes.Contains(e.EndpointB)))
            {
                neighbors[e.EndpointA].Add(new Arc { To = e.EndpointB, Edge = e });
                neighbors[e.EndpointB].Add(new Arc { To = e.EndpointA, Edge = e });
            }
            foreach (var list in neighbors.Values) list.Sort((a, b) => StringComparer.Ordinal.Compare(a.To, b.To));
            var legs = new List<GalaxyReviewLegSnapshot>(); var allSteps = new List<string>();
            long cost = 0, geographic = 0; string from = origin;
            for (int index = 0; index < targets.Count; index++)
            {
                string destination = targets[index].NodeId;
                var distances = new Dictionary<string, int>(StringComparer.Ordinal) { [from] = 0 };
                var queue = new Queue<string>(); queue.Enqueue(from);
                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    foreach (var arc in neighbors[node]) if (!distances.ContainsKey(arc.To))
                    { distances.Add(arc.To, distances[node] + 1); queue.Enqueue(arc.To); }
                }
                if (!distances.TryGetValue(destination, out var minimum)) return Error(GalaxyReviewPlanError.UnreachableLeg, index);
                var scores = new Dictionary<string, long>(StringComparer.Ordinal) { [from] = 0 };
                var previous = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var node in distances.Where(p => p.Value > 0 && p.Value <= minimum).OrderBy(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal))
                {
                    long best = long.MaxValue; string? predecessor = null;
                    foreach (var arc in neighbors[node.Key].Where(a => distances.TryGetValue(a.To, out var distance) && distance == node.Value - 1))
                    {
                        var a = byNode[arc.To].Center; var b = byNode[node.Key].Center;
                        long dx = (long)b.X - a.X, dy = (long)b.Y - a.Y;
                        long candidate = checked(scores[arc.To] + FloorSqrt(dx * dx + dy * dy));
                        if (candidate < best || candidate == best && (predecessor == null || ComparePrefixes(arc.To, predecessor, previous) < 0))
                        { best = candidate; predecessor = arc.To; }
                    }
                    previous.Add(node.Key, predecessor!); scores.Add(node.Key, best);
                }
                var steps = new List<string>();
                for (string node = destination; node != from; node = previous[node]) steps.Add(node);
                steps.Reverse();
                var costs = new List<NavigationCostSnapshot>(); string start = from;
                foreach (var step in steps)
                {
                    var edge = neighbors[start].Single(a => a.To == step).Edge;
                    costs.Add(new NavigationCostSnapshot(edge.ConnectionId, start, step, edge.Kind, 1, 0, 0)); start = step;
                }
                var route = new StrategicRouteSnapshot(RouteErrorCode.None, steps, costs, minimum);
                legs.Add(new GalaxyReviewLegSnapshot(index, from, targets[index], route, scores[destination]));
                allSteps.AddRange(steps); cost = checked(cost + minimum); geographic = checked(geographic + scores[destination]); from = destination;
            }
            return new GalaxyReviewRoutePlanSnapshot(GalaxyReviewRoutePolicy.MinimumCrossingsDirectV1, mapHash, origin,
                GalaxyReviewPlanError.None, null, legs, allSteps, cost, geographic);
        }

        private static int ComparePrefixes(string a, string b, Dictionary<string, string> previous)
        {
            var left = new List<string> { a }; var right = new List<string> { b };
            while (previous.TryGetValue(a, out var pa)) { left.Add(pa); a = pa; }
            while (previous.TryGetValue(b, out var pb)) { right.Add(pb); b = pb; }
            left.Reverse(); right.Reverse();
            for (int i = 0; i < Math.Min(left.Count, right.Count); i++)
            { int comparison = StringComparer.Ordinal.Compare(left[i], right[i]); if (comparison != 0) return comparison; }
            return left.Count.CompareTo(right.Count);
        }

        // Input comes from bounded generated-map coordinates; no floating point or runtime sqrt.
        private static long FloorSqrt(long square)
        {
            long low = 0, high = Math.Min(square, 10000000), result = 0;
            while (low <= high)
            {
                long middle = low + (high - low) / 2;
                if (middle * middle <= square) { result = middle; low = middle + 1; } else high = middle - 1;
            }
            return result;
        }
    }
}
