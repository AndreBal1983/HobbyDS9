using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.UnityClient;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class CampaignDesignerTests
{
    private static readonly EntityId Actor=new("civ.federation"), Colony=new("colony.sol");
    private static ScenarioDto Content()
    {
        var s=VerticalSliceFixture.Scenario(humans:false,weighted:true);
        s.InitialState.Civilizations!.Single(c=>c.CivilizationId==Actor.Value).Credits=1000;
        PopulationFixture.SetDilithium(s.InitialState.Colonies!.Single(c=>c.ColonyId==Colony.Value),100);
        return s;
    }
    private static StrategicClient Start(ScenarioDto? s=null)
    {
        s??=Content();var content=s;
        return new(CampaignApplication.StartScenario(content),save=>CampaignApplication.LoadSave(content,save));
    }
    private static string Json(StrategicClient c)=>JsonSerializer.Serialize(c.ExportSave());
    private static DesignerFit Fit()
    {
        var fixture=JsonSerializer.Deserialize<DesignerFixture>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"scenarios/ship-design-fixtures/fixture.json")))!;
        return new(fixture.Drafts.Single(d=>d.ClassId=="class.escort"));
    }
    private static CampaignDesigner Workspace(StrategicClient c)
    {
        var w=new CampaignDesigner(c,Actor);var fit=Fit().ToDto();fit.ClassId="class."+w.BlueprintId;
        w.Draft!.StartFit(new(fit));return w;
    }
    [Fact]
    public void CampaignProviderReadsActualCostsAndAccessWithoutFixtureCatalogue()
    {
        var s=Content();s.Definitions.ShipDesignCatalogue!.Components!.Single(c=>c.VersionId=="component.energy-bank.v1").Cost!.Credits=123;
        s.Definitions.ShipDesignVersionAccess!.Single(g=>g.VersionId=="component.energy-bank.v1").RequiredEngineeringApplicationDefinitionIds!.Add("engineering.advanced-starship-engineering");
        var c=Start(s);var w=Workspace(c);var provider=Assert.IsType<ApplicationShipDesignPreviewProvider>(w.Draft!.Provider);
        Assert.False(provider.Available("component.energy-bank.v1"));
        Assert.Contains(provider.ComponentFacts("component.energy-bank.v1",DesignerProfile.Combat),m=>m.Caption=="Credits / industry" && m.Value.StartsWith("123"));
        Assert.False(w.CanPublish);Assert.Contains(w.Draft.Preview().Issues,i=>i.Explanation.Contains("unavailable",StringComparison.OrdinalIgnoreCase));
        Assert.Equal(CommandErrorCode.ShipDesignVersionUnavailable,w.Publish().ErrorCode);
    }
    [Fact]
    public void EngineeringUnlockRefreshesAccessWithoutDiscardingDraftOrProfile()
    {
        var s=Content();s.Definitions.ShipDesignVersionAccess!.Single(g=>g.VersionId=="component.energy-bank.v1").RequiredEngineeringApplicationDefinitionIds!.Add("engineering.advanced-starship-engineering");
        foreach(var app in s.Definitions.EngineeringApplications!) {app.ScienceRequirements=new();app.BaseEngineeringCost=600;}
        var c=Start(s);var w=Workspace(c);w.Draft!.Rename("Kept");w.Draft.SelectProfile(DesignerProfile.Combat);var before=w.Draft.Current;
        Assert.True(c.Engineering(c.Snapshot,Actor,new("engineering.advanced-starship-engineering")).Accepted);
        for(var i=0;i<5 && !c.Snapshot.Campaign.Civilizations.Single(x=>x.CivilizationId==Actor).MatureEngineeringApplicationDefinitionIds.Any();i++)
            CompleteTurn(c);
        w.Refresh();Assert.True(w.Draft.Current.SameAs(before));Assert.Equal(DesignerProfile.Combat,w.Draft.Profile);
        Assert.True(((IShipDesignCatalogueFacts)w.Draft.Provider).Available("component.energy-bank.v1"));Assert.True(w.CanPublish);
    }
    [Theory]
    [InlineData("revision",CommandErrorCode.StaleRevision)]
    [InlineData("turn",CommandErrorCode.StaleTurn)]
    [InlineData("power",CommandErrorCode.InvalidShipBlueprint)]
    [InlineData("capacity",CommandErrorCode.InvalidShipBlueprint)]
    public void RejectedPublicationRetainsCompleteDraftAndDoesNotRefreshAndRetry(string problem,CommandErrorCode expected)
    {
        var c=Start();var w=Workspace(c);
        if(problem=="revision")Assert.True(c.Enqueue(c.Snapshot,Actor,Colony,new("ship-design.scout")).Accepted);
        if(problem=="turn")CompleteTurn(c);
        if(problem=="power")w.Draft!.SetPower("component.command.v1",100,100);
        if(problem=="capacity")w.Draft!.SetQuantity("component.energy-bank.v1",100);
        var fit=w.Draft!.Current;var before=Json(c);var observed=w.Observed;
        var result=w.Publish();Assert.Equal(expected,result.ErrorCode);Assert.Equal(before,Json(c));Assert.True(w.Draft.Current.SameAs(fit));
        Assert.Same(observed,w.Observed);Assert.True(w.RefreshRequired);Assert.False(w.CanPublish);Assert.Contains(expected.ToString(),w.Notice);
        Assert.Throws<InvalidOperationException>(()=>w.Publish());Assert.Equal(before,Json(c));
        w.Refresh();Assert.True(w.Draft.Current.SameAs(fit));Assert.False(w.RefreshRequired);
    }
    [Fact]
    public void PublicationUsesUniqueCanonicalIdentityAndOnlyAcceptedResultMarksPublished()
    {
        var c=Start();var w=Workspace(c);w.Draft!.Rename("Player class");var before=c.ExportSave();
        Assert.True(w.CanPublish);Assert.True(w.Publish().Accepted);var record=Assert.Single(c.Blueprints());
        Assert.Equal(w.BlueprintId,record.Blueprint.BlueprintId);Assert.Equal(1,record.Blueprint.Revision);
        Assert.Equal("Player class",record.Blueprint.Fit.DisplayName);Assert.False(w.Draft.Modified);Assert.False(w.CanPublish);
        Assert.Throws<InvalidOperationException>(()=>w.Publish());Assert.Single(c.Blueprints());
        Assert.Equal(before.GameState.RngState.State,c.ExportSave().GameState.RngState.State);
        Assert.Equal(before.GameState.NextEntitySequence,c.ExportSave().GameState.NextEntitySequence);
        Assert.Equal(before.GameState.Civilizations!.Single(x=>x.CivilizationId==Actor.Value).Credits,c.Snapshot.Campaign.Civilizations.Single(x=>x.CivilizationId==Actor).Credits);
        Assert.NotEqual(w.BlueprintId,new CampaignDesigner(c,Actor).BlueprintId);
    }
    [Fact]
    public void EditingHistoricalRevisionIsDetachedAndPublishesNextLatestWithStableClassIdentity()
    {
        var c=Start();var w=Workspace(c);Assert.True(w.Publish().Accepted);var first=JsonSerializer.Serialize(c.ExportSave().GameState.Blueprints![0]);
        w.Draft!.Rename("Second");Assert.True(w.Publish().Accepted);
        w.Edit(w.History.Single(b=>b.Blueprint.Revision==1));
        Assert.Equal(3,w.NextRevision);Assert.Equal("Escort",w.Draft.Current.Name);w.Draft.Rename("Third");Assert.True(w.Publish().Accepted);
        Assert.Equal(first,JsonSerializer.Serialize(c.ExportSave().GameState.Blueprints![0]));
        Assert.Equal(new[]{1,2,3},c.Blueprints().Select(b=>b.Blueprint.Revision));
        Assert.Single(c.Blueprints().Select(b=>b.Blueprint.Fit.ClassId).Distinct());
    }
    [Fact]
    public void StaleSameStampAfterLoadCannotPublishOrQueueIntoReplacementSession()
    {
        var c=Start();var w=Workspace(c);Assert.True(w.Publish().Accepted);w.Draft!.Rename("Unsaved");
        var production=new ProductionOverview(c,c.Snapshot);var option=Assert.Single(production.Blueprints(Colony));
        var fit=w.Draft.Current;var save=c.ExportSave();c.Load(save);var before=Json(c);w.Refresh();
        Assert.True(w.SessionReplaced);Assert.False(w.CanPublish);Assert.True(w.Draft.Current.SameAs(fit));Assert.Contains("Campaign replaced",w.Notice);
        Assert.Throws<InvalidOperationException>(()=>w.Publish());Assert.Throws<InvalidOperationException>(()=>production.EnqueueBlueprint(Colony,option));
        Assert.Equal(before,Json(c));Assert.Single(new CampaignDesigner(c,Actor).History);
    }
    [Fact]
    public void FailedLoadRetainsUsableWorkspace()
    {
        var c=Start();var w=Workspace(c);var save=c.ExportSave();save.ContentHash="wrong";
        Assert.Throws<ArgumentException>(()=>c.Load(save));Assert.False(w.SessionReplaced);Assert.True(w.CanPublish);
    }
    [Theory]
    [InlineData("credits",CommandErrorCode.InsufficientCredits)]
    [InlineData("resource",CommandErrorCode.InsufficientStrategicResource)]
    [InlineData("yard",CommandErrorCode.ShipbuildingUnavailable)]
    public void ConstructionProjectionAndFailedSubmissionAgreeWithoutPayment(string problem,CommandErrorCode expected)
    {
        var s=Content();if(problem=="credits")s.InitialState.Civilizations!.Single(x=>x.CivilizationId==Actor.Value).Credits=0;
        if(problem=="resource")PopulationFixture.SetDilithium(s.InitialState.Colonies!.Single(x=>x.ColonyId==Colony.Value),0);
        if(problem=="yard")s.InitialState.Colonies!.Single(x=>x.ColonyId==Colony.Value).Infrastructure!.RemoveAll(i=>i.DefinitionId=="infrastructure.shipyard");
        var c=Start(s);var w=Workspace(c);Assert.True(w.Publish().Accepted);
        var model=new ProductionOverview(c,c.Snapshot);var option=Assert.Single(model.Blueprints(Colony));var before=Json(c);
        Assert.False(option.CanBuild);Assert.Equal(expected,option.ErrorCode);Assert.Equal(expected,model.EnqueueBlueprint(Colony,option).ErrorCode);Assert.Equal(before,Json(c));
    }
    [Fact]
    public void ConstructionPinsRevisionChargesOnceBuildsOnRealTurnsAndSurvivesSave()
    {
        var c=Start();var w=Workspace(c);Assert.True(w.Publish().Accepted);
        var model=new ProductionOverview(c,c.Snapshot);var option=Assert.Single(model.Blueprints(Colony));Assert.True(option.CanBuild);
        var credits=c.Snapshot.Campaign.Civilizations.Single(x=>x.CivilizationId==Actor).Credits;
        Assert.True(model.EnqueueBlueprint(Colony,option).Accepted);var after=Json(c);
        Assert.Throws<InvalidOperationException>(()=>model.EnqueueBlueprint(Colony,option));Assert.Equal(after,Json(c));
        Assert.Equal(credits-option.Blueprint.Blueprint.Performance.Cost!.Credits,c.Snapshot.Campaign.Civilizations.Single(x=>x.CivilizationId==Actor).Credits);
        w.Refresh();w.Draft!.Rename("Newer");Assert.True(w.Publish().Accepted);
        Assert.Equal(1,c.Snapshot.Campaign.Colonies.Single(x=>x.ColonyId==Colony).ProductionQueue.Single().Blueprint!.Revision);
        for(var i=0;i<200 && !c.Snapshot.Campaign.Ships.Any(x=>x.Blueprint!=null);i++)CompleteTurn(c);
        var ship=Assert.Single(c.Snapshot.Campaign.Ships,x=>x.Blueprint!=null);
        Assert.Equal(w.BlueprintId,ship.Blueprint!.BlueprintId);Assert.Equal(1,ship.Blueprint.Revision);Assert.Equal("Escort",ship.ResolvedDesign!.DisplayName);
        Assert.Equal(option.Blueprint.Blueprint.Performance.Compiled!.MovementBudget,ship.ResolvedDesign.MovementBudget);
        var fleetShips=c.Snapshot.Campaign.Ships.Where(x=>x.FleetId==ship.FleetId).ToList();
        Assert.Equal(fleetShips.Min(x=>x.ResolvedDesign!.MovementBudget),c.Snapshot.FleetTravel.Single(x=>x.FleetId==ship.FleetId.Value).MovementBudget);
        var saved=Json(c);c.Load(c.ExportSave());Assert.Equal(saved,Json(c));Assert.Equal(1,c.Snapshot.Campaign.Ships.Single(x=>x.Blueprint!=null).Blueprint!.Revision);
    }
    [Fact]
    public void BlueprintOptionsExcludeForeignRecordsAndColoniesAndStaticBuildingStillWorks()
    {
        var c=Start();var foreign=new EntityId("civ.cardassian");var observed=c.Snapshot;
        Assert.Equal(CommandErrorCode.ActorNotControlled,c.PublishBlueprint(observed,foreign,"blueprint.foreign",1,Fit().ToDto()).ErrorCode);
        var w=Workspace(c);Assert.True(w.Publish().Accepted);var model=new ProductionOverview(c,c.Snapshot);
        Assert.All(model.Worlds,x=>Assert.Equal(Actor,x.OwnerCivilizationId));
        Assert.Empty(model.Blueprints(new("colony.cardassia")));
        Assert.Equal(CommandErrorCode.ColonyNotOwned,c.EnqueueBlueprint(c.Snapshot,Actor,new("colony.cardassia"),w.BlueprintId,1).ErrorCode);
        Assert.True(c.Enqueue(c.Snapshot,Actor,Colony,new("ship-design.scout")).Accepted);
    }
    [Fact]
    public void UnavailableCatalogueIsSafeAndEmpty()
    {
        var s=Content();s.Definitions.ShipDesignCatalogue=null;s.Definitions.ShipDesignVersionAccess!.Clear();
        var w=new CampaignDesigner(Start(s),Actor);Assert.Null(w.Draft);Assert.False(w.CanPublish);Assert.Contains("unavailable",w.Notice);
    }
    [Fact]
    public void UnavailableHullCannotBePublished()
    {
        var s=Content();s.Definitions.ShipDesignVersionAccess!.Single(g=>g.VersionId==Fit().HullId)
            .RequiredEngineeringApplicationDefinitionIds!.Add("engineering.advanced-starship-engineering");
        var c=Start(s);var w=Workspace(c);var before=Json(c);
        Assert.False(w.CanPublish);Assert.Equal(CommandErrorCode.ShipDesignVersionUnavailable,w.Publish().ErrorCode);
        Assert.Equal(before,Json(c));Assert.NotEmpty(w.Draft!.Current.Installed);
    }
    [Theory]
    [InlineData("installations")]
    [InlineData("allocations")]
    [InlineData("quantity")]
    public void MalformedFitCrossesDetachedCommandBoundaryAndIsRejectedTransactionally(string error)
    {
        var c=Start();var d=Fit().ToDto();
        if(error=="installations")d.Installations=null;
        if(error=="allocations")d.Profiles![0].Allocations=null;
        if(error=="quantity")d.Installations![0].Quantity=-1;
        var before=Json(c);var result=c.PublishBlueprint(c.Snapshot,Actor,"blueprint.invalid",1,d);
        Assert.False(result.Accepted);Assert.Equal(before,Json(c));Assert.Contains(result.ErrorCode.ToString(),CommandFeedback.Describe(result));
    }
    [Fact]
    public void DuplicateRevisionAndForeignOwnershipAreRejectedAtLiveBoundary()
    {
        var s=Content();s.InitialState.Civilizations!.Single(c=>c.CivilizationId=="civ.cardassian").ControllerKind=ControllerKind.Human;
        var c=Start(s);var w=Workspace(c);Assert.True(w.Publish().Accepted);var before=Json(c);
        Assert.Equal(CommandErrorCode.BlueprintRevisionConflict,c.PublishBlueprint(c.Snapshot,Actor,w.BlueprintId,1,w.Draft!.Current.ToDto()).ErrorCode);
        Assert.Equal(CommandErrorCode.BlueprintNotOwned,c.PublishBlueprint(c.Snapshot,new("civ.cardassian"),w.BlueprintId,2,w.Draft.Current.ToDto()).ErrorCode);
        Assert.Equal(before,Json(c));Assert.Empty(new ProductionOverview(c,c.Snapshot,new EntityId("civ.cardassian")).Blueprints(new("colony.cardassia")));
    }
    [Fact]
    public void PendingCombatBlocksPreviewActionsAndAuthoritativeCommands()
    {
        var s=Content();s.InitialState.Fleets!.First(f=>f.OwnerCivilizationId=="civ.cardassian").LocationNodeId="nav.sol";
        var c=Start(s);var w=Workspace(c);Assert.True(w.Publish().Accepted);
        Assert.Equal(TurnStatus.AwaitingCombat,c.EndTurn(c.Snapshot).Status);w.Refresh();w.Draft!.Rename("Pending");
        Assert.False(w.CanPublish);var before=Json(c);Assert.Equal(CommandErrorCode.CombatPending,w.Publish().ErrorCode);
        var option=Assert.Single(new ProductionOverview(c,c.Snapshot,Actor).Blueprints(Colony));
        Assert.Equal(CommandErrorCode.CombatPending,option.ErrorCode);
        Assert.Equal(CommandErrorCode.CombatPending,c.EnqueueBlueprint(c.Snapshot,Actor,Colony,w.BlueprintId,1).ErrorCode);Assert.Equal(before,Json(c));
    }
    [Fact]
    public void NewClassAndHistoricalSelectionRequireDiscardAndKeepPreservesIdentity()
    {
        var c=Start();var w=Workspace(c);Assert.True(w.Publish().Accepted);w.Draft!.Rename("Kept");var id=w.BlueprintId;
        w.NewClass();Assert.True(w.Draft.ConfirmationPending);w.Draft.CancelDiscard();Assert.Equal(id,w.BlueprintId);Assert.Equal("Kept",w.Draft.Current.Name);
        w.NewClass();w.Draft.ConfirmDiscard();Assert.NotEqual(id,w.BlueprintId);Assert.Equal(1,w.NextRevision);Assert.Empty(w.Draft.Current.Installed);
    }
    private static void CompleteTurn(StrategicClient c)
    {
        var result=c.EndTurn(c.Snapshot);
        if(result.Status==TurnStatus.AwaitingCombat)
        {
            foreach(var request in c.Pending())c.Resolve(c.Snapshot,request);
            Assert.Equal(TurnStatus.Completed,c.FinalizeCombats(c.Snapshot).Status);
        }
        else Assert.Equal(TurnStatus.Completed,result.Status);
    }
}
