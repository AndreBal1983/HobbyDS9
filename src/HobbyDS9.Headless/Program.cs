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
string? inspectTacticalActor = null;
var resistanceTurns = Enum.GetValues<ResistanceState>().ToDictionary(r=>r,r=>0L);
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
    else if (args[i] == "--inspect-tactical-v2")
    {
        if (++i >= args.Length || !EntityId.TryParse(args[i], out _))
            throw new ArgumentException("--inspect-tactical-v2 requires a canonical actor ID.");
        inspectTacticalActor = args[i];
    }
    else if (!args[i].StartsWith("--", StringComparison.Ordinal)) path = args[i];
    else throw new ArgumentException("Usage: HobbyDS9.Headless [scenario.json] [--turns nonnegative-count] [--combat-order canonical|reverse] [--inspect-tactical-v2 actor-id]");
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
    foreach(var world in session.CaptureSnapshot().Campaign.Colonies) resistanceTurns[world.State.Politics.Resistance]++;


}
var snapshot = session.CaptureSnapshot().Campaign;
Console.WriteLine(FormattableString.Invariant($"{scenario.ScenarioId}: completed={turns}, turn={snapshot.TurnNumber}, revision={snapshot.StateRevision}, AI commands={commands}, phase={snapshot.Phase}"));
Console.WriteLine(FormattableString.Invariant($"combats generated={generated}, resolved={resolved}, awaiting checkpoints={checkpoints}, host order={combatOrder}"));
Console.WriteLine(FormattableString.Invariant($"phases={phases}, shots={shots}, hits={hits}, shield damage={shieldDamage}, hull damage={hullDamage}, ships destroyed={destroyedShips}, fleets destroyed={destroyedFleets}, fleets retreated={retreatedFleets}"));
Console.WriteLine(FormattableString.Invariant($"ships={snapshot.Ships.Count}, fleets={snapshot.Fleets.Count}, queued={snapshot.Colonies.Sum(c => c.ProductionQueue.Count)}, unlocks={snapshot.Civilizations.Sum(c => c.MatureEngineeringApplicationDefinitionIds.Count)}, nextEntity={snapshot.NextEntitySequence}, rng={snapshot.RngState.State:x16}"));
var stateBytes = JsonSerializer.SerializeToUtf8Bytes(session.ExportSave());
Console.WriteLine($"save-sha256={Convert.ToHexString(SHA256.HashData(stateBytes)).ToLowerInvariant()}");
if (inspectTacticalActor != null)
{
    var inspection = ((ICampaignTacticalV2Inspection)session).InspectCampaignTacticalV2(inspectTacticalActor);
    if (inspection.ErrorCode != CampaignTacticalInspectionError.None)
        throw new InvalidOperationException("Tactical v2 inspection rejected: " + inspection.ErrorCode);
    Console.WriteLine(FormattableString.Invariant($"tactical-v2-inspection-format={inspection.FormatVersion}, can-activate={inspection.CanActivate}, campaign-resolver={inspection.CampaignResolverId}/{inspection.CampaignResolverVersion}, owned-ships={inspection.OwnedShips.Count}, owned-revisions={inspection.OwnedBlueprintRevisions.Count}"));
    Console.WriteLine("tactical-v2-blockers=" + string.Join(",", inspection.Blockers));
    foreach (var source in inspection.OwnedShips)
    {
        Console.WriteLine(FormattableString.Invariant($"tactical-source={source.Ship.ShipId}, design={source.Ship.ShipDesignDefinitionId}, revision={source.Ship.Blueprint?.Revision.ToString(CultureInfo.InvariantCulture) ?? "static"}, can-project-v2={source.CanProjectTacticalV2}"));
        if (source.AuthoredEquipment != null) AuthoredEquipmentDiagnostics.Write(source.AuthoredEquipment);
    }
}

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

var nav = snapshot.NavigationStatistics;
Console.WriteLine($"movement-cost-paid={nav.TraversalCostPaid}, edges-completed={nav.EdgesCompleted}, partial-traversal-turns={nav.PartialTraversalTurns}");
foreach(var resistance in Enum.GetValues<ResistanceState>()) Console.WriteLine($"resistance {resistance}: worlds={snapshot.Colonies.Count(c=>c.State.Politics.Resistance==resistance)}, world-turns={resistanceTurns[resistance]}");
Console.WriteLine($"political-disruptions={snapshot.Colonies.Sum(c=>c.State.Politics.Disruptions)}");

foreach(var civilization in snapshot.Civilizations)
{
    var research=civilization.Research!;var counts=research.Statistics;
    var initial=scenario.InitialState.Civilizations!.Single(c=>c.CivilizationId==civilization.CivilizationId.Value);
    if((BigInteger)counts.TotalScienceAllocated+counts.TotalEngineeringAllocated!=counts.TotalResearchGenerated
        ||(BigInteger)counts.TotalScienceSpent+counts.TotalEngineeringSpent+research.ScienceReserve-initial.ScienceReserve+research.EngineeringReserve-initial.EngineeringReserve!=counts.TotalResearchGenerated)
        throw new InvalidOperationException("Research conservation failed.");
    Console.WriteLine($"{civilization.CivilizationId} research: conceptual-generated={counts.TotalResearchGenerated/ResearchUnits.Scale}, internal-generated={counts.TotalResearchGenerated}, science-allocated={counts.TotalScienceAllocated}, engineering-allocated={counts.TotalEngineeringAllocated}, science-spent={counts.TotalScienceSpent}, engineering-spent={counts.TotalEngineeringSpent}, science-reserve={research.ScienceReserve}, engineering-reserve={research.EngineeringReserve}, starting-science-reserve={initial.ScienceReserve}, starting-engineering-reserve={initial.EngineeringReserve}, breakthroughs={counts.TotalBreakthroughs}, levels-completed={counts.TotalScienceLevelsCompleted}, matured={counts.TotalEngineeringApplicationsMatured}");
    Console.WriteLine("  levels: "+string.Join(", ",research.Disciplines.Select(d=>$"{d.Discipline}={d.Level}:{d.Progress} spent={d.TotalSpent}")));
}
