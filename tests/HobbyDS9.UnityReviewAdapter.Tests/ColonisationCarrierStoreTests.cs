using System.Text;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class ColonisationCarrierStoreTests : IDisposable
{
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Colonisation-Carrier-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    static readonly Lazy<CampaignColonisationPackageDto> package=new(CreatePackage);
    static CampaignColonisationPackageDto CreatePackage()
    {
        var c=new ReviewNewtonsoftCodec();
        byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name));
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        template.ReplayOperationLimit=65536;
        var activities=CampaignActivitiesApplication.CreatePackage(template,c.DecodeActivityCatalogue(Read("activities-v1","catalogue.json")),
            c.DecodeActivityReadyClasses(Read("activities-v1","ready-classes.json")),c.DecodeActivitySupport(Read("activities-v1","support.json")),c.DecodeActivityContent(Read("activities-v1","activities.json")),2048);
        return CampaignTacticalReviewSession.CreateColonisationPackage(activities,c.DecodeColonisationCatalogue(Read("colonisation-v1","catalogue.json")),
            c.DecodeColonisationReadyClasses(Read("colonisation-v1","ready-classes.json")),c.DecodeColonisationSupport(Read("colonisation-v1","support.json")),c.DecodeColonisationPolicy(Read("colonisation-v1","settlement.json")));
    }
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,
        HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    ICampaignColonisationSession Session()=>CampaignColonisationApplication.StartGame(package.Value,Settings());
    ReviewColonisationSlotStore Store(Action<UnityReviewWriteStage>? probe=null)=>new(directory,codec,probe);
    string FileFor(string slot)=>Path.Combine(directory,"unity-campaign-colonisation-v1-"+slot+".json");
    static string Json(object value)=>JsonConvert.SerializeObject(value);
    CampaignColonisationSaveDto Root()=>Session().ExportColonisationSave();
    static byte[] Bytes(JObject value)=>Encoding.UTF8.GetBytes(value.ToString(Formatting.None));
    static void QueueKit(ICampaignColonisationSession session)
    {
        var status=session.CaptureStatus();
        Assert.True(session.SubmitColonisation(new(){ActorId="civ.federation",CommandId="command.carrier.kit",Turn=status.Turn,Revision=status.CampaignRevision,
            Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}}).Accepted);
    }
    [Fact]public void ActualTypedKitAndAdvanceRoundTripAllFactsAndRequireAuthoritativeReplay()
    {
        var session=Session();QueueKit(session);var stamp=session.CaptureStatus();
        Assert.True(session.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);
        var root=session.ExportColonisationSave();var copy=codec.CopySave(root);
        Assert.Equal(Json(root),Json(copy));Assert.NotSame(root.FrozenPlanets,copy.FrozenPlanets);Assert.NotSame(root.Colonisation,copy.Colonisation);
        Assert.Equal(Json(root),Json(CampaignColonisationApplication.LoadSave(package.Value,copy).ExportColonisationSave()));
        Assert.NotEmpty(copy.Production!);copy.Production![0].CompletedIndustry++;
        var forged=codec.CopySave(copy); // Well-formed bytes do not establish simulation truth.
        Assert.Throws<ArgumentException>(()=>CampaignColonisationApplication.LoadSave(package.Value,forged));
        Assert.Equal(Json(root),Json(session.ExportColonisationSave()));
    }
    [Theory]
    [InlineData("marker")][InlineData("policy")][InlineData("core-marker")][InlineData("missing-frozen-list")]
    [InlineData("null-frozen-list")][InlineData("missing-manifests")][InlineData("null-manifests")][InlineData("null-row")]
    [InlineData("unknown")][InlineData("integer-string")][InlineData("integer-float")][InlineData("missing-deep-reference")]
    [InlineData("journal-xor")][InlineData("action-mismatch")][InlineData("two-payloads")][InlineData("nested-xor")]
    [InlineData("missing-consumption")][InlineData("duplicate-order")][InlineData("wrong-kind")][InlineData("prepaid-field")]
    public void ClosedElevenCarrierRejectsOmissionsNullHistoriesAndWrongUnions(string change)
    {
        var session=Session();QueueKit(session);var wire=JObject.Parse(Json(session.ExportColonisationSave()));
        var order=(JObject)wire["Production"]![0]!;var command=wire["Journal"]![0]!["Colonisation"]!;
        switch(change)
        {
            case "marker":wire["FormatVersion"]=10;break;
            case "policy":wire["PolicyId"]=CampaignStreamedActivitiesIdentity.Policy;break;
            case "core-marker":wire["Campaign"]!["Integrated"]!["Physical"]!["Core"]!["ProfileId"]=CampaignStreamedActivitiesIdentity.EmbeddedCoreProfile;break;
            case "missing-frozen-list":((JObject)wire["FrozenPlanets"]!).Remove("GenesisControls");break;
            case "null-frozen-list":wire["FrozenPlanets"]!["GenesisControls"]=JValue.CreateNull();break;
            case "missing-manifests":((JObject)wire["Colonisation"]!).Remove("ManifestRecords");break;
            case "null-manifests":wire["Colonisation"]!["ManifestRecords"]=JValue.CreateNull();break;
            case "null-row":((JArray)wire["Colonisation"]!["Bodies"]!).Add(JValue.CreateNull());break;
            case "unknown":wire["Unexpected"]=true;break;
            case "integer-string":wire["FormatVersion"]="11";break;
            case "integer-float":wire["FormatVersion"]=11.0;break;
            case "missing-deep-reference":((JObject)wire["FrozenPlanets"]!["Bodies"]![0]!).Remove("MinorActorId");break;
            case "journal-xor":wire["Journal"]![0]!["Colonisation"]=JValue.CreateNull();break;
            case "action-mismatch":command["Action"]=(int)ColonisationAction.EmbarkSettlers;break;
            case "two-payloads":command["CancelSurvey"]=JObject.FromObject(new CancelPlanetSurveyDto{SurveyId="survey.forged"});break;
            case "nested-xor":wire["Journal"]![0]!["Colonisation"]=JValue.CreateNull();wire["Journal"]![0]!["Existing"]=JObject.FromObject(new CampaignActivityOperationDto());break;
            case "missing-consumption":order.Remove("ConsumedResources");break;
            case "duplicate-order":((JArray)wire["Production"]!).Add(order.DeepClone());break;
            case "wrong-kind":order["Kind"]=99;break;
            case "prepaid-field":order["PaidCredits"]=0;break;
        }
        Assert.Throws<InvalidDataException>(()=>codec.DecodeColonisationRoot(Bytes(wire)));
    }
    [Fact]public void DuplicatePropertiesAndOldTenCodecCannotCrossNamespaces()
    {
        var bytes=codec.EncodeColonisationRoot(Root());var text=Encoding.UTF8.GetString(bytes);
        Assert.Throws<InvalidDataException>(()=>codec.DecodeColonisationRoot(Encoding.UTF8.GetBytes(text.Replace("\"FormatVersion\":11,","\"FormatVersion\":11,\"FormatVersion\":11,"))));
        Assert.Throws<InvalidDataException>(()=>codec.DecodeStreamedActivitiesRoot(bytes));
        var old=CampaignStreamedActivitiesApplication.StartGame(CampaignStreamedActivitiesApplication.CreatePackage(package.Value.Activities!),Settings()).ExportStreamedActivitiesSave();
        Assert.Throws<InvalidDataException>(()=>codec.DecodeColonisationRoot(codec.EncodeStreamedActivitiesRoot(old)));
        Assert.Throws<InvalidDataException>(()=>codec.DecodeColonisationRoot(Array.Empty<byte>()));
        Assert.Throws<InvalidDataException>(()=>codec.DecodeColonisationRoot(Encoding.UTF8.GetBytes("{}")));
    }
    [Fact]public void RealManufacturedTransportRetainsLoadedCohortAndTerminalLineageThroughCodecAndReplay()
    {
        var settings=Settings();settings.Map!.SystemCount=250;settings.Map.EmptyRegionCount=250;settings.Map.Width=2500000;settings.Map.Height=2000000;
        var session=CampaignColonisationApplication.StartGame(package.Value,settings);QueueKit(session);var status=session.CaptureStatus();
        Assert.True(session.SubmitActivity(new(){ActorId="civ.federation",CommandId="command.carrier.transport",ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,
            Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId="blueprint.ready.galaxy-colony-transport",BlueprintRevision=1}).Accepted);
        for(int i=0;i<48&&session.CaptureColonisation("civ.federation").Transports.Count==0;i++)
        {status=session.CaptureStatus();Assert.True(session.AdvanceTurn(status.Turn,status.CampaignRevision).Accepted);}
        var view=session.CaptureColonisation("civ.federation");var ship=Assert.Single(view.Transports);var home=Assert.Single(view.Worlds,w=>w.Id=="colony.sol");
        status=session.CaptureStatus();
        Assert.True(session.SubmitColonisation(new(){ActorId="civ.federation",CommandId="command.carrier.embark",Turn=status.Turn,Revision=status.CampaignRevision,
            Action=ColonisationAction.EmbarkSettlers,Embark=new(){ShipId=ship.ShipId,ColonyId=home.Id,CohortId=home.Cohorts.First(c=>c.Population>=400).Id,Population=100,Food=20,
                Materials=new(){new(){ResourceDefinitionId="resource.dilithium",Amount=2}}}}).Accepted);
        var loaded=session.ExportColonisationSave();var manifest=Assert.Single(loaded.Colonisation!.ManifestRecords!);
        Assert.Equal(100,manifest.Passengers!.Population);Assert.Equal(Json(loaded),Json(codec.CopySave(loaded)));
        Assert.Equal(Json(loaded),Json(CampaignColonisationApplication.LoadSave(package.Value,codec.CopySave(loaded)).ExportColonisationSave()));
        foreach(var field in new[]{"Cargo","MaterialsDelivered","MaterialsLost"})
        {
            var malformed=JObject.Parse(Json(loaded));malformed["Colonisation"]!["ManifestRecords"]![0]![field]=JValue.CreateNull();
            Assert.Throws<InvalidDataException>(()=>codec.DecodeColonisationRoot(Bytes(malformed)));
        }
        status=session.CaptureStatus();Assert.True(session.SubmitColonisation(new(){ActorId="civ.federation",CommandId="command.carrier.unload",Turn=status.Turn,Revision=status.CampaignRevision,
            Action=ColonisationAction.UnloadTransport,Unload=new(){ManifestId=manifest.ManifestId,ColonyId=home.Id}}).Accepted);
        var terminal=session.ExportColonisationSave();Assert.Single(terminal.Colonisation!.ManifestOrigins!);Assert.Single(terminal.Colonisation.ManifestResolutions!);
        Store().Write("lineage",terminal,false);var returned=Store().Read("lineage");
        Assert.Equal(Json(terminal),Json(CampaignColonisationApplication.LoadSave(package.Value,returned).ExportColonisationSave()));
    }
    [Fact]public void SeparateNamespaceConfinedSlotsAndExplicitOverwriteRetainExactBytes()
    {
        Assert.Equal("HobbyDS9CampaignColonisationOnly-v1",ReviewColonisationSlotStore.Namespace);
        var root=Root();Store().Write("one",root,false);var bytes=File.ReadAllBytes(FileFor("one"));
        Assert.Equal(bytes,codec.EncodeColonisationRoot(Store().Read("one")));
        Assert.True(Store().TryCheckWriteAdmission("one",out var error));Assert.Equal(ReviewCheckpointError.SlotOccupied,error);
        Assert.Equal(ReviewCheckpointError.SlotOccupied,Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Write("one",root,false)).Error);
        Store().Write("one",root,true);Assert.Equal(bytes,File.ReadAllBytes(FileFor("one")));
        foreach(var slot in new[]{"../escape","Upper","/absolute","_first","a.b",new string('a',33)})
            Assert.Equal(ReviewCheckpointError.InvalidSlot,Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Read(slot)).Error);
        Assert.Equal(ReviewCheckpointError.NotFound,Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Read("missing")).Error);
        Assert.Single(Directory.GetFiles(directory));
    }
    [Theory][InlineData(UnityReviewWriteStage.AfterPartialWrite)][InlineData(UnityReviewWriteStage.BeforeReplace)]
    public void InterruptedReplacementRetainsPriorBytesAndCleansOnlyOwnedTemporary(UnityReviewWriteStage stage)
    {
        var root=Root();Store().Write("one",root,false);var bytes=File.ReadAllBytes(FileFor("one"));
        var broken=Store(current=>{if(current==stage)throw new IOException("Test-owned interruption.");});
        Assert.Equal(ReviewCheckpointError.WriteFailed,Assert.Throws<ReviewCheckpointStoreException>(()=>broken.Write("one",root,true)).Error);
        Assert.Equal(bytes,File.ReadAllBytes(FileFor("one")));Assert.Empty(Directory.GetFiles(directory,"*.tmp"));
    }
    [Fact]public void FinalNoOverwriteRaceAndBoundedCorruptReadsPreserveWinner()
    {
        var root=Root();var racing=Store(stage=>{if(stage==UnityReviewWriteStage.BeforeReplace)Store().Write("race",root,false);});
        Assert.Equal(ReviewCheckpointError.SlotOccupied,Assert.Throws<ReviewCheckpointStoreException>(()=>racing.Write("race",root,false)).Error);
        Assert.Equal(codec.EncodeColonisationRoot(root),File.ReadAllBytes(FileFor("race")));Assert.Empty(Directory.GetFiles(directory,"*.tmp"));
        File.WriteAllText(FileFor("bad"),"{}");
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Read("bad")).Error);
        using(var file=new FileStream(FileFor("large"),FileMode.CreateNew))file.SetLength(ReviewColonisationSlotStore.MaximumBytes+1L);
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,Assert.Throws<ReviewCheckpointStoreException>(()=>Store().Read("large")).Error);
    }
    public void Dispose()
    {
        var full=Path.GetFullPath(directory);var parent=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(!full.StartsWith(parent,StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(full).StartsWith("HobbyDS9-Colonisation-Carrier-",StringComparison.Ordinal))throw new InvalidOperationException();
        if(Directory.Exists(full))Directory.Delete(full,true);
    }
}
