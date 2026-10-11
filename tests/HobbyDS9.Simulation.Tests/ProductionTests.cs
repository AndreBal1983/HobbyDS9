using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.VerticalSliceFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class ProductionTests
{
    private static ScenarioDto EmptyScenario()
    {
        var s = Scenario(); PopulationFixture.RemoveMars(s); var ids = s.InitialState.Ships!.Select(x => x.ShipId).Concat(s.InitialState.Fleets!.Select(f => f.FleetId)).ToHashSet();
        s.InitialState.EntityIdentities.RemoveAll(e => ids.Contains(e.EntityId)); s.InitialState.Ships!.Clear(); s.InitialState.Fleets!.Clear(); return s;
    }
    private static SimulationSession EmptySession() => Session(EmptyScenario());
    [Fact]
    public void EnqueuePaysExactlyOnceAllocatesOneIdAndPreservesFifo()
    {
        var scenario = EmptyScenario(); PopulationFixture.SetDilithium(scenario.InitialState.Colonies!.Single(c => c.ColonyId == "colony.sol"), 30);
        var s = Session(scenario); var old = s.CaptureSnapshot();
        Assert.True(Enqueue(s).Accepted); Assert.True(Enqueue(s, "cmd.second").Accepted);
        var state = s.ExportSave().GameState; var colony = Colony(s);
        Assert.Equal(30, state.Civilizations!.Single(c => c.CivilizationId == "civ.federation").Credits); Assert.Equal(26, PopulationFixture.Dilithium(colony));
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
    [InlineData("research", CommandErrorCode.DesignEngineeringPrerequisiteMissing)]
    [InlineData("credits", CommandErrorCode.InsufficientCredits)]
    [InlineData("resource", CommandErrorCode.InsufficientStrategicResource)]
    public void OrdinaryRejectionsPayAndAllocateNothing(string error, CommandErrorCode expected)
    {
        var scenario = EmptyScenario(); if (error == "credits") scenario.InitialState.Civilizations![0].Credits = 9;
        if (error == "resource") PopulationFixture.SetDilithium(scenario.InitialState.Colonies![0], 1);
        var s = Session(scenario); var before = Json(s);
        var colony = error == "unknown" ? "colony.absent" : error == "foreign" ? "colony.cardassia" : error == "neutral" ? "colony.bajor" : "colony.sol";
        var design = error == "design" ? "ship-design.absent" : error == "nonship" ? "economy.baseline" : error == "research" ? "ship-design.advanced-scout" : "ship-design.scout";
        Assert.Equal(expected, Enqueue(s, design: design, colony: colony).ErrorCode); Assert.Equal(before, Json(s));
    }

    [Theory]
    [InlineData(0, 2, 10)]
    [InlineData(8, 1, 6)]
    [InlineData(19, 0, 0)]
    public void CancellationRefundsUnusedFractionAndNeverRecyclesIds(long progress, long resource, long credits)
    {
        var s = EmptySession(); Assert.True(Enqueue(s).Accepted); var save = s.ExportSave(); save.GameState.Colonies!.Single(c => c.ColonyId == "colony.sol").ProductionQueue![0].CompletedIndustry = progress;
        var scenario = EmptyScenario(); s = new(save, scenario.Definitions, Compatibility(scenario)); var before = s.ExportSave().GameState;
        Assert.True(Cancel(s, Colony(s).ProductionQueue![0].QueueItemId).Accepted);
        Assert.Equal(before.Civilizations!.Single(c => c.CivilizationId == "civ.federation").Credits + credits, s.CaptureSnapshot().Civilizations.Single(c => c.CivilizationId.Value == "civ.federation").Credits);
        Assert.Equal(8 + resource, PopulationFixture.Dilithium(Colony(s))); Assert.Empty(Colony(s).ProductionQueue!); Assert.Equal(2UL, s.CaptureSnapshot().NextEntitySequence);
        if (resource >= 1) { Assert.True(Enqueue(s, "cmd.again").Accepted); Assert.Equal("queue-item.spawn.0000000000000002", Colony(s).ProductionQueue![0].QueueItemId); }
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
    public void InfrastructureIndustryAppliesOnlyToHeadAndDiscardsExcessAndPersistsMidQueue()
    {
        var scenario = EmptyScenario(); var sol = scenario.InitialState.Colonies![0]; PopulationFixture.SetDilithium(sol, 30); sol.Cohorts!.Single(c => c.SpeciesDefinitionId == "species.human").Population = 949;
        var s = Session(scenario); Assert.True(Enqueue(s).Accepted); Assert.True(Enqueue(s, "cmd.second").Accepted);
        Advance(s); Assert.Equal(1255, PopulationRules.Population(Colony(s))); Assert.Equal(8, Colony(s).ProductionQueue![0].CompletedIndustry); Assert.Equal(0, Colony(s).ProductionQueue![1].CompletedIndustry);
        s = new(JsonSerializer.Deserialize<SaveEnvelopeDto>(Json(s))!, scenario.Definitions, Compatibility(scenario));
        Advance(s); Advance(s); Assert.Single(Colony(s).ProductionQueue!); Assert.Equal(0, Colony(s).ProductionQueue![0].CompletedIndustry);
        Assert.Contains(s.CaptureSnapshot().Ships, ship => ship.ShipId.Value == "ship.spawn.0000000000000003");
        Assert.Equal(5UL, s.CaptureSnapshot().NextEntitySequence); Assert.Equal(10, s.CaptureSnapshot().Ships.Last().CurrentHull);
        Advance(s); Assert.Equal(8, Colony(s).ProductionQueue![0].CompletedIndustry);
    }

    [Fact]
    public void ResearchGateOpensAfterUnlockAndNoIndustryStoredWhenQueueEmpty()
    {
        var s = EmptySession(); var command = Command(s, CommandKind.SetEngineeringTarget, "cmd.research"); command.SetEngineeringTarget = new() { TargetDefinitionId = "engineering.advanced-starship-engineering" };
        Assert.True(s.SubmitCommand(command).Accepted); Advance(s); Advance(s); Assert.Equal(CommandErrorCode.DesignEngineeringPrerequisiteMissing, Enqueue(s, design: "ship-design.advanced-scout").ErrorCode);
        Advance(s); Assert.True(Enqueue(s, design: "ship-design.advanced-scout").Accepted); Advance(s); Assert.Equal(8, Colony(s).ProductionQueue![0].CompletedIndustry);
    }

    [Theory]
    [InlineData("allocation")]
    [InlineData("later")]
    [InlineData("industry")]
    public void ProductionFaultsRollbackEarlierEconomyResearchAndCreatedEntities(string error)
    {
        var scenario = EmptyScenario(); var failed = true;
        var s = Session(scenario, (phase, _) => !(failed && error == "later" && phase == StrategicTurnPhase.Movement));
        Assert.True(Enqueue(s).Accepted); var save = s.ExportSave();
        save.GameState.Colonies!.Single(c => c.ColonyId == "colony.sol").ProductionQueue![0].CompletedIndustry = 19;
        if (error == "allocation") save.GameState.NextEntitySequence = ulong.MaxValue - 1;
        s = new(save, scenario.Definitions, Compatibility(scenario), new Dictionary<CommandKind, ICommandHandler>(), (phase, _) => !(failed && error == "later" && phase == StrategicTurnPhase.Movement));
        if (error == "industry")
        {
            scenario.Definitions.Infrastructure!.Single(i => i.DefinitionId == "infrastructure.shipyard").Industry = long.MaxValue;
            save.GameState.Colonies!.Single(c => c.ColonyId == "colony.sol").Automation = 10000;
            s = new(save, scenario.Definitions, Compatibility(scenario));
        }
        var before = Json(s);
        if (error == "later") Assert.Equal(TurnErrorCode.PhaseFailed, Advance(s).ErrorCode); else Assert.Throws<OverflowException>(() => s.AdvanceTurn(save.GameState.TurnNumber, save.GameState.StateRevision));
        Assert.Equal(before, Json(s)); failed = false;
        if (error == "later") { Assert.Equal(TurnStatus.Completed, Advance(s).Status); Assert.Single(s.CaptureSnapshot().Ships); }
    }
}
