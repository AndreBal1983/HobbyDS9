using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class ColonisationCycleIntegrationTests:IDisposable
{
    readonly Xunit.Abstractions.ITestOutputHelper output;
    public ColonisationCycleIntegrationTests(Xunit.Abstractions.ITestOutputHelper output){this.output=output;}
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Colonisation-Growth-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    CampaignColonisationPackageDto Base()
    {
        byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        return ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
    }
    byte[] Resource()=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/colonisation-roster-v2/roster.json"));
    CampaignColonisationCyclePackageDto Package()=>ColonisationCyclePlaytestOptions.CreatePackage(ColonisationGrowthPlaytestOptions.CreatePackage(ColonisationPlaytestOptions.CreateRosterPackage(Base(),Resource(),codec),Planner(),codec),CyclePolicy(),codec);
    byte[] CyclePolicy()=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/colonisation-cycle-v1/cycle-policy.json"));
    byte[] Planner()=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/colonisation-expansion-v1/planner.json"));
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    ReviewColonisationCycleSlotStore Store()=>new(directory,codec);
    ReviewUiModel Model()=>new(Package(),Settings(),codec,Store());
    static PhysicalReviewHost Host(ReviewUiModel m)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)!;
    static ICampaignColonisationCycleSession Session(ReviewUiModel m)=>(ICampaignColonisationCycleSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Host(m))!;
    byte[] Root(ReviewUiModel m)=>codec.EncodeCycleRoot(Session(m).ExportCycleSave());
    static ColonisationCommandDto Kit(ReviewUiModel m)=>new(){ActorId=m.Current.Frame.Actor,CommandId="command.roster.kit",Turn=m.Current.Frame.Status.Turn,Revision=m.Current.Frame.Status.CampaignRevision,Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}};
    [Theory][InlineData(30)][InlineData(250)]
    public async Task RoutineCommandProfileKeepsPreviewReadOnlyAndPublishesExactlyOnce(int systemCount)
    {
        var settings=Settings();settings.Map!.SystemCount=systemCount;settings.Map.EmptyRegionCount=systemCount;
        if(systemCount==250){settings.Map.Width=2000000;settings.Map.Height=2000000;}
        var m=new ReviewUiModel(Package(),settings,codec,Store());var before=Root(m);
        var frame=m.Current.Frame;var option=m.ColonisationActivities().BuildOptions.First(o=>o.Kind==ActivityProductionKind.PublishedShip&&o.Supported);
        var command=new CampaignActivityCommandDto{ActorId=frame.Actor,CommandId="command.polish.production",ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
            Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId=option.DefinitionId,BlueprintRevision=option.BlueprintRevision};
        foreach(var kind in new[]{ColonisationInteractionKind.Preview,ColonisationInteractionKind.Submission})
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();var marks=new Dictionary<string,double>();
            typeof(PhysicalReviewHost).GetProperty("ColonisationInteractionProbe",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(Host(m),(Action<string>)(phase=>marks[phase]=clock.Elapsed.TotalMilliseconds));
            var work=m.BeginColonisationInteraction(kind,command)!;var beginMs=clock.Elapsed.TotalMilliseconds;Assert.NotNull(work);
            Assert.Null(m.BeginColonisationInteraction(kind,command));
            var result=await Task.Run(work.Execute);var workerEnd=clock.Elapsed.TotalMilliseconds;
            Assert.True(m.CompleteColonisationInteraction(work,result),m.Message);var receiveEnd=clock.Elapsed.TotalMilliseconds;
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(new{systemCount,kind=kind.ToString(),beginMs,workerMs=workerEnd-beginMs,receiveMs=receiveEnd-workerEnd,marks,noUnity=true,noLatencyThreshold=true}));
            Assert.False(m.CompleteColonisationInteraction(work,result));Assert.False(m.Busy);
            if(kind==ColonisationInteractionKind.Preview){Assert.True(result.Eligibility!.Accepted);Assert.Equal(before,Root(m));Assert.Same(frame,m.Current.Frame);}
            else {Assert.True(result.Committed);Assert.NotSame(frame,m.Current.Frame);var accepted=Root(m);Assert.NotEqual(before,accepted);Assert.False(m.CompleteColonisationInteraction(work,result));Assert.Equal(accepted,Root(m));}
        }
        typeof(PhysicalReviewHost).GetProperty("ColonisationInteractionProbe",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(Host(m),null);
    }
    [Fact]public void PinnedCompositionAndSeparateNamespaceNeverReinterpretOldSlots()
    {
        var p=Package();var m=Model();Assert.True(m.ColonisationCycleCampaign);Assert.True(m.SupportsKitFreePassengers);Assert.False(m.ColonisationExpansionCampaign);Assert.True(m.ColonisationCampaign);Assert.False(m.ColonisationRosterCampaign);
        var bytes=CyclePolicy();bytes[0]^=1;Assert.Throws<ArgumentException>(()=>ColonisationCyclePlaytestOptions.CreatePackage(p.BaseGrowth!,bytes,codec));
        var old=CampaignColonisationApplication.StartRosterGame(p.BaseGrowth!.BaseRosterV2!,Settings()).ExportColonisationSave();
        var oldStore=ReviewColonisationSlotStore.CreateRosterV2(directory,codec);oldStore.Write("same",old,false);
        Store().Write("same",Session(m).ExportCycleSave(),false);
        Assert.True(File.Exists(Path.Combine(directory,"unity-campaign-colonisation-v2-same.json")));
        Assert.True(File.Exists(Path.Combine(directory,"unity-campaign-colonisation-cycle-v1-same.json")));
        Assert.Equal(codec.EncodeColonisationRoot(old),codec.EncodeColonisationRoot(oldStore.Read("same")));
        Assert.Equal(Root(m),codec.EncodeCycleRoot(Store().Read("same")));
        Assert.ThrowsAny<Exception>(()=>codec.DecodeColonisationRoot(Root(m)));
        Assert.ThrowsAny<Exception>(()=>codec.DecodeCycleRoot(codec.EncodeColonisationRoot(old)));
        Assert.ThrowsAny<Exception>(()=>Session(m).ExportPlayableSave());
        Assert.ThrowsAny<Exception>(()=>Session(m).ExportSave());
        Assert.Equal(Root(m),codec.EncodeCycleRoot(Session(m).ForkInteraction(m.Current.Frame.Status.Turn,m.Current.Frame.Status.CampaignRevision).ExportCycleSave()));
        var ops=(ICampaignColonisationOperations)Session(m);Assert.IsAssignableFrom<ICampaignColonisationCycleSession>(ops.ForkOperations(m.Current.Frame.Status.Turn,m.Current.Frame.Status.CampaignRevision));
    }
    [Fact]public async Task WorkerMutationSaveReopenAndAsyncLoadRetainFullExpansionPlansAndActorViews()
    {
        var m=Model();var p=Package();var frame=m.Current.Frame;var before=Root(m);
        Assert.Equal(3,Assert.Single(m.ColonisationActivities().Ships).StudyWork);Assert.Equal("blueprint.ready.constitution-explorer",Assert.Single(m.Colonisation().OwnShipSources).BlueprintId);Assert.Throws<ArgumentException>(()=>Session(m).CaptureColonisation("civ.cardassian"));
        var work=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:Kit(m))!;Assert.True(m.Busy);Assert.Equal(before,Root(m));
        Assert.True(m.CompleteColonisationInteraction(work,await Task.Run(work.Execute)));Assert.Throws<InvalidOperationException>(()=>work.Execute());
        Assert.Throws<InvalidOperationException>(()=>Host(m).CaptureColonisation(frame));
        var save=m.BeginStaticCheckpointOperation("typed",false)!;Assert.True(m.CompleteStaticCheckpointOperation(save,await Task.Run(save.Execute)),m.Message);var expected=Root(m);
        var continued=ReviewUiModel.ContinueColonisationCycle(p,codec,Store(),"typed");Assert.Equal(expected,Root(continued));Assert.True(continued.ColonisationCycleCampaign);
        Assert.True(continued.Current.ReopenFromSlot("typed").Succeeded); // Explicit host Reopen retains the typed wrapper.
        var controller=m.Current;var load=m.BeginStaticCheckpointOperation("typed",true)!;Assert.True(m.CompleteStaticCheckpointOperation(load,await Task.Run(load.Execute)),m.Message);
        Assert.NotSame(controller,m.Current);Assert.Equal(expected,Root(m));Assert.True(m.ColonisationCycleCampaign);
        var advance=m.BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance)!;Assert.True(m.CompleteColonisationInteraction(advance,await Task.Run(advance.Execute)));Assert.Equal(p.PackageHash,Session(m).ExportCycleSave().PackageHash);
        Assert.NotEmpty(Assert.Single(Session(m).ExportCycleSave().AiPlans!).LatestActions!);
        var advanced=Root(m);Assert.True(m.TryCheckWriteAdmission("advanced",out var admission));Assert.Equal(ReviewCheckpointError.None,admission);var after=m.BeginStaticCheckpointOperation("advanced",false)!;Assert.True(m.CompleteStaticCheckpointOperation(after,await Task.Run(after.Execute)));
        Assert.Equal(advanced,Root(ReviewUiModel.ContinueColonisationCycle(p,codec,Store(),"advanced")));
        Assert.True(m.TryCheckWriteAdmission("advanced",out admission));Assert.Equal(ReviewCheckpointError.SlotOccupied,admission);
        Assert.True(m.Save("sync"));Assert.True(m.Load("sync"));Assert.Equal(advanced,Root(m));
    }
    [Fact]public async Task WrongVersionAndCancelledCompletionCannotReplaceLiveSourceOrClearNewWork()
    {
        var m=Model();var expected=Root(m);var frame=m.Current.Frame;var controller=m.Current;
        var wrongPackage=Package();wrongPackage.PackageHash=new string('0',64);
        var wrongRoot=Session(m).ExportCycleSave();wrongRoot.PackageHash=wrongPackage.PackageHash;Store().Write("wrong",wrongRoot,false);
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
        Assert.ThrowsAny<Exception>(()=>codec.DecodeCycleRoot(System.Text.Encoding.UTF8.GetBytes(text)));
    }
    [Fact]public void AtomicWriteFailurePreservesExistingBytesAndNamespace()
    {
        var root=Session(Model()).ExportCycleSave();Store().Write("atomic",root,false);
        var path=Path.Combine(directory,"unity-campaign-colonisation-cycle-v1-atomic.json");var before=File.ReadAllBytes(path);
        var broken=new ReviewColonisationCycleSlotStore(directory,codec,p=>{if(p==UnityReviewWriteStage.AfterPartialWrite)throw new IOException();});
        Assert.Throws<ReviewCheckpointStoreException>(()=>broken.Write("atomic",root,true));Assert.Equal(before,File.ReadAllBytes(path));
        Assert.Single(Directory.GetFiles(directory));Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Write("../escape",root,false));
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
        var heldStore=new ReviewColonisationCycleSlotStore(directory,codec,phase=>{if(phase==UnityReviewWriteStage.BeforeWrite){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(15)))throw new TimeoutException();}});
        var old=new ReviewUiModel(p,Settings(),codec,heldStore);var before=Root(old);var work=old.BeginStaticCheckpointOperation("old-owned",false)!;var task=Task.Run(work.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));old.AbandonStaticCheckpointOperation(work);Assert.False(old.Busy);
            var otherDirectory=Path.Combine(directory,"other-owner");var otherStore=new ReviewColonisationCycleSlotStore(otherDirectory,codec);
            var next=new ReviewUiModel(p,Settings(),codec,otherStore);var advance=next.BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance)!;
            Assert.True(next.CompleteColonisationInteraction(advance,await Task.Run(advance.Execute)));var nextRoot=Root(next);
            var newer=next.BeginStaticCheckpointOperation("new-owned",false)!;var newerDone=await Task.Run(newer.Execute);
            release.Set();var oldDone=await task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.False(next.CompleteStaticCheckpointOperation(work,oldDone));Assert.True(next.Busy);
            Assert.True(next.CompleteStaticCheckpointOperation(newer,newerDone));Assert.Equal(nextRoot,Root(next));Assert.Equal(before,Root(old));
            // A write already entered before cancellation may finish, but only in its captured store/slot with its captured root.
            Assert.Equal(before,codec.EncodeCycleRoot(Store().Read("old-owned")));
            Assert.Equal(nextRoot,codec.EncodeCycleRoot(otherStore.Read("new-owned")));
            Assert.False(File.Exists(Path.Combine(directory,"unity-campaign-colonisation-cycle-v1-new-owned.json")));
            Assert.False(File.Exists(Path.Combine(otherDirectory,"unity-campaign-colonisation-cycle-v1-old-owned.json")));
        }
        finally{release.Set();await task.WaitAsync(TimeSpan.FromSeconds(20));old.AbandonStaticCheckpointOperation(work);}
    }
    [Fact]public async Task ActualHost250TwoEstablishedCyclesAndThirdCommitSaveContinueWhole14()
    {
        var p=Package();var settings=Settings();settings.Map!.SystemCount=250;settings.Map.EmptyRegionCount=250;settings.Map.Width=2500000;settings.Map.Height=2000000;
        var m=new ReviewUiModel(p,settings,codec,Store());var checkpoints=new Dictionary<string,CampaignColonisationCycleSaveDto>();
        for(int n=0;n<=128;n++)
        {
            var root=Session(m).ExportCycleSave();var actor=Assert.Single(root.CycleActors!);var active=actor.Active;var plan=active?.Assignment;
            void Capture(string name){if(!checkpoints.ContainsKey(name))checkpoints.Add(name,root);}
            if(active!=null)
            {
                if(root.BaseCampaign!.Production!.Any(q=>q.OwnerId==actor.ActorId&&(q.QueueItemId==plan!.KitQueueId||q.QueueItemId==plan.TransportQueueId)))Capture("queue");
                if(root.BaseCampaign.Colonisation!.ManifestRecords!.Any(x=>x.ManifestId==active.SettlementManifestId&&x.Stage==SettlerManifestStage.Loaded))Capture("loaded");
                if(root.BaseCampaign.Colonisation.CampSources!.Any(x=>x.ManifestId==active.SettlementManifestId&&x.Stage==ColonyEstablishmentStage.Founding))Capture("founding");
            }
            output.WriteLine($"host turn={m.Current.Frame.Status.Turn} first={root.AiPlans![0].Stage} receipts={actor.Receipts!.Count} active={active?.Ordinal} committed={active?.SettlementManifestId}");
            if(actor.Receipts!.Count(x=>x.Outcome==ColonisationCycleOutcome.Established)>=2&&active?.Ordinal==3&&active.SettlementManifestId!=null)
            {
                Capture("established-next");Assert.Equal(ColonisationAiStage.GrowthClosed,root.AiPlans![0].Stage);
                Assert.Equal(new[]{"established-next","founding","loaded","queue"},checkpoints.Keys.OrderBy(x=>x).ToArray());
                // Each is a real immutable export of this sole host journey, not a manufactured stage.
                foreach(var pair in checkpoints)
                {
                    var bytes=codec.EncodeCycleRoot(pair.Value);Store().Write(pair.Key,pair.Value,false);
                    var next=ReviewUiModel.ContinueColonisationCycle(p,codec,Store(),pair.Key);Assert.Equal(bytes,Root(next));Assert.True(next.ColonisationCycleCampaign);
                }
                var expected=codec.EncodeCycleRoot(root);var source=m.Current;var frame=source.Frame;
                var save=m.BeginStaticCheckpointOperation("whole",false)!;Assert.True(m.CompleteStaticCheckpointOperation(save,await Task.Run(save.Execute)),m.Message);
                var nextModel=ReviewUiModel.ContinueColonisationCycle(p,codec,Store(),"whole");Assert.NotSame(source,nextModel.Current);Assert.NotSame(Host(m),Host(nextModel));Assert.Equal(expected,Root(nextModel));
                Assert.Equal(root.CycleActors![0].SupplyCampId,Session(nextModel).ExportCycleSave().CycleActors![0].SupplyCampId);
                Assert.DoesNotContain(nextModel.Colonisation().Worlds,w=>w.Id==root.AiPlans![0].HomeColonyId);Assert.Throws<ArgumentException>(()=>Session(nextModel).CaptureColonisation(actor.ActorId));
                await CheckTampering(root,expected);
                var controllerOnly=ReviewUiModel.ContinueColonisationCycle(p,codec,Store(),"whole");Assert.True(controllerOnly.Current.ReopenFromSlot("whole").Succeeded);Assert.Equal(expected,Root(controllerOnly));
                var week=nextModel.BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance)!;Assert.True(nextModel.CompleteColonisationInteraction(week,await Task.Run(week.Execute)));Assert.Equal(frame.Status.Turn+1,nextModel.Current.Frame.Status.Turn);return;
            }
            Assert.Equal(SessionPhase.Planning,m.Current.Frame.Status.Phase);if(n==128)break;
            var work=m.BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance)!;Assert.NotNull(work);Assert.True(m.CompleteColonisationInteraction(work,await Task.Run(work.Execute)),m.Message);
        }
        Assert.Fail("Normal host250 did not complete two cycles and commit third within128 ordinary advances.");
    }
    async Task CheckTampering(CampaignColonisationCycleSaveDto source,byte[] before)
    {
        var p=Package();var m=ReviewUiModel.ContinueColonisationCycle(p,codec,Store(),"whole");var frame=m.Current.Frame;var controller=m.Current;
        foreach(var kind in new[]{"target","ordinal","source"})
        {
            var root=codec.CopyCycleSave(source);
            if(kind=="target")root.CycleActors![0].Receipts![0].TargetPlanetId=root.CycleActors[0].Receipts![1].TargetPlanetId;
            else if(kind=="ordinal")root.CycleActors![0].Receipts![0].StartedTurn++;
            else root.PackageHash=new string('0',64);
            Store().Write("forged-"+kind,root,false);m.LocalDesignDirty=true;
            var load=m.BeginStaticCheckpointOperation("forged-"+kind,true,discard:true)!;Assert.False(m.CompleteStaticCheckpointOperation(load,await Task.Run(load.Execute)));
            Assert.Same(frame,m.Current.Frame);Assert.Same(controller,m.Current);Assert.True(m.LocalDesignDirty);Assert.Equal(before,Root(m));
        }
        var original=ColonisationExpansionPlaytestOptions.CreatePackage(p.BaseGrowth!.BaseRosterV2!,Planner(),codec);
        var old=codec.EncodeColonisationExpansionRoot(CampaignColonisationExpansionApplication.StartGame(original,Settings()).ExportColonisationExpansionSave());
        var growth=codec.EncodeGrowthRoot(CampaignColonisationGrowthApplication.StartGrowthGame(p.BaseGrowth!,Settings()).ExportGrowthSave());
        var repeatPackage=CampaignColonisationRepeatApplication.CreateRepeatPackage(p.BaseGrowth!);
        var repeat=codec.EncodeRepeatRoot(CampaignColonisationRepeatApplication.StartRepeatGame(repeatPackage,Settings()).ExportRepeatSave());
        var bytes=new[]{old,growth,repeat,before};
        await Task.WhenAll(Enumerable.Range(0,8).Select(i=>Task.Run(()=>{
            var b=bytes[i%4];var copy=i%4==0?codec.EncodeColonisationExpansionRoot(codec.DecodeColonisationExpansionRoot(b)):i%4==1?codec.EncodeGrowthRoot(codec.DecodeGrowthRoot(b)):i%4==2?codec.EncodeRepeatRoot(codec.DecodeRepeatRoot(b)):codec.EncodeCycleRoot(codec.DecodeCycleRoot(b));Assert.Equal(b,copy);
        })));
        Assert.ThrowsAny<Exception>(()=>codec.DecodeColonisationExpansionRoot(before));Assert.ThrowsAny<Exception>(()=>codec.DecodeGrowthRoot(before));Assert.ThrowsAny<Exception>(()=>codec.DecodeRepeatRoot(before));
        Assert.All(bytes.Take(3),b=>Assert.ThrowsAny<Exception>(()=>codec.DecodeCycleRoot(b)));
        var detached=codec.DecodeCycleRoot(before);detached.CycleActors![0].Receipts![0].TargetPlanetId="mutated";detached.CycleActors[0].Active!.Assignment!.HoldReason="mutated";Assert.Equal(before,Root(m));
    }
    [Theory][InlineData("null")][InlineData("row")][InlineData("duplicate")][InlineData("missing")][InlineData("foreign")][InlineData("count")][InlineData("kind")][InlineData("text")][InlineData("number")]
    public void CycleShapeRejectsMalformedRowsInBothWireAndObjectPaths(string kind)
    {
        var root=Session(Model()).ExportCycleSave();var j=Newtonsoft.Json.Linq.JObject.Parse(System.Text.Encoding.UTF8.GetString(codec.EncodeCycleRoot(root)));var a=j["CycleActors"]![0]!;
        switch(kind)
        {
            case "null":j["CycleActors"]=null;break;case "row":j["CycleActors"]![0]=null;break;
            case "duplicate":((Newtonsoft.Json.Linq.JArray)j["CycleActors"]!).Add(a.DeepClone());break;
            case "missing":((Newtonsoft.Json.Linq.JObject)a).Remove("SupplyCampId");break;
            case "foreign":a["ActorId"]="foreign";break;case "number":a["NextOrdinal"]="1";break;
            case "text":a["SupplyCampId"]=new string('x',513);break;
            case "kind":a["Receipts"]=Newtonsoft.Json.Linq.JArray.FromObject(new[]{new ColonisationCycleReceiptDto{Ordinal=1,Outcome=(ColonisationCycleOutcome)9}});break;
            case "count":a["Receipts"]=Newtonsoft.Json.Linq.JArray.FromObject(Enumerable.Range(1,8193).Select(n=>new ColonisationCycleReceiptDto{Ordinal=n}));break;
        }
        Assert.ThrowsAny<Exception>(()=>codec.DecodeCycleRoot(System.Text.Encoding.UTF8.GetBytes(j.ToString(Newtonsoft.Json.Formatting.None))));
        if(kind!="missing"&&kind!="number")
        {
            var typed=j.ToObject<CampaignColonisationCycleSaveDto>()!;Assert.ThrowsAny<Exception>(()=>codec.EncodeCycleRoot(typed));
        }
    }
    [Fact]public void CycleRealHumanAiCombatAftermathFreshOrderAndWholeTypedContinue()
    {
        // Supported small generated host campaign, not the native250 expedition or a modified genesis.
        var package=Package();var m=new ReviewUiModel(package,Settings(),codec,Store());
        var human=m.Current.Frame.Actor;Assert.Equal("civ.federation",human);var flow=new ReviewCombatFlow(m);
        int advances=0;
        // Keep the original publicly observed scout stationary for ordinary natural fleet joining.
        var scout=m.ColonisationActivities().Ships.Where(s=>s.Stationary&&s.StudyWork>0)
            .OrderBy(s=>s.ShipId,StringComparer.Ordinal).FirstOrDefault();
        Assert.NotNull(scout);var fleet=scout!.FleetId;
        var original=m.Current.Frame.OwnStrategicFleets.Single(f=>f.FleetId.Value==fleet);
        var system=m.GeneratedMap().Regions.Single(r=>r.NodeId==original.LocationNodeId.Value).SystemId;
        var yard=m.Colonisation().Worlds.Where(w=>w.SystemId==system&&w.Stage==ColonyEstablishmentStage.Established&&w.Population>0&&w.Shipbuilding)
            .OrderByDescending(w=>w.Industry).ThenBy(w=>w.Id,StringComparer.Ordinal).FirstOrDefault();
        Assert.NotNull(yard); // No eligible co-located yard is a real unsupported preparation premise.
        var option=m.ColonisationActivities().BuildOptions.Single(o=>o.DefinitionId=="blueprint.ready.defiant-combat"&&o.BlueprintRevision==1);
        Assert.True(option.Supported,option.Reason);
        var credits=m.Current.Frame.Strategy!.Civilization.Credits;
        var stocks=Newtonsoft.Json.JsonConvert.SerializeObject(yard!.Stocks.OrderBy(r=>r.ResourceDefinitionId,StringComparer.Ordinal));
        var priorQueues=m.Colonisation().Production.Select(q=>q.Id).ToHashSet(StringComparer.Ordinal);
        var priorShips=m.Colonisation().OwnShipSources.Select(s=>s.ShipId).ToHashSet(StringComparer.Ordinal);
        var command=new CampaignActivityCommandDto{ActorId=human,CommandId="command.cycle.combat.prepare",
            ExpectedTurn=m.Current.Frame.Status.Turn,ExpectedRevision=m.Current.Frame.Status.CampaignRevision,
            Kind=CampaignActivityCommandKind.EnqueueShip,TargetId=yard.Id,DefinitionId=option.DefinitionId,BlueprintRevision=option.BlueprintRevision};
        var preview=m.BeginColonisationInteraction(ColonisationInteractionKind.Preview,command:command);
        Assert.NotNull(preview);var eligibility=preview!.Execute();
        Assert.True(m.CompleteColonisationInteraction(preview,eligibility),m.Message);
        Assert.NotNull(eligibility.Eligibility);Assert.True(eligibility.Eligibility!.Accepted,eligibility.Eligibility.Reason);
        var submit=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,command:command);
        Assert.NotNull(submit);var submitted=submit!.Execute();
        Assert.True(m.CompleteColonisationInteraction(submit,submitted),m.Message);Assert.True(submitted.Committed,m.Message);
        var queued=Assert.Single(m.Colonisation().Production,q=>!priorQueues.Contains(q.Id));
        Assert.Equal(yard.Id,queued.ColonyId);Assert.Equal(option.DefinitionId,queued.DefinitionId);Assert.Equal(1,queued.BlueprintRevision);
        Assert.Equal(option.Cost.Industry,queued.RequiredIndustry);Assert.Equal(0,queued.CompletedIndustry);
        Assert.All(queued.Consumed,r=>Assert.Equal(0,r.Amount));Assert.Equal(credits,m.Current.Frame.Strategy!.Civilization.Credits);
        Assert.Equal(stocks,Newtonsoft.Json.JsonConvert.SerializeObject(m.Colonisation().Worlds.Single(w=>w.Id==yard.Id).Stocks.OrderBy(r=>r.ResourceDefinitionId,StringComparer.Ordinal)));
        Assert.Equal(option.Cost.Resources!.OrderBy(r=>r.ResourceDefinitionId,StringComparer.Ordinal).Select(r=>(r.ResourceDefinitionId,r.Amount)),
            queued.Required.OrderBy(r=>r.ResourceDefinitionId,StringComparer.Ordinal).Select(r=>(r.ResourceDefinitionId,r.Amount)));
        output.WriteLine($"prepare yard={yard.Id} scoutFleet={fleet} queue={queued.Id} industry={queued.RequiredIndustry} creditsAtQueue={credits}; localMaterials={stocks}");
        bool sawWork=false,sawConsumption=false;
        while(!m.Colonisation().OwnShipSources.Any(s=>!priorShips.Contains(s.ShipId)&&s.BlueprintId==option.DefinitionId&&s.Revision==1)
            &&m.Current.Frame.Status.Phase==SessionPhase.Planning&&advances<80)
        {
            Assert.Equal(human,m.Current.Frame.Actor);
            Assert.True(flow.DraftNextOrder(fleet,CampaignTravelIntent.StayEngage),flow.Notice);
            Assert.True(flow.SubmitNextOrder("command.cycle.combat.prepare.stay."+advances,true),m.Message);
            Assert.True(flow.AdvancePlanning(true),m.Message);advances++;
            var progress=m.Colonisation().Production.SingleOrDefault(q=>q.Id==queued.Id);
            if(progress!=null)
            {
                sawWork|=progress.CompletedIndustry>0&&progress.CompletedIndustry<progress.RequiredIndustry;
                sawConsumption|=progress.Consumed.Any(r=>r.Amount>0);
                output.WriteLine($"prepare advances={advances} turn={m.Current.Frame.Status.Turn} queue={progress.Id} work={progress.CompletedIndustry}/{progress.RequiredIndustry} consumed={Newtonsoft.Json.JsonConvert.SerializeObject(progress.Consumed)}");
            }
        }
        Assert.Equal(SessionPhase.Planning,m.Current.Frame.Status.Phase); // Early combat is narrower evidence, not bypassed.
        Assert.True(sawWork,"Real streamed intermediate work was not observed.");Assert.True(sawConsumption,"Real local material consumption was not observed.");
        var built=Assert.Single(m.Colonisation().OwnShipSources,s=>!priorShips.Contains(s.ShipId)&&s.BlueprintId==option.DefinitionId&&s.Revision==1);
        Assert.Contains(m.Current.Frame.OwnStrategicFleets.Single(f=>f.FleetId.Value==fleet).ShipIds,s=>s.Value==built.ShipId);
        Assert.Equal(fleet,m.ColonisationActivities().Ships.Single(s=>s.ShipId==built.ShipId).FleetId); // Natural join only; no transfer fallback.
        // Full-root construction provenance is assertion-only, after the public completion/member observations.
        var construction=Session(m).ExportCycleSave().BaseCampaign!;
        var receipt=Assert.Single(construction.ProductionHistory!,r=>r.Order!.QueueItemId==queued.Id);
        Assert.True(receipt.Completed);Assert.Equal(built.ShipId,receipt.ShipId);
        Assert.Equal(human,receipt.Order!.OwnerId);Assert.Equal(yard.Id,receipt.Order.ColonyId);
        Assert.Equal(queued.RequiredIndustry,receipt.Order.CompletedIndustry);
        Assert.Equal(receipt.Order.RequiredResources!.OrderBy(r=>r.ResourceDefinitionId,StringComparer.Ordinal).Select(r=>(r.ResourceDefinitionId,r.Amount)),
            receipt.Order.ConsumedResources!.OrderBy(r=>r.ResourceDefinitionId,StringComparer.Ordinal).Select(r=>(r.ResourceDefinitionId,r.Amount)));
        var frozen=Assert.Single(construction.PublishedBuiltShips!,s=>s.QueueItemId==queued.Id);
        Assert.Equal(built.ShipId,frozen.ShipId);Assert.Equal(human,frozen.OwnerCivilizationId);Assert.Equal(fleet,frozen.FleetIdAtConstruction);
        Assert.Equal(option.DefinitionId,frozen.BlueprintId);Assert.Equal(1,frozen.BlueprintRevision);Assert.Equal(receipt.ResolvedTurn,frozen.TurnBuilt);
        Assert.False(string.IsNullOrEmpty(frozen.PublicationHash));Assert.False(string.IsNullOrEmpty(frozen.SupportHash));Assert.False(string.IsNullOrEmpty(frozen.FitId));
        output.WriteLine($"prepare completed queue={queued.Id} ship={built.ShipId} naturalFleet={fleet} turn={receipt.ResolvedTurn} totalAdvances={advances}");
        while(m.Current.Frame.Status.Phase==SessionPhase.Planning&&advances<80)
        {
            var frame=m.Current.Frame;Assert.Equal(human,frame.Actor);
            Assert.Contains(frame.OwnStrategicFleets,f=>f.FleetId.Value==fleet);
            // Only lawful current contact/route observations choose commands; no exported root is read here.
            var contact=frame.Travel!.KnownContacts.Where(c=>c.Fresh).OrderBy(c=>c.FleetId,StringComparer.Ordinal)
                .FirstOrDefault(c=>flow.PreviewRoute(fleet,c.LastSeenNodeId)?.Route.Success==true);
            Assert.True(flow.DraftNextOrder(fleet,contact==null?CampaignTravelIntent.StayEngage:CampaignTravelIntent.Follow,
                contact?.LastSeenNodeId,contact?.FleetId),flow.Notice);
            Assert.True(flow.SubmitNextOrder("command.cycle.combat.reach."+advances,true),m.Message);
            Assert.True(flow.AdvancePlanning(true),m.Message);advances++;
            output.WriteLine($"reach advances={advances} turn={m.Current.Frame.Status.Turn} phase={m.Current.Frame.Status.Phase} ownContacts={m.Current.Frame.Travel!.KnownContacts.Count}");
        }
        Assert.Equal(SessionPhase.AwaitingCombat,m.Current.Frame.Status.Phase); // No-contact remains a real failure.
        Assert.NotEmpty(m.Current.Frame.Encounters);Assert.InRange(m.Current.Frame.Encounters.Count,1,16);
        void Archive(byte[] bytes,string label)
        {
            // This invocation's external output confinement is verified by the reviewed build preflight.
            // Ordinary test runners may use their own ignored bin output; no machine-specific source guard.
            var path=Path.Combine(AppContext.BaseDirectory,"cycle-combat-"+label+"-"+Guid.NewGuid().ToString("N")+".json");
            using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))file.Write(bytes,0,bytes.Length);
            output.WriteLine($"archive path={path} bytes={bytes.Length} sha256={Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant()}");
        }
        var waiting=Root(m);Assert.True(m.Save("combat-waiting"),m.Message);Archive(waiting,"waiting");
        // Root inspection is assertion-only, after lawful public contact; never an order-selection input.
        var waitingAudit=codec.DecodeCycleRoot(waiting);
        var waitingPhysical=waitingAudit.BaseCampaign!.Campaign!.Integrated!.Physical!;
        var waitingGame=waitingPhysical.Core.Campaign.GameState;
        Assert.All(m.Current.Frame.Encounters,id=>{
            var participants=waitingPhysical.Requests.Single(e=>e.EncounterId==id).Participants;
            Assert.Contains(waitingPhysical.Core.Battles,b=>b.State.EncounterId==id&&b.State.Formations.Any(f=>f.SideId==human));
            Assert.Contains(participants,p=>p.CivilizationId==human);
            Assert.Contains(participants,p=>waitingGame.Civilizations!.Single(c=>c.CivilizationId==p.CivilizationId).ControllerKind==ControllerKind.AI);
        });
        var waitingHost=Host(m);var waitingController=m.Current;
        m=ReviewUiModel.ContinueColonisationCycle(package,codec,Store(),"combat-waiting");
        Assert.NotSame(waitingHost,Host(m));Assert.NotSame(waitingController,m.Current);Assert.Equal(waiting,Root(m));
        Assert.Equal(human,m.Current.Frame.Actor);flow=new ReviewCombatFlow(m);
        string Stocks(CampaignTacticalOwnShip ship)=>Newtonsoft.Json.JsonConvert.SerializeObject(new{
            ship.Condition.Hull,Shields=ship.Condition.Shields.ToArray(),Weapons=ship.Weapons.OrderBy(w=>w.WeaponId,StringComparer.Ordinal)
                .Select(w=>new{w.WeaponId,w.Ammunition}).ToArray()});
        var beforeStocks=new Dictionary<string,string>(StringComparer.Ordinal);
        // Capture every lawful own encounter view BEFORE any round can change a ship.
        foreach(var encounter in m.Current.Frame.Encounters.OrderBy(x=>x,StringComparer.Ordinal).ToArray())
        {
            Assert.True(m.Current.SelectEncounter(encounter));Assert.Equal(human,m.Current.Frame.Actor);
            foreach(var ship in m.Current.Frame.Battle!.OwnShips)
            {
                var stock=Stocks(ship);
                if(beforeStocks.TryGetValue(ship.Condition.ShipId,out var prior))Assert.Equal(prior,stock);
                else beforeStocks.Add(ship.Condition.ShipId,stock);
            }
        }
        Assert.NotEmpty(beforeStocks);
        var staged=new Dictionary<string,(long Hull,string Stocks)>(StringComparer.Ordinal);
        void Acknowledge()
        {
            for(int i=0;flow.PendingPlayback!=null;i++)
            {Assert.InRange(i,0,2);var playback=flow.Playback!;Assert.True(flow.AcknowledgePlayback(playback,flow.PendingPlayback!.Round));}
        }
        foreach(var encounter in m.Current.Frame.Encounters.OrderBy(x=>x,StringComparer.Ordinal).ToArray())
        {
            Assert.True(m.Current.SelectEncounter(encounter));Assert.Equal(human,m.Current.Frame.Actor);
            for(int commits=0;!m.Current.Frame.Battle!.Complete;commits++)
            {
                Assert.InRange(commits,0,2);Acknowledge();
                if(flow.Stage==ReviewCombatStage.RoundResult)Assert.True(flow.PlanNextRound());
                var battle=m.Current.Frame.Battle!;Assert.Equal(encounter,battle.EncounterId);
                Assert.True(flow.CanCommitRound,"Real AI must resolve its own commitment without actor switching.");
                foreach(var order in battle.StandingOrders.Where(o=>o.CanOrder))
                    Assert.True(flow.DraftFleet(order.FleetId,CampaignFleetManeuver.Hold,null),flow.Notice);
                Assert.True(flow.CommitRound("command.cycle.combat.round."+encounter+"."+battle.Round,true),m.Message);
                Assert.Equal(human,m.Current.Frame.Actor);
            }
            Acknowledge();
            foreach(var ship in m.Current.Frame.Battle!.OwnShips)
                staged[ship.Condition.ShipId]=(ship.Condition.Hull,Stocks(ship));
        }
        Assert.NotEmpty(staged);Assert.True(flow.CanFinalize,"The actual whole batch, including AI work, must be complete.");
        var pendingTurn=m.Current.Frame.Status.Turn;Assert.True(flow.FinalizeAftermath(true),m.Message);
        Assert.Equal(SessionPhase.Planning,m.Current.Frame.Status.Phase);Assert.Empty(m.Current.Frame.Encounters);
        Assert.Equal(pendingTurn+1,m.Current.Frame.Status.Turn);
        var finalStocks=m.Current.Frame.OwnShips.ToDictionary(s=>s.Condition.ShipId,Stocks,StringComparer.Ordinal);
        foreach(var pair in staged)
        {
            if(pair.Value.Hull==0)Assert.False(finalStocks.ContainsKey(pair.Key));
            else Assert.Equal(pair.Value.Stocks,finalStocks[pair.Key]);
        }
        Assert.All(staged.Keys,id=>Assert.True(beforeStocks.ContainsKey(id),"Every staged ship requires a real pre-round baseline."));
        Assert.True(staged.Any(p=>beforeStocks[p.Key]!=p.Value.Stocks),
            "A harmless encounter does not prove persistent resource aftermath.");
        var after=Root(m);var aftermathAudit=codec.DecodeCycleRoot(after);
        Assert.Equal(pendingTurn,aftermathAudit.BaseCampaign!.Colonisation!.LastSettlementTurn);
        Assert.Equal(pendingTurn,aftermathAudit.BaseCampaign.LastSettlementProof!.Turn);
        var aftermathPhysical=aftermathAudit.BaseCampaign.Campaign!.Integrated!.Physical!;
        Assert.Equal(SessionPhase.Planning,aftermathPhysical.Phase);
        Assert.Empty(aftermathPhysical.Requests);Assert.Empty(aftermathPhysical.Core.Battles);
        Assert.False(flow.FinalizeAftermath(true));Assert.Equal(after,Root(m));
        Archive(after,"aftermath"); // Preserve honest aftermath even when the real human fleet did not survive.
        var survivor=m.Current.Frame.OwnStrategicFleets.OrderBy(f=>f.FleetId.Value,StringComparer.Ordinal).FirstOrDefault();
        Assert.NotNull(survivor); // No-survivor is narrower evidence, never a passed fresh-order gate.
        Assert.True(flow.DraftNextOrder(survivor!.FleetId.Value,CampaignTravelIntent.Stay),flow.Notice);
        Assert.True(flow.SubmitNextOrder("command.cycle.combat.fresh",true),m.Message);
        Assert.Equal(pendingTurn+1,m.Current.Frame.Status.Turn);Assert.Equal(human,m.Current.Frame.Actor);
        Assert.Equal(finalStocks, m.Current.Frame.OwnShips.ToDictionary(s=>s.Condition.ShipId,Stocks,StringComparer.Ordinal));
        var committed=Root(m);Assert.True(m.Save("combat-after-fresh-order"),m.Message);Archive(committed,"after-fresh-order");
        var oldHost=Host(m);var oldController=m.Current;
        var continued=ReviewUiModel.ContinueColonisationCycle(package,codec,Store(),"combat-after-fresh-order");
        Assert.NotSame(oldHost,Host(continued));Assert.NotSame(oldController,continued.Current);
        Assert.Equal(human,continued.Current.Frame.Actor);Assert.Equal(committed,Root(continued));
        Assert.Equal(finalStocks,continued.Current.Frame.OwnShips.ToDictionary(s=>s.Condition.ShipId,Stocks,StringComparer.Ordinal));
        Assert.Equal(CampaignTravelIntent.Stay,continued.Current.Frame.Travel!.OwnOrders.Single(o=>o.FleetId==survivor.FleetId.Value).Intent);
        output.WriteLine($"Complete typed14 combat gate: normalAdvances={advances}, aftermathTurn={continued.Current.Frame.Status.Turn}, stagedShips={staged.Count}; no fresh Advance after final order/save.");
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
