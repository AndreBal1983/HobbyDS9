using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class SaveLibraryPresentationTests
{
    private static SaveLibraryRow Row(string id = "ref.1", long generation = 3, SaveLibraryHealth health = SaveLibraryHealth.Validated, string name = "帰還 — α 🚀", SaveLibraryKind kind = SaveLibraryKind.Manual)
        => new(id, "campaign.a", "branch.a", "checkpoint." + id, generation, 9, name, "Federation", "Homeward", "Date not reported", new(146, 2), kind, health, true);
    private static SaveLibraryView View(IEnumerable<SaveLibraryRow>? rows = null, long generation = 10, string epoch = "session.a", SaveLibraryOperation? operation = null, SaveLibraryRecovery? recovery = null)
        => new(epoch, generation, "branch.a", 9, "Federation · Homeward", new(148, 6), new(146, 2), SaveLibraryDurability.Unsaved,
            new(), rows ?? new[] { Row() }, null, "opaque:%2F日本語\0next", "1–25 of 348", SaveLibraryPageState.Ready,
            operation ?? new(SaveLibraryOperationPhase.None, SaveLibraryOperationReason.Auto, null, "", true), recovery);

    [Fact] public void LoadingReviewsTheExactTargetAndCurrentProgressBeforeReplacingTheSession()
    {
        var model = new SaveLibraryPresentation(View()); var boundary = model.BeginRender();
        Assert.True(model.TryPrepare(boundary, SaveLibraryAction.Load, "ref.1", "", out var load));
        Assert.True(load!.RequiresConfirmation); Assert.Equal(148, load.ReviewedCurrent.Turn);
        Assert.Equal(146, load.ReviewedLastDurable.Turn); Assert.Equal("ref.1", load.Target!.ReferenceId);
        Assert.True(model.TryConfirm(load)); Assert.True(model.TryPublish(boundary, View(generation: 11)));
        Assert.False(model.TryConfirm(load));
    }
    [Fact] public void DuplicateUnicodeNamesSelectOnlyStableIdentityAndCopyThePage()
    {
        var input = new List<SaveLibraryRow> { Row("ref.1"), Row("ref.2") }; var model = new SaveLibraryPresentation(View(input));
        input.Clear(); var boundary = model.BeginRender();
        Assert.Equal(2, model.View.Rows.Count);
        Assert.True(model.TryPrepare(boundary, SaveLibraryAction.Delete, "ref.2", "", out var intent));
        Assert.Equal("帰還 — α 🚀", intent!.Target!.Name); Assert.Equal("ref.2", intent.Target.ReferenceId);
        Assert.Equal(3, intent.Target.ReferenceGeneration); Assert.Equal(9, intent.Target.BranchGeneration);
        Assert.Equal("checkpoint.ref.2", intent.Target.CheckpointId); Assert.Equal("branch.a", intent.ActiveBranchId);
        Assert.True(model.TryConfirm(intent));
        Assert.False(model.TryPrepare(boundary, SaveLibraryAction.Delete, intent.Target.Name, "", out _));
    }

    [Fact] public void OpaqueCursorIsUnchangedAndOnlyTheOutstandingRequestCanPublish()
    {
        var model = new SaveLibraryPresentation(View()); var rendered = model.BeginRender();
        var query = new SaveLibraryQuery("星 <b>save</b>", SaveLibraryKind.Milestone, SaveLibraryHealth.Corrupt, true, 25, model.View.NextCursor);
        Assert.True(model.TryQuery(rendered, query, out var request));
        Assert.Equal("opaque:%2F日本語\0next", request!.Query.Cursor); Assert.Equal(query.Search, request.Query.Search);
        Assert.False(model.TryQuery(rendered, query, out _));
        Assert.False(model.TryPublish(rendered, View(generation: 11)));
        Assert.True(model.TryPublish(request.Boundary, View(new[] { Row("next") }, generation: 11)));
        Assert.False(model.TryPublish(request.Boundary, View(generation: 12)));
        Assert.Equal("next", model.View.Rows[0].ReferenceId);
        Assert.Null(query.AtCursor(null).Cursor); Assert.Equal(SaveLibraryKind.Milestone, query.AtCursor(null).Kind);
    }

    [Fact] public void ReplacingGenerationOrRerenderingInvalidatesReviewedTargetsAndDetachedControls()
    {
        var model = new SaveLibraryPresentation(View()); var first = model.BeginRender();
        Assert.True(model.TryPrepare(first, SaveLibraryAction.Overwrite, "ref.1", "", out var overwrite));
        Assert.True(model.TryPublish(first, View(new[] { Row(generation: 4) }, generation: 11)));
        Assert.False(model.TryConfirm(overwrite!));
        var current = model.BeginRender(); Assert.False(model.TryPrepare(first, SaveLibraryAction.Load, "ref.1", "", out _));
        Assert.True(model.TryPrepare(current, SaveLibraryAction.Rename, "ref.1", "新しい名前", out var rename));
        model.BeginRender(); Assert.False(model.TryConfirm(rename!));
    }

    [Fact] public void OldSessionRetirementAndRegressingCatalogueCannotAffectReplacement()
    {
        var model = new SaveLibraryPresentation(View()); var boundary = model.BeginRender();
        Assert.False(model.TryPublish(boundary, View(epoch: "replacement.session")));
        Assert.False(model.TryPublish(boundary, View(generation: 9)));
        Assert.True(model.TryPrepare(boundary, SaveLibraryAction.Delete, "ref.1", "", out var intent));
        model.Retire();
        Assert.False(model.TryConfirm(intent!)); Assert.False(model.TryPublish(boundary, View(generation: 11)));
        Assert.False(model.TryQuery(model.BeginRender(), new(), out _));
    }

    [Fact] public void ReplacementPanelRejectsOldBoundaryEvenWhenEpochAndGenerationsMatch()
    {
        var old = new SaveLibraryPresentation(View()); var replacement = new SaveLibraryPresentation(View());
        var oldBoundary = old.BeginRender(); var newBoundary = replacement.BeginRender();
        Assert.Equal(oldBoundary.RenderToken, newBoundary.RenderToken);
        Assert.False(replacement.TryPublish(oldBoundary, View(generation: 11)));
        Assert.False(replacement.TryPrepare(oldBoundary, SaveLibraryAction.Delete, "ref.1", "", out _));
        Assert.True(replacement.TryPrepare(newBoundary, SaveLibraryAction.Delete, "ref.1", "", out _));
    }

    [Fact] public void UnknownCommitOffersReceiptCheckAndNeverCallsItFailedOrAllowsNewOverwrite()
    {
        var op = new SaveLibraryOperation(SaveLibraryOperationPhase.OutcomeUnknown, SaveLibraryOperationReason.SaveAndQuit, "same.operation.日本語", "Receipt lookup pending", true);
        var model = new SaveLibraryPresentation(View(operation: op)); var boundary = model.BeginRender();
        Assert.Equal("Save status needs checking", op.Heading);
        Assert.False(model.Can(SaveLibraryAction.RetryExistingOperation)); Assert.False(model.Can(SaveLibraryAction.QuitWithoutSaving));
        Assert.False(model.TryPrepare(boundary, SaveLibraryAction.Overwrite, "ref.1", "", out _));
        Assert.True(model.TryPrepare(boundary, SaveLibraryAction.CheckOutcome, null, "", out var check));
        Assert.Equal("same.operation.日本語", check!.ExistingOperationId); Assert.True(model.Can(SaveLibraryAction.ReturnToSession));
        Assert.True(model.Can(SaveLibraryAction.ExportSession)); Assert.True(model.Can(SaveLibraryAction.StorageHelp));
        Assert.Equal(SaveLibraryDurability.Unsaved, model.View.Durability);
    }

    [Fact] public void RetryReusesExistingIdentityAndManualQuitRequiresExplicitReview()
    {
        var op = new SaveLibraryOperation(SaveLibraryOperationPhase.ConfirmedNotCommitted, SaveLibraryOperationReason.SaveAndQuit, "op.existing", "Disk full", true);
        var model = new SaveLibraryPresentation(View(operation: op)); var boundary = model.BeginRender();
        Assert.True(model.TryPrepare(boundary, SaveLibraryAction.RetryExistingOperation, null, "", out var retry));
        Assert.Equal("op.existing", retry!.ExistingOperationId); Assert.False(retry.RequiresConfirmation);
        Assert.True(model.TryPrepare(boundary, SaveLibraryAction.QuitWithoutSaving, null, "", out var quit));
        Assert.True(quit!.RequiresConfirmation); Assert.True(model.TryConfirm(quit)); Assert.False(model.Can(SaveLibraryAction.ContinueSingleplayer));
        Assert.Equal(148, quit.ReviewedCurrent.Turn); Assert.Equal(146, quit.ReviewedLastDurable.Turn);
        Assert.Equal(148, model.View.Current.Turn); Assert.Equal(146, model.View.LastDurable.Turn);
    }

    [Fact] public void AutoFailureContinueDoesNotClearPersistentWarningOrMarkSessionDurable()
    {
        var model = new SaveLibraryPresentation(View(operation: new(SaveLibraryOperationPhase.ConfirmedNotCommitted, SaveLibraryOperationReason.Auto, "op.auto", "Storage unavailable", true)));
        Assert.True(model.TryPrepare(model.BeginRender(), SaveLibraryAction.ContinueSingleplayer, null, "", out var intent));
        Assert.Equal(SaveLibraryAction.ContinueSingleplayer, intent!.Action);
        Assert.True(model.View.Operation.Warning); Assert.Equal(SaveLibraryDurability.Unsaved, model.View.Durability);
        var multiplayer = new SaveLibraryPresentation(View(operation: new(SaveLibraryOperationPhase.ConfirmedNotCommitted, SaveLibraryOperationReason.Auto, "op.auto", "", false)));
        Assert.False(multiplayer.Can(SaveLibraryAction.ContinueSingleplayer));
    }

    [Theory]
    [InlineData(SaveLibraryOperationPhase.WaitingForAdmission, "Waiting to save")]
    [InlineData(SaveLibraryOperationPhase.Saving, "Saving")]
    [InlineData(SaveLibraryOperationPhase.Committed, "Saved")]
    [InlineData(SaveLibraryOperationPhase.ConfirmedNotCommitted, "Save failed")]
    public void OperationProgressIsDistinctFromDurability(SaveLibraryOperationPhase phase, string heading)
    {
        var model = new SaveLibraryPresentation(View(operation: new(phase, SaveLibraryOperationReason.Manual, "op", "", true)));
        Assert.Equal(heading, model.View.Operation.Heading); Assert.Equal(SaveLibraryDurability.Unsaved, model.View.Durability);
        Assert.Equal(phase == SaveLibraryOperationPhase.ConfirmedNotCommitted, model.Can(SaveLibraryAction.RetryExistingOperation));
    }

    [Fact] public void RecoveryNeedsValidatedCandidateAndExplicitLossWindowAndPreservesDamagedLatest()
    {
        Assert.Throws<ArgumentException>(() => new SaveLibraryRecovery("Newest is corrupt", Row(health: SaveLibraryHealth.Corrupt), "Loss window"));
        var candidate = Row("recovery"); var incomplete = new SaveLibraryPresentation(View(recovery: new("Newest turn 148 is corrupt", candidate, "")));
        Assert.False(incomplete.Can(SaveLibraryAction.Recover));
        var model = new SaveLibraryPresentation(View(recovery: new("Newest turn 148 is corrupt", candidate, "After turn 146 revision 2 through interrupted turn 148 capture")));
        Assert.True(model.TryPrepare(model.BeginRender(), SaveLibraryAction.Recover, null, "", out var recovery));
        Assert.True(recovery!.RequiresConfirmation); Assert.Equal("recovery", recovery.Target!.ReferenceId);
        Assert.Contains("interrupted", recovery.RecoveryLossWindow); Assert.True(model.TryConfirm(recovery));
        Assert.Equal("Newest turn 148 is corrupt", model.View.Recovery!.LatestDamagedCaption);
    }

    [Fact] public void HundredsOfMetadataRowsMustBePagedAndUnknownHealthCannotLoad()
    {
        Assert.Throws<ArgumentException>(() => View(Enumerable.Range(0, 348).Select(i => Row("ref." + i))));
        Assert.Throws<ArgumentException>(() => View(new[] { Row(), Row() }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SaveLibraryQuery(pageSize: 348));
        var model = new SaveLibraryPresentation(View(new[] { Row(health: SaveLibraryHealth.Unknown) }));
        Assert.False(model.TryPrepare(model.BeginRender(), SaveLibraryAction.Load, "ref.1", "", out _));
        Assert.Equal("Turn not reported", new SaveLibraryProgress(null, null).Caption);
    }

    [Fact] public void EmptyLibraryCanReviewNamedManualSaveWithoutReusingAnOldReceipt()
    {
        var model = new SaveLibraryPresentation(View(Array.Empty<SaveLibraryRow>(), operation: new(SaveLibraryOperationPhase.Committed, SaveLibraryOperationReason.Manual, "old.receipt", "", true)));
        var boundary = model.BeginRender();
        Assert.False(model.TryPrepare(boundary, SaveLibraryAction.CreateManual, null, "  ", out _));
        Assert.True(model.TryPrepare(boundary, SaveLibraryAction.CreateManual, "ignored.ref", "新しい帰還 🚀", out var intent));
        Assert.Null(intent!.Target); Assert.Null(intent.ExistingOperationId); Assert.Null(intent.PendingRequestId);
        Assert.Equal("新しい帰還 🚀", intent.ProposedName); Assert.Equal("session.a", intent.Boundary.SourceEpoch);
        Assert.Equal("branch.a", intent.ActiveBranchId); Assert.Equal(9, intent.ExpectedActiveBranchGeneration);
        Assert.Equal(148, intent.ReviewedCurrent.Turn); Assert.Equal(6, intent.ReviewedCurrent.Revision); Assert.Equal(146, intent.ReviewedLastDurable.Turn);
        Assert.True(intent.RequiresConfirmation); Assert.True(model.TryConfirm(intent));
        Assert.True(model.TryPublish(boundary, View(Array.Empty<SaveLibraryRow>(), generation: 11)));
        Assert.False(model.TryConfirm(intent));
    }

    [Theory]
    [InlineData(SaveLibraryOperationPhase.WaitingForAdmission)]
    [InlineData(SaveLibraryOperationPhase.Saving)]
    [InlineData(SaveLibraryOperationPhase.OutcomeUnknown)]
    public void NewManualSaveCannotReplaceAnUnresolvedOrAdmittedOperation(SaveLibraryOperationPhase phase)
    {
        var model = new SaveLibraryPresentation(View(Array.Empty<SaveLibraryRow>(), operation: new(phase, SaveLibraryOperationReason.Manual, "op", "", true)));
        Assert.False(model.TryPrepare(model.BeginRender(), SaveLibraryAction.CreateManual, null, "Name", out _));
    }

    [Fact] public void ValidationIsAnExplicitSingleTargetRequestAndOnlyHostResultCanEnableLoad()
    {
        var model = new SaveLibraryPresentation(View(new[] { Row(health: SaveLibraryHealth.Unknown) })); var boundary = model.BeginRender();
        Assert.True(model.TryPrepare(boundary, SaveLibraryAction.Validate, "ref.1", "", out var check));
        Assert.Equal("ref.1", check!.Target!.ReferenceId); Assert.Equal(3, check.Target.ReferenceGeneration);
        Assert.Equal(SaveLibraryHealth.Unknown, model.View.Rows[0].Health); Assert.False(model.Can(SaveLibraryAction.Load, model.View.Rows[0]));
        Assert.True(model.TryPublish(boundary, View(new[] { Row(health: SaveLibraryHealth.Checking) })));
        Assert.False(model.TryPrepare(boundary, SaveLibraryAction.Validate, "ref.1", "", out _));
        var checking = model.BeginRender(); Assert.False(model.Can(SaveLibraryAction.Validate, model.View.Rows[0])); Assert.False(model.Can(SaveLibraryAction.Load, model.View.Rows[0]));
        Assert.True(model.TryPublish(checking, View())); var validated = model.BeginRender();
        Assert.True(model.TryPrepare(validated, SaveLibraryAction.Load, "ref.1", "", out _));
        model.Retire(); Assert.False(model.TryPrepare(validated, SaveLibraryAction.Validate, "ref.1", "", out _));
    }

    [Fact] public void WaitingCancellationUsesAdmissionIdentityAndNeverACapturedCommitReceipt()
    {
        var waiting = new SaveLibraryOperation(SaveLibraryOperationPhase.WaitingForAdmission, SaveLibraryOperationReason.Manual, "previous.receipt", "", true, pendingRequestId: "pending.opaque:日本語");
        var model = new SaveLibraryPresentation(View(operation: waiting)); var boundary = model.BeginRender();
        Assert.True(model.TryPrepare(boundary, SaveLibraryAction.CancelWaitingRequest, "ignored", "", out var cancel));
        Assert.Equal("pending.opaque:日本語", cancel!.PendingRequestId); Assert.Null(cancel.ExistingOperationId); Assert.Null(cancel.Target);
        Assert.False(cancel.RequiresConfirmation); Assert.Equal("Waiting to save", waiting.Heading);
        Assert.True(model.TryPublish(boundary, View(operation: new(SaveLibraryOperationPhase.Saving, SaveLibraryOperationReason.Manual, "admitted.receipt", "", true, pendingRequestId: "pending.opaque:日本語"))));
        Assert.False(model.TryPrepare(boundary, SaveLibraryAction.CancelWaitingRequest, null, "", out _));
        Assert.False(model.TryPrepare(model.BeginRender(), SaveLibraryAction.CancelWaitingRequest, null, "", out _));
        var noAdmissionId = new SaveLibraryPresentation(View(operation: new(SaveLibraryOperationPhase.WaitingForAdmission, SaveLibraryOperationReason.Manual, "receipt.is.not.pending.id", "", true)));
        Assert.False(noAdmissionId.Can(SaveLibraryAction.CancelWaitingRequest));
    }

    [Fact] public void UnknownOutcomeOnlyShowsCheckingWhenHostReportsLookupStarted()
    {
        var op = new SaveLibraryOperation(SaveLibraryOperationPhase.OutcomeUnknown, SaveLibraryOperationReason.Manual, "receipt", "", true, outcomeLookupInProgress: true);
        var model = new SaveLibraryPresentation(View(operation: op));
        Assert.Equal("Checking save status…", op.Heading); Assert.False(model.Can(SaveLibraryAction.CheckOutcome));
        Assert.False(model.Can(SaveLibraryAction.CancelWaitingRequest)); Assert.False(model.Can(SaveLibraryAction.RetryExistingOperation));
        Assert.Equal("Turn 148", model.View.Current.Caption); Assert.Contains("revision 6", model.View.Current.DiagnosticCaption);
        Assert.Equal("Time not reported", model.View.LastSavedTimeCaption);
    }

    [Fact] public void MissingCreationBranchGenerationStaysUnknownWhileReferenceAndActiveHeadRemainStamped()
    {
        var row = new SaveLibraryRow("ref", "campaign.a", "creation.branch", "checkpoint", 4, null, "Name", "Federation", "Original branch", "Date not reported", new(146, 2), SaveLibraryKind.Manual, SaveLibraryHealth.Validated);
        var model = new SaveLibraryPresentation(View(new[] { row }));
        Assert.True(model.TryPrepare(model.BeginRender(), SaveLibraryAction.Delete, "ref", "", out var delete));
        Assert.Null(delete!.Target!.BranchGeneration); Assert.Equal(4, delete.Target.ReferenceGeneration);
        Assert.Equal("creation.branch", delete.Target.BranchId); Assert.Equal("branch.a", delete.ActiveBranchId); Assert.Equal(9, delete.ExpectedActiveBranchGeneration);
        Assert.True(model.TryConfirm(delete));
        model.Retire(); Assert.False(model.TryConfirm(delete));
    }
}
