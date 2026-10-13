using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.TacticalUxPrototype.Tests.Prototype
{
    // Test-only presentation contracts, NOT Application APIs or campaign state.
    public enum Maneuver { Hold = 1, Charge, Flank, Screen, FightingWithdrawal }
    public enum BoundaryStatus { Ready, Waiting, Report, EncounterComplete, Finalized }
    public enum ScreenState { Ready, Review, Sending, Waiting, Report, EncounterComplete, CompletionReview, Completing, Finalized, RefreshRequired }

    public sealed class FleetOrder
    {
        public string Fleet { get; }
        public Maneuver Maneuver { get; }
        public string? FocusFleet { get; }
        public FleetOrder(string fleet, Maneuver maneuver, string? focusFleet)
        { Fleet = fleet; Maneuver = maneuver; FocusFleet = focusFleet; }
    }
    // Optional supplied per-package facts. No class-to-stock or readiness inference.
    public sealed class OwnPackage
    {
        public string Package { get; }
        public string WeaponClass { get; }
        public bool? UsesPhysicalAmmunition { get; }
        public long? Ammunition { get; }
        public long? MagazineCapacity { get; }
        public string? ReadinessReason { get; }
        public OwnPackage(string package, string weaponClass, bool? physical, long? ammunition,
            long? capacity, string? readinessReason)
        { Package = package; WeaponClass = weaponClass; UsesPhysicalAmmunition = physical;
            Ammunition = ammunition; MagazineCapacity = capacity; ReadinessReason = readinessReason; }
    }
    public sealed class OwnShip
    {
        public string Ship { get; }
        public string Fleet { get; }
        public string Type { get; }
        public long Hull { get; }
        public long MaxHull { get; }
        public IReadOnlyList<long> Shields { get; }
        public IReadOnlyList<long> ShieldMaximum { get; }
        // Null means projection absent; empty means explicitly supplied no packages.
        public IReadOnlyList<OwnPackage>? Packages { get; }
        public bool Hidden { get; }
        public bool CloakCapable { get; }
        public OwnShip(string ship, string fleet, string type, long hull, long maxHull,
            IEnumerable<long> shields, IEnumerable<long> maximum, IEnumerable<OwnPackage>? packages, bool hidden, bool capable)
        {
            Ship = ship; Fleet = fleet; Type = type; Hull = hull; MaxHull = maxHull;
            Shields = SectorCopy(shields); ShieldMaximum = SectorCopy(maximum);
            Packages = packages == null ? null : Array.AsReadOnly(packages.ToArray()); Hidden = hidden; CloakCapable = capable;
        }
        internal static IReadOnlyList<long> SectorCopy(IEnumerable<long> source)
        {
            var copy = source.ToArray();
            if (copy.Length != 4) throw new ArgumentException("Four observed sectors required; do not fill missing data.");
            return Array.AsReadOnly(copy);
        }
    }
    // Privacy by construction: no reserve, fit, hidden flag, fresh-order or max-hull field.
    public sealed class VisibleShip
    {
        public string Ship { get; }
        public string Fleet { get; }
        public string Type { get; }
        public long Hull { get; }
        public IReadOnlyList<long> Shields { get; }
        public VisibleShip(string ship, string fleet, string type, long hull, IEnumerable<long> shields)
        { Ship = ship; Fleet = fleet; Type = type; Hull = hull; Shields = OwnShip.SectorCopy(shields); }
    }
    public sealed class ReportRow
    {
        public int Sequence { get; }
        public int Round { get; }
        public string Message { get; }
        // Only a pre-filtered actor-safe message; no raw checkpoint/event input.
        public ReportRow(int sequence, int round, string message)
        { Sequence = sequence; Round = round; Message = message; }
    }
    public sealed class EncounterSummary
    {
        public string Encounter { get; }
        public bool Complete { get; }
        public EncounterSummary(string encounter, bool complete) { Encounter = encounter; Complete = complete; }
    }
    public sealed class Boundary
    {
        public string Encounter { get; }
        public string Actor { get; }
        public int Round { get; }
        public long BoundaryRevision { get; }
        public BoundaryStatus Status { get; }
        public IReadOnlyList<FleetOrder> StandingOrders { get; }
        public IReadOnlyList<OwnShip> OwnShips { get; }
        public IReadOnlyList<VisibleShip> VisibleShips { get; }
        public IReadOnlyList<ReportRow> Reports { get; }
        // Extra test-owned future inputs, NOT supplied by Stage A Observe().
        public IReadOnlyList<string> LegalFocusChoices { get; }
        public IReadOnlyList<EncounterSummary> Encounters { get; }
        public bool? BatchReady { get; }
        public bool? TravelStopped { get; }
        public bool? ApproachCloak { get; }
        public string? GeometryText { get; }
        public Boundary(string encounter, string actor, int round, long revision, BoundaryStatus status,
            IEnumerable<FleetOrder> orders, IEnumerable<OwnShip> own, IEnumerable<VisibleShip> visible,
            IEnumerable<ReportRow>? reports = null, IEnumerable<string>? legalFocus = null,
            IEnumerable<EncounterSummary>? encounters = null, bool? batchReady = null,
            bool? travelStopped = null, bool? approachCloak = null, string? geometryText = null)
        {
            Encounter = encounter; Actor = actor; Round = round; BoundaryRevision = revision; Status = status;
            StandingOrders = Array.AsReadOnly(orders.ToArray()); OwnShips = Array.AsReadOnly(own.ToArray());
            VisibleShips = Array.AsReadOnly(visible.ToArray()); Reports = Array.AsReadOnly((reports ?? Array.Empty<ReportRow>()).ToArray());
            LegalFocusChoices = Array.AsReadOnly((legalFocus ?? Array.Empty<string>()).ToArray());
            Encounters = Array.AsReadOnly((encounters ?? Array.Empty<EncounterSummary>()).ToArray());
            BatchReady = batchReady; TravelStopped = travelStopped; ApproachCloak = approachCloak; GeometryText = geometryText;
        }
    }
    public sealed class RoundIntent
    {
        public long Generation { get; }
        public string Request { get; }
        public string Encounter { get; }
        public string Actor { get; }
        public int Round { get; }
        public long BoundaryRevision { get; }
        public IReadOnlyList<FleetOrder> Orders { get; }
        public RoundIntent(long generation, string request, Boundary boundary, IEnumerable<FleetOrder> orders)
        {
            Generation = generation; Request = request; Encounter = boundary.Encounter; Actor = boundary.Actor;
            Round = boundary.Round; BoundaryRevision = boundary.BoundaryRevision; Orders = Array.AsReadOnly(orders.ToArray());
        }
    }
    public sealed class OperationIntent
    {
        public long Generation { get; }
        public string Request { get; }
        public Boundary CommittedBoundary { get; }
        public OperationIntent(long generation, string request, Boundary boundary)
        { Generation = generation; Request = request; CommittedBoundary = boundary; }
    }
    public interface IPrototypeActions
    {
        // These are test recording seams. There is NO production adapter.
        void RecordRound(RoundIntent intent);
        void RecordCommittedSave(OperationIntent intent);
        void RecordCompletion(OperationIntent intent);
    }
}
