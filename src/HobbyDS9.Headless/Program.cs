using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

// Host owns file/JSON/hash adapters. The graph skeleton does not execute turns.
var path = args.Length == 0 ? Path.Combine(AppContext.BaseDirectory, "scenarios", "ds9-slice", "scenario.json") : args[0];
var bytes = File.ReadAllBytes(path);
var scenario = JsonSerializer.Deserialize<ScenarioDto>(bytes) ?? throw new InvalidDataException("Missing scenario.");
// ContentHash pins the exact UTF-8 package bytes; it is assigned by the host,
// not included in its own hash preimage.
scenario.ContentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
FoundationValidator.ValidateScenario(scenario).ThrowIfInvalid(nameof(scenario));
var session = new SimulationSession(new SaveEnvelopeDto
{
    SaveFormatVersion = FoundationVersions.SaveFormatVersion, ScenarioId = scenario.ScenarioId,
    RulesVersion = scenario.RulesVersion, ContentHash = scenario.ContentHash, GameState = scenario.InitialState
}, scenario.Definitions, new SaveCompatibility(scenario.ScenarioId, scenario.RulesVersion, scenario.ContentHash));
var snapshot = session.CaptureSnapshot();
Console.WriteLine($"{scenario.ScenarioId}: {snapshot.Civilizations.Count} civilizations, {session.Galaxy.Nodes.Count} nodes, {session.Galaxy.Edges.Count} edges, {snapshot.Colonies.Count} colonies.");
Console.WriteLine("Scenario identity/graph skeleton validated. Colony economy is implemented; this host does not advance turns.");
