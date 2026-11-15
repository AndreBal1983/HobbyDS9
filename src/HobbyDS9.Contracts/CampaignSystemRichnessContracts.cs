using System.Collections.Generic;
namespace HobbyDS9.Contracts
{
    public static class CampaignSystemRichnessIdentity
    {
        public const int Format=15;
        public const string Profile="generated-colonisation-system-richness-v1",PackageProfile="generated-colonisation-system-richness-package-v1";
        public const string Generation="system-richness-v1",Inventory="planetary-inventory-richness-v1";
        public const string Disclosure="system-presence-retained-arrival-v1",RadiusUnit="milli-au-procedural-reference-v1";
        public const int MaximumPlanets=6040,MaximumBelts=1000;
    }
    // Closed provisional content recipe. No AI, mining or navigation rule overrides.
    public sealed class SystemRichnessPolicyDto
    {
        public string GenerationVersion {get;set;}=CampaignSystemRichnessIdentity.Generation;
        public int MinimumPlanets {get;set;}=3;
        public int MaximumPlanets {get;set;}=6;
        public int BeltChanceBps {get;set;}=7500;
    }
    public sealed class CampaignSystemRichnessPackageDto:CampaignColonisationCyclePackageDto
    {
        public SystemRichnessPolicyDto? Generation {get;set;}=new SystemRichnessPolicyDto();
        public CampaignSystemRichnessPackageDto(){FormatVersion=2;ProfileId=CampaignSystemRichnessIdentity.PackageProfile;}
    }
    public sealed class SystemAsteroidBeltDto
    {
        public string FeatureId {get;set;}="";
        public string SystemId {get;set;}="";
        public int InnerRadiusMilliAu {get;set;}
        public int OuterRadiusMilliAu {get;set;}
        public string VisualSeed {get;set;}="";
    }
    // A distinct runtime type keeps the complete old Format14 carrier byte/shape unchanged.
    public sealed class CampaignSystemRichnessSaveDto:CampaignColonisationCycleSaveDto
    {
        public string GenerationVersion {get;set;}=CampaignSystemRichnessIdentity.Generation;
        public List<SystemAsteroidBeltDto>? SystemBelts {get;set;}=new List<SystemAsteroidBeltDto>();
        public CampaignSystemRichnessSaveDto(){FormatVersion=CampaignSystemRichnessIdentity.Format;ProfileId=CampaignSystemRichnessIdentity.Profile;}
    }
    public sealed class SystemAsteroidBeltSnapshot
    {
        public string FeatureId {get;}public string SystemId {get;}public int InnerRadiusMilliAu {get;}public int OuterRadiusMilliAu {get;}public string VisualSeed {get;}
        public string RadiusUnit=>CampaignSystemRichnessIdentity.RadiusUnit;
        public string DisclosurePolicy=>CampaignSystemRichnessIdentity.Disclosure;
        public SystemAsteroidBeltSnapshot(SystemAsteroidBeltDto b)
        {FeatureId=b.FeatureId;SystemId=b.SystemId;InnerRadiusMilliAu=b.InnerRadiusMilliAu;OuterRadiusMilliAu=b.OuterRadiusMilliAu;VisualSeed=b.VisualSeed;}
    }
}
