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
        private static bool BeltMinePowered()=>BeltMiningPolicy.PowerSupply>=BeltMiningPolicy.PowerDemand;
        private ColonyStateDto? BeltMineSponsor(GameStateDto game,BeltMineDto mine)
        {
            var body=beltBodies!.Definitions!.Single(b=>b.BodyId==mine.BodyId);
            var sponsor=game.Colonies!.SingleOrDefault(c=>c.ColonyId==mine.ColonyId&&c.OwnerCivilizationId==mine.ActorId&&c.SystemId==body.SystemId);
            return sponsor!=null&&EstablishedColony(sponsor.ColonyId)&&PopulationRules.Population(sponsor)>0?sponsor:null;
        }
        private ColonyStateDto? BeltMineOrderSponsor(GameStateDto game,ColonisationProductionItemDto order)
        {
            if(beltBodies==null||order.Kind!=ColonisationProductionKind.BeltMining)return null;
            var mine=beltBodies.Mines!.SingleOrDefault(m=>m.QueueItemId==order.QueueItemId&&m.Stage==BeltMineStage.Queued
                &&m.ActorId==order.OwnerId&&m.ColonyId==order.ColonyId);
            if(mine==null)return null;
            var sponsor=BeltMineSponsor(game,mine);
            return sponsor?.OwnerCivilizationId==order.OwnerId?sponsor:null;
        }
        private BeltBodyError ValidateBeltMine(BeltBodyCommandDto c)
        {
            if(beltBodies==null||colonisation==null)return BeltBodyError.Unavailable;
            if(!BeltMinePowered())return BeltBodyError.MineUnavailable;
            var game=state.Campaign.GameState;var body=beltBodies.Definitions!.SingleOrDefault(b=>b.BodyId==c.BodyId);
            if(body==null)return BeltBodyError.UnknownBody;
            if(!ColonisationSystemsForActor(game,c.ActorId).Contains(body.SystemId))return BeltBodyError.BodyNotObserved;
            if(!beltBodies.Observations!.Any(o=>o.ActorId==c.ActorId&&o.BodyId==body.BodyId))return BeltBodyError.SurveyRequired;
            var colony=game.Colonies!.SingleOrDefault(w=>w.ColonyId==c.ColonyId&&w.OwnerCivilizationId==c.ActorId);
            if(colony==null||!EstablishedColony(colony.ColonyId)||PopulationRules.Population(colony)==0)return BeltBodyError.ColonyUnavailable;
            if(colony.SystemId!=body.SystemId)return BeltBodyError.WrongSystem;
            if(beltBodies.Deposits!.Single(d=>d.BodyId==body.BodyId).Remaining==0)return BeltBodyError.DepositExhausted;
            if(beltBodies.Mines!.Any(m=>m.BodyId==body.BodyId&&m.Stage!=BeltMineStage.Cancelled))return BeltBodyError.MineExists;
            if(beltBodies.Mines!.Count>=BeltBodiesEditionIdentity.MaximumMines)return BeltBodyError.Capacity;
            var actor=game.Civilizations!.Single(a=>a.CivilizationId==c.ActorId);
            if(actor.Credits<BeltMiningPolicy.Credits)return BeltBodyError.InsufficientCredits;
            return BeltBodyError.None;
        }
        private string EnqueueBeltMine(BeltBodyCommandDto c)
        {
            var game=state.Campaign.GameState;var colony=game.Colonies!.Single(w=>w.ColonyId==c.ColonyId&&w.OwnerCivilizationId==c.ActorId);
            Require(colony.ProductionQueue!.Count==0,"Streamed queue remains the sole Industry authority.");
            var id=CampaignAllocation.Allocate(game,"belt-mine",null);var queue=CampaignAllocation.Allocate(game,"queue-item",null);
            game.Civilizations!.Single(a=>a.CivilizationId==c.ActorId).Credits=checked(game.Civilizations.Single(a=>a.CivilizationId==c.ActorId).Credits-BeltMiningPolicy.Credits);
            beltBodies!.Mines!.Add(new BeltMineDto{InstallationId=id,QueueItemId=queue,ActorId=c.ActorId,BodyId=c.BodyId,ColonyId=colony.ColonyId,Stage=BeltMineStage.Queued,
                LatestReport="Queued in the sponsor's shared Industry queue. Deployment requires clear local access."});
            colonisation!.Production.Add(new ColonisationProductionItemDto{QueueItemId=queue,ColonyId=colony.ColonyId,OwnerId=c.ActorId,Kind=ColonisationProductionKind.BeltMining,
                DefinitionId=BeltMiningPolicy.InstallationDefinition,RequiredIndustry=BeltMiningPolicy.Industry,RequiredResources=new List<ResourceAmountDto>(),ConsumedResources=new List<ResourceAmountDto>()});
            return id;
        }
        private BeltBodyError ValidateCancelBeltMine(BeltBodyCommandDto c)
        {
            if(beltBodies==null)return BeltBodyError.Unavailable;
            var mine=beltBodies.Mines!.SingleOrDefault(m=>m.InstallationId==c.MineId&&m.ActorId==c.ActorId&&m.BodyId==c.BodyId);
            return mine!=null&&mine.Stage==BeltMineStage.Queued&&colonisation!.Production.Any(q=>q.QueueItemId==mine.QueueItemId)?BeltBodyError.None:BeltBodyError.MineUnavailable;
        }
        private void CancelBeltMine(BeltBodyCommandDto c)
        {
            var mine=beltBodies!.Mines!.Single(m=>m.InstallationId==c.MineId&&m.ActorId==c.ActorId);
            Require(CancelColonisationProduction(state.Campaign.GameState,new CampaignActivityCommandDto{ActorId=c.ActorId,TargetId=mine.QueueItemId})==CampaignTacticalError.None,"Owned queued belt construction required.");
        }
        private void CancelBeltMineQueue(GameStateDto game,ColonisationProductionItemDto q)
        {
            var mine=beltBodies!.Mines!.Single(m=>m.QueueItemId==q.QueueItemId&&m.Stage==BeltMineStage.Queued);
            mine.Stage=BeltMineStage.Cancelled;mine.LatestReport="Construction cancelled. Completed Industry is not refunded.";
            game.EntityIdentities.RemoveAll(e=>e.EntityId==mine.InstallationId);
        }
        private bool CompleteBeltMine(GameStateDto game,ColonisationProductionItemDto q)
        {
            var mine=beltBodies!.Mines!.Single(m=>m.QueueItemId==q.QueueItemId&&m.Stage==BeltMineStage.Queued);
            var sponsor=BeltMineOrderSponsor(game,q);
            if(sponsor==null){mine.LatestReport="Deployment paused: the original sponsor is unavailable.";return false;}
            var route=PopulationLogistics.Path(game,physicalNavigation!,sponsor.SystemId,sponsor.SystemId,mine.ActorId);
            if(route.integrity==0){mine.LatestReport="Industry complete; deployment is waiting for clear local freight access.";return false;}
            mine.Stage=BeltMineStage.Active;mine.CompletedTurn=game.TurnNumber;mine.LatestReport="Uncrewed mine deployed. Extraction begins next Economy phase.";
            return true;
        }
        private void ExecuteBeltExtraction(GameStateDto game)
        {
            if(beltBodies==null)return;
            Require(BeltMiningPolicy.PowerSupply>=BeltMiningPolicy.PowerDemand,"Closed mining rig requires its own operating power.");
            beltBodies.LastShipments!.Clear();
            foreach(var mine in beltBodies.Mines!.Where(m=>m.Stage==BeltMineStage.Active||m.Stage==BeltMineStage.Exhausted).OrderBy(m=>m.BodyId,StringComparer.Ordinal).ThenBy(m=>m.InstallationId,StringComparer.Ordinal))
            {
                if(!BeltMinePowered()){mine.LatestReport="Paused: the closed mine recipe has insufficient power.";continue;}
                if(BeltMineSponsor(game,mine)==null){mine.LatestReport="Paused: the original sponsor is unavailable; local ore is retained.";continue;}
                Require(mine.CompletedTurn<game.TurnNumber,"A new mine cannot extract on its completion week.");
                var ore=beltBodies.Deposits!.Single(d=>d.BodyId==mine.BodyId);
                var extracted=Math.Min(ore.Remaining,Math.Min(BeltMiningPolicy.WeeklyExtraction,BeltMiningPolicy.MineStorage-ore.LocalStock));
                if(extracted>0)
                {
                    Require(mine.LastExtractionTurn!=game.TurnNumber,"Mine extraction already processed this week.");
                    ore.Remaining-=extracted;ore.LocalStock=checked(ore.LocalStock+extracted);mine.TotalExtracted=checked(mine.TotalExtracted+extracted);mine.LastExtractionTurn=game.TurnNumber;
                    game.PopulationStatistics!.ResourceExtracted=checked(game.PopulationStatistics.ResourceExtracted+extracted);
                    mine.LatestReport="Ore extracted into finite local mine storage; shipment uses the sponsor's remaining freight capacity.";
                }
                else if(ore.LocalStock>=BeltMiningPolicy.MineStorage)mine.LatestReport="Local mine storage is full; extraction paused until ore is shipped.";
                if(ore.Remaining==0){mine.Stage=BeltMineStage.Exhausted;mine.LatestReport="Deposit exhausted. Remaining local ore can still be shipped.";}
            }
        }
        private void DeliverBeltOre(GameStateDto game,IDictionary<string,long> residual,WeightedNavigationGraph graph)
        {
            if(beltBodies==null)return;
            foreach(var mine in beltBodies.Mines!.Where(m=>m.Stage==BeltMineStage.Active||m.Stage==BeltMineStage.Exhausted).OrderBy(m=>m.BodyId,StringComparer.Ordinal).ThenBy(m=>m.InstallationId,StringComparer.Ordinal))
            {
                var sponsor=BeltMineSponsor(game,mine);var ore=beltBodies.Deposits!.Single(d=>d.BodyId==mine.BodyId);
                if(sponsor==null||ore.LocalStock==0)continue;
                Require(mine.CompletedTurn<game.TurnNumber,"A new mine cannot ship on its completion week.");
                var route=PopulationLogistics.Path(game,graph,sponsor.SystemId,sponsor.SystemId,mine.ActorId);
                var capacity=residual.TryGetValue(sponsor.ColonyId,out var remaining)?remaining:0;
                var requested=Math.Min(BeltMiningPolicy.WeeklyTransferLimit,Math.Min(ore.LocalStock,capacity));
                var delivered=PopulationRules.ScaleBy(PopulationRules.ScaleBy(requested,sponsor.Stability),route.integrity);
                if(delivered<=0){mine.LatestReport=route.integrity==0?"Shipment blocked by local freight access; ore remains in finite mine storage.":"No effective sponsor freight capacity remains; ore is retained locally.";continue;}
                Require(mine.LastDeliveryTurn!=game.TurnNumber,"Mine shipment already processed this week.");
                ore.LocalStock-=delivered;PopulationRules.AddStock(sponsor.Stockpiles!,BeltMiningPolicy.Resource,delivered);residual[sponsor.ColonyId]-=delivered;
                mine.TotalDelivered=checked(mine.TotalDelivered+delivered);mine.LastDeliveryTurn=game.TurnNumber;mine.LatestReport="Ore delivered through the sponsor's shared remaining freight endpoint.";
                game.PopulationStatistics!.ResourceDelivered=checked(game.PopulationStatistics.ResourceDelivered+delivered);
                beltBodies.LastShipments!.Add(new BeltShipmentDto{InstallationId=mine.InstallationId,ActorId=mine.ActorId,BodyId=mine.BodyId,ColonyId=mine.ColonyId,ResourceDefinitionId=BeltMiningPolicy.Resource,Amount=delivered,Turn=game.TurnNumber});
            }
        }
        private void ValidateBeltMiningState(GameStateDto game)
        {
            if(beltBodies==null)return;
            var mines=beltBodies.Mines!;var all=colonisation!.Production.Concat(colonisation.History.Select(r=>r.Order!)).ToList();
            Require(mines.Count<=BeltBodiesEditionIdentity.MaximumMines&&mines.Select(m=>m.InstallationId).Distinct(StringComparer.Ordinal).Count()==mines.Count
                &&mines.Select(m=>m.QueueItemId).Distinct(StringComparer.Ordinal).Count()==mines.Count,"Bounded unique belt installations required.");
            foreach(var mine in mines)
            {
                Require(EntityId.TryParse(mine.InstallationId,out var id)&&mine.InstallationId.StartsWith("belt-mine.",StringComparison.Ordinal)&&id.TryGetAllocationSequence(out var sequence)&&sequence<game.NextEntitySequence
                    &&controllers.ContainsKey(mine.ActorId)&&beltBodies.Definitions!.Any(b=>b.BodyId==mine.BodyId)&&Enum.IsDefined(typeof(BeltMineStage),mine.Stage)
                    &&mine.TotalExtracted>=0&&mine.TotalDelivered>=0&&mine.TotalDelivered<=mine.TotalExtracted,"Actual mine identity and cumulative material required.");
                var q=all.SingleOrDefault(q=>q.QueueItemId==mine.QueueItemId);
                Require(q!=null&&q.Kind==ColonisationProductionKind.BeltMining&&q.OwnerId==mine.ActorId&&q.ColonyId==mine.ColonyId&&q.Blueprint==null
                    &&q.DefinitionId==BeltMiningPolicy.InstallationDefinition&&q.RequiredIndustry==BeltMiningPolicy.Industry&&q.RequiredResources!.Count==0&&q.ConsumedResources!.Count==0,"Exact paid closed mine recipe required.");
                var colony=game.Colonies!.SingleOrDefault(c=>c.ColonyId==mine.ColonyId);
                Require(colony!=null&&colony.SystemId==beltBodies.Definitions!.Single(b=>b.BodyId==mine.BodyId).SystemId,"A mine retains its original same-system sponsor.");
                if(mine.Stage==BeltMineStage.Queued)Require(mine.CompletedTurn==null&&mine.TotalExtracted==0&&mine.TotalDelivered==0&&colonisation.Production.Contains(q!),"Queued mine association required.");
                else
                {
                    var terminal=colonisation.History.SingleOrDefault(r=>r.Order!.QueueItemId==mine.QueueItemId);
                    Require(terminal!=null&&terminal.Completed==(mine.Stage!=BeltMineStage.Cancelled),"Mine construction history required.");
                    if(mine.Stage==BeltMineStage.Cancelled)Require(mine.CompletedTurn==null&&mine.TotalExtracted==0&&mine.TotalDelivered==0,"Cancelled construction grants no mine yield.");
                    else Require(mine.CompletedTurn>=initialTurn&&mine.CompletedTurn<=game.TurnNumber&&mine.CompletedTurn==terminal!.ResolvedTurn,"Actual deployment turn required.");
                }
                Require(game.EntityIdentities.Any(e=>e.EntityId==mine.InstallationId)==(mine.Stage!=BeltMineStage.Cancelled),"Mine live identity mismatch.");
                Require((mine.TotalExtracted==0?mine.LastExtractionTurn==null:mine.LastExtractionTurn>mine.CompletedTurn&&mine.LastExtractionTurn<=game.TurnNumber)
                    &&(mine.TotalDelivered==0?mine.LastDeliveryTurn==null:mine.LastDeliveryTurn>mine.CompletedTurn&&mine.LastDeliveryTurn<=game.TurnNumber),"Mine work/delivery turn mismatch.");
            }
            Require(all.Count(q=>q.Kind==ColonisationProductionKind.BeltMining)==mines.Count,"Every mine order has exactly one installation lineage.");
            foreach(var body in beltBodies.Definitions!)
            {
                var deposit=beltBodies.Deposits!.Single(d=>d.BodyId==body.BodyId);var lineage=mines.Where(m=>m.BodyId==body.BodyId).ToList();
                Require(lineage.Count(m=>m.Stage!=BeltMineStage.Cancelled)<=1&&deposit.Remaining>=0&&deposit.LocalStock>=0&&deposit.LocalStock<=BeltMiningPolicy.MineStorage
                    &&(BigInteger)deposit.Remaining+lineage.Aggregate(BigInteger.Zero,(sum,m)=>sum+m.TotalExtracted)==body.InitialDeposit
                    &&lineage.Aggregate(BigInteger.Zero,(sum,m)=>sum+m.TotalExtracted-m.TotalDelivered)==deposit.LocalStock,"Finite ore and local stock must be conserved.");
                foreach(var mine in lineage.Where(m=>m.Stage==BeltMineStage.Exhausted))Require(deposit.Remaining==0,"Only a depleted deposit is exhausted.");
            }
            Require(beltBodies.LastShipments!.Count<=BeltBodiesEditionIdentity.MaximumLastShipments&&beltBodies.LastShipments.Select(r=>r.InstallationId).Distinct(StringComparer.Ordinal).Count()==beltBodies.LastShipments.Count,"One bounded last shipment per mine.");
            foreach(var receipt in beltBodies.LastShipments)
            {
                var mine=mines.SingleOrDefault(m=>m.InstallationId==receipt.InstallationId);
                Require(mine!=null&&receipt.ActorId==mine.ActorId&&receipt.BodyId==mine.BodyId&&receipt.ColonyId==mine.ColonyId&&receipt.ResourceDefinitionId==BeltMiningPolicy.Resource
                    &&receipt.Amount>0&&receipt.Amount<=BeltMiningPolicy.WeeklyTransferLimit&&receipt.Turn==mine.LastDeliveryTurn&&receipt.Turn<=game.TurnNumber,"Actual belt shipment lineage required.");
            }
        }
    }
}
