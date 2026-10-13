using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.CampaignTacticalV2.Prototypes
{
    public static class ReviewExamples
    {
        public static PrototypeShip Ship(string id, string fleet, string owner, bool hidden = false, int defense = 0)
            => new PrototypeShip { Id = id, Fleet = fleet, Owner = owner, Type = "provisional-type", Fit = "fit.review", FitRevision = 1,
                Hull = 100, MaxHull = 100, Shields = new long[] { 10, 5, 5, 0 }, ShieldMaximum = new long[] { 10, 10, 10, 10 },
                Ammunition = 7, MagazineCapacity = 12, Opportunities = 2,
                DefenseOpportunities = defense, TorpedoDamage = 6, CloakCapable = hidden, Hidden = hidden,
                EffectivePower = 80, BaseAllocation = 62, CloakRequirement = 6 };
        public static PrototypeCheckpoint Encounter(bool hiddenA = true, bool hiddenB = false, bool screenB = true)
        {
            var a = Ship("ship.a", "fleet.a", "actor.a", hiddenA);
            var b = Ship("ship.b", "fleet.b", "actor.b", hiddenB, 2);
            return new PrototypeCheckpoint { Encounter = "encounter.review", FixtureRolls = new[] { 1000, 9000 }, InterceptionChanceBps = 5000,
                TargetRanking = TargetRankingCandidate.LowestHullRatio,
                Ships = new List<PrototypeShip> { a, b }, StandingOrders = new List<PrototypeFleetOrder> {
                    new PrototypeFleetOrder { Fleet = a.Fleet, Maneuver = PrototypeManeuver.Hold },
                    new PrototypeFleetOrder { Fleet = b.Fleet, Maneuver = screenB ? PrototypeManeuver.Screen : PrototypeManeuver.Hold } } };
        }
        public static PrototypeRoundCommand Command(RoundAccessPrototype session, string actor, PrototypeManeuver? maneuver = null)
        {
            var view = session.Observe(actor); var orders = session.BaselineOrders(actor).ToList();
            if (maneuver.HasValue) foreach (var order in orders) order.Maneuver = maneuver.Value;
            else
            {
                var standing = session.Export().StandingOrders;
                foreach (var order in orders) order.Maneuver = standing.Single(o => o.Fleet == order.Fleet).Maneuver;
            }
            return new PrototypeRoundCommand { Encounter = view.Encounter, Actor = actor, Command = actor + ".round." + view.Round,
                Round = view.Round, Revision = view.Revision, Orders = orders };
        }
        public static FleetJourney Journey(string fleet, string owner, string origin, long speed, bool engage, params TravelLeg[] legs)
            => new FleetJourney(fleet, owner, origin, speed, engage, legs);
        public static IReadOnlyList<string> Run()
        {
            var lines = new List<string> { "PROTOTYPE ONLY: no campaign activation or trusted import; all formulas and fixture rolls require review." };
            var composition = new[] { new MobilityInput("capital", 30, 20, 70, 60, 1),
                new MobilityInput("cruiser", 70, 60, 50, 55, 2), new MobilityInput("escort", 90, 80, 30, 40, 2) };
            foreach (MobilityCandidate candidate in Enum.GetValues(typeof(MobilityCandidate)))
                foreach (var c in FleetCandidates.Compare(composition, 70, candidate, 10))
                    lines.Add("mobility " + candidate + " ship=" + c.Ship + " personal=" + c.PersonalMobility + " coordination=" + c.Coordination
                        + " screening-opportunities=" + c.ScreeningOpportunities + " flank-target-aspect=" + c.FlankTargetAspect);
            var crossings = new[] { Journey("a", "A", "X", 2, true, new TravelLeg("B", 1), new TravelLeg("C", 1)),
                Journey("b", "B", "Y", 2, false, new TravelLeg("B", 1), new TravelLeg("D", 1)) };
            foreach (ContactBoundaryCandidate boundary in Enum.GetValues(typeof(ContactBoundaryCandidate)))
            {
                var travel = TravelCandidates.Run(crossings, new[] { new LawfulTravelContact("a", "b", new[] { "B" }) }, boundary, SharedStartCandidate.DepartBeforeRenewedContact);
                lines.Add("travel simultaneous-transit " + boundary + " meetings=" + travel.Meetings.Count);
                foreach (var meeting in travel.Meetings) lines.Add("contact node=" + meeting.Node + " time=" + meeting.Time + " stopped=" + string.Join(",", meeting.Fleets));
            }
            foreach (var speed in new long[] { 1, 3 })
            {
                // Pursuer chooses only a declared known destination, not the target's hidden continuation.
                var pursuit = new[] { Journey("pursuer", "A", "A", speed, true, new TravelLeg("B", 1)),
                    Journey("departing", "B", "A", 2, false, new TravelLeg("B", 1), new TravelLeg("C", 1), new TravelLeg("D", 1)) };
                var travel = TravelCandidates.Run(pursuit, new[] { new LawfulTravelContact("pursuer", "departing", new[] { "B" }) },
                    ContactBoundaryCandidate.ArrivalsBeforeDepartures, SharedStartCandidate.DepartBeforeRenewedContact);
                lines.Add("travel known-contact-pursuit speed=" + speed + " meetings=" + travel.Meetings.Count);
            }
            foreach (var bothHidden in new[] { false, true })
            {
                var session = new RoundAccessPrototype(Encounter(true, bothHidden));
                var a = Command(session, "actor.a"); var b = Command(session, "actor.b");
                if (!session.Commit(a) || !session.Commit(b)) throw new InvalidOperationException("Prototype walkthrough rejected.");
                var result = session.Export();
                lines.Add("round mutual-cloak=" + bothHidden + " boundary=" + result.NextRound + " rolls=" + result.RollCursor
                    + " ammo=" + string.Join(",", result.Ships.Select(s => s.Id + ":" + s.Ammunition)) + " hidden=" + result.Ships.Count(s => s.Hidden));
                foreach (var e in session.Observe("actor.b").Reports) lines.Add("lawful-report actor.b " + e.Message);
            }
            lines.Add("activation=false; session-owned current stocks, live campaign allocation and reviewed movement rules are still required.");
            return lines.AsReadOnly();
        }
    }
}
