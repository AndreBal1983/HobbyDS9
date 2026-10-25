using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        public IReadOnlyList<ColonisationPlanetObservationDto> CaptureColonisationPlanets(string actor)
        {
            lock(gate)
            {
                Require(colonisation!=null&&Actor(actor)==CampaignTacticalError.None,"Controlled colonisation actor required.");
                var game=state.Campaign.GameState;
                return game.Planets!.Where(p=>physicalNavigation!.KnownNode(game,actor,definitions.NavigationRegions!.Single(r=>r.AssociatedSystemId==p.SystemId).NodeId))
                    .OrderBy(p=>p.PlanetId,StringComparer.Ordinal).Select(p=>{
                        var survey=colonisation!.State.Surveys!.SingleOrDefault(s=>s.ActorId==actor&&s.PlanetId==p.PlanetId);
                        var known=survey?.Stage==PlanetSurveyStage.Completed||game.Colonies!.Any(c=>c.PlanetId==p.PlanetId&&c.OwnerCivilizationId==actor);
                        return new ColonisationPlanetObservationDto{PlanetId=p.PlanetId,BodyId=colonisation.State.Bodies!.Single(b=>b.PlanetId==p.PlanetId).BodyId,SystemId=p.SystemId,
                            DisplayName=definitions.Planets!.Single(d=>d.DefinitionId==p.DefinitionId).DisplayName,Surveyed=known,
                            Environment=known?PopulationData.Copy(definitions.Planets.Single(d=>d.DefinitionId==p.DefinitionId)):null,
                            KnownControl=known?ColonisationData.Copy(colonisation.State.PlanetControls!.Single(c=>c.PlanetId==p.PlanetId)):null,
                            ActiveSurveyId=survey?.Stage==PlanetSurveyStage.Active?survey.SurveyId:null,CompletedSurveyWeeks=survey?.CompletedWeeks??0};
                    }).ToList().AsReadOnly();
            }
        }
        public CampaignTacticalMutationResult SubmitColonisation(ColonisationCommandDto? command)
        {
            lock(gate)
            {
                if(colonisation==null||command==null)return Result(CampaignTacticalError.UnsupportedSource);
                if(!publishedExecuting)return PublishedMutation(c=>c.SubmitColonisation(command));
                var game=state.Campaign.GameState;var actor=Actor(command.ActorId);if(actor!=CampaignTacticalError.None)return Result(actor);
                if(CurrentPhase!=SessionPhase.Planning)return Result(CampaignTacticalError.WrongPhase);
                if(command.Turn!=game.TurnNumber)return Result(CampaignTacticalError.StaleTurn);
                if(command.Revision!=game.StateRevision)return Result(CampaignTacticalError.StaleRevision);
                if(!EntityId.TryParse(command.CommandId,out _)||command.CommandId.StartsWith("ai-command.",StringComparison.Ordinal))return Result(CampaignTacticalError.InvalidOrder);
                if(state.AcceptedRoundCommands.Contains(command.CommandId,StringComparer.Ordinal)||game.AcceptedCommandIds!.Contains(command.CommandId,StringComparer.Ordinal))return Result(CampaignTacticalError.DuplicateCommand);
                var payloads=new object?[]{command.StartSurvey,command.CancelSurvey,command.Embark,command.Unload,command.Deploy,command.Withdraw,command.Evacuate,command.Deliver,command.EnqueueKit,command.LoadSupplies};
                if(payloads.Count(x=>x!=null)!=1)return Result(CampaignTacticalError.InvalidOrder);
                CampaignTacticalError error;
                switch(command.Action)
                {
                    case ColonisationAction.StartPlanetSurvey when command.StartSurvey!=null:error=StartPlanetSurvey(game,command.ActorId,command.StartSurvey);break;
                    case ColonisationAction.CancelPlanetSurvey when command.CancelSurvey!=null:error=CancelPlanetSurvey(game,command.ActorId,command.CancelSurvey);break;
                    case ColonisationAction.EnqueueKit when command.EnqueueKit!=null:error=EnqueueSettlementKit(game,command.ActorId,command.EnqueueKit);break;
                    default:return Result(CampaignTacticalError.UnsupportedSource);
                }
                if(error!=CampaignTacticalError.None)return Result(error);
                game.StateRevision=checked(game.StateRevision+1);state.AcceptedRoundCommands.Add(command.CommandId);
                colonisation.Journal.Add(new ColonisationOperationDto{Colonisation=ColonisationData.Copy(command)});
                return Result(CampaignTacticalError.None);
            }
        }
        private bool ShipAt(GameStateDto game,ShipStateDto ship,string system)
        {
            var fleet=game.Fleets!.SingleOrDefault(f=>f.FleetId==ship.FleetId&&f.OwnerCivilizationId==ship.OwnerCivilizationId);
            return ship.CurrentHull>0&&fleet!=null&&fleet.TransitTotalCost==0&&fleet.RouteNodeIds!.Count==0
                &&definitions.NavigationRegions!.Any(n=>n.AssociatedSystemId==system&&n.NodeId==fleet.LocationNodeId);
        }
        private CampaignTacticalError StartPlanetSurvey(GameStateDto game,string actor,PlanetSurveyOrderDto order)
        {
            var planet=game.Planets!.SingleOrDefault(p=>p.PlanetId==order.PlanetId);var ship=game.Ships!.SingleOrDefault(s=>s.ShipId==order.ShipId&&s.OwnerCivilizationId==actor);
            if(planet==null||ship==null||!ShipAt(game,ship,planet.SystemId)||(ActivityCapability(game,ship)?.StudyWork??0)<1)return CampaignTacticalError.InvalidOrder;
            var a=colonisation!.State;var prior=a.Surveys!.SingleOrDefault(s=>s.ActorId==actor&&s.PlanetId==planet.PlanetId);
            if(prior?.Stage==PlanetSurveyStage.Completed||prior?.Stage==PlanetSurveyStage.Active||a.Surveys!.Any(s=>s.ShipId==ship.ShipId&&s.Stage==PlanetSurveyStage.Active)
                ||activities!.State.Findings!.Any(f=>f.ShipId==ship.ShipId&&(f.Stage==ActivityProgressStage.Active||f.Stage==ActivityProgressStage.AwaitingClaim)))return CampaignTacticalError.InvalidOrder;
            if(prior!=null)a.Surveys!.Remove(prior);
            var id=CampaignAllocation.Allocate(game,"planet-survey",null);
            a.Surveys!.Add(new PlanetSurveyDto{SurveyId=id,ActorId=actor,PlanetId=planet.PlanetId,ShipId=ship.ShipId,Stage=PlanetSurveyStage.Active,StartedTurn=game.TurnNumber});
            return CampaignTacticalError.None;
        }
        private CampaignTacticalError CancelPlanetSurvey(GameStateDto game,string actor,CancelPlanetSurveyDto command)
        {
            var survey=colonisation!.State.Surveys!.SingleOrDefault(s=>s.SurveyId==command.SurveyId&&s.ActorId==actor&&s.Stage==PlanetSurveyStage.Active);
            if(survey==null)return CampaignTacticalError.InvalidOrder;
            survey.Stage=PlanetSurveyStage.Cancelled;survey.CompletedWeeks=0;survey.ResolvedTurn=game.TurnNumber;game.EntityIdentities.RemoveAll(e=>e.EntityId==survey.SurveyId);
            return CampaignTacticalError.None;
        }
        private CampaignTacticalError EnqueueSettlementKit(GameStateDto game,string actor,EnqueueSettlementKitDto order)
        {
            var colony=game.Colonies!.SingleOrDefault(c=>c.ColonyId==order.ColonyId&&c.OwnerCivilizationId==actor);
            if(colony==null||!EstablishedColony(colony.ColonyId)||PopulationRules.Population(colony)==0)return CampaignTacticalError.InvalidOrder;
            var policy=colonisation!.Policy;var resources=StreamResources(policy.KitResources!);
            colonisation.Production.Add(new ColonisationProductionItemDto{QueueItemId=CampaignAllocation.Allocate(game,"queue-item",null),ColonyId=colony.ColonyId,OwnerId=actor,
                Kind=ColonisationProductionKind.SettlementKit,DefinitionId=policy.EquipmentResourceId,RequiredIndustry=policy.KitIndustry,RequiredResources=resources,
                ConsumedResources=resources.Select(r=>new ResourceAmountDto{ResourceDefinitionId=r.ResourceDefinitionId,Amount=0}).ToList()});
            return CampaignTacticalError.None;
        }
        private void TickPlanetSurveys(GameStateDto game,CampaignColonisationStateDto a)
        {
            foreach(var survey in a.Surveys!.Where(s=>s.Stage==PlanetSurveyStage.Active).OrderBy(s=>s.SurveyId,StringComparer.Ordinal))
            {
                var ship=game.Ships!.SingleOrDefault(s=>s.ShipId==survey.ShipId&&s.OwnerCivilizationId==survey.ActorId);
                var planet=game.Planets!.Single(p=>p.PlanetId==survey.PlanetId);
                if(ship==null||!ShipAt(game,ship,planet.SystemId)||(ActivityCapability(game,ship)?.StudyWork??0)<1)
                {survey.Stage=PlanetSurveyStage.Interrupted;survey.CompletedWeeks=0;survey.ResolvedTurn=game.TurnNumber;game.EntityIdentities.RemoveAll(e=>e.EntityId==survey.SurveyId);continue;}
                survey.CompletedWeeks++;
                if(survey.CompletedWeeks==2){survey.Stage=PlanetSurveyStage.Completed;survey.ResolvedTurn=game.TurnNumber;game.EntityIdentities.RemoveAll(e=>e.EntityId==survey.SurveyId);}
            }
        }
    }
}
