using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
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

    [Fact]public void TypedOutputsJoinActualVersionGatesAndPreserveAuthoredParentFactsWithoutMutation()
    {
        var session=CampaignColonisationCycleApplication.StartCycleGame(Package(),Settings());var before=Wire(session);
        var projects=session.CaptureStrategy(Human).Research.Engineering;
        var outputs=projects.SelectMany(p=>p.Definition.Outputs).ToArray();
        Assert.Equal(17,outputs.Count(o=>o.Kind==EngineeringOutputKind.Module));
        Assert.Equal(2,outputs.Count(o=>o.Kind==EngineeringOutputKind.Hull));
        Assert.Single(outputs,o=>o.Kind==EngineeringOutputKind.ShipDesign);
        var compact=Assert.Single(projects.Single(p=>p.Definition.DefinitionId.Value=="engineering.compact-life-support").Definition.Outputs);
        Assert.Equal("component.utility.life-support.v2",compact.Identity);
        Assert.Equal("Reduce module hull space",EngineeringPresentation.Target(compact));Assert.Equal("12 → 9 hull space",EngineeringPresentation.Expected(compact));
        var current=Assert.IsType<ComponentVersionSnapshot>(compact.Component);var parent=Assert.IsType<ComponentVersionSnapshot>(compact.ParentComponent);
        Assert.Equal("component.utility.life-support.v1",parent.VersionId);Assert.Equal(parent.VersionId,current.ParentVersionId);
        Assert.Equal(12,parent.Capacity);Assert.Equal(9,current.Capacity);Assert.Equal(1,current.CrewRequired);Assert.Equal(1,parent.CrewRequired);
        Assert.All(current.Modes,m=>{Assert.Equal(6,m.FullPower);Assert.Equal(6,m.MinimumPower);Assert.Equal(40,Assert.Single(m.Outputs,o=>o.Kind==ShipOutputKind.CrewSupport).Amount);});
        Assert.Equal(3,parent.Cost.Industry);Assert.Equal(6,current.Cost.Industry);Assert.Equal(2,parent.Cost.Credits);Assert.Equal(4,current.Cost.Credits);
        Assert.Contains(compact.RequiredProjects,id=>id.Value=="engineering.compact-life-support");
        Assert.Equal(before,Wire(session));
    }

    [Fact]public void VisibleProjectsFollowFullAuthoritativeEligibilityAndKeepActiveAndCompletedWork()
    {
        var locked=CampaignColonisationCycleApplication.StartCycleGame(Package(),Settings()).CaptureStrategy(Human).Research.Engineering;
        Assert.Equal(20,locked.Count);Assert.DoesNotContain(locked,p=>EngineeringPresentation.Visible(p,true));
        var session=CampaignColonisationCycleApplication.StartCycleGame(Package(scienceReady:true),Settings());var cards=session.CaptureStrategy(Human).Research.Engineering;
        Assert.Contains(cards,p=>p.CanTarget&&EngineeringPresentation.Visible(p,true));
        Assert.All(cards.Where(p=>p.ScienceSatisfied.All(v=>v)&&p.PrerequisitesSatisfied.Any(v=>!v)),p=>Assert.False(EngineeringPresentation.Visible(p,true)));
        var scout=cards.Single(p=>p.Definition.DefinitionId.Value=="engineering.advanced-starship-engineering");Assert.True(scout.CanTarget);Assert.False(EngineeringPresentation.Visible(scout,true));
        var project=cards.First(p=>p.CanTarget&&EngineeringPresentation.SupportedOutput(p,true));var stamp=session.CaptureStatus();
        Assert.True(session.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.visible-project",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CommandKind.SetEngineeringTarget,SetEngineeringTarget=new(){TargetDefinitionId=project.Definition.DefinitionId.Value}}).Accepted);
        var active=session.CaptureStrategy(Human).Research.Engineering.Single(p=>p.IsTarget);Assert.False(active.CanTarget);Assert.True(EngineeringPresentation.Visible(active,true));
        stamp=session.CaptureStatus();Assert.True(session.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);
        var completed=session.CaptureStrategy(Human).Research.Engineering.Single(p=>p.Definition.DefinitionId==project.Definition.DefinitionId);Assert.Equal(EngineeringMaturityBand.Mature,completed.Band);Assert.True(EngineeringPresentation.Visible(completed,true));
    }

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
        Assert.DoesNotContain("component.utility.reactor.v2",library.AvailableVersionIds);
        Assert.Contains(library.Catalogue.Components,c=>c.VersionId=="component.utility.reactor.v2");
        Assert.Equal(47,library.Catalogue.Components.Count);
        Assert.Equal(6,library.Catalogue.Hulls.Count);
        Assert.Contains("component.utility.reactor.v1",library.AvailableVersionIds);
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
        Assert.Contains("component.utility.reactor.v3",finalLibrary.AvailableVersionIds);
        Assert.Contains("hull.utility.v3",finalLibrary.AvailableVersionIds);
        var before=Wire(session);
        Assert.Equal(before,Wire(CampaignColonisationCycleApplication.LoadCycleSave(package,session.ExportCycleSave())));
    }
}
