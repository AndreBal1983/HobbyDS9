using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class GeneratedGalaxyIdentity
    {
        public const int FormatVersion = 1;
        public const string Profile = "generated-galaxy-review-v1";
        public const string GeneratorVersion = "irregular-regions-v1";
        public const string TuningVersion = "provisional-review-30-40-50-v1";
    }

    public sealed class GeneratedGalaxySettingsDto
    {
        // Canonical unsigned 64-bit decimal; a string avoids JSON/JavaScript precision loss.
        public string Seed { get; set; } = "";
        public int SystemCount { get; set; } = 40;
        public List<string> HomeCivilizationDefinitionIds { get; set; } = null!;
    }

    // Presentation coordinates only. Origin upper left; X right, Y down.
    public sealed class GeneratedGalaxyPointDto
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public sealed class GeneratedGalaxyRegionDto
    {
        public string NodeId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? SystemId { get; set; }
        public GeneratedGalaxyPointDto Center { get; set; } = null!;
        // Clockwise in screen coordinates; unclosed polygon (last-to-first implicit).
        public List<GeneratedGalaxyPointDto> Boundary { get; set; } = null!;
    }

    public sealed class GeneratedGalaxySystemDto
    {
        public string SystemId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string NodeId { get; set; } = "";
    }

    public sealed class GeneratedGalaxyHomeCandidateDto
    {
        public string CivilizationDefinitionId { get; set; } = "";
        public string SystemId { get; set; } = "";
        public string NodeId { get; set; } = "";
    }

    // Separate map document, never a campaign save or actor-filtered observation.
    // All fields are required on open; unsupported versions fail without migration.
    public sealed class GeneratedGalaxyDocumentDto
    {
        public int FormatVersion { get; set; }
        public string ProfileId { get; set; } = "";
        public string GeneratorVersion { get; set; } = "";
        public string TuningVersion { get; set; } = "";
        public string ContentHash { get; set; } = "";
        public GeneratedGalaxySettingsDto Settings { get; set; } = null!;
        public int Width { get; set; }
        public int Height { get; set; }
        public List<GeneratedGalaxyRegionDto> Regions { get; set; } = null!;
        public List<GeneratedGalaxySystemDto> Systems { get; set; } = null!;
        public List<NavigationConnectionDto> Connections { get; set; } = null!;
        public List<GeneratedGalaxyHomeCandidateDto> HomeCandidates { get; set; } = null!;
    }
}
