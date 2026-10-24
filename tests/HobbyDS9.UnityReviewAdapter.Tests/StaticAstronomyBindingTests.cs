using System.Text;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class StaticAstronomyBindingTests : IDisposable
{
    private static PhysicalReviewHost ControllerHost(ReviewController controller)=>(PhysicalReviewHost)typeof(ReviewController)
        .GetField("host",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(controller)!;
    [Fact] public void ControllerInterfaceReopenUsesV2AndProtectsOldHandlesAndFailedDraft()
    {
        Store().Write("one",Root(),false);
        var opened=PhysicalReviewHost.OpenStaticAstronomyCampaignFromSlot(Template(),codec,Store(),"one","civ.federation");
        var oldHost=opened.Host!;var c=new ReviewController(oldHost,"civ.federation");var oldFrame=c.Frame;
        var oldHandle=oldHost.SaveCommitted();var oldPair=oldHost.CaptureGeneratedPresentation(oldFrame);
        // Public controller invokes the actual internal ITacticalClientHost.Reopen seam.
        Assert.True(c.ReopenFromSlot("one").Succeeded);var next=ControllerHost(c);
        Assert.NotSame(oldHost,next);Assert.NotSame(oldFrame,c.Frame);
        var pair=next.CaptureGeneratedPresentation(c.Frame);next.ValidateGeneratedPresentation(c.Frame,pair);
        Assert.Throws<InvalidOperationException>(()=>next.ValidateGeneratedPresentation(c.Frame,oldPair));
        Assert.Throws<ArgumentException>(()=>next.LoadCommitted(oldHandle,c.Frame.Actor,null));
        oldHost.ValidateGeneratedPresentation(oldFrame,oldPair); // Old host was not mutated.
        Assert.True(c.SaveCommittedToSlot("after-reopen").Succeeded);
        Assert.Equal(CampaignStaticAstronomyIdentity.Profile,Store().Read("after-reopen").ProfileId);
        var fleet=c.Frame.OwnStrategicFleets.First().FleetId.Value;
        Assert.True(c.EditTravel(fleet,CampaignTravelIntent.StayEngage,null));
        var frame=c.Frame;var draft=c.TravelDraft;
        Assert.False(c.ReopenFromSlot("missing",true).Succeeded);Assert.Same(frame,c.Frame);Assert.Same(draft,c.TravelDraft);
        Assert.Same(next,ControllerHost(c));next.ValidateGeneratedPresentation(frame,pair);
    }
    [Fact] public void ControllerInterfaceReopenKeepsV1FactoryAndExactSaveBytes()
    {
        var oldBytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"static-v1/aftermath.json"));
        var store=new ReviewGeneratedCampaignSlotStore(directory,codec);store.Write("old",codec.DecodeGeneratedCampaignRoot(oldBytes),false);
        var opened=PhysicalReviewHost.OpenGeneratedCampaignFromSlot(Template(),codec,store,"old","civ.federation");
        var c=new ReviewController(opened.Host!,"civ.federation");Assert.True(c.ReopenFromSlot("old").Succeeded);
        var next=ControllerHost(c);Assert.NotSame(opened.Host,next);
        Assert.Throws<InvalidOperationException>(()=>next.CaptureGeneratedPresentation(c.Frame));
        Assert.True(c.SaveCommittedToSlot("copied").Succeeded);
        Assert.Equal(oldBytes,File.ReadAllBytes(Path.Combine(directory,"unity-generated-campaign-v7-copied.json")));
    }
    private readonly string directory=Path.Combine(Path.GetTempPath(),"hobby-static-binding-"+Guid.NewGuid().ToString("N"));
    private readonly ReviewNewtonsoftCodec codec=new();
    private static CampaignPlayableFleetPackageDto Template()=>PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
    private static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=250,EmptyRegionCount=250,Width=2500000,Height=2000000,
        QuietEmptyAreaTargetPermille=2000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    private static readonly Lazy<byte[]> Initial=new(()=>new ReviewNewtonsoftCodec().EncodeStaticAstronomyCampaignRoot(
        CampaignStaticAstronomyApplication.StartGame(Template(),Settings()).ExportGeneratedSave()));
    private CampaignGeneratedSaveDto Root()=>codec.DecodeStaticAstronomyCampaignRoot(Initial.Value);
    private ReviewStaticAstronomyCampaignSlotStore Store(Action<UnityReviewWriteStage>? probe=null)=>new(directory,codec,probe);
    private ReviewUiModel Model(){Store().Write("initial",Root(),false);return ReviewUiModel.ContinueStaticAstronomyCampaign(Template(),codec,Store(),"initial");}
    private static string Json<T>(T value)=>JsonSerializer.Serialize(value);
    [Fact] public void NewModelStartsExplicitV2AndSavesOnlyItsOwnProfile()
    {
        var m=new ReviewUiModel(Template(),Settings(),codec,Store());
        Assert.True(m.StaticAstronomyCampaign);Assert.True(m.GeneratedCampaign);
        Assert.True(m.IsCurrentGeneratedPresentation(m.GeneratedPresentation()));Assert.True(m.Save("new"));
        var root=Store().Read("new");Assert.Equal(CampaignStaticAstronomyIdentity.Profile,root.ProfileId);
        Assert.Equal(CampaignStaticAstronomyIdentity.Policy,root.PolicyId);
        Assert.ThrowsAny<Exception>(()=>codec.EncodeGeneratedCampaignRoot(root));
        Assert.Single(Directory.GetFiles(directory));
    }
    private sealed class FaultCodec : IStaticAstronomyReviewHostCodec
    {
        private readonly ReviewNewtonsoftCodec inner=new();
        public bool FailKnownCopy;
        public CampaignGeneratedMapDto CopyKnownGeneratedMap(CampaignGeneratedMapDto map)=>FailKnownCopy?throw new IOException("late presentation copy"):inner.CopyKnownGeneratedMap(map);
        public CampaignGeneratedSaveDto CopyStaticAstronomySave(CampaignGeneratedSaveDto root)=>inner.CopyStaticAstronomySave(root);
        public CampaignGeneratedSaveDto CopySave(CampaignGeneratedSaveDto root)=>inner.CopySave(root);
        public CampaignPlayableFleetPackageDto CopyPackage(CampaignPlayableFleetPackageDto value)=>inner.CopyPackage(value);
        public CampaignPlayableFleetSaveDto CopySave(CampaignPlayableFleetSaveDto value)=>inner.CopySave(value);
        public CampaignIntegratedPlaytestPackageDto CopyPackage(CampaignIntegratedPlaytestPackageDto value)=>inner.CopyPackage(value);
        public CampaignIntegratedPlaytestSaveDto CopySave(CampaignIntegratedPlaytestSaveDto value)=>inner.CopySave(value);
        public CampaignTacticalTravelPackageDto CopyPackage(CampaignTacticalTravelPackageDto value)=>inner.CopyPackage(value);
        public CampaignTacticalTravelSaveDto CopySave(CampaignTacticalTravelSaveDto value)=>inner.CopySave(value);
    }
    [Fact] public void LatePresentationFailureCannotPublishCandidateAtEitherReopenBoundary()
    {
        Store().Write("one",Root(),false);var faulty=new FaultCodec();
        var opened=PhysicalReviewHost.OpenStaticAstronomyCampaignFromSlot(Template(),faulty,Store(),"one","civ.federation");
        Assert.True(opened.Succeeded);var host=opened.Host!;var before=host.Read("civ.federation");var pair=host.CaptureGeneratedPresentation(before);
        var checkpoint=host.SaveCommitted();faulty.FailKnownCopy=true;
        Assert.Throws<IOException>(()=>host.LoadCommitted(checkpoint,before.Actor,null));
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,host.LoadCommittedFromSlot("one",before.Actor).Error);
        Assert.False(PhysicalReviewHost.OpenStaticAstronomyCampaignFromSlot(Template(),faulty,Store(),"one",before.Actor).Succeeded);
        faulty.FailKnownCopy=false;Assert.Equal(before.Generation,host.Read(before.Actor).Generation);
        host.ValidateGeneratedPresentation(before,pair);
    }

    [Fact] public void DirectContinueSaveReopenIsExactAndPresentationCannotBeMutatedOrUsedAfterAdvance()
    {
        var m=Model();Assert.True(m.StaticAstronomyCampaign);Assert.True(m.GeneratedCampaign);
        var pair=m.GeneratedPresentation();Assert.True(m.IsCurrentGeneratedPresentation(pair));
        Assert.Equal(pair.Actor,pair.Astronomy.ActorId);Assert.Equal(pair.MapContentHash,pair.Astronomy.MapContentHash);
        var map=pair.KnownMap;var original=Json(map);map.Regions.Clear();Assert.Equal(original,Json(pair.KnownMap));
        Assert.True(m.Save("again"));Assert.Equal(Initial.Value,File.ReadAllBytes(Path.Combine(directory,"unity-generated-static-astronomy-v2-again.json")));
        var fresh=ReviewUiModel.ContinueStaticAstronomyCampaign(Template(),codec,Store(),"again");
        Assert.Equal(original,Json(fresh.GeneratedPresentation().KnownMap));Assert.Equal(Json(pair.Astronomy),Json(fresh.GeneratedPresentation().Astronomy));
        Assert.False(fresh.IsCurrentGeneratedPresentation(pair)); // Different host, identical state.
        Assert.True(m.Advance());Assert.False(m.IsCurrentGeneratedPresentation(pair));
        Assert.True(m.Load("initial",true));Assert.False(m.IsCurrentGeneratedPresentation(pair));
    }
    [Fact] public void ForeignActorAndOldFrameCannotAuthorizePresentation()
    {
        Store().Write("one",Root(),false);
        var opened=PhysicalReviewHost.OpenStaticAstronomyCampaignFromSlot(Template(),codec,Store(),"one","civ.federation");
        Assert.True(opened.Succeeded);var host=opened.Host!;var frame=opened.Frame!;var pair=host.CaptureGeneratedPresentation(frame);
        Assert.Throws<ArgumentException>(()=>host.Read("civ.cardassian")); // AI actor cannot obtain a player frame at all.
        var checkpoint=host.SaveCommitted();var reloaded=host.LoadCommitted(checkpoint,frame.Actor,null);
        Assert.Throws<InvalidOperationException>(()=>host.CaptureGeneratedPresentation(frame));
        host.ValidateGeneratedPresentation(reloaded,host.CaptureGeneratedPresentation(reloaded));
    }
    [Fact] public void LateInvalidMapLoadRetainsLiveFrameDraftPresentationAndSavedFile()
    {
        var m=Model();Assert.True(m.Save("good"));var original=File.ReadAllBytes(Path.Combine(directory,"unity-generated-static-astronomy-v2-good.json"));
        var broken=Root();broken.Map.Settings.Map.Seed="43";Store().Write("bad",broken,false);
        var current=m.Current;var pair=m.GeneratedPresentation();var draft=m.ResearchInputs()!;draft.Adjust(ScienceDiscipline.Biotech,6);
        Assert.False(m.Load("bad",true));Assert.Same(current,m.Current);Assert.Same(draft,m.ResearchDraft);Assert.True(m.HasLocalEdits);
        Assert.True(m.IsCurrentGeneratedPresentation(pair));Assert.Equal(original,File.ReadAllBytes(Path.Combine(directory,"unity-generated-static-astronomy-v2-good.json")));
        Assert.Throws<InvalidOperationException>(()=>ReviewUiModel.ContinueStaticAstronomyCampaign(Template(),codec,Store(),"bad"));
    }
    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void MixedV1V2IdentityNeverFallsBack(int field)
    {
        var v2=Root();var v1=codec.DecodeGeneratedCampaignRoot(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"static-v1/aftermath.json")));
        Assert.ThrowsAny<Exception>(()=>codec.EncodeGeneratedCampaignRoot(v2));Assert.ThrowsAny<Exception>(()=>codec.EncodeStaticAstronomyCampaignRoot(v1));
        if(field==0){v2.ProfileId=v1.ProfileId;v1.ProfileId=CampaignStaticAstronomyIdentity.Profile;}
        if(field==1){v2.PolicyId=v1.PolicyId;v1.PolicyId=CampaignStaticAstronomyIdentity.Policy;}
        if(field==2){v2.Campaign.Integrated!.Physical!.Core!.ProfileId=CampaignGeneratedIdentity.EmbeddedCoreProfile;v1.Campaign.Integrated!.Physical!.Core!.ProfileId=CampaignStaticAstronomyIdentity.EmbeddedCoreProfile;}
        Assert.ThrowsAny<Exception>(()=>codec.EncodeStaticAstronomyCampaignRoot(v2));Assert.ThrowsAny<Exception>(()=>codec.EncodeGeneratedCampaignRoot(v1));
    }
    [Fact] public void ClosedShapeRejectsMalformedDuplicateUnknownAndStrippedRoots()
    {
        var bytes=Initial.Value;var json=Encoding.UTF8.GetString(bytes);
        foreach(var text in new[]{json[..^1],json.Insert(1,"\"Unknown\":true,"),json.Replace("\"FormatVersion\":7","\"FormatVersion\":7,\"FormatVersion\":7")})
            Assert.ThrowsAny<Exception>(()=>codec.DecodeStaticAstronomyCampaignRoot(Encoding.UTF8.GetBytes(text)));
        Assert.ThrowsAny<Exception>(()=>codec.EncodePlayableFleetRoot(Root().Campaign));
        Assert.ThrowsAny<Exception>(()=>codec.DecodeStaticAstronomyCampaignRoot(new byte[ReviewStaticAstronomyCampaignSlotStore.MaximumBytes+1]));
    }
    [Theory] [InlineData(UnityReviewWriteStage.BeforeWrite)] [InlineData(UnityReviewWriteStage.AfterPartialWrite)] [InlineData(UnityReviewWriteStage.BeforeReplace)]
    public void FaultedWriteKeepsPriorRootAndOtherProfileSlot(UnityReviewWriteStage stage)
    {
        Store().Write("one",Root(),false);var path=Path.Combine(directory,"unity-generated-static-astronomy-v2-one.json");var before=File.ReadAllBytes(path);
        var old=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"static-v1/aftermath.json"));
        var v1=new ReviewGeneratedCampaignSlotStore(directory,codec);v1.Write("one",codec.DecodeGeneratedCampaignRoot(old),false);
        var oldPath=Path.Combine(directory,"unity-generated-campaign-v7-one.json");var oldBytes=File.ReadAllBytes(oldPath);
        var failing=Store(s=>{if(s==stage)throw new IOException("injected");});
        Assert.Throws<ReviewCheckpointStoreException>(()=>failing.Write("one",Root(),true));
        Assert.Equal(before,File.ReadAllBytes(path));Assert.Equal(oldBytes,File.ReadAllBytes(oldPath));Assert.Equal(2,Directory.GetFiles(directory).Length);
        Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Write("v1",v1.Read("one"),false));
        Assert.Throws<ReviewCheckpointStoreException>(()=>v1.Write("v2",Root(),false));
    }
    [Fact] public void V1GoldenBytesDirectContinueAndProfileRemainUnchanged()
    {
        var oldBytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"static-v1/aftermath.json"));var root=codec.DecodeGeneratedCampaignRoot(oldBytes);
        Assert.Equal(oldBytes,codec.EncodeGeneratedCampaignRoot(root));var oldStore=new ReviewGeneratedCampaignSlotStore(directory,codec);oldStore.Write("old",root,false);
        var old=ReviewUiModel.ContinueGeneratedCampaign(Template(),codec,oldStore,"old");Assert.False(old.StaticAstronomyCampaign);
        Assert.Throws<InvalidOperationException>(()=>old.GeneratedPresentation());Assert.True(old.Save("copy"));
        Assert.Equal(oldBytes,File.ReadAllBytes(Path.Combine(directory,"unity-generated-campaign-v7-copy.json")));
    }
    [Fact] public void AdvisoryProbePreservesModelDraftsErrorsAndProfileSeparation()
    {
        var m=Model();var controller=m.Current;var frame=controller.Frame;var pair=m.GeneratedPresentation();
        var draft=m.ResearchInputs()!;draft.Adjust(ScienceDiscipline.Biotech,6);
        var message=m.Message;var last=controller.LastCheckpointError;var original=File.ReadAllBytes(Path.Combine(directory,"unity-generated-static-astronomy-v2-initial.json"));
        Assert.True(m.TryCheckWriteAdmission("initial",out var error));Assert.Equal(ReviewCheckpointError.SlotOccupied,error);
        Assert.Same(controller,m.Current);Assert.Same(frame,m.Current.Frame);Assert.Same(draft,m.ResearchDraft);Assert.True(m.HasLocalEdits);
        Assert.Equal(message,m.Message);Assert.Equal(last,controller.LastCheckpointError);Assert.False(m.Busy);Assert.True(m.IsCurrentGeneratedPresentation(pair));
        Assert.Equal(original,File.ReadAllBytes(Path.Combine(directory,"unity-generated-static-astronomy-v2-initial.json")));
        var v1=new ReviewGeneratedCampaignSlotStore(directory,codec);
        Assert.True(v1.TryCheckWriteAdmission("initial",out var v1Empty));Assert.Equal(ReviewCheckpointError.None,v1Empty);
        Assert.True(m.Close(true));var closedMessage=m.Message;
        Assert.True(m.TryCheckWriteAdmission("initial",out var closed));Assert.Equal(ReviewCheckpointError.Busy,closed);Assert.Equal(closedMessage,m.Message);
    }
    [Fact] public void StaticAdmissionHasNoWriteAndKeepsFinalRaceAndSerializationChecks()
    {
        var store=Store();Assert.True(store.TryCheckWriteAdmission("one",out var empty));Assert.Equal(ReviewCheckpointError.None,empty);
        Assert.False(Directory.Exists(directory));Assert.True(store.TryCheckWriteAdmission("../escape",out var invalid));Assert.Equal(ReviewCheckpointError.InvalidSlot,invalid);
        Directory.CreateDirectory(directory);var path=Path.Combine(directory,"unity-generated-static-astronomy-v2-one.json");
        var racing=Encoding.UTF8.GetBytes("unrelated racing file");File.WriteAllBytes(path,racing);
        Assert.True(store.TryCheckWriteAdmission("one",out var occupied));Assert.Equal(ReviewCheckpointError.SlotOccupied,occupied);
        Assert.Equal(ReviewCheckpointError.SerializationFailed,Assert.Throws<ReviewCheckpointStoreException>(()=>store.Write("one",null!,false)).Error);
        Assert.Equal(ReviewCheckpointError.SlotOccupied,Assert.Throws<ReviewCheckpointStoreException>(()=>store.Write("one",Root(),false)).Error);
        Assert.Equal(racing,File.ReadAllBytes(path));Assert.Single(Directory.GetFiles(directory));
    }
    [Fact] public void StaleFrameAndPendingWorkCannotProbeTheStore()
    {
        var store=new CountingAdmissionStore();var host=PhysicalReviewHost.StartStaticAstronomyCampaign(Template(),Settings(),codec,store);
        var controller=new ReviewController(host,"civ.federation");var old=controller.Frame;
        Assert.True(controller.TryCheckWriteAdmission("one",out var occupied));Assert.Equal(ReviewCheckpointError.SlotOccupied,occupied);Assert.Equal(1,store.Probes);
        store.Throw=true;Assert.True(controller.TryCheckWriteAdmission("one",out var fault));Assert.Equal(ReviewCheckpointError.WriteFailed,fault);
        Assert.Equal(ReviewCheckpointError.None,controller.LastCheckpointError);Assert.Equal(2,store.Probes);store.Throw=false;
        var fleet=old.OwnStrategicFleets.First().FleetId.Value;Assert.True(controller.EditTravel(fleet,CampaignTravelIntent.StayEngage));Assert.True(controller.ReviewTravel());
        Assert.NotNull(controller.ConfirmTravel("probe-pending"));Assert.True(controller.TryCheckWriteAdmission("one",out var busy));Assert.Equal(ReviewCheckpointError.Busy,busy);Assert.Equal(2,store.Probes);
        Assert.True(host.AdvancePlanning(old).Accepted);
        Assert.True(host.TryCheckWriteAdmission(old,"one",out var stale));Assert.Equal(ReviewCheckpointError.Busy,stale);Assert.Equal(2,store.Probes);
        var unsupported=PhysicalReviewHost.StartStaticAstronomyCampaign(Template(),Settings(),codec,new UnsupportedAdmissionStore());
        Assert.False(new ReviewController(unsupported,"civ.federation").TryCheckWriteAdmission("one",out _));
    }
    private sealed class CountingAdmissionStore : IStaticAstronomyReviewCheckpointStore,IReviewCheckpointWriteAdmission
    {
        public int Probes;public bool Throw;
        public bool TryCheckWriteAdmission(string slot,out ReviewCheckpointError error){Probes++;if(Throw)throw new IOException();error=ReviewCheckpointError.SlotOccupied;return true;}
        public void Write(string slot,CampaignGeneratedSaveDto root,bool overwrite)=>throw new IOException();
        public CampaignGeneratedSaveDto Read(string slot)=>throw new IOException();
    }
    private sealed class UnsupportedAdmissionStore : IStaticAstronomyReviewCheckpointStore
    {
        public void Write(string slot,CampaignGeneratedSaveDto root,bool overwrite)=>throw new IOException();
        public CampaignGeneratedSaveDto Read(string slot)=>throw new IOException();
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
