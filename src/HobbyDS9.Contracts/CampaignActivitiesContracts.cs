using System.Collections.Generic;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Contracts
{
    public static class CampaignActivitiesIdentity
    {
        public const int Format=9,PackageFormat=1;
        public const string Profile="generated-campaign-activities-v1",Policy="generated-activities-mixed-production-v1";
        public const string PackageProfile="generated-activities-package-v1",EmbeddedCoreProfile="generated-campaign-owned-core-activities-v1";
    }
    public sealed class CampaignActivitiesPackageDto
    {
        public int FormatVersion {get;set;}=1;
        public string ProfileId {get;set;}=CampaignActivitiesIdentity.PackageProfile;
        public string PackageHash {get;set;}="";
        public CampaignPlayableFleetPackageDto? BaseTemplate {get;set;}
        public ShipDesignCatalogueDto? Catalogue {get;set;}
        public List<ShipClassDraftDto>? ReadyClasses {get;set;}
        public ActivityTacticalSupportCatalogueDto? Support {get;set;}
        public CampaignActivityContentDto? Content {get;set;}
        public int RecordBudget {get;set;}=2048;
    }
    public sealed class CampaignActivityContentDto
    {
        public string ContentId {get;set;}="";
        public int Revision {get;set;}
        public List<ActivityStructureDefinitionDto>? Structures {get;set;}
        public List<ActivitySiteDefinitionDto>? Sites {get;set;}
        public List<ActivityMinorDefinitionDto>? Minors {get;set;}
        public List<ActivitySpeciesDto>? Species {get;set;}
    }
    public sealed class ActivityStructureDefinitionDto
    {
        public string DefinitionId {get;set;}="";
        public string DisplayName {get;set;}="";
        public ShipDesignCostDto? Cost {get;set;}
        public long Industry {get;set;}
        public long Research {get;set;}
        public long FoodOutput {get;set;}
        public long PowerDemand {get;set;}
    }
    public sealed class ActivitySiteDefinitionDto
    {
        public string DefinitionId {get;set;}="";
        public string Title {get;set;}="";
        public string DiscoveryText {get;set;}="";
        public List<ActivityChoiceDefinitionDto>? Choices {get;set;}
    }
    public sealed class ActivityChoiceDefinitionDto
    {
        public string ChoiceId {get;set;}="";
        public string FindingId {get;set;}="";
        public string Caption {get;set;}="";
        public string Report {get;set;}="";
        public int RequiredStudy {get;set;}
        public int RequiredContact {get;set;}
        public int Work {get;set;}
        public int Deadline {get;set;}
        public long Credits {get;set;}
        public ScienceDiscipline Discipline {get;set;}
        public long Insight {get;set;}
        public int HullRiskBps {get;set;}
        public int HullDamage {get;set;}
        public string ContactOutcome {get;set;}="";
        public ActivityMinorRelation? RequiredRelation {get;set;}
        public ActivityMinorRelation? AcceptedRelation {get;set;}
        public ActivityMinorRelation? RefusedRelation {get;set;}
        public int ConsentChanceBps {get;set;}=10000;
    }
    public sealed class ActivityMinorDefinitionDto
    {
        public string DefinitionId {get;set;}="";
        public string DisplayName {get;set;}="";
        public string SpeciesName {get;set;}="";
        public string SpeciesId {get;set;}="";
        public bool WarpCapable {get;set;}
        public ActivitySiteDefinitionDto? Encounter {get;set;}
    }
    public sealed class ActivitySpeciesDto
    {public string SpeciesId {get;set;}="";public string DisplayName {get;set;}="";}
    public sealed class ActivityBodyDto
    {
        public string BodyId {get;set;}="";public string SystemId {get;set;}="";
        public string DisplayName {get;set;}="";public string? GovernmentActorId {get;set;}
    }
    public enum ActivityMinorRelation { Observed=1, Accepted=2, Refused=3, Cooperating=4, Aided=5, Declined=6 }
    public sealed class ActivityMinorRelationDto
    {
        public string ActorId {get;set;}="";public string MinorActorId {get;set;}="";
        public ActivityMinorRelation Relation {get;set;}public int ChangedTurn {get;set;}
    }
    public enum ActivityProductionKind { PublishedShip=1, Infrastructure=2 }
    public sealed class ActivityProductionItemDto
    {
        public string ColonyId {get;set;}="";
        public ActivityProductionKind Kind {get;set;}
        public string DefinitionId {get;set;}="";
        // One ordered payment/progress receipt, also for a structure. Blueprint is null for structures.
        public ProductionQueueItemDto? Paid {get;set;}
    }
    public sealed class ActivityStructureReceiptDto
    {
        public string QueueItemId {get;set;}="";
        public string ColonyId {get;set;}="";
        public string DefinitionId {get;set;}="";
        public string OwnerId {get;set;}="";
        public int CompletedTurn {get;set;}
    }
    public sealed class ActivitySiteStateDto
    {
        public string SiteId {get;set;}="";
        public string DefinitionId {get;set;}="";
        public string NodeId {get;set;}="";
        public string SystemId {get;set;}="";
        public string BodyId {get;set;}="";
        public string? MinorActorId {get;set;}
    }
    public sealed class ActivityMinorStateDto
    {
        public string ActorId {get;set;}="";
        public string DefinitionId {get;set;}="";
        public string BodyId {get;set;}="";
        public long Population {get;set;}
        public string SpeciesId {get;set;}="";
    }
    public enum ActivityProgressStage { Active=1, AwaitingClaim=2, Completed=3, Aborted=4, Failed=5, Expired=6 }
    public sealed class ActivityFindingDto
    {
        public string ActorId {get;set;}="";
        public string SiteId {get;set;}="";
        public string ChoiceId {get;set;}="";
        public string FindingId {get;set;}="";
        public long FindingBudget {get;set;}
        public string ShipId {get;set;}="";
        public string FleetId {get;set;}="";
        public ActivityProgressStage Stage {get;set;}
        public int WorkDone {get;set;}
        public int RemainingAdvances {get;set;}
        public int StartedTurn {get;set;}
        public int? ResolvedTurn {get;set;}
        public long PaidCredits {get;set;}
        public long InsightApplied {get;set;}
        public long InsightUnlearnable {get;set;}
        public List<ResearchReceiptDto>? ResearchReceipts {get;set;}=new List<ResearchReceiptDto>();
        public string Report {get;set;}="";
        public string ContactOutcome {get;set;}="";
    }
    public sealed class CampaignActivityStateDto
    {
        public List<ActivityProductionItemDto>? Production {get;set;}=new List<ActivityProductionItemDto>();
        public List<ActivityStructureReceiptDto>? Structures {get;set;}=new List<ActivityStructureReceiptDto>();
        public List<ActivitySiteStateDto>? Sites {get;set;}=new List<ActivitySiteStateDto>();
        public List<ActivityMinorStateDto>? Minors {get;set;}=new List<ActivityMinorStateDto>();
        public List<ActivityFindingDto>? Findings {get;set;}=new List<ActivityFindingDto>();
        public List<ActivityBodyDto>? Bodies {get;set;}=new List<ActivityBodyDto>();
        public List<ActivityMinorRelationDto>? Relations {get;set;}=new List<ActivityMinorRelationDto>();
        public List<ActivitySiteObservationDto>? Observations {get;set;}=new List<ActivitySiteObservationDto>();
    }
    public sealed class ActivitySiteObservationDto
    { public string ActorId {get;set;}="";public string SiteId {get;set;}="";public int FirstObservedTurn {get;set;} }
    // Actor-filtered presentation. It never carries the generated root, foreign orders,
    // undiscovered site counts, or any capability/eligibility permission token.
    public sealed class CampaignActivityObservationDto
    {
        public string ActorId {get;set;}="";public int Turn {get;set;}public long Revision {get;set;}
        public List<ActivityKnownSiteDto> Sites {get;set;}=new List<ActivityKnownSiteDto>();
        public List<ActivityProductionItemDto> Production {get;set;}=new List<ActivityProductionItemDto>();
        public List<ActivityStructureReceiptDto> Structures {get;set;}=new List<ActivityStructureReceiptDto>();
        public List<ActivityFindingDto> Findings {get;set;}=new List<ActivityFindingDto>();
        public List<ActivityOwnShipDto> Ships {get;set;}=new List<ActivityOwnShipDto>();
        public List<ActivityOwnColonyDto> Colonies {get;set;}=new List<ActivityOwnColonyDto>();
        public List<ActivityBuildOptionDto> BuildOptions {get;set;}=new List<ActivityBuildOptionDto>();
        public long Credits {get;set;}
    }
    public sealed class ActivityKnownSiteDto
    {
        public ActivitySiteStateDto Site {get;set;}=new ActivitySiteStateDto();
        public ActivitySiteDefinitionDto Definition {get;set;}=new ActivitySiteDefinitionDto();
        public ActivityBodyDto Body {get;set;}=new ActivityBodyDto();
        public ActivityMinorStateDto? Minor {get;set;}
        public ActivityMinorDefinitionDto? MinorDefinition {get;set;}
        public ActivityMinorRelation? Relation {get;set;}
        public List<ActivityKnownChoiceDto> Choices {get;set;}=new List<ActivityKnownChoiceDto>();
    }
    public sealed class ActivityKnownChoiceDto
    {
        public string ChoiceId {get;set;}="";public string Caption {get;set;}="";public string FindingId {get;set;}="";
        public int RequiredStudy {get;set;}public int RequiredContact {get;set;}public int Work {get;set;}public int Deadline {get;set;}
        public long Credits {get;set;}public ScienceDiscipline Discipline {get;set;}
        public long LearnableInsightInternalUnits {get;set;}public long UnlearnableInsightInternalUnits {get;set;}
        public int HullRiskBps {get;set;}public int HullDamage {get;set;}
    }
    public sealed class ActivityOwnShipDto
    {
        public string ShipId {get;set;}="";public string FleetId {get;set;}="";public string DisplayName {get;set;}="";public string HullVersionId {get;set;}="";
        public string NodeId {get;set;}="";public bool Stationary {get;set;}public int StudyWork {get;set;}public int ContactCapacity {get;set;}
    }
    public sealed class ActivityOwnColonyDto
    {
        public string ColonyId {get;set;}="";public string DisplayName {get;set;}="";public long Industry {get;set;}public long Research {get;set;}public long Food {get;set;}
    }
    public sealed class ActivityBuildOptionDto
    {
        public ActivityProductionKind Kind {get;set;}public string DefinitionId {get;set;}="";public int BlueprintRevision {get;set;}
        public string DisplayName {get;set;}="";public string HullVersionId {get;set;}="";public ShipDesignCostDto Cost {get;set;}=new ShipDesignCostDto();
        public bool Supported {get;set;}public string Reason {get;set;}="";
    }
    public sealed class CampaignActivityEligibilityDto
    {
        public string ActorId {get;set;}="";public int Turn {get;set;}public long Revision {get;set;}
        public bool Accepted {get;set;}public CampaignTacticalError Error {get;set;}public string Reason {get;set;}="";
        public long? IndustryAfterCompletion {get;set;}public long? ResearchAfterCompletion {get;set;}public long? FoodAfterCompletion {get;set;}
    }
    public enum CampaignActivityCommandKind { EnqueueShip=1, EnqueueStructure=2, CancelProduction=3, StartStudy=4, ClaimFinding=5, AbortStudy=6 }
    public sealed class CampaignActivityCommandDto
    {
        public string ActorId {get;set;}="";
        public string CommandId {get;set;}="";
        public int ExpectedTurn {get;set;}
        public long ExpectedRevision {get;set;}
        public CampaignActivityCommandKind Kind {get;set;}
        public string TargetId {get;set;}=""; // Colony for enqueue; site for study; queue identity for cancel.
        public string DefinitionId {get;set;}=""; // Exact blueprint or structure/choice identity.
        public int BlueprintRevision {get;set;}
        public string ShipId {get;set;}="";
    }
    public sealed class CampaignActivityOperationDto
    {
        public CampaignPlayableOperationDto? Common {get;set;}
        public CampaignActivityCommandDto? Activity {get;set;}
    }
    public sealed class CampaignActivitiesSaveDto
    {
        public int FormatVersion {get;set;}=CampaignActivitiesIdentity.Format;
        public string ProfileId {get;set;}=CampaignActivitiesIdentity.Profile;
        public string PolicyId {get;set;}=CampaignActivitiesIdentity.Policy;
        public string TemplatePackageHash {get;set;}="";
        public string PackageHash {get;set;}="";
        public string SupportHash {get;set;}="";
        public NaturalGalaxyDocumentDto? Map {get;set;}
        public CampaignPlayableFleetSaveDto? Campaign {get;set;}
        public List<CampaignFrontierReceiptDto>? Discoveries {get;set;}
        public List<CampaignPublishedBuiltShipDto>? PublishedBuiltShips {get;set;}
        public CampaignActivityStateDto? Activities {get;set;}
        public List<CampaignActivityOperationDto>? Journal {get;set;}
    }
}
