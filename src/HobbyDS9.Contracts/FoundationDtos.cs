using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class FoundationVersions
    {
        public const int SaveFormatVersion = 6;
        public const int ContentFormatVersion = 3;
        public const string RandomAlgorithmId = "splitmix64-rejection-v1";
        public const int RandomAlgorithmVersion = 1;
    }

    public sealed class RngStateDto
    {
        public string AlgorithmId { get; set; } = string.Empty;
        public int AlgorithmVersion { get; set; }
        // Nullable distinguishes missing state from the valid seed/state zero.
        public ulong? State { get; set; }
    }

    public sealed class EntityAllocationStateDto
    {
        public ulong NextEntitySequence { get; set; }
    }

    public sealed class EntityIdentityDto
    {
        public string EntityId { get; set; } = string.Empty;
        public string? DefinitionId { get; set; }
    }

    // Versioned content: labels stay here, while mutable facts stay in GameStateDto.
    public sealed class DefinitionCatalogDto
    {
        public int ContentFormatVersion { get; set; }
        public List<CivilizationDefinitionDto>? Civilizations { get; set; }
        public List<StarSystemDefinitionDto>? StarSystems { get; set; }
        public List<GalaxyEdgeDto>? GalaxyEdges { get; set; }
        public List<EconomicProfileDefinitionDto>? EconomicProfiles { get; set; }
        public List<ResearchNodeDefinitionDto>? ResearchNodes { get; set; }
        public List<string> DefinitionIds { get; set; } = new List<string>();
    }

    public sealed class GameStateDto
    {
        // Required since save format 4; missing JSON is not implicitly Planning.
        public SessionPhase? Phase { get; set; }
        public int TurnNumber { get; set; }
        public long StateRevision { get; set; }
        public ulong NextEntitySequence { get; set; }
        public RngStateDto RngState { get; set; } = new RngStateDto();
        public List<EntityIdentityDto> EntityIdentities { get; set; } = new List<EntityIdentityDto>();
        // Required since format 2. Missing JSON must not silently erase deduplication.
        public List<string>? AcceptedCommandIds { get; set; }
        public List<CivilizationStateDto>? Civilizations { get; set; }
        public List<StarSystemStateDto>? StarSystems { get; set; }
        public List<ColonyStateDto>? Colonies { get; set; }
    }

    public sealed class ScenarioDto
    {
        public string ScenarioId { get; set; } = string.Empty;
        public string RulesVersion { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public DefinitionCatalogDto Definitions { get; set; } = new DefinitionCatalogDto();
        public GameStateDto InitialState { get; set; } = new GameStateDto();
    }

    public sealed class SaveEnvelopeDto
    {
        public int SaveFormatVersion { get; set; }
        public string RulesVersion { get; set; } = string.Empty;
        public string ScenarioId { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public GameStateDto GameState { get; set; } = new GameStateDto();
    }
}
