using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Application;

// Only the host handles JSON/files/package hashing and diagnostics.
var path = Path.Combine(AppContext.BaseDirectory, "scenarios", "ds9-slice", "scenario.json");
var turns = 0;
var combatOrder = "canonical";
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--turns")
    {
        if (++i >= args.Length || !int.TryParse(args[i], NumberStyles.None, CultureInfo.InvariantCulture, out turns))
            throw new ArgumentException("--turns requires a nonnegative integer.");
    }
    else if (args[i] == "--combat-order")
    {
        if (++i >= args.Length || (args[i] != "canonical" && args[i] != "reverse"))
            throw new ArgumentException("--combat-order requires canonical or reverse.");
        combatOrder = args[i];
    }
    else if (!args[i].StartsWith("--", StringComparison.Ordinal)) path = args[i];
    else throw new ArgumentException("Usage: HobbyDS9.Headless [scenario.json] [--turns nonnegative-count] [--combat-order canonical|reverse]");
}
var bytes = File.ReadAllBytes(path);
var jsonResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();
jsonResolver.Modifiers.Add(type =>
{
    if (type.Type.Assembly != typeof(GameStateDto).Assembly) return;
    foreach (var property in type.Properties)
        if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
            property.IsRequired = true;
});
var scenario = JsonSerializer.Deserialize<ScenarioDto>(bytes, new JsonSerializerOptions { TypeInfoResolver = jsonResolver, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow }) ?? throw new InvalidDataException("Missing scenario.");
scenario.ContentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
var session = CampaignApplication.StartScenario(scenario);
var commands = 0;
var generated = 0; var resolved = 0; var checkpoints = 0;
long phases = 0, shots = 0, hits = 0, destroyedShips = 0, destroyedFleets = 0, retreatedFleets = 0;
BigInteger shieldDamage = 0, hullDamage = 0;
for (var i = 0; i < turns; i++)
{
    var current = session.CaptureSnapshot().Campaign; var result = session.AdvanceTurn(current.TurnNumber, current.StateRevision);
    commands += result.CommandResults.Count;
    if (result.CommandResults.Any(c => !c.Accepted)) throw new InvalidOperationException("Autonomous command failed.");
    if (result.Status == TurnStatus.AwaitingCombat)
    {
        checkpoints++;
        var requests = session.GetPendingCombatRequests(); generated += requests.Count;
        var ordered = combatOrder == "reverse" ? requests.Reverse() : requests;
        foreach (var request in ordered)
        {
            var p = session.CaptureSnapshot().Campaign;
            var combat = session.ResolveCombat(request);
            phases += combat.Events.Count(e => e.Kind == CombatEventKind.PhaseStarted);
            shots += combat.Events.Count(e => e.Kind == CombatEventKind.AttackHit || e.Kind == CombatEventKind.AttackMissed);
            hits += combat.Events.Count(e => e.Kind == CombatEventKind.AttackHit);
            shieldDamage += combat.Events.Where(e => e.Kind == CombatEventKind.ShieldDamaged).Aggregate(BigInteger.Zero, (sum, e) => sum + e.Amount!.Value);
            hullDamage += combat.Events.Where(e => e.Kind == CombatEventKind.ShipDamaged).Aggregate(BigInteger.Zero, (sum, e) => sum + e.Amount!.Value);
            destroyedShips += combat.Events.Count(e => e.Kind == CombatEventKind.ShipDestroyed);
            destroyedFleets += combat.FleetChanges.Count(f => f.Disposition == FleetCombatDisposition.Destroyed);
            retreatedFleets += combat.FleetChanges.Count(f => f.Disposition == FleetCombatDisposition.Retreated);
            if (session.SubmitCombatResult(combat, p.TurnNumber, p.StateRevision).Status != CombatSubmissionStatus.Buffered) throw new InvalidOperationException("Combat buffering failed.");
            resolved++;
        }
        var finalStamp = session.CaptureSnapshot().Campaign; result = session.FinalizeCombat(finalStamp.TurnNumber, finalStamp.StateRevision);
    }
    if (result.Status != TurnStatus.Completed) throw new InvalidOperationException("Autonomous turn failed.");

}
var snapshot = session.CaptureSnapshot().Campaign;
Console.WriteLine(FormattableString.Invariant($"{scenario.ScenarioId}: completed={turns}, turn={snapshot.TurnNumber}, revision={snapshot.StateRevision}, AI commands={commands}, phase={snapshot.Phase}"));
Console.WriteLine(FormattableString.Invariant($"combats generated={generated}, resolved={resolved}, awaiting checkpoints={checkpoints}, host order={combatOrder}"));
Console.WriteLine(FormattableString.Invariant($"phases={phases}, shots={shots}, hits={hits}, shield damage={shieldDamage}, hull damage={hullDamage}, ships destroyed={destroyedShips}, fleets destroyed={destroyedFleets}, fleets retreated={retreatedFleets}"));
Console.WriteLine(FormattableString.Invariant($"ships={snapshot.Ships.Count}, fleets={snapshot.Fleets.Count}, queued={snapshot.Colonies.Sum(c => c.ProductionQueue.Count)}, unlocks={snapshot.Civilizations.Sum(c => c.UnlockedResearchDefinitionIds.Count)}, nextEntity={snapshot.NextEntitySequence}, rng={snapshot.RngState.State:x16}"));
var stateBytes = JsonSerializer.SerializeToUtf8Bytes(session.ExportSave());
Console.WriteLine($"save-sha256={Convert.ToHexString(SHA256.HashData(stateBytes)).ToLowerInvariant()}");

var stats = snapshot.PopulationStatistics;
Console.WriteLine($"births={stats.Births}, deaths={stats.Deaths}, migrated={stats.Migrated}, refugees={stats.RefugeesMoved}, food-delivered={stats.FoodDelivered}, resource-extracted={stats.ResourceExtracted}, resource-delivered={stats.ResourceDelivered}, resource-consumed={stats.ResourceConsumed}, production-completed={stats.ProductionCompleted}");
foreach (var civilization in snapshot.Civilizations)
{
    var worlds = snapshot.Colonies.Where(c => c.OwnerCivilizationId == civilization.CivilizationId).ToList();
    Console.WriteLine($"{civilization.CivilizationId}: credits={civilization.Credits}, population={worlds.Sum(c => c.Population)}");
    foreach (var species in worlds.SelectMany(c => c.State.Cohorts).GroupBy(c => c.SpeciesDefinitionId).OrderBy(g => g.Key, StringComparer.Ordinal))
        Console.WriteLine($"  {species.Key}: {species.Sum(c => c.Population)}");
}
foreach (var world in snapshot.Colonies) Console.WriteLine($"{world.Environment.DisplayName}: population={world.Population}, stability={world.State.Stability}, target={world.Economy.TargetStability}, food={world.Food}");
