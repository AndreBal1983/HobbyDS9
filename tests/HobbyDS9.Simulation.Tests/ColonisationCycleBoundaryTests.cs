using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;
using static HobbyDS9.Simulation.Tests.ColonisationCycleTests;
namespace HobbyDS9.Simulation.Tests;
// Only cached public checkpoints are replay roots. All mutations below are labelled defensive fixtures.
public sealed class ColonisationCycleBoundaryTests
{
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    internal static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Private)!.GetValue(o)!;
    internal static object Expansion(CampaignTacticalReviewSession s)=>Field<object>(s,"expansion");
    internal static object Context(CampaignTacticalReviewSession s)=>Field<object>(s,"colonisation");
    internal static GameStateDto Live(CampaignTacticalReviewSession s)=>Field<CampaignTacticalReviewSaveDto>(s,"state").Campaign.GameState;
    internal static CampaignColonisationStateDto Facts(CampaignTacticalReviewSession s)=>Field<CampaignColonisationStateDto>(Context(s),"State");
    internal static ColonisationCycleActorDto Owner(CampaignTacticalReviewSession s)=>Assert.Single(Field<List<ColonisationCycleActorDto>>(Expansion(s),"CycleActors"));
    internal static ColonisationAiPlanDto First(CampaignTacticalReviewSession s)=>Assert.Single(Field<List<ColonisationAiPlanDto>>(Expansion(s),"Plans"));
    internal static object? Call(CampaignTacticalReviewSession s,string name,params object?[] args)=>typeof(CampaignTacticalReviewSession).GetMethods(Private).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(s,args);
    internal static object Search()=>Activator.CreateInstance(typeof(CampaignTacticalReviewSession).GetNestedType("ExpansionSearch",BindingFlags.NonPublic)!,true)!;
    [Fact]public void ActualReturnKeepsIntendedTargetWhenMutableAssignmentIsCleared()
    {
        var s=Fresh("loaded");var a=Owner(s);var id=a.Active!.SettlementManifestId;var intended=a.Active.CommittedTargetPlanetId;var firstCamp=First(s).CampId;
        a.Active.Assignment!.Stage=ColonisationAiStage.Recovering;a.Active.Assignment.TargetPlanetId=null;a.Active.Assignment.TargetNodeId=null;
        for(var n=0;n<8&&Owner(s).Active!=null;n++)Advance(s);
        var receipt=Assert.Single(Owner(s).Receipts!);Assert.Equal(ColonisationCycleOutcome.Returned,receipt.Outcome);Assert.Equal(id,receipt.SettlementManifestId);Assert.Equal(intended,receipt.TargetPlanetId);
        var resolution=Assert.Single(Facts(s).ManifestResolutions!,m=>m.ManifestId==id);Assert.Equal(SettlerManifestStage.Unloaded,resolution.Stage);Assert.NotEqual(intended,resolution.DestinationPlanetId);
        Assert.Equal(100,resolution.PopulationTransferred);Assert.Contains(resolution.CargoTransferred!,r=>r.ResourceDefinitionId=="resource.settlement-kit"&&r.Amount==1);Assert.Equal(firstCamp,First(s).CampId);
    }
    [Fact]public void ActualCarrierLossKeepsLatchAndRecordsOneTerminalWithoutFreeRetry()
    {
        var s=Fresh("loaded");var a=Owner(s);var id=a.Active!.SettlementManifestId;var target=a.Active.CommittedTargetPlanetId;var m=Facts(s).ManifestRecords!.Single(m=>m.ManifestId==id);var g=Live(s);
        var ship=g.Ships!.Single(x=>x.ShipId==m.ShipId);ship.CurrentHull=0;Call(s,"TickCarriedSettlers",g,Facts(s));Call(s,"ReconcileCycle",g);
        var receipt=Assert.Single(Owner(s).Receipts!);Assert.Equal(ColonisationCycleOutcome.Lost,receipt.Outcome);Assert.Equal(target,receipt.TargetPlanetId);Assert.Equal(id,receipt.SettlementManifestId);Assert.Equal(100,m.PassengerDeaths);Assert.Null(Owner(s).Active);
        Call(s,"ReconcileCycle",g);Assert.Single(Owner(s).Receipts!);Assert.False((bool)Call(s,"PlanCycleAction",a.ActorId,Search())!);Assert.Null(Owner(s).Active);
    }
    [Fact]public void EstablishedReceiptRemainsHistoricalSuccessAfterLaterEmptyOutpost()
    {
        var s=Fresh("closed1");var receipt=Assert.Single(Owner(s).Receipts!);var before=Wire(receipt);var camp=Facts(s).CampSources!.Single(c=>c.CampId==receipt.CampId);var g=Live(s);
        Assert.Contains(Facts(s).CampTerminals!,t=>t.CampId==camp.CampId&&t.Kind==SettlementTerminalKind.Established&&t.Turn==receipt.TerminalTurn);
        // Metadata/provenance fixture: later empty state is not forged as a public conquest/save history.
        camp.Stage=ColonyEstablishmentStage.EmptyOutpost;Facts(s).CampTerminals!.Add(new(){CampId=camp.CampId,Kind=SettlementTerminalKind.Extinct,Turn=g.TurnNumber});
        Call(s,"ReconcileCycle",g);Call(s,"ValidateCycleState",g);Assert.Equal(before,Wire(Assert.Single(Owner(s).Receipts!)));Assert.False((bool)Call(s,"RepeatNeedsSupply",g,camp)!);
    }
    [Fact]public void DeficitButNotUrgentRecipientGetsRealStreamedFoodQueueOnlyOnce()
    {
        var s=Fresh("closed1");var g=Live(s);var a=Owner(s);var camp=Facts(s).CampSources!.Single(c=>c.CampId==First(s).CampId);var world=g.Colonies!.Single(w=>w.ColonyId==camp.ColonyId);
        var donor=g.Colonies!.Where(w=>w.OwnerCivilizationId==a.ActorId&&w.ColonyId!=world.ColonyId).SelectMany(w=>w.Cohorts!).OrderByDescending(c=>c.Population).First();
        // Conserve actual own population while creating a one-food deficit with existing high reserves.
        var transfer=201-PopulationRules.Population(world);Assert.True(transfer>0&&donor.Population>transfer);donor.Population-=transfer;world.Cohorts![0].Population+=transfer;
        var output=(WorldEconomySnapshot)Call(s,"ProjectOwnedWorld",world,g)!;Assert.Equal(1,output.FoodDemand-output.FoodOutput);Assert.False((bool)Call(s,"RepeatNeedsSupply",g,camp)!);
        Assert.Null(a.Active);var credits=g.Civilizations!.Single(c=>c.CivilizationId==a.ActorId).Credits;var stocks=Wire(world.Stockpiles!);var queues=Field<List<ColonisationProductionItemDto>>(Context(s),"Production").Select(q=>q.QueueItemId).ToHashSet();
        Assert.True((bool)Call(s,"PlanCycleRecipientFoodReadiness",a.ActorId)!);a=Owner(s);g=Live(s);world=g.Colonies!.Single(w=>w.ColonyId==world.ColonyId);
        var q=Assert.Single(Field<List<ColonisationProductionItemDto>>(Context(s),"Production"),q=>!queues.Contains(q.QueueItemId));Assert.Equal(ColonisationProductionKind.Infrastructure,q.Kind);Assert.Equal(world.ColonyId,q.ColonyId);Assert.Equal(0,q.CompletedIndustry);Assert.All(q.ConsumedResources!,r=>Assert.Equal(0,r.Amount));
        Assert.Equal(credits,g.Civilizations!.Single(c=>c.CivilizationId==a.ActorId).Credits);Assert.Equal(stocks,Wire(world.Stockpiles!));Assert.Equal(output.FoodOutput,((WorldEconomySnapshot)Call(s,"ProjectOwnedWorld",world,g)!).FoodOutput);Assert.Null(a.Active);
        Call(s,"PlanCycleRecipientFoodReadiness",a.ActorId);Assert.Single(Field<List<ColonisationProductionItemDto>>(Context(s),"Production"),x=>x.ColonyId==q.ColonyId&&x.DefinitionId==q.DefinitionId);
        for(int n=0;n<32&&Field<List<ColonisationProductionItemDto>>(Context(s),"Production").Any(x=>x.QueueItemId==q.QueueItemId);n++)Advance(s);
        world=Live(s).Colonies!.Single(w=>w.ColonyId==q.ColonyId);Assert.True(((WorldEconomySnapshot)Call(s,"ProjectOwnedWorld",world,Live(s))!).FoodOutput>output.FoodOutput);
        Assert.Contains(Field<List<ColonisationProductionReceiptDto>>(Context(s),"History"),x=>x.Order!.QueueItemId==q.QueueItemId&&x.Completed);
    }
    [Fact]public void LoadedCycleAllLateFaultsPreserveEntireRootAndActorCommitments()
    {
        var s=Fresh("loaded");var before=Wire(s.ExportCycleSave());foreach(var point in new[]{"after-command","before-copy","after-transcript-copy","after-cache-copy"})
        {bool seen=false;s.PublishedTransactionProbe=p=>{if(p==point){seen=true;throw new InvalidOperationException("Cycle fault");}};var f=s.CaptureStatus();Assert.False(s.AdvanceTurn(f.Turn,f.CampaignRevision).Accepted);Assert.True(seen);Assert.Equal(before,Wire(s.ExportCycleSave()));}
        s.PublishedTransactionProbe=null;
        foreach(var phase in Enum.GetValues<StrategicTurnPhase>()){bool seen=false;s.PhysicalPhaseProbe=p=>{if(p==phase){seen=true;return false;}return true;};var f=s.CaptureStatus();Assert.False(s.AdvanceTurn(f.Turn,f.CampaignRevision).Accepted);Assert.True(seen);Assert.Equal(before,Wire(s.ExportCycleSave()));}s.PhysicalPhaseProbe=null;
    }
    [Fact]public void HiddenForeignFactsAndActorOrderingDoNotChangeCycleDecisions()
    {
        var a=CampaignTacticalReviewSession.StartCycleGame(Package(),PublishedConstructionProfileTests.Settings(30));var b=CampaignTacticalReviewSession.StartCycleGame(Package(),PublishedConstructionProfileTests.Settings(30));
        var g=Live(b);g.Civilizations!.Reverse();g.Colonies!.Reverse();g.Ships!.Reverse();g.Fleets!.Reverse();g.Civilizations.Single(c=>c.ControllerKind==ControllerKind.Human).Credits+=123;
        Call(a,"PlanExpansion",Call(a,"TravelCopy"));Call(b,"PlanExpansion",Call(b,"TravelCopy"));Assert.Equal(Wire(First(a)),Wire(First(b)));Assert.Equal(Wire(Owner(a)),Wire(Owner(b)));
    }
    [Fact]public void PlausibleReceiptLatchOrdinalAndForeignSourceTamperingFailFullReplay()
    {
        var root=Root("next-real");foreach(var change in new Action<CampaignColonisationCycleSaveDto>[] {
            x=>x.CycleActors![0].Active!.CommittedTargetPlanetId=x.CycleActors[0].Receipts![0].TargetPlanetId,
            x=>x.CycleActors![0].Receipts![0].TargetPlanetId=x.CycleActors[0].Active!.CommittedTargetPlanetId!,
            x=>x.CycleActors![0].Receipts![0].TerminalTurn++,x=>x.CycleActors![0].NextOrdinal++,
            x=>x.CycleActors![0].SupplyCampId="foreign",x=>x.CycleActors![0].Active!.Assignment!.HomeColonyId="colony.sol"})
        {var bad=Copy(root);change(bad);Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadCycleSave(Package(),bad));}
    }
    [Fact]public void BootstrapReturnThenRealRetryRetainsBothActualManifestProvenances()
    {
        var s=Fresh("bootstrap-loaded");var first=First(s);var id=first.ManifestId;Assert.Null(first.CampId);first.Stage=ColonisationAiStage.Recovering;first.TargetPlanetId=null;first.TargetNodeId=null;
        for(int n=0;n<12&&!Facts(s).ManifestRecords!.Any(m=>m.OwnerId==first.ActorId&&m.Kind==SettlerManifestKind.Settlement&&m.ManifestId!=id);n++)Advance(s);
        Assert.Contains(Facts(s).ManifestResolutions!,r=>r.ManifestId==id&&r.Stage==SettlerManifestStage.Unloaded);
        Assert.Contains(Facts(s).ManifestRecords!,m=>m.OwnerId==first.ActorId&&m.Kind==SettlerManifestKind.Settlement&&m.ManifestId!=id);
        Call(s,"ValidateCycleState",Live(s));
        for(int n=0;n<96&&Owner(s).Active==null&&Owner(s).Receipts!.Count==0;n++)Advance(s);
        Assert.True(Owner(s).Active!=null||Owner(s).Receipts!.Count>0);Call(s,"ValidateCycleState",Live(s));
        Assert.Contains(Facts(s).ManifestResolutions!,r=>r.ManifestId==id&&r.Stage==SettlerManifestStage.Unloaded);
    }
    [Fact]public void NewCycleCapacityTypeIsOnlyForVisitByteExhaustionAndMalformedRootStaysGeneric()
    {
        var root=Root("loaded");var policy=Copy(Package().BaseGrowth!.BaseRosterV2!.BaseV1!.Settlement!);policy.MaximumValidationVisits=1;
        Assert.Throws<ColonisationRootCapacityException>(()=>ColonisationRootBudget.ValidateCycle(root,policy));
        var malformed=Copy(root);malformed.CycleActors![0].Active!.Assignment!.OwnAttemptedTargets=null;
        Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.ValidateCycle(malformed,policy));
        var legacy=CampaignTacticalReviewSession.StartRepeatGame(ColonisationRepeatTests.Package(),PublishedConstructionProfileTests.Settings(30)).ExportRepeatSave();
        Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.ValidateRepeat(legacy,policy));
    }

    [Fact]public void BootstrapPostImmigrationFoodGapIsMaintainedBeforeGrowthWaitingWithoutPhantomCycle()
    {
        var s=Fresh("before-immigration");var first=First(s);var a=Owner(s);var g=Live(s);var camp=Facts(s).CampSources!.Single(c=>c.CampId==first.CampId);var world=g.Colonies!.Single(w=>w.ColonyId==camp.ColonyId);
        var donor=g.Colonies!.Single(w=>w.ColonyId==first.HomeColonyId).Cohorts!.OrderByDescending(c=>c.Population).First();
        var transfer=101-PopulationRules.Population(world);Assert.True(transfer>0);donor.Population-=transfer;world.Cohorts![0].Population+=transfer;
        var current=(WorldEconomySnapshot)Call(s,"ProjectOwnedWorld",world,g)!;Assert.Equal(current.FoodDemand,current.FoodOutput);Assert.False((bool)Call(s,"RepeatNeedsSupply",g,camp)!);
        var people=Copy(donor);people.CohortId="fixture.preview-people";people.Population=100;var forecast=Copy(world);forecast.Cohorts!.Add(people);var future=(WorldEconomySnapshot)Call(s,"ProjectOwnedWorld",forecast,g)!;
        Assert.Equal(0,future.Crowding);Assert.True(future.FoodDemand>future.FoodOutput);Assert.DoesNotContain(Facts(s).ManifestRecords!,m=>m.OwnerId==a.ActorId&&m.Kind==SettlerManifestKind.ImmigrationOnly);
        Assert.True((bool)Call(s,"PlanCycleAction",a.ActorId,Search())!);Assert.Null(Owner(s).Active);Assert.Empty(Owner(s).Receipts!);
        Assert.Contains(Field<List<ColonisationProductionItemDto>>(Context(s),"Production"),q=>q.ColonyId==world.ColonyId&&q.Kind==ColonisationProductionKind.Infrastructure);
        Assert.NotEqual(ColonisationAiStage.GrowthClosed,First(s).Stage);
    }
    [Fact]public void EarliestRetainedCampGetsRealNewSupplyAfterMultipleCycleClosures()
    {
        var s=Fresh("closed2");var g=Live(s);var a=Owner(s);var old=Wire(a.Receipts!);var camp=Facts(s).CampSources!.Single(c=>c.CampId==First(s).CampId);var world=g.Colonies!.Single(w=>w.ColonyId==camp.ColonyId);
        var donor=g.Colonies!.Where(w=>w.OwnerCivilizationId==a.ActorId&&w.ColonyId!=world.ColonyId).SelectMany(w=>w.Cohorts!).OrderByDescending(c=>c.Population).First();var transfer=201-PopulationRules.Population(world);Assert.True(transfer>0);donor.Population-=transfer;world.Cohorts![0].Population+=transfer;
        Facts(s).FoodConsumed+=world.Food;world.Food=0;Assert.True((bool)Call(s,"RepeatNeedsSupply",g,camp)!);
        var previous=Facts(s).ManifestRecords!.Select(m=>m.ManifestId).ToHashSet();SettlerManifestDto? supply=null;
        for(int n=0;n<16;n++)
        {
            supply=Facts(s).ManifestRecords!.FirstOrDefault(m=>m.OwnerId==a.ActorId&&m.Kind==SettlerManifestKind.SupplyOnly&&!previous.Contains(m.ManifestId));
            if(supply?.Stage==SettlerManifestStage.Delivered)break;
            if(supply?.Stage==SettlerManifestStage.Loaded)Assert.Equal(camp.CampId,Owner(s).SupplyCampId);
            Advance(s);
        }
        Assert.NotNull(supply);Assert.Equal(SettlerManifestStage.Delivered,supply!.Stage);Assert.True(supply.FoodDelivered>0);
        Assert.Contains(Facts(s).ManifestResolutions!,x=>x.ManifestId==supply.ManifestId&&x.FoodTransferred>0&&x.DestinationPlanetId==world.PlanetId);Assert.Equal(old,Wire(Owner(s).Receipts!));
    }
    [Fact]public void LoadedCycleKeepsExactCarrierWithAnotherEligibleTransportAndBlockedReturnKeepsCargo()
    {
        var s=Fresh("loaded");var a=Owner(s);var m=Facts(s).ManifestRecords!.Single(m=>m.ManifestId==a.Active!.SettlementManifestId);var g=Live(s);var ship=g.Ships!.Single(x=>x.ShipId==m.ShipId);
        var other=Copy(ship);other.ShipId="aaa.fixture-other-transport";g.Ships!.Add(other);var records=Field<List<CampaignPublishedBuiltShipDto>>(Field<object>(s,"published"),"Records");var provenance=Copy(records.Single(x=>x.ShipId==ship.ShipId));provenance.ShipId=other.ShipId;records.Add(provenance);
        var capability=Call(s,"TransportCapability",g,other)!;Assert.True(Convert.ToInt64(capability.GetType().GetProperty("Passengers")!.GetValue(capability))>=100);
        var fleet=g.Fleets!.Single(f=>f.FleetId==ship.FleetId);fleet.TransitTotalCost=10;fleet.TransitProgress=1;a.Active!.Assignment!.TransportShipId=other.ShipId;
        Assert.False((bool)Call(s,"PlanCycleAction",a.ActorId,Search())!);Assert.Equal(m.ShipId,Owner(s).Active!.Assignment!.TransportShipId);
        fleet.TransitTotalCost=0;fleet.TransitProgress=0;fleet.RouteNodeIds!.Clear();fleet.LocationNodeId=a.Active.Assignment.TargetNodeId!;g.NavigationKnowledge!.Single(k=>k.CivilizationId==a.ActorId).Connections!.Clear();var before=Wire(m);
        Assert.False((bool)Call(s,"RepeatReturn",a.ActorId,ship,m,Search())!);Assert.Equal(before,Wire(m));Assert.Equal(m.ManifestId,Owner(s).Active!.SettlementManifestId);
    }
    [Fact]public void CycleDeployCannotChangeLatchedIntentionAndForeignSourceCannotProvideFood()
    {
        var s=Fresh("loaded");var a=Owner(s);var active=a.Active!;First(s).LastPlannedTurn=Live(s).TurnNumber;First(s).LatestActions!.Clear();
        var slot=Enum.Parse(typeof(CampaignTacticalReviewSession).GetNestedType("MissionSlot",BindingFlags.NonPublic)!,"Cycle");
        Assert.False((bool)Call(s,"AssignedColonisation",slot,a.ActorId,ColonisationAiAction.Deploy,new ColonisationCommandDto{Action=ColonisationAction.RequestDeployment,Deploy=new(){ManifestId=active.SettlementManifestId!,PlanetId="planet.sol"}})!);
        // Only the failed bounded receipt/hold may change, not physical state or immutable commitment.
        Assert.Equal(Wire(Copy(ColonisationCycleTests.Root("loaded")).BaseCampaign),Wire(s.ExportCycleSave().BaseCampaign));Assert.Equal(active.CommittedTargetPlanetId,Owner(s).Active!.CommittedTargetPlanetId);
        var g=Live(s);var camp=Facts(s).CampSources!.Single(c=>c.CampId==First(s).CampId);var receiver=g.Colonies!.Single(w=>w.ColonyId==camp.ColonyId);
        foreach(var world in g.Colonies!.Where(w=>w.OwnerCivilizationId==a.ActorId&&w.ColonyId!=receiver.ColonyId)){Facts(s).FoodConsumed+=world.Food;world.Food=0;}
        var original=Wire(new{g,facts=Facts(s)});Assert.False((bool)Call(s,"PlanRepeatSupply",a.ActorId,camp,null,Search())!);Assert.Equal(original,Wire(new{g,facts=Facts(s)}));
    }

    [Fact]public void MalformedCurrentCycleInputIsNotSwallowedAsOrdinaryHeadroomHold()
    {
        var s=Fresh("closed1");Owner(s).Receipts![0].ClosedTurn--;First(s).OwnAttemptedTargets=null;var before=Wire(new{game=Live(s),owner=Owner(s),first=First(s)});
        var error=Assert.Throws<TargetInvocationException>(()=>Call(s,"PlanCycleAction",Owner(s).ActorId,Search()));Assert.IsType<ArgumentException>(error.InnerException);Assert.IsNotType<ColonisationRootCapacityException>(error.InnerException);
        Assert.Equal(before,Wire(new{game=Live(s),owner=Owner(s),first=First(s)}));
    }

}
