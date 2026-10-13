using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.CampaignTacticalV2.Prototypes
{
    public enum ContactBoundaryCandidate { DeparturesFirst = 1, ArrivalsBeforeDepartures = 2 }
    public enum SharedStartCandidate { DepartBeforeRenewedContact = 1, ContactBeforeDeparture = 2 }
    public sealed class TravelLeg
    {
        public string Node { get; }
        public long Cost { get; }
        public TravelLeg(string node, long cost)
        { if (string.IsNullOrWhiteSpace(node) || cost <= 0) throw new ArgumentException("Invalid leg."); Node = node; Cost = cost; }
    }
    public sealed class FleetJourney
    {
        public string Fleet { get; }
        public string Owner { get; }
        public string Origin { get; }
        public long Speed { get; }
        public bool Engage { get; }
        public IReadOnlyList<TravelLeg> Legs { get; }
        // Nonzero progress belongs only to the first locked weighted leg.
        public long InitialProgress { get; }
        public FleetJourney(string fleet, string owner, string origin, long speed, bool engage,
            IEnumerable<TravelLeg> legs, long initialProgress = 0)
        {
            if (new[] { fleet, owner, origin }.Any(string.IsNullOrWhiteSpace) || speed <= 0 || initialProgress < 0)
                throw new ArgumentException("Invalid journey.");
            var copy = legs.ToList();
            if (copy.Any(l => l == null) || (initialProgress > 0 && (copy.Count == 0 || initialProgress >= copy[0].Cost)))
                throw new ArgumentException("Invalid locked edge.");
            Fleet = fleet; Owner = owner; Origin = origin; Speed = speed; Engage = engage;
            Legs = copy.AsReadOnly(); InitialProgress = initialProgress;
        }
    }
    public sealed class LawfulTravelContact
    {
        public string FleetA { get; }
        public string FleetB { get; }
        public IReadOnlyList<string> KnownContactNodes { get; }
        public LawfulTravelContact(string a, string b, IEnumerable<string> nodes)
        { FleetA = a; FleetB = b; KnownContactNodes = nodes.Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToList().AsReadOnly(); }
    }
    public sealed class TravelMeeting
    {
        public string Node { get; }
        public ExactTime Time { get; }
        public IReadOnlyList<string> Fleets { get; }
        internal TravelMeeting(string node, ExactTime time, IEnumerable<string> fleets)
        { Node = node; Time = time; Fleets = fleets.OrderBy(f => f, StringComparer.Ordinal).ToList().AsReadOnly(); }
    }
    public sealed class JourneyResult
    {
        public string Fleet { get; }
        public string Node { get; }
        public string? TransitDestination { get; }
        public ExactTime EdgeProgress { get; }
        public bool StoppedByBattle { get; }
        public IReadOnlyList<string> RemainingRoute { get; }
        internal JourneyResult(string fleet, string node, string? destination, ExactTime progress, bool stopped, IEnumerable<string> route)
        { Fleet = fleet; Node = node; TransitDestination = destination; EdgeProgress = progress; StoppedByBattle = stopped; RemainingRoute = route.ToList().AsReadOnly(); }
    }
    public sealed class TravelComparison
    {
        public IReadOnlyList<TravelMeeting> Meetings { get; }
        public IReadOnlyList<JourneyResult> Fleets { get; }
        internal TravelComparison(List<TravelMeeting> meetings, List<JourneyResult> fleets)
        { Meetings = meetings.AsReadOnly(); Fleets = fleets.AsReadOnly(); }
    }
    // Exact rational time, no probability, hidden-route inference or campaign import.
    public static class TravelCandidates
    {
        private sealed class Visit
        {
            internal string Node = "";
            internal ExactTime Arrival, Departure;
            internal bool Last;
        }
        private static List<Visit> Visits(FleetJourney f)
        {
            var result = new List<Visit>(); var at = ExactTime.Zero; var node = f.Origin;
            if (f.InitialProgress == 0)
                result.Add(new Visit { Node = node, Arrival = at, Departure = f.Legs.Count == 0 ? ExactTime.One : at, Last = f.Legs.Count == 0 });
            for (var i = 0; i < f.Legs.Count; i++)
            {
                var leg = f.Legs[i];
                at += new ExactTime(leg.Cost - (i == 0 ? f.InitialProgress : 0), f.Speed);
                if (at > ExactTime.One) break;
                node = leg.Node;
                bool last = i == f.Legs.Count - 1 || at.Equals(ExactTime.One);
                result.Add(new Visit { Node = node, Arrival = at, Departure = last ? ExactTime.One : at, Last = last });
            }
            return result;
        }
        public static TravelComparison Run(IEnumerable<FleetJourney> journeys, IEnumerable<LawfulTravelContact> lawfulContacts,
            ContactBoundaryCandidate boundary, SharedStartCandidate sharedStart)
        {
            if (!Enum.IsDefined(typeof(ContactBoundaryCandidate), boundary) || !Enum.IsDefined(typeof(SharedStartCandidate), sharedStart))
                throw new ArgumentException("Unknown timing candidate.");
            var fleets = journeys.OrderBy(f => f.Fleet, StringComparer.Ordinal).ToList();
            if (fleets.Select(f => f.Fleet).Distinct(StringComparer.Ordinal).Count() != fleets.Count) throw new ArgumentException("Duplicate fleet.");
            var index = fleets.ToDictionary(f => f.Fleet, StringComparer.Ordinal);
            var visits = fleets.ToDictionary(f => f.Fleet, Visits, StringComparer.Ordinal);
            var contacts = lawfulContacts.ToList();
            if (contacts.Any(c => !index.ContainsKey(c.FleetA) || !index.ContainsKey(c.FleetB) || c.FleetA == c.FleetB
                || index[c.FleetA].Owner == index[c.FleetB].Owner || c.KnownContactNodes.Any(string.IsNullOrWhiteSpace)))
                throw new ArgumentException("Contact must explicitly declare lawful hostile knowledge.");
            // A visit is a sector-time fact; an interior edge position never counts as a sector.
            bool Occupies(Visit v, ExactTime t)
            {
                if (t < v.Arrival || t > v.Departure) return false;
                if (v.Last) return true;
                if (t.Equals(ExactTime.Zero)) return sharedStart == SharedStartCandidate.ContactBeforeDeparture;
                return boundary == ContactBoundaryCandidate.ArrivalsBeforeDepartures || t < v.Departure;
            }
            var times = visits.Values.SelectMany(v => v.Select(p => p.Arrival)).Append(ExactTime.Zero).Distinct().OrderBy(t => t).ToList();
            var stopped = new Dictionary<string, TravelMeeting>(StringComparer.Ordinal);
            var meetings = new List<TravelMeeting>();
            foreach (var time in times)
            {
                var links = new List<(string Node, string A, string B)>();
                foreach (var c in contacts)
                {
                    if (stopped.ContainsKey(c.FleetA) || stopped.ContainsKey(c.FleetB) || (!index[c.FleetA].Engage && !index[c.FleetB].Engage)) continue;
                    foreach (var a in visits[c.FleetA].Where(v => Occupies(v, time)))
                    foreach (var b in visits[c.FleetB].Where(v => v.Node == a.Node && Occupies(v, time)))
                        if (c.KnownContactNodes.Contains(a.Node, StringComparer.Ordinal)) links.Add((a.Node, c.FleetA, c.FleetB));
                }
                foreach (var group in links.GroupBy(l => l.Node, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
                {
                    // Connected hostile/contact components, not every fleet at the location.
                    var remaining = group.SelectMany(l => new[] { l.A, l.B }).ToHashSet(StringComparer.Ordinal);
                    while (remaining.Count > 0)
                    {
                        var component = new HashSet<string>(StringComparer.Ordinal) { remaining.OrderBy(s => s, StringComparer.Ordinal).First() };
                        bool changed;
                        do
                        {
                            changed = false;
                            foreach (var l in group)
                                if (component.Contains(l.A) || component.Contains(l.B))
                                { changed |= component.Add(l.A); changed |= component.Add(l.B); }
                        } while (changed);
                        var meeting = new TravelMeeting(group.Key, time, component);
                        meetings.Add(meeting);
                        foreach (var id in component) { stopped.Add(id, meeting); remaining.Remove(id); }
                    }
                }
            }
            var results = new List<JourneyResult>();
            foreach (var f in fleets)
            {
                if (stopped.TryGetValue(f.Fleet, out var meeting))
                { results.Add(new JourneyResult(f.Fleet, meeting.Node, null, ExactTime.Zero, true, Array.Empty<string>())); continue; }
                var elapsed = ExactTime.Zero; var node = f.Origin; var done = false;
                for (var i = 0; i < f.Legs.Count; i++)
                {
                    if (elapsed.Equals(ExactTime.One))
                    {
                        results.Add(new JourneyResult(f.Fleet, node, null, ExactTime.Zero, false, f.Legs.Skip(i).Select(l => l.Node)));
                        done = true; break; // next edge has not begun after the weekly budget is exhausted.
                    }
                    var paid = i == 0 ? f.InitialProgress : 0;
                    var duration = new ExactTime(f.Legs[i].Cost - paid, f.Speed);
                    if (elapsed + duration > ExactTime.One)
                    {
                        results.Add(new JourneyResult(f.Fleet, node, f.Legs[i].Node, (ExactTime.One - elapsed) * f.Speed + new ExactTime(paid, 1),
                            false, f.Legs.Skip(i).Select(l => l.Node)));
                        done = true; break;
                    }
                    elapsed += duration; node = f.Legs[i].Node;
                }
                if (!done) results.Add(new JourneyResult(f.Fleet, node, null, ExactTime.Zero, false, Array.Empty<string>()));
            }
            return new TravelComparison(meetings, results);
        }
    }
}
