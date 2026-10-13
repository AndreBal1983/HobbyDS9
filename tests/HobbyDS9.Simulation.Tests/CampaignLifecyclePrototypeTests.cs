using System.Globalization;
using System.Numerics;
using System.Text.Json;
using HobbyDS9.CampaignTacticalV2.Prototypes;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class CampaignLifecyclePrototypeTests
{
    private static string Canonical(RoundAccessPrototype session) => JsonSerializer.Serialize(session.Export());
    private static RoundAccessPrototype Reload(RoundAccessPrototype session) =>
        new(JsonSerializer.Deserialize<PrototypeCheckpoint>(Canonical(session))!);
    private static void Round(RoundAccessPrototype session, bool reverse = false)
    {
        var a = ReviewExamples.Command(session, "actor.a");
        var b = ReviewExamples.Command(session, "actor.b");
        Assert.True(session.Commit(reverse ? b : a)); Assert.True(session.Commit(reverse ? a : b));
    }
    [Fact]
    public void PortablePrototypeHasNoCampaignDependenciesAndCampaignHasNoImport()
    {
        var prototype = typeof(RoundAccessPrototype).Assembly;
        Assert.Equal(".NETStandard,Version=v2.1", prototype.GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
            .Cast<System.Runtime.Versioning.TargetFrameworkAttribute>().Single().FrameworkName);
        Assert.DoesNotContain(prototype.GetReferencedAssemblies(), a => a.Name!.StartsWith("HobbyDS9.", StringComparison.Ordinal));
        foreach (var assembly in new[] { typeof(HobbyDS9.Application.CampaignApplication).Assembly,
            typeof(HobbyDS9.Simulation.SimulationSession).Assembly, typeof(HobbyDS9.Contracts.GameStateDto).Assembly })
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name == prototype.GetName().Name);
        Assert.DoesNotContain(typeof(PrototypeFleetOrder).GetProperties(), p => p.Name.Contains("Protect") || p.Name.Contains("ShipTarget"));
        Assert.DoesNotContain(typeof(VisibleEnemy).GetProperties(), p => p.Name.Contains("Ammunition") || p.Name.Contains("Order") || p.Name.Contains("Fit"));
    }
    [Fact]
    public void RationalArithmeticIsExactNormalizedAndOverflowSafe()
    {
        Assert.Equal(new ExactTime(1, 3), new ExactTime(2, 6));
        Assert.Equal(ExactTime.One, new ExactTime(1, 3) + new ExactTime(2, 3));
        var large = new ExactTime((BigInteger)long.MaxValue * long.MaxValue, 3);
        Assert.True(large > ExactTime.One);
        Assert.Equal(ExactTime.One, new ExactTime(1, long.MaxValue) * long.MaxValue);
        Assert.Throws<ArgumentException>(() => default(ExactTime).CompareTo(ExactTime.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExactTime(1, 0));
    }
    [Theory]
    [InlineData(MobilityCandidate.IndividualThreshold, 0, 0, 2)]
    [InlineData(MobilityCandidate.CoordinatedThreshold, 6, 2, 2)]
    public void EscortKeepsIndividualMobilityAndFiniteCapabilityContribution(MobilityCandidate candidate, int coordination, int cruiser, int escort)
    {
        var ships = new[] { new MobilityInput("capital", 30, 20, 70, 60, 1), new MobilityInput("cruiser", 70, 60, 50, 55, 2),
            new MobilityInput("escort", 90, 80, 30, 40, 2) };
        var result = FleetCandidates.Compare(ships, 70, candidate, 10);
        Assert.Equal(new[] { 25, 65, 85 }, result.Select(r => r.PersonalMobility));
        Assert.All(result, c => Assert.Equal(coordination, c.Coordination));
        Assert.Equal(new[] { 0, cruiser, escort }, result.Select(c => c.ScreeningOpportunities));
        Assert.Equal(LocalTargetAspectCandidate.Fore, result[0].FlankTargetAspect);
        Assert.Equal(LocalTargetAspectCandidate.Lateral, result[2].FlankTargetAspect);
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(FleetCandidates.Compare(ships.Reverse(), 70, candidate, 10)));
    }
    [Theory]
    [InlineData(TargetRankingCandidate.LowestId, "ship.a")]
    [InlineData(TargetRankingCandidate.LowestHullRatio, "ship.b")]
    public void AutomaticTargetAlternativesUseVisibleConditionAndStableTies(TargetRankingCandidate ranking, string first)
    {
        var a = ReviewExamples.Ship("ship.a", "fleet.enemy", "actor.enemy");
        var b = ReviewExamples.Ship("ship.b", "fleet.enemy", "actor.enemy");
        a.Hull = 80; b.Hull = 90; b.MaxHull = 200;
        Assert.Equal(first, FleetCandidates.RankVisibleTargets(new[] { b, a }, ranking)[0]);
        a.Hidden = true;
        Assert.Throws<ArgumentException>(() => FleetCandidates.RankVisibleTargets(new[] { a, b }, ranking));
    }
    [Fact]
    public void NoHardFleetCapOrGenericCompositionPenalty()
    {
        var fleet = Enumerable.Range(0, 10000).Select(i => new MobilityInput("ship." + i.ToString("D5", CultureInfo.InvariantCulture), 90, 80, 30, 40, 2)).ToArray();
        var result = FleetCandidates.Compare(fleet, 70, MobilityCandidate.IndividualThreshold, 10);
        Assert.Equal(10000, result.Count); Assert.Equal(20000, result.Sum(c => c.ScreeningOpportunities));
        Assert.All(result, c => Assert.Equal(85, c.PersonalMobility));
    }
    private static LawfulTravelContact Contact(params string[] nodes) => new("a", "b", nodes);
    private static TravelComparison Travel(FleetJourney a, FleetJourney b, ContactBoundaryCandidate policy = ContactBoundaryCandidate.ArrivalsBeforeDepartures,
        SharedStartCandidate start = SharedStartCandidate.DepartBeforeRenewedContact, params string[] nodes) =>
        TravelCandidates.Run(new[] { a, b }, new[] { Contact(nodes.Length == 0 ? new[] { "B" } : nodes) }, policy, start);
    [Theory]
    [InlineData(1, 0)]
    [InlineData(3, 1)]
    public void FasterPursuerReachesKnownContactButSlowerOneFails(long speed, int meetings)
    {
        var a = ReviewExamples.Journey("a", "A", "A", speed, true, new TravelLeg("B", 1));
        var b = ReviewExamples.Journey("b", "B", "A", 2, false, new TravelLeg("B", 1), new TravelLeg("C", 1), new TravelLeg("D", 1));
        var result = Travel(a, b);
        Assert.Equal(meetings, result.Meetings.Count);
        if (meetings > 0) { Assert.Equal(new ExactTime(1, 2), result.Meetings.Single().Time); Assert.All(result.Fleets, f => Assert.Empty(f.RemainingRoute)); }
    }
    [Theory]
    [InlineData(ContactBoundaryCandidate.DeparturesFirst, 0)]
    [InlineData(ContactBoundaryCandidate.ArrivalsBeforeDepartures, 1)]
    public void SimultaneousArrivalDeparturePoliciesHaveReviewableDifferentOutcomes(ContactBoundaryCandidate policy, int meetings)
    {
        var a = ReviewExamples.Journey("a", "A", "X", 2, true, new TravelLeg("B", 1), new TravelLeg("C", 1));
        var b = ReviewExamples.Journey("b", "B", "Y", 2, false, new TravelLeg("B", 1), new TravelLeg("D", 1));
        var result = Travel(a, b, policy);
        Assert.Equal(meetings, result.Meetings.Count);
        if (meetings == 1) Assert.All(result.Fleets, f => { Assert.Equal("B", f.Node); Assert.True(f.StoppedByBattle); Assert.Empty(f.RemainingRoute); });
    }
    [Theory]
    [InlineData(SharedStartCandidate.DepartBeforeRenewedContact, 0)]
    [InlineData(SharedStartCandidate.ContactBeforeDeparture, 1)]
    public void SharedSectorDepartureIsAnExplicitPolicy(SharedStartCandidate policy, int meetings)
    {
        var a = ReviewExamples.Journey("a", "A", "A", 1, true, new TravelLeg("B", 1));
        var b = ReviewExamples.Journey("b", "B", "A", 1, false, new TravelLeg("C", 1));
        var result = Travel(a, b, start: policy, nodes: new[] { "A" });
        Assert.Equal(meetings, result.Meetings.Count);
    }
    [Fact]
    public void StationaryMutualReengagementWorksButLeavingTogetherDoesNotForceContact()
    {
        var a = ReviewExamples.Journey("a", "A", "B", 1, true);
        var b = ReviewExamples.Journey("b", "B", "B", 1, true);
        Assert.Equal(ExactTime.Zero, Travel(a, b).Meetings.Single().Time);
        a = ReviewExamples.Journey("a", "A", "B", 1, false, new TravelLeg("C", 1));
        b = ReviewExamples.Journey("b", "B", "B", 1, false, new TravelLeg("D", 1));
        Assert.Empty(Travel(a, b, start: SharedStartCandidate.ContactBeforeDeparture).Meetings);
    }
    [Fact]
    public void OppositeEdgeCrossingIsNotSectorContact()
    {
        var a = ReviewExamples.Journey("a", "A", "B", 1, true, new TravelLeg("C", 1));
        var b = ReviewExamples.Journey("b", "B", "C", 1, true, new TravelLeg("B", 1));
        Assert.Empty(Travel(a, b, nodes: new[] { "B", "C" }).Meetings);
    }
    [Fact]
    public void WeightedPartialEdgeHasNoFalseOriginOccupancy()
    {
        var a = new FleetJourney("a", "A", "B", 2, true, new[] { new TravelLeg("C", 5) }, 1);
        var b = ReviewExamples.Journey("b", "B", "B", 1, true);
        var result = Travel(a, b, start: SharedStartCandidate.ContactBeforeDeparture);
        Assert.Empty(result.Meetings);
        Assert.Equal("C", result.Fleets[0].TransitDestination); Assert.Equal(new ExactTime(3, 1), result.Fleets[0].EdgeProgress);
        Assert.Equal(new[] { "C" }, result.Fleets[0].RemainingRoute);
    }
    [Fact]
    public void PartialProgressCanBeFractionalWithoutRoundingOrTeleport()
    {
        var a = ReviewExamples.Journey("a", "A", "A", 3, false, new TravelLeg("B", 2), new TravelLeg("C", 5));
        var result = TravelCandidates.Run(new[] { a }, Array.Empty<LawfulTravelContact>(), ContactBoundaryCandidate.ArrivalsBeforeDepartures, SharedStartCandidate.DepartBeforeRenewedContact);
        Assert.Equal("B", result.Fleets.Single().Node); Assert.Equal("C", result.Fleets.Single().TransitDestination);
        Assert.Equal(new ExactTime(1, 1), result.Fleets.Single().EdgeProgress);
    }
    [Fact]
    public void ArrivalAtWeekEndDoesNotStartTheNextEdgeOrLoseTheRoute()
    {
        var a = ReviewExamples.Journey("a", "A", "A", 1, false, new TravelLeg("B", 1), new TravelLeg("C", 5));
        var result = TravelCandidates.Run(new[] { a }, Array.Empty<LawfulTravelContact>(), ContactBoundaryCandidate.ArrivalsBeforeDepartures, SharedStartCandidate.DepartBeforeRenewedContact).Fleets.Single();
        Assert.Equal("B", result.Node); Assert.Null(result.TransitDestination);
        Assert.Equal(ExactTime.Zero, result.EdgeProgress); Assert.Equal(new[] { "C" }, result.RemainingRoute);
    }
    [Fact]
    public void BattleStopsOnlyItsConnectedParticipantsAndIsPermutationInvariant()
    {
        var journeys = new[] {
            ReviewExamples.Journey("a", "A", "X", 3, true, new TravelLeg("B", 1), new TravelLeg("C", 1)),
            ReviewExamples.Journey("b", "B", "Y", 3, true, new TravelLeg("B", 1), new TravelLeg("D", 1)),
            ReviewExamples.Journey("unrelated", "A", "Z", 3, true, new TravelLeg("B", 1), new TravelLeg("E", 1)) };
        var contacts = new[] { Contact("B") };
        var one = TravelCandidates.Run(journeys, contacts, ContactBoundaryCandidate.ArrivalsBeforeDepartures, SharedStartCandidate.DepartBeforeRenewedContact);
        var two = TravelCandidates.Run(journeys.Reverse(), contacts.Reverse(), ContactBoundaryCandidate.ArrivalsBeforeDepartures, SharedStartCandidate.DepartBeforeRenewedContact);
        Assert.Equal(JsonSerializer.Serialize(one), JsonSerializer.Serialize(two));
        Assert.Equal(new[] { "a", "b" }, one.Meetings.Single().Fleets);
        Assert.Equal("E", one.Fleets.Single(f => f.Fleet == "unrelated").Node);
    }
    [Fact]
    public void UnknownContactNodeAndMissingHostileKnowledgeCannotGrantAnEncounter()
    {
        var a = ReviewExamples.Journey("a", "A", "B", 1, true);
        var b = ReviewExamples.Journey("b", "B", "B", 1, true);
        Assert.Empty(Travel(a, b, nodes: new[] { "hidden-node" }).Meetings);
        Assert.Empty(TravelCandidates.Run(new[] { a, b }, Array.Empty<LawfulTravelContact>(), ContactBoundaryCandidate.ArrivalsBeforeDepartures, SharedStartCandidate.DepartBeforeRenewedContact).Meetings);
    }
    [Theory]
    [InlineData(false, 5, 7, 2)]
    [InlineData(true, 7, 7, 0)]
    public void HiddenAttackIsOrdinaryRoundOneAndMutualCloakIsManeuverOnly(bool bothHidden, int ammoA, int ammoB, int cursor)
    {
        var session = new RoundAccessPrototype(ReviewExamples.Encounter(true, bothHidden));
        Assert.Empty(session.Observe("actor.b").VisibleEnemies);
        Round(session);
        var state = session.Export();
        Assert.Equal(2, state.NextRound); Assert.Equal(cursor, state.RollCursor);
        Assert.Equal(ammoA, state.Ships.Single(s => s.Owner == "actor.a").Ammunition);
        Assert.Equal(ammoB, state.Ships.Single(s => s.Owner == "actor.b").Ammunition);
        Assert.All(state.Ships, s => Assert.False(s.Hidden));
        Assert.DoesNotContain(session.Observe("actor.b").Reports, e => e.Message.Contains("ship.a", StringComparison.Ordinal));
        if (!bothHidden)
        {
            Assert.Contains(session.Observe("actor.b").Reports, e => e.Message == "interception-succeeded");
            Assert.Contains(session.Observe("actor.b").Reports, e => e.Message == "interception-failed");
            Assert.Equal(4, state.Ships.Single(s => s.Owner == "actor.b").Shields[0]);
        }
    }
    [Fact]
    public void MixedFleetExposesOnlyVisibleMembersAndRevealsEverySurvivor()
    {
        var input = ReviewExamples.Encounter();
        input.Ships.Add(ReviewExamples.Ship("ship.visible", "fleet.a", "actor.a"));
        var session = new RoundAccessPrototype(input);
        Assert.Equal("ship.visible", session.Observe("actor.b").VisibleEnemies.Single().Ship);
        Round(session); Assert.Equal(2, session.Observe("actor.b").VisibleEnemies.Count);
    }
    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 0)]
    public void ScreeningIsFleetLocalAndAttemptsHaveOpportunityCosts(bool sameFleet, int attempts)
    {
        var input = ReviewExamples.Encounter();
        input.Ships[1].DefenseOpportunities = 0; input.Ships[1].Opportunities = 0;
        var escort = ReviewExamples.Ship("ship.escort", sameFleet ? "fleet.b" : "fleet.escort", "actor.b", false, 2);
        escort.Ammunition = 0; input.Ships.Add(escort);
        if (!sameFleet) input.StandingOrders.Add(new PrototypeFleetOrder { Fleet = escort.Fleet, Maneuver = PrototypeManeuver.Screen });
        var session = new RoundAccessPrototype(input); Round(session);
        Assert.Equal(attempts, session.Export().RollCursor);
        Assert.DoesNotContain(session.Observe("actor.b").Reports, r => r.Message.Contains("ship.a", StringComparison.Ordinal));
    }
    [Fact]
    public void LoadedStandingFocusFallsBackWithoutChangingIntentOrTargetingHiddenShips()
    {
        var input = ReviewExamples.Encounter(false, false, false);
        input.Ships.Add(ReviewExamples.Ship("dead", "fleet.dead", "actor.b")); input.Ships.Last().Hull = 0;
        input.StandingOrders.Add(new PrototypeFleetOrder { Fleet = "fleet.dead", Maneuver = PrototypeManeuver.Hold });
        input.StandingOrders[0].FocusFleet = "fleet.dead";
        var session = new RoundAccessPrototype(input);
        var a = ReviewExamples.Command(session, "actor.a");
        // An exact retained preference remains lawful when unavailable; changed selections
        // must name a currently visible hostile fleet.
        a.Orders[0].FocusFleet = "fleet.dead";
        Assert.True(session.Commit(a));
        Assert.True(session.Commit(ReviewExamples.Command(session, "actor.b")));
        Assert.Contains(session.Observe("actor.a").Reports, r => r.Message == "focus-fallback: visible fleet fleet.b");
        Assert.Equal("fleet.dead", session.Export().StandingOrders.Single(o => o.Fleet == "fleet.a").FocusFleet);
    }
    [Fact]
    public void CommitmentsRemainPrivateAndAiCannotCounterPick()
    {
        var session = new RoundAccessPrototype(ReviewExamples.Encounter());
        var beforeB = JsonSerializer.Serialize(session.Observe("actor.b"));
        var beforeAI = JsonSerializer.Serialize(session.BaselineOrders("actor.b"));
        Assert.True(session.Commit(ReviewExamples.Command(session, "actor.a", PrototypeManeuver.Flank)));
        Assert.Equal(beforeB, JsonSerializer.Serialize(session.Observe("actor.b")));
        Assert.Equal(beforeAI, JsonSerializer.Serialize(session.BaselineOrders("actor.b")));
        Assert.True(session.Observe("actor.a").Committed);
        Assert.Equal(1, session.Observe("actor.a").Round);
    }
    [Theory]
    [InlineData("foreign")]
    [InlineData("stale")]
    [InlineData("replay")]
    [InlineData("exit")]
    [InlineData("hidden")]
    [InlineData("round")]
    [InlineData("encounter")]
    [InlineData("duplicate")]
    public void RejectedOrdersPreserveStocksStampReportsAndFixtureCursor(string defect)
    {
        var session = new RoundAccessPrototype(ReviewExamples.Encounter());
        if (defect == "replay") Assert.True(session.Commit(ReviewExamples.Command(session, "actor.a")));
        var before = Canonical(session); var command = ReviewExamples.Command(session, "actor.b");
        switch (defect)
        {
            case "foreign": command.Orders[0].Fleet = "fleet.a"; break;
            case "stale": command.Revision++; break;
            case "replay": command.Command = "actor.a.round.1"; break;
            case "exit": command.Orders[0].Maneuver = (PrototypeManeuver)99; break;
            case "hidden": command.Orders[0].FocusFleet = "fleet.a"; break;
            case "round": command.Round++; break;
            case "encounter": command.Encounter = "other"; break;
            case "duplicate": command.Orders.Add(command.Orders[0]); break;
        }
        Assert.False(session.Commit(command)); Assert.Equal(before, Canonical(session));
    }
    [Theory]
    [InlineData("exit")]
    [InlineData("foreign")]
    [InlineData("hidden")]
    [InlineData("power")]
    [InlineData("ammo")]
    [InlineData("shield")]
    public void MalformedLoadedCommitmentsAndResourcesAreRejected(string defect)
    {
        var session = new RoundAccessPrototype(ReviewExamples.Encounter());
        Assert.True(session.Commit(ReviewExamples.Command(session, "actor.a")));
        var saved = session.Export();
        switch (defect)
        {
            case "exit": saved.Commitments[0].Orders[0].Maneuver = (PrototypeManeuver)99; break;
            case "foreign": saved.Commitments[0].Orders[0].Fleet = "fleet.b"; break;
            case "hidden": saved.Commitments[0].Orders[0].FocusFleet = "fleet.a"; break;
            case "power": saved.Ships[0].EffectivePower = 62; break;
            case "ammo": saved.Ships[0].Ammunition = 13; break;
            case "shield": saved.Ships[0].Shields[0] = 11; break;
        }
        Assert.Throws<ArgumentException>(() => new RoundAccessPrototype(saved));
    }
    [Fact]
    public void SourceExportsAndOwnObservationsAreDetached()
    {
        var input = ReviewExamples.Encounter(); var session = new RoundAccessPrototype(input); var before = Canonical(session);
        input.Ships[0].Ammunition = 999; input.Ships[0].Shields[0] = 999;
        var exported = session.Export(); exported.Ships[0].Shields[0] = 999;
        session.Observe("actor.a").OwnShips[0].Ammunition = 999;
        Assert.Equal(before, Canonical(session));
    }
    [Theory]
    [InlineData("en-US")]
    [InlineData("nl-NL")]
    [InlineData("tr-TR")]
    public void CanonicalReverseRepeatedAndCommitmentSaveReloadAgree(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var canonical = new RoundAccessPrototype(ReviewExamples.Encounter());
            var reverse = new RoundAccessPrototype(ReviewExamples.Encounter());
            var resumed = new RoundAccessPrototype(ReviewExamples.Encounter());
            var reordered = ReviewExamples.Encounter(); reordered.Ships.Reverse(); reordered.StandingOrders.Reverse();
            var repeated = new RoundAccessPrototype(reordered);
            for (var round = 1; round <= 3; round++)
            {
                Round(canonical); Round(reverse, true); Round(repeated);
                var a = ReviewExamples.Command(resumed, "actor.a");
                var b = ReviewExamples.Command(resumed, "actor.b");
                Assert.True(resumed.Commit(a)); resumed = Reload(resumed); Assert.True(resumed.Commit(b)); resumed = Reload(resumed);
                Assert.Equal(Canonical(canonical), Canonical(reverse));
                Assert.Equal(Canonical(canonical), Canonical(resumed));
                Assert.Equal(Canonical(canonical), Canonical(repeated));
            }
            Assert.Equal(4, canonical.Export().NextRound);
            var before = Canonical(canonical); Assert.False(canonical.Commit(ReviewExamples.Command(canonical, "actor.a"))); Assert.Equal(before, Canonical(canonical));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
    [Fact]
    public void SuccessivePrototypeEncounterUsesRetainedPoolsWithoutRefill()
    {
        var session = new RoundAccessPrototype(ReviewExamples.Encounter());
        for (var i = 0; i < 3; i++) Round(session);
        var survivors = session.Export().Ships.Where(s => s.Hull > 0).Select(RoundAccessPrototype.Copy).ToList();
        var next = ReviewExamples.Encounter(); next.Encounter = "encounter.next"; next.Ships = survivors;
        var second = new RoundAccessPrototype(next); second = Reload(second);
        Assert.Equal(JsonSerializer.Serialize(survivors), JsonSerializer.Serialize(second.Export().Ships));
        Assert.True(second.Export().Ships[0].Ammunition < 7);
        Round(second); Assert.All(second.Export().Ships, s => Assert.True(s.Ammunition <= survivors.Single(p => p.Id == s.Id).Ammunition));
    }
    [Fact]
    public void LaterEncounterFailureCannotPartiallyApplyBatchAndFinalizationIsOnce()
    {
        var first = ReviewExamples.Encounter(); first.Encounter = "encounter.a";
        var second = ReviewExamples.Encounter(); second.Encounter = "encounter.b";
        foreach (var s in second.Ships) { s.Id += ".2"; s.Fleet += ".2"; }
        foreach (var o in second.StandingOrders) o.Fleet += ".2";
        var batch = new PrototypeBatch(new[] { second, first });
        Assert.False(batch.FinalizeBatch());
        foreach (var id in new[] { "encounter.b", "encounter.a" })
            for (var round = 1; round <= 3; round++)
            {
                var helper = new RoundAccessPrototype(batch.ExportEncounter(id));
                Assert.True(batch.Commit(ReviewExamples.Command(helper, "actor.a")));
                Assert.True(batch.Commit(ReviewExamples.Command(helper, "actor.b")));
            }
        var before = JsonSerializer.Serialize(batch.CampaignShips);
        var staged = JsonSerializer.Serialize(batch.ExportEncounter("encounter.a"));
        Assert.False(batch.FinalizeBatch("encounter.b"));
        Assert.Equal(before, JsonSerializer.Serialize(batch.CampaignShips));
        Assert.Equal(staged, JsonSerializer.Serialize(batch.ExportEncounter("encounter.a")));
        Assert.True(batch.FinalizeBatch()); Assert.False(batch.FinalizeBatch()); Assert.NotEqual(before, JsonSerializer.Serialize(batch.CampaignShips));
    }
    [Fact]
    public void LargeFleetRemainsOneCommandUnitWithLawfulVisibility()
    {
        var input = ReviewExamples.Encounter();
        input.Ships.Clear();
        for (var i = 0; i < 1000; i++)
        {
            var p = ReviewExamples.Ship("ship." + i.ToString("D4", CultureInfo.InvariantCulture), i % 2 == 0 ? "fleet.a" : "fleet.b",
                i % 2 == 0 ? "actor.a" : "actor.b");
            p.Opportunities = 0; p.DefenseOpportunities = 0; input.Ships.Add(p);
        }
        var session = new RoundAccessPrototype(input); Assert.Single(session.BaselineOrders("actor.a"));
        Assert.Equal(500, session.Observe("actor.a").OwnShips.Count); Assert.Equal(500, session.Observe("actor.a").VisibleEnemies.Count);
        Round(session); Assert.Equal(1000, session.Export().Ships.Count);
    }
}
