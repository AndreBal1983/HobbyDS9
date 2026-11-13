using System;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private bool GrowthSupplyNeeded(GameStateDto game,ColonisationAiPlanDto p)
        {
            var camp=colonisation!.State.CampSources!.SingleOrDefault(c=>c.CampId==p.CampId&&c.OwnerId==p.ActorId&&c.Stage==ColonyEstablishmentStage.Established);
            var world=camp==null?null:game.Colonies!.SingleOrDefault(c=>c.ColonyId==camp.ColonyId&&c.OwnerCivilizationId==p.ActorId);
            if(world==null||PopulationRules.Population(world)<=0)return false;
            var output=ProjectOwnedWorld(world,game);var deficit=System.Numerics.BigInteger.Max(0,(System.Numerics.BigInteger)output.FoodDemand-output.FoodOutput);
            return deficit>0&&(System.Numerics.BigInteger)world.Food<deficit*((System.Numerics.BigInteger)colonisation.Policy.MaximumFood+expansion!.Source.Policy.ContingencyWeeks);
        }
        private bool GrowthReceiver(ColonyStateDto world,PopulationCohortDto people,GameStateDto game)
        {
            if(!EstablishedColony(world.ColonyId)||PopulationRules.Population(world)<=0||!PopulationRules.CanReceive(world,people,people.LegalStatus))return false;
            var candidate=PopulationData.Copy(world);candidate.Cohorts!.Add(PopulationData.Copy(people));
            var projection=ProjectOwnedWorld(candidate,game);
            var environment=WorldEconomy.Planet(world,game,definitions);var species=definitions.Species!.Single(s=>s.DefinitionId==people.SpeciesDefinitionId);
            return projection.Crowding==0&&projection.FoodOutput>=projection.FoodDemand
                &&(environment.Habitats!.Any(h=>h.NaturalCapacity>0&&WorldEconomy.Suitability(species,environment,h.Kind)>0)
                    ||WorldEconomy.Installed(world,definitions,i=>i.ArtificialCapacity)>0);
        }
        private bool PlanGrowthShipment(string actor,ColonisationAiPlanDto p,ColonyStateDto home,ColonyStateDto world,PopulationCohortDto? cohort,ShipStateDto transport,ExpansionSearch searches)
        {
            var game=state.Campaign.GameState;var policy=colonisation!.Policy;
            if(colonisation.State.ManifestRecords!.Any(m=>m.OwnerId==actor&&m.Kind==SettlerManifestKind.ImmigrationOnly))
            {p.Stage=ColonisationAiStage.GrowthClosed;return false;}
            p.Stage=ColonisationAiStage.Immigrating;
            if(cohort==null||PopulationRules.Population(home)-policy.Settlers<policy.SourcePopulationFloor)
            {p.HoldReason="Immigration waits for a sustainable real source cohort.";return false;}
            var people=PopulationData.Copy(cohort);people.Population=policy.Settlers;
            if(!GrowthReceiver(world,people,game)){p.HoldReason="Immigration waits for actual receiving habitat and sustainable food output.";return false;}
            if(!ShipAt(game,transport,home.SystemId))return ExpansionMove(actor,transport,physicalNavigation!.NodeForSystem(home.SystemId)!,searches);
            if(ExpansionFoodAvailable(home,game)<policy.MaximumFood){p.HoldReason="Immigration retains the source food reserve.";return false;}
            var route=ExpansionRoute(game,actor,physicalNavigation!.NodeForSystem(home.SystemId)!,physicalNavigation.NodeForSystem(world.SystemId)!,searches);
            if(route==null){p.HoldReason="Immigration has no lawful known route.";return false;}
            var fleet=game.Fleets!.Single(f=>f.FleetId==transport.FleetId);var turns=physicalNavigation.Travel(game,fleet,definitions,route).EstimatedTurns;
            if((BigInteger)turns+1+expansion!.Source.Policy.ContingencyWeeks>policy.MaximumFood){p.HoldReason="Immigration voyage exceeds carried-food contingency.";return false;}
            return ExpansionColonisation(actor,ColonisationAiAction.PassengerLoad,new(){Action=ColonisationAction.LoadPassengers,Embark=new(){ShipId=transport.ShipId,ColonyId=home.ColonyId,CohortId=cohort.CohortId,Population=policy.Settlers,Food=policy.MaximumFood}});
        }
        private bool PlanGrowthPassengers(string actor,ColonisationAiPlanDto p,ColonyStateDto home,ShipStateDto transport,SettlerManifestDto manifest,SettlementCampDto? camp,ExpansionSearch searches)
        {
            var game=state.Campaign.GameState;var world=camp==null?null:game.Colonies!.SingleOrDefault(c=>c.ColonyId==camp.ColonyId&&c.OwnerCivilizationId==actor);
            var returning=p.Stage==ColonisationAiStage.Recovering||world==null||camp!.Stage!=ColonyEstablishmentStage.Established
                ||manifest.Food<=expansion!.Source.Policy.ContingencyWeeks||!GrowthReceiver(world,manifest.Passengers!,game);
            p.Stage=returning?ColonisationAiStage.Recovering:ColonisationAiStage.Immigrating;
            var receiver=returning?home:world!;
            if(ShipAt(game,transport,receiver.SystemId))
            {
                var accepted=ExpansionColonisation(actor,ColonisationAiAction.Unload,new(){Action=ColonisationAction.UnloadTransport,Unload=new(){ManifestId=manifest.ManifestId,ColonyId=receiver.ColonyId}});
                if(accepted){p=ExpansionPlan(actor);p.Stage=ColonisationAiStage.GrowthClosed;p.HoldReason=returning?"Immigration returned; one attempt closed without a frontier arrival.":"Immigration arrived; one planned trip completed.";}
                return accepted;
            }
            var moved=ExpansionMove(actor,transport,physicalNavigation!.NodeForSystem(receiver.SystemId)!,searches);
            if(!moved)p.HoldReason="Immigration waits on its actual route; passengers remain aboard.";
            return moved;
        }
    }
}
