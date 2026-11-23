using HobbyDS9.Contracts;
using HobbyDS9.Persistence;
using HobbyDS9.TacticalReviewClient;
using Xunit;
using static HobbyDS9.CampaignRepositoryHost.Tests.RepositoryFixture;

namespace HobbyDS9.CampaignRepositoryHost.Tests;

public sealed class SaveQueueTests
{
    internal sealed class Faults : ICampaignRepository
    {
        private readonly ICampaignRepository storage;
        public Faults(ICampaignRepository storage) => this.storage = storage;

        public Func<CheckpointCommit, DurableReceipt>? Commit;
        public Func<string, DurableReceipt?>? Lookup;
        public Func<string, BranchInfo>? Branch;
        public DurableReceipt CommitCheckpoint(CheckpointCommit request) =>
            Commit == null ? storage.CommitCheckpoint(request) : Commit(request);
        public DurableReceipt? FindOperation(string id) =>
            Lookup == null ? storage.FindOperation(id) : Lookup(id);
        public BranchInfo GetBranch(string id) =>
            Branch == null ? storage.GetBranch(id) : Branch(id);
        public DurableReceipt ForkBranch(string op, string branch, string origin) => storage.ForkBranch(op, branch, origin);
        public DurableReceipt SetReference(string op, ReferenceUpdate reference, string checkpoint) => storage.SetReference(op, reference, checkpoint);
        public DurableReceipt DeleteReference(string op, string id, long generation) => storage.DeleteReference(op, id, generation);
        public IReadOnlyList<SaveReference> ListReferences(string after = "", int limit = 50) => storage.ListReferences(after, limit);
        public ReferencePage QueryReferences(ReferenceQuery query) => storage.QueryReferences(query);
        public CheckpointImage ReadCheckpoint(string checkpoint) => storage.ReadCheckpoint(checkpoint);
        public DurableReceipt ImportJson(string path) => storage.ImportJson(path);
        public string ExportJson(string checkpoint, string export, bool overwrite = false) => storage.ExportJson(checkpoint, export, overwrite);
        public string Backup(string id) => storage.Backup(id);
        public DurableReceipt ApplyRetention(string op, BranchInfo branch, int rolling = 10, int milestones = 3, long budget = 512L * 1024 * 1024) =>
            storage.ApplyRetention(op, branch, rolling, milestones, budget);
        public RepositoryUsage GetStorageUsage(BranchInfo branch, long budget = 512L * 1024 * 1024) => storage.GetStorageUsage(branch, budget);
        public void Dispose() { }
    }

    private static RepositorySaveQueue Queue(RepositoryFixture f, PhysicalReviewHost host, ICampaignRepository repository,
        Action<int>? backoff = null) => new(host, host.Read(Human), repository, new BranchInfo("main", null, 0), f.Wire, workerBackoff: backoff);

    [Fact]
    public void AutosaveCoalescesToLatestBoundaryAndManualIntentIsNeverRestamped()
    {
        using var f = new RepositoryFixture();
        var host = f.Host();
        var initial = f.Image(host);
        using var repository = f.Repository(initial);
        var queue = Queue(f, host, repository);
        Assert.Equal(SaveRequestStatus.Queued, queue.OfferAutosave(host.Read(Human), AutosaveBoundary.CompletedTurn));
        var first = Assert.IsType<RepositorySaveWork>(queue.PrepareNext(host.Read(Human)).Work);
        Assert.Equal(1, queue.Snapshot.InFlightImages);
        Assert.Equal(SaveRequestStatus.Queued, queue.RequestManual(host.Read(Human), Manual(), out var waiting));
        Advance(host);
        Assert.Equal(SaveRequestStatus.Queued, queue.OfferAutosave(host.Read(Human), AutosaveBoundary.CompletedTurn));
        Advance(host);
        Assert.Equal(SaveRequestStatus.Coalesced, queue.OfferAutosave(host.Read(Human), AutosaveBoundary.CompletedTurn));
        Assert.Equal(SaveRequestStatus.Busy, queue.PrepareNext(host.Read(Human)).Status);
        var firstResult = first.Execute();
        Assert.True(queue.Complete(first));
        Assert.False(queue.Complete(first));
        Assert.Equal(initial.ReadPayload(), repository.ReadCheckpoint(firstResult.Receipt!.CheckpointId!).ReadPayload());
        Assert.True(queue.Snapshot.HasUnsavedProgress);
        Run(queue, host); // Retention is a separate worker.
        var changed = queue.PrepareNext(host.Read(Human));
        Assert.Equal(SaveRequestStatus.NeedsConfirmation, changed.Status);
        Assert.Equal(waiting, changed.PendingRequestId);
        Assert.Equal("manual", changed.ManualReference!.Id);
        Assert.True(queue.Snapshot.ManualNeedsConfirmation);
        var latest = Run(queue, host);
        Assert.Equal(f.Bytes(host), repository.ReadCheckpoint(latest.Receipt!.CheckpointId!).ReadPayload());
        Assert.False(queue.Snapshot.HasUnsavedProgress);
        Assert.False(queue.Snapshot.CanQuitAfterManualSave);
        Assert.Equal(2, repository.ListReferences().Count);
        Assert.True(queue.Rebind(host, host.Read(Human), repository, repository.GetBranch("main")));
        Assert.False(queue.Snapshot.ManualNeedsConfirmation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LostAcknowledgementKeepsReceiptAndReconcilesActualHead(bool failHeadRead)
    {
        using var f = new RepositoryFixture();
        var host = f.Host();
        using var storage = f.Repository(f.Image(host));
        using var faults = new Faults(storage);
        var queue = Queue(f, host, faults);
        Assert.Equal(SaveRequestStatus.Queued, queue.RequestManual(host.Read(Human), Manual(), out _));
        var future = f.Host();
        Advance(future);
        var futureImage = CycleCheckpointValidator.Map(f.Image(future));
        DurableReceipt? first = null, newer = null;
        CheckpointCommit? initial = null;
        faults.Commit = request =>
        {
            if (initial != null)
            {
                Assert.Same(initial, request);
                return storage.CommitCheckpoint(request);
            }
            initial = request;
            first = storage.CommitCheckpoint(request);
            newer = storage.CommitCheckpoint(new("independent-writer", "main", first.BranchGeneration, first.CheckpointId, futureImage));
            throw new RepositoryException(RepositoryError.OutcomeUnknown);
        };
        faults.Lookup = _ => throw new RepositoryException(RepositoryError.Unavailable);
        Assert.Equal(SaveOutcome.Unknown, Run(queue, host).Outcome);
        Assert.True(queue.Snapshot.OutcomeUnknown);
        Assert.Equal(1, queue.Snapshot.InFlightImages);
        Assert.False(queue.CanReplaceSession);
        Assert.False(queue.Snapshot.CanQuitAfterManualSave);
        faults.Lookup = null;
        if (failHeadRead) faults.Branch = _ => throw new RepositoryException(RepositoryError.Unavailable);
        var resolved = Run(queue, host);
        Assert.Equal(SaveOutcome.Committed, resolved.Outcome);
        Assert.Equal(first!.Fingerprint, resolved.Receipt!.Fingerprint);
        Assert.Equal(first.CheckpointId, queue.Snapshot.LastDurableCheckpointId);
        Assert.False(queue.Snapshot.OutcomeUnknown);
        Assert.Equal(0, queue.Snapshot.InFlightImages);
        if (failHeadRead)
        {
            Assert.Null(queue.Snapshot.ConfirmedBranch);
            Assert.Equal(RepositoryError.Unavailable, queue.Snapshot.LastError);
            var refresh = Assert.IsType<RepositorySaveWork>(queue.PrepareNext(host.Read(Human)).Work);
            Assert.Equal(SaveWorkKind.RefreshHead, refresh.Kind);
            faults.Branch = null;
            refresh.Execute();
            Assert.True(queue.Complete(refresh));
        }
        Assert.Equal(newer!.CheckpointId, queue.Snapshot.ConfirmedBranch!.Head);
        Assert.Equal(newer.BranchGeneration, queue.Snapshot.ConfirmedBranch.Generation);
    }

    [Fact]
    public void BusyRetriesAreBoundedAndStorageFullCannotAuthorizeQuit()
    {
        using var f = new RepositoryFixture();
        var host = f.Host();
        using var storage = f.Repository(f.Image(host));
        using var faults = new Faults(storage);
        var delays = new List<int>();
        var queue = Queue(f, host, faults, delays.Add);
        var tries = 0;
        CheckpointCommit? original = null;
        faults.Commit = request =>
        {
            if (original == null) original = request;
            Assert.Same(original, request);
            if (++tries <= 3) throw new RepositoryException(RepositoryError.Busy);
            throw new RepositoryException(RepositoryError.StorageFull);
        };
        Assert.Equal(SaveRequestStatus.Queued, queue.RequestManual(host.Read(Human), Manual(), out _));
        var before = f.Bytes(host);
        var refused = Run(queue, host);
        Assert.Equal(SaveOutcome.NotCommitted, refused.Outcome);
        Assert.Equal(RepositoryError.StorageFull, refused.Error);
        Assert.Equal(new[] { 125, 250, 500 }, delays);
        Assert.Equal(4, tries);
        Assert.False(queue.Snapshot.CanQuitAfterManualSave);
        Assert.True(queue.Snapshot.HasUnsavedProgress);
        Assert.Equal(before, f.Bytes(host));
        faults.Commit = null;
        Assert.Equal(SaveRequestStatus.Queued, queue.RequestManual(host.Read(Human), Manual(), out _));
        Assert.Equal(SaveOutcome.Committed, Run(queue, host).Outcome);
        Assert.True(queue.Snapshot.CanQuitAfterManualSave);
    }

    [Fact]
    public void ConfirmedMissingOperationDoesNotRetryTheWrite()
    {
        using var f = new RepositoryFixture();
        var host = f.Host();
        using var repository = f.Repository(f.Image(host));
        using var faults = new Faults(repository);
        var queue = Queue(f, host, faults);
        var writes = 0;
        faults.Commit = _ =>
        {
            writes++;
            throw new RepositoryException(RepositoryError.OutcomeUnknown);
        };
        Assert.Equal(SaveRequestStatus.Queued, queue.RequestManual(host.Read(Human), Manual(), out var cancelled));
        Assert.True(queue.CancelWaitingRequest(cancelled));
        Assert.Equal(SaveRequestStatus.Unchanged, queue.PrepareNext(host.Read(Human)).Status);
        Assert.Equal(SaveRequestStatus.Queued, queue.RequestManual(host.Read(Human), Manual(), out _));

        Assert.Equal(SaveOutcome.NotCommitted, Run(queue, host).Outcome);
        Assert.Equal(1, writes);
        Assert.Empty(repository.ListReferences());
        Assert.True(queue.Snapshot.HeadConfirmed);
        Assert.True(queue.Snapshot.HasUnsavedProgress);
        Assert.False(queue.Snapshot.CanQuitAfterManualSave);
    }

    [Fact]
    public void ReceiptFromAReplacedSessionCannotMarkItSavedOrAdmitAnotherWrite()
    {
        using var f = new RepositoryFixture();
        var host = f.Host();
        var image = f.Image(host);
        using var repository = f.Repository(image);
        var queue = Queue(f, host, repository);
        Assert.Equal(SaveRequestStatus.Queued, queue.RequestManual(host.Read(Human), Manual(), out _));
        var write = Assert.IsType<RepositorySaveWork>(queue.PrepareNext(host.Read(Human)).Work);
        Advance(host);
        var continuation = host.PrepareRepositoryRestore(host.Read(Human), image.ReadPayload(), image.Edition.Identity, f.Wire).Work!;
        var loaded = continuation.Execute(() => false);
        Assert.Equal(CampaignRepositoryError.None, host.CompleteRepositoryRestore(continuation, loaded, out var current));
        var receipt = write.Execute();
        Assert.Equal(SaveOutcome.Committed, receipt.Outcome);
        Assert.True(queue.Complete(write));
        Assert.Equal(receipt.Receipt!.CheckpointId, repository.GetBranch("main").Head);
        Assert.Null(queue.Snapshot.LastDurable);
        Assert.False(queue.Snapshot.CanQuitAfterManualSave);
        Assert.Equal(SaveRequestStatus.Stale, queue.RequestManual(current!, Manual("other"), out _));
        Assert.Equal(SaveRequestStatus.Stale, queue.PrepareNext(current!).Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelAfterValidationOrBusyAdoptionDiscardsCandidate(bool busyFirst)
    {
        using var f = new RepositoryFixture();
        var host = f.Host();
        var image = f.Image(host);
        using var repository = f.Repository(image);
        var receipt = repository.CommitCheckpoint(new("original", "main", 0, null, CycleCheckpointValidator.Map(image), Manual()));
        Advance(host);
        var before = f.Bytes(host);
        var queue = new RepositorySaveQueue(host, host.Read(Human), repository, repository.GetBranch("main"), f.Wire);
        var frame = host.Read(Human);
        var work = Assert.IsType<RepositoryPayloadWork>(queue.PreparePayloadRead(frame, receipt.CheckpointId!).Work);
        Assert.Equal(CampaignRepositoryError.None, work.Execute().Error);
        Assert.Equal(SaveRequestStatus.Busy, queue.PrepareNext(frame).Status);
        if (busyFirst)
        {
            var body = host.CaptureBeltBodies(frame)!.Bodies.First();
            var command = new BeltBodyCommandDto
            {
                ActorId = Human, CommandId = "command.repository.restore.busy",
                ExpectedTurn = frame.Status.Turn, ExpectedRevision = frame.Status.CampaignRevision,
                Action = BeltBodyAction.StartSurvey, BodyId = body.BodyId, ShipId = body.SurveyShipIds.First()
            };
            var preview = host.PrepareBeltBodyInteraction(frame, BeltBodyInteractionKind.Preview, command);
            Assert.Equal(CampaignRepositoryError.Busy, queue.CompletePayloadRestore(work, out _));
            Assert.Equal(1, queue.Snapshot.InFlightImages);
            work.Cancel();
            Assert.True(host.CompleteBeltBodyInteraction(preview, preview.Execute()));
        }
        else work.Cancel();

        Assert.Equal(CampaignRepositoryError.Cancelled, queue.CompletePayloadRestore(work, out var adopted));
        Assert.Null(adopted);
        Assert.Equal(0, queue.Snapshot.InFlightImages);
        Assert.Equal(before, f.Bytes(host));
        var retry = Assert.IsType<RepositoryPayloadWork>(queue.PreparePayloadRead(host.Read(Human), receipt.CheckpointId!).Work);
        var done = retry.Execute();
        Assert.Equal(CampaignRepositoryError.None, queue.CompletePayloadRestore(retry, out adopted));
        Assert.True(queue.Rebind(host, adopted!, repository, repository.GetBranch("main")));
        Assert.True(queue.ConfirmRestoredCheckpoint(done.Loaded!));
        Assert.False(queue.Snapshot.HasUnsavedProgress);
        Assert.Equal(image.ReadPayload(), f.Bytes(host));
    }
}
