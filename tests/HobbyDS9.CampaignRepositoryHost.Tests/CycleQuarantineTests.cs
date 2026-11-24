using System.Security.Cryptography;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using HobbyDS9.TacticalReviewClient;
using Microsoft.Data.Sqlite;
using Xunit;
using static HobbyDS9.CampaignRepositoryHost.Tests.RepositoryFixture;

namespace HobbyDS9.CampaignRepositoryHost.Tests;

public sealed class CycleQuarantineTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ImpossibleCounterInStrictlyDecodedMatchingEditionPinsExactEvidence(bool invalidTurn)
    {
        using var fixture = new RepositoryFixture();
        var host = fixture.Host();
        var image = fixture.Image(host);
        var root = fixture.Codec.DecodeCycleRoot(image.ReadPayload());
        var state = root.BaseCampaign!.Campaign!.Integrated!.Physical!.Core.Campaign.GameState;
        if (invalidTurn) state.TurnNumber = 0;
        else state.StateRevision = -1;
        var invalid = fixture.Codec.EncodeCycleRoot(root);
        var adapter = new CycleCampaignRepositoryAdapter(fixture.Package(), fixture.Codec, fixture.Wire);
        Assert.True(adapter.Validate(invalid, image.Edition.Identity).DefiniteCorruption);
        Assert.False(adapter.Validate(invalid).DefiniteCorruption);
        Assert.False(adapter.Validate(invalid, image.Edition.Identity + "different").DefiniteCorruption);

        using var repository = fixture.Repository(image);
        var receipt = repository.CommitCheckpoint(new("original", "main", 0, null, CycleCheckpointValidator.Map(image), Manual()));
        var checkpoint = receipt.CheckpointId!;
        var hash = Convert.ToHexString(SHA256.HashData(invalid)).ToLowerInvariant();

        // Leave the stored length, checksum and index coherent. This isolates
        // the host's typed invalidity signal from storage's integrity checks.
        using (var connection = new SqliteConnection("Data Source=" + Path.Combine(fixture.DirectoryPath, "campaign", "campaign.sqlite") + ";Pooling=False"))
        {
            connection.Open();
            using var update = connection.CreateCommand();
            update.CommandText = "UPDATE Checkpoint SET Payload=$bytes, PayloadLength=$length, PayloadHash=$hash WHERE Id=$id";
            update.Parameters.AddWithValue("$bytes", invalid);
            update.Parameters.AddWithValue("$length", invalid.Length);
            update.Parameters.AddWithValue("$hash", hash);
            update.Parameters.AddWithValue("$id", checkpoint);
            Assert.Equal(1, update.ExecuteNonQuery());
        }

        var error = Assert.Throws<RepositoryException>(() => repository.ReadCheckpoint(checkpoint));
        Assert.Equal(RepositoryError.Corrupt, error.Error);
        var evidence = Assert.Single(repository.ListQuarantinedCheckpoints());
        Assert.Equal(CheckpointCorruptionKind.InvalidPayload, evidence.Kind);
        Assert.Equal(checkpoint, evidence.CheckpointId);
        Assert.Equal(hash, evidence.ObservedPayloadHash);
        Assert.True(Assert.Single(repository.ListReferences()).Quarantined);
        Assert.Equal(invalid, File.ReadAllBytes(repository.ExportQuarantinedPayload(checkpoint, "observed")));
        Assert.Equal(image.ReadPayload(), fixture.Bytes(host));
    }

    [Fact]
    public void TransientCodecFailureKeepsExistingSaveReadableAndUnpinned()
    {
        using var fixture = new RepositoryFixture();
        var image = fixture.Image(fixture.Host());
        var temporarilyUnavailable = false;
        var codec = new DelegatedCampaignRepositoryCodec(fixture.Wire.Version, fixture.Codec.EncodeCycleRoot, bytes =>
        {
            if (temporarilyUnavailable) throw new IOException("Injected transient codec failure.");
            return fixture.Codec.DecodeCycleRoot(bytes);
        });
        var adapter = new CycleCampaignRepositoryAdapter(fixture.Package(), fixture.Codec, codec);
        var validator = new CycleCheckpointValidator(adapter, image.Edition.Identity);
        using var repository = new SqliteCampaignRepository(Path.Combine(fixture.DirectoryPath, "campaign"), "campaign", image.Edition.Identity, validator);
        var receipt = repository.CommitCheckpoint(new("original", "main", 0, null, CycleCheckpointValidator.Map(image), Manual()));

        temporarilyUnavailable = true;
        var error = Assert.Throws<RepositoryException>(() => repository.ReadCheckpoint(receipt.CheckpointId!));
        Assert.Equal(RepositoryError.ValidationFailed, error.Error);
        Assert.Empty(repository.ListQuarantinedCheckpoints());
        Assert.False(Assert.Single(repository.ListReferences()).Quarantined);

        temporarilyUnavailable = false;
        Assert.Equal(image.ReadPayload(), repository.ReadCheckpoint(receipt.CheckpointId!).ReadPayload());
    }

    [Fact]
    public void WrongAvailablePackageDoesNotQuarantineAStoredMatchingEditionSave()
    {
        using var fixture = new RepositoryFixture();
        var image = fixture.Image(fixture.Host());
        string checkpoint;
        using (var matching = fixture.Repository(image))
        {
            checkpoint = matching.CommitCheckpoint(new("original", "main", 0, null, CycleCheckpointValidator.Map(image), Manual())).CheckpointId!;
        }
        using (var wrong = fixture.Repository(image, format: 17))
        {
            var error = Assert.Throws<RepositoryException>(() => wrong.ReadCheckpoint(checkpoint));
            Assert.Equal(RepositoryError.IncompatibleEdition, error.Error);
            Assert.Empty(wrong.ListQuarantinedCheckpoints());
            Assert.False(Assert.Single(wrong.ListReferences()).Quarantined);
        }
        using var reopened = fixture.Repository(image);
        Assert.Equal(image.ReadPayload(), reopened.ReadCheckpoint(checkpoint).ReadPayload());
    }
}
