using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class NaturalGalaxyIdentity
    {
        public const int FormatVersion = 4;
        public const string Profile = "generated-galaxy-review-v1";
        public const string GeneratorVersion = "natural-environment-regions-v5";
        public const string TuningVersion = "provisional-natural-star-field-v1";
    }

    public sealed class NaturalGalaxySettingsDto
    {
        public ScaledGalaxySettingsDto Map { get; set; } = null!;
        public int IsolatedStarSharePermille { get; set; } = 100;
        public int AssociationSpreadPermille { get; set; } = 1000;
    }

    public sealed class NaturalGalaxyTuningDto
    {
        public EnvironmentGalaxyTuningDto Environment { get; set; } = null!;
        public int IsolatedStarSeparation { get; set; }
        public int MinimumAssociationSize { get; set; }
        public int MaximumAssociationSize { get; set; }
        public int MinimumAssociationRadius { get; set; }
        public int MaximumAssociationRadius { get; set; }
        public int MinimumAssociationSeparation { get; set; }
        public int MaximumAssociationSeparation { get; set; }
        public int ExtraSiteSeparation { get; set; }
        public int StellarPlacementPressurePermille { get; set; }
        public int QuietDisturbanceThreshold { get; set; }
    }

    public enum NaturalStarGroupKind { Isolated = 1, LooseAssociation = 2 }
    public sealed class NaturalStarGroupDto
    {
        public string GroupId { get; set; } = "";
        public NaturalStarGroupKind Kind { get; set; }
        public GeneratedGalaxyPointDto Center { get; set; } = null!;
        public int RadiusX { get; set; }
        public int RadiusY { get; set; }
        public List<string> StarFeatureIds { get; set; } = null!;
    }

    public sealed class NaturalGalaxyDocumentDto
    {
        public int FormatVersion { get; set; }
        public string ProfileId { get; set; } = "";
        public string GeneratorVersion { get; set; } = "";
        public string TuningVersion { get; set; } = "";
        public string ContentHash { get; set; } = "";
        public NaturalGalaxySettingsDto Settings { get; set; } = null!;
        public NaturalGalaxyTuningDto Tuning { get; set; } = null!;
        public List<EnvironmentGalaxyFeatureDto> Features { get; set; } = null!;
        public List<NaturalStarGroupDto> StarGroups { get; set; } = null!;
        public EnvironmentGalaxyLayoutDto Layout { get; set; } = null!;
        public List<EnvironmentGalaxyRegionInfluenceDto> RegionInfluences { get; set; } = null!;
    }
}
