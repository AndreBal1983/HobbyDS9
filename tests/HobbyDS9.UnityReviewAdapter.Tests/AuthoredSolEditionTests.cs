using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class AuthoredSolEditionTests
{
    const string Human="civ.federation";
    readonly ReviewNewtonsoftCodec codec=new();
    static byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
    static string Json(object? value)=>JsonSerializer.Serialize(value,value?.GetType()??typeof(object));
    CampaignColonisationExpansionPackageDto Growth(bool solEight=true)
    {
        var template=ReviewExactPackageLoader.LoadPlayableFleet(Read("playable-fleet-v1","playable"),Read("playable-fleet-v1","package"),Read("playable-fleet-v1","physical"),Read("playable-fleet-v1","core"),Read(solEight?"sol-eight-planets-v1":"engineering-development-v1","scenario"),codec);
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        var colony=ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
        var roster=ColonisationPlaytestOptions.CreateRosterPackage(colony,Read("colonisation-roster-v2","roster"),codec);
        return ColonisationGrowthPlaytestOptions.CreatePackage(roster,Read("colonisation-expansion-v1","planner"),codec);
    }
    static NaturalGalaxySettingsDto Settings(int size=30)=>new(){Map=new(){Seed="42",SystemCount=size,EmptyRegionCount=size,
        Width=size==30?750000:2500000,Height=size==30?600000:2000000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    byte[] Wire(ICampaignColonisationCycleSession session)=>codec.EncodeCycleRoot(session.ExportCycleSave());
    static GameStateDto Game(CampaignColonisationCycleSaveDto root)=>root.BaseCampaign!.Campaign!.Integrated!.Physical!.Core.Campaign.GameState;

    [Theory][InlineData(30)][InlineData(250)]
    public void NewEditionDisclosesExactlyTheMappedAuthoredSolBeltWithoutChangingPhysicalWorldOrV1(int size)
    {
        var growth=Growth();var oldPackage=CampaignColonisationCycleApplication.CreateSystemRichnessPackage(growth,new());
        var package=AuthoredSolPlaytestOptions.CreatePackage(growth);
        Assert.IsType<CampaignAuthoredSolPackageDto>(codec.CopyCyclePackage(package));
        var old=CampaignColonisationCycleApplication.StartCycleGame(oldPackage,Settings(size));
        var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings(size));
        var oldRoot=Assert.IsType<CampaignSystemRichnessSaveDto>(old.ExportCycleSave());
        var root=Assert.IsType<CampaignAuthoredSolSaveDto>(session.ExportCycleSave());
        Assert.Equal(16,root.FormatVersion);Assert.Equal(CampaignAuthoredSolIdentity.Profile,root.ProfileId);
        Assert.Equal(CampaignAuthoredSolIdentity.Generation,root.GenerationVersion);Assert.NotEqual(oldPackage.PackageHash,package.PackageHash);
        Assert.Equal(Json(oldRoot.BaseCampaign!.Map),Json(root.BaseCampaign!.Map));
        Assert.Equal(Json(oldRoot.BaseCampaign.FrozenPlanets),Json(root.BaseCampaign.FrozenPlanets));
        Assert.Equal(Json(Game(oldRoot).Colonies),Json(Game(root).Colonies));
        Assert.Equal(Json(Game(oldRoot).Civilizations),Json(Game(root).Civilizations));
        Assert.Equal(Json(Game(oldRoot).RngState),Json(Game(root).RngState));
        Assert.Equal(Game(oldRoot).NextEntitySequence,Game(root).NextEntitySequence);
        Assert.Equal(Json(oldRoot.SystemBelts),Json(root.SystemBelts!.Where(b=>b.FeatureId!=CampaignAuthoredSolIdentity.FeatureId).ToList()));
        Assert.Empty(old.CaptureKnownAstronomy(Human).SystemBelts);Assert.Equal(CampaignSystemRichnessIdentity.Generation,old.CaptureKnownAstronomy(Human).SystemBeltGeneration);
        var sol=session.CaptureColonisationPlanets(Human).Single(p=>p.PlanetId=="planet.sol").SystemId;
        Assert.Equal(8,session.CaptureColonisationPlanets(Human).Count(p=>p.SystemId==sol));
        var sky=session.CaptureKnownAstronomy(Human);var belt=Assert.Single(sky.SystemBelts);
        Assert.Equal(CampaignAuthoredSolIdentity.Generation,sky.SystemBeltGeneration);
        Assert.Equal(CampaignAuthoredSolIdentity.FeatureId,belt.FeatureId);Assert.Equal(sol,belt.SystemId);
        Assert.Equal(2200,belt.InnerRadiusMilliAu);Assert.Equal(3200,belt.OuterRadiusMilliAu);
        Assert.Equal(CampaignAuthoredSolIdentity.RadiusUnit,belt.RadiusUnit);Assert.Matches("^[0-9a-f]{64}$",belt.VisualSeed);
        var before=Wire(session);session.CaptureKnownAstronomy(Human);session.CaptureColonisationPlanets(Human);Assert.Equal(before,Wire(session));
        Assert.Equal(20,session.CaptureStrategy(Human).Research.Engineering.Count);
    }

    [Fact]
    public void NewCarrierLoadsExactlyAfterOrdinaryTurnAndRejectsOlderOrTamperedEditions()
    {
        var growth=Growth();var package=AuthoredSolPlaytestOptions.CreatePackage(growth);
        var oldPackage=CampaignColonisationCycleApplication.CreateSystemRichnessPackage(growth,new());
        var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings());
        var old=CampaignColonisationCycleApplication.StartCycleGame(oldPackage,Settings());
        var initial=Wire(session);var root=Assert.IsType<CampaignAuthoredSolSaveDto>(codec.DecodeCycleRoot(initial));
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(oldPackage,root));
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(package,old.ExportCycleSave()));
        Assert.Equal(initial,Wire(CampaignColonisationCycleApplication.LoadCycleSave(package,root)));
        var bad=Assert.IsType<CampaignAuthoredSolSaveDto>(codec.CopyCycleSave(root));
        bad.SystemBelts!.Single(b=>b.FeatureId==CampaignAuthoredSolIdentity.FeatureId).OuterRadiusMilliAu=3199;
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(package,bad));
        var missing=JObject.Parse(System.Text.Encoding.UTF8.GetString(initial));missing.Remove("SystemBelts");
        Assert.ThrowsAny<Exception>(()=>codec.DecodeCycleRoot(System.Text.Encoding.UTF8.GetBytes(missing.ToString())));
        Assert.Equal(initial,Wire(session));
        var stamp=session.CaptureStatus();Assert.True(session.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);
        var next=Wire(session);var reopened=CampaignColonisationCycleApplication.LoadCycleSave(package,codec.DecodeCycleRoot(next));
        Assert.Equal(next,Wire(reopened));
        Assert.Equal(Json(session.CaptureKnownAstronomy(Human).SystemBelts),Json(reopened.CaptureKnownAstronomy(Human).SystemBelts));
    }

    [Fact]
    public void NewHostAndStoreUseTheirExplicitEditionWithoutTouchingOldRootBytes()
    {
        var growth=Growth();var package=AuthoredSolPlaytestOptions.CreatePackage(growth);
        Assert.Equal("-engineering20-sol08-authoredbelt1",AuthoredSolPlaytestOptions.SaveDirectorySuffix);
        var directory=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"authored-sol-store-"+Guid.NewGuid().ToString("N")));
        var store=new ReviewColonisationCycleSlotStore(directory,codec);
        var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings());
        store.Write("authored",session.ExportCycleSave(),false);
        var read=Assert.IsType<CampaignAuthoredSolSaveDto>(store.Read("authored"));Assert.Equal(Wire(session),codec.EncodeCycleRoot(read));
        var continued=ReviewUiModel.ContinueColonisationCycle(package,codec,store,"authored");Assert.True(continued.ColonisationCycleCampaign);
        Assert.Equal(8,continued.ColonisationPlanets().Count(p=>p.SystemId==continued.ColonisationPlanets().Single(x=>x.PlanetId=="planet.sol").SystemId));
        var old=CampaignColonisationCycleApplication.StartCycleGame(CampaignColonisationCycleApplication.CreateSystemRichnessPackage(growth,new()),Settings());
        var oldBytes=Wire(old);Assert.IsType<CampaignSystemRichnessSaveDto>(codec.DecodeCycleRoot(oldBytes));
        Assert.Equal(oldBytes,codec.EncodeCycleRoot(codec.DecodeCycleRoot(oldBytes)));
        Assert.DoesNotContain(CampaignAuthoredSolIdentity.Generation,System.Text.Encoding.UTF8.GetString(oldBytes));
    }

    [Fact]
    public void NewEditionWillNotManufactureSolFromTheEarlierEarthMarsOnlyTemplate()
    {
        var package=AuthoredSolPlaytestOptions.CreatePackage(Growth(solEight:false));
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.StartCycleGame(package,Settings()));
    }
}
