using System.Security.Cryptography;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using HobbyDS9.TacticalReviewClient;
using Microsoft.Data.Sqlite;
using Xunit;
using static HobbyDS9.CampaignRepositoryHost.Tests.RepositoryFixture;

namespace HobbyDS9.CampaignRepositoryHost.Tests;

public sealed class BridgeIntegrationTests
{
    [Theory][InlineData(14)][InlineData(17)][InlineData(18)]
    public void ActualCarrierCommitColdReopenImportExportAndNextActionRemainExact(int format)
    {
        using var f=new RepositoryFixture();var host=f.Host(format);Advance(host);var image=f.Image(host);var before=f.Bytes(host);
        Directory.CreateDirectory(f.DirectoryPath);var legacy=Path.Combine(f.DirectoryPath,"legacy.json");File.WriteAllBytes(legacy,before);var originalHash=SHA256.HashData(before);
        string checkpoint;
        using(var repo=f.Repository(image,format))
        {
            var receipt=repo.CommitCheckpoint(new("save-actual","main",0,null,CycleCheckpointValidator.Map(image),Manual(),"test-epoch"));checkpoint=receipt.CheckpointId!;
            Assert.Equal(before,repo.ReadCheckpoint(checkpoint).ReadPayload());
            Assert.Equal(receipt.StorageSequence,repo.CommitCheckpoint(new("save-actual","main",0,null,CycleCheckpointValidator.Map(image),Manual(),"test-epoch")).StorageSequence);
            var imported=repo.ImportJson(legacy);Assert.Equal(imported.CheckpointId,repo.ImportJson(legacy).CheckpointId);
            var exported=repo.ExportJson(checkpoint,"export-actual");Assert.Equal(before,File.ReadAllBytes(exported));
        }
        using(var reopened=f.Repository(image,format))
        {
            var cold=reopened.ReadCheckpoint(checkpoint);var restored=f.Host(format);
            var preparation=restored.PrepareRepositoryRestore(restored.Read(Human),cold.ReadPayload(),cold.EditionIdentity,f.Wire);var work=preparation.Work!;var done=work.Execute(()=>false);
            Assert.Equal(CampaignRepositoryError.None,restored.CompleteRepositoryRestore(work,done,out _));Assert.Equal(before,f.Bytes(restored));
            Advance(host);Advance(restored);Assert.Equal(f.Bytes(host),f.Bytes(restored));
        }
        Assert.Equal(originalHash,SHA256.HashData(File.ReadAllBytes(legacy)));Assert.Equal(before,File.ReadAllBytes(legacy));
    }
    [Fact] public void OlderContinuationKeepsBothIndependentFuturesAndManualPins()
    {
        using var f=new RepositoryFixture();var host=f.Host();var first=f.Image(host);using var repo=f.Repository(first);
        var a=repo.CommitCheckpoint(new("first","main",0,null,CycleCheckpointValidator.Map(first),Manual("first"),"epoch-a"));
        Advance(host);Advance(host);var future=repo.CommitCheckpoint(new("future","main",a.BranchGeneration,a.CheckpointId,CycleCheckpointValidator.Map(f.Image(host)),Manual("future"),"epoch-a"));
        repo.ForkBranch("fork-old","continuation",a.CheckpointId!);var alternate=f.Host();Advance(alternate);
        var head=repo.GetBranch("continuation");var other=repo.CommitCheckpoint(new("alternate","continuation",head.Generation,head.Head,CycleCheckpointValidator.Map(f.Image(alternate)),Manual("alternate"),"epoch-b"));
        Assert.Equal(future.CheckpointId,repo.GetBranch("main").Head);Assert.Equal(other.CheckpointId,repo.GetBranch("continuation").Head);
        Assert.Equal(3,repo.ListReferences().Count);Assert.NotEqual(repo.ReadCheckpoint(future.CheckpointId!).ReadPayload(),repo.ReadCheckpoint(other.CheckpointId!).ReadPayload());
        Assert.Equal(first.ReadPayload(),repo.ReadCheckpoint(a.CheckpointId!).ReadPayload());
    }
    [Theory][InlineData("Turn=Turn+1")][InlineData("Revision=Revision+1")][InlineData("Phase='AwaitingCombat'")]
    public void IndexedMetadataCannotReplaceValidatedPayloadFacts(string mutation)
    {
        using var f=new RepositoryFixture();var host=f.Host();var image=f.Image(host);using var repo=f.Repository(image);
        var receipt=repo.CommitCheckpoint(new("original","main",0,null,CycleCheckpointValidator.Map(image),Manual(),"epoch"));var before=f.Bytes(host);
        using(var sql=new SqliteConnection("Data Source="+Path.Combine(f.DirectoryPath,"campaign","campaign.sqlite")+";Pooling=False"))
        {sql.Open();using var c=sql.CreateCommand();c.CommandText="UPDATE Checkpoint SET "+mutation+" WHERE Id=$id";c.Parameters.AddWithValue("$id",receipt.CheckpointId);Assert.Equal(1,c.ExecuteNonQuery());}
        var error=Assert.Throws<RepositoryException>(()=>repo.ReadCheckpoint(receipt.CheckpointId!));Assert.Equal(RepositoryError.Corrupt,error.Error);
        Assert.Equal(before,f.Bytes(host));
    }
    [Fact] public void ExplicitPatchedNativeRecipeIsActuallyLoadedWhenStaged()
    {
        SqliteProviderBootstrap.EnsureInitialized();using var c=new SqliteConnection("Data Source=:memory:");c.Open();using var command=c.CreateCommand();command.CommandText="SELECT sqlite_version()";
        var version=Assert.IsType<string>(command.ExecuteScalar());var marker=Path.Combine(AppContext.BaseDirectory,"hobbyds9-native-recipe.txt");
        if(File.Exists(marker)){Assert.StartsWith("3.54.1",version);Assert.Contains("4b59d3cded9dc1608544160f3b4b10ac974c6ac0d6ffc23064e5f4c8dbd05aea",File.ReadAllText(marker));}
        else Assert.False(string.IsNullOrWhiteSpace(version)); // Headless generated-data evidence only, not R2/native acceptance.
    }
}
