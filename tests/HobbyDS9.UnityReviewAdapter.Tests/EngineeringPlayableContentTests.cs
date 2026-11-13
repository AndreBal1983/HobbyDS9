using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class EngineeringPlayableContentTests
{
    const string Human="civ.federation";
    readonly ReviewNewtonsoftCodec codec=new();
    static byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
    CampaignColonisationCyclePackageDto Package(bool engineering=true,bool scienceReady=false)
    {
        var scenario=Read(engineering?"engineering-development-v1":"playable-fleet-v1","scenario");
        if(scienceReady)
        {
            // Test-only starting fixture; shipped content retains original Science/reserves.
            var json=JObject.Parse(System.Text.Encoding.UTF8.GetString(scenario));
            foreach(var actor in json["InitialState"]!["Civilizations"]!)
            {
                foreach(var discipline in actor["Disciplines"]!)discipline["Level"]=10;
                actor["EngineeringReserve"]=2000000;
            }
            scenario=System.Text.Encoding.UTF8.GetBytes(json.ToString());
        }
        var template=ReviewExactPackageLoader.LoadPlayableFleet(Read("playable-fleet-v1","playable"),Read("playable-fleet-v1","package"),
            Read("playable-fleet-v1","physical"),Read("playable-fleet-v1","core"),scenario,codec);
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        var colony=ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
        var roster=ColonisationPlaytestOptions.CreateRosterPackage(colony,Read("colonisation-roster-v2","roster"),codec);
        return ColonisationCyclePlaytestOptions.CreatePackage(ColonisationGrowthPlaytestOptions.CreatePackage(roster,
            Read("colonisation-expansion-v1","planner"),codec),Read("colonisation-cycle-v1","cycle-policy"),codec);
    }
    static NaturalGalaxySettingsDto Settings(int size=30)=>new(){Map=new(){Seed="42",SystemCount=size,EmptyRegionCount=size,Width=size==30?750000:2500000,Height=size==30?600000:2000000,
        HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    byte[] Wire(ICampaignColonisationCycleSession s)=>codec.EncodeCycleRoot(s.ExportCycleSave());

    [Theory][InlineData(30)][InlineData(250)]
    public void ActualPlayableCompositionExposesTwentyLockedProjectsAndRejectsOtherContentSaves(int size)
    {
        var package=Package();var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings(size));
        var cards=session.CaptureStrategy(Human).Research.Engineering;
        Assert.Equal(20,cards.Count);
        Assert.Equal(20,cards.Select(c=>c.Definition.DefinitionId).Distinct().Count());
        Assert.All(cards,c=>Assert.Equal(CommandErrorCode.EngineeringScienceRequirementMissing,c.UnavailableReason));
        var ids=new[]{"advanced-starship-engineering","compact-reactor","high-output-reactor","efficient-drive","high-speed-drive",
            "maneuver-drive","resilient-shield","efficient-shield","multilayer-shield","compact-energy-bank","long-range-energy-bank",
            "improved-launcher","compact-life-support","efficient-life-support","expanded-computing","precision-fire-control",
            "long-range-sensors","compact-laboratories","reinforced-hull","expanded-hull"}.Select(id=>"engineering."+id).OrderBy(id=>id,StringComparer.Ordinal);
        Assert.Equal(ids,cards.Select(c=>c.Definition.DefinitionId.Value));
        var library=session.CaptureDesignLibrary(Human).Catalogue!;
        Assert.Contains("component.reactor.v1",library.AvailableVersionIds);
        Assert.DoesNotContain("component.reactor.v2",library.AvailableVersionIds);
        Assert.Contains(library.Catalogue.Components,c=>c.VersionId=="component.reactor.v2");
        var before=Wire(session);
        Assert.Equal(before,Wire(CampaignColonisationCycleApplication.LoadCycleSave(package,session.ExportCycleSave())));
        var legacyPackage=Package(engineering:false);
        Assert.NotEqual(legacyPackage.PackageHash,package.PackageHash);
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(legacyPackage,session.ExportCycleSave()));
        var legacy=CampaignColonisationCycleApplication.StartCycleGame(legacyPackage,Settings(size));
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(package,legacy.ExportCycleSave()));
        Assert.Equal(before,Wire(session));
    }

    [Fact]
    public void CycleSpendsOnAllTwentyProjectsRetainsVersionGatesAndReloadsWithoutRewritingReadyPublications()
    {
        var package=Package(scienceReady:true);var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings());
        var published=JsonSerializer.Serialize(session.CaptureDesignLibrary(Human).Published);
        var completed=0;
        while(completed<20)
        {
            var project=session.CaptureStrategy(Human).Research.Engineering.First(c=>c.CanTarget);
            var stamp=session.CaptureStatus();
            Assert.True(session.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.engineering."+completed,
                ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CommandKind.SetEngineeringTarget,
                SetEngineeringTarget=new(){TargetDefinitionId=project.Definition.DefinitionId.Value}}).Accepted);
            stamp=session.CaptureStatus();Assert.True(session.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);
            completed++;
            var research=session.CaptureStrategy(Human).Research;
            Assert.Equal(completed,research.Engineering.Count(c=>c.MaturityPercent==100));
            Assert.True(research.EngineeringReserve<2000000);
            Assert.Equal(published,JsonSerializer.Serialize(session.CaptureDesignLibrary(Human).Published));
        }
        var finalLibrary=session.CaptureDesignLibrary(Human).Catalogue!;
        Assert.Contains("component.reactor.v3",finalLibrary.AvailableVersionIds);
        Assert.Contains("hull.utility.v3",finalLibrary.AvailableVersionIds);
        var before=Wire(session);
        Assert.Equal(before,Wire(CampaignColonisationCycleApplication.LoadCycleSave(package,session.ExportCycleSave())));
    }
}
