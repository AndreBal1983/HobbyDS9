using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.CampaignTacticalV2.Prototypes;

internal static class LifecyclePrototypeDiagnostics
{
    internal static IReadOnlyList<string> Run()
    {
        var lines = ReviewExamples.Run().ToList();
        var histories = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var variant in new[] { "canonical", "reverse", "repeated-reordered", "reload" })
        {
            var input = ReviewExamples.Encounter();
            if (variant == "repeated-reordered") { input.Ships.Reverse(); input.StandingOrders.Reverse(); }
            var session = new RoundAccessPrototype(input);
            for (var round = 1; round <= 3; round++)
            {
                var a = ReviewExamples.Command(session, "actor.a");
                var b = ReviewExamples.Command(session, "actor.b");
                bool reverse = variant == "reverse";
                if (!session.Commit(reverse ? b : a)) throw new InvalidOperationException("First prototype commitment rejected.");
                if (variant == "reload") session = Reload(session);
                if (!session.Commit(reverse ? a : b)) throw new InvalidOperationException("Joint prototype commitment rejected.");
                if (variant == "reload") session = Reload(session);
            }
            histories.Add(variant, JsonSerializer.Serialize(session.Export()));
            lines.Add("prototype-three-round " + variant + " sha256=" + Hash(histories[variant]));
        }
        if (histories.Values.Distinct(StringComparer.Ordinal).Count() != 1) throw new InvalidOperationException("Prototype ordering/reload diverged.");
        var first = JsonSerializer.Deserialize<PrototypeCheckpoint>(histories["canonical"])!;
        // Explicitly new toy encounter with retained stocks, not a campaign allocation.
        var second = ReviewExamples.Encounter(); second.Encounter = "encounter.second";
        second.Ships = first.Ships.Select(RoundAccessPrototype.Copy).ToList();
        foreach (var ship in second.Ships) ship.Hidden = ship.CloakCapable;
        var next = Reload(new RoundAccessPrototype(second));
        lines.Add("prototype-second-encounter-start " + Pools(next.Export()));
        var a2 = ReviewExamples.Command(next, "actor.a"); var b2 = ReviewExamples.Command(next, "actor.b");
        if (!next.Commit(a2) || !next.Commit(b2)) throw new InvalidOperationException("Second prototype encounter rejected.");
        lines.Add("prototype-second-encounter-round-one " + Pools(next.Export()));

        var one = ReviewExamples.Encounter(); one.Encounter = "encounter.batch.a";
        var two = ReviewExamples.Encounter(); two.Encounter = "encounter.batch.b";
        foreach (var ship in two.Ships) { ship.Id += ".2"; ship.Fleet += ".2"; }
        foreach (var order in two.StandingOrders) order.Fleet += ".2";
        var batch = new PrototypeBatch(new[] { two, one });
        foreach (var encounter in new[] { two.Encounter, one.Encounter })
            for (var round = 1; round <= 3; round++)
            {
                var helper = new RoundAccessPrototype(batch.ExportEncounter(encounter));
                if (!batch.Commit(ReviewExamples.Command(helper, "actor.a")) || !batch.Commit(ReviewExamples.Command(helper, "actor.b")))
                    throw new InvalidOperationException("Batch prototype commitment rejected.");
            }
        var before = JsonSerializer.Serialize(batch.CampaignShips);
        var rejected = !batch.FinalizeBatch(two.Encounter);
        var unchanged = before == JsonSerializer.Serialize(batch.CampaignShips);
        var accepted = batch.FinalizeBatch();
        var once = !batch.FinalizeBatch();
        if (!rejected || !unchanged || !accepted || !once) throw new InvalidOperationException("Prototype atomicity failed.");
        lines.Add("prototype-batch later-failure-rollback=True finalize-once=True ships=" + batch.CampaignShips.Count);

        foreach (SharedStartCandidate start in Enum.GetValues<SharedStartCandidate>())
        {
            var leave = new[] { ReviewExamples.Journey("a", "A", "B", 2, true, new TravelLeg("C", 1)),
                ReviewExamples.Journey("b", "B", "B", 2, false, new TravelLeg("D", 1)) };
            var travel = TravelCandidates.Run(leave, new[] { new LawfulTravelContact("a", "b", new[] { "B" }) },
                ContactBoundaryCandidate.ArrivalsBeforeDepartures, start);
            lines.Add("travel shared-start " + start + " meetings=" + travel.Meetings.Count);
        }
        var weighted = TravelCandidates.Run(new[] { new FleetJourney("weighted", "A", "B", 2, true, new[] { new TravelLeg("C", 5) }, 1) },
            Array.Empty<LawfulTravelContact>(), ContactBoundaryCandidate.ArrivalsBeforeDepartures, SharedStartCandidate.DepartBeforeRenewedContact);
        lines.Add("travel weighted-edge origin=" + weighted.Fleets[0].Node + " destination=" + weighted.Fleets[0].TransitDestination
            + " progress=" + weighted.Fleets[0].EdgeProgress + " sector-contact=False");
        return lines.AsReadOnly();
    }
    private static RoundAccessPrototype Reload(RoundAccessPrototype session) =>
        new(JsonSerializer.Deserialize<PrototypeCheckpoint>(JsonSerializer.Serialize(session.Export()))!);
    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    private static string Pools(PrototypeCheckpoint state) => string.Join(";", state.Ships.Select(s => s.Id + " hull=" + s.Hull
        + " shields=" + string.Join(",", s.Shields) + " ammo=" + s.Ammunition + " fit=" + s.Fit + "/" + s.FitRevision));
}
