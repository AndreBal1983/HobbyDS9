using System.Text;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class ActivitiesCarrierStoreTests : IDisposable
{
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Activities-Carrier-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    static readonly Lazy<CampaignActivitiesPackageDto> package=new(CreatePackage);
    static CampaignActivitiesPackageDto CreatePackage()
    {
        var c=new ReviewNewtonsoftCodec();
        byte[] Read(string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/activities-v1",name));
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        template.ReplayOperationLimit=65536;
        return CampaignActivitiesApplication.CreatePackage(template,c.DecodeActivityCatalogue(Read("catalogue.json")),
            c.DecodeActivityReadyClasses(Read("ready-classes.json")),c.DecodeActivitySupport(Read("support.json")),c.DecodeActivityContent(Read("activities.json")),2048);
    }
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,
        HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    ICampaignActivitiesSession Session()=>CampaignActivitiesApplication.StartGame(package.Value,Settings());
    ReviewActivitiesSlotStore Store(Action<UnityReviewWriteStage>? probe=null)=>new(directory,codec,probe);
    string FileFor(string slot)=>Path.Combine(directory,"unity-campaign-activities-v1-"+slot+".json");
    static string Json(object value)=>JsonConvert.SerializeObject(value);
    CampaignActivitiesSaveDto Root()=>Session().ExportActivitiesSave();
    static byte[] Bytes(JObject value)=>Encoding.UTF8.GetBytes(value.ToString(Formatting.None));

    [Fact] public void ActualPaidQueueCopiesWithoutAliasesAndReplaysThroughApplication()
    {
        var session=Session();var stamp=session.CaptureStatus();
        Assert.True(session.SubmitActivity(new(){ActorId="civ.federation",CommandId="command.codec.structure",ExpectedTurn=stamp.Turn,
            ExpectedRevision=stamp.CampaignRevision,Kind=CampaignActivityCommandKind.EnqueueStructure,TargetId="colony.sol",DefinitionId="infrastructure.fabrication-annex.v1"}).Accepted);
        var root=session.ExportActivitiesSave();var copy=codec.CopySave(root);
        Assert.Equal(Json(root),Json(copy));Assert.NotSame(root.Activities,copy.Activities);
        Assert.NotSame(root.Activities!.Production![0].Paid,copy.Activities!.Production![0].Paid);
        Assert.Equal(Json(root),Json(CampaignActivitiesApplication.LoadSave(package.Value,copy).ExportActivitiesSave()));
        copy.Activities.Production[0].Paid!.CompletedIndustry++;
        // A syntactically valid carrier remains insufficient authority to load.
        var forged=codec.CopySave(copy);
        Assert.Throws<ArgumentException>(()=>CampaignActivitiesApplication.LoadSave(package.Value,forged));
        Assert.Equal(Json(root),Json(session.ExportActivitiesSave()));
    }
    [Theory]
    [InlineData("marker")][InlineData("policy")][InlineData("missing-state-list")][InlineData("null-state-list")]
    [InlineData("null-row")][InlineData("unknown")][InlineData("integer-string")][InlineData("integer-float")]
    [InlineData("null-site-identity")][InlineData("missing-site-identity")][InlineData("journal-xor")]
    public void ClosedCarrierRejectsMalformedNewShape(string change)
    {
        var wire=JObject.Parse(Json(Root()));
        switch(change)
        {
            case "marker":wire["FormatVersion"]=8;break;
            case "policy":wire["PolicyId"]="generated-activities-mixed-production-v2";break;
            case "missing-state-list":((JObject)wire["Activities"]!).Remove("Findings");break;
            case "null-state-list":wire["Activities"]!["Findings"]=JValue.CreateNull();break;
            case "null-row":((JArray)wire["Activities"]!["Findings"]!).Add(JValue.CreateNull());break;
            case "unknown":wire["Unexpected"]=true;break;
            case "integer-string":wire["FormatVersion"]="9";break;
            case "integer-float":wire["FormatVersion"]=9.0;break;
            case "null-site-identity":wire["Activities"]!["Sites"]![0]!["SiteId"]=JValue.CreateNull();break;
            case "missing-site-identity":((JObject)wire["Activities"]!["Sites"]![0]!).Remove("SiteId");break;
            case "journal-xor":((JArray)wire["Journal"]!).Add(new JObject{{"Common",JValue.CreateNull()},{"Activity",JValue.CreateNull()}});break;
        }
        Assert.Throws<InvalidDataException>(()=>codec.DecodeActivitiesRoot(Bytes(wire)));
    }
    [Fact] public void DuplicatePropertiesAndHistoricalRootsHaveNoFallback()
    {
        var text=Json(Root());
        Assert.Throws<InvalidDataException>(()=>codec.DecodeActivitiesRoot(Encoding.UTF8.GetBytes(text.Replace("\"FormatVersion\":9,","\"FormatVersion\":9,\"FormatVersion\":9,"))));
        Assert.Throws<InvalidDataException>(()=>codec.DecodeActivitiesRoot(Encoding.UTF8.GetBytes("{}")));
        Assert.Throws<InvalidDataException>(()=>codec.DecodeActivitiesRoot(Array.Empty<byte>()));
    }
    [Fact] public void SeparateNamespaceOverwriteAndConfinedSlotsPreserveExactBytes()
    {
        Assert.Equal("HobbyDS9CampaignActivitiesOnly-v1",ReviewActivitiesSlotStore.Namespace);
        var root=Root();Store().Write("one",root,false);var bytes=File.ReadAllBytes(FileFor("one"));
        Assert.Equal(bytes,codec.EncodeActivitiesRoot(Store().Read("one")));
        Assert.True(Store().TryCheckWriteAdmission("one",out var error));Assert.Equal(ReviewCheckpointError.SlotOccupied,error);
        Assert.Equal(ReviewCheckpointError.SlotOccupied,Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Write("one",root,false)).Error);
        Store().Write("one",root,true);Assert.Equal(bytes,File.ReadAllBytes(FileFor("one")));
        foreach(var slot in new[]{"../escape","Upper","/absolute","_first","a.b",new string('a',33)})
            Assert.Equal(ReviewCheckpointError.InvalidSlot,Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Read(slot)).Error);
        Assert.Equal(ReviewCheckpointError.NotFound,Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Read("missing")).Error);
        Assert.Single(Directory.GetFiles(directory));
    }
    [Theory][InlineData(UnityReviewWriteStage.AfterPartialWrite)][InlineData(UnityReviewWriteStage.BeforeReplace)]
    public void InterruptedReplacementPreservesPriorCheckpointAndCleansOwnedTemporary(UnityReviewWriteStage stage)
    {
        var root=Root();Store().Write("one",root,false);var bytes=File.ReadAllBytes(FileFor("one"));
        var broken=Store(current=>{if(current==stage)throw new IOException("test-owned interruption");});
        Assert.Equal(ReviewCheckpointError.WriteFailed,Assert.Throws<ReviewCheckpointStoreException>(()=>broken.Write("one",root,true)).Error);
        Assert.Equal(bytes,File.ReadAllBytes(FileFor("one")));Assert.Empty(Directory.GetFiles(directory,"*.tmp"));
    }
    [Fact] public void FinalNoOverwriteRacePreservesWinnerAndCorruptReadIsBounded()
    {
        var root=Root();var racing=Store(stage=>{if(stage==UnityReviewWriteStage.BeforeReplace)Store().Write("race",root,false);});
        Assert.Equal(ReviewCheckpointError.SlotOccupied,Assert.Throws<ReviewCheckpointStoreException>(()=>racing.Write("race",root,false)).Error);
        Assert.Equal(codec.EncodeActivitiesRoot(root),File.ReadAllBytes(FileFor("race")));Assert.Empty(Directory.GetFiles(directory,"*.tmp"));
        File.WriteAllText(FileFor("bad"),"{}");
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Read("bad")).Error);
        using(var file=new FileStream(FileFor("large"),FileMode.CreateNew))file.SetLength(ReviewActivitiesSlotStore.MaximumBytes+1L);
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Read("large")).Error);
    }
    public void Dispose()
    {
        var full=Path.GetFullPath(directory);var parent=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(!full.StartsWith(parent,StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(full).StartsWith("HobbyDS9-Activities-Carrier-",StringComparison.Ordinal))throw new InvalidOperationException();
        if(Directory.Exists(full))Directory.Delete(full,true);
    }
}
