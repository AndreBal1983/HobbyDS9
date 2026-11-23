using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class CampaignAuthoredSolIdentity
    {
        public const int Format=16,PackageFormat=3;
        public const string Profile="generated-colonisation-authored-sol-belt-v1";
        public const string PackageProfile="generated-colonisation-authored-sol-belt-package-v1";
        public const string Generation="system-richness-sol-belt-v2";
        public const string SaveDirectorySuffix="-engineering20-sol08-authoredbelt1";
        public const string FeatureId="belt.authored.sol.asteroid.v1";
        public const string RadiusUnit="milli-au-authored-reference-v1";
    }
    // Explicit peer of the closed richness-v1 carriers; no extra fields on older roots.
    public sealed class CampaignAuthoredSolPackageDto:CampaignColonisationCyclePackageDto
    {
        public SystemRichnessPolicyDto? Generation {get;set;}=new SystemRichnessPolicyDto{GenerationVersion=CampaignAuthoredSolIdentity.Generation};
        public CampaignAuthoredSolPackageDto(){FormatVersion=CampaignAuthoredSolIdentity.PackageFormat;ProfileId=CampaignAuthoredSolIdentity.PackageProfile;}
    }
    public sealed class CampaignAuthoredSolSaveDto:CampaignColonisationCycleSaveDto
    {
        public string GenerationVersion {get;set;}=CampaignAuthoredSolIdentity.Generation;
        public List<SystemAsteroidBeltDto>? SystemBelts {get;set;}=new List<SystemAsteroidBeltDto>();
        public CampaignAuthoredSolSaveDto(){FormatVersion=CampaignAuthoredSolIdentity.Format;ProfileId=CampaignAuthoredSolIdentity.Profile;}
    }
}
