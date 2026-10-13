using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.TacticalUxPrototype.Tests.Prototype;
using Xunit;

namespace HobbyDS9.TacticalUxPrototype.Tests
{
    public sealed class ChronologyRegressionTests
    {
        private sealed class Port : IPrototypeActions
        {
            public List<RoundIntent> Rounds { get; } = new List<RoundIntent>();
            public List<OperationIntent> Completions { get; } = new List<OperationIntent>();
            public void RecordRound(RoundIntent request) => Rounds.Add(request);
            public void RecordCommittedSave(OperationIntent request) { }
            public void RecordCompletion(OperationIntent request) => Completions.Add(request);
        }
        private static Boundary B(BoundaryStatus status = BoundaryStatus.Ready, int round = 1, long revision = 8,
            string encounter = "battle", string actor = "human", long ammo = 5)
            => new Boundary(encounter, actor, round, revision, status,
                new[] { new FleetOrder("A", Maneuver.Hold, "X") },
                new[] { new OwnShip("own", "A", "fixture", 90, 100, new long[] { 3, 13, 40, 5 },
                    new long[] { 40, 40, 40, 40 }, new[] { new OwnPackage("launcher.fixture", "Torpedo", true, ammo, 7, "Supplied readiness") }, false, true) },
                new[] { new VisibleShip("enemy", "X", "visible", 92, new long[] { 0, 5, 5, 0 }) },
                new[] { new ReportRow(1, 1, "observed report") }, new[] { "X" }, batchReady: true);
        private static (TacticalPresenter Ui, Port Port) Make(Boundary? b = null)
        { var port = new Port(); return (new TacticalPresenter(port, b ?? B()), port); }
        private static RoundIntent Send(TacticalPresenter ui, Port port)
        {
            Assert.True(ui.EditOrder("A", Maneuver.Screen, "X"));
            Assert.True(ui.ReviewRound()); Assert.True(ui.CommitReviewedRound());
            return Assert.Single(port.Rounds);
        }

        [Theory]
        [InlineData(BoundaryStatus.Ready, 1, 8)]
        [InlineData(BoundaryStatus.Ready, 1, 10)]
        [InlineData(BoundaryStatus.Ready, 2, 8)]
        [InlineData(BoundaryStatus.Ready, 2, 9)]
        [InlineData(BoundaryStatus.Waiting, 2, 9)]
        [InlineData(BoundaryStatus.Report, 2, 10)]
        public void RefreshCannotRegressOrReinterpretFrozenBoundary(BoundaryStatus status, int round, long revision)
        {
            var original = B(BoundaryStatus.Report, 2, 9);
            var (ui, port) = Make(original);
            Assert.True(ui.BeginNextRound()); Assert.True(ui.EditOrder("A", Maneuver.Screen, "X"));
            Assert.True(ui.InspectShip("own"));
            Assert.False(ui.Refresh(B(status, round, revision), 1));
            Assert.Same(original, ui.Observation); Assert.True(ui.Dirty);
            Assert.Equal(Maneuver.Screen, ui.OrderFor("A").Maneuver);
            Assert.Equal(ScreenState.Ready, ui.State); Assert.Equal("own", ui.SelectedShip);
            Assert.Empty(port.Rounds);
        }
        [Theory]
        [InlineData(BoundaryStatus.Ready, 4, 12)]
        [InlineData(BoundaryStatus.Waiting, 4, 12)]
        [InlineData(BoundaryStatus.Report, 4, 12)]
        [InlineData(BoundaryStatus.Ready, 3, 12)]
        [InlineData(BoundaryStatus.EncounterComplete, 2, 10)]
        public void CompletedEncounterCannotReopenOrRewind(BoundaryStatus status, int round, long revision)
        {
            var original = B(BoundaryStatus.EncounterComplete, 4, 11);
            var (ui, port) = Make(original);
            Assert.False(ui.Refresh(B(status, round, revision), 1));
            Assert.Same(original, ui.Observation); Assert.Equal(ScreenState.EncounterComplete, ui.State);
            Assert.Empty(port.Rounds);
        }
        [Theory]
        [InlineData(BoundaryStatus.Waiting, 1, 7)]
        [InlineData(BoundaryStatus.Waiting, 1, 9)]
        [InlineData(BoundaryStatus.Waiting, 2, 9)]
        [InlineData(BoundaryStatus.Report, 1, 8)]
        [InlineData(BoundaryStatus.Report, 2, 8)]
        [InlineData(BoundaryStatus.Report, 3, 10)]
        [InlineData(BoundaryStatus.Report, 4, 11)]
        [InlineData(BoundaryStatus.EncounterComplete, 1, 8)]
        [InlineData(BoundaryStatus.EncounterComplete, 2, 8)]
        [InlineData(BoundaryStatus.EncounterComplete, 4, 11)]
        public void MatchedImpossibleRoundReceiptIsHandledButNotApplied(BoundaryStatus status, int round, long revision)
        {
            var original = B(); var (ui, port) = Make(original); var sent = Send(ui, port);
            Assert.True(ui.ReceiveRound(1, sent.Request, B(status, round, revision)));
            Assert.Same(original, ui.Observation); Assert.True(ui.Dirty);
            Assert.Equal(ScreenState.RefreshRequired, ui.State);
            Assert.Equal(Maneuver.Screen, ui.OrderFor("A").Maneuver);
            Assert.False(ui.CommitReviewedRound()); Assert.Single(port.Rounds);
        }
        [Fact] public void PrivateWaitingCannotChangeFrozenPools()
        {
            var original = B(); var (ui, port) = Make(original); var sent = Send(ui, port);
            Assert.True(ui.ReceiveRound(1, sent.Request, B(BoundaryStatus.Waiting, ammo: 0)));
            Assert.Same(original, ui.Observation); Assert.True(ui.Dirty);
            Assert.Equal(ScreenState.RefreshRequired, ui.State); Assert.Empty(ui.AcknowledgedOrders);
        }
        [Theory]
        [InlineData("wrong", 4, 12)]
        [InlineData("battle", 4, 11)]
        [InlineData("battle", 3, 12)]
        [InlineData("battle", 5, 12)]
        public void WrongScopeOrImpossibleCompletionCannotShowSuccess(string encounter, int round, long revision)
        {
            var original = B(BoundaryStatus.EncounterComplete, 4, 11);
            var (ui, port) = Make(original);
            Assert.True(ui.ReviewCompletion()); Assert.True(ui.CompleteReviewedBatch());
            var sent = Assert.Single(port.Completions);
            Assert.True(ui.ReceiveCompletion(1, sent.Request, B(BoundaryStatus.Finalized, round, revision, encounter)));
            Assert.Same(original, ui.Observation); Assert.Equal(ScreenState.RefreshRequired, ui.State);
            Assert.DoesNotContain("PROTOTYPE completion;", ui.Notice);
            Assert.Single(port.Completions); Assert.Empty(port.Rounds);
        }
        [Fact] public void SameStampWaitingIsPrivateNotResolvedAndCannotRollBackToReady()
        {
            var (ui, port) = Make(); var sent = Send(ui, port);
            var waiting = B(BoundaryStatus.Waiting);
            ui.ReceiveRound(1, sent.Request, waiting);
            Assert.Equal(ScreenState.Waiting, ui.State); Assert.Single(ui.AcknowledgedOrders);
            Assert.False(ui.Refresh(B(), 1));
            Assert.Same(waiting, ui.Observation); Assert.Equal(Maneuver.Screen, ui.OrderFor("A").Maneuver);
        }
        [Theory]
        [InlineData(1, BoundaryStatus.Report, 2)]
        [InlineData(2, BoundaryStatus.Report, 3)]
        [InlineData(1, BoundaryStatus.EncounterComplete, 2)]
        [InlineData(2, BoundaryStatus.EncounterComplete, 3)]
        [InlineData(3, BoundaryStatus.EncounterComplete, 4)]
        public void OneResolvedRoundMayContinueOrCompleteEarly(int sentRound, BoundaryStatus status, int nextRound)
        {
            var (ui, port) = Make(B(round: sentRound, revision: 8)); var sent = Send(ui, port);
            var result = B(status, nextRound, 12); // Strictly newer, not a guessed +1 production revision.
            Assert.True(ui.ReceiveRound(1, sent.Request, result)); Assert.Same(result, ui.Observation);
            Assert.False(ui.Dirty); Assert.Single(port.Rounds);
            Assert.Equal(status == BoundaryStatus.Report ? ScreenState.Report : ScreenState.EncounterComplete, ui.State);
        }
        [Fact] public void ExplicitRefreshCanCatchUpButLaterDelayedReadCannotUndoIt()
        {
            var (ui, port) = Make(); var later = B(BoundaryStatus.Report, 3, 20, ammo: 1);
            Assert.True(ui.Refresh(later, 1)); Assert.Same(later, ui.Observation);
            Assert.False(ui.Refresh(B(BoundaryStatus.Report, 2, 19), 1)); Assert.Same(later, ui.Observation);
            Assert.Empty(port.Rounds);
        }
        [Fact] public void IdenticalSameCursorRefreshPreservesLocalReportAcknowledgmentAndDraft()
        {
            var (ui, port) = Make(B(BoundaryStatus.Report, 2, 9)); ui.BeginNextRound();
            ui.EditOrder("A", Maneuver.Screen, "X"); ui.ReviewRound();
            Assert.True(ui.Refresh(B(BoundaryStatus.Report, 2, 9), 1));
            Assert.True(ui.Dirty); Assert.Equal(ScreenState.Review, ui.State); Assert.Empty(port.Rounds);
        }
        [Fact] public void SameCursorRefreshCanShowOwnPrivateWaitingWithoutInventingCommitmentDetails()
        {
            var (ui, port) = Make(); ui.EditOrder("A", Maneuver.Screen, "X");
            Assert.True(ui.Refresh(B(BoundaryStatus.Waiting), 1));
            Assert.Equal(ScreenState.Waiting, ui.State); Assert.True(ui.Dirty);
            Assert.Empty(ui.AcknowledgedOrders); Assert.Contains("unsent Screen", string.Join("\n", ui.RenderText()));
            Assert.Contains("accepted commitment details absent", string.Join("\n", ui.RenderText()));
            Assert.Empty(port.Rounds);
        }
        [Fact] public void SameCursorPoolReplacementRejectedWithoutDroppingDraft()
        {
            var original = B(); var (ui, port) = Make(original); ui.EditOrder("A", Maneuver.Screen, "X");
            Assert.False(ui.Refresh(B(ammo: 0), 1));
            Assert.Same(original, ui.Observation); Assert.True(ui.Dirty); Assert.Empty(port.Rounds);
        }
        [Fact] public void EncounterNavigationUsesPerEncounterWatermark()
        {
            var original = B(BoundaryStatus.Report, 3, 10); var (ui, port) = Make(original);
            Assert.True(ui.NavigateEncounter(B(encounter: "other")));
            var other = ui.Observation;
            Assert.False(ui.NavigateEncounter(B())); Assert.Same(other, ui.Observation);
            Assert.True(ui.NavigateEncounter(B(BoundaryStatus.Report, 3, 10)));
            Assert.Equal(3, ui.Observation.Round); Assert.Empty(port.Rounds);
        }
        [Fact] public void ValidatedNewGenerationLoadMayIntentionallyRestoreOlderSavedBoundary()
        {
            var (ui, port) = Make(B(BoundaryStatus.Finalized, 4, 12));
            Assert.True(ui.TryReplaceSession(B(), 2, true, true));
            Assert.Equal(1, ui.Observation.Round); Assert.Equal(ScreenState.Ready, ui.State);
            Assert.False(ui.Refresh(B(BoundaryStatus.Report, 3, 20), 1)); Assert.Empty(port.Rounds);
        }
        [Theory]
        [InlineData(BoundaryStatus.Ready, 0, 8)]
        [InlineData(BoundaryStatus.Waiting, 4, 8)]
        [InlineData(BoundaryStatus.Report, 1, 8)]
        [InlineData(BoundaryStatus.EncounterComplete, 1, 8)]
        [InlineData(BoundaryStatus.Finalized, 5, 8)]
        [InlineData(BoundaryStatus.Ready, 1, -1)]
        public void ClaimedValidatedLoadCannotReplaceStateWithImpossiblePrototypeShape(BoundaryStatus status, int round, long revision)
        {
            var original = B(); var (ui, port) = Make(original); ui.EditOrder("A", Maneuver.Screen, "X");
            Assert.False(ui.TryReplaceSession(B(status, round, revision), 2, true, true));
            Assert.Same(original, ui.Observation); Assert.True(ui.Dirty); Assert.Empty(port.Rounds);
        }
        [Theory]
        [InlineData(BoundaryStatus.Ready, 3, 14)]
        [InlineData(BoundaryStatus.Report, 3, 14)]
        [InlineData(BoundaryStatus.EncounterComplete, 4, 14)]
        [InlineData(BoundaryStatus.Finalized, 4, 14)]
        public void FinalizedEncounterCannotReopenOrAcquireANewPrototypeFinalization(BoundaryStatus status, int round, long revision)
        {
            var original = B(BoundaryStatus.Finalized, 4, 12); var (ui, port) = Make(original);
            Assert.False(ui.Refresh(B(status, round, revision), 1));
            Assert.Same(original, ui.Observation); Assert.Equal(ScreenState.Finalized, ui.State); Assert.Empty(port.Rounds);
        }
        [Fact] public void MatchingCompletionMayFinalizeEarlyTerminalCursorExactlyOnce()
        {
            var (ui, port) = Make(B(BoundaryStatus.EncounterComplete, 2, 9));
            ui.ReviewCompletion(); ui.CompleteReviewedBatch(); var sent = Assert.Single(port.Completions);
            var finalized = B(BoundaryStatus.Finalized, 2, 12);
            Assert.True(ui.ReceiveCompletion(1, sent.Request, finalized)); Assert.Same(finalized, ui.Observation);
            Assert.False(ui.ReceiveCompletion(1, sent.Request, finalized)); Assert.Single(port.Completions);
        }
        [Fact] public void ExplicitRefreshCanConfirmSameEncounterFinalizationWithoutNewCommand()
        {
            var (ui, port) = Make(B(BoundaryStatus.EncounterComplete, 4, 11));
            Assert.True(ui.Refresh(B(BoundaryStatus.Finalized, 4, 12), 1));
            Assert.Equal(ScreenState.Finalized, ui.State); Assert.Empty(port.Completions);
        }
    }
}
