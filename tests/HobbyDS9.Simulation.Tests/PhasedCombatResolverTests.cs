using System.Numerics;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.PhasedCombatFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class PhasedCombatResolverTests
{
    private static CombatResultDto Resolve(CombatRequestDto r, List<FleetCombatPlanDto>? p = null) => new DeterministicPhasedCombatResolver().Resolve(r, p ?? Plans(r));
    private static CombatShipDto Ship(CombatRequestDto r, int participant = 0, int ship = 0) => r.Participants[participant].Ships[ship];
    private static void Attacks(CombatShipDto s, long l = 0, long m = 0, long c = 0) { s.CombatProfile!.LongRangeAttack = l; s.CombatProfile.MediumRangeAttack = m; s.CombatProfile.CloseRangeAttack = c; }
    private static List<CombatEventDto> Shots(CombatResultDto r) => r.Events.Where(e => e.Kind == CombatEventKind.AttackHit || e.Kind == CombatEventKind.AttackMissed).ToList();
    private static long HullLoss(CombatResultDto r, string id) => r.Events.Where(e => e.Kind == CombatEventKind.ShipDamaged && e.ShipId == id).Sum(e => e.Amount!.Value);
    [Fact]
    public void KnownSeedPinsPhaseOrderRollVectorHullAndFinalRandomState()
    {
        var r = Request(); foreach (var ship in r.Participants.SelectMany(p => p.Ships)) { ship.CombatProfile!.AccuracyBps = 8000; ship.CombatProfile.EvasionBps = 1500; }
        var result = Resolve(r);
        Assert.Equal(new[] { CombatPhaseKind.LongRange, CombatPhaseKind.MediumRange, CombatPhaseKind.CloseRange }, result.Events.Where(e => e.Kind == CombatEventKind.PhaseStarted).Select(e => e.Phase!.Value));
        // Independently calculated SplitMix64 seed 47 rolls: 3765,7531,1384,9150,8878,5277.
        Assert.Equal(new[] { true, false, true, false, false, true }, Shots(result).Select(e => e.Kind == CombatEventKind.AttackHit));
        Assert.Equal(0xb54cda58fbbee8adUL, result.FinalRandomState); Assert.Equal(new long[] { 90, 80 }, result.ShipChanges.Select(s => s.RemainingHull));
        Assert.Equal(CombatSubmissionError.None, CombatEncounterScaffold.GetResultError(Checkpoint(r), r, result));
    }
    [Theory]
    [InlineData(0, 0)] [InlineData(10000, 6)]
    public void AccuracyEndpointsStillConsumeOneRollPerIntent(int accuracy, int hits)
    {
        var r = Request(); foreach (var s in r.Participants.SelectMany(p => p.Ships)) s.CombatProfile!.AccuracyBps = accuracy;
        var result = Resolve(r); Assert.Equal(6, Shots(result).Count); Assert.Equal(hits, Shots(result).Count(e => e.Kind == CombatEventKind.AttackHit)); Assert.Equal(0xb54cda58fbbee8adUL, result.FinalRandomState);
    }
    [Fact]
    public void ZeroAttacksConsumeNoDrawsAndOnePositiveAttackConsumesExactlyOne()
    {
        var r = Request(); Attacks(Ship(r)); Attacks(Ship(r, 1)); var empty = Resolve(r); Assert.Empty(Shots(empty)); Assert.Equal(47UL, empty.FinalRandomState); Assert.Empty(empty.ShipChanges);
        Attacks(Ship(r), m: 1); var one = Resolve(r); Assert.Single(Shots(one)); Assert.Equal(CombatPhaseKind.MediumRange, Shots(one)[0].Phase); Assert.Equal(0x9e3779b97f4a7c44UL, one.FinalRandomState);
    }
    [Theory]
    [InlineData(CombatTactic.Standard, 11)] [InlineData(CombatTactic.FocusFire, 11)] [InlineData(CombatTactic.Evasive, 8)] [InlineData(CombatTactic.Overcharge, 13)]
    public void TacticDamageUsesExactFloor(CombatTactic tactic, long expected)
    {
        var r = Request(); Attacks(Ship(r), 11); Attacks(Ship(r, 1)); var p = Plans(r); p[0].LongRangeTactic = tactic;
        Assert.Equal(expected, HullLoss(Resolve(r, p), Ship(r, 1).ShipId));
        Attacks(Ship(r), 1); Assert.Equal(1, HullLoss(Resolve(r, p), Ship(r, 1).ShipId));
    }
    public static IEnumerable<object[]> TacticPairs()
    {
        foreach (var a in Enum.GetValues<CombatTactic>()) foreach (var b in Enum.GetValues<CombatTactic>()) yield return new object[] { a, b };
    }
    [Theory]
    [MemberData(nameof(TacticPairs))]
    public void ExactAccuracyEvasionModifiersProveRollBoundaryForEveryTacticPair(CombatTactic attacker, CombatTactic target)
    {
        var accuracy = new Dictionary<CombatTactic, int> { [CombatTactic.Standard] = 0, [CombatTactic.FocusFire] = 1500, [CombatTactic.Evasive] = -500, [CombatTactic.Overcharge] = -1000 };
        var evasion = new Dictionary<CombatTactic, int> { [CombatTactic.Standard] = 0, [CombatTactic.FocusFire] = -1000, [CombatTactic.Evasive] = 2000, [CombatTactic.Overcharge] = -1500 };
        var r = Request(); Attacks(Ship(r), 1); Attacks(Ship(r, 1)); Ship(r, 1).CombatProfile!.EvasionBps = 2000;
        Ship(r).CombatProfile!.AccuracyBps = 3765 - accuracy[attacker] + 2000 + evasion[target]; var plans = Plans(r); plans[0].LongRangeTactic = attacker; plans[1].LongRangeTactic = target;
        Assert.Equal(CombatEventKind.AttackMissed, Assert.Single(Shots(Resolve(r, plans))).Kind);
        Ship(r).CombatProfile!.AccuracyBps++; Assert.Equal(CombatEventKind.AttackHit, Assert.Single(Shots(Resolve(r, plans))).Kind);
    }
    [Theory]
    [InlineData(10000, CombatTactic.FocusFire, 7000, CombatTactic.Standard, false)]
    [InlineData(3000, CombatTactic.Standard, 0, CombatTactic.Overcharge, false)]
    [InlineData(10000, CombatTactic.FocusFire, 0, CombatTactic.FocusFire, true)]
    [InlineData(0, CombatTactic.Evasive, 0, CombatTactic.Standard, false)]
    [InlineData(10000, CombatTactic.FocusFire, 10000, CombatTactic.Evasive, false)]
    public void AccuracyAndEvasionClampBeforeHitChance(int accuracy, CombatTactic attack, int evasion, CombatTactic defend, bool hit)
    {
        var r = Request(); Attacks(Ship(r), 1); Attacks(Ship(r, 1)); Ship(r).CombatProfile!.AccuracyBps = accuracy; Ship(r, 1).CombatProfile!.EvasionBps = evasion;
        var p = Plans(r); p[0].LongRangeTactic = attack; p[1].LongRangeTactic = defend;
        Assert.Equal(hit, Assert.Single(Shots(Resolve(r, p))).Kind == CombatEventKind.AttackHit);
    }
    [Theory]
    [InlineData(CombatTargetPriority.LowestHullRatio, 2)] [InlineData(CombatTargetPriority.HighestPhaseAttack, 1)] [InlineData(CombatTargetPriority.LowestShipId, 1)]
    public void TargetPriorityRanksAllEnemyCivilizationsAndExcludesAlliedShips(CombatTargetPriority priority, int target)
    {
        var r = Request(3, 2); foreach (var s in r.Participants.SelectMany(p => p.Ships)) Attacks(s);
        Attacks(Ship(r), 1); Ship(r, 2).Hull = 20; Attacks(Ship(r, 2), 5); Ship(r, 4).Hull = 10;
        var p = Plans(r); p[0].TargetPriority = priority; var result = Resolve(r, p);
        var first = Shots(result).First(e => e.SourceShipId == Ship(r).ShipId);
        Assert.Equal(Ship(r, target == 2 ? 4 : 2).ShipId, first.ShipId); Assert.DoesNotContain(Shots(result), e => e.SourceShipId == Ship(r).ShipId && e.FleetId == r.Participants[1].FleetId);
    }
    [Fact]
    public void HullRatioComparisonAndTiesAreExactAtInt64Limits()
    {
        var r = Request(3); foreach (var s in r.Participants.SelectMany(p => p.Ships)) { s.MaxHull = long.MaxValue; s.Hull = long.MaxValue - 1; Attacks(s); }
        Attacks(Ship(r), 1); Ship(r, 2).Hull = long.MaxValue - 2; var p = Plans(r); p[0].TargetPriority = CombatTargetPriority.LowestHullRatio;
        Assert.Equal(Ship(r, 2).ShipId, Assert.Single(Shots(Resolve(r, p))).ShipId);
        Ship(r, 2).Hull = Ship(r, 1).Hull; Assert.Equal(Ship(r, 1).ShipId, Assert.Single(Shots(Resolve(r, p))).ShipId);
    }
    [Fact]
    public void ThreatRankingChangesWithPhaseAndTiesUseOrdinalIds()
    {
        var r = Request(3); Attacks(Ship(r), 1, 1, 1); Attacks(Ship(r, 1), 2, 0, 2); Attacks(Ship(r, 2), 0, 2, 2);
        var p = Plans(r); p[0].TargetPriority = CombatTargetPriority.HighestPhaseAttack;
        Assert.Equal(new[] { Ship(r, 1).ShipId, Ship(r, 2).ShipId, Ship(r, 1).ShipId }, Shots(Resolve(r, p)).Where(e => e.SourceShipId == Ship(r).ShipId).Select(e => e.ShipId));
    }
    [Fact]
    public void SimultaneousLethalVolleyHasNoFirstStrikeAdvantageOrLaterShots()
    {
        var r = Request(); foreach (var s in r.Participants.SelectMany(p => p.Ships)) Attacks(s, 100, 100, 100);
        var result = Resolve(r); Assert.Equal(2, Shots(result).Count); Assert.Single(result.Events, e => e.Kind == CombatEventKind.PhaseStarted); Assert.All(result.ShipChanges, c => Assert.Equal(0, c.RemainingHull));
        Assert.Equal(2, result.FleetChanges.Count); Assert.All(result.FleetChanges, f => Assert.Equal(FleetCombatDisposition.Destroyed, f.Disposition)); Assert.Equal(0x3c6ef372fe94f859UL, result.FinalRandomState);
        r.Participants.Reverse(); Assert.Equal(Json(result), Json(Resolve(r)));
    }
    [Fact]
    public void FocusedOverkillAggregatesWithoutNegativeHullOrDuplicateDestruction()
    {
        var r = Request(2, 1, 3); foreach (var s in r.Participants.SelectMany(p => p.Ships)) Attacks(s, 80);
        var result = Resolve(r); Assert.Equal(6, Shots(result).Count); Assert.Equal(2, result.ShipChanges.Count); Assert.All(result.ShipChanges, c => Assert.Equal(0, c.RemainingHull));
        Assert.Equal(2, result.Events.Count(e => e.Kind == CombatEventKind.ShipDestroyed)); Assert.All(result.Events.Where(e => e.Kind == CombatEventKind.ShipDamaged), e => Assert.Equal(100, e.Amount));
    }
    [Fact]
    public void ShieldLossDoesNotPersistAndShieldsDoNotRegenerateBetweenPhases()
    {
        var r = Request(); Attacks(Ship(r), 3, 3, 3); Attacks(Ship(r, 1)); Ship(r, 1).CombatProfile!.ShieldCapacity = 7; Ship(r, 1).Hull = 50;
        var result = Resolve(r); Assert.Equal(new long?[] { 3, 3, 1 }, result.Events.Where(e => e.Kind == CombatEventKind.ShieldDamaged).Select(e => e.Amount));
        var change = Assert.Single(result.ShipChanges); Assert.Equal(48, change.RemainingHull); Assert.Equal(2, HullLoss(result, Ship(r, 1).ShipId));
        Attacks(Ship(r), 3); var shieldOnly = Resolve(r); Assert.Empty(shieldOnly.ShipChanges); Assert.Equal(3, Assert.Single(shieldOnly.Events, e => e.Kind == CombatEventKind.ShieldDamaged).Amount);
        Assert.Equal(Json(shieldOnly), Json(Resolve(r))); // Every encounter starts with full shields, even on a damaged hull.
    }
    [Theory]
    [InlineData(0, false)] [InlineData(89, false)] [InlineData(90, true)] [InlineData(100, true)]
    public void RetreatUsesPostVolleyHullAndInclusiveThreshold(int threshold, bool retreats)
    {
        var r = Request(); Attacks(Ship(r), 10, 0, 0); Attacks(Ship(r, 1)); var p = Plans(r); p[1].RetreatHullPercent = threshold; p[1].RetreatNodeId = threshold == 0 ? null : "node.exit";
        var result = Resolve(r, p); Assert.Equal(retreats ? 1 : 0, result.FleetChanges.Count); Assert.Equal(retreats ? 1 : 3, result.Events.Count(e => e.Kind == CombatEventKind.PhaseStarted));
        if (retreats) { var f = Assert.Single(result.FleetChanges); Assert.Equal(FleetCombatDisposition.Retreated, f.Disposition); Assert.Equal("node.exit", f.RetreatNodeId); Assert.True(result.Events.FindIndex(e => e.Kind == CombatEventKind.ShipDamaged) < result.Events.FindIndex(e => e.Kind == CombatEventKind.FleetRetreated)); }
    }
    [Fact]
    public void RetreatDenominatorIncludesDestroyedShipsAndUsesFullPrecision()
    {
        var r = Request(2, 1, 2); foreach (var s in r.Participants.SelectMany(p => p.Ships)) { s.Hull = s.MaxHull = long.MaxValue; Attacks(s); }
        Attacks(Ship(r), long.MaxValue); var p = Plans(r); p[1].RetreatHullPercent = 50; p[1].RetreatNodeId = "node.exit";
        var result = Resolve(r, p); Assert.Equal(0, Assert.Single(result.ShipChanges).RemainingHull); Assert.Equal(FleetCombatDisposition.Retreated, Assert.Single(result.FleetChanges).Disposition);
        Assert.Single(result.Events, e => e.Kind == CombatEventKind.PhaseStarted);
    }
    [Fact]
    public void DestroyedFleetDoesNotRetreatAndRetreatedShipsHaveNoLaterTargetsOrShots()
    {
        var r = Request(3); Attacks(Ship(r), 100); Attacks(Ship(r, 1), 1); Attacks(Ship(r, 2), 1);
        var p = Plans(r); p[1].RetreatHullPercent = 100; p[1].RetreatNodeId = "node.exit"; p[2].RetreatHullPercent = 100; p[2].RetreatNodeId = "node.other";
        var result = Resolve(r, p); Assert.Contains(result.FleetChanges, f => f.FleetId == p[1].FleetId && f.Disposition == FleetCombatDisposition.Destroyed);
        Assert.DoesNotContain(result.Events, e => e.Kind == CombatEventKind.FleetRetreated && e.FleetId == p[1].FleetId); Assert.Contains(result.FleetChanges, f => f.FleetId == p[2].FleetId && f.Disposition == FleetCombatDisposition.Retreated);
        Assert.Single(result.Events, e => e.Kind == CombatEventKind.PhaseStarted);
    }
    [Fact]
    public void ArbitrarilyLargeAggregatedDamageIsAppliedAsBoundedActualLosses()
    {
        var r = Request(2, 1, 3); foreach (var s in r.Participants.SelectMany(p => p.Ships)) { s.Hull = s.MaxHull = long.MaxValue; s.CombatProfile!.ShieldCapacity = long.MaxValue; Attacks(s, long.MaxValue); }
        var result = Resolve(r, Plans(r, CombatTactic.Overcharge));
        Assert.Equal(2, result.ShipChanges.Count); Assert.All(result.ShipChanges, c => Assert.Equal(0, c.RemainingHull));
        Assert.All(result.Events.Where(e => e.Kind == CombatEventKind.ShieldDamaged || e.Kind == CombatEventKind.ShipDamaged), e => Assert.Equal(long.MaxValue, e.Amount));
        Assert.Equal(CombatSubmissionError.None, CombatEncounterScaffold.GetResultError(Checkpoint(r), r, result));
    }
    [Fact]
    public void QuickResolveUsesExactlyTheSpecifiedDefaultPlansAndExplicitPlansChangeOutcomes()
    {
        var r = Request(); foreach (var s in r.Participants.SelectMany(p => p.Ships)) s.CombatProfile!.AccuracyBps = 5000;
        var p = Plans(r); foreach (var plan in p) { plan.LongRangeTactic = CombatTactic.FocusFire; plan.CloseRangeTactic = CombatTactic.Overcharge; plan.TargetPriority = CombatTargetPriority.LowestHullRatio; }
        var resolver = new DeterministicPhasedCombatResolver(); Assert.Equal(Json(resolver.Resolve(r)), Json(resolver.Resolve(r, p)));
        Assert.NotEqual(Json(resolver.Resolve(r)), Json(resolver.Resolve(r, Plans(r, CombatTactic.Evasive))));
    }
    [Fact]
    public void InvalidRequestOrPlanThrowsBeforeMutatingAnyInputAndResolverIdentitiesStayDistinct()
    {
        var r = Request(); var p = Plans(r); var before = Json(r); p[0].RetreatHullPercent = 1; var plansBefore = Json(p);
        Assert.Throws<ArgumentException>(() => Resolve(r, p)); Assert.Equal(before, Json(r)); Assert.Equal(plansBefore, Json(p));
        Assert.Throws<ArgumentException>(() => new NoLossCombatResolver().Resolve(r)); r.ResolverId = NoLossCombatResolver.ResolverId;
        Assert.Throws<ArgumentException>(() => Resolve(r)); Assert.Equal(r.RandomState, new NoLossCombatResolver().Resolve(r).FinalRandomState);
    }
    [Fact]
    public void SoleSurvivingCivilizationHasNoLaterTargetsOrDraws()
    {
        var r = Request(); Attacks(Ship(r), 100, 100, 100); Attacks(Ship(r, 1)); var result = Resolve(r);
        Assert.Single(Shots(result)); Assert.Single(result.Events, e => e.Kind == CombatEventKind.PhaseStarted); Assert.Equal(0x9e3779b97f4a7c44UL, result.FinalRandomState);
        Assert.Single(result.FleetChanges); Assert.Single(result.ShipChanges); Assert.Equal(0, result.ShipChanges[0].RemainingHull);
    }
    [Fact]
    public void ADeadShipCannotFireNextPhaseAndRetreatedFleetCannotTargetOrBeTargeted()
    {
        var r = Request(3); Attacks(Ship(r), 100, 1, 1); Attacks(Ship(r, 1), 1, 1, 1); Attacks(Ship(r, 2), 1, 1, 1);
        var result = Resolve(r); Assert.Equal(1, Shots(result).Count(e => e.SourceShipId == Ship(r, 1).ShipId)); Assert.Equal(3, result.Events.Count(e => e.Kind == CombatEventKind.PhaseStarted));
        r = Request(3); var p = Plans(r); p[1].RetreatHullPercent = 100; p[1].RetreatNodeId = "node.exit"; result = Resolve(r, p);
        Assert.DoesNotContain(Shots(result), e => e.Phase != CombatPhaseKind.LongRange && (e.SourceFleetId == p[1].FleetId || e.FleetId == p[1].FleetId));
        Assert.Equal(3, result.Events.Count(e => e.Kind == CombatEventKind.PhaseStarted));
    }

}
