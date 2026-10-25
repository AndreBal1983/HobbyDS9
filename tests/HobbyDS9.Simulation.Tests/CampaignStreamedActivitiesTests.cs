using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class CampaignStreamedActivitiesTests
{
    const string Human="civ.federation";
    static string Json(object o)=>JsonSerializer.Serialize(o);
    static CampaignStreamedActivitiesPackageDto Package()=>CampaignTacticalReviewSession.CreateStreamedActivitiesPackage(CampaignActivitiesTests.Package());
    static GameStateDto Game(CampaignStreamedActivitiesSaveDto root)=>root.Campaign!.Integrated!.Physical!.Core.Campaign.GameState;
    static CampaignActivityCommandDto Cmd(CampaignTacticalReviewSession s,CampaignActivityCommandKind kind,string target,string definition="",int revision=0)
    {var f=s.CaptureStatus();return new(){ActorId=Human,CommandId="command.stream."+f.CampaignRevision,ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,Kind=kind,TargetId=target,DefinitionId=definition,BlueprintRevision=revision};}
    static void Advance(CampaignTacticalReviewSession s)
    {var f=s.CaptureStatus();var r=s.AdvanceTurn(f.Turn,f.CampaignRevision);Assert.True(r.Accepted,r.Error+" "+s.LastPhysicalFailure);}
    static void Rehash(CampaignStreamedActivitiesPackageDto p)
    {var c=p.ContentPackage!;c=CampaignTacticalReviewSession.CreateActivitiesPackage(c.BaseTemplate!,c.Catalogue!,c.ReadyClasses!,c.Support!,c.Content!,c.RecordBudget);p.ContentPackage=c;p.PackageHash=CampaignStreamedActivitiesData.PackageHash(p);}
    [Fact] public void UnpaidQueueCompletesFrozenR1WithActualConsumptionAndExactReplay()
    {
        var p=Package();var s=CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings());
        var before=s.ExportStreamedActivitiesSave();var credits=Game(before).Civilizations!.Single(c=>c.CivilizationId==Human).Credits;
        var stocks=Json(Game(before).Colonies!.Single(c=>c.ColonyId=="colony.sol").Stockpiles!);
        var order=Cmd(s,CampaignActivityCommandKind.EnqueueShip,"colony.sol","blueprint.ready.constitution-explorer",1);
        Assert.True(s.EvaluateActivity(order).Accepted);Assert.Equal(Json(before),Json(s.ExportStreamedActivitiesSave()));
        var q=s.SubmitActivity(order);Assert.True(q.Accepted,q.Error+" "+s.LastPhysicalFailure);
        var root=s.ExportStreamedActivitiesSave();Assert.Equal(credits,Game(root).Civilizations!.Single(c=>c.CivilizationId==Human).Credits);
        Assert.Equal(stocks,Json(Game(root).Colonies!.Single(c=>c.ColonyId=="colony.sol").Stockpiles!));
        Assert.All(root.Production!.Single().ConsumedResources!,r=>Assert.Equal(0,r.Amount));Assert.Empty(root.Activities!.Production!);
        Assert.Throws<ArgumentException>(()=>s.ExportActivitiesSave());Assert.Throws<InvalidOperationException>(()=>s.ExportPlayableSave());
        var draft=ActivityReadyClassTests.Ready().Single(d=>d.ClassId=="class.constitution-explorer.v1");draft.DisplayName="Later Explorer";
        var f=s.CaptureStatus();Assert.True(s.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.stream.r2",ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,
            Kind=CommandKind.PublishShipBlueprint,PublishShipBlueprint=new(){BlueprintId=order.DefinitionId,Revision=2,Draft=draft}}).Accepted);
        for(int i=0;i<40&&!s.ExportStreamedActivitiesSave().PublishedBuiltShips!.Any(r=>r.OwnerCivilizationId==Human);i++)Advance(s);
        root=s.ExportStreamedActivitiesSave();var built=root.PublishedBuiltShips!.Single(r=>r.OwnerCivilizationId==Human);Assert.Equal(1,built.BlueprintRevision);
        var receipt=root.ProductionHistory!.Single(r=>r.ShipId==built.ShipId);Assert.True(receipt.Completed);
        Assert.Equal(receipt.Order!.RequiredIndustry,receipt.Order.CompletedIndustry);Assert.Equal(Json(receipt.Order.RequiredResources!),Json(receipt.Order.ConsumedResources!));
        Assert.Equal(Json(root),Json(CampaignTacticalReviewSession.LoadStreamedActivities(p,root).ExportStreamedActivitiesSave()));
        var corrupted=JsonSerializer.Deserialize<CampaignStreamedActivitiesSaveDto>(Json(root))!;corrupted.ProductionHistory![0].Order!.ConsumedResources![0].Amount++;
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadStreamedActivities(p,corrupted));
    }
    [Fact] public void ZeroStockOrderStallsAndCancelsAtJournalEdgeWithoutRefundOrFutureAdvanceReserve()
    {
        var p=Package();var initial=p.ContentPackage!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.InitialState;
        foreach(var c in initial.Colonies!){foreach(var stock in c.Stockpiles!)stock.Amount=0;}
        foreach(var planet in initial.Planets!)foreach(var deposit in planet.RemainingDeposits!)deposit.Amount=0;
        p.ContentPackage.BaseTemplate.ReplayOperationLimit=5;Rehash(p);
        var s=CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings());
        var command=Cmd(s,CampaignActivityCommandKind.EnqueueStructure,"colony.sol","infrastructure.fabrication-annex.v1");
        Assert.True(s.SubmitActivity(command).Accepted,s.LastPhysicalFailure);Advance(s);Advance(s);
        var stalled=s.ExportStreamedActivitiesSave();var pending=stalled.Production!.Single(q=>q.OwnerId==Human);Assert.Equal(0,pending.CompletedIndustry);
        var stamp=s.CaptureStatus();Assert.False(s.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);Assert.Equal(Json(stalled),Json(s.ExportStreamedActivitiesSave()));
        var local=Json(Game(stalled).Colonies!.Single(c=>c.ColonyId==pending.ColonyId).Stockpiles!);var money=Game(stalled).Civilizations!.Single(c=>c.CivilizationId==Human).Credits;
        Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.CancelProduction,pending.QueueItemId)).Accepted);
        var canceled=s.ExportStreamedActivitiesSave();Assert.Equal(local,Json(Game(canceled).Colonies!.Single(c=>c.ColonyId==pending.ColonyId).Stockpiles!));
        Assert.Equal(money,Game(canceled).Civilizations!.Single(c=>c.CivilizationId==Human).Credits);Assert.False(canceled.ProductionHistory!.Single().Completed);
        Assert.Equal(Json(canceled),Json(CampaignTacticalReviewSession.LoadStreamedActivities(p,canceled).ExportStreamedActivitiesSave()));
    }
    [Fact] public void ActualDeliveryFeedsStructureHeadWithoutPersistentStockPlanOrSecondConsumption()
    {
        var p=Package();var scenario=p.ContentPackage!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!;var initial=scenario.InitialState;
        var yard=initial.Colonies!.Single(c=>c.ColonyId=="colony.sol");var destination=initial.Colonies!.Single(c=>c.ColonyId=="colony.mars");
        foreach(var c in initial.Colonies!)foreach(var stock in c.Stockpiles!)stock.Amount=c==yard?30:0;
        foreach(var planet in initial.Planets!)foreach(var deposit in planet.RemainingDeposits!)deposit.Amount=0;
        Rehash(p);var s=CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings());
        var original=s.ExportStreamedActivitiesSave();var desired=Json(Game(original).Colonies!.Single(c=>c.ColonyId==destination.ColonyId).DesiredStockpiles!);
        Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.EnqueueStructure,destination.ColonyId,"infrastructure.hydroponic-extension.v1")).Accepted);
        for(int i=0;i<30&&!s.ExportStreamedActivitiesSave().Activities!.Structures!.Any(r=>r.ColonyId==destination.ColonyId);i++)Advance(s);
        var root=s.ExportStreamedActivitiesSave();Assert.Contains(root.Activities!.Structures!,r=>r.ColonyId==destination.ColonyId);
        Assert.Equal(desired,Json(Game(root).Colonies!.Single(c=>c.ColonyId==destination.ColonyId).DesiredStockpiles!));
        Assert.True(Game(root).PopulationStatistics!.ResourceDelivered>Game(original).PopulationStatistics!.ResourceDelivered);
        var retained=root.Production!.SelectMany(q=>q.ConsumedResources!).Concat(root.ProductionHistory!.SelectMany(r=>r.Order!.ConsumedResources!)).Sum(r=>r.Amount);
        Assert.Equal(retained,Game(root).PopulationStatistics!.ResourceConsumed-Game(original).PopulationStatistics!.ResourceConsumed);
        Assert.Equal(Json(root),Json(CampaignTacticalReviewSession.LoadStreamedActivities(p,root).ExportStreamedActivitiesSave()));
    }
    [Fact] public void InvalidInitialRevisionRejectsAndDefensiveDetachedCounterEdgeRetainsCancelIncrement()
    {
        var p=Package();p.ContentPackage!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.StateRevision=long.MaxValue-2;Rehash(p);
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings()));
        p=Package();var s=CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings());
        Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.EnqueueStructure,"colony.sol","infrastructure.fabrication-annex.v1")).Accepted,s.LastPhysicalFailure);
        // Fault-injected boundary, not a claimed reachable full-replay save. Trusted starts require revision zero.
        var state=(CampaignTacticalReviewSaveDto)typeof(CampaignTacticalReviewSession).GetField("state",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(s)!;
        state.Campaign.GameState.StateRevision=long.MaxValue-1;
        var before=s.ExportStreamedActivitiesSave();Assert.Equal(long.MaxValue-1,s.CaptureStatus().CampaignRevision);
        var f=s.CaptureStatus();var r=s.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.stream.last.optional",ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,
            Kind=CommandKind.SetResearchAllocation,SetResearchAllocation=new(){ScienceAllocationUnits=300}});
        Assert.False(r.Accepted);Assert.Equal(Json(before),Json(s.ExportStreamedActivitiesSave()));
        Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.CancelProduction,before.Production!.Single().QueueItemId)).Accepted,s.LastPhysicalFailure);
        Assert.Equal(long.MaxValue,s.CaptureStatus().CampaignRevision);Assert.Empty(s.ExportStreamedActivitiesSave().Production!);
    }
    [Fact] public void LateProvisionFailureRollsBackDeliveriesConsumptionIdsAndAllOtherFacts()
    {
        var p=Package();var s=CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings());
        Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.EnqueueShip,"colony.sol","blueprint.ready.defiant-combat",1)).Accepted);
        for(int i=0;i<30;i++)
        {
            var before=s.ExportStreamedActivitiesSave();s.PublishedTransactionProbe=phase=>{if(phase=="after-provision")throw new InvalidOperationException("Deliberate streamed completion failure");};
            var f=s.CaptureStatus();var result=s.AdvanceTurn(f.Turn,f.CampaignRevision);
            if(!result.Accepted){Assert.Equal(Json(before),Json(s.ExportStreamedActivitiesSave()));s.PublishedTransactionProbe=null;Advance(s);Assert.NotEmpty(s.ExportStreamedActivitiesSave().PublishedBuiltShips!);return;}
        }
        Assert.Fail("No streamed completion reached the deliberate late fault.");
    }
    [Fact] public void ExactRecordBoundaryReservesBothStructureAndShipCompletionAndReleasesCanceledReservation()
    {
        var p=Package();p.ContentPackage!.RecordBudget=32;Rehash(p);
        var s=CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings(250));
        Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.EnqueueStructure,"colony.sol","infrastructure.fabrication-annex.v1")).Accepted,s.LastPhysicalFailure);
        for(int i=0;i<15;i++)Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.EnqueueShip,"colony.sol","blueprint.ready.constitution-explorer",1)).Accepted,s.LastPhysicalFailure);
        var full=s.ExportStreamedActivitiesSave();Assert.Equal(16,full.Production!.Count);
        Assert.Equal(CampaignTacticalError.Overflow,s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.EnqueueShip,"colony.sol","blueprint.ready.defiant-combat",1)).Error);
        Assert.Equal(Json(full),Json(s.ExportStreamedActivitiesSave()));
        long Reserved(CampaignStreamedActivitiesSaveDto r)=>2L*r.Production!.Count+r.ProductionHistory!.Count+r.PublishedBuiltShips!.Count+r.Activities!.Structures!.Count+r.Activities.Findings!.Count;
        Assert.Equal(32,Reserved(full));
        for(int i=0;i<40&&s.ExportStreamedActivitiesSave().Activities!.Structures!.Count==0;i++)Advance(s);
        var structure=s.ExportStreamedActivitiesSave();Assert.Single(structure.Activities!.Structures!);Assert.Equal(32,Reserved(structure));
        for(int i=0;i<40&&s.ExportStreamedActivitiesSave().PublishedBuiltShips!.Count==0;i++)Advance(s);
        var ship=s.ExportStreamedActivitiesSave();Assert.Single(ship.PublishedBuiltShips!);Assert.Equal(32,Reserved(ship));
        foreach(var q in ship.Production!.Take(2))Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.CancelProduction,q.QueueItemId)).Accepted,s.LastPhysicalFailure);
        Assert.Equal(30,Reserved(s.ExportStreamedActivitiesSave()));
        Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.EnqueueShip,"colony.sol","blueprint.ready.defiant-combat",1)).Accepted,s.LastPhysicalFailure);
        var final=s.ExportStreamedActivitiesSave();Assert.Equal(32,Reserved(final));
        Assert.Equal(Json(final),Json(CampaignTacticalReviewSession.LoadStreamedActivities(p,final).ExportStreamedActivitiesSave()));
    }
    [Fact] public void ActualLawfulCombatConsumesReservedRoundsAndAftermathWhileStalledOrderRemainsCancelable()
    {
        var p=Package();p.ContentPackage!.Content!.Structures![0].Cost!.Industry=1000000;Rehash(p);
        var app=CampaignStreamedActivitiesApplication.StartGame(p,PublishedConstructionProfileTests.Settings());
        var initial=app.CaptureStatus();Assert.True(app.SubmitActivity(new(){ActorId=Human,CommandId="command.stream.longqueue",ExpectedTurn=initial.Turn,ExpectedRevision=initial.CampaignRevision,
            Kind=CampaignActivityCommandKind.EnqueueStructure,TargetId="colony.sol",DefinitionId=p.ContentPackage.Content.Structures[0].DefinitionId}).Accepted);
        var system=app.CaptureStrategy(Human).Colonies.Single(c=>c.ColonyId.Value=="colony.sol").SystemId;
        var home=app.CaptureGeneratedMap(Human).Regions.Single(r=>r.SystemId==system.Value).NodeId;
        void Order(string fleet,CampaignTravelIntent intent,IReadOnlyList<string> route)
        {var status=app.CaptureStatus();var r=app.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.stream.guard."+status.CampaignRevision,ExpectedTurn=status.Turn,
            ExpectedRevision=status.CampaignRevision,Order=new(){FleetId=fleet,Intent=intent,RouteNodeIds=route.ToList()}});Assert.True(r.Accepted,r.Error.ToString());}
        void Step(){var f=app.CaptureStatus();var r=app.AdvanceTurn(f.Turn,f.CampaignRevision);Assert.True(r.Accepted,r.Error.ToString());}
        var visited=new HashSet<string>();
        for(int i=0;i<8&&app.CaptureStatus().Phase==SessionPhase.Planning;i++)
        {
            var fleet=app.GetOwnStrategicFleets(Human).First();visited.Add(fleet.LocationNodeId.Value);
            if(fleet.TransitTotalCost==0&&fleet.RouteNodeIds.Count==0)
            {
                var route=app.CaptureGeneratedMap(Human).Regions.Where(r=>!visited.Contains(r.NodeId)).Select(r=>app.FindKnownRoute(Human,fleet.LocationNodeId.Value,r.NodeId))
                    .Where(r=>r.Success&&r.TotalCost>0).OrderBy(r=>r.TotalCost).ThenBy(r=>r.Steps.Last(),StringComparer.Ordinal).FirstOrDefault();
                if(route!=null)Order(fleet.FleetId.Value,CampaignTravelIntent.Leave,route.Steps);
            }
            Step();
        }
        var guarded=false;var returning=false;
        for(int i=0;i<128&&app.CaptureStatus().Phase==SessionPhase.Planning;i++)
        {
            var fleet=app.GetOwnStrategicFleets(Human).First();
            if(fleet.TransitTotalCost==0&&fleet.RouteNodeIds.Count==0)
            {
                if(fleet.LocationNodeId.Value==home&&!guarded){Order(fleet.FleetId.Value,CampaignTravelIntent.StayEngage,Array.Empty<string>());guarded=true;}
                else if(fleet.LocationNodeId.Value!=home&&!returning){var route=app.FindKnownRoute(Human,fleet.LocationNodeId.Value,home);Assert.True(route.Success);Order(fleet.FleetId.Value,CampaignTravelIntent.Leave,route.Steps);returning=true;}
            }
            Step();
        }
        Assert.Equal(SessionPhase.AwaitingCombat,app.CaptureStatus().Phase);
        void Liability()
        {
            var r=app.ExportStreamedActivitiesSave();var terminals=r.Production!.Count+r.Activities!.Findings!.Count(f=>f.Stage==ActivityProgressStage.Active||f.Stage==ActivityProgressStage.AwaitingClaim);
            var debt=app.CaptureStatus().Phase==SessionPhase.AwaitingCombat?1+r.Campaign!.Integrated!.Physical!.Core.Battles.Where(b=>b.State.Completion==TacticalCompletion.None).Sum(b=>4-b.State.NextRound):0;
            Assert.True(app.RemainingPlayableOperations>=terminals+debt);
        }
        Liability();var pending=app.ExportStreamedActivitiesSave();app=CampaignStreamedActivitiesApplication.LoadSave(p,pending);Assert.Equal(Json(pending),Json(app.ExportStreamedActivitiesSave()));
        foreach(var id in app.GetEncounters(Human))
        while(!app.Observe(id,Human).Complete)
        {
            var o=app.Observe(id,Human);Assert.True(app.CommitRound(new(){ActorId=Human,CommandId="command.stream.round."+app.CaptureStatus().CampaignRevision,EncounterId=id,
                ExpectedTurn=o.Turn,ExpectedRound=o.Round,ExpectedBoundaryRevision=o.BoundaryRevision,Replacements=new()}).Accepted);Liability();
        }
        var f=app.CaptureStatus();Assert.True(app.FinalizeBatch(f.Turn,f.CampaignRevision).Accepted);Liability();
        var root=app.ExportStreamedActivitiesSave();var order=root.Production!.Single(q=>q.OwnerId==Human);var stamp=app.CaptureStatus();
        Assert.True(app.SubmitActivity(new(){ActorId=Human,CommandId="command.stream.aftercombat.cancel",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CampaignActivityCommandKind.CancelProduction,TargetId=order.QueueItemId}).Accepted);
        root=app.ExportStreamedActivitiesSave();Assert.Equal(Json(root),Json(CampaignStreamedActivitiesApplication.LoadSave(p,root).ExportStreamedActivitiesSave()));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void Actual250StreamedProfileFindsAndResolvesFiniteScienceOrMinorConsentFromKnownRoutes(bool minor)
    {
        var p=Package();var s=CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings(250));
        Assert.Empty(s.CaptureStreamedActivities(Human).Activities.Sites);
        var site=CampaignActivitiesTests.Discover(s,minor);var ship=s.CaptureActivities(Human).Ships.First();
        if(ship.NodeId!=site.Site.NodeId)
        {
            CampaignActivitiesTests.Travel(s,ship.FleetId,CampaignActivitiesTests.KnownPath(s.CaptureGeneratedMap(Human),ship.NodeId,site.Site.NodeId));
            for(int i=0;i<20&&s.CaptureActivities(Human).Ships.First().NodeId!=site.Site.NodeId;i++)Advance(s);
        }
        Assert.Equal(site.Site.NodeId,s.CaptureActivities(Human).Ships.First().NodeId);
        var choice=minor?site.Choices.Single(c=>c.ChoiceId.EndsWith(".channel")):site.Choices.First(c=>c.RequiredStudy<=3);
        var start=Cmd(s,CampaignActivityCommandKind.StartStudy,site.Site.SiteId,choice.ChoiceId);start.ShipId=ship.ShipId;
        Assert.True(s.SubmitActivity(start).Accepted,s.LastPhysicalFailure);
        for(int i=0;i<3&&s.CaptureActivities(Human).Findings.Last().Stage==ActivityProgressStage.Active;i++)Advance(s);
        Assert.Equal(ActivityProgressStage.AwaitingClaim,s.CaptureActivities(Human).Findings.Last().Stage);
        var before=s.ExportStreamedActivitiesSave();s=CampaignTacticalReviewSession.LoadStreamedActivities(p,before);Assert.Equal(Json(before),Json(s.ExportStreamedActivitiesSave()));
        Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.ClaimFinding,site.Site.SiteId,choice.ChoiceId)).Accepted,s.LastPhysicalFailure);
        var finding=s.CaptureActivities(Human).Findings.Last();Assert.Equal(ActivityProgressStage.Completed,finding.Stage);
        if(minor)
        {
            var current=s.CaptureActivities(Human).Sites.Single(x=>x.Site.SiteId==site.Site.SiteId);Assert.Null(current.MinorDefinition!.Encounter);Assert.Empty(current.Definition.Choices!);
            Assert.Contains(current.Relation!.Value,new[]{ActivityMinorRelation.Accepted,ActivityMinorRelation.Refused});
            Assert.Equal(current.Relation==ActivityMinorRelation.Accepted,current.Choices.Any(c=>c.ChoiceId.EndsWith(".aid")||c.ChoiceId.EndsWith(".exchange")));
        }
        else Assert.True(finding.InsightApplied>0);
        var complete=s.ExportStreamedActivitiesSave();Assert.False(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.StartStudy,site.Site.SiteId,choice.ChoiceId)).Accepted);
        Assert.Equal(Json(complete),Json(s.ExportStreamedActivitiesSave()));Assert.Equal(Json(complete),Json(CampaignTacticalReviewSession.LoadStreamedActivities(p,complete).ExportStreamedActivitiesSave()));
    }
}
