using System;
using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public sealed class EntityIdentitySnapshot
    {
        public EntityId EntityId { get; }
        public DefinitionId? DefinitionId { get; }
        public EntityIdentitySnapshot(EntityId entityId, DefinitionId? definitionId)
        {
            if (!entityId.IsValid || (definitionId.HasValue && !definitionId.Value.IsValid))
                throw new ArgumentException("Uninitialized snapshot identity.");
            EntityId = entityId; DefinitionId = definitionId;
        }
    }

    public sealed class ActorSnapshot
    {
        public EntityId CivilizationId { get; }
        public ControllerKind ControllerKind { get; }
        public bool IsControlled => ControllerKind == ControllerKind.Human;
        public ActorSnapshot(EntityId civilizationId, ControllerKind controllerKind)
        {
            if (!civilizationId.IsValid) throw new ArgumentException("Uninitialized actor identity.");
            CivilizationId = civilizationId; ControllerKind = controllerKind;
        }
    }

    public sealed class RngSnapshot
    {
        public string AlgorithmId { get; }
        public int AlgorithmVersion { get; }
        public ulong State { get; }
        public RngSnapshot(string algorithmId, int algorithmVersion, ulong state)
        { AlgorithmId = algorithmId; AlgorithmVersion = algorithmVersion; State = state; }
    }

    // Read-only projections, not serialization DTOs. Every collection is a copy
    // and every element is immutable. No retained mutable session references.
    public sealed class SessionSnapshot
    {
        public string ScenarioId { get; }
        public string RulesVersion { get; }
        public string ContentHash { get; }
        public SessionPhase Phase { get; }
        public int TurnNumber { get; }
        public long StateRevision { get; }
        public ulong NextEntitySequence { get; }
        public RngSnapshot RngState { get; }
        public IReadOnlyList<EntityIdentitySnapshot> Entities { get; }
        public IReadOnlyList<DefinitionId> DefinitionIds { get; }
        public IReadOnlyList<ActorSnapshot> Actors { get; }
        public IReadOnlyList<EntityId> AcceptedCommandIds { get; }
        public IReadOnlyList<CivilizationSnapshot> Civilizations { get; }
        public IReadOnlyList<StarSystemSnapshot> StarSystems { get; }
        public IReadOnlyList<GalaxyEdge> GalaxyEdges { get; }
        public IReadOnlyList<ColonySnapshot> Colonies { get; }
        public IReadOnlyList<ShipSnapshot> Ships { get; }
        public IReadOnlyList<FleetSnapshot> Fleets { get; }
        public IReadOnlyList<ShipDesignSnapshot> ShipDesigns { get; }
        public IReadOnlyList<CombatEncounterSnapshot> Combats { get; }

        public SessionSnapshot(string scenarioId, string rulesVersion, string contentHash, int turnNumber, long revision,
            ulong nextEntitySequence, RngSnapshot rng, IEnumerable<EntityIdentitySnapshot> entities,
            IEnumerable<DefinitionId> definitions, IEnumerable<ActorSnapshot> actors, IEnumerable<EntityId> commands,
            IEnumerable<CivilizationSnapshot>? civilizations = null, IEnumerable<StarSystemSnapshot>? systems = null,
            IEnumerable<GalaxyEdge>? edges = null, SessionPhase phase = SessionPhase.Planning, IEnumerable<ColonySnapshot>? colonies = null, IEnumerable<ShipSnapshot>? ships = null,
            IEnumerable<FleetSnapshot>? fleets = null, IEnumerable<ShipDesignSnapshot>? shipDesigns = null, IEnumerable<CombatEncounterSnapshot>? combats = null)
        {
            ScenarioId = scenarioId; RulesVersion = rulesVersion; ContentHash = contentHash;
            Phase = phase; TurnNumber = turnNumber; StateRevision = revision; NextEntitySequence = nextEntitySequence;
            RngState = rng ?? throw new ArgumentNullException(nameof(rng));
            Entities = new List<EntityIdentitySnapshot>(entities).AsReadOnly();
            DefinitionIds = new List<DefinitionId>(definitions).AsReadOnly();
            Actors = new List<ActorSnapshot>(actors).AsReadOnly();
            AcceptedCommandIds = new List<EntityId>(commands).AsReadOnly();
            Civilizations = new List<CivilizationSnapshot>(civilizations ?? Array.Empty<CivilizationSnapshot>()).AsReadOnly();
            StarSystems = new List<StarSystemSnapshot>(systems ?? Array.Empty<StarSystemSnapshot>()).AsReadOnly();
            GalaxyEdges = new List<GalaxyEdge>(edges ?? Array.Empty<GalaxyEdge>()).AsReadOnly();
            Colonies = new List<ColonySnapshot>(colonies ?? Array.Empty<ColonySnapshot>()).AsReadOnly();
            Ships = new List<ShipSnapshot>(ships ?? Array.Empty<ShipSnapshot>()).AsReadOnly();
            Fleets = new List<FleetSnapshot>(fleets ?? Array.Empty<FleetSnapshot>()).AsReadOnly();
            ShipDesigns = new List<ShipDesignSnapshot>(shipDesigns ?? Array.Empty<ShipDesignSnapshot>()).AsReadOnly();
            Combats = new List<CombatEncounterSnapshot>(combats ?? Array.Empty<CombatEncounterSnapshot>()).AsReadOnly();
        }
    }
}
