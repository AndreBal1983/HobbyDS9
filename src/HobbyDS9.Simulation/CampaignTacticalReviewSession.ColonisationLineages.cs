using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private void ValidateColonisationLineages(GameStateDto game)
        {
            var a=colonisation!.State;var policy=colonisation.Policy;
            bool Allocated(string id)=>EntityId.TryParse(id,out var entity)&&entity.TryGetAllocationSequence(out var n)&&n<game.NextEntitySequence;
            bool Live(string id)=>game.EntityIdentities!.Any(e=>e.EntityId==id);
            void Unique(IEnumerable<string> ids){var rows=ids.ToList();Require(rows.Distinct(StringComparer.Ordinal).Count()==rows.Count,"Duplicate colonisation retained identity.");}
            bool Vector(List<ResourceAmountDto>? rows)=>rows!=null&&rows.All(r=>r!=null&&r.Amount>=0&&definitions.Resources!.Any(d=>d.DefinitionId==r.ResourceDefinitionId))
                &&rows.Select(r=>r.ResourceDefinitionId).Distinct(StringComparer.Ordinal).Count()==rows.Count
                &&rows.Select(r=>r.ResourceDefinitionId).SequenceEqual(rows.Select(r=>r.ResourceDefinitionId).OrderBy(x=>x,StringComparer.Ordinal));
            Require(a.ManifestRecords!=null&&a.ManifestOrigins!=null&&a.ManifestResolutions!=null&&a.ColonyOrigins!=null&&a.LandedCohortLineages!=null&&a.CampSources!=null&&a.CampTerminals!=null&&a.DeploymentIntents!=null,"Complete colonisation lineage collections required.");
            Unique(a.ManifestRecords!.Select(m=>m.ManifestId));Unique(a.ManifestOrigins!.Select(m=>m.ManifestId));Unique(a.ManifestResolutions!.Select(m=>m.ManifestId));
            Require(a.ManifestOrigins.Count==a.ManifestRecords.Count,"Every manifest has exactly one physical origin.");
            foreach(var m in a.ManifestRecords)
            {
                Require(m!=null&&Allocated(m.ManifestId)&&controllers.ContainsKey(m.OwnerId)&&Enum.IsDefined(typeof(SettlerManifestKind),m.Kind)&&Enum.IsDefined(typeof(SettlerManifestStage),m.Stage)
                    &&(GrowthEnabled||m.Kind!=SettlerManifestKind.ImmigrationOnly)&&m.Blueprint!=null&&Vector(m.Cargo)&&Vector(m.MaterialsDelivered)&&Vector(m.MaterialsLost)&&m.Food>=0&&m.FoodConsumed>=0&&m.FoodDelivered>=0&&m.FoodLost>=0
                    &&m.PassengerDeaths>=0&&m.PopulationTransferred>=0&&m.EquipmentConsumed>=0&&m.EquipmentConsumed<=1&&m.FoodShortageWeeks>=0&&m.LastSurvivalTurn<game.TurnNumber,"Invalid retained transport facts.");
                var origin=a.ManifestOrigins.SingleOrDefault(o=>o.ManifestId==m.ManifestId);
                Require(origin!=null&&game.Colonies!.Any(c=>c.ColonyId==origin.SourceColonyId&&c.OwnerCivilizationId==m.OwnerId)&&Vector(origin.LoadedMaterials)
                    &&origin.LoadedFood>=0&&origin.LoadedFood<=policy.MaximumFood&&origin.Turn>=initialTurn&&origin.Turn<=game.TurnNumber,"Invalid physical manifest origin.");
                var source=CampaignBlueprints.Find(game,m.Blueprint!);
                Require(source!=null&&source.OwnerCivilizationId==m.OwnerId,"Missing frozen transport publication.");
                var supported=PublishedTacticalSupportCompiler.EvaluateColonisation(new ShipDesignCatalogueSnapshot(source!.FrozenCatalogue!),CampaignBlueprints.Restore(source),CampaignBlueprints.FrozenAccess(source.FrozenCatalogue!),colonisation.Support).Ship;
                Require(supported!=null&&supported.Passengers>=policy.Settlers&&supported.Tactical.FitId==m.FitId&&supported.Tactical.SupportHash==m.SupportHash,"Different frozen transport capability.");
                if(m.Kind==SettlerManifestKind.SupplyOnly)
                    Require(origin!.Kind==ManifestOriginKind.Supply&&origin.SourceCohortId==null&&origin.EmbarkedCohortId==null&&origin.EmbarkedPopulation==0&&m.Passengers==null
                        &&m.PassengerDeaths==0&&m.PopulationTransferred==0&&m.EquipmentConsumed==0&&origin.LoadedMaterials!.All(r=>r.ResourceDefinitionId=="resource.dilithium"&&r.Amount<=supported!.Materials),"Supply manifest must not invent people or equipment.");
                else
                    Require(origin!.Kind==ManifestOriginKind.Passenger&&EntityId.TryParse(origin.SourceCohortId,out _)&&Allocated(origin.EmbarkedCohortId!)&&origin.EmbarkedPopulation>0&&origin.EmbarkedPopulation<=policy.Settlers
                        &&(BigInteger)(m.Passengers?.Population??0)+m.PopulationTransferred+m.PassengerDeaths==origin.EmbarkedPopulation
                        &&(m.Stage==SettlerManifestStage.Loaded?m.Passengers!=null&&m.Passengers.Population>0&&m.Passengers.CohortId==origin.EmbarkedCohortId:m.Passengers==null),"Passenger lineage is not conserved.");
                if(m.Kind==SettlerManifestKind.ImmigrationOnly)
                    Require(m.EquipmentConsumed==0&&PopulationRules.Stock(origin!.LoadedMaterials!,policy.EquipmentResourceId)==0
                        &&m.Stage!=SettlerManifestStage.Deployed&&m.Stage!=SettlerManifestStage.Delivered
                        &&origin.EmbarkedPopulation==policy.Settlers,"Kit-free passenger provenance required.");
                Require((BigInteger)m.Food+m.FoodConsumed+m.FoodDelivered+m.FoodLost==origin!.LoadedFood,"Manifest food is not conserved.");
                foreach(var id in origin.LoadedMaterials!.Select(r=>r.ResourceDefinitionId).Concat(m.Cargo!.Select(r=>r.ResourceDefinitionId)).Concat(m.MaterialsDelivered!.Select(r=>r.ResourceDefinitionId)).Concat(m.MaterialsLost!.Select(r=>r.ResourceDefinitionId)).Distinct(StringComparer.Ordinal))
                    Require((BigInteger)PopulationRules.Stock(origin.LoadedMaterials!,id)==(BigInteger)PopulationRules.Stock(m.Cargo!,id)+PopulationRules.Stock(m.MaterialsDelivered!,id)+PopulationRules.Stock(m.MaterialsLost!,id)+(id==policy.EquipmentResourceId?m.EquipmentConsumed:0),"Manifest material is not conserved.");
                var resolution=a.ManifestResolutions.SingleOrDefault(r=>r.ManifestId==m.ManifestId);
                if(m.Stage==SettlerManifestStage.Loaded)Require(resolution==null&&Live(m.ManifestId)&&game.Ships!.Any(s=>s.ShipId==m.ShipId&&s.OwnerCivilizationId==m.OwnerId),"Loaded manifest lost its actual ship or identity.");
                else Require(resolution!=null&&!Live(m.ManifestId)&&resolution.Stage==m.Stage&&resolution.Turn>=origin.Turn&&resolution.Turn<=game.TurnNumber
                    &&resolution.PopulationTransferred==m.PopulationTransferred&&resolution.PopulationLost==m.PassengerDeaths&&resolution.FoodTransferred==m.FoodDelivered&&resolution.FoodLost==m.FoodLost
                    &&FrontierSame(resolution.CargoTransferred,m.MaterialsDelivered)&&FrontierSame(resolution.CargoLost,m.MaterialsLost),"Terminal manifest differs from its retained resolution.");
            }
            Require(a.ManifestResolutions.All(r=>a.ManifestRecords.Any(m=>m.ManifestId==r.ManifestId)),"Unknown manifest resolution.");
            Unique(a.ColonyOrigins!.Select(o=>o.ColonyId));Unique(a.CampSources!.Select(c=>c.CampId));Unique(a.CampTerminals!.Select(c=>c.CampId));Unique(a.DeploymentIntents!.Select(i=>i.IntentId));
            foreach(var camp in a.CampSources)
            {
                Require(camp!=null&&Allocated(camp.CampId)&&Enum.IsDefined(typeof(ColonyEstablishmentStage),camp.Stage)&&camp.DeployedTurn>=initialTurn&&camp.DeployedTurn<=game.TurnNumber
                    &&camp.CompletedIndustry>=0&&camp.CompletedIndustry<=policy.EstablishmentIndustry&&camp.FedWeeks>=0&&camp.Deaths>=0&&camp.Deaths<=policy.Settlers&&camp.FoodConsumed>=0
                    &&game.Colonies!.Any(c=>c.ColonyId==camp.ColonyId&&c.OwnerCivilizationId==camp.OwnerId)
                    &&a.LandedCohortLineages!.Count(l=>l.ColonyId==camp.ColonyId&&l.ManifestId==camp.ManifestId)==1
                    &&a.ManifestRecords.Any(m=>m.ManifestId==camp.ManifestId&&m.Stage==SettlerManifestStage.Deployed&&m.OwnerId==camp.OwnerId),"Invalid retained founding source.");
                var terminal=a.CampTerminals.SingleOrDefault(t=>t.CampId==camp.CampId);
                Require(camp.Stage==ColonyEstablishmentStage.Founding?terminal==null&&Live(camp.CampId):terminal!=null&&!Live(camp.CampId),"Camp terminal liability differs from its actual stage.");
            }
            foreach(var intent in a.DeploymentIntents)
                Require(intent!=null&&Allocated(intent.IntentId)&&Live(intent.IntentId)&&a.ManifestRecords.Any(m=>m.ManifestId==intent.ManifestId&&m.OwnerId==intent.ActorId&&m.Kind==SettlerManifestKind.Settlement&&m.Stage==SettlerManifestStage.Loaded)
                    &&a.Bodies!.Any(b=>b.PlanetId==intent.PlanetId),"Invalid live deployment intent.");
            Require(a.DeploymentIntents.Select(i=>i.ManifestId).Distinct(StringComparer.Ordinal).Count()==a.DeploymentIntents.Count
                &&a.DeploymentIntents.Select(i=>(i.ActorId,i.PlanetId)).Distinct().Count()==a.DeploymentIntents.Count,"Duplicate deployment intent.");
        }
    }
}
