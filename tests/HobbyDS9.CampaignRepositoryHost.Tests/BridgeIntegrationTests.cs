using System.Security.Cryptography;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.RepositoryTestFixtures;
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
    [Fact]
    public void Format18CombatCheckpointRecoversPendingRoundsAndFinalizedCampaign()
    {
        using var f = new RepositoryFixture();
        var uninterrupted = f.Host();
        var session = Session(uninterrupted);
        CycleRepositoryTestCombat.ReachAwaitingCombat(uninterrupted, session, useFormat18Itinerary: true);

        var restored = ColdRestore("awaiting");
        var encounter = Assert.Single(session.GetEncounters(Human));
        Assert.NotEmpty(Physical(session).Requests);
        Assert.Empty(Assert.Single(Physical(session).Core.Battles).ResolvedRounds);
        Assert.False(Session(restored).Observe(encounter, Human).Complete);

        for (var rounds = 0; rounds < 32 && !session.Observe(encounter, Human).Complete; ++rounds)
        {
            foreach (var host in new[] { uninterrupted, restored })
            {
                var current = Session(host);
                var battle = current.Observe(encounter, Human);
                Assert.True(current.CommitRound(new()
                {
                    ActorId = Human, EncounterId = encounter,
                    CommandId = $"command.repository.cold.round.{battle.Round}",
                    ExpectedTurn = battle.Turn, ExpectedRound = battle.Round,
                    ExpectedBoundaryRevision = battle.BoundaryRevision, Replacements = new()
                }).Accepted);
            }
            Assert.Equal(f.Bytes(uninterrupted), f.Bytes(restored));
        }
        Assert.True(session.Observe(encounter, Human).Complete);
        Assert.Equal(SessionPhase.AwaitingCombat, session.CaptureStatus().Phase);
        Assert.NotEmpty(Assert.Single(Physical(session).Core.Battles).ResolvedRounds);

        restored = ColdRestore("resolved");
        Assert.True(Session(restored).Observe(encounter, Human).Complete);
        foreach (var host in new[] { uninterrupted, restored })
        {
            var current = Session(host);
            var stamp = current.CaptureStatus();
            Assert.True(current.FinalizeBatch(stamp.Turn, stamp.CampaignRevision).Accepted);
            Assert.Equal(SessionPhase.Planning, current.CaptureStatus().Phase);
            Assert.Empty(current.GetEncounters(Human));
        }
        Assert.Equal(f.Bytes(uninterrupted), f.Bytes(restored));
        restored = ColdRestore("finalized");
        Advance(uninterrupted);
        Advance(restored);
        Assert.Equal(f.Bytes(uninterrupted), f.Bytes(restored));

        PhysicalReviewHost ColdRestore(string boundary)
        {
            var image = f.Image(uninterrupted);
            string checkpoint;
            using (var repository = f.Repository(image))
            {
                var branch = boundary == "awaiting" ? new BranchInfo("main", null, 0) : repository.GetBranch("main");
                var receipt = repository.CommitCheckpoint(new("save-" + boundary, branch.Id, branch.Generation,
                    branch.Head, CycleCheckpointValidator.Map(image), Manual(boundary)));
                checkpoint = receipt.CheckpointId!;
            }
            var restarted = f.Host();
            using var reopened = f.Repository(image);
            var currentBranch = reopened.GetBranch("main");
            var queue = new RepositorySaveQueue(restarted, restarted.Read(Human), reopened, currentBranch, f.Wire);
            var work = Assert.IsType<RepositoryPayloadWork>(queue.PreparePayloadRead(restarted.Read(Human), checkpoint).Work);
            var result = work.Execute();
            Assert.Equal(CampaignRepositoryError.None, result.Error);
            Assert.Equal(image.PayloadHash, result.Summary!.PayloadHash);
            Assert.Equal(CampaignRepositoryError.None, queue.CompletePayloadRestore(work, out var frame));
            Assert.True(queue.Rebind(restarted, frame!, reopened, currentBranch));
            Assert.True(queue.ConfirmRestoredCheckpoint(result.Loaded!));
            Assert.Equal(image.ReadPayload(), f.Bytes(restarted));
            return restarted;
        }

        static CampaignTacticalTravelSaveDto Physical(ICampaignColonisationCycleSession current) =>
            current.ExportCycleSave().BaseCampaign!.Campaign!.Integrated!.Physical!;
    }

    [Fact]
    public void ProgressedFormat18ColdRestorePreservesPaidMineAndPendingRefinement()
    {
        using var f = new RepositoryFixture();
        var uninterrupted = f.Host();
        CycleRepositoryTestProgress.MineAndRefine(Session(uninterrupted), Human);
        var image = f.Image(uninterrupted);
        var pending = CycleRepositoryTestProgress.AssertMineAndRefinement(
            Assert.IsType<CampaignBeltBodiesSaveDto>(f.Codec.DecodeCycleRoot(image.ReadPayload())));

        string checkpoint;
        using (var repository = f.Repository(image))
        {
            var queue = new RepositorySaveQueue(uninterrupted, uninterrupted.Read(Human), repository, new BranchInfo("main", null, 0), f.Wire);
            Assert.Equal(SaveRequestStatus.Queued, queue.RequestManual(uninterrupted.Read(Human), Manual(), out _));
            var committed = Run(queue, uninterrupted);
            Assert.Equal(SaveOutcome.Committed, committed.Outcome);
            checkpoint = committed.Receipt!.CheckpointId!;
            Assert.True(queue.Snapshot.CanQuitAfterManualSave);
        }

        var restarted = f.Host();
        using var reopened = f.Repository(image);
        var branch = reopened.GetBranch("main");
        var cold = new RepositorySaveQueue(restarted, restarted.Read(Human), reopened, branch, f.Wire);
        var work = Assert.IsType<RepositoryPayloadWork>(cold.PreparePayloadRead(restarted.Read(Human), checkpoint).Work);
        var candidate = work.Execute();
        Assert.Equal(CampaignRepositoryError.None, candidate.Error);
        Assert.Equal(image.PayloadHash, candidate.Summary!.PayloadHash);
        Assert.Equal(CampaignRepositoryError.None, cold.CompletePayloadRestore(work, out var restoredFrame));
        Assert.True(cold.Rebind(restarted, restoredFrame!, reopened, branch));
        Assert.True(cold.ConfirmRestoredCheckpoint(candidate.Loaded!));
        Assert.Equal(f.Bytes(uninterrupted), f.Bytes(restarted));

        foreach (var host in new[] { uninterrupted, restarted })
        {
            var session = Session(host);
            var stamp = session.CaptureStatus();
            var refinement = (ICampaignEngineeringRefinementSession)session;
            Assert.True(refinement.SubmitEngineeringRefinement(new()
            {
                ActorId = Human, CommandId = "command.repository.cold.discard",
                ExpectedTurn = stamp.Turn, ExpectedRevision = stamp.CampaignRevision,
                Action = EngineeringRefinementAction.Discard, ProjectId = pending
            }).Accepted);
            Advance(host);
        }
        Assert.Equal(f.Bytes(uninterrupted), f.Bytes(restarted));
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
