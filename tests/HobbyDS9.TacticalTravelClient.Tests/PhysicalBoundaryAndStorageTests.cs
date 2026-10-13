using System.Text;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.TacticalReviewStorage;
using Xunit;
using static HobbyDS9.TacticalTravelClient.Tests.PhysicalFixture;

namespace HobbyDS9.TacticalTravelClient.Tests;

public sealed class PhysicalBoundaryAndStorageTests
{
    [Theory]
    [InlineData("turn",CampaignTacticalError.StaleTurn)]
    [InlineData("revision",CampaignTacticalError.StaleRevision)]
    [InlineData("foreign-fleet",CampaignTacticalError.InvalidOrder)]
    [InlineData("foreign-actor",CampaignTacticalError.InvalidOrder)]
    [InlineData("unknown-actor",CampaignTacticalError.UnknownActor)]
    [InlineData("unknown-contact",CampaignTacticalError.InvalidOrder)]
    [InlineData("contact-stamp",CampaignTacticalError.InvalidOrder)]
    [InlineData("unknown-route",CampaignTacticalError.InvalidOrder)]
    public void RealBackendRejectsTravelCommandDefectsWithoutChangingTheCompletePhysicalRoot(string defect,CampaignTacticalError error)
    {
        using var f=new PhysicalFixture(); f.Own.SaveCommittedToSlot("before");
        var before=File.ReadAllBytes(f.PathFor("before")); var s=f.Own.Frame.Status;
        var command=new SubmitCampaignTravelOrderDto {
            ActorId=A,CommandId="command.invalid.travel",ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,
            Order=new CampaignTravelOrderDto { FleetId=FA,Intent=CampaignTravelIntent.Follow,ContactFleetId=FB,
                ExpectedContactSeenTurn=f.Own.Frame.Travel!.KnownContacts.Single(k=>k.FleetId == FB).LastSeenTurn,
                RouteNodeIds=f.Own.FindKnownRoute(FA,"nav.bajor")!.Route.Steps.ToList() } };
        switch(defect)
        {
            case "turn": command.ExpectedTurn++; break;
            case "revision": command.ExpectedRevision++; break;
            case "foreign-fleet": command.Order.FleetId=FB; break;
            case "foreign-actor": command.ActorId=B; break;
            case "unknown-actor": command.ActorId="civ.unknown"; break;
            case "unknown-contact": command.Order.ContactFleetId="fleet.hidden"; break;
            case "contact-stamp": command.Order.ExpectedContactSeenTurn++; break;
            case "unknown-route": command.Order.RouteNodeIds=new List<string>{"nav.hidden"}; break;
        }
        Assert.Equal(error,f.Host.SubmitTravelOrder(command).Error);
        f.Own.SaveCommittedToSlot("after"); Assert.Equal(before,File.ReadAllBytes(f.PathFor("after")));
    }
    [Fact]
    public void StaleTravelReceiptRetainsDraftUntilExplicitRefreshAndDoesNotResubmitIt()
    {
        using var f=new PhysicalFixture(); var oldOffer=f.Own.FindKnownRoute(FA,"nav.bajor")!;
        Assert.True(f.Own.EditTravel(FA,CampaignTravelIntent.Follow,oldOffer,FB)); Assert.True(f.Own.ReviewTravel());
        var work=f.Own.ConfirmTravel("command.stale")!;
        Travel(f.Host,f.Other,CampaignTravelIntent.Leave,"nav.bajoran-wormhole");
        var result=f.Host.Execute(work); Assert.Equal(CampaignTacticalError.StaleRevision,result.Result.Error);
        Assert.True(f.Own.Receive(result)); Assert.True(f.Own.Dirty); Assert.True(f.Own.RefreshRequired);
        Assert.False(f.Own.ReviewTravel()); Assert.Equal(CampaignTravelIntent.Stay,f.Own.Frame.Travel!.OwnOrders.Single().Intent);
        Assert.True(f.Own.Refresh()); Assert.False(f.Own.Dirty); Assert.True(f.Own.DraftInvalidated);
        Assert.False(f.Own.EditTravel(FA,CampaignTravelIntent.Follow,oldOffer,FB));
        Assert.True(f.Own.EditTravel(FA,CampaignTravelIntent.Follow,f.Own.FindKnownRoute(FA,"nav.bajor"),FB));
    }
    [Fact]
    public void WrongControllerAndDuplicateTravelCallbacksDoNotConsumeRequestsOrApplyTwice()
    {
        using var f=new PhysicalFixture();
        Assert.True(f.Own.EditTravel(FA,CampaignTravelIntent.StayEngage)); Assert.True(f.Own.ReviewTravel());
        Assert.True(f.Other.EditTravel(FB,CampaignTravelIntent.StayEngage)); Assert.True(f.Other.ReviewTravel());
        var aw=f.Own.ConfirmTravel("command.a")!; var bw=f.Other.ConfirmTravel("command.b")!;
        var ar=f.Host.Execute(aw);
        Assert.False(f.Other.Receive(ar)); Assert.True(f.Other.HasPendingWork);
        Assert.True(f.Own.Receive(ar)); Assert.False(f.Own.Receive(ar));
        var duplicate=f.Host.Execute(aw); Assert.Equal(CampaignTacticalError.DuplicateCommand,duplicate.Result.Error);
        Assert.False(f.Own.Receive(duplicate));
        var br=f.Host.Execute(bw); Assert.Equal(CampaignTacticalError.StaleRevision,br.Result.Error);
        Assert.True(f.Other.Receive(br)); Assert.True(f.Other.RefreshRequired);
        Assert.Equal(1,f.Own.Frame.Status.CampaignRevision);
    }
    [Fact]
    public void DirtyTravelDraftSurvivesSaveAndFailedLoadsAndRequiresExplicitSuccessfulDiscard()
    {
        using var f=new PhysicalFixture();
        Assert.True(f.Own.EditTravel(FA,CampaignTravelIntent.Follow,f.Own.FindKnownRoute(FA,"nav.bajor"),FB));
        Assert.True(f.Own.SaveCommittedToSlot("local").Succeeded); Assert.True(f.Own.Dirty);
        Assert.Equal(CampaignTravelIntent.Stay,f.Store.Read("local").Orders.Single(o=>o.FleetId == FA).Intent);
        Assert.Equal(ReviewCheckpointError.DirtyDraft,f.Own.ReopenFromSlot("local").Error);
        var frame=f.Own.Frame;
        Assert.Equal(ReviewCheckpointError.NotFound,f.Own.LoadCommittedFromSlot("missing",true).Error);
        Assert.Same(frame,f.Own.Frame); Assert.True(f.Own.Dirty);
        Assert.True(f.Own.ReopenFromSlot("local",true).Succeeded); Assert.False(f.Own.Dirty);
        Assert.Equal(CampaignTravelIntent.Stay,f.Own.Frame.Travel!.OwnOrders.Single().Intent);
    }
    [Fact]
    public void ObsoleteRoundAndRouteOffersCannotAffectTheReopenedCompletePhysicalHost()
    {
        using var f=new PhysicalFixture(); var oldOffer=f.Own.FindKnownRoute(FA,"nav.bajor")!;
        f.StartPursuit(); f.Own.SaveCommittedToSlot("before");
        var work=BeginRound(f.Own); var receipt=f.Host.Execute(work); Assert.True(f.Own.Receive(receipt));
        Assert.True(f.Own.ReopenFromSlot("before").Succeeded); Assert.False(f.Own.Receive(receipt));
        var opened=PhysicalReviewHost.OpenFromSlot(f.Package,f.Codec,f.Store,"before",A,f.Own.Frame.Battle!.EncounterId);
        Assert.True(opened.Succeeded); Assert.Throws<InvalidOperationException>(()=>opened.Host!.Execute(work));
        Assert.False(f.Own.Frame.Battle.OwnCommitmentAccepted);
        Assert.False(f.Own.EditTravel(FA,CampaignTravelIntent.Follow,oldOffer,FB));
    }
    [Theory]
    [InlineData("truncated")]
    [InlineData("garbage")]
    [InlineData("wrong-package")]
    [InlineData("wrong-version")]
    [InlineData("nested-only")]
    [InlineData("orders")]
    [InlineData("proof")]
    [InlineData("receipt")]
    [InlineData("contact")]
    [InlineData("legacy-format")]
    public void InvalidPhysicalRootLoadRetainsCurrentHostFrameDraftAndGoodCheckpoint(string defect)
    {
        using var f=new PhysicalFixture(); f.StartPursuit(); f.Own.SaveCommittedToSlot("valid");
        var bytes=File.ReadAllBytes(f.PathFor("valid")); var root=f.Codec.DecodePrivateSave(bytes); byte[] invalid;
        switch(defect)
        {
            case "truncated": invalid=bytes[..(bytes.Length/2)]; break;
            case "garbage": invalid=Encoding.UTF8.GetBytes("PRIVATE enemy destination"); break;
            case "wrong-package": root.PackageHash=new string('0',64); invalid=f.Codec.EncodePrivateSave(root); break;
            case "wrong-version": root.FormatVersion=1; invalid=f.Codec.EncodePrivateSave(root); break;
            case "nested-only": invalid=new JsonReviewHostCodec().EncodePrivateSave(root.Core); break;
            case "orders": root.Orders=null!; invalid=f.Codec.EncodePrivateSave(root); break;
            case "proof": root.MovementProof=null; invalid=f.Codec.EncodePrivateSave(root); break;
            case "receipt": root.LastMovement[0].CostPaid++; invalid=f.Codec.EncodePrivateSave(root); break;
            case "contact": root.Contacts[0].LastSeenNodeId="nav.gamma-exit"; invalid=f.Codec.EncodePrivateSave(root); break;
            default: root.Core.Campaign.SaveFormatVersion=13; invalid=f.Codec.EncodePrivateSave(root); break;
        }
        File.WriteAllBytes(f.PathFor("bad"),invalid);
        Assert.True(f.Own.EditFleet(FA,CampaignFleetManeuver.Screen,null));
        var frame=f.Own.Frame; var draft=JsonSerializer.Serialize(f.Own.Draft);
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,f.Own.LoadCommittedFromSlot("bad",true).Error);
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,f.Own.ReopenFromSlot("bad",true).Error);
        Assert.Same(frame,f.Own.Frame); Assert.Equal(draft,JsonSerializer.Serialize(f.Own.Draft));
        Assert.Equal(bytes,File.ReadAllBytes(f.PathFor("valid")));
        Assert.DoesNotContain("PRIVATE",ReviewTextRenderer.Render(f.Own)); Assert.DoesNotContain(f.Root,ReviewTextRenderer.Render(f.Own));
        Assert.True(f.Own.Refresh()); Assert.Equal(frame.Generation,f.Own.Frame.Generation);
    }
    [Theory]
    [InlineData(ReviewWriteStage.BeforeTemporaryWrite)]
    [InlineData(ReviewWriteStage.AfterPartialWrite)]
    [InlineData(ReviewWriteStage.AfterFlushBeforeReplace)]
    public void PhysicalFailedOverwriteUsesThePreservedAtomicProtocolWithoutDeletingPriorOrOtherTempFiles(ReviewWriteStage stage)
    {
        bool fail=false;
        using var f=new PhysicalFixture(probe:s=>{ if(fail && s == stage) throw new IOException("PRIVATE root path"); });
        f.StartPursuit(); f.Own.SaveCommittedToSlot("prior"); var before=File.ReadAllBytes(f.PathFor("prior"));
        var other=Path.Combine(f.DirectoryPath,".physical-review-checkpoint-other.unrelated.tmp"); File.WriteAllText(other,"preserve");
        Assert.True(f.Own.EditFleet(FA,CampaignFleetManeuver.Screen,null)); var frame=f.Own.Frame; fail=true;
        Assert.Equal(ReviewCheckpointError.WriteFailed,f.Own.SaveCommittedToSlot("prior",true).Error);
        Assert.Equal(before,File.ReadAllBytes(f.PathFor("prior"))); Assert.Equal("preserve",File.ReadAllText(other));
        Assert.Equal(2,Directory.GetFiles(f.DirectoryPath).Length); Assert.Same(frame,f.Own.Frame); Assert.True(f.Own.Dirty);
    }
    [Fact]
    public void B1AndPhysicalSlotsAreSeparateAndCrossFormatBytesRejectWithoutMigrationOrNestedExtraction()
    {
        using var f=new PhysicalFixture(); f.StartPursuit(); f.Own.SaveCommittedToSlot("same");
        var b1Codec=new JsonReviewHostCodec(); var b1Store=new DiskReviewCheckpointStore(f.DirectoryPath,b1Codec);
        var b1Host=ReviewHost.Start(f.Package.CorePackage!,b1Codec,b1Store); var b1=new ReviewController(b1Host,A);
        Assert.True(b1.SaveCommittedToSlot("same").Succeeded);
        var b1Path=Path.Combine(f.DirectoryPath,"review-checkpoint-same.json"); var physical=File.ReadAllBytes(f.PathFor("same"));
        var b1Bytes=File.ReadAllBytes(b1Path); Assert.NotEqual(b1Bytes,physical);
        File.WriteAllBytes(f.PathFor("b1-copied"),b1Bytes);
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,f.Own.LoadCommittedFromSlot("b1-copied").Error);
        var b1Copied=Path.Combine(f.DirectoryPath,"review-checkpoint-physical-copied.json"); File.WriteAllBytes(b1Copied,physical);
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,b1.LoadCommittedFromSlot("physical-copied").Error);
        Assert.True(b1.LoadCommittedFromSlot("same").Succeeded); Assert.True(f.Own.LoadCommittedFromSlot("same").Succeeded);
        Assert.Equal(physical,File.ReadAllBytes(f.PathFor("same"))); Assert.Equal(b1Bytes,File.ReadAllBytes(b1Path));
    }
    [Theory]
    [InlineData("../outside")]
    [InlineData("a/b")]
    [InlineData("UPPER")]
    public void PhysicalInvalidSlotPathsCannotTouchOutsideTheReviewDirectory(string slot)
    {
        using var f=new PhysicalFixture();
        Assert.Equal(ReviewCheckpointError.InvalidSlot,f.Own.SaveCommittedToSlot(slot).Error);
        Assert.Equal(ReviewCheckpointError.InvalidSlot,f.Own.LoadCommittedFromSlot(slot).Error);
        Assert.False(Directory.Exists(f.Root));
    }
    [Fact]
    public async Task PhysicalWaitingReloadDisplaysLawfulOwnStateAndNoPrivateCarrierEnemyStocksOrOrders()
    {
        using var f=new PhysicalFixture(); f.StartPursuit(); Commit(f.Host,f.Own); f.Other.Refresh(); f.Own.SaveCommittedToSlot("private");
        var child=await f.Child("inspect","private",actor:B);
        Assert.Equal(0,child.Exit); Assert.Equal(ReviewTextRenderer.Render(f.Other)+ReviewTextRenderer.RenderReport(f.Other),child.Output);
        Assert.DoesNotContain("ship.equipment-cruiser",child.Output);
        Assert.DoesNotContain("Own accepted "+FA,child.Output); Assert.DoesNotContain("Own travel "+FA,child.Output);
        Assert.DoesNotContain("MovementProof",child.Output); Assert.DoesNotContain("RandomState",child.Output);
        foreach(var type in new[]{typeof(ReviewFrame),typeof(ReviewCheckpointResult),typeof(PhysicalHostOpenResult)})
            Assert.DoesNotContain(type.GetProperties(),p=>p.PropertyType == typeof(CampaignTacticalTravelSaveDto));
    }
    [Fact]
    public void FailedPhysicalAdvanceDoesNotInstallPartialReceiptsOrDiscardLocalTravelDraft()
    {
        using var f=new PhysicalFixture(); var old=f.Own.Frame;
        Assert.True(f.Own.EditTravel(FA,CampaignTravelIntent.Leave,f.Own.FindKnownRoute(FA,"nav.bajor")));
        Travel(f.Host,f.Other,CampaignTravelIntent.Leave,"nav.bajor");
        var result=f.Host.AdvancePlanning(old);
        Assert.Equal(CampaignTacticalError.StaleRevision,result.Error); Assert.Same(old,f.Own.Frame); Assert.True(f.Own.Dirty);
        Assert.Empty(f.Host.Read(A).Travel!.OwnReceipts);
    }
    [Fact]
    public void BackendSuppliedStaleContactRemainsLastSeenAndIsNotOfferedForNewFollow()
    {
        using var f=new PhysicalFixture(adjust:p=>p.ContactFreshnessTurns=0);
        f.Advance();
        var contact=Assert.Single(f.Own.Frame.Travel!.KnownContacts);
        Assert.False(contact.Fresh); Assert.Equal(1,contact.LastSeenTurn); Assert.Equal(2,f.Own.Frame.Status.Turn);
        Assert.False(f.Own.EditTravel(FA,CampaignTravelIntent.Follow,f.Own.FindKnownRoute(FA,"nav.bajor"),FB));
        Assert.Contains("fresh False; current position and destination unavailable",ReviewTextRenderer.Render(f.Own));
    }
    [Fact]
    public void UnknownKnownRouteResultCannotBecomeALeaveDraftOrMutation()
    {
        using var f=new PhysicalFixture(); var revision=f.Own.Frame.Status.CampaignRevision;
        var route=f.Own.FindKnownRoute(FA,"nav.unknown")!;
        Assert.False(route.Route.Success); Assert.False(f.Own.EditTravel(FA,CampaignTravelIntent.Leave,route));
        Assert.False(f.Own.Dirty); Assert.Equal(revision,f.Host.Read(A).Status.CampaignRevision);
    }
    [Fact]
    public void CancelAndExplicitResetPreserveOrClearOnlyTheLocalTravelEdit()
    {
        using var f=new PhysicalFixture();
        Assert.True(f.Own.EditTravel(FA,CampaignTravelIntent.Follow,f.Own.FindKnownRoute(FA,"nav.bajor"),FB));
        Assert.True(f.Own.ReviewTravel()); Assert.True(f.Own.CancelReview()); Assert.True(f.Own.Dirty);
        Assert.Equal(CampaignTravelIntent.Stay,Assert.Single(f.Own.Frame.Travel!.OwnOrders).Intent);
        Assert.True(f.Own.ResetTravelDraft()); Assert.False(f.Own.Dirty);
        Assert.Null(f.Own.ConfirmTravel("command.cancelled")); Assert.Equal(0,f.Own.Frame.Status.CampaignRevision);
    }
    [Fact]
    public void PhysicalSerializationFailurePreservesPreviouslyValidFileAndTravelDraft()
    {
        using var f=new PhysicalFixture(); var codec=new FaultPhysicalCodec();
        var store=new DiskPhysicalReviewCheckpointStore(f.DirectoryPath,codec);
        var host=PhysicalReviewHost.Start(f.Package,codec,store); var ui=new ReviewController(host,A);
        Assert.True(ui.SaveCommittedToSlot("serialization").Succeeded);
        var bytes=File.ReadAllBytes(f.PathFor("serialization"));
        Assert.True(ui.EditTravel(FA,CampaignTravelIntent.Follow,ui.FindKnownRoute(FA,"nav.bajor"),FB)); codec.Fail=true;
        Assert.Equal(ReviewCheckpointError.SerializationFailed,ui.SaveCommittedToSlot("serialization",true).Error);
        Assert.Equal(bytes,File.ReadAllBytes(f.PathFor("serialization"))); Assert.True(ui.Dirty);
        Assert.Single(Directory.GetFiles(f.DirectoryPath)); Assert.DoesNotContain("PRIVATE",ReviewTextRenderer.Render(ui));
    }
    private sealed class FaultPhysicalCodec : IPhysicalReviewFileCodec
    {
        private readonly JsonPhysicalReviewCodec inner=new JsonPhysicalReviewCodec();
        public bool Fail { get; set; }
        public CampaignTacticalTravelPackageDto CopyPackage(CampaignTacticalTravelPackageDto p)=>inner.CopyPackage(p);
        public CampaignTacticalTravelSaveDto CopySave(CampaignTacticalTravelSaveDto s)=>inner.CopySave(s);
        public byte[] EncodePrivateSave(CampaignTacticalTravelSaveDto s)=>Fail?throw new IOException("PRIVATE root contents"):inner.EncodePrivateSave(s);
        public CampaignTacticalTravelSaveDto DecodePrivateSave(byte[] bytes)=>inner.DecodePrivateSave(bytes);
    }
}
