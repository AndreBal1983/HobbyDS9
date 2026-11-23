using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;
namespace HobbyDS9.Simulation.Tests;
public sealed class ColonisationRosterTests
{
    [Fact]public void BothActorsUseTheirActualOwnedR1SourceAndOriginalCountsWithoutBuildCredit()
    {
        var baseV1=ColonisationInventoryTests.Package();var p=CampaignColonisationApplication.CreateRosterPackage(baseV1);
        var old=CampaignColonisationApplication.StartGame(baseV1,PublishedConstructionProfileTests.Settings()).ExportColonisationSave();
        var app=CampaignColonisationApplication.StartRosterGame(p,PublishedConstructionProfileTests.Settings());var root=app.ExportColonisationSave();var game=Game(root);
        Assert.NotEqual(old.TemplatePackageHash,root.TemplatePackageHash);Assert.Equal(p.PackageHash,root.TemplatePackageHash);Assert.Equal(old.ProfileId,root.ProfileId);Assert.Equal(old.PolicyId,root.PolicyId);
        Assert.Equal(Game(old).Ships!.Select(s=>s.ShipId),game.Ships!.Select(s=>s.ShipId));Assert.Empty(root.PublishedBuiltShips!);Assert.Empty(root.ProductionHistory!);
        Assert.Equal(Json(Game(old).Colonies!),Json(game.Colonies!));Assert.Equal(Json(Game(old).Civilizations!),Json(game.Civilizations!));
        foreach(var ship in game.Ships!)
        {
            var id="blueprint.ready.constitution-explorer"+(ship.OwnerCivilizationId==Human?"":".ai");var source=game.Blueprints!.Single(b=>b.BlueprintId==id&&b.OwnerCivilizationId==ship.OwnerCivilizationId&&b.Revision==1);
            var supported=PublishedTacticalSupportCompiler.EvaluateColonisation(new ShipDesignCatalogueSnapshot(source.FrozenCatalogue!),CampaignBlueprints.Restore(source),CampaignBlueprints.FrozenAccess(source.FrozenCatalogue!),p.BaseV1!.Support!);
            Assert.True(supported.Supported);Assert.Equal(id,supported.Ship!.Tactical.BlueprintId);Assert.Equal(30,ship.CurrentHull);
            var fit=PublishedBuiltShipValidation.ToFit(supported.Ship.Tactical);var pool=root.Campaign!.Integrated!.Physical!.Core.Pools.Single(x=>x.ShipId==ship.ShipId);
            Assert.Equal(fit.DefinitionId,pool.FitId);Assert.All(pool.Shields,x=>Assert.Equal(0,x.Current));Assert.Empty(pool.Ammunition);
        }
        var before=Json(root);Assert.Equal(before,Json(CampaignColonisationApplication.LoadRosterSave(p,root).ExportColonisationSave()));
        Assert.Throws<ArgumentException>(()=>CampaignColonisationApplication.LoadSave(baseV1,root));Assert.Throws<ArgumentException>(()=>CampaignColonisationApplication.LoadRosterSave(p,old));
        // Caller metadata is not retained by an already-owned session.
        p.StartingRoster!.ClassId="changed";Assert.Equal(before,Json(app.ExportColonisationSave()));Assert.Throws<ArgumentException>(()=>CampaignColonisationApplication.StartRosterGame(p,PublishedConstructionProfileTests.Settings()));
    }
    [Fact]public void QueuedRosterGameReplaysExactAndWrongHashOrActorFitFails()
    {
        var p=CampaignColonisationApplication.CreateRosterPackage(ColonisationInventoryTests.Package());var app=CampaignColonisationApplication.StartRosterGame(p,PublishedConstructionProfileTests.Settings());var stamp=app.CaptureStatus();
        Assert.True(app.SubmitActivity(new(){ActorId=Human,CommandId="command.roster.defiant",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId="blueprint.ready.defiant-combat",BlueprintRevision=1}).Accepted);
        var root=app.ExportColonisationSave();Assert.Equal(107,Assert.Single(root.Production!).RequiredIndustry);Assert.Equal(Json(root),Json(CampaignColonisationApplication.LoadRosterSave(p,root).ExportColonisationSave()));
        var wrong=System.Text.Json.JsonSerializer.Deserialize<CampaignColonisationSaveDto>(Json(root))!;wrong.TemplatePackageHash=p.BaseV1!.PackageHash;Assert.Throws<ArgumentException>(()=>CampaignColonisationApplication.LoadRosterSave(p,wrong));
        wrong=System.Text.Json.JsonSerializer.Deserialize<CampaignColonisationSaveDto>(Json(root))!;var humanFit=wrong.Campaign!.Integrated!.Physical!.Core.Pools.Single(x=>x.ShipId=="ship.equipment-cruiser").FitId;wrong.Campaign.Integrated.Physical.Core.Pools.Single(x=>x.ShipId!="ship.equipment-cruiser").FitId=humanFit;Assert.Throws<ArgumentException>(()=>CampaignColonisationApplication.LoadRosterSave(p,wrong));
    }
    [Fact]public void NormalPublishedForkAdvanceRetainsRosterAuthorityAndRejectsMalformedWrappers()
    {
        var p=CampaignTacticalReviewSession.CreateColonisationRosterPackage(ColonisationInventoryTests.Package());
        var session=CampaignTacticalReviewSession.StartColonisationRoster(p,PublishedConstructionProfileTests.Settings());
        Advance(session);var root=session.ExportColonisationSave();
        Assert.Equal(Json(root),Json(CampaignTacticalReviewSession.LoadColonisationRoster(p,root).ExportColonisationSave()));
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.StartColonisationRoster(null!,PublishedConstructionProfileTests.Settings()));
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.StartColonisationRoster(new(),PublishedConstructionProfileTests.Settings()));
        var missing=CampaignTacticalReviewSession.CreateColonisationRosterPackage(ColonisationInventoryTests.Package());missing.StartingRoster=null;
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.StartColonisationRoster(missing,PublishedConstructionProfileTests.Settings()));
        var wrong=CampaignTacticalReviewSession.CreateColonisationRosterPackage(ColonisationInventoryTests.Package());wrong.StartingRoster!.Revision=2;
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.StartColonisationRoster(wrong,PublishedConstructionProfileTests.Settings()));
    }    [Fact]public void RehashedSupportedAlternativeHullCannotMasqueradeAsClosedConstitutionRoster()
    {
        var official=ColonisationInventoryTests.Package();
        var replacement=BlueprintCopies.Copy(official.ReadyClasses!.Single(d=>d.ClassId=="class.defiant-combat.v1"));replacement.ClassId="class.constitution-explorer.v1";
        var old=official.Activities!; var swapped=BlueprintCopies.Copy(official.ReadyClasses!.Single(d=>d.ClassId=="class.constitution-explorer.v1"));swapped.ClassId="class.defiant-combat.v1";
        var changedReady=old.ReadyClasses!.Select(d=>d.ClassId==replacement.ClassId?BlueprintCopies.Copy(replacement):d.ClassId==swapped.ClassId?BlueprintCopies.Copy(swapped):BlueprintCopies.Copy(d)).ToList();
        var activities=CampaignTacticalReviewSession.CreateActivitiesPackage(old.BaseTemplate!,old.Catalogue!,changedReady,old.Support!,old.Content!,old.RecordBudget);
        var ready=official.ReadyClasses!.Select(d=>d.ClassId==replacement.ClassId?BlueprintCopies.Copy(replacement):d.ClassId==swapped.ClassId?BlueprintCopies.Copy(swapped):BlueprintCopies.Copy(d)).ToList();
        // Both ordinary factories validate and rehash this supported custom content; no stale hash causes rejection.
        var custom=CampaignTacticalReviewSession.CreateColonisationPackage(activities,official.Catalogue!,ready,official.Support!,official.Settlement!);
        Assert.NotEqual(official.PackageHash,custom.PackageHash);
        var wrapped=CampaignTacticalReviewSession.CreateColonisationRosterPackage(custom);Assert.NotEqual(official.PackageHash,wrapped.PackageHash);
        var source=new ShipDesignCompiler(new ShipDesignCatalogueSnapshot(custom.Catalogue!)).Publish(replacement,CampaignBlueprints.FrozenAccess(custom.Catalogue!),"blueprint.ready.constitution-explorer",1);
        Assert.True(source.Published);var supported=PublishedTacticalSupportCompiler.EvaluateColonisation(new ShipDesignCatalogueSnapshot(custom.Catalogue!),source.Blueprint!,CampaignBlueprints.FrozenAccess(custom.Catalogue!),custom.Support!);
        Assert.True(supported.Supported);Assert.Equal("hull.defiant-combat.v1",source.Blueprint!.Fit!.HullVersionId);
        var error=Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.StartColonisationRoster(wrapped,PublishedConstructionProfileTests.Settings()));
        Assert.Contains("closed roster class/hull",error.Message,StringComparison.Ordinal);
    }
}
