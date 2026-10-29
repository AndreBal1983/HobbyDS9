using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private void ValidateCycleState(GameStateDto game)
        {
            var actors=expansion!.CycleActors;Require(actors.Count==expansion.Plans.Count&&actors.Select(a=>a.ActorId).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(expansion.Plans.Select(a=>a.ActorId).OrderBy(x=>x,StringComparer.Ordinal)),"Exact cycle actor census required.");
            var all=new HashSet<string>(StringComparer.Ordinal);
            foreach(var a in actors)
            {
                Require(a.Receipts!=null&&a.NextOrdinal>=1&&a.Receipts.Count+(a.Active==null?0:1)==a.NextOrdinal-1,"Contiguous cycle ordinal census required.");
                var first=ExpansionPlan(a.ActorId);var firstCamp=colonisation!.State.CampSources!.SingleOrDefault(c=>c.CampId==first.CampId&&c.OwnerId==a.ActorId);
                string? bootstrap=firstCamp?.ManifestId;
                if(bootstrap==null)bootstrap=colonisation.State.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==first.ManifestId&&m.OwnerId==a.ActorId&&m.Kind==SettlerManifestKind.Settlement)?.ManifestId;
                if(bootstrap!=null)Require(all.Add(bootstrap),"Duplicate bootstrap manifest.");
                // Bootstrap may legitimately return/lose and retry before its first success.
                // The first retained cycle start permanently separates those real attempts.
                var cutoff=a.Receipts!.Count>0?a.Receipts[0].StartedTurn:a.Active?.StartedTurn??int.MaxValue;
                foreach(var prior in colonisation.State.ManifestRecords!.Where(m=>m.OwnerId==a.ActorId&&m.Kind==SettlerManifestKind.Settlement&&m.ManifestId!=bootstrap))
                {
                    var origin=colonisation.State.ManifestOrigins!.SingleOrDefault(o=>o.ManifestId==prior.ManifestId);
                    if(origin==null||origin.Turn>=cutoff)continue;
                    var failedCamp=colonisation.State.CampSources!.SingleOrDefault(c=>c.OwnerId==a.ActorId&&c.ManifestId==prior.ManifestId);
                    Require(colonisation.State.ManifestResolutions!.Any(v=>v.ManifestId==prior.ManifestId&&(v.Stage==SettlerManifestStage.Unloaded||v.Stage==SettlerManifestStage.Destroyed))
                        ||failedCamp!=null&&colonisation.State.CampTerminals!.Any(t=>t.CampId==failedCamp.CampId&&t.Kind!=SettlementTerminalKind.Established),"Historical bootstrap attempt needs actual failed/returned terminal.");
                    Require(all.Add(prior.ManifestId),"Aliased historical bootstrap manifest.");
                }
                for(var n=0;n<a.Receipts!.Count;n++)
                {
                    var x=a.Receipts[n];Require(x.Ordinal==n+1&&x.StartedTurn>=1&&x.StartedTurn<=x.TerminalTurn&&x.TerminalTurn<=x.ClosedTurn&&x.ClosedTurn<=game.TurnNumber
                        &&(n==0||x.StartedTurn>a.Receipts[n-1].ClosedTurn),"Invalid cycle terminal chronology.");
                    Require(all.Add(x.SettlementManifestId),"Aliased cycle manifest.");
                    var m=colonisation.State.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==x.SettlementManifestId&&m.OwnerId==a.ActorId&&m.Kind==SettlerManifestKind.Settlement);
                    Require(m!=null&&colonisation.State.ManifestOrigins!.Any(o=>o.ManifestId==m.ManifestId&&o.Turn>=x.StartedTurn),"Cycle terminal needs real committed manifest.");
                    var camp=colonisation.State.CampSources!.SingleOrDefault(c=>c.CampId==x.CampId&&c.ManifestId==x.SettlementManifestId&&c.OwnerId==a.ActorId);
                    if(camp!=null)Require(colonisation.State.ColonyOrigins!.Any(o=>o.ColonyId==camp.ColonyId&&o.ManifestId==x.SettlementManifestId&&o.PlanetId==x.TargetPlanetId),"Cycle camp target differs from committed target.");
                    var resolution=colonisation.State.ManifestResolutions!.SingleOrDefault(v=>v.ManifestId==x.SettlementManifestId);
                    if(x.Outcome==ColonisationCycleOutcome.Established)Require(camp!=null&&colonisation.State.CampTerminals!.Any(t=>t.CampId==camp.CampId&&t.Kind==SettlementTerminalKind.Established&&t.Turn==x.TerminalTurn),"Historical established cycle terminal required.");
                    else if(x.Outcome==ColonisationCycleOutcome.Returned)Require(resolution?.Stage==SettlerManifestStage.Unloaded&&resolution.Turn==x.TerminalTurn,"Returned cycle needs actual unload terminal.");
                    else Require(resolution?.Stage==SettlerManifestStage.Destroyed&&resolution.Turn==x.TerminalTurn||camp!=null&&colonisation.State.CampTerminals!.Any(t=>t.CampId==camp.CampId&&t.Kind!=SettlementTerminalKind.Established&&t.Turn==x.TerminalTurn),"Lost cycle needs actual destruction/empty terminal.");
                    Require(ColonisationPlanetsForActor(game,a.ActorId).Any(o=>o.PlanetId==x.TargetPlanetId&&o.Surveyed),"Cycle historical target needs own survey.");
                }
                Require(a.SupplyCampId==null||colonisation.State.CampSources!.Any(c=>c.CampId==a.SupplyCampId&&c.OwnerId==a.ActorId),"Foreign cycle supply commitment.");
                Require(!colonisation.State.ManifestRecords!.Any(m=>m.OwnerId==a.ActorId&&m.Kind==SettlerManifestKind.SupplyOnly&&m.Stage==SettlerManifestStage.Loaded)||a.SupplyCampId!=null,"Loaded cycle supply needs destination.");
                if(a.Active!=null)
                {
                    var m=a.Active;var p=m.Assignment;
                    Require(m.Ordinal==a.NextOrdinal-1&&m.Ordinal==a.Receipts.Count+1&&m.StartedTurn>=1&&m.StartedTurn<=game.TurnNumber
                        &&(a.Receipts.Count==0||m.StartedTurn>a.Receipts.Last().ClosedTurn),"Invalid active cycle chronology.");
                    Require((m.SettlementManifestId==null)==(m.CommittedTargetPlanetId==null),"Cycle manifest and intended target commit atomically.");
                    Require(p!=null&&p.ActorId==a.ActorId&&p.GoalId==CampaignColonisationCycleIdentity.Goal&&p.LatestActions!=null&&p.LatestActions.Count==0&&(int)p.Stage>=1&&(int)p.Stage<=8,"Invalid active cycle assignment.");
                    bool World(string? id)=>id==null||game.Colonies!.Any(w=>w.ColonyId==id&&w.OwnerCivilizationId==a.ActorId);
                    bool Ship(string? id)=>id==null||game.Ships!.Any(s=>s.ShipId==id&&s.OwnerCivilizationId==a.ActorId)||published!.Records.Any(s=>s.ShipId==id&&s.OwnerCivilizationId==a.ActorId)||published.Package.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Ships!.Any(s=>s.ShipId==id&&s.OwnerCivilizationId==a.ActorId);
                    bool Queue(string? id)=>id==null||colonisation.Production.Any(q=>q.QueueItemId==id&&q.OwnerId==a.ActorId)||colonisation.History.Any(h=>h.Order!.QueueItemId==id&&h.Order.OwnerId==a.ActorId);
                    Require(World(p!.HomeColonyId)&&World(p.YardColonyId)&&World(p.EstablishedColonyId)&&Ship(p.TransportShipId)&&Ship(p.ScoutShipId)&&Queue(p.TransportQueueId)&&Queue(p.KitQueueId),"Foreign active cycle assignment.");
                    Require(p.SourceCohortId==null||game.Colonies!.Where(w=>w.ColonyId==p.HomeColonyId&&w.OwnerCivilizationId==a.ActorId).SelectMany(w=>w.Cohorts!).Any(c=>c.CohortId==p.SourceCohortId)
                        ||colonisation.State.ManifestOrigins!.Any(o=>o.SourceColonyId==p.HomeColonyId&&o.SourceCohortId==p.SourceCohortId&&colonisation.State.ManifestRecords!.Any(v=>v.ManifestId==o.ManifestId&&v.OwnerId==a.ActorId)),"Foreign cycle cohort.");
                    Require((p.TargetPlanetId==null)==(p.TargetNodeId==null),"Incomplete cycle target.");
                    if(p.TargetPlanetId!=null)Require(ColonisationPlanetsForActor(game,a.ActorId).Any(o=>o.PlanetId==p.TargetPlanetId&&physicalNavigation!.NodeForSystem(o.SystemId)==p.TargetNodeId&&physicalNavigation.KnownNode(game,a.ActorId,p.TargetNodeId!)),"Unobserved cycle target.");
                    Require(p.OwnAttemptedTargets!=null&&p.OwnAttemptedTargets.Count<=expansion.Source.Policy.FailedTargets&&p.OwnAttemptedTargets.Distinct(StringComparer.Ordinal).Count()==p.OwnAttemptedTargets.Count
                        &&p.OwnAttemptedTargets.All(id=>colonisation.State.Surveys!.Any(s=>s.ActorId==a.ActorId&&s.PlanetId==id)),"Invalid cycle target memory.");
                    Require(p.BlueprintId==null?p.BlueprintRevision==0:game.Blueprints!.Any(b=>b.BlueprintId==p.BlueprintId&&b.Revision==p.BlueprintRevision&&b.OwnerCivilizationId==a.ActorId),"Foreign cycle blueprint.");
                    Require(p.ManifestId==null||colonisation.State.ManifestRecords!.Any(v=>v.ManifestId==p.ManifestId&&v.OwnerId==a.ActorId),"Foreign cycle cargo.");
                    Require(p.CampId==null||colonisation.State.CampSources!.Any(c=>c.CampId==p.CampId&&c.OwnerId==a.ActorId&&c.ManifestId==m.SettlementManifestId),"Foreign cycle camp.");
                    if(m.SettlementManifestId!=null)
                    {
                        Require(all.Add(m.SettlementManifestId)&&colonisation.State.ManifestRecords!.Any(v=>v.ManifestId==m.SettlementManifestId&&v.OwnerId==a.ActorId&&v.Kind==SettlerManifestKind.Settlement),"Invalid active cycle manifest.");
                        Require(ColonisationPlanetsForActor(game,a.ActorId).Any(o=>o.PlanetId==m.CommittedTargetPlanetId&&o.Surveyed),"Committed cycle target needs own survey.");
                        Require(!colonisation.State.DeploymentIntents!.Any(i=>i.ManifestId==m.SettlementManifestId&&i.PlanetId!=m.CommittedTargetPlanetId),"Cycle deploy differs from committed target.");
                    }
                    Require(p.Stage==ColonisationAiStage.Preparing||p.HomeColonyId!=null,"Cycle stage needs source.");
                    Require(p.Stage!=ColonisationAiStage.Assembling||p.TransportShipId!=null&&p.TargetPlanetId!=null,"Cycle assembly incomplete.");
                    Require(p.Stage!=ColonisationAiStage.Loaded&&p.Stage!=ColonisationAiStage.Deploying||m.SettlementManifestId!=null&&p.TransportShipId!=null,"Cycle voyage incomplete.");
                    Require(p.LastPlannedTurn>=0&&p.LastPlannedTurn<=game.TurnNumber&&p.DisputedBoundaries>=0&&p.DisputedBoundaries<=expansion.Source.Policy.DisputedWeeks,"Cycle work boundary invalid.");
                }
            }
            Require(colonisation!.State.ManifestRecords!.Where(m=>m.Kind==SettlerManifestKind.Settlement&&actors.Any(a=>a.ActorId==m.OwnerId)).All(m=>all.Contains(m.ManifestId)),"Every AI settlement belongs to bootstrap or exact cycle.");
        }
        private long CycleReceiptCharge()=>!CycleEnabled?0:expansion!.CycleActors.Sum(a=>(long)a.Receipts!.Count+(a.Active==null?0:1));
    }
}
