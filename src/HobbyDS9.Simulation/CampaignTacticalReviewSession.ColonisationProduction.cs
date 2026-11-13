using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private HobbyDS9.Contracts.ShipDesign.ShipDesignCostDto? ColonisationStructureCost(string id)=>id==colonisation!.Policy.Mine!.Infrastructure!.DefinitionId
            ?colonisation.Policy.Mine.Cost:activities!.Content.Structures!.SingleOrDefault(s=>s.DefinitionId==id)?.Cost;
        private CampaignTacticalError EnqueueColonisation(GameStateDto game,CampaignActivityCommandDto command,ControllerKind controller)
        {
            var colony=game.Colonies!.SingleOrDefault(c=>c.ColonyId==command.TargetId&&c.OwnerCivilizationId==command.ActorId);
            var actor=game.Civilizations!.SingleOrDefault(c=>c.CivilizationId==command.ActorId&&c.ControllerKind==controller);
            if(colony==null||actor==null||game.Phase!=SessionPhase.Planning||!EstablishedColony(colony.ColonyId)||PopulationRules.Population(colony)==0)return CampaignTacticalError.InvalidOrder;
            long work;List<ResourceAmountDto> resources;BlueprintReferenceDto? reference=null;
            if(command.Kind==CampaignActivityCommandKind.EnqueueShip)
            {
                reference=new BlueprintReferenceDto{BlueprintId=command.DefinitionId,Revision=command.BlueprintRevision};
                var source=CampaignBlueprints.Find(game,reference);
                if(source==null||source.OwnerCivilizationId!=command.ActorId||!PublishedFit(game,reference,true).IsSupportedByCompiler
                    ||!WorldEconomy.Infrastructure(colony,definitions).Any(i=>i.definition.Shipbuilding))return CampaignTacticalError.UnsupportedSource;
                work=source.CompiledDesign!.IndustryCost;resources=StreamResources(source.CompiledDesign.ResourceCosts!);
            }
            else
            {
                var cost=ColonisationStructureCost(command.DefinitionId);
                if(cost==null||colony.Infrastructure!.Any(i=>i.DefinitionId==command.DefinitionId&&i.Count>0)
                    ||colonisation!.Production.Any(q=>q.ColonyId==colony.ColonyId&&q.Kind==ColonisationProductionKind.Infrastructure&&q.DefinitionId==command.DefinitionId))return CampaignTacticalError.InvalidOrder;
                work=cost.Industry;resources=StreamResources(cost.Resources!);
            }
            Require(colony.ProductionQueue!.Count==0,"Streamed queue is the sole production authority.");
            var id=CampaignAllocation.Allocate(game,"queue-item",null);
            colonisation!.Production.Add(new ColonisationProductionItemDto{QueueItemId=id,ColonyId=colony.ColonyId,OwnerId=command.ActorId,
                Kind=reference==null?ColonisationProductionKind.Infrastructure:ColonisationProductionKind.PublishedShip,DefinitionId=command.DefinitionId,
                Blueprint=reference,RequiredIndustry=work,RequiredResources=resources,
                ConsumedResources=resources.Select(r=>new ResourceAmountDto{ResourceDefinitionId=r.ResourceDefinitionId,Amount=0}).ToList()});
            return CampaignTacticalError.None;
        }
        private CampaignTacticalError CancelColonisationProduction(GameStateDto game,CampaignActivityCommandDto command)
        {
            var q=colonisation!.Production.SingleOrDefault(q=>q.QueueItemId==command.TargetId&&q.OwnerId==command.ActorId);
            if(q==null)return CampaignTacticalError.InvalidOrder;
            colonisation.History.Add(new ColonisationProductionReceiptDto{Order=ColonisationData.Copy(q),ResolvedTurn=game.TurnNumber});
            colonisation.Production.Remove(q);game.EntityIdentities.RemoveAll(i=>i.EntityId==q.QueueItemId);
            // No refund: consumed material is gone, unconsumed local stocks never left the colony.
            return CampaignTacticalError.None;
        }
        private IReadOnlyDictionary<string,IReadOnlyList<ResourceAmountDto>> ColonisationDemand(GameStateDto game)
        {
            var result=new Dictionary<string,IReadOnlyList<ResourceAmountDto>>(StringComparer.Ordinal);
            foreach(var group in colonisation!.Production.GroupBy(q=>q.ColonyId,StringComparer.Ordinal))
            {
                var q=group.First();var colony=game.Colonies!.Single(c=>c.ColonyId==q.ColonyId);
                if(!EstablishedColony(colony.ColonyId)||PopulationRules.Population(colony)==0)continue;
                var industry=Math.Max(1,ProjectOwnedWorld(colony,game).Industry);
                var next=q.CompletedIndustry+Math.Min(q.RequiredIndustry-q.CompletedIndustry,industry);
                result.Add(q.ColonyId,q.RequiredResources!.Select(r=>new ResourceAmountDto{ResourceDefinitionId=r.ResourceDefinitionId,
                    Amount=StreamedMaterialWork.Cumulative(r.Amount,q.RequiredIndustry,next)-StreamedMaterialWork.Cumulative(r.Amount,q.RequiredIndustry,q.CompletedIndustry)}).ToList());
            }
            return result;
        }
        private void ExecuteColonisationProduction(CampaignTacticalTravelSaveDto next)
        {
            var game=next.Core.Campaign.GameState;
            var phaseBefore=SimulationSession.CopyState(game);var historyBefore=colonisation!.History.Count;
            var cycleConsumption=new List<(string ColonyId,string ResourceDefinitionId,long Amount)>();
            var heads=colonisation!.Production.GroupBy(q=>q.ColonyId,StringComparer.Ordinal).Select(g=>g.First())
                .Where(q=>EstablishedColony(q.ColonyId)&&PopulationRules.Population(game.Colonies!.Single(c=>c.ColonyId==q.ColonyId))>0).OrderBy(q=>q.ColonyId,StringComparer.Ordinal).ToList();
            var outputs=heads.ToDictionary(q=>q.ColonyId,q=>ProjectOwnedWorld(game.Colonies!.Single(c=>c.ColonyId==q.ColonyId),game).Industry,StringComparer.Ordinal);
            foreach(var q in heads)
            {
                var colony=game.Colonies!.Single(c=>c.ColonyId==q.ColonyId);
                var plan=StreamedMaterialWork.Plan(q.RequiredResources!,colony.Stockpiles!,q.RequiredIndustry,q.CompletedIndustry,outputs[q.ColonyId]);
                foreach(var delta in plan.ConsumedDelta)
                {
                    if(delta.Amount==0)continue;
                    cycleConsumption.Add((q.ColonyId,delta.ResourceDefinitionId,delta.Amount));
                    PopulationRules.AddStock(colony.Stockpiles!,delta.ResourceDefinitionId,-delta.Amount);
                    q.ConsumedResources!.Single(r=>r.ResourceDefinitionId==delta.ResourceDefinitionId).Amount=checked(q.ConsumedResources.Single(r=>r.ResourceDefinitionId==delta.ResourceDefinitionId).Amount+delta.Amount);
                    game.PopulationStatistics!.ResourceConsumed=checked(game.PopulationStatistics.ResourceConsumed+delta.Amount);
                }
                q.CompletedIndustry=plan.CompletedWork;
                if(q.CompletedIndustry!=q.RequiredIndustry)continue;
                var before=SimulationSession.CopyState(game);ShipStateDto? ship=null;
                if(q.Kind==ColonisationProductionKind.PublishedShip)
                {
                    var source=CampaignBlueprints.Find(game,q.Blueprint!)!;
                    ship=ProductionPhase.Spawn(game,definitions,colony,"",q.Blueprint,source.CompiledDesign!);
                    ProvisionColonisation(next,before,ship,q);
                }
                else if(q.Kind==ColonisationProductionKind.Infrastructure)
                {
                    Require(!colony.Infrastructure!.Any(i=>i.DefinitionId==q.DefinitionId&&i.Count>0),"Duplicate streamed structure completion.");
                    colony.Infrastructure!.Add(new InfrastructureStateDto{DefinitionId=q.DefinitionId,Count=1});
                    activities!.State.Structures!.Add(new ActivityStructureReceiptDto{QueueItemId=q.QueueItemId,ColonyId=q.ColonyId,OwnerId=q.OwnerId,DefinitionId=q.DefinitionId,CompletedTurn=game.TurnNumber});
                }
                else
                {
                    Require(q.Kind==ColonisationProductionKind.SettlementKit&&q.DefinitionId==colonisation.Policy.EquipmentResourceId,"Actual settlement equipment recipe required.");
                    PopulationRules.AddStock(colony.Stockpiles!,q.DefinitionId,1);
                }
                game.PopulationStatistics!.ProductionCompleted=checked(game.PopulationStatistics.ProductionCompleted+1);
                game.EntityIdentities.RemoveAll(i=>i.EntityId==q.QueueItemId);
                colonisation.History.Add(new ColonisationProductionReceiptDto{Order=ColonisationData.Copy(q),Completed=true,ResolvedTurn=game.TurnNumber,ShipId=ship?.ShipId});
                colonisation.Production.Remove(q);
            }
            foreach(var fleet in game.Fleets!.Where(f=>!next.Core.FleetSettings.Any(s=>s.FleetId==f.FleetId)))
            {
                next.Core.FleetSettings.Add(new CampaignTacticalFleetSettingDto{FleetId=fleet.FleetId});
                next.Orders.Add(new CampaignTravelOrderDto{FleetId=fleet.FleetId,Intent=CampaignTravelIntent.Stay,RouteNodeIds=new List<string>()});
            }
            ValidateColonisationProduction(game);
            var guardedAfter=SimulationSession.CopyState(game);
            var completed=colonisation.History.Skip(historyBefore).ToList();
            foreach(var old in phaseBefore.Colonies!)
            {
                var current=guardedAfter.Colonies!.Single(c=>c.ColonyId==old.ColonyId);
                // Only the exact material delta of this cycle can change stocks.
                var head=heads.SingleOrDefault(q=>q.ColonyId==old.ColonyId);
                var expected=old.Stockpiles!.Select(PopulationData.Copy).ToList();
                if(head!=null)
                {
                    foreach(var delta in cycleConsumption.Where(c=>c.ColonyId==old.ColonyId))PopulationRules.AddStock(expected,delta.ResourceDefinitionId,-delta.Amount);
                }
                foreach(var kit in completed.Where(r=>r.Completed&&r.Order!.ColonyId==old.ColonyId&&r.Order.Kind==ColonisationProductionKind.SettlementKit))
                    PopulationRules.AddStock(expected,kit.Order!.DefinitionId,1);
                Require(FrontierSame(expected,current.Stockpiles),"Streamed Production changed unrelated stocks.");current.Stockpiles=old.Stockpiles;
                var additions=completed.Where(r=>r.Completed&&r.Order!.ColonyId==old.ColonyId&&r.Order.Kind==ColonisationProductionKind.Infrastructure)
                    .Select(r=>new InfrastructureStateDto{DefinitionId=r.Order!.DefinitionId,Count=1}).ToList();
                Require(FrontierSame(old.Infrastructure!.Concat(additions).OrderBy(i=>i.DefinitionId,StringComparer.Ordinal).ToList(),current.Infrastructure),"Streamed Production changed unrelated infrastructure.");current.Infrastructure=old.Infrastructure;
            }
            var retired=completed.Select(r=>r.Order!.QueueItemId).ToHashSet(StringComparer.Ordinal);
            phaseBefore.EntityIdentities.RemoveAll(i=>retired.Contains(i.EntityId));
            RequireOwnedChanges(phaseBefore,guardedAfter,false,production:true);
        }
        private void ProvisionColonisation(CampaignTacticalTravelSaveDto next,GameStateDto before,ShipStateDto ship,ColonisationProductionItemDto q)
        {
            var game=next.Core.Campaign.GameState;var supported=PublishedFit(before,q.Blueprint!,false);
            Require(supported.IsSupportedByCompiler,"Unsupported frozen streamed completion.");var fit=supported.Fit!;
            var source=CampaignBlueprints.Find(before,q.Blueprint!)!;
            var record=new CampaignPublishedBuiltShipDto{ShipId=ship.ShipId,BlueprintId=q.Blueprint!.BlueprintId,BlueprintRevision=q.Blueprint.Revision,
                OwnerCivilizationId=ship.OwnerCivilizationId,FleetIdAtConstruction=ship.FleetId,QueueItemId=q.QueueItemId,TurnBuilt=game.TurnNumber,
                SourceCompilerId=source.CompilerVersion,SourceRulesVersion=source.RulesVersion,SourceContentHash=source.SourceContentHash,
                PublicationHash=fit.SourceHash,SupportCatalogueId=fit.SupportCatalogueId,SupportCatalogueRevision=fit.SupportCatalogueRevision,SupportHash=fit.SupportHash,
                SupportCompilerId=fit.CompilerId,FitId=fit.FitId,FitRevision=fit.FitRevision};
            Require(colonisation!.Production.Contains(q)&&q.CompletedIndustry==q.RequiredIndustry&&q.OwnerId==ship.OwnerCivilizationId
                &&!before.Ships!.Any(s=>s.ShipId==ship.ShipId)&&game.Ships!.Count(s=>s.ShipId==ship.ShipId)==1
                &&new EntityId(ship.ShipId).TryGetAllocationSequence(out var seq)&&seq>=before.NextEntitySequence&&seq<game.NextEntitySequence,
                "Actual streamed completion association required.");
            var validated=PublishedBuiltShipValidation.ValidateRetained(new[]{record},game,published!.Support,initialTurn,activities!.Support,colonisation.Support)[0];
            published.Records.Add(PublishedBuiltShipValidation.Copy(record));published.Sources.Add(ship.ShipId,validated.Equipment);
            next.Core.Pools.Add(validated.Equipment.InitialPool());PublishedTransactionProbe?.Invoke("after-provision");
        }
        private void ValidateColonisationProduction(GameStateDto game)
        {
            var stream=colonisation!;Require(activities!.State.Production!.Count==0,"No prepaid carrier in streamed root.");
            var orders=stream.Production.Concat(stream.History.Select(r=>r.Order!)).ToList();
            Require(orders.Count<=published!.RecordBudget&&orders.All(q=>q!=null)&&orders.Select(q=>q.QueueItemId).Distinct(StringComparer.Ordinal).Count()==orders.Count,"Streamed identity/work budget.");
            BigInteger consumed=0;
            foreach(var q in orders)
            {
                Require(EntityId.TryParse(q.QueueItemId,out var id)&&q.QueueItemId.StartsWith("queue-item.",StringComparison.Ordinal)&&id.TryGetAllocationSequence(out var seq)&&seq<game.NextEntitySequence
                    &&game.Colonies!.Count(c=>c.ColonyId==q.ColonyId&&c.OwnerCivilizationId==q.OwnerId)==1&&q.RequiredIndustry>0&&q.CompletedIndustry>=0&&q.CompletedIndustry<=q.RequiredIndustry
                    &&q.RequiredResources!=null&&q.ConsumedResources!=null,"Complete streamed order association.");
                List<ResourceAmountDto> expected;long work;
                if(q.Kind==ColonisationProductionKind.PublishedShip)
                {
                    Require(q.Blueprint!=null&&q.Blueprint.BlueprintId==q.DefinitionId,"Streamed publication identity.");
                    var source=CampaignBlueprints.Find(game,q.Blueprint!);Require(source!=null&&source.OwnerCivilizationId==q.OwnerId&&PublishedFit(game,q.Blueprint!,false).IsSupportedByCompiler,"Frozen streamed source.");
                    expected=StreamResources(source!.CompiledDesign!.ResourceCosts!);work=source.CompiledDesign.IndustryCost;
                }
                else if(q.Kind==ColonisationProductionKind.Infrastructure)
                {
                    var cost=ColonisationStructureCost(q.DefinitionId);
                    Require(q.Kind==ColonisationProductionKind.Infrastructure&&q.Blueprint==null&&cost!=null,"Streamed structure source.");
                    expected=StreamResources(cost!.Resources!);work=cost.Industry;
                }
                else
                {
                    Require(q.Kind==ColonisationProductionKind.SettlementKit&&q.Blueprint==null&&q.DefinitionId==colonisation!.Policy.EquipmentResourceId,"Settlement equipment recipe source.");
                    expected=StreamResources(colonisation!.Policy.KitResources!);work=colonisation.Policy.KitIndustry;
                }
                Require(work==q.RequiredIndustry&&FrontierSame(expected,q.RequiredResources)&&q.ConsumedResources!.Count==expected.Count
                    &&q.ConsumedResources.Select(r=>r.ResourceDefinitionId).SequenceEqual(expected.Select(r=>r.ResourceDefinitionId))
                    &&expected.All(r=>r.ResourceDefinitionId!="resource.food"&&definitions.Resources!.Any(d=>d.DefinitionId==r.ResourceDefinitionId)),"Canonical streamed requirements.");
                foreach(var r in q.ConsumedResources!)
                {Require(r.Amount==StreamedMaterialWork.Cumulative(expected.Single(e=>e.ResourceDefinitionId==r.ResourceDefinitionId).Amount,work,q.CompletedIndustry),"Consumed vector differs from cumulative work.");consumed+=r.Amount;}
                if(stream.Production.Contains(q))Require(q.CompletedIndustry<q.RequiredIndustry&&game.EntityIdentities.Any(e=>e.EntityId==q.QueueItemId),"Live streamed queue identity.");
            }
            foreach(var r in stream.History)
            {
                var q=r.Order!;Require(r.ResolvedTurn>=initialTurn&&r.ResolvedTurn<=game.TurnNumber&&!game.EntityIdentities.Any(e=>e.EntityId==q.QueueItemId)
                    &&(r.Completed?q.CompletedIndustry==q.RequiredIndustry:q.CompletedIndustry<q.RequiredIndustry),"Streamed terminal receipt.");
                if(r.Completed&&q.Kind==ColonisationProductionKind.PublishedShip)Require(r.ShipId!=null&&published.Records.Count(p=>p.QueueItemId==q.QueueItemId&&p.ShipId==r.ShipId&&p.BlueprintId==q.Blueprint!.BlueprintId&&p.BlueprintRevision==q.Blueprint.Revision&&p.OwnerCivilizationId==q.OwnerId&&p.TurnBuilt==r.ResolvedTurn)==1,"Streamed retained built association.");
                else Require(r.ShipId==null,"No fabricated streamed ship.");
                if(r.Completed&&q.Kind==ColonisationProductionKind.Infrastructure)Require(activities.State.Structures!.Count(s=>s.QueueItemId==q.QueueItemId&&s.ColonyId==q.ColonyId&&s.DefinitionId==q.DefinitionId&&s.OwnerId==q.OwnerId&&s.CompletedTurn==r.ResolvedTurn)==1,"Streamed retained structure association.");
            }
            Require(stream.History.Count(r=>r.Completed&&r.Order!.Kind==ColonisationProductionKind.PublishedShip)==published.Records.Count
                &&stream.History.Count(r=>r.Completed&&r.Order!.Kind==ColonisationProductionKind.Infrastructure)==activities.State.Structures!.Count,"Every streamed completion receipted.");
            var original=published.Package.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.PopulationStatistics!.ResourceConsumed;
            Require(consumed+original+colonisation!.State.ManifestRecords!.Sum(m=>(long)m.EquipmentConsumed)==game.PopulationStatistics!.ResourceConsumed,"Actual streamed consumption conservation.");
        }
    }
}
