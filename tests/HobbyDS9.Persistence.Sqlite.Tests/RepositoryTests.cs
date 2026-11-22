using System.Diagnostics;
using System.Text;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace HobbyDS9.Persistence.Sqlite.Tests;

public sealed class RepositoryTests : IDisposable
{
    readonly string directory = Path.Combine(Path.GetTempPath(), "hobbyds9-repository-" + Guid.NewGuid().ToString("N"));
    static readonly ICheckpointValidator Validator = new TestValidator();
    static CheckpointImage Image(int turn = 1) => new(Encoding.UTF8.GetBytes(turn.ToString(System.Globalization.CultureInfo.InvariantCulture)), "test-edition-v1", turn, 0, "Planning");
    SqliteCampaignRepository Open(Action<RepositoryWriteStage>? fault = null) => new(directory, "campaign", "test-edition-v1", Validator, fault);
    static CheckpointCommit First(string op = "first", ReferenceUpdate? reference = null) => new(op, "main", 0, null, Image(), reference, "epoch-1");
    static void Error(RepositoryError expected, Action action) => Assert.Equal(expected, Assert.Throws<RepositoryException>(action).Error);

    [Fact] public void ExactBytesDetachedImagesAndDurableReceiptSurviveReopen()
    {
        var bytes = Encoding.UTF8.GetBytes("1"); var image = new CheckpointImage(bytes, "test-edition-v1", 1, 0, "Planning"); bytes[0] = 57;
        var returned = image.ReadPayload(); returned[0] = 57;
        DurableReceipt first;
        using (var repo = Open())
        {
            first = repo.CommitCheckpoint(new CheckpointCommit("first", "main", 0, null, image));
            Assert.Equal(image.ReadPayload(), repo.ReadCheckpoint(first.CheckpointId!).ReadPayload());
        }
        using var reopened = Open();
        Assert.Equal(first.StorageSequence, reopened.FindOperation("first")!.StorageSequence);
        Assert.Equal(first.CheckpointId, reopened.GetBranch("main").Head);
        Assert.Equal(first.CommittedUtc, reopened.FindOperation("first")!.CommittedUtc);
    }
    [Fact] public void IdenticalRetryPrecedesCasAndEverySemanticChangeConflicts()
    {
        using var repo = Open(); var first = repo.CommitCheckpoint(First());
        var next = repo.CommitCheckpoint(new("next", "main", 1, first.CheckpointId, Image(2)));
        Assert.Equal(first.CheckpointId, repo.CommitCheckpoint(First()).CheckpointId);
        Error(RepositoryError.OperationIdConflict, () => repo.CommitCheckpoint(new("first", "main", 0, null, Image(), sourceEpoch: "other")));
        Error(RepositoryError.OperationIdConflict, () => repo.CommitCheckpoint(new("first", "main", 0, null, Image(2), sourceEpoch: "epoch-1")));
        Error(RepositoryError.Conflict, () => repo.CommitCheckpoint(new("stale", "main", 1, first.CheckpointId, Image(3))));
        Assert.Null(repo.FindOperation("stale")); Assert.Equal(next.CheckpointId, repo.GetBranch("main").Head);
    }
    [Theory] [InlineData(RepositoryWriteStage.AfterPayload)] [InlineData(RepositoryWriteStage.BeforeCommit)]
    public void RealDiskFailureRollsBackPayloadSequenceBranchAndReference(RepositoryWriteStage failAt)
    {
        using (var repo = Open()) repo.CommitCheckpoint(First(reference: new("manual", 0, SaveReferenceKind.Manual, "original")));
        using (var repo = Open(stage => { if (stage == failAt) throw new IOException("injected disk fault"); }))
        {
            var branch = repo.GetBranch("main");
            Error(RepositoryError.WriteFailed, () => repo.CommitCheckpoint(new("failed", "main", branch.Generation, branch.Head, Image(2), new("manual", 1, SaveReferenceKind.Manual, "changed"))));
            Assert.Equal(branch.Head, repo.GetBranch("main").Head); Assert.Null(repo.FindOperation("failed")); Assert.Equal("original", Assert.Single(repo.ListReferences()).Name);
        }
        using var reopened = Open(); var head = reopened.GetBranch("main");
        Assert.Equal(2, reopened.CommitCheckpoint(new("next", "main", 1, head.Head, Image(2))).StorageSequence);
    }
    [Fact] public void LostAcknowledgmentIsUnknownAndRecoveredIdenticalRetryDoesNotDuplicate()
    {
        DurableReceipt recovered;
        using (var repo = Open(stage => { if (stage == RepositoryWriteStage.AfterCommit) throw new IOException("response lost"); }))
        {
            Error(RepositoryError.OutcomeUnknown, () => repo.CommitCheckpoint(First()));
            recovered = repo.FindOperation("first")!; Assert.NotNull(recovered);
        }
        using var reopened = Open();
        Assert.Equal(recovered.CheckpointId, reopened.CommitCheckpoint(First()).CheckpointId);
        Assert.Equal(1, reopened.GetBranch("main").Generation);
        Assert.Null(reopened.FindOperation("confirmed-absent"));
    }
    [Fact] public void OlderCheckpointForkHasIndependentFutureAndManualPins()
    {
        using var repo = Open(); var first = repo.CommitCheckpoint(First(reference: new("manual", 0, SaveReferenceKind.Manual, "初期 🖖")));
        var future = repo.CommitCheckpoint(new("future", "main", 1, first.CheckpointId, Image(2)));
        repo.ForkBranch("fork", "alternative", first.CheckpointId!);
        Assert.Equal(first.CheckpointId, repo.ForkBranch("fork", "alternative", first.CheckpointId!).CheckpointId);
        var other = repo.CommitCheckpoint(new("other", "alternative", 0, first.CheckpointId, Image(3)));
        Assert.Equal(future.CheckpointId, repo.GetBranch("main").Head);
        Assert.Equal(other.CheckpointId, repo.GetBranch("alternative").Head);
        Assert.Equal(first.CheckpointId, Assert.Single(repo.ListReferences()).CheckpointId);
        Assert.Equal(Encoding.UTF8.GetBytes("1"), repo.ReadCheckpoint(first.CheckpointId!).ReadPayload());
    }
    [Fact] public void HundredsOfUnicodeNamesPaginationCasAndDeleteRetainImmutablePayload()
    {
        using var repo = Open(); var cp = repo.CommitCheckpoint(First()).CheckpointId!;
        for (int i = 0; i < 220; i++) repo.SetReference("create-" + i, new("slot-" + i.ToString("D3"), 0, SaveReferenceKind.Manual, "同じ名前 🖖"), cp);
        var rows = new List<SaveReference>(); string cursor = "";
        while (true) { var page = repo.ListReferences(cursor, 37); if (page.Count == 0) break; rows.AddRange(page); cursor = page[^1].Id; }
        Assert.Equal(220, rows.Count); Assert.Equal(220, rows.Select(r => r.Id).Distinct().Count());
        repo.SetReference("rename", new("slot-000", 1, SaveReferenceKind.Manual, "changed"), cp);
        Assert.Equal(2, repo.SetReference("rename", new("slot-000", 1, SaveReferenceKind.Manual, "changed"), cp).ReferenceGeneration);
        Error(RepositoryError.Conflict, () => repo.DeleteReference("stale-delete", "slot-000", 1));
        repo.DeleteReference("delete", "slot-000", 2); Assert.Equal(3, repo.DeleteReference("delete", "slot-000", 2).ReferenceGeneration);
        Error(RepositoryError.Conflict, () => repo.SetReference("resurrect", new("slot-000", 0, SaveReferenceKind.Manual, "bad"), cp));
        Assert.NotNull(repo.ReadCheckpoint(cp));
    }
    [Fact] public void MetadataSpoofWrongEditionAndCorruptBytesNeverLoad()
    {
        string cp;
        using (var repo = Open())
        {
            Error(RepositoryError.ValidationFailed, () => repo.CommitCheckpoint(new("spoof", "main", 0, null, new CheckpointImage(Encoding.UTF8.GetBytes("1"), "test-edition-v1", 9, 0, "Planning"))));
            Error(RepositoryError.ValidationFailed, () => repo.CommitCheckpoint(new("bad", "main", 0, null, new CheckpointImage(Encoding.UTF8.GetBytes("bad"), "test-edition-v1", 1, 0, "Planning"))));
            cp = repo.CommitCheckpoint(First()).CheckpointId!;
        }
        using (var sql = new SqliteConnection("Data Source=" + Path.Combine(directory, "campaign.sqlite") + ";Pooling=False"))
        { sql.Open(); using var c = sql.CreateCommand(); c.CommandText = "UPDATE Checkpoint SET Payload=X'39'"; c.ExecuteNonQuery(); }
        using var reopened = Open(); Error(RepositoryError.Corrupt, () => reopened.ReadCheckpoint(cp));
    }
    [Fact] public void UnknownSchemaAndWrongCampaignOrEditionAreRejectedWithoutRewriting()
    {
        using (var repo = Open()) repo.CommitCheckpoint(First());
        Error(RepositoryError.IncompatibleEdition, () => new SqliteCampaignRepository(directory, "campaign", "other", Validator).Dispose());
        Error(RepositoryError.Conflict, () => new SqliteCampaignRepository(directory, "other", "test-edition-v1", Validator).Dispose());
        using (var sql = new SqliteConnection("Data Source=" + Path.Combine(directory, "campaign.sqlite") + ";Pooling=False"))
        { sql.Open(); using var c = sql.CreateCommand(); c.CommandText = "PRAGMA user_version=999"; c.ExecuteNonQuery(); }
        var before = File.ReadAllBytes(Path.Combine(directory, "campaign.sqlite"));
        Error(RepositoryError.IncompatibleSchema, () => Open().Dispose());
        Assert.Equal(before, File.ReadAllBytes(Path.Combine(directory, "campaign.sqlite")));
    }
    [Fact] public async Task TwoWriterProcessesAreExcludedAndLeaseRecoversAfterKill()
    {
        using (var repo = Open())
        {
            Error(RepositoryError.WriterInUse, () => Open().Dispose());
            using var denied = Child("lease"); await denied.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20)); Assert.Equal(23, denied.ExitCode);
        }
        using var child = Child("lease"); await Ready(child);
        Error(RepositoryError.WriterInUse, () => Open().Dispose());
        child.Kill(true); await child.WaitForExitAsync(); using var recovered = ReopenAfterKilledProcess(); Assert.Null(recovered.FindOperation("never-written"));
    }
    [Theory] [InlineData("before", false)] [InlineData("after", true)]
    public async Task KilledWriterBeforeOrAfterCommitRecoversExactlyOneCompleteCheckpoint(string stage, bool committed)
    {
        string first;
        using (var repo = Open()) first = repo.CommitCheckpoint(First()).CheckpointId!;
        using var child = Child(stage); await Ready(child); child.Kill(true); await child.WaitForExitAsync();
        using var reopened = ReopenAfterKilledProcess(); var receipt = reopened.FindOperation("child");
        Assert.Equal(committed, receipt != null);
        Assert.Equal(committed ? receipt!.CheckpointId : first, reopened.GetBranch("main").Head);
        Assert.Equal(Encoding.UTF8.GetBytes(committed ? "2" : "1"), reopened.ReadCheckpoint(reopened.GetBranch("main").Head!).ReadPayload());
    }
    [Fact] public void InvalidPathsNamesBoundsAndDeletedReferenceKindsAreRejected()
    {
        Error(RepositoryError.InvalidRequest, () => new SqliteCampaignRepository("relative", "campaign", "test-edition-v1", Validator).Dispose());
        using var repo = Open(); var cp = repo.CommitCheckpoint(First()).CheckpointId!;
        Error(RepositoryError.InvalidRequest, () => repo.SetReference("bad", new("../escape", 0, SaveReferenceKind.Manual, "a"), cp));
        Error(RepositoryError.InvalidRequest, () => repo.SetReference("bad", new("good", 0, (SaveReferenceKind)99, "a"), cp));
        Error(RepositoryError.InvalidRequest, () => repo.SetReference("bad", new("good", 0, SaveReferenceKind.Manual, new string('a', 121)), cp));
        Assert.Throws<ArgumentException>(() => new CheckpointImage(Array.Empty<byte>(), "test", 1, 0, "Planning"));
    }
    Process Child(string mode)
    {
        var root = FindRoot(); var dll = Path.Combine(root, "tools/HobbyDS9.Persistence.CrashProbe/bin/Release/net10.0/HobbyDS9.Persistence.CrashProbe.dll");
        var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? Path.GetFullPath(Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"));
        var start = new ProcessStartInfo(host) { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(dll); start.ArgumentList.Add(directory); start.ArgumentList.Add(mode);
        return Process.Start(start)!;
    }
    SqliteCampaignRepository ReopenAfterKilledProcess()
    {
        // Windows process exit notification can precede final kernel handle teardown.
        // Keep production's immediate WriterInUse result; bound only this crash harness.
        var until = Stopwatch.StartNew();
        while (true)
        {
            try { return Open(); }
            catch (RepositoryException e) when (e.Error == RepositoryError.WriterInUse && until.Elapsed < TimeSpan.FromSeconds(3)) { Thread.Sleep(20); }
        }
    }
    static async Task Ready(Process process)
    {
        try
        {
            var line = await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(20));
            if (line != "READY")
            {
                if (!process.HasExited) process.Kill(true);
                await process.WaitForExitAsync();
                Assert.Fail("Child output: " + line + " " + await process.StandardError.ReadToEndAsync());
            }
        }
        catch { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } throw; }
    }
    static string FindRoot()
    { var p = new DirectoryInfo(AppContext.BaseDirectory); while (!File.Exists(Path.Combine(p.FullName, "HobbyDS9.slnx"))) p = p.Parent!; return p.FullName; }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    sealed class TestValidator : ICheckpointValidator
    { public CheckpointImage Validate(byte[] bytes) => new(bytes, "test-edition-v1", int.Parse(Encoding.UTF8.GetString(bytes), System.Globalization.CultureInfo.InvariantCulture), 0, "Planning"); }
}
