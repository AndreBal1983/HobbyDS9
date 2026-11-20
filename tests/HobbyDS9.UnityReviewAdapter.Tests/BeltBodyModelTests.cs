using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class BeltBodyModelTests:IDisposable
{
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Belt-Model-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    static byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
    ReviewUiModel Model(bool belts=true)
    {
        var template=ReviewExactPackageLoader.LoadPlayableFleet(Read("playable-fleet-v1","playable"),Read("playable-fleet-v1","package"),Read("playable-fleet-v1","physical"),Read("playable-fleet-v1","core"),Read("sol-eight-planets-v1","scenario"),codec);
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        var colony=ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
        var roster=ColonisationPlaytestOptions.CreateRosterPackage(colony,Read("colonisation-roster-v2","roster"),codec);
        var growth=ColonisationGrowthPlaytestOptions.CreatePackage(roster,Read("colonisation-expansion-v1","planner"),codec);
        CampaignColonisationCyclePackageDto source=belts?BeltBodiesPlaytestOptions.CreatePackage(growth):EngineeringRefinementPlaytestOptions.CreatePackage(growth);
        return new ReviewUiModel(source,new NaturalGalaxySettingsDto{Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,
            HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}},codec,new ReviewColonisationCycleSlotStore(directory,codec));
    }
    static PhysicalReviewHost Host(ReviewUiModel m)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)!;
    byte[] Wire(ReviewUiModel m)=>codec.EncodeCycleRoot(((ICampaignColonisationCycleSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Host(m))!).ExportCycleSave());
    static BeltBodyCommandDto Survey(ReviewUiModel m)
    {
        var frame=m.Current.Frame;var body=m.CaptureBeltBodies(frame.Actor)!.Bodies.Single(b=>b.FeatureId==CampaignAuthoredSolIdentity.FeatureId);
        return new(){ActorId=frame.Actor,CommandId="command.belt.model.survey",ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
            Action=BeltBodyAction.StartSurvey,BodyId=body.BodyId,ShipId=body.SurveyShipIds.First()};
    }
    [Fact]public async Task RealPreviewIsReadOnlyThenSubmissionPublishesOnceAndPreservesDraft()
    {
        var m=Model();var frame=m.Current.Frame;var before=Wire(m);Assert.True(m.SupportsBeltBodies);Assert.True(m.SupportsEngineeringRefinement);
        Assert.Null(m.CaptureBeltBodies("civ.cardassian"));var command=Survey(m);
        var draft=m.ResearchInputs()!;draft.SetWeight(ScienceDiscipline.Energy,700);var weights=draft.Values().Select(v=>v.Weight).ToArray();
        var preview=m.BeginBeltBodyInteraction(BeltBodyInteractionKind.Preview,command)!;Assert.NotNull(preview);Assert.True(m.Busy);
        Assert.True(m.SupportsBeltBodies);Assert.Null(m.CaptureBeltBodies(frame.Actor));
        var checkedAction=await Task.Run(preview.Execute);Assert.True(checkedAction.Preview!.CanSubmit);
        Assert.True(m.CompleteBeltBodyInteraction(preview,checkedAction),m.Message);Assert.False(m.Busy);Assert.Same(frame,m.Current.Frame);Assert.Equal(before,Wire(m));
        var work=m.BeginBeltBodyInteraction(BeltBodyInteractionKind.Submission,command)!;
        var completion=await Task.Run(work.Execute);Assert.True(completion.Result!.Accepted);
        Assert.True(m.CompleteBeltBodyInteraction(work,completion),m.Message);Assert.True(completion.Committed);Assert.False(m.Busy);
        Assert.False(m.CompleteBeltBodyInteraction(work,completion));Assert.NotSame(frame,m.Current.Frame);
        Assert.True(m.ResearchDraft!.Dirty);Assert.True(m.ResearchDraft.Current(m.Current.Frame));Assert.Equal(weights,m.ResearchDraft.Values().Select(v=>v.Weight));
        Assert.NotNull(m.CaptureBeltBodies(m.Current.Frame.Actor)!.Bodies.Single(b=>b.BodyId==command.BodyId).ActiveSurveyId);
        Assert.Null(m.BeginBeltBodyInteraction(BeltBodyInteractionKind.Submission,command));Assert.False(m.Busy);
    }
    [Fact]public async Task CancelledWorkerRetainsBusyUntilSettledAndPublishesNothing()
    {
        var m=Model();var frame=m.Current.Frame;var before=Wire(m);var host=Host(m);var command=Survey(m);
        var probe=typeof(PhysicalReviewHost).GetProperty("BeltBodyInteractionProbe",BindingFlags.Instance|BindingFlags.NonPublic)!;
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        probe.SetValue(host,(Action<string>)(phase=>{if(phase=="before-fork"){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(15)))throw new TimeoutException();}}));
        var work=m.BeginBeltBodyInteraction(BeltBodyInteractionKind.Submission,command)!;var task=Task.Run(work.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));m.AbandonBeltBodyInteraction(work);Assert.True(m.Busy);
            Assert.Null(m.BeginBeltBodyInteraction(BeltBodyInteractionKind.Preview,command));release.Set();
            var completion=await task.WaitAsync(TimeSpan.FromSeconds(20));Assert.False(m.CompleteBeltBodyInteraction(work,completion));
            Assert.False(m.Busy);Assert.False(completion.Committed);Assert.Same(frame,m.Current.Frame);Assert.Equal(before,Wire(m));
        }
        finally{release.Set();await task.WaitAsync(TimeSpan.FromSeconds(20));probe.SetValue(host,null);m.AbandonBeltBodyInteraction(work);}
    }
    [Fact]public void PriorRefinementEditionKeepsItsCapabilityWithoutBeltActions()
    {
        var m=Model(false);var frame=m.Current.Frame;var before=Wire(m);
        Assert.True(m.SupportsEngineeringRefinement);Assert.False(m.SupportsBeltBodies);Assert.Null(m.CaptureBeltBodies(frame.Actor));
        Assert.Null(m.BeginBeltBodyInteraction(BeltBodyInteractionKind.Preview,new(){ActorId=frame.Actor,ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
            CommandId="command.old.belt",BodyId="body."+CampaignAuthoredSolIdentity.FeatureId,Action=BeltBodyAction.StartSurvey}));
        Assert.False(m.Busy);Assert.Same(frame,m.Current.Frame);Assert.Equal(before,Wire(m));
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
