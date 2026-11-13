using System.Reflection;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;
namespace HobbyDS9.Simulation.Tests;
public sealed class ColonisationRosterJourneyTests
{
    static CampaignColonisationRosterPackageDto Package()=>CampaignTacticalReviewSession.CreateColonisationRosterPackage(ColonisationInventoryTests.Package());
    [Fact]public void ActualTwoActorGenesisHasOnlyExactNewAuthoredConstructionSourcesAndRejectsLegacyClone()
    {
        var p=Package();var s=CampaignTacticalReviewSession.StartColonisationRoster(p,PublishedConstructionProfileTests.Settings());
        // Read-only inspection proves source-template closure, rather than relying only on the external queue gate.
        var context=typeof(CampaignTacticalReviewSession).GetField("published",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(s)!;
        var genesis=(CampaignPlayableFleetPackageDto)context.GetType().GetField("Package",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(context)!;
        var scenario=genesis.Integrated!.Physical!.CorePackage!.Campaign!;var game=scenario.InitialState;var equipment=scenario.Definitions.AuthoredTacticalEquipment!;
        Assert.Equal(game.Civilizations!.Count,genesis.Integrated.ConstructionTemplates!.Count);
        foreach(var template in genesis.Integrated.ConstructionTemplates)
        {
            var ship=game.Ships!.Single(x=>x.ShipId==template.SourceProvisionShipId);var provision=equipment.InitialShipProvisions!.Single(x=>x.ShipId==ship.ShipId);
            var fit=equipment.Fits!.Single(x=>x.DefinitionId==provision.FitDefinitionId&&x.Revision==provision.FitRevision);
            Assert.Equal(ship.ShipDesignDefinitionId,template.ShipDesignDefinitionId);Assert.StartsWith("ship-design.colonisation-roster-v2.",template.ShipDesignDefinitionId);
            Assert.DoesNotContain("provisional-cruiser",fit.DefinitionId);Assert.Equal(30,ship.CurrentHull);
        }
        var old=p.BaseV1!.Activities!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Ships!.First(x=>x.OwnerCivilizationId!=Human).ShipDesignDefinitionId;
        var before=Json(s.ExportColonisationSave());var f=s.CaptureStatus();
        Assert.False(s.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.roster.legacy-clone",ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,Kind=CommandKind.EnqueueProduction,EnqueueProduction=new(){ColonyId="colony.sol",DefinitionId=old}}).Accepted);
        Assert.Equal(before,Json(s.ExportColonisationSave()));
    }
    [Fact]public void ActualStreamedTransportKitAndLoadedCohortRetainExactV2R1AcrossFullReplay()
    {
        var p=Package();var s=CampaignTacticalReviewSession.StartColonisationRoster(p,PublishedConstructionProfileTests.Settings(250));
        var kit=Command(s,ColonisationAction.EnqueueKit);kit.EnqueueKit=new(){ColonyId="colony.sol"};Assert.True(s.SubmitColonisation(kit).Accepted);
        var f=s.CaptureStatus();Assert.True(s.SubmitActivity(new(){ActorId=Human,CommandId="command.roster.actual.transport",ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId="blueprint.ready.galaxy-colony-transport",BlueprintRevision=1}).Accepted);
        for(int i=0;i<48&&!s.ExportColonisationSave().PublishedBuiltShips!.Any(b=>b.OwnerCivilizationId==Human);i++)Advance(s);
        var built=s.ExportColonisationSave();var own=Assert.Single(built.PublishedBuiltShips!,b=>b.OwnerCivilizationId==Human);Assert.Equal(1,own.BlueprintRevision);
        var home=s.CaptureColonisation(Human).Worlds.Single(w=>w.Id=="colony.sol");var cohort=home.Cohorts.First(c=>c.Population>=400);
        var load=Command(s,ColonisationAction.EmbarkSettlers);load.Embark=new(){ShipId=own.ShipId!,ColonyId=home.Id,CohortId=cohort.Id,Population=100,Food=20,Materials=new()};Assert.True(s.SubmitColonisation(load).Accepted,s.LastPhysicalFailure);
        var loaded=s.ExportColonisationSave();var manifest=Assert.Single(loaded.Colonisation!.ManifestRecords!);Assert.Equal(100,manifest.Passengers!.Population);Assert.Equal(1,manifest.Cargo!.Single(r=>r.ResourceDefinitionId=="resource.settlement-kit").Amount);Assert.Equal(20,manifest.Food);
        Assert.Equal(p.PackageHash,loaded.TemplatePackageHash);Assert.Equal(Json(loaded),Json(CampaignTacticalReviewSession.LoadColonisationRoster(p,loaded).ExportColonisationSave()));
        var unload=Command(s,ColonisationAction.UnloadTransport);unload.Unload=new(){ManifestId=manifest.ManifestId,ColonyId=home.Id};Assert.True(s.SubmitColonisation(unload).Accepted,s.LastPhysicalFailure);
        var final=s.ExportColonisationSave();Assert.Equal(Json(final),Json(CampaignTacticalReviewSession.LoadColonisationRoster(p,final).ExportColonisationSave()));Assert.Equal(SettlerManifestStage.Unloaded,Assert.Single(final.Colonisation!.ManifestRecords!).Stage);
    }
    [Fact]public void ActualAiMovementCombatAndAftermathReplayUsingTheV2AuthoredSources()
    {
        var p=Package();var s=CampaignTacticalReviewSession.StartColonisationRoster(p,PublishedConstructionProfileTests.Settings());
        for(int i=0;i<80&&s.CaptureStatus().Phase==SessionPhase.Planning;i++)Advance(s);
        Assert.Equal(SessionPhase.AwaitingCombat,s.CaptureStatus().Phase);var waiting=s.ExportColonisationSave();Assert.Equal(Json(waiting),Json(CampaignTacticalReviewSession.LoadColonisationRoster(p,waiting).ExportColonisationSave()));
        foreach(var id in s.Encounters(Human))for(int round=0;round<16&&!s.Observe(id,Human).Complete;round++)
        {
            var v=s.Observe(id,Human);Assert.True(s.CommitRound(new(){ActorId=Human,EncounterId=id,CommandId="command.roster.round."+v.Round,ExpectedTurn=v.Turn,ExpectedRound=v.Round,ExpectedBoundaryRevision=v.BoundaryRevision,Replacements=new()}).Accepted,s.LastPhysicalFailure);
        }
        Assert.All(s.Encounters(Human),id=>Assert.True(s.Observe(id,Human).Complete));var status=s.CaptureStatus();Assert.True(s.FinalizeBatch(status.Turn,status.CampaignRevision).Accepted,s.LastPhysicalFailure);
        var final=s.ExportColonisationSave();Assert.Equal(p.PackageHash,final.TemplatePackageHash);Assert.Equal(Json(final),Json(CampaignTacticalReviewSession.LoadColonisationRoster(p,final).ExportColonisationSave()));
    }
}