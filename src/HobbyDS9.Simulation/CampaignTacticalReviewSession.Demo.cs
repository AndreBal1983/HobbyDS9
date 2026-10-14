using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private CampaignSingleplayerDemoPackageDto? demo;
        private bool DemoMutationBlocked=>demo!=null&&DemoSnapshot().Outcome!=CampaignDemoOutcome.Active;
        private static CampaignSingleplayerDemoPackageDto CopyDemoSettings(CampaignSingleplayerDemoPackageDto p)
            => new CampaignSingleplayerDemoPackageDto { FormatVersion=p.FormatVersion,ProfileId=p.ProfileId,PackageHash=p.PackageHash,
                PolicyStatus=p.PolicyStatus,DefaultActivationAllowed=p.DefaultActivationAllowed,HumanActorId=p.HumanActorId,
                OpponentActorId=p.OpponentActorId,ObjectiveNodeId=p.ObjectiveNodeId,LastTurn=p.LastTurn,AiPolicyId=p.AiPolicyId,ObjectivePolicyId=p.ObjectivePolicyId };
        private static void ValidateDemoPackage(CampaignSingleplayerDemoPackageDto p)
        {
            Require(p!=null&&p.FormatVersion==4&&p.ProfileId==CampaignSingleplayerDemoIdentity.Profile&&Hash(p.PackageHash)
                &&p.PolicyStatus=="proposed"&&p.DefaultActivationAllowed==false&&p.AiPolicyId==CampaignSingleplayerDemoIdentity.AiPolicy
                &&p.ObjectivePolicyId==CampaignSingleplayerDemoIdentity.ObjectivePolicy&&p.LastTurn==12&&p.ObjectiveNodeId=="nav.bajor"&&p.Integrated!=null,"Unsupported demo policy/package.");
            ValidateIntegratedPackage(p.Integrated!);
            var game=p.Integrated!.Physical!.CorePackage!.Campaign!.InitialState;
            Require(game.TurnNumber==1&&game.Civilizations!.Count(c=>c.ControllerKind==ControllerKind.Human)==1
                &&game.Civilizations.Any(c=>c.CivilizationId==p.HumanActorId&&c.ControllerKind==ControllerKind.Human)
                &&game.Civilizations.Any(c=>c.CivilizationId==p.OpponentActorId&&c.ControllerKind==ControllerKind.AI)
                &&game.Fleets!.All(f=>f.OwnerCivilizationId==p.HumanActorId||f.OwnerCivilizationId==p.OpponentActorId),"Demo requires one human and its declared AI opponent.");
            Require(game.NavigationKnowledge!.Where(k=>k.CivilizationId==p.HumanActorId||k.CivilizationId==p.OpponentActorId)
                .All(k=>k.Nodes!.Any(n=>n.Id==p.ObjectiveNodeId&&n.Level!=NavigationKnowledgeLevel.Unknown)),"Demo objective must be lawful known topology.");
        }
        public static CampaignTacticalReviewSession StartDemo(CampaignSingleplayerDemoPackageDto p)
        {
            ValidateDemoPackage(p);var i=p.Integrated!;
            return new CampaignTacticalReviewSession(i.Physical!.CorePackage!,null,i.Physical,null,i,null,p);
        }
        public static CampaignTacticalReviewSession LoadDemo(CampaignSingleplayerDemoPackageDto p,CampaignSingleplayerDemoSaveDto s)
        {
            ValidateDemoPackage(p);
            Require(s!=null&&s.FormatVersion==4&&s.ProfileId==p.ProfileId&&s.PackageHash==p.PackageHash&&s.Integrated!=null
                &&s.OpponentPlan!=null&&s.OpponentPlan.PolicyId==p.AiPolicyId&&s.OpponentPlan.ActorId==p.OpponentActorId
                &&s.OpponentPlan.GoalNodeId==p.ObjectiveNodeId,"Unsupported demo save/plan; no migration.");
            var i=p.Integrated!;var saved=s.Integrated!;
            Require(saved.FormatVersion==3&&saved.ProfileId==i.ProfileId&&saved.PackageHash==i.PackageHash&&saved.Physical!=null
                &&saved.BuiltShips!=null&&saved.BuiltShips.All(b=>b!=null),"Invalid nested integrated checkpoint.");
            RequireTravelShape(saved.Physical!);
            var session=new CampaignTacticalReviewSession(i.Physical!.CorePackage!,saved.Physical!.Core,i.Physical,saved.Physical,i,saved,p);
            Require(s.OpponentPlan!.LastPlannedTurn==session.DemoLastPlannedTurn,"Forged demo planning boundary.");
            return session;
        }
        private int DemoLastPlannedTurn=>state.Campaign.GameState.TurnNumber-(CurrentPhase==SessionPhase.Planning?1:0);
        public CampaignSingleplayerDemoSaveDto ExportDemoSave()
        {
            lock(gate)
            {
                Require(demo!=null,"Not a single-player demo.");
                return new CampaignSingleplayerDemoSaveDto {FormatVersion=4,ProfileId=demo!.ProfileId!,PackageHash=demo.PackageHash!,
                    Integrated=ExportIntegratedCore(),OpponentPlan=new CampaignDemoPlanDto {PolicyId=demo.AiPolicyId!,ActorId=demo.OpponentActorId!,
                        GoalNodeId=demo.ObjectiveNodeId!,LastPlannedTurn=DemoLastPlannedTurn}};
            }
        }
        public CampaignDemoSnapshot CaptureDemo(){lock(gate){Require(demo!=null,"Not a demo.");return DemoSnapshot();}}
        private CampaignDemoSnapshot DemoSnapshot()
        {
            var game=state.Campaign.GameState;var outcome=CampaignDemoOutcome.Active;
            // Trusted evaluator may inspect actual results, but returns no enemy positions/stocks/counts.
            if(CurrentPhase==SessionPhase.Planning)
            {
                if(!game.Ships!.Any(s=>s.OwnerCivilizationId==demo!.HumanActorId))outcome=CampaignDemoOutcome.Defeat;
                else if(!game.Ships.Any(s=>s.OwnerCivilizationId==demo!.OpponentActorId))outcome=CampaignDemoOutcome.Victory;
                else if(game.TurnNumber>demo!.LastTurn)
                {
                    // Only the terminal evaluator sees both actual forces, never the AI planner/UI.
                    long Strength(string actor)=>game.Ships.Where(s=>s.OwnerCivilizationId==actor&&game.Fleets!.Any(f=>f.FleetId==s.FleetId
                        &&f.LocationNodeId==demo.ObjectiveNodeId&&f.TransitTotalCost==0)).Sum(s=>s.CurrentHull);
                    outcome=DemoDeadlineOutcome(Strength(demo.HumanActorId!),Strength(demo.OpponentActorId!));
                }
            }
            return new CampaignDemoSnapshot(demo!.HumanActorId!,demo.ObjectiveNodeId!,demo.LastTurn!.Value,game.TurnNumber,CurrentPhase,outcome);
        }
        internal static CampaignDemoOutcome DemoDeadlineOutcome(long human,long opponent)=>human==0||human<opponent
            ?CampaignDemoOutcome.Defeat:human>opponent?CampaignDemoOutcome.Victory:CampaignDemoOutcome.Stalemate;
        private void PlanDemoTravel(CampaignTacticalTravelSaveDto staged)
        {
            var game=staged.Core.Campaign.GameState;var actor=demo!.OpponentActorId!;
            foreach(var fleet in game.Fleets!.Where(f=>f.OwnerCivilizationId==actor).OrderBy(f=>f.FleetId,StringComparer.Ordinal))
            {
                if(fleet.TransitTotalCost>0)continue; // Locked partial-edge route remains authoritative.
                var route=new WeightedNavigationGraph(definitions).FindRoute(game,actor,fleet.LocationNodeId,demo.ObjectiveNodeId!);
                var order=new CampaignTravelOrderDto {FleetId=fleet.FleetId,Intent=fleet.LocationNodeId==demo.ObjectiveNodeId?CampaignTravelIntent.StayEngage:CampaignTravelIntent.Leave,
                    RouteNodeIds=route.Success?route.Steps.ToList():new List<string>()};
                if(fleet.LocationNodeId==demo.ObjectiveNodeId){order.RouteNodeIds.Clear();}
                if(!route.Success&&fleet.LocationNodeId!=demo.ObjectiveNodeId){order.Intent=CampaignTravelIntent.Stay;}
                var command=new SubmitCampaignTravelOrderDto {ActorId=actor,CommandId="ai-command.demo.route.t"+game.TurnNumber.ToString("D10",CultureInfo.InvariantCulture)+"."+fleet.FleetId,
                    ExpectedTurn=game.TurnNumber,ExpectedRevision=game.StateRevision,Order=order};
                var error=ValidateTravelCommand(command,staged,true);
                Require(error==CampaignTacticalError.None,"Invalid lawful demo travel commitment: "+error);
                ApplyTravelCommand(staged,command);
            }
        }
        private List<CampaignTacticalFleetOrderDto> DemoTacticalOrders(CampaignTacticalBattleCheckpointDto b,string actor)
        {
            // Project strictly round-start own conditions and visible enemy conditions first.
            // This policy has no commitments, future orders, enemy ammo, RNG or hidden equipment input.
            var own=b.State.Formations.Where(f=>f.SideId==actor&&f.Status==TacticalFormationStatus.Active)
                .SelectMany(f=>f.Ships.Where(s=>s.Hull>0).Select(s=>Condition(f.FormationId,s))).ToList();
            var enemies=b.State.Formations.Where(f=>f.SideId!=actor&&f.Status==TacticalFormationStatus.Active)
                .SelectMany(f=>f.Ships.Where(s=>s.Hull>0&&!b.HiddenShipIds.Contains(s.ShipId,StringComparer.Ordinal)).Select(s=>Condition(f.FormationId,s)))
                .OrderBy(s=>s.Hull).ThenBy(s=>s.FleetId,StringComparer.Ordinal).ThenBy(s=>s.ShipId,StringComparer.Ordinal).ToList();
            return DemoAiPolicy.Orders(own,enemies);
        }
    }
}
