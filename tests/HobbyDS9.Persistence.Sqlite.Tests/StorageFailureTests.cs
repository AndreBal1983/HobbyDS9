using System.Text;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace HobbyDS9.Persistence.Sqlite.Tests;

public sealed class StorageFailureTests : IDisposable
{
    readonly string directory = Path.Combine(Path.GetTempPath(), "hobbyds9-storage-failure-" + Guid.NewGuid().ToString("N"));
    static readonly Validator CarrierValidator = new();
    SqliteCampaignRepository Open() => new(Path.Combine(directory, "repository"), "campaign", "fixture", CarrierValidator);
    static CheckpointImage Image(int turn, int padding = 0) => new(Encoding.UTF8.GetBytes(turn.ToString(System.Globalization.CultureInfo.InvariantCulture) + new string(' ', padding)), "fixture", turn, 0, "Planning");
    [Fact] public void EnginePageLimitProducesSqliteFullWithoutFillingFilesystemAndRetainsPreviousRecovery()
    {
        string cp;
        using (var repo = Open())
        {
            cp = repo.CommitCheckpoint(new("one", "main", 0, null, Image(1), new("manual", 0, SaveReferenceKind.Manual, "Last good"))).CheckpointId!;
            repo.ConfigureConnectionForTests(sql =>
            {
                using var c = sql.CreateCommand(); c.CommandText = "PRAGMA page_count"; var pages = (long)c.ExecuteScalar()!;
                c.CommandText = "PRAGMA max_page_count=" + pages.ToString(System.Globalization.CultureInfo.InvariantCulture); c.ExecuteScalar();
            });
            var failure = Assert.Throws<RepositoryException>(() => repo.CommitCheckpoint(new("full", "main", 1, cp, Image(2, 200000), new("manual", 1, SaveReferenceKind.Manual, "Never published"))));
            Assert.Equal(RepositoryError.StorageFull, failure.Error); Assert.Equal(13, Assert.IsType<SqliteException>(failure.InnerException).SqliteErrorCode);
            Assert.Null(repo.FindOperation("full")); Assert.Equal(cp, repo.GetBranch("main").Head);
            Assert.Equal("Last good", Assert.Single(repo.ListReferences()).Name); Assert.NotNull(repo.ReadCheckpoint(cp));
        }
        using var reopened = Open(); Assert.NotNull(reopened.ReadCheckpoint(cp)); Assert.Null(reopened.FindOperation("full"));
    }
    [Fact] public void QueryOnlyProducesSqliteReadonlyWithoutChangingOwnerAclOrExistingReference()
    {
        using var repo = Open(); var cp = repo.CommitCheckpoint(new("one", "main", 0, null, Image(1))).CheckpointId!;
        repo.ConfigureConnectionForTests(sql => { using var c = sql.CreateCommand(); c.CommandText = "PRAGMA query_only=ON"; c.ExecuteNonQuery(); });
        var failure = Assert.Throws<RepositoryException>(() => repo.CommitCheckpoint(new("readonly", "main", 1, cp, Image(2))));
        Assert.Equal(RepositoryError.ReadOnly, failure.Error); Assert.Equal(8, Assert.IsType<SqliteException>(failure.InnerException).SqliteErrorCode);
        Assert.Null(repo.FindOperation("readonly")); Assert.Equal(cp, repo.GetBranch("main").Head); Assert.NotNull(repo.ReadCheckpoint(cp));
    }
    [Fact] public void FullDatabaseImportKeepsOriginalAndVerifiedCopyWithoutPublishingAnotherReference()
    {
        using var repo = Open(); var cp = repo.CommitCheckpoint(new("one", "main", 0, null, Image(1))).CheckpointId!;
        repo.ConfigureConnectionForTests(sql =>
        {
            using var c = sql.CreateCommand(); c.CommandText = "PRAGMA page_count"; var pages = (long)c.ExecuteScalar()!;
            c.CommandText = "PRAGMA max_page_count=" + pages.ToString(System.Globalization.CultureInfo.InvariantCulture); c.ExecuteScalar();
        });
        var bytes = Image(2, 200000).ReadPayload(); var source = Path.Combine(directory, "legacy-original.json"); File.WriteAllBytes(source, bytes);
        Assert.Equal(RepositoryError.StorageFull, Assert.Throws<RepositoryException>(() => repo.ImportJson(source)).Error);
        Assert.Equal(bytes, File.ReadAllBytes(source));
        Assert.Equal(bytes, File.ReadAllBytes(Assert.Single(Directory.GetFiles(Path.Combine(directory, "repository/migration-backups")))));
        Assert.Empty(repo.ListReferences()); Assert.Equal(cp, repo.GetBranch("main").Head); Assert.NotNull(repo.ReadCheckpoint(cp));
    }
    [Fact] public void ConfirmedPrecommitBusyIsDistinctAndSameRequestCanRetryAfterWriterReleases()
    {
        using var repo = Open(); var cp = repo.CommitCheckpoint(new("one", "main", 0, null, Image(1))).CheckpointId!;
        var request = new CheckpointCommit("busy", "main", 1, cp, Image(2));
        // Deliberate engine contention, not a second cooperating host lease.
        using (var other = new SqliteConnection("Data Source=" + Path.Combine(directory, "repository/campaign.sqlite") + ";Pooling=False"))
        {
            other.Open(); using var held = other.BeginTransaction(deferred: false);
            var failure = Assert.Throws<RepositoryException>(() => repo.CommitCheckpoint(request));
            Assert.Equal(RepositoryError.Busy, failure.Error); Assert.Contains(Assert.IsType<SqliteException>(failure.InnerException).SqliteErrorCode, new[] { 5, 6 });
            Assert.Null(repo.FindOperation("busy")); Assert.Equal(cp, repo.GetBranch("main").Head);
        }
        var committed = repo.CommitCheckpoint(request); Assert.Equal(2, committed.StorageSequence);
        Assert.Equal(committed.CheckpointId, repo.CommitCheckpoint(request).CheckpointId);
    }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    sealed class Validator : ICheckpointValidator
    {
        public CheckpointImage Validate(byte[] bytes) => new(bytes, "fixture", int.Parse(Encoding.UTF8.GetString(bytes), System.Globalization.CultureInfo.InvariantCulture), 0, "Planning");
    }
}
