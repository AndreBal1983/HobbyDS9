using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class NavigationNeighborIndexTests
{
    static DefinitionCatalogDto Topology(bool reverse = false)
    {
        var definitions = new DefinitionCatalogDto
        {
            NavigationRegions = new[] { "nav.hub", "nav.z", "nav.a", "nav.a-1", "nav.isolated" }
                .Select(id => new NavigationRegionDefinitionDto
                { NodeId = id, DisplayName = id, PhenomenonDefinitionIds = new() }).ToList(),
            NavigationConnections = new()
            {
                new() { ConnectionId = "edge.z", EndpointA = "nav.z", EndpointB = "nav.hub", Kind = GalaxyEdgeKind.Normal, BaseTraversalCost = 1 },
                new() { ConnectionId = "edge.a", EndpointA = "nav.hub", EndpointB = "nav.a", Kind = GalaxyEdgeKind.Normal, BaseTraversalCost = 1 },
                new() { ConnectionId = "edge.branch", EndpointA = "nav.a-1", EndpointB = "nav.hub", Kind = GalaxyEdgeKind.Normal, BaseTraversalCost = 1 }
            },
            Phenomena = new()
        };
        if (reverse)
        {
            definitions.NavigationRegions.Reverse();
            definitions.NavigationConnections.Reverse();
            foreach (var edge in definitions.NavigationConnections)
                (edge.EndpointA, edge.EndpointB) = (edge.EndpointB, edge.EndpointA);
        }
        return definitions;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NeighborOrderAndBidirectionalityDoNotDependOnInputOrderOrOrientation(bool reverse)
    {
        var definitions = Topology(reverse);
        var before = JsonSerializer.Serialize(definitions);
        var graph = new WeightedNavigationGraph(definitions);
        Assert.Equal(new[] { "nav.a", "nav.a-1", "nav.z" }, graph.Neighbors("nav.hub"));
        foreach (var leaf in new[] { "nav.a", "nav.a-1", "nav.z" })
            Assert.Equal(new[] { "nav.hub" }, graph.Neighbors(leaf));
        foreach (var missing in new[] { "nav.isolated", "nav.unknown", "NAV.HUB", "", null! })
            Assert.Empty(graph.Neighbors(missing));
        Assert.Equal(before, JsonSerializer.Serialize(definitions));
    }

    [Fact]
    public void NeighborResultsAreFreshReadOnlyAndDetachedFromLaterInputMutation()
    {
        var definitions = Topology();
        var graph = new WeightedNavigationGraph(definitions);
        var neighbors = graph.Neighbors("nav.hub");
        Assert.NotSame(neighbors, graph.Neighbors("nav.hub"));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)neighbors)[0] = "nav.forged");
        Assert.Throws<NotSupportedException>(() => ((IList<string>)neighbors).Clear());
        foreach (var missing in new[] { "nav.isolated", "nav.unknown", "NAV.HUB", "", null! })
        {
            var empty = graph.Neighbors(missing);
            Assert.NotSame(empty, graph.Neighbors(missing));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)empty).Add("nav.forged"));
        }
        definitions.NavigationConnections![0].EndpointA = "nav.forged";
        definitions.NavigationConnections.Clear();
        definitions.NavigationRegions!.Clear();
        Assert.Equal(new[] { "nav.a", "nav.a-1", "nav.z" }, neighbors);
        Assert.Equal(neighbors, graph.Neighbors("nav.hub"));
        Assert.Equal(new[] { "nav.hub" }, graph.Neighbors("nav.z"));
    }
}
