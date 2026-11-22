#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HobbyDS9.UnityReview
{
    // Presentation contracts only. These are deliberately not repository DTOs or write commands.
    public enum SaveLibraryKind { Manual, Auto, Milestone }
    public enum SaveLibraryHealth { Unknown, Checking, Validated, Corrupt, Incompatible, Expired }
    public enum SaveLibraryPageState { Ready, Loading, Error }
    public enum SaveLibraryDurability { Unknown, Clean, Unsaved }
    public enum SaveLibraryOperationPhase { None, WaitingForAdmission, Saving, Committed, ConfirmedNotCommitted, OutcomeUnknown }
    public enum SaveLibraryOperationReason { Auto, Manual, SaveAndQuit }
    public enum SaveLibraryAction { Load, Export, ExportSession, StorageHelp, Rename, Duplicate, Overwrite, Delete, CheckOutcome, RetryExistingOperation, ContinueSingleplayer, ReturnToSession, QuitWithoutSaving, Recover, CreateManual, Validate, CancelWaitingRequest }

    public sealed class SaveLibraryProgress
    {
        public long? Turn { get; }
        public long? Revision { get; }
        public string Caption => Turn.HasValue ? "Turn " + Turn.Value : "Turn not reported";
        public string DiagnosticCaption => Caption + (Revision.HasValue ? " · revision " + Revision.Value : " · revision not reported");
        public SaveLibraryProgress(long? turn, long? revision) { Turn = turn; Revision = revision; }
    }

    public sealed class SaveLibraryRow
    {
        public string ReferenceId { get; }
        public string CampaignId { get; }
        public string BranchId { get; }
        public string CheckpointId { get; }
        public long ReferenceGeneration { get; }
        // This is creation-branch metadata, not reference membership or the active continuation head.
        public long? BranchGeneration { get; }
        public string Name { get; }
        public string Campaign { get; }
        public string Branch { get; }
        public string CheckpointCaption { get; }
        public SaveLibraryProgress Progress { get; }
        public SaveLibraryKind Kind { get; }
        public SaveLibraryHealth Health { get; }
        public bool OlderContinuation { get; }
        public SaveLibraryRow(string referenceId, string campaignId, string branchId, string checkpointId,
            long referenceGeneration, long? branchGeneration, string name, string campaign, string branch,
            string checkpointCaption, SaveLibraryProgress progress, SaveLibraryKind kind, SaveLibraryHealth health, bool olderContinuation = false)
        {
            ReferenceId = Required(referenceId); CampaignId = Required(campaignId); BranchId = Required(branchId); CheckpointId = Required(checkpointId);
            ReferenceGeneration = referenceGeneration; BranchGeneration = branchGeneration;
            Name = name ?? ""; Campaign = campaign ?? ""; Branch = branch ?? ""; CheckpointCaption = checkpointCaption ?? "Not reported";
            Progress = progress ?? throw new ArgumentNullException(nameof(progress)); Kind = kind; Health = health; OlderContinuation = olderContinuation;
        }
        internal static string Required(string value) => !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("A stable opaque identity is required.");
    }

    public sealed class SaveLibraryQuery
    {
        public string Search { get; }
        public SaveLibraryKind? Kind { get; }
        public SaveLibraryHealth? Health { get; }
        public bool ActiveBranchOnly { get; }
        public int PageSize { get; }
        public string? Cursor { get; }
        public SaveLibraryQuery(string search = "", SaveLibraryKind? kind = null, SaveLibraryHealth? health = null,
            bool activeBranchOnly = false, int pageSize = 25, string? cursor = null)
        {
            if (pageSize < 1 || pageSize > 50) throw new ArgumentOutOfRangeException(nameof(pageSize), "The presentation supports bounded pages of 1–50 metadata rows.");
            Search = search ?? ""; Kind = kind; Health = health; ActiveBranchOnly = activeBranchOnly; PageSize = pageSize; Cursor = cursor;
        }
        public SaveLibraryQuery AtCursor(string? cursor) => new SaveLibraryQuery(Search, Kind, Health, ActiveBranchOnly, PageSize, cursor);
    }

    public sealed class SaveLibraryOperation
    {
        public SaveLibraryOperationPhase Phase { get; }
        public SaveLibraryOperationReason Reason { get; }
        public string? ExistingOperationId { get; }
        public string? PendingRequestId { get; }
        public bool OutcomeLookupInProgress { get; }
        public string Detail { get; }
        public bool Singleplayer { get; }
        public SaveLibraryOperation(SaveLibraryOperationPhase phase, SaveLibraryOperationReason reason, string? existingOperationId, string detail, bool singleplayer,
            string? pendingRequestId = null, bool outcomeLookupInProgress = false)
        { Phase = phase; Reason = reason; ExistingOperationId = existingOperationId; Detail = detail ?? ""; Singleplayer = singleplayer; PendingRequestId = pendingRequestId; OutcomeLookupInProgress = outcomeLookupInProgress; }
        public string Heading => Phase switch
        {
            SaveLibraryOperationPhase.WaitingForAdmission => "Waiting to save",
            SaveLibraryOperationPhase.Saving => "Saving",
            SaveLibraryOperationPhase.Committed => "Saved",
            SaveLibraryOperationPhase.ConfirmedNotCommitted => "Save failed",
            SaveLibraryOperationPhase.OutcomeUnknown => OutcomeLookupInProgress ? "Checking save status…" : "Save status needs checking",
            _ => "No save status reported"
        };
        public bool Warning => Phase == SaveLibraryOperationPhase.ConfirmedNotCommitted || Phase == SaveLibraryOperationPhase.OutcomeUnknown;
    }

    public sealed class SaveLibraryRecovery
    {
        public string LatestDamagedCaption { get; }
        public SaveLibraryRow? ValidatedCandidate { get; }
        public string LossWindow { get; }
        public SaveLibraryRecovery(string latestDamagedCaption, SaveLibraryRow? validatedCandidate, string lossWindow)
        {
            LatestDamagedCaption = latestDamagedCaption ?? "";
            if (validatedCandidate != null && validatedCandidate.Health != SaveLibraryHealth.Validated) throw new ArgumentException("Recovery requires an explicitly validated candidate.");
            ValidatedCandidate = validatedCandidate; LossWindow = lossWindow ?? "";
        }
        public bool CanReview => ValidatedCandidate != null && !string.IsNullOrWhiteSpace(LossWindow);
    }

    public sealed class SaveLibraryView
    {
        public string SourceEpoch { get; }
        public long CatalogueGeneration { get; }
        public string ActiveBranchId { get; }
        public long ActiveBranchGeneration { get; }
        public string ContextCaption { get; }
        public SaveLibraryProgress Current { get; }
        public SaveLibraryProgress LastDurable { get; }
        public string LastSavedTimeCaption { get; }
        public SaveLibraryDurability Durability { get; }
        public SaveLibraryQuery Query { get; }
        public IReadOnlyList<SaveLibraryRow> Rows { get; }
        public string? PreviousCursor { get; }
        public string? NextCursor { get; }
        public string PageCaption { get; }
        public SaveLibraryPageState PageState { get; }
        public SaveLibraryOperation Operation { get; }
        public SaveLibraryRecovery? Recovery { get; }
        public SaveLibraryView(string sourceEpoch, long catalogueGeneration, string activeBranchId, long activeBranchGeneration,
            string contextCaption, SaveLibraryProgress current, SaveLibraryProgress lastDurable, SaveLibraryDurability durability,
            SaveLibraryQuery query, IEnumerable<SaveLibraryRow> rows, string? previousCursor, string? nextCursor,
            string pageCaption, SaveLibraryPageState pageState, SaveLibraryOperation operation, SaveLibraryRecovery? recovery = null, string? lastSavedTimeCaption = null)
        {
            SourceEpoch = SaveLibraryRow.Required(sourceEpoch); CatalogueGeneration = catalogueGeneration;
            ActiveBranchId = SaveLibraryRow.Required(activeBranchId); ActiveBranchGeneration = activeBranchGeneration;
            ContextCaption = contextCaption ?? ""; Current = current ?? throw new ArgumentNullException(nameof(current)); LastDurable = lastDurable ?? throw new ArgumentNullException(nameof(lastDurable));
            LastSavedTimeCaption = string.IsNullOrWhiteSpace(lastSavedTimeCaption) ? "Time not reported" : lastSavedTimeCaption;
            Durability = durability; Query = query ?? throw new ArgumentNullException(nameof(query));
            var copy = (rows ?? throw new ArgumentNullException(nameof(rows))).Take(query.PageSize + 1).ToArray();
            if (copy.Length > query.PageSize || copy.Any(r => r == null) || copy.Select(r => r.ReferenceId).Distinct(StringComparer.Ordinal).Count() != copy.Length)
                throw new ArgumentException("A metadata page must be bounded and contain unique stable reference IDs.");
            Rows = new ReadOnlyCollection<SaveLibraryRow>(copy); PreviousCursor = previousCursor; NextCursor = nextCursor;
            PageCaption = pageCaption ?? ""; PageState = pageState; Operation = operation ?? throw new ArgumentNullException(nameof(operation)); Recovery = recovery;
        }
    }

    public readonly struct SaveLibraryBoundary
    {
        public string SourceEpoch { get; }
        public long CatalogueGeneration { get; }
        public long RenderToken { get; }
        internal object Owner { get; }
        internal SaveLibraryBoundary(object owner, string epoch, long generation, long token) { Owner = owner; SourceEpoch = epoch; CatalogueGeneration = generation; RenderToken = token; }
    }

    public sealed class SaveLibraryIntent
    {
        public SaveLibraryBoundary Boundary { get; }
        public SaveLibraryAction Action { get; }
        public SaveLibraryRow? Target { get; }
        public string ActiveBranchId { get; }
        public long ExpectedActiveBranchGeneration { get; }
        public string? ExistingOperationId { get; }
        public string? PendingRequestId { get; }
        public string ProposedName { get; }
        public string RecoveryLossWindow { get; }
        public SaveLibraryProgress ReviewedCurrent { get; }
        public SaveLibraryProgress ReviewedLastDurable { get; }
        public bool RequiresConfirmation => Action == SaveLibraryAction.Load || Action == SaveLibraryAction.CreateManual || Action == SaveLibraryAction.Rename || Action == SaveLibraryAction.Duplicate || Action == SaveLibraryAction.Overwrite || Action == SaveLibraryAction.Delete || Action == SaveLibraryAction.QuitWithoutSaving || Action == SaveLibraryAction.Recover;
        internal SaveLibraryIntent(SaveLibraryBoundary boundary, SaveLibraryAction action, SaveLibraryRow? target, SaveLibraryView view, string proposedName)
        {
            Boundary = boundary; Action = action; Target = target; ActiveBranchId = view.ActiveBranchId; ExpectedActiveBranchGeneration = view.ActiveBranchGeneration;
            ExistingOperationId = action == SaveLibraryAction.CheckOutcome || action == SaveLibraryAction.RetryExistingOperation ? view.Operation.ExistingOperationId : null;
            PendingRequestId = action == SaveLibraryAction.CancelWaitingRequest ? view.Operation.PendingRequestId : null;
            ProposedName = proposedName; RecoveryLossWindow = action == SaveLibraryAction.Recover ? view.Recovery?.LossWindow ?? "" : "";
            ReviewedCurrent = view.Current; ReviewedLastDurable = view.LastDurable;
        }
    }

    public sealed class SaveLibraryQueryIntent
    {
        public SaveLibraryBoundary Boundary { get; }
        public SaveLibraryQuery Query { get; }
        internal SaveLibraryQueryIntent(SaveLibraryBoundary boundary, SaveLibraryQuery query) { Boundary = boundary; Query = query; }
    }

    // Owns only local presentation lifetime. The receiving host must independently validate all intent stamps.
    public sealed class SaveLibraryPresentation
    {
        private long renderToken;
        private bool retired;
        private readonly object owner = new object();
        public SaveLibraryView View { get; private set; }
        public SaveLibraryPresentation(SaveLibraryView initial) { View = initial ?? throw new ArgumentNullException(nameof(initial)); }
        public SaveLibraryBoundary BeginRender() => new SaveLibraryBoundary(owner, View.SourceEpoch, View.CatalogueGeneration, ++renderToken);
        public bool IsCurrent(SaveLibraryBoundary boundary) => !retired && ReferenceEquals(boundary.Owner, owner) && boundary.RenderToken == renderToken && boundary.SourceEpoch == View.SourceEpoch && boundary.CatalogueGeneration == View.CatalogueGeneration;
        public void Retire() { retired = true; ++renderToken; }
        public bool TryPublish(SaveLibraryBoundary requestBoundary, SaveLibraryView replacement)
        {
            if (replacement == null || !IsCurrent(requestBoundary) || replacement.SourceEpoch != View.SourceEpoch || replacement.CatalogueGeneration < View.CatalogueGeneration) return false;
            View = replacement; ++renderToken; return true;
        }
        public bool TryQuery(SaveLibraryBoundary boundary, SaveLibraryQuery query, out SaveLibraryQueryIntent? intent)
        {
            intent = null;
            if (!IsCurrent(boundary) || query == null) return false;
            // One metadata request at a time: retire the old controls and accept a response only to this request.
            intent = new SaveLibraryQueryIntent(BeginRender(), query); return true;
        }
        public bool Can(SaveLibraryAction action, SaveLibraryRow? row = null)
        {
            if (retired) return false;
            var op = View.Operation;
            // An unresolved/saving operation cannot be replaced by a new overwrite intent.
            var inFlight = op.Phase == SaveLibraryOperationPhase.OutcomeUnknown || op.Phase == SaveLibraryOperationPhase.Saving || op.Phase == SaveLibraryOperationPhase.WaitingForAdmission;
            switch (action)
            {
                case SaveLibraryAction.CheckOutcome: return op.Phase == SaveLibraryOperationPhase.OutcomeUnknown && !op.OutcomeLookupInProgress && !string.IsNullOrWhiteSpace(op.ExistingOperationId);
                case SaveLibraryAction.CancelWaitingRequest: return op.Phase == SaveLibraryOperationPhase.WaitingForAdmission && !string.IsNullOrWhiteSpace(op.PendingRequestId);
                case SaveLibraryAction.CreateManual: return !inFlight;
                case SaveLibraryAction.Validate: return View.PageState == SaveLibraryPageState.Ready && row?.Health == SaveLibraryHealth.Unknown;
                case SaveLibraryAction.RetryExistingOperation: return op.Phase == SaveLibraryOperationPhase.ConfirmedNotCommitted && !string.IsNullOrEmpty(op.ExistingOperationId);
                case SaveLibraryAction.ContinueSingleplayer: return op.Singleplayer && op.Reason == SaveLibraryOperationReason.Auto && op.Warning;
                case SaveLibraryAction.ReturnToSession: return op.Reason != SaveLibraryOperationReason.Auto && op.Warning;
                case SaveLibraryAction.ExportSession:
                case SaveLibraryAction.StorageHelp: return op.Warning;
                case SaveLibraryAction.QuitWithoutSaving: return op.Reason == SaveLibraryOperationReason.SaveAndQuit && op.Phase == SaveLibraryOperationPhase.ConfirmedNotCommitted;
                case SaveLibraryAction.Recover: return !inFlight && View.Recovery?.CanReview == true;
                case SaveLibraryAction.Load: return !inFlight && View.PageState == SaveLibraryPageState.Ready && row?.Health == SaveLibraryHealth.Validated;
                case SaveLibraryAction.Export: return row != null && row.Health == SaveLibraryHealth.Validated;
                case SaveLibraryAction.Overwrite: return !inFlight && row != null && row.Kind == SaveLibraryKind.Manual && View.PageState == SaveLibraryPageState.Ready;
                default: return !inFlight && row != null && View.PageState == SaveLibraryPageState.Ready;
            }
        }
        public bool TryPrepare(SaveLibraryBoundary boundary, SaveLibraryAction action, string? referenceId, string proposedName, out SaveLibraryIntent? intent)
        {
            intent = null;
            if (!IsCurrent(boundary)) return false;
            if (action == SaveLibraryAction.CreateManual && string.IsNullOrWhiteSpace(proposedName)) return false;
            var hasRowTarget = action == SaveLibraryAction.Load || action == SaveLibraryAction.Export || action == SaveLibraryAction.Rename || action == SaveLibraryAction.Duplicate || action == SaveLibraryAction.Overwrite || action == SaveLibraryAction.Delete || action == SaveLibraryAction.Validate;
            var row = action == SaveLibraryAction.Recover ? View.Recovery?.ValidatedCandidate : hasRowTarget ? View.Rows.FirstOrDefault(r => r.ReferenceId == referenceId) : null;
            if (!Can(action, row)) return false;
            intent = new SaveLibraryIntent(boundary, action, row, View, proposedName ?? ""); return true;
        }
        public bool TryConfirm(SaveLibraryIntent intent)
        {
            if (intent == null || !IsCurrent(intent.Boundary) || !intent.RequiresConfirmation || intent.ExpectedActiveBranchGeneration != View.ActiveBranchGeneration || intent.ActiveBranchId != View.ActiveBranchId) return false;
            var current = intent.Action == SaveLibraryAction.Recover ? View.Recovery?.ValidatedCandidate : View.Rows.FirstOrDefault(r => r.ReferenceId == intent.Target?.ReferenceId);
            if (intent.Target != null && (current == null || current.ReferenceGeneration != intent.Target.ReferenceGeneration || current.BranchGeneration != intent.Target.BranchGeneration || current.CheckpointId != intent.Target.CheckpointId || current.BranchId != intent.Target.BranchId || current.CampaignId != intent.Target.CampaignId)) return false;
            return Can(intent.Action, current) && (intent.Action != SaveLibraryAction.Recover || intent.RecoveryLossWindow == View.Recovery?.LossWindow);
        }
    }
}
