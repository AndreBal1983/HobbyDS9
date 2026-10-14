using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class CampaignIntegratedPlaytestIdentity
    {
        public const int PackageFormat = 3, SaveFormat = 3;
        public const string Profile = "campaign-tactical-v2-integrated-playtest-1";
    }
    public sealed class CampaignIntegratedConstructionTemplateDto
    {
        public string? ShipDesignDefinitionId { get; set; }
        // Exact immutable authored provision, including fit revision and initial stocks.
        public string? SourceProvisionShipId { get; set; }
    }
    public sealed class CampaignIntegratedPlaytestPackageDto
    {
        public int? FormatVersion { get; set; }
        public string? ProfileId { get; set; }
        public string? PackageHash { get; set; }
        public string? PolicyStatus { get; set; }
        public bool? DefaultActivationAllowed { get; set; }
        public CampaignTacticalTravelPackageDto? Physical { get; set; }
        public List<CampaignIntegratedConstructionTemplateDto>? ConstructionTemplates { get; set; }
    }
    public sealed class CampaignIntegratedBuiltShipDto
    {
        public string? ShipId { get; set; }
        public string? ShipDesignDefinitionId { get; set; }
        public string? SourceProvisionShipId { get; set; }
        public string? OwnerCivilizationId { get; set; }
        public string? FleetIdAtConstruction { get; set; }
        public string? QueueItemId { get; set; }
        public int? TurnBuilt { get; set; }
    }
    public sealed class CampaignIntegratedPlaytestSaveDto
    {
        public int FormatVersion { get; set; }
        public string ProfileId { get; set; } = "";
        public string PackageHash { get; set; } = "";
        public CampaignTacticalTravelSaveDto? Physical { get; set; }
        // Append-only provenance, retained even when the produced ship is destroyed.
        public List<CampaignIntegratedBuiltShipDto>? BuiltShips { get; set; }
    }
}
