using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace HobbyDS9.Simulation.Tests;

public sealed class WorldEconomyPlanetLookupTests
{
    readonly ITestOutputHelper output;
    public WorldEconomyPlanetLookupTests(ITestOutputHelper output) { this.output = output; }
    static (ColonyStateDto world, GameStateDto state, DefinitionCatalogDto definitions, PlanetDefinitionDto expected) Fixture()
    {
        var expected = new PlanetDefinitionDto { DefinitionId = "definition.target" };
        return (new() { PlanetId = "planet.target" },
            new() { Planets = new() { new() { PlanetId = "planet.other", DefinitionId = "definition.other" }, new() { PlanetId = "planet.target", DefinitionId = expected.DefinitionId } } },
            new() { Planets = new() { new() { DefinitionId = "definition.other" }, expected } }, expected);
    }
    static string Json(object value) => JsonSerializer.Serialize(value);

    [Fact] public void ReorderedLargeCataloguesReturnTheExactDefinitionAndDoNotMutateInputs()
    {
        var x = Fixture();
        for (var i = 0; i < 200; i++)
        {
            x.state.Planets!.Add(new() { PlanetId = "planet.extra." + i, DefinitionId = "definition.extra." + i });
            x.definitions.Planets!.Add(new() { DefinitionId = "definition.extra." + i });
        }
        var before = Json(new { x.world, x.state, x.definitions });
        Assert.Same(x.expected, WorldEconomy.Planet(x.world, x.state, x.definitions));
        Assert.Equal(before, Json(new { x.world, x.state, x.definitions }));
        x.state.Planets!.Reverse(); x.definitions.Planets!.Reverse();
        Assert.Same(x.expected, WorldEconomy.Planet(x.world, x.state, x.definitions));
    }

    [Fact] public void UnrelatedDuplicateIdsAreNotNewlyRejectedByGlobalIndexConstruction()
    {
        var x = Fixture();
        x.state.Planets!.Add(new() { PlanetId = "planet.other", DefinitionId = "different" });
        x.definitions.Planets!.Add(new() { DefinitionId = "definition.other" });
        Assert.Same(x.expected, WorldEconomy.Planet(x.world, x.state, x.definitions));
    }

    [Fact] public void IdEqualityRemainsOrdinalIncludingNullValuesAtThisLowLevel()
    {
        var x = Fixture(); x.world.PlanetId = "PLANET.TARGET";
        Assert.Throws<InvalidOperationException>(() => WorldEconomy.Planet(x.world, x.state, x.definitions));
        x.world.PlanetId = "planet.target"; x.expected.DefinitionId = "DEFINITION.TARGET";
        Assert.Throws<InvalidOperationException>(() => WorldEconomy.Planet(x.world, x.state, x.definitions));
        x.world.PlanetId = null!; x.state.Planets![1].PlanetId = null!;
        x.state.Planets[1].DefinitionId = null!; x.expected.DefinitionId = null!;
        Assert.Same(x.expected, WorldEconomy.Planet(x.world, x.state, x.definitions));
    }

    [Fact] public void EveryCallSeesChangedKeysReplacedRowsListsAndDefinitionValues()
    {
        var x = Fixture(); Assert.Same(x.expected, WorldEconomy.Planet(x.world, x.state, x.definitions));
        x.world.PlanetId = "planet.other";
        Assert.Same(x.definitions.Planets![0], WorldEconomy.Planet(x.world, x.state, x.definitions));
        x.state.Planets![0].DefinitionId = "definition.target";
        Assert.Same(x.expected, WorldEconomy.Planet(x.world, x.state, x.definitions));
        x.state.Planets = new() { new() { PlanetId = x.world.PlanetId, DefinitionId = "definition.new" } };
        var replacement = new PlanetDefinitionDto { DefinitionId = "definition.new", DisplayName = "current" };
        x.definitions.Planets = new() { replacement };
        Assert.Same(replacement, WorldEconomy.Planet(x.world, x.state, x.definitions));
        replacement.DisplayName = "changed";
        Assert.Equal("changed", WorldEconomy.Planet(x.world, x.state, x.definitions).DisplayName);
        var independent = Fixture(); Assert.Same(independent.expected, WorldEconomy.Planet(independent.world, independent.state, independent.definitions));
    }

    [Theory]
    [InlineData("missing-state", "InvalidOperationException")]
    [InlineData("duplicate-state", "InvalidOperationException")]
    [InlineData("missing-definition", "InvalidOperationException")]
    [InlineData("duplicate-definition", "InvalidOperationException")]
    [InlineData("null-state-list", "ArgumentNullException")]
    [InlineData("null-definition-list", "ArgumentNullException")]
    [InlineData("empty-state-list", "InvalidOperationException")]
    [InlineData("empty-definition-list", "InvalidOperationException")]
    [InlineData("null-state-before", "NullReferenceException")]
    [InlineData("null-state-after", "NullReferenceException")]
    [InlineData("null-definition-before", "NullReferenceException")]
    [InlineData("null-definition-after", "NullReferenceException")]
    [InlineData("empty-definitions-broken-state-and-world", "InvalidOperationException")]
    [InlineData("null-first-definition-and-state-list", "NullReferenceException")]
    [InlineData("null-world", "NullReferenceException")]
    [InlineData("null-state", "NullReferenceException")]
    [InlineData("null-definitions", "NullReferenceException")]
    public void MalformedInputsKeepTheOriginalExceptionAndEvaluationOrder(string scenario, string expectedType)
    {
        var x = Fixture();
        switch (scenario)
        {
            case "missing-state": x.world.PlanetId = "missing"; break;
            case "duplicate-state": x.state.Planets!.Add(new() { PlanetId = x.world.PlanetId, DefinitionId = x.expected.DefinitionId }); break;
            case "missing-definition": x.expected.DefinitionId = "missing"; break;
            case "duplicate-definition": x.definitions.Planets!.Add(new() { DefinitionId = x.expected.DefinitionId }); break;
            case "null-state-list": x.state.Planets = null; break;
            case "null-definition-list": x.definitions.Planets = null; break;
            case "empty-state-list": x.state.Planets!.Clear(); break;
            case "empty-definition-list": x.definitions.Planets!.Clear(); break;
            case "null-state-before": x.state.Planets!.Insert(0, null!); break;
            case "null-state-after": x.state.Planets!.Add(null!); break;
            case "null-definition-before": x.definitions.Planets!.Insert(0, null!); break;
            case "null-definition-after": x.definitions.Planets!.Add(null!); break;
            case "empty-definitions-broken-state-and-world": x.definitions.Planets!.Clear(); x.state.Planets = null; x.world = null!; break;
            case "null-first-definition-and-state-list": x.definitions.Planets!.Insert(0, null!); x.state.Planets = null; break;
            case "null-world": x.world = null!; break;
            case "null-state": x.state = null!; break;
            case "null-definitions": x.definitions = null!; break;
        }
        var before = Json(new { x.world, x.state, x.definitions });
        var exception = Record.Exception(() => WorldEconomy.Planet(x.world, x.state, x.definitions));
        Assert.NotNull(exception); Assert.Equal(expectedType, exception!.GetType().Name);
        Assert.Equal(before, Json(new { x.world, x.state, x.definitions }));
        // Baseline and candidate run these exact tests; the external receipt compares exact messages too.
        output.WriteLine(Json(new { scenario, type = exception.GetType().FullName, exception.Message }));
    }

    [Fact] public void ActualProjectAndHypotheticalReceivingCohortKeepTheirOwnValuesAndSourcePurity()
    {
        var s = VerticalSliceFixture.Scenario();
        var world = s.InitialState.Colonies!.Single(w => w.ColonyId == "colony.sol");
        var before = Json(s); var actual = WorldEconomy.Project(world, s.InitialState, s.Definitions);
        var candidate = PopulationData.Copy(world); var arriving = PopulationData.Copy(world.Cohorts![0]);
        arriving.CohortId = "cohort.hypothetical-arrival"; arriving.Population = 100; candidate.Cohorts!.Add(arriving);
        var hypotheticalBefore = Json(candidate);
        var hypothetical = WorldEconomy.Project(candidate, s.InitialState, s.Definitions);
        Assert.Equal(actual.Population + 100, hypothetical.Population);
        Assert.Equal(hypothetical.Population / 100 + (hypothetical.Population % 100 == 0 ? 0 : 1), hypothetical.FoodDemand);
        Assert.Equal(Json(actual), Json(WorldEconomy.Project(world, s.InitialState, s.Definitions)));
        Assert.Equal(hypotheticalBefore, Json(candidate)); Assert.Equal(before, Json(s));
        Assert.Same(WorldEconomy.Planet(world, s.InitialState, s.Definitions), WorldEconomy.Planet(candidate, s.InitialState, s.Definitions));
    }
}
