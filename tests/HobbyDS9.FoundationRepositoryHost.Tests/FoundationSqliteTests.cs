using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.FoundationRepositoryHost;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.FoundationRepositoryHost.Tests;

public sealed partial class FoundationCheckpointTests
{
    private sealed class TestDirectory : IDisposable
    {
        private static readonly string Parent = Path.GetFullPath(Path.Combine(PersistenceTestPaths.TemporaryRoot(), "HobbyDS9.FoundationRepositoryHost.Tests"));
        internal string Root { get; } = Path.Combine(Parent, Guid.NewGuid().ToString("N"));
        internal TestDirectory() { Directory.CreateDirectory(Root); }
        public void Dispose()
        {
            var resolved = Path.GetFullPath(Root);
            if (!resolved.StartsWith(Parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup escaped its private directory.");
            Directory.Delete(resolved, recursive: true);
        }
    }
    private static SqliteCampaignRepository Repository(string directory, FoundationCheckpointValidator validator) =>
        new(directory, "foundation-test", validator.EditionIdentity, validator);

    private static void ContinueCheckpoint(ICampaignSession session)
    {
        if (session.ExportSave().GameState.Phase == SessionPhase.AwaitingCombat)
        {
            foreach (var request in session.GetPendingCombatRequests())
            {
                var stamp = session.ExportSave().GameState;
                Assert.Equal(CombatSubmissionStatus.Buffered,
                    session.SubmitCombatResult(session.ResolveCombat(request), stamp.TurnNumber, stamp.StateRevision).Status);
            }
            var state = session.ExportSave().GameState;
            Assert.Equal(TurnStatus.Completed, session.FinalizeCombat(state.TurnNumber, state.StateRevision).Status);
        }
        Complete(session);
    }

    [Theory]
    [InlineData("progressed")][InlineData("pending")][InlineData("partially-buffered")]
    public void ActualSqliteLegacyImportPreservesOriginalBackupExportReopenAndContinuation(string checkpoint)
    {
        using var files = new TestDirectory();
        var (validator, session) = Fixture(combat: checkpoint != "progressed");
        if (checkpoint == "progressed") { Complete(session); Complete(session); }
        else
        {
            Assert.Equal(TurnStatus.AwaitingCombat, session.AdvanceTurn(1, 0).Status);
            Assert.Equal(2, session.GetPendingCombatRequests().Count);
            if (checkpoint == "partially-buffered")
            {
                var request = session.GetPendingCombatRequests()[0]; var stamp = session.ExportSave().GameState;
                Assert.Equal(CombatSubmissionStatus.Buffered,
                    session.SubmitCombatResult(session.ResolveCombat(request), stamp.TurnNumber, stamp.StateRevision).Status);
                Assert.Single(session.GetPendingCombatRequests()); Assert.Single(session.GetResolvedCombats());
            }
        }
        var original = Bytes(" \r\n" + JsonConvert.SerializeObject(session.ExportSave(), Formatting.Indented) + "\t\n");
        var source = Path.Combine(files.Root, "campaign-1.json"); File.WriteAllBytes(source, original);
        var sourceHash = CheckpointImage.Hash(original); var database = Path.Combine(files.Root, "repository");
        DurableReceipt receipt;
        using (var repo = Repository(database, validator))
        {
            receipt = repo.ImportJson(source);
            var repeat = repo.ImportJson(source);
            Assert.Equal(receipt.CheckpointId, repeat.CheckpointId); Assert.Equal(receipt.StorageSequence, repeat.StorageSequence);
            Assert.Single(repo.ListReferences());
            Assert.Equal(original, repo.ReadCheckpoint(receipt.CheckpointId!).ReadPayload());
            Assert.Equal(original, File.ReadAllBytes(repo.ExportJson(receipt.CheckpointId!, "original-export")));
        }
        Assert.Equal(sourceHash, CheckpointImage.Hash(File.ReadAllBytes(source)));
        Assert.Equal(original, File.ReadAllBytes(source));
        var migrationBackup = Assert.Single(Directory.GetFiles(Path.Combine(database, "migration-backups"), "*.json"));
        Assert.Equal(original, File.ReadAllBytes(migrationBackup));
        using (var reopened = Repository(database, validator))
        {
            Assert.Equal(receipt.CheckpointId, reopened.ImportJson(source).CheckpointId);
            var image = reopened.ReadCheckpoint(receipt.CheckpointId!);
            var exported = reopened.ExportJson(receipt.CheckpointId!, "reopened-export");
            Assert.Equal(original, File.ReadAllBytes(exported)); Assert.Equal(sourceHash, image.PayloadHash);
            var restored = validator.Restore(File.ReadAllBytes(exported));
            Assert.Equal(Json(session), Json(restored));
            ContinueCheckpoint(session); ContinueCheckpoint(restored); Assert.Equal(Json(session), Json(restored));
        }
        Assert.Equal(original, File.ReadAllBytes(source)); Assert.Equal(original, File.ReadAllBytes(migrationBackup));
    }

    [Theory]
    [InlineData("invalid")][InlineData("mismatched")]
    public void ActualSqliteRejectedLegacyImportPublishesNothingAndPreservesOriginal(string reason)
    {
        using var files = new TestDirectory(); var (validator, session) = Fixture();
        var root = JObject.Parse(Json(session));
        if (reason == "invalid") root["GameState"]!["RngState"]!["State"] = null;
        else root["ScenarioId"] = "scenario.wrong";
        var original = Bytes(root.ToString(Formatting.Indented)); var hash = CheckpointImage.Hash(original);
        var source = Path.Combine(files.Root, "campaign-1.json"); File.WriteAllBytes(source, original);
        var database = Path.Combine(files.Root, "repository");
        using (var repo = Repository(database, validator))
        {
            Assert.Equal(RepositoryError.ValidationFailed, Assert.Throws<RepositoryException>(() => repo.ImportJson(source)).Error);
            Assert.Empty(repo.ListReferences());
            using var read = new SqliteConnection(new SqliteConnectionStringBuilder
                { DataSource = Path.Combine(database, "campaign.sqlite"), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            read.Open(); using var count = read.CreateCommand();
            count.CommandText = "SELECT (SELECT COUNT(*) FROM Checkpoint)+(SELECT COUNT(*) FROM Branch)+(SELECT COUNT(*) FROM PersistenceOperation)";
            Assert.Equal(0L, count.ExecuteScalar());
            Assert.False(Directory.Exists(Path.Combine(database, "migration-backups")));
        }
        using (var reopened = Repository(database, validator)) Assert.Empty(reopened.ListReferences());
        Assert.Equal(original, File.ReadAllBytes(source)); Assert.Equal(hash, CheckpointImage.Hash(File.ReadAllBytes(source)));
    }

    [Theory]
    [InlineData("syntax", true)]
    [InlineData("utf8", true)]
    [InlineData("scenario", false)]
    [InlineData("version", false)]
    public void StoredFoundationRejectionOnlyQuarantinesProvenMalformedSyntax(string damage, bool definite)
    {
        using var files = new TestDirectory();
        var (validator, session) = Fixture();
        var database = Path.Combine(files.Root, "repository");
        var good = validator.Validate(Encode(session.ExportSave()));
        Complete(session);
        var later = validator.Validate(Encode(session.ExportSave()));
        string earlierId, damagedId;
        using (var repository = Repository(database, validator))
        {
            earlierId = repository.CommitCheckpoint(new("first", "main", 0, null, good)).CheckpointId!;
            damagedId = repository.CommitCheckpoint(new("second", "main", 1, earlierId, later,
                new("auto", 0, SaveReferenceKind.Auto, "Latest"))).CheckpointId!;
        }

        var bytes = later.ReadPayload();
        if (damage == "syntax") bytes[0] = (byte)']';
        else if (damage == "utf8") bytes[0] = 0xff;
        else
        {
            var root = JObject.Parse(Json(session));
            if (damage == "scenario") root["ScenarioId"] = "other.scenario";
            else root["SaveFormatVersion"] = 999;
            bytes = Bytes(root.ToString(Formatting.None));
        }

        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(database, "campaign.sqlite"),
            Pooling = false
        }.ToString()))
        {
            connection.Open();
            using var replace = connection.CreateCommand();
            replace.CommandText = "UPDATE Checkpoint SET Payload=$bytes,PayloadHash=$hash,PayloadLength=$length WHERE Id=$id";
            replace.Parameters.AddWithValue("$bytes", bytes);
            replace.Parameters.AddWithValue("$hash", CheckpointImage.Hash(bytes));
            replace.Parameters.AddWithValue("$length", bytes.Length);
            replace.Parameters.AddWithValue("$id", damagedId);
            Assert.Equal(1, replace.ExecuteNonQuery());
        }

        using (var repository = Repository(database, validator))
        {
            Assert.Equal(definite ? RepositoryError.Corrupt : RepositoryError.ValidationFailed,
                Assert.Throws<RepositoryException>(() => repository.ReadCheckpoint(damagedId)).Error);
            Assert.Equal(good.ReadPayload(), repository.ReadCheckpoint(earlierId).ReadPayload());
            if (definite)
            {
                Assert.Equal(CheckpointCorruptionKind.InvalidPayload, Assert.Single(repository.ListQuarantinedCheckpoints()).Kind);
                repository.ForkBranch("recover", "recovery", earlierId);
                repository.CommitCheckpoint(new("advance", "recovery", 0, earlierId, later));
                repository.ApplyRetention("prune", repository.GetBranch("recovery"), rolling: 1, softAutoBytes: 1);
            }
            else Assert.Empty(repository.ListQuarantinedCheckpoints());
        }

        if (definite)
        {
            using var reopened = Repository(database, validator);
            Assert.DoesNotContain(reopened.ListReferences(), row => row.CheckpointId == damagedId);
            Assert.Equal(bytes, File.ReadAllBytes(reopened.ExportQuarantinedPayload(damagedId, "raw-evidence")));
        }
    }
}
