using System.Text;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Newtonsoft.Json;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class ColonisationGrowthVocabularyTests
{
    static CampaignColonisationExpansionPackageDto Package(ReviewNewtonsoftCodec c)
    {
        byte[] Read(string f,string n)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",f,n+".json"));
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        var a=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),c);
        var colony=ColonisationPlaytestOptions.CreatePackage(a,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),c);
        return ColonisationExpansionPlaytestOptions.CreatePackage(ColonisationPlaytestOptions.CreateRosterPackage(colony,Read("colonisation-roster-v2","roster"),c),Read("colonisation-expansion-v1","planner"),c);
    }
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    static byte[] Raw(object x)=>Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(x,Formatting.None));
    static SettlerManifestDto Manifest()=>new(){ManifestId="manifest.wire-test",OwnerId="civ.federation",ShipId="ship.wire-test",Kind=SettlerManifestKind.SupplyOnly,Stage=SettlerManifestStage.Loaded,Blueprint=new(){BlueprintId="blueprint.wire-test",Revision=1},FitId="fit.wire-test",SupportHash=new string('a',64)};
    [Fact]public void OldCompleteTwelveRejectsGrowthProfileAndAppendedPlanVocabularyOnEncodeAndDecode()
    {
        var c=new ReviewNewtonsoftCodec();var p=Package(c);var original=CampaignColonisationExpansionApplication.StartGame(p,Settings()).ExportColonisationExpansionSave();var before=c.EncodeColonisationExpansionRoot(original);
        foreach(var change in new Action<CampaignColonisationExpansionSaveDto>[] {
            x=>x.ProfileId=CampaignColonisationGrowthIdentity.Profile,x=>x.AiPlans![0].Stage=ColonisationAiStage.Immigrating,x=>x.AiPlans![0].Stage=ColonisationAiStage.GrowthClosed,
            x=>x.AiPlans![0].LatestActions!.Add(new(){CommandId="command.wire-test",Action=ColonisationAiAction.PassengerLoad}) })
        {
            var x=c.DecodeColonisationExpansionRoot(before);change(x);Assert.Throws<InvalidDataException>(()=>c.EncodeColonisationExpansionRoot(x));Assert.Throws<InvalidDataException>(()=>c.DecodeColonisationExpansionRoot(Raw(x)));
        }
        Assert.Equal(before,c.EncodeColonisationExpansionRoot(c.DecodeColonisationExpansionRoot(before)));
    }
    [Fact]public void OldElevenAndEmbeddedHistoricalProofsKeepOriginalManifestAndCommandVocabulary()
    {
        var c=new ReviewNewtonsoftCodec();var p=Package(c);var old=CampaignColonisationApplication.StartGame(p.BaseRosterV2!.BaseV1!,Settings()).ExportColonisationSave();
        var manifest=Manifest();old.Colonisation!.ManifestRecords!.Add(manifest);var baseline=c.EncodeColonisationRoot(old);Assert.Equal(baseline,c.EncodeColonisationRoot(c.DecodeColonisationRoot(baseline)));
        manifest.Kind=SettlerManifestKind.ImmigrationOnly;Assert.Throws<InvalidDataException>(()=>c.EncodeColonisationRoot(old));Assert.Throws<InvalidDataException>(()=>c.DecodeColonisationRoot(Raw(old)));
        var root=CampaignColonisationExpansionApplication.StartGame(p,Settings()).ExportColonisationExpansionSave();
        var game=root.BaseCampaign!.Campaign!.Integrated!.Physical!.Core.Campaign.GameState;
        root.BaseCampaign.LastSettlementProof=new(){BeforeGameState=game,AfterGameState=game,BeforeColonisation=root.BaseCampaign.Colonisation,AfterColonisation=root.BaseCampaign.Colonisation,
            Receipts=new(){new(){Intent=new(),ManifestBefore=Manifest()}}};
        c.EncodeColonisationExpansionRoot(root);root.BaseCampaign.LastSettlementProof.Receipts![0].ManifestBefore!.Kind=SettlerManifestKind.ImmigrationOnly;
        Assert.Throws<InvalidDataException>(()=>c.EncodeColonisationExpansionRoot(root));Assert.Throws<InvalidDataException>(()=>c.DecodeColonisationExpansionRoot(Raw(root)));
        root=CampaignColonisationExpansionApplication.StartGame(p,Settings()).ExportColonisationExpansionSave();root.BaseCampaign!.Journal!.Add(new(){Colonisation=new(){ActorId="civ.federation",CommandId="command.wire-passengers",Action=ColonisationAction.LoadPassengers,Embark=new()}});
        Assert.Throws<InvalidDataException>(()=>c.EncodeColonisationExpansionRoot(root));Assert.Throws<InvalidDataException>(()=>c.DecodeColonisationExpansionRoot(Raw(root)));
    }
}
