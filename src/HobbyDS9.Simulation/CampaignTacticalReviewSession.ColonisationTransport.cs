using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private ColonisationSupportedShip? TransportCapability(GameStateDto game,ShipStateDto ship)
        {
            if(ship.CurrentHull<=0||ship.Blueprint==null)return null;
            var source=CampaignBlueprints.Find(game,ship.Blueprint);
            if(source==null||source.OwnerCivilizationId!=ship.OwnerCivilizationId)return null;
            return PublishedTacticalSupportCompiler.EvaluateColonisation(new ShipDesignCatalogueSnapshot(source.FrozenCatalogue!),CampaignBlueprints.Restore(source),
                CampaignBlueprints.FrozenAccess(source.FrozenCatalogue!),colonisation!.Support).Ship;
        }
        private bool EmptyTransport(GameStateDto game,ShipStateDto ship,ColonyStateDto colony,out ColonisationSupportedShip? capacity)
        {
            capacity=TransportCapability(game,ship);
            return capacity!=null&&capacity.Passengers>=colonisation!.Policy.Settlers&&ship.OwnerCivilizationId==colony.OwnerCivilizationId&&ShipAt(game,ship,colony.SystemId)
                &&!colonisation.State.ManifestRecords!.Any(m=>m.ShipId==ship.ShipId&&m.Stage==SettlerManifestStage.Loaded)
                &&!colonisation.State.Surveys!.Any(s=>s.ShipId==ship.ShipId&&s.Stage==PlanetSurveyStage.Active)
                &&!activities!.State.Findings!.Any(f=>f.ShipId==ship.ShipId&&(f.Stage==ActivityProgressStage.Active||f.Stage==ActivityProgressStage.AwaitingClaim));
        }
        private static bool Materials(List<ResourceAmountDto>? materials,int capacity)=>materials!=null&&materials.Count<=1&&materials.All(r=>r!=null&&r.ResourceDefinitionId=="resource.dilithium"&&r.Amount>0&&r.Amount<=capacity);
        private CampaignTacticalError EmbarkSettlers(GameStateDto game,string actor,EmbarkSettlersDto order)
        {
            var colony=game.Colonies!.SingleOrDefault(c=>c.ColonyId==order.ColonyId&&c.OwnerCivilizationId==actor);
            var ship=game.Ships!.SingleOrDefault(s=>s.ShipId==order.ShipId&&s.OwnerCivilizationId==actor);
            if(colony==null||ship==null||!EstablishedColony(colony.ColonyId)||!EmptyTransport(game,ship,colony,out var capability))return CampaignTacticalError.InvalidOrder;
            var policy=colonisation!.Policy;var source=colony.Cohorts!.SingleOrDefault(c=>c.CohortId==order.CohortId);
            if(source==null||order.Population!=policy.Settlers||source.Population-order.Population<(game.Displacements!.SingleOrDefault(d=>d.CohortId==source.CohortId)?.Population??0)
                ||(BigInteger)order.Population*10000>(BigInteger)source.Population*policy.MaximumSourceCohortBps||PopulationRules.Population(colony)-order.Population<policy.SourcePopulationFloor
                ||order.Food<policy.MinimumFood||order.Food>policy.MaximumFood||order.Food>capability!.Food||colony.Food<order.Food
                ||!Materials(order.Materials,capability.Materials)||PopulationRules.Stock(colony.Stockpiles!,policy.EquipmentResourceId)<1
                ||order.Materials!.Any(r=>PopulationRules.Stock(colony.Stockpiles!,r.ResourceDefinitionId)<r.Amount))return CampaignTacticalError.InvalidOrder;
            var id=CampaignAllocation.Allocate(game,"settler-manifest",null);var cohort=PopulationData.Copy(source);
            cohort.CohortId=CampaignAllocation.Allocate(game,"cohort",cohort.SpeciesDefinitionId);cohort.Population=order.Population;
            source.Population-=order.Population;colony.Food-=order.Food;
            var cargo=order.Materials!.Select(PopulationData.Copy).Append(new ResourceAmountDto{ResourceDefinitionId=policy.EquipmentResourceId,Amount=1}).OrderBy(r=>r.ResourceDefinitionId,StringComparer.Ordinal).ToList();
            foreach(var r in cargo)PopulationRules.AddStock(colony.Stockpiles!,r.ResourceDefinitionId,-r.Amount);
            colonisation.State.ManifestRecords!.Add(new SettlerManifestDto{ManifestId=id,OwnerId=actor,ShipId=ship.ShipId,Kind=SettlerManifestKind.Settlement,Stage=SettlerManifestStage.Loaded,
                Blueprint=BlueprintCopies.Copy(ship.Blueprint!),FitId=capability!.Tactical.FitId,SupportHash=capability.Tactical.SupportHash,Passengers=cohort,Food=order.Food,Cargo=cargo,LastSurvivalTurn=game.TurnNumber-1,LatestReport="Settlers and physical supplies embarked."});
            colonisation.State.ManifestOrigins!.Add(new ManifestOriginLineageDto{ManifestId=id,SourceColonyId=colony.ColonyId,SourceCohortId=source.CohortId,EmbarkedCohortId=cohort.CohortId,
                Kind=ManifestOriginKind.Passenger,EmbarkedPopulation=cohort.Population,LoadedFood=order.Food,LoadedMaterials=cargo.Select(PopulationData.Copy).ToList(),Turn=game.TurnNumber});
            return CampaignTacticalError.None;
        }
        private CampaignTacticalError LoadCarriedSupplies(GameStateDto game,string actor,LoadCarriedSuppliesDto order)
        {
            var colony=game.Colonies!.SingleOrDefault(c=>c.ColonyId==order.SourceColonyId&&c.OwnerCivilizationId==actor);
            var ship=game.Ships!.SingleOrDefault(s=>s.ShipId==order.ShipId&&s.OwnerCivilizationId==actor);
            if(colony==null||ship==null||!EstablishedColony(colony.ColonyId)||PopulationRules.Population(colony)==0||!EmptyTransport(game,ship,colony,out var capability)
                ||order.Food<0||order.Food>capability!.Food||order.Food>colony.Food||!Materials(order.Materials,capability.Materials)
                ||order.Food==0&&order.Materials!.Count==0||order.Materials!.Any(r=>PopulationRules.Stock(colony.Stockpiles!,r.ResourceDefinitionId)<r.Amount))return CampaignTacticalError.InvalidOrder;
            var id=CampaignAllocation.Allocate(game,"settler-manifest",null);colony.Food-=order.Food;
            var cargo=order.Materials!.Select(PopulationData.Copy).ToList();foreach(var r in cargo)PopulationRules.AddStock(colony.Stockpiles!,r.ResourceDefinitionId,-r.Amount);
            colonisation!.State.ManifestRecords!.Add(new SettlerManifestDto{ManifestId=id,OwnerId=actor,ShipId=ship.ShipId,Kind=SettlerManifestKind.SupplyOnly,Stage=SettlerManifestStage.Loaded,
                Blueprint=BlueprintCopies.Copy(ship.Blueprint!),FitId=capability!.Tactical.FitId,SupportHash=capability.Tactical.SupportHash,Food=order.Food,Cargo=cargo,LastSurvivalTurn=game.TurnNumber-1,LatestReport="Physical supplies loaded; no passengers."});
            colonisation.State.ManifestOrigins!.Add(new ManifestOriginLineageDto{ManifestId=id,SourceColonyId=colony.ColonyId,Kind=ManifestOriginKind.Supply,
                LoadedFood=order.Food,LoadedMaterials=cargo.Select(PopulationData.Copy).ToList(),Turn=game.TurnNumber});return CampaignTacticalError.None;
        }
        private static void AddVector(List<ResourceAmountDto> target,string resource,long amount)
        {
            if(amount==0)return;var row=target.SingleOrDefault(r=>r.ResourceDefinitionId==resource);
            if(row==null){target.Add(new ResourceAmountDto{ResourceDefinitionId=resource,Amount=amount});target.Sort((a,b)=>StringComparer.Ordinal.Compare(a.ResourceDefinitionId,b.ResourceDefinitionId));}
            else row.Amount=checked(row.Amount+amount);
        }
        private void MoveCarriedSupply(GameStateDto game,SettlerManifestDto m,ColonyStateDto colony,long food,IReadOnlyList<ResourceAmountDto> materials)
        {
            colony.Food=checked(colony.Food+food);m.Food-=food;m.FoodDelivered=checked(m.FoodDelivered+food);
            game.PopulationStatistics!.FoodDelivered=checked(game.PopulationStatistics.FoodDelivered+food);
            foreach(var r in materials)
            {
                m.Cargo!.Single(x=>x.ResourceDefinitionId==r.ResourceDefinitionId).Amount-=r.Amount;
                PopulationRules.AddStock(colony.Stockpiles!,r.ResourceDefinitionId,r.Amount);AddVector(m.MaterialsDelivered!,r.ResourceDefinitionId,r.Amount);
                game.PopulationStatistics.ResourceDelivered=checked(game.PopulationStatistics.ResourceDelivered+r.Amount);
            }
        }
        private void FinishManifest(GameStateDto game,CampaignColonisationStateDto a,SettlerManifestDto manifest,SettlerManifestStage stage,string? planet)
        {
            Require(manifest.Stage==SettlerManifestStage.Loaded&&!a.ManifestResolutions!.Any(r=>r.ManifestId==manifest.ManifestId),"Manifest already resolved.");
            manifest.Stage=stage;game.EntityIdentities.RemoveAll(e=>e.EntityId==manifest.ManifestId);
            a.ManifestResolutions!.Add(new ManifestResolutionDto{ManifestId=manifest.ManifestId,Stage=stage,Turn=game.TurnNumber,DestinationPlanetId=planet,
                PopulationTransferred=manifest.PopulationTransferred,PopulationLost=manifest.PassengerDeaths,FoodTransferred=manifest.FoodDelivered,FoodLost=manifest.FoodLost,
                CargoTransferred=manifest.MaterialsDelivered!.Select(PopulationData.Copy).ToList(),CargoLost=manifest.MaterialsLost!.Select(PopulationData.Copy).ToList()});
        }
        private CampaignTacticalError DeliverSupplies(GameStateDto game,string actor,DeliverCarriedSuppliesDto order)
        {
            var a=colonisation!.State;var m=a.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==order.ManifestId&&m.OwnerId==actor&&m.Stage==SettlerManifestStage.Loaded);
            var colony=game.Colonies!.SingleOrDefault(c=>c.ColonyId==order.ColonyId&&c.OwnerCivilizationId==actor);
            var ship=m==null?null:game.Ships!.SingleOrDefault(s=>s.ShipId==m.ShipId&&s.OwnerCivilizationId==actor);
            if(m==null||colony==null||ship==null||PopulationRules.Population(colony)==0||!ShipAt(game,ship,colony.SystemId)||TransportCapability(game,ship)==null
                ||CurrentCamp(a,colony.ColonyId)?.Stage==ColonyEstablishmentStage.EmptyOutpost
                ||order.Food<0||order.Food>m.Food||!Materials(order.Materials,10)||order.Food==0&&order.Materials!.Count==0
                ||order.Materials!.Any(r=>PopulationRules.Stock(m.Cargo!,r.ResourceDefinitionId)<r.Amount))return CampaignTacticalError.InvalidOrder;
            MoveCarriedSupply(game,m,colony,order.Food,order.Materials!);m.LatestReport="Physical supplies delivered.";
            if(m.Kind==SettlerManifestKind.SupplyOnly&&m.Food==0&&m.Cargo!.All(r=>r.Amount==0))FinishManifest(game,a,m,SettlerManifestStage.Delivered,colony.PlanetId);
            return CampaignTacticalError.None;
        }
        private CampaignTacticalError UnloadTransport(GameStateDto game,string actor,UnloadSettlersDto order)
        {
            var a=colonisation!.State;var m=a.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==order.ManifestId&&m.OwnerId==actor&&m.Stage==SettlerManifestStage.Loaded);
            var colony=game.Colonies!.SingleOrDefault(c=>c.ColonyId==order.ColonyId&&c.OwnerCivilizationId==actor);
            var ship=m==null?null:game.Ships!.SingleOrDefault(s=>s.ShipId==m.ShipId&&s.OwnerCivilizationId==actor);
            if(m==null||colony==null||ship==null||!EstablishedColony(colony.ColonyId)||PopulationRules.Population(colony)==0||!ShipAt(game,ship,colony.SystemId)
                ||TransportCapability(game,ship)==null||a.DeploymentIntents!.Any(i=>i.ManifestId==m.ManifestId))return CampaignTacticalError.InvalidOrder;
            if(m.Kind!=SettlerManifestKind.SupplyOnly)
            {
                var passengers=m.Passengers;if(passengers==null||passengers.Population<=0||!PopulationRules.CanReceive(colony,passengers,passengers.LegalStatus))return CampaignTacticalError.InvalidOrder;
                var candidate=PopulationData.Copy(colony);candidate.Cohorts!.Add(PopulationData.Copy(passengers));var projected=ProjectOwnedWorld(candidate,game);
                var species=definitions.Species!.Single(s=>s.DefinitionId==passengers.SpeciesDefinitionId);var environment=WorldEconomy.Planet(colony,game,definitions);
                if(projected.Crowding>0||!environment.Habitats!.Any(h=>h.NaturalCapacity>0&&WorldEconomy.Suitability(species,environment,h.Kind)>0)
                    &&WorldEconomy.Installed(colony,definitions,i=>i.ArtificialCapacity)==0)return CampaignTacticalError.InvalidOrder;
                colony.Cohorts!.Add(PopulationData.Copy(passengers));CohortCompaction.Execute(colony,game);
                if(colony.Cohorts.Count>PopulationRules.CohortCeiling)return CampaignTacticalError.InvalidOrder;
                a.LandedCohortLineages!.Add(new LandedCohortLineageDto{ManifestId=m.ManifestId,ColonyId=colony.ColonyId,CohortId=passengers.CohortId,Population=passengers.Population,Turn=game.TurnNumber});
                m.PopulationTransferred=checked(m.PopulationTransferred+passengers.Population);m.Passengers=null;
            }
            var remaining=m.Cargo!.Where(r=>r.Amount>0).Select(PopulationData.Copy).ToList();MoveCarriedSupply(game,m,colony,m.Food,remaining);
            m.LatestReport=m.Kind==SettlerManifestKind.SupplyOnly?"Physical supplies unloaded.":"Passengers and remaining supplies unloaded.";
            FinishManifest(game,a,m,SettlerManifestStage.Unloaded,colony.PlanetId);return CampaignTacticalError.None;
        }
    }
}
