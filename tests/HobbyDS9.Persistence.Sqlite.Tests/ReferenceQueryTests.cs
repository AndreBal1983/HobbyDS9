using System.Text;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using Xunit;

namespace HobbyDS9.Persistence.Sqlite.Tests;

public sealed class ReferenceQueryTests : IDisposable
{
    readonly string directory = Path.Combine(Path.GetTempPath(), "hobbyds9-query-" + Guid.NewGuid().ToString("N"));
    readonly Validator validator = new();
    SqliteCampaignRepository Open() => new(directory, "campaign", "fixture", validator);
    static CheckpointImage Image(int turn) => new(Encoding.UTF8.GetBytes(turn.ToString(System.Globalization.CultureInfo.InvariantCulture)), "fixture", turn, 0, "Planning");
    [Fact] public void FilteredNewestFirstPagesAreBoundedStableAndNeverReplayPayload()
    {
        using var repo = Open(); var a = repo.CommitCheckpoint(new("a", "a", 0, null, Image(1))).CheckpointId!;
        repo.ForkBranch("fork", "b", a);
        var b = repo.CommitCheckpoint(new("b", "b", 0, a, Image(2))).CheckpointId!;
        for (var i = 0; i < 24; i++) repo.SetReference("put-" + i, new("slot-" + i.ToString("D3"), 0, i % 2 == 0 ? SaveReferenceKind.Manual : SaveReferenceKind.Auto, "探査 " + i), i % 3 == 0 ? a : b);
        var calls = validator.Calls;
        var result = new List<SaveReference>(); ReferenceCursor? cursor = null;
        do
        {
            var page = repo.QueryReferences(new("探査", SaveReferenceKind.Manual, "b", 3, cursor));
            Assert.InRange(page.Rows.Count, 0, 3); result.AddRange(page.Rows); cursor = page.Next;
        } while (cursor != null);
        Assert.Equal(calls, validator.Calls); Assert.Equal(8, result.Count); Assert.Equal(8, result.Select(r => r.Id).Distinct().Count());
        Assert.All(result, r => { Assert.Equal("b", r.CheckpointBranchId); Assert.Equal(SaveReferenceKind.Manual, r.Kind); Assert.Equal(2, r.Turn); });
        Assert.Equal(result.Select(r => r.Id), result.OrderByDescending(r => r.UpdatedUtc, StringComparer.Ordinal).ThenBy(r => r.Id, StringComparer.Ordinal).Select(r => r.Id));
    }
    [Fact] public void MetadataMutationInvalidatesCursorAndChangedFilterCannotReuseIt()
    {
        using var repo = Open(); var cp = repo.CommitCheckpoint(new("a", "main", 0, null, Image(1))).CheckpointId!;
        for (int i = 0; i < 3; i++) repo.SetReference("put-" + i, new("slot-" + i, 0, SaveReferenceKind.Manual, "Name"), cp);
        var first = repo.QueryReferences(new(limit: 1)); Assert.True(first.HasMore);
        Assert.Equal(RepositoryError.InvalidRequest, Assert.Throws<RepositoryException>(() => repo.QueryReferences(new("Different", limit: 1, cursor: first.Next))).Error);
        repo.SetReference("rename", new("slot-0", 1, SaveReferenceKind.Manual, "New name"), cp);
        Assert.Equal(RepositoryError.Conflict, Assert.Throws<RepositoryException>(() => repo.QueryReferences(new(limit: 1, cursor: first.Next))).Error);
        var refreshed = repo.QueryReferences(new("New name", limit: 1)); Assert.Single(refreshed.Rows); Assert.False(refreshed.HasMore);
    }
    [Fact] public void SearchUsesLiteralUnicodeAndParameterDataRatherThanSqlWildcardsOrActiveBranchGuessing()
    {
        using var repo = Open(); var a = repo.CommitCheckpoint(new("a", "original", 0, null, Image(1))).CheckpointId!;
        repo.ForkBranch("fork", "current", a);
        repo.SetReference("put", new("literal", 0, SaveReferenceKind.Manual, "🖖 %_ Quote' case"), a);
        Assert.Single(repo.QueryReferences(new("%_")).Rows);
        Assert.Single(repo.QueryReferences(new("🖖")).Rows);
        Assert.Empty(repo.QueryReferences(new("CASE")).Rows); // Defined exact case-sensitive search.
        Assert.Empty(repo.QueryReferences(new("' OR 1=1 --")).Rows);
        var saved = Assert.Single(repo.QueryReferences(new(checkpointBranchId: "original")).Rows);
        Assert.Equal("original", saved.CheckpointBranchId);
        Assert.Empty(repo.QueryReferences(new(checkpointBranchId: "current")).Rows);
    }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    sealed class Validator : ICheckpointValidator
    {
        public int Calls;
        public CheckpointImage Validate(byte[] bytes) { Calls++; return Image(int.Parse(Encoding.UTF8.GetString(bytes), System.Globalization.CultureInfo.InvariantCulture)); }
    }
}
