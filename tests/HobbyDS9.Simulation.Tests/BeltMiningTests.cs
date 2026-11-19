using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;
namespace HobbyDS9.Simulation.Tests;
public sealed class BeltMiningTests
{
    static CampaignBeltBodiesPackageDto Package()
    {
        var previous=EngineeringRefinementLifecycleTests.Package();
        var p=new CampaignBeltBodiesPackageDto{BaseGrowth=previous.BaseGrowth,Generation=previous.Generation,Policy=previous.Policy,RefinementPolicyId=previous.RefinementPolicyId};
        p.PackageHash=BeltBodiesEditionData.Hash(p);return p;
    }
    static CampaignTacticalReviewSession Fresh()=>CampaignTacticalReviewSession.StartCycleGame(Package(),PublishedConstructionProfileTests.Settings(30));
    static CampaignBeltBodiesSaveDto Root(CampaignTacticalReviewSession s)=>Assert.IsType<CampaignBeltBodiesSaveDto>(s.ExportCycleSave());
    static string Wire(CampaignTacticalReviewSession s)=>JsonSerializer.Serialize(Root(s));
    static BeltBodySnapshot Sol(CampaignTacticalReviewSession s)=>s.CaptureBeltBodies(Human)!.Bodies.Single(b=>b.FeatureId==CampaignAuthoredSolIdentity.FeatureId);
    static BeltBodyCommandDto Command(CampaignTacticalReviewSession s,BeltBodyAction action)
    {
        var stamp=s.CaptureStatus();return new(){ActorId=Human,CommandId="command.belt.mining."+stamp.Turn+"."+stamp.CampaignRevision,ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Action=action,BodyId=Sol(s).BodyId};
    }
    static void Survey(CampaignTacticalReviewSession s)
    {
        var c=Command(s,BeltBodyAction.StartSurvey);c.ShipId=Sol(s).SurveyShipIds.First();var result=s.SubmitBeltCommand(c);Assert.True(result.Accepted,result.Error+" "+s.LastPhysicalFailure);
        Advance(s);Advance(s);Assert.True(Sol(s).Surveyed);Assert.Equal(0,Sol(s).HabitabilityBps);Assert.False(Sol(s).SettlementSupported);
    }
    static string Enqueue(CampaignTacticalReviewSession s)
    {
        var c=Command(s,BeltBodyAction.EnqueueMine);c.ColonyId="colony.sol";var result=s.SubmitBeltCommand(c);Assert.True(result.Accepted,result.Error+" "+s.LastPhysicalFailure);return result.TargetId!;
    }
    static BeltMineDto Mine(CampaignTacticalReviewSession s,string id)=>Root(s).BeltBodies!.Mines!.Single(m=>m.InstallationId==id);
    static long Stock(GameStateDto game,string colony)=>PopulationRules.Stock(game.Colonies!.Single(c=>c.ColonyId==colony).Stockpiles!,BeltMiningPolicy.Resource);
    [Fact] public void SurveyAndSharedPaidConstructionGrantNoFreeOreThenFiniteMineActuallyDeliversAndReloads()
    {
        var s=Fresh();var before=Wire(s);var hidden=Command(s,BeltBodyAction.EnqueueMine);hidden.ColonyId="colony.sol";
        Assert.Equal(BeltBodyError.SurveyRequired,s.PreviewBeltCommand(hidden).Error);Assert.False(s.SubmitBeltCommand(hidden).Accepted);Assert.Equal(before,Wire(s));
        Survey(s);var initial=Root(s).BeltBodies!.Deposits!.Single(d=>d.BodyId==Sol(s).BodyId).Remaining;var id=Enqueue(s);
        var queued=Root(s);Assert.Equal(BeltMineStage.Queued,Mine(s,id).Stage);Assert.Empty(queued.BeltBodies!.LastShipments!);
        var stamp=s.CaptureStatus();Assert.True(s.SubmitColonisation(new(){ActorId=Human,CommandId="command.belt.mining.queued-kit",Turn=stamp.Turn,Revision=stamp.CampaignRevision,Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}}).Accepted);
        var kit=Root(s).BaseCampaign!.Production!.Single(q=>q.Kind==ColonisationProductionKind.SettlementKit&&q.OwnerId==Human).QueueItemId;
        for(int i=0;i<64&&Mine(s,id).Stage==BeltMineStage.Queued;i++)
        {
            var old=Root(s);var workBefore=old.BaseCampaign!.Production!.Where(q=>q.OwnerId==Human&&q.ColonyId=="colony.sol").Sum(q=>q.CompletedIndustry);
            var industryBefore=s.CaptureColonisation(Human).Worlds.Single(w=>w.Id=="colony.sol").Industry;
            Advance(s);var next=Root(s);
            var all=next.BaseCampaign!.Production!.Concat(next.BaseCampaign.ProductionHistory!.Select(r=>r.Order!));
            var workAfter=all.Where(q=>q.OwnerId==Human&&q.ColonyId=="colony.sol").Sum(q=>q.CompletedIndustry);
            var industryAfter=s.CaptureColonisation(Human).Worlds.Single(w=>w.Id=="colony.sol").Industry;
            Assert.InRange(workAfter-workBefore,0,Math.Max(industryBefore,industryAfter));
            Assert.Equal(0,all.Single(q=>q.QueueItemId==kit).CompletedIndustry);
        }
        Assert.Equal(BeltMineStage.Active,Mine(s,id).Stage);Assert.Equal(0,Mine(s,id).TotalExtracted);Assert.Equal(initial,Root(s).BeltBodies!.Deposits!.Single(d=>d.BodyId==Sol(s).BodyId).Remaining);
        stamp=s.CaptureStatus();Assert.True(s.SubmitActivity(new(){ActorId=Human,CommandId="command.belt.mining.cancel-kit",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CampaignActivityCommandKind.CancelProduction,TargetId=kit}).Accepted);
        var original=Root(s);var gameBefore=Game(original.BaseCampaign!);var sponsor=gameBefore.Colonies!.Single(c=>c.ColonyId=="colony.sol");
        var originalPlanet=gameBefore.Planets!.Single(p=>p.PlanetId==sponsor.PlanetId);var originalRemaining=PopulationRules.Stock(originalPlanet.RemainingDeposits!,BeltMiningPolicy.Resource);
        Advance(s);var mined=Root(s);var gameAfter=Game(mined.BaseCampaign!);var active=Mine(s,id);
        Assert.Equal(3,active.TotalExtracted);var shipment=Assert.Single(mined.BeltBodies!.LastShipments!,r=>r.InstallationId==id);Assert.InRange(shipment.Amount,1,3);Assert.Equal(shipment.Amount,active.TotalDelivered);
        var ordinary=gameAfter.LastDeliveries!.Where(r=>r.ResourceDefinitionId==BeltMiningPolicy.Resource).Sum(r=>(r.DestinationPlanetId==sponsor.PlanetId?r.Delivered:0)-(r.OriginPlanetId==sponsor.PlanetId?r.Delivered:0));
        var local=originalRemaining-PopulationRules.Stock(gameAfter.Planets!.Single(p=>p.PlanetId==sponsor.PlanetId).RemainingDeposits!,BeltMiningPolicy.Resource);
        Assert.Equal(Stock(gameBefore,"colony.sol")+shipment.Amount+ordinary+local,Stock(gameAfter,"colony.sol"));
        var deposit=mined.BeltBodies.Deposits!.Single(d=>d.BodyId==Sol(s).BodyId);Assert.Equal(initial,deposit.Remaining+deposit.LocalStock+active.TotalDelivered);
        Assert.DoesNotContain(gameAfter.Planets!,p=>p.PlanetId==Sol(s).BodyId);Assert.Equal(0,gameAfter.LastDeliveries!.Count(r=>r.OriginPlanetId==Sol(s).BodyId));
        var wire=Wire(s);Assert.Equal(wire,Wire(CampaignTacticalReviewSession.LoadCycleSave(Package(),mined)));
        var second=Command(s,BeltBodyAction.EnqueueMine);second.ColonyId="colony.sol";Assert.Equal(BeltBodyError.MineExists,s.PreviewBeltCommand(second).Error);
    }
    [Fact] public void CancellingPaidQueuedMineKeepsSpentWorkAndAllowsReplacementWithoutReward()
    {
        var s=Fresh();Survey(s);var id=Enqueue(s);Advance(s);var root=Root(s);var q=root.BaseCampaign!.Production!.Single(q=>q.QueueItemId==Mine(s,id).QueueItemId);Assert.True(q.CompletedIndustry>0);Assert.True(q.CompletedIndustry<BeltMiningPolicy.Industry);
        var cancel=Command(s,BeltBodyAction.CancelMine);cancel.MineId=id;Assert.True(s.SubmitBeltCommand(cancel).Accepted);var cancelled=Root(s);Assert.Equal(BeltMineStage.Cancelled,Mine(s,id).Stage);
        Assert.Equal(0,Mine(s,id).TotalExtracted);Assert.Equal(0,Mine(s,id).TotalDelivered);var receipt=Assert.Single(cancelled.BaseCampaign!.ProductionHistory!,r=>r.Order!.QueueItemId==q.QueueItemId);Assert.False(receipt.Completed);Assert.Equal(q.CompletedIndustry,receipt.Order!.CompletedIndustry);
        Assert.DoesNotContain(Game(cancelled.BaseCampaign).EntityIdentities,e=>e.EntityId==id);Assert.NotEqual(id,Enqueue(s));
        var wire=Wire(s);Assert.Equal(wire,Wire(CampaignTacticalReviewSession.LoadCycleSave(Package(),Root(s))));
    }
    [Fact] public void MineEventuallyExhaustsFiniteDepositAndCannotExtractAgain()
    {
        var s=Fresh();Survey(s);var id=Enqueue(s);var initial=Root(s).BeltBodies!.Definitions!.Single(b=>b.BodyId==Sol(s).BodyId).InitialDeposit;
        for(int i=0;i<256&&(Mine(s,id).Stage==BeltMineStage.Queued||Mine(s,id).Stage==BeltMineStage.Active);i++)Advance(s);
        var mine=Mine(s,id);Assert.Equal(BeltMineStage.Exhausted,mine.Stage);Assert.Equal(initial,mine.TotalExtracted);
        var deposit=Root(s).BeltBodies!.Deposits!.Single(b=>b.BodyId==Sol(s).BodyId);Assert.Equal(0,deposit.Remaining);Assert.Equal(initial,deposit.LocalStock+mine.TotalDelivered);
        for(int i=0;i<8;i++)Advance(s);Assert.Equal(initial,Mine(s,id).TotalExtracted);Assert.Equal(0,Root(s).BeltBodies!.Deposits!.Single(b=>b.BodyId==Sol(s).BodyId).Remaining);
        var wire=Wire(s);Assert.Equal(wire,Wire(CampaignTacticalReviewSession.LoadCycleSave(Package(),Root(s))));
    }
    [Fact] public void QueriesAndTwoWeekSurveyRespectSharedShipTasksAndGrantNoStockOrHabitat()
    {
        var s=Fresh();var initial=Wire(s);var body=Sol(s);Assert.False(body.Surveyed);Assert.Null(body.HabitabilityBps);Assert.Null(body.ObservedRemaining);Assert.Null(body.ResourceDefinitionId);
        var start=Command(s,BeltBodyAction.StartSurvey);start.ShipId=body.SurveyShipIds.First();Assert.True(s.PreviewBeltCommand(start).CanSubmit);
        Assert.Equal(initial,Wire(s));Assert.True(s.SubmitBeltCommand(start).Accepted);
        var stamp=s.CaptureStatus();Assert.False(s.SubmitColonisation(new(){ActorId=Human,CommandId="command.belt.planet-busy",Turn=stamp.Turn,Revision=stamp.CampaignRevision,
            Action=ColonisationAction.StartPlanetSurvey,StartSurvey=new(){PlanetId="planet.mars",ShipId=start.ShipId}}).Accepted);
        Advance(s);Assert.False(Sol(s).Surveyed);Assert.Null(Sol(s).ObservedRemaining);Assert.Equal(1,Sol(s).CompletedSurveyWeeks);
        Advance(s);Assert.True(Sol(s).Surveyed);Assert.Equal(0,Sol(s).HabitabilityBps);Assert.False(Sol(s).SettlementSupported);
        Assert.Empty(Root(s).BeltBodies!.Mines!);Assert.Empty(Root(s).BeltBodies!.LastShipments!);
        stamp=s.CaptureStatus();Assert.True(s.SubmitColonisation(new(){ActorId=Human,CommandId="command.belt.planet-free",Turn=stamp.Turn,Revision=stamp.CampaignRevision,
            Action=ColonisationAction.StartPlanetSurvey,StartSurvey=new(){PlanetId="planet.mars",ShipId=start.ShipId}}).Accepted);
    }
    [Fact] public void FailedPublicationRollsBackSurveyIdentityHistoryAndAllResources()
    {
        var s=Fresh();var before=Wire(s);var start=Command(s,BeltBodyAction.StartSurvey);start.ShipId=Sol(s).SurveyShipIds.First();
        s.PublishedTransactionProbe=phase=>{if(phase=="before-copy")throw new InvalidOperationException("Injected publication failure");};
        Assert.False(s.SubmitBeltCommand(start).Accepted);s.PublishedTransactionProbe=null;Assert.Equal(before,Wire(s));
        Assert.True(s.SubmitBeltCommand(start).Accepted);var after=Wire(s);Assert.False(s.SubmitBeltCommand(start).Accepted);Assert.Equal(after,Wire(s));
        var foreign=Command(s,BeltBodyAction.StartSurvey);foreign.ActorId="civ.cardassian";foreign.ShipId=start.ShipId;Assert.Equal(BeltBodyError.ActorNotControlled,s.SubmitBeltCommand(foreign).Error);Assert.Equal(after,Wire(s));
    }
    [Fact] public void ActualMineCallbacksShareResidualBudgetAndRetainBlockedOreWithinFiniteStorage()
    {
        var s=Fresh();Survey(s);var id=Enqueue(s);for(int i=0;i<64&&Mine(s,id).Stage==BeltMineStage.Queued;i++)Advance(s);
        // Start with a real paid/activated installation. These local callback fixtures
        // isolate residual-capacity and access cases without inventing a conquest order.
        var root=Root(s);var game=Game(root.BaseCampaign!);var sponsor=game.Colonies!.Single(c=>c.ColonyId=="colony.sol");sponsor.Stability=10000;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var extract=typeof(CampaignTacticalReviewSession).GetMethod("ExecuteBeltExtraction",flags)!;
        var deliver=typeof(CampaignTacticalReviewSession).GetMethod("DeliverBeltOre",flags)!;
        var graph=(WeightedNavigationGraph)typeof(CampaignTacticalReviewSession).GetField("physicalNavigation",flags)!.GetValue(s)!;
        var state=(BeltBodyStateDto)typeof(CampaignTacticalReviewSession).GetField("beltBodies",flags)!.GetValue(s)!;
        var mine=state.Mines!.Single(m=>m.InstallationId==id);var deposit=state.Deposits!.Single(d=>d.BodyId==mine.BodyId);
        var noCapacity=new Dictionary<string,long>{{sponsor.ColonyId,0}};
        for(int i=0;i<5;i++){game.TurnNumber++;extract.Invoke(s,new object[]{game});deliver.Invoke(s,new object[]{game,noCapacity,graph});}
        Assert.Equal(BeltMiningPolicy.MineStorage,deposit.LocalStock);Assert.Equal(0,mine.TotalDelivered);
        var observed=s.CaptureBeltBodies(Human)!.Bodies.Single(b=>b.BodyId==mine.BodyId);Assert.True(observed.ResourceIsCurrent);Assert.Equal(deposit.Remaining,observed.ObservedRemaining);
        var remaining=deposit.Remaining;game.TurnNumber++;extract.Invoke(s,new object[]{game});Assert.Equal(remaining,deposit.Remaining);
        var budget=new Dictionary<string,long>{{sponsor.ColonyId,1}};var before=Stock(game,sponsor.ColonyId);deliver.Invoke(s,new object[]{game,budget,graph});
        Assert.Equal(1,mine.TotalDelivered);Assert.Equal(before+1,Stock(game,sponsor.ColonyId));Assert.Equal(0,budget[sponsor.ColonyId]);
        var enemy=game.Fleets!.First(f=>f.OwnerCivilizationId!=Human);enemy.LocationNodeId=graph.NodeForSystem(sponsor.SystemId)!;enemy.TransitTotalCost=0;enemy.TransitProgress=0;enemy.RouteNodeIds!.Clear();
        game.TurnNumber++;extract.Invoke(s,new object[]{game});var stock=deposit.LocalStock;budget[sponsor.ColonyId]=3;deliver.Invoke(s,new object[]{game,budget,graph});
        Assert.Equal(stock,deposit.LocalStock);Assert.Equal(3,budget[sponsor.ColonyId]);Assert.Equal(1,mine.TotalDelivered);
        var frozenRemaining=deposit.Remaining;sponsor.Cohorts!.ForEach(c=>c.Population=0);game.TurnNumber++;extract.Invoke(s,new object[]{game});deliver.Invoke(s,new object[]{game,budget,graph});Assert.Equal(frozenRemaining,deposit.Remaining);Assert.Equal(stock,deposit.LocalStock);
    }

    [Fact] public void BeltEditionKeepsRealRefinementAcceptContinueDiscardAndInterleavedReload()
    {
        var s=Fresh();var frozen=JsonSerializer.Serialize(Game(Root(s).BaseCampaign!).Blueprints!);
        EngineeringRefinementCommandDto R(EngineeringRefinementAction action=EngineeringRefinementAction.Start,string? project=null,string? parent="component.reactor.v1",EngineeringRefinementGoal goal=EngineeringRefinementGoal.ReduceCapacity)
        {var f=s.CaptureStatus();return new(){ActorId=Human,CommandId="command.belt.refine."+f.Turn+"."+f.CampaignRevision,ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,Action=action,ProjectId=project,ParentVersionId=action==EngineeringRefinementAction.Start?parent:null,Goal=goal};}
        void Complete(){for(int i=0;i<32&&s.CaptureEngineeringRefinement(Human)!.Projects.Last().Stage==EngineeringRefinementStage.Working;i++)Advance(s);Assert.Equal(EngineeringRefinementStage.Pending,s.CaptureEngineeringRefinement(Human)!.Projects.Last().Stage);}
        Assert.True(s.SubmitEngineeringRefinement(R()).Accepted);
        var survey=Command(s,BeltBodyAction.StartSurvey);survey.ShipId=Sol(s).SurveyShipIds.First();Assert.True(s.SubmitBeltCommand(survey).Accepted);
        var stamp=s.CaptureStatus();Assert.True(s.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.belt.refine.allocate",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CommandKind.SetResearchAllocation,SetResearchAllocation=new(){ScienceAllocationUnits=0}}).Accepted);
        Complete();for(int i=0;i<2&&!Sol(s).Surveyed;i++)Advance(s);Assert.True(Sol(s).Surveyed);
        var first=s.CaptureEngineeringRefinement(Human)!.Projects.Last();var accepted=s.SubmitEngineeringRefinement(R(EngineeringRefinementAction.Accept,first.ProjectId));Assert.True(accepted.Accepted,accepted.Error+" "+s.LastPhysicalFailure);
        Assert.Contains(first.Outcome!.VersionId,s.CaptureIntegratedLibrary(Human).Catalogue!.AvailableVersionIds);
        var start=Enum.GetValues<EngineeringRefinementGoal>().Select(g=>R(parent:first.Outcome.VersionId,goal:g)).First(c=>s.PreviewEngineeringRefinement(c).CanStart);Assert.True(s.SubmitEngineeringRefinement(start).Accepted);Complete();
        var second=s.CaptureEngineeringRefinement(Human)!.Projects.Last();var next=Enum.GetValues<EngineeringRefinementGoal>().Select(g=>R(EngineeringRefinementAction.Continue,second.ProjectId,goal:g)).First(c=>s.PreviewEngineeringRefinement(c).CanStart);Assert.True(s.SubmitEngineeringRefinement(next).Accepted);Complete();
        var third=s.CaptureEngineeringRefinement(Human)!.Projects.Last();Assert.True(s.SubmitEngineeringRefinement(R(EngineeringRefinementAction.Discard,third.ProjectId)).Accepted);
        Assert.Equal(frozen,JsonSerializer.Serialize(Game(Root(s).BaseCampaign!).Blueprints!));var wire=Wire(s);Assert.Equal(wire,Wire(CampaignTacticalReviewSession.LoadCycleSave(Package(),Root(s))));
    }

}
