using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        // Called only on the private, canonically synchronized activity candidate. This
        // policy owns positive-only infrastructure changes; the older fixed-world proof
        // is deliberately never used for this profile.
        private bool ActivityCapacity(int proposedDelta)
        {
            if(colonisation!=null)return ColonisationCapacity(proposedDelta);
            if(Streams!=null)return StreamedCapacity(proposedDelta);
            var game=state.Campaign.GameState;var ctx=activities!;
            bool Fail(string reason){LastPhysicalFailure=reason;return false;}
            if(proposedDelta<0||proposedDelta>1||playableOperationLimit<1||playableOperationLimit>65536)return Fail("Activity journal domain.");
            BigInteger work=0,horizon=0;
            var records=(BigInteger)BuildRegistry.Count()+published!.Records.Count+ctx.State.Production!.Count+ctx.State.Structures!.Count+ctx.State.Findings!.Count;
            if(records>published.RecordBudget)return Fail("Activity retained-record capacity.");
            foreach(var colony in game.Colonies!.OrderBy(c=>c.ColonyId,StringComparer.Ordinal))
            {
                var queue=ctx.State.Production.Where(q=>q.ColonyId==colony.ColonyId).ToList();
                if(queue.Count==0)continue;
                long floor=long.MaxValue,food=0;
                var planet=definitions.Planets!.Single(p=>p.DefinitionId==game.Planets!.Single(p=>p.PlanetId==colony.PlanetId).DefinitionId);
                var remaining=ctx.Content.Structures!.Where(s=>!colony.Infrastructure!.Any(i=>i.DefinitionId==s.DefinitionId&&i.Count>0)).ToList();
                for(int mask=0;mask<(1<<remaining.Count);mask++)
                foreach(var policy in definitions.GovernancePolicies!)
                foreach(ResistanceState resistance in Enum.GetValues(typeof(ResistanceState)))
                foreach(int disruption in new[]{0,1})
                {
                    // Conservative aggregate projection bound includes actual cohort/
                    // habitat/species cross-products, not merely the outer loop count.
                    work+=64+game.Planets!.Count+definitions.Planets!.Count+(BigInteger)(colony.Cohorts!.Count+1)*(definitions.Species!.Count+1)*(planet.Habitats!.Count+1)*16
                        +(BigInteger)(colony.Cohorts.Count+1)*(colony.Cohorts.Count+1)*(planet.Habitats.Count+1)*8
                        +(BigInteger)(colony.Infrastructure!.Count+4)*(definitions.Infrastructure!.Count+4)
                        +colony.Cohorts.Sum(c=>(long)c.Memories!.Count)+colony.Stockpiles!.Count+colony.Politics!.Events!.Count
                        +colony.Politics.CohortReports!.Sum(r=>(long)r.Reasons!.Count);
                    if(work>80000000)return Fail("Activity projection work budget.");
                    var copy=PopulationData.Copy(colony);
                    for(int i=0;i<remaining.Count;i++)if((mask&(1<<i))!=0)copy.Infrastructure!.Add(new InfrastructureStateDto{DefinitionId=remaining[i].DefinitionId,Count=1});
                    copy.Politics!.GovernancePolicyDefinitionId=policy.DefinitionId;copy.Politics.Resistance=resistance;copy.Politics.DisruptionRemainingTurns=disruption;
                    var projected=WorldEconomy.Project(copy,game,definitions);floor=Math.Min(floor,projected.Industry);food=Math.Max(food,projected.FoodOutput);
                }
                if(floor<=0||floor==long.MaxValue)return Fail("No positive Industry floor across allowed structure outcomes.");
                BigInteger turns=0;
                foreach(var item in queue)
                {var q=item.Paid!;var remainingWork=q.RequiredIndustry-q.CompletedIndustry;if(remainingWork<=0)return Fail("Invalid pending mixed work.");
                    turns+=Math.Max(1,remainingWork/floor+(remainingWork%floor==0?0:1));}
                horizon=BigInteger.Max(horizon,turns);
                if((BigInteger)colony.Food+turns*food>long.MaxValue)return Fail("Queued-world food arithmetic headroom.");
            }
            var active=ctx.State.Findings!.Where(f=>f.Stage==ActivityProgressStage.Active).ToList();
            if(active.Count>0)horizon=BigInteger.Max(horizon,active.Max(f=>f.RemainingAdvances));
            var decisions=ctx.State.Findings.Count(f=>f.Stage==ActivityProgressStage.Active||f.Stage==ActivityProgressStage.AwaitingClaim);
            var potential=(BigInteger)game.Ships!.Count+ctx.State.Production.Count(q=>q.Kind==ActivityProductionKind.PublishedShip);
            var batch=2+3*(potential/2);
            var debt=CurrentPhase==SessionPhase.AwaitingCombat?PlayableAdvanceCapacity.RequiredSlots(travel!,controllers)-1:0;
            var reserve=horizon*batch+debt+decisions;
            if((BigInteger)UsedPlayableOperations+proposedDelta+reserve>playableOperationLimit)return Fail("Activity journal reserve does not fit.");
            var futureRevisions=(playableOperationLimit-UsedPlayableOperations)+horizon*(game.Colonies!.Count+potential+3*(potential/2)+2)+debt+decisions;
            if((BigInteger)game.TurnNumber+horizon+1>int.MaxValue||(BigInteger)game.StateRevision+futureRevisions>long.MaxValue
                ||(BigInteger)game.NextEntitySequence+2*potential+horizon*(potential+1+PopulationRules.CohortCeiling*(BigInteger)game.Colonies!.Count*game.Colonies!.Count)
                    +(BigInteger)playableOperationLimit*(game.Colonies!.Count+1)>ulong.MaxValue)return Fail("Activity arithmetic headroom.");
            return true;
        }

        private void ValidateActivityState()
        {
            var ctx=activities!;var game=state.Campaign.GameState;var a=ctx.State;
            if(colonisation!=null){ValidateColonisationRootBudget();ValidateColonisationState(game);WarmColonisationViews(game);}
            Require(a.Production!=null&&a.Structures!=null&&a.Findings!=null&&a.Sites!=null&&a.Minors!=null&&a.Bodies!=null&&a.Relations!=null&&a.Observations!=null,"Complete activity state.");
            Require(game.Colonies!.All(c=>c.ProductionQueue!.Count==0),"Mixed production is the sole published queue.");
            if(colonisation!=null)ValidateColonisationProduction(game);
            else if(Streams!=null)ValidateStreamedProduction(game);
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var entry in a.Production!)
            {
                var colony=game.Colonies.Single(c=>c.ColonyId==entry.ColonyId);var q=entry.Paid!;
                Require(q!=null&&ids.Add(q.QueueItemId)&&EntityId.TryParse(q.QueueItemId,out var id)&&id.TryGetAllocationSequence(out var sequence)&&sequence<game.NextEntitySequence
                    &&game.EntityIdentities.Any(e=>e.EntityId==q.QueueItemId)&&q.RequiredIndustry>0&&q.CompletedIndustry>=0&&q.CompletedIndustry<q.RequiredIndustry,"Actual mixed queue identity/work.");
                if(entry.Kind==ActivityProductionKind.PublishedShip)
                {
                    Require(q.Blueprint!=null&&q.ShipDesignDefinitionId==""&&q.Blueprint.BlueprintId==entry.DefinitionId,"Published queue source.");
                    var record=CampaignBlueprints.Find(game,q.Blueprint!);
                    Require(record!=null&&record.OwnerCivilizationId==colony.OwnerCivilizationId&&PublishedFit(game,q.Blueprint!,false).IsSupportedByCompiler
                        &&q.RequiredIndustry==record.CompiledDesign!.IndustryCost&&q.PaidCredits==record.CompiledDesign.CreditCost
                        &&FrontierSame(q.PaidResources,record.CompiledDesign.ResourceCosts),"Frozen mixed ship payment/source.");
                }
                else
                {
                    var source=ctx.Content.Structures!.Single(s=>s.DefinitionId==entry.DefinitionId);
                    Require(entry.Kind==ActivityProductionKind.Infrastructure&&q.Blueprint==null&&q.ShipDesignDefinitionId==""&&q.RequiredIndustry==source.Cost!.Industry
                        &&q.PaidCredits==source.Cost.Credits&&FrontierSame(q.PaidResources,source.Cost.Resources),"Structure payment/source.");
                    Require(a.Production.Count(p=>p.ColonyId==entry.ColonyId&&p.DefinitionId==entry.DefinitionId)==1&&!colony.Infrastructure!.Any(i=>i.DefinitionId==entry.DefinitionId&&i.Count>0),"Unique pending structure.");
                }
            }
            foreach(var receipt in a.Structures!)
            {
                var colony=game.Colonies.Single(c=>c.ColonyId==receipt.ColonyId);
                Require(ids.Add(receipt.QueueItemId)&&receipt.OwnerId==colony.OwnerCivilizationId&&receipt.CompletedTurn>=initialTurn&&receipt.CompletedTurn<=game.TurnNumber
                    &&(ctx.Content.Structures!.Any(s=>s.DefinitionId==receipt.DefinitionId)||colonisation!=null&&ColonisationStructureCost(receipt.DefinitionId)!=null)&&colony.Infrastructure!.Single(i=>i.DefinitionId==receipt.DefinitionId).Count==1,"Completed structure receipt.");
            }
            var original=published!.Package.Integrated!.Physical!.CorePackage!.Campaign!.InitialState;
            if(colonisation!=null)ValidateColonisationWorlds(game,original);
            else foreach(var colony in game.Colonies!)
            {
                var first=original.Colonies!.Single(c=>c.ColonyId==colony.ColonyId);
                Require(colony.OwnerCivilizationId==first.OwnerCivilizationId&&colony.Automation==first.Automation&&colony.SystemId==first.SystemId&&colony.PlanetId==first.PlanetId,"Activity fixed world identity.");
                Require(FrontierSame(colony.Infrastructure!.Where(i=>!(ctx.Content.Structures!.Any(s=>s.DefinitionId==i.DefinitionId)||colonisation!=null&&ColonisationStructureCost(i.DefinitionId)!=null)).ToList(),first.Infrastructure)
                    &&colony.Infrastructure.Count(i=>ctx.Content.Structures!.Any(s=>s.DefinitionId==i.DefinitionId)||colonisation!=null&&ColonisationStructureCost(i.DefinitionId)!=null)==a.Structures.Count(s=>s.ColonyId==colony.ColonyId),"Only receipted infrastructure additions.");
            }
            Require(a.Sites!.Count==6&&a.Minors!.Count==2&&a.Bodies!.Count==6
                &&a.Sites.Select(s=>s.SiteId).Distinct(StringComparer.Ordinal).Count()==6
                &&a.Sites.All(s=>generatedMap!.Layout.Systems.Any(n=>n.SystemId==s.SystemId&&n.NodeId==s.NodeId)
                    &&a.Bodies.Single(b=>b.BodyId==s.BodyId).SystemId==s.SystemId),"Activity body/site identity.");
            Require(a.Observations!.Select(o=>(o.ActorId,o.SiteId)).Distinct().Count()==a.Observations.Count
                &&a.Observations.All(o=>controllers.ContainsKey(o.ActorId)&&a.Sites.Any(s=>s.SiteId==o.SiteId)&&o.FirstObservedTurn>=initialTurn&&o.FirstObservedTurn<=game.TurnNumber),"Actor site observations.");
            Require(a.Relations!.Select(r=>(r.ActorId,r.MinorActorId)).Distinct().Count()==a.Relations.Count
                &&a.Relations.All(r=>controllers.ContainsKey(r.ActorId)&&a.Minors!.Any(m=>m.ActorId==r.MinorActorId)&&Enum.IsDefined(typeof(ActivityMinorRelation),r.Relation)
                    &&r.ChangedTurn>=initialTurn&&r.ChangedTurn<=game.TurnNumber),"Actor minor relations.");
            Require(a.Findings!.Select(f=>(f.ActorId,f.SiteId,f.FindingId)).Distinct().Count()==a.Findings.Count,"Finite finding budget identity.");
            foreach(var finding in a.Findings)
            {
                var site=a.Sites.Single(s=>s.SiteId==finding.SiteId);var choice=ActivityDefinition(site.DefinitionId).Choices!.Single(c=>c.ChoiceId==finding.ChoiceId);
                Require(controllers.ContainsKey(finding.ActorId)&&a.Observations.Any(o=>o.ActorId==finding.ActorId&&o.SiteId==site.SiteId)
                    &&finding.FindingId==choice.FindingId&&finding.FindingBudget==choice.Insight&&finding.PaidCredits==choice.Credits
                    &&finding.WorkDone>=0&&finding.WorkDone<=choice.Work+4&&finding.RemainingAdvances>=0&&finding.RemainingAdvances<=choice.Deadline
                    &&finding.StartedTurn>=initialTurn&&finding.StartedTurn<=game.TurnNumber&&Enum.IsDefined(typeof(ActivityProgressStage),finding.Stage)
                    &&finding.InsightApplied>=0&&finding.InsightUnlearnable>=0&&finding.ResearchReceipts!=null
                    &&finding.ResearchReceipts.All(r=>r.CivilizationId==finding.ActorId&&r.Amount>=0),"Finding source/work/reward receipt.");
                Require(finding.Stage==ActivityProgressStage.Completed?finding.ResolvedTurn.HasValue&&finding.ResolvedTurn>=finding.StartedTurn&&finding.ResolvedTurn<=game.TurnNumber
                    :(finding.InsightApplied==0&&finding.InsightUnlearnable==0&&finding.ResearchReceipts!.Count==0),"No unearned finding reward.");
            }
        }
    }
}
