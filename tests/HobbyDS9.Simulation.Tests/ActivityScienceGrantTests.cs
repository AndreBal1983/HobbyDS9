using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ActivityScienceGrantTests
{
    private static string Json(object? value) => JsonSerializer.Serialize(value);

    [Fact]
    public void NamedInsightPreservesExistingBanksAllocationsEngineeringAndDiscoverySeed()
    {
        var state = ScienceCoreTests.Initial(123);
        state.ScienceReserve = 777;
        state.EngineeringReserve = 33;
        state.EngineeringTargetDefinitionId = "engineering.example";
        state.ScienceAllocations!.ForEach(a => a.Weight = a.Discipline == ScienceDiscipline.Computing ? 600 : 0);
        var weights = state.ScienceAllocations;
        string beforeWeights = Json(weights);
        var receipts = new List<ResearchReceiptDto>();

        var result = ScienceRules.GrantActivityInsight(state, 10000, "civ.alpha", ScienceDiscipline.Energy, 20, receipts);

        Assert.Equal((12000L, 0L), result);
        Assert.Equal(777, state.ScienceReserve);
        Assert.Equal(33, state.EngineeringReserve);
        Assert.Equal("engineering.example", state.EngineeringTargetDefinitionId);
        Assert.Same(weights, state.ScienceAllocations);
        Assert.Equal(beforeWeights, Json(state.ScienceAllocations));
        Assert.Equal(123UL, state.ResearchDiscoverySeed);
        Assert.Equal(420, state.ScienceAllocationUnits);
        Assert.All(state.Disciplines!, d => Assert.Equal(d.Discipline == ScienceDiscipline.Energy ? 12000 : 0, d.TotalSpent));
        Assert.Equal(12000, state.Statistics!.TotalResearchGenerated);
        Assert.Equal(12000, state.Statistics.TotalScienceAllocated);
        Assert.Equal(12000, state.Statistics.TotalScienceSpent);
        Assert.Equal(0, state.Statistics.TotalEngineeringAllocated);
        Assert.Equal(0, state.Statistics.TotalEngineeringSpent);
        Assert.Single(receipts, r => r.Category == "activity-insight" && r.Amount == 12000);
        Assert.Empty(ScienceRules.Validate(state, 10000));
    }

    [Theory]
    [InlineData(5000)] [InlineData(6700)] [InlineData(10000)] [InlineData(15000)] [InlineData(30000)]
    public void TargetProgressAndBreakthroughsMatchExistingRulesAcrossPaces(int pace)
    {
        var state = ScienceCoreTests.Initial(71);
        state.ScienceReserve = 900;
        var expected = ScienceCoreTests.Initial(71);
        expected.ScienceReserve = ScienceRules.InternalUnits(500);
        expected.ScienceAllocations!.ForEach(a => a.Weight = a.Discipline == ScienceDiscipline.Construction ? 600 : 0);
        var expectedReceipts = new List<ResearchReceiptDto>();
        ScienceRules.SpendScience(expected, pace, "civ.alpha", expectedReceipts);
        var actualReceipts = new List<ResearchReceiptDto>();

        var actual = ScienceRules.GrantActivityInsight(state, pace, "civ.alpha", ScienceDiscipline.Construction, 500, actualReceipts);

        Assert.Equal(ScienceRules.InternalUnits(500) - expected.ScienceReserve, actual.LearnedInternalUnits);
        Assert.Equal(expected.ScienceReserve, actual.UnlearnableInternalUnits);
        Assert.Equal(Json(expected.Disciplines), Json(state.Disciplines));
        Assert.Equal(Json(expectedReceipts), Json(actualReceipts.Where(r => r.Category.StartsWith("science-"))));
        Assert.Equal(expected.Statistics!.TotalBreakthroughs, state.Statistics!.TotalBreakthroughs);
        Assert.Equal(expected.Statistics.TotalScienceLevelsCompleted, state.Statistics.TotalScienceLevelsCompleted);
        Assert.Equal(900, state.ScienceReserve);
    }

    [Fact]
    public void TerminalSurplusIsExplicitlyUnlearnableAndDoesNotBecomeFungibleIncome()
    {
        var state = ScienceCoreTests.Initial();
        var target = state.Disciplines!.Single(d => d.Discipline == ScienceDiscipline.Energy);
        target.Level = 9;
        target.ProgressTowardNextLevel = ScienceRules.IntervalCost(9, 10000) - 5;
        state.ScienceReserve = 19;
        var receipts = new List<ResearchReceiptDto>();

        var result = ScienceRules.GrantActivityInsight(state, 10000, "civ.alpha", ScienceDiscipline.Energy, 1, receipts);

        Assert.Equal((5L, 595L), result);
        Assert.Equal(10, target.Level);
        Assert.Equal(0, target.ProgressTowardNextLevel);
        Assert.Equal(19, state.ScienceReserve);
        Assert.Equal(5, state.Statistics!.TotalResearchGenerated);
        Assert.Equal(5, state.Statistics.TotalScienceAllocated);
        Assert.Equal(5, state.Statistics.TotalScienceSpent);
        Assert.Single(receipts, r => r.Category == "activity-insight-unlearnable" && r.Amount == 595);
        string before = Json(state);
        var more = ScienceRules.GrantActivityInsight(state, 10000, "civ.alpha", ScienceDiscipline.Energy, 20, new());
        Assert.Equal((0L, 12000L), more);
        Assert.Equal(before, Json(state));
    }

    [Fact]
    public void LateCounterOverflowAndInvalidInputsLeaveStateAndReceiptListUnchanged()
    {
        var state = ScienceCoreTests.Initial();
        state.ScienceReserve = 99;
        state.Statistics!.TotalResearchGenerated = long.MaxValue;
        state.Statistics.TotalScienceAllocated = long.MaxValue;
        var receipts = new List<ResearchReceiptDto> { new() { Category = "prior", Amount = 17 } };
        string before = Json(state), beforeReceipts = Json(receipts);
        Assert.Throws<OverflowException>(() => ScienceRules.GrantActivityInsight(state, 10000, "civ.alpha", ScienceDiscipline.Energy, 1, receipts));
        Assert.Equal(before, Json(state)); Assert.Equal(beforeReceipts, Json(receipts));
        Assert.Throws<OverflowException>(() => ScienceRules.GrantActivityInsight(state, 10000, "civ.alpha", ScienceDiscipline.Energy, long.MaxValue, receipts));
        Assert.Throws<ArgumentException>(() => ScienceRules.GrantActivityInsight(state, 1, "civ.alpha", ScienceDiscipline.Energy, 1, receipts));
        Assert.Throws<ArgumentException>(() => ScienceRules.GrantActivityInsight(state, 10000, "civ.alpha", (ScienceDiscipline)99, 1, receipts));
        Assert.Throws<ArgumentException>(() => ScienceRules.GrantActivityInsight(state, 10000, "civ.alpha", ScienceDiscipline.Energy, -1, receipts));
        Assert.Throws<ArgumentException>(() => ScienceRules.GrantActivityInsight(null!, 10000, "civ.alpha", ScienceDiscipline.Energy, 1, receipts));
        Assert.Throws<ArgumentException>(() => ScienceRules.GrantActivityInsight(state, 10000, "civ.alpha", ScienceDiscipline.Energy, 1, null!));
        Assert.Equal(before, Json(state)); Assert.Equal(beforeReceipts, Json(receipts));
    }

    [Fact]
    public void ReorderedCollectionsAndReloadProduceSameTargetedReceiptsAndProgress()
    {
        var first = ScienceCoreTests.Initial(415);
        first.ScienceReserve = 711;
        var reordered = JsonSerializer.Deserialize<ScienceEngineeringStateDto>(Json(first))!;
        reordered.Disciplines!.Reverse(); reordered.ScienceAllocations!.Reverse();
        var a = new List<ResearchReceiptDto>(); var b = new List<ResearchReceiptDto>();
        ScienceRules.GrantActivityInsight(first, 10000, "civ.alpha", ScienceDiscipline.Propulsion, 250, a);
        ScienceRules.GrantActivityInsight(reordered, 10000, "civ.alpha", ScienceDiscipline.Propulsion, 250, b);
        Assert.Equal(Json(a), Json(b));
        Assert.Equal(Json(first.Disciplines), Json(reordered.Disciplines.OrderBy(d => d.Discipline)));
        var reloaded = JsonSerializer.Deserialize<ScienceEngineeringStateDto>(Json(first))!;
        a.Clear(); b.Clear();
        ScienceRules.GrantActivityInsight(first, 10000, "civ.alpha", ScienceDiscipline.Propulsion, 60, a);
        ScienceRules.GrantActivityInsight(reloaded, 10000, "civ.alpha", ScienceDiscipline.Propulsion, 60, b);
        Assert.Equal(Json(first), Json(reloaded)); Assert.Equal(Json(a), Json(b));
    }

    [Fact]
    public void ZeroBudgetDoesNotChangeStateOrProduceReceipts()
    {
        var state = ScienceCoreTests.Initial(); string before = Json(state);
        var receipts = new List<ResearchReceiptDto>();
        Assert.Equal((0L, 0L), ScienceRules.GrantActivityInsight(state, 10000, "civ.alpha", ScienceDiscipline.Biotech, 0, receipts));
        Assert.Equal(before, Json(state)); Assert.Empty(receipts);
    }
}
