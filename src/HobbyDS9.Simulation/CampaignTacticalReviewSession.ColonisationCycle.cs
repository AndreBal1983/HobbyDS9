using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        public static CampaignColonisationCyclePackageDto CreateCyclePackage(CampaignColonisationExpansionPackageDto growth)
        {var p=new CampaignColonisationCyclePackageDto{BaseGrowth=growth};p.PackageHash=ColonisationData.CycleHash(p);return p;}
        public static CampaignTacticalReviewSession StartCycleGame(CampaignColonisationCyclePackageDto p,NaturalGalaxySettingsDto settings)
        {
            var source=new ExpansionSource(p);var roster=p.BaseGrowth!.BaseRosterV2!;var b=roster.BaseV1!;
            var civs=b.Activities!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
            var map=GeneratedCampaignFactory.Frozen(NaturalGalaxyGenerator.Generate(settings,civs),civs);
            return StartActivitiesFrozen(b.Activities,map,null,b,null,new ColonisationRosterSource(roster),source);
        }
        public static CampaignTacticalReviewSession LoadCycleSave(CampaignColonisationCyclePackageDto p,CampaignColonisationCycleSaveDto root)
        {
            var source=new ExpansionSource(p);var roster=p.BaseGrowth!.BaseRosterV2!;
            ColonisationRootBudget.ValidateCycle(root,roster.BaseV1!.Settlement!);Require(root.PackageHash==source.Hash,"Different cycle source.");
            var replay=LoadColonisationCore(roster.BaseV1,root.BaseCampaign!,new ColonisationRosterSource(roster),source);
            Require(FrontierSame(root,replay.ExportCycleSave()),"Cycle differs from complete ordered replay.");return replay;
        }
        public CampaignColonisationCycleSaveDto ExportCycleSave()
        {lock(gate){Require(CycleEnabled,"Explicit cycle root required.");return new CampaignColonisationCycleSaveDto{PackageHash=expansion!.Source.Hash,
            BaseCampaign=ExportColonisationCore(),AiPlans=expansion.Plans.Select(ColonisationData.Copy).ToList(),CycleActors=expansion.CycleActors.Select(ColonisationData.Copy).ToList()};}}
        private ColonisationCycleActorDto CycleActor(string actor)=>expansion!.CycleActors.Single(a=>a.ActorId==actor);
        private bool CycleEnabled=>expansion?.Source.Edition==ColonisationSourceEdition.Cycle;
        private bool BoundAssignments=>RepeatEnabled||CycleEnabled;
        private string? SupplyCommitment(string actor)=>CycleEnabled?CycleActor(actor).SupplyCampId:RepeatGoal(actor).SupplyCampId;
        private void SetSupplyCommitment(string actor,string? id){if(CycleEnabled)CycleActor(actor).SupplyCampId=id;else RepeatGoal(actor).SupplyCampId=id;}
    }
}
