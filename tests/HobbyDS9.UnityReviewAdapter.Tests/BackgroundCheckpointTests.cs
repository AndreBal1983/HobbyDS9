using System.Diagnostics;
using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class BackgroundCheckpointTests
{
    const string Human="civ.federation";
    static readonly ReviewNewtonsoftCodec Codec=new();
    static CampaignPlayableFleetPackageDto Template()=>PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
    static readonly Lazy<byte[]> Initial=new(()=>Codec.EncodeStaticAstronomyCampaignRoot(CampaignStaticAstronomyApplication.StartGame(Template(),new(){Map=new(){Seed="42",SystemCount=250,EmptyRegionCount=250,Width=2500000,Height=2000000,QuietEmptyAreaTargetPermille=2000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}}).ExportGeneratedSave()));
    static CampaignGeneratedSaveDto Root()=>Codec.DecodeStaticAstronomyCampaignRoot(Initial.Value);
    static PhysicalReviewHost Host(ReviewUiModel model)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(model)!;
    sealed class Fixture : IDisposable
    {
        internal readonly string DirectoryPath=Path.Combine(Path.GetTempPath(),"HobbyDS9-Background-Checkpoint-"+Guid.NewGuid().ToString("N"));
        internal readonly ReviewStaticAstronomyCampaignSlotStore Store;
        internal readonly ReviewUiModel Model;
        internal Action<UnityReviewWriteStage>? Probe;
        internal Fixture()
        {
            Store=new(DirectoryPath,Codec,stage=>Probe?.Invoke(stage));Store.Write("input",Root(),false);
            Model=ReviewUiModel.ContinueStaticAstronomyCampaign(Template(),Codec,Store,"input");
        }
        internal string FileFor(string slot)=>Path.Combine(DirectoryPath,"unity-generated-static-astronomy-v2-"+slot+".json");
        public void Dispose(){if(Directory.Exists(DirectoryPath))Directory.Delete(DirectoryPath,true);}
    }
    [Fact] public async Task BlockedWriterLeavesActorPresentationReadableAndBusyAdmissionNeverWaitsOnStoreGate()
    {
        using var f=new Fixture();using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        var model=f.Model;var before=model.Current.Frame;var presentation=model.GeneratedPresentation();var draft=model.ResearchInputs()!;
        model.LocalDesignDirty=true;model.OrganizationName="unsent name";
        f.Probe=s=>{if(s==UnityReviewWriteStage.AfterPartialWrite){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(10)))throw new TimeoutException();}};
        var op=model.BeginStaticCheckpointOperation("saved",false)!;var worker=Task.Run(op.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
            // These calls would deadlock against the old SaveCommittedToSlot gate scope.
            await Task.Run(()=>{Assert.Same(before,model.Current.Frame);Assert.True(model.IsCurrentGeneratedPresentation(presentation));Host(model).Read(Human,null);}).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(model.TryCheckWriteAdmission("saved",out var error));Assert.Equal(ReviewCheckpointError.Busy,error);
            Assert.Null(model.BeginStaticCheckpointOperation("other",false));Assert.False(model.Advance());Assert.True(model.Busy);
        }
        finally{release.Set();}
        var result=await worker;Assert.True(model.CompleteStaticCheckpointOperation(op,result));
        Assert.Same(before,model.Current.Frame);Assert.Same(draft,model.ResearchInputs());Assert.True(model.LocalDesignDirty);Assert.Equal("unsent name",model.OrganizationName);
        Assert.Equal(ReviewCheckpointError.None,model.Current.LastCheckpointError);Assert.False(model.Busy);
    }
    [Fact] public void BackgroundAndSyncSnapshotsAreByteExactAndWorkCannotRunTwice()
    {
        using var f=new Fixture();Assert.True(f.Model.Save("sync"));
        var timer=Stopwatch.StartNew();var op=f.Model.BeginStaticCheckpointOperation("background",false)!;timer.Stop();
        Assert.NotNull(op);var result=op.Execute();Assert.True(f.Model.CompleteStaticCheckpointOperation(op,result));
        Assert.Equal(File.ReadAllBytes(f.FileFor("sync")),File.ReadAllBytes(f.FileFor("background")));
        Assert.Throws<InvalidOperationException>(()=>op.Execute());
        Console.WriteLine("Detached capture 250 ms="+timer.Elapsed.TotalMilliseconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
    }
    [Fact] public void InvalidLoadAfterClosedShapeAdmissionPreservesFrameAllDraftsAndExistingFiles()
    {
        using var f=new Fixture();var bad=Root();bad.PackageHash=new string('a',64);f.Store.Write("bad",bad,false);
        Assert.NotNull(Codec.DecodeStaticAstronomyCampaignRoot(File.ReadAllBytes(f.FileFor("bad"))));
        var bytes=File.ReadAllBytes(f.FileFor("bad"));var model=f.Model;var before=model.Current;var frame=before.Frame;
        var draft=model.ResearchInputs();model.LocalDesignDirty=true;model.OrganizationName="retain";
        Assert.Null(model.BeginStaticCheckpointOperation("bad",true));Assert.False(model.Busy);
        var op=model.BeginStaticCheckpointOperation("bad",true,discard:true)!;
        Assert.False(model.CompleteStaticCheckpointOperation(op,op.Execute()));
        Assert.Same(before,model.Current);Assert.Same(frame,model.Current.Frame);Assert.Same(draft,model.ResearchInputs());
        Assert.True(model.LocalDesignDirty);Assert.Equal("retain",model.OrganizationName);Assert.Equal(bytes,File.ReadAllBytes(f.FileFor("bad")));
    }
    [Fact] public void SuccessfulLoadPublishesBothHostAndControllerOnlyOnCompleteAndInvalidatesOldPair()
    {
        using var f=new Fixture();var model=f.Model;var previous=model.Current;var previousHost=Host(model);var pair=model.GeneratedPresentation();
        var draft=model.ResearchInputs();model.LocalDesignDirty=true;model.OrganizationName="discard only on success";
        var op=model.BeginStaticCheckpointOperation("input",true,discard:true)!;var completed=op.Execute();
        Assert.Same(previous,model.Current);Assert.Same(previousHost,Host(model));Assert.True(model.LocalDesignDirty);
        Assert.True(model.CompleteStaticCheckpointOperation(op,completed));
        Assert.NotSame(previous,model.Current);Assert.NotSame(previousHost,Host(model));Assert.False(model.IsCurrentGeneratedPresentation(pair));
        Assert.NotSame(draft,model.ResearchInputs());Assert.False(model.LocalDesignDirty);Assert.Equal("",model.OrganizationName);
        Assert.True(model.Advance());Assert.Equal(2,model.Current.Frame.Status.Turn);
    }
    [Fact] public async Task AbandonedWriteMayFinishButItsCompletionCannotReleaseOrPublishANewerLease()
    {
        using var f=new Fixture();using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();var m=f.Model;
        f.Probe=s=>{if(s==UnityReviewWriteStage.AfterPartialWrite){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(10)))throw new TimeoutException();}};
        var old=m.BeginStaticCheckpointOperation("old",false)!;var worker=Task.Run(old.Execute);
        Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));m.AbandonStaticCheckpointOperation(old);
        var newer=m.BeginStaticCheckpointOperation("input",true)!;Assert.True(m.Busy);
        release.Set();var completed=await worker;Assert.False(m.CompleteStaticCheckpointOperation(old,completed));Assert.True(m.Busy);
        m.AbandonStaticCheckpointOperation(old);Assert.True(m.Busy);
        Assert.True(File.Exists(f.FileFor("old"))); // No false promise of rolling back a durable write.
        Assert.True(m.CompleteStaticCheckpointOperation(newer,newer.Execute()));Assert.False(m.Busy);
    }
    [Fact] public void AbandonBeforeExecuteDoesNotWriteAndStaleCompletedLoadDoesNotReplaceFrame()
    {
        using var f=new Fixture();var m=f.Model;var save=m.BeginStaticCheckpointOperation("never",false)!;
        m.AbandonStaticCheckpointOperation(save);Assert.False(m.CompleteStaticCheckpointOperation(save,save.Execute()));Assert.False(File.Exists(f.FileFor("never")));
        var load=m.BeginStaticCheckpointOperation("input",true)!;var result=load.Execute();
        Assert.True(m.Current.Refresh());var refreshed=m.Current.Frame;
        Assert.False(m.CompleteStaticCheckpointOperation(load,result));Assert.Same(refreshed,m.Current.Frame);Assert.False(m.Busy);
    }
    [Fact] public void RacedEmptySlotAndInterruptedWriteKeepExistingBytesAndDrafts()
    {
        using var f=new Fixture();var m=f.Model;var frame=m.Current.Frame;m.LocalDesignDirty=true;
        Assert.True(m.TryCheckWriteAdmission("race",out var empty));Assert.Equal(ReviewCheckpointError.None,empty);
        var op=m.BeginStaticCheckpointOperation("race",false)!;f.Store.Write("race",Root(),false);var before=File.ReadAllBytes(f.FileFor("race"));
        Assert.False(m.CompleteStaticCheckpointOperation(op,op.Execute()));Assert.Equal(ReviewCheckpointError.SlotOccupied,m.Current.LastCheckpointError);Assert.Equal(before,File.ReadAllBytes(f.FileFor("race")));
        f.Probe=s=>{if(s==UnityReviewWriteStage.AfterPartialWrite)throw new IOException("interrupted");};
        var overwrite=m.BeginStaticCheckpointOperation("race",false,true)!;Assert.False(m.CompleteStaticCheckpointOperation(overwrite,overwrite.Execute()));
        Assert.Equal(before,File.ReadAllBytes(f.FileFor("race")));Assert.Empty(Directory.GetFiles(f.DirectoryPath,"*.tmp"));Assert.Same(frame,m.Current.Frame);Assert.True(m.LocalDesignDirty);
    }
    class CopyCodec : IStaticAstronomyReviewHostCodec
    {
        internal Action? Validate;
        internal bool Fail;
        public void ValidateOwnedStaticAstronomyExport(CampaignPlayableFleetPackageDto t,CampaignGeneratedSaveDto r){Validate?.Invoke();if(Fail)throw new IOException("validation");Codec.ValidateOwnedStaticAstronomyExport(t,r);}
        public CampaignGeneratedSaveDto CopyStaticAstronomySave(CampaignGeneratedSaveDto v)=>Codec.CopyStaticAstronomySave(v);
        public CampaignGeneratedMapDto CopyKnownGeneratedMap(CampaignGeneratedMapDto v)=>Codec.CopyKnownGeneratedMap(v);
        public CampaignGeneratedSaveDto CopySave(CampaignGeneratedSaveDto v)=>Codec.CopySave(v);
        public CampaignPlayableFleetPackageDto CopyPackage(CampaignPlayableFleetPackageDto v)=>Codec.CopyPackage(v);
        public CampaignPlayableFleetSaveDto CopySave(CampaignPlayableFleetSaveDto v)=>Codec.CopySave(v);
        public CampaignIntegratedPlaytestPackageDto CopyPackage(CampaignIntegratedPlaytestPackageDto v)=>Codec.CopyPackage(v);
        public CampaignIntegratedPlaytestSaveDto CopySave(CampaignIntegratedPlaytestSaveDto v)=>Codec.CopySave(v);
        public CampaignTacticalTravelPackageDto CopyPackage(CampaignTacticalTravelPackageDto v)=>Codec.CopyPackage(v);
        public CampaignTacticalTravelSaveDto CopySave(CampaignTacticalTravelSaveDto v)=>Codec.CopySave(v);
    }
    sealed class BlockingCodec : CopyCodec,IBackgroundStaticCheckpointCodec,IStaticAstronomyOwnedExportValidator { }
    sealed class MemoryStore : IBackgroundStaticCheckpointStore
    {
        internal int Writes;
        public CampaignGeneratedSaveDto Read(string slot)=>Root();
        public void Write(string slot,CampaignGeneratedSaveDto root,bool overwrite){Writes++;}
    }
    [Fact] public async Task BlockedReplayDoesNotHoldHostGateAndRejectsChangedAuthorityBeforeWriting()
    {
        var codec=new BlockingCodec();var store=new MemoryStore();var opened=PhysicalReviewHost.OpenStaticAstronomyCampaignFromSlot(Template(),codec,store,"input",Human);Assert.True(opened.Succeeded);
        var host=opened.Host!;var frame=host.Read(Human,null);var controller=new ReviewController(host,Human);var work=host.PrepareStaticCheckpointSave(frame,"output",false);
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();codec.Validate=()=>{entered.Set();if(!release.Wait(TimeSpan.FromSeconds(10)))throw new TimeoutException();};
        var worker=Task.Run(()=>work.Execute(()=>false));
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
            await Task.Run(()=>{var read=host.Read(Human,null);Assert.True(host.AdvancePlanning(read).Accepted);}).WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally{release.Set();}
        Assert.Equal(ReviewCheckpointError.Busy,(await worker).Error);Assert.Equal(0,store.Writes);
        Assert.Equal(ReviewCheckpointError.None,controller.LastCheckpointError);
    }
    [Fact] public void FailedFullValidationNeverReachesStore()
    {
        var codec=new BlockingCodec{Fail=true};var store=new MemoryStore();var host=PhysicalReviewHost.OpenStaticAstronomyCampaignFromSlot(Template(),codec,store,"input",Human).Host!;
        var frame=host.Read(Human,null);var work=host.PrepareStaticCheckpointSave(frame,"output",false);
        Assert.Equal(ReviewCheckpointError.SerializationFailed,work.Execute(()=>false).Error);Assert.Equal(0,store.Writes);Assert.True(host.IsCurrentStaticCheckpointSource(frame));
    }
    [Fact] public void CustomCodecWithoutOptInRemainsSyncOnly()
    {
        var store=new MemoryStore();var host=PhysicalReviewHost.OpenStaticAstronomyCampaignFromSlot(Template(),new CopyCodec(),store,"input",Human).Host!;
        Assert.Throws<InvalidOperationException>(()=>host.PrepareStaticCheckpointSave(host.Read(Human,null),"new",false));
        Assert.True(host.SaveCommittedToSlot("sync").Succeeded);Assert.Equal(1,store.Writes);
    }
    [Fact] public void DetachedCaptureAt1000PreservesExactSavedRoot()
    {
        var session=CampaignStaticAstronomyApplication.StartGame(Template(),new(){Map=new(){Seed="42",SystemCount=1000,EmptyRegionCount=1000,Width=5000000,Height=4000000,QuietEmptyAreaTargetPermille=2000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}});
        var bytes=Codec.EncodeStaticAstronomyCampaignRoot(session.ExportGeneratedSave());
        var dir=Path.Combine(Path.GetTempPath(),"HobbyDS9-Background-1000-"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=new ReviewStaticAstronomyCampaignSlotStore(dir,Codec);store.Write("input",Codec.DecodeStaticAstronomyCampaignRoot(bytes),false);
            var model=ReviewUiModel.ContinueStaticAstronomyCampaign(Template(),Codec,store,"input");
            var timer=Stopwatch.StartNew();var op=model.BeginStaticCheckpointOperation("captured",false)!;timer.Stop();
            Assert.True(model.CompleteStaticCheckpointOperation(op,op.Execute()));
            Assert.Equal(bytes,File.ReadAllBytes(Path.Combine(dir,"unity-generated-static-astronomy-v2-captured.json")));
            Console.WriteLine("Detached capture 1000 ms="+timer.Elapsed.TotalMilliseconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
        }
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
}
