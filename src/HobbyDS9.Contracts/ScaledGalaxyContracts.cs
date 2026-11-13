using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class ScaledGalaxyIdentity
    {
        public const int FormatVersion = 3;
        public const string Profile = "generated-galaxy-review-v1";
        public const string GeneratorVersion = "environment-clustered-regions-v4";
        public const string TuningVersion = "provisional-quiet-empty-area-v1";
        public const int MaximumJsonBytes = 16 * 1024 * 1024;
        public const int MaximumSystems = 1000;
        public const int MaximumRegions = 2000;
        public const int MaximumConnections = 6000;
        public const int MaximumFeatures = 1250;
        public const int MaximumVerticesPerRegion = 64;
        public const int MaximumTotalVertices = 20000;
        public const int MaximumTotalContributions = 24000;
        public const int MaximumCoordinate = 5000000;
    }

    public sealed class ScaledGalaxySettingsDto
    {
        public string Seed { get; set; } = "";
        public int SystemCount { get; set; } = 250;
        public int EmptyRegionCount { get; set; } = 250;
        public int Width { get; set; } = 2500000;
        public int Height { get; set; } = 2000000;
        // Soft statistical target, NOT a promise about every empty cell.
        public int QuietEmptyAreaTargetPermille { get; set; } = 2000;
        // Counts per 1,000,000 x 1,000,000 coordinate area; resolved counts persist in Tuning.
        public int BlackHolesPerBaselineArea { get; set; } = 2;
        public int NebulaePerBaselineArea { get; set; } = 3;
        public List<string> HomeCivilizationDefinitionIds { get; set; } = null!;
        public bool StellarInfluenceEnabled { get; set; } = true;
        public bool BlackHoleInfluenceEnabled { get; set; } = true;
        public bool NebulaInfluenceEnabled { get; set; } = true;
    }

    public sealed class ScaledGalaxyTuningDto
    {
        public EnvironmentGalaxyTuningDto Environment { get; set; } = null!;
        public int StarClusterSize { get; set; }
        public int ClusterRadius { get; set; }
        public int MinimumClusterSeparation { get; set; }
        public int ExtraSiteSeparation { get; set; }
        public int QuietDisturbanceThreshold { get; set; }
    }

    public sealed class ScaledGalaxyDocumentDto
    {
        public int FormatVersion { get; set; }
        public string ProfileId { get; set; } = "";
        public string GeneratorVersion { get; set; } = "";
        public string TuningVersion { get; set; } = "";
        public string ContentHash { get; set; } = "";
        public ScaledGalaxySettingsDto Settings { get; set; } = null!;
        public ScaledGalaxyTuningDto Tuning { get; set; } = null!;
        public List<EnvironmentGalaxyFeatureDto> Features { get; set; } = null!;
        public EnvironmentGalaxyLayoutDto Layout { get; set; } = null!;
        public List<EnvironmentGalaxyRegionInfluenceDto> RegionInfluences { get; set; } = null!;
    }
}
