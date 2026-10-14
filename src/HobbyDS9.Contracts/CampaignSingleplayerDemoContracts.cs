namespace HobbyDS9.Contracts
{
    public static class CampaignSingleplayerDemoIdentity
    {
        public const int Format = 4;
        public const string Profile = "campaign-tactical-v2-singleplayer-demo-1";
        public const string AiPolicy = "lawful-bajor-defence-demo-v1";
    }
    public enum CampaignDemoOutcome { Active, Victory, Defeat, Stalemate }
    public sealed class CampaignSingleplayerDemoPackageDto
    {
        public int? FormatVersion { get; set; }
        public string? ProfileId { get; set; }
        public string? PackageHash { get; set; }
        public string? PolicyStatus { get; set; }
        public bool? DefaultActivationAllowed { get; set; }
        public string? HumanActorId { get; set; }
        public string? OpponentActorId { get; set; }
        public string? ObjectiveNodeId { get; set; }
        public int? LastTurn { get; set; }
        public string? AiPolicyId { get; set; }
        public CampaignIntegratedPlaytestPackageDto? Integrated { get; set; }
    }
    public sealed class CampaignDemoPlanDto
    {
        public string PolicyId { get; set; } = "";
        public string ActorId { get; set; } = "";
        public string GoalNodeId { get; set; } = "";
        public int LastPlannedTurn { get; set; }
    }
    public sealed class CampaignSingleplayerDemoSaveDto
    {
        public int FormatVersion { get; set; }
        public string ProfileId { get; set; } = "";
        public string PackageHash { get; set; } = "";
        public CampaignIntegratedPlaytestSaveDto? Integrated { get; set; }
        public CampaignDemoPlanDto? OpponentPlan { get; set; }
    }
}
