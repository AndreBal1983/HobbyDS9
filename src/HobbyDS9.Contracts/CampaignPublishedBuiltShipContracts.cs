namespace HobbyDS9.Contracts
{
    // Detached candidate provenance only. Deliberately NOT a field of any existing save root.
    // A future profile must reconstruct and compare this through authoritative replay.
    public sealed class CampaignPublishedBuiltShipDto
    {
        public string? ShipId { get; set; }
        public string? BlueprintId { get; set; }
        public int? BlueprintRevision { get; set; }
        public string? OwnerCivilizationId { get; set; }
        public string? FleetIdAtConstruction { get; set; }
        public string? QueueItemId { get; set; }
        public int? TurnBuilt { get; set; }
        public string? SourceCompilerId { get; set; }
        public string? SourceRulesVersion { get; set; }
        public string? SourceContentHash { get; set; }
        public string? PublicationHash { get; set; }
        public string? SupportCatalogueId { get; set; }
        public int? SupportCatalogueRevision { get; set; }
        public string? SupportHash { get; set; }
        public string? SupportCompilerId { get; set; }
        public string? FitId { get; set; }
        public int? FitRevision { get; set; }
    }
}
