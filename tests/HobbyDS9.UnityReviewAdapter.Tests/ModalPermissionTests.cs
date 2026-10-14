using HobbyDS9.UnityReview;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.UnityReviewAdapter.Tests.UnityReviewFixture;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class ModalPermissionTests
{
    [Fact]
    public void CancelThenQueuedConfirmMustNotRunTheCanceledAction()
    {
        var gate=new ReviewModalPermission();int actions=0,cancellations=0;
        var callbacks=gate.TryOpen(()=>actions++,()=>cancellations++)!;
        callbacks.Cancel();callbacks.Confirm();
        Assert.Equal(1,cancellations);Assert.Equal(0,actions);
    }
    [Fact]
    public void TwoQueuedConfirmsMustRunTheGrantedActionOnlyOnce()
    {
        var gate=new ReviewModalPermission();int actions=0;
        var callbacks=gate.TryOpen(()=>actions++,()=>{})!;
        callbacks.Confirm();callbacks.Confirm();
        Assert.Equal(1,actions);
    }
    [Fact]
    public void TwoQueuedCancelsCancelOnlyOnceAndNeverRunConfirm()
    {
        var gate=new ReviewModalPermission();int actions=0,cancels=0;
        var callbacks=gate.TryOpen(()=>actions++,()=>cancels++)!;
        callbacks.Cancel();callbacks.Cancel();callbacks.Confirm();
        Assert.Equal(0,actions);Assert.Equal(1,cancels);Assert.False(gate.IsOpen);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public void ReplacementPermissionIsUntouchedByOldConfirmOrCancel(bool oldCancel)
    {
        var gate=new ReviewModalPermission();int oldActions=0,oldCancels=0,newActions=0,newCancels=0;
        var old=gate.TryOpen(()=>oldActions++,()=>oldCancels++)!;old.Cancel();
        var current=gate.TryOpen(()=>newActions++,()=>newCancels++)!;
        if(oldCancel)old.Cancel();else old.Confirm();
        Assert.True(gate.IsOpen);Assert.Equal(0,oldActions);Assert.Equal(1,oldCancels);Assert.Equal(0,newCancels);
        current.Confirm();Assert.Equal(1,newActions);Assert.False(gate.IsOpen);
    }
    [Theory]
    [InlineData("view-replacement")][InlineData("disable")][InlineData("failed-view-build")]
    public void InvalidationRejectsBothCallbacksAndKeepsTheNextPermission(string lifetimeReason)
    {
        // Exercise the actual method called by ReviewEntry lifecycle paths; no engine event is simulated.
        Assert.False(string.IsNullOrEmpty(lifetimeReason));
        var gate=new ReviewModalPermission();int oldActions=0,oldCancels=0,newActions=0;
        var old=gate.TryOpen(()=>oldActions++,()=>oldCancels++)!;gate.Invalidate();Assert.False(gate.IsOpen);
        var current=gate.TryOpen(()=>newActions++,()=>{})!;
        old.Confirm();old.Cancel();Assert.True(gate.IsOpen);Assert.Equal(0,oldActions);Assert.Equal(0,oldCancels);
        current.Confirm();Assert.Equal(1,newActions);
    }
    [Fact]
    public void OpeningWhileActiveDoesNotReplaceItsPermission()
    {
        var gate=new ReviewModalPermission();int first=0,second=0;
        var current=gate.TryOpen(()=>first++,()=>{})!;
        Assert.Null(gate.TryOpen(()=>second++,()=>{}));current.Confirm();
        Assert.Equal(1,first);Assert.Equal(0,second);
    }
    [Fact]
    public void PermissionIsConsumedBeforeReentrantConfirm()
    {
        var gate=new ReviewModalPermission();int actions=0;ReviewModalCallbacks? callbacks=null;
        callbacks=gate.TryOpen(()=>{actions++;Assert.False(gate.IsOpen);callbacks!.Confirm();},()=>{})!;
        callbacks.Confirm();Assert.Equal(1,actions);
    }
    [Fact]
    public void CallbackCanOpenNextModalWithoutAnOldTerminalCallbackClosingIt()
    {
        var gate=new ReviewModalPermission();int actions=0,nextActions=0,nextCancels=0;ReviewModalCallbacks? next=null;
        var old=gate.TryOpen(()=>{actions++;next=gate.TryOpen(()=>nextActions++,()=>nextCancels++);},()=>{})!;
        old.Confirm();Assert.NotNull(next);Assert.True(gate.IsOpen);
        old.Cancel();old.Confirm();Assert.True(gate.IsOpen);Assert.Equal(1,actions);Assert.Equal(0,nextCancels);
        next!.Confirm();Assert.Equal(1,nextActions);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public void ThrowingExternalCallbackStillConsumesPermission(bool cancel)
    {
        var gate=new ReviewModalPermission();int calls=0;
        Action throwing=()=>{calls++;throw new InvalidOperationException("controlled test failure");};
        var callbacks=gate.TryOpen(throwing,throwing)!;
        Assert.Throws<InvalidOperationException>(()=>{if(cancel)callbacks.Cancel();else callbacks.Confirm();});
        callbacks.Cancel();callbacks.Confirm();Assert.Equal(1,calls);Assert.False(gate.IsOpen);
        Assert.NotNull(gate.TryOpen(()=>{},()=>{}));
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public void ActualSameFrameOverwriteSuccessOrFailureGetsOnlyOneWrite(bool fail)
    {
        bool inject=false;int writeAttempts=0;
        using var f=new UnityReviewFixture(probe:s=>{if(s==UnityReviewWriteStage.BeforeWrite){writeAttempts++;if(inject)throw new IOException("controlled failure");}});
        var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);
        Assert.True(ui.Save("same-frame"));writeAttempts=0;inject=fail;
        Assert.True(ui.DraftTravel(FA,CampaignTravelIntent.StayEngage,null,null));
        var frame=ui.Current.Frame;var file=File.ReadAllBytes(f.PathFor("same-frame"));bool? result=null;int calls=0;
        var gate=new ReviewModalPermission();var callbacks=gate.TryOpen(()=>{calls++;result=ui.Save("same-frame",true);},()=>{})!;
        callbacks.Confirm();Assert.Equal(!fail,result);Assert.Same(frame,ui.Current.Frame);
        callbacks.Confirm();callbacks.Cancel();Assert.Equal(1,calls);Assert.Equal(1,writeAttempts);
        Assert.Same(frame,ui.Current.Frame);Assert.True(ui.Current.Dirty);Assert.Equal(file,File.ReadAllBytes(f.PathFor("same-frame")));
        Assert.DoesNotContain(f.DirectoryPath,ui.Message);Assert.DoesNotContain("controlled failure",ui.Message);
    }
    [Fact]
    public void CanceledOverwriteThenQueuedConfirmDoesNotTouchTheRealSlot()
    {
        int writes=0;using var f=new UnityReviewFixture(probe:s=>{if(s==UnityReviewWriteStage.BeforeWrite)writes++;});
        var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);Assert.True(ui.Save("kept"));writes=0;
        var before=File.ReadAllBytes(f.PathFor("kept"));var frame=ui.Current.Frame;
        var gate=new ReviewModalPermission();var callbacks=gate.TryOpen(()=>ui.Save("kept",true),()=>{})!;
        callbacks.Cancel();callbacks.Confirm();Assert.Equal(0,writes);
        Assert.Equal(before,File.ReadAllBytes(f.PathFor("kept")));Assert.Same(frame,ui.Current.Frame);
    }
    [Theory]
    [InlineData("missing")][InlineData("malformed")][InlineData("wrong-format")]
    public void ActualFailedLoadKeepsFrameAndDraftWithNoSecondAttempt(string problem)
    {
        using var f=new UnityReviewFixture();var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);
        Assert.True(ui.Save("valid"));
        if(problem=="malformed")File.WriteAllText(f.PathFor("candidate"),"{bad}");
        if(problem=="wrong-format"){var root=f.Store.Read("valid");root.FormatVersion=1;f.Store.Write("candidate",root,false);}
        Assert.True(ui.DraftTravel(FA,CampaignTravelIntent.StayEngage,null,null));var controller=ui.Current;var frame=controller.Frame;
        int loads=0;bool? result=null;var gate=new ReviewModalPermission();
        var callbacks=gate.TryOpen(()=>{loads++;result=ui.Load("candidate",true);},()=>{})!;
        callbacks.Confirm();callbacks.Confirm();callbacks.Cancel();
        Assert.False(result);Assert.Equal(1,loads);Assert.Same(controller,ui.Current);Assert.Same(frame,ui.Current.Frame);Assert.True(ui.Current.Dirty);
    }
    [Fact]
    public void ActualSuccessfulLoadReplacesFrameOnceAndRejectsOldCallback()
    {
        using var f=new UnityReviewFixture();var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);
        Assert.True(ui.Save("valid"));Assert.True(ui.DraftTravel(FA,CampaignTravelIntent.StayEngage,null,null));var before=ui.Current.Frame;
        int loads=0,cancels=0;var gate=new ReviewModalPermission();
        var callbacks=gate.TryOpen(()=>{loads++;Assert.True(ui.Load("valid",true));},()=>cancels++)!;
        callbacks.Confirm();var loaded=ui.Current.Frame;Assert.NotSame(before,loaded);Assert.False(ui.Current.Dirty);
        callbacks.Confirm();callbacks.Cancel();Assert.Equal(1,loads);Assert.Equal(0,cancels);Assert.Same(loaded,ui.Current.Frame);
    }
    [Fact]
    public void EachGateOwnsOnlyItsOwnRequest()
    {
        var first=new ReviewModalPermission();var second=new ReviewModalPermission();int a=0,b=0;
        var old=first.TryOpen(()=>a++,()=>{})!;var other=second.TryOpen(()=>b++,()=>{})!;
        first.Invalidate();old.Confirm();old.Cancel();Assert.True(second.IsOpen);Assert.Equal(0,a);
        other.Confirm();Assert.Equal(1,b);
    }
}
