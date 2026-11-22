#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    // Actual disconnected widgets. Source compilation does not claim execution or native/visual acceptance.
    public sealed class SaveLibraryPanelTests
    {
        private static SaveLibraryRow Row(string id, long generation = 4, SaveLibraryHealth health = SaveLibraryHealth.Validated, long? creationBranchGeneration = 8)
            => new SaveLibraryRow(id, "campaign", "branch", "checkpoint." + id, generation, creationBranchGeneration, "帰還 <b>α</b> 🚀", "Federation", "Homeward", "Date not reported", new SaveLibraryProgress(146, 2), SaveLibraryKind.Manual, health, true);
        private static SaveLibraryView View(long generation = 10, SaveLibraryOperationPhase phase = SaveLibraryOperationPhase.None, SaveLibraryOperationReason reason = SaveLibraryOperationReason.Auto,
            bool empty = false, SaveLibraryHealth health = SaveLibraryHealth.Validated, string? pendingRequestId = null, bool lookupStarted = false)
            => new SaveLibraryView("epoch", generation, "branch", 8, "Federation · Homeward", new SaveLibraryProgress(148, 6), new SaveLibraryProgress(146, 2), SaveLibraryDurability.Unsaved,
                new SaveLibraryQuery(), empty ? Array.Empty<SaveLibraryRow>() : new[] { Row("first", generation, health), Row("second", generation, health) }, null, "opaque.next/日本語", "2 of 348 saves", SaveLibraryPageState.Ready,
                new SaveLibraryOperation(phase, reason, "existing.op", "Storage unavailable", true, pendingRequestId, lookupStarted), empty ? null : new SaveLibraryRecovery("Newest checkpoint · corrupt · turn 148", Row("recovery"), "Progress after the checked turn 146 save is absent"), lastSavedTimeCaption: "22 Nov 2026 · 15:42 UTC");
        private static void InvokeRetained(Button button)
            => typeof(Clickable).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button.clickable, new object?[] { null });

        [Test] public void BoundedRowsShowLiteralUnicodeAndDuplicateNamesSelectDistinctTargets()
        {
            var intents = new List<SaveLibraryIntent>(); using var panel = new SaveLibraryPanel(View(), _ => { }, intents.Add, () => true);
            var rows = panel.Query<VisualElement>("save-library-row").ToList(); Assert.That(rows.Count, Is.EqualTo(2));
            var second = rows[1].Q<Button>("save-library-select"); Assert.That(second.enableRichText, Is.False);
            InvokeRetained(second);
            Assert.That(panel.Q<Label>("save-library-selected-name").text, Is.EqualTo("帰還 <b>α</b> 🚀"));
            Assert.That(panel.Q<Label>("save-library-selected-name").enableRichText, Is.False);
            InvokeRetained(panel.Q<Button>("save-library-action-Delete")); Assert.That(intents.Count, Is.Zero);
            InvokeRetained(panel.Q<Button>("save-library-confirm")); Assert.That(intents.Single().Target!.ReferenceId, Is.EqualTo("second"));
            Assert.That(intents.Single().Target!.ReferenceGeneration, Is.EqualTo(10));
        }

        [Test] public void UnknownOutcomeShowsCheckOnlyForWritesAndKeepsSessionWarning()
        {
            var intents = new List<SaveLibraryIntent>(); using var panel = new SaveLibraryPanel(View(phase: SaveLibraryOperationPhase.OutcomeUnknown, reason: SaveLibraryOperationReason.SaveAndQuit), _ => { }, intents.Add, () => true);
            Assert.That(panel.Q<Label>("save-library-operation-heading").text, Is.EqualTo("Save status needs checking"));
            Assert.That(panel.Q<Button>("save-library-action-RetryExistingOperation"), Is.Null);
            Assert.That(panel.Q<Button>("save-library-action-QuitWithoutSaving"), Is.Null);
            Assert.That(panel.Q<Label>("save-library-session-retained"), Is.Not.Null);
            InvokeRetained(panel.Q<Button>("save-library-action-CheckOutcome"));
            Assert.That(intents.Single().ExistingOperationId, Is.EqualTo("existing.op"));
            Assert.That(panel.Q<Label>("save-library-warning-progress").text, Does.Contain("Turn 148").And.Contain("Turn 146"));
        }

        [Test] public void CancelledOrRefreshedConfirmationAndRetiredSourceCannotEmit()
        {
            var intents = new List<SaveLibraryIntent>(); var current = true;
            using var panel = new SaveLibraryPanel(View(), _ => { }, intents.Add, () => current);
            InvokeRetained(panel.Q<Button>("save-library-select"));
            InvokeRetained(panel.Q<Button>("save-library-action-Delete")); var cancelled = panel.Q<Button>("save-library-confirm");
            InvokeRetained(panel.Q<Button>("save-library-cancel")); InvokeRetained(cancelled); Assert.That(intents.Count, Is.Zero);
            InvokeRetained(panel.Q<Button>("save-library-action-Delete")); var stale = panel.Q<Button>("save-library-confirm");
            Assert.That(panel.TryPublish(panel.CurrentBoundary, View(11)), Is.True); InvokeRetained(stale); Assert.That(intents.Count, Is.Zero);
            var retired = panel.Query<Button>("save-library-select").ToList()[1]; var liveBoundary = panel.CurrentBoundary;
            current = false; InvokeRetained(retired); Assert.That(panel.CurrentBoundary.RenderToken, Is.EqualTo(liveBoundary.RenderToken));
            Assert.That(panel.TryPublish(panel.CurrentBoundary, View(12)), Is.False);
            current = true; panel.Dispose(); InvokeRetained(retired); Assert.That(intents.Count, Is.Zero);
        }

        [Test] public void QueryUsesWholeLibraryDraftAndOpaqueCursorThenRejectsOldControls()
        {
            SaveLibraryQueryIntent? query = null; var intents = new List<SaveLibraryIntent>();
            using var panel = new SaveLibraryPanel(View(), value => query = value, intents.Add, () => true);
            var oldSelect = panel.Q<Button>("save-library-select"); InvokeRetained(panel.Q<Button>("save-library-next"));
            Assert.That(query!.Query.Cursor, Is.EqualTo("opaque.next/日本語")); Assert.That(panel.enabledSelf, Is.False);
            InvokeRetained(oldSelect); Assert.That(panel.Q<Label>("save-library-no-selection"), Is.Not.Null);
            Assert.That(panel.TryPublish(query.Boundary, View(11)), Is.True);
            panel.Q<TextField>("save-library-search").value = "未知🚀"; InvokeRetained(panel.Q<Button>("save-library-kind"));
            InvokeRetained(panel.Q<Button>("save-library-apply")); Assert.That(query.Query.Search, Is.EqualTo("未知🚀"));
            Assert.That(query.Query.Kind, Is.EqualTo(SaveLibraryKind.Manual)); Assert.That(query.Query.Cursor, Is.Null); Assert.That(intents.Count, Is.Zero);
        }

        [Test] public void AutoContinueKeepsWarningAndRecoveryShowsExactLossBeforeConfirmation()
        {
            var intents = new List<SaveLibraryIntent>(); using var panel = new SaveLibraryPanel(View(phase: SaveLibraryOperationPhase.ConfirmedNotCommitted), _ => { }, intents.Add, () => true);
            Assert.That(panel.Q<ScrollView>("save-library-workspace-scroll"), Is.Not.Null);
            Assert.That(panel.Q("save-library-operation").parent, Is.SameAs(panel));
            InvokeRetained(panel.Q<Button>("save-library-action-ContinueSingleplayer")); Assert.That(panel.Q("save-library-operation"), Is.Not.Null);
            InvokeRetained(panel.Q<Button>("save-library-recover")); Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(panel.Q<Label>("save-library-confirm-loss").text, Does.Contain("after the checked turn 146 save"));
            Assert.That(panel.Q<Label>("save-library-damaged").text, Does.Contain("corrupt"));
            var confirm = panel.Q<Button>("save-library-confirm"); InvokeRetained(confirm); InvokeRetained(confirm);
            Assert.That(intents.Count, Is.EqualTo(2)); Assert.That(intents[1].Action, Is.EqualTo(SaveLibraryAction.Recover));
            Assert.That(intents[1].Target!.ReferenceId, Is.EqualTo("recovery"));
        }

        [Test] public void EmptyLibraryCreatesNamedManualSaveOnlyAfterReviewAndRejectsStaleReview()
        {
            var intents = new List<SaveLibraryIntent>(); using var panel = new SaveLibraryPanel(View(empty: true), _ => { }, intents.Add, () => true);
            var create = panel.Q<Button>("save-library-action-CreateManual"); Assert.That(create.enabledSelf, Is.False);
            panel.Q<TextField>("save-library-create-name").value = "初めての帰還 🚀"; Assert.That(create.enabledSelf, Is.True);
            InvokeRetained(create); Assert.That(intents.Count, Is.Zero);
            Assert.That(panel.Q<Label>("save-library-confirm-name").text, Does.Contain("初めての帰還 🚀"));
            Assert.That(panel.Q<Label>("save-library-confirm-target").text, Does.Contain("Turn 148").And.Not.Contain("revision"));
            var stale = panel.Q<Button>("save-library-confirm"); Assert.That(panel.TryPublish(panel.CurrentBoundary, View(11, empty: true)), Is.True);
            InvokeRetained(stale); Assert.That(intents.Count, Is.Zero);
            panel.Q<TextField>("save-library-create-name").value = "初めての帰還 🚀";
            InvokeRetained(panel.Q<Button>("save-library-action-CreateManual")); InvokeRetained(panel.Q<Button>("save-library-confirm"));
            Assert.That(intents.Single().Action, Is.EqualTo(SaveLibraryAction.CreateManual)); Assert.That(intents.Single().Target, Is.Null);
            Assert.That(intents.Single().ExistingOperationId, Is.Null); Assert.That(intents.Single().ReviewedCurrent.Revision, Is.EqualTo(6));
            Assert.That(panel.Q<Foldout>("save-library-diagnostics").value, Is.False);
            Assert.That(panel.Q<Label>("save-library-diagnostic-session").text, Does.Contain("revision 6"));
            Assert.That(panel.Query<Label>("save-library-metric-value").ToList().Any(l => l.text.Contains("15:42 UTC")), Is.True);
        }

        [Test] public void UnknownRowRequestsExactValidationAndLoadWaitsForCheckedHostResult()
        {
            var intents = new List<SaveLibraryIntent>(); using var panel = new SaveLibraryPanel(View(health: SaveLibraryHealth.Unknown), _ => { }, intents.Add, () => true);
            InvokeRetained(panel.Query<Button>("save-library-select").ToList()[1]); var check = panel.Q<Button>("save-library-action-Validate");
            Assert.That(check.enabledSelf, Is.True); Assert.That(panel.Q<Button>("save-library-action-Load").enabledSelf, Is.False);
            InvokeRetained(check); Assert.That(intents.Single().Action, Is.EqualTo(SaveLibraryAction.Validate)); Assert.That(intents.Single().Target!.ReferenceId, Is.EqualTo("second"));
            Assert.That(panel.Q<Label>("save-library-selected-detail").text, Does.Contain("Not checked"));
            Assert.That(panel.TryPublish(panel.CurrentBoundary, View(health: SaveLibraryHealth.Checking)), Is.True);
            InvokeRetained(check); Assert.That(intents.Count, Is.EqualTo(1)); Assert.That(panel.Q<Button>("save-library-action-Validate").enabledSelf, Is.False);
            Assert.That(panel.Q<Button>("save-library-action-Load").enabledSelf, Is.False);
            Assert.That(panel.TryPublish(panel.CurrentBoundary, View()), Is.True);
            Assert.That(panel.Q<Button>("save-library-action-Load").enabledSelf, Is.True);
        }

        [Test] public void WaitingCancelEmitsOnlyPendingIdentityAndAdmittedStateRetiresOldCancel()
        {
            var intents = new List<SaveLibraryIntent>(); using var panel = new SaveLibraryPanel(View(phase: SaveLibraryOperationPhase.WaitingForAdmission, pendingRequestId: "pending.opaque"), _ => { }, intents.Add, () => true);
            Assert.That(panel.Q<Label>("save-library-operation-heading").text, Is.EqualTo("Waiting to save"));
            var cancel = panel.Q<Button>("save-library-action-CancelWaitingRequest"); InvokeRetained(cancel);
            Assert.That(intents.Single().PendingRequestId, Is.EqualTo("pending.opaque")); Assert.That(intents.Single().ExistingOperationId, Is.Null);
            Assert.That(panel.TryPublish(panel.CurrentBoundary, View(phase: SaveLibraryOperationPhase.Saving, pendingRequestId: "pending.opaque")), Is.True);
            InvokeRetained(cancel); Assert.That(intents.Count, Is.EqualTo(1)); Assert.That(panel.Q<Button>("save-library-action-CancelWaitingRequest"), Is.Null);
            Assert.That(panel.Q<Label>("save-library-operation-heading").text, Is.EqualTo("Saving"));
        }

        [Test] public void LookupCopyStartsOnlyFromHostUpdateAndRetiredNewControlsCannotEmit()
        {
            var intents = new List<SaveLibraryIntent>(); var current = true;
            using var panel = new SaveLibraryPanel(View(phase: SaveLibraryOperationPhase.OutcomeUnknown), _ => { }, intents.Add, () => current);
            InvokeRetained(panel.Q<Button>("save-library-action-CheckOutcome"));
            Assert.That(panel.Q<Label>("save-library-operation-heading").text, Is.EqualTo("Save status needs checking"));
            Assert.That(panel.TryPublish(panel.CurrentBoundary, View(phase: SaveLibraryOperationPhase.OutcomeUnknown, lookupStarted: true)), Is.True);
            Assert.That(panel.Q<Label>("save-library-operation-heading").text, Is.EqualTo("Checking save status…"));
            Assert.That(panel.Q<Button>("save-library-action-CheckOutcome"), Is.Null);
            Assert.That(panel.TryPublish(panel.CurrentBoundary, View(empty: true)), Is.True);
            panel.Q<TextField>("save-library-create-name").value = "New name"; var create = panel.Q<Button>("save-library-action-CreateManual");
            current = false; InvokeRetained(create); Assert.That(panel.Q<Button>("save-library-confirm"), Is.Null); Assert.That(intents.Count, Is.EqualTo(1));
        }

        [Test] public void CreationBranchFilterAndUnknownGenerationDoNotClaimActiveMembership()
        {
            var initial = View(); var view = new SaveLibraryView(initial.SourceEpoch, initial.CatalogueGeneration, initial.ActiveBranchId, initial.ActiveBranchGeneration,
                initial.ContextCaption, initial.Current, initial.LastDurable, initial.Durability, initial.Query,
                new[] { Row("unknown.branch.generation", creationBranchGeneration: null) }, null, null, "1 save shown", SaveLibraryPageState.Ready, initial.Operation);
            using var panel = new SaveLibraryPanel(view, _ => { }, _ => { }, () => true);
            Assert.That(panel.Q<Button>("save-library-branch").text, Is.EqualTo("Created on any branch"));
            InvokeRetained(panel.Q<Button>("save-library-branch")); Assert.That(panel.Q<Button>("save-library-branch").text, Is.EqualTo("Created on active branch"));
            InvokeRetained(panel.Q<Button>("save-library-select"));
            Assert.That(panel.Q<Label>("save-library-selected-detail").text, Does.Contain("Created on branch"));
            Assert.That(panel.Q<Label>("save-library-diagnostic-target").text, Does.Contain("generation Not reported"));
            Assert.That(panel.Q<Foldout>("save-library-diagnostics").value, Is.False);
        }
    }
}
