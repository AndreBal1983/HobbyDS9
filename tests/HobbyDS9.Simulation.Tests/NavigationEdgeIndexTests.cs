using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
public sealed class NavigationEdgeIndexTests
{
    [Fact]public void AllDirectedPairsMatchPriorOrdinalScanAndEveryReturnedEdgeIsDetached()
    {
        var d=CampaignPhysicalTravelTests.Package().CorePackage!.Campaign!.Definitions;
        var original=d.NavigationConnections!.Select(PopulationData.Copy).ToArray();
        var graph=new WeightedNavigationGraph(d);
        var endpoints=d.NavigationRegions!.Select(n=>n.NodeId).Concat(new[]{"nav.unknown","NAV.SOL",null!}).ToArray();
        foreach(var from in endpoints)foreach(var to in endpoints)
        {
            var expected=original.FirstOrDefault(e=>(e.EndpointA==from&&e.EndpointB==to)||(e.EndpointB==from&&e.EndpointA==to));
            var actual=graph.Edge(from,to);
            Assert.Equal(JsonSerializer.Serialize(expected),JsonSerializer.Serialize(actual));
            if(actual!=null){actual.EndpointA="nav.changed";actual.BaseTraversalCost=999;Assert.Equal(JsonSerializer.Serialize(expected),JsonSerializer.Serialize(graph.Edge(from,to)));}
        }
        var retained=original[0];var copy=graph.Edge(retained.EndpointB,retained.EndpointA)!;
        d.NavigationConnections![0].EndpointA="nav.mutated";d.NavigationConnections[0].BaseTraversalCost=0;d.NavigationConnections.Clear();
        Assert.Equal(JsonSerializer.Serialize(copy),JsonSerializer.Serialize(graph.Edge(retained.EndpointB,retained.EndpointA)));
    }
    [Fact]public void IndexedEdgesDoNotFreezeLiveEffectChangesExpiryOrNonadjacentError()
    {
        var p=CampaignPhysicalTravelTests.Package();var d=p.CorePackage!.Campaign!.Definitions;var state=p.CorePackage.Campaign.InitialState;
        var graph=new WeightedNavigationGraph(d);var edge=d.NavigationConnections![0];
        var initial=graph.Cost(state,edge.EndpointA,edge.EndpointB).EffectiveCost;
        var effect=new TraversalEffectDto{EffectId="effect.index-test",ConnectionId=edge.ConnectionId,StartTurn=state.TurnNumber,EndTurn=state.TurnNumber,TraversalCostIncrease=7};
        state.TraversalEffects!.Add(effect);
        Assert.Equal(initial+7,graph.Cost(state,edge.EndpointB,edge.EndpointA).EffectiveCost);
        effect.TraversalCostIncrease=11;Assert.Equal(initial+11,graph.Cost(state,edge.EndpointA,edge.EndpointB).EffectiveCost);
        Assert.Equal(initial,graph.Cost(state,edge.EndpointA,edge.EndpointB,(long)state.TurnNumber+1).EffectiveCost);
        state.TurnNumber++;Assert.Equal(initial,graph.Cost(state,edge.EndpointB,edge.EndpointA).EffectiveCost);
        Assert.Equal("Nonadjacent navigation route.",Assert.Throws<ArgumentException>(()=>graph.Cost(state,edge.EndpointA,edge.EndpointA)).Message);
    }
}
