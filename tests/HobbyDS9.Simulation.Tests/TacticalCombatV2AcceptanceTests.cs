using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using Xunit.Abstractions;
using static HobbyDS9.Simulation.Tests.TacticalCombatV2Fixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class TacticalCombatV2AcceptanceTests
{
    private readonly ITestOutputHelper output;
    public TacticalCombatV2AcceptanceTests(ITestOutputHelper output) { this.output = output; }
    [Fact]
    public void ReferenceVectorPinsEventsStateAndEncounterLocalStream()
    {
        var s = Encounter(); var results = new DeterministicTacticalCombatV2Resolver().ResolveAutomatic(s);
        Assert.Equal(3, results.Count);
        Assert.Equal(0xb54cda58fbbee8adUL, results[^1].NextState.RandomState);
        Assert.All(results[^1].NextState.Formations, f => { Assert.Equal(1000, f.Ships[0].Hull); Assert.Equal(20, f.Ships[0].Shields[0].Current); });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Json(results)))).ToLowerInvariant();
        output.WriteLine("v2-reference-sha256=" + hash);
        Assert.Equal("ccae7836c085936ed962b9ba001764ae4252f135fdf62fa1fbe2c0ea0bcef07c", hash);
    }
    [Fact]
    public void CanonicalReverseRepeatedReloadedAndBattleOrderHaveIdenticalResults()
    {
        var a = Encounter(4, 3); var b = Encounter(4, 3, 99); b.EncounterId = "encounter.other";
        var resolver = new DeterministicTacticalCombatV2Resolver();
        var firstA = Json(resolver.ResolveAutomatic(a)); var firstB = Json(resolver.ResolveAutomatic(b));
        Assert.Equal(firstB, Json(resolver.ResolveAutomatic(b))); Assert.Equal(firstA, Json(resolver.ResolveAutomatic(a)));
        var reversed = Reload(a); Reverse(reversed);
        Assert.Equal(firstA, Json(resolver.ResolveAutomatic(reversed)));
        Assert.Equal(firstA, Json(resolver.ResolveAutomatic(Reload(a))));
        Assert.Equal(47UL, a.RandomState); Assert.Equal(99UL, b.RandomState);
    }
    private static void Reverse(TacticalEncounterV2Dto s)
    {
        s.Formations.Reverse(); s.Geometry.Reverse();
        foreach (var f in s.Formations) { f.Ships.Reverse(); foreach (var ship in f.Ships) { ship.Shields.Reverse(); ship.Weapons.Reverse(); ship.Mounts.Reverse(); } }
    }
    public static IEnumerable<object[]> ManeuverPairs()
    {
        foreach (var a in Enum.GetValues<TacticalManeuver>()) foreach (var b in Enum.GetValues<TacticalManeuver>()) yield return new object[] { a, b };
    }
    [Theory]
    [MemberData(nameof(ManeuverPairs))]
    public void EveryOpposingOrderPairHasBoundedRepeatableGeometryAtEveryRange(TacticalManeuver a, TacticalManeuver b)
    {
        foreach (var range in Enum.GetValues<TacticalRange>())
        {
            var s = Encounter(); Unarmed(s); s.Geometry[0].Range = range;
            s.Formations[0].StandingOrder = Order(s, 0, a); s.Formations[1].StandingOrder = Order(s, 1, b);
            var r = Round(s); Assert.InRange((int)r.NextState.Geometry[0].Range, 0, 3);
            var reversed = Reload(s); Reverse(reversed); Assert.Equal(Trace(r), Trace(Round(reversed)));
            Assert.All(r.NextState.Formations.SelectMany(f => f.Ships).SelectMany(ship => ship.Shields), q => Assert.Equal(50, q.Current));
        }
    }
    [Fact]
    public void SerializedEventsRoundTripWithoutResolverOrRandomDraws()
    {
        var result = Round(Encounter()); var json = Json(result.Events); var state = result.NextState;
        var restored = System.Text.Json.JsonSerializer.Deserialize<List<TacticalEventV2>>(json)!;
        Assert.Equal(json, Json(restored)); Assert.Equal(state.RandomState, result.NextState.RandomState);
        Assert.All(typeof(TacticalEventV2).GetProperties(), property => Assert.Null(property.SetMethod));
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)] [InlineData(12)] [InlineData(13)] [InlineData(14)] [InlineData(15)]
    [InlineData(16)] [InlineData(17)] [InlineData(18)] [InlineData(19)] [InlineData(20)] [InlineData(21)] [InlineData(22)] [InlineData(23)]
    [InlineData(24)] [InlineData(25)] [InlineData(26)] [InlineData(27)] [InlineData(28)] [InlineData(29)] [InlineData(30)] [InlineData(31)]
    public void InvalidStateRejectsAtomically(int corruption)
    {
        var s = Encounter();
        switch (corruption)
        {
            case 0: s.FormatVersion++; break;
            case 1: s.ResolverId = CampaignCombatRules.ResolverId; break;
            case 2: s.RulesId = "science-engineering-v1"; break;
            case 3: s.RandomAlgorithmId = "unknown"; break;
            case 4: s.NextRound = 4; break;
            case 5: s.Completion = TacticalCompletion.RoundLimit; break;
            case 6: s.Rules.ArmorDenominator = 0; break;
            case 7: s.Rules.SpeedWeight = int.MaxValue; break;
            case 8: s.Rules.HoldDefenseBps = -1; break;
            case 9: s.Formations[1].FormationId = s.Formations[0].FormationId; break;
            case 10: Ship(s, 1).ShipId = Ship(s).ShipId; break;
            case 11: Ship(s).Hull = -1; break;
            case 12: Ship(s).Armor = -1; break;
            case 13: Ship(s).Shields[0].Sector = (TacticalSector)4; break;
            case 14: Ship(s).Shields[0].Current = 51; break;
            case 15: Ship(s).Shields[0].Sector = TacticalSector.Port; break;
            case 16: Ship(s).Shields.RemoveAt(0); break;
            case 17: Ship(s).Weapons[0].Coverage = TacticalArc.None; break;
            case 18: Ship(s).Mounts[0].WeaponClass = TacticalWeaponClass.Cannon; break;
            case 19: Ship(s).PowerBudget = 0; break;
            case 20: Ship(s).Weapons[0].MagazineCapacity = 1; break;
            case 21: Ship(s).Weapons[0].NextFireRound = 2; break;
            case 22: Ship(s).Weapons[0].Damage = -1; break;
            case 23: s.Geometry.Clear(); break;
            case 24: s.Geometry.Add(s.Geometry[0]); break;
            case 25: s.Geometry[0].FacingA = (TacticalSector)(-1); break;
            case 26: s.Formations[0].Status = TacticalFormationStatus.Destroyed; break;
            case 27: s.Formations[0].ObjectiveCompleted = true; break;
            case 28: s.Formations[0].StandingOrder.Maneuver = (TacticalManeuver)9; break;
            case 29: s.Formations[0].StandingOrder.TargetFormationId = s.Formations[0].FormationId; break;
            case 30: s.Formations[0].StandingOrder.ProtectedFormationId = s.Formations[1].FormationId; break;
            case 31: Ship(s).Weapons = null!; break;
        }
        var before = Json(s); Assert.Throws<ArgumentException>(() => Round(s)); Assert.Equal(before, Json(s));
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void InvalidJointOrdersRejectWithoutPartialCommit(int corruption)
    {
        var s = Encounter(); var good = Order(s, 0, TacticalManeuver.Charge); var bad = Order(s, 1, TacticalManeuver.Charge);
        var batch = new TacticalOrderBatchV2Dto { EncounterId = s.EncounterId, Round = 1, Replacements = new() { good, bad } };
        switch (corruption)
        {
            case 0: batch.Round = 2; break;
            case 1: batch.EncounterId = "other"; break;
            case 2: batch.Replacements.Add(good); break;
            case 3: bad.FormationId = "formation.unknown"; break;
            case 4: bad.TargetFormationId = bad.FormationId; break;
            case 5: bad.TargetPriority = (TacticalTargetPriority)99; break;
            case 6: batch.Replacements[1] = null!; break;
        }
        var before = Json(s); var ordersBefore = Json(batch);
        Assert.Throws<ArgumentException>(() => new DeterministicTacticalCombatV2Resolver().ResolveRound(s, batch));
        Assert.Equal(before, Json(s)); Assert.Equal(ordersBefore, Json(batch));
    }
    [Fact]
    public void ManySeedPropertyRunsMaintainInvariantsAndOrdering()
    {
        for (ulong seed = 0; seed < 100; seed++)
        {
            var s = Encounter(4, 3, seed); var generator = new SplitMix64Random(seed);
            foreach (var f in s.Formations)
            {
                f.Speed = (int)generator.NextBelow(101); f.Agility = (int)generator.NextBelow(101);
                f.StandingOrder.Maneuver = (TacticalManeuver)(1 + generator.NextBelow(8));
                f.StandingOrder.PreferredRange = (TacticalRange)generator.NextBelow(4);
                foreach (var ship in f.Ships)
                {
                    ship.Armor = (long)generator.NextBelow(21); ship.Hull = ship.MaxHull = 50 + (long)generator.NextBelow(151);
                    ship.ShieldRechargePerRound = (long)generator.NextBelow(10); ship.Weapons[0].AccuracyBps = (int)generator.NextBelow(10001);
                    ship.Weapons[0].OpportunitiesPerRound = 2;
                    Weapon(ship, TacticalWeaponClass.Torpedo, 20, 2).CadenceRounds = 2;
                }
            }
            var reversed = Reload(s); Reverse(reversed);
            var resolver = new DeterministicTacticalCombatV2Resolver(); var expected = resolver.ResolveAutomatic(s);
            Assert.Equal(Json(expected), Json(resolver.ResolveAutomatic(reversed)));
            Assert.Equal(Json(expected), Json(resolver.ResolveAutomatic(s)));
            Assert.All(expected, r => { TacticalCombatV2State.Validate(r.NextState); Assert.Equal(Enumerable.Range(0, r.Events.Count), r.Events.Select(e => e.Sequence)); });
        }
    }
    [Theory]
    [InlineData(2, 500)] [InlineData(20, 50)]
    public void MeasuredThousandShipStressHasNoFleetRestriction(int formations, int shipsPerFormation)
    {
        var s = Encounter(formations, shipsPerFormation);
        var resolver = new DeterministicTacticalCombatV2Resolver(); resolver.ResolveAutomatic(Encounter());
        var allocation = GC.GetAllocatedBytesForCurrentThread(); var timer = Stopwatch.StartNew();
        var results = resolver.ResolveAutomatic(s); timer.Stop(); allocation = GC.GetAllocatedBytesForCurrentThread() - allocation;
        output.WriteLine($"stress formations={formations}, ships={formations * shipsPerFormation}, rounds={results.Count}, events={results.Sum(r => r.Events.Count)}, elapsed-ms={timer.Elapsed.TotalMilliseconds:F3}, allocated-bytes={allocation}, runtime={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}, os={System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        Assert.Equal(3, results.Count); Assert.Equal(1000, results[^1].NextState.Formations.Sum(f => f.Ships.Count));
        Assert.InRange(allocation, 1, 512L * 1024 * 1024);
        Assert.All(results[^1].NextState.Formations.SelectMany(f => f.Ships), ship => Assert.InRange(ship.Hull, 0, ship.MaxHull));
        Assert.All(s.Formations.SelectMany(f => f.Ships), ship => Assert.Equal(1000, ship.Hull));
    }
    [Fact]
    public void MeasuredMixedWeaponStressIncludesScreeningSaturationAndRoundPersistence()
    {
        var s = Encounter(10, 100);
        foreach (var f in s.Formations)
        {
            f.StandingOrder.Maneuver = TacticalManeuver.Screen;
            foreach (var ship in f.Ships) { ship.Weapons[0].OpportunitiesPerRound = 2; Weapon(ship, TacticalWeaponClass.Torpedo, 20, 2); }
        }
        var resolver = new DeterministicTacticalCombatV2Resolver(); resolver.ResolveAutomatic(Encounter());
        long allocated = GC.GetAllocatedBytesForCurrentThread(); var timer = Stopwatch.StartNew();
        var results = resolver.ResolveAutomatic(s); timer.Stop(); allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        output.WriteLine($"stress mixed formations=10, ships=1000, rounds={results.Count}, events={results.Sum(r => r.Events.Count)}, elapsed-ms={timer.Elapsed.TotalMilliseconds:F3}, allocated-bytes={allocated}");
        Assert.InRange(results.Count, 1, 3); Assert.Contains(results.SelectMany(r => r.Events), e => e.Kind == TacticalEventKind.TorpedoIntercepted);
        Assert.Contains(results.SelectMany(r => r.Events), e => e.Kind == TacticalEventKind.AttackHit);
        Assert.InRange(allocated, 1, 512L * 1024 * 1024);
        TacticalCombatV2State.Validate(Reload(results[^1].NextState));
    }
}
