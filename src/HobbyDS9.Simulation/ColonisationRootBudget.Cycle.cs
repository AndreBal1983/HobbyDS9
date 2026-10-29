using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal sealed class ColonisationRootCapacityException:ArgumentException { internal ColonisationRootCapacityException():base("Cycle root work or byte capacity exhausted."){} }
    internal static partial class ColonisationRootBudget
    {
        internal const long CycleActorVisits=512,CycleActorBytes=262144,CycleReceiptVisits=128,CycleReceiptBytes=16384,CycleOuterVisits=64,CycleOuterBytes=16384;
        internal static void ValidateCycle(CampaignColonisationCycleSaveDto root,ColonisationPolicyDto policy)
        {
            if(root==null||root.FormatVersion!=CampaignColonisationCycleIdentity.Format||root.ProfileId!=CampaignColonisationCycleIdentity.Profile||root.BaseCampaign==null
                ||root.AiPlans==null||root.CycleActors==null||root.AiPlans.Count>4||root.CycleActors.Count!=root.AiPlans.Count)Fail("Complete bounded cycle root required.");
            void Text(string? x,int max=512){if(x!=null&&x.Length>max)Fail("Cycle string bound.");}
            void Plan(ColonisationAiPlanDto p,string goal,bool shared)
            {
                if(p==null||p.ActorId==null||p.GoalId!=goal||!Enum.IsDefined(typeof(ColonisationAiStage),p.Stage)||!shared&&(int)p.Stage>8||p.OwnAttemptedTargets==null||p.OwnAttemptedTargets.Count>8
                    ||p.LatestActions==null||p.LatestActions.Count>(shared?8:0)||p.HoldReason==null)Fail("Bounded cycle assignment required.");
                Text(p.ActorId);Text(p.GoalId);Text(p.HomeColonyId);Text(p.YardColonyId);Text(p.SourceCohortId);Text(p.ScoutShipId);Text(p.TransportShipId);Text(p.BlueprintId);
                Text(p.TargetPlanetId);Text(p.TargetNodeId);Text(p.KitQueueId);Text(p.TransportQueueId);Text(p.ManifestId);Text(p.CampId);Text(p.EstablishedColonyId);Text(p.HoldReason,128);
                foreach(var id in p.OwnAttemptedTargets){if(id==null)Fail("Null cycle target.");Text(id);}
                foreach(var a in p.LatestActions){if(a==null||a.CommandId==null||!Enum.IsDefined(typeof(ColonisationAiAction),a.Action)||!Enum.IsDefined(typeof(CampaignTacticalError),a.Error))Fail("Invalid cycle receipt.");Text(a.CommandId,128);}
            }
            Text(root.PackageHash);var actors=new HashSet<string>(StringComparer.Ordinal);var cycles=new HashSet<string>(StringComparer.Ordinal);long receipts=0;
            foreach(var p in root.AiPlans){Plan(p,"establish-first-frontier-colony-v1",true);if(!actors.Add(p.ActorId))Fail("Duplicate cycle actor.");}
            foreach(var a in root.CycleActors)
            {
                if(a==null||a.ActorId==null||!actors.Contains(a.ActorId)||!cycles.Add(a.ActorId)||a.Receipts==null||a.Receipts.Count>policy.RecordBudget)Fail("Invalid cycle actor census.");
                Text(a.ActorId);Text(a.SupplyCampId);receipts=checked(receipts+a.Receipts.Count+(a.Active==null?0:1));
                if(receipts>policy.RecordBudget)Fail("Cycle receipt record bound.");
                if(a.Active!=null){if(a.Active.Assignment==null)Fail("Active cycle assignment required.");Plan(a.Active.Assignment,CampaignColonisationCycleIdentity.Goal,false);Text(a.Active.SettlementManifestId);Text(a.Active.CommittedTargetPlanetId);}
                foreach(var x in a.Receipts){if(x==null||x.SettlementManifestId==null||x.TargetPlanetId==null||!Enum.IsDefined(typeof(ColonisationCycleOutcome),x.Outcome))Fail("Invalid cycle terminal receipt.");Text(x.SettlementManifestId);Text(x.TargetPlanetId);Text(x.CampId);}
            }
            ValidateCore(root.BaseCampaign,policy,checked(CycleOuterVisits+actors.Count*CycleActorVisits+receipts*CycleReceiptVisits),checked(CycleOuterBytes+actors.Count*CycleActorBytes+receipts*CycleReceiptBytes),ColonisationSourceEdition.Growth,true);
        }
    }
}
