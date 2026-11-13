using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;
namespace HobbyDS9.Simulation.Tests;
// Counter/shortage/failure injections below are explicitly private defensive fixtures,
// never claimed admissible replay roots. Their starting snapshots are real public journeys.
public sealed class ColonisationGrowthBoundaryTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Private)!.GetValue(o)!;
    static object Context(CampaignTacticalReviewSession s)=>Field<object>(s,"colonisation");
    static GameStateDto Live(CampaignTacticalReviewSession s)=>Field<CampaignTacticalReviewSaveDto>(s,"state").Campaign.GameState;
    static CampaignColonisationStateDto Facts(CampaignTacticalReviewSession s)=>Field<CampaignColonisationStateDto>(Context(s),"State");
    static ColonisationPolicyDto Policy(CampaignTacticalReviewSession s)=>Field<ColonisationPolicyDto>(Context(s),"Policy");
    static object? Call(CampaignTacticalReviewSession s,string method,params object?[] args)=>typeof(CampaignTacticalReviewSession).GetMethod(method,Private)!.Invoke(s,args);
    static bool Capacity(CampaignTacticalReviewSession s,int delta=0)=>(bool)Call(s,"ColonisationCapacity",delta,null,0)!;
    static string Json(object x)=>JsonSerializer.Serialize(x);
    static readonly Lazy<string> ActualBothActorCargo=new(()=>
    {
        var s=ColonisationGrowthTests.Fresh();var stamp=s.CaptureStatus();Assert.True(s.SubmitActivity(new(){ActorId=Human,CommandId="command.boundary.transport",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.mars",DefinitionId="blueprint.ready.galaxy-colony-transport",BlueprintRevision=1}).Accepted);
        for(int n=0;n<160;n++)
        {
            var root=s.ExportGrowthSave();if(root.BaseCampaign!.PublishedBuiltShips!.Any(b=>b.OwnerCivilizationId==Human)&&root.BaseCampaign.Colonisation!.ManifestRecords!.Any(m=>m.OwnerId!=Human&&m.Kind==SettlerManifestKind.ImmigrationOnly&&m.Stage==SettlerManifestStage.Loaded))return Json(root);
            Advance(s);
        }
        throw new InvalidOperationException("Real small AI cargo branch absent within bound");
    });
    static CampaignTacticalReviewSession Fresh()=>CampaignTacticalReviewSession.LoadGrowthSave(ColonisationGrowthTests.Package(),JsonSerializer.Deserialize<CampaignColonisationExpansionSaveDto>(ActualBothActorCargo.Value)!);
    static ColonisationCommandDto Load(CampaignTacticalReviewSession s)
    {
        var root=s.ExportGrowthSave();var game=Game(root.BaseCampaign!);var home=game.Colonies!.Single(c=>c.ColonyId=="colony.sol");var ship=root.BaseCampaign!.PublishedBuiltShips!.Single(b=>b.OwnerCivilizationId==Human).ShipId!;
        var c=Command(s,ColonisationAction.LoadPassengers);c.Embark=new(){ColonyId=home.ColonyId,ShipId=ship,CohortId=home.Cohorts!.First(co=>co.Population>=400).CohortId,Population=100,Food=20};return c;
    }
    static SettlerManifestDto LoadHuman(CampaignTacticalReviewSession s)
    {Assert.True(s.SubmitColonisation(Load(s)).Accepted,s.LastPhysicalFailure);return Facts(s).ManifestRecords!.Single(m=>m.OwnerId==Human&&m.Stage==SettlerManifestStage.Loaded);}
    static long Minimum(CampaignTacticalReviewSession s,bool records)
    {
        var policy=Policy(s);var original=records?policy.RecordBudget:policy.JournalBudget;int low=0,high=original;
        while(low<high){var mid=low+(high-low)/2;if(records)policy.RecordBudget=mid;else policy.JournalBudget=mid;if(Capacity(s))high=mid;else low=mid+1;}
        if(records)policy.RecordBudget=original;else policy.JournalBudget=original;return low;
    }
    [Fact]public void RealAllActorPassengerLiabilityHasExactRecordAndJournalConversions()
    {
        var s=Fresh();Assert.Single(Facts(s).ManifestRecords!,m=>m.Kind==SettlerManifestKind.ImmigrationOnly&&m.Stage==SettlerManifestStage.Loaded);
        var records=Minimum(s,true);var journal=Minimum(s,false);Policy(s).RecordBudget=(int)records+3;var before=Json(s.ExportGrowthSave());
        Assert.False(s.SubmitColonisation(Load(s)).Accepted);Assert.Equal(before,Json(s.ExportGrowthSave()));
        Policy(s).RecordBudget=(int)records+4;Policy(s).JournalBudget=(int)journal+1;before=Json(s.ExportGrowthSave());Assert.False(s.SubmitColonisation(Load(s)).Accepted);Assert.Equal(before,Json(s.ExportGrowthSave()));
        Policy(s).JournalBudget=(int)journal+2;var m=LoadHuman(s);Assert.Equal(2,Facts(s).ManifestRecords!.Count(m=>m.Kind==SettlerManifestKind.ImmigrationOnly&&m.Stage==SettlerManifestStage.Loaded));
        Assert.Equal(records+4,Minimum(s,true));Assert.Equal(journal+2,Minimum(s,false));
        var c=Command(s,ColonisationAction.UnloadTransport);c.Unload=new(){ManifestId=m.ManifestId,ColonyId="colony.sol"};Assert.True(s.SubmitColonisation(c).Accepted,s.LastPhysicalFailure);
        Assert.Equal(records+4,Minimum(s,true));Assert.Equal(journal+2,Minimum(s,false));Assert.True(Capacity(s));
    }
    [Fact]public void ActualTwoLoadIdsAndOneTerminalRevisionFitExactlyOrRejectWholeCandidate()
    {
        foreach(var excess in new[]{true,false})
        {
            var s=Fresh();var baseJournal=Minimum(s,false);var used=s.ExportGrowthSave().BaseCampaign!.Journal!.Count;var terminals=baseJournal-used;
            Live(s).StateRevision=long.MaxValue-terminals-(excess?1:2);var before=Json(s.ExportGrowthSave());
            var result=s.SubmitColonisation(Load(s));Assert.Equal(!excess,result.Accepted);
            if(excess){Assert.Equal(before,Json(s.ExportGrowthSave()));continue;}
            var m=Facts(s).ManifestRecords!.Single(m=>m.OwnerId==Human&&m.Stage==SettlerManifestStage.Loaded);
            var c=Command(s,ColonisationAction.UnloadTransport);c.Unload=new(){ManifestId=m.ManifestId,ColonyId="colony.sol"};Assert.True(s.SubmitColonisation(c).Accepted,s.LastPhysicalFailure);
            Assert.Equal(long.MaxValue-terminals,s.CaptureStatus().CampaignRevision);
        }
        foreach(var excess in new[]{true,false})
        {
            var s=Fresh();var root=s.ExportGrowthSave().BaseCampaign!;var a=root.Colonisation!;
            var future=(ulong)(2*root.Production!.Count+6*a.ManifestRecords!.Count(m=>m.Stage==SettlerManifestStage.Loaded&&m.Kind==SettlerManifestKind.Settlement)+3*a.CampSources!.Count(c=>c.Stage==ColonyEstablishmentStage.Founding)+a.ManifestRecords!.Count(m=>m.Stage==SettlerManifestStage.Loaded&&m.Kind==SettlerManifestKind.ReturnOnly));
            Live(s).NextEntitySequence=ulong.MaxValue-future-(excess?1UL:2UL);var before=Json(s.ExportGrowthSave());var seq=Live(s).NextEntitySequence;
            Assert.Equal(!excess,s.SubmitColonisation(Load(s)).Accepted);
            if(excess)Assert.Equal(before,Json(s.ExportGrowthSave()));else Assert.Equal(seq+2,Live(s).NextEntitySequence);
        }
    }
    [Theory][InlineData(false)][InlineData(true)]public void LostCarrierOrFullStarvationRetainsExactPassengerDeathAndTerminalFacts(bool starve)
    {
        var s=Fresh();var m=LoadHuman(s);var game=Live(s);var a=Facts(s);var id=m.ManifestId;
        if(starve){a.FoodConsumed+=m.Food;m.FoodConsumed+=m.Food;m.Food=0;}else game.Ships!.Single(ship=>ship.ShipId==m.ShipId).CurrentHull=0;
        for(int n=0;n<105&&m.Stage==SettlerManifestStage.Loaded;n++){Call(s,"TickCarriedSettlers",game,a);game.TurnNumber++;}
        Assert.Equal(SettlerManifestStage.Destroyed,m.Stage);Assert.Null(m.Passengers);Assert.Equal(100,m.PassengerDeaths);Assert.Equal(0,m.PopulationTransferred);Assert.Equal(0,m.EquipmentConsumed);
        var resolution=Assert.Single(a.ManifestResolutions!,r=>r.ManifestId==id);Assert.Equal(100,resolution.PopulationLost);Assert.Equal(starve?0:20,resolution.FoodLost);
        Assert.DoesNotContain(game.EntityIdentities,e=>e.EntityId==id);Assert.True(Capacity(s));
    }
    [Fact]public void BlockedReturnKeepsPeopleAndLostAiTripClosesWithoutReplacementOrNewGrowth()
    {
        var s=Fresh();var game=Live(s);var a=Facts(s);var context=Field<object>(s,"expansion");var plan=Assert.Single(Field<List<ColonisationAiPlanDto>>(context,"Plans"));
        var m=a.ManifestRecords!.Single(m=>m.OwnerId==plan.ActorId&&m.Kind==SettlerManifestKind.ImmigrationOnly&&m.Stage==SettlerManifestStage.Loaded);var home=game.Colonies!.Single(c=>c.ColonyId==plan.HomeColonyId);var ship=game.Ships!.Single(ship=>ship.ShipId==m.ShipId);var fleet=game.Fleets!.Single(f=>f.FleetId==ship.FleetId);
        // Local route-loss/cancellation fixture only; not a replayable history claim.
        fleet.RouteNodeIds!.Clear();fleet.TransitProgress=0;fleet.TransitTotalCost=0;game.NavigationKnowledge!.Single(k=>k.CivilizationId==plan.ActorId).Connections!.Clear();plan.Stage=ColonisationAiStage.Recovering;
        var search=Activator.CreateInstance(typeof(CampaignTacticalReviewSession).GetNestedType("ExpansionSearch",BindingFlags.NonPublic)!,true)!;var before=Json(m);
        Assert.False((bool)Call(s,"PlanGrowthPassengers",plan.ActorId,plan,home,ship,m,a.CampSources!.Single(c=>c.CampId==plan.CampId),search)!);Assert.Equal(before,Json(m));Assert.Equal(ColonisationAiStage.Recovering,plan.Stage);
        ship.CurrentHull=0;Call(s,"TickCarriedSettlers",game,a);Assert.Equal(SettlerManifestStage.Destroyed,m.Stage);
        var q=Field<List<ColonisationProductionItemDto>>(Context(s),"Production").Count;
        Assert.False((bool)Call(s,"PlanExpansionAction",plan.ActorId,search)!);Assert.Equal(ColonisationAiStage.GrowthClosed,plan.Stage);Assert.Equal(q,Field<List<ColonisationProductionItemDto>>(Context(s),"Production").Count);
        Assert.Single(a.ManifestRecords!,m=>m.OwnerId==plan.ActorId&&m.Kind==SettlerManifestKind.ImmigrationOnly);
    }
    [Fact]public void PassengerNegativesAndFoundingOrEmptyReceiversCannotMutateCargoOrOwnership()
    {
        var s=Fresh();var before=Json(s.ExportGrowthSave());
        foreach(var mutate in new Action<ColonisationCommandDto>[] {
            c=>c.Embark!.Population=99,c=>c.Embark!.Food=7,c=>c.Embark!.Food=21,c=>c.Embark!.CohortId="cohort.foreign",
            c=>c.Embark!.ShipId="ship.foreign",c=>c.Embark!.ColonyId="colony.mars",c=>c.Turn--,c=>c.Revision--,
            c=>c.Embark!.Materials=new(){new(){ResourceDefinitionId="resource.settlement-kit",Amount=1}} })
        {var c=Load(s);mutate(c);Assert.False(s.SubmitColonisation(c).Accepted);Assert.Equal(before,Json(s.ExportGrowthSave()));}
        var m=LoadHuman(s);var game=Live(s);var a=Facts(s);
        foreach(var stage in new[]{ColonyEstablishmentStage.Founding,ColonyEstablishmentStage.EmptyOutpost})
        {
            // Only this kernel fixture marks the existing own Mars world as a different
            // stage, without claiming the invented camp is an admissible source root.
            var camp=new SettlementCampDto{CampId="camp.stage-test",ColonyId="colony.mars",OwnerId=Human,Stage=stage};a.CampSources!.Add(camp);
            var snapshot=Json(new{game,a});Assert.Equal(CampaignTacticalError.InvalidOrder,Call(s,"UnloadTransport",game,Human,new UnloadSettlersDto{ManifestId=m.ManifestId,ColonyId="colony.mars"}));
            Assert.Equal(snapshot,Json(new{game,a}));a.CampSources.Remove(camp);
        }
        before=Json(s.ExportGrowthSave());var wrong=Command(s,ColonisationAction.UnloadTransport);wrong.Unload=new(){ManifestId=m.ManifestId,ColonyId="colony.cardassia"};Assert.False(s.SubmitColonisation(wrong).Accepted);Assert.Equal(before,Json(s.ExportGrowthSave()));
        var app=HobbyDS9.Application.CampaignColonisationGrowthApplication.LoadGrowthSave(ColonisationGrowthTests.Package(),s.ExportGrowthSave());Assert.Throws<ArgumentException>(()=>app.CaptureColonisation("civ.cardassian"));Assert.Throws<ArgumentException>(()=>app.CaptureColonisationPlanets("civ.cardassian"));
    }
    [Fact]public void OwnReceiverForecastIgnoresHiddenForeignFoodButRejectsUnsustainableGrowthAndKeepsSupportReservation()
    {
        var s=Fresh();var game=Live(s);var a=Facts(s);var context=Field<object>(s,"expansion");var plan=Assert.Single(Field<List<ColonisationAiPlanDto>>(context,"Plans"));
        var m=a.ManifestRecords!.Single(m=>m.OwnerId==plan.ActorId&&m.Kind==SettlerManifestKind.ImmigrationOnly&&m.Stage==SettlerManifestStage.Loaded);var receiver=game.Colonies!.Single(c=>c.ColonyId==plan.EstablishedColonyId);
        var projection=(bool)Call(s,"GrowthReceiver",receiver,m.Passengers!,game)!;Assert.True(projection);
        var foreign=game.Colonies!.First(c=>c.OwnerCivilizationId!=plan.ActorId);foreign.Food+=1234;a.FoodProduced+=1234;
        Assert.Equal(projection,(bool)Call(s,"GrowthReceiver",receiver,m.Passengers!,game)!);
        // Explicit own-cohort redistribution fixture, no invented people and no replay claim.
        var home=game.Colonies!.Single(c=>c.ColonyId==plan.HomeColonyId);home.Cohorts![0].Population-=200;receiver.Cohorts![0].Population+=200;
        Assert.False((bool)Call(s,"GrowthReceiver",receiver,m.Passengers!,game)!);
        // A completed/aborted goal still reserves its real carrier for ordinary food support.
        m.Stage=SettlerManifestStage.Unloaded;m.Passengers=null;plan.Stage=ColonisationAiStage.GrowthClosed;
        var fleet=game.Ships!.Single(ship=>ship.ShipId==plan.TransportShipId).FleetId;
        Assert.True((bool)Call(s,"MissionFleet",game,fleet)!);Assert.Equal(ColonisationAiStage.GrowthClosed,plan.Stage);
    }
    [Fact]public void ClosedLostImmigrationUsesPaidReplacementOnlyForActualSupplyNeed()
    {
        var s=Fresh();
        for(int n=0;n<16&&s.ExportGrowthSave().AiPlans![0].Stage!=ColonisationAiStage.GrowthClosed;n++)Advance(s);
        var game=Live(s);var a=Facts(s);var plan=Assert.Single(Field<List<ColonisationAiPlanDto>>(Field<object>(s,"expansion"),"Plans"));
        var actor=plan.ActorId;var m=Assert.Single(a.ManifestRecords!,m=>m.OwnerId==actor&&m.Kind==SettlerManifestKind.ImmigrationOnly);
        Assert.Equal(SettlerManifestStage.Unloaded,m.Stage);
        CampaignCombatConsequences.Apply(game,new CombatResultDto{ShipChanges=new(){new(){ShipId=m.ShipId,RemainingHull=0}},FleetChanges=new()});
        Field<CampaignTacticalReviewSaveDto>(s,"state").Pools.RemoveAll(pool=>pool.ShipId==m.ShipId);
        // Controlled loss/shortage fixture only. Redistribute existing own people,
        // consume existing food honestly; subsequent procurement is real streamed work.
        var home=game.Colonies!.Single(c=>c.ColonyId==plan.HomeColonyId);var world=game.Colonies!.Single(c=>c.ColonyId==plan.EstablishedColonyId);
        home.Cohorts![0].Population-=300;world.Cohorts![0].Population+=300;a.FoodConsumed+=world.Food;world.Food=0;
        Call(s,"PlanExpansion",Call(s,"TravelCopy"));var root=s.ExportGrowthSave();plan=Assert.Single(root.AiPlans!);
        Assert.Equal(ColonisationAiStage.GrowthClosed,plan.Stage);Assert.Contains(plan.LatestActions!,r=>r.Action==ColonisationAiAction.Ship&&r.Accepted);
        var queue=Assert.Single(root.BaseCampaign!.Production!,q=>q.QueueItemId==plan.TransportQueueId&&q.OwnerId==actor&&q.Kind==ColonisationProductionKind.PublishedShip);
        Assert.Equal(0,queue.CompletedIndustry);Assert.All(queue.ConsumedResources!,r=>Assert.Equal(0,r.Amount));
        var queueId=queue.QueueItemId;bool partial=false,supply=false;
        for(int n=0;n<80;n++)
        {
            if(s.CaptureStatus().Phase==SessionPhase.Planning)Advance(s);
            else
            {
                var encounters=s.Encounters(Human);Assert.InRange(encounters.Count,0,8);
                foreach(var id in encounters)
                {
                    for(int round=0;round<16&&!s.Observe(id,Human).Complete;round++)
                    {var v=s.Observe(id,Human);Assert.True(s.CommitRound(new(){ActorId=Human,EncounterId=id,CommandId="command.growth.support."+id+"."+v.Turn+"."+v.Round,ExpectedTurn=v.Turn,ExpectedRound=v.Round,ExpectedBoundaryRevision=v.BoundaryRevision,Replacements=new()}).Accepted,s.LastPhysicalFailure);}
                    Assert.True(s.Observe(id,Human).Complete);
                }
                var status=s.CaptureStatus();Assert.True(s.FinalizeBatch(status.Turn,status.CampaignRevision).Accepted,s.LastPhysicalFailure);
            }
            root=s.ExportGrowthSave();
            partial|=root.BaseCampaign!.Production!.Any(q=>q.QueueItemId==queueId&&q.CompletedIndustry>0&&q.CompletedIndustry<q.RequiredIndustry);
            Assert.Single(root.BaseCampaign.Colonisation!.ManifestRecords!,r=>r.OwnerId==actor&&r.Kind==SettlerManifestKind.ImmigrationOnly);
            var built=root.BaseCampaign.ProductionHistory!.SingleOrDefault(r=>r.Order!.QueueItemId==queueId&&r.Completed);
            supply=built!=null&&root.BaseCampaign.Colonisation.ManifestRecords!.Any(r=>r.OwnerId==actor&&r.ShipId==built.ShipId&&r.Kind==SettlerManifestKind.SupplyOnly);
            if(supply)break;
        }
        Assert.True(partial,Json(new{root.AiPlans,root.BaseCampaign!.Production,world=Game(root.BaseCampaign).Colonies!.Where(c=>c.OwnerCivilizationId==actor),history=root.BaseCampaign.ProductionHistory}));Assert.True(supply,"Replacement completed but no actual supply manifest: "+Json(root.AiPlans!));
        var completion=Assert.Single(root.BaseCampaign!.ProductionHistory!,r=>r.Order!.QueueItemId==queueId);Assert.True(completion.Completed);
        Assert.Equal(completion.Order!.RequiredIndustry,completion.Order.CompletedIndustry);Assert.All(completion.Order.RequiredResources!,r=>Assert.Equal(r.Amount,completion.Order.ConsumedResources!.Single(c=>c.ResourceDefinitionId==r.ResourceDefinitionId).Amount));
        Assert.Equal(ColonisationAiStage.GrowthClosed,Assert.Single(root.AiPlans!).Stage);
    }

}


