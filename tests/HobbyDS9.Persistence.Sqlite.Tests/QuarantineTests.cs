using System.Globalization;
using System.Text;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace HobbyDS9.Persistence.Sqlite.Tests;

public sealed class QuarantineTests : IDisposable
{
    private readonly string directory = Path.Combine(PersistenceTestPaths.TemporaryRoot(), "hobbyds9-quarantine-" + Guid.NewGuid().ToString("N"));
    private readonly Validator validator = new();
    private string Database => Path.Combine(directory, "campaign.sqlite");
    private SqliteCampaignRepository Open(Action<RepositoryWriteStage>? fault = null, bool upgrade = false) =>
        new(directory, "campaign", "quarantine-fixture", validator, fault, upgrade);
    private static CheckpointImage Image(string text) =>
        new(Encoding.UTF8.GetBytes(text), "quarantine-fixture", int.Parse(text, CultureInfo.InvariantCulture), 0, "Planning");
    private static void Error(RepositoryError expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<RepositoryException>(action).Error);

    [Fact]
    public void ObservedCorruptionSurvivesReopenForkAndRetentionOnHealthyBranch()
    {
        string older;
        string damaged;
        using (var repository = Open())
        {
            older = repository.CommitCheckpoint(new("first", "main", 0, null, Image("1"),
                new("first-auto", 0, SaveReferenceKind.Auto, "Earlier recovery"))).CheckpointId!;
            damaged = repository.CommitCheckpoint(new("second", "main", 1, older, Image("2"),
                new("second-auto", 0, SaveReferenceKind.Auto, "Damaged recovery"))).CheckpointId!;
        }
        Damage(damaged, "X");
        QuarantinedCheckpoint observed;
        using (var repository = Open())
        {
            var firstPage = repository.QueryReferences(new(limit: 1));
            Error(RepositoryError.Corrupt, () => repository.ReadCheckpoint(damaged));
            Assert.True(repository.ListReferences().Single(row => row.CheckpointId == damaged).Quarantined);
            Error(RepositoryError.Conflict, () => repository.QueryReferences(new(limit: 1, cursor: firstPage.Next)));
            observed = Assert.Single(repository.ListQuarantinedCheckpoints());
            Assert.True(observed.CanExport);
            Assert.Equal(CheckpointCorruptionKind.StoredIntegrity, observed.Kind);
            Assert.Equal(1, observed.ObservedPayloadBytes);
            var afterQuarantine = repository.QueryReferences(new(limit: 1));
            Error(RepositoryError.Corrupt, () => repository.ReadCheckpoint(damaged));
            Assert.Single(repository.QueryReferences(new(limit: 1, cursor: afterQuarantine.Next)).Rows);
            Assert.Equal(observed.DetectedUtc, Assert.Single(repository.ListQuarantinedCheckpoints()).DetectedUtc);
            Error(RepositoryError.Corrupt, () => repository.ForkBranch("skip", "bad-recovery", damaged));
        }

        using (var repository = Open())
        {
            Assert.Equal(Image("1").ReadPayload(), repository.ReadCheckpoint(older).ReadPayload());
            repository.ForkBranch("recover", "recovery", older);
            var latest = repository.CommitCheckpoint(new("resume", "recovery", 0, older, Image("3"),
                new("recovery-auto", 0, SaveReferenceKind.Auto, "New recovery")));
            repository.ApplyRetention("prune", repository.GetBranch("recovery"), rolling: 1, softAutoBytes: 1);
            Assert.Equal(Image("3").ReadPayload(), repository.ReadCheckpoint(latest.CheckpointId!).ReadPayload());
        }
        using var reopened = Open();
        Error(RepositoryError.Corrupt, () => reopened.ReadCheckpoint(damaged));
        Assert.DoesNotContain(reopened.ListReferences(), row => row.CheckpointId == damaged);
        Assert.Equal(observed.ObservedPayloadHash, Assert.Single(reopened.ListQuarantinedCheckpoints()).ObservedPayloadHash);
        var path = reopened.ExportQuarantinedPayload(damaged, "evidence");
        Assert.Equal("X", File.ReadAllText(path));
        Assert.EndsWith(".bin", path);
        Error(RepositoryError.Conflict, () => reopened.ExportQuarantinedPayload(damaged, "evidence"));
        Error(RepositoryError.Corrupt, () => reopened.ExportJson(damaged, "save"));
        var usage = reopened.GetStorageUsage(reopened.GetBranch("recovery"), softAutoBytes: 1);
        Assert.Equal(1, usage.QuarantinedPayloadBytes);
        Assert.Equal(2, usage.ProtectedPayloadBytes);
        Assert.Equal(1, usage.AutomaticPayloadBytes);
        var backup = reopened.Backup("includes-evidence");
        var restore = Path.Combine(directory, "restored");
        Directory.CreateDirectory(restore);
        File.Copy(backup, Path.Combine(restore, "campaign.sqlite"));
        using var restored = new SqliteCampaignRepository(restore, "campaign", "quarantine-fixture", validator);
        Assert.Single(restored.ListQuarantinedCheckpoints());
        Assert.Equal("X", File.ReadAllText(restored.ExportQuarantinedPayload(damaged, "from-backup")));
    }

    [Theory]
    [InlineData("edition", RepositoryError.IncompatibleEdition)]
    [InlineData("transient", RepositoryError.ValidationFailed)]
    [InlineData("busy", RepositoryError.Busy)]
    public void UnavailableOrTransientValidatorCannotQuarantineHealthyBytes(string failure, RepositoryError expected)
    {
        using var repository = Open();
        var checkpoint = repository.CommitCheckpoint(new("create", "main", 0, null, Image("1"))).CheckpointId!;
        validator.Failure = failure;
        Error(expected, () => repository.ReadCheckpoint(checkpoint));
        Assert.Empty(repository.ListQuarantinedCheckpoints());
        validator.Failure = null;
        Assert.Equal("1", Encoding.UTF8.GetString(repository.ReadCheckpoint(checkpoint).ReadPayload()));
    }

    [Fact]
    public void CancelledValidationCannotQuarantineHealthyBytes()
    {
        using var repository = Open();
        var checkpoint = repository.CommitCheckpoint(new("create", "main", 0, null, Image("1"))).CheckpointId!;
        validator.Failure = "cancel";
        Assert.Throws<OperationCanceledException>(() => repository.ReadCheckpoint(checkpoint));
        Assert.Empty(repository.ListQuarantinedCheckpoints());
        validator.Failure = null;
        Assert.Equal("1", Encoding.UTF8.GetString(repository.ReadCheckpoint(checkpoint).ReadPayload()));
    }

    [Theory]
    [InlineData("invalid", CheckpointCorruptionKind.InvalidPayload)]
    [InlineData("metadata", CheckpointCorruptionKind.MetadataMismatch)]
    public void DefiniteValidatorOrStampRejectionPinsIntactRawBytes(string failure, CheckpointCorruptionKind kind)
    {
        using var repository = Open();
        var checkpoint = repository.CommitCheckpoint(new("create", "main", 0, null, Image("1"))).CheckpointId!;
        validator.Failure = failure;
        Error(RepositoryError.Corrupt, () => repository.ReadCheckpoint(checkpoint));
        Assert.Equal(kind, Assert.Single(repository.ListQuarantinedCheckpoints()).Kind);
        Assert.Equal("1", File.ReadAllText(repository.ExportQuarantinedPayload(checkpoint, "evidence")));
    }

    [Fact]
    public void MalformedPayloadStillPinsMetadataAndRejectsRawExport()
    {
        using var repository = Open();
        var checkpoint = repository.CommitCheckpoint(new("create", "main", 0, null, Image("1"))).CheckpointId!;
        repository.ConfigureConnectionForTests(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA ignore_check_constraints=ON; UPDATE Checkpoint SET Payload=X''; PRAGMA ignore_check_constraints=OFF;";
            command.ExecuteNonQuery();
        });
        Error(RepositoryError.Corrupt, () => repository.ReadCheckpoint(checkpoint));
        var observed = Assert.Single(repository.ListQuarantinedCheckpoints());
        Assert.False(observed.CanExport);
        Assert.Equal(0, observed.ObservedPayloadBytes);
        Error(RepositoryError.Unavailable, () => repository.ExportQuarantinedPayload(checkpoint, "no-bytes"));
        Assert.False(Directory.Exists(Path.Combine(directory, "quarantine-exports")));
    }

    [Fact]
    public void QuarantinedMalformedSourceStampDoesNotBlockBrowsingOtherSaves()
    {
        using var repository = Open();
        repository.CommitCheckpoint(new("good", "good", 0, null, Image("1"),
            new("good-ref", 0, SaveReferenceKind.Manual, "Good")));
        var damaged = repository.CommitCheckpoint(new("bad", "bad", 0, null, Image("2"),
            new("bad-ref", 0, SaveReferenceKind.Auto, "Damaged"))).CheckpointId!;
        repository.ConfigureConnectionForTests(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Checkpoint SET Phase=$phase WHERE Id=$id";
            command.Parameters.AddWithValue("$phase", new string('P', 65));
            command.Parameters.AddWithValue("$id", damaged);
            Assert.Equal(1, command.ExecuteNonQuery());
        });
        Error(RepositoryError.Corrupt, () => repository.ReadCheckpoint(damaged));
        var rows = repository.QueryReferences(new()).Rows;
        Assert.Equal(2, rows.Count);
        Assert.True(rows.Single(row => row.Id == "bad-ref").Quarantined);
        Assert.Equal("", rows.Single(row => row.Id == "bad-ref").Phase);
        Assert.Equal("Planning", rows.Single(row => row.Id == "good-ref").Phase);
    }

    [Theory]
    [InlineData(RepositoryWriteStage.BeforeCommit, RepositoryError.WriteFailed, false)]
    [InlineData(RepositoryWriteStage.AfterCommit, RepositoryError.OutcomeUnknown, true)]
    public void QuarantineWriteFailureIsExplicitAndFencesRetentionUntilRechecked(RepositoryWriteStage stage, RepositoryError expected, bool committed)
    {
        string damage, good;
        using (var repository = Open())
        {
            damage = repository.CommitCheckpoint(new("old", "main", 0, null, Image("1"))).CheckpointId!;
            good = repository.CommitCheckpoint(new("new", "good", 0, null, Image("2"))).CheckpointId!;
        }
        Damage(damage, "X");
        bool fail = true;
        using var reopened = Open(at => { if (fail && at == stage) throw new IOException("injected quarantine persistence failure"); });
        Error(expected, () => reopened.ReadCheckpoint(damage));
        Assert.Equal(committed ? 1 : 0, reopened.ListQuarantinedCheckpoints().Count);
        Error(RepositoryError.WriteFailed, () => reopened.ApplyRetention("prune", reopened.GetBranch("good"), softAutoBytes: 1));
        Assert.Null(reopened.FindOperation("prune"));
        Assert.Equal(2, reopened.LogicalPayloadBytes);
        fail = false;
        Error(RepositoryError.Corrupt, () => reopened.ReadCheckpoint(damage));
        reopened.ApplyRetention("prune", reopened.GetBranch("good"), softAutoBytes: 1);
        Assert.Equal("X", File.ReadAllText(reopened.ExportQuarantinedPayload(damage, "retained")));
        Assert.Equal("2", Encoding.UTF8.GetString(reopened.ReadCheckpoint(good).ReadPayload()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RetentionRediscoversUnrecordedCorruptionAfterRestartWithoutCallerReread(bool failAgain)
    {
        string good, damaged;
        using (var repository = Open())
        {
            good = repository.CommitCheckpoint(new("first", "main", 0, null, Image("1"))).CheckpointId!;
            damaged = repository.CommitCheckpoint(new("second", "main", 1, good, Image("2"),
                new("auto", 0, SaveReferenceKind.Auto, "Damaged"))).CheckpointId!;
        }
        Damage(damaged, "X");
        using (var failed = Open(at => { if (at == RepositoryWriteStage.BeforeCommit) throw new IOException("pin did not commit"); }))
        {
            Error(RepositoryError.WriteFailed, () => failed.ReadCheckpoint(damaged));
            Assert.Empty(failed.ListQuarantinedCheckpoints());
        }

        using (var repository = Open())
        {
            Assert.Equal("1", Encoding.UTF8.GetString(repository.ReadCheckpoint(good).ReadPayload()));
            repository.ForkBranch("recover", "recovery", good);
            repository.CommitCheckpoint(new("advance", "recovery", 0, good, Image("3")));
        }
        if (failAgain)
        {
            using var failed = Open(at => { if (at == RepositoryWriteStage.BeforeCommit) throw new IOException("pin still cannot commit"); });
            Error(RepositoryError.WriteFailed, () => failed.ApplyRetention("prune", failed.GetBranch("recovery"), rolling: 1, softAutoBytes: 1));
            Assert.Null(failed.FindOperation("prune"));
            Assert.Empty(failed.ListQuarantinedCheckpoints());
            Assert.Equal(3, failed.LogicalPayloadBytes);
            Assert.Single(failed.ListReferences());
        }

        using (var repository = Open())
        {
            repository.ApplyRetention("prune", repository.GetBranch("recovery"), rolling: 1, softAutoBytes: 1);
            Assert.Empty(repository.ListReferences());
        }
        using var reopened = Open();
        Assert.Equal(damaged, Assert.Single(reopened.ListQuarantinedCheckpoints()).CheckpointId);
        Assert.Equal("X", File.ReadAllText(reopened.ExportQuarantinedPayload(damaged, "retried")));
    }

    [Fact]
    public void UnclassifiedDeletionCandidateAbortsRetentionAndHostCheckRunsOutsideWriteTransaction()
    {
        using var repository = Open();
        var first = repository.CommitCheckpoint(new("first", "main", 0, null, Image("1")));
        repository.CommitCheckpoint(new("second", "main", 1, first.CheckpointId, Image("2")));
        var checkedOutsideTransaction = false;
        validator.BeforeValidate = bytes =>
        {
            if (Encoding.UTF8.GetString(bytes) != "1") return;
            repository.ConfigureConnectionForTests(connection =>
            {
                using var transaction = connection.BeginTransaction();
                transaction.Rollback();
                checkedOutsideTransaction = true;
            });
            throw new IOException("temporary validation failure");
        };
        Error(RepositoryError.ValidationFailed, () => repository.ApplyRetention("prune", repository.GetBranch("main"), softAutoBytes: 1));
        Assert.True(checkedOutsideTransaction);
        Assert.Null(repository.FindOperation("prune"));
        Assert.Empty(repository.ListQuarantinedCheckpoints());
        validator.BeforeValidate = null;
        Assert.Equal("1", Encoding.UTF8.GetString(repository.ReadCheckpoint(first.CheckpointId!).ReadPayload()));
    }

    [Fact]
    public void ExplicitSchema2UpgradeBacksUpAndPreservesTimestampAndReceipt()
    {
        DurableReceipt receipt;
        string updatedUtc;
        var request = new CheckpointCommit("original", "main", 0, null, Image("1"),
            new("manual", 0, SaveReferenceKind.Manual, "Original"));
        using (var repository = Open())
        {
            receipt = repository.CommitCheckpoint(request);
            updatedUtc = Assert.Single(repository.ListReferences()).UpdatedUtc;
        }
        using (var connection = new SqliteConnection("Data Source=" + Database + ";Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE CheckpointQuarantine; PRAGMA user_version=2";
            command.ExecuteNonQuery();
        }
        Error(RepositoryError.IncompatibleSchema, () => Open().Dispose());
        using var upgraded = Open(upgrade: true);
        Assert.Equal(receipt.StorageSequence, upgraded.CommitCheckpoint(request).StorageSequence);
        Assert.Equal(updatedUtc, Assert.Single(upgraded.ListReferences()).UpdatedUtc);
        Assert.Equal("1", Encoding.UTF8.GetString(upgraded.ReadCheckpoint(receipt.CheckpointId!).ReadPayload()));
        Assert.Empty(upgraded.ListQuarantinedCheckpoints());
        var backupPath = Assert.Single(Directory.GetFiles(Path.Combine(directory, "backups")));
        using var backup = new SqliteConnection("Data Source=" + backupPath + ";Mode=ReadOnly;Pooling=False");
        backup.Open();
        using var schema = backup.CreateCommand();
        schema.CommandText = "PRAGMA user_version";
        Assert.Equal(2L, schema.ExecuteScalar());
    }

    private void Damage(string checkpointId, string text)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Checkpoint SET Payload=$bytes WHERE Id=$id";
        command.Parameters.AddWithValue("$id", checkpointId);
        command.Parameters.AddWithValue("$bytes", Encoding.UTF8.GetBytes(text));
        Assert.Equal(1, command.ExecuteNonQuery());
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private sealed class Validator : ICheckpointValidator
    {
        public string? Failure;
        public Action<byte[]>? BeforeValidate;
        public CheckpointImage Validate(byte[] bytes)
        {
            BeforeValidate?.Invoke(bytes);
            switch (Failure)
            {
                case "invalid": throw new InvalidCheckpointPayloadException("Fixture attests definite invalidity.");
                case "metadata": return new CheckpointImage(bytes, "quarantine-fixture", 4, 0, "Planning");
                case "edition": return new CheckpointImage(bytes, "missing-edition", 1, 0, "Planning");
                case "transient": throw new IOException("Fixture read failed.");
                case "busy": throw new RepositoryException(RepositoryError.Busy);
                case "cancel": throw new OperationCanceledException();
                default: return Image(Encoding.UTF8.GetString(bytes));
            }
        }
    }
}
