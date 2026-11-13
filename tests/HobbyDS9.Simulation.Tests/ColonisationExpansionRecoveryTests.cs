using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationExpansionTests;
namespace HobbyDS9.Simulation.Tests;
public sealed class ColonisationExpansionRecoveryTests
{
    private const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    private static object Call(CampaignTacticalReviewSession s,string name,params object[] args)=>typeof(CampaignTacticalReviewSession).GetMethod(name,Private)!.Invoke(s,args)!;
    private static GameStateDto Live(CampaignTacticalReviewSession s)=>((CampaignTacticalReviewSaveDto)typeof(CampaignTacticalReviewSession).GetField("state",Private)!.GetValue(s)!).Campaign.GameState;
    private static CampaignColonisationStateDto Context(CampaignTacticalReviewSession s)
    {var c=typeof(CampaignTacticalReviewSession).GetField("colonisation",Private)!.GetValue(s)!;return (CampaignColonisationStateDto)c.GetType().GetField("State",Private)!.GetValue(c)!;}
    private sealed record Branches(string Founding,string Established);
    private static readonly Lazy<Branches> ActualSmallBranches=new(()=>{
        var s=CampaignTacticalReviewSession.StartColonisationExpansion(Package(),PublishedConstructionProfileTests.Settings(30));string? founding=null;
        for(var i=0;i<100;i++)
        {
            var root=s.ExportColonisationExpansionSave();var p=Assert.Single(root.AiPlans!);
            if(p.CampId!=null&&p.EstablishedColonyId==null)founding??=Json(root);
            if(p.EstablishedColonyId!=null)return new Branches(founding!,Json(root));
            Assert.Equal(SessionPhase.Planning,s.CaptureStatus().Phase);ColonisationTransportTests.Advance(s);
        }
        throw new InvalidOperationException("Actual small public expedition did not establish within bound.");
    });
    [Theory][InlineData(false)][InlineData(true)]
    public void ControlledLowFoodCampUsesRealEvacuationAndRetainsReturnCargoWhenRouteUnavailable(bool blocked)
    {
        // ONLY this local failure fixture reduces current food, charging the same
        // conserved global consumed counter. It is not presented as loadable history.
        var root=JsonSerializer.Deserialize<CampaignColonisationExpansionSaveDto>(ActualSmallBranches.Value.Founding)!;
        var s=CampaignTacticalReviewSession.LoadColonisationExpansion(Package(),root);var p=Assert.Single(root.AiPlans!);var game=Live(s);var a=Context(s);
        var camp=a.CampSources!.Single(c=>c.CampId==p.CampId);var world=game.Colonies!.Single(c=>c.ColonyId==camp.ColonyId);var before=world.Cohorts!.Sum(c=>c.Population);
        a.FoodConsumed=checked(a.FoodConsumed+world.Food-1);world.Food=1;
        var ship=game.Ships!.Single(s=>s.ShipId==p.TransportShipId);var node=game.Fleets!.Single(f=>f.FleetId==ship.FleetId).LocationNodeId;
        if(blocked)game.NavigationKnowledge!.Single(k=>k.CivilizationId==p.ActorId).Connections!.Clear();
        Call(s,"PlanExpansion",Call(s,"TravelCopy"));var after=s.ExportColonisationExpansionSave();var plan=Assert.Single(after.AiPlans!);
        var returned=Assert.Single(after.BaseCampaign!.Colonisation!.ManifestRecords!,m=>m.Kind==SettlerManifestKind.ReturnOnly&&m.OwnerId==p.ActorId);
        Assert.Equal(before,returned.Passengers!.Population);Assert.Equal(1,returned.Food);Assert.Equal(0,returned.PassengerDeaths);Assert.Equal(SettlerManifestStage.Loaded,returned.Stage);
        Assert.Equal(ColonyEstablishmentStage.EmptyOutpost,after.BaseCampaign.Colonisation.CampSources!.Single(c=>c.CampId==p.CampId).Stage);
        Assert.Equal(0,ColonisationTransportTests.Game(after.BaseCampaign).Colonies!.Single(c=>c.ColonyId==camp.ColonyId).Cohorts!.Sum(c=>c.Population));
        Assert.Contains(plan.LatestActions!,r=>r.Action==ColonisationAiAction.Evacuate&&r.Accepted);Assert.Equal(ColonisationAiStage.Recovering,plan.Stage);
        if(blocked)
        {
            Assert.Equal(node,ColonisationTransportTests.Game(after.BaseCampaign).Fleets!.Single(f=>f.FleetId==ship.FleetId).LocationNodeId);
            Assert.DoesNotContain(plan.LatestActions!,r=>r.Action==ColonisationAiAction.Travel&&r.Accepted);Assert.Contains("route",plan.HoldReason,StringComparison.OrdinalIgnoreCase);
        }
    }
    [Fact]public void ActualEstablishedSelfSufficientMissionReleasesFleetAndYardToOrdinaryAi()
    {
        var root=JsonSerializer.Deserialize<CampaignColonisationExpansionSaveDto>(ActualSmallBranches.Value.Established)!;var p=Assert.Single(root.AiPlans!);
        var s=CampaignTacticalReviewSession.LoadColonisationExpansion(Package(),root);var game=Live(s);var fleet=game.Ships!.Single(ship=>ship.ShipId==p.TransportShipId).FleetId;
        Assert.False((bool)Call(s,"MissionWorld",p.ActorId,p.HomeColonyId!));Assert.False((bool)Call(s,"MissionWorld",p.ActorId,p.YardColonyId!));
        Assert.False((bool)Call(s,"MissionFleet",game,fleet));ColonisationTransportTests.Advance(s);
        var after=s.ExportColonisationExpansionSave();Assert.Contains(after.BaseCampaign!.Campaign!.Integrated!.Physical!.LastMovement,m=>m.FleetId==fleet&&m.EdgesCompleted>0);
        Assert.Equal(Json(after),Json(CampaignTacticalReviewSession.LoadColonisationExpansion(Package(),after).ExportColonisationExpansionSave()));
    }
    [Theory][InlineData(1,0)][InlineData(3,1)]
    public void OptionalMissionCannotConsumeMandatoryAdvanceOrQueueTerminalRevisionReserve(int remaining,int acceptedQueues)
    {
        // Explicit arithmetic boundary fixture, not fabricated replay history.
        var s=CampaignTacticalReviewSession.StartColonisationExpansion(Package(),PublishedConstructionProfileTests.Settings(30));Live(s).StateRevision=long.MaxValue-remaining;
        ColonisationTransportTests.Advance(s);var root=s.ExportColonisationExpansionSave();Assert.Equal(acceptedQueues,root.BaseCampaign!.Production!.Count(q=>q.OwnerId=="civ.cardassian"));
        Assert.Contains(root.AiPlans![0].LatestActions!,r=>!r.Accepted&&r.Error==CampaignTacticalError.Overflow);
        if(remaining==1)
        {
            var before=Json(root);var stamp=s.CaptureStatus();Assert.False(s.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);Assert.Equal(before,Json(s.ExportColonisationExpansionSave()));
        }
    }
}
