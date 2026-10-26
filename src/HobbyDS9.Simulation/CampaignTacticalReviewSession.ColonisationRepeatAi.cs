using System;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private enum MissionSlot { First,Second }
        private ColonisationAiPlanDto AssignedPlan(string actor,MissionSlot slot)=>slot==MissionSlot.First?ExpansionPlan(actor):RepeatGoal(actor).Assignment!;
        private bool ExpansionAttempt(MissionSlot slot,string actor,ColonisationAiAction action,Func<CampaignTacticalReviewSession,string,CampaignTacticalError> execute)
        {
            var accepted=ExpansionAttempt(actor,action,execute);
            if(!accepted&&slot==MissionSlot.Second)AssignedPlan(actor,slot).HoldReason="Second expedition action unavailable; cargo and history retained.";
            return accepted;
        }
        private bool ExpansionColonisation(MissionSlot slot,string actor,ColonisationAiAction action,ColonisationCommandDto command)
        {
            if(slot==MissionSlot.First)return ExpansionColonisation(actor,action,command);
            return ExpansionAttempt(slot,actor,action,(c,id)=>{
                var g=c.state.Campaign.GameState;command.ActorId=actor;command.CommandId=id;command.Turn=g.TurnNumber;command.Revision=g.StateRevision;
                var result=c.ApplyColonisationCommand(command,ControllerKind.AI);
                if(result.Accepted&&slot==MissionSlot.Second&&command.Action==ColonisationAction.EmbarkSettlers)
                {
                    var goal=c.RepeatGoal(actor);var manifest=c.colonisation!.State.ManifestRecords!.Single(m=>m.OwnerId==actor&&m.ShipId==command.Embark!.ShipId&&m.Stage==SettlerManifestStage.Loaded);
                    Require(goal.SettlementManifestId==null||goal.SettlementManifestId==manifest.ManifestId,"Second settlement attempt already committed.");
                    goal.SettlementManifestId=manifest.ManifestId;goal.Assignment!.ManifestId=manifest.ManifestId;
                }
                return result.Error;
            });
        }
        private bool ExpansionBuild(MissionSlot slot,string actor,ColonyStateDto world,BlueprintRevisionDto source)
        {
            if(slot==MissionSlot.First)return ExpansionBuild(actor,world,source);
            return ExpansionAttempt(slot,actor,ColonisationAiAction.Ship,(c,id)=>{
                var g=c.state.Campaign.GameState;var error=c.EnqueueActivity(g,new(){ActorId=actor,Kind=CampaignActivityCommandKind.EnqueueShip,TargetId=world.ColonyId,DefinitionId=source.BlueprintId,BlueprintRevision=source.Revision},ControllerKind.AI);
                if(error==CampaignTacticalError.None){g.StateRevision=checked(g.StateRevision+1);g.AcceptedCommandIds!.Add(id);c.AssignedPlan(actor,slot).TransportQueueId=c.colonisation!.Production.Last().QueueItemId;}return error;});
        }
        private bool ExpansionMove(MissionSlot slot,string actor,ShipStateDto ship,string destination,ExpansionSearch searches)
        {
            if(slot==MissionSlot.First)return ExpansionMove(actor,ship,destination,searches);
            var game=state.Campaign.GameState;var fleet=game.Fleets!.Single(f=>f.FleetId==ship.FleetId);
            if(fleet.TransitTotalCost>0||fleet.RouteNodeIds!.Count>0||fleet.LocationNodeId==destination)return false;
            var route=ExpansionRoute(game,actor,fleet.LocationNodeId,destination,searches);
            if(route==null){AssignedPlan(actor,slot).HoldReason="No currently legal known route; physical cargo retained.";return false;}
            return ExpansionAttempt(slot,actor,ColonisationAiAction.Travel,(c,id)=>{
                var g=c.state.Campaign.GameState;var command=new SubmitCampaignTravelOrderDto{ActorId=actor,CommandId=id,ExpectedTurn=g.TurnNumber,ExpectedRevision=g.StateRevision,
                    Order=new CampaignTravelOrderDto{FleetId=ship.FleetId,Intent=CampaignTravelIntent.Leave,RouteNodeIds=route}};
                var error=c.ValidateTravelCommand(command,c.travel!,true);if(error==CampaignTacticalError.None)ApplyTravelCommand(c.travel!,command);return error;});
        }
        private bool RepeatNeedsSupply(GameStateDto game,SettlementCampDto camp)
        {
            var world=game.Colonies!.SingleOrDefault(c=>c.ColonyId==camp.ColonyId&&c.OwnerCivilizationId==camp.OwnerId);
            if(world==null||PopulationRules.Population(world)==0||camp.Stage==ColonyEstablishmentStage.EmptyOutpost)return false;
            var output=ProjectOwnedWorld(world,game);var deficit=BigInteger.Max(0,(BigInteger)output.FoodDemand-output.FoodOutput);
            return deficit>0&&(BigInteger)world.Food<deficit*((BigInteger)colonisation!.Policy.MaximumFood+expansion!.Source.Policy.ContingencyWeeks);
        }
        private SettlementCampDto? RepeatCamp(ColonisationAiPlanDto p,string? manifest=null)
            =>colonisation!.State.CampSources!.SingleOrDefault(c=>c.OwnerId==p.ActorId&&(p.CampId!=null?c.CampId==p.CampId:manifest!=null&&c.ManifestId==manifest));
        private void ReconcileRepeat(GameStateDto game)
        {
            foreach(var first in expansion!.Plans)
            {
                var firstManifest=colonisation!.State.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==first.ManifestId&&m.OwnerId==first.ActorId&&m.Kind==SettlerManifestKind.Settlement);
                var camp=RepeatCamp(first,firstManifest?.ManifestId);
                if(camp!=null){first.CampId=camp.CampId;if(camp.Stage==ColonyEstablishmentStage.Established)first.EstablishedColonyId=camp.ColonyId;
                    if(first.Stage!=ColonisationAiStage.Recovering&&first.Stage!=ColonisationAiStage.Immigrating&&first.Stage!=ColonisationAiStage.GrowthClosed&&camp.Stage!=ColonyEstablishmentStage.EmptyOutpost)first.Stage=ColonisationAiStage.Supporting;}
                ReconcileRepeatLanding(first);
                var goal=RepeatGoal(first.ActorId);var p=goal.Assignment!;var second=RepeatCamp(p,goal.SettlementManifestId);
                if(second!=null){p.CampId=second.CampId;if(second.Stage==ColonyEstablishmentStage.Established)p.EstablishedColonyId=second.ColonyId;}
                ReconcileRepeatLanding(p);
                UpdateRepeatOutcome(goal,game);

            }
        }
        private void UpdateRepeatOutcome(ColonisationRepeatGoalDto goal,GameStateDto game)
        {
            if(goal.State!=ColonisationRepeatState.Active)return;
            var p=goal.Assignment!;var second=RepeatCamp(p,goal.SettlementManifestId);
            var manifest=colonisation!.State.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==goal.SettlementManifestId);
            if(second!=null){p.CampId=second.CampId;if(second.Stage==ColonyEstablishmentStage.Established)p.EstablishedColonyId=second.ColonyId;}
            if(second?.Stage==ColonyEstablishmentStage.Established){goal.State=ColonisationRepeatState.Established;goal.ClosedTurn=game.TurnNumber;p.Stage=ColonisationAiStage.Supporting;}
            else if(second?.Stage==ColonyEstablishmentStage.EmptyOutpost||manifest?.Stage==SettlerManifestStage.Destroyed){goal.State=ColonisationRepeatState.Lost;goal.ClosedTurn=game.TurnNumber;p.Stage=ColonisationAiStage.Suspended;}
            else if(manifest?.Stage==SettlerManifestStage.Unloaded){goal.State=ColonisationRepeatState.Aborted;goal.ClosedTurn=game.TurnNumber;p.Stage=ColonisationAiStage.Suspended;}
        }
        private bool RepeatReturn(string actor,ShipStateDto ship,SettlerManifestDto m,ExpansionSearch searches)
        {
            var game=state.Campaign.GameState;var first=ExpansionPlan(actor);
            var home=game.Colonies!.Where(c=>c.OwnerCivilizationId==actor&&EstablishedColony(c.ColonyId)&&PopulationRules.Population(c)>0)
                .OrderByDescending(c=>c.ColonyId==first.HomeColonyId).ThenBy(c=>c.ColonyId,StringComparer.Ordinal).FirstOrDefault();
            if(home==null){first.HoldReason="No current owned return colony; passengers remain aboard.";return false;}
            var intent=colonisation!.State.DeploymentIntents!.SingleOrDefault(i=>i.ManifestId==m.ManifestId);
            if(intent!=null)return ExpansionColonisation(actor,ColonisationAiAction.Withdraw,new(){Action=ColonisationAction.WithdrawDeployment,Withdraw=new(){IntentId=intent.IntentId}});
            if(ShipAt(game,ship,home.SystemId))return ExpansionColonisation(actor,ColonisationAiAction.Unload,new(){Action=ColonisationAction.UnloadTransport,Unload=new(){ManifestId=m.ManifestId,ColonyId=home.ColonyId}});
            return ExpansionMove(actor,ship,physicalNavigation!.NodeForSystem(home.SystemId)!,searches);
        }
        private void ReconcileRepeatLanding(ColonisationAiPlanDto p)
        {
            var receipt=colonisation!.SettlementProof!.Receipts!.SingleOrDefault(r=>r.Intent!.ActorId==p.ActorId&&r.Intent.ManifestId==p.ManifestId);
            if(receipt==null||receipt.Outcome==DeploymentBoundaryOutcome.Deployed)return;
            if(receipt.Outcome==DeploymentBoundaryOutcome.Disputed)p.DisputedBoundaries=Math.Min(expansion!.Source.Policy.DisputedWeeks,p.DisputedBoundaries+1);
            if(receipt.Outcome==DeploymentBoundaryOutcome.Interrupted||p.DisputedBoundaries>=expansion!.Source.Policy.DisputedWeeks)
            {p.Stage=ColonisationAiStage.Recovering;if(!p.OwnAttemptedTargets!.Contains(receipt.Intent!.PlanetId)&&p.OwnAttemptedTargets.Count<expansion!.Source.Policy.FailedTargets)p.OwnAttemptedTargets.Add(receipt.Intent.PlanetId);}
        }
        private bool PlanRepeatAction(string actor,ExpansionSearch searches)
        {
            var game=state.Campaign.GameState;var first=ExpansionPlan(actor);var goal=RepeatGoal(actor);var second=goal.Assignment!;
            second.LastPlannedTurn=game.TurnNumber;UpdateRepeatOutcome(goal,game);
            var camps=colonisation!.State.CampSources!.Where(c=>c.OwnerId==actor&&c.Stage!=ColonyEstablishmentStage.EmptyOutpost)
                .Where(c=>game.Colonies!.Any(w=>w.ColonyId==c.ColonyId&&w.OwnerCivilizationId==actor&&PopulationRules.Population(w)>0))
                .OrderByDescending(c=>RepeatNeedsSupply(game,c)).ThenBy(c=>game.Colonies!.Single(w=>w.ColonyId==c.ColonyId).Food).ThenBy(c=>c.ColonyId,StringComparer.Ordinal).ToList();
            var urgent=camps.FirstOrDefault(c=>RepeatNeedsSupply(game,c));
            var loaded=colonisation.State.ManifestRecords!.Where(m=>m.OwnerId==actor&&m.Stage==SettlerManifestStage.Loaded).OrderBy(m=>m.ManifestId,StringComparer.Ordinal).FirstOrDefault();
            if(loaded!=null)
            {
                var slot=loaded.ManifestId==goal.SettlementManifestId?MissionSlot.Second:MissionSlot.First;var p=AssignedPlan(actor,slot);
                p.TransportShipId=loaded.ShipId;p.ManifestId=loaded.ManifestId;
                if(slot==MissionSlot.Second&&urgent!=null&&urgent.CampId!=p.CampId)p.Stage=ColonisationAiStage.Recovering;
                if(loaded.Kind==SettlerManifestKind.SupplyOnly)return PlanRepeatSupply(actor,colonisation.State.CampSources!.SingleOrDefault(c=>c.CampId==goal.SupplyCampId&&c.OwnerId==actor),loaded,searches);
                if(loaded.Kind==SettlerManifestKind.ReturnOnly||slot==MissionSlot.Second&&p.Stage==ColonisationAiStage.Recovering)
                {var ship=game.Ships!.SingleOrDefault(s=>s.ShipId==loaded.ShipId&&s.OwnerCivilizationId==actor&&s.CurrentHull>0);return ship!=null&&RepeatReturn(actor,ship,loaded,searches);}
                return PlanExpansionAction(actor,searches,slot);
            }
            goal.SupplyCampId=null;
            // Existing first-camp food commitments remain live even after the second goal closes.
            if(urgent!=null)return PlanRepeatSupply(actor,urgent,null,searches);
            if(first.Stage!=ColonisationAiStage.GrowthClosed)return PlanExpansionAction(actor,searches);
            if(goal.State==ColonisationRepeatState.Waiting)
            {
                var camp=RepeatCamp(first);var world=camp==null?null:game.Colonies!.SingleOrDefault(w=>w.ColonyId==camp.ColonyId&&w.OwnerCivilizationId==actor);
                if(camp?.Stage!=ColonyEstablishmentStage.Established||world==null||PopulationRules.Population(world)==0||world.FoodShortageTurns>0)
                {second.HoldReason="Second expedition waits for the actual first colony's viability.";return false;}
                goal.State=ColonisationRepeatState.Active;goal.StartedTurn=game.TurnNumber;
                // Reuse actual assignments, never terminal records or first-goal stage.
                second.TransportShipId=first.TransportShipId;second.HomeColonyId=first.HomeColonyId;second.YardColonyId=first.YardColonyId;
            }
            if(goal.State!=ColonisationRepeatState.Active){second.HoldReason="Second expedition closed; ordinary food support remains available.";return false;}
            return PlanExpansionAction(actor,searches,MissionSlot.Second);
        }
        private bool PlanRepeatSupply(string actor,SettlementCampDto? camp,SettlerManifestDto? manifest,ExpansionSearch searches)
        {
            var game=state.Campaign.GameState;var p=ExpansionPlan(actor);var goal=RepeatGoal(actor);
            var world=camp==null?null:game.Colonies!.SingleOrDefault(c=>c.ColonyId==camp.ColonyId&&c.OwnerCivilizationId==actor&&PopulationRules.Population(c)>0);
            var ship=manifest==null?game.Ships!.Where(s=>s.OwnerCivilizationId==actor&&s.CurrentHull>0).OrderByDescending(s=>s.ShipId==p.TransportShipId).ThenBy(s=>s.ShipId,StringComparer.Ordinal)
                .FirstOrDefault(s=>TransportCapability(game,s)?.Passengers>=colonisation!.Policy.Settlers):game.Ships!.SingleOrDefault(s=>s.ShipId==manifest.ShipId&&s.OwnerCivilizationId==actor&&s.CurrentHull>0);
            if(manifest!=null)
            {
                if(ship==null){p.HoldReason="Supply manifest carrier unavailable; cargo awaits its survival boundary.";return false;}
                // A lost receiver permits only a real own-home return, not phantom delivery to an empty outpost.
                world??=game.Colonies!.Where(c=>c.OwnerCivilizationId==actor&&EstablishedColony(c.ColonyId)&&PopulationRules.Population(c)>0).OrderByDescending(c=>c.ColonyId==p.HomeColonyId).ThenBy(c=>c.ColonyId,StringComparer.Ordinal).FirstOrDefault();
                if(world==null)return false;
                if(ShipAt(game,ship,world.SystemId))return ExpansionAttempt(actor,ColonisationAiAction.Deliver,(c,id)=>{
                    var g=c.state.Campaign.GameState;c.RepeatGoal(actor).SupplyCampId=null;
                    return c.ApplyColonisationCommand(new(){ActorId=actor,CommandId=id,Turn=g.TurnNumber,Revision=g.StateRevision,Action=ColonisationAction.DeliverCarriedSupplies,
                        Deliver=new(){ManifestId=manifest.ManifestId,ColonyId=world.ColonyId,Food=manifest.Food,Materials=manifest.Cargo!.Where(r=>r.Amount>0).Select(PopulationData.Copy).ToList()}},ControllerKind.AI).Error;});
                return ExpansionMove(actor,ship,physicalNavigation!.NodeForSystem(world.SystemId)!,searches);
            }
            if(world==null)return false;
            var origin=ship==null?physicalNavigation!.NodeForSystem(world.SystemId)!:game.Fleets!.Single(f=>f.FleetId==ship.FleetId).LocationNodeId;
            var paths=ExpansionRoutes(game,actor,origin,searches);
            var sources=game.Colonies!.Where(c=>c.ColonyId!=world.ColonyId&&c.OwnerCivilizationId==actor&&EstablishedColony(c.ColonyId)&&PopulationRules.Population(c)>0&&ExpansionFoodAvailable(c,game)>0)
                .Where(c=>paths!=null&&paths.Distance.ContainsKey(physicalNavigation!.NodeForSystem(c.SystemId)!))
                .OrderBy(c=>paths!.Distance[physicalNavigation!.NodeForSystem(c.SystemId)!]).ThenBy(c=>c.ColonyId,StringComparer.Ordinal).ToList();
            var home=sources.FirstOrDefault();if(home==null){p.HoldReason="No distinct owned resident-reserved food source; no supply loaded.";return false;}
            if(ship==null)
            {
                var yard=game.Colonies!.Where(c=>c.OwnerCivilizationId==actor&&EstablishedColony(c.ColonyId)&&WorldEconomy.Infrastructure(c,definitions).Any(i=>i.definition.Shipbuilding)).OrderByDescending(c=>ProjectOwnedWorld(c,game).Industry).ThenBy(c=>c.ColonyId,StringComparer.Ordinal).FirstOrDefault();
                var blueprint=game.Blueprints!.Where(b=>b.OwnerCivilizationId==actor&&b.Fit!.ClassId=="class.galaxy-colony-transport.v1").OrderBy(b=>b.BlueprintId,StringComparer.Ordinal).ThenByDescending(b=>b.Revision).FirstOrDefault();
                if(yard!=null&&blueprint!=null&&!colonisation!.Production.Any(q=>q.OwnerId==actor&&q.Blueprint?.BlueprintId==blueprint.BlueprintId)){p.YardColonyId=yard.ColonyId;p.BlueprintId=blueprint.BlueprintId;p.BlueprintRevision=blueprint.Revision;return ExpansionBuild(actor,yard,blueprint);}return false;
            }
            p.TransportShipId=ship.ShipId;
            if(ShipAt(game,ship,home.SystemId))return ExpansionAttempt(actor,ColonisationAiAction.SupplyLoad,(c,id)=>{
                var g=c.state.Campaign.GameState;c.RepeatGoal(actor).SupplyCampId=camp!.CampId;var result=c.ApplyColonisationCommand(new(){ActorId=actor,CommandId=id,Turn=g.TurnNumber,Revision=g.StateRevision,Action=ColonisationAction.LoadCarriedSupplies,
                    LoadSupplies=new(){ShipId=ship.ShipId,SourceColonyId=home.ColonyId,Food=Math.Min(ExpansionFoodAvailable(home,game),colonisation!.Policy.MaximumFood)}},ControllerKind.AI);
                if(result.Accepted)c.RepeatGoal(actor).SupplyCampId=camp!.CampId;return result.Error;});
            return ExpansionMove(actor,ship,physicalNavigation!.NodeForSystem(home.SystemId)!,searches);
        }
        private bool RepeatMissionFleet(GameStateDto game,string fleet)
        {
            foreach(var g in expansion!.RepeatGoals)
            {
                var first=ExpansionPlan(g.ActorId);var p=g.Assignment!;
                var assigned=game.Ships!.Where(s=>s.OwnerCivilizationId==g.ActorId&&s.FleetId==fleet).Select(s=>s.ShipId).ToList();
                if(colonisation!.State.ManifestRecords!.Any(m=>m.OwnerId==g.ActorId&&m.Stage==SettlerManifestStage.Loaded&&assigned.Contains(m.ShipId)))return true;
                if(colonisation.State.Surveys!.Any(s=>s.ActorId==g.ActorId&&s.Stage==PlanetSurveyStage.Active&&assigned.Contains(s.ShipId)))return true;
                if(g.State==ColonisationRepeatState.Active&&(assigned.Contains(p.TransportShipId!)||assigned.Contains(p.ScoutShipId!)))return true;
                if(first.EstablishedColonyId==null&&first.Stage!=ColonisationAiStage.Suspended&&(assigned.Contains(first.TransportShipId!)||assigned.Contains(first.ScoutShipId!)))return true;
                if(first.Stage==ColonisationAiStage.Immigrating&&assigned.Contains(first.TransportShipId!))return true;
                if(colonisation.State.CampSources!.Any(c=>c.OwnerId==g.ActorId&&RepeatNeedsSupply(game,c))&&assigned.Contains(first.TransportShipId!))return true;
            }return false;
        }
        private bool RepeatMissionWorld(string actor,string colony)
        {
            var g=RepeatGoal(actor);var p=g.Assignment!;var first=ExpansionPlan(actor);var game=state.Campaign.GameState;
            bool Kit(ColonisationAiPlanDto plan)=>plan.HomeColonyId==colony&&PopulationRules.Stock(game.Colonies!.Single(c=>c.ColonyId==colony).Stockpiles!,colonisation!.Policy.EquipmentResourceId)<1;
            if(first.EstablishedColonyId==null&&first.Stage!=ColonisationAiStage.Suspended&&(Kit(first)||first.YardColonyId==colony&&first.TransportShipId==null))return true;
            if(g.State==ColonisationRepeatState.Active&&(g.SettlementManifestId==null&&Kit(p)||p.YardColonyId==colony&&p.TransportShipId==null))return true;
            return first.YardColonyId==colony&&!game.Ships!.Any(s=>s.OwnerCivilizationId==actor&&s.CurrentHull>0&&TransportCapability(game,s)?.Passengers>=colonisation!.Policy.Settlers)
                &&colonisation!.State.CampSources!.Any(c=>c.OwnerId==actor&&RepeatNeedsSupply(game,c));
        }
    }
}
