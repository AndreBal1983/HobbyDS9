namespace HobbyDS9.Contracts
{
    // Same complete Format12 wire shape; explicit opt-in source/profile, never conversion.
    public static class CampaignColonisationGrowthIdentity
    {
        public const int Format=12;
        public const string Profile="generated-colonisation-expansion-v2";
        public const string PackageProfile="generated-colonisation-expansion-package-v2";
        public const string Protocol="passenger-transport-established-v1";
        public const string Planner="colonisation-ai-first-colony-growth-v1";
        public const string Budget="colonisation-growth-root-preflight-v1";
    }
}
