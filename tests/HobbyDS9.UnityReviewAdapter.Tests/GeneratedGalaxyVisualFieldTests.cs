using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class GeneratedGalaxyVisualFieldTests
{
    [Fact] public void SixFacesAndOutsideAreZeroWhileInteriorAndWispsExist()
    {
        uint seed=GeneratedGalaxyVisualField.Seed("42","nebula.one");
        for(int axis=0;axis<3;axis++)foreach(double sign in new[]{-1.0,1.0})
        for(int i=-4;i<=4;i++)for(int j=-4;j<=4;j++)
        {
            var p=new[]{i/4.0,j/4.0,0.0};p[2]=p[axis];p[axis]=sign;
            Assert.Equal(0,GeneratedGalaxyVisualField.Density(p[0],p[1],p[2],seed));
        }
        Assert.Equal(0,GeneratedGalaxyVisualField.Density(1.01,0,0,seed));
        Assert.InRange(GeneratedGalaxyVisualField.Density(-.25,.1,0,seed),.05,1);
        Assert.True(GeneratedGalaxyVisualField.Density(.65,.2,.1,seed)>0);
    }
    [Fact] public void VisualSeedIdentityAndContinuousNoiseAreStable()
    {
        uint a=GeneratedGalaxyVisualField.Seed("42","nebula.one");
        Assert.Equal(a,GeneratedGalaxyVisualField.Seed("42","nebula.one"));
        Assert.NotEqual(a,GeneratedGalaxyVisualField.Seed("43","nebula.one"));
        Assert.NotEqual(a,GeneratedGalaxyVisualField.Seed("42","nebula.two"));
        for(int k=-4;k<5;k++)
        {
            Assert.InRange(Math.Abs(GeneratedGalaxyVisualField.Noise(k-1e-5,.41,.73,a)-GeneratedGalaxyVisualField.Noise(k+1e-5,.41,.73,a)),0,1e-6);
            for(int z=-5;z<=5;z++)Assert.InRange(GeneratedGalaxyVisualField.Density(k/5.0,.2,z/5.0,a),0,1);
        }
    }
    [Theory][InlineData(900,480)][InlineData(1500,850)][InlineData(2800,1200)]
    public void ReferencePlaneEqualsViewportWhileCameraPanHasTrueDepthDisparity(double width,double height)
    {
        var v=new GeneratedGalaxyViewport{MaximumZoom=32};v.SetMap(5000000,4000000);v.Resize(width,height);
        foreach(double zoom in new[]{1.0,3.0,32.0})
        {
            v.Fit();v.ZoomAt(zoom,width*.3,height*.7);v.Pan(width*9,-height*9);
            foreach(var p in new[]{(0.0,0.0),(2500000.0,2000000.0),(5000000.0,4000000.0)})
            {
                double u=v.X(p.Item1)/width,w=v.Y(p.Item2)/height;
                double px=v.WorldX(0)/5000000+u*width/v.Scale/5000000;
                double py=v.WorldY(0)/4000000+w*height/v.Scale/4000000;
                double cx=v.WorldX(width/2)/5000000,cy=v.WorldY(height/2)/4000000;
                double q=GeneratedGalaxyVisualField.Perspective(0);
                Assert.Equal(p.Item1/5000000,cx+(px-cx)/q,10);
                Assert.Equal(p.Item2/4000000,cy+(py-cy)/q,10);
            }
        }
        // Independent perspective projection of one fixed object at two depths:
        // moving camera changes their relative separation; depth-off does not.
        double Project(double point,double camera,double z)=>camera+(point-camera)*GeneratedGalaxyVisualField.Perspective(z);
        double Disparity(double camera)=>Project(.7,camera,.32)-Project(.7,camera,-.32);
        Assert.NotEqual(Disparity(.5),Disparity(.6));
        Assert.Equal(0,Project(.7,.6,0)-Project(.7,.6,0));
        Assert.InRange(GeneratedGalaxyVisualField.Perspective(double.MaxValue),1,1.1);
    }
    [Theory][InlineData(1280,720)][InlineData(3440,1440)][InlineData(10000,10000)]
    public void OutputAllocationHasFixedPixelCap(double width,double height)
    {
        var p=GeneratedGalaxyVisualField.TargetSize(width,height);
        Assert.InRange(p.width,1,960);Assert.InRange(p.height,1,540);
        Assert.True(p.width*p.height<=960*540);
        Assert.Equal((1,1),GeneratedGalaxyVisualField.TargetSize(double.NaN,720));
    }
}
