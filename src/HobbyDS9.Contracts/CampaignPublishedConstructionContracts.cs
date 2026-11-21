using System.Collections.Generic;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Contracts
{
    // Explicit portable opt-in only; no legacy carrier, default or codec registration.
    public static class CampaignPublishedConstructionIdentity
    {
        public const int Format=8, PackageFormat=1;
        public const string Profile="generated-campaign-published-construction-v1";
        public const string Policy="generated-static-centres-published-support-v1";
        public const string PackageProfile="generated-published-construction-package-v1";
        public const string EmbeddedCoreProfile="generated-campaign-owned-core-published-construction-v1";
    }
    public sealed class CampaignPublishedConstructionPackageDto
    {
        public int? FormatVersion { get; set; }
        public string? ProfileId { get; set; }
        public string? PackageHash { get; set; }
        public string? PolicyStatus { get; set; }
        public bool? DefaultActivationAllowed { get; set; }
        public CampaignPlayableFleetPackageDto? BaseTemplate { get; set; }
        public TacticalSupportCatalogueDto? Support { get; set; }
        public int? RetainedRecordBudget { get; set; }
    }
    public sealed class CampaignPublishedSupportIdentityDto
    {
        public string? SchemaId { get; set; }
        public string? CatalogueId { get; set; }
        public int? Revision { get; set; }
        public string? SupportHash { get; set; }
        public string? SupportCompilerId { get; set; }
        public string? SourceCompilerId { get; set; }
        public string? CanonicalEncodingId { get; set; }
    }
    public sealed class CampaignPublishedConstructionSaveDto
    {
        public int FormatVersion { get; set; }
        public string? ProfileId { get; set; }
        public string? PolicyId { get; set; }
        public string? TemplatePackageHash { get; set; }
        public string? PackageHash { get; set; }
        public CampaignPublishedSupportIdentityDto? SupportIdentity { get; set; }
        public NaturalGalaxyDocumentDto? Map { get; set; }
        public CampaignPlayableFleetSaveDto? Campaign { get; set; }
        public List<CampaignFrontierReceiptDto>? Discoveries { get; set; }
        public List<CampaignPublishedBuiltShipDto>? PublishedBuiltShips { get; set; }
    }
}
