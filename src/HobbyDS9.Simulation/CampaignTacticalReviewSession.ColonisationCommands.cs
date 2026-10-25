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
                return ColonisationPlanetsForActor(state.Campaign.GameState,actor);
            }
        }
        private IReadOnlyList<ColonisationPlanetObservationDto> ColonisationPlanetsForActor(GameStateDto game,string actor)
        {
                // Star-chart adjacency is not body discovery. Only actual presence,
                // retained arrival receipts and owned worlds disclose a body's existence.
                var coverage=GeneratedCoverage(game,actor).ToHashSet(StringComparer.Ordinal);
                var arrivals=coverage.Concat(discoveries.Where(d=>d.ActorId==actor).Select(d=>d.ArrivalNodeId)).ToHashSet(StringComparer.Ordinal);
                return game.Planets!.Where(p=>arrivals.Contains(definitions.NavigationRegions!.Single(r=>r.AssociatedSystemId==p.SystemId).NodeId))
                    .OrderBy(p=>p.PlanetId,StringComparer.Ordinal).Select(p=>{
                        var survey=colonisation!.State.Surveys!.SingleOrDefault(s=>s.ActorId==actor&&s.PlanetId==p.PlanetId);
                        var known=survey?.Stage==PlanetSurveyStage.Completed||game.Colonies!.Any(c=>c.PlanetId==p.PlanetId&&c.OwnerCivilizationId==actor);
                        return new ColonisationPlanetObservationDto{PlanetId=p.PlanetId,BodyId=colonisation.State.Bodies!.Single(b=>b.PlanetId==p.PlanetId).BodyId,SystemId=p.SystemId,
                            DisplayName=definitions.Planets!.Single(d=>d.DefinitionId==p.DefinitionId).DisplayName,Surveyed=known,
                            Environment=known?PopulationData.Copy(definitions.Planets.Single(d=>d.DefinitionId==p.DefinitionId)):null,
                            KnownControl=!known?null:coverage.Contains(definitions.NavigationRegions!.Single(r=>r.AssociatedSystemId==p.SystemId).NodeId)
                                ?ColonisationData.Copy(colonisation.State.PlanetControls!.Single(c=>c.PlanetId==p.PlanetId)):survey?.LastObservedControl==null?null:ColonisationData.Copy(survey.LastObservedControl),
                            ControlObservedTurn=!known?null:coverage.Contains(definitions.NavigationRegions!.Single(r=>r.AssociatedSystemId==p.SystemId).NodeId)?game.TurnNumber:survey?.ControlObservedTurn,
                            ActiveSurveyId=survey?.Stage==PlanetSurveyStage.Active?survey.SurveyId:null,CompletedSurveyWeeks=survey?.CompletedWeeks??0};
                    }).ToList().AsReadOnly();
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
                    case ColonisationAction.EmbarkSettlers when command.Embark!=null:error=EmbarkSettlers(game,command.ActorId,command.Embark);break;
                    case ColonisationAction.LoadCarriedSupplies when command.LoadSupplies!=null:error=LoadCarriedSupplies(game,command.ActorId,command.LoadSupplies);break;
                    case ColonisationAction.UnloadTransport when command.Unload!=null:error=UnloadTransport(game,command.ActorId,command.Unload);break;
                    case ColonisationAction.DeliverCarriedSupplies when command.Deliver!=null:error=DeliverSupplies(game,command.ActorId,command.Deliver);break;
                    case ColonisationAction.RequestDeployment when command.Deploy!=null:error=RequestDeployment(game,command.ActorId,command.Deploy);break;
                    case ColonisationAction.WithdrawDeployment when command.Withdraw!=null:error=WithdrawDeployment(game,command.ActorId,command.Withdraw);break;
                    case ColonisationAction.EvacuateColony when command.Evacuate!=null:error=EvacuateFounding(game,command.ActorId,command.Evacuate);break;
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
        private void ObserveSurveyControls(GameStateDto game,CampaignColonisationStateDto a)
        {
            foreach(var actor in game.Civilizations!.OrderBy(c=>c.CivilizationId,StringComparer.Ordinal))
            {
                var coverage=GeneratedCoverage(game,actor.CivilizationId).ToHashSet(StringComparer.Ordinal);
                foreach(var survey in a.Surveys!.Where(s=>s.ActorId==actor.CivilizationId&&s.Stage==PlanetSurveyStage.Completed))
                {
                    var planet=game.Planets!.Single(p=>p.PlanetId==survey.PlanetId);
                    if(!coverage.Contains(definitions.NavigationRegions!.Single(n=>n.AssociatedSystemId==planet.SystemId).NodeId))continue;
                    survey.LastObservedControl=ColonisationData.Copy(a.PlanetControls!.Single(c=>c.PlanetId==planet.PlanetId));survey.ControlObservedTurn=game.TurnNumber;
                }
            }
        }
    }
}
