namespace HobbyDS9.Contracts
{
    public static class CampaignSingleplayerDemoIdentity
    {
        public const int Format = 4;
        public const string Profile = "campaign-tactical-v2-singleplayer-demo-1";
        public const string AiPolicy = "lawful-bajor-defence-demo-v1";
        public const string ObjectivePolicy = "bajor-surviving-hull-presence-demo-v1";
    }
    public enum CampaignDemoOutcome { Active, Victory, Defeat, Stalemate }
    public sealed class CampaignDemoSnapshot
    {
        public string HumanActorId { get; }
        public string ObjectiveNodeId { get; }
        public int LastTurn { get; }
        public int Turn { get; }
        public SessionPhase Phase { get; }
        public CampaignDemoOutcome Outcome { get; }
        public string ObjectiveText { get; }
        public CampaignDemoSnapshot(string human,string node,int lastTurn,int turn,SessionPhase phase,CampaignDemoOutcome outcome)
        {HumanActorId=human;ObjectiveNodeId=node;LastTurn=lastTurn;Turn=turn;Phase=phase;Outcome=outcome;
            ObjectiveText="Defend Bajor through turn "+lastTurn+" with greater surviving hull strength there than the opponent, or defeat the opposing military. Losing all ships or the deadline contest loses; an equal contested strength is a stalemate.";}
    }
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
        public string? ObjectivePolicyId { get; set; }
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
