using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.TacticalReviewStorage;
using Xunit;
using static HobbyDS9.TacticalReviewStorage.Tests.DiskReviewFixture;

namespace HobbyDS9.TacticalReviewStorage.Tests;

public sealed class DurableCheckpointTests
{
    [Fact]
    public async Task AcceptedWaitingCheckpointFinishesInAFreshProcessWithIdenticalReportsStocksAndSaveVector()
    {
        using var canonical=new DiskReviewFixture(); using var resumed=new DiskReviewFixture();
        canonical.Allocate(); resumed.Allocate();
        Send(canonical.Host,canonical.A); Send(resumed.Host,resumed.A);
        Assert.True(resumed.B.EditFleet(FleetB,CampaignFleetManeuver.Screen,null));
        var saved=resumed.A.SaveCommittedToSlot("waiting"); Assert.True(saved.Succeeded);
        Assert.Equal("waiting",saved.Checkpoint!.Slot); Assert.True(resumed.B.Dirty);
        var child=await resumed.Child("finish","waiting","finished");
        Assert.Equal(0,child.Exit); Assert.Empty(child.Error);
        Send(canonical.Host,canonical.B); canonical.A.Refresh(); canonical.B.Refresh();
        var expectedReport=canonical.CompleteAndFinalize();
        Assert.True(canonical.A.SaveCommittedToSlot("canonical").Succeeded);
        var expected=File.ReadAllBytes(canonical.PathFor("canonical")); var actual=File.ReadAllBytes(resumed.PathFor("finished"));
        Assert.Equal(expected,actual);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(expected)),Convert.ToHexString(SHA256.HashData(actual)));
        Assert.Contains(expectedReport,child.Output);
        Assert.DoesNotContain("RandomState",child.Output); Assert.DoesNotContain("AcceptedRoundCommands",child.Output);
        Assert.Equal(ReviewMode.Waiting,resumed.A.Mode); // Child cannot mutate the original host.
        var reopened=ReviewHost.OpenFromSlot(resumed.Package,resumed.Codec,new DiskReviewCheckpointStore(resumed.ReviewDirectory,resumed.Codec),"finished",ActorA);
        Assert.True(reopened.Succeeded); Assert.False(reopened.Frame!.Status.ProductionActivationAllowed);
        Assert.Equal(JsonSerializer.Serialize(canonical.A.Frame.OwnShips),JsonSerializer.Serialize(reopened.Frame.OwnShips));
        Assert.Equal(SessionPhase.Planning,reopened.Frame.Status.Phase);
    }
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task RoundBoundaryReopensInANewProcessWithIdenticalLawfulInspectionAndReports(int rounds)
    {
        using var f=new DiskReviewFixture(); f.Allocate();
        for(var r=0;r<rounds;r++) f.Round();
        Assert.True(f.A.SaveCommittedToSlot("boundary").Succeeded);
        var before=ReviewTextRenderer.Render(f.A)+ReviewTextRenderer.RenderReport(f.A);
        var child=await f.Child("inspect","boundary");
        Assert.Equal(0,child.Exit); Assert.Empty(child.Error); Assert.Equal(before,child.Output);
    }
    [Fact]
    public void SaveKeepsLocalDraftAndOnlyExplicitDirtyConsentPermitsReloadOrReopen()
    {
        using var f=new DiskReviewFixture(); f.Allocate();
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Flank,FleetB));
        Assert.True(f.A.SaveCommittedToSlot("draft").Succeeded); Assert.True(f.A.Dirty);
        Assert.Equal(ReviewCheckpointError.DirtyDraft,f.A.LoadCommittedFromSlot("draft").Error);
        Assert.Equal(ReviewCheckpointError.DirtyDraft,f.A.ReopenFromSlot("draft").Error);
        Assert.True(f.A.Dirty); Assert.True(f.A.ReopenFromSlot("draft",true).Succeeded);
        Assert.False(f.A.Dirty); Assert.Empty(f.A.Frame.Battle!.OwnCommittedOrders);
        Assert.Equal(CampaignFleetManeuver.Hold,Assert.Single(f.A.Frame.Battle.StandingOrders).Maneuver);
    }
    [Fact]
    public void DuplicateSavesAndLoadsDoNotAlterBackendRevisionStocksOrPrivateCommitments()
    {
        using var f=new DiskReviewFixture(); f.Allocate(); Send(f.Host,f.A);
        var before=JsonSerializer.Serialize(f.A.Frame.Battle); var revision=f.A.Frame.Status.CampaignRevision;
        Assert.True(f.A.SaveCommittedToSlot("repeat").Succeeded); var bytes=File.ReadAllBytes(f.PathFor("repeat"));
        Assert.Equal(ReviewCheckpointError.SlotOccupied,f.A.SaveCommittedToSlot("repeat").Error);
        Assert.True(f.A.SaveCommittedToSlot("repeat",true).Succeeded); Assert.Equal(bytes,File.ReadAllBytes(f.PathFor("repeat")));
        var generation=f.A.Frame.Generation;
        Assert.True(f.A.LoadCommittedFromSlot("repeat").Succeeded); Assert.True(f.A.LoadCommittedFromSlot("repeat").Succeeded);
        Assert.Equal(generation+2,f.A.Frame.Generation); Assert.Equal(revision,f.A.Frame.Status.CampaignRevision);
        Assert.Equal(before,JsonSerializer.Serialize(f.A.Frame.Battle)); Assert.Equal(ReviewMode.Waiting,f.A.Mode);
        Assert.Single(Directory.GetFiles(f.ReviewDirectory));
    }
    [Fact]
    public void ReopenInvalidatesPreReopenWorkAndCallbacksEvenIfTheSavedGenerationNumbersCoincide()
    {
        using var f=new DiskReviewFixture(); f.Allocate(); Assert.True(f.A.SaveCommittedToSlot("before").Succeeded);
        Assert.True(f.A.LoadCommittedFromSlot("before").Succeeded);
        var priorGeneration=f.A.Frame.Generation;
        var work=Begin(f.A); var receipt=f.Host.Execute(work); Assert.True(f.A.Receive(receipt));
        Assert.True(f.A.ReopenFromSlot("before").Succeeded);
        Assert.Equal(priorGeneration,f.A.Frame.Generation); // Host identity, not just this number, changes.
        var frame=f.A.Frame; Assert.False(f.A.Receive(receipt)); Assert.Same(frame,f.A.Frame);
        var opened=ReviewHost.OpenFromSlot(f.Package,f.Codec,f.Store,"before",ActorA,frame.Battle!.EncounterId);
        Assert.True(opened.Succeeded);
        Assert.Throws<InvalidOperationException>(()=>opened.Host!.Execute(work));
        Assert.False(f.A.Frame.Battle!.OwnCommitmentAccepted);
        Assert.Equal(ReviewMode.Orders,f.A.Mode);
    }
    [Fact]
    public void PendingWorkBlocksDurableSaveLoadAndReopenWithoutLosingTheRequest()
    {
        using var f=new DiskReviewFixture(); f.Allocate(); Assert.True(f.A.SaveCommittedToSlot("busy").Succeeded);
        var work=Begin(f.A); var frame=f.A.Frame;
        Assert.Equal(ReviewCheckpointError.Busy,f.A.SaveCommittedToSlot("busy",true).Error);
        Assert.Equal(ReviewCheckpointError.Busy,f.A.LoadCommittedFromSlot("busy",true).Error);
        Assert.Equal(ReviewCheckpointError.Busy,f.A.ReopenFromSlot("busy",true).Error);
        Assert.Same(frame,f.A.Frame); Assert.True(f.A.HasPendingWork);
        Assert.True(f.A.Receive(f.Host.Execute(work)));
    }
    [Theory]
    [InlineData("")]
    [InlineData("../escape")]
    [InlineData("..")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("UPPER")]
    [InlineData("a.")]
    [InlineData("a ")]
    [InlineData("_leading")]
    [InlineData("é")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("absolute")]
    public void InvalidSlotNamesCannotCreateReadOrOverwriteOutsideTheReviewDirectory(string input)
    {
        using var f=new DiskReviewFixture(); var slot=input == "absolute" ? Path.Combine(f.Root,"outside") : input;
        var before=f.A.Frame;
        Assert.Equal(ReviewCheckpointError.InvalidSlot,f.A.SaveCommittedToSlot(slot,true).Error);
        Assert.Equal(ReviewCheckpointError.InvalidSlot,f.A.LoadCommittedFromSlot(slot).Error);
        Assert.Equal(ReviewCheckpointError.InvalidSlot,f.A.ReopenFromSlot(slot).Error);
        Assert.Same(before,f.A.Frame); Assert.False(Directory.Exists(f.Root));
    }
    [Theory]
    [InlineData("")]
    [InlineData("relative-review")]
    [InlineData("root")]
    public void StoreRequiresAnExplicitNonRootAbsoluteReviewDirectory(string directory)
    {
        using var f=new DiskReviewFixture();
        var input=directory == "root" ? Path.GetPathRoot(f.Root)! : directory;
        var error=Assert.Throws<ReviewCheckpointStoreException>(()=>new DiskReviewCheckpointStore(input,f.Codec));
        Assert.Equal(ReviewCheckpointError.InvalidDirectory,error.Error);
        Assert.False(Directory.Exists(f.Root));
    }
    [Theory]
    [InlineData(ReviewWriteStage.BeforeTemporaryWrite)]
    [InlineData(ReviewWriteStage.AfterPartialWrite)]
    [InlineData(ReviewWriteStage.AfterFlushBeforeReplace)]
    public void FailedOverwriteRetainsExactPriorFileLiveStateDraftAndOtherTemporaryFiles(ReviewWriteStage stage)
    {
        bool fail=false;
        using var f=new DiskReviewFixture(s=>{ if(fail && s == stage) throw new IOException("PRIVATE enemy-orders and file path"); });
        f.Allocate(); Assert.True(f.A.SaveCommittedToSlot("stable").Succeeded);
        var before=File.ReadAllBytes(f.PathFor("stable"));
        var unrelated=Path.Combine(f.ReviewDirectory,".review-checkpoint-other.unrelated.tmp"); File.WriteAllText(unrelated,"leave me");
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Screen,FleetB));
        var frame=f.A.Frame; fail=true;
        Assert.Equal(ReviewCheckpointError.WriteFailed,f.A.SaveCommittedToSlot("stable",true).Error);
        Assert.Equal(before,File.ReadAllBytes(f.PathFor("stable"))); Assert.Same(frame,f.A.Frame); Assert.True(f.A.Dirty);
        Assert.Equal("leave me",File.ReadAllText(unrelated)); Assert.Equal(2,Directory.GetFiles(f.ReviewDirectory).Length);
        Assert.DoesNotContain("PRIVATE",ReviewTextRenderer.Render(f.A)); Assert.DoesNotContain(f.Root,ReviewTextRenderer.Render(f.A));
        fail=false; Assert.True(f.A.LoadCommittedFromSlot("stable",true).Succeeded); Assert.False(f.A.Dirty);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HostCopyOrFileSerializationFailureCannotReplaceThePreviouslyValidCheckpoint(bool failCopy)
    {
        var codec=new FaultCodec(); using var f=new DiskReviewFixture(codec:codec); f.Allocate();
        Assert.True(f.A.SaveCommittedToSlot("serialization").Succeeded); var before=File.ReadAllBytes(f.PathFor("serialization"));
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Flank,FleetB));
        codec.FailCopy=failCopy; codec.FailEncode=!failCopy;
        Assert.Equal(ReviewCheckpointError.SerializationFailed,f.A.SaveCommittedToSlot("serialization",true).Error);
        Assert.Equal(before,File.ReadAllBytes(f.PathFor("serialization"))); Assert.True(f.A.Dirty);
        Assert.Single(Directory.GetFiles(f.ReviewDirectory)); Assert.DoesNotContain("PRIVATE",ReviewTextRenderer.Render(f.A));
    }
    [Fact]
    public void FailedFirstWriteLeavesNoPartialDestinationOrOwnTemporaryFile()
    {
        using var f=new DiskReviewFixture(stage=>{ if(stage == ReviewWriteStage.AfterPartialWrite) throw new IOException(); });
        Assert.Equal(ReviewCheckpointError.WriteFailed,f.A.SaveCommittedToSlot("first").Error);
        Assert.False(File.Exists(f.PathFor("first"))); Assert.Empty(Directory.GetFiles(f.ReviewDirectory));
    }
    [Theory]
    [InlineData("truncated")]
    [InlineData("garbage")]
    [InlineData("null")]
    [InlineData("wrong-package")]
    [InlineData("unsupported-review")]
    [InlineData("next-format")]
    [InlineData("unsupported-legacy")]
    [InlineData("wrong-resolver")]
    [InlineData("unknown-field")]
    public void InvalidPrivateCarriersFailWithoutReplacingHostFrameDraftOrPriorMetadata(string defect)
    {
        using var f=new DiskReviewFixture(); f.Allocate(); Send(f.Host,f.A);
        Assert.True(f.A.SaveCommittedToSlot("valid").Succeeded); var valid=File.ReadAllBytes(f.PathFor("valid"));
        var dto=f.Codec.DecodePrivateSave(valid); byte[] bad;
        switch(defect)
        {
            case "truncated": bad=valid[..(valid.Length/2)]; break;
            case "garbage": bad=Encoding.UTF8.GetBytes("PRIVATE opponent data is not JSON"); break;
            case "null": bad=Encoding.UTF8.GetBytes("null"); break;
            case "wrong-package": dto.PackageHash=new string('0',64); bad=f.Codec.EncodePrivateSave(dto); break;
            case "unsupported-review": dto.FormatVersion=999; bad=f.Codec.EncodePrivateSave(dto); break;
            case "next-format": dto.FormatVersion=2; bad=f.Codec.EncodePrivateSave(dto); break;
            case "unsupported-legacy": dto.Campaign.SaveFormatVersion=13; bad=f.Codec.EncodePrivateSave(dto); break;
            case "wrong-resolver": dto.ResolverVersion=999; bad=f.Codec.EncodePrivateSave(dto); break;
            default: bad=Encoding.UTF8.GetBytes("{\"PRIVATE_enemy_reserves\":123,"+Encoding.UTF8.GetString(valid)[1..]); break;
        }
        File.WriteAllBytes(f.PathFor("bad"),bad);
        // Use another live boundary so a failed load would visibly rewind if it were applied.
        Send(f.Host,f.B); Assert.True(f.A.Refresh());
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Screen,FleetB));
        var frame=f.A.Frame; var draft=JsonSerializer.Serialize(f.A.Draft);
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,f.A.LoadCommittedFromSlot("bad",true).Error);
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,f.A.ReopenFromSlot("bad",true).Error);
        Assert.Same(frame,f.A.Frame); Assert.Equal(draft,JsonSerializer.Serialize(f.A.Draft));
        Assert.Equal(valid,File.ReadAllBytes(f.PathFor("valid")));
        var text=ReviewTextRenderer.Render(f.A); Assert.DoesNotContain("PRIVATE",text); Assert.DoesNotContain(f.Root,text);
        Assert.True(f.A.Refresh()); Assert.Equal(frame.Generation,f.A.Frame.Generation);
    }
    [Fact]
    public void MissingOrCorruptNamedSlotNeverFallsBackToAnotherValidSlot()
    {
        using var f=new DiskReviewFixture(); f.Allocate(); f.A.SaveCommittedToSlot("valid");
        var missing=ReviewHost.OpenFromSlot(f.Package,f.Codec,f.Store,"missing",ActorA);
        Assert.Equal(ReviewCheckpointError.NotFound,missing.Error); Assert.Null(missing.Host);
        File.WriteAllText(f.PathFor("corrupt"),"bad");
        var corrupt=ReviewHost.OpenFromSlot(f.Package,f.Codec,f.Store,"corrupt",ActorA);
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,corrupt.Error); Assert.Null(corrupt.Host);
        Assert.False(f.A.Frame.Battle!.OwnCommitmentAccepted);
    }
    [Fact]
    public void ActorProjectionIsValidatedBeforeReplacingTheRunningHost()
    {
        using var f=new DiskReviewFixture(); f.Allocate(); f.A.SaveCommittedToSlot("actor");
        f.Round(); var frame=f.Host.Read(ActorA,f.A.Frame.Battle!.EncounterId);
        var result=f.Host.LoadCommittedFromSlot("actor","civ.unknown");
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,result.Error); Assert.Null(result.Frame);
        Assert.Equal(JsonSerializer.Serialize(frame),JsonSerializer.Serialize(f.Host.Read(ActorA,frame.Battle!.EncounterId)));
        var opened=ReviewHost.OpenFromSlot(f.Package,f.Codec,f.Store,"actor","civ.unknown");
        Assert.False(opened.Succeeded); Assert.Null(opened.Host);
    }
    [Fact]
    public async Task CrossProcessInspectionDoesNotExposeHiddenEnemyIdentityFreshOrdersOrPrivateSaveDiagnostics()
    {
        using var f=new DiskReviewFixture();
        Assert.True(f.Host.SetApproachCloak(f.A.Frame,"command.cloak",FleetA,true).Accepted); f.A.Refresh(); f.B.Refresh();
        f.Allocate(); Send(f.Host,f.A); f.B.Refresh();
        Assert.True(f.A.SaveCommittedToSlot("cloak").Succeeded);
        var child=await f.Child("inspect","cloak",actor:ActorB);
        Assert.Equal(0,child.Exit); Assert.Empty(child.Error);
        Assert.Equal(ReviewTextRenderer.Render(f.B)+ReviewTextRenderer.RenderReport(f.B),child.Output);
        Assert.DoesNotContain("ship.equipment-cruiser",child.Output); Assert.DoesNotContain("Own accepted "+FleetA,child.Output);
        Assert.DoesNotContain("RandomState",child.Output); Assert.DoesNotContain("Commitments",child.Output);
    }
    [Fact]
    public void ReviewStoreNeverDiscoversOrChangesOrdinaryCampaignFiles()
    {
        using var f=new DiskReviewFixture(); Directory.CreateDirectory(f.ReviewDirectory);
        var ordinary=Path.Combine(f.ReviewDirectory,"campaign-1.json"); File.WriteAllText(ordinary,"ordinary-user-save");
        Assert.True(f.A.SaveCommittedToSlot("review").Succeeded);
        Assert.Equal("ordinary-user-save",File.ReadAllText(ordinary));
        Assert.Equal(ReviewCheckpointError.NotFound,f.A.LoadCommittedFromSlot("campaign-1").Error);
        Assert.Equal("ordinary-user-save",File.ReadAllText(ordinary)); Assert.Equal(2,Directory.GetFiles(f.ReviewDirectory).Length);
    }
    [Fact]
    public void PublicCheckpointMetadataHasNoPayloadExceptionOrFilesystemPath()
    {
        using var f=new DiskReviewFixture(); var result=f.A.SaveCommittedToSlot("metadata");
        Assert.True(result.Succeeded); Assert.Null(result.Frame);
        Assert.Equal(new[]{"CampaignRevision","Slot","Turn"},typeof(ReviewSlotCheckpoint).GetProperties().Select(p=>p.Name).OrderBy(x=>x,StringComparer.Ordinal));
        Assert.Equal("Private committed review slot checkpoint",result.Checkpoint!.ToString());
        foreach(var type in new[]{typeof(ReviewCheckpointResult),typeof(ReviewHostOpenResult),typeof(ReviewSlotCheckpoint)})
            Assert.DoesNotContain(type.GetProperties(),p=>p.PropertyType == typeof(CampaignTacticalReviewSaveDto) || typeof(Exception).IsAssignableFrom(p.PropertyType));
    }
    [Fact]
    public void MaximumLengthSafeSlotWorksAndNullSlotCannotCauseFilesystemMutation()
    {
        using var f=new DiskReviewFixture();
        Assert.Equal(ReviewCheckpointError.InvalidSlot,f.A.SaveCommittedToSlot(null!).Error);
        Assert.False(Directory.Exists(f.Root));
        var slot=new string('a',32); Assert.True(f.A.SaveCommittedToSlot(slot).Succeeded);
        Assert.True(f.A.ReopenFromSlot(slot).Succeeded);
        Assert.Single(Directory.GetFiles(f.ReviewDirectory));
    }
    [Fact]
    public void AFileAtTheConfiguredDirectoryCannotBeRepurposedAsReviewStorage()
    {
        using var f=new DiskReviewFixture(); Directory.CreateDirectory(f.Root);
        File.WriteAllText(f.ReviewDirectory,"preserve this file");
        var error=Assert.Throws<ReviewCheckpointStoreException>(()=>new DiskReviewCheckpointStore(f.ReviewDirectory,f.Codec));
        Assert.Equal(ReviewCheckpointError.InvalidDirectory,error.Error);
        Assert.Equal("preserve this file",File.ReadAllText(f.ReviewDirectory));
    }
    [Fact]
    public void ASlotOccupiedDuringStagingIsPreservedWhenOverwriteWasNotAuthorized()
    {
        bool occupy=false; DiskReviewFixture? fixture=null;
        using var f=new DiskReviewFixture(stage=>{
            if(occupy && stage == ReviewWriteStage.AfterFlushBeforeReplace)
                File.Copy(fixture!.PathFor("source"),fixture.PathFor("new"));
        });
        fixture=f; Assert.True(f.A.SaveCommittedToSlot("source").Succeeded);
        var source=File.ReadAllBytes(f.PathFor("source")); occupy=true;
        Assert.Equal(ReviewCheckpointError.SlotOccupied,f.A.SaveCommittedToSlot("new").Error);
        Assert.Equal(source,File.ReadAllBytes(f.PathFor("new")));
        Assert.Equal(2,Directory.GetFiles(f.ReviewDirectory).Length);
    }
    [Fact]
    public async Task CorruptCarrierInAFreshProcessProducesOnlyAFixedErrorAndNoFallback()
    {
        using var f=new DiskReviewFixture(); f.Allocate(); Assert.True(f.A.SaveCommittedToSlot("valid").Succeeded);
        File.WriteAllText(f.PathFor("bad"),"{ PRIVATE_ENEMY_ORDERS: ");
        var child=await f.Child("inspect","bad");
        Assert.Equal(3,child.Exit); Assert.Empty(child.Error);
        Assert.Equal("Checkpoint result: InvalidCheckpoint"+Environment.NewLine,child.Output);
        Assert.DoesNotContain("PRIVATE",child.Output); Assert.DoesNotContain(f.Root,child.Output);
    }
    private sealed class FaultCodec : IReviewFileCodec
    {
        private readonly JsonReviewHostCodec inner=new JsonReviewHostCodec();
        public bool FailCopy { get; set; }
        public bool FailEncode { get; set; }
        public CampaignTacticalReviewPackageDto CopyPackage(CampaignTacticalReviewPackageDto p) => inner.CopyPackage(p);
        public CampaignTacticalReviewSaveDto CopySave(CampaignTacticalReviewSaveDto s)
            => FailCopy ? throw new IOException("PRIVATE save payload") : inner.CopySave(s);
        public byte[] EncodePrivateSave(CampaignTacticalReviewSaveDto s)
            => FailEncode ? throw new IOException("PRIVATE save payload") : inner.EncodePrivateSave(s);
        public CampaignTacticalReviewSaveDto DecodePrivateSave(byte[] bytes) => inner.DecodePrivateSave(bytes);
    }
}
