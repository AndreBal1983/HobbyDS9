#nullable enable
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    /// <summary>Disconnected C1 proposal. Emits stamped intents; never opens, writes or deletes a save.</summary>
    public sealed class SaveLibraryPanel : VisualElement, IDisposable
    {
        private readonly SaveLibraryPresentation presentation;
        private readonly Action<SaveLibraryQueryIntent> requestPage;
        private readonly Action<SaveLibraryIntent> requestAction;
        private readonly Func<bool> sourceIsCurrent;
        private string? selectedReferenceId;
        private SaveLibraryBoundary boundary;
        private VisualElement confirmation = null!;
        private VisualElement workspace = null!;
        private bool disposed;
        private long confirmationToken;
        private static readonly Color Ink = new Color(.035f, .055f, .085f);
        private static readonly Color Cell = new Color(.063f, .106f, .161f);
        private static readonly Color Lavender = new Color(.725f, .675f, .902f);
        private static readonly Color Amber = new Color(.94f, .65f, .36f);

        public SaveLibraryPanel(SaveLibraryView initial, Action<SaveLibraryQueryIntent> requestPage,
            Action<SaveLibraryIntent> requestAction, Func<bool> sourceIsCurrent)
        {
            presentation = new SaveLibraryPresentation(initial);
            this.requestPage = requestPage ?? throw new ArgumentNullException(nameof(requestPage));
            this.requestAction = requestAction ?? throw new ArgumentNullException(nameof(requestAction));
            this.sourceIsCurrent = sourceIsCurrent ?? throw new ArgumentNullException(nameof(sourceIsCurrent));
            name = "save-library-panel";
            style.backgroundColor = Ink; style.color = new Color(.89f, .92f, .96f); style.flexGrow = 1;
            style.paddingLeft = style.paddingRight = 24; style.paddingTop = style.paddingBottom = 18;
            RegisterCallback<DetachFromPanelEvent>(_ => Dispose());
            Render();
        }

        public SaveLibraryBoundary CurrentBoundary => boundary;
        // Host uses the request's boundary, not a newly read boundary, when publishing asynchronous results.
        public bool TryPublish(SaveLibraryBoundary requestBoundary, SaveLibraryView replacement)
        {
            if (disposed || !sourceIsCurrent() || !presentation.TryPublish(requestBoundary, replacement)) return false;
            Render(); return true;
        }
        public void Dispose() { if (disposed) return; disposed = true; presentation.Retire(); SetEnabled(false); }
        private bool Live(SaveLibraryBoundary captured) => !disposed && sourceIsCurrent() && presentation.IsCurrent(captured);

        private void Render()
        {
            ++confirmationToken; Clear(); SetEnabled(true); boundary = presentation.BeginRender(); var captured = boundary; var view = presentation.View;
            Text(this, "SAVE LIBRARY", "save-library-heading", 28, Amber);
            Text(this, "C1 proposal · disconnected presentation", "save-library-proposal", 14, Lavender);
            var status = Flow(this, "save-library-session");
            Metric(status, "Campaign / active branch", view.ContextCaption);
            Metric(status, "In memory", view.Current.Caption);
            Metric(status, "Last durable acknowledgement", view.LastDurable.Caption);
            Metric(status, "Session", view.Durability == SaveLibraryDurability.Unsaved ? "Unsaved progress" : view.Durability == SaveLibraryDurability.Clean ? "Durable acknowledgement matches session" : "Durability not reported");
            RenderOperation(captured);
            var viewport = new ScrollView { name = "save-library-workspace-scroll" };
            viewport.style.flexGrow = 1; viewport.style.minHeight = 0; Add(viewport); workspace = viewport;
            RenderFilters(captured);
            var content = Flow(workspace, "save-library-content"); content.style.alignItems = Align.Stretch;
            var list = Box(content, "save-library-list"); list.style.flexGrow = 2; list.style.flexBasis = 650; list.style.minWidth = 300;
            Text(list, "SAVED CHECKPOINTS", "save-library-list-title", 20, Lavender);
            Text(list, view.PageCaption, "save-library-page-caption", 16);
            if (view.PageState != SaveLibraryPageState.Ready)
                Text(list, view.PageState == SaveLibraryPageState.Loading ? "Loading library metadata…" : "Library metadata unavailable · saved checkpoint health is unchanged", "save-library-page-state", 18, Amber);
            var scroll = new ScrollView { name = "save-library-rows" }; scroll.style.maxHeight = 460; scroll.style.minHeight = 150; list.Add(scroll);
            if (view.Rows.Count == 0 && view.PageState == SaveLibraryPageState.Ready) Text(scroll, "No saves match this query.", "save-library-empty", 18);
            foreach (var row in view.Rows)
            {
                var item = Box(scroll, "save-library-row"); item.style.marginBottom = 5; item.style.paddingTop = item.style.paddingBottom = 5;
                item.userData = row.ReferenceId;
                var select = Button(item, row.Name, "save-library-select", () => { selectedReferenceId = row.ReferenceId; Render(); }, captured);
                select.style.unityTextAlign = TextAnchor.MiddleLeft; select.style.whiteSpace = WhiteSpace.Normal;
                if (row.ReferenceId == selectedReferenceId) { select.style.borderLeftColor = Amber; select.style.borderLeftWidth = 4; }
                Text(item, row.Campaign + " · " + row.Branch, "save-library-row-context", 14, Lavender);
                Text(item, row.Kind + "  |  " + row.Progress.Caption + "  |  " + row.Health, "save-library-row-status", 16);
                Text(item, row.CheckpointCaption, "save-library-row-checkpoint", 14);
            }
            var pages = Flow(list, "save-library-pagination");
            var previous = Button(pages, "Previous", "save-library-previous", () => Query(view.Query.AtCursor(view.PreviousCursor), captured), captured);
            previous.SetEnabled(view.PreviousCursor != null && view.PageState == SaveLibraryPageState.Ready);
            var next = Button(pages, "Next", "save-library-next", () => Query(view.Query.AtCursor(view.NextCursor), captured), captured);
            next.SetEnabled(view.NextCursor != null && view.PageState == SaveLibraryPageState.Ready);
            var inspector = Box(content, "save-library-inspector"); inspector.style.flexGrow = 1; inspector.style.flexBasis = 360; inspector.style.minWidth = 280;
            RenderInspector(inspector, captured);
            RenderRecovery(captured);
            confirmation = Box(workspace, "save-library-confirmation"); confirmation.style.display = DisplayStyle.None;
            Text(workspace, "Accepted policy · 10 rolling + 3 milestones · 512 MiB soft automatic-data budget · manual saves protected · older continuations branch · Quick Save deferred", "save-library-policy", 14, Lavender);
        }

        private void RenderFilters(SaveLibraryBoundary captured)
        {
            var query = presentation.View.Query; var filters = Flow(workspace, "save-library-filters");
            var search = new TextField("Search entire library") { name = "save-library-search", value = query.Search }; search.style.flexGrow = 1; search.style.minWidth = 260; filters.Add(search);
            SaveLibraryKind? kind = query.Kind; SaveLibraryHealth? health = query.Health; bool branch = query.ActiveBranchOnly;
            // Local query draft only. Apply creates one host query, never filters the currently visible page.
            var kindButton = new Button { name = "save-library-kind", text = "Kind: " + (kind?.ToString() ?? "All") }; filters.Add(kindButton);
            kindButton.clicked += () => { if (!Live(captured)) return; kind = !kind.HasValue ? SaveLibraryKind.Manual : kind == SaveLibraryKind.Manual ? SaveLibraryKind.Auto : kind == SaveLibraryKind.Auto ? SaveLibraryKind.Milestone : (SaveLibraryKind?)null; kindButton.text = "Kind: " + (kind?.ToString() ?? "All"); };
            var healthButton = new Button { name = "save-library-health", text = "Health: " + (health?.ToString() ?? "All") }; filters.Add(healthButton);
            healthButton.clicked += () => { if (!Live(captured)) return; health = !health.HasValue ? SaveLibraryHealth.Unknown : health == SaveLibraryHealth.Expired ? (SaveLibraryHealth?)null : (SaveLibraryHealth)((int)health.Value + 1); healthButton.text = "Health: " + (health?.ToString() ?? "All"); };
            var branchButton = new Button { name = "save-library-branch", text = branch ? "Branch: Active" : "Branch: All" }; filters.Add(branchButton);
            branchButton.clicked += () => { if (!Live(captured)) return; branch = !branch; branchButton.text = branch ? "Branch: Active" : "Branch: All"; };
            Button(filters, "Apply search / filters", "save-library-apply", () => Query(new SaveLibraryQuery(search.value, kind, health, branch, query.PageSize), captured), captured);
        }

        private void Query(SaveLibraryQuery query, SaveLibraryBoundary captured)
        {
            if (!Live(captured) || !presentation.TryQuery(captured, query, out var intent) || intent == null) return;
            boundary = intent.Boundary;
            // Old rows/confirmation are frozen until an explicit host result (including an error page) arrives.
            this.Q<Label>("save-library-page-caption").text = "Requesting library metadata…";
            SetEnabled(false); requestPage(intent);
        }

        private void RenderInspector(VisualElement inspector, SaveLibraryBoundary captured)
        {
            var row = presentation.View.Rows.FirstOrDefault(r => r.ReferenceId == selectedReferenceId);
            Text(inspector, "SELECTED SAVE", "save-library-inspector-title", 20, Lavender);
            if (row == null) { Text(inspector, "Select a checkpoint to review its branch and actions.", "save-library-no-selection", 18); return; }
            Text(inspector, row.Name, "save-library-selected-name", 22, Amber);
            Text(inspector, "Campaign · " + row.Campaign + "\nBranch · " + row.Branch + "\nCheckpoint · " + row.CheckpointCaption + "\n" + row.Progress.Caption + "\nHealth · " + row.Health, "save-library-selected-detail", 18);
            Text(inspector, row.Health == SaveLibraryHealth.Validated ? "Validated checkpoint · save contents checked again before load" : "Load unavailable until a complete compatible checkpoint is validated", "save-library-load-health", 16, Lavender);
            if (row.OlderContinuation) Text(inspector, "Older continuation creates a branch on accepted mutation or explicit branch. Later history remains.", "save-library-branch-warning", 16, Amber);
            var actions = Flow(inspector, "save-library-actions");
            ActionButton(actions, "Load", SaveLibraryAction.Load, row, captured);
            ActionButton(actions, "Export", SaveLibraryAction.Export, row, captured);
            var nameDraft = new TextField("Name for rename / duplicate") { name = "save-library-name-draft", value = row.Name }; inspector.Add(nameDraft);
            var changes = Flow(inspector, "save-library-changes");
            ActionButton(changes, "Rename…", SaveLibraryAction.Rename, row, captured, () => nameDraft.value);
            ActionButton(changes, "Duplicate…", SaveLibraryAction.Duplicate, row, captured, () => nameDraft.value);
            ActionButton(changes, "Overwrite…", SaveLibraryAction.Overwrite, row, captured);
            ActionButton(changes, "Delete…", SaveLibraryAction.Delete, row, captured);
        }

        private void RenderOperation(SaveLibraryBoundary captured)
        {
            var op = presentation.View.Operation;
            if (op.Phase == SaveLibraryOperationPhase.None) return;
            var area = Box(this, "save-library-operation"); area.style.borderLeftColor = op.Warning ? Amber : Lavender; area.style.borderLeftWidth = 6;
            Text(area, op.Heading, "save-library-operation-heading", 21, op.Warning ? Amber : Lavender);
            Text(area, op.Detail, "save-library-operation-detail", 17);
            if (op.Phase == SaveLibraryOperationPhase.OutcomeUnknown) Text(area, "The write may have committed. Check its outcome before another write.", "save-library-unknown-warning", 17, Amber);
            if (op.Reason == SaveLibraryOperationReason.SaveAndQuit && op.Warning) Text(area, "Session retained · no automatic exit or rollback", "save-library-session-retained", 17, Amber);
            if (op.Warning) Text(area, "In memory · " + presentation.View.Current.Caption + "  |  Last durable acknowledgement · " + presentation.View.LastDurable.Caption, "save-library-warning-progress", 16);
            var actions = Flow(area, "save-library-operation-actions");
            OperationButton(actions, "Check outcome", SaveLibraryAction.CheckOutcome, captured);
            OperationButton(actions, "Retry same operation", SaveLibraryAction.RetryExistingOperation, captured);
            OperationButton(actions, "Continue singleplayer", SaveLibraryAction.ContinueSingleplayer, captured);
            OperationButton(actions, "Return to session", SaveLibraryAction.ReturnToSession, captured);
            OperationButton(actions, "Export session…", SaveLibraryAction.ExportSession, captured);
            OperationButton(actions, "Storage help…", SaveLibraryAction.StorageHelp, captured);
            OperationButton(actions, "Quit without saving…", SaveLibraryAction.QuitWithoutSaving, captured);
            if (op.Warning) Text(area, "Export can preserve a separate copy. Its completion is reported separately from this save.", "save-library-export-guidance", 14, Lavender);
        }

        private void RenderRecovery(SaveLibraryBoundary captured)
        {
            var recovery = presentation.View.Recovery; if (recovery == null) return;
            var area = Box(workspace, "save-library-recovery");
            Text(area, "RECOVERY REVIEW", "save-library-recovery-heading", 20, Lavender);
            Text(area, "Newest damaged checkpoint · " + recovery.LatestDamagedCaption, "save-library-damaged", 17, Amber);
            var candidate = recovery.ValidatedCandidate;
            Text(area, candidate == null ? "No validated recovery candidate supplied" : "Validated candidate · " + candidate.Name + " · " + candidate.Branch + " · " + candidate.Progress.Caption, "save-library-candidate", 17);
            Text(area, string.IsNullOrWhiteSpace(recovery.LossWindow) ? "Loss window not reported · recovery unavailable" : "Loss window · " + recovery.LossWindow, "save-library-loss-window", 17, Amber);
            Text(area, "Explicit selection required · no automatic fallback · candidate rechecked before load", "save-library-recovery-disclosure", 14, Lavender);
            var action = Button(area, "Review recovery…", "save-library-recover", () => Prepare(SaveLibraryAction.Recover, null, "", captured), captured);
            action.SetEnabled(presentation.Can(SaveLibraryAction.Recover));
        }

        private void OperationButton(VisualElement area, string caption, SaveLibraryAction action, SaveLibraryBoundary captured)
        { if (presentation.Can(action)) ActionButton(area, caption, action, null, captured); }
        private void ActionButton(VisualElement area, string caption, SaveLibraryAction action, SaveLibraryRow? row, SaveLibraryBoundary captured, Func<string>? draft = null)
        {
            var button = Button(area, caption, "save-library-action-" + action, () => Prepare(action, row?.ReferenceId, draft?.Invoke() ?? "", captured), captured);
            button.SetEnabled(presentation.Can(action, row));
        }
        private void Prepare(SaveLibraryAction action, string? referenceId, string draft, SaveLibraryBoundary captured)
        {
            if (!Live(captured) || !presentation.TryPrepare(captured, action, referenceId, draft, out var intent) || intent == null) return;
            if (!intent.RequiresConfirmation) { requestAction(intent); return; }
            var thisConfirmation = ++confirmationToken;
            confirmation.Clear(); confirmation.style.display = DisplayStyle.Flex;
            Text(confirmation, "CONFIRM " + action.ToString().ToUpperInvariant(), "save-library-confirm-title", 20, Amber);
            Text(confirmation, intent.Target == null ? "Quit without saving keeps only previously durable progress.\nIn memory · " + presentation.View.Current.Caption + "\nLast durable acknowledgement · " + presentation.View.LastDurable.Caption : intent.Target.Name + " · " + intent.Target.Campaign + " · " + intent.Target.Branch + "\n" + intent.Target.Progress.Caption + " · " + intent.Target.CheckpointCaption, "save-library-confirm-target", 18);
            if (action == SaveLibraryAction.Rename || action == SaveLibraryAction.Duplicate) Text(confirmation, "Proposed name · " + intent.ProposedName, "save-library-confirm-name", 18);
            if (action == SaveLibraryAction.Recover) Text(confirmation, "Loss window · " + intent.RecoveryLossWindow, "save-library-confirm-loss", 18, Amber);
            if (action == SaveLibraryAction.Overwrite) Text(confirmation, "This checkpoint and the state captured for replacement will be checked again before saving.", "save-library-overwrite-warning", 16);
            Text(confirmation, "This exact target is reviewed. If it changes, review again.", "save-library-confirm-generation", 14, Lavender);
            var actions = Flow(confirmation, "save-library-confirm-actions");
            Button(actions, "Confirm " + action, "save-library-confirm", () =>
            {
                if (thisConfirmation != confirmationToken || !presentation.TryConfirm(intent)) return;
                ++confirmationToken;
                confirmation.style.display = DisplayStyle.None; requestAction(intent);
            }, captured);
            Button(actions, "Cancel", "save-library-cancel", () => { ++confirmationToken; confirmation.Clear(); confirmation.style.display = DisplayStyle.None; }, captured);
        }

        private Button Button(VisualElement parent, string caption, string elementName, Action action, SaveLibraryBoundary captured)
        {
            var result = new Button(() => { if (Live(captured)) action(); }) { name = elementName, text = caption, enableRichText = false };
            result.style.backgroundColor = new Color(.16f, .21f, .29f); result.style.color = new Color(.9f, .93f, .97f);
            result.style.paddingTop = result.style.paddingBottom = 9; result.style.paddingLeft = result.style.paddingRight = 12;
            result.style.fontSize = 17; result.style.minHeight = 38; result.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(result); return result;
        }
        private static VisualElement Flow(VisualElement parent, string elementName)
        {
            var result = new VisualElement { name = elementName }; result.style.flexDirection = FlexDirection.Row; result.style.flexWrap = Wrap.Wrap; result.style.marginTop = result.style.marginBottom = 7; parent.Add(result); return result;
        }
        private static VisualElement Box(VisualElement parent, string elementName)
        {
            var result = new VisualElement { name = elementName }; result.style.backgroundColor = Cell;
            result.style.paddingLeft = result.style.paddingRight = 12; result.style.paddingTop = result.style.paddingBottom = 10;
            result.style.marginRight = result.style.marginBottom = 10; parent.Add(result); return result;
        }
        private static void Metric(VisualElement parent, string caption, string value)
        {
            var result = Box(parent, "save-library-metric"); result.style.flexGrow = 1; result.style.minWidth = 210;
            Text(result, caption, "save-library-metric-label", 15, Lavender); Text(result, value, "save-library-metric-value", 20);
        }
        private static Label Text(VisualElement parent, string text, string elementName, int size, Color? color = null)
        {
            var label = new Label(text) { name = elementName, enableRichText = false }; label.style.fontSize = size; label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 6; if (color.HasValue) label.style.color = color.Value; parent.Add(label); return label;
        }
    }
}
