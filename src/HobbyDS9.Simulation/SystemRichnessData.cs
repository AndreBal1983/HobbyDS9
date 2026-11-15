using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    internal static class SystemRichnessData
    {
        internal static SystemRichnessPolicyDto Copy(SystemRichnessPolicyDto p)=>new SystemRichnessPolicyDto{GenerationVersion=p.GenerationVersion,MinimumPlanets=p.MinimumPlanets,MaximumPlanets=p.MaximumPlanets,BeltChanceBps=p.BeltChanceBps};
        internal static void Validate(SystemRichnessPolicyDto? p)
            =>Require(p!=null&&p.GenerationVersion==CampaignSystemRichnessIdentity.Generation&&p.MinimumPlanets==3&&p.MaximumPlanets==6&&p.BeltChanceBps==7500,"Unsupported provisional system richness recipe.");
        internal static string Hash(CampaignSystemRichnessPackageDto p)
        {
            Require(p.FormatVersion==2&&p.ProfileId==CampaignSystemRichnessIdentity.PackageProfile&&p.BaseGrowth!=null,"Different richness package.");Validate(p.Generation);
            var old=new CampaignColonisationCyclePackageDto{BaseGrowth=p.BaseGrowth,Policy=p.Policy};
            return CampaignActivitiesData.Identity(CampaignSystemRichnessIdentity.Profile,ColonisationData.CycleHash(old),CampaignSystemRichnessIdentity.Generation,
                "uniform-count=3,4,5,6;preserve-starters;existing-environments;belts=7500bps;inner=2200..4199;thickness=300..999;max=6040,1000;carrier15-v1");
        }
        internal static string Rank(string seed,string system,string domain)
            =>CampaignActivitiesData.Identity(CampaignSystemRichnessIdentity.Generation,seed,system,domain);
        internal static int Draw(string seed,string system,string domain,int modulus)
            =>(int)(uint.Parse(Rank(seed,system,domain).Substring(0,8),NumberStyles.HexNumber,CultureInfo.InvariantCulture)%(uint)modulus);
        internal static int Target(string seed,string system,SystemRichnessPolicyDto p)
            =>p.MinimumPlanets+Draw(seed,system,"planet-count",p.MaximumPlanets-p.MinimumPlanets+1);
        internal static List<SystemAsteroidBeltDto> Belts(NaturalGalaxyDocumentDto map,SystemRichnessPolicyDto p)
        {
            Validate(p);var homes=map.Layout.HomeCandidates.Select(h=>h.SystemId).ToHashSet(StringComparer.Ordinal);
            return map.Layout.Systems.Where(s=>!homes.Contains(s.SystemId)&&Draw(map.Settings.Map.Seed,s.SystemId,"belt-exists",10000)<p.BeltChanceBps)
                .OrderBy(s=>s.SystemId,StringComparer.Ordinal).Select(s=>{
                    var inner=2200+Draw(map.Settings.Map.Seed,s.SystemId,"belt-inner",2000);
                    return new SystemAsteroidBeltDto{FeatureId="belt.richness.v1."+s.SystemId,SystemId=s.SystemId,InnerRadiusMilliAu=inner,
                        OuterRadiusMilliAu=inner+300+Draw(map.Settings.Map.Seed,s.SystemId,"belt-thickness",700),VisualSeed=Rank(map.Settings.Map.Seed,s.SystemId,"belt-visual")};
                }).ToList();
        }
        internal static SystemAsteroidBeltDto Copy(SystemAsteroidBeltDto b)=>new SystemAsteroidBeltDto{FeatureId=b.FeatureId,SystemId=b.SystemId,InnerRadiusMilliAu=b.InnerRadiusMilliAu,OuterRadiusMilliAu=b.OuterRadiusMilliAu,VisualSeed=b.VisualSeed};
        internal static ColonisationPolicyDto Effective(ColonisationPolicyDto p,bool richness)
        {if(!richness)return p;var copy=ColonisationData.Copy(p);copy.MaximumPlanets=CampaignSystemRichnessIdentity.MaximumPlanets;return copy;}
        internal static void ValidateBelts(CampaignSystemRichnessSaveDto root)
        {
            Require(root.GenerationVersion==CampaignSystemRichnessIdentity.Generation&&root.SystemBelts!=null&&root.SystemBelts.Count<=CampaignSystemRichnessIdentity.MaximumBelts,"Bounded richness astronomy required.");
            var ids=new HashSet<string>(StringComparer.Ordinal);var systems=new HashSet<string>(StringComparer.Ordinal);
            foreach(var b in root.SystemBelts!)Require(b!=null&&EntityId.TryParse(b.FeatureId,out _)&&EntityId.TryParse(b.SystemId,out _)
                &&ids.Add(b.FeatureId)&&systems.Add(b.SystemId)&&b.InnerRadiusMilliAu>=2200&&b.InnerRadiusMilliAu<=4199
                &&b.OuterRadiusMilliAu>=b.InnerRadiusMilliAu+300&&b.OuterRadiusMilliAu<=b.InnerRadiusMilliAu+999
                &&b.VisualSeed!=null&&b.VisualSeed.Length==64&&b.VisualSeed.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f'),"Invalid or duplicate system belt.");
        }
    }
}
