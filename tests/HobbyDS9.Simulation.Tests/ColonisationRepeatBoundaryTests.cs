using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;
namespace HobbyDS9.Simulation.Tests;
// Poison/loss/counter changes in this class are isolated defensive kernel fixtures.
// Only the lazy checkpoint before those changes is a normally replayable public journey.
public sealed class ColonisationRepeatBoundaryTests
{
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Private)!.GetValue(o)!;
    static object Context(CampaignTacticalReviewSession s)=>Field<object>(s,"colonisation");
    static object Expansion(CampaignTacticalReviewSession s)=>Field<object>(s,"expansion");
    static GameStateDto Live(CampaignTacticalReviewSession s)=>Field<CampaignTacticalReviewSaveDto>(s,"state").Campaign.GameState;
    static CampaignColonisationStateDto Facts(CampaignTacticalReviewSession s)=>Field<CampaignColonisationStateDto>(Context(s),"State");
    static ColonisationRepeatGoalDto Goal(CampaignTacticalReviewSession s)=>Assert.Single(Field<List<ColonisationRepeatGoalDto>>(Expansion(s),"RepeatGoals"));
    static ColonisationAiPlanDto First(CampaignTacticalReviewSession s)=>Assert.Single(Field<List<ColonisationAiPlanDto>>(Expansion(s),"Plans"));
    static object? Call(CampaignTacticalReviewSession s,string name,params object?[] args)=>typeof(CampaignTacticalReviewSession).GetMethods(Private).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(s,args);
    static object Search()=>Activator.CreateInstance(typeof(CampaignTacticalReviewSession).GetNestedType("ExpansionSearch",BindingFlags.NonPublic)!,true)!;
    static string Json(object x)=>JsonSerializer.Serialize(x);
    static readonly Lazy<string> Loaded=new(()=>{
        var s=CampaignTacticalReviewSession.StartRepeatGame(ColonisationRepeatTests.Package(),PublishedConstructionProfileTests.Settings(250));
        for(int n=0;n<=128;n++){var root=s.ExportRepeatSave();var id=root.RepeatGoals![0].SettlementManifestId;
            if(id!=null&&root.BaseCampaign!.Colonisation!.ManifestRecords!.Any(m=>m.ManifestId==id&&m.Stage==SettlerManifestStage.Loaded))return Json(root);
            if(n<128)Advance(s);}
        throw new InvalidOperationException("Real second loaded checkpoint absent within128");});
    static CampaignTacticalReviewSession Fresh()=>CampaignTacticalReviewSession.LoadRepeatSave(ColonisationRepeatTests.Package(),JsonSerializer.Deserialize<CampaignColonisationRepeatSaveDto>(Loaded.Value)!);
    [Fact]public void SecondLoadedManifestRetainsItsOwnCarrierEvenWithAnotherEarlierCapableShip()
    {
        var s=Fresh();var goal=Goal(s);var manifest=Facts(s).ManifestRecords!.Single(m=>m.ManifestId==goal.SettlementManifestId);var game=Live(s);
        var actual=game.Ships!.Single(x=>x.ShipId==manifest.ShipId);var other=JsonSerializer.Deserialize<ShipStateDto>(Json(actual))!;other.ShipId="aaa.fixture-other-carrier";game.Ships!.Insert(0,other);
        var records=Field<List<CampaignPublishedBuiltShipDto>>(Field<object>(s,"published"),"Records");var source=JsonSerializer.Deserialize<CampaignPublishedBuiltShipDto>(Json(records.Single(r=>r.ShipId==actual.ShipId)))!;source.ShipId=other.ShipId;records.Add(source);
        var capability=Call(s,"TransportCapability",game,other);Assert.NotNull(capability);Assert.True(Convert.ToInt64(capability!.GetType().GetProperty("Passengers")!.GetValue(capability))>=100);
        // Freeze movement in this identity-selection-only fixture so no invented ship is submitted to a command validator.
        var fleet=game.Fleets!.Single(f=>f.FleetId==actual.FleetId);fleet.TransitTotalCost=10;fleet.TransitProgress=1;
        goal.Assignment!.TransportShipId=other.ShipId;Assert.False((bool)Call(s,"PlanRepeatAction",goal.ActorId,Search())!);
        Assert.Equal(manifest.ShipId,Goal(s).Assignment!.TransportShipId);Assert.Equal(manifest.ManifestId,Goal(s).SettlementManifestId);
    }
    [Fact]public void NoDistinctReservedFoodSourceCannotLoadReceiverFoodOrCreatePhantomSupply()
    {
        var s=Fresh();var game=Live(s);var goal=Goal(s);var camp=Facts(s).CampSources!.Single(c=>c.CampId==First(s).CampId);var receiver=game.Colonies!.Single(c=>c.ColonyId==camp.ColonyId);
        foreach(var world in game.Colonies!.Where(w=>w.OwnerCivilizationId==goal.ActorId&&w.ColonyId!=receiver.ColonyId)){Facts(s).FoodConsumed+=world.Food;world.Food=0;}
        Assert.True(receiver.Food>0);var before=Json(new{game,facts=Facts(s)});var production=Field<List<ColonisationProductionItemDto>>(Context(s),"Production").Count;
        Assert.False((bool)Call(s,"PlanRepeatSupply",goal.ActorId,camp,null,Search())!);Assert.Equal(before,Json(new{game,facts=Facts(s)}));Assert.Equal(production,Field<List<ColonisationProductionItemDto>>(Context(s),"Production").Count);Assert.Null(Goal(s).SupplyCampId);
    }
    [Fact]public void BlockedReturnRetainsPeopleAndExactManifestWhileLossClosesOnlySecondGoal()
    {
        var s=Fresh();var goal=Goal(s);var m=Facts(s).ManifestRecords!.Single(m=>m.ManifestId==goal.SettlementManifestId);var game=Live(s);var ship=game.Ships!.Single(x=>x.ShipId==m.ShipId);var fleet=game.Fleets!.Single(f=>f.FleetId==ship.FleetId);
        fleet.RouteNodeIds!.Clear();fleet.TransitTotalCost=0;fleet.TransitProgress=0;game.NavigationKnowledge!.Single(k=>k.CivilizationId==goal.ActorId).Connections!.Clear();
        // Select a genuinely different owned home node in this route-loss fixture.
        var home=game.Colonies!.Single(c=>c.ColonyId==First(s).HomeColonyId);Assert.NotEqual(home.SystemId,game.Colonies!.Single(c=>c.ColonyId==First(s).EstablishedColonyId).SystemId);
        fleet.LocationNodeId=goal.Assignment!.TargetNodeId!;var before=Json(m);Assert.False((bool)Call(s,"RepeatReturn",goal.ActorId,ship,m,Search())!);Assert.Equal(before,Json(m));
        ship.CurrentHull=0;Call(s,"TickCarriedSettlers",game,Facts(s));Call(s,"UpdateRepeatOutcome",goal,game);
        Assert.Equal(ColonisationRepeatState.Lost,goal.State);Assert.Equal(SettlerManifestStage.Destroyed,m.Stage);Assert.Equal(100,m.PassengerDeaths);Assert.Equal(ColonisationAiStage.GrowthClosed,First(s).Stage);
        Assert.Equal(m.ManifestId,goal.SettlementManifestId);Assert.Single(Facts(s).ManifestResolutions!,r=>r.ManifestId==m.ManifestId&&r.PopulationLost==100);
    }
    [Fact]public void BothAssignmentsAndSupplyCommitmentRemainAtomicDuringLoadedAdvanceFaults()
    {
        var s=Fresh();var before=Json(s.ExportRepeatSave());
        foreach(var point in new[]{"after-command","before-copy","after-transcript-copy","after-cache-copy"}){
            bool seen=false;s.PublishedTransactionProbe=p=>{if(p==point){seen=true;throw new InvalidOperationException("Repeat loaded fault");}};var f=s.CaptureStatus();
            Assert.False(s.AdvanceTurn(f.Turn,f.CampaignRevision).Accepted);Assert.True(seen);Assert.Equal(before,Json(s.ExportRepeatSave()));}
        s.PublishedTransactionProbe=null;
    }
    [Fact]public void HiddenForeignFactsAndCensusOrderDoNotChangeOwnFirstOrSecondActionLedger()
    {
        var a=CampaignTacticalReviewSession.StartRepeatGame(ColonisationRepeatTests.Package(),PublishedConstructionProfileTests.Settings(30));var b=CampaignTacticalReviewSession.StartRepeatGame(ColonisationRepeatTests.Package(),PublishedConstructionProfileTests.Settings(30));
        var game=Live(b);game.Civilizations!.Reverse();game.Colonies!.Reverse();game.Ships!.Reverse();game.Fleets!.Reverse();var human=game.Civilizations.Single(c=>c.ControllerKind==ControllerKind.Human);human.Credits+=123;
        Call(a,"PlanExpansion",Call(a,"TravelCopy"));Call(b,"PlanExpansion",Call(b,"TravelCopy"));Assert.Equal(Json(First(a)),Json(First(b)));Assert.Equal(Json(Goal(a)),Json(Goal(b)));
    }
    [Fact]public void FirstCampBindingSurvivesManifestReuseAndBothRetainedCampSupplySurvivesClosure()
    {
        var s=Fresh();for(int n=0;n<8&&Goal(s).State!=ColonisationRepeatState.Established;n++)Advance(s);Assert.Equal(ColonisationRepeatState.Established,Goal(s).State);
        var goal=Goal(s);var first=First(s);var originalCamp=first.CampId;var secondCamp=goal.Assignment!.CampId;Assert.NotEqual(originalCamp,secondCamp);
        first.ManifestId=goal.SettlementManifestId;Call(s,"ReconcileRepeat",Live(s));Assert.Equal(originalCamp,First(s).CampId);Assert.Equal(secondCamp,Goal(s).Assignment!.CampId);
        foreach(var campId in new[]{originalCamp,secondCamp}){
            var camp=Facts(s).CampSources!.Single(c=>c.CampId==campId);var world=Live(s).Colonies!.Single(c=>c.ColonyId==camp.ColonyId);Assert.True(PopulationRules.Population(world)>0);
            var donor=Live(s).Colonies!.Where(w=>w.OwnerCivilizationId==goal.ActorId&&w.ColonyId!=world.ColonyId).SelectMany(w=>w.Cohorts!).OrderByDescending(c=>c.Population).First();var move=donor.Population-1;donor.Population-=move;world.Cohorts![0].Population+=move;
            var projection=(WorldEconomySnapshot)Call(s,"ProjectOwnedWorld",world,Live(s))!;Assert.True(projection.FoodDemand>projection.FoodOutput,Json(projection));
            var old=world.Food;world.Food=0;Assert.True((bool)Call(s,"RepeatNeedsSupply",Live(s),camp)!);world.Food=old;world.Cohorts[0].Population-=move;donor.Population+=move;
        }
        Assert.Equal(ColonisationAiStage.GrowthClosed,First(s).Stage);Assert.Equal(ColonisationRepeatState.Established,Goal(s).State);
    }
    [Fact]public void ClosedSecondGoalStillPaysForReplacementToSupplyTheFirstRetainedCamp()
    {
        var s=Fresh();for(int n=0;n<8&&Goal(s).State!=ColonisationRepeatState.Established;n++)Advance(s);var goal=Goal(s);Assert.Equal(ColonisationRepeatState.Established,goal.State);
        var game=Live(s);var camp=Facts(s).CampSources!.Single(c=>c.CampId==First(s).CampId);var world=game.Colonies!.Single(c=>c.ColonyId==camp.ColonyId);// Controlled same-actor population redistribution creates a genuine deficit, without adding people/resources.
        var donor=game.Colonies!.Where(w=>w.OwnerCivilizationId==goal.ActorId&&w.ColonyId!=world.ColonyId).SelectMany(w=>w.Cohorts!).OrderByDescending(c=>c.Population).First();var move=donor.Population-1;donor.Population-=move;world.Cohorts![0].Population+=move;
        var projection=(WorldEconomySnapshot)Call(s,"ProjectOwnedWorld",world,game)!;Assert.True(projection.FoodDemand>projection.FoodOutput,Json(projection));Facts(s).FoodConsumed+=world.Food;world.Food=0;
        var ships=game.Ships!.Where(x=>x.OwnerCivilizationId==goal.ActorId).Where(x=>{var cap=Call(s,"TransportCapability",game,x);return cap!=null&&Convert.ToInt64(cap.GetType().GetProperty("Passengers")!.GetValue(cap))>=100;}).ToList();Assert.NotEmpty(ships);
        CampaignCombatConsequences.Apply(game,new CombatResultDto{ShipChanges=ships.Select(x=>new ShipCombatChangeDto{ShipId=x.ShipId,RemainingHull=0}).ToList(),FleetChanges=new()});
        foreach(var ship in ships)Field<CampaignTacticalReviewSaveDto>(s,"state").Pools.RemoveAll(p=>p.ShipId==ship.ShipId);
        var before=Field<List<ColonisationProductionItemDto>>(Context(s),"Production").Select(q=>q.QueueItemId).ToHashSet();
        Assert.True((bool)Call(s,"PlanRepeatAction",goal.ActorId,Search())!);var queue=Assert.Single(Field<List<ColonisationProductionItemDto>>(Context(s),"Production"),q=>!before.Contains(q.QueueItemId));
        Assert.Equal(ColonisationProductionKind.PublishedShip,queue.Kind);Assert.Equal(0,queue.CompletedIndustry);Assert.All(queue.ConsumedResources!,r=>Assert.Equal(0,r.Amount));Assert.Equal(ColonisationRepeatState.Established,Goal(s).State);Assert.Equal(goal.SettlementManifestId,Goal(s).SettlementManifestId);
    }
    [Fact]public void ActualReturnKernelUnloadsSecondPeopleAndKitThenClosesWithoutAnotherAttempt()
    {
        var s=Fresh();var goal=Goal(s);var id=goal.SettlementManifestId;var firstCamp=First(s).CampId;
        // Deliberately trigger recovery on a valid actual loaded checkpoint; this trigger is a defensive fixture, not replay provenance.
        goal.Assignment!.Stage=ColonisationAiStage.Recovering;
        for(int n=0;n<8&&Goal(s).State==ColonisationRepeatState.Active;n++)Advance(s);
        goal=Goal(s);Assert.Equal(ColonisationRepeatState.Aborted,goal.State);Assert.NotNull(goal.ClosedTurn);Assert.Equal(id,goal.SettlementManifestId);Assert.Equal(firstCamp,First(s).CampId);
        var m=Facts(s).ManifestRecords!.Single(m=>m.ManifestId==id);Assert.Equal(SettlerManifestStage.Unloaded,m.Stage);Assert.Equal(100,m.PopulationTransferred);Assert.Equal(0,m.PassengerDeaths);Assert.Equal(0,m.EquipmentConsumed);
        Assert.Contains(Facts(s).ManifestResolutions!,r=>r.ManifestId==id&&r.PopulationTransferred==100&&r.CargoTransferred!.Any(x=>x.ResourceDefinitionId=="resource.settlement-kit"&&x.Amount==1));
        for(int n=0;n<3;n++)Advance(s);Assert.Equal(id,Goal(s).SettlementManifestId);Assert.Equal(ColonisationRepeatState.Aborted,Goal(s).State);Assert.Equal(2,Facts(s).ManifestRecords!.Count(m=>m.OwnerId==goal.ActorId&&m.Kind==SettlerManifestKind.Settlement));
    }
    [Fact]public void UnsupportedOwnershipPoisonIsRejectedAndCannotSpendForeignSourceFoodOrLoseCargo()
    {
        var s=Fresh();var root=s.ExportRepeatSave();var before=Json(root);var first=First(s);var goal=Goal(s);var manifest=Facts(s).ManifestRecords!.Single(m=>m.ManifestId==goal.SettlementManifestId);
        // There is no conquest/ownership-change command in this slice. This is a fail-closed poison fixture, not a claimed capture journey.
        var poisoned=JsonSerializer.Deserialize<CampaignColonisationRepeatSaveDto>(before)!;Game(poisoned.BaseCampaign!).Colonies!.Single(w=>w.ColonyId==first.HomeColonyId).OwnerCivilizationId=Human;
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadRepeatSave(ColonisationRepeatTests.Package(),poisoned));Assert.Equal(before,Json(s.ExportRepeatSave()));
        var game=Live(s);var home=game.Colonies!.Single(w=>w.ColonyId==first.HomeColonyId);home.OwnerCivilizationId=Human;
        var camp=Facts(s).CampSources!.Single(c=>c.CampId==first.CampId);var cargo=Json(manifest);var food=home.Food;var count=Facts(s).ManifestRecords!.Count;
        Assert.False((bool)Call(s,"PlanRepeatSupply",goal.ActorId,camp,null,Search())!);Assert.Equal(food,home.Food);Assert.Equal(cargo,Json(manifest));Assert.Equal(count,Facts(s).ManifestRecords!.Count);Assert.Null(goal.SupplyCampId);
    }
}
