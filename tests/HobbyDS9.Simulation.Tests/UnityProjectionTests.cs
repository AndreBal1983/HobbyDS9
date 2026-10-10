using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityClient;
using Xunit;
using static HobbyDS9.Simulation.Tests.PhasedCombatFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class UnityProjectionTests
{
    [Fact]
    public void FleetAndLabelLookupUsesExplicitAssociationNotNodeString()
    {
        var app = CampaignApplication.StartScenario(VerticalSliceFixture.Scenario()); var snapshot = app.CaptureSnapshot();
        var nodes = new[] { new NavigationNodeSnapshot(new("node.unrelated-to-sol"), new EntityId("system.sol")),
            new NavigationNodeSnapshot(new("system.sol"), null) };
        var projection = GalaxyProjection.Nodes(nodes, snapshot.Campaign, snapshot.Content);
        var sol = projection.Single(n => n.NodeId.Value == "node.unrelated-to-sol");
        Assert.Equal("Sol", sol.Label); Assert.Single(sol.Fleets); Assert.Equal("fleet.federation-scout", sol.Fleets[0].FleetId.Value);
        var empty = projection.Single(n => n.NodeId.Value == "system.sol");
        Assert.Null(empty.SystemId); Assert.Equal("Transit region", empty.Label); Assert.Empty(empty.Fleets);
    }
    [Fact]
    public void ProjectionIsOrderedAndCannotChangeAuthoritativeSnapshot()
    {
        var app = CampaignApplication.StartScenario(VerticalSliceFixture.Scenario()); var snapshot = app.CaptureSnapshot(); var before = Json(app.ExportSave());
        var projected = GalaxyProjection.Nodes(snapshot);
        Assert.Equal(Json(projected), Json(GalaxyProjection.Nodes(snapshot.Navigation.Nodes.Reverse(), snapshot.Campaign, snapshot.Content)));
        Assert.Throws<NotSupportedException>(() => ((IList<GalaxyNodeView>)projected).Clear());
        Assert.Equal(before, Json(app.ExportSave()));
    }
    [Fact]
    public void SelectionIsClientOnlyAndReconcilesMissingEntities()
    {
        var snapshot = CampaignApplication.StartScenario(VerticalSliceFixture.Scenario()).CaptureSnapshot(); var selection = new GalaxySelection();
        selection.SelectNode(new("system.sol")); selection.Reconcile(snapshot); Assert.NotNull(selection.Node);
        selection.SelectFleet(new("fleet.federation-scout")); Assert.Null(selection.Node); selection.Reconcile(snapshot); Assert.NotNull(selection.Fleet);
        selection.SelectFleet(new("fleet.destroyed")); selection.Reconcile(snapshot); Assert.Null(selection.Fleet);
        selection.SelectNode(new("node.removed")); selection.Reconcile(snapshot); Assert.Null(selection.Node);
    }
    [Fact]
    public void RetainedProjectionRemainsDetachedAcrossMovement()
    {
        var app = CampaignApplication.StartScenario(VerticalSliceFixture.Scenario()); var old = GalaxyProjection.Nodes(app.CaptureSnapshot()); var before = Json(old);
        Assert.True(app.SubmitFleetRoute(new("cmd.move"), new("civ.federation"), 1, 0, new("fleet.federation-scout"), new[] { new NavigationNodeId("system.bajor") }).Accepted);
        app.AdvanceTurn(1, 1); var current = GalaxyProjection.Nodes(app.CaptureSnapshot());
        Assert.Equal(before, Json(old)); Assert.Contains(current.Single(n => n.SystemId == new EntityId("system.bajor")).Fleets, f => f.FleetId.Value == "fleet.federation-scout");
        Assert.Contains(old.Single(n => n.SystemId == new EntityId("system.sol")).Fleets, f => f.FleetId.Value == "fleet.federation-scout");
    }
}
