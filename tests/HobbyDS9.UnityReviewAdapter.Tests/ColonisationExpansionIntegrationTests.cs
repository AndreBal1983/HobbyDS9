using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class ColonisationExpansionIntegrationTests:IDisposable
{
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Colonisation-Expansion-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    CampaignColonisationPackageDto Base()
    {
        byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        return ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
    }
    byte[] Resource()=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/colonisation-roster-v2/roster.json"));
    CampaignColonisationExpansionPackageDto Package()=>ColonisationExpansionPlaytestOptions.CreatePackage(ColonisationPlaytestOptions.CreateRosterPackage(Base(),Resource(),codec),Planner(),codec);
    byte[] Planner()=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/colonisation-expansion-v1/planner.json"));
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    ReviewColonisationExpansionSlotStore Store()=>new(directory,codec);
    ReviewUiModel Model()=>new(Package(),Settings(),codec,Store());
    static PhysicalReviewHost Host(ReviewUiModel m)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)!;
    static ICampaignColonisationExpansionSession Session(ReviewUiModel m)=>(ICampaignColonisationExpansionSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Host(m))!;
    byte[] Root(ReviewUiModel m)=>codec.EncodeColonisationExpansionRoot(Session(m).ExportColonisationExpansionSave());
    static ColonisationCommandDto Kit(ReviewUiModel m)=>new(){ActorId=m.Current.Frame.Actor,CommandId="command.roster.kit",Turn=m.Current.Frame.Status.Turn,Revision=m.Current.Frame.Status.CampaignRevision,Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}};
    [Fact]public void PinnedCompositionAndSeparateNamespaceNeverReinterpretOldSlots()
    {
        var p=Package();var m=Model();Assert.True(m.ColonisationExpansionCampaign);Assert.True(m.ColonisationCampaign);Assert.False(m.ColonisationRosterCampaign);
        var bytes=Planner();bytes[0]^=1;Assert.Throws<ArgumentException>(()=>ColonisationExpansionPlaytestOptions.CreatePackage(p.BaseRosterV2!,bytes,codec));
        var old=CampaignColonisationApplication.StartRosterGame(p.BaseRosterV2!,Settings()).ExportColonisationSave();
        var oldStore=ReviewColonisationSlotStore.CreateRosterV2(directory,codec);oldStore.Write("same",old,false);
        Store().Write("same",Session(m).ExportColonisationExpansionSave(),false);
        Assert.True(File.Exists(Path.Combine(directory,"unity-campaign-colonisation-v2-same.json")));
        Assert.True(File.Exists(Path.Combine(directory,"unity-campaign-colonisation-expansion-v1-same.json")));
        Assert.Equal(codec.EncodeColonisationRoot(old),codec.EncodeColonisationRoot(oldStore.Read("same")));
        Assert.Equal(Root(m),codec.EncodeColonisationExpansionRoot(Store().Read("same")));
        Assert.ThrowsAny<Exception>(()=>codec.DecodeColonisationRoot(Root(m)));
        Assert.ThrowsAny<Exception>(()=>codec.DecodeColonisationExpansionRoot(codec.EncodeColonisationRoot(old)));
        Assert.ThrowsAny<Exception>(()=>Session(m).ExportPlayableSave());
        Assert.ThrowsAny<Exception>(()=>Session(m).ExportSave());
        Assert.Equal(Root(m),codec.EncodeColonisationExpansionRoot(Session(m).ForkInteraction(m.Current.Frame.Status.Turn,m.Current.Frame.Status.CampaignRevision).ExportColonisationExpansionSave()));
        var ops=(ICampaignColonisationOperations)Session(m);Assert.IsAssignableFrom<ICampaignColonisationExpansionSession>(ops.ForkOperations(m.Current.Frame.Status.Turn,m.Current.Frame.Status.CampaignRevision));
    }
    [Fact]public async Task WorkerMutationSaveReopenAndAsyncLoadRetainFullExpansionPlansAndActorViews()
    {
        var m=Model();var p=Package();var frame=m.Current.Frame;var before=Root(m);
        Assert.Equal(3,Assert.Single(m.ColonisationActivities().Ships).StudyWork);Assert.Equal("blueprint.ready.constitution-explorer",Assert.Single(m.Colonisation().OwnShipSources).BlueprintId);Assert.Throws<ArgumentException>(()=>Session(m).CaptureColonisation("civ.cardassian"));
        var work=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:Kit(m))!;Assert.True(m.Busy);Assert.Equal(before,Root(m));
        Assert.True(m.CompleteColonisationInteraction(work,await Task.Run(work.Execute)));Assert.Throws<InvalidOperationException>(()=>work.Execute());
        Assert.Throws<InvalidOperationException>(()=>Host(m).CaptureColonisation(frame));
        var save=m.BeginStaticCheckpointOperation("typed",false)!;Assert.True(m.CompleteStaticCheckpointOperation(save,await Task.Run(save.Execute)),m.Message);var expected=Root(m);
        var continued=ReviewUiModel.ContinueColonisationExpansion(p,codec,Store(),"typed");Assert.Equal(expected,Root(continued));Assert.True(continued.ColonisationExpansionCampaign);
        Assert.True(continued.Current.ReopenFromSlot("typed").Succeeded); // Explicit host Reopen retains the typed wrapper.
        var controller=m.Current;var load=m.BeginStaticCheckpointOperation("typed",true)!;Assert.True(m.CompleteStaticCheckpointOperation(load,await Task.Run(load.Execute)),m.Message);
        Assert.NotSame(controller,m.Current);Assert.Equal(expected,Root(m));Assert.True(m.ColonisationExpansionCampaign);
        var advance=m.BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance)!;Assert.True(m.CompleteColonisationInteraction(advance,await Task.Run(advance.Execute)));Assert.Equal(p.PackageHash,Session(m).ExportColonisationExpansionSave().PackageHash);
        Assert.NotEmpty(Assert.Single(Session(m).ExportColonisationExpansionSave().AiPlans!).LatestActions!);
        var advanced=Root(m);Assert.True(m.TryCheckWriteAdmission("advanced",out var admission));Assert.Equal(ReviewCheckpointError.None,admission);var after=m.BeginStaticCheckpointOperation("advanced",false)!;Assert.True(m.CompleteStaticCheckpointOperation(after,await Task.Run(after.Execute)));
        Assert.Equal(advanced,Root(ReviewUiModel.ContinueColonisationExpansion(p,codec,Store(),"advanced")));
        Assert.True(m.TryCheckWriteAdmission("advanced",out admission));Assert.Equal(ReviewCheckpointError.SlotOccupied,admission);
        Assert.True(m.Save("sync"));Assert.True(m.Load("sync"));Assert.Equal(advanced,Root(m));
    }
    [Fact]public async Task WrongVersionAndCancelledCompletionCannotReplaceLiveSourceOrClearNewWork()
    {
        var m=Model();var expected=Root(m);var frame=m.Current.Frame;var controller=m.Current;
        var wrongPackage=Package();wrongPackage.PackageHash=new string('0',64);
        var wrongRoot=Session(m).ExportColonisationExpansionSave();wrongRoot.PackageHash=wrongPackage.PackageHash;Store().Write("wrong",wrongRoot,false);
        m.LocalDesignDirty=true;Assert.Null(m.BeginStaticCheckpointOperation("wrong",true));
        var wrong=m.BeginStaticCheckpointOperation("wrong",true,discard:true)!;Assert.False(m.CompleteStaticCheckpointOperation(wrong,await Task.Run(wrong.Execute)));Assert.Same(controller,m.Current);Assert.Same(frame,m.Current.Frame);Assert.Equal(expected,Root(m));Assert.True(m.LocalDesignDirty);m.LocalDesignDirty=false;
        var save=m.BeginStaticCheckpointOperation("valid",false)!;Assert.True(m.CompleteStaticCheckpointOperation(save,await Task.Run(save.Execute)));
        var old=m.BeginStaticCheckpointOperation("valid",true)!;var done=await Task.Run(old.Execute);m.AbandonStaticCheckpointOperation(old);
        var newer=m.BeginStaticCheckpointOperation("valid",true)!;Assert.False(m.CompleteStaticCheckpointOperation(old,done));Assert.True(m.Busy);m.AbandonStaticCheckpointOperation(old);Assert.True(m.Busy);
        Assert.True(m.CompleteStaticCheckpointOperation(newer,await Task.Run(newer.Execute)));Assert.Equal(expected,Root(m));
        Assert.Throws<InvalidOperationException>(()=>old.Execute());
    }
    [Fact]public async Task HeldExpansionPreviewCancellationKeepsExactCachedSourceAndOldFinallyCannotClearNewLease()
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
    [Theory]
    [InlineData("missing")][InlineData("extra")][InlineData("null")][InlineData("policy")][InlineData("enum")][InlineData("overflow")][InlineData("duplicate")][InlineData("text")][InlineData("hold")][InlineData("plans")][InlineData("receipt")][InlineData("target-null")]
    public void ClosedCarrierRejectsMalformedOuterAndInnerFacts(string kind)
    {
        var j=Newtonsoft.Json.Linq.JObject.Parse(System.Text.Encoding.UTF8.GetString(Root(Model())));
        switch(kind)
        {
            case "text":j["AiPlans"]![0]!["TargetPlanetId"]=new string('x',513);break;
            case "hold":j["AiPlans"]![0]!["HoldReason"]=new string('x',129);break;
            case "plans":j["AiPlans"]=new Newtonsoft.Json.Linq.JArray(Enumerable.Range(0,5).Select(_=>j["AiPlans"]![0]!.DeepClone()));break;
            case "receipt":j["AiPlans"]![0]!["LatestActions"]=Newtonsoft.Json.Linq.JArray.FromObject(new[]{new ColonisationAiActionReceiptDto{CommandId=new string('x',129),Action=ColonisationAiAction.Kit}});break;
            case "target-null":j["AiPlans"]![0]!["OwnAttemptedTargets"]=new Newtonsoft.Json.Linq.JArray(Newtonsoft.Json.Linq.JValue.CreateNull());break;
            case "missing":j.Remove("AiPlans");break;
            case "extra":j["Untrusted"]=true;break;
            case "null":j["AiPlans"]=null;break;
            case "policy":j["BaseCampaign"]!["FrozenPlanets"]!["PolicyId"]=CampaignColonisationIdentity.InventoryPolicy;break;
            case "enum":j["AiPlans"]![0]!["Stage"]=999;break;
            case "overflow":j["AiPlans"]![0]!["LastPlannedTurn"]=long.MaxValue;break;
        }
        var text=j.ToString(Newtonsoft.Json.Formatting.None);
        if(kind=="duplicate")text=text.Insert(1,"\"FormatVersion\":12,");
        Assert.ThrowsAny<Exception>(()=>codec.DecodeColonisationExpansionRoot(System.Text.Encoding.UTF8.GetBytes(text)));
    }
    [Fact]public void AtomicWriteFailurePreservesExistingBytesAndNamespace()
    {
        var root=Session(Model()).ExportColonisationExpansionSave();Store().Write("atomic",root,false);
        var path=Path.Combine(directory,"unity-campaign-colonisation-expansion-v1-atomic.json");var before=File.ReadAllBytes(path);
        var broken=new ReviewColonisationExpansionSlotStore(directory,codec,p=>{if(p==UnityReviewWriteStage.AfterPartialWrite)throw new IOException();});
        Assert.Throws<ReviewCheckpointStoreException>(()=>broken.Write("atomic",root,true));Assert.Equal(before,File.ReadAllBytes(path));
        Assert.Single(Directory.GetFiles(directory));Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Write("../escape",root,false));
    }
    [Fact]public async Task ActualAiLoadedManifestAndPlanSurviveTypedStoreContinueAndWorkerFork()
    {
        var p=Package();var settings=Settings();settings.Map!.SystemCount=250;settings.Map.EmptyRegionCount=250;settings.Map.Width=2500000;settings.Map.Height=2000000;
        var session=CampaignColonisationExpansionApplication.StartGame(p,settings);
        for(int i=0;i<160&&session.ExportColonisationExpansionSave().AiPlans!.Single().ManifestId==null;i++)
        {var f=session.CaptureStatus();Assert.Equal(SessionPhase.Planning,f.Phase);Assert.True(session.AdvanceTurn(f.Turn,f.CampaignRevision).Accepted);}
        var root=session.ExportColonisationExpansionSave();var plan=Assert.Single(root.AiPlans!);Assert.NotNull(plan.ManifestId);
        var manifest=Assert.Single(root.BaseCampaign!.Colonisation!.ManifestRecords!,x=>x.ManifestId==plan.ManifestId);Assert.Equal(100,manifest.Passengers!.Population);
        Store().Write("loaded",root,false);var m=ReviewUiModel.ContinueColonisationExpansion(p,codec,Store(),"loaded");
        Assert.Equal(codec.EncodeColonisationExpansionRoot(root),Root(m));Assert.DoesNotContain(m.Colonisation().Worlds,w=>w.Id==plan.HomeColonyId);
        Assert.Throws<ArgumentException>(()=>Session(m).CaptureColonisation(plan.ActorId));
        var preview=m.BeginColonisationInteraction(ColonisationInteractionKind.Preview,colonyCommand:Kit(m))!;Assert.True(m.CompleteColonisationInteraction(preview,await Task.Run(preview.Execute)));
        Assert.Equal(codec.EncodeColonisationExpansionRoot(root),Root(m));var advance=m.BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance)!;
        Assert.True(m.CompleteColonisationInteraction(advance,await Task.Run(advance.Execute)));
        Assert.Equal(p.PackageHash,Session(m).ExportColonisationExpansionSave().PackageHash);
        var save=m.BeginStaticCheckpointOperation("loaded-next",false)!;Assert.True(m.CompleteStaticCheckpointOperation(save,await Task.Run(save.Execute)));
        Assert.Equal(Root(m),Root(ReviewUiModel.ContinueColonisationExpansion(p,codec,Store(),"loaded-next")));
    }
    [Theory][InlineData(ColonisationInteractionKind.Submission)][InlineData(ColonisationInteractionKind.PlanningAdvance)]
    public async Task CancelledMutationLeaseNeverPublishesOrClearsNewerExpansionWork(ColonisationInteractionKind kind)
    {
        var m=Model();var before=Root(m);var controller=m.Current;
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        var probe=typeof(PhysicalReviewHost).GetProperty("ColonisationInteractionProbe",BindingFlags.Instance|BindingFlags.NonPublic)!;
        probe.SetValue(Host(m),(Action<string>)(phase=>{if(phase=="before-action"){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(15)))throw new TimeoutException();}}));
        var work=m.BeginColonisationInteraction(kind,colonyCommand:kind==ColonisationInteractionKind.Submission?Kit(m):null)!;var task=Task.Run(work.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));Assert.Equal(before,Root(m));m.AbandonColonisationInteraction(work);Assert.True(m.Busy);
            release.Set();var done=await task.WaitAsync(TimeSpan.FromSeconds(20));Assert.False(m.CompleteColonisationInteraction(work,done));m.AbandonColonisationInteraction(work);
            Assert.False(m.Busy);Assert.Same(controller,m.Current);Assert.Equal(before,Root(m));probe.SetValue(Host(m),null);
            var newer=m.BeginColonisationInteraction(ColonisationInteractionKind.Preview,colonyCommand:Kit(m))!;m.AbandonColonisationInteraction(work);Assert.True(m.Busy);
            Assert.True(m.CompleteColonisationInteraction(newer,await Task.Run(newer.Execute)));Assert.Equal(before,Root(m));
        }
        finally{release.Set();await task.WaitAsync(TimeSpan.FromSeconds(20));probe.SetValue(Host(m),null);m.AbandonColonisationInteraction(work);}
    }
    [Fact]public async Task HeldSaveKeepsCapturedRootStoreAndSlotWithoutPublishingIntoNewOwner()
    {
        var p=Package();using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        var heldStore=new ReviewColonisationExpansionSlotStore(directory,codec,phase=>{if(phase==UnityReviewWriteStage.BeforeWrite){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(15)))throw new TimeoutException();}});
        var old=new ReviewUiModel(p,Settings(),codec,heldStore);var before=Root(old);var work=old.BeginStaticCheckpointOperation("old-owned",false)!;var task=Task.Run(work.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));old.AbandonStaticCheckpointOperation(work);Assert.False(old.Busy);
            var otherDirectory=Path.Combine(directory,"other-owner");var otherStore=new ReviewColonisationExpansionSlotStore(otherDirectory,codec);
            var next=new ReviewUiModel(p,Settings(),codec,otherStore);var advance=next.BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance)!;
            Assert.True(next.CompleteColonisationInteraction(advance,await Task.Run(advance.Execute)));var nextRoot=Root(next);
            var newer=next.BeginStaticCheckpointOperation("new-owned",false)!;var newerDone=await Task.Run(newer.Execute);
            release.Set();var oldDone=await task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.False(next.CompleteStaticCheckpointOperation(work,oldDone));Assert.True(next.Busy);
            Assert.True(next.CompleteStaticCheckpointOperation(newer,newerDone));Assert.Equal(nextRoot,Root(next));Assert.Equal(before,Root(old));
            // A write already entered before cancellation may finish, but only in its captured store/slot with its captured root.
            Assert.Equal(before,codec.EncodeColonisationExpansionRoot(Store().Read("old-owned")));
            Assert.Equal(nextRoot,codec.EncodeColonisationExpansionRoot(otherStore.Read("new-owned")));
            Assert.False(File.Exists(Path.Combine(directory,"unity-campaign-colonisation-expansion-v1-new-owned.json")));
            Assert.False(File.Exists(Path.Combine(otherDirectory,"unity-campaign-colonisation-expansion-v1-old-owned.json")));
        }
        finally{release.Set();await task.WaitAsync(TimeSpan.FromSeconds(20));old.AbandonStaticCheckpointOperation(work);}
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}