using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class FoundationVersions
    {
        public const int SaveFormatVersion = 1;
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

    // Identity-only catalog/state. No civilization, colony, ship or turn behavior
    // is implemented by these headers. Future gameplay schemas must be versioned.
    public sealed class DefinitionCatalogDto
    {
        public List<string> DefinitionIds { get; set; } = new List<string>();
    }

    public sealed class GameStateDto
    {
        public int TurnNumber { get; set; }
        public long StateRevision { get; set; }
        public ulong NextEntitySequence { get; set; }
        public RngStateDto RngState { get; set; } = new RngStateDto();
        public List<EntityIdentityDto> EntityIdentities { get; set; } = new List<EntityIdentityDto>();
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
