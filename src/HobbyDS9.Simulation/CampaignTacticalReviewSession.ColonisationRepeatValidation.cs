using System;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private void ValidateRepeatState(GameStateDto game)
        {
            var goals=expansion!.RepeatGoals;Require(goals.Count==expansion.Plans.Count&&goals.Select(g=>g.ActorId).OrderBy(x=>x,StringComparer.Ordinal)
                .SequenceEqual(expansion.Plans.Select(g=>g.ActorId).OrderBy(x=>x,StringComparer.Ordinal)),"Exact repeat actor census required.");
            foreach(var goal in goals)
            {
                var p=goal.Assignment;Require(p!=null&&p.ActorId==goal.ActorId&&p.GoalId==CampaignColonisationRepeatIdentity.Goal&&p.LatestActions!=null&&p.LatestActions.Count==0
                    &&Enum.IsDefined(typeof(ColonisationAiStage),p.Stage)&&(int)p.Stage<=8&&p.HoldReason!=null&&p.HoldReason.Length<=128,"Invalid second assignment.");
                Require(Enum.IsDefined(typeof(ColonisationRepeatState),goal.State)&&(!goal.StartedTurn.HasValue||goal.StartedTurn>=1&&goal.StartedTurn<=game.TurnNumber)
                    &&(!goal.ClosedTurn.HasValue||goal.StartedTurn.HasValue&&goal.ClosedTurn>=goal.StartedTurn&&goal.ClosedTurn<=game.TurnNumber),"Invalid repeat lifecycle.");
                bool World(string? id)=>id==null||game.Colonies!.Any(c=>c.ColonyId==id&&c.OwnerCivilizationId==goal.ActorId);
                bool Ship(string? id)=>id==null||game.Ships!.Any(s=>s.ShipId==id&&s.OwnerCivilizationId==goal.ActorId)||published!.Records.Any(s=>s.ShipId==id&&s.OwnerCivilizationId==goal.ActorId)
                    ||published.Package.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Ships!.Any(s=>s.ShipId==id&&s.OwnerCivilizationId==goal.ActorId);
                bool Queue(string? id)=>id==null||colonisation!.Production.Any(q=>q.QueueItemId==id&&q.OwnerId==goal.ActorId)||colonisation!.History.Any(h=>h.Order!.QueueItemId==id&&h.Order.OwnerId==goal.ActorId);
                Require(World(p!.HomeColonyId)&&World(p.YardColonyId)&&World(p.EstablishedColonyId)&&Ship(p.ScoutShipId)&&Ship(p.TransportShipId)&&Queue(p.KitQueueId)&&Queue(p.TransportQueueId),"Foreign repeat assignment.");
                Require(p.SourceCohortId==null||game.Colonies!.Where(c=>c.ColonyId==p.HomeColonyId&&c.OwnerCivilizationId==goal.ActorId).SelectMany(c=>c.Cohorts!).Any(c=>c.CohortId==p.SourceCohortId)
                    ||colonisation!.State.ManifestOrigins!.Any(o=>o.SourceColonyId==p.HomeColonyId&&o.SourceCohortId==p.SourceCohortId&&colonisation.State.ManifestRecords!.Any(m=>m.ManifestId==o.ManifestId&&m.OwnerId==goal.ActorId)),"Foreign repeat cohort.");
                Require((p.TargetPlanetId==null)==(p.TargetNodeId==null),"Incomplete repeat target.");
                if(p.TargetPlanetId!=null){var observed=ColonisationPlanetsForActor(game,goal.ActorId).SingleOrDefault(o=>o.PlanetId==p.TargetPlanetId);Require(observed!=null&&physicalNavigation!.NodeForSystem(observed.SystemId)==p.TargetNodeId&&physicalNavigation.KnownNode(game,goal.ActorId,p.TargetNodeId!),"Unobserved repeat target.");}
                Require(p.OwnAttemptedTargets!=null&&p.OwnAttemptedTargets.Count<=expansion.Source.Policy.FailedTargets&&p.OwnAttemptedTargets.Distinct(StringComparer.Ordinal).Count()==p.OwnAttemptedTargets.Count
                    &&p.OwnAttemptedTargets.All(id=>colonisation!.State.Surveys!.Any(s=>s.ActorId==goal.ActorId&&s.PlanetId==id)),"Invalid repeat target memory.");
                Require(p.BlueprintId==null?p.BlueprintRevision==0:game.Blueprints!.Any(b=>b.BlueprintId==p.BlueprintId&&b.Revision==p.BlueprintRevision&&b.OwnerCivilizationId==goal.ActorId),"Foreign repeat blueprint.");
                Require(p.ManifestId==null||colonisation!.State.ManifestRecords!.Any(m=>m.ManifestId==p.ManifestId&&m.OwnerId==goal.ActorId),"Foreign repeat cargo.");
                Require(p.CampId==null||colonisation!.State.CampSources!.Any(c=>c.CampId==p.CampId&&c.OwnerId==goal.ActorId&&c.ManifestId==goal.SettlementManifestId),"Foreign repeat camp.");
                var manifest=goal.SettlementManifestId==null?null:colonisation!.State.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==goal.SettlementManifestId&&m.OwnerId==goal.ActorId&&m.Kind==SettlerManifestKind.Settlement);
                Require(goal.SettlementManifestId==null||manifest!=null,"Repeat needs its real Settlement manifest.");
                Require(goal.State!=ColonisationRepeatState.Waiting||goal.SettlementManifestId==null&&!goal.StartedTurn.HasValue&&!goal.ClosedTurn.HasValue,"Waiting repeat already committed.");
                Require(goal.State==ColonisationRepeatState.Waiting||goal.StartedTurn.HasValue,"Repeat needs start boundary.");
                Require(goal.State==ColonisationRepeatState.Waiting||goal.State==ColonisationRepeatState.Active?!goal.ClosedTurn.HasValue:goal.ClosedTurn.HasValue,"Repeat closure boundary.");
                if(goal.State==ColonisationRepeatState.Established)Require(manifest!=null&&p.EstablishedColonyId!=null&&colonisation!.State.CampSources!.Any(c=>c.ManifestId==manifest.ManifestId&&c.CampId==p.CampId&&c.ColonyId==p.EstablishedColonyId&&colonisation.State.CampTerminals!.Any(t=>t.CampId==c.CampId&&t.Kind==SettlementTerminalKind.Established)),"Repeat success needs actual establishment.");
                if(goal.State==ColonisationRepeatState.Aborted)Require(manifest?.Stage==SettlerManifestStage.Unloaded,"Aborted repeat needs actual returned settlers.");
                if(goal.State==ColonisationRepeatState.Lost)Require(manifest?.Stage==SettlerManifestStage.Destroyed||colonisation!.State.CampSources!.Any(c=>c.CampId==p.CampId&&c.ManifestId==goal.SettlementManifestId&&c.Stage==ColonyEstablishmentStage.EmptyOutpost),"Lost repeat needs actual cargo/camp loss.");
                Require(!colonisation!.State.ManifestRecords!.Any(m=>m.OwnerId==goal.ActorId&&m.Kind==SettlerManifestKind.SupplyOnly&&m.Stage==SettlerManifestStage.Loaded)||goal.SupplyCampId!=null,"Loaded supply needs retained destination.");
                Require(goal.SupplyCampId==null||colonisation!.State.CampSources!.Any(c=>c.CampId==goal.SupplyCampId&&c.OwnerId==goal.ActorId),"Foreign supply commitment.");
                if(goal.State==ColonisationRepeatState.Waiting)Require(p.Stage==ColonisationAiStage.Preparing&&p.HomeColonyId==null&&p.YardColonyId==null&&p.TransportShipId==null&&p.ScoutShipId==null&&p.ManifestId==null&&p.CampId==null&&p.KitQueueId==null&&p.TransportQueueId==null,"Waiting repeat carries no second reservations.");
                Require(p.Stage==ColonisationAiStage.Preparing||p.HomeColonyId!=null,"Repeat stage needs real source.");
                Require(p.Stage!=ColonisationAiStage.Assembling||p.TransportShipId!=null&&p.TargetPlanetId!=null,"Incomplete second assembly.");
                Require(p.Stage!=ColonisationAiStage.Loaded&&p.Stage!=ColonisationAiStage.Deploying||goal.SettlementManifestId!=null&&p.TargetPlanetId!=null&&p.TransportShipId!=null,"Incomplete second voyage.");
                Require(p.LastPlannedTurn>=0&&p.LastPlannedTurn<=game.TurnNumber&&p.DisputedBoundaries>=0&&p.DisputedBoundaries<=expansion.Source.Policy.DisputedWeeks,"Invalid repeat work boundary.");
            }
        }
    }
}
