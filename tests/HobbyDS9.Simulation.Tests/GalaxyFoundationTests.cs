using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class GalaxyFoundationTests
{
    private static EntityId Id(string value) => new(value);
    private static GalaxyEdgeDto Edge(string a, string b, GalaxyEdgeKind kind = GalaxyEdgeKind.Normal) =>
        new() { EndpointA = a, EndpointB = b, Kind = kind };
    private static ScenarioDto Scenario()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "scenarios", "ds9-slice", "scenario.json"));
        var scenario = JsonSerializer.Deserialize<ScenarioDto>(bytes)!;
        scenario.ContentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return scenario;
    }
    private static SaveCompatibility Compatibility(ScenarioDto s) => new(s.ScenarioId, s.RulesVersion, s.ContentHash);
    private static SaveEnvelopeDto Save(ScenarioDto s) => new()
    {
        SaveFormatVersion = FoundationVersions.SaveFormatVersion, ScenarioId = s.ScenarioId,
        RulesVersion = s.RulesVersion, ContentHash = s.ContentHash, GameState = s.InitialState
    };
    private static SimulationSession Session(ScenarioDto s) => new(Save(s), s.Definitions, Compatibility(s));
    private static string GraphJson(GalaxyGraph g) => JsonSerializer.Serialize(new { g.Nodes, g.Edges });

    [Fact]
    public void FixedSkeletonContainsOnlyRequiredCivilizationsNodesOwnersAndEdges()
    {
        var scenario = Scenario();
        var report = FoundationValidator.ValidateScenario(scenario);
        Assert.True(report.IsValid, string.Join(", ", report.Issues.Select(i => i.Code + ":" + i.Path)));
        Assert.Equal("ds9-strategic-slice-v1", scenario.ScenarioId);
        Assert.Equal(new[] { "Cardassian Union", "Dominion", "Federation" }, scenario.Definitions.Civilizations!.Select(c => c.DisplayName).OrderBy(n => n, StringComparer.Ordinal));
        Assert.Equal(new[] { "Bajor", "Bajoran Wormhole", "Cardassia", "Founders' Homeworld", "Gamma Exit", "Sol" },
            scenario.Definitions.StarSystems!.Select(n => n.DisplayName).OrderBy(n => n, StringComparer.Ordinal));
        var session = Session(scenario);
        var state = session.ExportSave().GameState;
        Assert.Equal(3, state.Civilizations!.Count);
        Assert.Equal(6, state.StarSystems!.Count);
        Assert.Equal(9, state.EntityIdentities.Count);
        Assert.Equal(5, session.Galaxy.Edges.Count);
        Assert.Equal(ControllerKind.Human, state.Civilizations.Single(c => c.CivilizationId == "civ.federation").ControllerKind);
        Assert.All(state.Civilizations.Where(c => c.CivilizationId != "civ.federation"), c => Assert.Equal(ControllerKind.AI, c.ControllerKind));
        Assert.Equal("civ.federation", state.StarSystems.Single(n => n.SystemId == "system.sol").OwnerCivilizationId);
        Assert.Equal("civ.cardassian", state.StarSystems.Single(n => n.SystemId == "system.cardassia").OwnerCivilizationId);
        Assert.Equal("civ.dominion", state.StarSystems.Single(n => n.SystemId == "system.founders-homeworld").OwnerCivilizationId);
        Assert.All(state.StarSystems.Where(n => n.SystemId == "system.bajor" || n.SystemId == "system.bajoran-wormhole" || n.SystemId == "system.gamma-exit"), n => Assert.Null(n.OwnerCivilizationId));
        var wormhole = Assert.Single(session.Galaxy.Edges, e => e.Kind == GalaxyEdgeKind.Wormhole);
        Assert.Equal("system.bajoran-wormhole", wormhole.EndpointA.Value);
        Assert.Equal("system.gamma-exit", wormhole.EndpointB.Value);
        Assert.All(session.Galaxy.Edges, e => Assert.Equal(1, e.TraversalCost));
        Assert.Equal(47UL, state.RngState.State);
        Assert.Equal(1UL, state.NextEntitySequence);
        Assert.Equal(0, state.StateRevision);
        Assert.Empty(state.AcceptedCommandIds!);
    }

    [Theory]
    [InlineData("system.sol", "system.founders-homeworld", "system.bajor,system.bajoran-wormhole,system.gamma-exit,system.founders-homeworld")]
    [InlineData("system.founders-homeworld", "system.sol", "system.gamma-exit,system.bajoran-wormhole,system.bajor,system.sol")]
    [InlineData("system.sol", "system.cardassia", "system.bajor,system.cardassia")]
    [InlineData("system.bajoran-wormhole", "system.gamma-exit", "system.gamma-exit")]
    [InlineData("system.sol", "system.sol", "")]
    public void SkeletonBfsMatchesKnownRoutes(string origin, string destination, string expected)
    {
        var graph = Session(Scenario()).Galaxy;
        var result = graph.FindRoute(Id(origin), Id(destination));
        Assert.True(result.Success);
        Assert.Equal(expected == "" ? Array.Empty<string>() : expected.Split(','), result.Steps.Select(n => n.Value));
        Assert.True(graph.ValidateRoute(Id(origin), result.Steps).Success);
    }

    [Fact]
    public void AllScenarioInputOrderingAndEdgeOrientationAreIrrelevant()
    {
        var scenario = Scenario();
        var baseline = Session(scenario);
        var save = JsonSerializer.Serialize(baseline.ExportSave());
        var snapshot = JsonSerializer.Serialize(baseline.CaptureSnapshot());
        var graph = GraphJson(baseline.Galaxy);
        var random = new Random(1047); // Test permutation generator, never simulation RNG.
        for (var i = 0; i < 40; i++)
        {
            scenario.Definitions.DefinitionIds = scenario.Definitions.DefinitionIds.OrderBy(_ => random.Next()).ToList();
            scenario.Definitions.Civilizations = scenario.Definitions.Civilizations!.OrderBy(_ => random.Next()).ToList();
            scenario.Definitions.StarSystems = scenario.Definitions.StarSystems!.OrderBy(_ => random.Next()).ToList();
            scenario.InitialState.EntityIdentities = scenario.InitialState.EntityIdentities.OrderBy(_ => random.Next()).ToList();
            scenario.InitialState.Civilizations = scenario.InitialState.Civilizations!.OrderBy(_ => random.Next()).ToList();
            scenario.InitialState.StarSystems = scenario.InitialState.StarSystems!.OrderBy(_ => random.Next()).ToList();
            scenario.Definitions.GalaxyEdges = scenario.Definitions.GalaxyEdges!.OrderBy(_ => random.Next()).ToList();
            foreach (var edge in scenario.Definitions.GalaxyEdges)
                if (random.Next(2) == 1) (edge.EndpointA, edge.EndpointB) = (edge.EndpointB, edge.EndpointA);
            var session = Session(scenario);
            Assert.True(FoundationValidator.ValidateScenario(scenario).IsValid);
            Assert.Equal(save, JsonSerializer.Serialize(session.ExportSave()));
            Assert.Equal(snapshot, JsonSerializer.Serialize(session.CaptureSnapshot()));
            Assert.Equal(graph, GraphJson(session.Galaxy));
            foreach (var origin in baseline.Galaxy.Nodes)
            foreach (var destination in baseline.Galaxy.Nodes)
                Assert.Equal(baseline.Galaxy.FindRoute(origin, destination).Steps, session.Galaxy.FindRoute(origin, destination).Steps);
        }
    }

    [Fact]
    public void BfsUsesOrdinalNeighborsForEqualLengthPathsAndUnitCostForWormholes()
    {
        var nodes = new[] { Id("node.d"), Id("node.c"), Id("node.b"), Id("node.a"), Id("node.isolated") };
        var edges = new[] { Edge("node.a", "node.c", GalaxyEdgeKind.Wormhole), Edge("node.c", "node.d"), Edge("node.a", "node.b"), Edge("node.b", "node.d", GalaxyEdgeKind.Wormhole) };
        foreach (var ordered in new[] { edges, edges.Reverse().ToArray() })
        {
            var graph = new GalaxyGraph(nodes, ordered);
            Assert.Equal(new[] { "node.b", "node.c" }, graph.GetNeighbors(Id("node.a")).Select(n => n.Value));
            Assert.Equal(new[] { "node.b", "node.d" }, graph.FindRoute(Id("node.a"), Id("node.d")).Steps.Select(n => n.Value));
            Assert.Equal(RouteErrorCode.Unreachable, graph.FindRoute(Id("node.a"), Id("node.isolated")).ErrorCode);
        }
    }

    [Theory]
    [InlineData("duplicate-node", ValidationCode.DuplicateId)]
    [InlineData("missing-endpoint", ValidationCode.MissingEndpoint)]
    [InlineData("self-edge", ValidationCode.SelfEdge)]
    [InlineData("duplicate-edge", ValidationCode.DuplicateEdge)]
    [InlineData("reversed-edge", ValidationCode.DuplicateEdge)]
    [InlineData("different-kind-duplicate", ValidationCode.DuplicateEdge)]
    [InlineData("invalid-node", ValidationCode.InvalidId)]
    [InlineData("invalid-endpoint", ValidationCode.InvalidId)]
    [InlineData("invalid-kind", ValidationCode.InvalidEdgeKind)]
    [InlineData("missing-kind", ValidationCode.MissingField)]
    [InlineData("null-edge", ValidationCode.MissingField)]
    [InlineData("null-edges", ValidationCode.MissingField)]
    [InlineData("null-nodes", ValidationCode.MissingField)]
    public void GraphValidationRejectsInvalidTopologyWithoutChangingDtos(string example, ValidationCode expected)
    {
        List<string>? nodes = new() { "node.a", "node.b" };
        List<GalaxyEdgeDto>? edges = new() { Edge("node.a", "node.b") };
        switch (example)
        {
            case "duplicate-node": nodes.Add("node.a"); break;
            case "missing-endpoint": edges[0].EndpointB = "node.absent"; break;
            case "self-edge": edges[0].EndpointB = "node.a"; break;
            case "duplicate-edge": edges.Add(Edge("node.a", "node.b")); break;
            case "reversed-edge": edges.Add(Edge("node.b", "node.a")); break;
            case "different-kind-duplicate": edges.Add(Edge("node.b", "node.a", GalaxyEdgeKind.Wormhole)); break;
            case "invalid-node": nodes[0] = "Bad"; break;
            case "invalid-endpoint": edges[0].EndpointA = "Bad"; break;
            case "invalid-kind": edges[0].Kind = (GalaxyEdgeKind)99; break;
            case "missing-kind": edges[0].Kind = null; break;
            case "null-edge": edges.Add(null!); break;
            case "null-edges": edges = null; break;
            case "null-nodes": nodes = null; break;
        }
        var before = JsonSerializer.Serialize(new { nodes, edges });
        var report = GalaxyGraph.Validate(nodes, edges);
        Assert.Contains(report.Issues, issue => issue.Code == expected);
        Assert.Equal(before, JsonSerializer.Serialize(new { nodes, edges }));
        if (nodes != null && edges != null && nodes.All(n => EntityId.TryParse(n, out _)))
            Assert.Throws<ArgumentException>(() => new GalaxyGraph(nodes.Select(Id), edges));
    }

    [Theory]
    [InlineData("missing-controller", ValidationCode.MissingField)]
    [InlineData("unknown-controller", ValidationCode.InvalidControllerKind)]
    [InlineData("missing-owner", ValidationCode.MissingCivilization)]
    [InlineData("bad-owner", ValidationCode.InvalidId)]
    [InlineData("duplicate-civilization", ValidationCode.DuplicateId)]
    [InlineData("duplicate-system", ValidationCode.DuplicateId)]
    [InlineData("missing-header", ValidationCode.MissingEntity)]
    [InlineData("mismatched-header", ValidationCode.IdentityMismatch)]
    [InlineData("wrong-definition-kind", ValidationCode.MissingDefinition)]
    [InlineData("empty-label", ValidationCode.MissingField)]
    [InlineData("duplicate-definition", ValidationCode.DuplicateId)]
    [InlineData("old-content", ValidationCode.UnsupportedContentFormat)]
    [InlineData("old-save", ValidationCode.UnsupportedSaveFormat)]
    [InlineData("missing-civilizations", ValidationCode.MissingField)]
    [InlineData("missing-systems", ValidationCode.MissingField)]
    [InlineData("missing-content-civilizations", ValidationCode.MissingField)]
    [InlineData("missing-content-systems", ValidationCode.MissingField)]
    [InlineData("missing-content-edges", ValidationCode.MissingField)]
    [InlineData("null-civilization", ValidationCode.MissingField)]
    [InlineData("null-system", ValidationCode.MissingField)]
    [InlineData("null-civilization-definition", ValidationCode.MissingField)]
    [InlineData("null-system-definition", ValidationCode.MissingField)]
    public void SessionIngressRejectsInvalidTypedStateAndContent(string example, ValidationCode expected)
    {
        var s = Scenario(); var save = Save(s);
        switch (example)
        {
            case "missing-controller": s.InitialState.Civilizations![0].ControllerKind = null; break;
            case "unknown-controller": s.InitialState.Civilizations![0].ControllerKind = (ControllerKind)0; break;
            case "missing-owner": s.InitialState.StarSystems![0].OwnerCivilizationId = "civ.absent"; break;
            case "bad-owner": s.InitialState.StarSystems![0].OwnerCivilizationId = "Bad"; break;
            case "duplicate-civilization": s.InitialState.Civilizations!.Add(s.InitialState.Civilizations[0]); break;
            case "duplicate-system": s.InitialState.StarSystems!.Add(s.InitialState.StarSystems[0]); break;
            case "missing-header": s.InitialState.EntityIdentities.RemoveAt(0); break;
            case "mismatched-header": s.InitialState.EntityIdentities[0].DefinitionId = null; break;
            case "wrong-definition-kind": s.InitialState.Civilizations![0].DefinitionId = s.Definitions.StarSystems![0].DefinitionId; break;
            case "empty-label": s.Definitions.StarSystems![0].DisplayName = " "; break;
            case "duplicate-definition": s.Definitions.StarSystems!.Add(s.Definitions.StarSystems[0]); break;
            case "old-content": s.Definitions.ContentFormatVersion = 0; break;
            case "old-save": save.SaveFormatVersion = 2; break;
            case "missing-civilizations": s.InitialState.Civilizations = null; break;
            case "missing-systems": s.InitialState.StarSystems = null; break;
            case "missing-content-civilizations": s.Definitions.Civilizations = null; break;
            case "missing-content-systems": s.Definitions.StarSystems = null; break;
            case "missing-content-edges": s.Definitions.GalaxyEdges = null; break;
            case "null-civilization": s.InitialState.Civilizations!.Add(null!); break;
            case "null-system": s.InitialState.StarSystems!.Add(null!); break;
            case "null-civilization-definition": s.Definitions.Civilizations!.Add(null!); break;
            case "null-system-definition": s.Definitions.StarSystems!.Add(null!); break;
        }
        var before = JsonSerializer.Serialize(new { s, save });
        var report = FoundationValidator.ValidateSave(save, s.Definitions, Compatibility(s));
        Assert.Contains(report.Issues, i => i.Code == expected);
        Assert.Throws<ArgumentException>(() => new SimulationSession(save, s.Definitions, Compatibility(s)));
        Assert.Equal(before, JsonSerializer.Serialize(new { s, save }));
    }

    [Fact]
    public void ExplicitRouteValidationHandlesEmptyBacktrackingAndInvalidStepsWithoutMutation()
    {
        var session = Session(Scenario()); var graph = session.Galaxy;
        var before = JsonSerializer.Serialize(session.ExportSave());
        Assert.True(graph.ValidateRoute(Id("system.sol"), Array.Empty<EntityId>()).Success);
        Assert.True(graph.ValidateRoute(Id("system.sol"), new[] { Id("system.bajor"), Id("system.sol") }).Success);
        Assert.Equal(RouteErrorCode.NonAdjacent, graph.ValidateRoute(Id("system.sol"), new[] { Id("system.sol") }).ErrorCode);
        Assert.Equal(RouteErrorCode.NonAdjacent, graph.ValidateRoute(Id("system.sol"), new[] { Id("system.cardassia") }).ErrorCode);
        Assert.Equal(RouteErrorCode.UnknownSystem, graph.ValidateRoute(Id("system.sol"), new[] { Id("system.unknown") }).ErrorCode);
        Assert.Equal(RouteErrorCode.InvalidSystemId, graph.ValidateRoute(Id("system.sol"), new EntityId[] { default }).ErrorCode);
        Assert.Equal(RouteErrorCode.InvalidRoute, graph.ValidateRoute(Id("system.sol"), null).ErrorCode);
        Assert.Equal(RouteErrorCode.UnknownSystem, graph.ValidateRoute(Id("system.unknown"), Array.Empty<EntityId>()).ErrorCode);
        Assert.Equal(RouteErrorCode.InvalidSystemId, graph.ValidateRoute(default, Array.Empty<EntityId>()).ErrorCode);
        Assert.Equal(RouteErrorCode.InvalidSystemId, graph.FindRoute(default, Id("system.sol")).ErrorCode);
        Assert.Equal(RouteErrorCode.UnknownSystem, graph.FindRoute(Id("system.sol"), Id("system.unknown")).ErrorCode);
        Assert.Equal(before, JsonSerializer.Serialize(session.ExportSave()));
    }

    [Fact]
    public void ControllerAndOwnershipSurviveSaveReloadAndDrivePublicCommandControl()
    {
        var s = Scenario(); var session = Session(s);
        var save = JsonSerializer.Deserialize<SaveEnvelopeDto>(JsonSerializer.Serialize(session.ExportSave()))!;
        var content = JsonSerializer.Deserialize<DefinitionCatalogDto>(JsonSerializer.Serialize(s.Definitions))!;
        var resumed = new SimulationSession(save, content, Compatibility(s));
        Assert.Equal(JsonSerializer.Serialize(session.CaptureSnapshot()), JsonSerializer.Serialize(resumed.CaptureSnapshot()));
        Assert.Equal(GraphJson(session.Galaxy), GraphJson(resumed.Galaxy));
        CommandEnvelope Command(string actor) => new()
        {
            CommandId = "cmd.test", ActorCivilizationId = actor, ExpectedTurn = 1, ExpectedRevision = 0,
            Kind = CommandKind.SetResearchTarget, SetResearchTarget = new() { TargetDefinitionId = "definition.future" }
        };
        Assert.Equal(CommandErrorCode.UnsupportedCommand, resumed.SubmitCommand(Command("civ.federation")).ErrorCode);
        Assert.Equal(CommandErrorCode.ActorNotControlled, resumed.SubmitCommand(Command("civ.cardassian")).ErrorCode);
        Assert.Equal(CommandErrorCode.ActorNotControlled, resumed.SubmitCommand(Command("civ.dominion")).ErrorCode);
        Assert.Equal(CommandErrorCode.UnknownActor, resumed.SubmitCommand(Command("system.sol")).ErrorCode);
        Assert.Equal(JsonSerializer.Serialize(session.ExportSave()), JsonSerializer.Serialize(resumed.ExportSave()));
        save.GameState.Civilizations!.Single(c => c.CivilizationId == "civ.federation").ControllerKind = ControllerKind.AI;
        var changed = new SimulationSession(save, content, Compatibility(s));
        Assert.Equal(CommandErrorCode.ActorNotControlled, changed.SubmitCommand(Command("civ.federation")).ErrorCode);
        Assert.Equal(CommandErrorCode.UnsupportedCommand, resumed.SubmitCommand(Command("civ.federation")).ErrorCode);
    }

    [Fact]
    public void NewDtoCollectionsAndGraphAreDetachedAndImmutable()
    {
        var s = Scenario(); var session = Session(s); var snapshot = session.CaptureSnapshot();
        var baseline = JsonSerializer.Serialize(session.ExportSave()); var graph = GraphJson(session.Galaxy);
        s.InitialState.Civilizations![0].ControllerKind = ControllerKind.AI;
        s.InitialState.Civilizations.Clear();
        s.InitialState.StarSystems![0].OwnerCivilizationId = null;
        s.InitialState.StarSystems.Clear();
        s.Definitions.Civilizations![0].DisplayName = "Other";
        s.Definitions.StarSystems![0].DefinitionId = "other";
        s.Definitions.GalaxyEdges![0].Kind = GalaxyEdgeKind.Wormhole;
        s.Definitions.GalaxyEdges.Clear();
        var exported = session.ExportSave();
        exported.GameState.Civilizations![0].ControllerKind = ControllerKind.Human;
        exported.GameState.Civilizations.Clear();
        exported.GameState.StarSystems![0].OwnerCivilizationId = "civ.other";
        exported.GameState.StarSystems.Clear();
        Assert.Equal(baseline, JsonSerializer.Serialize(session.ExportSave()));
        Assert.Equal(graph, GraphJson(session.Galaxy));
        Assert.Equal(3, snapshot.Civilizations.Count); Assert.Equal(6, snapshot.StarSystems.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<CivilizationSnapshot>)snapshot.Civilizations).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<StarSystemSnapshot>)snapshot.StarSystems).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<GalaxyEdge>)snapshot.GalaxyEdges).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<EntityId>)session.Galaxy.Nodes).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<EntityId>)session.Galaxy.GetNeighbors(Id("system.sol"))).Clear());
        var route = session.Galaxy.FindRoute(Id("system.sol"), Id("system.bajor"));
        Assert.Throws<NotSupportedException>(() => ((IList<EntityId>)route.Steps).Clear());
        foreach (var type in new[] { typeof(CivilizationSnapshot), typeof(StarSystemSnapshot), typeof(GalaxyEdge), typeof(GalaxyGraph) })
            Assert.All(type.GetProperties(), p => Assert.Null(p.SetMethod));
    }

    [Fact]
    public void StandaloneGraphCopiesItsInputsAndCanonicalizesEveryEdge()
    {
        var nodes = new List<EntityId> { Id("node.b"), Id("node.a"), Id("node.isolated") };
        var edge = Edge("node.b", "node.a", GalaxyEdgeKind.Wormhole);
        var edges = new List<GalaxyEdgeDto> { edge };
        var graph = new GalaxyGraph(nodes, edges);
        nodes.Clear(); edges.Clear(); edge.EndpointA = "node.other"; edge.Kind = GalaxyEdgeKind.Normal;
        Assert.Equal(new[] { "node.a", "node.b", "node.isolated" }, graph.Nodes.Select(n => n.Value));
        Assert.Equal("node.a", graph.Edges[0].EndpointA.Value);
        Assert.Equal("node.b", graph.Edges[0].EndpointB.Value);
        Assert.Equal(GalaxyEdgeKind.Wormhole, graph.Edges[0].Kind);
        Assert.Equal(new[] { Id("node.b") }, graph.FindRoute(Id("node.a"), Id("node.b")).Steps);
        Assert.Empty(graph.GetNeighbors(Id("node.isolated")));
        Assert.Empty(graph.GetNeighbors(Id("node.unknown")));
        var empty = new GalaxyGraph(Array.Empty<EntityId>(), Array.Empty<GalaxyEdgeDto>());
        Assert.Empty(empty.Nodes); Assert.Empty(empty.Edges);
        Assert.Equal(RouteErrorCode.UnknownSystem, empty.FindRoute(Id("node.a"), Id("node.a")).ErrorCode);
    }

    [Fact]
    public void MissingNewListsAndControllerCannotBeLostThroughJsonDefaults()
    {
        var state = JsonSerializer.Deserialize<GameStateDto>("{}")!;
        Assert.Null(state.Civilizations); Assert.Null(state.StarSystems);
        var catalog = JsonSerializer.Deserialize<DefinitionCatalogDto>("{}")!;
        Assert.Null(catalog.Civilizations); Assert.Null(catalog.StarSystems); Assert.Null(catalog.GalaxyEdges);
        Assert.Equal(0, catalog.ContentFormatVersion);
        Assert.Null(JsonSerializer.Deserialize<CivilizationStateDto>("{}")!.ControllerKind);
        Assert.Null(JsonSerializer.Deserialize<GalaxyEdgeDto>("{}")!.Kind);
    }

    [Fact]
    public void WormholeTagIsIndependentOfLabelsAndIdentifierNames()
    {
        var graph = new GalaxyGraph(new[] { Id("node.x"), Id("node.y"), Id("node.z") },
            new[] { Edge("node.x", "node.y", GalaxyEdgeKind.Wormhole), Edge("node.y", "node.z") });
        Assert.Equal(new[] { "node.y", "node.z" }, graph.FindRoute(Id("node.x"), Id("node.z")).Steps.Select(n => n.Value));
        Assert.Equal(GalaxyEdgeKind.Wormhole, graph.Edges[0].Kind);
        Assert.Equal(1, graph.Edges[0].TraversalCost);
    }
}
