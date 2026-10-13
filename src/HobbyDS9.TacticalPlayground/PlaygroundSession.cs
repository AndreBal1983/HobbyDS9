using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.TacticalPlayground;

public sealed class PlaygroundRoundRecord
{
    public List<TacticalOrderV2Dto> PlayerOrders { get; set; } = new();
    public List<TacticalEventV2> Events { get; set; } = new();
}
public sealed class PlaygroundSave
{
    public int FormatVersion { get; set; } = 1;
    public int FixtureVersion { get; set; } = PlaygroundScenarios.FixtureVersion;
    public string Scenario { get; set; } = string.Empty;
    public ulong Seed { get; set; }
    public TacticalEncounterV2Dto State { get; set; } = new();
    public List<PlaygroundRoundRecord> Rounds { get; set; } = new();
}

public sealed class PlaygroundSession
{
    private TacticalEncounterV2Dto state;
    private readonly List<PlaygroundRoundRecord> rounds = new();
    public string Scenario { get; }
    public ulong Seed { get; }
    public TacticalEncounterV2Dto State => TacticalCombatV2State.Copy(state);
    public PlaygroundSession(string scenario, ulong seed, bool reverseFixture = false)
    {
        Scenario = PlaygroundScenarios.Find(scenario).Id; Seed = seed;
        state = PlaygroundScenarios.Create(Scenario, seed);
        if (reverseFixture)
        {
            state.Formations.Reverse(); state.Geometry.Reverse();
            foreach (var f in state.Formations) { f.Ships.Reverse(); foreach (var s in f.Ships) { s.Weapons.Reverse(); s.Mounts.Reverse(); s.Shields.Reverse(); } }
        }
        state = TacticalCombatV2State.Copy(state);
    }
    public TacticalRoundV2Result Advance(IReadOnlyList<TacticalOrderV2Dto> playerOrders, int expectedRound)
    {
        if (playerOrders == null || expectedRound != state.NextRound) throw new ArgumentException("Stale or missing round orders.");
        foreach (var order in playerOrders)
            if (order == null || !state.Formations.Any(f => f.FormationId == order.FormationId && f.SideId == PlaygroundScenarios.PlayerSide && f.Status == TacticalFormationStatus.Active))
                throw new ArgumentException("Only active human formations can be ordered.");
        var copies = playerOrders.Select(PlaygroundScenarios.CloneOrder).OrderBy(o => o.FormationId, StringComparer.Ordinal).ToList();
        var joint = PlaygroundScenarios.OpposingOrders(Scenario, state);
        joint.AddRange(copies);
        var result = new DeterministicTacticalCombatV2Resolver().ResolveRound(state, new TacticalOrderBatchV2Dto
        { EncounterId = state.EncounterId, Round = expectedRound, Replacements = joint });
        state = result.NextState; // Publish only after complete resolver validation.
        rounds.Add(new PlaygroundRoundRecord { PlayerOrders = copies, Events = result.Events.ToList() });
        return result;
    }
    public PlaygroundSave Export() => new()
    {
        Scenario = Scenario, Seed = Seed, State = State,
        Rounds = rounds.Select(r => new PlaygroundRoundRecord { PlayerOrders = r.PlayerOrders.Select(PlaygroundScenarios.CloneOrder).ToList(), Events = r.Events.ToList() }).ToList()
    };
    public static PlaygroundSession Restore(PlaygroundSave save)
    {
        if (save == null || save.FormatVersion != 1 || save.FixtureVersion != PlaygroundScenarios.FixtureVersion || save.Rounds == null || save.Rounds.Count > 3)
            throw new ArgumentException("Unknown/malformed standalone playground save. No campaign migration is supported.");
        var session = new PlaygroundSession(save.Scenario, save.Seed);
        foreach (var round in save.Rounds)
        {
            if (round == null || round.PlayerOrders == null || round.Events == null) throw new ArgumentException("Missing round history.");
            var result = session.Advance(round.PlayerOrders, session.state.NextRound);
            if (PlaygroundFiles.Json(result.Events) != PlaygroundFiles.Json(round.Events)) throw new ArgumentException("Saved event history does not match replay.");
        }
        if (PlaygroundFiles.Json(session.State) != PlaygroundFiles.Json(TacticalCombatV2State.Copy(save.State)))
            throw new ArgumentException("Saved boundary does not match authoritative replay.");
        return session;
    }
    public string Checksum() => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(Export(), PlaygroundFiles.Options))).ToLowerInvariant();
}

// JSON/filesystem belong ONLY in this net10 console host, never portable Simulation.
public static class PlaygroundFiles
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions { WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)); options.MakeReadOnly(populateMissingResolver: true); return options;
    }
    public static string Json<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static PlaygroundSession Load(string path) => PlaygroundSession.Restore(JsonSerializer.Deserialize<PlaygroundSave>(Read(path), Options)
        ?? throw new ArgumentException("Empty playground save."));
    public static string Read(string path)
    {
        var file = new FileInfo(path); if (file.Length > 16 * 1024 * 1024) throw new ArgumentException("Sandbox file exceeds 16 MiB.");
        return File.ReadAllText(path);
    }
    public static void Save(string path, PlaygroundSession session)
    {
        var save = session.Export(); PlaygroundSession.Restore(save);
        var target = Path.GetFullPath(path); var directory = Path.GetDirectoryName(target)!; Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, ".playground-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { JsonSerializer.Serialize(stream, save, Options); stream.Flush(flushToDisk: true); }
            File.Move(temporary, target, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
