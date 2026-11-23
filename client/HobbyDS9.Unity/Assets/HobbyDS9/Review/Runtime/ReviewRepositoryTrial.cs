#if HOBBYDS9_STATE_REPOSITORY_TRIAL
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.CampaignRepositoryHost;
using HobbyDS9.Contracts;
using HobbyDS9.Persistence;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    // Presentation adapter. The repository owns metadata and the queue owns complete images, receipts and admission.
    internal sealed class ReviewRepositoryTrial : IDisposable
    {
        private static readonly SaveLibraryAction[] Actions = {
            SaveLibraryAction.CreateManual, SaveLibraryAction.Overwrite, SaveLibraryAction.Validate, SaveLibraryAction.Load,
            SaveLibraryAction.Export, SaveLibraryAction.CheckOutcome, SaveLibraryAction.CancelWaitingRequest,
            SaveLibraryAction.ContinueSingleplayer, SaveLibraryAction.ReturnToSession, SaveLibraryAction.QuitWithoutSaving,
            SaveLibraryAction.StorageHelp
        };

        private readonly Dictionary<string, SaveLibraryHealth> health = new Dictionary<string, SaveLibraryHealth>(StringComparer.Ordinal);
        private readonly Dictionary<string, BranchInfo> validatedBranches = new Dictionary<string, BranchInfo>(StringComparer.Ordinal);
        private readonly List<ReferenceCursor?> cursors = new List<ReferenceCursor?>();
        private ReferencePage? page;
        private long catalogueSequence;
        private string? previousToken, nextToken;
        private ReviewFrame? observedFrame;
        private string? failure;
        private string? maintenanceWarning;
        private string? committedUtc;
        private SaveLibraryOperationReason operationReason;
        private bool failedManual;
        private ForkContinuation? fork;
        private RepositorySaveWork? activeWork;
        private SaveWorkKind? unresolvedKind;

        internal sealed class ForkContinuation
        {
            internal string CheckpointId { get; }
            internal string BranchId { get; } = "branch-" + Guid.NewGuid().ToString("N");
            internal string OperationId { get; } = "fork-" + Guid.NewGuid().ToString("N");
            internal ForkContinuation(string checkpointId) { CheckpointId = checkpointId; }
        }

        internal ReviewUiModel Owner { get; }
        internal string Id { get; }
        internal ICampaignRepository Store { get; }
        internal RepositorySaveQueue Queue { get; }
        internal SaveLibraryQuery Query { get; private set; } = new SaveLibraryQuery();
        internal BranchInfo Branch { get; private set; }
        internal bool Disposed { get; private set; }
        internal bool SaveAndQuit { get; set; }
        internal bool PageError { get; private set; }
        internal string? Failure => failure ?? StorageWarning(Queue.Snapshot);
        internal bool HasPage => page != null;
        internal bool Current => !Disposed && !Owner.Closed;

        internal ReviewRepositoryTrial(ReviewUiModel owner, string id, ICampaignRepository store, BranchInfo branch,
            ICampaignRepositoryCodec wire)
        {
            Owner = owner; Id = id; Store = store; Branch = branch;
            Queue = owner.AttachRepositoryTrial(store, branch, wire);
            observedFrame = owner.Current.Frame;
        }

        internal void Observe()
        {
            var frame = Owner.Current.Frame;
            if (ReferenceEquals(frame, observedFrame) || Owner.Busy || Owner.Current.HasPendingWork || Owner.Current.RefreshRequired) return;
            var before = Queue.Snapshot.Current;
            if (Queue.ObserveFrame(frame) != CampaignRepositoryError.None) return;
            observedFrame = frame;
            var after = Queue.Snapshot.Current!;
            if (before == null || after.SameState(before)) return;
            if (after.Phase == SessionPhase.AwaitingCombat)
            {
                Queue.OfferAutosave(frame, before.Phase == SessionPhase.Planning ? AutosaveBoundary.AwaitingCombat : AutosaveBoundary.BufferedCombatResult);
            }
            else if (before.Phase == SessionPhase.AwaitingCombat)
            {
                Queue.OfferAutosave(frame, AutosaveBoundary.CombatFinalized);
            }
            else if (before.Turn < after.Turn)
            {
                Queue.OfferAutosave(frame, AutosaveBoundary.CompletedTurn);
            }
        }

        internal ReferenceQuery BuildQuery(SaveLibraryQuery query)
        {
            ReferenceCursor? cursor = null;
            if (query.Cursor != null)
            {
                if (!SameFilter(query) || page == null) throw new RepositoryException(RepositoryError.Conflict);
                if (query.Cursor == nextToken) cursor = page.Next;
                else if (query.Cursor == previousToken && cursors.Count > 1) cursor = cursors[cursors.Count - 2];
                else throw new RepositoryException(RepositoryError.Conflict);
            }
            return new ReferenceQuery(query.Search, query.Kind.HasValue ? ToStorageKind(query.Kind.Value) : null,
                query.ActiveBranchOnly ? Branch.Id : null, query.PageSize, cursor);
        }

        internal void PublishPage(SaveLibraryQuery query, ReferenceQuery request, ReferencePage result)
        {
            if (query.Cursor == null) { cursors.Clear(); cursors.Add(null); }
            else if (query.Cursor == nextToken)
            {
                cursors.Add(request.Cursor);
                if (cursors.Count > 32) cursors.RemoveAt(0);
            }
            else if (query.Cursor == previousToken && cursors.Count > 1) cursors.RemoveAt(cursors.Count - 1);
            Query = query; page = result; catalogueSequence = result.CatalogueSequence; PageError = false;
            previousToken = cursors.Count > 1 ? Guid.NewGuid().ToString("N") : null;
            nextToken = result.HasMore ? Guid.NewGuid().ToString("N") : null;
        }

        internal void FailPage(SaveLibraryQuery query)
        {
            Query = query.AtCursor(null); page = null; PageError = true;
            cursors.Clear(); previousToken = nextToken = null;
        }

        private bool SameFilter(SaveLibraryQuery query) => query.Search == Query.Search && query.Kind == Query.Kind
            && query.ActiveBranchOnly == Query.ActiveBranchOnly && query.PageSize == Query.PageSize;

        internal bool Matches(SaveLibraryIntent intent)
        {
            var snapshot = Queue.Snapshot;
            if (intent.Boundary.SourceEpoch != snapshot.Current?.EpochIdentity || intent.Boundary.CatalogueGeneration != catalogueSequence
                || intent.ActiveBranchId != Branch.Id || intent.ExpectedActiveBranchGeneration != Branch.Generation) return false;
            if (intent.RequiresConfirmation && (snapshot.Current?.Turn != intent.ReviewedCurrent.Turn
                || snapshot.Current?.Revision != intent.ReviewedCurrent.Revision || snapshot.LastDurable?.Turn != intent.ReviewedLastDurable.Turn
                || snapshot.LastDurable?.Revision != intent.ReviewedLastDurable.Revision)) return false;
            if (intent.Target == null) return true;
            return page?.Rows.Any(row => row.Id == intent.Target.ReferenceId && row.Generation == intent.Target.ReferenceGeneration
                && row.CheckpointId == intent.Target.CheckpointId && row.CheckpointBranchId == intent.Target.BranchId) == true;
        }

        internal SaveRequestStatus RequestManual(SaveLibraryIntent intent)
        {
            if (!Matches(intent) || !Queue.Snapshot.HeadConfirmed) return SaveRequestStatus.Stale;
            var target = intent.Target;
            if ((target != null && target.Kind != SaveLibraryKind.Manual) ||
                (target == null && (string.IsNullOrWhiteSpace(intent.ProposedName) || intent.ProposedName.Length > 120)))
                return SaveRequestStatus.Unavailable;
            var reference = new ReferenceUpdate(target?.ReferenceId ?? "manual-" + Guid.NewGuid().ToString("N"),
                target?.ReferenceGeneration ?? 0, SaveReferenceKind.Manual, target?.Name ?? intent.ProposedName,
                target == null ? "" : page!.Rows.Single(row => row.Id == target.ReferenceId).Notes);
            var status = Queue.RequestManual(Owner.Current.Frame, reference, out _);
            if (status == SaveRequestStatus.Queued)
            {
                failedManual = false; failure = null;
                operationReason = SaveAndQuit ? SaveLibraryOperationReason.SaveAndQuit : SaveLibraryOperationReason.Manual;
            }
            return status;
        }

        internal void CompleteSave(RepositorySaveWork work, SaveWorkResult result)
        {
            if (!Queue.Complete(work)) return;
            activeWork = null;
            Branch = Queue.Snapshot.ConfirmedBranch ?? Branch;
            var kind = work.Kind == SaveWorkKind.Resolution ? unresolvedKind ?? work.Kind : work.Kind;
            unresolvedKind = result.Outcome == SaveOutcome.Unknown ? kind : null;
            if (kind == SaveWorkKind.Maintenance)
            {
                maintenanceWarning = result.Outcome == SaveOutcome.Committed && !result.Error.HasValue ? null
                    : "Recovery cleanup could not be confirmed. Saved checkpoints may use more storage until it succeeds.";
                return;
            }
            if (kind == SaveWorkKind.RefreshHead) return;
            if (!work.Manual && !Queue.Snapshot.PendingRequestId.HasValue) operationReason = SaveLibraryOperationReason.Auto;
            if (result.Outcome == SaveOutcome.Committed)
            {
                committedUtc = result.Receipt!.CommittedUtc;
                if (work.Manual || !failedManual) { failure = null; failedManual = false; }
            }
            else if (result.Outcome != SaveOutcome.Cancelled)
            {
                failure = result.Outcome == SaveOutcome.Unknown ? "The repository could not confirm the write."
                    : FailureCaption(result.Error);
                failedManual = work.Manual;
            }
        }

        internal void BeginSave(RepositorySaveWork work) => activeWork = work;

        private string? StorageWarning(SaveQueueSnapshot snapshot)
            => !snapshot.HeadConfirmed && !snapshot.OutcomeUnknown
                ? "The active branch could not be checked. New saves wait until the repository confirms it."
                : maintenanceWarning;

        internal void RecordValidation(string checkpointId, RepositoryPayloadResult result, BranchInfo? validatedBranch)
        {
            var checkedHealth = ValidationHealth(result.Error, result.StorageError);
            if (!checkedHealth.HasValue) return;
            if (checkedHealth == SaveLibraryHealth.Validated && validatedBranch == null) return;
            if (health.Count >= 100) { health.Clear(); validatedBranches.Clear(); }
            health[checkpointId] = checkedHealth.Value;
            if (validatedBranch != null) validatedBranches[validatedBranch.Id] = validatedBranch;
        }

        internal static SaveLibraryHealth? ValidationHealth(CampaignRepositoryError hostError, RepositoryError? storageError)
        {
            if (hostError == CampaignRepositoryError.None) return SaveLibraryHealth.Validated;
            if (hostError == CampaignRepositoryError.Cancelled || hostError == CampaignRepositoryError.Stale
                || hostError == CampaignRepositoryError.Busy) return null;
            if (hostError == CampaignRepositoryError.Incompatible || storageError == RepositoryError.IncompatibleEdition)
                return SaveLibraryHealth.Incompatible;
            if (storageError == RepositoryError.NotFound) return SaveLibraryHealth.Expired;
            if (storageError == RepositoryError.Corrupt) return SaveLibraryHealth.Corrupt;
            return null;
        }

        internal ForkContinuation? PrepareFork(SaveLibraryRow target)
        {
            if (fork != null && fork.CheckpointId != target.CheckpointId) throw new RepositoryException(RepositoryError.Conflict);
            return target.OlderContinuation ? fork ??= new ForkContinuation(target.CheckpointId) : null;
        }

        internal BranchInfo ConfirmLoadBranch(SaveLibraryRow target, BranchInfo activeBranch, ForkContinuation? continuation)
        {
            if (activeBranch.Head == target.CheckpointId)
            {
                var branch = Store.GetBranch(activeBranch.Id);
                if (branch.Generation != activeBranch.Generation || branch.Head != activeBranch.Head)
                    throw new RepositoryException(RepositoryError.Conflict);
                return branch;
            }
            var origin = Store.GetBranch(target.BranchId);
            if (origin.Generation != target.BranchGeneration || (origin.Head != target.CheckpointId) != target.OlderContinuation)
                throw new RepositoryException(RepositoryError.Conflict);
            if (!target.OlderContinuation) return origin;
            // The explicit Load confirmation discloses this fork. Never advance the existing branch at a historic checkpoint.
            if (continuation?.CheckpointId != target.CheckpointId) throw new RepositoryException(RepositoryError.Conflict);
            try { Store.ForkBranch(continuation.OperationId, continuation.BranchId, target.CheckpointId); }
            catch (RepositoryException error) when (error.Error == RepositoryError.OutcomeUnknown)
            {
                var receipt = Store.FindOperation(continuation.OperationId);
                if (receipt?.CheckpointId != target.CheckpointId) throw;
            }
            var created = Store.GetBranch(continuation.BranchId);
            if (created.Head != target.CheckpointId || created.Generation != 0) throw new RepositoryException(RepositoryError.Conflict);
            return created;
        }

        internal void AfterLoad(BranchInfo branch, SaveLibraryRow selected)
        {
            fork = null;
            validatedBranches[Branch.Id] = Branch;
            Branch = branch; failure = null; maintenanceWarning = null; failedManual = false; SaveAndQuit = false;
            committedUtc = page?.Rows.FirstOrDefault(row => row.Id == selected.ReferenceId)?.UpdatedUtc;
            observedFrame = Owner.Current.Frame;
            FailPage(Query); PageError = false;
        }

        internal SaveLibraryView View(bool loading = false)
        {
            var snapshot = Queue.Snapshot;
            var current = snapshot.Current!;
            var durable = snapshot.LastDurable;
            var phase = snapshot.OutcomeUnknown ? SaveLibraryOperationPhase.OutcomeUnknown
                : snapshot.PendingRequestId.HasValue ? SaveLibraryOperationPhase.WaitingForAdmission
                : snapshot.WorkerActive && snapshot.OperationId != null ? SaveLibraryOperationPhase.Saving
                : failure != null || snapshot.ManualNeedsConfirmation ? SaveLibraryOperationPhase.ConfirmedNotCommitted
                : snapshot.LastManualOutcome == SaveOutcome.Committed ? SaveLibraryOperationPhase.Committed
                : SaveLibraryOperationPhase.None;
            var reason = failedManual || activeWork?.Manual == true || snapshot.PendingRequestId.HasValue || phase == SaveLibraryOperationPhase.Committed
                ? (SaveAndQuit ? SaveLibraryOperationReason.SaveAndQuit : SaveLibraryOperationReason.Manual) : operationReason;
            var detail = snapshot.ManualNeedsConfirmation ? "The game advanced while the save waited. Review the current state and confirm again."
                : unresolvedKind == SaveWorkKind.Maintenance ? "The repository could not confirm recovery cleanup."
                : failure ?? (phase == SaveLibraryOperationPhase.Committed ? "The repository confirmed the save." : "");
            var operation = new SaveLibraryOperation(phase, reason, snapshot.OutcomeUnknown ? snapshot.OperationId : null,
                detail, true, snapshot.PendingRequestId?.ToString("N"), snapshot.OutcomeUnknown && snapshot.WorkerActive,
                phase == SaveLibraryOperationPhase.Saving && activeWork is { Kind: SaveWorkKind.Checkpoint, Manual: false });
            var rows = page?.Rows.Select(Row) ?? Array.Empty<SaveLibraryRow>();
            return new SaveLibraryView(current.EpochIdentity, catalogueSequence, Branch.Id, Branch.Generation,
                "Belt mining trial · " + BranchCaption(Branch.Id), new SaveLibraryProgress(current.Turn, current.Revision),
                new SaveLibraryProgress(durable?.Turn, durable?.Revision), snapshot.HasUnsavedProgress ? SaveLibraryDurability.Unsaved : SaveLibraryDurability.Clean,
                Query, rows, previousToken, nextToken, snapshot.AutomaticPaused ? "Automatic saves paused at the storage budget." : "Newest saved references first",
                loading ? SaveLibraryPageState.Loading : PageError ? SaveLibraryPageState.Error : SaveLibraryPageState.Ready, operation,
                lastSavedTimeCaption: TimeCaption(committedUtc), supportedActions: Actions, healthFilterEnabled: false, storageWarning: StorageWarning(snapshot));
        }

        private SaveLibraryRow Row(SaveReference row)
        {
            var origin = row.CheckpointBranchId == Branch.Id ? Branch
                : validatedBranches.TryGetValue(row.CheckpointBranchId, out var known) ? known : null;
            var older = origin != null && row.CheckpointId != origin.Head && row.CheckpointId != Branch.Head;
            var progress = row.Turn > 0 ? new SaveLibraryProgress(row.Turn, row.Revision) : new SaveLibraryProgress(null, null);
            return new SaveLibraryRow(row.Id, Id, row.CheckpointBranchId, row.CheckpointId, row.Generation, origin?.Generation,
                row.Name, "Belt mining trial", BranchCaption(row.CheckpointBranchId), TimeCaption(row.UpdatedUtc),
                progress, ToViewKind(row.Kind), Health(row), older);
        }

        private SaveLibraryHealth Health(SaveReference row) => row.Quarantined ? SaveLibraryHealth.Corrupt
            : !row.PayloadAvailable ? SaveLibraryHealth.Expired
            : health.TryGetValue(row.CheckpointId, out var checkedHealth) ? checkedHealth : SaveLibraryHealth.Unknown;

        private static string BranchCaption(string id) => "Branch " + id.Substring(Math.Max(0, id.Length - 8));
        private static string TimeCaption(string? utc) => DateTimeOffset.TryParseExact(utc, "O", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var time) ? time.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) : "Time not reported";
        private static SaveReferenceKind ToStorageKind(SaveLibraryKind kind) => kind == SaveLibraryKind.Auto ? SaveReferenceKind.Auto
            : kind == SaveLibraryKind.Milestone ? SaveReferenceKind.Milestone : SaveReferenceKind.Manual;
        private static SaveLibraryKind ToViewKind(SaveReferenceKind kind) => kind == SaveReferenceKind.Auto ? SaveLibraryKind.Auto
            : kind == SaveReferenceKind.Milestone ? SaveLibraryKind.Milestone : SaveLibraryKind.Manual;

        internal static string FailureCaption(RepositoryError? error) => error switch
        {
            RepositoryError.StorageFull => "Storage is full. Free space and review a new manual save.",
            RepositoryError.ReadOnly => "The trial storage is read-only.",
            RepositoryError.WriterInUse => "Another player has the trial storage open.",
            RepositoryError.IncompatibleEdition => "This trial storage belongs to a different campaign edition.",
            RepositoryError.IncompatibleSchema => "Use the build that created this trial storage, or start a new trial ID.",
            RepositoryError.Busy => "The trial storage is busy. Try again after the current operation.",
            RepositoryError.Conflict => "The save list or branch changed. Review the current state again.",
            _ => "The save was not confirmed. Your game is still open."
        };

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Store.Dispose();
        }
    }
}
#endif
