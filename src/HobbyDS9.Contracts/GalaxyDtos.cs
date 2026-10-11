using System;
using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public enum ControllerKind { Human = 1, AI = 2 }
    public enum GalaxyEdgeKind { Normal = 1, Wormhole = 2 }

    public sealed class CivilizationDefinitionDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public sealed class StarSystemDefinitionDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public sealed class CivilizationStateDto : ScienceEngineeringStateDto
    {
        public string CivilizationId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public ControllerKind? ControllerKind { get; set; }
        public long Credits { get; set; }

    }

    public sealed class StarSystemStateDto
    {
        public string SystemId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public string? OwnerCivilizationId { get; set; }
    }

    // Input orientation is arbitrary; the runtime graph canonicalizes endpoints.
    public sealed class GalaxyEdgeDto
    {
        public string EndpointA { get; set; } = string.Empty;
        public string EndpointB { get; set; } = string.Empty;
        public GalaxyEdgeKind? Kind { get; set; }
    }

    public sealed class CivilizationSnapshot
    {
        public EntityId CivilizationId { get; }
        public DefinitionId DefinitionId { get; }
        public ControllerKind ControllerKind { get; }
        public long Credits { get; }
        public ScienceEngineeringSnapshot? Research { get; }
        public long EngineeringReserve { get; }
        public DefinitionId? EngineeringTargetDefinitionId { get; }
        public IReadOnlyList<DefinitionId> MatureEngineeringApplicationDefinitionIds { get; }
        public CivilizationSnapshot(EntityId id, DefinitionId definitionId, ControllerKind controllerKind, long credits = 0, long researchReserve = 0,
            DefinitionId? researchTargetDefinitionId = null, IEnumerable<DefinitionId>? unlockedResearchDefinitionIds = null, ScienceEngineeringSnapshot? research = null)
        {
            Research = research; CivilizationId = id; DefinitionId = definitionId; ControllerKind = controllerKind; Credits = credits;
            EngineeringReserve = researchReserve; EngineeringTargetDefinitionId = researchTargetDefinitionId;
            MatureEngineeringApplicationDefinitionIds = new List<DefinitionId>(unlockedResearchDefinitionIds ?? Array.Empty<DefinitionId>()).AsReadOnly();
        }
    }

    public sealed class StarSystemSnapshot
    {
        public EntityId SystemId { get; }
        public DefinitionId DefinitionId { get; }
        public EntityId? OwnerCivilizationId { get; }
        public StarSystemSnapshot(EntityId id, DefinitionId definitionId, EntityId? owner)
        { SystemId = id; DefinitionId = definitionId; OwnerCivilizationId = owner; }
    }

    public sealed class GalaxyEdge
    {
        public EntityId EndpointA { get; }
        public EntityId EndpointB { get; }
        public GalaxyEdgeKind Kind { get; }
        public int TraversalCost => 1;
        public GalaxyEdge(EntityId a, EntityId b, GalaxyEdgeKind kind)
        {
            if (!a.IsValid || !b.IsValid || a == b) throw new ArgumentException("Invalid graph endpoints.");
            if (kind != GalaxyEdgeKind.Normal && kind != GalaxyEdgeKind.Wormhole) throw new ArgumentException("Invalid edge kind.");
            EndpointA = a.CompareTo(b) < 0 ? a : b;
            EndpointB = a.CompareTo(b) < 0 ? b : a;
            Kind = kind;
        }
    }

    public enum RouteErrorCode { None = 0, InvalidSystemId = 1, UnknownSystem = 2, Unreachable = 3, InvalidRoute = 4, NonAdjacent = 5, CostOverflow = 6 }
    public sealed class RouteResult
    {
        public RouteErrorCode ErrorCode { get; }
        public bool Success => ErrorCode == RouteErrorCode.None;
        // Destination steps, excluding origin. Same-node and empty routes cost zero.
        public IReadOnlyList<EntityId> Steps { get; }
        public RouteResult(RouteErrorCode error, IEnumerable<EntityId> steps)
        { ErrorCode = error; Steps = new List<EntityId>(steps).AsReadOnly(); }
    }
}
