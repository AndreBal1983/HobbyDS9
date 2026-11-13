using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class CampaignGeneratedIdentity
    {
        public const int Format = 7;
        public const string Profile = "generated-campaign-v1";
        public const string Policy = "generated-neighbor-exploration-v1";
        public const string EmbeddedCoreProfile = "generated-campaign-owned-core-v1";
    }

    // Trusted host/store boundary only. Never pass this tree to a renderer or AI planner.
    public sealed class CampaignGeneratedSaveDto
    {
        public int FormatVersion { get; set; }
        public string ProfileId { get; set; } = "";
        public string PolicyId { get; set; } = "";
        public string TemplatePackageHash { get; set; } = "";
        public string PackageHash { get; set; } = "";
        public NaturalGalaxyDocumentDto Map { get; set; } = null!;
        public CampaignPlayableFleetSaveDto Campaign { get; set; } = null!;
        public List<CampaignFrontierReceiptDto> Discoveries { get; set; } = null!;
    }

    // Detached, actor-filtered geometry, not a generated document or command authority.
    public sealed class CampaignGeneratedMapDto
    {
        public string ActorId { get; set; } = "";
        public string MapContentHash { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public int ObservationTurn { get; set; }
        public long CampaignRevision { get; set; }
        // Local coverage means no ordinary contact was detected; it does not disprove stealth.
        public List<string> ObservedNodeIds { get; set; } = null!;
        public List<GeneratedGalaxyRegionDto> Regions { get; set; } = null!;
        public List<NavigationConnectionDto> Connections { get; set; } = null!;
        public List<EnvironmentGalaxyFeatureDto> Features { get; set; } = null!;
    }
}
