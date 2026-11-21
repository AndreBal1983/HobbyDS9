using System;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private static SettlementCampDto? CurrentCamp(CampaignColonisationStateDto a,string colony)=>a.CampSources!.Where(c=>c.ColonyId==colony).OrderByDescending(c=>c.DeployedTurn).FirstOrDefault();
        private bool MayDeploy(GameStateDto game,CampaignColonisationStateDto a,SettlerManifestDto m,string planetId)
        {
            var planet=game.Planets!.SingleOrDefault(p=>p.PlanetId==planetId);var ship=game.Ships!.SingleOrDefault(s=>s.ShipId==m.ShipId&&s.OwnerCivilizationId==m.OwnerId);
            if(planet==null||ship==null||m.Stage!=SettlerManifestStage.Loaded||m.Kind!=SettlerManifestKind.Settlement||m.Passengers==null||m.Passengers.Population<=0||m.Food<=0
                ||!ShipAt(game,ship,planet.SystemId)||TransportCapability(game,ship)==null||PopulationRules.Stock(m.Cargo!,colonisation!.Policy.EquipmentResourceId)!=1)return false;
            var node=physicalNavigation!.NodeForSystem(planet.SystemId);
            if(game.Fleets!.Any(f=>f.LocationNodeId==node&&f.TransitTotalCost==0&&f.ShipIds!.Count>0&&CampaignPhysicalMovement.Hostile(declaredHostilities!,m.OwnerId,f.OwnerCivilizationId)))return false;
            var controller=a.PlanetControls!.Single(c=>c.PlanetId==planetId);var existing=game.Colonies!.SingleOrDefault(c=>c.PlanetId==planetId);
            if(controller.Kind!=PlanetControllerKind.Unclaimed&&!(controller.Kind==PlanetControllerKind.Empire&&controller.ActorId==m.OwnerId&&existing!=null
                &&CurrentCamp(a,existing.ColonyId)?.Stage==ColonyEstablishmentStage.EmptyOutpost&&PopulationRules.Population(existing)==0))return false;
            if(!a.Surveys!.Any(s=>s.ActorId==m.OwnerId&&s.PlanetId==planetId&&s.Stage==PlanetSurveyStage.Completed)
                &&!(existing!=null&&existing.OwnerCivilizationId==m.OwnerId))return false;
            var environment=definitions.Planets!.Single(d=>d.DefinitionId==planet.DefinitionId);var species=definitions.Species!.Single(s=>s.DefinitionId==m.Passengers.SpeciesDefinitionId);
            return environment.Habitats!.Any(h=>h.NaturalCapacity>=m.Passengers.Population&&WorldEconomy.Suitability(species,environment,h.Kind)>0);
        }
        private CampaignTacticalError RequestDeployment(GameStateDto game,string actor,DeploySettlersDto command)
        {
            var a=colonisation!.State;var m=a.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==command.ManifestId&&m.OwnerId==actor);
            if(m==null||!MayDeploy(game,a,m,command.PlanetId)||a.DeploymentIntents!.Any(i=>i.ManifestId==m.ManifestId||i.ActorId==actor&&i.PlanetId==command.PlanetId))return CampaignTacticalError.InvalidOrder;
            a.DeploymentIntents!.Add(new SettlementDeploymentIntentDto{IntentId=CampaignAllocation.Allocate(game,"settlement-intent",null),ActorId=actor,ManifestId=m.ManifestId,PlanetId=command.PlanetId});
            return CampaignTacticalError.None;
        }
        private CampaignTacticalError WithdrawDeployment(GameStateDto game,string actor,WithdrawDeploymentDto command)
        {
            var a=colonisation!.State;var intent=a.DeploymentIntents!.SingleOrDefault(i=>i.IntentId==command.IntentId&&i.ActorId==actor);
            if(intent==null)return CampaignTacticalError.InvalidOrder;a.DeploymentIntents!.Remove(intent);game.EntityIdentities.RemoveAll(e=>e.EntityId==intent.IntentId);return CampaignTacticalError.None;
        }
        private CampaignTacticalError EvacuateFounding(GameStateDto game,string actor,EvacuateFoundingColonyDto command)
        {
            var a=colonisation!.State;var camp=a.CampSources!.SingleOrDefault(c=>c.CampId==command.CampId&&c.OwnerId==actor&&c.Stage==ColonyEstablishmentStage.Founding);
            var colony=camp==null?null:game.Colonies!.SingleOrDefault(c=>c.ColonyId==camp.ColonyId&&c.OwnerCivilizationId==actor);
            var ship=game.Ships!.SingleOrDefault(s=>s.ShipId==command.ShipId&&s.OwnerCivilizationId==actor);
            if(camp==null||colony==null||ship==null||CurrentCamp(a,colony.ColonyId)!=camp||colony.Cohorts!.Count!=1||PopulationRules.Population(colony)<=0
                ||PopulationRules.Population(colony)>colonisation.Policy.Settlers||!EmptyTransport(game,ship,colony,out var support))return CampaignTacticalError.InvalidOrder;
            var people=PopulationData.Copy(colony.Cohorts[0]);var food=Math.Min(colony.Food,support!.Food);var amount=Math.Min(PopulationRules.Stock(colony.Stockpiles!,"resource.dilithium"),support.Materials);
            var cargo=amount==0?new System.Collections.Generic.List<ResourceAmountDto>():new System.Collections.Generic.List<ResourceAmountDto>{new ResourceAmountDto{ResourceDefinitionId="resource.dilithium",Amount=amount}};
            var id=CampaignAllocation.Allocate(game,"settler-manifest",null);colony.Cohorts.Clear();colony.Food-=food;
            foreach(var r in cargo)PopulationRules.AddStock(colony.Stockpiles!,r.ResourceDefinitionId,-r.Amount);
            a.ManifestRecords!.Add(new SettlerManifestDto{ManifestId=id,OwnerId=actor,ShipId=ship.ShipId,Kind=SettlerManifestKind.ReturnOnly,Stage=SettlerManifestStage.Loaded,
                Blueprint=BlueprintCopies.Copy(ship.Blueprint!),FitId=support.Tactical.FitId,SupportHash=support.Tactical.SupportHash,Passengers=people,Food=food,Cargo=cargo,
                LastSurvivalTurn=game.TurnNumber-1,LatestReport="Founding settlers evacuated; this group must return to an established colony."});
            a.ManifestOrigins!.Add(new ManifestOriginLineageDto{ManifestId=id,SourceColonyId=colony.ColonyId,SourceCohortId=people.CohortId,EmbarkedCohortId=people.CohortId,
                Kind=ManifestOriginKind.Passenger,EmbarkedPopulation=people.Population,LoadedFood=food,LoadedMaterials=cargo.Select(PopulationData.Copy).ToList(),Turn=game.TurnNumber});
            camp.Stage=ColonyEstablishmentStage.EmptyOutpost;camp.LatestReport="Founding camp evacuated; remaining local stock and infrastructure retained.";
            a.CampTerminals!.Add(new SettlementCampTerminalDto{CampId=camp.CampId,Kind=SettlementTerminalKind.Evacuated,Turn=game.TurnNumber,ReturnManifestId=id});
            game.EntityIdentities.RemoveAll(e=>e.EntityId==camp.CampId);return CampaignTacticalError.None;
        }
    }
}
