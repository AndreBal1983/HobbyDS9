using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.TacticalCombatV2Fixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class TacticalCombatV2Tests
{
    [Theory]
    [InlineData(TacticalManeuver.MaintainDistance)] [InlineData(TacticalManeuver.Charge)] [InlineData(TacticalManeuver.Flank)]
    [InlineData(TacticalManeuver.Screen)] [InlineData(TacticalManeuver.Breakthrough)] [InlineData(TacticalManeuver.Pursuit)]
    [InlineData(TacticalManeuver.FightingWithdrawal)] [InlineData(TacticalManeuver.Disengage)]
    public void EveryIntentIsLegalPersistsAndStopsWithinThreeRounds(TacticalManeuver maneuver)
    {
        var state = Encounter(); state.Formations[0].StandingOrder = Order(state, 0, maneuver);
        var results = new DeterministicTacticalCombatV2Resolver().ResolveAutomatic(state);
        Assert.InRange(results.Count, 1, 3);
        Assert.All(results, r => Assert.Equal(maneuver, r.NextState.Formations[0].StandingOrder.Maneuver));
        Assert.NotEqual(TacticalCompletion.None, results[^1].NextState.Completion);
        Assert.Throws<ArgumentException>(() => Round(results[^1].NextState));
    }
    [Theory]
    [InlineData(TacticalRange.Distant)] [InlineData(TacticalRange.Engagement)] [InlineData(TacticalRange.Close)] [InlineData(TacticalRange.Intermixed)]
    public void RangeClosesAndReopensAtAllBoundaries(TacticalRange initial)
    {
        var s = Encounter(); Unarmed(s); s.Geometry[0].Range = initial;
        var close = Round(s, Order(s, 0, TacticalManeuver.Charge), Order(s, 1, TacticalManeuver.Charge)).NextState;
        Assert.Equal((TacticalRange)Math.Min(3, (int)initial + 1), close.Geometry[0].Range);
        var open = Round(close, Order(close, 0, TacticalManeuver.FightingWithdrawal), Order(close, 1, TacticalManeuver.FightingWithdrawal)).NextState;
        Assert.Equal((TacticalRange)Math.Max(0, Math.Min(3, (int)initial + 1) - 1), open.Geometry[0].Range);
    }
    [Fact]
    public void SuperiorFlankExposesPortAndDeniesEnemyForwardCannon()
    {
        var s = Encounter(); Unarmed(s); s.Geometry[0].Range = TacticalRange.Close;
        s.Formations[0].Agility = 100;
        Weapon(Ship(s), TacticalWeaponClass.Cannon, 20, arc: TacticalArc.Fore);
        Weapon(Ship(s, 1), TacticalWeaponClass.Cannon, 30, arc: TacticalArc.Fore);
        var r = Round(s, Order(s, 0, TacticalManeuver.Flank, TacticalRange.Close), Order(s, 1, TacticalManeuver.MaintainDistance, TacticalRange.Close));
        Assert.Equal(TacticalSector.Port, r.NextState.Geometry[0].FacingB);
        var shot = Assert.Single(r.Events, e => e.Kind == TacticalEventKind.WeaponDischarged);
        Assert.Equal(Ship(s).ShipId, shot.ShipId); Assert.Equal(TacticalSector.Port, shot.Sector);
        Assert.Equal(30, Ship(r.NextState, 1).Shields.Single(q => q.Sector == TacticalSector.Port).Current);
        Assert.Equal(1000, Ship(r.NextState, 1).Hull);
    }
    [Theory]
    [InlineData(0, TacticalSector.Fore)] [InlineData(1, TacticalSector.Port)] [InlineData(2, TacticalSector.Starboard)] [InlineData(3, TacticalSector.Aft)]
    public void AspectSelectsFacingWithIsolatedShieldOverflow(int scenario, TacticalSector expected)
    {
        var s = Encounter(); Unarmed(s); Weapon(Ship(s), TacticalWeaponClass.Beam, 70);
        var a = Order(s, 0, TacticalManeuver.MaintainDistance);
        var b = Order(s, 1, TacticalManeuver.MaintainDistance);
        if (scenario == 1) { s.Formations[0].Agility = 100; a.Maneuver = TacticalManeuver.Flank; }
        if (scenario == 2) { s.Formations[1].Agility = 100; b.Maneuver = TacticalManeuver.Flank; }
        if (scenario == 3) { b.Maneuver = TacticalManeuver.FightingWithdrawal; s.Formations[0].Speed = 100; a.Maneuver = TacticalManeuver.Pursuit; }
        var r = Round(s, a, b); var target = scenario == 2 ? Ship(r.NextState) : Ship(r.NextState, 1);
        if (scenario == 2)
        {
            // The attacker is outflanked: its own exposed facing is Starboard.
            s = Encounter(); Unarmed(s); Weapon(Ship(s, 1), TacticalWeaponClass.Beam, 70);
            s.Formations[1].Agility = 100;
            r = Round(s, Order(s, 1, TacticalManeuver.Flank)); target = Ship(r.NextState);
        }
        Assert.Equal(980, target.Hull);
        Assert.Equal(0, target.Shields.Single(q => q.Sector == expected).Current);
        Assert.All(target.Shields.Where(q => q.Sector != expected), q => Assert.Equal(50, q.Current));
        Assert.All(target.Shields, q => Assert.Equal(50, q.Maximum));
    }
    [Theory]
    [InlineData(1)] [InlineData(49)] [InlineData(50)]
    public void NoWeaponBypassesActiveShield(long damage)
    {
        var s = Encounter(); Unarmed(s); Weapon(Ship(s), TacticalWeaponClass.Torpedo, damage);
        var r = Round(s); Assert.Equal(1000, Ship(r.NextState, 1).Hull);
        Assert.Equal(50 - damage, Ship(r.NextState, 1).Shields[0].Current);
        Assert.DoesNotContain(r.Events, e => e.Kind == TacticalEventKind.HullDamage);
    }
    [Fact]
    public void SharedRechargeCapsSuppressesHitSectorAndNeverOccursAfterRoundThree()
    {
        var s = Encounter(); Unarmed(s); Weapon(Ship(s), TacticalWeaponClass.Beam, 5);
        Ship(s, 1).ShieldRechargePerRound = 6;
        foreach (var q in Ship(s, 1).Shields) q.Current = 40;
        var first = Round(s); var b = Ship(first.NextState, 1);
        Assert.Equal(new long[] { 35, 46, 40, 40 }, b.Shields.Select(q => q.Current));
        Assert.Equal(6, first.Events.Where(e => e.Kind == TacticalEventKind.ShieldRecharged).Sum(e => e.Amount));
        var second = Round(first.NextState); var third = Round(second.NextState);
        Assert.Equal(new long[] { 25, 50, 42, 40 }, Ship(third.NextState, 1).Shields.Select(q => q.Current));
        Assert.DoesNotContain(third.Events, e => e.Kind == TacticalEventKind.ShieldRecharged);
        Assert.Equal(TacticalCompletion.RoundLimit, third.NextState.Completion);
    }
    [Fact]
    public void RechargeIsPoweredAndCannotRetroactivelyStopHullOverflow()
    {
        var s = Encounter(); Unarmed(s); Weapon(Ship(s), TacticalWeaponClass.Beam, 70);
        Ship(s, 1).ShieldRechargePerRound = long.MaxValue;
        s.Rules.SuppressRechargeOnHitSector = false;
        var r = Round(s); Assert.Equal(980, Ship(r.NextState, 1).Hull); Assert.Equal(50, Ship(r.NextState, 1).Shields[0].Current);
        Ship(s, 1).ShieldPowerAllocated = 0; r = Round(s);
        Assert.DoesNotContain(r.Events, e => e.Kind == TacticalEventKind.ShieldRecharged);
    }
    [Fact]
    public void MagazineAndCadenceSpendOnlyOnLaunchAndNeverRefill()
    {
        var s = Encounter(); Unarmed(s); var w = Weapon(Ship(s), TacticalWeaponClass.Torpedo, 1, 2);
        w.CadenceRounds = 2; w.Ammunition = 3;
        var first = Round(s); Assert.Equal(1, Ship(first.NextState).Weapons[0].Ammunition);
        var second = Round(first.NextState); Assert.DoesNotContain(second.Events, e => e.Kind == TacticalEventKind.WeaponDischarged);
        var third = Round(second.NextState); Assert.Single(third.Events, e => e.Kind == TacticalEventKind.WeaponDischarged);
        Assert.Equal(0, Ship(third.NextState).Weapons[0].Ammunition);
        Assert.Equal(5, Ship(third.NextState).Weapons[0].NextFireRound);
    }
    [Fact]
    public void NoLegalArcMeansNoMagazineOrCadenceConsumption()
    {
        var s = Encounter(); Unarmed(s); Weapon(Ship(s), TacticalWeaponClass.Torpedo, 20, arc: TacticalArc.Fore);
        var r = Round(s, Order(s, 0, TacticalManeuver.FightingWithdrawal));
        Assert.Equal(10, Ship(r.NextState).Weapons[0].Ammunition); Assert.Equal(1, Ship(r.NextState).Weapons[0].NextFireRound);
        Assert.Equal(s.RandomState, r.NextState.RandomState);
    }
    [Theory]
    [InlineData(TacticalManeuver.Screen, 2, 0)] [InlineData(TacticalManeuver.MaintainDistance, 1, 1)]
    [InlineData(TacticalManeuver.Charge, 0, 2)] [InlineData(TacticalManeuver.FightingWithdrawal, 2, 0)]
    public void ReactiveDefenseConsumesRealBeamOpportunityAndCanSaturate(TacticalManeuver intent, int intercepts, int offense)
    {
        var s = Encounter(); Unarmed(s); s.Rules.InterceptionBaseBps = 10000;
        Weapon(Ship(s), TacticalWeaponClass.Torpedo, 20, 3);
        Weapon(Ship(s, 1), TacticalWeaponClass.Beam, 5, 2);
        var r = Round(s, Order(s, 1, intent));
        Assert.Equal(intercepts, r.Events.Count(e => e.Kind == TacticalEventKind.TorpedoIntercepted));
        Assert.Equal(offense, r.Events.Count(e => e.Kind == TacticalEventKind.WeaponDischarged && e.ShipId == Ship(s, 1).ShipId));
        Assert.Equal(7, Ship(r.NextState).Weapons[0].Ammunition);
        Assert.Equal(3 - intercepts, r.Events.Count(e => (e.Kind == TacticalEventKind.AttackHit || e.Kind == TacticalEventKind.AttackMissed) && e.ShipId == Ship(s).ShipId));
    }
    [Fact]
    public void FailedInterceptionStillCostsOffenseAndCanUseCadence()
    {
        var s = Encounter(); Unarmed(s); s.Rules.InterceptionBaseBps = 0; s.Formations[1].Sensors = 0;
        Weapon(Ship(s), TacticalWeaponClass.Torpedo, 1);
        var beam = Weapon(Ship(s, 1), TacticalWeaponClass.Beam, 10); beam.TrackingBps = 0; beam.CadenceRounds = 2;
        var r = Round(s, Order(s, 1, TacticalManeuver.Screen));
        Assert.Single(r.Events, e => e.Kind == TacticalEventKind.InterceptionAttempt);
        Assert.DoesNotContain(r.Events, e => e.Kind == TacticalEventKind.TorpedoIntercepted);
        Assert.DoesNotContain(r.Events, e => e.Kind == TacticalEventKind.WeaponDischarged && e.ShipId == Ship(s, 1).ShipId);
        Assert.Equal(3, Ship(r.NextState, 1).Weapons[0].NextFireRound);
    }
    [Theory]
    [InlineData(TacticalRange.Distant, 0)] [InlineData(TacticalRange.Engagement, 1)] [InlineData(TacticalRange.Close, 1)]
    public void ScreenProtectsOnlyNearbyDesignatedAllies(TacticalRange screenRange, int expected)
    {
        var s = Encounter(3); Unarmed(s); s.Rules.InterceptionBaseBps = 10000;
        Weapon(Ship(s, 1), TacticalWeaponClass.Torpedo, 10);
        Weapon(Ship(s, 2), TacticalWeaponClass.Beam, 5);
        var screen = Order(s, 2, TacticalManeuver.Screen, screenRange); screen.ProtectedFormationId = s.Formations[0].FormationId;
        s.Geometry.Single(g => g.FormationA == s.Formations[1].FormationId).Range = screenRange;
        // A superior lawful flank bypasses the screen's target-access denial, testing interception itself.
        s.Formations[1].Agility = 100;
        var attack = Order(s, 1, TacticalManeuver.Flank, screenRange); attack.TargetFormationId = s.Formations[0].FormationId;
        var r = Round(s, screen, attack);
        Assert.Equal(expected, r.Events.Count(e => e.Kind == TacticalEventKind.TorpedoIntercepted && e.ShipId == Ship(s, 2).ShipId));
    }
    [Theory]
    [InlineData(0, 100)] [InlineData(10, 62)] [InlineData(20, 45)]
    public void ArmorUsesExactRationalFloor(long armor, long expected) => Assert.Equal(new BigInteger(expected), DeterministicTacticalCombatV2Resolver.ArmorDamage(100, armor));
    [Fact]
    public void ArmorIsAppliedOnceToWholeVolleyRatherThanRoundedPerShot()
    {
        var small = Encounter(); Unarmed(small); Shields(Ship(small, 1), 0); Ship(small, 1).Armor = 20;
        Weapon(Ship(small), TacticalWeaponClass.Beam, 1, 10);
        var large = Reload(small); Ship(large).Weapons[0].Damage = 10; Ship(large).Weapons[0].OpportunitiesPerRound = 1;
        Assert.Equal(996, Ship(Round(small).NextState, 1).Hull);
        Assert.Equal(Ship(Round(small).NextState, 1).Hull, Ship(Round(large).NextState, 1).Hull);
    }
    [Fact]
    public void ExtremeDamageAndArmorAreOverflowSafe()
    {
        var damage = (BigInteger)long.MaxValue * 64;
        Assert.Equal(damage * 100 / (100 + (BigInteger)6 * long.MaxValue), DeterministicTacticalCombatV2Resolver.ArmorDamage(damage, long.MaxValue));
        var s = Encounter(); Unarmed(s); Shields(Ship(s, 1), long.MaxValue); Ship(s, 1).Hull = Ship(s, 1).MaxHull = long.MaxValue; Ship(s, 1).Armor = long.MaxValue;
        Weapon(Ship(s), TacticalWeaponClass.Beam, long.MaxValue, 64);
        var r = Round(s); Assert.Equal(long.MaxValue - 1049, Ship(r.NextState, 1).Hull);
        Assert.Contains(r.Events, e => e.Kind == TacticalEventKind.ArmorMitigation && e.Detail.Contains("incoming=581072438321850875841"));
    }
    [Fact]
    public void SimultaneousLethalVolleyFiresBothShipsAndStopsImmediately()
    {
        var s = Encounter(); foreach (var ship in s.Formations.SelectMany(f => f.Ships)) { Shields(ship, 0); ship.Weapons[0].Damage = 1000; }
        var r = Round(s);
        Assert.Equal(2, r.Events.Count(e => e.Kind == TacticalEventKind.WeaponDischarged));
        Assert.All(r.NextState.Formations, f => Assert.Equal(TacticalFormationStatus.Destroyed, f.Status));
        Assert.Equal(TacticalCompletion.Decisive, r.NextState.Completion); Assert.Equal(2, r.NextState.NextRound);
        Assert.Throws<ArgumentException>(() => Round(r.NextState));
        Assert.Equal(Enumerable.Range(0, r.Events.Count), r.Events.Select(e => e.Sequence));
        Assert.True(r.Events.Where(e => e.Kind == TacticalEventKind.AttackHit).Max(e => e.Sequence) < r.Events.Where(e => e.Kind == TacticalEventKind.ShipDestroyed).Min(e => e.Sequence));
    }
    [Fact]
    public void DestroyedShipDoesNotFireOnLaterRound()
    {
        var s = Encounter(2, 2); foreach (var ship in s.Formations.SelectMany(f => f.Ships)) Shields(ship, 0);
        Ship(s).Weapons[0].Damage = 1000;
        var first = Round(s); Assert.Equal(0, Ship(first.NextState, 1).Hull);
        var second = Round(first.NextState); Assert.DoesNotContain(second.Events, e => e.Kind == TacticalEventKind.WeaponDischarged && e.ShipId == Ship(s, 1).ShipId);
    }
    [Fact]
    public void InteractiveLaterOrdersChangeResultsWithoutResolvingFutureRounds()
    {
        var s = Encounter(); Unarmed(s); Weapon(Ship(s), TacticalWeaponClass.Cannon, 100, arc: TacticalArc.Fore);
        var first = Round(s); Assert.Equal(2, first.NextState.NextRound); Assert.All(first.Events, e => Assert.Equal(1, e.Round));
        var hold = Round(first.NextState); var charge = Round(first.NextState, Order(s, 0, TacticalManeuver.Charge));
        Assert.NotEqual(Trace(hold), Trace(charge)); Assert.Equal(900, Ship(charge.NextState, 1).Hull + Ship(charge.NextState, 1).Shields[0].Current - 50);
        var reopen = Round(charge.NextState, Order(s, 0, TacticalManeuver.FightingWithdrawal), Order(s, 1, TacticalManeuver.FightingWithdrawal));
        Assert.DoesNotContain(reopen.Events, e => e.Kind == TacticalEventKind.WeaponDischarged);
    }
    [Fact]
    public void InputAndResultExportsHaveNoMutableAliases()
    {
        var s = Encounter(); var before = Json(s); var order = Order(s, 0, TacticalManeuver.Charge);
        var r = Round(s, order); Assert.Equal(before, Json(s));
        var expected = Trace(r); var export = r.NextState;
        export.Rules.ArmorNumerator = 99; export.Formations[0].StandingOrder.Maneuver = TacticalManeuver.Disengage;
        Ship(export).Weapons[0].Damage = 999; Ship(export).Mounts.Clear(); Ship(export).Shields[0].Current = 0; export.Geometry.Clear();
        Ship(s).Hull = 1; order.Maneuver = TacticalManeuver.Disengage;
        Assert.Equal(expected, Trace(r));
    }
    [Fact]
    public void SaveReloadAtEveryBoundaryAndEventReplayAreExact()
    {
        var s = Encounter(); var resolver = new DeterministicTacticalCombatV2Resolver();
        while (s.Completion == TacticalCompletion.None)
        {
            var expected = resolver.ResolveRound(s); var reloaded = resolver.ResolveRound(Reload(s));
            Assert.Equal(Trace(expected), Trace(reloaded));
            var rng = expected.NextState.RandomState;
            Assert.Equal(Json(expected.Events), Json(expected.Events)); Assert.Equal(rng, expected.NextState.RandomState);
            s = Reload(expected.NextState); TacticalCombatV2State.Validate(s);
        }
        Assert.Equal(4, s.NextRound);
    }
    [Theory]
    [InlineData(TacticalObjective.Hold)] [InlineData(TacticalObjective.Delay)]
    public void BoundedHoldAndDelayCompleteWithSurvivors(TacticalObjective objective)
    {
        var s = Encounter(); Unarmed(s); s.Formations[0].Objective = objective;
        var rounds = new DeterministicTacticalCombatV2Resolver().ResolveAutomatic(s);
        Assert.Equal(2, rounds.Count); Assert.Equal(TacticalCompletion.Objective, rounds[^1].NextState.Completion);
        Assert.All(rounds[^1].NextState.Formations, f => Assert.Equal(TacticalFormationStatus.Active, f.Status));
    }
    [Theory]
    [InlineData(TacticalObjective.Breakthrough, TacticalManeuver.Breakthrough, TacticalRange.Close, TacticalFormationStatus.BrokenThrough)]
    [InlineData(TacticalObjective.Disengage, TacticalManeuver.Disengage, TacticalRange.Engagement, TacticalFormationStatus.Retreated)]
    public void ExitObjectivesNeedWonContestsAndRecordProgress(TacticalObjective objective, TacticalManeuver maneuver, TacticalRange range, TacticalFormationStatus status)
    {
        var s = Encounter(); Unarmed(s); s.Formations[0].Speed = 100; s.Formations[0].Objective = objective;
        s.Formations[0].StandingOrder = Order(s, 0, maneuver); s.Geometry[0].Range = range;
        var first = Round(s); Assert.Equal(1, first.NextState.Formations[0].ObjectiveProgress);
        Assert.Equal(TacticalFormationStatus.Active, first.NextState.Formations[0].Status);
        var second = Round(first.NextState); Assert.Equal(status, second.NextState.Formations[0].Status);
        Assert.Equal(TacticalCompletion.Objective, second.NextState.Completion);
    }
    [Fact]
    public void MatchingPursuerBlocksFleeingAndNoHardFleetCapExists()
    {
        var s = Encounter(); Unarmed(s);
        var r = Round(s, Order(s, 0, TacticalManeuver.Disengage), Order(s, 1, TacticalManeuver.Pursuit));
        Assert.Equal(TacticalRange.Engagement, r.NextState.Geometry[0].Range); Assert.Equal(TacticalFormationStatus.Active, r.NextState.Formations[0].Status);
    }
    [Fact]
    public void UnpoweredUnequippedOrOutOfRangeEquipmentDoesNotFire()
    {
        var s = Encounter(); Unarmed(s); Weapon(Ship(s), TacticalWeaponClass.Cannon, 10);
        var beam = Weapon(Ship(s), TacticalWeaponClass.Beam, 10); beam.AllocatedPower = 0;
        var torpedo = Weapon(Ship(s), TacticalWeaponClass.Torpedo, 10); torpedo.Equipped = false; torpedo.AllocatedPower = 0;
        var r = Round(s); Assert.DoesNotContain(r.Events, e => e.Kind == TacticalEventKind.WeaponDischarged); Assert.Equal(s.RandomState, r.NextState.RandomState);
    }
    [Fact]
    public void TorpedoYieldIsConstantWhileDistantWarningChangesChance()
    {
        var s = Encounter(); Unarmed(s); Weapon(Ship(s), TacticalWeaponClass.Torpedo, 70);
        var close = Round(s); s.Geometry[0].Range = TacticalRange.Distant;
        s.Formations[0].StandingOrder.PreferredRange = s.Formations[1].StandingOrder.PreferredRange = TacticalRange.Distant;
        var far = Round(s);
        Assert.Equal(70, Assert.Single(far.Events, e => e.Kind == TacticalEventKind.WeaponDischarged).Amount);
        Assert.Equal(8500, Assert.Single(far.Events, e => e.Kind == TacticalEventKind.AttackHit || e.Kind == TacticalEventKind.AttackMissed).ChanceBps);
        Assert.Equal(10000, Assert.Single(close.Events, e => e.Kind == TacticalEventKind.AttackHit).ChanceBps);
    }
    [Fact]
    public void FasterHoldingFormationCanDenyChargeAndFasterPursuitDeniesWithdrawal()
    {
        var s = Encounter(); Unarmed(s); s.Formations[1].Speed = 100;
        var held = Round(s, Order(s, 0, TacticalManeuver.Charge));
        Assert.Equal(TacticalRange.Engagement, held.NextState.Geometry[0].Range);
        var caught = Round(s, Order(s, 0, TacticalManeuver.FightingWithdrawal), Order(s, 1, TacticalManeuver.Pursuit));
        Assert.Equal(TacticalRange.Close, caught.NextState.Geometry[0].Range);
    }
    [Theory]
    [InlineData(TacticalTargetPriority.LowestId, 0)] [InlineData(TacticalTargetPriority.LowestHullRatio, 1)]
    [InlineData(TacticalTargetPriority.CommandSupport, 2)] [InlineData(TacticalTargetPriority.Screen, 2)]
    public void TargetPriorityAndConcentrationAreSeparateFromManeuver(TacticalTargetPriority priority, int targetIndex)
    {
        var s = Encounter(2, 3); Unarmed(s); Weapon(Ship(s), TacticalWeaponClass.Beam, 10);
        Ship(s, 1, 1).Hull = 100; Ship(s, 1, 2).Role = priority == TacticalTargetPriority.Screen ? TacticalShipRole.Escort : TacticalShipRole.CommandSupport;
        var order = Order(s, 0, TacticalManeuver.MaintainDistance); order.TargetPriority = priority;
        var r = Round(s, order);
        Assert.Equal(Ship(s, 1, targetIndex).ShipId, Assert.Single(r.Events, e => e.Kind == TacticalEventKind.WeaponDischarged).TargetShipId);
        Assert.Equal(TacticalManeuver.MaintainDistance, r.NextState.Formations[0].StandingOrder.Maneuver);
    }
    [Fact]
    public void ScreenDeniesProtectedTargetAccessAndSuccessfulFlankCanBypass()
    {
        var s = Encounter(3); Unarmed(s); Weapon(Ship(s, 1), TacticalWeaponClass.Beam, 10);
        var screen = Order(s, 2, TacticalManeuver.Screen); screen.ProtectedFormationId = s.Formations[0].FormationId;
        var attack = Order(s, 1, TacticalManeuver.MaintainDistance); attack.TargetFormationId = s.Formations[0].FormationId;
        var blocked = Round(s, screen, attack);
        Assert.Equal(Ship(s, 2).ShipId, Assert.Single(blocked.Events, e => e.Kind == TacticalEventKind.WeaponDischarged).TargetShipId);
        attack.Maneuver = TacticalManeuver.Flank; s.Formations[1].Agility = 100;
        var bypassed = Round(s, screen, attack);
        Assert.Equal(Ship(s).ShipId, Assert.Single(bypassed.Events, e => e.Kind == TacticalEventKind.WeaponDischarged).TargetShipId);
    }
    [Theory]
    [InlineData(0, 0)] [InlineData(10000, 2)]
    public void HitEndpointsConsumeExactlyOneLocalDrawPerDischarge(int chance, int hits)
    {
        var s = Encounter(); foreach (var ship in s.Formations.SelectMany(f => f.Ships)) ship.Weapons[0].AccuracyBps = chance;
        var r = Round(s); Assert.Equal(hits, r.Events.Count(e => e.Kind == TacticalEventKind.AttackHit));
        Assert.Equal(unchecked(47UL + 2 * 0x9e3779b97f4a7c15UL), r.NextState.RandomState);
    }
    [Fact]
    public void InvalidArmorArithmeticRejectsWithoutNumericWrap()
    {
        Assert.Throws<ArgumentException>(() => DeterministicTacticalCombatV2Resolver.ArmorDamage(-1, 0));
        Assert.Throws<ArgumentException>(() => DeterministicTacticalCombatV2Resolver.ArmorDamage(1, -1));
        Assert.Throws<ArgumentException>(() => DeterministicTacticalCombatV2Resolver.ArmorDamage(1, 1, denominator: 0));
        Assert.Equal(new BigInteger(100), DeterministicTacticalCombatV2Resolver.ArmorDamage(100, long.MaxValue, numerator: 0));
    }
}
