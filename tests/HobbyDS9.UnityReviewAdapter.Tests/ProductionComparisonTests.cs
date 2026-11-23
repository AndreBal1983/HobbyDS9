using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class ProductionComparisonTests
{
    const string Human="civ.federation";
    readonly ReviewNewtonsoftCodec codec=new();
    static byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
    CampaignColonisationCyclePackageDto Package()
    {
        var template=ReviewExactPackageLoader.LoadPlayableFleet(Read("playable-fleet-v1","playable"),Read("playable-fleet-v1","package"),Read("playable-fleet-v1","physical"),Read("playable-fleet-v1","core"),Read("engineering-development-v1","scenario"),codec);
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        var colony=ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
        var roster=ColonisationPlaytestOptions.CreateRosterPackage(colony,Read("colonisation-roster-v2","roster"),codec);
        return ColonisationCyclePlaytestOptions.CreatePackage(ColonisationGrowthPlaytestOptions.CreatePackage(roster,Read("colonisation-expansion-v1","planner"),codec),Read("colonisation-cycle-v1","cycle-policy"),codec);
    }
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,
        HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    byte[] Wire(ICampaignColonisationCycleSession session)=>codec.EncodeCycleRoot(session.ExportCycleSave());

    [Fact]
    public void SupportedReadyClassesExposeRealCapabilitiesWithoutChangingPriceOrState()
    {
        var session=CampaignColonisationCycleApplication.StartCycleGame(Package(),Settings());var before=Wire(session);
        var options=session.CaptureActivities(Human).BuildOptions;
        var explorer=Assert.Single(options,o=>o.DefinitionId=="blueprint.ready.constitution-explorer");
        var facts=Assert.IsType<ActivityBuildComparisonDto>(explorer.Comparison);
        Assert.True(explorer.Supported);Assert.Equal(3,facts.StudyWork);Assert.Equal(1,facts.ContactCapacity);
        Assert.Equal(30,facts.MaxHull);Assert.Equal(0,facts.Armor);Assert.Equal(0,facts.ShieldCapacity);Assert.Equal(30,facts.Speed);
        Assert.True(facts.MovementBudget>0);
        var weapon=Assert.Single(facts.Weapons!);Assert.Equal(7,weapon.Damage);Assert.Equal(2,weapon.OpportunitiesPerRound);
        Assert.Equal(2,weapon.CadenceRounds);Assert.Equal(6500,weapon.AccuracyBps);Assert.Equal(4200,weapon.TrackingBps);
        Assert.True(weapon.Powered);Assert.Equal(weapon.RequiredPower,weapon.AllocatedPower);
        var cruiser=Assert.Single(options,o=>o.DefinitionId=="blueprint.ready.galaxy-cruiser");
        Assert.Equal(4,cruiser.Comparison!.StudyWork);Assert.Equal(3,cruiser.Comparison.ContactCapacity);Assert.Equal(2,cruiser.Comparison.Weapons!.Count);
        var transport=Assert.Single(options,o=>o.DefinitionId=="blueprint.ready.galaxy-colony-transport");
        Assert.Equal(0,transport.Comparison!.StudyWork);Assert.Equal(0,transport.Comparison.ContactCapacity);Assert.Empty(transport.Comparison.Weapons!);
        Assert.All(options.Where(o=>o.Kind==ActivityProductionKind.Infrastructure),o=>Assert.Null(o.Comparison));
        Assert.Equal(before,Wire(session));
        var loaded=CampaignColonisationCycleApplication.LoadCycleSave(Package(),codec.DecodeCycleRoot(before));
        Assert.Equal(explorer.Cost.Industry,Assert.Single(loaded.CaptureActivities(Human).BuildOptions,o=>o.DefinitionId==explorer.DefinitionId).Cost.Industry);
    }

    [Fact]
    public void MutatedComparisonCannotPoisonSessionOrLaterCachedCapture()
    {
        var session=CampaignColonisationCycleApplication.StartCycleGame(Package(),Settings());var before=Wire(session);
        var first=Assert.Single(session.CaptureActivities(Human).BuildOptions,o=>o.DefinitionId=="blueprint.ready.constitution-explorer");
        first.Comparison!.StudyWork=999;first.Comparison.Weapons![0].Damage=999;first.Cost.Industry=999;
        var next=Assert.Single(session.CaptureActivities(Human).BuildOptions,o=>o.DefinitionId==first.DefinitionId);
        Assert.NotSame(first.Comparison,next.Comparison);Assert.Equal(3,next.Comparison!.StudyWork);Assert.Equal(7,Assert.Single(next.Comparison.Weapons!).Damage);
        Assert.NotEqual(999,next.Cost.Industry);Assert.Equal(before,Wire(session));
    }

    [Fact]
    public void LegalButUnsupportedPublicationDoesNotAcquireInventedRatings()
    {
        var session=CampaignColonisationCycleApplication.StartCycleGame(Package(),Settings());
        var ids=new[]{"reactor","life-support","command","propulsion","escort-sensors"};
        var draft=new ShipClassDraftDto{ClassId="class.utility-comparison-test",DisplayName="Utility comparison test",RoleId="role.escort",HullVersionId="hull.utility.v1",EmbarkedCrew=30,
            Installations=ids.Select(id=>new ShipInstallationDto{InstallationId="fit."+id,ComponentVersionId="component.utility."+id+".v1",Quantity=1}).ToList(),
            Profiles=new[]{OperatingProfileKind.Cruise,OperatingProfileKind.Combat,OperatingProfileKind.Survey}.Select(profile=>new ShipOperatingProfileDto{Kind=profile,
                Allocations=ids.Select(id=>new ProfilePowerAllocationDto{InstallationId="fit."+id,RequestedPower=Power(profile,id),AllocatedPower=Power(profile,id)}).ToList()}).ToList()};
        var stamp=session.CaptureStatus();var accepted=session.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.publish.utility-comparison",
            ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CommandKind.PublishShipBlueprint,
            PublishShipBlueprint=new(){BlueprintId="blueprint.utility-comparison-test",Revision=1,Draft=draft}});
        Assert.True(accepted.Accepted,accepted.Error.ToString());
        var option=Assert.Single(session.CaptureActivities(Human).BuildOptions,o=>o.DefinitionId=="blueprint.utility-comparison-test");
        Assert.False(option.Supported);Assert.Null(option.Comparison);Assert.True(option.Cost.Industry>0);Assert.NotEmpty(option.Reason);
    }
    static long Power(OperatingProfileKind profile,string component)=>component=="reactor"?0:component=="life-support"?6:component=="command"?4:
        component=="propulsion"?(profile==OperatingProfileKind.Cruise?30:profile==OperatingProfileKind.Combat?12:8):profile==OperatingProfileKind.Cruise?4:profile==OperatingProfileKind.Combat?6:10;
}
