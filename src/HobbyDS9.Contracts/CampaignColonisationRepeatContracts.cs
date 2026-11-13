using System.Collections.Generic;
namespace HobbyDS9.Contracts
{
    public static class CampaignColonisationRepeatIdentity
    {
        public const int Format=13;
        public const string Profile="generated-colonisation-repeat-v1";
        public const string PackageProfile="generated-colonisation-repeat-package-v1";
        public const string Planner="colonisation-ai-second-frontier-v1";
        public const string Budget="colonisation-repeat-root-preflight-v1";
        public const string Goal="establish-second-frontier-colony-v1";
    }
    public sealed class ColonisationRepeatPolicyDto
    {
        public string Algorithm {get;set;}=CampaignColonisationRepeatIdentity.Planner;
        public int AdditionalColonies {get;set;}=1;
        public int SettlementAttempts {get;set;}=1;
    }
    public sealed class CampaignColonisationRepeatPackageDto
    {
        public int FormatVersion {get;set;}=1;
        public string ProfileId {get;set;}=CampaignColonisationRepeatIdentity.PackageProfile;
        public string PackageHash {get;set;}="";
        public CampaignColonisationExpansionPackageDto? BaseGrowth {get;set;}
        public ColonisationRepeatPolicyDto? Policy {get;set;}=new ColonisationRepeatPolicyDto();
    }
    public enum ColonisationRepeatState { Waiting=1,Active=2,Established=3,Aborted=4,Lost=5 }
    public sealed class ColonisationRepeatGoalDto
    {
        public string ActorId {get;set;}="";
        public ColonisationRepeatState State {get;set;}=ColonisationRepeatState.Waiting;
        public int? StartedTurn {get;set;}
        public int? ClosedTurn {get;set;}
        public string? SettlementManifestId {get;set;}
        public string? SupplyCampId {get;set;}
        public ColonisationAiPlanDto? Assignment {get;set;}
    }
    public sealed class CampaignColonisationRepeatSaveDto
    {
        public int FormatVersion {get;set;}=CampaignColonisationRepeatIdentity.Format;
        public string ProfileId {get;set;}=CampaignColonisationRepeatIdentity.Profile;
        public string PackageHash {get;set;}="";
        public CampaignColonisationSaveDto? BaseCampaign {get;set;}
        public List<ColonisationAiPlanDto>? AiPlans {get;set;}
        public List<ColonisationRepeatGoalDto>? RepeatGoals {get;set;}
    }
}
