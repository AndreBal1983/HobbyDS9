using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public static class TacticalCombatV2State
    {
        public static void Validate(TacticalEncounterV2Dto state)
        {
            Require(state != null, "Missing encounter.");
            Require(state.FormatVersion == TacticalCombatV2Identity.FormatVersion && state.ResolverId == TacticalCombatV2Identity.ResolverId
                && state.ResolverVersion == TacticalCombatV2Identity.ResolverVersion && state.RulesId == TacticalCombatV2Identity.RulesId
                && state.RandomAlgorithmId == TacticalCombatV2Identity.RandomAlgorithmId, "Unknown v2 contract identity.");
            Id(state.EncounterId);
            Require(state.NextRound >= 1 && state.NextRound <= 4 && Known(state.Completion), "Invalid round/completion.");
            Require(state.Rules != null, "Missing frozen rules.");
            var r = state.Rules;
            Require(new[] { r.SpeedWeight, r.AgilityWeight, r.SensorsWeight, r.CommandWeight, r.CohesionWeight }.All(v => v >= 0 && v <= 100)
                && r.SpeedWeight + r.AgilityWeight + r.SensorsWeight + r.CommandWeight + r.CohesionWeight > 0
                && r.ManeuverTieMargin >= 0 && r.ManeuverTieMargin <= 100000, "Invalid maneuver tuning.");
            Require(r.ArmorNumerator >= 0 && r.ArmorNumerator <= 1000000 && r.ArmorDenominator > 0 && r.ArmorDenominator <= 1000000, "Invalid armor tuning.");
            Require(new[] { r.DistantTorpedoHitPenaltyBps, r.DistantInterceptionBonusBps, r.InterceptionBaseBps,
                r.ScreenDefenseBps, r.WithdrawalDefenseBps, r.HoldDefenseBps, r.ChargeDefenseBps }.All(Bps), "Invalid defense tuning.");
            Require(state.Formations != null && state.Geometry != null && state.Formations.Count >= 2, "Missing formations/geometry.");
            var formations = new Dictionary<string, TacticalFormationV2Dto>(StringComparer.Ordinal);
            var ships = new HashSet<string>(StringComparer.Ordinal);
            foreach (var f in state.Formations)
            {
                Require(f != null, "Missing formation."); Id(f.FormationId); Id(f.SideId);
                Require(formations.TryAdd(f.FormationId, f), "Duplicate formation.");
                Require(f.StandingOrder != null, "Missing standing order.");
                Require(new[] { f.Speed, f.Agility, f.Sensors, f.Command, f.Cohesion }.All(v => v >= 0 && v <= 100), "Formation rating outside 0..100.");
                Require(Known(f.Status) && Known(f.Objective) && f.ObjectiveRequiredRounds >= 1 && f.ObjectiveRequiredRounds <= 3
                    && f.ObjectiveProgress >= 0 && f.ObjectiveProgress < state.NextRound && f.ObjectiveProgress <= f.ObjectiveRequiredRounds
                    && f.ObjectiveCompleted == (f.ObjectiveProgress == f.ObjectiveRequiredRounds), "Invalid objective state.");
                Require(f.Ships != null && f.Ships.Count > 0, "Empty formation.");
                Require(f.Objective != TacticalObjective.Defeat || (f.ObjectiveProgress == 0 && !f.ObjectiveCompleted), "Defeat is a decisive outcome, not a timed goal.");
                foreach (var s in f.Ships)
                {
                    Require(s != null, "Missing ship."); Id(s.ShipId); Require(ships.Add(s.ShipId), "Duplicate ship.");
                    Require(Known(s.Role) && s.MaxHull > 0 && s.Hull >= 0 && s.Hull <= s.MaxHull && s.Armor >= 0 && Bps(s.EvasionBps), "Invalid hull/armor/evasion.");
                    Require(s.PowerBudget >= 0 && s.ShieldPowerRequired >= 0 && s.ShieldPowerAllocated >= 0
                        && s.ShieldPowerAllocated <= s.ShieldPowerRequired && s.ShieldRechargePerRound >= 0, "Invalid shield power/recharge.");
                    Require(s.ShieldRechargePerRound == 0 || s.ShieldPowerRequired > 0, "Positive shield recharge requires a positive power requirement.");
                    Require(s.Shields != null && s.Shields.Count == 4 && s.Mounts != null && s.Weapons != null, "Missing shield/mount/weapon data.");
                    var sectors = new HashSet<TacticalSector>();
                    foreach (var q in s.Shields)
                    {
                        Require(q != null && Known(q.Sector) && sectors.Add(q.Sector) && q.Maximum >= 0 && q.Current >= 0 && q.Current <= q.Maximum, "Invalid or duplicate shield sector.");
                    }
                    var mounts = new Dictionary<string, TacticalMountV2Dto>(StringComparer.Ordinal);
                    foreach (var m in s.Mounts)
                    {
                        Require(m != null, "Missing mount."); Id(m.MountId);
                        Require(mounts.TryAdd(m.MountId, m) && Known(m.WeaponClass) && Arc(m.Coverage) && m.Slots > 0, "Invalid/duplicate mount.");
                    }
                    var weapons = new HashSet<string>(StringComparer.Ordinal);
                    BigInteger power = s.ShieldPowerAllocated;
                    foreach (var w in s.Weapons)
                    {
                        Require(w != null, "Missing weapon."); Id(w.WeaponId); Id(w.MountId);
                        Require(weapons.Add(w.WeaponId) && Known(w.Class) && Arc(w.Coverage) && mounts.TryGetValue(w.MountId, out var m)
                            && m.WeaponClass == w.Class && (w.Coverage & m.Coverage) == w.Coverage, "Invalid equipment/mount coverage.");
                        Require(w.Damage >= 0 && w.RequiredPower > 0 && w.AllocatedPower >= 0 && w.AllocatedPower <= w.RequiredPower
                            && (w.Equipped || w.AllocatedPower == 0) && Bps(w.AccuracyBps) && Bps(w.TrackingBps)
                            && w.OpportunitiesPerRound > 0 && w.OpportunitiesPerRound <= 64 && w.CadenceRounds >= 1 && w.CadenceRounds <= 3
                            && w.NextFireRound >= 1 && w.NextFireRound <= state.NextRound + w.CadenceRounds - 1
                            && w.MagazineCapacity >= 0 && w.Ammunition >= 0 && w.Ammunition <= w.MagazineCapacity
                            && (w.Class == TacticalWeaponClass.Torpedo || (w.MagazineCapacity == 0 && w.Ammunition == 0)), "Invalid weapon statistics/cadence/ammunition.");
                        power += w.AllocatedPower;
                    }
                    Require(power <= s.PowerBudget, "Combat profile exceeds available power.");
                    foreach (var m in s.Mounts)
                        Require(s.Weapons.Count(w => w.Equipped && w.MountId == m.MountId) <= m.Slots, "Hull mount slots exceeded.");
                }
                var destroyed = f.Ships.All(s => s.Hull == 0);
                Require((f.Status == TacticalFormationStatus.Destroyed) == destroyed, "Hull and formation status disagree.");
                Require(state.NextRound != 1 || f.Status == TacticalFormationStatus.Active, "An initial encounter cannot contain exited formations.");
                Require(f.Status != TacticalFormationStatus.BrokenThrough || (f.Objective == TacticalObjective.Breakthrough && f.ObjectiveCompleted), "Invalid breakthrough.");
                Require(f.Status != TacticalFormationStatus.Retreated || f.StandingOrder.Maneuver == TacticalManeuver.Disengage, "Retreat contradicts standing order.");
            }
            Require(formations.Values.Select(f => f.SideId).Distinct(StringComparer.Ordinal).Count() >= 2, "No opposing sides.");
            foreach (var f in state.Formations) ValidateOrder(f.StandingOrder, f, formations);
            var pairs = new HashSet<(string, string)>();
            foreach (var g in state.Geometry)
            {
                Require(g != null && StringComparer.Ordinal.Compare(g.FormationA, g.FormationB) < 0
                    && formations.TryGetValue(g.FormationA, out var a) && formations.TryGetValue(g.FormationB, out var b)
                    && a.SideId != b.SideId && pairs.Add((g.FormationA, g.FormationB))
                    && Known(g.Range) && Known(g.FacingA) && Known(g.FacingB), "Invalid/duplicate formation geometry.");
            }
            foreach (var a in state.Formations) foreach (var b in state.Formations)
                if (a.SideId != b.SideId && StringComparer.Ordinal.Compare(a.FormationId, b.FormationId) < 0)
                    Require(pairs.Contains((a.FormationId, b.FormationId)), "Missing opposing formation geometry.");
            var activeSides = state.Formations.Where(f => f.Status == TacticalFormationStatus.Active).Select(f => f.SideId).Distinct(StringComparer.Ordinal).Count();
            var achieved = state.Formations.Any(f => f.ObjectiveCompleted);
            Require(state.Completion != TacticalCompletion.None || (state.NextRound <= 3 && activeSides >= 2 && !achieved), "Active encounter is already terminal.");
            Require(state.Completion == TacticalCompletion.None || state.NextRound >= 2, "Unresolved terminal encounter.");
            Require(state.Completion != TacticalCompletion.RoundLimit || (state.NextRound == 4 && activeSides >= 2 && !achieved), "Invalid round limit.");
            Require(state.Completion != TacticalCompletion.Decisive || (activeSides < 2 && !achieved), "Invalid decisive completion.");
            Require(state.Completion != TacticalCompletion.Objective || achieved, "Invalid objective completion.");
        }
        internal static void ValidateOrder(TacticalOrderV2Dto order, TacticalFormationV2Dto f, IReadOnlyDictionary<string, TacticalFormationV2Dto> formations)
        {
            Require(order != null && order.FormationId == f.FormationId && Known(order.Maneuver) && Known(order.TargetPriority) && Known(order.PreferredRange), "Invalid formation order.");
            Require(order.TargetFormationId == null || (formations.TryGetValue(order.TargetFormationId, out var target) && target.SideId != f.SideId), "Illegal enemy target.");
            Require(order.ProtectedFormationId == null || (order.Maneuver == TacticalManeuver.Screen && order.ProtectedFormationId != f.FormationId
                && formations.TryGetValue(order.ProtectedFormationId, out var ally) && ally.SideId == f.SideId), "Illegal screening target.");
        }
        internal static bool Known<T>(T value) where T : struct => Enum.IsDefined(typeof(T), value);
        internal static bool Bps(int value) => value >= 0 && value <= 10000;
        private static bool Arc(TacticalArc value) => value != TacticalArc.None && (value & ~TacticalArc.All) == 0;
        private static void Id(string id) => Require(EntityId.TryParse(id, out _), "Invalid canonical v2 ID.");
        internal static void Require([DoesNotReturnIf(false)] bool condition, string message)
        { if (!condition) throw new ArgumentException(message); }

        public static TacticalEncounterV2Dto Copy(TacticalEncounterV2Dto source)
        {
            Validate(source);
            var r = source.Rules;
            return new TacticalEncounterV2Dto
            {
                FormatVersion = source.FormatVersion, ResolverId = source.ResolverId, ResolverVersion = source.ResolverVersion,
                RulesId = source.RulesId, RandomAlgorithmId = source.RandomAlgorithmId, EncounterId = source.EncounterId,
                RandomState = source.RandomState, NextRound = source.NextRound, Completion = source.Completion,
                Rules = new TacticalRulesV2Dto { SpeedWeight = r.SpeedWeight, AgilityWeight = r.AgilityWeight, SensorsWeight = r.SensorsWeight,
                    CommandWeight = r.CommandWeight, CohesionWeight = r.CohesionWeight, ManeuverTieMargin = r.ManeuverTieMargin,
                    ArmorNumerator = r.ArmorNumerator, ArmorDenominator = r.ArmorDenominator, DistantTorpedoHitPenaltyBps = r.DistantTorpedoHitPenaltyBps,
                    DistantInterceptionBonusBps = r.DistantInterceptionBonusBps, InterceptionBaseBps = r.InterceptionBaseBps,
                    ScreenDefenseBps = r.ScreenDefenseBps, WithdrawalDefenseBps = r.WithdrawalDefenseBps, HoldDefenseBps = r.HoldDefenseBps,
                    ChargeDefenseBps = r.ChargeDefenseBps, SuppressRechargeOnHitSector = r.SuppressRechargeOnHitSector },
                Formations = source.Formations.OrderBy(f => f.FormationId, StringComparer.Ordinal).Select(f => new TacticalFormationV2Dto
                {
                    FormationId = f.FormationId, SideId = f.SideId, Speed = f.Speed, Agility = f.Agility, Sensors = f.Sensors,
                    Command = f.Command, Cohesion = f.Cohesion, Status = f.Status, StandingOrder = CopyOrder(f.StandingOrder),
                    Objective = f.Objective, ObjectiveRequiredRounds = f.ObjectiveRequiredRounds, ObjectiveProgress = f.ObjectiveProgress, ObjectiveCompleted = f.ObjectiveCompleted,
                    Ships = f.Ships.OrderBy(s => s.ShipId, StringComparer.Ordinal).Select(s => new TacticalShipV2Dto
                    {
                        ShipId = s.ShipId, Role = s.Role, Hull = s.Hull, MaxHull = s.MaxHull, Armor = s.Armor, EvasionBps = s.EvasionBps,
                        PowerBudget = s.PowerBudget, ShieldPowerRequired = s.ShieldPowerRequired, ShieldPowerAllocated = s.ShieldPowerAllocated, ShieldRechargePerRound = s.ShieldRechargePerRound,
                        Shields = s.Shields.OrderBy(q => q.Sector).Select(q => new TacticalShieldV2Dto { Sector = q.Sector, Maximum = q.Maximum, Current = q.Current }).ToList(),
                        Mounts = s.Mounts.OrderBy(m => m.MountId, StringComparer.Ordinal).Select(m => new TacticalMountV2Dto { MountId = m.MountId, WeaponClass = m.WeaponClass, Coverage = m.Coverage, Slots = m.Slots }).ToList(),
                        Weapons = s.Weapons.OrderBy(w => w.WeaponId, StringComparer.Ordinal).Select(w => new TacticalWeaponV2Dto
                        { WeaponId = w.WeaponId, MountId = w.MountId, Class = w.Class, Coverage = w.Coverage, Equipped = w.Equipped, RequiredPower = w.RequiredPower, AllocatedPower = w.AllocatedPower,
                            Damage = w.Damage, AccuracyBps = w.AccuracyBps, TrackingBps = w.TrackingBps, OpportunitiesPerRound = w.OpportunitiesPerRound,
                            CadenceRounds = w.CadenceRounds, NextFireRound = w.NextFireRound, MagazineCapacity = w.MagazineCapacity, Ammunition = w.Ammunition }).ToList()
                    }).ToList()
                }).ToList(),
                Geometry = source.Geometry.OrderBy(g => g.FormationA, StringComparer.Ordinal).ThenBy(g => g.FormationB, StringComparer.Ordinal)
                    .Select(g => new TacticalGeometryV2Dto { FormationA = g.FormationA, FormationB = g.FormationB, Range = g.Range, FacingA = g.FacingA, FacingB = g.FacingB }).ToList()
            };
        }
        internal static TacticalOrderV2Dto CopyOrder(TacticalOrderV2Dto order) => new TacticalOrderV2Dto
        { FormationId = order.FormationId, Maneuver = order.Maneuver, TargetPriority = order.TargetPriority, PreferredRange = order.PreferredRange,
            TargetFormationId = order.TargetFormationId, ProtectedFormationId = order.ProtectedFormationId };
    }
}
