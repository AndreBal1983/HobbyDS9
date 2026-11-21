using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class ColonisationRosterIntegrationTests:IDisposable
{
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Colonisation-Roster-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    CampaignColonisationPackageDto Base()
    {
        byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        return ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
    }
    byte[] Resource()=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/colonisation-roster-v2/roster.json"));
    CampaignColonisationRosterPackageDto Package()=>ColonisationPlaytestOptions.CreateRosterPackage(Base(),Resource(),codec);
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    ReviewColonisationSlotStore Store()=>ReviewColonisationSlotStore.CreateRosterV2(directory,codec);
    ReviewUiModel Model()=>new(Package(),Settings(),codec,Store());
    static PhysicalReviewHost Host(ReviewUiModel m)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)!;
    static ICampaignColonisationSession Session(ReviewUiModel m)=>(ICampaignColonisationSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Host(m))!;
    byte[] Root(ReviewUiModel m)=>codec.EncodeColonisationRoot(Session(m).ExportColonisationSave());
    static ColonisationCommandDto Kit(ReviewUiModel m)=>new(){ActorId=m.Current.Frame.Actor,CommandId="command.roster.kit",Turn=m.Current.Frame.Status.Turn,Revision=m.Current.Frame.Status.CampaignRevision,Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}};
    [Fact]public void PinnedCompositionAndTwoNamespacesNeverReinterpretOneAnothersSlots()
    {
        var p=Package();var model=Model();Assert.True(model.ColonisationRosterCampaign);Assert.True(model.ColonisationCampaign);
        var bytes=Resource();bytes[0]^=1;Assert.Throws<ArgumentException>(()=>ColonisationPlaytestOptions.CreateRosterPackage(Base(),bytes,codec));
        var store=Store();var oldStore=new ReviewColonisationSlotStore(directory,codec);var old=CampaignColonisationApplication.StartGame(p.BaseV1!,Settings()).ExportColonisationSave();var root=Session(model).ExportColonisationSave();
        oldStore.Write("same",old,false);store.Write("same",root,false);
        Assert.True(File.Exists(Path.Combine(directory,"unity-campaign-colonisation-v1-same.json")));Assert.True(File.Exists(Path.Combine(directory,"unity-campaign-colonisation-v2-same.json")));
        Assert.Equal(codec.EncodeColonisationRoot(old),codec.EncodeColonisationRoot(oldStore.Read("same")));Assert.Equal(Root(model),codec.EncodeColonisationRoot(store.Read("same")));
        Assert.Throws<ArgumentException>(()=>new ReviewUiModel(p,Settings(),codec,oldStore));Assert.Throws<ArgumentException>(()=>new ReviewUiModel(p.BaseV1!,Settings(),codec,store));
        Assert.False(PhysicalReviewHost.OpenColonisationFromSlot(p.BaseV1!,codec,store,"same",model.Current.Frame.Actor).Succeeded);
        Assert.False(PhysicalReviewHost.OpenColonisationRosterFromSlot(p,codec,oldStore,"same",model.Current.Frame.Actor).Succeeded);
        Assert.True(ReviewUiModel.ContinueColonisationRoster(p,codec,store,"same").ColonisationRosterCampaign);
    }
    [Fact]public async Task WorkerMutationSaveReopenAndAsyncLoadRetainExactV2SourceAndActorViews()
    {
        var m=Model();var p=Package();var frame=m.Current.Frame;var before=Root(m);
        Assert.Equal(3,Assert.Single(m.ColonisationActivities().Ships).StudyWork);Assert.Equal("blueprint.ready.constitution-explorer",Assert.Single(m.Colonisation().OwnShipSources).BlueprintId);Assert.Throws<ArgumentException>(()=>Session(m).CaptureColonisation("civ.cardassian"));
        var work=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:Kit(m))!;Assert.True(m.Busy);Assert.Equal(before,Root(m));
        Assert.True(m.CompleteColonisationInteraction(work,await Task.Run(work.Execute)));Assert.Throws<InvalidOperationException>(()=>work.Execute());
        Assert.Throws<InvalidOperationException>(()=>Host(m).CaptureColonisation(frame));
        var save=m.BeginStaticCheckpointOperation("typed",false)!;Assert.True(m.CompleteStaticCheckpointOperation(save,await Task.Run(save.Execute)),m.Message);var expected=Root(m);
        var continued=ReviewUiModel.ContinueColonisationRoster(p,codec,Store(),"typed");Assert.Equal(expected,Root(continued));Assert.True(continued.ColonisationRosterCampaign);
        Assert.True(continued.Current.ReopenFromSlot("typed").Succeeded); // Explicit host Reopen retains the typed wrapper.
        var controller=m.Current;var load=m.BeginStaticCheckpointOperation("typed",true)!;Assert.True(m.CompleteStaticCheckpointOperation(load,await Task.Run(load.Execute)),m.Message);
        Assert.NotSame(controller,m.Current);Assert.Equal(expected,Root(m));Assert.True(m.ColonisationRosterCampaign);
        var advance=m.BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance)!;Assert.True(m.CompleteColonisationInteraction(advance,await Task.Run(advance.Execute)));Assert.Equal(p.PackageHash,Session(m).ExportColonisationSave().TemplatePackageHash);
    }
    [Fact]public async Task WrongVersionAndCancelledCompletionCannotReplaceLiveSourceOrClearNewWork()
    {
        var m=Model();var expected=Root(m);var frame=m.Current.Frame;var controller=m.Current;
        Store().Write("wrong",CampaignColonisationApplication.StartGame(Base(),Settings()).ExportColonisationSave(),false);
        m.LocalDesignDirty=true;Assert.Null(m.BeginStaticCheckpointOperation("wrong",true));
        var wrong=m.BeginStaticCheckpointOperation("wrong",true,discard:true)!;Assert.False(m.CompleteStaticCheckpointOperation(wrong,await Task.Run(wrong.Execute)));Assert.Same(controller,m.Current);Assert.Same(frame,m.Current.Frame);Assert.Equal(expected,Root(m));Assert.True(m.LocalDesignDirty);m.LocalDesignDirty=false;
        var save=m.BeginStaticCheckpointOperation("valid",false)!;Assert.True(m.CompleteStaticCheckpointOperation(save,await Task.Run(save.Execute)));
        var old=m.BeginStaticCheckpointOperation("valid",true)!;var done=await Task.Run(old.Execute);m.AbandonStaticCheckpointOperation(old);
        var newer=m.BeginStaticCheckpointOperation("valid",true)!;Assert.False(m.CompleteStaticCheckpointOperation(old,done));Assert.True(m.Busy);m.AbandonStaticCheckpointOperation(old);Assert.True(m.Busy);
        Assert.True(m.CompleteStaticCheckpointOperation(newer,await Task.Run(newer.Execute)));Assert.Equal(expected,Root(m));
        Assert.Throws<InvalidOperationException>(()=>old.Execute());
    }
    [Fact]public async Task HeldV2PreviewCancellationKeepsExactCachedSourceAndOldFinallyCannotClearNewLease()
    {
        var m=Model();var before=Root(m);var frame=m.Current.Frame;var pair=m.GeneratedPresentation();
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        var probe=typeof(PhysicalReviewHost).GetProperty("ColonisationInteractionProbe",BindingFlags.Instance|BindingFlags.NonPublic)!;
        probe.SetValue(Host(m),(Action<string>)(phase=>{if(phase=="before-fork"){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(15)))throw new TimeoutException();}}));
        var work=m.BeginColonisationInteraction(ColonisationInteractionKind.Preview,colonyCommand:Kit(m))!;var task=Task.Run(work.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));m.AbandonColonisationInteraction(work);Assert.True(m.Busy);Assert.Same(pair,Host(m).CaptureGeneratedPresentation(frame));
            Assert.Null(m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:Kit(m)));Assert.Throws<InvalidOperationException>(()=>Host(m).Read(frame.Actor));
            release.Set();var done=await task.WaitAsync(TimeSpan.FromSeconds(20));Assert.False(m.CompleteColonisationInteraction(work,done));m.AbandonColonisationInteraction(work);Assert.False(m.Busy);Assert.Equal(before,Root(m));
            probe.SetValue(Host(m),null);var next=m.BeginColonisationInteraction(ColonisationInteractionKind.Preview,colonyCommand:Kit(m))!;
            m.AbandonColonisationInteraction(work);Assert.True(m.Busy);Assert.True(m.CompleteColonisationInteraction(next,await Task.Run(next.Execute)));Assert.Equal(before,Root(m));
        }
        finally{release.Set();await task.WaitAsync(TimeSpan.FromSeconds(20));probe.SetValue(Host(m),null);m.AbandonColonisationInteraction(work);}
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}