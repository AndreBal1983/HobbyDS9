using System.Text;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using Xunit;

namespace HobbyDS9.Persistence.Sqlite.Tests;

public sealed class RecoverySafetyTests : IDisposable
{
    readonly string directory = Path.Combine(PersistenceTestPaths.TemporaryRoot(), "hobbyds9-recovery-" + Guid.NewGuid().ToString("N"));
    static CheckpointImage Image(int turn) => new(Encoding.UTF8.GetBytes(turn.ToString()), "recovery-fixture", turn, 0, "Planning");
    SqliteCampaignRepository Open() => new(Path.Combine(directory, "repository"), "campaign", "recovery-fixture", new Validator());
    static void Error(RepositoryError expected, Action action) => Assert.Equal(expected, Assert.Throws<RepositoryException>(action).Error);

    [Theory] [InlineData(false)] [InlineData(true)]
    public void CorruptReplacementCannotPruneVerifiedFallbackEvenWhenAvailableIsSet(bool matchingHash)
    {
        string older, newest;
        using (var repo = Open())
        {
            older = repo.CommitCheckpoint(new("older", "main", 0, null, Image(1), new("auto-1", 0, SaveReferenceKind.Auto, "Older"))).CheckpointId!;
            newest = repo.CommitCheckpoint(new("newest", "main", 1, older, Image(2), new("auto-2", 0, SaveReferenceKind.Auto, "Newest"))).CheckpointId!;
            repo.ConfigureConnectionForTests(connection =>
            {
                using var command = connection.CreateCommand();
                command.CommandText = "UPDATE Checkpoint SET Payload=$bytes,PayloadHash=CASE WHEN $match=1 THEN $hash ELSE PayloadHash END WHERE Id=$id";
                var invalid = Encoding.UTF8.GetBytes("x");
                command.Parameters.AddWithValue("$bytes", invalid); command.Parameters.AddWithValue("$hash", CheckpointImage.Hash(invalid));
                command.Parameters.AddWithValue("$match", matchingHash ? 1 : 0); command.Parameters.AddWithValue("$id", newest); command.ExecuteNonQuery();
            });
            Assert.Equal(Image(1).ReadPayload(), repo.ReadCheckpoint(older).ReadPayload());
            var before = repo.ListReferences().Select(r => (r.Id, r.Generation, r.PayloadAvailable)).ToArray();
            var bytes = repo.LogicalPayloadBytes;
            Error(matchingHash ? RepositoryError.ValidationFailed : RepositoryError.Corrupt,
                () => repo.ApplyRetention("unsafe-prune", repo.GetBranch("main"), rolling: 1, softAutoBytes: 1));
            Assert.Null(repo.FindOperation("unsafe-prune")); Assert.Equal(bytes, repo.LogicalPayloadBytes);
            Assert.Equal(before, repo.ListReferences().Select(r => (r.Id, r.Generation, r.PayloadAvailable)).ToArray());
        }
        using var reopened = Open();
        Assert.Equal(Image(1).ReadPayload(), reopened.ReadCheckpoint(older).ReadPayload());
        Assert.Equal(2, reopened.ListReferences().Count); Assert.Null(reopened.FindOperation("unsafe-prune"));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void ReimportOfDeletedReferenceIsExplicitlyUnavailableIncludingAfterPayloadExpiry(bool prune)
    {
        Directory.CreateDirectory(directory); var source = Path.Combine(directory, "original.json");
        var original = Image(1).ReadPayload(); File.WriteAllBytes(source, original);
        string operation;
        using (var repo = Open())
        {
            var imported = repo.ImportJson(source); operation = imported.OperationId;
            var reference = Assert.Single(repo.ListReferences()); repo.DeleteReference("delete", reference.Id, reference.Generation);
            if (prune)
            {
                repo.CommitCheckpoint(new("replacement", "main", 0, null, Image(2), new("auto", 0, SaveReferenceKind.Auto, "Recovery")));
                repo.ApplyRetention("prune", repo.GetBranch("main"), softAutoBytes: 1);
                Error(RepositoryError.Unavailable, () => repo.ReadCheckpoint(imported.CheckpointId!));
            }
            Error(RepositoryError.Unavailable, () => repo.ImportJson(source));
            Assert.Equal(imported.CheckpointId, repo.FindOperation(operation)!.CheckpointId);
            Assert.DoesNotContain(repo.ListReferences(), r => r.Id == reference.Id);
        }
        using var reopened = Open(); Error(RepositoryError.Unavailable, () => reopened.ImportJson(source));
        Assert.NotNull(reopened.FindOperation(operation)); Assert.Equal(original, File.ReadAllBytes(source));
        Assert.Equal(original, File.ReadAllBytes(Assert.Single(Directory.GetFiles(Path.Combine(directory, "repository/migration-backups")))));
    }

    [Fact] public void ReimportDoesNotReportHistoricalSuccessForCorruptProtectedPayload()
    {
        Directory.CreateDirectory(directory); var source = Path.Combine(directory, "original.json"); File.WriteAllBytes(source, Image(1).ReadPayload());
        using var repo = Open(); var original = repo.ImportJson(source);
        repo.ConfigureConnectionForTests(connection =>
        {
            using var command = connection.CreateCommand(); command.CommandText = "UPDATE Checkpoint SET Payload=X'39'"; command.ExecuteNonQuery();
        });
        Error(RepositoryError.Corrupt, () => repo.ImportJson(source));
        Assert.Equal(original.CheckpointId, repo.FindOperation(original.OperationId)!.CheckpointId);
        Assert.Single(repo.ListReferences()); Assert.Equal(Image(1).ReadPayload(), File.ReadAllBytes(source));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void RetentionValidatesOutsideWriteTransactionAndRechecksVerifiedReplacement(bool changeAfterValidation)
    {
        Action? onValidate = null;
        using var repo = new SqliteCampaignRepository(Path.Combine(directory, "repository"), "campaign", "recovery-fixture", new CallbackValidator(() => onValidate?.Invoke()));
        var older = repo.CommitCheckpoint(new("older", "main", 0, null, Image(1), new("auto-1", 0, SaveReferenceKind.Auto, "Older")));
        var newest = repo.CommitCheckpoint(new("newest", "main", 1, older.CheckpointId, Image(2), new("auto-2", 0, SaveReferenceKind.Auto, "Newest")));
        var calls = 0;
        onValidate = () =>
        {
            onValidate = null; calls++;
            repo.ConfigureConnectionForTests(connection =>
            {
                // Beginning a separate transaction would fail if game validation
                // were still called inside the repository's write transaction.
                using (var independent = connection.BeginTransaction()) independent.Rollback();
                if (!changeAfterValidation) return;
                using var command = connection.CreateCommand();
                command.CommandText = "UPDATE Checkpoint SET Payload=$bytes,PayloadHash=$hash,Turn=3 WHERE Id=$id";
                command.Parameters.AddWithValue("$bytes", Image(3).ReadPayload()); command.Parameters.AddWithValue("$hash", Image(3).PayloadHash);
                command.Parameters.AddWithValue("$id", newest.CheckpointId!); command.ExecuteNonQuery();
            });
        };
        if (changeAfterValidation)
        {
            Error(RepositoryError.Conflict, () => repo.ApplyRetention("prune", repo.GetBranch("main"), rolling: 1, softAutoBytes: 1));
            Assert.Null(repo.FindOperation("prune")); Assert.Equal(2, repo.ListReferences().Count);
            Assert.Equal(Image(1).ReadPayload(), repo.ReadCheckpoint(older.CheckpointId!).ReadPayload());
        }
        else Assert.NotNull(repo.ApplyRetention("prune", repo.GetBranch("main"), rolling: 1, softAutoBytes: 1));
        Assert.Equal(1, calls);
    }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    sealed class Validator : ICheckpointValidator
    {
        public CheckpointImage Validate(byte[] bytes) => new(bytes, "recovery-fixture", int.Parse(Encoding.UTF8.GetString(bytes)), 0, "Planning");
    }
    sealed class CallbackValidator : ICheckpointValidator
    {
        readonly Action callback;
        public CallbackValidator(Action callback) { this.callback = callback; }
        public CheckpointImage Validate(byte[] bytes)
        {
            callback(); return new(bytes, "recovery-fixture", int.Parse(Encoding.UTF8.GetString(bytes)), 0, "Planning");
        }
    }
}
