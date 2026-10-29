using System;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private void CommitCycleManifest(string actor,string ship)
        {
            var active=CycleActor(actor).Active!;var p=active.Assignment!;
            var manifest=colonisation!.State.ManifestRecords!.Single(m=>m.OwnerId==actor&&m.ShipId==ship&&m.Stage==SettlerManifestStage.Loaded&&m.Kind==SettlerManifestKind.Settlement);
            Require(active.SettlementManifestId==null&&active.CommittedTargetPlanetId==null&&p.TargetPlanetId!=null,"Cycle already committed or target absent.");
            var target=ColonisationPlanetsForActor(state.Campaign.GameState,actor).SingleOrDefault(o=>o.PlanetId==p.TargetPlanetId&&o.Surveyed);
            Require(target!=null&&physicalNavigation!.NodeForSystem(target.SystemId)==p.TargetNodeId,"Cycle commit needs actual surveyed target.");
            active.SettlementManifestId=manifest.ManifestId;active.CommittedTargetPlanetId=p.TargetPlanetId;p.ManifestId=manifest.ManifestId;
        }
        private void ReconcileCycle(GameStateDto game)
        {
            foreach(var first in expansion!.Plans)
            {
                var camp=RepeatCamp(first);
                if(camp==null){var m=colonisation!.State.ManifestRecords!.SingleOrDefault(x=>x.ManifestId==first.ManifestId&&x.Kind==SettlerManifestKind.Settlement);camp=RepeatCamp(first,m?.ManifestId);}
                if(camp!=null){first.CampId=camp.CampId;if(camp.Stage==ColonyEstablishmentStage.Established)first.EstablishedColonyId=camp.ColonyId;
                    if(first.Stage!=ColonisationAiStage.Recovering&&first.Stage!=ColonisationAiStage.Immigrating&&first.Stage!=ColonisationAiStage.GrowthClosed&&camp.Stage!=ColonyEstablishmentStage.EmptyOutpost)first.Stage=ColonisationAiStage.Supporting;}
                ReconcileRepeatLanding(first);var owner=CycleActor(first.ActorId);var active=owner.Active;if(active==null)continue;
                var p=active.Assignment!;var source=colonisation!.State.CampSources!.SingleOrDefault(c=>c.OwnerId==first.ActorId&&c.ManifestId==active.SettlementManifestId);
                if(source!=null){p.CampId=source.CampId;if(source.Stage==ColonyEstablishmentStage.Established)p.EstablishedColonyId=source.ColonyId;}
                ReconcileRepeatLanding(p);
                UpdateCycleOutcome(owner,game);
            }
        }
        private void UpdateCycleOutcome(ColonisationCycleActorDto owner,GameStateDto game)
        {
            var active=owner.Active;if(active==null)return;
            var source=colonisation!.State.CampSources!.SingleOrDefault(c=>c.OwnerId==owner.ActorId&&c.ManifestId==active.SettlementManifestId);
                if(active.SettlementManifestId==null)return;
                var terminal=source==null?null:colonisation!.State.CampTerminals!.FirstOrDefault(t=>t.CampId==source.CampId&&t.Kind==SettlementTerminalKind.Established);
                var resolution=colonisation!.State.ManifestResolutions!.SingleOrDefault(x=>x.ManifestId==active.SettlementManifestId);
                ColonisationCycleOutcome? outcome=null;int turn=0;
                if(terminal!=null){outcome=ColonisationCycleOutcome.Established;turn=terminal.Turn;}
                else if(source?.Stage==ColonyEstablishmentStage.EmptyOutpost){outcome=ColonisationCycleOutcome.Lost;turn=colonisation!.State.CampTerminals!.Where(t=>t.CampId==source.CampId).Min(t=>t.Turn);}
                else if(resolution?.Stage==SettlerManifestStage.Unloaded){outcome=ColonisationCycleOutcome.Returned;turn=resolution.Turn;}
                else if(resolution?.Stage==SettlerManifestStage.Destroyed){outcome=ColonisationCycleOutcome.Lost;turn=resolution.Turn;}
                if(outcome.HasValue)
                {
                    owner.Receipts!.Add(new ColonisationCycleReceiptDto{Ordinal=active.Ordinal,StartedTurn=active.StartedTurn,ClosedTurn=game.TurnNumber,TerminalTurn=turn,
                        Outcome=outcome.Value,SettlementManifestId=active.SettlementManifestId,TargetPlanetId=active.CommittedTargetPlanetId!,CampId=source?.CampId});
                    owner.Active=null; // The reserved receipt charge is replaced, never added at closure.
                }
        }
        private bool PlanCycleAction(string actor,ExpansionSearch searches)
        {
            var game=state.Campaign.GameState;var first=ExpansionPlan(actor);var owner=CycleActor(actor);UpdateCycleOutcome(owner,game);
            var camps=colonisation!.State.CampSources!.Where(c=>c.OwnerId==actor&&c.Stage!=ColonyEstablishmentStage.EmptyOutpost)
                .Where(c=>game.Colonies!.Any(w=>w.ColonyId==c.ColonyId&&w.OwnerCivilizationId==actor&&PopulationRules.Population(w)>0))
                .OrderByDescending(c=>RepeatNeedsSupply(game,c)).ThenBy(c=>game.Colonies!.Single(w=>w.ColonyId==c.ColonyId).Food).ThenBy(c=>c.ColonyId,StringComparer.Ordinal).ToList();
            var urgent=camps.FirstOrDefault(c=>RepeatNeedsSupply(game,c));
            var loaded=colonisation.State.ManifestRecords!.Where(m=>m.OwnerId==actor&&m.Stage==SettlerManifestStage.Loaded).OrderBy(m=>m.ManifestId,StringComparer.Ordinal).FirstOrDefault();
            if(loaded!=null)
            {
                if(loaded.Kind==SettlerManifestKind.SupplyOnly)return PlanRepeatSupply(actor,colonisation.State.CampSources!.SingleOrDefault(c=>c.CampId==owner.SupplyCampId&&c.OwnerId==actor),loaded,searches);
                var cycle=owner.Active?.SettlementManifestId==loaded.ManifestId;var p=cycle?owner.Active!.Assignment!:first;
                p.TransportShipId=loaded.ShipId;p.ManifestId=loaded.ManifestId;
                if(cycle&&urgent!=null&&urgent.CampId!=p.CampId)p.Stage=ColonisationAiStage.Recovering;
                if(loaded.Kind==SettlerManifestKind.ReturnOnly||cycle&&p.Stage==ColonisationAiStage.Recovering)
                {var ship=game.Ships!.SingleOrDefault(s=>s.ShipId==loaded.ShipId&&s.OwnerCivilizationId==actor&&s.CurrentHull>0);return ship!=null&&RepeatReturn(actor,ship,loaded,searches);}
                return PlanAssignedExpansionAction(actor,searches,cycle?MissionSlot.Cycle:MissionSlot.First);
            }
            owner.SupplyCampId=null;
            if(urgent!=null)return PlanRepeatSupply(actor,urgent,null,searches);
            if(PlanCycleRecipientFoodReadiness(actor))return true;
            if(first.Stage!=ColonisationAiStage.GrowthClosed)return PlanExpansionAction(actor,searches);
            if(owner.Active!=null)return PlanAssignedExpansionAction(actor,searches,MissionSlot.Cycle);
            if(owner.Receipts!.Any(x=>x.ClosedTurn>=game.TurnNumber))return false;
            if(camps.Any(c=>c.Stage!=ColonyEstablishmentStage.Established||game.Colonies!.Single(w=>w.ColonyId==c.ColonyId).FoodShortageTurns>0
                ||CycleFoodDeficit(game.Colonies!.Single(w=>w.ColonyId==c.ColonyId),game)))
            {first.HoldReason="Cycle waits for current established frontier food self-sufficiency.";return false;}
            if(owner.NextOrdinal==int.MaxValue){first.HoldReason="Cycle ordinal capacity exhausted; ordinary logistics retained.";return false;}
            // Activation reserves its complete future receipt BEFORE any cycle procurement.
            var candidate=ForkPublished();var own=candidate.CycleActor(actor);var ordinal=own.NextOrdinal;
            own.Active=new ColonisationCycleMissionDto{Ordinal=ordinal,StartedTurn=game.TurnNumber,Assignment=new ColonisationAiPlanDto{ActorId=actor,GoalId=CampaignColonisationCycleIdentity.Goal,
                TransportShipId=first.TransportShipId,HomeColonyId=first.HomeColonyId,YardColonyId=first.YardColonyId}};
            own.NextOrdinal=checked(ordinal+1);
            if(!candidate.ColonisationCapacity(1,null,1)){first.HoldReason="Cycle receipt capacity unavailable; no new procurement.";return false;}
            try{candidate.ValidateColonisationRootBudget();}catch(ColonisationRootCapacityException){first.HoldReason="Cycle root headroom unavailable; no new procurement.";return false;}
            candidate.ValidateActivityState();
            AdoptPublished(candidate);return true;
        }
        private bool CycleFoodDeficit(ColonyStateDto world,GameStateDto game)
        {var output=ProjectOwnedWorld(world,game);return output.FoodOutput<output.FoodDemand;}
        private bool PlanCycleRecipientFoodReadiness(string actor)
        {
            var game=state.Campaign.GameState;var first=ExpansionPlan(actor);
            foreach(var camp in colonisation!.State.CampSources!.Where(c=>c.OwnerId==actor&&c.Stage==ColonyEstablishmentStage.Established).OrderBy(c=>c.ColonyId,StringComparer.Ordinal))
            {
                var world=game.Colonies!.SingleOrDefault(w=>w.ColonyId==camp.ColonyId&&w.OwnerCivilizationId==actor&&PopulationRules.Population(w)>0);if(world==null)continue;
                var projection=ProjectOwnedWorld(world,game);var need=projection.FoodDemand>projection.FoodOutput;
                if(!need&&camp.CampId==first.CampId&&first.Stage!=ColonisationAiStage.GrowthClosed)
                {
                    var source=game.Colonies!.SingleOrDefault(c=>c.ColonyId==first.HomeColonyId&&c.OwnerCivilizationId==actor);
                    var cohort=source?.Cohorts!.OrderBy(c=>c.CohortId,StringComparer.Ordinal).FirstOrDefault(c=>(BigInteger)c.Population*colonisation.Policy.MaximumSourceCohortBps>=(BigInteger)colonisation.Policy.Settlers*10000);
                    if(cohort!=null){var people=PopulationData.Copy(cohort);people.Population=colonisation.Policy.Settlers;
                        if(PopulationRules.CanReceive(world,people,people.LegalStatus)){var copy=PopulationData.Copy(world);copy.Cohorts!.Add(people);var future=ProjectOwnedWorld(copy,game);need=future.Crowding==0&&future.FoodOutput<future.FoodDemand;}}
                }
                if(!need)continue;
                foreach(var structure in activities!.Content.Structures!.Where(s=>s.FoodOutput>0).OrderBy(s=>s.DefinitionId,StringComparer.Ordinal))
                {
                    if(world.Infrastructure!.Any(i=>i.DefinitionId==structure.DefinitionId&&i.Count>0)||colonisation.Production.Any(q=>q.ColonyId==world.ColonyId&&q.DefinitionId==structure.DefinitionId))continue;
                    var copy=PopulationData.Copy(world);copy.Infrastructure!.Add(new InfrastructureStateDto{DefinitionId=structure.DefinitionId,Count=1});
                    if(ProjectOwnedWorld(copy,game).FoodOutput<=projection.FoodOutput)continue;
                    return ExpansionAttempt(actor,ColonisationAiAction.Infrastructure,(c,id)=>{var g=c.state.Campaign.GameState;var error=c.EnqueueActivity(g,new(){ActorId=actor,Kind=CampaignActivityCommandKind.EnqueueStructure,TargetId=world.ColonyId,DefinitionId=structure.DefinitionId},ControllerKind.AI);
                        if(error==CampaignTacticalError.None){g.StateRevision=checked(g.StateRevision+1);g.AcceptedCommandIds!.Add(id);}return error;});
                }
                first.HoldReason="Recipient food readiness waits for an available effective structure and real construction inputs.";
            }
            return false;
        }
        private bool CycleMissionFleet(GameStateDto game,string fleet)
        {
            foreach(var owner in expansion!.CycleActors)
            {
                var first=ExpansionPlan(owner.ActorId);var p=owner.Active?.Assignment;
                var ids=game.Ships!.Where(s=>s.OwnerCivilizationId==owner.ActorId&&s.FleetId==fleet).Select(s=>s.ShipId).ToList();
                if(colonisation!.State.ManifestRecords!.Any(m=>m.OwnerId==owner.ActorId&&m.Stage==SettlerManifestStage.Loaded&&ids.Contains(m.ShipId))
                    ||colonisation.State.Surveys!.Any(s=>s.ActorId==owner.ActorId&&s.Stage==PlanetSurveyStage.Active&&ids.Contains(s.ShipId)))return true;
                if(p!=null&&p.Stage!=ColonisationAiStage.Suspended&&(ids.Contains(p.TransportShipId!)||ids.Contains(p.ScoutShipId!)))return true;
                if(first.EstablishedColonyId==null&&first.Stage!=ColonisationAiStage.Suspended&&(ids.Contains(first.TransportShipId!)||ids.Contains(first.ScoutShipId!)))return true;
                if(first.Stage==ColonisationAiStage.Immigrating&&ids.Contains(first.TransportShipId!))return true;
                if(colonisation.State.CampSources!.Any(c=>c.OwnerId==owner.ActorId&&RepeatNeedsSupply(game,c))&&ids.Contains(first.TransportShipId!))return true;
            }return false;
        }
        private bool CycleMissionWorld(string actor,string colony)
        {
            var first=ExpansionPlan(actor);var p=CycleActor(actor).Active?.Assignment;var game=state.Campaign.GameState;
            bool Kit(ColonisationAiPlanDto x)=>x.HomeColonyId==colony&&PopulationRules.Stock(game.Colonies!.Single(c=>c.ColonyId==colony).Stockpiles!,colonisation!.Policy.EquipmentResourceId)<1;
            if(first.EstablishedColonyId==null&&first.Stage!=ColonisationAiStage.Suspended&&(Kit(first)||first.YardColonyId==colony&&first.TransportShipId==null))return true;
            if(p!=null&&p.Stage!=ColonisationAiStage.Suspended&&(CycleActor(actor).Active!.SettlementManifestId==null&&Kit(p)||p.YardColonyId==colony&&p.TransportShipId==null))return true;
            return first.YardColonyId==colony&&!game.Ships!.Any(s=>s.OwnerCivilizationId==actor&&s.CurrentHull>0&&TransportCapability(game,s)?.Passengers>=colonisation!.Policy.Settlers)
                &&colonisation!.State.CampSources!.Any(c=>c.OwnerId==actor&&RepeatNeedsSupply(game,c));
        }
    }
}
