using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityClient;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ProductionOverviewTests
{
    private static StrategicClient Start()
    {
        var scenario = VerticalSliceFixture.Scenario(false, true);
        return new StrategicClient(CampaignApplication.StartScenario(scenario), save => CampaignApplication.LoadSave(scenario, save));
    }
    private static string Save(StrategicClient client) => JsonSerializer.Serialize(client.ExportSave());
    [Fact]
    public void OwnedWorldsOptionsAndProjectionAreDetachedOrderedAndReadOnly()
    {
        var client = Start(); var before = Save(client);
        var model = new ProductionOverview(client, client.Snapshot);
        var expected = client.Snapshot.Campaign.Colonies.Where(c => c.OwnerCivilizationId == model.Actor!.CivilizationId)
            .OrderBy(c => c.ColonyId.Value, StringComparer.Ordinal).Select(c => c.ColonyId);
        Assert.Equal(expected, model.Worlds.Select(c => c.ColonyId));
        Assert.Equal(2, model.Worlds.Count);
        foreach (var world in model.Worlds)
        {
            Assert.NotEmpty(model.Designs(world.ColonyId));
            Assert.All(model.Designs(world.ColonyId), option => Assert.Contains(option.Design, client.Snapshot.Campaign.ShipDesigns));
        }
        Assert.Throws<NotSupportedException>(() => ((IList<ColonySnapshot>)model.Worlds).Clear());
        Assert.Equal(before, Save(client));
    }
    [Fact]
    public void QueueCancelUseRealApplicationAndRejectStaleObservedRevisionWithoutMutation()
    {
        var client = Start(); var model = new ProductionOverview(client, client.Snapshot);
        var world = new EntityId("colony.sol"); var design = new DefinitionId("ship-design.scout");
        var revision = client.Snapshot.Campaign.StateRevision;
        Assert.True(model.Enqueue(world, design).Accepted);
        Assert.Equal(revision + 1, client.Snapshot.Campaign.StateRevision);
        Assert.Empty(model.Worlds.Single(c => c.ColonyId == world).ProductionQueue);
        var before = Save(client);
        Assert.Equal(CommandErrorCode.StaleRevision, model.Enqueue(world, design).ErrorCode);
        Assert.Equal(before, Save(client));
        var updated = new ProductionOverview(client, client.Snapshot);
        var item = updated.Worlds.Single(c => c.ColonyId == world).ProductionQueue.Single();
        Assert.True(updated.Cancel(world, item.QueueItemId).Accepted);
        Assert.Empty(client.Snapshot.Campaign.Colonies.Single(c => c.ColonyId == world).ProductionQueue);
        Assert.Equal(revision + 2, client.Snapshot.Campaign.StateRevision);
        Assert.Single(updated.Worlds.Single(c => c.ColonyId == world).ProductionQueue);
    }
    [Fact]
    public void OverviewDoesNotDependOnMapSelection()
    {
        var client = Start(); var model = new ProductionOverview(client, client.Snapshot);
        var selection = new GalaxySelection(); selection.SelectFleet(new EntityId("fleet.federation-scout"));
        var worlds = model.Worlds.Select(w => w.ColonyId).ToArray();
        selection.SelectNode(new NavigationNodeId("nav.bajor"));
        Assert.Equal(worlds, model.Worlds.Select(w => w.ColonyId));
        Assert.True(model.Enqueue(new EntityId("colony.sol"), new DefinitionId("ship-design.scout")).Accepted);
    }
}
