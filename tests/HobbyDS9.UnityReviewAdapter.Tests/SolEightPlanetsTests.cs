using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class SolEightPlanetsTests
{
    const string Human="civ.federation", Rival="civ.cardassian";
    static readonly string[] Ids={"planet.mercury","planet.venus","planet.sol","planet.mars","planet.jupiter","planet.saturn","planet.uranus","planet.neptune"};
    static readonly long[] Distances={57900000,108200000,149600000,228000000,778500000,1432000000,2867000000,4515000000};
    readonly ReviewNewtonsoftCodec codec=new();
    static byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
    CampaignColonisationCyclePackageDto Package(bool sol=true,byte[]? scenario=null)
    {
        var template=ReviewExactPackageLoader.LoadPlayableFleet(Read("playable-fleet-v1","playable"),Read("playable-fleet-v1","package"),
            Read("playable-fleet-v1","physical"),Read("playable-fleet-v1","core"),scenario??Read(sol?"sol-eight-planets-v1":"engineering-development-v1","scenario"),codec);
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        var colony=ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
        var roster=ColonisationPlaytestOptions.CreateRosterPackage(colony,Read("colonisation-roster-v2","roster"),codec);
        return ColonisationCyclePlaytestOptions.CreatePackage(ColonisationGrowthPlaytestOptions.CreatePackage(roster,
            Read("colonisation-expansion-v1","planner"),codec),Read("colonisation-cycle-v1","cycle-policy"),codec);
    }
    static NaturalGalaxySettingsDto Settings(int size=30)=>new(){Map=new(){Seed="42",SystemCount=size,EmptyRegionCount=size,
        Width=size==30?750000:2500000,Height=size==30?600000:2000000,
        HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    byte[] Wire(ICampaignColonisationCycleSession session)=>codec.EncodeCycleRoot(session.ExportCycleSave());
    static ColonisationPlanetObservationDto[] Sol(ICampaignColonisationCycleSession session)
    {
        var known=session.CaptureColonisationPlanets(Human);
        var home=known.Single(p=>p.PlanetId=="planet.sol").SystemId;
        return known.Where(p=>p.SystemId==home).OrderBy(p=>p.PlanetId,StringComparer.Ordinal).ToArray();
    }

    [Fact]
    public void AuthoredExpansionPreservesEveryExistingFactAndStoresSourcedMeanDistances()
    {
        var original=JObject.Parse(System.Text.Encoding.UTF8.GetString(Read("engineering-development-v1","scenario")));
        var next=JObject.Parse(System.Text.Encoding.UTF8.GetString(Read("sol-eight-planets-v1","scenario")));
        var additions=Ids.Except(new[]{"planet.sol","planet.mars"}).ToArray();
        foreach(var id in additions)
        {
            var definition="planet-definition."+id.Substring("planet.".Length);
            var body=next["Definitions"]!["Planets"]!.Single(p=>(string?)p["DefinitionId"]==definition);
            Assert.Equal("Unclassified",(string?)body["PlanetClass"]);
            Assert.Empty(body["Deposits"]!);
            Assert.All(body["Habitats"]!,h=>Assert.Equal(0,(long)h["NaturalCapacity"]!));
            Assert.Equal(3000,(int)body["Gravity"]!);Assert.Equal(0,(int)body["Pressure"]!);
            Assert.Equal(200,(int)body["Radiation"]!);Assert.Equal("atmosphere.vacuum",(string?)body["Atmosphere"]);
            Assert.Equal(10000,(int)body["Habitats"]!.Single(h=>(int)h["Kind"]! == (int)HabitatKind.Volcanic)["ShareBps"]!);
            var physical=next["InitialState"]!["Planets"]!.Single(x=>(string?)x["PlanetId"]==id);
            Assert.Empty(physical["RemainingDeposits"]!);
            Assert.DoesNotContain(next["InitialState"]!["Colonies"]!,c=>(string?)c["PlanetId"]==id);
            body.Remove();
            next["Definitions"]!["DefinitionIds"]!.Single(x=>(string?)x==definition).Remove();
            physical.Remove();
            next["InitialState"]!["EntityIdentities"]!.Single(x=>(string?)x["EntityId"]==id).Remove();
        }
        next["ScenarioId"]=original["ScenarioId"];
        // Only DefinitionIds are intentionally canonically sorted in the new authoring.
        original["Definitions"]!["DefinitionIds"]=new JArray(original["Definitions"]!["DefinitionIds"]!.Select(x=>(string)x!).OrderBy(x=>x,StringComparer.Ordinal));
        Assert.True(JToken.DeepEquals(original,next),"Existing colonies, populations, resources, designs, research and initial knowledge must be unchanged.");
        var astronomy=JObject.Parse(System.Text.Encoding.UTF8.GetString(Read("sol-eight-planets-v1","astronomy")));
        Assert.Equal("sol-mean-orbits-v1",(string?)astronomy["Version"]);
        Assert.Equal("km",(string?)astronomy["DistanceUnit"]);
        Assert.Equal("https://nssdc.gsfc.nasa.gov/planetary/factsheet/",(string?)astronomy["SourceUrl"]);
        Assert.Equal("2026-11-15",(string?)astronomy["RetrievedOn"]);
        Assert.Equal(Ids,astronomy["Planets"]!.Select(p=>(string)p["PlanetId"]!));
        Assert.Equal(Distances,astronomy["Planets"]!.Select(p=>(long)p["MeanSunDistanceKm"]!));
        Assert.Equal(Enumerable.Range(1,8),astronomy["Planets"]!.Select(p=>(int)p["Order"]!));
    }

    [Theory][InlineData(30)][InlineData(250)]
    public void ActualCycleMapsAllEightHomeBodiesWithoutSurveyOrForeignDisclosureAndRoundtrips(int size)
    {
        var package=Package();var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings(size));
        var sol=Sol(session);
        Assert.Equal(Ids.OrderBy(x=>x,StringComparer.Ordinal),sol.Select(p=>p.PlanetId));
        Assert.NotEqual("system.sol",sol[0].SystemId); // Actual generated home association, not a GUI-only solar-system fixture.
        Assert.All(sol.Where(p=>p.PlanetId!="planet.sol"&&p.PlanetId!="planet.mars"),p=>{
            Assert.False(p.Surveyed);Assert.Null(p.Environment);Assert.Null(p.KnownControl);
            Assert.Null(p.ActiveSurveyId);Assert.Equal(0,p.CompletedSurveyWeeks);
        });
        Assert.Throws<ArgumentException>(()=>session.CaptureColonisationPlanets(Rival)); // Public API does not impersonate an AI actor.
        Assert.Equal(20,session.CaptureStrategy(Human).Research.Engineering.Count);
        var root=JObject.Parse(System.Text.Encoding.UTF8.GetString(Wire(session)));
        var controls=root.SelectTokens("$..GenesisControls").First().ToArray();
        foreach(var id in Ids.Except(new[]{"planet.sol","planet.mars"}))
        {
            var control=controls.Single(c=>(string?)c["PlanetId"]==id);
            Assert.Equal((int)PlanetControllerKind.Unclaimed,(int)control["Kind"]!);Assert.Null(control["ActorId"]?.Value<string>());
        }
        var before=Wire(session);
        var loaded=CampaignColonisationCycleApplication.LoadCycleSave(package,codec.DecodeCycleRoot(before));
        Assert.Equal(before,Wire(loaded));
        Assert.Equal(JsonSerializer.Serialize(sol),JsonSerializer.Serialize(Sol(loaded)));
        Assert.Equal(before,Wire(session)); // Observations/load do not mutate the original.
    }

    [Fact]
    public void AuthoredBodiesAtRivalHomeDoNotLeakThroughTheirNamesOrTheAstronomyTable()
    {
        // Test-only relocation checks the same stable IDs at a non-observed home.
        // No public AI impersonation or global Sol-name visibility exception is needed.
        var scenario=JObject.Parse(System.Text.Encoding.UTF8.GetString(Read("sol-eight-planets-v1","scenario")));
        var added=Ids.Except(new[]{"planet.sol","planet.mars"}).ToArray();
        foreach(var id in added)
        {
            scenario["InitialState"]!["Planets"]!.Single(p=>(string?)p["PlanetId"]==id)["SystemId"]="system.cardassia";
            var definition="planet-definition."+id.Substring("planet.".Length);
            scenario["Definitions"]!["Planets"]!.Single(p=>(string?)p["DefinitionId"]==definition)["SystemDefinitionId"]="star-system.cardassia";
        }
        var package=Package(scenario:System.Text.Encoding.UTF8.GetBytes(scenario.ToString()));
        var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings());
        Assert.DoesNotContain(session.CaptureColonisationPlanets(Human),p=>added.Contains(p.PlanetId));
        Assert.Equal(2,Sol(session).Length);
    }

    [Fact]
    public void ChangedContentRejectsOldSavesBothWaysAndSuccessfulTurnRetainsBodiesOnReload()
    {
        var package=Package();var oldPackage=Package(sol:false);
        Assert.NotEqual(package.PackageHash,oldPackage.PackageHash);
        var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings());
        var old=CampaignColonisationCycleApplication.StartCycleGame(oldPackage,Settings());
        var before=Wire(session);
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(oldPackage,session.ExportCycleSave()));
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(package,old.ExportCycleSave()));
        Assert.Equal(before,Wire(session));
        Assert.Equal(2,Sol(old).Length);
        var stamp=session.CaptureStatus();Assert.True(session.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);
        var after=Wire(session);var loaded=CampaignColonisationCycleApplication.LoadCycleSave(package,codec.DecodeCycleRoot(after));
        Assert.Equal(after,Wire(loaded));Assert.Equal(8,Sol(loaded).Length);
    }
}
