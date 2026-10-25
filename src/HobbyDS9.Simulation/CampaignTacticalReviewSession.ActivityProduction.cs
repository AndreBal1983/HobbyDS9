using System;
using System.Linq;
using System.Collections.Generic;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private GameStateDto RunActivityPlanningAi(CampaignTacticalTravelSaveDto next)
        {
            var staged=SimulationSession.CopyState(next.Core.Campaign.GameState);
            foreach(var actor in staged.Civilizations!.Where(a=>a.ControllerKind==ControllerKind.AI).OrderBy(a=>a.CivilizationId,StringComparer.Ordinal))
            {
                if(staged.Ships!.Count(s=>s.OwnerCivilizationId==actor.CivilizationId)
                    +(Streams?.Production.Count(q=>q.Kind==ActivityProductionKind.PublishedShip&&q.OwnerId==actor.CivilizationId)
                    ??activities!.State.Production!.Count(q=>q.Kind==ActivityProductionKind.PublishedShip&&staged.Colonies!.Single(c=>c.ColonyId==q.ColonyId).OwnerCivilizationId==actor.CivilizationId))>=3)continue;
                foreach(var colony in staged.Colonies!.Where(c=>c.OwnerCivilizationId==actor.CivilizationId).OrderBy(c=>c.ColonyId,StringComparer.Ordinal))
                {
                    if(Streams!=null?Streams.Production.Any(q=>q.ColonyId==colony.ColonyId):activities!.State.Production!.Any(q=>q.ColonyId==colony.ColonyId))continue;
                    foreach(var blueprint in staged.Blueprints!.Where(b=>b.OwnerCivilizationId==actor.CivilizationId)
                        .GroupBy(b=>b.BlueprintId,StringComparer.Ordinal).Select(g=>g.OrderByDescending(b=>b.Revision).First())
                        .OrderBy(b=>b.CompiledDesign!.CreditCost).ThenBy(b=>b.BlueprintId,StringComparer.Ordinal))
                    {
                        var candidate=ForkPublished();candidate.travel=CampaignTacticalTravelData.Copy(next);candidate.state=candidate.travel.Core;
                        candidate.state.Campaign.GameState=SimulationSession.CopyState(staged);
                        var result=candidate.EnqueueActivity(candidate.state.Campaign.GameState,new CampaignActivityCommandDto{
                            ActorId=actor.CivilizationId,Kind=CampaignActivityCommandKind.EnqueueShip,TargetId=colony.ColonyId,
                            DefinitionId=blueprint.BlueprintId,BlueprintRevision=blueprint.Revision},ControllerKind.AI);
                        if(result!=CampaignTacticalError.None)continue;
                        candidate.ValidateActivityState();
                        // AI procurement is part of the upcoming advance, not a free
                        // journal entry. Reserve that actual advance before payment commits.
                        if(!candidate.ActivityCapacity(1))continue;
                        activities=candidate.activities;staged=SimulationSession.CopyState(candidate.state.Campaign.GameState);break;
                    }
                    break;
                }
            }
            return staged;
        }
        public CampaignTacticalMutationResult SubmitActivity(CampaignActivityCommandDto? command)
        {
            lock(gate)
            {
                if(activities==null||command==null)return Result(CampaignTacticalError.UnsupportedSource);
                if(!publishedExecuting)return PublishedMutation(c=>c.SubmitActivity(command));
                var game=state.Campaign.GameState;
                var actor=Actor(command.ActorId);if(actor!=CampaignTacticalError.None)return Result(actor);
                if(CurrentPhase!=SessionPhase.Planning)return Result(CampaignTacticalError.WrongPhase);
                if(command.ExpectedTurn!=game.TurnNumber)return Result(CampaignTacticalError.StaleTurn);
                if(command.ExpectedRevision!=game.StateRevision)return Result(CampaignTacticalError.StaleRevision);
                if(!EntityId.TryParse(command.CommandId,out _)||command.CommandId.StartsWith("ai-command.",StringComparison.Ordinal))return Result(CampaignTacticalError.InvalidOrder);
                if(state.AcceptedRoundCommands.Contains(command.CommandId,StringComparer.Ordinal)||game.AcceptedCommandIds!.Contains(command.CommandId,StringComparer.Ordinal))return Result(CampaignTacticalError.DuplicateCommand);
                CampaignTacticalError error;
                switch(command.Kind)
                {
                    case CampaignActivityCommandKind.EnqueueShip:
                    case CampaignActivityCommandKind.EnqueueStructure:
                        error=EnqueueActivity(game,command,ControllerKind.Human);break;
                    case CampaignActivityCommandKind.CancelProduction:
                        error=CancelActivityProduction(game,command);break;
                    case CampaignActivityCommandKind.StartStudy:error=StartActivityStudy(game,command);break;
                    case CampaignActivityCommandKind.ClaimFinding:error=ResolveActivity(game,command,false);break;
                    case CampaignActivityCommandKind.AbortStudy:error=ResolveActivity(game,command,true);break;
                    default:return Result(CampaignTacticalError.UnsupportedSource);
                }
                if(error!=CampaignTacticalError.None)return Result(error);
                game.StateRevision=checked(game.StateRevision+1);
                state.AcceptedRoundCommands.Add(command.CommandId);
                activities.Journal.Add(new CampaignActivityOperationDto{Activity=CampaignActivitiesData.Copy(command)});
                return Result(CampaignTacticalError.None);
            }
        }

        private CampaignTacticalError EnqueueActivity(GameStateDto game,CampaignActivityCommandDto command,ControllerKind controller)
        {
            if(Streams!=null)return EnqueueStreamed(game,command,controller);
            var colony=game.Colonies!.SingleOrDefault(c=>c.ColonyId==command.TargetId&&c.OwnerCivilizationId==command.ActorId);
            if(colony==null)return CampaignTacticalError.InvalidOrder;
            Require(colony.ProductionQueue!.Count==0,"Activity boundaries never expose a second production queue.");
            if(command.Kind==CampaignActivityCommandKind.EnqueueShip)
            {
                var reference=new BlueprintReferenceDto{BlueprintId=command.DefinitionId,Revision=command.BlueprintRevision};
                var record=CampaignBlueprints.Find(game,reference);
                if(record==null||record.OwnerCivilizationId!=command.ActorId||!PublishedFit(game,reference,true).IsSupportedByCompiler)return CampaignTacticalError.UnsupportedSource;
                var result=new EnqueueBlueprintConstructionHandler(definitions).ExecuteForOwner(game,new CommandEnvelope{
                    ActorCivilizationId=command.ActorId,Kind=CommandKind.EnqueueBlueprintConstruction,EnqueueBlueprintConstruction=new EnqueueBlueprintConstructionPayload{
                        ColonyId=colony.ColonyId,BlueprintId=command.DefinitionId,Revision=command.BlueprintRevision}},controller);
                if(result!=CommandErrorCode.None)return CampaignTacticalError.InvalidOrder;
                var paid=colony.ProductionQueue.Single();colony.ProductionQueue.Clear();
                activities!.State.Production!.Add(new ActivityProductionItemDto{ColonyId=colony.ColonyId,Kind=ActivityProductionKind.PublishedShip,DefinitionId=reference.BlueprintId,Paid=paid});
                return CampaignTacticalError.None;
            }
            var structure=activities!.Content.Structures!.SingleOrDefault(s=>s.DefinitionId==command.DefinitionId);
            if(structure==null||colony.Infrastructure!.Any(i=>i.DefinitionId==structure.DefinitionId&&i.Count>0)
                ||activities.State.Production!.Any(q=>q.ColonyId==colony.ColonyId&&q.DefinitionId==structure.DefinitionId))return CampaignTacticalError.InvalidOrder;
            var owner=game.Civilizations!.Single(c=>c.CivilizationId==command.ActorId);
            if(owner.ControllerKind!=controller||owner.Credits<structure.Cost!.Credits||structure.Cost.Resources!.Any(r=>PopulationRules.Stock(colony.Stockpiles!,r.ResourceDefinitionId)<r.Amount))return CampaignTacticalError.InvalidOrder;
            var id=CampaignAllocation.Allocate(game,"queue-item",null);owner.Credits-=structure.Cost.Credits;
            foreach(var r in structure.Cost.Resources!)PopulationRules.AddStock(colony.Stockpiles!,r.ResourceDefinitionId,-r.Amount);
            activities.State.Production!.Add(new ActivityProductionItemDto{ColonyId=colony.ColonyId,Kind=ActivityProductionKind.Infrastructure,DefinitionId=structure.DefinitionId,
                Paid=new ProductionQueueItemDto{QueueItemId=id,RequiredIndustry=structure.Cost.Industry,PaidCredits=structure.Cost.Credits,PaidResources=structure.Cost.Resources.Select(PopulationData.Copy).ToList()}});
            return CampaignTacticalError.None;
        }

        private CampaignTacticalError CancelActivityProduction(GameStateDto game,CampaignActivityCommandDto command)
        {
            if(Streams!=null)return CancelStreamed(game,command);
            var entry=activities!.State.Production!.SingleOrDefault(q=>q.Paid!.QueueItemId==command.TargetId);
            if(entry==null)return CampaignTacticalError.InvalidOrder;
            var colony=game.Colonies!.Single(c=>c.ColonyId==entry.ColonyId);
            if(colony.OwnerCivilizationId!=command.ActorId)return CampaignTacticalError.InvalidOrder;
            // Reuse the authoritative proportional refund, stock and consumption arithmetic.
            colony.ProductionQueue!.Add(entry.Paid!);
            var result=new CancelProductionHandler().Execute(game,new CommandEnvelope{ActorCivilizationId=command.ActorId,Kind=CommandKind.CancelProduction,
                CancelProduction=new CancelProductionPayload{ColonyId=colony.ColonyId,QueueItemId=entry.Paid!.QueueItemId}});
            Require(result==CommandErrorCode.None&&colony.ProductionQueue.Count==0,"Mixed cancellation association.");
            activities.State.Production!.Remove(entry);return CampaignTacticalError.None;
        }

        private void ExecuteActivityProduction(CampaignTacticalTravelSaveDto next)
        {
            if(Streams!=null){ExecuteStreamedProduction(next);return;}
            var game=next.Core.Campaign.GameState;
            var heads=game.Colonies!.OrderBy(c=>c.ColonyId,StringComparer.Ordinal)
                .Select(c=>(Colony:c,Head:activities!.State.Production!.FirstOrDefault(q=>q.ColonyId==c.ColonyId)))
                .Where(x=>x.Head!=null).ToList();
            // Capture this cycle's output before any completion. No head or colony gets carry.
            var industry=heads.ToDictionary(x=>x.Colony.ColonyId,x=>WorldEconomy.Project(x.Colony,game,definitions).Industry,StringComparer.Ordinal);
            var guardedBefore=SimulationSession.CopyState(game);
            foreach(var x in heads)guardedBefore.Colonies!.Single(c=>c.ColonyId==x.Colony.ColonyId).ProductionQueue!.Add(CampaignActivitiesData.Copy(x.Head!.Paid!));
            foreach(var x in heads.Where(x=>x.Head!.Kind==ActivityProductionKind.PublishedShip))x.Colony.ProductionQueue!.Add(CampaignActivitiesData.Copy(x.Head!.Paid!));
            var before=SimulationSession.CopyState(game);
            ProductionPhase.Execute(game,definitions);
            // Provision before structures update the shared completion counter.
            ProvisionCompletedShips(next,before);
            foreach(var x in heads)
            {
                var item=x.Head!;var paid=item.Paid!;
                if(item.Kind==ActivityProductionKind.PublishedShip)
                {
                    if(x.Colony.ProductionQueue!.Count==0)activities!.State.Production!.Remove(item);
                    else item.Paid=CampaignActivitiesData.Copy(x.Colony.ProductionQueue.Single());
                    x.Colony.ProductionQueue!.Clear();continue;
                }
                var available=industry[x.Colony.ColonyId];
                if(available<paid.RequiredIndustry-paid.CompletedIndustry){paid.CompletedIndustry=checked(paid.CompletedIndustry+available);continue;}
                Require(!x.Colony.Infrastructure!.Any(i=>i.DefinitionId==item.DefinitionId&&i.Count>0),"Structure completion uniqueness.");
                x.Colony.Infrastructure!.Add(new InfrastructureStateDto{DefinitionId=item.DefinitionId,Count=1});
                game.PopulationStatistics!.ResourceConsumed=checked(game.PopulationStatistics.ResourceConsumed+PopulationRules.Sum(paid.PaidResources!.Select(r=>r.Amount)));
                game.PopulationStatistics.ProductionCompleted=checked(game.PopulationStatistics.ProductionCompleted+1);
                game.EntityIdentities.RemoveAll(i=>i.EntityId==paid.QueueItemId);
                activities!.State.Structures!.Add(new ActivityStructureReceiptDto{QueueItemId=paid.QueueItemId,ColonyId=x.Colony.ColonyId,
                    DefinitionId=item.DefinitionId,OwnerId=x.Colony.OwnerCivilizationId!,CompletedTurn=game.TurnNumber});
                activities.State.Production!.Remove(item);
            }
            var guardedAfter=SimulationSession.CopyState(game);
            foreach(var x in heads)
            {
                var c=guardedAfter.Colonies!.Single(c=>c.ColonyId==x.Colony.ColonyId);
                if(activities!.State.Production!.Contains(x.Head!))c.ProductionQueue!.Add(CampaignActivitiesData.Copy(x.Head!.Paid!));
                // Only this production adapter may append a fully receipted structure.
                // The complete state validator independently reconstructs these additions.
                c.Infrastructure=guardedBefore.Colonies!.Single(b=>b.ColonyId==c.ColonyId).Infrastructure;
            }
            SimulationSession.RequirePhaseChanges(guardedBefore,guardedAfter,false,allowProduction:true);
        }
    }
}
