using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    internal static partial class ColonisationData
    {
        internal static string RepeatHash(CampaignColonisationRepeatPackageDto p)
        {
            Require(p!=null&&p.FormatVersion==1&&p.ProfileId==CampaignColonisationRepeatIdentity.PackageProfile&&p.BaseGrowth!=null
                &&p.BaseGrowth.PackageHash==GrowthHash(p.BaseGrowth)&&p.Policy!=null&&p.Policy.Algorithm==CampaignColonisationRepeatIdentity.Planner
                &&p.Policy.AdditionalColonies==1&&p.Policy.SettlementAttempts==1,"Different closed repeat source.");
            return CampaignActivitiesData.Identity(p!.ProfileId,p.BaseGrowth!.PackageHash,CampaignColonisationRepeatIdentity.Profile,
                CampaignColonisationRepeatIdentity.Planner,CampaignColonisationRepeatIdentity.Budget,"one-second-settlement;supply-camp-commitment-v1;establish-only;attempt1;shared8/4;512visits;262144bytes;outer64/16384");
        }
        internal static ColonisationRepeatGoalDto Copy(ColonisationRepeatGoalDto p)=>new ColonisationRepeatGoalDto{
            ActorId=p.ActorId,State=p.State,StartedTurn=p.StartedTurn,ClosedTurn=p.ClosedTurn,SettlementManifestId=p.SettlementManifestId,SupplyCampId=p.SupplyCampId,
            Assignment=p.Assignment==null?null:Copy(p.Assignment)};
    }
}
