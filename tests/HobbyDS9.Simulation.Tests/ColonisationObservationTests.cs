using HobbyDS9.Application;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;

namespace HobbyDS9.Simulation.Tests;

public sealed class ColonisationObservationTests
{
    [Fact] public void NewFacadeReadsOwnDetachedFactsAndMissionWithoutMutatingCanonicalRoot()
    {
        var app=CampaignColonisationApplication.StartGame(ColonisationInventoryTests.Package(),PublishedConstructionProfileTests.Settings());
        var before=Json(app.ExportColonisationSave());var view=app.CaptureColonisation(Human);Assert.Equal(Human,view.Actor);Assert.Equal(2,view.Worlds.Count);Assert.Empty(view.Transports);Assert.Empty(view.Production);Assert.Empty(view.LatestDeployments);
        Assert.All(view.Worlds,w=>{Assert.True(w.Shipbuilding);Assert.Null(w.ConstructionUnavailableReason(true));});
        Assert.Equal(app.CaptureStatus().CampaignRevision,view.Revision);Assert.All(view.Presence,p=>{Assert.DoesNotContain("owner",p.Caption,StringComparison.OrdinalIgnoreCase);Assert.InRange(p.ObservationTurn,0,view.Turn);});
        var actions=app.CaptureActivities(Human);Assert.Equal(4,actions.BuildOptions.Count(o=>o.Kind==ActivityProductionKind.PublishedShip));Assert.Contains(actions.BuildOptions,o=>o.DefinitionId=="infrastructure.frontier-mine.v1");
        var mission=app.CaptureFrontierCommission(Human);Assert.Equal(FrontierCommissionAvailability.Available,mission.Availability);Assert.Equal(4,mission.Objectives.Count);Assert.All(mission.Objectives,o=>Assert.Null(o.Evidence));
        var planets=app.CaptureColonisationPlanets(Human);Assert.All(planets,p=>Assert.Contains(view.Worlds,w=>w.SystemId==p.SystemId));planets[0].Environment!.Gravity=999;
        actions.BuildOptions.Clear();Assert.Equal(before,Json(app.ExportColonisationSave()));Assert.Throws<ArgumentException>(()=>app.CaptureColonisation("civ.cardassian"));
        Assert.DoesNotContain(view.Worlds,w=>w.Id=="colony.cardassia");Assert.All(view.Worlds,w=>Assert.True(w.Shipbuilding));
    }
    [Fact] public void OwnFoodDemandAndPublicBuildingBenefitsAreAuthoritativeDetachedAndReadOnly()
    {
        var package=ColonisationInventoryTests.Package();var packageBefore=Json(package);
        var app=CampaignColonisationApplication.StartGame(package,PublishedConstructionProfileTests.Settings());
        var before=Json(app.ExportColonisationSave());var view=app.CaptureColonisation(Human);
        var earth=view.Worlds.Single(w=>w.Id=="colony.sol");var mars=view.Worlds.Single(w=>w.Id=="colony.mars");
        Assert.Equal(1000,earth.Population);Assert.Equal(10,earth.FoodDemand);Assert.Equal(14,earth.FoodOutput);Assert.Equal(4,earth.LocalFoodBalance);
        Assert.Equal(250,mars.Population);Assert.Equal(3,mars.FoodDemand);Assert.Equal(0,mars.FoodOutput);Assert.Equal(-3,mars.LocalFoodBalance);
        Assert.Equal(0,mars.ShortageWeeks);Assert.DoesNotContain(view.Worlds,w=>w.Id=="colony.cardassia");
        var publicIds=app.CaptureActivities(Human).BuildOptions.Where(b=>b.Kind==ActivityProductionKind.Infrastructure).Select(b=>b.DefinitionId).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        Assert.Equal(publicIds,view.BuildingBenefits.Select(b=>b.DefinitionId));
        foreach(var authored in package.Activities!.Content!.Structures!)
        {
            var benefit=view.BuildingBenefits.Single(b=>b.DefinitionId==authored.DefinitionId);
            Assert.Equal(authored.FoodOutput,benefit.FoodOutput);Assert.Equal(authored.Industry,benefit.Industry);Assert.Equal(authored.Research,benefit.Research);Assert.Equal(authored.PowerDemand,benefit.PowerDemand);
        }
        var hydro=view.BuildingBenefits.Single(b=>b.DefinitionId=="infrastructure.hydroponic-extension.v1");Assert.Equal(6,hydro.FoodOutput);Assert.Equal(1,hydro.PowerDemand);
        var mine=view.BuildingBenefits.Single(b=>b.DefinitionId==package.Settlement!.Mine!.Infrastructure!.DefinitionId);
        Assert.Equal(3,Assert.Single(mine.Extraction).Amount);Assert.Equal(3,mine.PowerDemand);
        Assert.DoesNotContain(view.BuildingBenefits,b=>b.DefinitionId=="infrastructure.farms");
        Assert.Throws<NotSupportedException>(()=>((IList<InfrastructureDefinitionSnapshot>)view.BuildingBenefits).Clear());
        Assert.Throws<NotSupportedException>(()=>((IList<ResourceAmountSnapshot>)mine.Extraction).Clear());
        Assert.Null(typeof(ColonisationWorldSnapshot).GetProperty(nameof(earth.FoodDemand))!.SetMethod);
        Assert.Equal(before,Json(app.ExportColonisationSave()));Assert.Equal(packageBefore,Json(package));
        package.Settlement!.Mine!.Infrastructure!.Extraction![0].Amount=99;package.Activities!.Content!.Structures!.Single(s=>s.DefinitionId==hydro.DefinitionId).FoodOutput=999;
        Assert.Equal(6,hydro.FoodOutput);Assert.Equal(3,Assert.Single(mine.Extraction).Amount);Assert.Equal(6,app.CaptureColonisation(Human).BuildingBenefits.Single(b=>b.DefinitionId==hydro.DefinitionId).FoodOutput);
        Assert.Throws<ArgumentException>(()=>app.CaptureColonisation("civ.cardassian"));Assert.Equal(before,Json(app.ExportColonisationSave()));
    }
    [Fact] public void LegacySnapshotCallerHasUnavailableDemandAndNoInventedBuildingEffects()
    {
        var world=new ColonisationWorldSnapshot("own","p","s","World",ColonyEstablishmentStage.Established,null,250,4,18,0,0,0,0,0,0,"",Array.Empty<ResourceAmountSnapshot>(),Array.Empty<ColonisationCohortSnapshot>());
        Assert.Null(world.FoodDemand);Assert.Null(world.LocalFoodBalance);
        var view=new ColonisationCampaignSnapshot(Human,1,0,"hash",new[]{world},Array.Empty<ColonisationTransportSnapshot>(),Array.Empty<ColonisationQueueSnapshot>(),Array.Empty<ColonisationPresenceSnapshot>(),Array.Empty<ColonisationManifestReport>(),Array.Empty<ColonisationSurveySnapshot>(),Array.Empty<ColonisationIntentSnapshot>(),60,Array.Empty<ResourceAmountSnapshot>());
        Assert.Empty(view.BuildingBenefits);
    }
    [Theory]
    [InlineData(ColonyEstablishmentStage.Established,100,true,true,null)]
    [InlineData(ColonyEstablishmentStage.Established,100,false,true,"Requires a shipyard at this world")]
    [InlineData(ColonyEstablishmentStage.Established,100,false,false,null)]
    [InlineData(ColonyEstablishmentStage.Founding,100,false,true,"Requires an established world.")]
    [InlineData(ColonyEstablishmentStage.EmptyOutpost,0,true,true,"Requires an established world.")]
    [InlineData(ColonyEstablishmentStage.Established,0,true,true,"Requires a populated world.")]
    [InlineData(ColonyEstablishmentStage.Established,0,false,false,"Requires a populated world.")]
    public void SnapshotConstructionGuidanceUsedByPanelKeepsWorldAvailabilityBeforeYard(ColonyEstablishmentStage stage,long people,bool yard,bool ship,string? reason)
    {
        var world=new ColonisationWorldSnapshot("own","planet","system","Own world",stage,null,people,20,4,0,2,0,0,0,0,"",
            Array.Empty<ResourceAmountSnapshot>(),Array.Empty<ColonisationCohortSnapshot>(),yard);
        Assert.Equal(yard,world.Shipbuilding);Assert.Equal(reason,world.ConstructionUnavailableReason(ship));
        Assert.Null(typeof(ColonisationWorldSnapshot).GetProperty(nameof(world.Shipbuilding))!.SetMethod);
    }
    [Fact] public void AvailabilityAndDetachedForkUseFullTransactionsAndLeaveOriginalIdsAndJournalAlone()
    {
        var package=ColonisationInventoryTests.Package();var app=CampaignColonisationApplication.StartGame(package,PublishedConstructionProfileTests.Settings());var stamp=app.CaptureStatus();
        var command=new ColonisationCommandDto{ActorId=Human,CommandId="command.view.kit",Turn=stamp.Turn,Revision=stamp.CampaignRevision,Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}};
        var before=Json(app.ExportColonisationSave());Assert.True(app.EvaluateColonisation(command).Available);Assert.Equal(before,Json(app.ExportColonisationSave()));
        var fork=app.ForkInteraction(stamp.Turn,stamp.CampaignRevision);Assert.True(fork.SubmitColonisation(command).Accepted);Assert.Single(fork.CaptureColonisation(Human).Production);Assert.Equal(before,Json(app.ExportColonisationSave()));
        Assert.True(app.SubmitColonisation(command).Accepted);Assert.False(app.EvaluateColonisation(command).Available);
        var malformed=new ColonisationCommandDto{ActorId=Human,Action=ColonisationAction.EmbarkSettlers,Embark=new(){Materials=new(){null!}}};
        Assert.Equal(CampaignTacticalError.InvalidOrder,app.EvaluateColonisation(malformed).Error);
        Assert.Throws<ArgumentException>(()=>app.ForkInteraction(stamp.Turn,stamp.CampaignRevision));
    }
    [Fact] public void MineUsesRealNewHistoryAndNameButKitDoesNotCountAsInfrastructure()
    {
        var s=CampaignTacticalReviewSession.StartColonisation(ColonisationInventoryTests.Package(),PublishedConstructionProfileTests.Settings(250));
        var kit=Command(s,ColonisationAction.EnqueueKit);kit.EnqueueKit=new(){ColonyId="colony.sol"};Assert.True(s.SubmitColonisation(kit).Accepted);
        for(int i=0;i<16&&s.CaptureColonisation(Human).Production.Count>0;i++)Advance(s);
        Assert.Null(s.CaptureFrontierCommission(Human).Objectives.Single(o=>o.Id=="infrastructure").Evidence);
        var stamp=s.CaptureStatus();Assert.True(s.SubmitActivity(new(){ActorId=Human,CommandId="command.view.mine",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,
            Kind=CampaignActivityCommandKind.EnqueueStructure,TargetId="colony.sol",DefinitionId="infrastructure.frontier-mine.v1"}).Accepted);
        for(int i=0;i<32&&s.CaptureColonisation(Human).Production.Count>0;i++)Advance(s);
        var before=Json(s.ExportColonisationSave());var evidence=s.CaptureFrontierCommission(Human).Objectives.Single(o=>o.Id=="infrastructure").Evidence;
        Assert.NotNull(evidence);Assert.Contains("Mine",evidence.Label,StringComparison.OrdinalIgnoreCase);Assert.Equal(before,Json(s.ExportColonisationSave()));
        Assert.Equal(before,Json(CampaignTacticalReviewSession.LoadColonisation(ColonisationInventoryTests.Package(),s.ExportColonisationSave()).ExportColonisationSave()));
    }
}
