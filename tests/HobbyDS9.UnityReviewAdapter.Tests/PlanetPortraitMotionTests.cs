using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class PlanetPortraitMotionTests
{
    [Fact]public void SlowIdleSpinStopsForReducedMotionAndDragWhileManualPoseRemainsAvailable()
    {
        var motion=new PlanetPortraitMotion();for(var i=0;i<8;i++)Assert.True(motion.Advance(.125,true));Assert.Equal(4,motion.Yaw,10);
        motion.ReducedMotion=true;Assert.False(motion.Advance(.2,true));Assert.Equal(4,motion.Yaw,10);
        Assert.True(motion.BeginDrag(0,100,100));Assert.True(motion.MoveDrag(0,120,80));Assert.Equal(-3,motion.Yaw,10);Assert.Equal(5,motion.Pitch,10);
        motion.ReducedMotion=false;Assert.False(motion.Advance(.2,true));Assert.Equal(-3,motion.Yaw,10);
        Assert.True(motion.EndDrag(0));Assert.True(motion.Advance(.25,true));Assert.Equal(-2,motion.Yaw,10);
    }
    [Fact]public void OnlyCapturedPointerCanMoveOrEndAndCancelKeepsTheLastAcceptedPose()
    {
        var motion=new PlanetPortraitMotion();Assert.True(motion.BeginDrag(7,10,20));Assert.False(motion.BeginDrag(8,10,20));
        Assert.False(motion.MoveDrag(8,90,90));Assert.False(motion.EndDrag(8));Assert.True(motion.Dragging);
        Assert.True(motion.MoveDrag(7,30,40));Assert.Equal(-7,motion.Yaw,10);Assert.Equal(-5,motion.Pitch,10);
        motion.CancelDrag();Assert.False(motion.Dragging);Assert.False(motion.MoveDrag(7,60,60));Assert.Equal(-7,motion.Yaw,10);
        Assert.True(motion.BeginDrag(2,900,900));Assert.False(motion.MoveDrag(2,900,900));Assert.Equal(-7,motion.Yaw,10);
    }
    [Fact]public void HiddenOrUnfocusedTimeDoesNotAdvanceAndIdleHitchesNeverCauseCatchupSpins()
    {
        var motion=new PlanetPortraitMotion();Assert.False(motion.Advance(10,false));Assert.Equal(0,motion.Yaw);
        Assert.True(motion.Advance(90,true));Assert.Equal(1,motion.Yaw,10);
        foreach(var invalid in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity,-1,0})Assert.False(motion.Advance(invalid,true));
        Assert.Equal(1,motion.Yaw,10);
    }
    [Fact]public void ManualRotationWrapsClampsAndRejectsMalformedCoordinatesWithoutChangingPose()
    {
        var motion=new PlanetPortraitMotion();Assert.True(motion.Nudge(179,0));Assert.True(motion.Nudge(5,999));Assert.Equal(-176,motion.Yaw,10);Assert.Equal(65,motion.Pitch);
        Assert.True(motion.Nudge(0,-999));Assert.Equal(-65,motion.Pitch);Assert.False(motion.Nudge(double.NaN,0));Assert.Equal(-176,motion.Yaw,10);
        Assert.False(motion.BeginDrag(-1,0,0));Assert.False(motion.BeginDrag(0,double.NaN,0));Assert.True(motion.BeginDrag(0,0,0));Assert.False(motion.MoveDrag(0,0,double.PositiveInfinity));
        motion.CancelDrag();Assert.True(motion.Reset());Assert.False(motion.Reset());Assert.Equal(0,motion.Yaw);Assert.Equal(0,motion.Pitch);
    }
}
