using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class ColonisationRepeatIntegrationTests:IDisposable
{
    readonly Xunit.Abstractions.ITestOutputHelper output;
    public ColonisationRepeatIntegrationTests(Xunit.Abstractions.ITestOutputHelper output){this.output=output;}
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
    CampaignColonisationRepeatPackageDto Package()=>ColonisationRepeatPlaytestOptions.CreatePackage(ColonisationGrowthPlaytestOptions.CreatePackage(ColonisationPlaytestOptions.CreateRosterPackage(Base(),Resource(),codec),Planner(),codec),RepeatPolicy(),codec);
    byte[] RepeatPolicy()=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/colonisation-repeat-v1/repeat-policy.json"));
    byte[] Planner()=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/colonisation-expansion-v1/planner.json"));
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    ReviewColonisationRepeatSlotStore Store()=>new(directory,codec);
    ReviewUiModel Model()=>new(Package(),Settings(),codec,Store());
    static PhysicalReviewHost Host(ReviewUiModel m)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)!;
    static ICampaignColonisationRepeatSession Session(ReviewUiModel m)=>(ICampaignColonisationRepeatSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Host(m))!;
    byte[] Root(ReviewUiModel m)=>codec.EncodeRepeatRoot(Session(m).ExportRepeatSave());
    static ColonisationCommandDto Kit(ReviewUiModel m)=>new(){ActorId=m.Current.Frame.Actor,CommandId="command.roster.kit",Turn=m.Current.Frame.Status.Turn,Revision=m.Current.Frame.Status.CampaignRevision,Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}};
    [Fact]public void PinnedCompositionAndSeparateNamespaceNeverReinterpretOldSlots()
    {
        var p=Package();var m=Model();Assert.True(m.ColonisationRepeatCampaign);Assert.True(m.SupportsKitFreePassengers);Assert.False(m.ColonisationExpansionCampaign);Assert.True(m.ColonisationCampaign);Assert.False(m.ColonisationRosterCampaign);
        var bytes=RepeatPolicy();bytes[0]^=1;Assert.Throws<ArgumentException>(()=>ColonisationRepeatPlaytestOptions.CreatePackage(p.BaseGrowth!,bytes,codec));
        var old=CampaignColonisationApplication.StartRosterGame(p.BaseGrowth!.BaseRosterV2!,Settings()).ExportColonisationSave();
        var oldStore=ReviewColonisationSlotStore.CreateRosterV2(directory,codec);oldStore.Write("same",old,false);
        Store().Write("same",Session(m).ExportRepeatSave(),false);
        Assert.True(File.Exists(Path.Combine(directory,"unity-campaign-colonisation-v2-same.json")));
        Assert.True(File.Exists(Path.Combine(directory,"unity-campaign-colonisation-repeat-v1-same.json")));
        Assert.Equal(codec.EncodeColonisationRoot(old),codec.EncodeColonisationRoot(oldStore.Read("same")));
        Assert.Equal(Root(m),codec.EncodeRepeatRoot(Store().Read("same")));
        Assert.ThrowsAny<Exception>(()=>codec.DecodeColonisationRoot(Root(m)));
        Assert.ThrowsAny<Exception>(()=>codec.DecodeRepeatRoot(codec.EncodeColonisationRoot(old)));
        Assert.ThrowsAny<Exception>(()=>Session(m).ExportPlayableSave());
        Assert.ThrowsAny<Exception>(()=>Session(m).ExportSave());
        Assert.Equal(Root(m),codec.EncodeRepeatRoot(Session(m).ForkInteraction(m.Current.Frame.Status.Turn,m.Current.Frame.Status.CampaignRevision).ExportRepeatSave()));
        var ops=(ICampaignColonisationOperations)Session(m);Assert.IsAssignableFrom<ICampaignColonisationRepeatSession>(ops.ForkOperations(m.Current.Frame.Status.Turn,m.Current.Frame.Status.CampaignRevision));
    }
    [Fact]public async Task WorkerMutationSaveReopenAndAsyncLoadRetainFullExpansionPlansAndActorViews()
    {
        var m=Model();var p=Package();var frame=m.Current.Frame;var before=Root(m);
        Assert.Equal(3,Assert.Single(m.ColonisationActivities().Ships).StudyWork);Assert.Equal("blueprint.ready.constitution-explorer",Assert.Single(m.Colonisation().OwnShipSources).BlueprintId);Assert.Throws<ArgumentException>(()=>Session(m).CaptureColonisation("civ.cardassian"));
        var work=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:Kit(m))!;Assert.True(m.Busy);Assert.Equal(before,Root(m));
        Assert.True(m.CompleteColonisationInteraction(work,await Task.Run(work.Execute)));Assert.Throws<InvalidOperationException>(()=>work.Execute());
        Assert.Throws<InvalidOperationException>(()=>Host(m).CaptureColonisation(frame));
        var save=m.BeginStaticCheckpointOperation("typed",false)!;Assert.True(m.CompleteStaticCheckpointOperation(save,await Task.Run(save.Execute)),m.Message);var expected=Root(m);
        var continued=ReviewUiModel.ContinueColonisationRepeat(p,codec,Store(),"typed");Assert.Equal(expected,Root(continued));Assert.True(continued.ColonisationRepeatCampaign);
        Assert.True(continued.Current.ReopenFromSlot("typed").Succeeded); // Explicit host Reopen retains the typed wrapper.
        var controller=m.Current;var load=m.BeginStaticCheckpointOperation("typed",true)!;Assert.True(m.CompleteStaticCheckpointOperation(load,await Task.Run(load.Execute)),m.Message);
        Assert.NotSame(controller,m.Current);Assert.Equal(expected,Root(m));Assert.True(m.ColonisationRepeatCampaign);
        var advance=m.BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance)!;Assert.True(m.CompleteColonisationInteraction(advance,await Task.Run(advance.Execute)));Assert.Equal(p.PackageHash,Session(m).ExportRepeatSave().PackageHash);
        Assert.NotEmpty(Assert.Single(Session(m).ExportRepeatSave().AiPlans!).LatestActions!);
        var advanced=Root(m);Assert.True(m.TryCheckWriteAdmission("advanced",out var admission));Assert.Equal(ReviewCheckpointError.None,admission);var after=m.BeginStaticCheckpointOperation("advanced",false)!;Assert.True(m.CompleteStaticCheckpointOperation(after,await Task.Run(after.Execute)));
        Assert.Equal(advanced,Root(ReviewUiModel.ContinueColonisationRepeat(p,codec,Store(),"advanced")));
        Assert.True(m.TryCheckWriteAdmission("advanced",out admission));Assert.Equal(ReviewCheckpointError.SlotOccupied,admission);
        Assert.True(m.Save("sync"));Assert.True(m.Load("sync"));Assert.Equal(advanced,Root(m));
    }
    [Fact]public async Task WrongVersionAndCancelledCompletionCannotReplaceLiveSourceOrClearNewWork()
    {
        var m=Model();var expected=Root(m);var frame=m.Current.Frame;var controller=m.Current;
        var wrongPackage=Package();wrongPackage.PackageHash=new string('0',64);
        var wrongRoot=Session(m).ExportRepeatSave();wrongRoot.PackageHash=wrongPackage.PackageHash;Store().Write("wrong",wrongRoot,false);
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
        Assert.ThrowsAny<Exception>(()=>codec.DecodeRepeatRoot(System.Text.Encoding.UTF8.GetBytes(text)));
    }
    [Theory][InlineData("goals-null")][InlineData("row-null")][InlineData("duplicate")][InlineData("missing-supply")][InlineData("foreign")][InlineData("assignment-null")][InlineData("wrong-goal")][InlineData("receipt")][InlineData("supply-big")][InlineData("turn-string")][InlineData("state")]
    public void RepeatCarrierFailsClosedBeforeReplayForMalformedGoalShape(string kind)
    {
        var j=Newtonsoft.Json.Linq.JObject.Parse(System.Text.Encoding.UTF8.GetString(Root(Model())));var g=j["RepeatGoals"]![0]!;
        switch(kind)
        {
            case "goals-null":j["RepeatGoals"]=null;break;case "row-null":j["RepeatGoals"]![0]=null;break;
            case "duplicate":((Newtonsoft.Json.Linq.JArray)j["RepeatGoals"]!).Add(g.DeepClone());break;
            case "missing-supply":((Newtonsoft.Json.Linq.JObject)g).Remove("SupplyCampId");break;
            case "foreign":g["ActorId"]="foreign";break;case "assignment-null":g["Assignment"]=null;break;
            case "wrong-goal":g["Assignment"]!["GoalId"]="establish-first-frontier-colony-v1";break;
            case "receipt":g["Assignment"]!["LatestActions"]=Newtonsoft.Json.Linq.JArray.FromObject(new[]{new ColonisationAiActionReceiptDto()});break;
            case "supply-big":g["SupplyCampId"]=new string('x',513);break;case "turn-string":g["StartedTurn"]="1";break;case "state":g["State"]=999;break;
        }
        Assert.ThrowsAny<Exception>(()=>codec.DecodeRepeatRoot(System.Text.Encoding.UTF8.GetBytes(j.ToString(Newtonsoft.Json.Formatting.None))));
    }
    [Fact]public async Task PlausibleGoalTamperCannotReplaceFrameAndThreeCodecContextsRemainIndependent()
    {
        var p=Package();var m=Model();var before=Root(m);var frame=m.Current.Frame;var root=Session(m).ExportRepeatSave();root.RepeatGoals![0].Assignment!.HoldReason="forged";Store().Write("forged",root,false);
        m.LocalDesignDirty=true;Assert.False(m.Load("forged",true));Assert.Same(frame,m.Current.Frame);Assert.True(m.LocalDesignDirty);Assert.Equal(before,Root(m));
        var growth=codec.EncodeGrowthRoot(CampaignColonisationGrowthApplication.StartGrowthGame(p.BaseGrowth!,Settings()).ExportGrowthSave());
        var original=ColonisationExpansionPlaytestOptions.CreatePackage(p.BaseGrowth!.BaseRosterV2!,Planner(),codec);
        var old=codec.EncodeColonisationExpansionRoot(CampaignColonisationExpansionApplication.StartGame(original,Settings()).ExportColonisationExpansionSave());
        await Task.WhenAll(Enumerable.Range(0,6).Select(i=>Task.Run(()=>{for(int n=0;n<2;n++)
        {
            if(i%3==0)Assert.Equal(old,codec.EncodeColonisationExpansionRoot(codec.DecodeColonisationExpansionRoot(old)));
            else if(i%3==1)Assert.Equal(growth,codec.EncodeGrowthRoot(codec.DecodeGrowthRoot(growth)));
            else{var copy=codec.DecodeRepeatRoot(before);copy.RepeatGoals![0].Assignment!.HoldReason="detached";Assert.Equal(before,codec.EncodeRepeatRoot(codec.DecodeRepeatRoot(before)));}
        }})));
        Assert.ThrowsAny<Exception>(()=>codec.DecodeGrowthRoot(before));Assert.ThrowsAny<Exception>(()=>codec.DecodeRepeatRoot(growth));
        var copyPackage=codec.CopyRepeatPackage(p);copyPackage.BaseGrowth!.PlannerPolicy!.MaximumAttempts=1;Assert.Equal(before,Root(m));
    }
    [Fact]public void AtomicWriteFailurePreservesExistingBytesAndNamespace()
    {
        var root=Session(Model()).ExportRepeatSave();Store().Write("atomic",root,false);
        var path=Path.Combine(directory,"unity-campaign-colonisation-repeat-v1-atomic.json");var before=File.ReadAllBytes(path);
        var broken=new ReviewColonisationRepeatSlotStore(directory,codec,p=>{if(p==UnityReviewWriteStage.AfterPartialWrite)throw new IOException();});
        Assert.Throws<ReviewCheckpointStoreException>(()=>broken.Write("atomic",root,true));Assert.Equal(before,File.ReadAllBytes(path));
        Assert.Single(Directory.GetFiles(directory));Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Write("../escape",root,false));
    }
    [Fact]public async Task Actual250BothAssignmentsSupplyAndSecondColonySurviveEveryTypedPersistenceStage()
    {
        var p=Package();var settings=Settings();settings.Map!.SystemCount=250;settings.Map.EmptyRegionCount=250;settings.Map.Width=2500000;settings.Map.Height=2000000;
        var live=CampaignColonisationRepeatApplication.StartRepeatGame(p,settings);var seen=new HashSet<string>();
        for(int week=0;week<=128;week++)
        {
            var root=live.ExportRepeatSave();var goal=Assert.Single(root.RepeatGoals!);var plan=goal.Assignment!;
            var cargo=root.BaseCampaign!.Colonisation!.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==goal.SettlementManifestId);
            var stage=goal.State==ColonisationRepeatState.Established?"established":root.BaseCampaign.Colonisation.CampSources!.Any(c=>c.CampId==plan.CampId&&c.Stage==ColonyEstablishmentStage.Founding)?"founding":cargo?.Stage==SettlerManifestStage.Loaded?"loaded":goal.State==ColonisationRepeatState.Active&&root.BaseCampaign.Production!.Any(q=>q.QueueItemId==plan.KitQueueId)?"queue":null;
            var secondQueues=root.BaseCampaign.Production!.Where(q=>q.QueueItemId==plan.KitQueueId||q.QueueItemId==plan.TransportQueueId).ToArray();
            foreach(var queue in secondQueues)
            {
                Assert.Equal(goal.ActorId,queue.OwnerId);
                output.WriteLine($"SECOND queue turn={live.CaptureStatus().Turn} state={goal.State} queue={queue.QueueItemId} owner={queue.OwnerId} kind={queue.Kind} work={queue.CompletedIndustry}/{queue.RequiredIndustry} kitId={plan.KitQueueId} transportId={plan.TransportQueueId} manifest={cargo?.ManifestId} manifestStage={cargo?.Stage}");
                await Check("queue",root);
            }
            if(goal.SupplyCampId!=null)await Check("supply",root);
            if(stage!=null)await Check(stage,root);
            if(stage=="established"){Assert.Equal(new[]{"established","founding","loaded","queue","supply"},seen.OrderBy(x=>x).ToArray());return;}
            if(week==128)break;var status=live.CaptureStatus();Assert.Equal(SessionPhase.Planning,status.Phase);Assert.True(live.AdvanceTurn(status.Turn,status.CampaignRevision).Accepted);
        }
        Assert.Fail("Normal250 second mission did not establish within128 ordinary advances.");
        async Task Check(string stage,CampaignColonisationRepeatSaveDto root)
        {
            if(!seen.Add(stage))return;var expected=codec.EncodeRepeatRoot(root);Store().Write(stage,root,false);
            var model=ReviewUiModel.ContinueColonisationRepeat(p,codec,Store(),stage);Assert.Equal(expected,Root(model));Assert.True(model.ColonisationRepeatCampaign);Assert.False(model.ColonisationGrowthCampaign);
            Assert.Equal(root.RepeatGoals![0].SupplyCampId,Session(model).ExportRepeatSave().RepeatGoals![0].SupplyCampId);
            Assert.DoesNotContain(model.Colonisation().Worlds,w=>w.Id==root.AiPlans![0].HomeColonyId);Assert.Throws<ArgumentException>(()=>Session(model).CaptureColonisation(root.AiPlans![0].ActorId));
            var preview=model.BeginColonisationInteraction(ColonisationInteractionKind.Preview,colonyCommand:Kit(model))!;Assert.True(model.CompleteColonisationInteraction(preview,await Task.Run(preview.Execute)));Assert.Equal(expected,Root(model));
            var save=model.BeginStaticCheckpointOperation(stage+"-worker",false)!;Assert.True(model.CompleteStaticCheckpointOperation(save,await Task.Run(save.Execute)));Assert.Equal(expected,codec.EncodeRepeatRoot(Store().Read(stage+"-worker")));
            var controller=model.Current;Assert.True(model.Current.ReopenFromSlot(stage).Succeeded);Assert.Equal(expected,Root(model));
            Assert.True(model.Save(stage+"-sync"));Assert.True(model.Load(stage+"-sync"));Assert.NotSame(controller,model.Current);Assert.Equal(expected,Root(model));
            var advance=model.BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance)!;Assert.True(model.CompleteColonisationInteraction(advance,await Task.Run(advance.Execute)));Assert.Single(Session(model).ExportRepeatSave().RepeatGoals!);
        }
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
        var heldStore=new ReviewColonisationRepeatSlotStore(directory,codec,phase=>{if(phase==UnityReviewWriteStage.BeforeWrite){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(15)))throw new TimeoutException();}});
        var old=new ReviewUiModel(p,Settings(),codec,heldStore);var before=Root(old);var work=old.BeginStaticCheckpointOperation("old-owned",false)!;var task=Task.Run(work.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));old.AbandonStaticCheckpointOperation(work);Assert.False(old.Busy);
            var otherDirectory=Path.Combine(directory,"other-owner");var otherStore=new ReviewColonisationRepeatSlotStore(otherDirectory,codec);
            var next=new ReviewUiModel(p,Settings(),codec,otherStore);var advance=next.BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance)!;
            Assert.True(next.CompleteColonisationInteraction(advance,await Task.Run(advance.Execute)));var nextRoot=Root(next);
            var newer=next.BeginStaticCheckpointOperation("new-owned",false)!;var newerDone=await Task.Run(newer.Execute);
            release.Set();var oldDone=await task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.False(next.CompleteStaticCheckpointOperation(work,oldDone));Assert.True(next.Busy);
            Assert.True(next.CompleteStaticCheckpointOperation(newer,newerDone));Assert.Equal(nextRoot,Root(next));Assert.Equal(before,Root(old));
            // A write already entered before cancellation may finish, but only in its captured store/slot with its captured root.
            Assert.Equal(before,codec.EncodeRepeatRoot(Store().Read("old-owned")));
            Assert.Equal(nextRoot,codec.EncodeRepeatRoot(otherStore.Read("new-owned")));
            Assert.False(File.Exists(Path.Combine(directory,"unity-campaign-colonisation-repeat-v1-new-owned.json")));
            Assert.False(File.Exists(Path.Combine(otherDirectory,"unity-campaign-colonisation-repeat-v1-old-owned.json")));
        }
        finally{release.Set();await task.WaitAsync(TimeSpan.FromSeconds(20));old.AbandonStaticCheckpointOperation(work);}
    }
    [Fact]public async Task RealHumanKitFreeLoadUsesReviewedWorkerAndImmutableLoadedContinueThenMergesOnReturn()
    {
        var p=Package();var settings=Settings();settings.Map!.SystemCount=250;settings.Map.EmptyRegionCount=250;settings.Map.Width=2500000;settings.Map.Height=2000000;
        var source=CampaignColonisationRepeatApplication.StartRepeatGame(p,settings);var status=source.CaptureStatus();
        Assert.True(source.SubmitActivity(new(){ActorId="civ.federation",CommandId="command.growth.client.transport",ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.mars",DefinitionId="blueprint.ready.galaxy-colony-transport",BlueprintRevision=1}).Accepted);
        for(int week=0;week<32&&!source.ExportRepeatSave().BaseCampaign!.PublishedBuiltShips!.Any(b=>b.OwnerCivilizationId=="civ.federation");week++)
        {status=source.CaptureStatus();Assert.Equal(SessionPhase.Planning,status.Phase);Assert.True(source.AdvanceTurn(status.Turn,status.CampaignRevision).Accepted);}
        Store().Write("human-yard",source.ExportRepeatSave(),false);var m=ReviewUiModel.ContinueColonisationRepeat(p,codec,Store(),"human-yard");
        var observation=m.Colonisation();var ship=Assert.Single(observation.Transports);var home=observation.Worlds.First(w=>w.Stage==ColonyEstablishmentStage.Established&&w.Population>=600&&w.Cohorts.Any(c=>c.Population>=400));var cohort=home.Cohorts.First(c=>c.Population>=400);
        var frame=m.Current.Frame;var command=new ColonisationCommandDto{ActorId=frame.Actor,CommandId="command.growth.client.passengers",Turn=frame.Status.Turn,Revision=frame.Status.CampaignRevision,Action=ColonisationAction.LoadPassengers,Embark=new(){ShipId=ship.ShipId,ColonyId=home.Id,CohortId=cohort.Id,Population=100,Food=20}};
        var before=Root(m);var preview=m.BeginColonisationInteraction(ColonisationInteractionKind.Preview,colonyCommand:command)!;command.Embark.Food=999;
        var checkedOrder=await Task.Run(preview.Execute);Assert.True(m.CompleteColonisationInteraction(preview,checkedOrder));Assert.True(checkedOrder.ColonisationEligibility!.Available);Assert.Equal(before,Root(m));
        var submit=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:preview.ColonisationCommand)!;Assert.True(m.CompleteColonisationInteraction(submit,await Task.Run(submit.Execute)));
        var root=Session(m).ExportRepeatSave();var manifest=Assert.Single(root.BaseCampaign!.Colonisation!.ManifestRecords!,x=>x.OwnerId==frame.Actor&&x.Kind==SettlerManifestKind.ImmigrationOnly);
        Assert.Equal(100,manifest.Passengers!.Population);Assert.Equal(20,manifest.Food);Assert.Equal(0,manifest.EquipmentConsumed);Assert.DoesNotContain(manifest.Cargo!,r=>r.ResourceDefinitionId=="resource.settlement-kit");
        Assert.ThrowsAny<Exception>(()=>codec.DecodeGrowthRoot(Root(m)));Assert.ThrowsAny<Exception>(()=>codec.DecodeColonisationExpansionRoot(Root(m)));
        var saved=m.BeginStaticCheckpointOperation("human-loaded",false)!;Assert.True(m.CompleteStaticCheckpointOperation(saved,await Task.Run(saved.Execute)));var expected=Root(m);var old=m.Current;
        var next=ReviewUiModel.ContinueColonisationRepeat(p,codec,Store(),"human-loaded");Assert.NotSame(old,next.Current);Assert.True(next.SupportsKitFreePassengers);Assert.Equal(expected,Root(next));
        Assert.Equal(100,next.Colonisation().Transports.Single(t=>t.ShipId==ship.ShipId).Population);
        frame=next.Current.Frame;var unload=new ColonisationCommandDto{ActorId=frame.Actor,CommandId="command.growth.client.return",Turn=frame.Status.Turn,Revision=frame.Status.CampaignRevision,Action=ColonisationAction.UnloadTransport,Unload=new(){ManifestId=manifest.ManifestId,ColonyId=home.Id}};
        var work=next.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:unload)!;Assert.True(next.CompleteColonisationInteraction(work,await Task.Run(work.Execute)));
        var returned=Session(next).ExportRepeatSave();Assert.Equal(SettlerManifestStage.Unloaded,returned.BaseCampaign!.Colonisation!.ManifestRecords!.Single(x=>x.ManifestId==manifest.ManifestId).Stage);
        Assert.Contains(returned.BaseCampaign.Colonisation.LandedCohortLineages!,x=>x.ManifestId==manifest.ManifestId&&x.CohortId==manifest.Passengers.CohortId);
        Assert.DoesNotContain(returned.BaseCampaign.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.EntityIdentities,x=>x.EntityId==manifest.Passengers.CohortId);
        Assert.Equal(cohort.Population,next.Colonisation().Worlds.Single(w=>w.Id==home.Id).Cohorts.Single(c=>c.Id==cohort.Id).Population);
        var current=Root(next);var stale=next.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:unload);Assert.Null(stale);Assert.Equal(current,Root(next));
        var final=next.BeginStaticCheckpointOperation("human-return",false)!;Assert.True(next.CompleteStaticCheckpointOperation(final,await Task.Run(final.Execute)));Assert.Equal(current,Root(ReviewUiModel.ContinueColonisationRepeat(p,codec,Store(),"human-return")));
    }
    [Fact]public async Task SharedCodecNeverLeaksVocabularyContextAcrossConcurrentOldAndGrowthCalls()
    {
        var growth=Root(Model());var original=ColonisationExpansionPlaytestOptions.CreatePackage(Package().BaseGrowth!.BaseRosterV2!,Planner(),codec);
        var old=codec.EncodeColonisationExpansionRoot(CampaignColonisationExpansionApplication.StartGame(original,Settings()).ExportColonisationExpansionSave());
        var tasks=Enumerable.Range(0,4).Select(i=>Task.Run(()=>{
            for(int n=0;n<3;n++)
            {
                if(i%2==0){var copy=codec.DecodeRepeatRoot(growth);Assert.Equal(growth,codec.EncodeRepeatRoot(copy));copy.AiPlans![0].HoldReason="detached";Assert.Equal(growth,codec.EncodeRepeatRoot(codec.DecodeRepeatRoot(growth)));}
                else Assert.Equal(old,codec.EncodeColonisationExpansionRoot(codec.DecodeColonisationExpansionRoot(old)));
            }
        }));await Task.WhenAll(tasks);
        Assert.ThrowsAny<Exception>(()=>codec.DecodeRepeatRoot(old));Assert.ThrowsAny<Exception>(()=>codec.DecodeColonisationExpansionRoot(growth));
        var legacy=new ReviewUiModel(original,Settings(),codec,new ReviewColonisationExpansionSlotStore(directory,codec));Assert.False(legacy.SupportsKitFreePassengers);Assert.False(legacy.ColonisationRepeatCampaign);
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
