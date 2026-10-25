using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class CampaignStreamedActivitiesIdentity
    {
        public const int Format=10;
        public const string Profile="generated-campaign-activities-streamed-v1";
        public const string Policy="generated-activities-streamed-production-v1";
        public const string PackageProfile="generated-activities-streamed-package-v1";
        public const string EmbeddedCoreProfile="generated-campaign-owned-core-activities-streamed-v1";
    }
    public sealed class CampaignStreamedActivitiesPackageDto
    {
        public int FormatVersion {get;set;}=1;
        public string ProfileId {get;set;}=CampaignStreamedActivitiesIdentity.PackageProfile;
        public string PackageHash {get;set;}="";
        public CampaignActivitiesPackageDto? ContentPackage {get;set;}
    }
    // No prepaid carrier: resources are consumed only against actual completed work.
    public sealed class StreamedProductionItemDto
    {
        public string QueueItemId {get;set;}="";
        public string ColonyId {get;set;}="";
        public string OwnerId {get;set;}="";
        public ActivityProductionKind Kind {get;set;}
        public string DefinitionId {get;set;}="";
        public BlueprintReferenceDto? Blueprint {get;set;}
        public long RequiredIndustry {get;set;}
        public long CompletedIndustry {get;set;}
        public List<ResourceAmountDto>? RequiredResources {get;set;}=new List<ResourceAmountDto>();
        public List<ResourceAmountDto>? ConsumedResources {get;set;}=new List<ResourceAmountDto>();
    }
    public sealed class StreamedProductionReceiptDto
    {
        public StreamedProductionItemDto? Order {get;set;}
        public bool Completed {get;set;}
        public int ResolvedTurn {get;set;}
        public string? ShipId {get;set;}
    }
    public sealed class CampaignStreamedActivitiesSaveDto
    {
        public int FormatVersion {get;set;}=CampaignStreamedActivitiesIdentity.Format;
        public string ProfileId {get;set;}=CampaignStreamedActivitiesIdentity.Profile;
        public string PolicyId {get;set;}=CampaignStreamedActivitiesIdentity.Policy;
        public string TemplatePackageHash {get;set;}="";
        public string PackageHash {get;set;}="";
        public string SupportHash {get;set;}="";
        public NaturalGalaxyDocumentDto? Map {get;set;}
        public CampaignPlayableFleetSaveDto? Campaign {get;set;}
        public List<CampaignFrontierReceiptDto>? Discoveries {get;set;}
        public List<CampaignPublishedBuiltShipDto>? PublishedBuiltShips {get;set;}
        public CampaignActivityStateDto? Activities {get;set;}
        public List<StreamedProductionItemDto>? Production {get;set;}
        public List<StreamedProductionReceiptDto>? ProductionHistory {get;set;}
        public List<CampaignActivityOperationDto>? Journal {get;set;}
    }
    public sealed class StreamedOwnColonySupplyDto
    {
        public string ColonyId {get;set;}="";
        public List<ResourceAmountDto> Stocks {get;set;}=new List<ResourceAmountDto>();
        public List<LogisticsDeliveryDto> Deliveries {get;set;}=new List<LogisticsDeliveryDto>();
    }
    public sealed class CampaignStreamedActivityObservationDto
    {
        public CampaignActivityObservationDto Activities {get;set;}=new CampaignActivityObservationDto();
        public List<StreamedProductionItemDto> Production {get;set;}=new List<StreamedProductionItemDto>();
        public List<StreamedOwnColonySupplyDto> Supply {get;set;}=new List<StreamedOwnColonySupplyDto>();
        public List<StreamedProductionAvailabilityDto> Availability {get;set;}=new List<StreamedProductionAvailabilityDto>();
    }
    public sealed class StreamedProductionAvailabilityDto
    {
        public string QueueItemId {get;set;}="";
        public long WorkWithCurrentStocks {get;set;}
        public string Status {get;set;}="";
    }
}
