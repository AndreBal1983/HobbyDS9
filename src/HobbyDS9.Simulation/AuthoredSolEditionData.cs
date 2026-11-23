using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    internal static class AuthoredSolEditionData
    {
        internal static string Hash(CampaignAuthoredSolPackageDto p,bool refinementSource=false)
        {
            Require(p!=null&&p.FormatVersion==CampaignAuthoredSolIdentity.PackageFormat&&p.ProfileId==CampaignAuthoredSolIdentity.PackageProfile
                &&p.BaseGrowth!=null&&p.Generation!=null&&p.Generation.GenerationVersion==CampaignAuthoredSolIdentity.Generation
                &&p.Generation.MinimumPlanets==3&&p.Generation.MaximumPlanets==6&&p.Generation.BeltChanceBps==7500,"Different authored Sol belt package.");
            var old=new CampaignSystemRichnessPackageDto{BaseGrowth=p!.BaseGrowth,Policy=p.Policy};
            return CampaignActivitiesData.Identity(CampaignAuthoredSolIdentity.Profile,SystemRichnessData.Hash(old,refinementSource),CampaignAuthoredSolIdentity.Generation,
                AuthoredSolBeltData.Version,"sol-physical-association;inner2200;outer3200;no-mechanics;carrier16-v1");
        }
        internal static List<SystemAsteroidBeltDto> Belts(NaturalGalaxyDocumentDto map,FrozenColonisationPlanetsDto inventory)
        {
            var result=SystemRichnessData.Belts(map,new SystemRichnessPolicyDto());
            var authored=AuthoredSolBeltData.Create(inventory,map.Settings.Map.Seed);
            Require(authored!=null,"The authored Sol belt edition requires the actual mapped Earth/Mars/Jupiter inventory.");
            Require(!result.Any(b=>b.SystemId==authored!.SystemId),"Duplicate generated/authored Sol belt association.");
            result.Add(authored!);return result.OrderBy(b=>b.SystemId,System.StringComparer.Ordinal).ThenBy(b=>b.FeatureId,System.StringComparer.Ordinal).ToList();
        }
        internal static void ValidateBelts(CampaignAuthoredSolSaveDto root)
        {
            Require(root.GenerationVersion==CampaignAuthoredSolIdentity.Generation&&root.SystemBelts!=null&&root.SystemBelts.Count<=CampaignSystemRichnessIdentity.MaximumBelts
                &&root.SystemBelts.All(b=>b!=null)&&root.BaseCampaign?.FrozenPlanets!=null&&root.BaseCampaign.Map?.Settings?.Map!=null,"Bounded authored Sol astronomy required.");
            var physical=root.BaseCampaign!.FrozenPlanets!;
            Require(physical.InitialPlanets!=null&&physical.InitialPlanets.Count<=CampaignSystemRichnessIdentity.MaximumPlanets
                &&physical.Definitions!=null&&physical.Definitions.Count<=CampaignSystemRichnessIdentity.MaximumPlanets
                &&physical.Bodies!=null&&physical.Bodies.Count<=CampaignSystemRichnessIdentity.MaximumPlanets,"Bounded authored Sol association required before semantic checks.");
            var authored=root.SystemBelts!.Where(b=>b.FeatureId==AuthoredSolBeltData.FeatureId).ToArray();
            var expected=AuthoredSolBeltData.Create(root.BaseCampaign!.FrozenPlanets!,root.BaseCampaign.Map!.Settings.Map.Seed);
            Require(authored.Length==1&&expected!=null&&authored[0].SystemId==expected.SystemId
                &&authored[0].InnerRadiusMilliAu==expected.InnerRadiusMilliAu&&authored[0].OuterRadiusMilliAu==expected.OuterRadiusMilliAu
                &&authored[0].VisualSeed==expected.VisualSeed,"Different authored Sol belt identity or radial band.");
            var procedural=root.SystemBelts.Where(b=>b.FeatureId!=AuthoredSolBeltData.FeatureId).ToList();
            Require(procedural.All(b=>b.SystemId!=expected!.SystemId),"Duplicate authored Sol system belt.");
            SystemRichnessData.ValidateBelts(new CampaignSystemRichnessSaveDto{SystemBelts=procedural});
        }
    }
}
