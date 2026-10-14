using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class IntegratedParityProjectionTests
{
    private const string A="civ.federation",B="civ.cardassian";
    private sealed class Fixture { public List<ShipClassDraftDto> Drafts { get; set; }=new(); }
    private static string Json<T>(T value)=>JsonSerializer.Serialize(value);
    private static void Publish(ICampaignIntegratedPlaytestSession app,string actor,string id)
    {
        var draft=JsonSerializer.Deserialize<Fixture>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"scenarios/ship-design-fixtures/fixture.json")))!.Drafts.Single(d=>d.ClassId=="class.escort");
        var stamp=app.CaptureStatus();Assert.True(app.SubmitStrategicCommand(new CommandEnvelope {ActorCivilizationId=actor,
            CommandId="command.parity."+id,ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CommandKind.PublishShipBlueprint,
            PublishShipBlueprint=new PublishShipBlueprintPayload {BlueprintId=id,Revision=1,Draft=draft}}).Accepted);
    }
    [Fact]
    public void OwnedReferenceNamesCoverWorldFactsWithoutExposingForeignWorlds()
    {
        var app=CampaignIntegratedPlaytestApplication.StartPackage(IntegratedPlaytestTests.Package());var before=Json(app.ExportIntegratedSave());
        var view=app.CaptureStrategy(A);var species=view.Colonies.SelectMany(c=>c.State.Cohorts).Select(c=>c.SpeciesDefinitionId).Distinct();
        foreach(var id in species)Assert.Contains(view.References.Species,s=>s.DefinitionId==id&&!string.IsNullOrWhiteSpace(s.DisplayName));
        Assert.All(view.References.Species,s=>Assert.Contains(s.DefinitionId,species));
        foreach(var id in view.Colonies.SelectMany(c=>c.State.Infrastructure).Select(i=>i.DefinitionId).Distinct())
            Assert.Contains(view.References.Infrastructure,i=>i.DefinitionId==id&&!string.IsNullOrWhiteSpace(i.DisplayName));
        Assert.Contains(view.References.Resources,r=>r.DefinitionId=="resource.dilithium"&&r.DisplayName=="Dilithium");
        Assert.NotEmpty(view.References.GovernancePolicies);Assert.DoesNotContain("colony.cardassia",Json(view));
        Assert.Equal(before,Json(app.ExportIntegratedSave()));
    }
    [Fact]
    public void RealSameSessionPublicationAppearsOnlyInOwnedLibraryAndStillCannotConstructV2()
    {
        var p=IntegratedPlaytestTests.Package();var app=CampaignIntegratedPlaytestApplication.StartPackage(p);
        var old=app.CaptureDesignLibrary(A);Assert.NotNull(old.Catalogue);Assert.True(old.CanPublish);Assert.Empty(old.Published);
        Publish(app,A,"blueprint.parity-alpha");Publish(app,B,"blueprint.parity-beta");
        var own=app.CaptureDesignLibrary(A);Assert.Single(own.Published);Assert.Equal(A,own.Published.Single().OwnerCivilizationId.Value);
        Assert.Empty(old.Published);Assert.False(own.CanConstructPublishedBlueprints);
        app=CampaignIntegratedPlaytestApplication.LoadSave(p,JsonSerializer.Deserialize<CampaignIntegratedPlaytestSaveDto>(Json(app.ExportIntegratedSave()))!);
        Assert.Equal(Json(own.Published),Json(app.CaptureDesignLibrary(A).Published));
        var stamp=app.CaptureStatus();var before=Json(app.ExportIntegratedSave());Assert.False(app.SubmitStrategicCommand(new CommandEnvelope {
            ActorCivilizationId=A,CommandId="command.parity.unsupported",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,
            Kind=CommandKind.EnqueueBlueprintConstruction,EnqueueBlueprintConstruction=new EnqueueBlueprintConstructionPayload {
                ColonyId="colony.sol",BlueprintId="blueprint.parity-alpha",Revision=1}}).Accepted);
        Assert.Equal(before,Json(app.ExportIntegratedSave()));
    }
    [Theory]
    [InlineData("civ.unknown")][InlineData("civ.cardassian")]
    public void UnknownAndUncontrolledActorsCannotReadLibrary(string actor)
    {
        var p=IntegratedPlaytestTests.Package();p.Physical!.CorePackage!.Campaign!.InitialState.Civilizations!.Single(c=>c.CivilizationId==B).ControllerKind=ControllerKind.AI;
        var app=CampaignIntegratedPlaytestApplication.StartPackage(p);Assert.Throws<ArgumentException>(()=>app.CaptureDesignLibrary(actor));
    }
}
