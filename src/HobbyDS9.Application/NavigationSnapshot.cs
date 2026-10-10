using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;

namespace HobbyDS9.Application
{
    // Distinct type: clients must not infer a system identity from a node's value.
    public readonly struct NavigationNodeId : IEquatable<NavigationNodeId>
    {
        private readonly EntityId id;
        public string Value => id.Value;
        public bool IsValid => id.IsValid;
        public NavigationNodeId(string value) { id = new EntityId(value); }
        public bool Equals(NavigationNodeId other) => id.Equals(other.id);
        public override bool Equals(object? obj) => obj is NavigationNodeId other && Equals(other);
        public override int GetHashCode() => id.GetHashCode();
        public override string ToString() => id.ToString();
        public static bool operator ==(NavigationNodeId a, NavigationNodeId b) => a.Equals(b);
        public static bool operator !=(NavigationNodeId a, NavigationNodeId b) => !a.Equals(b);
    }

    public sealed class NavigationNodeSnapshot
    {
        public NavigationNodeId NodeId { get; }
        public EntityId? AssociatedSystemId { get; }
        public NavigationNodeSnapshot(NavigationNodeId nodeId, EntityId? associatedSystemId)
        {
            if (!nodeId.IsValid || (associatedSystemId.HasValue && !associatedSystemId.Value.IsValid)) throw new ArgumentException("Invalid navigation identity.");
            NodeId = nodeId; AssociatedSystemId = associatedSystemId;
        }
    }

    public sealed class NavigationLinkSnapshot
    {
        public NavigationNodeId EndpointA { get; }
        public NavigationNodeId EndpointB { get; }
        public GalaxyEdgeKind Kind { get; }
        public long TraversalCost { get; }
        internal NavigationLinkSnapshot(NavigationNodeId a, NavigationNodeId b, GalaxyEdgeKind kind, long cost)
        { EndpointA = a; EndpointB = b; Kind = kind; TraversalCost = cost; }
    }

    public sealed class NavigationMapSnapshot
    {
        public IReadOnlyList<NavigationNodeSnapshot> Nodes { get; }
        public IReadOnlyList<NavigationLinkSnapshot> Links { get; }
        internal NavigationMapSnapshot(IEnumerable<NavigationNodeSnapshot> nodes, IEnumerable<NavigationLinkSnapshot> links)
        { Nodes = new List<NavigationNodeSnapshot>(nodes).AsReadOnly(); Links = new List<NavigationLinkSnapshot>(links).AsReadOnly(); }
    }

    public enum NavigationRouteError { None = 0, InvalidNode = 1, UnknownNode = 2, Unreachable = 3 }
    public sealed class NavigationRoute
    {
        public NavigationRouteError ErrorCode { get; }
        public bool Success => ErrorCode == NavigationRouteError.None;
        // Excludes origin. A cost is not a turn estimate or a movement budget.
        public IReadOnlyList<NavigationNodeId> Steps { get; }
        public long? TraversalCost { get; }
        internal NavigationRoute(NavigationRouteError error, IEnumerable<NavigationNodeId> steps, long? cost)
        { ErrorCode = error; Steps = new List<NavigationNodeId>(steps).AsReadOnly(); TraversalCost = cost; }
    }
}
