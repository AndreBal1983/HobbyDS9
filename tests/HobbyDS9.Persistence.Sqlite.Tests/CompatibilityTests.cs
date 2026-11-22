using System.Text;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace HobbyDS9.Persistence.Sqlite.Tests;

public sealed class CompatibilityTests : IDisposable
{
    readonly string directory = Path.Combine(Path.GetTempPath(), "hobbyds9-import-" + Guid.NewGuid().ToString("N"));
    readonly FixtureValidator validator = new();
    SqliteCampaignRepository Open(Action<RepositoryWriteStage>? fault = null) => new(Path.Combine(directory, "repository"), "campaign", "fixture-not-game", validator, fault);
    static CheckpointImage Image(int turn) => new(Encoding.UTF8.GetBytes("{\"turn\":" + turn + "}"), "fixture-not-game", turn, 0, "Planning");
    string Source(byte[] bytes)
    { Directory.CreateDirectory(directory); var path = Path.Combine(directory, "original.json"); File.WriteAllBytes(path, bytes); return path; }
    [Fact] public void ValidatedImportIsIdempotentPreservesOriginalAndRecoveryCopyAndExportsExactBytes()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"turn\":1}\r\n"); var source = Source(bytes);
        using var repo = Open(); var imported = repo.ImportJson(source); var second = repo.ImportJson(source);
        Assert.Equal(imported.CheckpointId, second.CheckpointId); Assert.Equal(imported.OperationId, second.OperationId);
        Assert.Single(repo.ListReferences()); Assert.Equal(SaveReferenceKind.Manual, repo.ListReferences()[0].Kind);
        Assert.Equal(bytes, File.ReadAllBytes(source));
        Assert.Equal(bytes, File.ReadAllBytes(Assert.Single(Directory.GetFiles(Path.Combine(directory, "repository/migration-backups")))));
        var exported = repo.ExportJson(imported.CheckpointId!, "portable"); Assert.Equal(bytes, File.ReadAllBytes(exported));
        Assert.Equal(RepositoryError.Conflict, Assert.Throws<RepositoryException>(() => repo.ExportJson(imported.CheckpointId!, "portable")).Error);
    }
    [Fact] public void ValidationOrTransactionFailureNeverMutatesOriginalOrPublishesPartialImport()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"turn\":1}"); var source = Source(bytes);
        using (var repo = Open(stage => { if (stage == RepositoryWriteStage.BeforeCommit) throw new IOException("disk fault"); }))
        {
            Assert.Equal(RepositoryError.WriteFailed, Assert.Throws<RepositoryException>(() => repo.ImportJson(source)).Error);
            Assert.Empty(repo.ListReferences()); Assert.Equal(bytes, File.ReadAllBytes(source));
        }
        using var recovered = Open(); Assert.NotNull(recovered.ImportJson(source)); Assert.Single(recovered.ListReferences());
        var invalid = Encoding.UTF8.GetBytes("unsupported"); File.WriteAllBytes(source, invalid);
        Assert.Equal(RepositoryError.ValidationFailed, Assert.Throws<RepositoryException>(() => recovered.ImportJson(source)).Error);
        Assert.Equal(invalid, File.ReadAllBytes(source)); Assert.Single(recovered.ListReferences());
    }
    [Fact] public void BackupIncludesCommittedWalAndCanBeReopenedAndValidatedWithoutSource()
    {
        string backup, cp;
        using (var repo = Open())
        {
            cp = repo.CommitCheckpoint(new("one", "main", 0, null, Image(1), new("manual", 0, SaveReferenceKind.Manual, "Kept"))).CheckpointId!;
            backup = repo.Backup("verified");
            Assert.Equal(Image(1).ReadPayload(), repo.ReadCheckpoint(cp).ReadPayload());
        }
        var restored = Path.Combine(directory, "restored"); Directory.CreateDirectory(restored); File.Copy(backup, Path.Combine(restored, "campaign.sqlite"));
        using var independent = new SqliteCampaignRepository(restored, "campaign", "fixture-not-game", validator);
        Assert.Equal(cp, independent.GetBranch("main").Head); Assert.Equal(Image(1).ReadPayload(), independent.ReadCheckpoint(cp).ReadPayload());
    }
    [Fact] public void RetentionKeepsTenRollingThreeMilestonesManualPinsAndActiveHeadWithoutAncestryLeak()
    {
        using var repo = Open(); DurableReceipt? latest = null; var old = new List<string>();
        for (int i = 1; i <= 20; i++)
        {
            latest = repo.CommitCheckpoint(new("op-" + i, "main", i - 1, latest?.CheckpointId, Image(i), new("auto-" + i, 0, SaveReferenceKind.Auto, "Auto")));
            old.Add(latest.CheckpointId!);
            if (i <= 5) repo.SetReference("milestone-op-" + i, new("milestone-" + i, 0, SaveReferenceKind.Milestone, "Milestone"), latest.CheckpointId!);
        }
        repo.SetReference("keep", new("manual", 0, SaveReferenceKind.Manual, "Protected"), old[0]);
        repo.ForkBranch("old-future", "inactive", old[1]);
        var active = repo.GetBranch("main"); repo.ApplyRetention("gc", active);
        var refs = repo.ListReferences("", 500);
        Assert.Equal(10, refs.Count(r => r.Kind == SaveReferenceKind.Auto)); Assert.Equal(3, refs.Count(r => r.Kind == SaveReferenceKind.Milestone));
        Assert.Single(refs, r => r.Kind == SaveReferenceKind.Manual); Assert.NotNull(repo.ReadCheckpoint(old[0]));
        Assert.Equal(RepositoryError.Unavailable, Assert.Throws<RepositoryException>(() => repo.ReadCheckpoint(old[1])).Error);
        Assert.Equal(old[1], repo.GetBranch("inactive").Head); // Metadata remains, restore is explicitly unavailable.
        Assert.NotNull(repo.ReadCheckpoint(latest!.CheckpointId!));
        Assert.Equal(repo.FindOperation("op-2")!.CheckpointId, old[1]); // Durable receipt never expires into blind retry.
        Assert.Equal(latest.CheckpointId, repo.ApplyRetention("gc", active).CheckpointId);
    }
    [Fact] public void SoftBudgetCannotRemoveManualOrLatestRecoveryAndStaleRetentionRollsBack()
    {
        using var repo = Open(); var first = repo.CommitCheckpoint(new("one", "main", 0, null, Image(1), new("manual", 0, SaveReferenceKind.Manual, "Kept")));
        var stamp = repo.GetBranch("main");
        var next = repo.CommitCheckpoint(new("two", "main", 1, first.CheckpointId, Image(2), new("auto", 0, SaveReferenceKind.Auto, "Auto")));
        Assert.Equal(RepositoryError.Conflict, Assert.Throws<RepositoryException>(() => repo.ApplyRetention("stale", stamp)).Error);
        Assert.Null(repo.FindOperation("stale"));
        repo.ApplyRetention("tiny", repo.GetBranch("main"), softAutoBytes: 1);
        Assert.NotNull(repo.ReadCheckpoint(first.CheckpointId!)); Assert.NotNull(repo.ReadCheckpoint(next.CheckpointId!));
        Assert.True(repo.LogicalPayloadBytes > 1); Assert.Equal(2, repo.ListReferences().Count);
    }
    [Fact] public void CallbackCannotMutateRequestImageAndChangedBackupIsNeverOverwritten()
    {
        var source = Source(Image(1).ReadPayload());
        using var repo = Open(); var receipt = repo.ImportJson(source);
        var backup = Assert.Single(Directory.GetFiles(Path.Combine(directory, "repository/migration-backups")));
        File.WriteAllText(backup, "corrupted");
        Assert.Equal(RepositoryError.Corrupt, Assert.Throws<RepositoryException>(() => repo.ImportJson(source)).Error);
        Assert.Equal("corrupted", File.ReadAllText(backup)); Assert.NotNull(repo.ReadCheckpoint(receipt.CheckpointId!));
    }
    [Fact] public void ExpiredInactiveHeadCannotBecomeRetentionRootAndPruneLastAvailableRecovery()
    {
        using var repo = Open();
        var a = repo.CommitCheckpoint(new("a", "main", 0, null, Image(1), new("auto-a", 0, SaveReferenceKind.Auto, "A")));
        repo.ForkBranch("fork-b", "b", a.CheckpointId!);
        var b = repo.CommitCheckpoint(new("b", "b", 0, a.CheckpointId, Image(2), new("auto-b", 0, SaveReferenceKind.Auto, "B")));
        repo.ApplyRetention("prune-a", repo.GetBranch("b"), softAutoBytes: 1);
        Assert.Equal(RepositoryError.Unavailable, Assert.Throws<RepositoryException>(() => repo.ReadCheckpoint(a.CheckpointId!)).Error);
        var before = repo.ListReferences(); var bytes = repo.LogicalPayloadBytes;
        Assert.Equal(RepositoryError.Unavailable, Assert.Throws<RepositoryException>(() => repo.ApplyRetention("bad-root", repo.GetBranch("main"), softAutoBytes: 1)).Error);
        Assert.Null(repo.FindOperation("bad-root")); Assert.Equal(bytes, repo.LogicalPayloadBytes);
        Assert.Equal(before.Select(r => r.Id), repo.ListReferences().Select(r => r.Id));
        Assert.Equal(Image(2).ReadPayload(), repo.ReadCheckpoint(b.CheckpointId!).ReadPayload());
    }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    sealed class FixtureValidator : ICheckpointValidator
    {
        public CheckpointImage Validate(byte[] bytes)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(bytes); var turn = doc.RootElement.GetProperty("turn").GetInt32();
            return new CheckpointImage(bytes, "fixture-not-game", turn, 0, "Planning");
        }
    }
}
