using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class StaticAstronomyOwnedExportTests
{
    const string Human="civ.federation";
    static readonly ReviewNewtonsoftCodec Real=new();
    static CampaignPlayableFleetPackageDto Template()=>PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
    static readonly Lazy<byte[]> Initial=new(()=>Real.EncodeStaticAstronomyCampaignRoot(CampaignStaticAstronomyApplication.StartGame(Template(),new(){Map=new(){Seed="42",SystemCount=250,EmptyRegionCount=250,Width=2500000,Height=2000000,QuietEmptyAreaTargetPermille=2000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}}).ExportGeneratedSave()));
    static CampaignGeneratedSaveDto Root()=>Real.DecodeStaticAstronomyCampaignRoot(Initial.Value);
    static string Json<T>(T value)=>JsonSerializer.Serialize(value);
    sealed class Store : IStaticAstronomyReviewCheckpointStore, IGeneratedCampaignReviewCheckpointStore
    {
        internal CampaignGeneratedSaveDto Input=Root();
        internal CampaignGeneratedSaveDto? Captured;
        internal int Writes;
        public void Write(string slot,CampaignGeneratedSaveDto root,bool overwrite){Writes++;Captured=root;}
        public CampaignGeneratedSaveDto Read(string slot)=>Input;
    }
    // Deliberately lacks the optional capability. Nth-copy fault semantics must remain.
    class CopyCodec : IStaticAstronomyReviewHostCodec
    {
        internal int Copies,FailAt;
        internal Action<CampaignGeneratedSaveDto>? BeforeCopy;
        internal Action<CampaignGeneratedSaveDto>? Alter;
        public CampaignGeneratedSaveDto CopyStaticAstronomySave(CampaignGeneratedSaveDto value)
        {Copies++;if(Copies==FailAt)throw new IOException("copy fault");BeforeCopy?.Invoke(value);var root=Real.CopyStaticAstronomySave(value);Alter?.Invoke(root);return root;}
        public CampaignGeneratedMapDto CopyKnownGeneratedMap(CampaignGeneratedMapDto value)=>Real.CopyKnownGeneratedMap(value);
        public CampaignGeneratedSaveDto CopySave(CampaignGeneratedSaveDto value){Copies++;if(Copies==FailAt)throw new IOException("v1 copy fault");return Real.CopySave(value);}
        public CampaignPlayableFleetPackageDto CopyPackage(CampaignPlayableFleetPackageDto value)=>Real.CopyPackage(value);
        public CampaignPlayableFleetSaveDto CopySave(CampaignPlayableFleetSaveDto value)=>Real.CopySave(value);
        public CampaignIntegratedPlaytestPackageDto CopyPackage(CampaignIntegratedPlaytestPackageDto value)=>Real.CopyPackage(value);
        public CampaignIntegratedPlaytestSaveDto CopySave(CampaignIntegratedPlaytestSaveDto value)=>Real.CopySave(value);
        public CampaignTacticalTravelPackageDto CopyPackage(CampaignTacticalTravelPackageDto value)=>Real.CopyPackage(value);
        public CampaignTacticalTravelSaveDto CopySave(CampaignTacticalTravelSaveDto value)=>Real.CopySave(value);
    }
    sealed class OwnedCodec : CopyCodec,IStaticAstronomyOwnedExportValidator
    {
        internal int Validations;
        internal bool FailValidation;
        public void ValidateOwnedStaticAstronomyExport(CampaignPlayableFleetPackageDto template,CampaignGeneratedSaveDto ownedRoot)
        {Validations++;if(FailValidation)throw new IOException("validator fault");Real.ValidateOwnedStaticAstronomyExport(template,ownedRoot);}
    }
    static PhysicalReviewHost Open(CopyCodec codec,Store store)
    {
        var opened=PhysicalReviewHost.OpenStaticAstronomyCampaignFromSlot(Template(),codec,store,"input",Human);
        Assert.True(opened.Succeeded);codec.Copies=0;return opened.Host!;
    }
    [Fact] public void OptionalCapabilityAppliesOnlyToV2SlotSaveAndKeepsAllIngressAndHandlesOnOldPath()
    {
        var codec=new OwnedCodec();var store=new Store();var host=Open(codec,store);
        Assert.True(host.SaveCommittedToSlot("one").Succeeded);Assert.Equal(1,codec.Copies);Assert.Equal(1,codec.Validations);
        codec.Copies=0;var handle=host.SaveCommitted();Assert.Equal(2,codec.Copies);Assert.Equal(1,codec.Validations);
        codec.Copies=0;host.LoadCommitted(handle,Human,null);Assert.Equal(1,codec.Copies);Assert.Equal(1,codec.Validations);
        codec.Copies=0;Assert.True(host.LoadCommittedFromSlot("input",Human).Succeeded);Assert.Equal(1,codec.Copies);Assert.Equal(1,codec.Validations);
        codec.Copies=0;Assert.True(PhysicalReviewHost.OpenStaticAstronomyCampaignFromSlot(Template(),codec,store,"input",Human).Succeeded);
        Assert.Equal(1,codec.Copies);Assert.Equal(1,codec.Validations);
        var controller=new ReviewController(host,Human);codec.Copies=0;Assert.True(controller.ReopenFromSlot("input").Succeeded);
        Assert.Equal(1,codec.Copies);Assert.Equal(1,codec.Validations);
    }
    [Theory][InlineData(1)][InlineData(2)]
    public void UnsupportedCodecRetainsNthCopySlotSaveFailuresAndAtomicity(int nth)
    {
        var codec=new CopyCodec();var store=new Store();var host=Open(codec,store);codec.FailAt=nth;
        Assert.Equal(ReviewCheckpointError.SerializationFailed,host.SaveCommittedToSlot("bad").Error);
        Assert.Equal(nth,codec.Copies);Assert.Equal(0,store.Writes);
        codec.FailAt=0;codec.Copies=0;Assert.True(host.SaveCommittedToSlot("good").Succeeded);Assert.Equal(2,codec.Copies);
    }
    [Fact] public void CapabilityCannotChangeInMemoryNthCopyFailureOrV1Chain()
    {
        var codec=new OwnedCodec();var store=new Store();var host=Open(codec,store);codec.FailAt=2;
        Assert.Throws<IOException>(()=>host.SaveCommitted());Assert.Equal(2,codec.Copies);Assert.Equal(0,codec.Validations);
        codec.FailAt=0;codec.Copies=0;
        store.Input=Real.DecodeGeneratedCampaignRoot(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"static-v1/aftermath.json")));
        var old=PhysicalReviewHost.OpenGeneratedCampaignFromSlot(Template(),codec,store,"old",Human);Assert.True(old.Succeeded);
        codec.Copies=0;codec.FailAt=2;Assert.Equal(ReviewCheckpointError.SerializationFailed,old.Host!.SaveCommittedToSlot("bad").Error);
        Assert.Equal(2,codec.Copies);Assert.Equal(0,codec.Validations);Assert.Equal(0,store.Writes);
        codec.Copies=0;codec.FailAt=0;Assert.True(old.Host.SaveCommittedToSlot("good").Succeeded);Assert.Equal(2,codec.Copies);Assert.Equal(0,codec.Validations);
        Assert.Equal(Real.EncodeGeneratedCampaignRoot(store.Input),Real.EncodeGeneratedCampaignRoot(store.Captured!));
    }
    [Theory][InlineData("first-copy")][InlineData("validator")][InlineData("profile")][InlineData("package-hash")][InlineData("map")][InlineData("transcript")]
    public void FailedOwnedSelfValidationNeverWritesOrChangesFrameDraftEpochOrHandleSequence(string failure)
    {
        var codec=new OwnedCodec();var store=new Store();var host=Open(codec,store);var c=new ReviewController(host,Human);
        var handle=host.SaveCommitted();var pair=host.CaptureGeneratedPresentation(c.Frame);var before=c.Frame;
        Assert.True(c.EditTravel(before.OwnStrategicFleets.First().FleetId.Value,CampaignTravelIntent.StayEngage));var draft=c.TravelDraft;
        codec.Copies=0;
        if(failure=="first-copy")codec.FailAt=1;
        else if(failure=="validator")codec.FailValidation=true;
        else codec.Alter=root=>{switch(failure){case "profile":root.ProfileId=CampaignGeneratedIdentity.Profile;break;
            case "package-hash":root.PackageHash=new string('a',64);break;case "map":root.Map.Settings.Map.Seed="43";break;
            case "transcript":root.Campaign.Transcript.Add(new(){Common=new(){Kind=FrontierOperationKind.Advance,Turn=900,Revision=0}});break;}};
        Assert.Equal(ReviewCheckpointError.SerializationFailed,c.SaveCommittedToSlot("bad").Error);
        Assert.Equal(0,store.Writes);Assert.Same(before,c.Frame);Assert.Same(draft,c.TravelDraft);Assert.True(c.Dirty);
        host.ValidateGeneratedPresentation(before,pair);
        codec.FailAt=0;codec.FailValidation=false;codec.Alter=null;codec.Copies=0;
        var nextHandle=host.SaveCommitted();
        var id=typeof(ReviewSaveHandle).GetProperty("Id",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        Assert.Equal((long)id.GetValue(handle)!+1,(long)id.GetValue(nextHandle)!);
        var restored=host.LoadCommitted(handle,Human,null);Assert.Equal(Json(pair.Astronomy),Json(host.CaptureGeneratedPresentation(restored).Astronomy));
    }
    [Theory][InlineData("carrier")][InlineData("size")]
    public void FirstRealClosedCopyStillRejectsCarrierAndOversizedSnapshots(string failure)
    {
        var codec=new OwnedCodec();var store=new Store();var host=Open(codec,store);var frame=host.Read(Human);
        codec.BeforeCopy=root=>{if(failure=="carrier")root.PolicyId="wrong";
            else root.Map.Features[0].DisplayName=new string('x',ReviewStaticAstronomyCampaignSlotStore.MaximumBytes);};
        Assert.Equal(ReviewCheckpointError.SerializationFailed,host.SaveCommittedToSlot("bad").Error);
        Assert.Equal(1,codec.Copies);Assert.Equal(0,codec.Validations);Assert.Equal(0,store.Writes);
        Assert.Equal(frame.Generation,host.Read(Human).Generation);
    }
    [Fact] public void MutatingCapturedExportIngressAndOriginalTemplateCannotChangeAuthorityOrOtherHandles()
    {
        var store=new Store();var template=Template();var templateBefore=Json(template);
        var opened=PhysicalReviewHost.OpenStaticAstronomyCampaignFromSlot(template,Real,store,"input",Human);Assert.True(opened.Succeeded);var host=opened.Host!;
        var first=host.SaveCommitted();var second=host.SaveCommitted();var pair=host.CaptureGeneratedPresentation(opened.Frame!);
        Assert.True(host.SaveCommittedToSlot("saved").Succeeded);var canonical=Real.EncodeStaticAstronomyCampaignRoot(store.Captured!);
        Real.ValidateOwnedStaticAstronomyExport(template,store.Captured!);Assert.Equal(templateBefore,Json(template));
        store.Captured!.Map.Features.Clear();store.Captured.Campaign.Transcript.Clear();store.Captured.Campaign.Names.Clear();
        store.Captured.Campaign.Integrated.Physical!.Core.Campaign.GameState.Ships!.Clear();store.Input.Map.Layout.Regions.Clear();
        template.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.DefinitionIds.Clear();
        host.ValidateGeneratedPresentation(opened.Frame!,pair);
        var a=host.LoadCommitted(first,Human,null);Assert.Equal(Json(pair.Astronomy),Json(host.CaptureGeneratedPresentation(a).Astronomy));
        Assert.True(host.SaveCommittedToSlot("after-first").Succeeded);Assert.Equal(canonical,Real.EncodeStaticAstronomyCampaignRoot(store.Captured!));
        host.LoadCommitted(second,Human,null);Assert.True(host.SaveCommittedToSlot("after-second").Succeeded);Assert.Equal(canonical,Real.EncodeStaticAstronomyCampaignRoot(store.Captured!));
    }
}
