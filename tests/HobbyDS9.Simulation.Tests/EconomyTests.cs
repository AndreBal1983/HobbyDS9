using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
namespace HobbyDS9.Simulation.Tests;

public sealed class EconomyTests
{
    [Fact]
    public void ScenarioHasMixedPopulationAndMultiplePlanetsInSol()
    {
        var scenario = VerticalSliceFixture.Scenario();
        var report = FoundationValidator.ValidateScenario(scenario);
        Assert.True(report.IsValid, string.Join(";", report.Issues.Select(i => i.Path + ":" + i.Code)));
        Assert.Equal(2, scenario.InitialState.Planets!.Count(p => p.SystemId == "system.sol"));
        Assert.Equal(3, scenario.InitialState.Colonies!.Single(c => c.ColonyId == "colony.sol").Cohorts!.Count);
        Assert.Equal(7, scenario.Definitions.ContentFormatVersion);
    }
}
