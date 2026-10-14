using HobbyDS9.UnityReview;
using Xunit;

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
}
