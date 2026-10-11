using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.VerticalSliceFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class AiPlanningTests
{
    private static ScenarioDto OneAi()
    { var s = Scenario(); PopulationFixture.RemoveMars(s); s.InitialState.Civilizations!.Single(c => c.CivilizationId == "civ.cardassian").ControllerKind = ControllerKind.AI; return s; }
    private static void Node(ScenarioDto s, string id, long cost, params string[] prerequisites)
    { s.Definitions.DefinitionIds.Add(id); s.Definitions.EngineeringApplications!.Add(new() { DefinitionId = id, DisplayName = id, BaseEngineeringCost = cost*600, Kind = EngineeringProjectKind.NewCapability, ScienceRequirements = new(), PrerequisiteDefinitionIds = prerequisites.ToList() }); }
    private static void Design(ScenarioDto s, string id, long industry, params string[] research)
    { s.Definitions.DefinitionIds.Add(id); s.Definitions.ShipDesigns!.Add(new() { DefinitionId = id, DisplayName = id, MovementBudget = 1, IndustryCost = industry, CreditCost = 7, ResourceCosts = new() { new() { ResourceDefinitionId = "resource.dilithium", Amount = 1 } }, MaxHull = 12, CombatProfile = new(), RequiredEngineeringApplicationDefinitionIds = research.ToList() }); }

    [Fact]
    public void AiUsesLegalSharedCommandsInActorResearchColonyFleetOrderAndHumansAreNeverPlanned()
    {
        var s = Session(Scenario(false)); var result = Advance(s);
        Assert.Equal(new[] { "ai-command.t0000000001.a00000000.k8.n00000000", "ai-command.t0000000001.a00000000.k1.n00000000", "ai-command.t0000000001.a00000000.k4.n00000000",
            "ai-command.t0000000001.a00000001.k8.n00000000", "ai-command.t0000000001.a00000001.k1.n00000000", "ai-command.t0000000001.a00000001.k4.n00000000" }, result.CommandResults.Select(c => c.CommandId));
        Assert.All(result.CommandResults, r => { Assert.True(r.Accepted); Assert.True(EntityId.TryParse(r.CommandId, out _)); });
        Assert.Equal(new long[] { 1, 2, 3, 4, 5, 6 }, result.CommandResults.Select(c => c.StateRevision)); Assert.Equal(7, result.StateRevision); Assert.Empty(s.CaptureSnapshot().AcceptedCommandIds);
        var state = s.ExportSave().GameState; var human = state.Civilizations!.Single(c => c.CivilizationId == "civ.federation");
        Assert.Null(human.EngineeringTargetDefinitionId); Assert.Empty(human.MatureEngineeringApplicationDefinitionIds!); Assert.All(state.Colonies!.Where(c => c.OwnerCivilizationId == human.CivilizationId), c => Assert.Empty(c.ProductionQueue!));
        Assert.Equal("system.sol", state.Fleets!.Single(f => f.OwnerCivilizationId == human.CivilizationId).LocationNodeId);
        var before = Json(s); Assert.Equal(CommandErrorCode.ActorNotControlled, Enqueue(s, actor: "civ.cardassian", colony: "colony.cardassia").ErrorCode); Assert.Equal(before, Json(s));
        var command = Command(s, CommandKind.SetEngineeringTarget, "ai-command.t0000000002.a00000002.k8.n00000000"); command.SetEngineeringTarget = new() { TargetDefinitionId = "engineering.advanced-starship-engineering" };
        Assert.Equal(CommandErrorCode.ReservedCommandId, s.SubmitCommand(command).ErrorCode); Assert.Equal(before, Json(s));
    }

    [Fact]
    public void AiResearchAndDesignChoicesUseCostsPrerequisitesAndOrdinalTieBreaks()
    {
        var scenario = OneAi(); Node(scenario, "engineering.z", 2); Node(scenario, "engineering.a", 2); Node(scenario, "engineering.blocked", 1, "engineering.advanced-starship-engineering");
        Design(scenario, "ship-design.z", 10); Design(scenario, "ship-design.a", 10); Design(scenario, "ship-design.blocked", 1, "engineering.advanced-starship-engineering");
        scenario.Definitions.ShipDesigns!.Reverse(); scenario.Definitions.EngineeringApplications!.Reverse();
        var s = Session(scenario); var result = Advance(s); Assert.Equal(3, result.CommandResults.Count);
        var ai = s.ExportSave().GameState.Civilizations!.Single(c => c.CivilizationId == "civ.cardassian");
        Assert.Equal(new[] { "engineering.a" }, ai.MatureEngineeringApplicationDefinitionIds!); Assert.Equal(2*600, ai.EngineeringReserve); Assert.Null(ai.EngineeringTargetDefinitionId);
        var item = Assert.Single(s.ExportSave().GameState.Colonies!.Single(c => c.ColonyId == "colony.cardassia").ProductionQueue!); Assert.Equal("ship-design.a", item.ShipDesignDefinitionId); Assert.Equal(8, item.CompletedIndustry);
        Advance(s); Assert.Contains("engineering.z", s.ExportSave().GameState.Civilizations!.Single(c => c.CivilizationId == "civ.cardassian").MatureEngineeringApplicationDefinitionIds!);
        Assert.Contains(s.CaptureSnapshot().Ships, ship => ship.ShipDesignDefinitionId.Value == "ship-design.a");
    }

    [Fact]
    public void AiPlansBeforeEconomyAndOnlyForEmptyQueuesAndIdleFleets()
    {
        var scenario = OneAi(); PopulationFixture.SetDilithium(scenario.InitialState.Colonies!.Single(c => c.ColonyId == "colony.cardassia"), 1);
        scenario.InitialState.Fleets!.Single(f => f.OwnerCivilizationId == "civ.cardassian").RouteNodeIds = new() { "system.bajor", "system.sol" };
        var s = Session(scenario); var first = Advance(s); Assert.Single(first.CommandResults); Assert.Contains(".k8.", first.CommandResults[0].CommandId);
        Assert.Equal(4, PopulationFixture.Dilithium(s.ExportSave().GameState.Colonies!.Single(c => c.ColonyId == "colony.cardassia")));
        var second = Advance(s); Assert.Single(second.CommandResults); Assert.Contains(".k1.", second.CommandResults[0].CommandId);
        var third = Advance(s); Assert.DoesNotContain(third.CommandResults, r => r.CommandId!.Contains(".k1.", StringComparison.Ordinal));
        Assert.Equal("system.sol", s.CaptureSnapshot().Fleets.Single(f => f.FleetId.Value == "fleet.cardassian-scout").LocationNodeId.Value); // Contested regions freeze departure.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ForeignScoreWinsUniqueChoiceUsesNoRngAndNeutralTieUsesOneCampaignDraw(bool tied)
    {
        var scenario = OneAi(); var fleet = scenario.InitialState.Fleets!.Single(f => f.OwnerCivilizationId == "civ.cardassian"); fleet.LocationNodeId = "system.bajor";
        // Isolate AI routing draws from the new encounter-creation draws.
        var humanShips = scenario.InitialState.Ships!.Where(ship => ship.OwnerCivilizationId == "civ.federation").Select(ship => ship.ShipId).ToHashSet();
        var humanFleets = scenario.InitialState.Fleets!.Where(f => f.OwnerCivilizationId == "civ.federation").Select(f => f.FleetId).ToHashSet();
        scenario.InitialState.Ships!.RemoveAll(ship => humanShips.Contains(ship.ShipId)); scenario.InitialState.Fleets!.RemoveAll(f => humanFleets.Contains(f.FleetId));
        scenario.InitialState.EntityIdentities.RemoveAll(e => humanShips.Contains(e.EntityId) || humanFleets.Contains(e.EntityId));
        if (tied)
        { scenario.InitialState.StarSystems!.Single(n => n.SystemId == "system.sol").OwnerCivilizationId = null; scenario.InitialState.Colonies!.Single(c => c.ColonyId == "colony.sol").OwnerCivilizationId = null; }
        var s = Session(scenario); Advance(s); var current = s.CaptureSnapshot().Fleets.Single(f => f.FleetId.Value == fleet.FleetId);
        if (!tied) { Assert.Equal("system.sol", current.LocationNodeId.Value); Assert.Equal(47UL, s.CaptureSnapshot().RngState.State); }
        else
        {
            Assert.Equal(11400714819323198532UL, s.CaptureSnapshot().RngState.State);
            Assert.Contains(current.LocationNodeId.Value, new[] { "system.bajoran-wormhole", "system.sol" });
        }
    }

    [Theory]
    [InlineData(47UL, "node.z", 11400714819323198532UL)]
    [InlineData(49UL, "node.a", 11400714819323198534UL)]
    public void FranchiseNeutralTiedRoutingIsRepeatableIndependentOfCollectionOrderAndNeverAllocatesCommandIds(ulong seed, string destination, ulong expectedState)
    {
        // Use authored neutral IDs; this fixture has no franchise definitions, labels or systems.
        var defs = new DefinitionCatalogDto
        {
            ContentFormatVersion = FoundationVersions.ContentFormatVersion,
            DefinitionIds = new() { "civilization.fixture", "system.fixture", "ship-design.fixture" },
            Civilizations = new() { new() { DefinitionId = "civilization.fixture", DisplayName = "Fixture" } },
            StarSystems = new() { new() { DefinitionId = "system.fixture", DisplayName = "Fixture" } },
            Species = new(),
            Planets = new(),
            Resources = new(), GovernancePolicies = new(), PoliticalMemoryEffects = new(),
            Infrastructure = new(),
            EngineeringApplications = new(),
            ShipDesigns = new() { new() { DefinitionId = "ship-design.fixture", DisplayName = "Fixture", MovementBudget = 1, IndustryCost = 1, MaxHull = 1, CombatProfile = new(), ResourceCosts = new(), RequiredEngineeringApplicationDefinitionIds = new() } },
            GalaxyEdges = new() { new() { EndpointA = "node.origin", EndpointB = "node.z", Kind = GalaxyEdgeKind.Normal }, new() { EndpointA = "node.origin", EndpointB = "node.a", Kind = GalaxyEdgeKind.Wormhole } }
        };
        var state = new GameStateDto
        {
            Planets = new(),
            Displacements = new(),
            LastDeliveries = new(),
            PopulationStatistics = new(),
            PendingCombats = new(),
            ResolvedCombats = new(),
            Phase = SessionPhase.Planning, ResearchPaceFactor = 10000, LastResearchReceipts = new(),
            TurnNumber = 1,
            NextEntitySequence = 1,
            RngState = new SplitMix64Random(seed).ToDto(),
            AcceptedCommandIds = new(),
            Colonies = new(),
            Civilizations = new() { new() { CivilizationId = "civ.fixture", DefinitionId = "civilization.fixture", ControllerKind = ControllerKind.AI, MatureEngineeringApplicationDefinitionIds = new() } },
            StarSystems = new() { new() { SystemId = "node.origin", DefinitionId = "system.fixture", OwnerCivilizationId = "civ.fixture" }, new() { SystemId = "node.z", DefinitionId = "system.fixture" }, new() { SystemId = "node.a", DefinitionId = "system.fixture" } },
            Ships = new() { new() { ShipId = "ship.fixture", ShipDesignDefinitionId = "ship-design.fixture", OwnerCivilizationId = "civ.fixture", FleetId = "fleet.fixture", CurrentHull = 1 } },
            Fleets = new() { new() { FleetId = "fleet.fixture", OwnerCivilizationId = "civ.fixture", LocationNodeId = "node.origin", ShipIds = new() { "ship.fixture" }, RouteNodeIds = new() } }
        };
        state.EntityIdentities = state.StarSystems.Select(n => new EntityIdentityDto { EntityId = n.SystemId, DefinitionId = n.DefinitionId }).Concat(new[] { new EntityIdentityDto { EntityId = "civ.fixture", DefinitionId = "civilization.fixture" },
            new EntityIdentityDto { EntityId = "ship.fixture", DefinitionId = "ship-design.fixture" }, new EntityIdentityDto { EntityId = "fleet.fixture" } }).ToList();
        NavigationFixture.Definitions(defs,state.StarSystems.Select(n=>n.SystemId).ToArray()); NavigationFixture.State(state,defs);
        var fixture = new ScenarioDto { ScenarioId = "scenario.ai-tie", RulesVersion = "fixture-v1", ContentHash = new string('b', 64), Definitions = defs, InitialState = state };
        var a = Session(fixture); state.StarSystems.Reverse(); state.EntityIdentities.Reverse(); defs.GalaxyEdges.Reverse(); var b = Session(fixture);
        Assert.Equal(JsonSerializer.Serialize(Advance(a)), JsonSerializer.Serialize(Advance(b))); Assert.Equal(Json(a), Json(b));
        Assert.Equal(expectedState, a.CaptureSnapshot().RngState.State); Assert.Equal(destination, Assert.Single(a.CaptureSnapshot().Fleets).LocationNodeId.Value); Assert.Equal(1UL, a.CaptureSnapshot().NextEntitySequence); Assert.Equal(2, a.CaptureSnapshot().StateRevision);
    }

    private sealed class FailingEnqueue : ICommandHandler
    {
        public CommandErrorCode Execute(GameStateDto state, CommandEnvelope command)
        { state.Civilizations![0].Credits = 0; state.RngState.State = 999; state.NextEntitySequence = 900; return CommandErrorCode.InsufficientStrategicResource; }
    }
    [Fact]
    public void RejectedAiActionDiscardsItsPrivateStagingWithoutPartialPaymentOrAllocation()
    {
        var scenario = OneAi(); var s = new SimulationSession(Save(scenario), scenario.Definitions, Compatibility(scenario), new Dictionary<CommandKind, ICommandHandler> { [CommandKind.EnqueueProduction] = new FailingEnqueue() });
        var result = Advance(s); var failure = Assert.Single(result.CommandResults, r => !r.Accepted); Assert.Equal(CommandErrorCode.InsufficientStrategicResource, failure.ErrorCode);
        Assert.Equal(3, result.StateRevision); // Research + route + turn commit.
        Assert.Equal(56, s.CaptureSnapshot().Civilizations.Single(c => c.CivilizationId.Value == "civ.cardassian").Credits);
        Assert.Equal(13, PopulationFixture.Dilithium(s.ExportSave().GameState.Colonies!.Single(c => c.ColonyId == "colony.cardassia")));
        Assert.Empty(s.ExportSave().GameState.Colonies!.Single(c => c.ColonyId == "colony.cardassia").ProductionQueue!);
        Assert.Equal(47UL, s.CaptureSnapshot().RngState.State); Assert.Equal(1UL, s.CaptureSnapshot().NextEntitySequence);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("throw")]
    [InlineData("invariant")]
    public void LaterFailureRollsBackSuccessfulAiEnqueueLedgerRngAndEveryEarlierPhase(string fault)
    {
        var scenario = OneAi(); var failed = true;
        var s = Session(scenario, (phase, staging) =>
        {
            if (phase == StrategicTurnPhase.Movement && failed)
            {
                Assert.Single(staging.Colonies!.Single(c => c.ColonyId == "colony.cardassia").ProductionQueue!);
                Assert.True(staging.NextEntitySequence > 1); Assert.NotEmpty(staging.AcceptedCommandIds!);
                if (fault == "false") return false; if (fault == "throw") throw new ApplicationException("Injected after AI."); staging.Fleets![0].LocationNodeId = "system.absent";
            }
            return true;
        });
        var before = Json(s); if (fault == "false") Assert.Equal(TurnStatus.Rejected, Advance(s).Status); else if (fault == "throw") Assert.Throws<ApplicationException>(() => Advance(s)); else Assert.Throws<InvalidOperationException>(() => Advance(s));
        Assert.Equal(before, Json(s)); failed = false; var baseline = Session(scenario); Assert.Equal(JsonSerializer.Serialize(Advance(baseline)), JsonSerializer.Serialize(Advance(s))); Assert.Equal(Json(baseline), Json(s));
    }
    [Fact]
    public void OwnedColoniesArePlannedOrdinallyUsingRemainingTreasuryBeforeEconomy()
    {
        var scenario = OneAi();
        scenario.InitialState.Civilizations!.Single(c => c.CivilizationId == "civ.cardassian").Credits = 10;
        scenario.InitialState.Colonies!.Single(c => c.ColonyId == "colony.bajor").OwnerCivilizationId = "civ.cardassian";
        scenario.InitialState.StarSystems!.Single(n => n.SystemId == "system.bajor").OwnerCivilizationId = "civ.cardassian";
        // Isolate stationary production from the new reachable-target route search.
        foreach(var edge in scenario.InitialState.NavigationKnowledge!.Single(k=>k.CivilizationId=="civ.cardassian").Connections!) edge.Level=NavigationKnowledgeLevel.Unknown;
        scenario.InitialState.Colonies!.Reverse(); var s = Session(scenario); var result = Advance(s);
        Assert.Equal(3, result.CommandResults.Count); Assert.Contains(".k1.n00000000", result.CommandResults[2].CommandId);
        Assert.Single(s.ExportSave().GameState.Colonies!.Single(c => c.ColonyId == "colony.bajor").ProductionQueue!);
        Assert.Empty(s.ExportSave().GameState.Colonies!.Single(c => c.ColonyId == "colony.cardassia").ProductionQueue!);
        Assert.Equal(12, s.CaptureSnapshot().Civilizations.Single(c => c.CivilizationId.Value == "civ.cardassian").Credits);
        Assert.Equal(47UL, s.CaptureSnapshot().RngState.State); // Only adjacent node is owned: no routing action.
    }

    private sealed class FailingRoute : ICommandHandler
    {
        public CommandErrorCode Execute(GameStateDto state, CommandEnvelope command)
        { state.Fleets![0].RouteNodeIds!.Clear(); state.RngState.State = 999; return CommandErrorCode.InvalidRoute; }
    }
    [Fact]
    public void RejectedTiedRouteDoesNotConsumeTheProposedCampaignDraw()
    {
        var scenario = OneAi(); var fleet = scenario.InitialState.Fleets!.Single(f => f.OwnerCivilizationId == "civ.cardassian"); fleet.LocationNodeId = "system.bajor";
        scenario.InitialState.StarSystems!.Single(n => n.SystemId == "system.sol").OwnerCivilizationId = null;
        scenario.InitialState.Colonies!.Single(c => c.ColonyId == "colony.sol").OwnerCivilizationId = null;
        var s = new SimulationSession(Save(scenario), scenario.Definitions, Compatibility(scenario), new Dictionary<CommandKind, ICommandHandler> { [CommandKind.SetFleetRoute] = new FailingRoute() });
        var result = Advance(s); Assert.Equal(CommandErrorCode.InvalidRoute, Assert.Single(result.CommandResults, c => !c.Accepted).ErrorCode);
        Assert.Equal(47UL, s.CaptureSnapshot().RngState.State); Assert.Equal("system.bajor", s.CaptureSnapshot().Fleets.Single(f => f.FleetId.Value == fleet.FleetId).LocationNodeId.Value);
    }

    [Theory]
    [InlineData("movement")]
    [InlineData("encounter")]
    [InlineData("allocation")]
    public void CompletionTurnFaultDiscardsNewAiShipsFleetsAndAllEarlierWork(string fault)
    {
        var scenario = OneAi(); var preparation = Session(scenario); Advance(preparation); Advance(preparation);
        var save = preparation.ExportSave();
        if (fault == "allocation") save.GameState.NextEntitySequence = ulong.MaxValue - 1;
        var failed = true; var s = new SimulationSession(save, scenario.Definitions, Compatibility(scenario), new Dictionary<CommandKind, ICommandHandler>(), (phase, staging) =>
        {
            if (failed && fault != "allocation" && phase == (fault == "movement" ? StrategicTurnPhase.Movement : StrategicTurnPhase.EncounterCreation))
            {
                Assert.Equal(4, staging.Ships!.Count); Assert.Equal(4, staging.Fleets!.Count);
                Assert.Equal(5UL, staging.NextEntitySequence); // One earlier encounter plus this ship/fleet.
                Assert.Empty(staging.Colonies!.Single(c => c.ColonyId == "colony.cardassia").ProductionQueue!);
                Assert.Contains("engineering.advanced-starship-engineering", staging.Civilizations!.Single(c => c.CivilizationId == "civ.cardassian").MatureEngineeringApplicationDefinitionIds!);
                return false;
            }
            return true;
        });
        var before = Json(s);
        if (fault == "allocation") Assert.Throws<OverflowException>(() => Advance(s)); else Assert.Equal(TurnStatus.Rejected, Advance(s).Status);
        Assert.Equal(before, Json(s)); failed = false;
        if (fault != "allocation")
        { var uninterrupted = new SimulationSession(save, scenario.Definitions, Compatibility(scenario)); Assert.Equal(JsonSerializer.Serialize(Advance(uninterrupted)), JsonSerializer.Serialize(Advance(s))); Assert.Equal(Json(uninterrupted), Json(s)); }
    }

    [Fact]
    public void RevisionExhaustionAfterAcceptedAiActionDiscardsTheWholeTurn()
    {
        var scenario = OneAi(); scenario.InitialState.StateRevision = long.MaxValue - 1;
        var s = Session(scenario); var before = Json(s);
        Assert.Throws<OverflowException>(() => Advance(s)); Assert.Equal(before, Json(s));
    }

    [Fact]
    public void AiBuildsJoinAnExistingStationaryFleetWhenAllHomeNeighborsAreOwned()
    {
        var scenario = OneAi();
        scenario.InitialState.StarSystems!.Single(n => n.SystemId == "system.bajor").OwnerCivilizationId = "civ.cardassian";
        scenario.InitialState.Colonies!.Single(c => c.ColonyId == "colony.bajor").OwnerCivilizationId = "civ.cardassian";
        // Isolate stationary production from the new reachable-target route search.
        foreach(var edge in scenario.InitialState.NavigationKnowledge!.Single(k=>k.CivilizationId=="civ.cardassian").Connections!) edge.Level=NavigationKnowledgeLevel.Unknown;
        var s = Session(scenario);
        for (var turn = 0; turn < 25; turn++) Assert.Equal(TurnStatus.Completed, Advance(s).Status);
        var home = s.CaptureSnapshot().Fleets.Single(f => f.FleetId.Value == "fleet.cardassian-scout");
        Assert.Equal("system.cardassia", home.LocationNodeId.Value); Assert.Empty(home.RouteNodeIds); Assert.True(home.ShipIds.Count > 1);
        Assert.All(s.CaptureSnapshot().Ships.Where(ship => home.ShipIds.Contains(ship.ShipId)), ship => Assert.Equal(home.FleetId, ship.FleetId));
    }

}
