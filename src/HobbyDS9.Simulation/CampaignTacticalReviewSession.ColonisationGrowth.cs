using System;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        public static CampaignColonisationExpansionPackageDto CreateGrowthPackage(CampaignColonisationExpansionPackageDto original)
        {
            Require(original!=null&&original.PackageHash==ColonisationData.ExpansionHash(original),"Approved original expansion source required.");
            var p=new CampaignColonisationExpansionPackageDto{ProfileId=CampaignColonisationGrowthIdentity.PackageProfile,BaseRosterV2=original!.BaseRosterV2,PlannerPolicy=ColonisationData.Copy(original.PlannerPolicy!)};
            p.PlannerPolicy.Algorithm=CampaignColonisationGrowthIdentity.Planner;p.PackageHash=ColonisationData.GrowthHash(p);return p;
        }
        public static CampaignTacticalReviewSession StartGrowthGame(CampaignColonisationExpansionPackageDto package,NaturalGalaxySettingsDto settings)
        {
            var source=new ExpansionSource(package,ColonisationSourceEdition.Growth);var roster=package.BaseRosterV2!;var p=roster.BaseV1!;
            var civs=p.Activities!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
            var map=GeneratedCampaignFactory.Frozen(NaturalGalaxyGenerator.Generate(settings,civs),civs);
            return StartActivitiesFrozen(p.Activities,map,null,p,null,new ColonisationRosterSource(roster),source);
        }
        public static CampaignTacticalReviewSession LoadGrowthSave(CampaignColonisationExpansionPackageDto package,CampaignColonisationExpansionSaveDto root)
        {
            var source=new ExpansionSource(package,ColonisationSourceEdition.Growth);var roster=package.BaseRosterV2!;
            ColonisationRootBudget.ValidateGrowth(root,roster.BaseV1!.Settlement!);Require(root.PackageHash==source.Hash,"Different growth source.");
            var replay=LoadColonisationCore(roster.BaseV1,root.BaseCampaign!,new ColonisationRosterSource(roster),source);
            Require(FrontierSame(root,replay.ExportGrowthSave()),"Growth differs from complete ordered replay.");return replay;
        }
        public CampaignColonisationExpansionSaveDto ExportGrowthSave()
        {lock(gate){Require(GrowthEnabled&&!RepeatEnabled&&!CycleEnabled,"Explicit growth root required.");return ExportExpansionCore();}}
        private CampaignColonisationExpansionSaveDto ExportExpansionCore()=>new CampaignColonisationExpansionSaveDto{
            ProfileId=expansion!.Source.Profile,PackageHash=expansion.Source.Hash,BaseCampaign=ExportColonisationCore(),AiPlans=expansion.Plans.Select(ColonisationData.Copy).ToList()};
    }
}
