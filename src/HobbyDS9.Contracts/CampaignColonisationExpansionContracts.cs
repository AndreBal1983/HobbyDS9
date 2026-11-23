using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class CampaignColonisationExpansionIdentity
    {
        public const int Format=12;
        public const string Profile="generated-colonisation-expansion-v1";
        public const string PackageProfile="generated-colonisation-expansion-package-v1";
        public const string Planner="colonisation-ai-first-expedition-v1";
        public const string InventoryPolicy="planetary-inventory-expansion-units-v1";
        public const string InventoryAuthoring="expansion-environment-units100-v1";
        public const string Budget="colonisation-expansion-root-preflight-v1";
    }
    // Named provisional planner behaviour, not alternative physical gameplay rules.
    public sealed class ColonisationPlannerPolicyDto
    {
        public string Algorithm {get;set;}=CampaignColonisationExpansionIdentity.Planner;
        public int ActiveGoals {get;set;}=1;
        public int MaximumAttempts {get;set;}=8;
        public int MaximumAccepted {get;set;}=4;
        public int BodyCandidates {get;set;}=16;
        public int RouteCandidates {get;set;}=8;
        public int FailedTargets {get;set;}=8;
        public int BfsTraversals {get;set;}=2;
        public int KnownNodes {get;set;}=2048;
        public int KnownEdges {get;set;}=8192;
        public int HomeDeficitWeeks {get;set;}=2;
        public int ContingencyWeeks {get;set;}=2;
        public int DisputedWeeks {get;set;}=2;
    }
    public sealed class CampaignColonisationExpansionPackageDto
    {
        public int FormatVersion {get;set;}=1;
        public string ProfileId {get;set;}=CampaignColonisationExpansionIdentity.PackageProfile;
        public string PackageHash {get;set;}="";
        public CampaignColonisationRosterPackageDto? BaseRosterV2 {get;set;}
        public ColonisationPlannerPolicyDto? PlannerPolicy {get;set;}=new ColonisationPlannerPolicyDto();
    }
    public enum ColonisationAiStage { Preparing=1,Scouting=2,Assembling=3,Loaded=4,Deploying=5,Supporting=6,Recovering=7,Suspended=8,Immigrating=9,GrowthClosed=10 }
    public enum ColonisationAiAction { Kit=1,Ship=2,Infrastructure=3,Survey=4,Embark=5,Deploy=6,Withdraw=7,Unload=8,SupplyLoad=9,Deliver=10,Evacuate=11,Travel=12,Organise=13,CancelSurvey=14,PassengerLoad=15 }
    public sealed class ColonisationAiActionReceiptDto
    {
        public string CommandId {get;set;}="";
        public ColonisationAiAction Action {get;set;}
        public long BeforeRevision {get;set;}
        public long AfterRevision {get;set;}
        public bool Accepted {get;set;}
        public CampaignTacticalError Error {get;set;}
    }
    public sealed class ColonisationAiPlanDto
    {
        public string ActorId {get;set;}="";
        public string GoalId {get;set;}="establish-first-frontier-colony-v1";
        public ColonisationAiStage Stage {get;set;}=ColonisationAiStage.Preparing;
        public string? HomeColonyId {get;set;}public string? YardColonyId {get;set;}public string? SourceCohortId {get;set;}
        public string? ScoutShipId {get;set;}public string? TransportShipId {get;set;}
        public string? BlueprintId {get;set;}public int BlueprintRevision {get;set;}
        public string? TargetPlanetId {get;set;}public string? TargetNodeId {get;set;}
        public string? KitQueueId {get;set;}public string? TransportQueueId {get;set;}
        public string? ManifestId {get;set;}public string? CampId {get;set;}public string? EstablishedColonyId {get;set;}
        public int LastPlannedTurn {get;set;}public int DisputedBoundaries {get;set;}
        public string HoldReason {get;set;}="";
        public List<string>? OwnAttemptedTargets {get;set;}=new List<string>();
        public List<ColonisationAiActionReceiptDto>? LatestActions {get;set;}=new List<ColonisationAiActionReceiptDto>();
    }
    public sealed class CampaignColonisationExpansionSaveDto
    {
        public int FormatVersion {get;set;}=CampaignColonisationExpansionIdentity.Format;
        public string ProfileId {get;set;}=CampaignColonisationExpansionIdentity.Profile;
        public string PackageHash {get;set;}="";
        public CampaignColonisationSaveDto? BaseCampaign {get;set;}
        public List<ColonisationAiPlanDto>? AiPlans {get;set;}
    }
}
