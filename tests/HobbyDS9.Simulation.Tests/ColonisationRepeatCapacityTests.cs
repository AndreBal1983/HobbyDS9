using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;
namespace HobbyDS9.Simulation.Tests;
// Exact counter-edge changes are defensive fixtures, not admissible altered replay histories.
public sealed class ColonisationRepeatCapacityTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Private)!.GetValue(o)!;
    static object Context(CampaignTacticalReviewSession s)=>Field<object>(s,"colonisation");
    static GameStateDto Live(CampaignTacticalReviewSession s)=>Field<CampaignTacticalReviewSaveDto>(s,"state").Campaign.GameState;
    static ColonisationPolicyDto Policy(CampaignTacticalReviewSession s)=>Field<ColonisationPolicyDto>(Context(s),"Policy");
    static bool Capacity(CampaignTacticalReviewSession s)=>(bool)typeof(CampaignTacticalReviewSession).GetMethod("ColonisationCapacity",Private)!.Invoke(s,new object?[]{0,null,0})!;
    static string Json(object x)=>JsonSerializer.Serialize(x);
    static readonly Lazy<string> ActualTwoActorTransport=new(()=>{
        var s=CampaignTacticalReviewSession.StartRepeatGame(ColonisationRepeatTests.Package(),PublishedConstructionProfileTests.Settings(250));var f=s.CaptureStatus();
        Assert.True(s.SubmitActivity(new(){ActorId=Human,CommandId="command.repeat.capacity.transport",ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.mars",DefinitionId="blueprint.ready.galaxy-colony-transport",BlueprintRevision=1}).Accepted,s.LastPhysicalFailure);
        for(int n=0;n<=128;n++){var root=s.ExportRepeatSave();var id=root.RepeatGoals![0].SettlementManifestId;
            if(id!=null&&root.BaseCampaign!.PublishedBuiltShips!.Any(x=>x.OwnerCivilizationId==Human)&&root.BaseCampaign.Colonisation!.ManifestRecords!.Any(m=>m.ManifestId==id&&m.Stage==SettlerManifestStage.Loaded))return Json(root);
            if(n<128)Advance(s);}
        throw new InvalidOperationException("Real second AI cargo and human manufactured transport absent within128");});
    static CampaignTacticalReviewSession Fresh()=>CampaignTacticalReviewSession.LoadRepeatSave(ColonisationRepeatTests.Package(),JsonSerializer.Deserialize<CampaignColonisationRepeatSaveDto>(ActualTwoActorTransport.Value)!);
    static ColonisationCommandDto Load(CampaignTacticalReviewSession s){var root=s.ExportRepeatSave();var home=Game(root.BaseCampaign!).Colonies!.Single(c=>c.ColonyId=="colony.sol");var ship=Assert.Single(root.BaseCampaign!.PublishedBuiltShips!,x=>x.OwnerCivilizationId==Human);var c=Command(s,ColonisationAction.LoadPassengers);c.Embark=new(){ShipId=ship.ShipId!,ColonyId=home.ColonyId,CohortId=home.Cohorts!.First(x=>x.Population>=400).CohortId,Population=100,Food=20};return c;}
    static int Minimum(CampaignTacticalReviewSession s,bool record){var p=Policy(s);var old=record?p.RecordBudget:p.JournalBudget;int lo=0,hi=old;while(lo<hi){var mid=lo+(hi-lo)/2;if(record)p.RecordBudget=mid;else p.JournalBudget=mid;if(Capacity(s))hi=mid;else lo=mid+1;}if(record)p.RecordBudget=old;else p.JournalBudget=old;return lo;}
    [Fact]public void AllActorLiveSecondSettlementAndNewPassengersConvertExactlyTwoRecordsAndOneTerminal()
    {
        var s=Fresh();var initial=s.ExportRepeatSave();Assert.Contains(initial.BaseCampaign!.Colonisation!.ManifestRecords!,m=>m.OwnerId!=Human&&m.Kind==SettlerManifestKind.Settlement&&m.Stage==SettlerManifestStage.Loaded);
        var r=Minimum(s,true);var j=Minimum(s,false);Policy(s).RecordBudget=r+3;var before=Json(s.ExportRepeatSave());Assert.False(s.SubmitColonisation(Load(s)).Accepted);Assert.Equal(before,Json(s.ExportRepeatSave()));
        Policy(s).RecordBudget=r+4;Policy(s).JournalBudget=j+1;before=Json(s.ExportRepeatSave());Assert.False(s.SubmitColonisation(Load(s)).Accepted);Assert.Equal(before,Json(s.ExportRepeatSave()));
        Policy(s).JournalBudget=j+2;Assert.True(s.SubmitColonisation(Load(s)).Accepted,s.LastPhysicalFailure);Assert.Equal(r+4,Minimum(s,true));Assert.Equal(j+2,Minimum(s,false));
        var m=s.ExportRepeatSave().BaseCampaign!.Colonisation!.ManifestRecords!.Single(m=>m.OwnerId==Human&&m.Stage==SettlerManifestStage.Loaded);var unload=Command(s,ColonisationAction.UnloadTransport);unload.Unload=new(){ManifestId=m.ManifestId,ColonyId="colony.sol"};Assert.True(s.SubmitColonisation(unload).Accepted,s.LastPhysicalFailure);
        Assert.Equal(r+4,Minimum(s,true));Assert.Equal(j+2,Minimum(s,false));Assert.True(Capacity(s));Assert.Equal(initial.RepeatGoals![0].SettlementManifestId,s.ExportRepeatSave().RepeatGoals![0].SettlementManifestId);
    }
    [Theory][InlineData(true)][InlineData(false)]public void ActualLoadUsesTwoNewIdsButReservesZeroFuturePassengerIds(bool oneBelow)
    {
        var s=Fresh();var root=s.ExportRepeatSave().BaseCampaign!;var a=root.Colonisation!;
        var future=(ulong)(2*root.Production!.Count+6*a.ManifestRecords!.Count(m=>m.Stage==SettlerManifestStage.Loaded&&m.Kind==SettlerManifestKind.Settlement)+3*a.CampSources!.Count(c=>c.Stage==ColonyEstablishmentStage.Founding)+a.ManifestRecords!.Count(m=>m.Stage==SettlerManifestStage.Loaded&&m.Kind==SettlerManifestKind.ReturnOnly));
        Live(s).NextEntitySequence=ulong.MaxValue-future-(oneBelow?1UL:2UL);var before=Json(s.ExportRepeatSave());var seq=Live(s).NextEntitySequence;Assert.Equal(!oneBelow,s.SubmitColonisation(Load(s)).Accepted);
        if(oneBelow)Assert.Equal(before,Json(s.ExportRepeatSave()));else{Assert.Equal(seq+2,Live(s).NextEntitySequence);Assert.True(Capacity(s));}
    }
}
