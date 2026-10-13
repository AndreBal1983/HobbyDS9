using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.TacticalCombatV2State;

namespace HobbyDS9.Simulation
{
    public sealed class TacticalRoundV2Result
    {
        private readonly TacticalEncounterV2Dto state;
        // Each read exports a detached copy; mutating it cannot alter this result.
        public TacticalEncounterV2Dto NextState => Copy(state);
        public IReadOnlyList<TacticalEventV2> Events { get; }
        internal TacticalRoundV2Result(TacticalEncounterV2Dto state, List<TacticalEventV2> events)
        { this.state = Copy(state); Events = events.AsReadOnly(); }
    }

    // Stateless, encounter-local, explicit v2 fixtures only. No session or v1 adapter.
    public sealed class DeterministicTacticalCombatV2Resolver
    {
        public TacticalRoundV2Result ResolveRound(TacticalEncounterV2Dto input, TacticalOrderBatchV2Dto? orders = null)
        {
            var state = Copy(input);
            Require(state.Completion == TacticalCompletion.None, "Encounter is complete.");
            var formations = state.Formations.ToDictionary(f => f.FormationId, StringComparer.Ordinal);
            if (orders != null)
            {
                Require(orders.EncounterId == state.EncounterId && orders.Round == state.NextRound && orders.Replacements != null, "Stale/missing joint order batch.");
                var ids = new HashSet<string>(StringComparer.Ordinal);
                // Validate all orders before committing any replacement; caller input is never changed.
                foreach (var order in orders.Replacements)
                {
                    Require(order != null && formations.TryGetValue(order.FormationId, out var f) && f.Status == TacticalFormationStatus.Active
                        && ids.Add(order.FormationId), "Duplicate, inactive or unknown ordered formation.");
                    ValidateOrder(order, formations[order.FormationId], formations);
                }
                foreach (var order in orders.Replacements) formations[order.FormationId].StandingOrder = CopyOrder(order);
            }
            var round = new Round(state, formations);
            round.Resolve();
            Validate(state);
            return new TacticalRoundV2Result(state, round.Events);
        }
        public IReadOnlyList<TacticalRoundV2Result> ResolveAutomatic(TacticalEncounterV2Dto input)
        {
            var state = Copy(input); var results = new List<TacticalRoundV2Result>();
            while (state.Completion == TacticalCompletion.None)
            {
                var result = ResolveRound(state); results.Add(result); state = result.NextState;
            }
            return results.AsReadOnly();
        }
        // Exact floor, aggregated once per target/volley. No artificial minimum damage.
        public static BigInteger ArmorDamage(BigInteger damage, long armor, int numerator = 6, int denominator = 100)
        {
            Require(damage >= 0 && armor >= 0 && numerator >= 0 && numerator <= 1000000 && denominator > 0 && denominator <= 1000000, "Invalid armor arithmetic.");
            return damage * denominator / ((BigInteger)denominator + (BigInteger)numerator * armor);
        }
        private sealed class Combatant
        {
            internal readonly TacticalFormationV2Dto Formation;
            internal readonly TacticalShipV2Dto Ship;
            internal Combatant(TacticalFormationV2Dto f, TacticalShipV2Dto s) { Formation = f; Ship = s; }
        }
        private sealed class Battery
        {
            internal readonly Combatant Owner;
            internal readonly TacticalWeaponV2Dto Weapon;
            internal int Remaining;
            internal int DefenseRemaining;
            internal bool Used;
            internal Battery(Combatant owner, TacticalWeaponV2Dto weapon, int defense)
            { Owner = owner; Weapon = weapon; Remaining = weapon.OpportunitiesPerRound; DefenseRemaining = defense; }
        }
        private sealed class Shot
        {
            internal readonly Battery Battery;
            internal readonly Combatant Target;
            internal readonly TacticalGeometryV2Dto Geometry;
            internal bool Intercepted;
            internal Shot(Battery battery, Combatant target, TacticalGeometryV2Dto geometry)
            { Battery = battery; Target = target; Geometry = geometry; }
        }
        private sealed class Round
        {
            private readonly TacticalEncounterV2Dto state;
            private readonly Dictionary<string, TacticalFormationV2Dto> formations;
            private readonly Dictionary<(string, string), TacticalGeometryV2Dto> geometry;
            private readonly EncounterRandomStream random;
            private readonly int index;
            private readonly List<Combatant> active;
            private readonly Dictionary<TacticalTargetPriority, List<Combatant>> targetCandidates = new Dictionary<TacticalTargetPriority, List<Combatant>>();
            private readonly Dictionary<(string, string), bool> targetAccess = new Dictionary<(string, string), bool>();
            internal readonly List<TacticalEventV2> Events = new List<TacticalEventV2>();
            internal Round(TacticalEncounterV2Dto state, Dictionary<string, TacticalFormationV2Dto> formations)
            {
                this.state = state; this.formations = formations; index = state.NextRound;
                geometry = state.Geometry.ToDictionary(g => (g.FormationA, g.FormationB));
                random = new EncounterRandomStream(state.RandomState);
                active = state.Formations.Where(f => f.Status == TacticalFormationStatus.Active)
                    .SelectMany(f => f.Ships.Where(s => s.Hull > 0).Select(s => new Combatant(f, s)))
                    .OrderBy(c => c.Ship.ShipId, StringComparer.Ordinal).ToList();
            }
            private void Emit(TacticalEventKind kind, string? formation = null, string? ship = null, string? targetFormation = null,
                string? target = null, string? weapon = null, TacticalSector? sector = null, TacticalRange? range = null,
                long amount = 0, int chance = 0, string detail = "") => Events.Add(new TacticalEventV2(Events.Count, index, kind,
                    formation, ship, targetFormation, target, weapon, sector, range, amount, chance, detail));
            internal void Resolve()
            {
                Emit(TacticalEventKind.RoundStarted);
                foreach (var f in state.Formations.Where(f => f.Status == TacticalFormationStatus.Active))
                    Emit(TacticalEventKind.OrderCommitted, formation: f.FormationId, detail: f.StandingOrder.Maneuver.ToString());
                Maneuver();
                PrepareTargets();
                var batteries = active.SelectMany(c => c.Ship.Weapons.Where(w => w.Equipped && w.AllocatedPower == w.RequiredPower
                    && w.Damage > 0 && w.NextFireRound <= index && (w.Class != TacticalWeaponClass.Torpedo || w.Ammunition > 0))
                    .Select(w => new Battery(c, w, DefenseCapacity(c.Formation, w)))).ToList();
                foreach (var b in batteries)
                    Emit(TacticalEventKind.FiringOpportunity, b.Owner.Formation.FormationId, b.Owner.Ship.ShipId,
                        weapon: b.Weapon.WeaponId, amount: b.Remaining);
                // Torpedo launches committed before allocating reactive defenses; then remaining beams/cannons.
                var shots = new List<Shot>();
                foreach (var b in batteries.Where(b => b.Weapon.Class == TacticalWeaponClass.Torpedo)) Launch(b, shots);
                foreach (var shot in shots) Intercept(shot, batteries);
                foreach (var b in batteries.Where(b => b.Weapon.Class != TacticalWeaponClass.Torpedo)) Launch(b, shots);
                foreach (var b in batteries.Where(b => b.Used)) b.Weapon.NextFireRound = index + b.Weapon.CadenceRounds;
                var incoming = new Dictionary<string, BigInteger[]>(StringComparer.Ordinal);
                foreach (var shot in shots)
                {
                    if (shot.Intercepted) continue;
                    var b = shot.Battery; var chance = Clamp(b.Weapon.AccuracyBps - shot.Target.Ship.EvasionBps
                        - (b.Weapon.Class == TacticalWeaponClass.Torpedo && shot.Geometry.Range == TacticalRange.Distant ? state.Rules.DistantTorpedoHitPenaltyBps : 0));
                    var hit = random.NextBelow(10000) < (ulong)chance;
                    var facing = Facing(shot.Geometry, shot.Target.Formation.FormationId);
                    Emit(hit ? TacticalEventKind.AttackHit : TacticalEventKind.AttackMissed, b.Owner.Formation.FormationId, b.Owner.Ship.ShipId,
                        shot.Target.Formation.FormationId, shot.Target.Ship.ShipId, b.Weapon.WeaponId, facing, shot.Geometry.Range, hit ? b.Weapon.Damage : 0, chance);
                    if (!hit) continue;
                    if (!incoming.TryGetValue(shot.Target.Ship.ShipId, out var sectors))
                    { sectors = new BigInteger[4]; incoming.Add(shot.Target.Ship.ShipId, sectors); }
                    sectors[(int)facing] += b.Weapon.Damage;
                }
                ApplyDamage(incoming);
                Objectives();
                state.NextRound = index + 1;
                var sides = state.Formations.Where(f => f.Status == TacticalFormationStatus.Active).Select(f => f.SideId).Distinct(StringComparer.Ordinal).Count();
                state.Completion = state.Formations.Any(f => f.ObjectiveCompleted) ? TacticalCompletion.Objective
                    : sides < 2 ? TacticalCompletion.Decisive : index == 3 ? TacticalCompletion.RoundLimit : TacticalCompletion.None;
                if (state.Completion == TacticalCompletion.None) Recharge(incoming);
                else Emit(TacticalEventKind.EncounterCompleted, detail: state.Completion.ToString());
                state.RandomState = random.State;
            }
            private int Score(TacticalFormationV2Dto f)
            {
                var r = state.Rules;
                return f.Speed * r.SpeedWeight + f.Agility * r.AgilityWeight + f.Sensors * r.SensorsWeight
                    + f.Command * r.CommandWeight + f.Cohesion * r.CohesionWeight;
            }
            private static int Direction(TacticalFormationV2Dto f, TacticalRange range)
            {
                switch (f.StandingOrder.Maneuver)
                {
                    case TacticalManeuver.Charge: case TacticalManeuver.Breakthrough: case TacticalManeuver.Pursuit: return 1;
                    case TacticalManeuver.FightingWithdrawal: case TacticalManeuver.Disengage: return -1;
                    default: return Math.Sign((int)f.StandingOrder.PreferredRange - (int)range);
                }
            }
            private void Maneuver()
            {
                foreach (var g in state.Geometry)
                {
                    var a = formations[g.FormationA]; var b = formations[g.FormationB];
                    if (a.Status != TacticalFormationStatus.Active || b.Status != TacticalFormationStatus.Active) continue;
                    int da = Direction(a, g.Range), db = Direction(b, g.Range), difference = Score(a) - Score(b);
                    var direction = da == db ? da : Math.Abs(difference) <= state.Rules.ManeuverTieMargin
                        ? (da == 0 ? db : db == 0 ? da : 0) : difference > 0 ? da : db;
                    var previous = g.Range;
                    g.Range = (TacticalRange)Math.Max(0, Math.Min(3, (int)g.Range + direction));
                    g.FacingA = IsWithdrawing(a) ? TacticalSector.Aft : TacticalSector.Fore;
                    g.FacingB = IsWithdrawing(b) ? TacticalSector.Aft : TacticalSector.Fore;
                    // A won flank leaves its own forward weapons facing the opponent's lateral sector.
                    if (a.StandingOrder.Maneuver == TacticalManeuver.Flank && difference > state.Rules.ManeuverTieMargin)
                    { g.FacingA = TacticalSector.Fore; g.FacingB = TacticalSector.Port; }
                    else if (b.StandingOrder.Maneuver == TacticalManeuver.Flank && -difference > state.Rules.ManeuverTieMargin)
                    { g.FacingB = TacticalSector.Fore; g.FacingA = TacticalSector.Starboard; }
                    Emit(TacticalEventKind.Maneuver, a.FormationId, targetFormation: b.FormationId, sector: g.FacingA, range: g.Range,
                        detail: previous + "->" + g.Range + ";opponent-facing=" + g.FacingB + ";contest=" + difference.ToString(CultureInfo.InvariantCulture));
                }
            }
            private static bool IsWithdrawing(TacticalFormationV2Dto f) => f.StandingOrder.Maneuver == TacticalManeuver.FightingWithdrawal || f.StandingOrder.Maneuver == TacticalManeuver.Disengage;
            private TacticalGeometryV2Dto Pair(string a, string b) => StringComparer.Ordinal.Compare(a, b) < 0 ? geometry[(a, b)] : geometry[(b, a)];
            private static TacticalSector Facing(TacticalGeometryV2Dto g, string id) => id == g.FormationA ? g.FacingA : g.FacingB;
            private static bool Covers(TacticalWeaponV2Dto w, TacticalSector facing) => (w.Coverage & (TacticalArc)(1 << (int)facing)) != 0;
            private static bool InRange(TacticalWeaponV2Dto w, TacticalRange range) => w.Class == TacticalWeaponClass.Torpedo
                || (w.Class == TacticalWeaponClass.Beam ? range >= TacticalRange.Engagement : range >= TacticalRange.Close);
            private bool Eligible(Battery b, Combatant target)
            {
                if (target.Formation.SideId == b.Owner.Formation.SideId) return false;
                var g = Pair(b.Owner.Formation.FormationId, target.Formation.FormationId);
                if (!InRange(b.Weapon, g.Range) || !Covers(b.Weapon, Facing(g, b.Owner.Formation.FormationId))) return false;
                var key = (b.Owner.Formation.FormationId, target.Formation.FormationId);
                if (targetAccess.TryGetValue(key, out var access)) return access;
                // Screen can block direct acquisition of its designated ally while in nearby attack access.
                access = Facing(g, target.Formation.FormationId) == TacticalSector.Port || Facing(g, target.Formation.FormationId) == TacticalSector.Starboard
                    || !state.Formations.Any(f => f.Status == TacticalFormationStatus.Active && f.SideId == target.Formation.SideId
                    && f.StandingOrder.Maneuver == TacticalManeuver.Screen && f.StandingOrder.ProtectedFormationId == target.Formation.FormationId
                    && Nearby(Pair(b.Owner.Formation.FormationId, f.FormationId), g));
                targetAccess.Add(key, access); return access;
            }
            private static bool Nearby(TacticalGeometryV2Dto screen, TacticalGeometryV2Dto target) => screen.Range >= TacticalRange.Engagement
                && target.Range >= TacticalRange.Engagement && Math.Abs((int)screen.Range - (int)target.Range) <= 1;
            private Combatant? Target(Battery b)
            {
                var order = b.Owner.Formation.StandingOrder; Combatant? best = null;
                foreach (var candidate in targetCandidates[order.TargetPriority])
                    if (Eligible(b, candidate) && (best == null || Compare(candidate, best, order.TargetPriority, order.TargetFormationId) < 0)) best = candidate;
                return best;
            }
            private void PrepareTargets()
            {
                // All weapons in a formation have the same target access. Rank ships once,
                // then acquire from one candidate per hostile formation rather than O(ships²).
                var groups = active.GroupBy(c => c.Formation.FormationId).ToList();
                foreach (TacticalTargetPriority priority in Enum.GetValues(typeof(TacticalTargetPriority)))
                {
                    var candidates = new List<Combatant>();
                    foreach (var group in groups)
                    {
                        Combatant? best = null;
                        foreach (var c in group) if (best == null || Compare(c, best, priority, null) < 0) best = c;
                        candidates.Add(best!);
                    }
                    targetCandidates.Add(priority, candidates);
                }
            }
            private static int Compare(Combatant a, Combatant b, TacticalTargetPriority priority, string? targetFormation)
            {
                int compare = (a.Formation.FormationId == targetFormation ? 0 : 1).CompareTo(b.Formation.FormationId == targetFormation ? 0 : 1);
                if (compare == 0) compare = Rank(a, priority).CompareTo(Rank(b, priority));
                if (compare == 0 && priority == TacticalTargetPriority.LowestHullRatio)
                    compare = ((BigInteger)a.Ship.Hull * b.Ship.MaxHull).CompareTo((BigInteger)b.Ship.Hull * a.Ship.MaxHull);
                return compare == 0 ? StringComparer.Ordinal.Compare(a.Ship.ShipId, b.Ship.ShipId) : compare;
            }
            private static int Rank(Combatant c, TacticalTargetPriority p) => p == TacticalTargetPriority.CommandSupport
                ? (c.Ship.Role == TacticalShipRole.CommandSupport ? 0 : 1) : p == TacticalTargetPriority.Screen
                ? (c.Formation.StandingOrder.Maneuver == TacticalManeuver.Screen || c.Ship.Role == TacticalShipRole.Escort ? 0 : 1) : 0;
            private void Launch(Battery b, List<Shot> shots)
            {
                var target = Target(b); if (target == null) return;
                var g = Pair(b.Owner.Formation.FormationId, target.Formation.FormationId);
                while (b.Remaining > 0 && (b.Weapon.Class != TacticalWeaponClass.Torpedo || b.Weapon.Ammunition > 0))
                {
                    b.Remaining--; b.Used = true;
                    if (b.Weapon.Class == TacticalWeaponClass.Torpedo) b.Weapon.Ammunition--;
                    shots.Add(new Shot(b, target, g));
                    Emit(TacticalEventKind.WeaponDischarged, b.Owner.Formation.FormationId, b.Owner.Ship.ShipId, target.Formation.FormationId,
                        target.Ship.ShipId, b.Weapon.WeaponId, Facing(g, target.Formation.FormationId), g.Range, b.Weapon.Damage, detail: b.Weapon.Class.ToString());
                }
            }
            private int DefenseCapacity(TacticalFormationV2Dto f, TacticalWeaponV2Dto w)
            {
                if (w.Class != TacticalWeaponClass.Beam) return 0;
                var r = state.Rules; var m = f.StandingOrder.Maneuver;
                var fraction = m == TacticalManeuver.Screen ? r.ScreenDefenseBps : IsWithdrawing(f) ? r.WithdrawalDefenseBps
                    : m == TacticalManeuver.Charge || m == TacticalManeuver.Breakthrough || m == TacticalManeuver.Pursuit ? r.ChargeDefenseBps : r.HoldDefenseBps;
                return w.OpportunitiesPerRound * fraction / 10000;
            }
            private void Intercept(Shot shot, List<Battery> batteries)
            {
                // Screen allies get first access; then self defense, canonical ship/weapon order.
                var defenders = batteries.Where(b => b.DefenseRemaining > 0 && b.Owner.Formation.SideId == shot.Target.Formation.SideId)
                    .OrderBy(b => b.Owner.Formation.StandingOrder.Maneuver == TacticalManeuver.Screen ? 0 : 1);
                foreach (var b in defenders)
                {
                    bool self = b.Owner.Ship.ShipId == shot.Target.Ship.ShipId;
                    bool ally = b.Owner.Formation.StandingOrder.Maneuver == TacticalManeuver.Screen
                        && (b.Owner.Formation.FormationId == shot.Target.Formation.FormationId
                            || b.Owner.Formation.StandingOrder.ProtectedFormationId == shot.Target.Formation.FormationId);
                    if (!self && !ally) continue;
                    var g = Pair(b.Owner.Formation.FormationId, shot.Battery.Owner.Formation.FormationId);
                    if (!self && !Nearby(g, shot.Geometry)) continue;
                    if (!Covers(b.Weapon, Facing(g, b.Owner.Formation.FormationId))) continue;
                    b.Remaining--; b.DefenseRemaining--; b.Used = true;
                    var chance = Clamp(state.Rules.InterceptionBaseBps + b.Weapon.TrackingBps + b.Owner.Formation.Sensors * 20
                        + (g.Range == TacticalRange.Distant ? state.Rules.DistantInterceptionBonusBps : 0));
                    Emit(TacticalEventKind.InterceptionAttempt, b.Owner.Formation.FormationId, b.Owner.Ship.ShipId,
                        shot.Battery.Owner.Formation.FormationId, shot.Battery.Owner.Ship.ShipId, b.Weapon.WeaponId, range: g.Range, chance: chance,
                        detail: "incoming=" + shot.Battery.Weapon.WeaponId + ";protected=" + shot.Target.Ship.ShipId);
                    if (random.NextBelow(10000) >= (ulong)chance) continue;
                    shot.Intercepted = true;
                    Emit(TacticalEventKind.TorpedoIntercepted, b.Owner.Formation.FormationId, b.Owner.Ship.ShipId,
                        shot.Target.Formation.FormationId, shot.Target.Ship.ShipId, b.Weapon.WeaponId, amount: shot.Battery.Weapon.Damage);
                    break;
                }
            }
            private void ApplyDamage(Dictionary<string, BigInteger[]> incoming)
            {
                foreach (var c in active)
                {
                    if (!incoming.TryGetValue(c.Ship.ShipId, out var sectors)) continue;
                    BigInteger hullBound = 0;
                    foreach (var q in c.Ship.Shields)
                    {
                        var damage = sectors[(int)q.Sector]; if (damage == 0) continue;
                        long absorbed = (long)BigInteger.Min(damage, q.Current); q.Current -= absorbed; hullBound += damage - absorbed;
                        Emit(TacticalEventKind.ShieldImpact, c.Formation.FormationId, c.Ship.ShipId, sector: q.Sector, amount: absorbed,
                            detail: "incoming=" + damage.ToString(CultureInfo.InvariantCulture) + ";remaining=" + q.Current.ToString(CultureInfo.InvariantCulture));
                    }
                    if (hullBound == 0) continue;
                    var mitigated = ArmorDamage(hullBound, c.Ship.Armor, state.Rules.ArmorNumerator, state.Rules.ArmorDenominator);
                    // Exact arbitrary-size values are invariant decimal trace data; actual loss fits Int64.
                    Emit(TacticalEventKind.ArmorMitigation, c.Formation.FormationId, c.Ship.ShipId,
                        detail: "incoming=" + hullBound.ToString(CultureInfo.InvariantCulture) + ";after=" + mitigated.ToString(CultureInfo.InvariantCulture));
                    long loss = (long)BigInteger.Min(mitigated, c.Ship.Hull); c.Ship.Hull -= loss;
                    Emit(TacticalEventKind.HullDamage, c.Formation.FormationId, c.Ship.ShipId, amount: loss);
                    if (c.Ship.Hull == 0) Emit(TacticalEventKind.ShipDestroyed, c.Formation.FormationId, c.Ship.ShipId);
                }
            }
            private void Objectives()
            {
                // Evaluate every objective against the same post-volley/pre-exit set.
                var survivors = state.Formations.Where(f => f.Status == TacticalFormationStatus.Active && f.Ships.Any(s => s.Hull > 0)).ToList();
                foreach (var f in state.Formations.Where(f => f.Status == TacticalFormationStatus.Active))
                {
                    if (f.Ships.All(s => s.Hull == 0))
                    { f.Status = TacticalFormationStatus.Destroyed; Emit(TacticalEventKind.FormationDestroyed, f.FormationId); continue; }
                    var enemies = survivors.Where(e => e.SideId != f.SideId).ToList();
                    bool escaped = f.StandingOrder.Maneuver == TacticalManeuver.Disengage && enemies.All(e => Pair(f.FormationId, e.FormationId).Range == TacticalRange.Distant
                        && Score(f) > Score(e) + state.Rules.ManeuverTieMargin);
                    bool passed = f.StandingOrder.Maneuver == TacticalManeuver.Breakthrough && enemies.All(e => Pair(f.FormationId, e.FormationId).Range == TacticalRange.Intermixed
                        && Score(f) > Score(e) + state.Rules.ManeuverTieMargin);
                    var progress = (f.Objective == TacticalObjective.Hold || f.Objective == TacticalObjective.Delay)
                        || (f.Objective == TacticalObjective.Breakthrough && passed) || (f.Objective == TacticalObjective.Disengage && escaped);
                    if (progress)
                    {
                        f.ObjectiveProgress++; Emit(TacticalEventKind.ObjectiveProgress, f.FormationId, amount: f.ObjectiveProgress, detail: f.Objective.ToString());
                        f.ObjectiveCompleted = f.ObjectiveProgress == f.ObjectiveRequiredRounds;
                        if (f.ObjectiveCompleted) Emit(TacticalEventKind.ObjectiveCompleted, f.FormationId, detail: f.Objective.ToString());
                    }
                    if (f.Objective == TacticalObjective.Breakthrough && f.ObjectiveCompleted)
                    { f.Status = TacticalFormationStatus.BrokenThrough; Emit(TacticalEventKind.BrokenThrough, f.FormationId); }
                    else if (escaped && (f.Objective != TacticalObjective.Disengage || f.ObjectiveCompleted))
                    { f.Status = TacticalFormationStatus.Retreated; Emit(TacticalEventKind.Retreated, f.FormationId); }
                }
            }
            private void Recharge(Dictionary<string, BigInteger[]> incoming)
            {
                foreach (var c in active.Where(c => c.Ship.Hull > 0 && c.Formation.Status == TacticalFormationStatus.Active))
                {
                    var s = c.Ship; if (s.ShieldPowerAllocated < s.ShieldPowerRequired) continue;
                    long budget = s.ShieldRechargePerRound; incoming.TryGetValue(s.ShipId, out var hit);
                    // Fixed Fore/Port/Starboard/Aft priority, hit-facing suppression, one shared budget.
                    foreach (var q in s.Shields)
                    {
                        if (budget == 0) break;
                        if (state.Rules.SuppressRechargeOnHitSector && hit != null && hit[(int)q.Sector] > 0) continue;
                        var gain = Math.Min(budget, q.Maximum - q.Current); if (gain == 0) continue;
                        q.Current += gain; budget -= gain;
                        Emit(TacticalEventKind.ShieldRecharged, c.Formation.FormationId, s.ShipId, sector: q.Sector, amount: gain);
                    }
                }
            }
            private static int Clamp(int value) => Math.Max(0, Math.Min(10000, value));
        }
    }
}
