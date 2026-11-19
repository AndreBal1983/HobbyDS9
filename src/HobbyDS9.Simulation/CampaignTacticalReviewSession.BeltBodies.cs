using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private BeltBodyStateDto? beltBodies;
        private BeltBodyStateDto CreateBeltBodies()
        {
            var result=new BeltBodyStateDto();
            foreach(var belt in expansion!.Belts.OrderBy(b=>b.FeatureId,StringComparer.Ordinal))
            {
                var digest=CampaignActivitiesData.Identity(BeltBodiesEditionIdentity.Policy,generatedMap!.Settings.Map.Seed,belt.FeatureId);
                var amount=120+60*(int)(uint.Parse(digest.Substring(0,8),NumberStyles.HexNumber,CultureInfo.InvariantCulture)%5);
                var id="body."+belt.FeatureId;
                result.Definitions!.Add(new BeltBodyDefinitionDto{BodyId=id,FeatureId=belt.FeatureId,SystemId=belt.SystemId,DisplayName="Asteroid belt",HabitabilityBps=BeltMiningPolicy.NaturalHabitabilityBps,InitialDeposit=amount});
                result.Deposits!.Add(new BeltBodyDepositDto{BodyId=id,Remaining=amount});
            }
            return result;
        }
        private bool BeltSurveyBusy(string ship)=>beltBodies?.Surveys!.Any(s=>s.ShipId==ship&&s.Stage==BeltSurveyStage.Active)==true;
        private bool AnyBeltSurveyTaskBusy(GameStateDto game,string ship)=>BeltSurveyBusy(ship)
            ||colonisation!.State.Surveys!.Any(s=>s.ShipId==ship&&s.Stage==PlanetSurveyStage.Active)
            ||activities!.State.Findings!.Any(f=>f.ShipId==ship&&(f.Stage==ActivityProgressStage.Active||f.Stage==ActivityProgressStage.AwaitingClaim));
        private bool BeltCurrentlyObserved(GameStateDto game,string actor,string system)
        {
            var node=physicalNavigation!.NodeForSystem(system);
            return node!=null&&GeneratedCoverage(game,actor).Contains(node,StringComparer.Ordinal);
        }
        private BeltBodyError BeltHeader(BeltBodyCommandDto? c,ControllerKind controller)
        {
            if(beltBodies==null)return BeltBodyError.Unavailable;
            if(c==null||!Enum.IsDefined(typeof(BeltBodyAction),c.Action)||!EntityId.TryParse(c.CommandId,out _)
                ||c.CommandId.StartsWith("ai-command.",StringComparison.Ordinal)!=(controller==ControllerKind.AI))return BeltBodyError.InvalidCommand;
            var game=state.Campaign.GameState;
            if(Actor(c.ActorId,controller==ControllerKind.Human)!=CampaignTacticalError.None||game.Civilizations!.Single(a=>a.CivilizationId==c.ActorId).ControllerKind!=controller)return BeltBodyError.ActorNotControlled;
            if(CurrentPhase!=SessionPhase.Planning)return BeltBodyError.WrongPhase;
            if(c.ExpectedTurn!=game.TurnNumber)return BeltBodyError.StaleTurn;
            if(c.ExpectedRevision!=game.StateRevision)return BeltBodyError.StaleRevision;
            if(game.AcceptedCommandIds!.Contains(c.CommandId)||state.AcceptedRoundCommands.Contains(c.CommandId,StringComparer.Ordinal))return BeltBodyError.Duplicate;
            if(game.StateRevision==long.MaxValue||beltBodies.Operations!.Count>=BeltBodiesEditionIdentity.MaximumCommands)return BeltBodyError.Capacity;
            return BeltBodyError.None;
        }
        private BeltBodyError ValidateBeltCommand(BeltBodyCommandDto c,ControllerKind controller)
        {
            var header=BeltHeader(c,controller);if(header!=BeltBodyError.None)return header;
            var game=state.Campaign.GameState;var body=beltBodies!.Definitions!.SingleOrDefault(b=>b.BodyId==c.BodyId);
            // Undisclosed and absent bodies have the same failure, before any private facts.
            if(body==null||!ColonisationSystemsForActor(game,c.ActorId).Contains(body.SystemId))return BeltBodyError.UnknownBody;
            if(c.Action==BeltBodyAction.StartSurvey)
            {
                if(c.ShipId==null||c.ColonyId!=null||c.SurveyId!=null||c.MineId!=null)return BeltBodyError.InvalidCommand;
                if(beltBodies.Observations!.Any(o=>o.ActorId==c.ActorId&&o.BodyId==body.BodyId))return BeltBodyError.AlreadySurveyed;
                if(beltBodies.Surveys!.Count>=BeltBodiesEditionIdentity.MaximumSurveys||beltBodies.Observations!.Count>=BeltBodiesEditionIdentity.MaximumObservations)return BeltBodyError.Capacity;
                var ship=game.Ships!.SingleOrDefault(s=>s.ShipId==c.ShipId&&s.OwnerCivilizationId==c.ActorId);
                if(ship==null||!ShipAt(game,ship,body.SystemId))return BeltBodyError.ShipUnavailable;
                if(AnyBeltSurveyTaskBusy(game,ship.ShipId)||beltBodies.Surveys.Any(s=>s.ActorId==c.ActorId&&s.BodyId==body.BodyId&&s.Stage==BeltSurveyStage.Active))return BeltBodyError.ShipBusy;
                return (ActivityCapability(game,ship)?.StudyWork??0)>=1?BeltBodyError.None:BeltBodyError.StudyUnavailable;
            }
            if(c.Action==BeltBodyAction.CancelSurvey)
            {
                if(c.SurveyId==null||c.ShipId!=null||c.ColonyId!=null||c.MineId!=null)return BeltBodyError.InvalidCommand;
                return beltBodies.Surveys!.Any(s=>s.SurveyId==c.SurveyId&&s.ActorId==c.ActorId&&s.BodyId==c.BodyId&&s.Stage==BeltSurveyStage.Active)?BeltBodyError.None:BeltBodyError.SurveyUnavailable;
            }
            if(c.Action==BeltBodyAction.EnqueueMine)
            {
                if(c.ColonyId==null||c.ShipId!=null||c.SurveyId!=null||c.MineId!=null)return BeltBodyError.InvalidCommand;
                return ValidateBeltMine(c);
            }
            if(c.Action==BeltBodyAction.CancelMine)
            {
                if(c.MineId==null||c.ShipId!=null||c.ColonyId!=null||c.SurveyId!=null)return BeltBodyError.InvalidCommand;
                return ValidateCancelBeltMine(c);
            }
            return BeltBodyError.InvalidCommand;
        }
        private BeltBodyCommandResult BeltResult(BeltBodyError error,string? target=null)=>new BeltBodyCommandResult(error,state.Campaign.GameState.TurnNumber,state.Campaign.GameState.StateRevision,target);
        public BeltBodyCommandPreview PreviewBeltCommand(BeltBodyCommandDto command)
        {lock(gate)return new BeltBodyCommandPreview(ValidateBeltCommand(command,ControllerKind.Human),command?.Action??BeltBodyAction.StartSurvey);}
        public BeltBodyCommandResult SubmitBeltCommand(BeltBodyCommandDto command)=>ApplyBeltCommand(command,ControllerKind.Human);
        internal BeltBodyCommandResult SubmitBeltCommandForController(BeltBodyCommandDto command,ControllerKind controller)=>ApplyBeltCommand(command,controller);
        private BeltBodyCommandResult ApplyBeltCommand(BeltBodyCommandDto command,ControllerKind controller)
        {
            if(published!=null&&!publishedExecuting)
            {
                BeltBodyCommandResult? value=null;
                var result=PublishedMutation(c=>{value=c.ApplyBeltCommand(command,controller);return c.Result(value.Accepted?CampaignTacticalError.None:CampaignTacticalError.InvalidOrder);});
                return result.Accepted?value!:value!=null&&!value.Accepted?value:BeltResult(result.Error==CampaignTacticalError.Overflow?BeltBodyError.Capacity:BeltBodyError.InvalidCommand);
            }
            lock(gate)
            {
                var error=ValidateBeltCommand(command,controller);if(error!=BeltBodyError.None)return BeltResult(error);
                var c=BeltBodyCopies.Copy(command);var game=state.Campaign.GameState;string? target=null;
                if(c.Action==BeltBodyAction.StartSurvey)
                {
                    target=CampaignAllocation.Allocate(game,"belt-survey",null);
                    beltBodies!.Surveys!.Add(new BeltBodySurveyDto{SurveyId=target,ActorId=c.ActorId,ShipId=c.ShipId!,BodyId=c.BodyId,Stage=BeltSurveyStage.Active,StartedTurn=game.TurnNumber});
                }
                else if(c.Action==BeltBodyAction.CancelSurvey)
                {
                    var s=beltBodies!.Surveys!.Single(s=>s.SurveyId==c.SurveyId);s.Stage=BeltSurveyStage.Cancelled;s.CompletedWeeks=0;s.ResolvedTurn=game.TurnNumber;
                    game.EntityIdentities.RemoveAll(e=>e.EntityId==s.SurveyId);target=s.SurveyId;
                }
                else if(c.Action==BeltBodyAction.EnqueueMine)target=EnqueueBeltMine(c);
                else {CancelBeltMine(c);target=c.MineId;}
                game.AcceptedCommandIds!.Add(c.CommandId);game.StateRevision++;
                beltBodies!.Operations!.Add(new BeltBodyOperationDto{BaseJournalOffset=colonisation!.Journal.Count,Command=c});
                ValidateSave(state);return BeltResult(BeltBodyError.None,target);
            }
        }
        private void TickBeltSurveys(GameStateDto game)
        {
            if(beltBodies==null)return;
            foreach(var s in beltBodies.Surveys!.Where(s=>s.Stage==BeltSurveyStage.Active).OrderBy(s=>s.SurveyId,StringComparer.Ordinal))
            {
                var d=beltBodies.Definitions!.Single(b=>b.BodyId==s.BodyId);var ship=game.Ships!.SingleOrDefault(p=>p.ShipId==s.ShipId&&p.OwnerCivilizationId==s.ActorId);
                if(ship==null||!ShipAt(game,ship,d.SystemId)||(ActivityCapability(game,ship)?.StudyWork??0)<1)
                {s.Stage=BeltSurveyStage.Interrupted;s.CompletedWeeks=0;s.ResolvedTurn=game.TurnNumber;game.EntityIdentities.RemoveAll(e=>e.EntityId==s.SurveyId);continue;}
                if(++s.CompletedWeeks==BeltMiningPolicy.SurveyWeeks)
                {
                    s.Stage=BeltSurveyStage.Completed;s.ResolvedTurn=game.TurnNumber;game.EntityIdentities.RemoveAll(e=>e.EntityId==s.SurveyId);
                    beltBodies.Observations!.Add(new BeltBodyObservationDto{ActorId=s.ActorId,BodyId=s.BodyId,SurveyedTurn=game.TurnNumber,ObservedTurn=game.TurnNumber,ObservedRemaining=beltBodies.Deposits!.Single(d=>d.BodyId==s.BodyId).Remaining});
                }
            }
            RefreshBeltObservations(game);
        }
        private void RefreshBeltObservations(GameStateDto game)
        {
            if(beltBodies==null)return;
            foreach(var o in beltBodies.Observations!)
            {
                var d=beltBodies.Definitions!.Single(d=>d.BodyId==o.BodyId);
                if(!BeltCurrentlyObserved(game,o.ActorId,d.SystemId))continue;
                o.ObservedTurn=game.TurnNumber;o.ObservedRemaining=beltBodies.Deposits!.Single(p=>p.BodyId==o.BodyId).Remaining;
            }
        }
        public BeltBodiesSnapshot? CaptureBeltBodies(string actor)
        {
            lock(gate)
            {
                if(beltBodies==null)return null;
                Require(Actor(actor)==CampaignTacticalError.None,"Controlled belt observation required.");
                var game=state.Campaign.GameState;var systems=ColonisationSystemsForActor(game,actor);var rows=new List<BeltBodySnapshot>();
                foreach(var d in beltBodies.Definitions!.Where(d=>systems.Contains(d.SystemId)).OrderBy(d=>d.BodyId,StringComparer.Ordinal))
                {
                    var current=BeltCurrentlyObserved(game,actor,d.SystemId);var o=beltBodies.Observations!.SingleOrDefault(o=>o.ActorId==actor&&o.BodyId==d.BodyId);
                    // Current coverage can observe live ore even at AwaitingCombat,
                    // before the weekly survey-completion boundary. Queries stay pure.
                    if(current&&o!=null){o=BeltBodyCopies.Copy(o);o.ObservedTurn=game.TurnNumber;o.ObservedRemaining=beltBodies.Deposits!.Single(p=>p.BodyId==d.BodyId).Remaining;}
                    var survey=beltBodies.Surveys!.SingleOrDefault(s=>s.ActorId==actor&&s.BodyId==d.BodyId&&s.Stage==BeltSurveyStage.Active);
                    var m=beltBodies.Mines!.SingleOrDefault(m=>m.ActorId==actor&&m.BodyId==d.BodyId&&m.Stage!=BeltMineStage.Cancelled);
                    var q=m==null?null:colonisation!.Production.SingleOrDefault(q=>q.QueueItemId==m.QueueItemId);
                    var mine=m==null?null:new BeltMineSnapshot(m,beltBodies.Deposits!.Single(p=>p.BodyId==d.BodyId).LocalStock,q?.CompletedIndustry??BeltMiningPolicy.Industry);
                    var sponsors=game.Colonies!.Where(c=>c.OwnerCivilizationId==actor&&c.SystemId==d.SystemId&&EstablishedColony(c.ColonyId)&&PopulationRules.Population(c)>0).OrderBy(c=>c.ColonyId,StringComparer.Ordinal).Select(c=>c.ColonyId);
                    var ships=game.Ships!.Where(s=>s.OwnerCivilizationId==actor&&ShipAt(game,s,d.SystemId)&&!AnyBeltSurveyTaskBusy(game,s.ShipId)&&(ActivityCapability(game,s)?.StudyWork??0)>=1).OrderBy(s=>s.ShipId,StringComparer.Ordinal).Select(s=>s.ShipId);
                    rows.Add(new BeltBodySnapshot(d,current,o,survey,mine,sponsors,ships));
                }
                return new BeltBodiesSnapshot(actor,game.TurnNumber,game.StateRevision,rows);
            }
        }
        private void ValidateBeltBodies(GameStateDto game)
        {
            if(beltBodies==null)return;
            var b=beltBodies;Require(b.PolicyId==BeltBodiesEditionIdentity.Policy&&b.Definitions!=null&&b.Deposits!=null&&b.Observations!=null&&b.Surveys!=null&&b.Mines!=null&&b.LastShipments!=null&&b.Operations!=null,"Complete belt body state required.");
            Require(b.Definitions!.Count==expansion!.Belts.Count&&b.Definitions.Select(d=>d.BodyId).Distinct(StringComparer.Ordinal).Count()==b.Definitions.Count&&b.Deposits!.Count==b.Definitions.Count,"One distinct body/deposit per aggregate required.");
            foreach(var d in b.Definitions)
            {
                Require(d.BodyId=="body."+d.FeatureId&&expansion.Belts.Any(v=>v.FeatureId==d.FeatureId&&v.SystemId==d.SystemId)&&d.HabitabilityBps==BeltMiningPolicy.NaturalHabitabilityBps&&d.ResourceDefinitionId==BeltMiningPolicy.Resource&&d.InitialDeposit>=120&&d.InitialDeposit<=360&&d.InitialDeposit%60==0,"Invalid frozen belt definition.");
                var p=b.Deposits!.Single(p=>p.BodyId==d.BodyId);var mines=b.Mines!.Where(m=>m.BodyId==d.BodyId).ToList();
                Require(p.Remaining>=0&&p.Remaining<=d.InitialDeposit&&p.LocalStock>=0&&p.LocalStock<=BeltMiningPolicy.MineStorage
                    &&mines.Sum(m=>m.TotalExtracted)==d.InitialDeposit-p.Remaining&&mines.Sum(m=>m.TotalDelivered)+p.LocalStock==d.InitialDeposit-p.Remaining,"Finite belt material is not conserved.");
                Require(mines.Count(m=>m.Stage!=BeltMineStage.Cancelled)<=1,"One live installation per belt aggregate required.");
            }
            Require(b.Observations!.Select(o=>(o.ActorId,o.BodyId)).Distinct().Count()==b.Observations.Count,"Duplicate belt observations.");
            foreach(var o in b.Observations)
                Require(game.Civilizations!.Any(c=>c.CivilizationId==o.ActorId)&&b.Definitions.Any(d=>d.BodyId==o.BodyId)&&o.SurveyedTurn>0&&o.SurveyedTurn<=o.ObservedTurn&&o.ObservedTurn<=game.TurnNumber&&o.ObservedRemaining>=0&&o.ObservedRemaining<=b.Definitions.Single(d=>d.BodyId==o.BodyId).InitialDeposit,"Invalid belt observation.");
            Require(b.Surveys!.Select(s=>s.SurveyId).Distinct(StringComparer.Ordinal).Count()==b.Surveys.Count,"Duplicate belt survey identities.");
            foreach(var s in b.Surveys)
            {
                Require(b.Definitions.Any(d=>d.BodyId==s.BodyId)&&game.Civilizations!.Any(c=>c.CivilizationId==s.ActorId)&&Enum.IsDefined(typeof(BeltSurveyStage),s.Stage)&&s.CompletedWeeks>=0&&s.CompletedWeeks<=BeltMiningPolicy.SurveyWeeks,"Invalid belt survey.");
                Require(game.EntityIdentities.Any(e=>e.EntityId==s.SurveyId)==(s.Stage==BeltSurveyStage.Active),"Belt survey identity lifecycle mismatch.");
            }
            Require(b.Surveys.Where(s=>s.Stage==BeltSurveyStage.Active).Select(s=>s.ShipId).Distinct(StringComparer.Ordinal).Count()==b.Surveys.Count(s=>s.Stage==BeltSurveyStage.Active),"A ship cannot survey multiple belts.");
            ValidateBeltMiningState(game);
        }
    }
}
