using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HobbyDS9.TacticalUxPrototype.Tests.Prototype;
using HobbyDS9.UnityClient;
using Xunit;

namespace HobbyDS9.TacticalUxPrototype.Tests
{
    public sealed class PresenterInteractionTests
    {
        private sealed class RecordingPort : IPrototypeActions
        {
            public List<RoundIntent> Rounds { get; } = new List<RoundIntent>();
            public List<OperationIntent> Saves { get; } = new List<OperationIntent>();
            public List<OperationIntent> Completions { get; } = new List<OperationIntent>();
            public bool ThrowRound { get; set; }
            public bool ThrowSave { get; set; }
            public bool ThrowCompletion { get; set; }
            public void RecordRound(RoundIntent intent) { Rounds.Add(intent); if (ThrowRound) throw new InvalidOperationException(); }
            public void RecordCommittedSave(OperationIntent intent) { Saves.Add(intent); if (ThrowSave) throw new InvalidOperationException(); }
            public void RecordCompletion(OperationIntent intent) { Completions.Add(intent); if (ThrowCompletion) throw new InvalidOperationException(); }
            public int Count => Rounds.Count + Saves.Count + Completions.Count;
        }
        private static OwnShip Own(string id = "own", string fleet = "A", bool hidden = false, long ammo = 5, long hull = 90)
            => new OwnShip(id, fleet, "authored fixture", hull, 100, new long[] { 3, 13, 40, 5 },
                new long[] { 40, 40, 40, 40 }, ammo, 7, hidden, true);
        private static VisibleShip Enemy(string ship = "enemy", string fleet = "X")
            => new VisibleShip(ship, fleet, "observed cruiser", 92, new long[] { 0, 5, 5, 0 });
        private static Boundary Snapshot(BoundaryStatus status = BoundaryStatus.Ready, int round = 1, long revision = 8,
            bool? ready = null, bool visible = true, bool hiddenOwn = false, string encounter = "encounter",
            string actor = "human", bool extraFleet = false, string? focus = "X", IEnumerable<ReportRow>? reports = null,
            long ammo = 5, bool? stopped = null)
        {
            var orders = new List<FleetOrder> { new FleetOrder("A", Maneuver.Hold, focus) };
            var own = new List<OwnShip> { Own(hidden: hiddenOwn, ammo: ammo) };
            if (extraFleet) { orders.Add(new FleetOrder("B", Maneuver.Screen, focus)); own.Add(Own("escort", "B")); }
            return new Boundary(encounter, actor, round, revision, status, orders, own,
                visible ? new[] { Enemy(), Enemy("other-enemy", "Y") } : Array.Empty<VisibleShip>(),
                reports, visible ? new[] { "X", "Y" } : Array.Empty<string>(),
                new[] { new EncounterSummary(encounter, status == BoundaryStatus.EncounterComplete), new EncounterSummary("second", ready == true) },
                ready, stopped);
        }
        private static (TacticalPresenter Ui, RecordingPort Port) Make(Boundary? snapshot = null)
        { var port = new RecordingPort(); return (new TacticalPresenter(port, snapshot ?? Snapshot()), port); }
        private static RoundIntent Send(TacticalPresenter ui, RecordingPort port)
        { Assert.True(ui.ReviewRound()); Assert.True(ui.CommitReviewedRound()); return Assert.Single(port.Rounds); }

        [Fact] public void MarkerAndUnknownDependenciesCannotAdvertisePlayableCampaign()
        {
            var (ui, port) = Make();
            var text = string.Join("\n", ui.RenderText());
            Assert.Contains(TacticalPresenter.Marker, text);
            Assert.Contains("DEPENDENCY: lawful geometry", text);
            Assert.Contains("DEPENDENCY: authoritative batch", text);
            Assert.Contains("DEPENDENCY: authoritative stopped travel", text);
            Assert.Equal(0, port.Count);
        }
        [Theory]
        [InlineData(1280, 720, LayoutMode.Compact)]
        [InlineData(1920, 1080, LayoutMode.Standard)]
        [InlineData(3440, 1440, LayoutMode.Wide)]
        public void SelectionResizeReplayAndRenderingDoNotSend(double width, double height, LayoutMode expected)
        {
            var report = new[] { new ReportRow(9, 1, "visible fallback Y; standing X retained") };
            var (ui, port) = Make(Snapshot(reports: report));
            Assert.True(ui.EditOrder("A", Maneuver.Flank, "Y"));
            Assert.True(ui.SelectFleet("A")); Assert.True(ui.InspectShip("enemy"));
            ui.Resize(width, height);
            for (var i = 0; i < 4; i++) { ui.ReplayReport(); ui.RenderText(); }
            Assert.Equal(expected, ui.Layout); Assert.True(ui.Dirty);
            Assert.Equal(Maneuver.Flank, ui.OrderFor("A").Maneuver);
            Assert.Equal(0, port.Count);
            Assert.Equal(9, Assert.Single(ui.ReplayReport()).Sequence);
        }
        [Fact] public void InspectionDoesNotCreatePerShipFireOrder()
        {
            var (ui, port) = Make();
            Assert.True(ui.InspectShip("enemy")); Assert.False(ui.InspectShip("hidden-enemy"));
            var intent = Send(ui, port);
            Assert.Equal("A", Assert.Single(intent.Orders).Fleet);
            Assert.DoesNotContain(typeof(RoundIntent).GetProperties(), p => p.Name.Contains("Ship", StringComparison.Ordinal));
            Assert.DoesNotContain(typeof(FleetOrder).GetProperties(), p => p.Name.Contains("Protect", StringComparison.Ordinal) || p.Name.Contains("Priority", StringComparison.Ordinal));
        }
        [Theory]
        [InlineData(0)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(99)]
        public void UnknownOrExitManeuverNeverCreatesIntent(int value)
        {
            var (ui, port) = Make();
            Assert.False(ui.EditOrder("A", (Maneuver)value, "X"));
            Assert.False(ui.Dirty); Assert.Equal(0, port.Count);
        }
        [Fact] public void HiddenUnknownAndAlliedFocusChoicesRejectWithoutSynthesis()
        {
            var (ui, port) = Make(Snapshot(extraFleet: true));
            foreach (var focus in new[] { "hidden", "missing", "A", "B" })
                Assert.False(ui.EditOrder("A", Maneuver.Charge, focus));
            Assert.False(ui.EditOrder("foreign", Maneuver.Screen, "X"));
            Assert.Equal(0, port.Count);
        }
        [Fact] public void RetainedUnavailableFocusCanPersistButNewHiddenFocusCannot()
        {
            var (ui, port) = Make(Snapshot(visible: false, focus: "previously-known"));
            Assert.True(ui.EditOrder("A", Maneuver.Screen, "previously-known"));
            Assert.False(ui.EditOrder("A", Maneuver.Screen, "new-hidden"));
            var intent = Send(ui, port);
            Assert.Equal("previously-known", Assert.Single(intent.Orders).FocusFleet);
        }
        [Fact] public void FullOwnedFleetReplacementRetainsUntouchedOrderFields()
        {
            var (ui, port) = Make(Snapshot(extraFleet: true));
            Assert.True(ui.EditOrder("A", Maneuver.Flank, "Y"));
            var intent = Send(ui, port);
            Assert.Equal(2, intent.Orders.Count);
            Assert.Equal(Maneuver.Flank, intent.Orders.Single(o => o.Fleet == "A").Maneuver);
            Assert.Equal(Maneuver.Screen, intent.Orders.Single(o => o.Fleet == "B").Maneuver);
            Assert.Equal("X", intent.Orders.Single(o => o.Fleet == "B").FocusFleet);
            Assert.Equal(8, intent.BoundaryRevision); Assert.Equal(1, intent.Round);
        }
        [Fact] public void SameFleetTransportDoesNotCreateAutomaticWingOrProtectionTarget()
        {
            var own = new[] { Own("transport"), Own("escort") };
            var boundary = new Boundary("encounter", "human", 1, 8, BoundaryStatus.Ready,
                new[] { new FleetOrder("A", Maneuver.Screen, null) }, own, Array.Empty<VisibleShip>());
            var (ui, port) = Make(boundary);
            var intent = Send(ui, port);
            Assert.Single(intent.Orders); Assert.Equal(2, ui.Observation.OwnShips.Count);
            Assert.All(ui.Observation.OwnShips, s => Assert.Equal("A", s.Fleet));
        }
        [Fact] public void EditBackToStandingRemovesUnsentFlag()
        {
            var (ui, port) = Make();
            Assert.True(ui.EditOrder("A", Maneuver.Screen, "Y")); Assert.True(ui.Dirty);
            Assert.True(ui.EditOrder("A", Maneuver.Hold, "X")); Assert.False(ui.Dirty);
            Assert.Equal(0, port.Count);
        }
        [Fact] public void ReviewAndCancelAreLocalAndDoNotDiscardDraft()
        {
            var (ui, port) = Make();
            ui.EditOrder("A", Maneuver.Screen, null);
            Assert.False(ui.CommitReviewedRound());
            Assert.True(ui.ReviewRound()); Assert.False(ui.EditOrder("A", Maneuver.Charge, null));
            Assert.True(ui.CancelReview()); Assert.True(ui.Dirty);
            Assert.Equal(0, port.Count);
        }
        [Fact] public void DuplicateCommitAndWaitingClicksCannotSendExtraRound()
        {
            var (ui, port) = Make();
            var intent = Send(ui, port);
            Assert.False(ui.CommitReviewedRound()); Assert.False(ui.ReviewRound());
            Assert.True(ui.ReceiveRound(1, intent.Request, Snapshot(BoundaryStatus.Waiting)));
            Assert.Equal(ScreenState.Waiting, ui.State);
            Assert.False(ui.BeginNextRound()); Assert.False(ui.CommitReviewedRound()); Assert.False(ui.ReviewRound());
            Assert.Single(port.Rounds);
            Assert.False(ui.ReceiveRound(1, intent.Request, Snapshot(BoundaryStatus.Report, 2, 9)));
        }
        [Fact] public void RejectionRetainsEditsAndNeverRetries()
        {
            var (ui, port) = Make(); ui.EditOrder("A", Maneuver.Flank, "Y");
            var intent = Send(ui, port);
            Assert.True(ui.ReceiveRound(1, intent.Request, null, "stale boundary"));
            Assert.Equal(ScreenState.RefreshRequired, ui.State); Assert.True(ui.Dirty);
            ui.RenderText(); ui.ReplayReport();
            Assert.False(ui.ReviewRound()); Assert.Single(port.Rounds);
            Assert.True(ui.Refresh(Snapshot(), 1)); Assert.True(ui.Dirty);
            Assert.True(ui.ReviewRound()); Assert.True(ui.CommitReviewedRound());
            Assert.Equal(2, port.Rounds.Count);
            Assert.NotEqual(port.Rounds[0].Request, port.Rounds[1].Request);
        }
        [Fact] public void AmbiguousSendRequiresRefreshAndDoesNotAssumeSuccess()
        {
            var (ui, port) = Make(); port.ThrowRound = true; ui.EditOrder("A", Maneuver.Screen, "X");
            Send(ui, port);
            Assert.Equal(ScreenState.RefreshRequired, ui.State); Assert.True(ui.Dirty);
            Assert.False(ui.ReviewRound()); Assert.False(ui.CommitReviewedRound()); Assert.Single(port.Rounds);
        }
        [Fact] public void WrongActorOrUncommittedResponseCannotUnblockAcceptedRound()
        {
            foreach (var response in new[] { Snapshot(actor: "foreign"), Snapshot(BoundaryStatus.Ready) })
            {
                var (ui, port) = Make(); ui.EditOrder("A", Maneuver.Flank, "Y");
                var intent = Send(ui, port);
                Assert.True(ui.ReceiveRound(1, intent.Request, response));
                Assert.Equal(ScreenState.RefreshRequired, ui.State); Assert.True(ui.Dirty);
            }
        }
        [Fact] public void ReportAcknowledgmentDoesNotResolveFutureHumanChoice()
        {
            var (ui, port) = Make(); var intent = Send(ui, port);
            Assert.True(ui.ReceiveRound(1, intent.Request, Snapshot(BoundaryStatus.Report, 2, 9, ammo: 3)));
            Assert.Equal(3, Assert.Single(ui.Observation.OwnShips).ToyAmmunition);
            Assert.Equal(ScreenState.Report, ui.State);
            Assert.False(ui.CommitReviewedRound());
            Assert.True(ui.BeginNextRound()); Assert.Single(port.Rounds);
            Assert.Equal(ScreenState.Ready, ui.State);
        }
        [Fact] public void RoundFourCannotBeReviewedOrAdvanced()
        {
            var (ui, port) = Make(Snapshot(round: 4));
            Assert.False(ui.ReviewRound()); Assert.False(ui.BeginNextRound()); Assert.Equal(0, port.Count);
            Assert.DoesNotContain("Round boundary: 4", string.Join("\n", ui.RenderText()));
        }
        [Fact] public void FocusFallbackTextDoesNotRewriteStandingIntentOrConsumePools()
        {
            var rows = new[] { new ReportRow(20, 1, "Focus X unavailable; observed Y acquired. Standing X retained.") };
            var (ui, port) = Make(Snapshot(BoundaryStatus.Report, 2, 9, focus: "X", reports: rows, ammo: 1));
            var json = JsonSerializer.Serialize(ui.Observation);
            for (var i = 0; i < 5; i++) Assert.Contains("Standing X retained", string.Join("\n", ui.RenderText()));
            Assert.Equal("X", ui.OrderFor("A").FocusFleet);
            Assert.Equal(json, JsonSerializer.Serialize(ui.Observation)); Assert.Equal(0, port.Count);
        }
        [Theory] [InlineData(false)] [InlineData(true)]
        public void FullyAndMutuallyCloakedViewsShowCautionWithoutHiddenEnemyNames(bool ownHidden)
        {
            var (ui, port) = Make(Snapshot(visible: false, hiddenOwn: ownHidden, focus: null));
            var text = string.Join("\n", ui.RenderText());
            Assert.Contains("No visible enemy", text);
            Assert.DoesNotContain("enemy", text.Replace("No visible enemy", ""));
            Assert.Equal(Maneuver.Hold, ui.OrderFor("A").Maneuver);
            Assert.Empty(ui.Observation.LegalFocusChoices); Assert.Equal(0, port.Count);
        }
        [Fact] public void MixedCloakOwnMembersStayOneUnitAndVisibleEnemyModelHasNoHiddenSlots()
        {
            var own = new[] { Own("cloaked", hidden: true), Own("visible-transport", hidden: false) };
            var b = new Boundary("encounter", "human", 1, 8, BoundaryStatus.Ready,
                new[] { new FleetOrder("A", Maneuver.Hold, "X") }, own, new[] { Enemy() });
            var (ui, port) = Make(b);
            Assert.Single(Send(ui, port).Orders);
            Assert.Equal(2, ui.Observation.OwnShips.Count);
            var properties = typeof(VisibleShip).GetProperties().Select(p => p.Name).ToArray();
            Assert.Equal(new[] { "Ship", "Fleet", "Type", "Hull", "Shields" }, properties);
            Assert.DoesNotContain("Ammunition", JsonSerializer.Serialize(b.VisibleShips));
            Assert.DoesNotContain("Orders", JsonSerializer.Serialize(b.VisibleShips));
        }
        [Fact] public void RevealAndStockChangesOnlyArriveThroughInjectedObservedBoundary()
        {
            var (ui, port) = Make(Snapshot(visible: false, hiddenOwn: true, focus: null, ammo: 7));
            ui.ReplayReport(); ui.Resize(3440, 1440); ui.RenderText();
            Assert.True(Assert.Single(ui.Observation.OwnShips).Hidden);
            Assert.Equal(7, Assert.Single(ui.Observation.OwnShips).ToyAmmunition);
            var intent = Send(ui, port);
            ui.ReceiveRound(1, intent.Request, Snapshot(BoundaryStatus.Report, 2, 9, ammo: 5,
                reports: new[] { new ReportRow(1, 1, "Incoming torpedo intercepted; launcher not visible at that event.") }));
            Assert.False(Assert.Single(ui.Observation.OwnShips).Hidden);
            Assert.Equal(5, Assert.Single(ui.Observation.OwnShips).ToyAmmunition);
            Assert.Contains("launcher not visible", Assert.Single(ui.ReplayReport()).Message);
            Assert.DoesNotContain("hidden-launcher-id", string.Join("\n", ui.RenderText()));
        }
        [Theory] [InlineData(false)] [InlineData(true)]
        public void SavingCommittedBoundaryPreservesUnsentEditsForBothOutcomes(bool success)
        {
            var (ui, port) = Make(); ui.EditOrder("A", Maneuver.Flank, "Y");
            Assert.True(ui.SaveCommittedBoundary()); Assert.False(ui.SaveCommittedBoundary());
            var save = Assert.Single(port.Saves);
            var json = JsonSerializer.Serialize(save.CommittedBoundary);
            Assert.DoesNotContain("Flank", json); Assert.DoesNotContain("draft", json, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(Maneuver.Hold, Assert.Single(save.CommittedBoundary.StandingOrders).Maneuver);
            Assert.True(ui.ReceiveSave(1, save.Request, success)); Assert.True(ui.Dirty);
            Assert.Equal(Maneuver.Flank, ui.OrderFor("A").Maneuver);
            Assert.Empty(port.Rounds);
        }
        [Fact] public void SaveAdapterExceptionDoesNotDiscardEditsOrFakeSuccess()
        {
            var (ui, port) = Make(); ui.EditOrder("A", Maneuver.Screen, null); port.ThrowSave = true;
            ui.SaveCommittedBoundary();
            Assert.True(ui.Dirty); Assert.False(ui.HasPending);
            Assert.Contains("failed", ui.Notice); Assert.Empty(port.Rounds);
        }
        [Fact] public void SaveCommitAndSessionReplacementCannotRace()
        {
            var (ui, port) = Make(); var intent = Send(ui, port);
            Assert.False(ui.SaveCommittedBoundary());
            Assert.False(ui.TryReplaceSession(Snapshot(), 2, true, true));
            ui.ReceiveRound(1, intent.Request, Snapshot(BoundaryStatus.Waiting));
            Assert.True(ui.SaveCommittedBoundary());
            Assert.False(ui.TryReplaceSession(Snapshot(), 2, true, true));
            var save = Assert.Single(port.Saves); ui.ReceiveSave(1, save.Request, true);
            Assert.True(ui.TryReplaceSession(Snapshot(), 2, true, true));
        }
        [Fact] public void SuccessfulLoadDropsDraftsEvenWithIdenticalIdsAndStamps()
        {
            var (ui, port) = Make(); ui.EditOrder("A", Maneuver.Screen, "Y"); ui.InspectShip("own"); ui.SelectFleet("A");
            Assert.False(ui.TryReplaceSession(Snapshot(), 2, true, false));
            Assert.False(ui.TryReplaceSession(Snapshot(), 2, false, true));
            Assert.True(ui.Dirty);
            Assert.True(ui.TryReplaceSession(Snapshot(), 2, true, true));
            Assert.False(ui.Dirty); Assert.Equal(Maneuver.Hold, ui.OrderFor("A").Maneuver);
            Assert.Null(ui.SelectedFleet); Assert.Null(ui.SelectedShip); Assert.Equal(0, port.Count);
        }
        [Fact] public void ReceiptFromPreviousGenerationOrWrongRequestHasNoEffect()
        {
            var (ui, port) = Make(); var intent = Send(ui, port);
            Assert.False(ui.ReceiveRound(2, intent.Request, Snapshot(BoundaryStatus.Waiting)));
            Assert.False(ui.ReceiveRound(1, "wrong", Snapshot(BoundaryStatus.Waiting)));
            Assert.Equal(ScreenState.Sending, ui.State);
            ui.ReceiveRound(1, intent.Request, Snapshot(BoundaryStatus.Waiting));
            ui.TryReplaceSession(Snapshot(), 2, true, true);
            Assert.False(ui.ReceiveRound(1, intent.Request, Snapshot(BoundaryStatus.Report, 2, 9)));
            Assert.Equal(1, ui.Observation.Round);
        }
        [Fact] public void ChangedBoundaryRequiresFreshReviewAndDropsObsoleteProposal()
        {
            var (ui, port) = Make(); ui.EditOrder("A", Maneuver.Flank, "Y");
            Assert.True(ui.Refresh(Snapshot(round: 2, revision: 9), 1));
            Assert.False(ui.Dirty); Assert.Contains("Boundary changed", ui.Notice);
            Assert.False(ui.CommitReviewedRound()); Assert.Equal(0, port.Count);
        }
        [Fact] public void EncounterNavigationNeverSendsOrCarriesDraftToAnotherEncounter()
        {
            var (ui, port) = Make(); ui.EditOrder("A", Maneuver.Flank, "Y");
            Assert.False(ui.NavigateEncounter(Snapshot(encounter: "second")));
            ui.DiscardEdits();
            Assert.True(ui.NavigateEncounter(Snapshot(encounter: "second")));
            Assert.Equal("second", ui.Observation.Encounter);
            Assert.Equal(Maneuver.Hold, ui.OrderFor("A").Maneuver); Assert.Equal(0, port.Count);
        }
        [Theory] [InlineData(null)] [InlineData(false)]
        public void AbsentOrUnreadyBatchCannotFinalize(bool? ready)
        {
            var (ui, port) = Make(Snapshot(BoundaryStatus.EncounterComplete, 4, 11, ready));
            Assert.False(ui.ReviewCompletion()); Assert.False(ui.CompleteReviewedBatch()); Assert.Equal(0, port.Count);
        }
        [Fact] public void CompletionReviewDuplicateClicksAndLaterFailureRetainStagedReports()
        {
            var report = new[] { new ReportRow(4, 3, "survivors retain depleted stocks") };
            var (ui, port) = Make(Snapshot(BoundaryStatus.EncounterComplete, 4, 11, true, reports: report, ammo: 0));
            Assert.True(ui.ReviewCompletion()); Assert.True(ui.CompleteReviewedBatch());
            Assert.False(ui.CompleteReviewedBatch());
            var operation = Assert.Single(port.Completions);
            ui.ReceiveCompletion(1, operation.Request, null, "later encounter validation failed");
            Assert.Equal(ScreenState.RefreshRequired, ui.State);
            Assert.Equal(0, Assert.Single(ui.Observation.OwnShips).ToyAmmunition);
            Assert.Single(ui.ReplayReport()); Assert.Empty(port.Rounds);
        }
        [Fact] public void AcceptedPrototypeCompletionDisplaysStopAndNewOrdersWithoutAdvancingTurn()
        {
            var (ui, port) = Make(Snapshot(BoundaryStatus.EncounterComplete, 4, 11, true, ammo: 0));
            ui.ReviewCompletion(); ui.CompleteReviewedBatch();
            var operation = Assert.Single(port.Completions);
            Assert.True(ui.ReceiveCompletion(1, operation.Request, Snapshot(BoundaryStatus.Finalized, 4, 12, true, ammo: 0, stopped: true)));
            Assert.False(ui.ReceiveCompletion(1, operation.Request, Snapshot(BoundaryStatus.Finalized)));
            Assert.Contains("fresh next-turn orders required", string.Join("\n", ui.RenderText()));
            Assert.Contains("CAMPAIGN V2 DISABLED", string.Join("\n", ui.RenderText()));
            Assert.False(ui.ReviewRound()); Assert.False(ui.CompleteReviewedBatch());
            Assert.Single(port.Completions); Assert.Empty(port.Rounds);
        }
        [Fact] public void CompletionExceptionRequiresRefreshInsteadOfPartialUiSuccess()
        {
            var (ui, port) = Make(Snapshot(BoundaryStatus.EncounterComplete, 4, 11, true));
            port.ThrowCompletion = true; ui.ReviewCompletion(); ui.CompleteReviewedBatch();
            Assert.Equal(ScreenState.RefreshRequired, ui.State);
            Assert.Equal(BoundaryStatus.EncounterComplete, ui.Observation.Status);
            Assert.Single(port.Completions);
        }
        [Fact] public void RepeatedViewsAndSubsequentTestEncounterCannotRefillDepletedPools()
        {
            var (ui, port) = Make(Snapshot(BoundaryStatus.EncounterComplete, 4, 11, true, ammo: 0));
            var before = JsonSerializer.Serialize(ui.Observation);
            for (var i = 0; i < 3; i++) { ui.RenderText(); ui.ReplayReport(); ui.Resize(1280, 720); }
            Assert.Equal(before, JsonSerializer.Serialize(ui.Observation));
            Assert.True(ui.NavigateEncounter(Snapshot(encounter: "next-turn-test-owned", ammo: 0)));
            Assert.Equal(0, Assert.Single(ui.Observation.OwnShips).ToyAmmunition);
            Assert.Equal(new long[] { 3, 13, 40, 5 }, Assert.Single(ui.Observation.OwnShips).Shields);
            Assert.Equal(0, port.Count);
        }
        [Fact] public void DetachedSectorAndListInputsCannotMutateCapturedObservation()
        {
            var shields = new long[] { 1, 2, 3, 4 };
            var ship = new VisibleShip("enemy", "X", "visible", 92, shields);
            var list = new List<VisibleShip> { ship };
            var b = new Boundary("encounter", "human", 1, 8, BoundaryStatus.Ready,
                new[] { new FleetOrder("A", Maneuver.Hold, null) }, new[] { Own() }, list);
            shields[0] = 999; list.Clear();
            Assert.Equal(1, Assert.Single(b.VisibleShips).Shields[0]);
            Assert.Throws<NotSupportedException>(() => ((IList<long>)ship.Shields)[0] = 500);
        }
        [Fact] public void MissingSectorFieldsAreDependenciesNeverFilledToFour()
        {
            Assert.Throws<ArgumentException>(() => new VisibleShip("e", "X", "type", 90, new long[] { 1, 2 }));
        }
        [Fact] public void RenderingIsCultureIndependentAndDoesNotClampInt64Pools()
        {
            var old = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                var ship = new OwnShip("huge", "A", "fixture", long.MaxValue, long.MaxValue,
                    Enumerable.Repeat(long.MaxValue, 4), Enumerable.Repeat(long.MaxValue, 4), long.MaxValue, long.MaxValue, false, false);
                var b = new Boundary("e", "human", 1, 8, BoundaryStatus.Ready,
                    new[] { new FleetOrder("A", Maneuver.Hold, null) }, new[] { ship }, Array.Empty<VisibleShip>());
                var (ui, port) = Make(b);
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("nl-NL");
                Assert.Contains(long.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture), string.Join("\n", ui.RenderText()));
                Assert.Equal(0, port.Count);
            }
            finally { System.Globalization.CultureInfo.CurrentCulture = old; }
        }
        [Fact] public void PrototypeAssemblyDoesNotReferenceAnyProductionOrStageAAssembly()
        {
            var names = typeof(TacticalPresenter).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
            Assert.DoesNotContain(names, n => n != null && (n.StartsWith("HobbyDS9", StringComparison.Ordinal) || n.StartsWith("Unity", StringComparison.Ordinal)));
        }
        [Fact] public void AcknowledgedWaitingOrderIsDistinctFromUnsentAndOldStandingOrder()
        {
            var (ui, port) = Make(); ui.EditOrder("A", Maneuver.Screen, "Y");
            var intent = Send(ui, port); ui.ReceiveRound(1, intent.Request, Snapshot(BoundaryStatus.Waiting));
            Assert.False(ui.Dirty); Assert.Equal(Maneuver.Screen, ui.OrderFor("A").Maneuver);
            Assert.Equal("Y", Assert.Single(ui.AcknowledgedOrders).FocusFleet);
            Assert.Equal(Maneuver.Hold, Assert.Single(ui.Observation.StandingOrders).Maneuver);
            Assert.Contains("Own commitment accepted / waiting", string.Join("\n", ui.RenderText()));
            Assert.Single(port.Rounds);
        }
        [Fact] public void ReloadedWaitingDoesNotManufactureMissingAcceptedOrderDetails()
        {
            var (ui, port) = Make(Snapshot(BoundaryStatus.Waiting));
            Assert.Empty(ui.AcknowledgedOrders);
            Assert.Contains("DEPENDENCY: own accepted commitment details absent", string.Join("\n", ui.RenderText()));
            Assert.False(ui.ReviewRound()); Assert.Equal(0, port.Count);
        }
        [Fact] public void OwnAcknowledgmentIsDiscardedOnReplacementEvenWithIdenticalIdentity()
        {
            var (ui, port) = Make(); ui.EditOrder("A", Maneuver.Screen, "Y");
            var intent = Send(ui, port); ui.ReceiveRound(1, intent.Request, Snapshot(BoundaryStatus.Waiting));
            Assert.True(ui.TryReplaceSession(Snapshot(BoundaryStatus.Waiting), 2, true, true));
            Assert.Empty(ui.AcknowledgedOrders);
            Assert.Contains("DEPENDENCY: own accepted commitment details absent", string.Join("\n", ui.RenderText()));
        }
        [Fact] public void PendingSaveAndWrongSaveReceiptCannotEnableRoundCommit()
        {
            var (ui, port) = Make(); ui.EditOrder("A", Maneuver.Flank, "Y"); ui.ReviewRound(); ui.SaveCommittedBoundary();
            var save = Assert.Single(port.Saves);
            Assert.False(ui.CommitReviewedRound()); Assert.False(ui.ReceiveSave(2, save.Request, true));
            Assert.False(ui.CommitReviewedRound()); Assert.True(ui.ReceiveSave(1, save.Request, false));
            Assert.True(ui.Dirty); Assert.True(ui.CommitReviewedRound()); Assert.Single(port.Rounds);
        }
        [Fact] public void HiddenDataCannotEnterRoundIntentThroughReadOnlyInspection()
        {
            var (ui, port) = Make(); ui.InspectShip("other-enemy"); ui.RenderText();
            var intent = Send(ui, port); var json = JsonSerializer.Serialize(intent);
            foreach (var field in new[] { "OwnShips", "VisibleShips", "Hull", "Shields", "Ammunition", "Geometry", "Reports", "Seed", "Results" })
                Assert.DoesNotContain(field, json);
        }
        [Fact] public void UnknownGeometryBatchTravelAndApproachRemainAbsentRatherThanDefaulted()
        {
            var (ui, port) = Make();
            Assert.Null(ui.Observation.GeometryText); Assert.Null(ui.Observation.BatchReady);
            Assert.Null(ui.Observation.TravelStopped); Assert.Null(ui.Observation.ApproachCloak);
            Assert.Equal(0, port.Count);
        }
        [Fact] public void ReportOrderingRetainsProvidedSequenceWithoutReinterpretation()
        {
            var rows = new[] { new ReportRow(7, 1, "incoming observed"), new ReportRow(10, 1, "revealed at boundary") };
            var (ui, port) = Make(Snapshot(BoundaryStatus.Report, 2, 9, reports: rows));
            Assert.Equal(new[] { 7, 10 }, ui.ReplayReport().Select(r => r.Sequence));
            Assert.Equal(0, port.Count);
        }
        [Fact] public void CompleteEncounterRowsCannotInventAuthoritativeBatchReadiness()
        {
            var b = new Boundary("encounter", "human", 4, 11, BoundaryStatus.EncounterComplete,
                new[] { new FleetOrder("A", Maneuver.Hold, "X") }, new[] { Own(ammo: 0) }, new[] { Enemy() },
                encounters: new[] { new EncounterSummary("encounter", true), new EncounterSummary("second", true) });
            var (ui, port) = Make(b);
            Assert.Contains("Test-owned encounter second: complete / staged", string.Join("\n", ui.RenderText()));
            Assert.False(ui.ReviewCompletion()); Assert.Equal(0, port.Count);
        }
    }
}
