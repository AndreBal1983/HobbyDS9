using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class EnvironmentGalaxyIdentity
    {
        public const int FormatVersion = 2;
        public const string Profile = "generated-galaxy-review-v1";
        public const string GeneratorVersion = "environment-first-regions-v3";
        public const string TuningVersion = "provisional-environment-influence-v1";
    }

    public enum GalaxyEnvironmentFeatureKind { Star = 1, BlackHole = 2, Nebula = 3 }

    public sealed class EnvironmentGalaxySettingsDto
    {
        public GeneratedGalaxySettingsDto Map { get; set; } = null!;
        // Persisted generation inputs for controlled comparisons; not visibility/gameplay switches.
        public bool StellarInfluenceEnabled { get; set; }
        public bool BlackHoleInfluenceEnabled { get; set; }
        public bool NebulaInfluenceEnabled { get; set; }
    }

    public sealed class EnvironmentGalaxyFeatureDto
    {
        public string FeatureId { get; set; } = "";
        public GalaxyEnvironmentFeatureKind Kind { get; set; }
        public string DisplayName { get; set; } = "";
        public GeneratedGalaxyPointDto Position { get; set; } = null!;
        // Axis-aligned ellipse of generation influence, NOT physical extent or sensor coverage.
        public int RadiusX { get; set; }
        public int RadiusY { get; set; }
        public int DensityWeight { get; set; }
        public string? SystemId { get; set; }
        // Star/black-hole mandatory site. A nebula footprint spans cells and has no single node.
        public string? NodeId { get; set; }
    }

    public sealed class EnvironmentGalaxyTuningDto
    {
        public int BaseDensity { get; set; }
        public int MaximumDensity { get; set; }
        public int MinimumSiteSeparation { get; set; }
        public int MinimumStarSeparation { get; set; }
        public int OuterMargin { get; set; }
        public int BlackHoleCount { get; set; }
        public int NebulaCount { get; set; }
        public int CandidateDrawsPerRegion { get; set; }
        public int PlacementAttempts { get; set; }
    }

    public sealed class EnvironmentGalaxyContributionDto
    {
        public string FeatureId { get; set; } = "";
        public int Amount { get; set; }
    }

    public sealed class EnvironmentGalaxyRegionInfluenceDto
    {
        public string NodeId { get; set; } = "";
        public int BaseDensity { get; set; }
        public int TargetDensity { get; set; }
        public List<EnvironmentGalaxyContributionDto> Contributions { get; set; } = null!;
    }

    public sealed class EnvironmentGalaxyLayoutDto
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<GeneratedGalaxyRegionDto> Regions { get; set; } = null!;
        public List<GeneratedGalaxySystemDto> Systems { get; set; } = null!;
        public List<NavigationConnectionDto> Connections { get; set; } = null!;
        public List<GeneratedGalaxyHomeCandidateDto> HomeCandidates { get; set; } = null!;
    }

    public sealed class EnvironmentGalaxyDocumentDto
    {
        public int FormatVersion { get; set; }
        public string ProfileId { get; set; } = "";
        public string GeneratorVersion { get; set; } = "";
        public string TuningVersion { get; set; } = "";
        public string ContentHash { get; set; } = "";
        public EnvironmentGalaxySettingsDto Settings { get; set; } = null!;
        public EnvironmentGalaxyTuningDto Tuning { get; set; } = null!;
        public List<EnvironmentGalaxyFeatureDto> Features { get; set; } = null!;
        public EnvironmentGalaxyLayoutDto Layout { get; set; } = null!;
        public List<EnvironmentGalaxyRegionInfluenceDto> RegionInfluences { get; set; } = null!;
    }
}
