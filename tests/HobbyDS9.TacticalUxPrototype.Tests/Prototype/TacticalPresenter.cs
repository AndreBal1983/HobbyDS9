using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.UnityClient;

namespace HobbyDS9.TacticalUxPrototype.Tests.Prototype
{
    public sealed class TacticalPresenter
    {
        public const string Marker = "PROTOTYPE / TEST-OWNED DATA / CAMPAIGN V2 DISABLED";
        private readonly IPrototypeActions actions;
        private Boundary boundary;
        private readonly Dictionary<string, FleetOrder> drafts = new Dictionary<string, FleetOrder>(StringComparer.Ordinal);
        private long sequence;
        private string? pendingAction;
        private string? pendingSave;
        private IReadOnlyList<FleetOrder> sentOrders = Array.Empty<FleetOrder>();
        public IReadOnlyList<FleetOrder> AcknowledgedOrders { get; private set; } = Array.Empty<FleetOrder>();
        public long Generation { get; private set; }
        public ScreenState State { get; private set; }
        public string? SelectedFleet { get; private set; }
        public string? SelectedShip { get; private set; }
        public LayoutMode Layout { get; private set; } = LayoutMode.Standard;
        public string Notice { get; private set; } = "";
        public Boundary Observation => boundary; // Deep immutable test-owned model.
        public bool Dirty => drafts.Count != 0;
        public bool HasPending => pendingAction != null || pendingSave != null;
        public TacticalPresenter(IPrototypeActions actions, Boundary initial, long generation = 1)
        { this.actions = actions; boundary = initial; Generation = generation; State = StateFor(initial); }
        private static ScreenState StateFor(Boundary b) => b.Status switch
        {
            BoundaryStatus.Ready => b.Round >= 1 && b.Round <= 3 ? ScreenState.Ready : ScreenState.RefreshRequired,
            BoundaryStatus.Waiting => ScreenState.Waiting,
            BoundaryStatus.Report => ScreenState.Report,
            BoundaryStatus.EncounterComplete => ScreenState.EncounterComplete,
            BoundaryStatus.Finalized => ScreenState.Finalized,
            _ => ScreenState.RefreshRequired
        };
        public FleetOrder OrderFor(string fleet) => drafts.TryGetValue(fleet, out var changed)
            ? changed : State == ScreenState.Waiting && AcknowledgedOrders.Any(o => o.Fleet == fleet)
                ? AcknowledgedOrders.Single(o => o.Fleet == fleet) : boundary.StandingOrders.Single(o => o.Fleet == fleet);
        public bool SelectFleet(string fleet)
        {
            if (!boundary.StandingOrders.Any(o => o.Fleet == fleet)) return false;
            SelectedFleet = fleet; SelectedShip = null; return true;
        }
        public bool InspectShip(string ship)
        {
            if (!boundary.OwnShips.Any(s => s.Ship == ship) && !boundary.VisibleShips.Any(s => s.Ship == ship)) return false;
            SelectedShip = ship; return true;
        }
        public void Resize(double width, double height) => Layout = ResponsiveLayoutPolicy.ForViewport(width, height).Mode;
        public IReadOnlyList<ReportRow> ReplayReport() => boundary.Reports; // No clock/RNG/commands.
        public bool EditOrder(string fleet, Maneuver maneuver, string? focus)
        {
            if (State != ScreenState.Ready || HasPending || !Enum.IsDefined(typeof(Maneuver), maneuver)
                || !boundary.StandingOrders.Any(o => o.Fleet == fleet)) return false;
            var standing = boundary.StandingOrders.Single(o => o.Fleet == fleet);
            // Advisory picker guard only; NOT firing acquisition or campaign validation.
            if (focus != null && focus != standing.FocusFleet && !boundary.LegalFocusChoices.Contains(focus, StringComparer.Ordinal)) return false;
            var changed = new FleetOrder(fleet, maneuver, focus);
            if (standing.Maneuver == maneuver && standing.FocusFleet == focus) drafts.Remove(fleet);
            else drafts[fleet] = changed;
            return true;
        }
        public bool DiscardEdits()
        {
            if (HasPending || State != ScreenState.Ready) return false;
            drafts.Clear(); return true;
        }
        public bool ReviewRound()
        {
            if (State != ScreenState.Ready || HasPending || boundary.Round < 1 || boundary.Round > 3) return false;
            State = ScreenState.Review; return true;
        }
        public bool CancelReview()
        {
            if (State != ScreenState.Review || HasPending) return false;
            State = ScreenState.Ready; return true;
        }
        public bool CommitReviewedRound()
        {
            if (State != ScreenState.Review || HasPending) return false;
            var request = NextRequest();
            pendingAction = request; State = ScreenState.Sending; Notice = "";
            // Stage A requires FULL owned active-fleet orders, not just changed rows.
            var orders = boundary.StandingOrders.Select(o => OrderFor(o.Fleet)).ToArray();
            sentOrders = Array.AsReadOnly(orders);
            try { actions.RecordRound(new RoundIntent(Generation, request, boundary, orders)); }
            catch { pendingAction = null; State = ScreenState.RefreshRequired; Notice = "Ambiguous prototype send; refresh before any retry."; }
            return true;
        }
        public bool ReceiveRound(long generation, string request, Boundary? observed, string? rejection = null)
        {
            if (generation != Generation || pendingAction != request || State != ScreenState.Sending) return false;
            pendingAction = null;
            if (rejection != null || observed == null)
            { State = ScreenState.RefreshRequired; Notice = rejection ?? "Missing response; explicit refresh required."; return true; }
            if (observed.Actor != boundary.Actor || observed.Encounter != boundary.Encounter
                || observed.Status != BoundaryStatus.Waiting && observed.Status != BoundaryStatus.Report && observed.Status != BoundaryStatus.EncounterComplete)
            { State = ScreenState.RefreshRequired; Notice = "Response has wrong scope or no accepted commitment boundary."; return true; }
            AcknowledgedOrders = observed.Status == BoundaryStatus.Waiting ? sentOrders : Array.Empty<FleetOrder>();
            boundary = observed; drafts.Clear(); State = StateFor(observed); Notice = "Prototype response only; no campaign command was executed.";
            SelectedShip = null; ReconcileSelection(); return true;
        }
        public bool BeginNextRound()
        {
            if (State != ScreenState.Report || HasPending || boundary.Round > 3 || boundary.Round < 1) return false;
            State = ScreenState.Ready; return true; // Does not resolve.
        }
        public bool Refresh(Boundary observed, long generation)
        {
            if (HasPending || generation != Generation || observed.Actor != boundary.Actor || observed.Encounter != boundary.Encounter) return false;
            if (observed.Round != boundary.Round || observed.BoundaryRevision != boundary.BoundaryRevision)
            { drafts.Clear(); AcknowledgedOrders = Array.Empty<FleetOrder>(); Notice = "Boundary changed; obsolete unsent proposal discarded. Review fresh orders."; }
            boundary = observed;
            foreach (var fleet in drafts.Keys.Where(f => !observed.StandingOrders.Any(o => o.Fleet == f)).ToArray()) drafts.Remove(fleet);
            State = StateFor(observed); ReconcileSelection(); return true;
        }
        public bool NavigateEncounter(Boundary observed)
        {
            if (Dirty || HasPending || State == ScreenState.Review || State == ScreenState.CompletionReview
                || observed.Actor != boundary.Actor || observed.Encounter == boundary.Encounter) return false;
            boundary = observed; AcknowledgedOrders = Array.Empty<FleetOrder>(); State = StateFor(observed); SelectedFleet = SelectedShip = null; return true;
        }
        public bool SaveCommittedBoundary()
        {
            if (HasPending || State == ScreenState.RefreshRequired) return false;
            var request = NextRequest(); pendingSave = request;
            try { actions.RecordCommittedSave(new OperationIntent(Generation, request, boundary)); }
            catch { pendingSave = null; Notice = "Prototype save failed; committed boundary and unsent edits retained."; }
            return true;
        }
        public bool ReceiveSave(long generation, string request, bool success)
        {
            if (generation != Generation || pendingSave != request) return false;
            pendingSave = null;
            Notice = success ? "Committed prototype boundary saved; unsent edits stay local and are not in the save."
                : "Prototype save failed; prior save and local edits must be retained by the adapter.";
            return true;
        }
        public bool TryReplaceSession(Boundary loaded, long newGeneration, bool validated, bool discardConsent)
        {
            if (!validated || HasPending || newGeneration <= Generation || Dirty && !discardConsent) return false;
            boundary = loaded; Generation = newGeneration; drafts.Clear(); AcknowledgedOrders = Array.Empty<FleetOrder>(); State = StateFor(loaded);
            SelectedFleet = SelectedShip = null; Notice = "Prototype session replaced; no old local edits reattached."; return true;
        }
        public bool ReviewCompletion()
        {
            if (State != ScreenState.EncounterComplete || HasPending || boundary.BatchReady != true) return false;
            State = ScreenState.CompletionReview; return true;
        }
        public bool CompleteReviewedBatch()
        {
            if (State != ScreenState.CompletionReview || HasPending || boundary.BatchReady != true) return false;
            var request = NextRequest(); pendingAction = request; State = ScreenState.Completing;
            try { actions.RecordCompletion(new OperationIntent(Generation, request, boundary)); }
            catch { pendingAction = null; State = ScreenState.RefreshRequired; Notice = "Ambiguous prototype completion; refresh without re-resolving."; }
            return true;
        }
        public bool ReceiveCompletion(long generation, string request, Boundary? finalized, string? rejection = null)
        {
            if (generation != Generation || pendingAction != request || State != ScreenState.Completing) return false;
            pendingAction = null;
            if (rejection != null || finalized == null || finalized.Actor != boundary.Actor || finalized.Status != BoundaryStatus.Finalized)
            { State = ScreenState.RefreshRequired; Notice = rejection ?? "Finalization not confirmed; staged prototype data retained."; return true; }
            boundary = finalized; State = ScreenState.Finalized; Notice = "PROTOTYPE completion; live campaign v2 remains disabled."; return true;
        }
        private string NextRequest() => "ui-prototype." + checked(++sequence).ToString(CultureInfo.InvariantCulture);
        private void ReconcileSelection()
        {
            if (SelectedFleet != null && !boundary.StandingOrders.Any(o => o.Fleet == SelectedFleet)) SelectedFleet = null;
            if (SelectedShip != null && !boundary.OwnShips.Any(s => s.Ship == SelectedShip) && !boundary.VisibleShips.Any(s => s.Ship == SelectedShip)) SelectedShip = null;
        }
        public IReadOnlyList<string> RenderText()
        {
            var rows = new List<string> { Marker, "Screen: " + State,
                boundary.Round >= 1 && boundary.Round <= 3 ? "Round boundary: " + boundary.Round.ToString(CultureInfo.InvariantCulture)
                    : "No playable next round; encounter review/completion only",
                Dirty ? "Unsent local edits" : State == ScreenState.Waiting ? "Own commitment accepted / waiting" : "Retained committed orders",
                boundary.GeometryText ?? "DEPENDENCY: lawful geometry/range projection absent",
                boundary.ApproachCloak.HasValue ? "Test-owned approach setting: " + boundary.ApproachCloak : "DEPENDENCY: strategic approach setting projection absent" };
            if (boundary.VisibleShips.Count == 0) rows.Add("No visible enemy. Possible attack; proceed cautiously. Standing orders unchanged.");
            if (State == ScreenState.Waiting && AcknowledgedOrders.Count == 0)
                rows.Add("DEPENDENCY: own accepted commitment details absent after reload; do not infer them from standing orders.");
            foreach (var o in boundary.StandingOrders)
            {
                var current = OrderFor(o.Fleet);
                var label = State == ScreenState.Waiting ? AcknowledgedOrders.Count > 0 ? "acknowledged" : "previous standing (accepted detail missing)" : "draft/retained";
                rows.Add("Fleet " + o.Fleet + ": " + label + " " + current.Maneuver + "; order focus " + (current.FocusFleet ?? "none")
                    + "; committed " + o.Maneuver + "; standing focus " + (o.FocusFleet ?? "none") + "; screening is same-fleet only");
            }
            foreach (var s in boundary.OwnShips)
            {
                rows.Add("Own " + s.Ship + " " + s.Type + ": Hull " + N(s.Hull) + "/" + N(s.MaxHull) + "; " + Sectors(s.Shields, s.ShieldMaximum));
                rows.Add("Stage A toy aggregate ammo " + N(s.ToyAmmunition) + "/" + N(s.ToyMagazineCapacity) + "; dependency: production per-package pools");
                rows.Add("Own visibility: " + (s.Hidden ? "cloaked this boundary" : "visible") + "; no mid-battle re-cloak control");
            }
            foreach (var s in boundary.VisibleShips)
                rows.Add("Visible " + s.Ship + " " + s.Type + ": hull condition " + N(s.Hull) + "; " + Sectors(s.Shields, null) + "; reserves/orders not disclosed");
            foreach (var report in boundary.Reports) rows.Add("Report " + report.Sequence.ToString(CultureInfo.InvariantCulture) + ": " + report.Message);
            foreach (var encounter in boundary.Encounters)
                rows.Add("Test-owned encounter " + encounter.Encounter + ": " + (encounter.Complete ? "complete / staged" : "rounds remain"));
            rows.Add(boundary.BatchReady.HasValue ? "Test-owned batch ready: " + boundary.BatchReady : "DEPENDENCY: authoritative batch readiness absent");
            rows.Add(boundary.TravelStopped.HasValue ? "Test-owned travel stopped: " + boundary.TravelStopped + "; fresh next-turn orders required"
                : "DEPENDENCY: authoritative stopped travel / next-turn Planning projection absent");
            return rows.AsReadOnly();
        }
        private static string N(long value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Sectors(IReadOnlyList<long> values, IReadOnlyList<long>? maxima)
        {
            var names = new[] { "Fore", "Port", "Starboard", "Aft" };
            return string.Join("; ", names.Select((n, i) => n + " " + N(values[i]) + (maxima == null ? "" : "/" + N(maxima[i]))));
        }
    }
}
