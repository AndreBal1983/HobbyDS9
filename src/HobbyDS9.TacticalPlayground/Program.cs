using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalPlayground;

public static class Program
{
    public static int Main(string[] args) => PlaygroundConsole.Run(args, Console.In, Console.Out, Console.Error);
}

public static class PlaygroundConsole
{
    public const string Help = "" +
        "HobbyDS9 tactical playground - standalone console sandbox, not campaign or Unity.\n" +
        "Usage: HobbyDS9.TacticalPlayground [--scenario cruiser|screen|breakthrough|1..3] [--seed 47]\n" +
        "  --script 'Flank;Charge;FightingWithdrawal' OR path/to/script.json (no prompts)\n" +
        "  --save path.json        Atomic standalone checkpoint after every resolved round\n" +
        "  --resume path.json      Verify saved history and resume the exact boundary/RNG\n" +
        "  --stop-after 1|2        Save and stop at this absolute round boundary; needs --script/--save\n" +
        "  --details               Include the complete authoritative event log\n" +
        "  --reverse-fixture       Debug ordering check for a new fixture\n" +
        "  --help                  Print this help\n" +
        "Scripts supply up to three remaining rounds. Short/blank rounds retain orders.\n" +
        "Interactive controls: Enter choose/retain orders; S save; L load; D detail toggle; Q quit.\n" +
        "At completion R replays the same seed for comparison; N returns to scenario menu.\n";

    public static int Run(string[] args, TextReader input, TextWriter output, TextWriter errors)
    {
        try
        {
            var options = PlaygroundOptions.Parse(args);
            if (options.Help) { output.Write(Help); return 0; }
            output.Write("WELCOME TO HOBBYDS9 TACTICAL COMBAT v2\nStandalone tactical sandbox. Sample authored fixtures; no racial bonuses or canon claims.\nEnemy orders follow a fixed script, NOT a tactical AI. At most three rounds.\n");
            var session = options.Resume != null ? PlaygroundFiles.Load(options.Resume)
                : new PlaygroundSession(options.Scenario ?? (options.Script != null ? "cruiser" : ChooseScenario(input, output)), options.Seed ?? 47, options.ReverseFixture);
            if (options.Script != null) return Scripted(session, options, output);
            var details = options.Detailed; PlaygroundSave? previous = null;
            while (true)
            {
                output.Write(PlaygroundScenarios.Find(session.Scenario).Briefing + "\n");
                output.Write("Opponent script (debug briefing): " + PlaygroundScenarios.Find(session.Scenario).OpponentScript + "\n");
                while (session.State.Completion == TacticalCompletion.None)
                {
                    output.Write(PlaygroundReports.Snapshot(session));
                    output.Write("\nEnter choose orders | S save | L load | D details | Q quit: ");
                    var command = Read(input).Trim().ToLowerInvariant();
                    if (command == "q") return 0;
                    if (command == "d") { details = !details; output.Write("Detailed event log " + (details ? "ON" : "OFF") + "\n"); continue; }
                    if (command == "s" || command == "l")
                    {
                        output.Write("Standalone JSON path (blank cancels): "); var path = Read(input).Trim(); if (path.Length == 0) continue;
                        try
                        {
                            if (command == "s") { PlaygroundFiles.Save(path, session); output.Write("Saved exact boundary: " + path + "\n"); }
                            else { var loaded = PlaygroundFiles.Load(path); session = loaded; previous = null; output.Write("Loaded verified boundary.\n"); }
                        }
                        catch (Exception ex) when (Expected(ex)) { output.Write("Save/load rejected; current session preserved: " + ex.Message + "\n"); }
                        continue;
                    }
                    if (command.Length != 0) { output.Write("Choose Enter, S, L, D or Q.\n"); continue; }
                    var before = session.State;
                    var orders = ChooseOrders(before, input, output);
                    output.Write("\nProposed joint human commitment (opponent does not counter-pick):\n");
                    foreach (var order in orders) output.Write($"  {order.FormationId}: {order.Maneuver}, {order.TargetPriority}, preferred {order.PreferredRange}, target {order.TargetFormationId ?? "any"}, protect {order.ProtectedFormationId ?? "self"}\n");
                    output.Write("Enter/Y resolves EXACTLY ONE round | N revises | Q quits: "); var confirmation = Read(input).Trim().ToLowerInvariant();
                    if (confirmation == "q") return 0;
                    if (confirmation != "" && confirmation != "y") { output.Write("No round resolved; orders unchanged.\n"); continue; }
                    var result = session.Advance(orders, before.NextRound);
                    output.Write(PlaygroundReports.Round(before, result, details));
                    AutoSave(options.Save, session, output);
                }
                output.Write(PlaygroundReports.Snapshot(session)); output.Write(PlaygroundReports.Outcome(session));
                if (previous != null) output.Write(PlaygroundReports.Compare(previous, session.Export()));
                output.Write("R replay SAME seed with new choices | N scenario menu | Q quit: ");
                var next = Read(input).Trim().ToLowerInvariant();
                if (next == "r") { previous = session.Export(); session = new PlaygroundSession(session.Scenario, session.Seed); }
                else if (next == "n") { previous = null; session = new PlaygroundSession(ChooseScenario(input, output), options.Seed ?? 47); }
                else if (next == "q" || next == "") return 0;
                else output.Write("Choose R, N or Q.\n");
            }
        }
        catch (EndOfInput)
        { output.Write("\nEnd of input. No unconfirmed round was resolved. Use --script for unattended play.\n"); return 0; }
        catch (Exception ex) when (Expected(ex))
        { errors.Write("Playground error: " + ex.Message + "\nUse --help for controls.\n"); return 2; }
    }
    private static bool Expected(Exception ex) => ex is ArgumentException || ex is System.Text.Json.JsonException || ex is IOException || ex is UnauthorizedAccessException;
    private static int Scripted(PlaygroundSession session, PlaygroundOptions options, TextWriter output)
    {
        if (options.StopAfter != null && (session.State.NextRound > options.StopAfter || session.State.Completion != TacticalCompletion.None))
            throw new ArgumentException("Requested stop boundary is already passed or encounter complete.");
        if (session.State.Completion != TacticalCompletion.None)
        { output.Write(PlaygroundReports.Snapshot(session)); output.Write(PlaygroundReports.Outcome(session)); return 0; }
        var script = PlaygroundScript.Read(options.Script!, session.State); int step = 0;
        output.Write("SCRIPTED RUN: no interactive input; fixed opponent sequence.\n");
        output.Write(PlaygroundScenarios.Find(session.Scenario).Briefing + "\n");
        while (session.State.Completion == TacticalCompletion.None)
        {
            output.Write(PlaygroundReports.Snapshot(session)); var before = session.State;
            var orders = step < script.Rounds.Count ? script.Rounds[step].Orders : new List<TacticalOrderV2Dto>(); step++;
            var result = session.Advance(orders, before.NextRound);
            output.Write(PlaygroundReports.Round(before, result, options.Detailed)); AutoSave(options.Save, session, output);
            if (options.StopAfter == before.NextRound)
            { output.Write("Stopped at saved round " + before.NextRound + "; resume with --resume.\nplayground-sha256=" + session.Checksum() + "\n"); return 0; }
        }
        output.Write(PlaygroundReports.Snapshot(session)); output.Write(PlaygroundReports.Outcome(session)); return 0;
    }
    private static void AutoSave(string? path, PlaygroundSession session, TextWriter output)
    {
        if (path == null) return;
        PlaygroundFiles.Save(path, session); output.Write("Saved exact boundary: " + path + "\n");
    }
    private static string ChooseScenario(TextReader input, TextWriter output)
    {
        output.Write("\nREADY-TO-PLAY ENGAGEMENTS\n");
        for (int i = 0; i < PlaygroundScenarios.All.Count; i++) output.Write($"  {i + 1}. {PlaygroundScenarios.All[i].Title}\n");
        return PlaygroundScenarios.All[Number(input, output, "Scenario [1]: ", 1, 3, 1) - 1].Id;
    }
    private static List<TacticalOrderV2Dto> ChooseOrders(TacticalEncounterV2Dto state, TextReader input, TextWriter output)
    {
        output.Write("\nMANEUVERS: 1 Maintain Distance / Hold & Fire | 2 Charge | 3 Flank | 4 Screen\n           5 Breakthrough | 6 Pursuit | 7 Fighting Withdrawal | 8 Disengage\nBlank retains the previous choice. Names or numbers work. Shields are never redistributed.\n");
        var result = new List<TacticalOrderV2Dto>();
        foreach (var f in state.Formations.Where(f => f.SideId == PlaygroundScenarios.PlayerSide && f.Status == TacticalFormationStatus.Active))
        {
            var order = PlaygroundScenarios.CloneOrder(f.StandingOrder);
            order.Maneuver = EnumChoice(input, output, f.FormationId + $" maneuver [{order.Maneuver}]: ", order.Maneuver);
            if (order.Maneuver != TacticalManeuver.Screen) order.ProtectedFormationId = null;
            if (order.Maneuver == TacticalManeuver.MaintainDistance || order.Maneuver == TacticalManeuver.Flank || order.Maneuver == TacticalManeuver.Screen)
            {
                output.Write("Preferred range: 1 Distant | 2 Engagement | 3 Close | 4 Intermixed\n");
                order.PreferredRange = (TacticalRange)(Number(input, output, $"Preferred [{order.PreferredRange}]: ", 1, 4, (int)order.PreferredRange + 1) - 1);
            }
            output.Write("Target priority: 1 LowestId | 2 LowestHullRatio | 3 CommandSupport | 4 Screen\n");
            order.TargetPriority = EnumChoice(input, output, $"Priority [{order.TargetPriority}]: ", order.TargetPriority);
            var targets = state.Formations.Where(t => t.SideId != f.SideId && t.Status == TacticalFormationStatus.Active).ToList();
            order.TargetFormationId = FormationChoice(input, output, "Target", targets, order.TargetFormationId);
            if (order.Maneuver == TacticalManeuver.Screen)
            {
                var allies = state.Formations.Where(a => a.SideId == f.SideId && a.FormationId != f.FormationId && a.Status == TacticalFormationStatus.Active).ToList();
                order.ProtectedFormationId = FormationChoice(input, output, "Protect (0 self only)", allies,
                    f.StandingOrder.Maneuver == TacticalManeuver.Screen ? order.ProtectedFormationId : allies.FirstOrDefault()?.FormationId);
            }
            result.Add(order);
        }
        return result;
    }
    private static T EnumChoice<T>(TextReader input, TextWriter output, string prompt, T previous) where T : struct, Enum
    {
        while (true)
        {
            output.Write(prompt); var text = Read(input); if (string.IsNullOrWhiteSpace(text)) return previous;
            if (PlaygroundScript.TryEnum(text, out T parsed)) return parsed;
            output.Write("Unknown choice; use a listed number/name or blank to retain.\n");
        }
    }
    private static string? FormationChoice(TextReader input, TextWriter output, string label, List<TacticalFormationV2Dto> formations, string? previous)
    {
        output.Write(label + ": 0 no designated formation" + string.Concat(formations.Select((f, i) => $" | {i + 1} {f.FormationId}")) + "\n");
        while (true)
        {
            output.Write($"{label} [{previous ?? "none"}]: "); var text = Read(input).Trim(); if (text.Length == 0) return previous;
            if (int.TryParse(text, out var number) && number >= 0 && number <= formations.Count) return number == 0 ? null : formations[number - 1].FormationId;
            var named = formations.FirstOrDefault(f => f.FormationId.Equals(text, StringComparison.OrdinalIgnoreCase)); if (named != null) return named.FormationId;
            output.Write("Choose an available formation number/ID, 0, or blank.\n");
        }
    }
    private static int Number(TextReader input, TextWriter output, string prompt, int minimum, int maximum, int previous)
    {
        while (true)
        {
            output.Write(prompt); var text = Read(input); if (string.IsNullOrWhiteSpace(text)) return previous;
            if (int.TryParse(text, out var value) && value >= minimum && value <= maximum) return value;
            output.Write($"Choose {minimum}..{maximum}, or blank.\n");
        }
    }
    private static string Read(TextReader input) => input.ReadLine() ?? throw new EndOfInput();
    private sealed class EndOfInput : Exception { }
}
