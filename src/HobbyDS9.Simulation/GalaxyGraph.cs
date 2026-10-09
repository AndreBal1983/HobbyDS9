using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Immutable topology. No renderer coordinates, names, mutable DTOs or RNG.
    public sealed class GalaxyGraph
    {
        private readonly Dictionary<EntityId, IReadOnlyList<EntityId>> adjacency;
        public IReadOnlyList<EntityId> Nodes { get; }
        public IReadOnlyList<GalaxyEdge> Edges { get; }

        public GalaxyGraph(IEnumerable<EntityId> nodes, IEnumerable<GalaxyEdgeDto> edges)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (edges == null) throw new ArgumentNullException(nameof(edges));
            var nodeList = nodes.ToList();
            var edgeList = edges.ToList();
            Validate(nodeList.Select(n => n.IsValid ? n.Value : string.Empty), edgeList).ThrowIfInvalid(nameof(edges));
            Nodes = nodeList.OrderBy(n => n).ToList().AsReadOnly();
            Edges = edgeList.Select(e => CanonicalEdge(e)).OrderBy(e => e.EndpointA).ThenBy(e => e.EndpointB).ToList().AsReadOnly();
            var neighbors = Nodes.ToDictionary(n => n, _ => new List<EntityId>());
            foreach (var edge in Edges)
            { neighbors[edge.EndpointA].Add(edge.EndpointB); neighbors[edge.EndpointB].Add(edge.EndpointA); }
            adjacency = neighbors.ToDictionary(p => p.Key, p => (IReadOnlyList<EntityId>)p.Value.OrderBy(n => n).ToList().AsReadOnly());
        }

        public IReadOnlyList<EntityId> GetNeighbors(EntityId systemId) => adjacency.TryGetValue(systemId, out var neighbors)
            ? neighbors : Array.Empty<EntityId>();

        public RouteResult FindRoute(EntityId origin, EntityId destination)
        {
            if (!origin.IsValid || !destination.IsValid) return Failure(RouteErrorCode.InvalidSystemId);
            if (!adjacency.ContainsKey(origin) || !adjacency.ContainsKey(destination)) return Failure(RouteErrorCode.UnknownSystem);
            var queue = new Queue<EntityId>();
            var seen = new HashSet<EntityId> { origin };
            var previous = new Dictionary<EntityId, EntityId>();
            queue.Enqueue(origin);
            while (queue.Count != 0)
            {
                var node = queue.Dequeue();
                if (node == destination)
                {
                    var route = new List<EntityId>();
                    while (node != origin) { route.Add(node); node = previous[node]; }
                    route.Reverse();
                    return new RouteResult(RouteErrorCode.None, route);
                }
                foreach (var next in adjacency[node])
                    if (seen.Add(next)) { previous.Add(next, node); queue.Enqueue(next); }
            }
            return Failure(RouteErrorCode.Unreachable);
        }

        public RouteResult ValidateRoute(EntityId origin, IEnumerable<EntityId>? steps)
        {
            if (!origin.IsValid) return Failure(RouteErrorCode.InvalidSystemId);
            if (!adjacency.ContainsKey(origin)) return Failure(RouteErrorCode.UnknownSystem);
            if (steps == null) return Failure(RouteErrorCode.InvalidRoute);
            var route = steps.ToList();
            var current = origin;
            foreach (var next in route)
            {
                if (!next.IsValid) return Failure(RouteErrorCode.InvalidSystemId);
                if (!adjacency.ContainsKey(next)) return Failure(RouteErrorCode.UnknownSystem);
                if (!adjacency[current].Contains(next)) return Failure(RouteErrorCode.NonAdjacent);
                current = next;
            }
            return new RouteResult(RouteErrorCode.None, route);
        }

        private static RouteResult Failure(RouteErrorCode code) => new RouteResult(code, Array.Empty<EntityId>());
        internal static GalaxyEdge CanonicalEdge(GalaxyEdgeDto edge)
        {
            var a = new EntityId(edge.EndpointA); var b = new EntityId(edge.EndpointB);
            return a.CompareTo(b) < 0 ? new GalaxyEdge(a, b, edge.Kind!.Value) : new GalaxyEdge(b, a, edge.Kind!.Value);
        }

        public static ValidationReport Validate(IEnumerable<string>? nodes, IEnumerable<GalaxyEdgeDto>? edges)
        {
            var issues = new List<ValidationIssue>();
            var ids = new HashSet<EntityId>();
            if (nodes == null) issues.Add(new ValidationIssue(ValidationCode.MissingField, "$.Nodes"));
            else foreach (var node in nodes)
            {
                if (!EntityId.TryParse(node, out var id)) issues.Add(new ValidationIssue(ValidationCode.InvalidId, "$.Nodes"));
                else if (!ids.Add(id)) issues.Add(new ValidationIssue(ValidationCode.DuplicateId, "$.Nodes"));
            }
            var pairs = new HashSet<(EntityId, EntityId)>();
            if (edges == null) issues.Add(new ValidationIssue(ValidationCode.MissingField, "$.Edges"));
            else
            {
                var index = 0;
                foreach (var edge in edges)
                {
                    var path = "$.Edges[" + (index++).ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
                    if (edge == null) { issues.Add(new ValidationIssue(ValidationCode.MissingField, path)); continue; }
                    if (!edge.Kind.HasValue) issues.Add(new ValidationIssue(ValidationCode.MissingField, path + ".Kind"));
                    else if (edge.Kind != GalaxyEdgeKind.Normal && edge.Kind != GalaxyEdgeKind.Wormhole)
                        issues.Add(new ValidationIssue(ValidationCode.InvalidEdgeKind, path + ".Kind"));
                    if (!EntityId.TryParse(edge.EndpointA, out var a) || !EntityId.TryParse(edge.EndpointB, out var b))
                    { issues.Add(new ValidationIssue(ValidationCode.InvalidId, path)); continue; }
                    if (!ids.Contains(a) || !ids.Contains(b)) issues.Add(new ValidationIssue(ValidationCode.MissingEndpoint, path));
                    if (a == b) issues.Add(new ValidationIssue(ValidationCode.SelfEdge, path));
                    var pair = a.CompareTo(b) <= 0 ? (a, b) : (b, a);
                    if (!pairs.Add(pair)) issues.Add(new ValidationIssue(ValidationCode.DuplicateEdge, path));
                }
            }
            return new ValidationReport(issues);
        }
    }
}
