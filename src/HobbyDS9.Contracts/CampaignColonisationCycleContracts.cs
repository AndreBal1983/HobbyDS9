using System.Collections.Generic;
namespace HobbyDS9.Contracts
{
    public static class CampaignColonisationCycleIdentity
    {
        public const int Format=14;
        public const string Profile="generated-colonisation-cycle-v1",PackageProfile="generated-colonisation-cycle-package-v1";
        public const string Planner="colonisation-ai-sustainable-cycle-v1",Budget="colonisation-cycle-root-preflight-v1",Goal="establish-frontier-cycle-v1";
    }
    public sealed class ColonisationCyclePolicyDto
    { public string Algorithm {get;set;}=CampaignColonisationCycleIdentity.Planner;public int ActiveMissions {get;set;}=1; }
    public sealed class CampaignColonisationCyclePackageDto
    {
        public int FormatVersion {get;set;}=1;public string ProfileId {get;set;}=CampaignColonisationCycleIdentity.PackageProfile;
        public string PackageHash {get;set;}="";public CampaignColonisationExpansionPackageDto? BaseGrowth {get;set;}
        public ColonisationCyclePolicyDto? Policy {get;set;}=new ColonisationCyclePolicyDto();
    }
    public enum ColonisationCycleOutcome { Established=1,Returned=2,Lost=3 }
    public sealed class ColonisationCycleReceiptDto
    {
        public int Ordinal {get;set;}public int StartedTurn {get;set;}public int TerminalTurn {get;set;}public int ClosedTurn {get;set;}
        public ColonisationCycleOutcome Outcome {get;set;}
        public string SettlementManifestId {get;set;}="";public string TargetPlanetId {get;set;}="";public string? CampId {get;set;}
    }
    public sealed class ColonisationCycleMissionDto
    {
        public int Ordinal {get;set;}public int StartedTurn {get;set;}
        public string? SettlementManifestId {get;set;}public string? CommittedTargetPlanetId {get;set;}
        public ColonisationAiPlanDto? Assignment {get;set;}
    }
    public sealed class ColonisationCycleActorDto
    {
        public string ActorId {get;set;}="";public int NextOrdinal {get;set;}=1;public string? SupplyCampId {get;set;}
        public ColonisationCycleMissionDto? Active {get;set;}
        public List<ColonisationCycleReceiptDto>? Receipts {get;set;}=new List<ColonisationCycleReceiptDto>();
    }
    public sealed class CampaignColonisationCycleSaveDto
    {
        public int FormatVersion {get;set;}=CampaignColonisationCycleIdentity.Format;public string ProfileId {get;set;}=CampaignColonisationCycleIdentity.Profile;
        public string PackageHash {get;set;}="";public CampaignColonisationSaveDto? BaseCampaign {get;set;}
        public List<ColonisationAiPlanDto>? AiPlans {get;set;}public List<ColonisationCycleActorDto>? CycleActors {get;set;}
    }
}
