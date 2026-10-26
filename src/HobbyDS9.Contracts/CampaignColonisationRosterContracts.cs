namespace HobbyDS9.Contracts
{
    // Content-only sibling of V1; the Format11 save graph and settlement rules are unchanged.
    public static class CampaignColonisationRosterIdentity
    {
        public const string PackageProfile="generated-colonisation-roster-package-v2";
        public const string Policy="colonisation-starting-roster-ready-explorers-v2";
    }
    public sealed class ColonisationStartingRosterDto
    {
        public string Identity {get;set;}=CampaignColonisationRosterIdentity.Policy;
        public string ClassId {get;set;}="class.constitution-explorer.v1";
        public string HullVersionId {get;set;}="hull.constitution-explorer.v1";
        public int Revision {get;set;}=1;
    }
    public sealed class CampaignColonisationRosterPackageDto
    {
        public int FormatVersion {get;set;}=1;
        public string ProfileId {get;set;}=CampaignColonisationRosterIdentity.PackageProfile;
        public string PackageHash {get;set;}="";
        public CampaignColonisationPackageDto? BaseV1 {get;set;}
        public ColonisationStartingRosterDto? StartingRoster {get;set;}
    }
}
