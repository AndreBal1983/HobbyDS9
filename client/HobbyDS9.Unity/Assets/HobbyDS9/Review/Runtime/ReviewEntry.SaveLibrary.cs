#if HOBBYDS9_STATE_REPOSITORY_TRIAL
#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using HobbyDS9.CampaignRepositoryHost;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using HobbyDS9.TacticalReviewClient;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private const string RepositoryArgument = "--hobbyds9-repository-trial";
        private ReviewRepositoryTrial? repositoryTrial;
        private SaveLibraryPanel? repositoryPanel;
        private RepositorySaveWork? repositorySaveWork;
        private RepositoryPayloadWork? repositoryPayloadWork;
        private bool repositoryForeground;
        private bool repositoryCheckRequested;
        private bool repositoryRetiring;
        private bool repositoryPumpRequested;
        private double repositoryWaitUntil;
        private string repositoryNotice = "";

        private ReviewRepositoryTrial? ActiveRepository => repositoryTrial?.Current == true && !repositoryRetiring
            && ReferenceEquals(model, repositoryTrial.Owner) ? repositoryTrial : null;

        private static string? TrialId()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, RepositoryArgument);
            return index >= 0 && index + 1 < args.Length && Guid.TryParseExact(args[index + 1], "N", out var id)
                && id != Guid.Empty ? id.ToString("N") : null;
        }

        private void AddRepositoryTrialButton(VisualElement commands)
        {
            if (TrialId() == null || model?.SupportsRepositoryTrial != true || ActiveRepository != null) return;
            AddButton(commands, "Open isolated Save Library", OpenRepositoryTrial).name = "campaign-repository-open";
        }

        private async void OpenRepositoryTrial()
        {
            var id = TrialId(); var owner = model;
            if (id == null || owner == null || repositoryTrial != null || repositoryForeground) return;
            var capture = owner.PrepareRepositoryTrial(out var wire, out var adapter);
            if (capture == null) { campaignMenuNotice = "The current campaign is busy. Open the trial again after it settles."; Draw(); return; }
            var path = Path.Combine(UnityEngine.Application.persistentDataPath, "HobbyDS9StateRepositoryTrial-v1", id);
            var epoch = viewEpoch; var busy = new object();
            repositoryForeground = true;
            ShowCampaignBusy(busy, "Opening isolated Save Library…");
            ICampaignRepository? opened = null;
            var open = false;
            try
            {
                opened = await Task.Run(() =>
                {
                    var identity = capture.Execute(() => false).Image?.Edition.Identity;
                    if (identity == null) throw new RepositoryException(RepositoryError.ValidationFailed);
                    return new SqliteCampaignRepository(path, id, identity, new CycleCheckpointValidator(adapter!, identity));
                });
                if (this == null || !isActiveAndEnabled || viewEpoch != epoch || !ReferenceEquals(model, owner)) return;
                // A new game never silently appends to a previous run. Loading binds the confirmed branch or forks the selected save.
                repositoryTrial = new ReviewRepositoryTrial(owner, id, opened, new BranchInfo("branch-" + Guid.NewGuid().ToString("N"), null, 0), wire);
                opened = null; open = true; repositoryNotice = ""; repositoryPumpRequested = true;
            }
            catch (RepositoryException error) { campaignMenuNotice = ReviewRepositoryTrial.FailureCaption(error.Error); }
            catch (Exception) { campaignMenuNotice = "The isolated Save Library could not be opened."; }
            finally
            {
                opened?.Dispose(); repositoryForeground = false;
                HideCampaignBusy(busy); DisposeRetiredRepository();
                if (this != null && isActiveAndEnabled && ReferenceEquals(model, owner))
                { if (open) OpenCampaignArchive(true); else Draw(); }
            }
        }

        private bool DrawRepositoryLibrary()
        {
            var trial = ActiveRepository;
            if (trial == null) return false;
            trial.SaveAndQuit = campaignAfterSave != null && campaignContinuationLabel == "quit";
            var host = PagePanel("Isolated Save Library");
            if (campaignAfterSave != null) host.Add(Readout("After a confirmed save: " + campaignContinuationDescription));
            if (repositoryNotice.Length > 0) host.Add(Readout(repositoryNotice));
            var footer = new VisualElement(); footer.AddToClassList("campaign-dialog-actions"); host.Add(footer);
            AddButton(footer, "Keep playing", ResumeCampaign).SetEnabled(repositoryPayloadWork == null);
            AddButton(footer, "Refresh saved checkpoints", () => RefreshRepositoryLibrary(trial)).SetEnabled(!repositoryForeground);
            SaveLibraryPanel? widget = null;
            widget = new SaveLibraryPanel(trial.View(!trial.HasPage && !trial.PageError),
                intent => ReadRepositoryPage(trial, widget!, intent.Boundary, intent.Query),
                intent => ActOnRepositoryLibrary(trial, widget!, intent),
                () => this != null && isActiveAndEnabled && ReferenceEquals(ActiveRepository, trial)
                    && ReferenceEquals(widget, repositoryPanel) && widget?.panel != null && confirmation == null && campaignBusyToken == null);
            repositoryPanel = widget; host.Add(widget);
            if (!trial.HasPage && !trial.PageError && !repositoryForeground)
                widget.schedule.Execute(() => { if (PanelCurrent(trial, widget)) RefreshRepositoryLibrary(trial); });
            return true;
        }

        private bool PanelCurrent(ReviewRepositoryTrial trial, SaveLibraryPanel? widget)
            => this != null && isActiveAndEnabled && ReferenceEquals(trial, ActiveRepository) && widget?.panel != null
                && ReferenceEquals(widget, repositoryPanel) && root?.Contains(widget) == true;

        private void RefreshRepositoryLibrary(ReviewRepositoryTrial trial)
        {
            var widget = repositoryPanel;
            if (PanelCurrent(trial, widget)) ReadRepositoryPage(trial, widget!, widget!.CurrentBoundary, trial.Query.AtCursor(null));
        }

        private async void ReadRepositoryPage(ReviewRepositoryTrial trial, SaveLibraryPanel widget, SaveLibraryBoundary boundary, SaveLibraryQuery query)
        {
            if (!PanelCurrent(trial, widget) || repositoryForeground || repositorySaveWork != null)
            {
                widget.TryPublish(boundary, trial.View());
                return;
            }
            repositoryForeground = true;
            try
            {
                var request = trial.BuildQuery(query);
                var result = await Task.Run(() => trial.Store.QueryReferences(request));
                if (!PanelCurrent(trial, widget) || !widget.CurrentBoundary.Equals(boundary)) return;
                trial.PublishPage(query, request, result);
                widget.TryPublish(boundary, trial.View());
            }
            catch (Exception)
            {
                if (!PanelCurrent(trial, widget) || !widget.CurrentBoundary.Equals(boundary)) return;
                trial.FailPage(query); widget.TryPublish(boundary, trial.View());
            }
            finally { repositoryForeground = false; DisposeRetiredRepository(); }
        }

        private void ActOnRepositoryLibrary(ReviewRepositoryTrial trial, SaveLibraryPanel widget, SaveLibraryIntent intent)
        {
            if (!PanelCurrent(trial, widget) || !widget.CurrentBoundary.Equals(intent.Boundary)) return;
            trial.Observe();
            if (!trial.Matches(intent)) { repositoryNotice = "The game or save list changed. Review the action again."; Draw(); return; }
            switch (intent.Action)
            {
                case SaveLibraryAction.CreateManual:
                case SaveLibraryAction.Overwrite:
                    try
                    {
                        var status = trial.RequestManual(intent);
                        repositoryNotice = status == SaveRequestStatus.Queued ? "" : "The save could not be queued. Review the current game again.";
                    }
                    catch (ArgumentException) { repositoryNotice = "Choose a valid save name of up to 120 characters."; }
                    Draw(); break;
                case SaveLibraryAction.Validate:
                case SaveLibraryAction.Load:
                    ReadRepositoryPayload(trial, widget, intent.Target!, intent.Action == SaveLibraryAction.Load); break;
                case SaveLibraryAction.Export:
                    ExportRepositoryPayload(trial, intent.Target!); break;
                case SaveLibraryAction.CancelWaitingRequest:
                    if (Guid.TryParseExact(intent.PendingRequestId, "N", out var pending) && trial.Queue.CancelWaitingRequest(pending))
                    { repositoryNotice = "Waiting save canceled."; Draw(); }
                    break;
                case SaveLibraryAction.CheckOutcome:
                    repositoryCheckRequested = trial.Queue.Snapshot.OutcomeUnknown && trial.Queue.Snapshot.OperationId == intent.ExistingOperationId;
                    break;
                case SaveLibraryAction.ReturnToSession:
                case SaveLibraryAction.ContinueSingleplayer:
                    ResumeCampaign(); break;
                case SaveLibraryAction.QuitWithoutSaving:
                    if (trial.SaveAndQuit && RepositoryMayDepart()) QuitCampaignPlayer();
                    break;
                case SaveLibraryAction.StorageHelp:
                    ShowCampaignDialog("Trial storage", "Free space or check write access for the isolated save directory. Your running campaign remains open.", "Back", null);
                    break;
            }
        }

        private async void ReadRepositoryPayload(ReviewRepositoryTrial trial, SaveLibraryPanel widget, SaveLibraryRow target, bool load)
        {
            if (repositoryForeground || repositorySaveWork != null || trial.Queue.Snapshot.PendingRequestId.HasValue)
            { repositoryNotice = "A save operation is still pending. Try again when it settles."; Draw(); return; }
            var owner = trial.Owner; var frame = owner.Current.Frame; var epoch = viewEpoch; var boundary = widget.CurrentBoundary;
            var mounted = root;
            var work = trial.Queue.PreparePayloadRead(frame, target.CheckpointId).Work;
            if (work == null) { repositoryNotice = "A save operation is still pending. Try again when it settles."; Draw(); return; }
            repositoryForeground = true; repositoryPayloadWork = work;
            if (load) ShowCampaignBusy(work, "Loading and checking the selected checkpoint…");
            else widget.SetEnabled(false);
            bool CurrentRequest() => this != null && isActiveAndEnabled && ReferenceEquals(trial, ActiveRepository)
                && ReferenceEquals(mounted, root) && root?.panel != null && viewEpoch == epoch && ReferenceEquals(frame, owner.Current.Frame)
                && ReferenceEquals(repositoryPayloadWork, work) && !work.CancellationRequested
                && (load ? ReferenceEquals(campaignBusyToken, work) : PanelCurrent(trial, widget) && widget.CurrentBoundary.Equals(boundary));
            var restored = false;
            try
            {
                var checkedPayload = await Task.Run(() =>
                {
                    var payload = work.Execute();
                    var origin = payload.Error == CampaignRepositoryError.None && !load && !work.CancellationRequested
                        ? trial.Store.GetBranch(target.BranchId) : null;
                    return (payload, origin);
                });
                if (!CurrentRequest()) return;
                var result = checkedPayload.payload;
                if (!load) trial.RecordValidation(target.CheckpointId, result, checkedPayload.origin);
                if (!load || result.Error != CampaignRepositoryError.None)
                {
                    repositoryNotice = result.Error == CampaignRepositoryError.None ? "Selected checkpoint checked."
                        : result.StorageError.HasValue ? ReviewRepositoryTrial.FailureCaption(result.StorageError) : "This checkpoint could not be validated.";
                    return;
                }
                var oldBranch = trial.Branch;
                var continuation = trial.PrepareFork(target);
                var branch = await Task.Run(() => trial.ConfirmLoadBranch(target, oldBranch, continuation));
                if (!CurrentRequest()) return;
                restored = owner.RestoreRepositoryTrial(trial.Queue, work, result, trial.Store, branch);
                if (restored)
                {
                    trial.AfterLoad(branch, target); repositoryNotice = "Selected checkpoint restored.";
                    repositoryPumpRequested = true;
                    flow = new ReviewCombatFlow(owner); workspace = new ReviewWorkspace(flow);
                    ResetDemoKeyboard();
                }
                else repositoryNotice = "The current game changed. Review the checkpoint again.";
            }
            catch (RepositoryException error)
            {
                repositoryNotice = ReviewRepositoryTrial.FailureCaption(error.Error);
                if (load && target.OlderContinuation)
                    repositoryNotice += " Review the same checkpoint again to confirm its continuation branch.";
            }
            catch (Exception) { repositoryNotice = "This checkpoint could not be loaded."; }
            finally
            {
                work.Cancel(); trial.Queue.CompletePayloadValidation(work);
                repositoryPayloadWork = null; repositoryForeground = false;
                var hidden = HideCampaignBusy(work); DisposeRetiredRepository();
                if (this != null && isActiveAndEnabled && ReferenceEquals(trial, ActiveRepository))
                { if (restored) ResumeCampaign(); else if (hidden || PanelCurrent(trial, widget)) Draw(); }
            }
        }

        private async void ExportRepositoryPayload(ReviewRepositoryTrial trial, SaveLibraryRow target)
        {
            if (repositoryForeground || repositorySaveWork != null)
            { repositoryNotice = "A save operation is still pending. Try again when it settles."; Draw(); return; }
            repositoryForeground = true;
            try
            {
                var exportId = "export-" + Guid.NewGuid().ToString("N");
                var path = await Task.Run(() => trial.Store.ExportJson(target.CheckpointId, exportId));
                repositoryNotice = "JSON copy exported to " + path;
            }
            catch (RepositoryException error) { repositoryNotice = ReviewRepositoryTrial.FailureCaption(error.Error); }
            catch (Exception) { repositoryNotice = "The JSON copy could not be exported."; }
            finally
            {
                repositoryForeground = false; DisposeRetiredRepository();
                if (PanelCurrent(trial, repositoryPanel)) Draw();
            }
        }

        private bool RepositoryMayDepart()
        {
            var trial = ActiveRepository;
            if (trial == null || !repositoryForeground && trial.Queue.CanReplaceSession && !trial.Queue.Snapshot.PendingRequestId.HasValue) return true;
            campaignMenuNotice = "A save is still pending. Open the Save Library to check its status before leaving.";
            Draw(); return false;
        }

        private void Update()
        {
            if (repositoryTrial != null && (model != repositoryTrial.Owner || repositoryTrial.Owner.Closed)) RetireRepositoryTrial();
            var trial = ActiveRepository;
            if (trial == null || trial.Owner.Busy || trial.Owner.Current.HasPendingWork) return;
            trial.Observe();
            if (repositoryForeground || repositorySaveWork != null || Time.realtimeSinceStartupAsDouble < repositoryWaitUntil) return;
            var snapshot = trial.Queue.Snapshot;
            if (snapshot.OutcomeUnknown && !repositoryCheckRequested) return;
            if (!repositoryPumpRequested && !snapshot.PendingAutosave && !snapshot.PendingRequestId.HasValue && !snapshot.OutcomeUnknown
                && snapshot.HeadConfirmed) return;
            repositoryCheckRequested = false;
            var admission = trial.Queue.PrepareNext(trial.Owner.Current.Frame);
            repositoryPumpRequested = admission.Status == SaveRequestStatus.Busy;
            if (admission.Work != null) RunRepositorySave(trial, admission.Work);
            else if (admission.Status == SaveRequestStatus.NeedsConfirmation && admission.PendingRequestId.HasValue && PanelCurrent(trial, repositoryPanel)) Draw();
        }

        private async void RunRepositorySave(ReviewRepositoryTrial trial, RepositorySaveWork work)
        {
            repositorySaveWork = work;
            trial.BeginSave(work);
            if (work.Kind != SaveWorkKind.Maintenance && PanelCurrent(trial, repositoryPanel)) Draw();
            var result = await Task.Run(work.Execute);
            trial.CompleteSave(work, result);
            repositorySaveWork = null;
            repositoryPumpRequested = true;
            if (result.Error.HasValue || result.Outcome == SaveOutcome.Unknown) repositoryWaitUntil = Time.realtimeSinceStartupAsDouble + 2;
            DisposeRetiredRepository();
            if (this == null || !isActiveAndEnabled || !ReferenceEquals(trial, ActiveRepository)) return;
            if (work.Manual && trial.Queue.Snapshot.CanQuitAfterManualSave && campaignAfterSave != null)
            {
                var next = campaignAfterSave; campaignAfterSave = null; campaignContinuationLabel = ""; campaignProtectedLoadSlot = "";
                next(); return;
            }
            if (PanelCurrent(trial, repositoryPanel))
            {
                if (result.Outcome == SaveOutcome.Committed && work.Kind != SaveWorkKind.RefreshHead) RefreshRepositoryLibrary(trial);
                else Draw();
            }
            else if (playerGuidance != null && !demoLanding) playerGuidance.text = PlayerMessage();
        }

        private string? RepositoryWarning()
        {
            var trial = ActiveRepository;
            var snapshot = trial?.Queue.Snapshot;
            if (snapshot == null || trial!.Failure == null) return null;
            var saved = snapshot.LastDurable == null ? "No confirmed save yet." : "Last confirmed save: turn " + snapshot.LastDurable.Turn + ".";
            return trial.Failure + " " + saved + " Open Save / Load for details.";
        }

        private void RetireRepositoryTrial()
        {
            repositoryRetiring = true; repositoryPanel?.Dispose(); repositoryPanel = null;
            repositoryPayloadWork?.Cancel(); repositorySaveWork?.Cancel();
            DisposeRetiredRepository();
        }

        private void DisposeRetiredRepository()
        {
            if (!repositoryRetiring || repositoryForeground || repositorySaveWork != null) return;
            repositoryTrial?.Dispose(); repositoryTrial = null; repositoryRetiring = false;
            repositoryCheckRequested = false; repositoryPumpRequested = false; repositoryWaitUntil = 0;
        }
    }
}
#endif
