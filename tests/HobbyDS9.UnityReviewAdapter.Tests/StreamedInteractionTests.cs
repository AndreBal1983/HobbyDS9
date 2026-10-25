using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;
using Xunit.Abstractions;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class StreamedInteractionTests : IDisposable
{
    readonly ITestOutputHelper output;
    public StreamedInteractionTests(ITestOutputHelper output){this.output=output;}
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Streamed-Interaction-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    CampaignStreamedActivitiesPackageDto Package()=>CampaignStreamedActivitiesApplication.CreatePackage(ActivitiesPlaytestOptions.CreatePackage(
        PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1")),
        new[]{"catalogue","ready-classes","support","activities"}.Select(n=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/activities-v1",n+".json"))).ToArray(),codec));
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    ReviewStreamedActivitiesSlotStore Store()=>new(directory,codec);
    ReviewUiModel Model()=>new(Package(),Settings(),codec,Store());
    static PhysicalReviewHost Host(ReviewUiModel m)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)!;
    static ICampaignStreamedActivitiesSession Session(ReviewUiModel m)=>(ICampaignStreamedActivitiesSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Host(m))!;
    byte[] Root(ReviewUiModel m)=>codec.EncodeStreamedActivitiesRoot(Session(m).ExportStreamedActivitiesSave());
    static void Probe(ReviewUiModel m,Action<string>? probe)=>typeof(PhysicalReviewHost).GetProperty("StreamedInteractionProbe",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(Host(m),probe);
    static CampaignActivityCommandDto Queue(ReviewUiModel m,string id="command.interaction.queue")=>new(){ActorId=m.Current.Frame.Actor,ExpectedTurn=m.Current.Frame.Status.Turn,ExpectedRevision=m.Current.Frame.Status.CampaignRevision,
        CommandId=id,Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId="blueprint.ready.constitution-explorer",BlueprintRevision=1};
    static async Task<StreamedInteractionCompletion> Execute(ReviewUiModel m,StreamedInteractionKind kind,CampaignActivityCommandDto? command=null)
    {
        var work=m.BeginStreamedInteraction(kind,command)!;Assert.NotNull(work);var result=await Task.Run(work.Execute);
        Assert.False(result.Failed);Assert.True(m.CompleteStreamedInteraction(work,result),m.Message);return result;
    }
    [Fact] public async Task DirectEnqueueWithoutPreviewCommitsOnceAndPreservesWholeRootAcrossTypedContinue()
    {
        var m=Model();var sync=Model();var command=Queue(m,"command.catalogue.direct");var before=Root(m);
        var money=m.Current.Frame.Strategy!.Civilization.Credits;
        string LocalStocks()=>Newtonsoft.Json.JsonConvert.SerializeObject(Session(m).ExportStreamedActivitiesSave().Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Colonies!.Single(c=>c.ColonyId=="colony.sol").Stockpiles);
        var stocks=LocalStocks();
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        Probe(m,phase=>{if(phase=="before-action"){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(20)))throw new TimeoutException();}});
        var work=m.BeginStreamedInteraction(StreamedInteractionKind.Submission,command)!;Assert.NotNull(work);var task=Task.Run(work.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));Assert.True(m.Busy);Assert.Null(m.BeginStreamedInteraction(StreamedInteractionKind.Submission,command));
            Assert.Equal(before,Root(m));Assert.Equal(stocks,LocalStocks());
            release.Set();var completion=await task.WaitAsync(TimeSpan.FromSeconds(20));Assert.True(m.CompleteStreamedInteraction(work,completion),m.Message);
            Assert.True(completion.Committed);Assert.False(m.CompleteStreamedInteraction(work,completion));Assert.Equal(money,m.Current.Frame.Strategy!.Civilization.Credits);
            var queued=Assert.Single(m.StreamedActivities().Production);Assert.All(queued.ConsumedResources!,r=>Assert.Equal(0,r.Amount));Assert.Equal(stocks,LocalStocks());
            Assert.True(sync.SubmitActivity(command),sync.Message);Assert.Equal(Root(sync),Root(m));var committed=Root(m);
            Assert.Null(m.BeginStreamedInteraction(StreamedInteractionKind.Submission,command));Assert.Equal(committed,Root(m));
            Assert.True(m.Save("catalogue"),m.Message);var reopened=ReviewUiModel.ContinueStreamedActivities(Package(),codec,Store(),"catalogue");
            Assert.Equal(committed,Root(reopened));Assert.Equal(m.StreamedActivities().Construction.Count,reopened.StreamedActivities().Construction.Count);
            Probe(m,null);var rejectedCommand=Queue(m,"command.catalogue.rejected");rejectedCommand.TargetId="colony.unavailable";
            var rejected=m.BeginStreamedInteraction(StreamedInteractionKind.Submission,rejectedCommand)!;var unavailable=await Task.Run(rejected.Execute);
            Assert.True(m.CompleteStreamedInteraction(rejected,unavailable));Assert.False(unavailable.Committed);Assert.False(unavailable.Result!.Accepted);
            Assert.Equal(committed,Root(m));Assert.Equal("This construction order is no longer available at the selected world.",m.Message);
        }
        finally{release.Set();await task.WaitAsync(TimeSpan.FromSeconds(20));Probe(m,null);m.AbandonStreamedInteraction(work);}
    }
    [Fact] public async Task DetachedPreviewSubmissionAndTurnExactlyMatchSyncRootsWithoutReplayOrDoubleExecution()
    {
        var sync=Model();var asyncModel=Model();var command=Queue(sync);var original=Root(asyncModel);var frame=asyncModel.Current.Frame;
        var work=asyncModel.BeginStreamedInteraction(StreamedInteractionKind.Preview,command)!;var result=await Task.Run(work.Execute);
        Assert.True(result.Eligibility!.Accepted);Assert.Equal(original,Root(asyncModel));Assert.Same(frame,asyncModel.Current.Frame);
        Assert.True(asyncModel.CompleteStreamedInteraction(work,result));Assert.False(result.Committed);Assert.Equal(original,Root(asyncModel));
        Assert.Throws<InvalidOperationException>(()=>work.Execute());Assert.False(asyncModel.CompleteStreamedInteraction(work,result));
        Assert.True(sync.SubmitActivity(command),sync.Message);Assert.True((await Execute(asyncModel,StreamedInteractionKind.Submission,command)).Committed);
        Assert.Equal(Root(sync),Root(asyncModel));Assert.True(sync.Advance(),sync.Message);Assert.True((await Execute(asyncModel,StreamedInteractionKind.PlanningAdvance)).Committed);
        Assert.Equal(Root(sync),Root(asyncModel));Assert.True(asyncModel.Save("worker"),asyncModel.Message);
        var reopened=ReviewUiModel.ContinueStreamedActivities(Package(),codec,Store(),"worker");Assert.Equal(Root(sync),Root(reopened));
    }
    [Fact] public async Task HeldCancelledWorkerKeepsRenderPairReadableAndRejectsAllWritersUntilDrained()
    {
        var m=Model();var root=Root(m);var frame=m.Current.Frame;var pair=m.GeneratedPresentation();var handle=Host(m).SaveCommitted();
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        Probe(m,phase=>{if(phase=="before-fork"){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(20)))throw new TimeoutException();}});
        var work=m.BeginStreamedInteraction(StreamedInteractionKind.Submission,Queue(m))!;var task=Task.Run(work.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));m.AbandonStreamedInteraction(work);Assert.True(m.Busy);
            var watch=Stopwatch.StartNew();Assert.True(m.IsCurrentGeneratedPresentation(pair));Assert.Same(pair,Host(m).CaptureGeneratedPresentation(frame));
            Assert.Throws<InvalidOperationException>(()=>Host(m).Read(frame.Actor));
            Assert.Throws<InvalidOperationException>(()=>Host(m).AdvancePlanning(frame));
            Assert.Throws<InvalidOperationException>(()=>Host(m).SubmitStrategicCommand(frame,new()));
            Assert.Throws<InvalidOperationException>(()=>Host(m).SubmitTravelOrder(new()));
            Assert.Throws<InvalidOperationException>(()=>Host(m).SetApproachCloak(frame,"command.cloak","missing",true));
            Assert.Throws<InvalidOperationException>(()=>Host(m).SaveCommitted());
            Assert.Throws<InvalidOperationException>(()=>Host(m).LoadCommitted(handle,frame.Actor,null));
            Assert.Throws<InvalidOperationException>(()=>Host(m).LoadCommittedFromSlot("missing",frame.Actor));
            Assert.Null(m.BeginStreamedInteraction(StreamedInteractionKind.Preview,Queue(m)));Assert.False(m.Close(true));
            Assert.True(watch.Elapsed<TimeSpan.FromSeconds(2),"busy render/rejections do not wait for the held worker");
            release.Set();var completion=await task.WaitAsync(TimeSpan.FromSeconds(20));Assert.False(m.CompleteStreamedInteraction(work,completion));
            Assert.False(m.Busy);Assert.Equal(root,Root(m));Assert.Same(frame,m.Current.Frame);
        }
        finally{release.Set();await task.WaitAsync(TimeSpan.FromSeconds(20));Probe(m,null);m.AbandonStreamedInteraction(work);}
    }
    [Theory] [InlineData("after-capture")] [InlineData("before-publication")]
    public async Task LateFailureCannotPublishPreparedAcceptedState(string phase)
    {
        var m=Model();var original=Root(m);var frame=m.Current.Frame;Probe(m,p=>{if(p==phase)throw new InvalidOperationException("injected late fault");});
        var work=m.BeginStreamedInteraction(StreamedInteractionKind.Submission,Queue(m))!;var completion=await Task.Run(work.Execute);
        Assert.False(m.CompleteStreamedInteraction(work,completion));Assert.False(m.Busy);Assert.Equal(original,Root(m));Assert.Same(frame,m.Current.Frame);
    }
    [Fact] public async Task CancelAtPublicationAndOldCompletionCannotClearANewerLease()
    {
        var m=Model();var original=Root(m);var work=m.BeginStreamedInteraction(StreamedInteractionKind.Submission,Queue(m))!;
        var completion=await Task.Run(work.Execute);Assert.True(completion.Result!.Accepted);Assert.False(completion.Committed);
        Probe(m,p=>{if(p=="before-publication")m.AbandonStreamedInteraction(work);});Assert.False(m.CompleteStreamedInteraction(work,completion));
        Assert.Equal(original,Root(m));Probe(m,null);
        var newer=m.BeginStreamedInteraction(StreamedInteractionKind.Preview,Queue(m))!;Assert.False(m.CompleteStreamedInteraction(work,completion));m.AbandonStreamedInteraction(work);Assert.True(m.Busy);
        var checkedOrder=await Task.Run(newer.Execute);Assert.True(m.CompleteStreamedInteraction(newer,checkedOrder));Assert.False(m.Busy);
    }
    [Fact] public async Task InputsAreCopiedAndUnrelatedResearchDesignAndOrganisationDraftsSurviveCommit()
    {
        var m=Model();var controller=m.Current;var draft=m.ResearchInputs()!;draft.ScienceUnits=draft.ScienceUnits==600?300:600;
        m.LocalDesignDirty=true;m.OrganizationName="Retained draft";m.OrganizationShips.Add(m.Current.Frame.OwnShips.First().Condition.ShipId);
        var command=Queue(m);var work=m.BeginStreamedInteraction(StreamedInteractionKind.Submission,command)!;command.TargetId="forged";command.ActorId="foreign";
        var completion=await Task.Run(work.Execute);Assert.True(m.CompleteStreamedInteraction(work,completion));Assert.Same(controller,m.Current);
        Assert.Equal("colony.sol",Assert.Single(m.StreamedActivities().Production).ColonyId);Assert.True(m.ResearchInputs()!.Dirty);Assert.Equal(draft.ScienceUnits,m.ResearchInputs()!.ScienceUnits);
        Assert.True(m.LocalDesignDirty);Assert.Equal("Retained draft",m.OrganizationName);Assert.Single(m.OrganizationShips);Assert.Null(m.BeginStreamedInteraction(StreamedInteractionKind.PlanningAdvance));
        var committed=Root(m);m.AbandonStreamedInteraction(work);Assert.True(completion.Committed);Assert.Equal(committed,Root(m));
    }
    [Fact] public async Task StaleFrameAndRefreshedRequiredControllerNeverPublish()
    {
        var m=Model();var stale=m.Current.Frame;Assert.True(Host(m).AdvancePlanning(stale).Accepted);
        Assert.Throws<InvalidOperationException>(()=>Host(m).PrepareStreamedInteraction(stale,StreamedInteractionKind.Preview,Queue(m)));
        Assert.True(m.Current.Refresh());var before=Root(m);var work=m.BeginStreamedInteraction(StreamedInteractionKind.Submission,Queue(m))!;var completion=await Task.Run(work.Execute);
        m.Current.MarkStrategicFailure(CampaignTacticalError.InvalidBatch);Assert.False(m.CompleteStreamedInteraction(work,completion));Assert.Equal(before,Root(m));
    }
    [Fact] public async Task Actual1000SavedQueuedRootWorkerAdvanceMatchesSynchronousAdvance()
    {
        using var file=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Fixtures/streamed-native1000-week2.json.gz"));using var compressed=new GZipStream(file,CompressionMode.Decompress);using var bytes=new MemoryStream();compressed.CopyTo(bytes);
        var root=codec.DecodeStreamedActivitiesRoot(bytes.ToArray());Store().Write("native",root,false);
        var sync=ReviewUiModel.ContinueStreamedActivities(Package(),codec,Store(),"native");var worker=ReviewUiModel.ContinueStreamedActivities(Package(),codec,Store(),"native");
        Assert.Equal(1000,root.Map!.Settings.Map.SystemCount);Assert.NotEmpty(worker.StreamedActivities().Production);Assert.Equal(Root(sync),Root(worker));
        Assert.True(sync.Advance(),sync.Message);
        var watch=Stopwatch.StartNew();var work=worker.BeginStreamedInteraction(StreamedInteractionKind.PlanningAdvance)!;var reservationMs=watch.Elapsed.TotalMilliseconds;
        var measured=await Task.Run(()=>{var before=GC.GetAllocatedBytesForCurrentThread();var clock=Stopwatch.StartNew();var result=work.Execute();return (Result:result,Milliseconds:clock.Elapsed.TotalMilliseconds,AllocatedBytes:GC.GetAllocatedBytesForCurrentThread()-before);});
        watch.Restart();Assert.True(worker.CompleteStreamedInteraction(work,measured.Result),worker.Message);var publicationMs=watch.Elapsed.TotalMilliseconds;
        output.WriteLine(".NET actual1000 owning-thread reservation ms={0:R}; worker ms={1:R}; cumulative thread allocated B={2}; owning-thread publication ms={3:R}",reservationMs,measured.Milliseconds,measured.AllocatedBytes,publicationMs);
        Assert.True(measured.Result.Committed);Assert.Equal(Root(sync),Root(worker));
        Assert.True(worker.Save("advanced"),worker.Message);var loaded=ReviewUiModel.ContinueStreamedActivities(Package(),codec,Store(),"advanced");Assert.Equal(Root(worker),Root(loaded));
    }
    [Fact] public async Task DetachedPublicSessionRetainsExactOuterRecordCapacityRejection()
    {
        var original=Package().ContentPackage!;
        var package=CampaignStreamedActivitiesApplication.CreatePackage(CampaignActivitiesApplication.CreatePackage(original.BaseTemplate!,original.Catalogue!,original.ReadyClasses!,original.Support!,original.Content!,32));
        var sync=new ReviewUiModel(package,Settings(),codec,Store());
        for(int i=0;i<16;i++)Assert.True(sync.SubmitActivity(Queue(sync,"command.capacity."+i)),sync.Message);
        Assert.True(sync.Save("capacity"));var worker=ReviewUiModel.ContinueStreamedActivities(package,codec,Store(),"capacity");var before=Root(sync);
        var command=Queue(sync,"command.capacity.rejected");Assert.False(sync.SubmitActivity(command));
        var result=await Execute(worker,StreamedInteractionKind.Submission,command);Assert.Equal(CampaignTacticalError.Overflow,result.Result!.Error);Assert.False(result.Committed);
        Assert.Equal(before,Root(sync));Assert.Equal(before,Root(worker));
    }
    [Fact] public async Task ForeignOwnerCannotCancelWorkAndBusyRenderRequiresTheExactCommittedPair()
    {
        var m=Model();var other=Model();var pair=m.GeneratedPresentation();var frame=m.Current.Frame;
        var constructor=typeof(GeneratedCampaignPresentation).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic).Single();
        var lookalike=(GeneratedCampaignPresentation)constructor.Invoke(new object[]{frame,pair.KnownMap,pair.Astronomy,codec});
        var work=m.BeginStreamedInteraction(StreamedInteractionKind.Submission,Queue(m))!;
        other.AbandonStreamedInteraction(work);Assert.False(work.Cancelled);Assert.True(m.Busy);
        Assert.True(m.IsCurrentGeneratedPresentation(pair));Assert.False(m.IsCurrentGeneratedPresentation(lookalike));
        var result=await Task.Run(work.Execute);var otherWork=other.BeginStreamedInteraction(StreamedInteractionKind.Preview,Queue(other))!;
        Assert.False(other.CompleteStreamedInteraction(work,result));Assert.True(other.Busy);Assert.True(m.CompleteStreamedInteraction(work,result));
        other.AbandonStreamedInteraction(otherWork);Assert.False(other.Busy);Assert.Single(m.StreamedActivities().Production);
    }
    [Fact] public async Task CancelledWorkerWaitingForSourceLockCannotExposeBlockingMainThreadReads()
    {
        var m=Model();var original=Root(m);var pair=m.GeneratedPresentation();var work=m.BeginStreamedInteraction(StreamedInteractionKind.Submission,Queue(m))!;
        var backend=typeof(CampaignIntegratedPlaytestApplication).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Session(m))!;
        var sourceGate=backend.GetType().GetField("gate",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(backend)!;
        using var locked=new ManualResetEventSlim();using var release=new ManualResetEventSlim();using var reached=new ManualResetEventSlim();
        var holder=Task.Run(()=>{lock(sourceGate){locked.Set();if(!release.Wait(TimeSpan.FromSeconds(20)))throw new TimeoutException();}});
        Assert.True(locked.Wait(TimeSpan.FromSeconds(5)));Probe(m,p=>{if(p=="before-fork")reached.Set();});var worker=Task.Run(work.Execute);
        try
        {
            Assert.True(reached.Wait(TimeSpan.FromSeconds(5)));Assert.False(worker.IsCompleted);m.AbandonStreamedInteraction(work);
            var watch=Stopwatch.StartNew();Assert.True(m.Busy);Assert.Null(m.BeginStreamedInteraction(StreamedInteractionKind.Preview,Queue(m)));
            Assert.True(m.IsCurrentGeneratedPresentation(pair));Assert.Throws<InvalidOperationException>(()=>Host(m).Read(m.Current.Frame.Actor));
            Assert.True(watch.Elapsed<TimeSpan.FromSeconds(2));release.Set();await holder.WaitAsync(TimeSpan.FromSeconds(20));
            var completion=await worker.WaitAsync(TimeSpan.FromSeconds(20));Assert.False(m.CompleteStreamedInteraction(work,completion));Assert.False(m.Busy);Assert.Equal(original,Root(m));
        }
        finally{release.Set();await holder.WaitAsync(TimeSpan.FromSeconds(20));await worker.WaitAsync(TimeSpan.FromSeconds(20));m.AbandonStreamedInteraction(work);}
    }
    public void Dispose()
    {
        if(!Directory.Exists(directory))return;var full=Path.GetFullPath(directory);
        if(Path.GetDirectoryName(full)!=Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)||!Path.GetFileName(full).StartsWith("HobbyDS9-Streamed-Interaction-",StringComparison.Ordinal))throw new InvalidOperationException("Unsafe test cleanup");
        foreach(var file in Directory.GetFiles(full))File.Delete(file);Directory.Delete(full,false);
    }
}
