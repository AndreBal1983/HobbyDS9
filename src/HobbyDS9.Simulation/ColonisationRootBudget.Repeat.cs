using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static partial class ColonisationRootBudget
    {
        internal const long RepeatActorVisits=512,RepeatActorBytes=262144,RepeatOuterVisits=64,RepeatOuterBytes=16384;
        internal static void ValidateRepeat(CampaignColonisationRepeatSaveDto root,ColonisationPolicyDto policy)
        {
            if(root==null||root.FormatVersion!=CampaignColonisationRepeatIdentity.Format||root.ProfileId!=CampaignColonisationRepeatIdentity.Profile
                ||root.BaseCampaign==null||root.AiPlans==null||root.RepeatGoals==null||root.AiPlans.Count>4||root.RepeatGoals.Count!=root.AiPlans.Count)Fail("Complete bounded repeat root required.");
            void Text(string? s,int max=512){if(s!=null&&s.Length>max)Fail("Repeat text bound.");}
            void Plan(ColonisationAiPlanDto p,string goal,bool shared)
            {
                if(p==null||p.ActorId==null||p.GoalId!=goal||!Enum.IsDefined(typeof(ColonisationAiStage),p.Stage)||p.OwnAttemptedTargets==null||p.OwnAttemptedTargets.Count>8
                    ||p.LatestActions==null||p.LatestActions.Count>(shared?8:0))Fail("Bounded repeat assignment required.");
                Text(p.ActorId);Text(p.GoalId);Text(p.HomeColonyId);Text(p.YardColonyId);Text(p.SourceCohortId);Text(p.ScoutShipId);Text(p.TransportShipId);Text(p.BlueprintId);
                Text(p.TargetPlanetId);Text(p.TargetNodeId);Text(p.KitQueueId);Text(p.TransportQueueId);Text(p.ManifestId);Text(p.CampId);Text(p.EstablishedColonyId);Text(p.HoldReason,128);
                foreach(var id in p.OwnAttemptedTargets){if(id==null)Fail("Null repeat target.");Text(id);}
                foreach(var a in p.LatestActions){if(a==null||a.CommandId==null||!Enum.IsDefined(typeof(ColonisationAiAction),a.Action)||!Enum.IsDefined(typeof(CampaignTacticalError),a.Error))Fail("Invalid repeat receipt.");Text(a.CommandId,128);}
            }
            Text(root.PackageHash);var actors=new HashSet<string>(StringComparer.Ordinal);var repeatActors=new HashSet<string>(StringComparer.Ordinal);
            foreach(var p in root.AiPlans){Plan(p,"establish-first-frontier-colony-v1",true);if(!actors.Add(p.ActorId))Fail("Duplicate repeat actor.");}
            foreach(var p in root.RepeatGoals)
            {
                if(p==null||p.ActorId==null||!repeatActors.Add(p.ActorId)||!actors.Contains(p.ActorId)||!Enum.IsDefined(typeof(ColonisationRepeatState),p.State)||p.Assignment==null)Fail("Invalid repeat census.");
                Text(p.ActorId);Text(p.SettlementManifestId);Text(p.SupplyCampId);Plan(p.Assignment,CampaignColonisationRepeatIdentity.Goal,false);
            }
            // Fixed from genesis; neither accepted work nor full target lists reduce future headroom.
            ValidateCore(root.BaseCampaign,policy,checked(RepeatOuterVisits+RepeatActorVisits*actors.Count),checked(RepeatOuterBytes+RepeatActorBytes*actors.Count),ColonisationSourceEdition.Growth);
        }
    }
}
