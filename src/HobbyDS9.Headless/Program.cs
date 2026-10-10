using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

// Only the host handles JSON/files/package hashing and diagnostics.
var path = Path.Combine(AppContext.BaseDirectory, "scenarios", "ds9-slice", "scenario.json");
var turns = 0;
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--turns")
    {
        if (++i >= args.Length || !int.TryParse(args[i], NumberStyles.None, CultureInfo.InvariantCulture, out turns))
            throw new ArgumentException("--turns requires a nonnegative integer.");
    }
    else if (!args[i].StartsWith("--", StringComparison.Ordinal)) path = args[i];
    else throw new ArgumentException("Usage: HobbyDS9.Headless [scenario.json] [--turns nonnegative-count]");
}
var bytes = File.ReadAllBytes(path);
var scenario = JsonSerializer.Deserialize<ScenarioDto>(bytes) ?? throw new InvalidDataException("Missing scenario.");
scenario.ContentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
FoundationValidator.ValidateScenario(scenario).ThrowIfInvalid(nameof(scenario));
var session = new SimulationSession(new SaveEnvelopeDto
{
    SaveFormatVersion = FoundationVersions.SaveFormatVersion, ScenarioId = scenario.ScenarioId,
    RulesVersion = scenario.RulesVersion, ContentHash = scenario.ContentHash, GameState = scenario.InitialState
}, scenario.Definitions, new SaveCompatibility(scenario.ScenarioId, scenario.RulesVersion, scenario.ContentHash));
var commands = 0;
for (var i = 0; i < turns; i++)
{
    var current = session.CaptureSnapshot(); var result = session.AdvanceTurn(current.TurnNumber, current.StateRevision);
    commands += result.CommandResults.Count;
    if (result.Status == TurnStatus.AwaitingCombat)
    {
        foreach (var request in session.GetPendingCombatRequests())
        {
            var p = session.CaptureSnapshot();
            var noChanges = new NoLossCombatResolver().Resolve(request);
            if (session.SubmitCombatResult(noChanges, p.TurnNumber, p.StateRevision).Status != CombatSubmissionStatus.Buffered) throw new InvalidOperationException("Combat buffering failed.");
        }
        var finalStamp = session.CaptureSnapshot(); result = session.FinalizeCombat(finalStamp.TurnNumber, finalStamp.StateRevision);
    }
    if (result.Status != TurnStatus.Completed || result.CommandResults.Any(c => !c.Accepted)) throw new InvalidOperationException("Autonomous turn failed.");

}
var snapshot = session.CaptureSnapshot();
Console.WriteLine(FormattableString.Invariant($"{scenario.ScenarioId}: completed={turns}, turn={snapshot.TurnNumber}, revision={snapshot.StateRevision}, AI commands={commands}, phase={snapshot.Phase}"));
Console.WriteLine(FormattableString.Invariant($"ships={snapshot.Ships.Count}, fleets={snapshot.Fleets.Count}, queued={snapshot.Colonies.Sum(c => c.ProductionQueue.Count)}, unlocks={snapshot.Civilizations.Sum(c => c.UnlockedResearchDefinitionIds.Count)}, nextEntity={snapshot.NextEntitySequence}, rng={snapshot.RngState.State:x16}"));
var stateBytes = JsonSerializer.SerializeToUtf8Bytes(session.ExportSave());
Console.WriteLine($"save-sha256={Convert.ToHexString(SHA256.HashData(stateBytes)).ToLowerInvariant()}");
