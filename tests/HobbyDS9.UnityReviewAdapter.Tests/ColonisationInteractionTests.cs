using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class ColonisationInteractionTests:IDisposable
{
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Colonisation-Interaction-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    CampaignColonisationPackageDto Package()
    {
        byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        return CampaignColonisationApplication.CreatePackage(activities,codec.DecodeColonisationCatalogue(Read("colonisation-v1","catalogue")),codec.DecodeColonisationReadyClasses(Read("colonisation-v1","ready-classes")),codec.DecodeColonisationSupport(Read("colonisation-v1","support")),codec.DecodeColonisationPolicy(Read("colonisation-v1","settlement")));
    }
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    ReviewColonisationSlotStore Store()=>new(directory,codec);
    ReviewUiModel Model()=>new(Package(),Settings(),codec,Store());
    static PhysicalReviewHost Host(ReviewUiModel m)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)!;
    static ICampaignColonisationSession Session(ReviewUiModel m)=>(ICampaignColonisationSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Host(m))!;
    byte[] Root(ReviewUiModel m)=>codec.EncodeColonisationRoot(Session(m).ExportColonisationSave());
    static ColonisationCommandDto Kit(ReviewUiModel m)=>new(){ActorId=m.Current.Frame.Actor,CommandId="command.player.kit",Turn=m.Current.Frame.Status.Turn,Revision=m.Current.Frame.Status.CampaignRevision,Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}};
    static void Probe(ReviewUiModel m,Action<string>? probe)=>typeof(PhysicalReviewHost).GetProperty("ColonisationInteractionProbe",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(Host(m),probe);
    [Fact]public async Task PreviewSubmitAdvanceAndTypedContinuePreserveEntireCanonicalRoot()
    {
        var m=Model();var sync=CampaignColonisationApplication.StartGame(Package(),Settings());var before=Root(m);var command=Kit(m);
        var preview=m.BeginColonisationInteraction(ColonisationInteractionKind.Preview,colonyCommand:command)!;
        var check=await Task.Run(preview.Execute);Assert.True(check.ColonisationEligibility!.Available);Assert.True(m.CompleteColonisationInteraction(preview,check));Assert.Equal(before,Root(m));
        var work=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:command)!;command.EnqueueKit!.ColonyId="not-a-world";
        var done=await Task.Run(work.Execute);Assert.True(m.CompleteColonisationInteraction(work,done),m.Message);Assert.True(done.Committed);Assert.False(m.CompleteColonisationInteraction(work,done));Assert.Throws<InvalidOperationException>(()=>work.Execute());
        Assert.True(sync.SubmitColonisation(Kit(Model())).Accepted);Assert.Equal(codec.EncodeColonisationRoot(sync.ExportColonisationSave()),Root(m));
        var advance=m.BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance)!;Assert.True(m.CompleteColonisationInteraction(advance,await Task.Run(advance.Execute)),m.Message);
        var stamp=sync.CaptureStatus();Assert.True(sync.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);Assert.Equal(codec.EncodeColonisationRoot(sync.ExportColonisationSave()),Root(m));
        var save=m.BeginStaticCheckpointOperation("typed",false)!;Assert.True(m.CompleteStaticCheckpointOperation(save,await Task.Run(save.Execute)),m.Message);
        var reopened=ReviewUiModel.ContinueColonisation(Package(),codec,Store(),"typed");Assert.Equal(Root(m),Root(reopened));Assert.Equal(m.FrontierCommission().CompletedObjectives,reopened.FrontierCommission().CompletedObjectives);
    }
    [Fact]public async Task CancelledSourceReaderKeepsBusyPairAndBlocksEveryWriterUntilDrain()
    {
        var m=Model();var before=Root(m);var frame=m.Current.Frame;var pair=m.GeneratedPresentation();
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        Probe(m,phase=>{if(phase=="before-fork"){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(15)))throw new TimeoutException();}});
        var work=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:Kit(m))!;var task=Task.Run(work.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));m.AbandonColonisationInteraction(work);Assert.True(m.Busy);Assert.Same(pair,Host(m).CaptureGeneratedPresentation(frame));Assert.True(m.IsCurrentGeneratedPresentation(pair));
            Assert.Throws<InvalidOperationException>(()=>Host(m).Read(frame.Actor));Assert.Throws<InvalidOperationException>(()=>Host(m).AdvancePlanning(frame));Assert.Throws<InvalidOperationException>(()=>Host(m).SaveCommitted());Assert.Throws<InvalidOperationException>(()=>Host(m).SubmitTravelOrder(new()));
            Assert.Null(m.BeginColonisationInteraction(ColonisationInteractionKind.Preview,colonyCommand:Kit(m)));
            release.Set();var completion=await task.WaitAsync(TimeSpan.FromSeconds(20));Assert.False(m.CompleteColonisationInteraction(work,completion));m.AbandonColonisationInteraction(work);Assert.False(m.Busy);Assert.Equal(before,Root(m));
        }
        finally{release.Set();await task.WaitAsync(TimeSpan.FromSeconds(20));Probe(m,null);m.AbandonColonisationInteraction(work);}
    }
    [Fact]public async Task LateFaultAndReentrantCancelDoNotPublishAndStaleFinallyDoesNotClearNewWork()
    {
        var m=Model();var before=Root(m);Probe(m,p=>{if(p=="after-capture")throw new InvalidOperationException("late test fault");});
        var failed=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:Kit(m))!;Assert.False(m.CompleteColonisationInteraction(failed,await Task.Run(failed.Execute)));Assert.Equal(before,Root(m));Probe(m,null);
        var old=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:Kit(m))!;var completion=await Task.Run(old.Execute);ColonisationInteractionWork? newer=null;
        Probe(m,p=>{if(p=="before-publication"){m.AbandonColonisationInteraction(old);newer=m.BeginColonisationInteraction(ColonisationInteractionKind.Preview,colonyCommand:Kit(m));}});
        Assert.False(m.CompleteColonisationInteraction(old,completion));Assert.NotNull(newer);Assert.True(m.Busy);Probe(m,null);m.AbandonColonisationInteraction(old);Assert.True(m.Busy);
        Assert.True(m.CompleteColonisationInteraction(newer!,await Task.Run(newer!.Execute)));Assert.Equal(before,Root(m));
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
