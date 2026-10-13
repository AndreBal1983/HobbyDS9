using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        internal Func<StrategicTurnPhase,bool>? PhysicalPhaseProbe { get; set; }
        internal string? LastPhysicalFailure { get; private set; }
        public static CampaignTacticalReviewSession StartTravel(CampaignTacticalTravelPackageDto package)
        {
            CampaignTacticalTravelData.ValidatePackage(package);
            return new CampaignTacticalReviewSession(package.CorePackage!,null,package,null);
        }
        public static CampaignTacticalReviewSession LoadTravel(CampaignTacticalTravelPackageDto package,CampaignTacticalTravelSaveDto save)
        {
            CampaignTacticalTravelData.ValidatePackage(package);RequireTravelShape(save);
            return new CampaignTacticalReviewSession(package.CorePackage!,save.Core,package,save);
        }
        private CampaignTacticalTravelSaveDto TravelCopy()
        {
            Require(travel!=null,"Session has no physical profile.");
            var copy=CampaignTacticalTravelData.Copy(travel);copy.Core=Copy(state);return copy;
        }
        public CampaignTacticalTravelSaveDto ExportTravelSave(){lock(gate)return TravelCopy();}
        public IReadOnlyList<CampaignKnownContact> KnownContacts(string actor)
        {
            lock(gate)
            {
                Require(travel!=null&&Actor(actor)==CampaignTacticalError.None,"Only a controlled human actor can inspect contact knowledge.");
                var turn=state.Campaign.GameState.TurnNumber;
                return travel.Contacts.Where(k=>k.ActorId==actor).OrderBy(k=>k.FleetId,StringComparer.Ordinal)
                    .Select(k=>new CampaignKnownContact(k,(long)turn-k.LastSeenTurn<=contactFreshness)).ToList().AsReadOnly();
            }
        }
        public IReadOnlyList<CampaignOwnTravelReceipt> OwnTravelReceipts(string actor)
        {
            lock(gate)
            {
                Require(travel!=null&&Actor(actor)==CampaignTacticalError.None,"Only a controlled human actor can inspect movement receipts.");
                return travel.LastMovement.Where(r=>r.OwnerId==actor).Select(r=>new CampaignOwnTravelReceipt(r)).ToList().AsReadOnly();
            }
        }
        public IReadOnlyList<CampaignOwnTravelOrder> OwnTravelOrders(string actor)
        {
            lock(gate)
            {
                Require(travel!=null&&Actor(actor)==CampaignTacticalError.None,"Only a controlled human actor can inspect travel orders.");
                return travel.Orders.Where(o=>state.Campaign.GameState.Fleets!.Any(f=>f.FleetId==o.FleetId&&f.OwnerCivilizationId==actor))
                    .Select(o=>new CampaignOwnTravelOrder(o)).ToList().AsReadOnly();
            }
        }
        public StrategicRouteSnapshot FindKnownTravelRoute(string actor,string origin,string destination)
        {
            lock(gate)
            {
                Require(travel!=null&&Actor(actor)==CampaignTacticalError.None,"Only a controlled human actor can query known routes.");
                return new WeightedNavigationGraph(definitions).FindRoute(state.Campaign.GameState,actor,origin,destination);
            }
        }
        private bool ValidTravelOrder(GameStateDto game,CampaignTravelOrderDto? order,IReadOnlyList<CampaignContactKnowledgeDto> contacts,bool allowStale=false)
        {
            if(order==null||order.RouteNodeIds==null||!Enum.IsDefined(typeof(CampaignTravelIntent),order.Intent))return false;
            var fleet=game.Fleets!.SingleOrDefault(f=>f.FleetId==order.FleetId);if(fleet==null)return false;
            var graph=new WeightedNavigationGraph(definitions);
            if(fleet.TransitTotalCost>0&&!order.RouteNodeIds.SequenceEqual(fleet.RouteNodeIds!))return false;
            if(!graph.ValidateRoute(game,fleet.OwnerCivilizationId,fleet.LocationNodeId,order.RouteNodeIds,
                fleet.TransitTotalCost>0?fleet.TransitTotalCost-fleet.TransitProgress:0))return false;
            if(order.Intent==CampaignTravelIntent.Stay||order.Intent==CampaignTravelIntent.StayEngage)
            { if(fleet.TransitTotalCost>0||order.RouteNodeIds.Count!=0)return false; }
            if(order.Intent==CampaignTravelIntent.Leave&&order.RouteNodeIds.Count==0)return false;
            if(order.Intent!=CampaignTravelIntent.Follow)return order.ContactFleetId==null&&order.ExpectedContactSeenTurn==null;
            var known=contacts.SingleOrDefault(k=>k.ActorId==fleet.OwnerCivilizationId&&k.FleetId==order.ContactFleetId);
            return order.ContactFleetId!=null&&authoredFleetOwners!.TryGetValue(order.ContactFleetId,out var targetOwner)
                &&CampaignPhysicalMovement.Hostile(declaredHostilities!,fleet.OwnerCivilizationId,targetOwner)
                &&known!=null&&known.LastSeenTurn==order.ExpectedContactSeenTurn&&(allowStale||(long)game.TurnNumber-known.LastSeenTurn<=contactFreshness)
                &&(order.RouteNodeIds.Count>0||known.TrackingNodeIds.Contains(fleet.LocationNodeId,StringComparer.Ordinal));
        }
        public CampaignTacticalMutationResult SubmitTravelOrder(SubmitCampaignTravelOrderDto? command)
        {
            lock(gate)
            {
                if(travel==null||command==null||command.Order==null)return Result(CampaignTacticalError.InvalidOrder);
                var game=state.Campaign.GameState;var actor=Actor(command.ActorId);if(actor!=CampaignTacticalError.None)return Result(actor);
                if(CurrentPhase!=SessionPhase.Planning)return Result(CampaignTacticalError.WrongPhase);
                if(command.ExpectedTurn!=game.TurnNumber)return Result(CampaignTacticalError.StaleTurn);
                if(!EntityId.TryParse(command.CommandId,out _)||command.CommandId.StartsWith("ai-command.",StringComparison.Ordinal))return Result(CampaignTacticalError.InvalidOrder);
                if(state.AcceptedRoundCommands.Contains(command.CommandId,StringComparer.Ordinal)||game.AcceptedCommandIds!.Contains(command.CommandId))return Result(CampaignTacticalError.DuplicateCommand);
                if(command.ExpectedRevision!=game.StateRevision)return Result(CampaignTacticalError.StaleRevision);
                if(!game.Fleets!.Any(f=>f.FleetId==command.Order.FleetId&&f.OwnerCivilizationId==command.ActorId)
                    ||!ValidTravelOrder(game,command.Order,travel.Contacts))return Result(CampaignTacticalError.InvalidOrder);
                if(game.StateRevision==long.MaxValue)return Result(CampaignTacticalError.Overflow);
                var next=TravelCopy();var order=CampaignTacticalTravelData.Copy(command.Order);
                next.Orders[next.Orders.FindIndex(o=>o.FleetId==order.FleetId)]=order;
                next.Core.Campaign.GameState.Fleets!.Single(f=>f.FleetId==order.FleetId).RouteNodeIds=new List<string>(order.RouteNodeIds);
                next.Core.AcceptedRoundCommands.Add(command.CommandId);next.Core.Campaign.GameState.StateRevision++;
                ValidateTravelSave(next);travel=CampaignTacticalTravelData.Copy(next);state=travel.Core;return Result(CampaignTacticalError.None);
            }
        }
        private List<CombatRequestDto> FreezePhysical(GameStateDto game,PhysicalMovementResult movement)
        {
            var random=new SplitMix64Random(game.RngState.State!.Value);var allocator=new EntityIdAllocator(game.NextEntitySequence);
            var requests=new List<CombatRequestDto>();
            foreach(var meeting in movement.Meetings.OrderBy(m=>m.Time).ThenBy(m=>m.Node,StringComparer.Ordinal)
                .ThenBy(m=>m.Fleets.OrderBy(id=>id,StringComparer.Ordinal).First(),StringComparer.Ordinal))
            {
                Require(allocator.NextEntitySequence<ulong.MaxValue,"Encounter allocation overflow.");
                var request=new CombatRequestDto { EncounterId=allocator.Allocate("encounter").Value,TurnNumber=game.TurnNumber,LocationNodeId=meeting.Node,
                    ContentHash=compatibility.ContentHash,RulesVersion=compatibility.RulesVersion,ResolverId=CampaignCombatRules.ResolverId,
                    ResolverVersion=CampaignCombatRules.ResolverVersion,RandomAlgorithmId=EncounterRandomStream.AlgorithmId,RandomState=random.NextUInt64() };
                foreach(var fleet in game.Fleets!.Where(f=>meeting.Fleets.Contains(f.FleetId)).OrderBy(f=>f.FleetId,StringComparer.Ordinal))
                    request.Participants.Add(new CombatParticipantDto { CivilizationId=fleet.OwnerCivilizationId,FleetId=fleet.FleetId,
                        Ships=fleet.ShipIds!.OrderBy(id=>id,StringComparer.Ordinal).Select(id=>{
                            var ship=game.Ships!.Single(s=>s.ShipId==id);var design=CampaignShipDesignResolver.Ship(game,definitions,ship);
                            return new CombatShipDto { ShipId=id,DesignId=design.DefinitionId,Hull=ship.CurrentHull,MaxHull=design.MaxHull,
                                CombatProfile=CombatProfiles.Copy(design.CombatProfile!) };
                        }).ToList() });
                CombatEncounterScaffold.ValidateRequest(request,true);requests.Add(CombatEncounterScaffold.CopyRequest(request));
                foreach(var receipt in movement.Receipts.Where(r=>meeting.Fleets.Contains(r.FleetId)))receipt.EncounterId=request.EncounterId;
            }
            game.RngState.State=random.State;game.NextEntitySequence=allocator.NextEntitySequence;return requests;
        }
        private static void RefreshPhysicalContacts(IEnumerable<CombatRequestDto> requests,int turn,List<CampaignContactKnowledgeDto> contacts)
        {
            foreach(var request in requests)
            foreach(var actor in request.Participants.Select(p=>p.CivilizationId).Distinct(StringComparer.Ordinal))
            foreach(var participant in request.Participants.Where(p=>p.CivilizationId!=actor))
            {
                var known=contacts.SingleOrDefault(k=>k.ActorId==actor&&k.FleetId==participant.FleetId);
                if(known!=null){known.LastSeenNodeId=request.LocationNodeId;known.LastSeenTurn=turn;}
            }
        }
        private CampaignTacticalMutationResult AdvancePhysicalTurn(int turn,long revision)
        {
            if(CurrentPhase!=SessionPhase.Planning)return Result(CampaignTacticalError.WrongPhase);
            if(turn!=state.Campaign.GameState.TurnNumber)return Result(CampaignTacticalError.StaleTurn);
            if(revision!=state.Campaign.GameState.StateRevision)return Result(CampaignTacticalError.StaleRevision);
            if(turn==int.MaxValue||revision==long.MaxValue)return Result(CampaignTacticalError.Overflow);
            var next=TravelCopy();
            try
            {
                void Probe(StrategicTurnPhase phase){if(PhysicalPhaseProbe!=null&&!PhysicalPhaseProbe(phase))throw new InvalidOperationException("Injected phase failure.");}
                Probe(StrategicTurnPhase.AiPlanning);
                var beforeAi=SimulationSession.CopyState(next.Core.Campaign.GameState);
                next.Core.Campaign.GameState=Legacy(next.Core).RunPlanningAiForPhysicalReview();
                var game=next.Core.Campaign.GameState;
                foreach(var order in next.Orders)
                {
                    var f=game.Fleets!.Single(f=>f.FleetId==order.FleetId);
                    if(controllers[f.OwnerCivilizationId]==ControllerKind.AI&&!f.RouteNodeIds!.SequenceEqual(beforeAi.Fleets!.Single(p=>p.FleetId==f.FleetId).RouteNodeIds!))
                    {order.Intent=f.RouteNodeIds!.Count>0?CampaignTravelIntent.Leave:CampaignTravelIntent.Stay;order.ContactFleetId=null;order.ExpectedContactSeenTurn=null;order.RouteNodeIds=new List<string>(f.RouteNodeIds);}
                }
                void Phase(StrategicTurnPhase phase,Action action)
                {
                    Probe(phase);var before=SimulationSession.CopyState(game);action();
                    FoundationValidator.ValidateGameState(game,definitions).ThrowIfInvalid(nameof(game));
                    SimulationSession.RequirePhaseChanges(before,SimulationSession.CopyState(game),allowEconomy:phase==StrategicTurnPhase.Economy,
                        allowResearch:phase==StrategicTurnPhase.Research,allowProduction:phase==StrategicTurnPhase.Production);
                }
                Phase(StrategicTurnPhase.Economy,()=>EconomyPhase.Execute(game,definitions));
                Phase(StrategicTurnPhase.Research,()=>ResearchPhase.Execute(game,definitions));
                Phase(StrategicTurnPhase.Production,()=>ProductionPhase.Execute(game,definitions));
                if(game.Ships!.Any(s=>!next.Core.Pools.Any(p=>p.ShipId==s.ShipId)))return Result(CampaignTacticalError.UnsupportedSource);
                Probe(StrategicTurnPhase.Movement);
                next.MovementProof=new CampaignPhysicalMovementProofDto { Start=SimulationSession.CopyState(game),
                    Orders=next.Orders.Select(CampaignTacticalTravelData.Copy).ToList(),Contacts=next.Contacts.Select(CampaignTacticalTravelData.Copy).ToList() };
                var movement=CampaignPhysicalMovement.Execute(game,definitions,next.Orders,next.Contacts,declaredHostilities!,contactFreshness);
                var movementGuard=SimulationSession.CopyState(game);
                foreach(var receipt in movement.Receipts.Where(r=>r.StoppedByEncounter))
                    movementGuard.Fleets!.Single(f=>f.FleetId==receipt.FleetId).RouteNodeIds=next.MovementProof.Start.Fleets!
                        .Single(f=>f.FleetId==receipt.FleetId).RouteNodeIds!.Skip((int)receipt.EdgesCompleted).ToList();
                SimulationSession.RequirePhaseChanges(next.MovementProof.Start,movementGuard,allowEconomy:false,allowMovement:true);
                Probe(StrategicTurnPhase.EncounterCreation);next.Requests=FreezePhysical(game,movement);next.LastMovement=movement.Receipts;
                foreach(var o in next.Orders)
                {
                    var f=game.Fleets!.Single(f=>f.FleetId==o.FleetId);o.RouteNodeIds=new List<string>(f.RouteNodeIds!);
                    if(movement.Receipts.Any(r=>r.FleetId==o.FleetId&&r.StoppedByEncounter)||o.RouteNodeIds.Count==0&&o.Intent==CampaignTravelIntent.Leave)
                    {o.Intent=CampaignTravelIntent.Stay;o.ContactFleetId=null;o.ExpectedContactSeenTurn=null;}
                }
                foreach(var request in next.Requests)
                {
                    next.Core.Battles.Add(Allocate(next.Core,request));
                }
                RefreshPhysicalContacts(next.Requests,turn,next.Contacts);
                next.Phase=next.Requests.Count>0?SessionPhase.AwaitingCombat:SessionPhase.Planning;
                game.StateRevision=checked(game.StateRevision+1);
                if(next.Phase==SessionPhase.Planning){game.TurnNumber++;game.AcceptedCommandIds!.Clear();}
                foreach(var b in next.Core.Battles)
                {
                    EnsureAi(next.Core,b);
                    while(b.State.Completion==TacticalCompletion.None&&b.State.Formations.Where(f=>f.Status==TacticalFormationStatus.Active).All(f=>controllers[f.SideId]==ControllerKind.AI))
                    {game.StateRevision=checked(game.StateRevision+1);ResolveJoint(next.Core,b);}
                }
                ValidateTravelSave(next);travel=CampaignTacticalTravelData.Copy(next);state=travel.Core;return Result(CampaignTacticalError.None);
            }
            catch(OverflowException){return Result(CampaignTacticalError.Overflow);}
            catch(Exception e)when(e is ArgumentException||e is InvalidOperationException){LastPhysicalFailure=e.ToString();return Result(CampaignTacticalError.InvalidBatch);}
        }
    }
}
