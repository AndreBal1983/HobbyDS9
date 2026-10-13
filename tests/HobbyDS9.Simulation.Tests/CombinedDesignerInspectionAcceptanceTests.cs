using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityClient;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

// Real client models linked into .NET: this is headless evidence, not Unity UI acceptance.
public sealed class CombinedDesignerInspectionAcceptanceTests
{
    private static readonly EntityId Actor = new("civ.federation"), Colony = new("colony.sol");
    private sealed class Campaign
    {
        internal ScenarioDto Scenario { get; }
        internal ICampaignSession Active { get; private set; }
        internal StrategicClient Client { get; }
        internal Campaign(ScenarioDto? scenario = null)
        {
            var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "scenarios/ds9-slice/scenario.json"));
            Scenario = scenario ?? JsonSerializer.Deserialize<ScenarioDto>(bytes)!;
            Scenario.ContentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            Active = CampaignApplication.StartScenario(Scenario);
            Client = new StrategicClient(Active, save => Active = CampaignApplication.LoadSave(Scenario, save));
        }
        internal string Saved => JsonSerializer.Serialize(Client.ExportSave());
        internal CampaignTacticalV2EligibilitySnapshot Inspect()
        {
            var before = Saved;
            var report = ((ICampaignTacticalV2Inspection)Active).InspectCampaignTacticalV2(Actor.Value);
            Assert.Equal(before, Saved); Assert.False(report.CanActivate);
            Assert.Equal(Client.Snapshot.Campaign.StateRevision, report.StateRevision);
            Assert.Equal(Client.Snapshot.Campaign.TurnNumber, report.TurnNumber);
            Assert.All(report.OwnedShips, source => Assert.False(source.CanProjectTacticalV2));
            return report;
        }
    }

    private static CampaignDesigner Courier(Campaign campaign)
    {
        var workspace = new CampaignDesigner(campaign.Client, Actor);
        var draft = workspace.Draft!;
        Assert.Empty(draft.Current.Installed); // The ordinary campaign starter, not an offline fixture.
        foreach (var component in new[] { "component.command.v1", "component.life-support.v1",
            "component.propulsion.v1", "component.reactor.v1" }) draft.Add(component);
        draft.SetCrew(30); draft.Rename("Combined Courier");
        foreach (var profile in Enum.GetValues<DesignerProfile>())
        {
            draft.SelectProfile(profile);
            draft.SetPower("component.command.v1", 4, 4); draft.SetPower("component.life-support.v1", 6, 6);
            var propulsion = profile == DesignerProfile.Cruise ? 30 : profile == DesignerProfile.Combat ? 12 : 8;
            draft.SetPower("component.propulsion.v1", propulsion, propulsion);
        }
        draft.SelectProfile(DesignerProfile.Cruise);
        return workspace;
    }

    private static void CompleteTurn(StrategicClient client)
    {
        var turn = client.EndTurn(client.Snapshot);
        if (turn.Status == TurnStatus.AwaitingCombat)
        {
            foreach (var encounter in client.Pending()) client.Resolve(client.Snapshot, encounter);
            Assert.Equal(TurnStatus.Completed, client.FinalizeCombats(client.Snapshot).Status);
        }
        else Assert.Equal(TurnStatus.Completed, turn.Status);
    }

    [Fact]
    public void RealDesignerPublishesBuildsHistoricalRevisionAndInspectionSurvivesSaveLoad()
    {
        var campaign = new Campaign(); var client = campaign.Client; var workspace = Courier(campaign);
        Assert.True(workspace.CanPublish); var before = client.ExportSave();
        Assert.True(workspace.Publish().Accepted);
        Assert.Equal(before.GameState.RngState.State, client.ExportSave().GameState.RngState.State);
        Assert.Equal(before.GameState.NextEntitySequence, client.ExportSave().GameState.NextEntitySequence);
        var revisionOne = Assert.Single(campaign.Inspect().OwnedBlueprintRevisions);
        var frozen = JsonSerializer.Serialize(revisionOne.Blueprint);
        var production = new ProductionOverview(client, client.Snapshot, Actor);
        var option = Assert.Single(production.Blueprints(Colony));
        Assert.True(option.CanBuild); Assert.True(production.EnqueueBlueprint(Colony, option).Accepted);
        var credits = before.GameState.Civilizations!.Single(c => c.CivilizationId == Actor.Value).Credits;
        Assert.Equal(credits - revisionOne.Blueprint.Performance.Cost!.Credits,
            client.Snapshot.Campaign.Civilizations.Single(c => c.CivilizationId == Actor).Credits);
        workspace.Refresh(); workspace.Draft!.Rename("Combined Courier II"); Assert.True(workspace.Publish().Accepted);
        Assert.Equal(1, Assert.Single(client.Snapshot.Campaign.Colonies.Single(c => c.ColonyId == Colony).ProductionQueue).Blueprint!.Revision);
        for (var i = 0; i < 5; i++) { CompleteTurn(client); campaign.Inspect(); }
        var source = Assert.Single(campaign.Inspect().OwnedShips, s => s.Ship.Blueprint != null);
        Assert.Equal(workspace.BlueprintId, source.Ship.Blueprint!.BlueprintId); Assert.Equal(1, source.Ship.Blueprint.Revision);
        Assert.Equal("Combined Courier", source.Ship.ResolvedDesign!.DisplayName);
        Assert.Equal(frozen, JsonSerializer.Serialize(source.FrozenBlueprint!.Blueprint));
        Assert.Equal(source.Ship.ResolvedDesign.MaxHull, source.Ship.CurrentHull);
        var saved = campaign.Saved; var inspection = JsonSerializer.Serialize(campaign.Inspect());
        client.Load(JsonSerializer.Deserialize<SaveEnvelopeDto>(saved)!);
        Assert.Equal(saved, campaign.Saved); Assert.Equal(inspection, JsonSerializer.Serialize(campaign.Inspect()));
        Assert.Equal(14, client.ExportSave().SaveFormatVersion); Assert.Equal(10, campaign.Scenario.Definitions.ContentFormatVersion);
    }

    [Fact]
    public void InspectionDoesNotRepairAStaleDesignerStampOrAutomaticallyRetryPublication()
    {
        var campaign = new Campaign(); var workspace = Courier(campaign); var observed = workspace.Observed;
        Assert.True(campaign.Client.Enqueue(campaign.Client.Snapshot, Actor, Colony, new("ship-design.scout")).Accepted);
        campaign.Inspect(); Assert.Same(observed, workspace.Observed);
        var before = campaign.Saved; var fit = workspace.Draft!.Current;
        Assert.Equal(CommandErrorCode.StaleRevision, workspace.Publish().ErrorCode);
        campaign.Inspect(); Assert.Equal(before, campaign.Saved); Assert.True(workspace.Draft.Current.SameAs(fit));
        Assert.True(workspace.RefreshRequired); Assert.False(workspace.CanPublish);
        Assert.Throws<InvalidOperationException>(() => workspace.Publish());
        workspace.Refresh(); Assert.True(workspace.Publish().Accepted);
        Assert.Single(campaign.Inspect().OwnedBlueprintRevisions);
    }

    [Fact]
    public void InspectionOfReplacementSessionCannotEnableOldDesignerOrProductionModels()
    {
        var campaign = new Campaign(); var workspace = Courier(campaign); Assert.True(workspace.Publish().Accepted);
        workspace.Draft!.Rename("Review-only edit"); var fit = workspace.Draft.Current;
        var production = new ProductionOverview(campaign.Client, campaign.Client.Snapshot, Actor);
        var option = Assert.Single(production.Blueprints(Colony)); var report = campaign.Inspect();
        campaign.Client.Load(campaign.Client.ExportSave()); workspace.Refresh();
        Assert.Equal(report.StateRevision, campaign.Inspect().StateRevision); // Same stamps, different session generation.
        var before = campaign.Saved;
        Assert.True(workspace.SessionReplaced); Assert.False(workspace.CanPublish);
        Assert.Throws<InvalidOperationException>(() => workspace.Publish());
        Assert.Throws<InvalidOperationException>(() => production.EnqueueBlueprint(Colony, option));
        Assert.Equal(before, campaign.Saved); Assert.True(workspace.Draft.Current.SameAs(fit));
        Assert.Single(new CampaignDesigner(campaign.Client, Actor).History);
    }

    [Fact]
    public void UnavailableEngineeringVersionAgreesBetweenLiveProviderInspectionAndCommandRejection()
    {
        var scenario = new Campaign().Scenario;
        scenario.Definitions.ShipDesignVersionAccess!.Single(g => g.VersionId == "component.propulsion.v1")
            .RequiredEngineeringApplicationDefinitionIds!.Add("engineering.advanced-starship-engineering");
        var campaign = new Campaign(scenario); var workspace = Courier(campaign);
        var report = campaign.Inspect();
        Assert.DoesNotContain("component.propulsion.v1", report.CurrentCatalogue!.AvailableVersionIds);
        Assert.False(((IShipDesignCatalogueFacts)workspace.Draft!.Provider).Available("component.propulsion.v1"));
        Assert.False(workspace.CanPublish); var before = campaign.Saved;
        Assert.Equal(CommandErrorCode.ShipDesignVersionUnavailable, workspace.Publish().ErrorCode);
        Assert.Equal(before, campaign.Saved); Assert.Empty(campaign.Inspect().OwnedBlueprintRevisions);
    }
}
