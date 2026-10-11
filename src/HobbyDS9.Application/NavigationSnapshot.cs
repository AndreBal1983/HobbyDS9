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
        public string DisplayName { get; }
        public IReadOnlyList<string> PhenomenonDefinitionIds { get; }
        public NavigationNodeSnapshot(NavigationNodeId nodeId, EntityId? associatedSystemId, string displayName = "", IEnumerable<string>? phenomena = null)
        {
            if (!nodeId.IsValid || (associatedSystemId.HasValue && !associatedSystemId.Value.IsValid)) throw new ArgumentException("Invalid navigation identity.");
            NodeId = nodeId; AssociatedSystemId = associatedSystemId; DisplayName = displayName; PhenomenonDefinitionIds = new List<string>(phenomena ?? Array.Empty<string>()).AsReadOnly();
        }
    }

    public sealed class NavigationLinkSnapshot
    {
        public NavigationNodeId EndpointA { get; }
        public NavigationNodeId EndpointB { get; }
        public GalaxyEdgeKind Kind { get; }
        public long TraversalCost { get; }
        public long BaseCost { get; }
        public long PhenomenonCost { get; }
        public long ArtificialCost { get; }
        internal NavigationLinkSnapshot(NavigationNodeId a, NavigationNodeId b, GalaxyEdgeKind kind, long cost, long baseCost = 1, long phenomenonCost = 0, long artificialCost = 0)
        { EndpointA = a; EndpointB = b; Kind = kind; TraversalCost = cost; BaseCost = baseCost; PhenomenonCost = phenomenonCost; ArtificialCost = artificialCost; }
    }

    public sealed class NavigationMapSnapshot
    {
        public IReadOnlyList<NavigationNodeSnapshot> Nodes { get; }
        public IReadOnlyList<NavigationLinkSnapshot> Links { get; }
        public IReadOnlyList<PhenomenonDefinitionSnapshot> Phenomena { get; }
        internal NavigationMapSnapshot(IEnumerable<NavigationNodeSnapshot> nodes, IEnumerable<NavigationLinkSnapshot> links, IEnumerable<PhenomenonDefinitionSnapshot>? phenomena = null)
        { Nodes = new List<NavigationNodeSnapshot>(nodes).AsReadOnly(); Links = new List<NavigationLinkSnapshot>(links).AsReadOnly(); Phenomena = new List<PhenomenonDefinitionSnapshot>(phenomena ?? Array.Empty<PhenomenonDefinitionSnapshot>()).AsReadOnly(); }
    }

    public enum NavigationRouteError { None = 0, InvalidNode = 1, UnknownNode = 2, Unreachable = 3, CostOverflow = 4 }
    public sealed class NavigationRoute
    {
        public NavigationRouteError ErrorCode { get; }
        public bool Success => ErrorCode == NavigationRouteError.None;
        // Excludes origin. A cost is not a turn estimate or a movement budget.
        public IReadOnlyList<NavigationNodeId> Steps { get; }
        public long? TraversalCost { get; }
        public IReadOnlyList<NavigationCostSnapshot> Costs { get; }
        internal NavigationRoute(NavigationRouteError error, IEnumerable<NavigationNodeId> steps, long? cost, IEnumerable<NavigationCostSnapshot>? costs = null)
        { ErrorCode = error; Steps = new List<NavigationNodeId>(steps).AsReadOnly(); TraversalCost = cost; Costs = new List<NavigationCostSnapshot>(costs ?? Array.Empty<NavigationCostSnapshot>()).AsReadOnly(); }
    }
}
