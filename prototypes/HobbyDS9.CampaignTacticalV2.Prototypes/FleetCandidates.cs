using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.CampaignTacticalV2.Prototypes
{
    public enum MobilityCandidate { IndividualThreshold = 1, CoordinatedThreshold = 2 }
    public enum TargetRankingCandidate { LowestId = 1, LowestHullRatio = 2 }
    public enum LocalTargetAspectCandidate { Fore = 1, Lateral = 2 }
    public enum PrototypeManeuver { Hold = 1, Charge = 2, Flank = 3, Screen = 4, FightingWithdrawal = 5 }
    public sealed class MobilityInput
    {
        public string Ship { get; }
        public int Speed { get; }
        public int Agility { get; }
        public int Command { get; }
        public int Cohesion { get; }
        public int DefensiveOpportunities { get; }
        public MobilityInput(string ship, int speed, int agility, int command, int cohesion, int defensiveOpportunities)
        {
            if (string.IsNullOrWhiteSpace(ship) || new[] { speed, agility, command, cohesion }.Any(x => x < 0 || x > 100)
                || defensiveOpportunities < 0) throw new ArgumentException("Invalid mobility source.");
            Ship = ship; Speed = speed; Agility = agility; Command = command; Cohesion = cohesion; DefensiveOpportunities = defensiveOpportunities;
        }
    }
    public sealed class MobilityContribution
    {
        public string Ship { get; }
        public int PersonalMobility { get; }
        public int Coordination { get; }
        public int ScreeningOpportunities { get; }
        public int ManeuverMargin { get; }
        public LocalTargetAspectCandidate FlankTargetAspect => ManeuverMargin > 0 ? LocalTargetAspectCandidate.Lateral : LocalTargetAspectCandidate.Fore;
        internal MobilityContribution(string ship, int mobility, int coordination, int opportunities, int margin)
        { Ship = ship; PersonalMobility = mobility; Coordination = coordination; ScreeningOpportunities = opportunities; ManeuverMargin = margin; }
    }
    public static class FleetCandidates
    {
        // Explicit review formula: floor((speed + agility)/2); no role labels or fleet-size divisor.
        // Optional positive coordination: max member min(command,cohesion) / supplied divisor.
        // Opportunity gate compares each ship with the supplied incoming threat; never creates extra shots.
        public static IReadOnlyList<MobilityContribution> Compare(IEnumerable<MobilityInput> ships, int threatMobility,
            MobilityCandidate candidate, int coordinationDivisor)
        {
            var source = ships.OrderBy(s => s.Ship, StringComparer.Ordinal).ToList();
            if (source.Count == 0 || source.Select(s => s.Ship).Distinct(StringComparer.Ordinal).Count() != source.Count
                || threatMobility < 0 || threatMobility > 100 || coordinationDivisor <= 0
                || !Enum.IsDefined(typeof(MobilityCandidate), candidate)) throw new ArgumentException("Invalid mobility candidate.");
            var support = candidate == MobilityCandidate.CoordinatedThreshold
                ? source.Max(s => Math.Min(s.Command, s.Cohesion)) / coordinationDivisor : 0;
            return source.Select(s => new MobilityContribution(s.Ship, (s.Speed + s.Agility) / 2, support,
                (s.Speed + s.Agility) / 2 + support >= threatMobility ? s.DefensiveOpportunities : 0,
                (s.Speed + s.Agility) / 2 + support - threatMobility)).ToList().AsReadOnly();
        }
        public static IReadOnlyList<string> RankVisibleTargets(IEnumerable<PrototypeShip> eligible, TargetRankingCandidate candidate)
        {
            if (!Enum.IsDefined(typeof(TargetRankingCandidate), candidate)) throw new ArgumentException("Unknown target ranking.");
            var targets = eligible.ToList();
            if (targets.Any(s => s == null || s.Hull <= 0 || s.MaxHull < s.Hull || s.Hidden)) throw new ArgumentException("Not a visible surviving target.");
            targets.Sort((a, b) => {
                var ratio = candidate == TargetRankingCandidate.LowestHullRatio
                    ? ((System.Numerics.BigInteger)a.Hull * b.MaxHull).CompareTo((System.Numerics.BigInteger)b.Hull * a.MaxHull) : 0;
                return ratio != 0 ? ratio : StringComparer.Ordinal.Compare(a.Id, b.Id);
            });
            return targets.Select(s => s.Id).ToList().AsReadOnly();
        }
    }
    // Typed proposed commands/checkpoints are confined to this assembly and have no campaign conversion.
    public sealed class PrototypeShip
    {
        public string Id { get; set; } = "";
        public string Fleet { get; set; } = "";
        public string Owner { get; set; } = "";
        public string Type { get; set; } = "";
        public string Fit { get; set; } = "";
        public int FitRevision { get; set; }
        public long Hull { get; set; }
        public long MaxHull { get; set; }
        public long[] Shields { get; set; } = Array.Empty<long>();
        public long[] ShieldMaximum { get; set; } = Array.Empty<long>();
        public long Ammunition { get; set; }
        public long MagazineCapacity { get; set; }
        public bool CloakCapable { get; set; }
        public bool Hidden { get; set; }
        public int Opportunities { get; set; }
        public int DefenseOpportunities { get; set; }
        public long TorpedoDamage { get; set; }
        public long EffectivePower { get; set; }
        public long BaseAllocation { get; set; }
        public long CloakRequirement { get; set; }
    }
    public sealed class PrototypeFleetOrder
    {
        public string Fleet { get; set; } = "";
        public PrototypeManeuver Maneuver { get; set; }
        public string? FocusFleet { get; set; }
    }
    public sealed class PrototypeRoundCommand
    {
        public string Encounter { get; set; } = "";
        public string Actor { get; set; } = "";
        public string Command { get; set; } = "";
        public int Round { get; set; }
        public long Revision { get; set; }
        public List<PrototypeFleetOrder> Orders { get; set; } = new List<PrototypeFleetOrder>();
    }
    public sealed class PrototypeCommitment
    {
        public string Actor { get; set; } = "";
        public List<PrototypeFleetOrder> Orders { get; set; } = new List<PrototypeFleetOrder>();
    }
    public sealed class PrototypeCheckpoint
    {
        public string Identity { get; set; } = RoundAccessPrototype.Identity;
        public string Encounter { get; set; } = "";
        public int NextRound { get; set; } = 1;
        public long Revision { get; set; }
        public int RollCursor { get; set; }
        public int[] FixtureRolls { get; set; } = Array.Empty<int>();
        public int InterceptionChanceBps { get; set; }
        public TargetRankingCandidate TargetRanking { get; set; }
        public List<PrototypeShip> Ships { get; set; } = new List<PrototypeShip>();
        public List<PrototypeFleetOrder> StandingOrders { get; set; } = new List<PrototypeFleetOrder>();
        public List<PrototypeCommitment> Commitments { get; set; } = new List<PrototypeCommitment>();
        public List<string> AcceptedCommands { get; set; } = new List<string>();
        public List<PrototypeObservedEvent> Reports { get; set; } = new List<PrototypeObservedEvent>();
    }
    public sealed class VisibleEnemy
    {
        public string Ship { get; }
        public string Fleet { get; }
        public string Type { get; }
        public long Hull { get; }
        public IReadOnlyList<long> Shields { get; }
        internal VisibleEnemy(PrototypeShip s)
        { Ship = s.Id; Fleet = s.Fleet; Type = s.Type; Hull = s.Hull; Shields = s.Shields.ToList().AsReadOnly(); }
    }
    public sealed class PrototypeObservedEvent
    {
        public int Round { get; set; }
        public string Audience { get; set; } = "";
        public string Message { get; set; } = "";
    }
    public sealed class PrototypeObservation
    {
        public string Encounter { get; }
        public int Round { get; }
        public long Revision { get; }
        public bool Committed { get; }
        public IReadOnlyList<PrototypeShip> OwnShips { get; }
        public IReadOnlyList<VisibleEnemy> VisibleEnemies { get; }
        public IReadOnlyList<PrototypeObservedEvent> Reports { get; }
        internal PrototypeObservation(PrototypeCheckpoint s, string actor)
        {
            Encounter = s.Encounter; Round = s.NextRound; Revision = s.Revision;
            Committed = s.Commitments.Any(c => c.Actor == actor);
            OwnShips = s.Ships.Where(p => p.Owner == actor).Select(RoundAccessPrototype.Copy).ToList().AsReadOnly();
            VisibleEnemies = s.Ships.Where(p => p.Owner != actor && !p.Hidden && p.Hull > 0).Select(p => new VisibleEnemy(p)).ToList().AsReadOnly();
            Reports = s.Reports.Where(e => e.Audience == actor).Select(e => new PrototypeObservedEvent { Round = e.Round, Audience = e.Audience, Message = e.Message }).ToList().AsReadOnly();
        }
    }
    // A deliberately small executable access/transaction proposal, NOT the production resolver.
    // Torpedoes hit automatically after a failed supplied fixture-roll interception; armor,
    // geometry, cadence, energy fire and recharge are intentionally absent. No live activation.
    public sealed class RoundAccessPrototype
    {
        public const string Identity = "campaign-lifecycle-review-prototype-1";
        private PrototypeCheckpoint state;
        public RoundAccessPrototype(PrototypeCheckpoint checkpoint) { Validate(checkpoint); state = Copy(checkpoint); }
        public PrototypeCheckpoint Export() => Copy(state);
        public PrototypeObservation Observe(string actor)
        { if (!state.Ships.Any(s => s.Owner == actor)) throw new ArgumentException("Unknown actor."); return new PrototypeObservation(state, actor); }
        public PrototypeFleetOrder[] BaselineOrders(string actor)
        {
            var observation = Observe(actor);
            var focus = observation.VisibleEnemies.OrderBy(e => e.Fleet, StringComparer.Ordinal).FirstOrDefault()?.Fleet;
            // Same observation, no opposing commitments: deterministic Hold baseline.
            return observation.OwnShips.Where(s => s.Hull > 0).Select(s => s.Fleet).Distinct(StringComparer.Ordinal).OrderBy(f => f, StringComparer.Ordinal)
                .Select(f => new PrototypeFleetOrder { Fleet = f, Maneuver = PrototypeManeuver.Hold, FocusFleet = focus }).ToArray();
        }
        public bool Commit(PrototypeRoundCommand command)
        {
            if (command == null || command.Encounter != state.Encounter || command.Round != state.NextRound || state.NextRound > 3
                || command.Revision != state.Revision || state.Revision == long.MaxValue || string.IsNullOrWhiteSpace(command.Command)
                || state.AcceptedCommands.Contains(command.Command, StringComparer.Ordinal)
                || state.Commitments.Any(c => c.Actor == command.Actor) || !state.Ships.Any(s => s.Owner == command.Actor && s.Hull > 0)
                || command.Orders == null) return false;
            var owned = state.Ships.Where(s => s.Owner == command.Actor && s.Hull > 0).Select(s => s.Fleet).Distinct(StringComparer.Ordinal).ToList();
            if (command.Orders.Count != owned.Count || command.Orders.Select(o => o?.Fleet).Distinct(StringComparer.Ordinal).Count() != owned.Count
                || command.Orders.Any(o => o == null || !owned.Contains(o.Fleet, StringComparer.Ordinal) || !Enum.IsDefined(typeof(PrototypeManeuver), o.Maneuver)
                    || o.FocusFleet != null && o.FocusFleet != state.StandingOrders.Single(s => s.Fleet == o.Fleet).FocusFleet
                        && !state.Ships.Any(s => s.Fleet == o.FocusFleet && s.Owner != command.Actor && s.Hull > 0 && !s.Hidden)))
                return false;
            var next = Copy(state);
            next.Commitments.Add(new PrototypeCommitment { Actor = command.Actor, Orders = command.Orders.Select(Copy).OrderBy(o => o.Fleet, StringComparer.Ordinal).ToList() });
            next.AcceptedCommands.Add(command.Command);
            var actors = next.Ships.Where(s => s.Hull > 0).Select(s => s.Owner).Distinct(StringComparer.Ordinal).ToList();
            if (actors.All(a => next.Commitments.Any(c => c.Actor == a)))
            {
                foreach (var c in next.Commitments)
                    foreach (var o in c.Orders) next.StandingOrders.Single(s => s.Fleet == o.Fleet).Maneuver = o.Maneuver;
                foreach (var c in next.Commitments)
                    foreach (var o in c.Orders) next.StandingOrders.Single(s => s.Fleet == o.Fleet).FocusFleet = o.FocusFleet;
                Resolve(next);
                next.Commitments.Clear();
                next.NextRound++;
                if (next.Ships.Where(s => s.Hull > 0).Select(s => s.Owner).Distinct(StringComparer.Ordinal).Count() < 2) next.NextRound = 4;
                next.Revision++;
            }
            // Commitment acceptance does not advance the observation stamp: the opposing actor
            // can submit from the exact same start-round observation. Its own pending flag changes.
            Validate(next); state = next; return true;
        }
        private static void Resolve(PrototypeCheckpoint s)
        {
            var active = s.Ships.Where(p => p.Hull > 0).OrderBy(p => p.Id, StringComparer.Ordinal).ToList();
            var defense = active.ToDictionary(p => p.Id, p => p.DefenseOpportunities, StringComparer.Ordinal);
            var offense = active.ToDictionary(p => p.Id, p => p.Opportunities, StringComparer.Ordinal);
            var pending = active.ToDictionary(p => p.Id, _ => new System.Numerics.BigInteger(0), StringComparer.Ordinal);
            void Report(string actor, string message) => s.Reports.Add(new PrototypeObservedEvent { Round = s.NextRound, Audience = actor, Message = message });
            foreach (var launcher in active)
            {
                var order = s.StandingOrders.Single(o => o.Fleet == launcher.Fleet);
                while (offense[launcher.Id] > 0 && launcher.Ammunition > 0)
                {
                    var eligible = active.Where(p => p.Owner != launcher.Owner && !p.Hidden).ToList();
                    if (eligible.Count == 0) break; // no draw, stock or opportunity spent on nonexistent targets.
                    var preferred = eligible.Where(p => p.Fleet == order.FocusFleet).ToList();
                    var pool = preferred.Count > 0 ? preferred : eligible;
                    var selected = FleetCandidates.RankVisibleTargets(pool, s.TargetRanking)[0];
                    var target = pool.Single(p => p.Id == selected);
                    if (order.FocusFleet != null && preferred.Count == 0) Report(launcher.Owner, "focus-fallback: visible fleet " + target.Fleet);
                    launcher.Ammunition--; offense[launcher.Id]--;
                    Report(launcher.Owner, "launched: " + launcher.Id + " -> " + target.Id);
                    // Incoming ordnance is visible; the hidden launcher is intentionally not named to its opponent.
                    Report(target.Owner, "incoming-torpedo: " + target.Id);
                    bool intercepted = false;
                    foreach (var defender in active.Where(p => p.Fleet == target.Fleet).OrderBy(p => p.Id, StringComparer.Ordinal))
                    {
                        var screen = s.StandingOrders.Single(o => o.Fleet == defender.Fleet).Maneuver == PrototypeManeuver.Screen;
                        if (!screen || defense[defender.Id] == 0 || offense[defender.Id] == 0) continue;
                        defense[defender.Id]--; offense[defender.Id]--; // finite attempt costs, including misses.
                        var roll = s.FixtureRolls[s.RollCursor % s.FixtureRolls.Length]; s.RollCursor = checked(s.RollCursor + 1);
                        intercepted = roll < s.InterceptionChanceBps;
                        Report(target.Owner, intercepted ? "interception-succeeded" : "interception-failed");
                        if (intercepted) break;
                    }
                    if (!intercepted) pending[target.Id] += launcher.TorpedoDamage;
                }
            }
            foreach (var target in active)
            {
                var absorbed = System.Numerics.BigInteger.Min(pending[target.Id], target.Shields[0]);
                target.Shields[0] -= (long)absorbed;
                target.Hull -= (long)System.Numerics.BigInteger.Min(target.Hull, pending[target.Id] - absorbed);
                if (s.NextRound == 1) target.Hidden = false; // all survivors reveal, firing irrelevant.
            }
        }
        public static bool CloakPowerFeasible(PrototypeShip ship) => ship.EffectivePower >= 0 && ship.BaseAllocation >= 0 && ship.CloakRequirement >= 0
            && (System.Numerics.BigInteger)ship.BaseAllocation + (ship.Hidden ? ship.CloakRequirement : 0) <= ship.EffectivePower;
        private static void Validate(PrototypeCheckpoint s)
        {
            if (s == null || s.Identity != Identity || string.IsNullOrWhiteSpace(s.Encounter) || s.NextRound < 1 || s.NextRound > 4
                || s.Revision < 0 || s.RollCursor < 0 || s.FixtureRolls == null || s.FixtureRolls.Length == 0 || s.FixtureRolls.Any(r => r < 0 || r >= 10000)
                || s.InterceptionChanceBps < 0 || s.InterceptionChanceBps > 10000 || s.Ships == null || s.StandingOrders == null
                || !Enum.IsDefined(typeof(TargetRankingCandidate), s.TargetRanking)
                || s.Commitments == null || s.AcceptedCommands == null || s.Reports == null || s.Ships.Count == 0)
                throw new ArgumentException("Invalid prototype checkpoint.");
            if (s.Ships.Any(p => p == null || new[] { p.Id, p.Fleet, p.Owner, p.Type, p.Fit }.Any(string.IsNullOrWhiteSpace) || p.FitRevision <= 0
                || p.MaxHull <= 0 || p.Hull < 0 || p.Hull > p.MaxHull || p.Shields == null || p.Shields.Length != 4
                || p.ShieldMaximum == null || p.ShieldMaximum.Length != 4 || p.ShieldMaximum.Any(v => v < 0)
                || p.Shields.Where((v, i) => v < 0 || v > p.ShieldMaximum[i]).Any()
                || p.Ammunition < 0 || p.MagazineCapacity < 0 || p.Ammunition > p.MagazineCapacity
                || p.Opportunities < 0 || p.DefenseOpportunities < 0 || p.DefenseOpportunities > p.Opportunities || p.TorpedoDamage < 0
                || p.Hidden && (!p.CloakCapable || s.NextRound != 1) || !CloakPowerFeasible(p))
                || s.Ships.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != s.Ships.Count
                || s.Ships.GroupBy(p => p.Fleet, StringComparer.Ordinal).Any(g => g.Select(p => p.Owner).Distinct(StringComparer.Ordinal).Count() != 1)
                || s.StandingOrders.Count != s.Ships.Select(p => p.Fleet).Distinct(StringComparer.Ordinal).Count()
                || s.StandingOrders.Select(o => o?.Fleet).Distinct(StringComparer.Ordinal).Count() != s.StandingOrders.Count
                || s.StandingOrders.Any(o => o == null || !s.Ships.Any(p => p.Fleet == o.Fleet) || !Enum.IsDefined(typeof(PrototypeManeuver), o.Maneuver)
                    || o.FocusFleet != null && !s.Ships.Any(p => p.Fleet == o.FocusFleet && p.Owner != s.Ships.First(x => x.Fleet == o.Fleet).Owner)))
                throw new ArgumentException("Invalid source or standing order.");
            if (s.AcceptedCommands.Any(string.IsNullOrWhiteSpace) || s.AcceptedCommands.Distinct(StringComparer.Ordinal).Count() != s.AcceptedCommands.Count
                || s.Commitments.Any(c => c == null || c.Orders == null || !s.Ships.Any(p => p.Owner == c.Actor && p.Hull > 0))
                || s.Commitments.Select(c => c.Actor).Distinct(StringComparer.Ordinal).Count() != s.Commitments.Count)
                throw new ArgumentException("Invalid accepted commitment.");
            foreach (var c in s.Commitments)
            {
                var fleets = s.Ships.Where(p => p.Owner == c.Actor && p.Hull > 0).Select(p => p.Fleet).Distinct(StringComparer.Ordinal).ToList();
                if (s.NextRound > 3 || c.Orders.Count != fleets.Count || c.Orders.Select(o => o?.Fleet).Distinct(StringComparer.Ordinal).Count() != fleets.Count
                    || c.Orders.Any(o => o == null || !fleets.Contains(o.Fleet, StringComparer.Ordinal) || !Enum.IsDefined(typeof(PrototypeManeuver), o.Maneuver)
                        || o.FocusFleet != null && o.FocusFleet != s.StandingOrders.Single(p => p.Fleet == o.Fleet).FocusFleet
                            && !s.Ships.Any(p => p.Fleet == o.FocusFleet && p.Owner != c.Actor && p.Hull > 0 && !p.Hidden)))
                    throw new ArgumentException("Invalid loaded commitment.");
            }
            if (s.Reports.Any(e => e == null || e.Round < 1 || e.Round >= s.NextRound || !s.Ships.Any(p => p.Owner == e.Audience) || string.IsNullOrWhiteSpace(e.Message)))
                throw new ArgumentException("Invalid report.");
        }
        internal static PrototypeFleetOrder Copy(PrototypeFleetOrder o) => new PrototypeFleetOrder { Fleet = o.Fleet, Maneuver = o.Maneuver, FocusFleet = o.FocusFleet };
        public static PrototypeShip Copy(PrototypeShip p) => new PrototypeShip { Id = p.Id, Fleet = p.Fleet, Owner = p.Owner, Type = p.Type, Fit = p.Fit,
            FitRevision = p.FitRevision, Hull = p.Hull, MaxHull = p.MaxHull, Shields = (long[])p.Shields.Clone(),
            ShieldMaximum = (long[])p.ShieldMaximum.Clone(), Ammunition = p.Ammunition, MagazineCapacity = p.MagazineCapacity, CloakCapable = p.CloakCapable,
            Hidden = p.Hidden, Opportunities = p.Opportunities, DefenseOpportunities = p.DefenseOpportunities, TorpedoDamage = p.TorpedoDamage,
            EffectivePower = p.EffectivePower, BaseAllocation = p.BaseAllocation, CloakRequirement = p.CloakRequirement };
        private static PrototypeCheckpoint Copy(PrototypeCheckpoint s) => new PrototypeCheckpoint
        {
            Identity = s.Identity, Encounter = s.Encounter, NextRound = s.NextRound, Revision = s.Revision, RollCursor = s.RollCursor,
            FixtureRolls = (int[])s.FixtureRolls.Clone(), InterceptionChanceBps = s.InterceptionChanceBps, TargetRanking = s.TargetRanking,
            Ships = s.Ships.OrderBy(p => p.Id, StringComparer.Ordinal).Select(Copy).ToList(),
            StandingOrders = s.StandingOrders.OrderBy(o => o.Fleet, StringComparer.Ordinal).Select(Copy).ToList(),
            Commitments = s.Commitments.OrderBy(c => c.Actor, StringComparer.Ordinal).Select(c => new PrototypeCommitment { Actor = c.Actor, Orders = c.Orders.OrderBy(o => o.Fleet, StringComparer.Ordinal).Select(Copy).ToList() }).ToList(),
            AcceptedCommands = s.AcceptedCommands.OrderBy(c => c, StringComparer.Ordinal).ToList(),
            Reports = s.Reports.Select(e => new PrototypeObservedEvent { Round = e.Round, Audience = e.Audience, Message = e.Message }).ToList()
        };
    }
}
