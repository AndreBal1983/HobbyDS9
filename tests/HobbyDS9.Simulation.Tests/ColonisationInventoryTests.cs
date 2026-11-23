using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ColonisationInventoryTests
{
    internal static CampaignColonisationPackageDto Package()=>CampaignTacticalReviewSession.CreateColonisationPackage(CampaignActivitiesTests.Package(),
        ColonisationContentTests.Read<HobbyDS9.Contracts.ShipDesign.ShipDesignCatalogueDto>("catalogue.json"),
        ColonisationContentTests.Read<HobbyDS9.Contracts.ShipDesign.ShipClassDraftDto[]>("ready-classes.json"),
        ColonisationContentTests.Read<HobbyDS9.Contracts.ShipDesign.ColonisationSupportCatalogueDto>("support.json"),ColonisationContentTests.Read<ColonisationPolicyDto>("settlement.json"));
    [Fact]public void PublicNewFactoryStartsFourCanonicalPublicationsWithoutChangingAnyOldProfile()
    {
        var package=Package();var old=CampaignTacticalReviewSession.StartStreamedActivities(CampaignTacticalReviewSession.CreateStreamedActivitiesPackage(package.Activities!),PublishedConstructionProfileTests.Settings());
        var oldBefore=JsonSerializer.Serialize(old.ExportStreamedActivitiesSave());var before=JsonSerializer.Serialize(package);
        var session=CampaignTacticalReviewSession.StartColonisation(package,PublishedConstructionProfileTests.Settings());
        Assert.Equal(SessionPhase.Planning,session.CaptureStatus().Phase);
        var game=session.CaptureCampaign();Assert.Equal(CampaignColonisationIdentity.Rules,game.RulesVersion);
        var initial=session.ExportColonisationSave();var frozen=JsonSerializer.Serialize(initial.FrozenPlanets);
        var stamp=session.CaptureStatus();var queued=session.SubmitActivity(new CampaignActivityCommandDto{ActorId="civ.federation",CommandId="command.colonisation.transport",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,
            Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId="blueprint.ready.galaxy-colony-transport",BlueprintRevision=1});
        Assert.True(queued.Accepted,queued.Error+" "+session.LastPhysicalFailure);Assert.Single(session.ExportColonisationSave().Production!);
        Assert.Equal(initial.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Civilizations!.Single(c=>c.CivilizationId=="civ.federation").Credits,
            session.ExportColonisationSave().Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Civilizations!.Single(c=>c.CivilizationId=="civ.federation").Credits);
        stamp=session.CaptureStatus();var advance=session.AdvanceTurn(stamp.Turn,stamp.CampaignRevision);Assert.True(advance.Accepted,advance.Error+" "+session.LastPhysicalFailure);
        Assert.Equal(frozen,JsonSerializer.Serialize(session.ExportColonisationSave().FrozenPlanets));
        Assert.Equal(before,JsonSerializer.Serialize(package));Assert.Equal(oldBefore,JsonSerializer.Serialize(old.ExportStreamedActivitiesSave()));
        Assert.Throws<ArgumentException>(()=>session.ExportStreamedActivitiesSave());Assert.Throws<ArgumentException>(()=>session.ExportActivitiesSave());
    }
    [Theory][InlineData(30)][InlineData(250)]
    public void FrozenInventoryCoversEverySystemWithActualVariablePlanetsAndNearbyLawfulOpportunities(int size)
    {
        var p=CampaignActivitiesTests.Package();var before=JsonSerializer.Serialize(p);
        var catalog=p.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
        var map=NaturalGalaxyGenerator.Generate(PublishedConstructionProfileTests.Settings(size),catalog);
        var activities=CampaignTacticalReviewSession.SeedColonisationActivities(p,map);var minorBefore=JsonSerializer.Serialize(activities.Minors);
        var built=GeneratedCampaignFactory.BuildColonisation(p.BaseTemplate,map,"frozen-wrapper-test",activities,ColonisationContentTests.Read<ColonisationPolicyDto>("settlement.json"));
        var scenario=built.Package.Integrated!.Physical!.CorePackage!.Campaign!;var d=scenario.Definitions;var game=scenario.InitialState;
        Assert.Equal(CampaignColonisationIdentity.Rules,scenario.RulesVersion);Assert.All(game.StarSystems!,s=>Assert.Null(s.OwnerCivilizationId));
        Assert.All(map.Layout.Systems,s=>Assert.Contains(game.Planets!,p=>p.SystemId==s.SystemId));
        Assert.InRange(game.Planets!.Count,size,size+40);Assert.Equal(game.Planets.Count,built.Inventory.Bodies!.Count);
        Assert.Equal(game.Planets.Count,built.Inventory.Bodies.Select(b=>b.BodyId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(activities.Bodies!,b=>Assert.Single(built.Inventory.Bodies,x=>x.BodyId==b.BodyId&&x.SystemId==b.SystemId&&x.MinorActorId==b.GovernmentActorId));
        Assert.Equal(2,built.Inventory.Bodies.Count(b=>b.MinorActorId!=null));Assert.Equal(minorBefore,JsonSerializer.Serialize(activities.Minors));
        Assert.True(d.Planets!.Select(p=>p.PlanetClass).Distinct(StringComparer.Ordinal).Count()>=5);
        Assert.Contains(game.Planets,p=>p.RemainingDeposits!.Any(r=>r.Amount>0));
        Assert.All(game.Planets.Where(p=>p.PlanetId.StartsWith("planet.colonisation.",StringComparison.Ordinal)),p=>{
            var definition=d.Planets!.Single(d=>d.DefinitionId==p.DefinitionId);
            Assert.Equal(JsonSerializer.Serialize(definition.Deposits),JsonSerializer.Serialize(p.RemainingDeposits));
            Assert.Equal(definition.PlanetClass=="Vacuum"?0:1000,definition.Habitats!.Sum(h=>h.NaturalCapacity));
        });
        var ownership=PlanetaryOwnershipContext.CreateForColonisation(d,game,built.Inventory.PlanetControls!,activities.Minors!);
        Assert.True(FoundationValidator.ValidatePlanetaryScenario(scenario,ownership).IsValid);
        Assert.False(FoundationValidator.ValidateScenario(scenario).IsValid);
        foreach(var actor in game.Civilizations!)
        {
            var home=map.Layout.HomeCandidates.Single(h=>h.CivilizationDefinitionId==actor.DefinitionId);
            var distances=GeneratedGalaxyData.Distances(GeneratedGalaxyData.Neighbors(map.Layout.Regions,map.Layout.Connections),home.NodeId);
            var cohort=game.Colonies!.First(c=>c.OwnerCivilizationId==actor.CivilizationId).Cohorts![0];var species=d.Species!.Single(s=>s.DefinitionId==cohort.SpeciesDefinitionId);
            var suitable=game.Planets.Where(p=>built.Inventory.PlanetControls!.Single(c=>c.PlanetId==p.PlanetId).Kind==PlanetControllerKind.Unclaimed)
                .Where(p=>distances[map.Layout.Systems.Single(s=>s.SystemId==p.SystemId).NodeId]>=2&&distances[map.Layout.Systems.Single(s=>s.SystemId==p.SystemId).NodeId]<=4)
                .Where(p=>d.Planets!.Single(x=>x.DefinitionId==p.DefinitionId).Habitats!.Any(h=>h.NaturalCapacity>=100&&WorldEconomy.Suitability(species,d.Planets!.Single(x=>x.DefinitionId==p.DefinitionId),h.Kind)>0)).ToList();
            Assert.True(suitable.Select(p=>p.SystemId).Distinct(StringComparer.Ordinal).Count()>=2);
        }
        var again=GeneratedCampaignFactory.BuildColonisation(p.BaseTemplate,map,"frozen-wrapper-test",activities,ColonisationContentTests.Read<ColonisationPolicyDto>("settlement.json"));
        Assert.Equal(JsonSerializer.Serialize(built.Package),JsonSerializer.Serialize(again.Package));
        Assert.Equal(JsonSerializer.Serialize(built.Inventory),JsonSerializer.Serialize(again.Inventory));
        Assert.Equal(before,JsonSerializer.Serialize(p));
        built.Inventory.PlanetControls![0].ActorId="civ.forged";Assert.NotEqual(JsonSerializer.Serialize(built.Inventory),JsonSerializer.Serialize(again.Inventory));
    }
}
