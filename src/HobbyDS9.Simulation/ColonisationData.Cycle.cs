using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    internal static partial class ColonisationData
    {
        internal static string CycleHash(CampaignColonisationCyclePackageDto p)
        {
            if(p is CampaignEngineeringRefinementPackageDto refinement)return EngineeringRefinementEditionData.Hash(refinement);
            Require(p?.BaseGrowth?.BaseRosterV2?.BaseV1?.Support?.FormatVersion==4,"Older cycle sources require their original support v4.");
            if(p is CampaignAuthoredSolPackageDto authored)return AuthoredSolEditionData.Hash(authored);
            if(p is CampaignSystemRichnessPackageDto rich)return SystemRichnessData.Hash(rich);
            return CycleBaseHash(p!);
        }
        internal static string RefinementCycleBaseHash(CampaignColonisationCyclePackageDto p)
        {
            Require(p?.BaseGrowth?.BaseRosterV2?.BaseV1?.Support?.FormatVersion==5,"Explicit refinement support source required.");
            return CycleBaseHash(p!);
        }
        private static string CycleBaseHash(CampaignColonisationCyclePackageDto p)
        {
            Require(p!=null&&p.GetType()==typeof(CampaignColonisationCyclePackageDto),"Unknown cycle package carrier type.");
            Require(p!=null&&p.FormatVersion==1&&p.ProfileId==CampaignColonisationCycleIdentity.PackageProfile&&p.BaseGrowth!=null
                &&p.BaseGrowth.PackageHash==GrowthHash(p.BaseGrowth)&&p.Policy!=null&&p.Policy.Algorithm==CampaignColonisationCycleIdentity.Planner&&p.Policy.ActiveMissions==1,"Different closed cycle source.");
            return CampaignActivitiesData.Identity(p!.ProfileId,p.BaseGrowth!.PackageHash,CampaignColonisationCycleIdentity.Profile,CampaignColonisationCycleIdentity.Planner,
                CampaignColonisationCycleIdentity.Budget,"one-active;unlimited-successive-until-capacity;first-growth;readiness-before-bootstrap;effective-food;all-camps-self-sufficient;next-week;immutable-target-and-terminal;shared8/4;512/262144;receipt128/16384;outer64/16384");
        }
        internal static ColonisationCycleMissionDto Copy(ColonisationCycleMissionDto m)=>new ColonisationCycleMissionDto{Ordinal=m.Ordinal,StartedTurn=m.StartedTurn,SettlementManifestId=m.SettlementManifestId,CommittedTargetPlanetId=m.CommittedTargetPlanetId,Assignment=m.Assignment==null?null:Copy(m.Assignment)};
        internal static ColonisationCycleReceiptDto Copy(ColonisationCycleReceiptDto x)=>new ColonisationCycleReceiptDto{Ordinal=x.Ordinal,StartedTurn=x.StartedTurn,TerminalTurn=x.TerminalTurn,ClosedTurn=x.ClosedTurn,Outcome=x.Outcome,SettlementManifestId=x.SettlementManifestId,TargetPlanetId=x.TargetPlanetId,CampId=x.CampId};
        internal static ColonisationCycleActorDto Copy(ColonisationCycleActorDto a)=>new ColonisationCycleActorDto{ActorId=a.ActorId,NextOrdinal=a.NextOrdinal,SupplyCampId=a.SupplyCampId,Active=a.Active==null?null:Copy(a.Active),Receipts=a.Receipts!.Select(Copy).ToList()};
    }
}
