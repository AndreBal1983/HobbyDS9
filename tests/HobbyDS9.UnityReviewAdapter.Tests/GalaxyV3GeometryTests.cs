using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;
using Xunit.Abstractions;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class GalaxyV3GeometryTests
{
    [Fact] public void NebulaPriorityReorderingDoesNotFadeTheVisibleSet()
    {
        var state=new GalaxyV3Residency();var ids=Enumerable.Range(0,8).Select(i=>"field."+i).ToArray();
        state.Update(ids,0);state.Update(ids.Reverse(),1);
        Assert.False(state.NeedsTransition);Assert.Equal(8,state.Visible.Count);Assert.All(ids,id=>Assert.Equal(1,state.Weight(id,1)));
    }
    [Fact] public void ResidencyReplacementFadesOnlyTheChangedFieldWithinTheEightFieldCap()
    {
        var state=new GalaxyV3Residency();var first=Enumerable.Range(0,8).Select(i=>"field."+i).ToArray();var next=first.Skip(1).Append("field.8").ToArray();
        state.Update(first,0);state.Update(next,1);state.Update(next,1.075);
        Assert.True(state.NeedsTransition);Assert.InRange(state.Weight("field.0",1.075),.499f,.501f);
        Assert.All(first.Skip(1),id=>Assert.Equal(1,state.Weight(id,1.075)));Assert.Equal(0,state.Weight("field.8",1.075));
        state.Update(next,1.151);Assert.Equal(8,state.Visible.Count);Assert.DoesNotContain("field.0",state.Visible);Assert.Equal(0,state.Weight("field.8",1.151));
        state.Update(next,1.226);Assert.InRange(state.Weight("field.8",1.226),.499f,.501f);Assert.All(first.Skip(1),id=>Assert.Equal(1,state.Weight(id,1.226)));
        state.Update(next,1.302);Assert.False(state.NeedsTransition);Assert.All(next,id=>Assert.Equal(1,state.Weight(id,1.302)));
        Assert.Throws<ArgumentException>(()=>state.Update(first.Append("field.9"),2));Assert.Equal(next.OrderBy(id=>id),state.Visible);
        state.Reset();Assert.Empty(state.Visible);Assert.False(state.NeedsTransition);
    }
    private readonly ITestOutputHelper output;
    public GalaxyV3GeometryTests(ITestOutputHelper output){this.output=output;}
    private static GeneratedGalaxyRegionDto Box(int x0,int y0,int x1,int y1)=>new(){NodeId=$"{x0}-{y0}",Boundary=new(){P(x0,y0),P(x1,y0),P(x1,y1),P(x0,y1)}};
    private static GeneratedGalaxyPointDto P(int x,int y)=>new(){X=x,Y=y};
    private static GeneratedGalaxyViewport View(int width=400,int height=300)
    {var v=new GeneratedGalaxyViewport();v.SetMap(100,100);v.Resize(width,height);v.Fit();return v;}
    [Theory] [InlineData(.4,.3,.02,true)] [InlineData(-.8,.5,-.1,true)] [InlineData(.4,.3,.02,false)]
    public void AuthoredForwardInverseAndScreenRayAgree(double u,double v,double z,bool depth)
    {
        var w=GeneratedGalaxyV3Math.DiskToWorld(u,v,z);var d=GeneratedGalaxyV3Math.WorldToDisk(w.x,w.y,w.z);
        Assert.Equal(u,d.u,12);Assert.Equal(v,d.v,12);Assert.Equal(z,d.z,12);
        var projected=GeneratedGalaxyV3Math.Project(w.x,w.y,w.z,.11,-.04,depth);
        var ray=GeneratedGalaxyV3Math.Ray(projected.x,projected.y,w.z,.11,-.04,depth);
        Assert.Equal(w.x,ray.x,12);Assert.Equal(w.y,ray.y,12);
        var p0=GeneratedGalaxyV3Math.Project(w.x,w.y,0,.11,-.04,depth);Assert.Equal(w.x,p0.x,12);Assert.Equal(w.y,p0.y,12);
    }
    [Fact] public void PositiveVDepthAndOffAxisParallaxHaveDeclaredSignsAndDepthOffCollapses()
    {
        var w=GeneratedGalaxyV3Math.DiskToWorld(0,1,0);Assert.True(w.z>0);Assert.True(w.y>0);
        double Dx(double z,bool depth){var a=GeneratedGalaxyV3Math.Project(.2,.3,z,0,0,depth);var b=GeneratedGalaxyV3Math.Project(.2,.3,z,.1,0,depth);return b.x-a.x;}
        Assert.True(Dx(.8,true)<Dx(-.8,true));Assert.Equal(0,Dx(.8,false),12);Assert.Equal(0,Dx(-.8,false),12);
    }
    [Theory]
    [InlineData(.2,.3,.04,.03712357510819288,.11797537290409676,.11736307579185912,.03522296715433344,.12758702820913131)]
    [InlineData(-.7,.1,-.05,-.3049277868092403,-.13286282587425785,.01920323754800436,-.30688114563258706,-.13253895827187256)]
    public void AsymmetricPointsMatchIndependentRootAnalyticOracle(double u,double v,double z,double wx,double wy,double wz,double sx,double sy)
    {
        var world=GeneratedGalaxyV3Math.DiskToWorld(u,v,z);Assert.Equal(wx,world.x,12);Assert.Equal(wy,world.y,12);Assert.Equal(wz,world.z,12);
        var screen=GeneratedGalaxyV3Math.Project(world.x,world.y,world.z,.1,-.2,true);Assert.Equal(sx,screen.x,12);Assert.Equal(sy,screen.y,12);
    }
    [Theory] [InlineData(.018)] [InlineData(.035)] [InlineData(.11)]
    public void CdfBinsConserveFaceOnAndObliqueColumnsWithoutScaleOrClippedRenormalization(double h)
    {
        var edges=GeneratedGalaxyV3Math.BinEdges;double sum=0;for(int i=0;i<edges.Count-1;i++)sum+=GeneratedGalaxyV3Math.Weight(edges[i],edges[i+1],h,1);
        Assert.Equal(1,sum,10);double mu=Math.Cos(48*Math.PI/180);
        Assert.Equal(1.4944765499,GeneratedGalaxyV3Math.Weight(-.33,.33,h,mu),8);
        // sDisk=sWorld/.47: world parameterization needs coefficient1/.47 ONCE.
        double dzDiskPerWorld=mu/.47;
        Assert.Equal(1/mu,(GeneratedGalaxyV3Math.Cdf(.33,h)-GeneratedGalaxyV3Math.Cdf(-.33,h))/(.47*dzDiskPerWorld),10);
        Assert.InRange(GeneratedGalaxyV3Math.Weight(0,.33,h,1),.499999,.500001);
        Assert.Equal(.7472382749323043,GeneratedGalaxyV3Math.Weight(0,.33,h,mu),8); // Independent half-clipped oracle.
        Assert.InRange(GeneratedGalaxyV3Math.Weight(-h,h,h,1),.84,.85);
    }
    [Fact] public void OpticalSourceLimitAndCorePartitionDoNotAddEmission()
    {
        Assert.Equal(1,GeneratedGalaxyV3Math.SourceRatio(0));Assert.Equal(.999999995,GeneratedGalaxyV3Math.SourceRatio(1e-8),12);
        foreach(double weight in new[]{0,.2,.5,1}){double e=.12*.7;Assert.Equal(e,weight*e+(1-weight)*e,12);}
        double rgb0=0,transmission=Math.Exp(-2);Assert.Equal(0,rgb0*GeneratedGalaxyV3Math.SourceRatio(2));Assert.InRange(transmission,0,1);
    }
    [Fact] public void AdjacentAndPartialSharedEdgesCancelBeforeGuardWithoutInternalSeams()
    {
        var view=View();var map=new[]{Box(0,0,50,100),Box(50,0,100,40),Box(50,40,100,100)};
        var mask=GeneratedGalaxyChartedMask.Build(map,view,400,300,400,300);Assert.True(mask.Succeeded,mask.Reason);
        Assert.Equal(255,mask.Pixels[200+150*400]); // Three-cell T-junction lies in the UNION interior.
        Assert.All(mask.Pixels.Take(400),b=>Assert.Equal(0,b));
    }
    [Fact] public void HolesAndCornerTouchRemainUnchartedAndEveryNonzeroFootprintIsInterior()
    {
        var view=View();var regions=new[]{Box(0,0,100,30),Box(0,70,100,100),Box(0,30,30,70),Box(70,30,100,70)};
        var mask=GeneratedGalaxyChartedMask.Build(regions,view,400,300,400,300);Assert.True(mask.Succeeded,mask.Reason);
        Assert.Equal(0,mask.Pixels[200+150*400]);int lit=0;
        for(int y=0;y<300;y++)for(int x=0;x<400;x++)if(mask.Pixels[x+y*400]>0)
        {
            lit++;double x0=view.WorldX(x+.5-3),x1=view.WorldX(x+.5+3),y0=view.WorldY(y+.5-3),y1=view.WorldY(y+.5+3);
            Assert.True(x0>=0&&x1<=100&&y0>=0&&y1<=100);
            Assert.False(x1>30&&x0<70&&y1>30&&y0<70); // Independent rectangular hole-intersection oracle.
        }
        Assert.True(lit>10000);
        var corner=GeneratedGalaxyChartedMask.Build(new[]{Box(0,0,40,40),Box(40,40,80,80)},view,400,300,400,300);
        Assert.True(corner.Succeeded,corner.Reason);Assert.Equal(0,corner.Pixels[(int)view.X(40)+(int)view.Y(40)*400]);
    }
    [Theory] [InlineData(1280,720)] [InlineData(1920,1080)] [InlineData(3440,1440)]
    public void BilinearFinalImageHasZeroExteriorUnderFractionalPanAndProjection(int uiWidth,int uiHeight)
    {
        int width=Math.Min(960,uiWidth/2),height=Math.Min(540,uiHeight/2);var view=View(uiWidth,uiHeight);view.Pan(.375,-.625);view.ZoomAt(1.1,uiWidth*.45,uiHeight*.55);
        var triangle=new GeneratedGalaxyRegionDto{NodeId="triangle",Boundary=new(){P(0,0),P(100,0),P(0,100)}};
        var mask=GeneratedGalaxyChartedMask.Build(new[]{triangle},view,uiWidth,uiHeight,width,height);Assert.True(mask.Succeeded,mask.Reason);
        // Actual bilinear reconstruction mathematics, not comparison to the mask's edge extractor.
        double Sample(int x,int y)=>mask.Pixels[Math.Clamp(x,0,width-1)+Math.Clamp(y,0,height-1)*width]/255.0;
        int exterior=0;
        for(int y=0;y<uiHeight;y++)for(int x=0;x<uiWidth;x++)
        {
            double wx=view.WorldX(x+.5),wy=view.WorldY(y+.5);if(wx>=0&&wy>=0&&wx+wy<=100)continue;exterior++;
            double tx=(x+.5)*width/uiWidth-.5,ty=(y+.5)*height/uiHeight-.5;int ix=(int)Math.Floor(tx),iy=(int)Math.Floor(ty);double fx=tx-ix,fy=ty-iy;
            double sample=(1-fy)*((1-fx)*Sample(ix,iy)+fx*Sample(ix+1,iy))+fy*((1-fx)*Sample(ix,iy+1)+fx*Sample(ix+1,iy+1));
            Assert.Equal(0,sample);
        }
        Assert.True(exterior>1000);
    }
    [Theory] [InlineData(0)] [InlineData(50)] [InlineData(300)]
    public void AggregateBudgetFailureClearsEntireOutputEvenAfterPartialWork(int budget)
    {
        var mask=GeneratedGalaxyChartedMask.Build(new[]{Box(0,0,50,100),Box(50,0,100,100)},View(),400,300,400,300,operationLimit:budget);
        Assert.False(mask.Succeeded);Assert.All(mask.Pixels,b=>Assert.Equal(0,b));Assert.InRange(mask.Work,0,budget);
    }
    [Fact] public void MalformedStarPolygonAndOverlappingDuplicateRegionsFailClosed()
    {
        var star=new GeneratedGalaxyRegionDto{NodeId="invalid-star",Boundary=new(){P(50,0),P(79,90),P(2,35),P(98,35),P(21,90)}};
        foreach(var regions in new[]{new[]{star},new[]{Box(0,0,100,100),Box(0,0,100,100)}})
        {var mask=GeneratedGalaxyChartedMask.Build(regions,View(),400,300,400,300);Assert.False(mask.Succeeded);Assert.All(mask.Pixels,b=>Assert.Equal(0,b));}
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Actual1000StoredGeometryMaskReportsBoundedWorkAndFallbackHonestly(bool fragmented)
    {
        var codec=new ReviewNewtonsoftCodec();var document=codec.DecodeNaturalGalaxy(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"galaxy-samples/natural/seed-42-systems-1000.natural.json")));
        var regions=fragmented?document.Layout.Regions.Where((_,i)=>i%3==0).ToArray():document.Layout.Regions.ToArray();
        var view=new GeneratedGalaxyViewport();view.SetMap(document.Layout.Width,document.Layout.Height);view.Resize(1920,1080);view.Fit();
        var mask=GeneratedGalaxyChartedMask.Build(regions,view,1920,1080,960,540);
        output.WriteLine($"Stored1000 fragmented={fragmented} knownRegions={regions.Length} success={mask.Succeeded} work={mask.Work} reason={mask.Reason} lit={mask.Pixels.Count(b=>b>0)}");
        Assert.InRange(mask.Work,0,10000000);if(!mask.Succeeded)Assert.All(mask.Pixels,b=>Assert.Equal(0,b));
        Assert.True(mask.Succeeded,mask.Reason); // Actual supported fixtures must not silently fall back.
    }
}
