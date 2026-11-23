#if HOBBYDS9_STATE_REPOSITORY_TRIAL
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Microsoft.Data.Sqlite;
using Xunit;

namespace HobbyDS9.CampaignRepositoryHost.Tests;

public sealed class SaveLibraryBindingTests
{
    [Fact]
    public void ColdOpenContinuesTheLatestHeadAndHistoricalLoadKeepsIndependentHistory()
    {
        using var fixture = new RepositoryFixture();
        var lostForkResponse = false;
        string oldCheckpoint, latestCheckpoint;
        using (var original = Open(fixture, "main"))
        {
            oldCheckpoint = Save(original, "Étoile-日本語");
            Assert.True(original.Owner.Advance());
            original.Observe();
            Assert.True(original.Queue.Snapshot.PendingAutosave);
            latestCheckpoint = Save(original, "Latest");
            Assert.NotEqual(oldCheckpoint, latestCheckpoint);
        }
        using var reopened = Open(fixture, "new-branch", stage =>
        {
            if (stage == RepositoryWriteStage.AfterCommit && lostForkResponse)
            { lostForkResponse = false; throw new IOException("Fork acknowledgement lost."); }
        });
        Page(reopened);
        var latest = Check(reopened, latestCheckpoint);
        Assert.False(latest.OlderContinuation);
        var head = reopened.ConfirmLoadBranch(latest, reopened.Branch, reopened.PrepareFork(latest));
        Assert.Equal("main", head.Id);
        Load(reopened, latest, head);
        Assert.Equal(latestCheckpoint, reopened.Queue.Snapshot.LastDurableCheckpointId);
        Assert.False(reopened.Queue.Snapshot.HasUnsavedProgress);

        Page(reopened);
        var earlier = Check(reopened, oldCheckpoint);
        Assert.Equal("Étoile-日本語", earlier.Name);
        Assert.True(earlier.OlderContinuation);
        var continuation = reopened.PrepareFork(earlier)!;
        lostForkResponse = true;
        var fork = reopened.ConfirmLoadBranch(earlier, reopened.Branch, continuation);
        Assert.Same(continuation, reopened.PrepareFork(earlier));
        Assert.Equal(fork.Id, reopened.ConfirmLoadBranch(earlier, reopened.Branch, continuation).Id);
        Assert.Equal(oldCheckpoint, fork.Head);
        Load(reopened, earlier, fork);
        Assert.True(reopened.Owner.Advance());
        reopened.Observe();
        var next = Save(reopened, "Independent future");
        Assert.Equal(latestCheckpoint, reopened.Store.GetBranch("main").Head);
        Assert.Equal(next, reopened.Store.GetBranch(fork.Id).Head);
    }

    [Fact]
    public void CancelledPayloadCheckKeepsVerifiedHealthAndDoesNotAdopt()
    {
        using var fixture = new RepositoryFixture();
        using var trial = Open(fixture, "main");
        var checkpoint = Save(trial, "Baseline");
        Assert.Equal(SaveLibraryHealth.Validated, Check(trial, checkpoint).Health);
        var frame = trial.Owner.Current.Frame;
        var work = trial.Queue.PreparePayloadRead(frame, checkpoint).Work!;
        work.Cancel();
        var result = work.Execute();
        Assert.Equal(CampaignRepositoryError.Cancelled, result.Error);
        trial.RecordValidation(checkpoint, result, null);
        Assert.False(trial.Queue.CompletePayloadValidation(work));
        Assert.Equal(SaveLibraryHealth.Validated, trial.View().Rows.Single().Health);
        Assert.Same(frame, trial.Owner.Current.Frame);
        Assert.Null(ReviewRepositoryTrial.ValidationHealth(CampaignRepositoryError.InvalidPayload, RepositoryError.Unavailable));
        Assert.Null(ReviewRepositoryTrial.ValidationHealth(CampaignRepositoryError.InvalidPayload, RepositoryError.ValidationFailed));
    }

    [Fact]
    public void RefreshedDurableQuarantineOverridesPriorValidationAndDisablesLoad()
    {
        using var fixture = new RepositoryFixture();
        using var trial = Open(fixture, "main");
        var checkpoint = Save(trial, "Previously checked");
        Assert.Equal(SaveLibraryHealth.Validated, Check(trial, checkpoint).Health);
        var database = Path.Combine(fixture.DirectoryPath, "campaign", "campaign.sqlite");
        using (var sql = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = database, Pooling = false }.ToString()))
        {
            sql.Open();
            using var command = sql.CreateCommand();
            command.CommandText = "UPDATE Checkpoint SET Payload=zeroblob(PayloadLength) WHERE Id=$id";
            command.Parameters.AddWithValue("$id", checkpoint);
            Assert.Equal(1, command.ExecuteNonQuery());
        }
        Assert.Equal(RepositoryError.Corrupt, Assert.Throws<RepositoryException>(() => trial.Store.ReadCheckpoint(checkpoint)).Error);
        Page(trial);
        var view = trial.View();
        var row = Assert.Single(view.Rows);
        Assert.Equal(SaveLibraryHealth.Corrupt, row.Health);
        var presentation = new SaveLibraryPresentation(view);
        var token = presentation.BeginRender();
        Assert.False(presentation.TryPrepare(token, SaveLibraryAction.Load, row.ReferenceId, "", out _));
    }

    [Fact]
    public void FailedSaveAndQuitKeepsTheRunningFrameAndNeverClaimsADurableSave()
    {
        using var fixture = new RepositoryFixture();
        using var trial = Open(fixture, "main", stage =>
        {
            if (stage == RepositoryWriteStage.BeforeCommit) throw new IOException("Trial write refused.");
        });
        trial.SaveAndQuit = true;
        var frame = trial.Owner.Current.Frame;
        var result = RequestSave(trial, "Failed manual save");
        Assert.Equal(SaveOutcome.NotCommitted, result.Outcome);
        Assert.False(trial.Queue.Snapshot.CanQuitAfterManualSave);
        Assert.Null(trial.Queue.Snapshot.LastDurable);
        Assert.False(trial.Owner.Closed);
        Assert.Same(frame, trial.Owner.Current.Frame);
        var view = trial.View();
        Assert.Equal(SaveLibraryOperationReason.SaveAndQuit, view.Operation.Reason);
        Assert.Equal(SaveLibraryOperationPhase.ConfirmedNotCommitted, view.Operation.Phase);
        Assert.Empty(trial.Store.QueryReferences(new ReferenceQuery()).Rows);
    }

    [Fact]
    public void ConfirmedSaveKeepsItsReceiptWhileTheBranchHeadIsUnavailable()
    {
        using var fixture = new RepositoryFixture();
        using var backing = Open(fixture, "main");
        var lost = false;
        var faults = new SaveQueueTests.Faults(backing.Store)
        {
            Commit = request =>
            {
                var receipt = backing.Store.CommitCheckpoint(request);
                if (lost) return receipt;
                lost = true;
                throw new RepositoryException(RepositoryError.OutcomeUnknown, request.OperationId);
            },
            Branch = _ => throw new RepositoryException(RepositoryError.Unavailable)
        };
        using var trial = new ReviewRepositoryTrial(backing.Owner, "campaign", faults, backing.Branch, fixture.Wire);
        var result = RequestSave(trial, "Persisted with lost acknowledgement");
        Assert.Equal(SaveOutcome.Committed, result.Outcome);
        Assert.Equal(SaveLibraryOperationPhase.Committed, trial.View().Operation.Phase);
        Assert.Equal(result.Receipt!.CheckpointId, trial.Queue.Snapshot.LastDurableCheckpointId);
        Assert.False(trial.Queue.Snapshot.HeadConfirmed);
        Assert.NotNull(trial.View().StorageWarning);
        faults.Branch = null;
        var work = trial.Queue.PrepareNext(trial.Owner.Current.Frame).Work!;
        Assert.Equal(SaveWorkKind.RefreshHead, work.Kind);
        trial.CompleteSave(work, work.Execute());
        Assert.True(trial.Queue.Snapshot.HeadConfirmed);
        Assert.Null(trial.View().StorageWarning);
    }

    private static ReviewRepositoryTrial Open(RepositoryFixture fixture, string branch, Action<RepositoryWriteStage>? fault = null)
    {
        var owner = new ReviewUiModel(fixture.Package(), new() { Map = new() { Seed = "42", SystemCount = 30,
            EmptyRegionCount = 30, Width = 750000, Height = 600000,
            HomeCivilizationDefinitionIds = new() { "civilization.federation", "civilization.cardassian" } } }, fixture.Codec,
            new ReviewColonisationCycleSlotStore(Path.Combine(fixture.DirectoryPath, "unused-json"), fixture.Codec));
        var capture = owner.PrepareRepositoryTrial(out var wire, out var adapter)!;
        var edition = capture.Execute(() => false).Image!.Edition.Identity;
        var storage = new SqliteCampaignRepository(Path.Combine(fixture.DirectoryPath, "campaign"), "campaign", edition,
            new CycleCheckpointValidator(adapter!, edition), fault);
        return new ReviewRepositoryTrial(owner, "campaign", storage, new BranchInfo(branch, null, 0), wire);
    }

    private static SaveWorkResult RequestSave(ReviewRepositoryTrial trial, string name)
    {
        var presentation = new SaveLibraryPresentation(trial.View());
        Assert.True(presentation.TryPrepare(presentation.BeginRender(), SaveLibraryAction.CreateManual, null, name, out var intent));
        Assert.True(presentation.TryConfirm(intent!));
        Assert.Equal(SaveRequestStatus.Queued, trial.RequestManual(intent!));
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var work = trial.Queue.PrepareNext(trial.Owner.Current.Frame).Work!;
            var result = work.Execute(); trial.CompleteSave(work, result);
            if (work.Manual) return result;
        }
        throw new InvalidOperationException("The manual save was not admitted after pending maintenance.");
    }

    private static string Save(ReviewRepositoryTrial trial, string name)
    {
        var result = RequestSave(trial, name);
        Assert.Equal(SaveOutcome.Committed, result.Outcome);
        Page(trial);
        return result.Receipt!.CheckpointId!;
    }

    private static void Page(ReviewRepositoryTrial trial)
    {
        var query = new SaveLibraryQuery();
        var request = trial.BuildQuery(query);
        trial.PublishPage(query, request, trial.Store.QueryReferences(request));
    }

    private static SaveLibraryRow Check(ReviewRepositoryTrial trial, string checkpoint)
    {
        var before = trial.View().Rows.Single(row => row.CheckpointId == checkpoint);
        var work = trial.Queue.PreparePayloadRead(trial.Owner.Current.Frame, checkpoint).Work!;
        var result = work.Execute();
        Assert.Equal(CampaignRepositoryError.None, result.Error);
        trial.RecordValidation(checkpoint, result, trial.Store.GetBranch(before.BranchId));
        Assert.True(trial.Queue.CompletePayloadValidation(work));
        return trial.View().Rows.Single(row => row.CheckpointId == checkpoint);
    }

    private static void Load(ReviewRepositoryTrial trial, SaveLibraryRow row, BranchInfo branch)
    {
        var work = trial.Queue.PreparePayloadRead(trial.Owner.Current.Frame, row.CheckpointId).Work!;
        var result = work.Execute();
        Assert.True(trial.Owner.RestoreRepositoryTrial(trial.Queue, work, result, trial.Store, branch));
        trial.AfterLoad(branch, row);
    }
}
#endif
