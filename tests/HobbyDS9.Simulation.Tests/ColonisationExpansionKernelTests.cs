using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationExpansionTests;
namespace HobbyDS9.Simulation.Tests;
// Deliberately isolated planning-phase/kernel fixtures. They do not claim that
// a privately poisoned state is an admissible save or a public journey.
public sealed class ColonisationExpansionKernelTests
{
    private const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    private static object Call(CampaignTacticalReviewSession s,string name,params object[] args)=>typeof(CampaignTacticalReviewSession).GetMethod(name,Private)!.Invoke(s,args)!;
    private static GameStateDto Live(CampaignTacticalReviewSession s)=>((CampaignTacticalReviewSaveDto)typeof(CampaignTacticalReviewSession).GetField("state",Private)!.GetValue(s)!).Campaign.GameState;
    private static ColonisationAiPlanDto Plan(CampaignTacticalReviewSession s)
    {var c=typeof(CampaignTacticalReviewSession).GetField("expansion",Private)!.GetValue(s)!;return Assert.Single((List<ColonisationAiPlanDto>)c.GetType().GetField("Plans",Private)!.GetValue(c)!);}
    private static void Phase(CampaignTacticalReviewSession s)=>Call(s,"PlanExpansion",Call(s,"TravelCopy"));
    private static CampaignTacticalReviewSession Fresh()=>CampaignTacticalReviewSession.StartColonisationExpansion(Package(),PublishedConstructionProfileTests.Settings(30));
    [Fact]public void StagedPlanningUsesOrdinaryUnworkedQueuesWithoutUpfrontTreasuryOrStockDebit()
    {
        var s=Fresh();var before=s.ExportColonisationExpansionSave();var g=ColonisationTransportTests.Game(before.BaseCampaign!);Phase(s);var after=s.ExportColonisationExpansionSave();var next=ColonisationTransportTests.Game(after.BaseCampaign!);
        Assert.Equal(Json(g.Civilizations!.Select(c=>c.Credits)),Json(next.Civilizations!.Select(c=>c.Credits)));
        Assert.Equal(Json(g.Colonies!.Select(c=>c.Stockpiles)),Json(next.Colonies!.Select(c=>c.Stockpiles)));Assert.Equal(Json(g.Colonies!.Select(c=>c.Cohorts)),Json(next.Colonies!.Select(c=>c.Cohorts)));
        Assert.Contains(after.BaseCampaign!.Production!,q=>q.Kind==ColonisationProductionKind.PublishedShip&&q.RequiredIndustry==141&&q.RequiredResources!.Single().Amount==3);
        Assert.Contains(after.BaseCampaign.Production!,q=>q.Kind==ColonisationProductionKind.SettlementKit&&q.RequiredIndustry==60&&q.RequiredResources!.Single().Amount==6);
        Assert.All(after.BaseCampaign.Production!,q=>{Assert.Equal(0,q.CompletedIndustry);Assert.All(q.ConsumedResources!,r=>Assert.Equal(0,r.Amount));});
        var p=Assert.Single(after.AiPlans!);Assert.Equal(g.StateRevision+p.LatestActions!.Count(a=>a.Accepted),next.StateRevision);
        Assert.Empty(after.BaseCampaign.Journal!);Assert.DoesNotContain(after.BaseCampaign.Campaign!.Integrated!.Physical!.Core.AcceptedRoundCommands,id=>id.StartsWith("ai-command.",StringComparison.Ordinal));
    }
    [Fact]public void RejectedActionAttemptsCountTowardExactBoundWithoutChangingOwnedGame()
    {
        var s=Fresh();Phase(s);var p=Plan(s);var game=Json(Live(s));var initial=p.LatestActions!.Count;var calls=0;
        Func<CampaignTacticalReviewSession,string,CampaignTacticalError> reject=(c,id)=>{calls++;return CampaignTacticalError.InvalidOrder;};
        for(var i=initial;i<10;i++)Assert.False((bool)Call(s,"ExpansionAttempt",p.ActorId,ColonisationAiAction.Embark,reject));
        Assert.Equal(8-initial,calls);Assert.Equal(8,Plan(s).LatestActions!.Count);Assert.Equal(game,Json(Live(s)));
        Assert.All(Plan(s).LatestActions!.Skip(initial),r=>{Assert.False(r.Accepted);Assert.Equal(r.BeforeRevision,r.AfterRevision);});
        Assert.NotNull(s.ExportColonisationExpansionSave());
    }
    [Fact]public void HomeFoodReserveUsesActualDemandAndIsPureForSupplyAndPassengerPlanning()
    {
        var s=Fresh();var before=Json(s.ExportColonisationExpansionSave());var copy=SimulationSession.CopyState(Live(s));var p=Plan(s);var home=copy.Colonies!.Single(c=>c.OwnerCivilizationId==p.ActorId);
        home.Cohorts![0].Population=2000;home.Food=20;
        var output=(WorldEconomySnapshot)Call(s,"ProjectOwnedWorld",home,copy);var expected=Math.Max(0,home.Food-Math.Max(0,output.FoodDemand-output.FoodOutput)*2);
        Assert.True(expected<20);Assert.Equal(expected,(long)Call(s,"ExpansionFoodAvailable",home,copy));
        home.Food=long.MaxValue;Assert.InRange((long)Call(s,"ExpansionFoodAvailable",home,copy),0,long.MaxValue);Assert.Equal(before,Json(s.ExportColonisationExpansionSave()));
    }
    [Fact]public void UnobservedForeignTreasuryAndFleetLocationDoNotEnterOwnPlanningChoices()
    {
        var a=Fresh();var b=Fresh();var g=Live(b);var human=g.Civilizations!.Single(c=>c.ControllerKind==ControllerKind.Human);human.Credits+=1000;
        var fleet=g.Fleets!.Single(f=>f.OwnerCivilizationId==human.CivilizationId);var knowledge=g.NavigationKnowledge!.Single(k=>k.CivilizationId==human.CivilizationId);
        fleet.LocationNodeId=knowledge.Nodes!.Where(n=>n.Level!=NavigationKnowledgeLevel.Unknown&&n.Id!=fleet.LocationNodeId).OrderBy(n=>n.Id,StringComparer.Ordinal).First().Id;
        Phase(a);Phase(b);var x=Plan(a);var y=Plan(b);
        Assert.Equal(x.HomeColonyId,y.HomeColonyId);Assert.Equal(x.YardColonyId,y.YardColonyId);Assert.Equal(x.TargetPlanetId,y.TargetPlanetId);Assert.Equal(x.Stage,y.Stage);
        Assert.Equal(x.LatestActions!.Select(r=>(r.Action,r.Accepted)),y.LatestActions!.Select(r=>(r.Action,r.Accepted)));
        var ta=(CampaignTacticalTravelSaveDto)Call(a,"TravelCopy");var tb=(CampaignTacticalTravelSaveDto)Call(b,"TravelCopy");
        Assert.Equal(Json(ta.Orders.Where(o=>Live(a).Fleets!.Any(f=>f.FleetId==o.FleetId&&f.OwnerCivilizationId==x.ActorId))),Json(tb.Orders.Where(o=>Live(b).Fleets!.Any(f=>f.FleetId==o.FleetId&&f.OwnerCivilizationId==y.ActorId))));
    }
    [Fact]public void FixedFuturePlanReservationIsIdenticalEmptyOrFullAtExactCombinedBudgetEdge()
    {
        var p=Package();var empty=Fresh().ExportColonisationExpansionSave();var full=JsonSerializer.Deserialize<CampaignColonisationExpansionSaveDto>(Json(empty))!;var plan=full.AiPlans![0];
        foreach(var property in typeof(ColonisationAiPlanDto).GetProperties().Where(p=>p.PropertyType==typeof(string)&&p.Name!=nameof(ColonisationAiPlanDto.GoalId)&&p.Name!=nameof(ColonisationAiPlanDto.HoldReason)))property.SetValue(plan,new string('\u2603',512));
        plan.HoldReason=new string('\u2603',128);plan.OwnAttemptedTargets=Enumerable.Range(0,8).Select(_=>new string('\u2603',512)).ToList();
        plan.LatestActions=Enumerable.Range(0,8).Select(_=>new ColonisationAiActionReceiptDto{CommandId=new string('a',128),Action=ColonisationAiAction.Travel,Error=CampaignTacticalError.None}).ToList();
        foreach(var work in new[]{true,false})
        {
            long Edge(CampaignColonisationExpansionSaveDto root)
            {
                var low=1L;var high=work?2000000L:67108864L;
                while(low<high){var mid=low+(high-low)/2;var policy=ColonisationData.Copy(p.BaseRosterV2!.BaseV1!.Settlement!);if(work)policy.MaximumValidationVisits=mid;else policy.MaximumCanonicalRootBytes=mid;
                    try{ColonisationRootBudget.ValidateExpansion(root,policy);high=mid;}catch(ArgumentException){low=mid+1;}}
                return low;
            }
            var edge=Edge(empty);Assert.Equal(edge,Edge(full));var policy=ColonisationData.Copy(p.BaseRosterV2!.BaseV1!.Settlement!);
            if(work)policy.MaximumValidationVisits=edge;else policy.MaximumCanonicalRootBytes=edge;ColonisationRootBudget.ValidateExpansion(full,policy);
            if(!work)Assert.True(JsonSerializer.SerializeToUtf8Bytes(full).LongLength<=edge);
            if(work)policy.MaximumValidationVisits--;else policy.MaximumCanonicalRootBytes--;Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.ValidateExpansion(full,policy));
        }
    }

    [Fact]public void SixteenNearerKnownUnsuitableBodiesCannotHideFartherObservedSuitableTarget()
    {
        var session=Fresh();var root=session.ExportColonisationExpansionSave();var p=Assert.Single(root.AiPlans!);var game=ColonisationTransportTests.Game(root.BaseCampaign!);
        var home=game.Colonies!.Single(c=>c.OwnerCivilizationId==p.ActorId);var cohort=home.Cohorts![0];var planet=game.Planets!.Single(w=>w.PlanetId==home.PlanetId);
        var environment=root.BaseCampaign!.FrozenPlanets!.Definitions!.Single(d=>d.DefinitionId==planet.DefinitionId);
        var observations=new List<ColonisationPlanetObservationDto>();var costs=new Dictionary<string,System.Numerics.BigInteger>();
        for(var n=0;n<17;n++)
        {
            var body=new ColonisationPlanetObservationDto{PlanetId="planet.observed."+n,SystemId="system.observed."+n,Surveyed=true,KnownControl=new(){Kind=PlanetControllerKind.Unclaimed},Environment=PopulationData.Copy(environment)};
            if(n<16)body.Environment.Gravity=1;observations.Add(body);costs.Add(body.SystemId,n+1);
        }
        Assert.Equal(observations[16],Call(session,"SelectExpansionBody",observations,p,cohort,costs));
        var unknown=new ColonisationPlanetObservationDto{PlanetId="planet.unknown",SystemId="system.unknown",Surveyed=false};costs.Add(unknown.SystemId,0);
        Assert.Equal(unknown,Call(session,"SelectExpansionBody",new[]{unknown},p,cohort,costs));
        unknown.Environment=new(){Gravity=int.MaxValue};Assert.Equal(unknown,Call(session,"SelectExpansionBody",new[]{unknown},p,cohort,costs));
    }
}

