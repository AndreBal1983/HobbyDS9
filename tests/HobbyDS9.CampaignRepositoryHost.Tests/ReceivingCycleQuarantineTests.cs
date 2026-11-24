using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace HobbyDS9.CampaignRepositoryHost.Tests;

public sealed class ReceivingCycleQuarantineTests
{
    [Fact]
    public void Format18CorruptionWithMatchingHashSurvivesRecoveryWhenInitialPinDidNotCommit()
    {
        using var fixture = new RepositoryFixture();
        var host = fixture.Host(format: 18);
        var initial = fixture.Image(host);
        RepositoryFixture.Advance(host);
        var next = fixture.Image(host);
        var earlier = CycleCheckpointValidator.Map(initial);
        var later = CycleCheckpointValidator.Map(next);
        string earlierId, damagedId;
        using (var repository = fixture.Repository(initial))
        {
            earlierId = repository.CommitCheckpoint(new("initial", "main", 0, null, earlier)).CheckpointId!;
            damagedId = repository.CommitCheckpoint(new("before-restart", "main", 1, earlierId, later,
                new("latest-auto", 0, SaveReferenceKind.Auto, "Latest"))).CheckpointId!;
        }

        var decoded = fixture.Codec.DecodeCycleRoot(next.ReadPayload());
        decoded.BaseCampaign!.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.StateRevision = -1;
        var damagedBytes = fixture.Codec.EncodeCycleRoot(decoded);
        var root = Path.Combine(fixture.DirectoryPath, "campaign");
        var database = Path.Combine(root, "campaign.sqlite");
        using (var sql = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = database, Pooling = false }.ToString()))
        {
            sql.Open();
            using var update = sql.CreateCommand();
            update.CommandText = "UPDATE Checkpoint SET Payload=$bytes,PayloadLength=$length,PayloadHash=$hash WHERE Id=$id";
            update.Parameters.AddWithValue("$bytes", damagedBytes);
            update.Parameters.AddWithValue("$length", damagedBytes.Length);
            update.Parameters.AddWithValue("$hash", CheckpointImage.Hash(damagedBytes));
            update.Parameters.AddWithValue("$id", damagedId);
            Assert.Equal(1, update.ExecuteNonQuery());
        }

        using (var interrupted = new SqliteCampaignRepository(root, "campaign", initial.Edition.Identity,
            fixture.Validator(initial), stage => { if (stage == RepositoryWriteStage.BeforeCommit) throw new IOException("quarantine pin did not commit"); }))
        {
            Assert.Equal(RepositoryError.WriteFailed,
                Assert.Throws<RepositoryException>(() => interrupted.ReadCheckpoint(damagedId)).Error);
            Assert.Empty(interrupted.ListQuarantinedCheckpoints());
        }

        using (var recovery = fixture.Repository(initial))
        {
            Assert.Empty(recovery.ListQuarantinedCheckpoints());
            Assert.Equal(earlier.ReadPayload(), recovery.ReadCheckpoint(earlierId).ReadPayload());
            recovery.ForkBranch("continue", "recovery", earlierId);
            var receipt = recovery.CommitCheckpoint(new("advance", "recovery", 0, earlierId, later));
            recovery.ApplyRetention("prune", recovery.GetBranch("recovery"), rolling: 1, softAutoBytes: 1);
            Assert.Equal(later.ReadPayload(), recovery.ReadCheckpoint(receipt.CheckpointId!).ReadPayload());
        }

        using var reopened = fixture.Repository(initial);
        var evidence = Assert.Single(reopened.ListQuarantinedCheckpoints());
        Assert.Equal(damagedId, evidence.CheckpointId);
        Assert.Equal(CheckpointCorruptionKind.InvalidPayload, evidence.Kind);
        Assert.DoesNotContain(reopened.ListReferences(), row => row.CheckpointId == damagedId);
        Assert.Equal(damagedBytes, File.ReadAllBytes(reopened.ExportQuarantinedPayload(damagedId, "cycle18-evidence")));
    }
}
