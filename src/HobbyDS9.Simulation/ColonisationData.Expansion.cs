using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    internal static partial class ColonisationData
    {
        internal static string ExpansionHash(CampaignColonisationExpansionPackageDto p)
        {
            Require(p!=null&&p.FormatVersion==1&&p.ProfileId==CampaignColonisationExpansionIdentity.PackageProfile&&p.BaseRosterV2!=null
                &&p.BaseRosterV2.PackageHash==RosterHash(p.BaseRosterV2),"Different expansion base.");
            var v=p!.PlannerPolicy;
            Require(v!=null&&v.Algorithm==CampaignColonisationExpansionIdentity.Planner&&v.ActiveGoals==1&&v.MaximumAttempts==8&&v.MaximumAccepted==4
                &&v.BodyCandidates==16&&v.RouteCandidates==8&&v.FailedTargets==8&&v.BfsTraversals==2&&v.KnownNodes==2048&&v.KnownEdges==8192
                &&v.HomeDeficitWeeks==2&&v.ContingencyWeeks==2&&v.DisputedWeeks==2,"Unsupported provisional expansion policy.");
            return CampaignActivitiesData.Identity(p.ProfileId,p.BaseRosterV2!.PackageHash,v!.Algorithm,CampaignColonisationExpansionIdentity.Budget,CampaignColonisationExpansionIdentity.InventoryPolicy,CampaignColonisationExpansionIdentity.InventoryAuthoring,
                string.Join(",",new[]{v.ActiveGoals,v.MaximumAttempts,v.MaximumAccepted,v.BodyCandidates,v.RouteCandidates,v.FailedTargets,v.BfsTraversals,v.KnownNodes,v.KnownEdges,v.HomeDeficitWeeks,v.ContingencyWeeks,v.DisputedWeeks}.Select(n=>n.ToString(CultureInfo.InvariantCulture))));
        }
        internal static ColonisationPlannerPolicyDto Copy(ColonisationPlannerPolicyDto p)=>new ColonisationPlannerPolicyDto{
            Algorithm=p.Algorithm,ActiveGoals=p.ActiveGoals,MaximumAttempts=p.MaximumAttempts,MaximumAccepted=p.MaximumAccepted,BodyCandidates=p.BodyCandidates,
            RouteCandidates=p.RouteCandidates,FailedTargets=p.FailedTargets,BfsTraversals=p.BfsTraversals,KnownNodes=p.KnownNodes,KnownEdges=p.KnownEdges,
            HomeDeficitWeeks=p.HomeDeficitWeeks,ContingencyWeeks=p.ContingencyWeeks,DisputedWeeks=p.DisputedWeeks};
        internal static ColonisationAiPlanDto Copy(ColonisationAiPlanDto p)=>new ColonisationAiPlanDto{
            ActorId=p.ActorId,GoalId=p.GoalId,Stage=p.Stage,HomeColonyId=p.HomeColonyId,YardColonyId=p.YardColonyId,SourceCohortId=p.SourceCohortId,
            ScoutShipId=p.ScoutShipId,TransportShipId=p.TransportShipId,BlueprintId=p.BlueprintId,BlueprintRevision=p.BlueprintRevision,
            TargetPlanetId=p.TargetPlanetId,TargetNodeId=p.TargetNodeId,KitQueueId=p.KitQueueId,TransportQueueId=p.TransportQueueId,
            ManifestId=p.ManifestId,CampId=p.CampId,EstablishedColonyId=p.EstablishedColonyId,LastPlannedTurn=p.LastPlannedTurn,DisputedBoundaries=p.DisputedBoundaries,
            HoldReason=p.HoldReason,OwnAttemptedTargets=p.OwnAttemptedTargets==null?null:new List<string>(p.OwnAttemptedTargets),
            LatestActions=p.LatestActions?.Select(a=>new ColonisationAiActionReceiptDto{CommandId=a.CommandId,Action=a.Action,BeforeRevision=a.BeforeRevision,AfterRevision=a.AfterRevision,Accepted=a.Accepted,Error=a.Error}).ToList()};
    }
}
