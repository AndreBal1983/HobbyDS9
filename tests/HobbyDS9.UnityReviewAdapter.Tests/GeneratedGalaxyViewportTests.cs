using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class GeneratedGalaxyViewportTests
{
    [Theory]
    [InlineData(900,480,600000)]
    [InlineData(1500,850,800000)]
    [InlineData(2800,1200,1000000)]
    public void FitRetainsAspectAndAllMapCorners(double width,double height,double mapHeight)
    {
        var view=new GeneratedGalaxyViewport();view.SetMap(1000000,mapHeight);view.Resize(width,height);
        Assert.InRange(view.X(0),29.99,width/2);
        Assert.InRange(view.Y(0),29.99,height/2);
        Assert.InRange(view.X(1000000),width/2,width-29.99);
        Assert.InRange(view.Y(mapHeight),height/2,height-29.99);
        Assert.Equal(view.X(100)-view.X(0),view.Y(100)-view.Y(0),8);
    }

    [Fact]
    public void ZoomAnchorsPointerAndResizePreservesViewedWorldCenter()
    {
        var view=new GeneratedGalaxyViewport();view.SetMap(1000000,800000);view.Resize(1500,850);
        var x=view.WorldX(600);var y=view.WorldY(350);
        view.ZoomAt(2,600,350);
        Assert.Equal(x,view.WorldX(600),6);Assert.Equal(y,view.WorldY(350),6);
        view.Pan(30,-20);x=view.WorldX(750);y=view.WorldY(425);
        view.Resize(900,480);
        Assert.Equal(x,view.WorldX(450),6);Assert.Equal(y,view.WorldY(240),6);
        view.Fit();Assert.Equal(1,view.Zoom);Assert.Equal(450,view.X(500000));Assert.Equal(240,view.Y(400000));
    }

    [Fact]
    public void InvalidInputCannotPoisonTransformAndPanCannotLoseEntireMap()
    {
        var view=new GeneratedGalaxyViewport();view.SetMap(1000000,800000);view.Resize(900,480);
        view.ZoomAt(double.PositiveInfinity,0,0);view.Pan(double.NaN,0);view.Resize(0,0);
        Assert.Equal(1,view.Zoom);Assert.Equal(450,view.X(500000));
        view.ZoomAt(100,450,240);Assert.Equal(8,view.Zoom);
        view.Pan(double.MaxValue,-double.MaxValue);
        Assert.InRange(view.X(0),-100000,450);Assert.InRange(view.Y(800000),240,100000);
        view.Fit();Assert.Equal(450,view.X(500000));
    }
}
