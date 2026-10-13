using System.Globalization;
using System.Text.Json;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalPlayground;

public sealed class PlaygroundOptions
{
    public string? Scenario { get; private set; }
    public ulong? Seed { get; private set; }
    public string? Script { get; private set; }
    public string? Resume { get; private set; }
    public string? Save { get; private set; }
    public int? StopAfter { get; private set; }
    public bool Detailed { get; private set; }
    public bool ReverseFixture { get; private set; }
    public bool Help { get; private set; }
    public static PlaygroundOptions Parse(string[] args)
    {
        var result = new PlaygroundOptions(); var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < args.Length; i++)
        {
            var flag = args[i]; if (!seen.Add(flag)) throw new ArgumentException("Duplicate option: " + flag);
            string Value() { if (++i >= args.Length || string.IsNullOrWhiteSpace(args[i])) throw new ArgumentException("Missing value for " + flag); return args[i]; }
            switch (flag)
            {
                case "--scenario": var id = Value(); result.Scenario = int.TryParse(id, out var index) && index >= 1 && index <= 3 ? PlaygroundScenarios.All[index - 1].Id : PlaygroundScenarios.Find(id).Id; break;
                case "--seed": var seed = Value(); if (!ulong.TryParse(seed, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)) throw new ArgumentException("Seed must be an unsigned decimal integer."); result.Seed = parsed; break;
                case "--script": result.Script = Value(); break;
                case "--resume": result.Resume = Value(); break;
                case "--save": result.Save = Value(); break;
                case "--stop-after": if (!int.TryParse(Value(), out var stop) || stop < 1 || stop > 2) throw new ArgumentException("Stop boundary must be round 1 or 2."); result.StopAfter = stop; break;
                case "--details": result.Detailed = true; break;
                case "--reverse-fixture": result.ReverseFixture = true; break;
                case "--help": case "-h": result.Help = true; break;
                default: throw new ArgumentException("Unknown option: " + flag);
            }
        }
        if (result.Resume != null && (result.Scenario != null || result.Seed != null || result.ReverseFixture)) throw new ArgumentException("Resume retains its saved scenario/seed; do not also specify a new fixture.");
        if (result.StopAfter != null && (result.Save == null || result.Script == null)) throw new ArgumentException("--stop-after requires --save and --script.");
        return result;
    }
}
public sealed class PlaygroundScriptRound
{
    public List<TacticalOrderV2Dto> Orders { get; set; } = new();
}
public sealed class PlaygroundScript
{
    public List<PlaygroundScriptRound> Rounds { get; set; } = new();
    public static PlaygroundScript Read(string source, TacticalEncounterV2Dto state)
    {
        PlaygroundScript script;
        if (File.Exists(source)) script = JsonSerializer.Deserialize<PlaygroundScript>(PlaygroundFiles.Read(source), PlaygroundFiles.Options)
            ?? throw new ArgumentException("Empty script.");
        else
        {
            if (source.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Script file not found: " + source);
            var lead = state.Formations.Where(f => f.SideId == PlaygroundScenarios.PlayerSide && f.Status == TacticalFormationStatus.Active)
                .OrderBy(f => f.FormationId, StringComparer.Ordinal).FirstOrDefault() ?? throw new ArgumentException("No active human formation for inline orders.");
            script = new PlaygroundScript();
            foreach (var token in source.Split(';'))
            {
                var round = new PlaygroundScriptRound();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    if (!TryEnum(token, out TacticalManeuver maneuver)) throw new ArgumentException("Unknown scripted maneuver: " + token);
                    var order = PlaygroundScenarios.CloneOrder(lead.StandingOrder); order.Maneuver = maneuver;
                    if (maneuver != TacticalManeuver.Screen) order.ProtectedFormationId = null;
                    round.Orders.Add(order);
                }
                script.Rounds.Add(round);
            }
        }
        if (script.Rounds == null || script.Rounds.Count > 3 || script.Rounds.Any(r => r == null || r.Orders == null)) throw new ArgumentException("Script must contain at most three non-null round records.");
        return script;
    }
    public static bool TryEnum<T>(string text, out T value) where T : struct, Enum
    {
        text = text.Trim().Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        if (typeof(T) == typeof(TacticalManeuver) && text.Equals("hold", StringComparison.OrdinalIgnoreCase)) text = "MaintainDistance";
        return Enum.TryParse(text, ignoreCase: true, out value) && Enum.IsDefined(value);
    }
}
