using System.Text;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

// Storage/shape carriers only: these deliberately do not claim generated-session
// replay validity. The actual generated factory/load tests follow its API pin.
public sealed class GeneratedCampaignStorageTests : IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"hobby-generated-storage-"+Guid.NewGuid().ToString("N"));
    private readonly ReviewNewtonsoftCodec codec=new();
    private ReviewGeneratedCampaignSlotStore Store(Action<UnityReviewWriteStage>? probe=null)=>new(directory,codec,probe);
    private CampaignGeneratedSaveDto Carrier()
    {
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        var playable=CampaignPlayableFleetApplication.StartPackage(template).ExportPlayableSave();
        playable.Integrated!.Physical!.Core!.ProfileId=CampaignGeneratedIdentity.EmbeddedCoreProfile;
        var map=codec.DecodeNaturalGalaxy(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"galaxy-samples/natural/seed-42-systems-250.natural.json")));
        return new CampaignGeneratedSaveDto{FormatVersion=CampaignGeneratedIdentity.Format,ProfileId=CampaignGeneratedIdentity.Profile,
            PolicyId=CampaignGeneratedIdentity.Policy,TemplatePackageHash=template.PackageHash!,PackageHash=new string('a',64),
            Map=map,Campaign=playable,Discoveries=new()};
    }
    [Fact] public void CompleteCarrierIsDetachedAndStoredInDisjointFormatSevenNamespace()
    {
        var root=Carrier();var encoded=codec.EncodeGeneratedCampaignRoot(root);Store().Write("campaign-1",root,false);
        var path=Assert.Single(Directory.GetFiles(directory));Assert.EndsWith("unity-generated-campaign-v7-campaign-1.json",path);
        Assert.Equal(encoded,File.ReadAllBytes(path));var decoded=Store().Read("campaign-1");
        Assert.Equal(encoded,codec.EncodeGeneratedCampaignRoot(decoded));decoded.Map.Layout.Regions.Clear();
        Assert.NotEmpty(Store().Read("campaign-1").Map.Layout.Regions);
        Assert.ThrowsAny<Exception>(()=>codec.DecodePlayableFleetRoot(encoded));
        Assert.ThrowsAny<Exception>(()=>codec.EncodePlayableFleetRoot(root.Campaign)); // Stripping outer7 cannot admit generated owned-core as6.
        Assert.ThrowsAny<Exception>(()=>codec.DecodeDemoRoot(encoded));
        Assert.ThrowsAny<Exception>(()=>codec.DecodeFrontierRoot(encoded));
    }
    [Theory] [InlineData(UnityReviewWriteStage.BeforeWrite)] [InlineData(UnityReviewWriteStage.AfterPartialWrite)] [InlineData(UnityReviewWriteStage.BeforeReplace)]
    public void InterruptedWriteKeepsPriorWholeRootAndRemovesOnlyTemporaryFile(UnityReviewWriteStage stage)
    {
        var root=Carrier();Store().Write("one",root,false);var path=Assert.Single(Directory.GetFiles(directory));var original=File.ReadAllBytes(path);
        root.PackageHash=new string('b',64);
        var failing=Store(s=>{if(s==stage)throw new IOException("injected");});
        Assert.Throws<ReviewCheckpointStoreException>(()=>failing.Write("one",root,true));
        Assert.Equal(original,File.ReadAllBytes(path));Assert.Single(Directory.GetFiles(directory));
    }
    [Fact] public void WrongIdentityTruncationDuplicateAndUnknownMembersAreRejectedWithoutChangingSavedBytes()
    {
        var root=Carrier();Store().Write("one",root,false);var path=Assert.Single(Directory.GetFiles(directory));var saved=File.ReadAllBytes(path);
        var json=Encoding.UTF8.GetString(saved);
        foreach(var text in new[]{json[..^1],json.Replace("\"FormatVersion\":7","\"FormatVersion\":7,\"FormatVersion\":7"),
            json.Insert(1,"\"Unknown\":true,"),json.Replace(CampaignGeneratedIdentity.Policy,"wrong-policy")})
            Assert.ThrowsAny<Exception>(()=>codec.DecodeGeneratedCampaignRoot(Encoding.UTF8.GetBytes(text)));
        root.Campaign.Integrated!.Physical!.Core!.ProfileId=CampaignPlayableFleetIdentity.EmbeddedCoreProfile;
        Assert.ThrowsAny<Exception>(()=>codec.EncodeGeneratedCampaignRoot(root));
        Assert.Equal(saved,File.ReadAllBytes(path));
    }
    [Fact] public void SlotTraversalOccupiedAndOversizeReadsFailBeforePublication()
    {
        var root=Carrier();Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Write("../escape",root,false));
        Store().Write("one",root,false);var path=Assert.Single(Directory.GetFiles(directory));var saved=File.ReadAllBytes(path);
        Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Write("one",root,false));Assert.Equal(saved,File.ReadAllBytes(path));
        var huge=Path.Combine(directory,"unity-generated-campaign-v7-huge.json");using(var f=new FileStream(huge,FileMode.CreateNew)){f.SetLength(ReviewGeneratedCampaignSlotStore.MaximumBytes+1L);}
        Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Read("huge"));Assert.Equal(saved,File.ReadAllBytes(path));
    }
    [Fact] public void AdvisoryAdmissionDoesNotCreateOrEncodeAndOriginalWriteErrorOrderingRemains()
    {
        var store=Store();Assert.True(store.TryCheckWriteAdmission("one",out var empty));Assert.Equal(ReviewCheckpointError.None,empty);
        Assert.False(Directory.Exists(directory));
        Assert.True(store.TryCheckWriteAdmission("../escape",out var invalid));Assert.Equal(ReviewCheckpointError.InvalidSlot,invalid);
        Assert.False(Directory.Exists(directory));
        Directory.CreateDirectory(directory);var path=Path.Combine(directory,"unity-generated-campaign-v7-one.json");
        File.WriteAllText(path,"existing bytes need not be decoded to ask for replacement");var before=File.ReadAllBytes(path);
        Assert.True(store.TryCheckWriteAdmission("one",out var occupied));Assert.Equal(ReviewCheckpointError.SlotOccupied,occupied);
        Assert.Equal(ReviewCheckpointError.SerializationFailed,Assert.Throws<ReviewCheckpointStoreException>(()=>store.Write("one",null!,false)).Error);
        Assert.Equal(before,File.ReadAllBytes(path));Assert.Single(Directory.GetFiles(directory));
    }
    [Fact] public void EmptyAdmissionIsNotPermissionToReplaceARacingFile()
    {
        var store=Store();Assert.True(store.TryCheckWriteAdmission("race",out var error));Assert.Equal(ReviewCheckpointError.None,error);
        Directory.CreateDirectory(directory);var path=Path.Combine(directory,"unity-generated-campaign-v7-race.json");
        var racing=Encoding.UTF8.GetBytes("created after advisory check");File.WriteAllBytes(path,racing);
        Assert.Equal(ReviewCheckpointError.SlotOccupied,Assert.Throws<ReviewCheckpointStoreException>(()=>store.Write("race",Carrier(),false)).Error);
        Assert.Equal(racing,File.ReadAllBytes(path));Assert.Single(Directory.GetFiles(directory));
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
