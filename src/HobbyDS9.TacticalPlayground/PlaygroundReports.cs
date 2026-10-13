using System.Globalization;
using System.Numerics;
using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.TacticalPlayground;

public static class PlaygroundReports
{
    private static string N(BigInteger n) => n.ToString(CultureInfo.InvariantCulture);
    public static string Snapshot(PlaygroundSession session)
    {
        var state = session.State; var b = new StringBuilder();
        void Line(string text) => b.Append(text).Append('\n');
        var cadence = state.Completion == TacticalCompletion.None ? $"next round {state.NextRound}/3" : $"resolved rounds {state.NextRound - 1}/3";
        Line($"\n{PlaygroundScenarios.Find(session.Scenario).Title} | seed {N(session.Seed)} | {cadence} | {state.Completion}");
        Line("OMNISCIENT SANDBOX VIEW: all fixture assets are visible; no campaign fog of war.");
        foreach (var f in state.Formations)
        {
            var o = f.StandingOrder;
            Line($"{f.FormationId} [{(f.SideId == PlaygroundScenarios.PlayerSide ? "YOU" : "SCRIPTED OPPONENT")}, {f.Status}] speed {f.Speed}, agility {f.Agility}");
            Line($"  Order: {o.Maneuver}; priority {o.TargetPriority}; preferred range {o.PreferredRange}; target {o.TargetFormationId ?? "any eligible"}; protect {o.ProtectedFormationId ?? "self only"}");
            Line($"  Local objective: {f.Objective} {f.ObjectiveProgress}/{f.ObjectiveRequiredRounds}{(f.ObjectiveCompleted ? " COMPLETE" : "")}");
            foreach (var s in f.Ships)
            {
                Line($"  {s.ShipId}: hull {N(s.Hull)}/{N(s.MaxHull)}, armor {N(s.Armor)}, shields " + string.Join(" / ", s.Shields.Select(q => $"{q.Sector} {N(q.Current)}/{N(q.Maximum)}")));
                Line($"    shield recharge {N(s.ShieldRechargePerRound)}/round; shield power {N(s.ShieldPowerAllocated)}/{N(s.ShieldPowerRequired)}");
                foreach (var w in s.Weapons)
                    Line($"    {w.WeaponId}: {w.Class}, arcs {w.Coverage}, {w.OpportunitiesPerRound} opportunities, yield {N(w.Damage)}, power {N(w.AllocatedPower)}/{N(w.RequiredPower)}, next ready round {w.NextFireRound}" +
                        (w.Class == TacticalWeaponClass.Torpedo ? $", ammo {N(w.Ammunition)}/{N(w.MagazineCapacity)}" : ""));
            }
        }
        foreach (var g in state.Geometry) Line($"  Geometry {g.FormationA} <-> {g.FormationB}: {g.Range}; exposed facings {g.FacingA} / {g.FacingB}");
        return b.ToString();
    }
    public static string Round(TacticalEncounterV2Dto before, TacticalRoundV2Result result, bool detailed)
    {
        var after = result.NextState; var events = result.Events; var b = new StringBuilder();
        void Line(string text) => b.Append(text).Append('\n');
        Line($"\nAFTER-ACTION REPORT - ROUND {before.NextRound}");
        foreach (var g in after.Geometry)
        {
            var old = before.Geometry.Single(p => p.FormationA == g.FormationA && p.FormationB == g.FormationB);
            Line($"  Maneuver {g.FormationA} / {g.FormationB}: {old.Range} -> {g.Range}; facings {old.FacingA}/{old.FacingB} -> {g.FacingA}/{g.FacingB}");
        }
        foreach (var f in after.Formations)
        {
            var shots = events.Where(e => e.Kind == TacticalEventKind.WeaponDischarged && e.FormationId == f.FormationId).ToList();
            var hits = events.Where(e => e.Kind == TacticalEventKind.AttackHit && e.FormationId == f.FormationId).ToList();
            var attempts = events.Count(e => e.Kind == TacticalEventKind.InterceptionAttempt && e.FormationId == f.FormationId);
            var intercepted = events.Count(e => e.Kind == TacticalEventKind.TorpedoIntercepted && e.FormationId == f.FormationId);
            Line($"  {f.FormationId}: {shots.Count} discharges, {hits.Count} hits, {events.Count(e => e.Kind == TacticalEventKind.AttackMissed && e.FormationId == f.FormationId)} misses; attempted yield {N(Sum(shots))}, landed yield {N(Sum(hits))}");
            Line($"    Point defense: {attempts} beam opportunities spent instead of offense, {intercepted} torpedoes intercepted (failed attempts also cost one opportunity).");
            foreach (var group in shots.GroupBy(e => (e.ShipId, e.WeaponId, e.TargetShipId, e.Sector)))
                Line($"    {group.Key.ShipId}/{group.Key.WeaponId} -> {group.Key.TargetShipId} ({group.Key.Sector}): {group.Count()} discharges, " +
                    $"{hits.Count(e => e.ShipId == group.Key.ShipId && e.WeaponId == group.Key.WeaponId && e.TargetShipId == group.Key.TargetShipId)} hits.");
            foreach (var s in f.Ships)
            {
                var shield = events.Where(e => e.Kind == TacticalEventKind.ShieldImpact && e.ShipId == s.ShipId).ToList();
                var armor = events.Where(e => e.Kind == TacticalEventKind.ArmorMitigation && e.ShipId == s.ShipId).ToList();
                var incoming = Sum(events.Where(e => e.Kind == TacticalEventKind.AttackHit && e.TargetShipId == s.ShipId));
                BigInteger hullBound = 0, postArmor = 0;
                // Read the backend's exact trace values; NEVER recalculate armor or eligibility.
                foreach (var e in armor) { hullBound += DetailNumber(e.Detail, "incoming"); postArmor += DetailNumber(e.Detail, "after"); }
                var actualHull = Sum(events.Where(e => e.Kind == TacticalEventKind.HullDamage && e.ShipId == s.ShipId));
                Line($"    {s.ShipId}: incoming landed {N(incoming)}; shields absorbed {N(Sum(shield))}; armor removed {N(hullBound - postArmor)}; post-armor {N(postArmor)}; hull lost {N(actualHull)}; overkill discarded {N(BigInteger.Max(0, postArmor - actualHull))}.");
                foreach (var e in shield) Line($"      {e.Sector} shield absorbed {N(e.Amount)} ({e.Detail}).");
                foreach (var e in events.Where(e => e.Kind == TacticalEventKind.ShieldRecharged && e.ShipId == s.ShipId)) Line($"      {e.Sector} recharged +{N(e.Amount)} AFTER the volley.");
                foreach (var e in events.Where(e => e.Kind == TacticalEventKind.ShipDestroyed && e.ShipId == s.ShipId)) Line($"      DESTROYED: {e.ShipId}");
                var oldShip = before.Formations.SelectMany(x => x.Ships).Single(x => x.ShipId == s.ShipId);
                foreach (var w in oldShip.Weapons.Where(w => !shots.Any(e => e.ShipId == s.ShipId && e.WeaponId == w.WeaponId)))
                    Line($"      No offensive discharge: {w.WeaponId}. Consult facing/arcs, range, cadence, ammo, power, target access or defensive spending; no shot is invented.");
            }
            Line($"    Local objective {f.Objective}: {f.ObjectiveProgress}/{f.ObjectiveRequiredRounds}; status {f.Status}{(f.ObjectiveCompleted ? "; COMPLETE" : "")}");
        }
        Line("  Attempted yield includes misses and intercepted torpedoes; landed yield reaches shields. Recharge does not undo hull damage.");
        if (detailed)
        {
            Line("  DETAILED AUTHORITATIVE EVENT LOG (display consumes no RNG)");
            foreach (var e in events) Line($"    #{e.Sequence:D3} {e.Kind}: {e.FormationId}/{e.ShipId}/{e.WeaponId} -> {e.TargetFormationId}/{e.TargetShipId}; sector={e.Sector}; range={e.Range}; amount={N(e.Amount)}; chance={e.ChanceBps}bps; {e.Detail}");
        }
        return b.ToString();
    }
    public static string Outcome(PlaygroundSession session)
    {
        var state = session.State; var b = new StringBuilder();
        b.Append("\nENCOUNTER OUTCOME: ").Append(state.Completion).Append('\n');
        if (state.Completion == TacticalCompletion.RoundLimit) b.Append("Three rounds elapsed. No decisive/local objective victory is implied.\n");
        foreach (var f in state.Formations)
            b.Append(f.FormationId).Append(": ").Append(f.Status).Append("; survivors ").Append(f.Ships.Count(s => s.Hull > 0)).Append('/').Append(f.Ships.Count)
                .Append("; hull ").Append(N(f.Ships.Aggregate(BigInteger.Zero, (sum, s) => sum + s.Hull))).Append("; objective ").Append(f.Objective)
                .Append(' ').Append(f.ObjectiveProgress).Append('/').Append(f.ObjectiveRequiredRounds).Append(f.ObjectiveCompleted ? " COMPLETE" : "").Append('\n');
        b.Append("Standalone local outcomes only: no campaign territory, movement, resupply or saves are affected.\n");
        b.Append("playground-sha256=").Append(session.Checksum()).Append('\n'); return b.ToString();
    }
    public static string Compare(PlaygroundSave previous, PlaygroundSave current)
    {
        string Facts(PlaygroundSave save)
        {
            var ships = save.State.Formations.Where(f => f.SideId == PlaygroundScenarios.PlayerSide).SelectMany(f => f.Ships).ToList();
            return $"{save.State.Completion}; human survivors={ships.Count(s => s.Hull > 0)}, hull={N(ships.Aggregate(BigInteger.Zero, (sum, s) => sum + s.Hull))}; orders=" +
                string.Join(" | ", save.Rounds.Select(r => string.Join(",", r.PlayerOrders.Select(o => o.FormationId + "=" + o.Maneuver))));
        }
        return "\nSAME-SEED COMPARISON\nPrevious: " + Facts(previous) + "\nCurrent:  " + Facts(current) + "\n";
    }
    private static BigInteger Sum(IEnumerable<TacticalEventV2> events) => events.Aggregate(BigInteger.Zero, (sum, e) => sum + e.Amount);
    private static BigInteger DetailNumber(string detail, string key)
    {
        var value = detail.Split(';').Single(piece => piece.StartsWith(key + "=", StringComparison.Ordinal)).Substring(key.Length + 1);
        return BigInteger.Parse(value, CultureInfo.InvariantCulture);
    }
}
