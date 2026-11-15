using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        public static CampaignColonisationCyclePackageDto CreateCyclePackage(CampaignColonisationExpansionPackageDto growth)
        {var p=new CampaignColonisationCyclePackageDto{BaseGrowth=growth};p.PackageHash=ColonisationData.CycleHash(p);return p;}
        public static CampaignSystemRichnessPackageDto CreateSystemRichnessPackage(CampaignColonisationExpansionPackageDto growth,SystemRichnessPolicyDto policy)
        {SystemRichnessData.Validate(policy);var p=new CampaignSystemRichnessPackageDto{BaseGrowth=growth,Generation=SystemRichnessData.Copy(policy)};p.PackageHash=ColonisationData.CycleHash(p);return p;}
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
            Require((source.Richness!=null)==(root is CampaignSystemRichnessSaveDto),"Different system-generation save edition; new campaign required.");
            ColonisationRootBudget.ValidateCycle(root,roster.BaseV1!.Settlement!);Require(root.PackageHash==source.Hash,"Different cycle source.");
            var replay=LoadColonisationCoreWithVerifiedExport(roster.BaseV1,root.BaseCampaign!,new ColonisationRosterSource(roster),out var verifiedBase,source);
            CampaignColonisationCycleSaveDto canonical;
            lock(replay.gate)
            {
                Require(replay.CycleEnabled,"Explicit cycle root required.");
                canonical=replay.CycleRoot(verifiedBase);
            }
            Require(FrontierSame(root,canonical),"Cycle differs from complete ordered replay.");return replay;
        }
        public CampaignColonisationCycleSaveDto ExportCycleSave()
        {lock(gate){Require(CycleEnabled,"Explicit cycle root required.");return CycleRoot(ExportColonisationCore());}}
        private CampaignColonisationCycleSaveDto CycleRoot(CampaignColonisationSaveDto core)
        {
            CampaignColonisationCycleSaveDto root=expansion!.Source.Richness==null?new CampaignColonisationCycleSaveDto():new CampaignSystemRichnessSaveDto{SystemBelts=expansion.Belts.Select(SystemRichnessData.Copy).ToList()};
            root.PackageHash=expansion.Source.Hash;root.BaseCampaign=core;root.AiPlans=expansion.Plans.Select(ColonisationData.Copy).ToList();root.CycleActors=expansion.CycleActors.Select(ColonisationData.Copy).ToList();return root;
        }
        private ColonisationCycleActorDto CycleActor(string actor)=>expansion!.CycleActors.Single(a=>a.ActorId==actor);
        private bool CycleEnabled=>expansion?.Source.Edition==ColonisationSourceEdition.Cycle;
        private bool BoundAssignments=>RepeatEnabled||CycleEnabled;
        private string? SupplyCommitment(string actor)=>CycleEnabled?CycleActor(actor).SupplyCampId:RepeatGoal(actor).SupplyCampId;
        private void SetSupplyCommitment(string actor,string? id){if(CycleEnabled)CycleActor(actor).SupplyCampId=id;else RepeatGoal(actor).SupplyCampId=id;}
    }
}
