using System.Text;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;
using static HobbyDS9.UnityReviewAdapter.Tests.UnityReviewFixture;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class SlotAndUiTests
{
    [Theory]
    [InlineData(UnityReviewWriteStage.BeforeWrite)]
    [InlineData(UnityReviewWriteStage.AfterPartialWrite)]
    [InlineData(UnityReviewWriteStage.BeforeReplace)]
    public void InterruptedOverwriteKeepsPriorFileAndClearsOnlyOwnTemp(UnityReviewWriteStage stage)
    {
        bool fail=false;using var f=new UnityReviewFixture(probe:s=>{if(fail&&s==stage)throw new IOException("private-path-test");});
        var root=f.RootSave("keep");var bytes=File.ReadAllBytes(f.PathFor("keep"));root.Core.FleetSettings[0].ApproachCloak=true;fail=true;
        var ex=Assert.Throws<ReviewCheckpointStoreException>(()=>f.Store.Write("keep",root,true));Assert.Equal(ReviewCheckpointError.WriteFailed,ex.Error);
        Assert.Equal(bytes,File.ReadAllBytes(f.PathFor("keep")));Assert.Single(Directory.GetFiles(f.DirectoryPath));
        Assert.DoesNotContain("private-path-test",ex.Message);
    }
    [Theory]
    [InlineData("")][InlineData("../campaign-1")][InlineData("Upper")][InlineData("a.b")][InlineData("a/b")][InlineData("-leading")][InlineData("é")][InlineData("abcdefghijklmnopqrstuvwxyz1234567")]
    public void RejectsUnboundedOrPathLikeSlotsBeforeCreatingDirectory(string slot)
    {
        using var f=new UnityReviewFixture();var ex=Assert.Throws<ReviewCheckpointStoreException>(()=>f.Store.Write(slot,f.Codec.CopySave(HobbyDS9.Application.CampaignTacticalTravelApplication.StartReviewPackage(f.Package).ExportSave()),false));
        Assert.Equal(ReviewCheckpointError.InvalidSlot,ex.Error);Assert.False(Directory.Exists(f.DirectoryPath));
    }
    [Theory]
    [InlineData("relative")][InlineData(".")][InlineData("")]
    public void RequiresExplicitFullyQualifiedNonRootDirectory(string path)
    {Assert.Equal(ReviewCheckpointError.InvalidDirectory,Assert.Throws<ReviewCheckpointStoreException>(()=>new ReviewUnitySlotStore(path,new ReviewNewtonsoftCodec())).Error);}
    [Fact]
    public void SlotNamespaceIsDisjointAndOverwriteIsExplicit()
    {
        using var f=new UnityReviewFixture();Directory.CreateDirectory(f.DirectoryPath);
        var sentinel=Path.Combine(f.DirectoryPath,"campaign-1.json");File.WriteAllText(sentinel,"default-v1-sentinel");
        f.RootSave("one");Assert.Equal(ReviewCheckpointError.SlotOccupied,Assert.Throws<ReviewCheckpointStoreException>(()=>f.Store.Write("one",f.Store.Read("one"),false)).Error);
        Assert.Equal("default-v1-sentinel",File.ReadAllText(sentinel));Assert.True(File.Exists(f.PathFor("one")));
        Assert.Equal(ReviewCheckpointError.NotFound,Assert.Throws<ReviewCheckpointStoreException>(()=>f.Store.Read("absent")).Error);
    }
    [Fact]
    public void SavingKeepsUnsentDraftFailedLoadKeepsControllerAndSuccessfulLoadReplacesIt()
    {
        using var f=new UnityReviewFixture();var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);
        Assert.True(ui.DraftTravel(FA,CampaignTravelIntent.StayEngage,null,null));var c=ui.Current;
        Assert.True(ui.Save("draft"));Assert.True(c.Dirty);Assert.DoesNotContain(f.Store.Read("draft").Orders,o=>o.Intent==CampaignTravelIntent.StayEngage);
        Assert.False(ui.Load("draft"));Assert.Same(c,ui.Current);Assert.True(c.Dirty);
        File.WriteAllText(f.PathFor("bad"),"{\"private-corrupt\":true}");
        Assert.False(ui.Load("bad",true));Assert.Same(c,ui.Current);Assert.True(c.Dirty);
        var corrupt=f.Store.Read("draft");corrupt.FormatVersion=1;f.Store.Write("wrong",corrupt,false);
        Assert.False(ui.Load("wrong",true));Assert.Same(c,ui.Current);Assert.True(c.Dirty);
        Assert.True(ui.Load("draft",true));Assert.NotSame(c,ui.Current);Assert.False(ui.Current.Dirty);
        Assert.DoesNotContain("private-corrupt",ui.Message);
    }
    [Fact]
    public void ActorSwitchAndExitRequireDirtyConsentAndUnknownActorLeavesOldState()
    {
        using var f=new UnityReviewFixture();var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);
        Assert.True(ui.Inspect(ui.Current.Frame.OwnShips.Single().Condition.ShipId));Assert.True(ui.DraftTravel(FA,CampaignTravelIntent.StayEngage,null,null));var original=ui.Current;
        Assert.False(ui.SwitchActor(B));Assert.False(ui.Close());Assert.Same(original,ui.Current);
        Assert.False(ui.SwitchActor("civ.unknown",true));Assert.Same(original,ui.Current);Assert.True(original.Dirty);
        Assert.True(ui.SwitchActor(B,true));Assert.Equal(B,ui.Current.Frame.Actor);Assert.Null(ui.Current.SelectedShip);Assert.False(ui.Current.Dirty);
        Assert.DoesNotContain("MovementProof",ui.Facts());Assert.DoesNotContain("RandomState",ui.Facts());
        Assert.True(ui.Close());Assert.False(ui.Advance());Assert.Equal("Review closed.",ui.Facts());
    }
    [Fact]
    public void BusyAndPendingGuardsRejectReentrantSaveAndReplacement()
    {
        ReviewUiModel? ui=null;bool nested=true;
        using var f=new UnityReviewFixture(probe:s=>{if(s==UnityReviewWriteStage.BeforeWrite){Assert.True(ui!.Busy);nested=ui.Close(true);}});
        ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);Assert.True(ui.Save("busy"));Assert.False(nested);Assert.False(ui.Closed);
        Assert.True(ui.DraftTravel(FA,CampaignTravelIntent.StayEngage,null,null));Assert.True(ui.Current.ReviewTravel());Assert.NotNull(ui.Current.ConfirmTravel("command.pending"));
        Assert.False(ui.Load("busy",true));Assert.False(ui.SwitchActor(B,true));Assert.False(ui.Close(true));Assert.False(ui.Reset());
    }
    [Fact]
    public void ActualUiModelHotseatResumeAndTwoBattlesUseRealCommandsAndExistingVector()
    {
        using var f=new UnityReviewFixture();var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);
        Assert.True(ui.SetCloak(FA,true,"command.physical.cloak"));
        Assert.True(ui.DraftTravel(FA,CampaignTravelIntent.Follow,"nav.bajor",FB));Assert.True(ui.SendTravel("command.physical."+A+".1"));
        Assert.True(ui.SwitchActor(B));Assert.True(ui.DraftTravel(FB,CampaignTravelIntent.Leave,"nav.bajoran-wormhole",null));Assert.True(ui.SendTravel("command.physical."+B+".1"));
        Assert.True(ui.Advance());
        for(int battle=0;battle<2;battle++)
        {
            Assert.True(ui.SwitchActor(A));
            while(!ui.Current.Frame.Battle!.Complete)
            {
                foreach(var actor in new[]{A,B})
                {
                    Assert.True(ui.SwitchActor(actor));var b=ui.Current.Frame.Battle!;var own=Assert.Single(b.StandingOrders);
                    var maneuver=b.Round==2?CampaignFleetManeuver.Flank:b.Round==3?CampaignFleetManeuver.Screen:CampaignFleetManeuver.Hold;
                    Assert.True(ui.DraftFleet(own.FleetId,maneuver,null,true));
                    Assert.Equal(own.FocusFleetId,Assert.Single(ui.Current.Draft).Focus);
                    Assert.True(ui.CommitRound("command.physical.round."+b.EncounterId+"."+actor+"."+b.Round));
                    Assert.True(ui.Save("ui-boundary",true));Assert.True(ui.Load("ui-boundary",true));
                }
                Assert.True(ui.SwitchActor(A));
            }
            Assert.True(ui.FinalizeBatch());
            if(battle==0)
            {
                var turn=ui.Current.Frame.Status.Turn;
                foreach(var actor in new[]{A,B})
                {Assert.True(ui.SwitchActor(actor));Assert.True(ui.DraftTravel(actor==A?FA:FB,CampaignTravelIntent.StayEngage,null,null));Assert.True(ui.SendTravel("command.physical."+actor+"."+turn));}
                Assert.True(ui.Advance());
            }
        }
        Assert.True(ui.Save("ui-final"));
        var vector=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(f.Store.Read("ui-final")))).ToLowerInvariant();
        Assert.Equal("a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177",vector);
        Assert.False(ui.Current.Frame.Status.ProductionActivationAllowed);
    }
    [Fact]
    public void ObsoleteActualControllerReceiptIsRejectedAfterCompleteRootReload()
    {
        using var f=new UnityReviewFixture();f.Pursue();var controller=f.Own;
        Assert.True(controller.ReviewRound());var work=controller.ConfirmRound("command.stale")!;
        var handle=f.Host.SaveCommitted();f.Host.LoadCommitted(handle,A,controller.Frame.Battle!.EncounterId);
        Assert.Throws<InvalidOperationException>(()=>f.Host.Execute(work));Assert.True(controller.HasPendingWork);
    }
    [Theory]
    [InlineData(1280,true)][InlineData(1920,false)][InlineData(3440,false)]
    public void PlannedReflowPolicyIsPresentationOnly(int width,bool stack)
    {Assert.Equal(stack,ReviewLayoutPolicy.StackPanels(width));Assert.True(ReviewLayoutPolicy.MinimumReadableFont>=16);}
    [Fact]
    public void ExplicitLaunchIsDefaultFalseAndRequiresProfileAndCanonicalActor()
    {
        Assert.False(ReviewLaunchOptions.DefaultEnabled);Assert.False(ReviewLaunchOptions.TryParse(Array.Empty<string>(),out _));
        Assert.False(ReviewLaunchOptions.TryParse(new[]{"--hobbyds9-physical-review"},out _));
        Assert.False(ReviewLaunchOptions.TryParse(new[]{"--hobbyds9-physical-review","--hobbyds9-review-profile","default","--hobbyds9-review-actor",A},out _));
        Assert.True(ReviewLaunchOptions.TryParse(new[]{"--hobbyds9-physical-review","--hobbyds9-review-profile","faster","--hobbyds9-review-actor",A},out var options));Assert.Equal(A,options!.Actor);
    }
}
