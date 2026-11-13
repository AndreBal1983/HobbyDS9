using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class NavigationValidationIndexTests
{
    static bool NavigationIssue(ValidationIssue issue)=>issue.Code==ValidationCode.InvalidFleetRoute&&issue.Path=="$.GameState.Navigation";
    [Theory]
    [InlineData("valid")][InlineData("reordered")][InlineData("reversed-endpoints")][InlineData("shared-natural-exact-max")]
    public void ValidInputsAndSharedEndpointHazardRemainValidWithoutMutation(string name)
    {
        var s=NavigationValidationIndexCases.Scenario();NavigationValidationIndexCases.Mutate(s,name);var before=JsonSerializer.Serialize(s);
        Assert.True(FoundationValidator.ValidateGameState(s.InitialState,s.Definitions).IsValid);
        Assert.Equal(before,JsonSerializer.Serialize(s));
    }
    [Theory]
    [InlineData("unknown-association")][InlineData("knowledge-unknown")][InlineData("knowledge-duplicate")][InlineData("knowledge-level")]
    [InlineData("effect-duplicate")][InlineData("effect-negative")][InlineData("effect-unknown")][InlineData("effect-both-targets")][InlineData("effect-dates")]
    [InlineData("effect-edge-penalty")][InlineData("effect-maxcost")][InlineData("effect-maxpenalty")][InlineData("natural-overflow")]
    [InlineData("stats-negative")][InlineData("route-unknown")][InlineData("route-unknown-knowledge")][InlineData("fleet-transit")]
    public void DynamicKnowledgeEffectsOverflowAndRoutesStillRejectAtNavigationPath(string name)
    {
        var s=NavigationValidationIndexCases.Scenario();NavigationValidationIndexCases.Mutate(s,name);var before=JsonSerializer.Serialize(s);
        Assert.Contains(FoundationValidator.ValidateGameState(s.InitialState,s.Definitions).Issues,NavigationIssue);
        Assert.Equal(before,JsonSerializer.Serialize(s));
    }
    [Theory]
    [InlineData("regions-null")][InlineData("connections-null")][InlineData("phenomena-null")][InlineData("region-null")][InlineData("connection-null")][InlineData("phenomenon-null")]
    [InlineData("region-duplicate")][InlineData("connection-duplicate")][InlineData("phenomenon-duplicate")][InlineData("region-phenomena-null")][InlineData("unknown-phenomenon")][InlineData("unknown-endpoint")][InlineData("self-edge")]
    public void InvalidDefinitionsReturnExistingReportsBeforeIndexConstruction(string name)
    {
        var s=NavigationValidationIndexCases.Scenario();NavigationValidationIndexCases.Mutate(s,name);
        Assert.False(FoundationValidator.ValidateGameState(s.InitialState,s.Definitions).IsValid);
    }
    [Fact] public void EveryInvocationReadsCurrentDefinitionsAndSystemsInsteadOfRetainingAnIndex()
    {
        var s=NavigationValidationIndexCases.Scenario();Assert.True(FoundationValidator.ValidateGameState(s.InitialState,s.Definitions).IsValid);
        var node=s.Definitions.NavigationRegions!.First(n=>n.AssociatedSystemId!=null);var system=node.AssociatedSystemId;
        node.AssociatedSystemId="system.absent";Assert.Contains(FoundationValidator.ValidateGameState(s.InitialState,s.Definitions).Issues,NavigationIssue);
        node.AssociatedSystemId=system;Assert.True(FoundationValidator.ValidateGameState(s.InitialState,s.Definitions).IsValid);
        NavigationValidationIndexCases.Hazard(s,long.MaxValue-1,shared:true);Assert.True(FoundationValidator.ValidateGameState(s.InitialState,s.Definitions).IsValid);
        s.Definitions.Phenomena![0].TraversalCostIncrease=long.MaxValue;
        Assert.Contains(FoundationValidator.ValidateGameState(s.InitialState,s.Definitions).Issues,NavigationIssue);
        s.Definitions.Phenomena[0].TraversalCostIncrease=0;Assert.True(FoundationValidator.ValidateGameState(s.InitialState,s.Definitions).IsValid);
    }
}
