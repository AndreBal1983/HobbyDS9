using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.VerticalSliceFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class ProductionTests
{
    private static ScenarioDto EmptyScenario()
    { var s = Scenario(); var ids = s.InitialState.Ships!.Select(x => x.ShipId).Concat(s.InitialState.Fleets!.Select(f => f.FleetId)).ToHashSet();
      s.InitialState.EntityIdentities.RemoveAll(e => ids.Contains(e.EntityId)); s.InitialState.Ships!.Clear(); s.InitialState.Fleets!.Clear(); return s; }
    private static SimulationSession EmptySession() => Session(EmptyScenario());
    [Fact]
    public void EnqueuePaysExactlyOnceAllocatesOneIdAndPreservesFifo()
    {
        var scenario = EmptyScenario(); scenario.InitialState.Colonies!.Single(c => c.ColonyId == "colony.sol").Materials = 30;
        var s = Session(scenario); var old = s.CaptureSnapshot();
        Assert.True(Enqueue(s).Accepted); Assert.True(Enqueue(s, "cmd.second").Accepted);
        var state = s.ExportSave().GameState; var colony = Colony(s);
        Assert.Equal(30, state.Civilizations!.Single(c => c.CivilizationId == "civ.federation").Credits); Assert.Equal(14, colony.Materials);
        Assert.Equal(new[] { "queue-item.spawn.0000000000000001", "queue-item.spawn.0000000000000002" }, colony.ProductionQueue!.Select(q => q.QueueItemId));
        Assert.Equal(3UL, state.NextEntitySequence); Assert.Equal(2, state.StateRevision); Assert.Empty(old.Colonies.Single(c => c.ColonyId.Value == "colony.sol").ProductionQueue);
        var before = Json(s); var duplicate = Command(s, CommandKind.EnqueueProduction, "cmd.enqueue"); duplicate.EnqueueProduction = new() { ColonyId = "colony.sol", DefinitionId = "ship-design.scout" };
        Assert.Equal(CommandErrorCode.DuplicateCommand, s.SubmitCommand(duplicate).ErrorCode); Assert.Equal(before, Json(s));
    }

    [Theory]
    [InlineData("unknown", CommandErrorCode.UnknownColony)]
    [InlineData("foreign", CommandErrorCode.ColonyNotOwned)]
    [InlineData("neutral", CommandErrorCode.ColonyNotOwned)]
    [InlineData("design", CommandErrorCode.UnknownShipDesign)]
    [InlineData("nonship", CommandErrorCode.UnknownShipDesign)]
    [InlineData("research", CommandErrorCode.DesignResearchPrerequisiteMissing)]
    [InlineData("credits", CommandErrorCode.InsufficientCredits)]
    [InlineData("materials", CommandErrorCode.InsufficientMaterials)]
    public void OrdinaryRejectionsPayAndAllocateNothing(string error, CommandErrorCode expected)
    {
        var scenario = EmptyScenario(); if (error == "credits") scenario.InitialState.Civilizations![0].Credits = 9;
        if (error == "materials") scenario.InitialState.Colonies![0].Materials = 7;
        var s = Session(scenario); var before = Json(s);
        var colony = error == "unknown" ? "colony.absent" : error == "foreign" ? "colony.cardassia" : error == "neutral" ? "colony.bajor" : "colony.sol";
        var design = error == "design" ? "ship-design.absent" : error == "nonship" ? "economy.baseline" : error == "research" ? "ship-design.advanced-scout" : "ship-design.scout";
        Assert.Equal(expected, Enqueue(s, design: design, colony: colony).ErrorCode); Assert.Equal(before, Json(s));
    }

    [Theory]
    [InlineData(0, 8, 10)]
    [InlineData(8, 4, 6)]
    [InlineData(19, 0, 0)]
    public void CancellationRefundsUnusedFractionAndNeverRecyclesIds(long progress, long materials, long credits)
    {
        var s = EmptySession(); Assert.True(Enqueue(s).Accepted); var save = s.ExportSave(); save.GameState.Colonies!.Single(c => c.ColonyId == "colony.sol").ProductionQueue![0].CompletedIndustry = progress;
        var scenario = EmptyScenario(); s = new(save, scenario.Definitions, Compatibility(scenario)); var before = s.ExportSave().GameState;
        Assert.True(Cancel(s, Colony(s).ProductionQueue![0].QueueItemId).Accepted);
        Assert.Equal(before.Civilizations!.Single(c => c.CivilizationId == "civ.federation").Credits + credits, s.CaptureSnapshot().Civilizations.Single(c => c.CivilizationId.Value == "civ.federation").Credits);
        Assert.Equal(2 + materials, Colony(s).Materials); Assert.Empty(Colony(s).ProductionQueue!); Assert.Equal(2UL, s.CaptureSnapshot().NextEntitySequence);
        if (materials >= 6) { Assert.True(Enqueue(s, "cmd.again").Accepted); Assert.Equal("queue-item.spawn.0000000000000002", Colony(s).ProductionQueue![0].QueueItemId); }
    }

    [Theory]
    [InlineData(long.MaxValue, long.MaxValue, 0, long.MaxValue)]
    [InlineData(long.MaxValue, long.MaxValue, 1, long.MaxValue - 1)]
    [InlineData(long.MaxValue, long.MaxValue, long.MaxValue - 1, 1)]
    [InlineData(long.MaxValue, 2, 1, 4611686018427387903L)]
    [InlineData(0, long.MaxValue, 12, 0)]
    public void RefundHasExactLargeValueFloorWithoutIntermediateOverflow(long paid, long required, long completed, long expected)
    { Assert.Equal(expected, RefundArithmetic.Unused(paid, required, completed)); }

    [Fact]
    public void PostGrowthIndustryAppliesOnlyToHeadAndDiscardsExcessAndPersistsMidQueue()
    {
        var scenario = EmptyScenario(); var sol = scenario.InitialState.Colonies![0]; sol.Materials = 30; sol.Population = 1249;
        var s = Session(scenario); Assert.True(Enqueue(s).Accepted); Assert.True(Enqueue(s, "cmd.second").Accepted);
        Advance(s); Assert.Equal(1299, Colony(s).Population); Assert.Equal(9, Colony(s).ProductionQueue![0].CompletedIndustry); Assert.Equal(0, Colony(s).ProductionQueue![1].CompletedIndustry);
        s = new(JsonSerializer.Deserialize<SaveEnvelopeDto>(Json(s))!, scenario.Definitions, Compatibility(scenario));
        Advance(s); Advance(s); Assert.Single(Colony(s).ProductionQueue!); Assert.Equal(0, Colony(s).ProductionQueue![0].CompletedIndustry);
        Assert.Contains(s.CaptureSnapshot().Ships, ship => ship.ShipId.Value == "ship.spawn.0000000000000003");
        Assert.Equal(5UL, s.CaptureSnapshot().NextEntitySequence); Assert.Equal(10, s.CaptureSnapshot().Ships.Last().CurrentHull);
        Advance(s); Assert.Equal(9, Colony(s).ProductionQueue![0].CompletedIndustry);
    }

    [Fact]
    public void ResearchGateOpensAfterUnlockAndNoIndustryStoredWhenQueueEmpty()
    {
        var s = EmptySession(); var command = Command(s, CommandKind.SetResearchTarget, "cmd.research"); command.SetResearchTarget = new() { TargetDefinitionId = "research.advanced-engineering" };
        Assert.True(s.SubmitCommand(command).Accepted); Advance(s); Advance(s); Assert.Equal(CommandErrorCode.DesignResearchPrerequisiteMissing, Enqueue(s, design: "ship-design.advanced-scout").ErrorCode);
        Advance(s); Assert.True(Enqueue(s, design: "ship-design.advanced-scout").Accepted); Advance(s); Assert.Equal(8, Colony(s).ProductionQueue![0].CompletedIndustry);
    }

    [Theory]
    [InlineData("allocation")]
    [InlineData("later")]
    [InlineData("industry")]
    public void ProductionFaultsRollbackEarlierEconomyResearchAndCreatedEntities(string error)
    {
        var scenario = EmptyScenario(); var failed = true;
        if (error == "industry") scenario.Definitions.EconomicProfiles![0].BaseIndustry = long.MaxValue;
        var s = Session(scenario, (phase, _) => !(failed && error == "later" && phase == StrategicTurnPhase.Movement));
        Assert.True(Enqueue(s).Accepted); var save = s.ExportSave();
        save.GameState.Colonies!.Single(c => c.ColonyId == "colony.sol").ProductionQueue![0].CompletedIndustry = 19;
        if (error == "allocation") save.GameState.NextEntitySequence = ulong.MaxValue - 1;
        s = new(save, scenario.Definitions, Compatibility(scenario), new Dictionary<CommandKind, ICommandHandler>(), (phase, _) => !(failed && error == "later" && phase == StrategicTurnPhase.Movement));
        var before = Json(s);
        if (error == "later") Assert.Equal(TurnErrorCode.PhaseFailed, Advance(s).ErrorCode); else Assert.Throws<OverflowException>(() => Advance(s));
        Assert.Equal(before, Json(s)); failed = false;
        if (error == "later") { Assert.Equal(TurnStatus.Completed, Advance(s).Status); Assert.Single(s.CaptureSnapshot().Ships); }
    }
}
