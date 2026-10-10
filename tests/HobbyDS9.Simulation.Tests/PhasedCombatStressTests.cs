using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.PhasedCombatFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class PhasedCombatStressTests
{
    [Theory]
    [InlineData(2, 1, 1)] [InlineData(2, 1, 2)] [InlineData(2, 2, 2)] [InlineData(3, 1, 2)]
    public void FixedSeedCompositionsValidateRepeatAndIgnoreRequestAndPlanOrdering(int civilizations, int fleets, int ships)
    {
        for (ulong seed = 0; seed < 48; seed++)
        {
            var request = Request(civilizations, fleets, ships, seed); var generator = new SplitMix64Random(seed + 1000);
            foreach (var ship in request.Participants.SelectMany(p => p.Ships))
            {
                ship.MaxHull = 1 + (long)generator.NextBelow(100); ship.Hull = 1 + (long)generator.NextBelow((ulong)ship.MaxHull);
                var profile = ship.CombatProfile!; profile.ShieldCapacity = (long)generator.NextBelow(50);
                profile.LongRangeAttack = (long)generator.NextBelow(40); profile.MediumRangeAttack = (long)generator.NextBelow(40); profile.CloseRangeAttack = (long)generator.NextBelow(40);
                profile.AccuracyBps = (int)generator.NextBelow(10001); profile.EvasionBps = (int)generator.NextBelow(6001);
            }
            var plans = Plans(request);
            foreach (var plan in plans)
            {
                plan.LongRangeTactic = (CombatTactic)(1 + generator.NextBelow(4)); plan.MediumRangeTactic = (CombatTactic)(1 + generator.NextBelow(4)); plan.CloseRangeTactic = (CombatTactic)(1 + generator.NextBelow(4));
                plan.TargetPriority = (CombatTargetPriority)(1 + generator.NextBelow(3)); plan.RetreatHullPercent = seed % 3 == 0 ? 25 : 0; plan.RetreatSystemId = plan.RetreatHullPercent == 0 ? null : "node.exit-" + plan.FleetId;
            }
            var before = Json(request); var plansBefore = Json(plans); var resolver = new DeterministicPhasedCombatResolver(); var result = resolver.Resolve(request, plans);
            Assert.Equal(before, Json(request)); Assert.Equal(plansBefore, Json(plans)); Assert.Equal(Json(result), Json(resolver.Resolve(request, plans)));
            var checkpoint = Checkpoint(request); var campaignRng = checkpoint.CampaignRandomState; var next = checkpoint.NextEntitySequence;
            var buffered = CombatEncounterScaffold.RecordResult(checkpoint, result); CombatEncounterScaffold.ValidateCheckpoint(Clone(buffered));
            Assert.Equal(campaignRng, buffered.CampaignRandomState); Assert.Equal(next, buffered.NextEntitySequence);
            var frozen = request.Participants.SelectMany(p => p.Ships).ToDictionary(s => s.ShipId, StringComparer.Ordinal);
            Assert.All(result.ShipChanges, c => Assert.InRange(c.RemainingHull, 0, frozen[c.ShipId].Hull));
            Assert.Equal(result.ShipChanges.Count, result.ShipChanges.Select(c => c.ShipId).Distinct().Count()); Assert.Equal(result.FleetChanges.Count, result.FleetChanges.Select(c => c.FleetId).Distinct().Count());
            Assert.Equal(result.ShipChanges.Select(c => c.ShipId).OrderBy(id => id, StringComparer.Ordinal), result.ShipChanges.Select(c => c.ShipId));
            Assert.Equal(result.FleetChanges.Select(c => c.FleetId).OrderBy(id => id, StringComparer.Ordinal), result.FleetChanges.Select(c => c.FleetId));
            VerifyEvents(result);
            request.Participants.Reverse(); foreach (var participant in request.Participants) participant.Ships.Reverse(); plans.Reverse();
            Assert.Equal(Json(result), Json(resolver.Resolve(request, plans)));
        }
    }
    private static void VerifyEvents(CombatResultDto result)
    {
        Assert.Equal(Enumerable.Range(0, result.Events.Count), result.Events.Select(e => e.Sequence));
        Assert.Equal(CombatEventKind.EncounterResolved, result.Events[^1].Kind); Assert.Single(result.Events, e => e.Kind == CombatEventKind.EncounterResolved);
        var phases = result.Events.Where(e => e.Kind == CombatEventKind.PhaseStarted).Select(e => e.Phase!.Value).ToArray();
        Assert.Equal(new[] { CombatPhaseKind.LongRange, CombatPhaseKind.MediumRange, CombatPhaseKind.CloseRange }.Take(phases.Length), phases);
        foreach (var phase in phases)
        {
            var events = result.Events.Where(e => e.Phase == phase).ToList(); Assert.Equal(CombatEventKind.PhaseStarted, events[0].Kind);
            int Category(CombatEventKind kind) => kind switch { CombatEventKind.PhaseStarted => 0, CombatEventKind.AttackHit or CombatEventKind.AttackMissed => 1, CombatEventKind.ShieldDamaged => 2, CombatEventKind.ShipDamaged => 3, CombatEventKind.ShipDestroyed => 4, CombatEventKind.FleetRetreated => 5, _ => 99 };
            Assert.Equal(events.Select(e => Category(e.Kind)).OrderBy(n => n), events.Select(e => Category(e.Kind)));
            foreach (var group in events.GroupBy(e => Category(e.Kind)).Where(g => g.Key != 0))
            {
                var ids = group.Select(e => group.Key == 1 ? e.SourceShipId! : group.Key == 5 ? e.FleetId! : e.ShipId!).ToList();
                Assert.Equal(ids.OrderBy(id => id, StringComparer.Ordinal), ids); Assert.Equal(ids.Count, ids.Distinct().Count());
            }
        }
    }
}
