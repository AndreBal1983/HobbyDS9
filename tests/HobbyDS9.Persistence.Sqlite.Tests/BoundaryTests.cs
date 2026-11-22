using System.Text;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace HobbyDS9.Persistence.Sqlite.Tests;

public sealed class BoundaryTests : IDisposable
{
    readonly string path = Path.Combine(Path.GetTempPath(), "hobbyds9-boundary-" + Guid.NewGuid().ToString("N"));
    readonly Validator validator = new();
    SqliteCampaignRepository Open() => new(path, "campaign", "edition", validator);
    static CheckpointImage Image() => new(new byte[] { 49 }, "edition", 1, 0, "Planning");
    [Fact] public void InvalidUnicodeCannotCollideWithReplacementCharacterFingerprint()
    {
        using var repo = Open(); var cp = repo.CommitCheckpoint(new("one", "main", 0, null, Image())).CheckpointId!;
        Assert.Equal(RepositoryError.InvalidRequest, Assert.Throws<RepositoryException>(() => repo.SetReference("unicode", new("ref", 0, SaveReferenceKind.Manual, "\ud800"), cp)).Error);
        Assert.Null(repo.FindOperation("unicode"));
        repo.SetReference("unicode", new("ref", 0, SaveReferenceKind.Manual, "\ufffd"), cp);
        Assert.Equal(RepositoryError.InvalidRequest, Assert.Throws<RepositoryException>(() => repo.SetReference("unicode", new("ref", 0, SaveReferenceKind.Manual, "\ud800"), cp)).Error);
        Assert.Equal("\ufffd", Assert.Single(repo.ListReferences()).Name);
        Assert.Throws<ArgumentException>(() => new CheckpointImage(new byte[] { 49 }, "edition\ud800", 1, 0, "Planning"));
    }
    [Theory] [InlineData("Name")] [InlineData("Notes")] [InlineData("Phase")] [InlineData("PayloadHash")] [InlineData("EditionIdentity")]
    public void TamperedOversizeTextIsRejectedBeforeManagedMaterialization(string field)
    {
        string cp;
        using (var repo = Open()) cp = repo.CommitCheckpoint(new("one", "main", 0, null, Image(), new("manual", 0, SaveReferenceKind.Manual, "Original"))).CheckpointId!;
        // Keep repository open so this exercises bounded reads, independently of
        // quick_check's own rejection on a later reopen.
        using var live = field == "EditionIdentity" ? null : Open();
        using (var sql = new SqliteConnection("Data Source=" + Path.Combine(path, "campaign.sqlite") + ";Pooling=False"))
        {
            sql.Open(); using var cmd = sql.CreateCommand();
            var table = field == "Name" || field == "Notes" ? "SaveReference" : field == "EditionIdentity" ? "RepositoryMeta" : "Checkpoint";
            cmd.CommandText = "PRAGMA ignore_check_constraints=ON; UPDATE " + table + " SET " + field + "=CAST(zeroblob(2000000) AS TEXT)"; cmd.ExecuteNonQuery();
        }
        if (field == "EditionIdentity") Assert.Equal(RepositoryError.Corrupt, Assert.Throws<RepositoryException>(() => Open().Dispose()).Error);
        else
        {
            Assert.Equal(RepositoryError.Corrupt, Assert.Throws<RepositoryException>(() => { if (field == "Name" || field == "Notes") live!.ListReferences(); else live!.ReadCheckpoint(cp); }).Error);
        }
    }
    [Fact] public void RetainedValidatorArrayCannotMutatePersistedImageAndMissingRecoveryQueryNeverLies()
    {
        using var repo = Open(); var original = Image(); var first = repo.CommitCheckpoint(new("one", "main", 0, null, original));
        validator.Retained![0] = 57;
        Assert.Equal(new byte[] { 49 }, repo.ReadCheckpoint(first.CheckpointId!).ReadPayload());
        repo.Dispose(); Assert.Throws<ObjectDisposedException>(() => repo.FindOperation("one"));
    }
    public void Dispose() { if (Directory.Exists(path)) Directory.Delete(path, true); }
    sealed class Validator : ICheckpointValidator
    {
        public byte[]? Retained;
        public CheckpointImage Validate(byte[] bytes) { Retained = bytes; return new(bytes, "edition", int.Parse(Encoding.UTF8.GetString(bytes), System.Globalization.CultureInfo.InvariantCulture), 0, "Planning"); }
    }
}
