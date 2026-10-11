using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Owns ephemeral battle state only. Campaign consequences are applied elsewhere.
    public sealed class DeterministicPhasedCombatResolver : IPlannedCombatResolver
    {
        public const string ResolverId = "combat-resolver.phased";
        public const int ResolverVersion = 1;
        public CombatResultDto Resolve(CombatRequestDto request)
        {
            ValidateRequest(request);
            var plans = request.Participants.Select(p => new FleetCombatPlanDto
            {
                FleetId = p.FleetId, LongRangeTactic = CombatTactic.FocusFire,
                MediumRangeTactic = CombatTactic.Standard, CloseRangeTactic = CombatTactic.Overcharge,
                TargetPriority = CombatTargetPriority.LowestHullRatio
            }).ToList();
            return Resolve(request, plans);
        }
        public CombatResultDto Resolve(CombatRequestDto request, IReadOnlyList<FleetCombatPlanDto> plans)
        {
            ValidateRequest(request); CombatPlanValidation.Validate(request, plans);
            var frozen = CombatEncounterScaffold.CopyRequest(request);
            var fleets = frozen.Participants.Select(p => new BattleFleet(p, plans.Single(plan => plan.FleetId == p.FleetId))).OrderBy(f => f.Id, StringComparer.Ordinal).ToList();
            var ships = fleets.SelectMany(f => f.Ships).OrderBy(s => s.Frozen.ShipId, StringComparer.Ordinal).ToList();
            var random = new EncounterRandomStream(frozen.RandomState);
            var result = new CombatResultDto { EncounterId = frozen.EncounterId, ResolverId = ResolverId, ResolverVersion = ResolverVersion, Status = CombatResolutionStatus.Resolved };
            foreach (var phase in new[] { CombatPhaseKind.LongRange, CombatPhaseKind.MediumRange, CombatPhaseKind.CloseRange })
            {
                var active = ships.Where(s => s.Hull > 0 && !s.Fleet.Retreated).ToList();
                if (active.Select(s => s.Fleet.CivilizationId).Distinct(StringComparer.Ordinal).Count() < 2) break;
                Emit(result, new CombatEventDto { Kind = CombatEventKind.PhaseStarted, Phase = phase });
                var incoming = RollVolley(active, phase, random, result);
                ApplyVolley(active, incoming, phase, result);
                Retreat(fleets, phase, result);
            }
            result.ShipChanges = ships.Where(s => s.Hull != s.Frozen.Hull).Select(s => new ShipCombatChangeDto { ShipId = s.Frozen.ShipId, RemainingHull = s.Hull }).ToList();
            foreach (var fleet in fleets)
            {
                if (fleet.Ships.All(s => s.Hull == 0)) result.FleetChanges.Add(new FleetCombatChangeDto { FleetId = fleet.Id, Disposition = FleetCombatDisposition.Destroyed });
                else if (fleet.Retreated) result.FleetChanges.Add(new FleetCombatChangeDto { FleetId = fleet.Id, Disposition = FleetCombatDisposition.Retreated, RetreatNodeId = fleet.RetreatNodeId });
            }
            result.FinalRandomState = random.State;
            Emit(result, new CombatEventDto { Kind = CombatEventKind.EncounterResolved });
            return result;
        }
        private static void ValidateRequest(CombatRequestDto request)
        {
            CombatEncounterScaffold.ValidateRequest(request, allocated: true);
            if (request.ResolverId != ResolverId || request.ResolverVersion != ResolverVersion || request.TurnNumber <= 0)
                throw new ArgumentException("Phased resolver identity/turn mismatch.", nameof(request));
        }
        private static Dictionary<string, BigInteger> RollVolley(List<BattleShip> active, CombatPhaseKind phase, EncounterRandomStream random, CombatResultDto result)
        {
            var incoming = new Dictionary<string, BigInteger>(StringComparer.Ordinal);
            // active was ordered by ShipId. No hull or shields change until every intent is rolled.
            foreach (var attacker in active)
            {
                var raw = Attack(attacker.Frozen.CombatProfile!, phase);
                if (raw == 0) continue;
                var target = SelectTarget(attacker, active, phase);
                if (target == null) continue;
                var offense = Modifiers(attacker.Fleet.Tactic(phase)); var defense = Modifiers(target.Fleet.Tactic(phase));
                var chance = Clamp(Clamp(attacker.Frozen.CombatProfile!.AccuracyBps + offense.accuracy) - Clamp(target.Frozen.CombatProfile!.EvasionBps + defense.evasion));
                var hit = random.NextBelow(10000) < (ulong)chance;
                Emit(result, new CombatEventDto { Kind = hit ? CombatEventKind.AttackHit : CombatEventKind.AttackMissed, Phase = phase,
                    SourceShipId = attacker.Frozen.ShipId, SourceFleetId = attacker.Fleet.Id, ShipId = target.Frozen.ShipId, FleetId = target.Fleet.Id });
                if (!hit) continue;
                var damage = BigInteger.Max(BigInteger.One, (BigInteger)raw * offense.damage / 10000);
                incoming.TryGetValue(target.Frozen.ShipId, out var total); incoming[target.Frozen.ShipId] = total + damage;
            }
            return incoming;
        }
        private static BattleShip? SelectTarget(BattleShip attacker, List<BattleShip> active, CombatPhaseKind phase)
        {
            BattleShip? best = null;
            foreach (var candidate in active)
            {
                if (candidate.Fleet.CivilizationId == attacker.Fleet.CivilizationId) continue;
                if (best == null || CompareTargets(candidate, best, attacker.Fleet.Priority, phase) < 0) best = candidate;
            }
            return best;
        }
        private static int CompareTargets(BattleShip a, BattleShip b, CombatTargetPriority priority, CombatPhaseKind phase)
        {
            var comparison = priority == CombatTargetPriority.LowestHullRatio
                ? ((BigInteger)a.Hull * b.Frozen.MaxHull).CompareTo((BigInteger)b.Hull * a.Frozen.MaxHull)
                : priority == CombatTargetPriority.HighestPhaseAttack ? Attack(b.Frozen.CombatProfile!, phase).CompareTo(Attack(a.Frozen.CombatProfile!, phase)) : 0;
            return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(a.Frozen.ShipId, b.Frozen.ShipId);
        }
        private static void ApplyVolley(List<BattleShip> active, Dictionary<string, BigInteger> incoming, CombatPhaseKind phase, CombatResultDto result)
        {
            var shieldEvents = new List<CombatEventDto>(); var hullEvents = new List<CombatEventDto>(); var destructionEvents = new List<CombatEventDto>();
            foreach (var target in active)
            {
                if (!incoming.TryGetValue(target.Frozen.ShipId, out var damage)) continue;
                // Aggregate damage can exceed Int64. Only actual shield/hull loss is converted,
                // each bounded by an independently valid Int64 capacity. Overkill is discarded.
                var shields = (long)BigInteger.Min(damage, target.Shields); target.Shields = checked(target.Shields - shields); damage -= shields;
                var hull = (long)BigInteger.Min(damage, target.Hull); target.Hull = checked(target.Hull - hull);
                CombatEventDto Impact(CombatEventKind kind, long? amount = null) => new CombatEventDto
                { Kind = kind, Phase = phase, ShipId = target.Frozen.ShipId, FleetId = target.Fleet.Id, Amount = amount };
                if (shields > 0) shieldEvents.Add(Impact(CombatEventKind.ShieldDamaged, shields));
                if (hull > 0) hullEvents.Add(Impact(CombatEventKind.ShipDamaged, hull));
                if (target.Hull == 0) destructionEvents.Add(Impact(CombatEventKind.ShipDestroyed));
            }
            foreach (var e in shieldEvents.Concat(hullEvents).Concat(destructionEvents)) Emit(result, e);
        }
        private static void Retreat(List<BattleFleet> fleets, CombatPhaseKind phase, CombatResultDto result)
        {
            foreach (var fleet in fleets)
            {
                if (fleet.Retreated || fleet.Threshold == 0 || fleet.Ships.All(s => s.Hull == 0)) continue;
                var remaining = fleet.Ships.Aggregate(BigInteger.Zero, (sum, s) => sum + s.Hull);
                var initialMaximum = fleet.Ships.Aggregate(BigInteger.Zero, (sum, s) => sum + s.Frozen.MaxHull);
                if (remaining * 100 > initialMaximum * fleet.Threshold) continue;
                fleet.Retreated = true;
                Emit(result, new CombatEventDto { Kind = CombatEventKind.FleetRetreated, Phase = phase, FleetId = fleet.Id });
            }
        }
        private static void Emit(CombatResultDto result, CombatEventDto e) { e.Sequence = result.Events.Count; result.Events.Add(e); }
        private static int Clamp(int value) => Math.Max(0, Math.Min(10000, value));
        private static long Attack(ShipCombatProfileDto p, CombatPhaseKind phase) => phase == CombatPhaseKind.LongRange ? p.LongRangeAttack : phase == CombatPhaseKind.MediumRange ? p.MediumRangeAttack : p.CloseRangeAttack;
        private static (int damage, int accuracy, int evasion) Modifiers(CombatTactic tactic)
        {
            switch (tactic)
            {
                case CombatTactic.Standard: return (10000, 0, 0);
                case CombatTactic.FocusFire: return (10000, 1500, -1000);
                case CombatTactic.Evasive: return (7500, -500, 2000);
                case CombatTactic.Overcharge: return (12500, -1000, -1500);
                default: throw new ArgumentOutOfRangeException(nameof(tactic));
            }
        }
        private sealed class BattleFleet
        {
            internal readonly string Id;
            internal readonly string CivilizationId;
            internal readonly CombatTargetPriority Priority;
            internal readonly int Threshold;
            internal readonly string? RetreatNodeId;
            private readonly CombatTactic longRange, mediumRange, closeRange;
            internal readonly List<BattleShip> Ships;
            internal bool Retreated;
            internal BattleFleet(CombatParticipantDto participant, FleetCombatPlanDto plan)
            {
                Id = participant.FleetId; CivilizationId = participant.CivilizationId; Priority = plan.TargetPriority;
                Threshold = plan.RetreatHullPercent; RetreatNodeId = plan.RetreatNodeId;
                longRange = plan.LongRangeTactic; mediumRange = plan.MediumRangeTactic; closeRange = plan.CloseRangeTactic;
                Ships = participant.Ships.Select(s => new BattleShip(s, this)).ToList();
            }
            internal CombatTactic Tactic(CombatPhaseKind phase) => phase == CombatPhaseKind.LongRange ? longRange : phase == CombatPhaseKind.MediumRange ? mediumRange : closeRange;
        }
        private sealed class BattleShip
        {
            internal readonly CombatShipDto Frozen;
            internal readonly BattleFleet Fleet;
            internal long Hull;
            internal long Shields;
            internal BattleShip(CombatShipDto ship, BattleFleet fleet) { Frozen = ship; Fleet = fleet; Hull = ship.Hull; Shields = ship.CombatProfile!.ShieldCapacity; }
        }
    }
}
