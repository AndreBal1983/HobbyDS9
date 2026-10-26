using System;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        public static CampaignColonisationRepeatPackageDto CreateRepeatPackage(CampaignColonisationExpansionPackageDto growth)
        {
            var p=new CampaignColonisationRepeatPackageDto{BaseGrowth=growth};p.PackageHash=ColonisationData.RepeatHash(p);return p;
        }
        public static CampaignTacticalReviewSession StartRepeatGame(CampaignColonisationRepeatPackageDto p,NaturalGalaxySettingsDto settings)
        {
            var source=new ExpansionSource(p);var roster=p.BaseGrowth!.BaseRosterV2!;var b=roster.BaseV1!;
            var civs=b.Activities!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
            var map=GeneratedCampaignFactory.Frozen(NaturalGalaxyGenerator.Generate(settings,civs),civs);
            return StartActivitiesFrozen(b.Activities,map,null,b,null,new ColonisationRosterSource(roster),source);
        }
        public static CampaignTacticalReviewSession LoadRepeatSave(CampaignColonisationRepeatPackageDto p,CampaignColonisationRepeatSaveDto root)
        {
            var source=new ExpansionSource(p);var roster=p.BaseGrowth!.BaseRosterV2!;
            ColonisationRootBudget.ValidateRepeat(root,roster.BaseV1!.Settlement!);Require(root.PackageHash==source.Hash,"Different repeat package.");
            var replay=LoadColonisationCore(roster.BaseV1,root.BaseCampaign!,new ColonisationRosterSource(roster),source);
            Require(FrontierSame(root,replay.ExportRepeatSave()),"Repeat differs from complete ordered replay.");return replay;
        }
        public CampaignColonisationRepeatSaveDto ExportRepeatSave()
        {lock(gate){Require(RepeatEnabled,"Explicit repeat root required.");return new CampaignColonisationRepeatSaveDto{
            PackageHash=expansion!.Source.Hash,BaseCampaign=ExportColonisationCore(),AiPlans=expansion.Plans.Select(ColonisationData.Copy).ToList(),RepeatGoals=expansion.RepeatGoals.Select(ColonisationData.Copy).ToList()};}}
        private ColonisationRepeatGoalDto RepeatGoal(string actor)=>expansion!.RepeatGoals.Single(p=>p.ActorId==actor);
    }
}
