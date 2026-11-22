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
}
