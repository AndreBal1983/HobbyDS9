using System.Text;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace HobbyDS9.Persistence.Sqlite.Tests;

public sealed class MetadataTests : IDisposable
{
    readonly string directory = Path.Combine(PersistenceTestPaths.TemporaryRoot(), "hobbyds9-metadata-" + Guid.NewGuid().ToString("N"));
    readonly CountingValidator validator = new();
    SqliteCampaignRepository Open(Action<RepositoryWriteStage>? fault = null, bool upgrade = false) => new(directory, "campaign", "fixture", validator, fault, upgrade);
    static CheckpointImage Image(int turn) => new(Encoding.UTF8.GetBytes(turn.ToString(System.Globalization.CultureInfo.InvariantCulture)), "fixture", turn, turn - 1, turn == 1 ? "Planning" : "AwaitingCombat");
    [Fact] public void ReferenceSummariesDoNotDecodePayloadAndRemainUnvalidatedMetadata()
    {
        using var repo = Open(); var first = repo.CommitCheckpoint(new("first", "main", 0, null, Image(1), new("save", 0, SaveReferenceKind.Manual, "Friendly name")));
        var second = repo.CommitCheckpoint(new("second", "main", 1, first.CheckpointId, Image(2)));
        repo.SetReference("overwrite", new("save", 1, SaveReferenceKind.Manual, "Review me"), second.CheckpointId!);
        var calls = validator.Calls; var row = Assert.Single(repo.ListReferences());
        Assert.Equal(calls, validator.Calls); Assert.Equal(2, row.Turn); Assert.Equal(1, row.Revision); Assert.Equal("AwaitingCombat", row.Phase);
        Assert.Equal(second.StorageSequence, row.StorageSequence); Assert.True(row.PayloadAvailable); Assert.True(DateTimeOffset.TryParse(row.UpdatedUtc, out _));
        Assert.Equal(2, row.Generation); Assert.Equal("Review me", row.Name);
    }
    [Fact] public void UsageSeparatesManualProtectedAutoAndPhysicalBytesAndReportsSoftOverflowWithoutDeleting()
    {
        using var repo = Open(); var first = repo.CommitCheckpoint(new("first", "main", 0, null, Image(1), new("manual", 0, SaveReferenceKind.Manual, "Protected")));
        var next = repo.CommitCheckpoint(new("next", "main", 1, first.CheckpointId, Image(12), new("auto", 0, SaveReferenceKind.Auto, "Automatic")));
        var calls = validator.Calls; var usage = repo.GetStorageUsage(repo.GetBranch("main"), softAutoBytes: 1);
        Assert.Equal(calls, validator.Calls); Assert.Equal(3, usage.LogicalPayloadBytes); Assert.Equal(2, usage.AutomaticPayloadBytes);
        Assert.Equal(3, usage.ProtectedPayloadBytes); Assert.True(usage.AutomaticBudgetExceeded); Assert.True(usage.PhysicalDatabaseBytes > 0); Assert.True(usage.WalBytes >= 0);
        Assert.Equal(2, repo.ListReferences().Count); Assert.NotNull(repo.ReadCheckpoint(next.CheckpointId!));
    }
    [Fact] public void ExplicitStorageUpgradeBacksUpAndPreservesExactPayloadReferenceAndOriginalUnknownTimestamp()
    {
        string checkpoint;
        using (var repo = Open()) checkpoint = repo.CommitCheckpoint(new("first", "main", 0, null, Image(1), new("manual", 0, SaveReferenceKind.Manual, "Original"))).CheckpointId!;
        SimulateSchema1();
        Assert.Equal(RepositoryError.IncompatibleSchema, Assert.Throws<RepositoryException>(() => Open().Dispose()).Error);
        using (var upgraded = Open(upgrade: true))
        {
            Assert.Equal(Image(1).ReadPayload(), upgraded.ReadCheckpoint(checkpoint).ReadPayload());
            var row = Assert.Single(upgraded.ListReferences()); Assert.Equal("", row.UpdatedUtc); Assert.Equal(1, row.Generation);
        }
        var backup = Assert.Single(Directory.GetFiles(Path.Combine(directory, "backups"), "*.sqlite"));
        Assert.Equal(1, Schema(backup)); Assert.Equal(2, Schema(Path.Combine(directory, "campaign.sqlite")));
        using var reopened = Open(); Assert.NotNull(reopened.FindOperation("first"));
    }
    [Fact] public void FailedStorageUpgradeRollsBackVersionAndRetainsVerifiedRecoveryBackup()
    {
        using (var repo = Open()) repo.CommitCheckpoint(new("first", "main", 0, null, Image(1)));
        SimulateSchema1();
        Assert.Equal(RepositoryError.WriteFailed, Assert.Throws<RepositoryException>(() => Open(stage => { if (stage == RepositoryWriteStage.BeforeCommit) throw new IOException("DDL fault"); }, true).Dispose()).Error);
        Assert.Equal(1, Schema(Path.Combine(directory, "campaign.sqlite")));
        var backup = Assert.Single(Directory.GetFiles(Path.Combine(directory, "backups"), "*.sqlite")); var hash = CheckpointImage.Hash(File.ReadAllBytes(backup));
        Assert.Equal(1, Schema(backup));
        using var recovered = Open(upgrade: true); Assert.NotNull(recovered.FindOperation("first"));
        Assert.Equal(hash, CheckpointImage.Hash(File.ReadAllBytes(backup))); Assert.Equal(2, Directory.GetFiles(Path.Combine(directory, "backups"), "*.sqlite").Length);
    }
    [Fact] public void LostUpgradeAcknowledgmentIsUnknownAndReopenRecognizesCommittedSchema()
    {
        using (var repo = Open()) repo.CommitCheckpoint(new("first", "main", 0, null, Image(1)));
        SimulateSchema1();
        Assert.Equal(RepositoryError.OutcomeUnknown, Assert.Throws<RepositoryException>(() => Open(stage => { if (stage == RepositoryWriteStage.AfterCommit) throw new IOException("upgrade acknowledgment lost"); }, true).Dispose()).Error);
        Assert.Equal(2, Schema(Path.Combine(directory, "campaign.sqlite")));
        using var recovered = Open(); Assert.NotNull(recovered.FindOperation("first"));
        Assert.Single(Directory.GetFiles(Path.Combine(directory, "backups"), "*.sqlite"));
    }
    [Fact] public void MetadataOperationRetriesRemainIdempotentAcrossSchemaUpgrade()
    {
        string cp; DurableReceipt original;
        var request = new CheckpointCommit("first", "main", 0, null, Image(1), new("manual", 0, SaveReferenceKind.Manual, "Original"));
        using (var repo = Open())
        {
            original = repo.CommitCheckpoint(request); cp = original.CheckpointId!;
            repo.SetReference("rename", new("manual", 1, SaveReferenceKind.Manual, "Reviewed"), cp);
        }
        SimulateSchema1();
        using var upgraded = Open(upgrade: true);
        Assert.Equal(original.Fingerprint, upgraded.CommitCheckpoint(request).Fingerprint);
        Assert.Equal(2, upgraded.SetReference("rename", new("manual", 1, SaveReferenceKind.Manual, "Reviewed"), cp).ReferenceGeneration);
        var row = Assert.Single(upgraded.ListReferences()); Assert.Equal(2, row.Generation); Assert.Equal("", row.UpdatedUtc);
        upgraded.SetReference("new-rename", new("manual", 2, SaveReferenceKind.Manual, "New edit"), cp);
        row = Assert.Single(upgraded.ListReferences()); Assert.Equal(3, row.Generation); Assert.True(DateTimeOffset.TryParse(row.UpdatedUtc, out _));
    }
    void SimulateSchema1()
    {
        using var sql = new SqliteConnection("Data Source=" + Path.Combine(directory, "campaign.sqlite") + ";Pooling=False"); sql.Open();
        using var c = sql.CreateCommand(); c.CommandText = "ALTER TABLE SaveReference DROP COLUMN UpdatedUtc; PRAGMA user_version=1"; c.ExecuteNonQuery();
    }
    static long Schema(string path)
    {
        using var sql = new SqliteConnection("Data Source=" + path + ";Mode=ReadOnly;Pooling=False"); sql.Open();
        using var c = sql.CreateCommand(); c.CommandText = "PRAGMA user_version"; return (long)c.ExecuteScalar()!;
    }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    sealed class CountingValidator : ICheckpointValidator
    {
        public int Calls;
        public CheckpointImage Validate(byte[] bytes) { Calls++; return Image(int.Parse(Encoding.UTF8.GetString(bytes), System.Globalization.CultureInfo.InvariantCulture)); }
    }
}
