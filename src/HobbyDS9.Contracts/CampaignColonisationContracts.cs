using System.Collections.Generic;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Contracts
{
    public static class CampaignColonisationIdentity
    {
        public const int Format=11;
        public const string Profile="generated-campaign-colonisation-v1",Policy="planetary-settlement-streamed-v1";
        public const string PackageProfile="generated-colonisation-package-v1",EmbeddedCoreProfile="generated-campaign-owned-core-colonisation-v1";
        public const string PopulationPolicy="conserved-settler-transport-v1",InventoryPolicy="planetary-inventory-all-systems-v1";
        public const string Rules="strategic-colonisation-planetary-v1";
        public const string InventoryAuthoring="colonisation-planets-v1;temperate-arid-oceanic-frozen-vacuum;natural=1000,1000,1000,1000,0;dilithium=0,120,240;four-nearby-primaries=240;ilex=wetland;namar=temperate;minor-gravity=100;pressure=100;radiation=10;atmosphere=oxygen";
    }
    public enum PlanetControllerKind { Unclaimed=0,Empire=1,IndependentGovernment=2 }
    public enum ColonyEstablishmentStage { Founding=1,Established=2,EmptyOutpost=3 }
    public enum SettlerManifestKind { Settlement=1,ReturnOnly=2,SupplyOnly=3 }
    public enum SettlerManifestStage { Loaded=1,Deployed=2,Unloaded=3,Destroyed=4,Delivered=5 }
    public enum SettlementTerminalKind { Established=1,Evacuated=2,Extinct=3 }
    public enum PlanetSurveyStage { Active=1,Completed=2,Cancelled=3,Interrupted=4 }
    public sealed class PlanetControlDto
    {
        public string PlanetId {get;set;}="";
        public PlanetControllerKind Kind {get;set;}
        public string? ActorId {get;set;}
    }
    public sealed class PlanetBodyBindingDto
    {public string PlanetId {get;set;}="";public string BodyId {get;set;}="";public string SystemId {get;set;}="";public string? MinorActorId {get;set;}}
    public sealed class FrozenColonisationPlanetsDto
    {
        public string PolicyId {get;set;}=CampaignColonisationIdentity.InventoryPolicy;
        public string ContentHash {get;set;}="";
        public List<PlanetDefinitionDto>? Definitions {get;set;}=new List<PlanetDefinitionDto>();
        public List<PlanetStateDto>? InitialPlanets {get;set;}=new List<PlanetStateDto>();
        public List<PlanetBodyBindingDto>? Bodies {get;set;}=new List<PlanetBodyBindingDto>();
        public List<PlanetControlDto>? GenesisControls {get;set;}=new List<PlanetControlDto>();
    }
    public sealed class PlanetSurveyDto
    {
        public string SurveyId {get;set;}="";public string ActorId {get;set;}="";public string PlanetId {get;set;}="";public string ShipId {get;set;}="";
        public PlanetSurveyStage Stage {get;set;}public int CompletedWeeks {get;set;}public int StartedTurn {get;set;}public int? ResolvedTurn {get;set;}
    }
    public sealed class ColonisationPlanetObservationDto
    {
        public string PlanetId {get;set;}="";public string BodyId {get;set;}="";public string SystemId {get;set;}="";public string DisplayName {get;set;}="";
        public bool Surveyed {get;set;}public PlanetDefinitionDto? Environment {get;set;}public PlanetControlDto? KnownControl {get;set;}
        public string? ActiveSurveyId {get;set;}public int CompletedSurveyWeeks {get;set;}
    }
    public sealed class SettlerManifestDto
    {
        public string ManifestId {get;set;}="";public string OwnerId {get;set;}="";public string ShipId {get;set;}="";
        public SettlerManifestKind Kind {get;set;}public SettlerManifestStage Stage {get;set;}
        public BlueprintReferenceDto? Blueprint {get;set;}
        public string FitId {get;set;}="";public string SupportHash {get;set;}="";
        public PopulationCohortDto? Passengers {get;set;}
        public long Food {get;set;}public int FoodShortageWeeks {get;set;}
        public List<ResourceAmountDto>? Cargo {get;set;}=new List<ResourceAmountDto>();
        public long FoodConsumed {get;set;}public long FoodDelivered {get;set;}public long FoodLost {get;set;}public long PassengerDeaths {get;set;}
        public long PopulationTransferred {get;set;}public int EquipmentConsumed {get;set;}
        public List<ResourceAmountDto>? MaterialsDelivered {get;set;}=new List<ResourceAmountDto>();
        public List<ResourceAmountDto>? MaterialsLost {get;set;}=new List<ResourceAmountDto>();
        public int LastSurvivalTurn {get;set;}public string LatestReport {get;set;}="";
    }
    public enum ManifestOriginKind { Passenger=1,Supply=2 }
    public sealed class ManifestOriginLineageDto
    {
        public string ManifestId {get;set;}="";public string SourceColonyId {get;set;}="";public string? SourceCohortId {get;set;}
        public string? EmbarkedCohortId {get;set;}public ManifestOriginKind Kind {get;set;}public long EmbarkedPopulation {get;set;}public int Turn {get;set;}
        public long LoadedFood {get;set;}public List<ResourceAmountDto>? LoadedMaterials {get;set;}=new List<ResourceAmountDto>();
    }
    public sealed class ManifestResolutionDto
    {
        public string ManifestId {get;set;}="";public SettlerManifestStage Stage {get;set;}public int Turn {get;set;}
        public string? DestinationPlanetId {get;set;}public long PopulationTransferred {get;set;}public long PopulationLost {get;set;}
        public long FoodTransferred {get;set;}public long FoodLost {get;set;}public List<ResourceAmountDto>? CargoTransferred {get;set;}=new List<ResourceAmountDto>();
        public List<ResourceAmountDto>? CargoLost {get;set;}=new List<ResourceAmountDto>();
    }
    public sealed class ColonyOriginDto
    {public string ColonyId {get;set;}="";public string PlanetId {get;set;}="";public string ManifestId {get;set;}="";public int Turn {get;set;}}
    public sealed class LandedCohortLineageDto
    {public string ManifestId {get;set;}="";public string ColonyId {get;set;}="";public string CohortId {get;set;}="";public long Population {get;set;}public int Turn {get;set;}}
    public sealed class SettlementCampDto
    {
        public string CampId {get;set;}="";public string ColonyId {get;set;}="";public string ManifestId {get;set;}="";
        public string OwnerId {get;set;}="";public ColonyEstablishmentStage Stage {get;set;}
        public int DeployedTurn {get;set;}public int FedWeeks {get;set;}public long CompletedIndustry {get;set;}
        public long FoodConsumed {get;set;}public long Deaths {get;set;}public string LatestReport {get;set;}="";
    }
    public sealed class SettlementCampTerminalDto
    {public string CampId {get;set;}="";public SettlementTerminalKind Kind {get;set;}public int Turn {get;set;}public string? ReturnManifestId {get;set;}}
    public sealed class SettlementDeploymentIntentDto
    {public string IntentId {get;set;}="";public string ManifestId {get;set;}="";public string PlanetId {get;set;}="";public string ActorId {get;set;}="";public bool Disputed {get;set;}}
    public sealed class CampaignColonisationStateDto
    {
        public List<PlanetControlDto>? PlanetControls {get;set;}=new List<PlanetControlDto>();
        public List<PlanetBodyBindingDto>? Bodies {get;set;}=new List<PlanetBodyBindingDto>();
        public List<PlanetSurveyDto>? Surveys {get;set;}=new List<PlanetSurveyDto>();
        public List<SettlerManifestDto>? ManifestRecords {get;set;}=new List<SettlerManifestDto>();
        public List<ManifestOriginLineageDto>? ManifestOrigins {get;set;}=new List<ManifestOriginLineageDto>();
        public List<ManifestResolutionDto>? ManifestResolutions {get;set;}=new List<ManifestResolutionDto>();
        public List<ColonyOriginDto>? ColonyOrigins {get;set;}=new List<ColonyOriginDto>();
        public List<LandedCohortLineageDto>? LandedCohortLineages {get;set;}=new List<LandedCohortLineageDto>();
        public List<SettlementCampDto>? CampSources {get;set;}=new List<SettlementCampDto>();
        public List<SettlementCampTerminalDto>? CampTerminals {get;set;}=new List<SettlementCampTerminalDto>();
        public List<SettlementDeploymentIntentDto>? DeploymentIntents {get;set;}=new List<SettlementDeploymentIntentDto>();
        public int LastSettlementTurn {get;set;}
    }
    public sealed class ColonisationPolicyDto
    {
        public string Identity {get;set;}="";public string PlanetInventoryPolicy {get;set;}="";
        public int MaximumPlanets {get;set;}public int MaximumStarterPlanets {get;set;}public long MaximumValidationVisits {get;set;}public long MaximumCanonicalRootBytes {get;set;}public int RecordBudget {get;set;}public int JournalBudget {get;set;}
        public int Settlers {get;set;}public int SourcePopulationFloor {get;set;}public int MaximumSourceCohortBps {get;set;}public int MinimumFood {get;set;}public int MaximumFood {get;set;}
        public string EquipmentResourceId {get;set;}="";public long KitIndustry {get;set;}public List<ResourceAmountDto>? KitResources {get;set;}=new List<ResourceAmountDto>();
        public long EstablishmentIndustry {get;set;}public int MinimumFedWeeks {get;set;}public InfrastructureDefinitionDto? Camp {get;set;}public long EstablishedFoodOutput {get;set;}
        public ColonisationInfrastructureRecipeDto? Mine {get;set;}
    }
    public sealed class ColonisationInfrastructureRecipeDto
    {public InfrastructureDefinitionDto? Infrastructure {get;set;}public ShipDesignCostDto? Cost {get;set;}}
    public sealed class CampaignColonisationPackageDto
    {
        public int FormatVersion {get;set;}=1;public string ProfileId {get;set;}=CampaignColonisationIdentity.PackageProfile;public string PackageHash {get;set;}="";
        public CampaignActivitiesPackageDto? Activities {get;set;}public ShipDesignCatalogueDto? Catalogue {get;set;}public List<ShipClassDraftDto>? ReadyClasses {get;set;}public ColonisationSupportCatalogueDto? Support {get;set;}public ColonisationPolicyDto? Settlement {get;set;}
    }
    public enum ColonisationProductionKind { PublishedShip=1,Infrastructure=2,SettlementKit=3 }
    public sealed class ColonisationProductionItemDto
    {
        public string QueueItemId {get;set;}="";public string ColonyId {get;set;}="";public string OwnerId {get;set;}="";
        public ColonisationProductionKind Kind {get;set;}public string DefinitionId {get;set;}="";public BlueprintReferenceDto? Blueprint {get;set;}
        public long RequiredIndustry {get;set;}public long CompletedIndustry {get;set;}
        public List<ResourceAmountDto>? RequiredResources {get;set;}=new List<ResourceAmountDto>();public List<ResourceAmountDto>? ConsumedResources {get;set;}=new List<ResourceAmountDto>();
    }
    public sealed class ColonisationProductionReceiptDto
    {public ColonisationProductionItemDto? Order {get;set;}public bool Completed {get;set;}public int ResolvedTurn {get;set;}public string? ShipId {get;set;}}
    public enum ColonisationAction { StartPlanetSurvey=1,CancelPlanetSurvey=2,EmbarkSettlers=3,UnloadTransport=4,RequestDeployment=5,WithdrawDeployment=6,EvacuateColony=7,DeliverCarriedSupplies=8,EnqueueKit=9,LoadCarriedSupplies=10 }
    public sealed class PlanetSurveyOrderDto {public string ShipId {get;set;}="";public string PlanetId {get;set;}="";}
    public sealed class CancelPlanetSurveyDto {public string SurveyId {get;set;}="";}
    public sealed class EmbarkSettlersDto
    {public string ShipId {get;set;}="";public string ColonyId {get;set;}="";public string CohortId {get;set;}="";public long Population {get;set;}public long Food {get;set;}public List<ResourceAmountDto>? Materials {get;set;}=new List<ResourceAmountDto>();}
    public sealed class UnloadSettlersDto {public string ManifestId {get;set;}="";public string ColonyId {get;set;}="";}
    public sealed class DeploySettlersDto {public string ManifestId {get;set;}="";public string PlanetId {get;set;}="";}
    public sealed class WithdrawDeploymentDto {public string IntentId {get;set;}="";}
    public sealed class EvacuateFoundingColonyDto {public string CampId {get;set;}="";public string ShipId {get;set;}="";}
    public sealed class DeliverCarriedSuppliesDto
    {public string ManifestId {get;set;}="";public string ColonyId {get;set;}="";public long Food {get;set;}public List<ResourceAmountDto>? Materials {get;set;}=new List<ResourceAmountDto>();}
    public sealed class LoadCarriedSuppliesDto
    {public string ShipId {get;set;}="";public string SourceColonyId {get;set;}="";public long Food {get;set;}public List<ResourceAmountDto>? Materials {get;set;}=new List<ResourceAmountDto>();}
    public sealed class EnqueueSettlementKitDto {public string ColonyId {get;set;}="";}
    public sealed class ColonisationCommandDto
    {
        public string CommandId {get;set;}="";public string ActorId {get;set;}="";public int Turn {get;set;}public long Revision {get;set;}public ColonisationAction Action {get;set;}
        public PlanetSurveyOrderDto? StartSurvey {get;set;}public CancelPlanetSurveyDto? CancelSurvey {get;set;}
        public EmbarkSettlersDto? Embark {get;set;}public UnloadSettlersDto? Unload {get;set;}public DeploySettlersDto? Deploy {get;set;}
        public WithdrawDeploymentDto? Withdraw {get;set;}public EvacuateFoundingColonyDto? Evacuate {get;set;}
        public DeliverCarriedSuppliesDto? Deliver {get;set;}public EnqueueSettlementKitDto? EnqueueKit {get;set;}public LoadCarriedSuppliesDto? LoadSupplies {get;set;}
    }
    public sealed class ColonisationOperationDto
    {public ColonisationCommandDto? Colonisation {get;set;}public CampaignActivityOperationDto? Existing {get;set;}}
    // Exact ownership ERA of the already retained physical MovementProof.Start.
    public sealed class ColonisationMovementOwnershipDto
    {public int Turn {get;set;}public long Revision {get;set;}public List<PlanetControlDto>? PlanetControls {get;set;}=new List<PlanetControlDto>();public List<ColonisationColonyStageDto>? ColonyStages {get;set;}=new List<ColonisationColonyStageDto>();}
    public sealed class ColonisationColonyStageDto
    {public string ColonyId {get;set;}="";public ColonyEstablishmentStage Stage {get;set;}}
    public enum DeploymentBoundaryOutcome { Deployed=1,Disputed=2,Interrupted=3 }
    public sealed class ColonisationDeploymentReceiptDto
    {
        public SettlementDeploymentIntentDto? Intent {get;set;}public SettlerManifestDto? ManifestBefore {get;set;}
        public DeploymentBoundaryOutcome Outcome {get;set;}
        public string? ColonyId {get;set;}public string? CampId {get;set;}public string? LandedCohortId {get;set;}
        public long PopulationTransferred {get;set;}public long FoodTransferred {get;set;}public int EquipmentConsumed {get;set;}
        public List<ResourceAmountDto>? MaterialsTransferred {get;set;}=new List<ResourceAmountDto>();
    }
    // LATEST exact boundary only, never append one full proof per week. Later Planning state is not this historical After.
    public sealed class ColonisationSettlementProofDto
    {
        public int Turn {get;set;}public GameStateDto? BeforeGameState {get;set;}public GameStateDto? AfterGameState {get;set;}
        public CampaignColonisationStateDto? BeforeColonisation {get;set;}
        public CampaignColonisationStateDto? AfterColonisation {get;set;}
        public List<ColonisationDeploymentReceiptDto>? Receipts {get;set;}=new List<ColonisationDeploymentReceiptDto>();
    }
    public sealed class CampaignColonisationSaveDto
    {
        public int FormatVersion {get;set;}=CampaignColonisationIdentity.Format;public string ProfileId {get;set;}=CampaignColonisationIdentity.Profile;public string PolicyId {get;set;}=CampaignColonisationIdentity.Policy;
        public string TemplatePackageHash {get;set;}="";public string PackageHash {get;set;}="";public string SupportHash {get;set;}="";
        public NaturalGalaxyDocumentDto? Map {get;set;}public CampaignPlayableFleetSaveDto? Campaign {get;set;}
        public FrozenColonisationPlanetsDto? FrozenPlanets {get;set;}
        public List<CampaignFrontierReceiptDto>? Discoveries {get;set;}public List<CampaignPublishedBuiltShipDto>? PublishedBuiltShips {get;set;}
        public CampaignActivityStateDto? Activities {get;set;}public CampaignColonisationStateDto? Colonisation {get;set;}
        public List<ColonisationProductionItemDto>? Production {get;set;}public List<ColonisationProductionReceiptDto>? ProductionHistory {get;set;}
        public ColonisationMovementOwnershipDto? MovementOwnership {get;set;}public ColonisationSettlementProofDto? LastSettlementProof {get;set;}
        public List<ColonisationOperationDto>? Journal {get;set;}
    }
}
