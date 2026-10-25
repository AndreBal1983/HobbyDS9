using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;
using Xunit.Abstractions;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class GalaxyChartedFeatherTests
{
 readonly ITestOutputHelper output;
 public GalaxyChartedFeatherTests(ITestOutputHelper output){this.output=output;}
 static GeneratedGalaxyPointDto P(int x,int y)=>new(){X=x,Y=y};
 static GeneratedGalaxyRegionDto Box(int a,int b,int c,int d)=>new(){NodeId=$"{a}-{b}-{c}-{d}",Boundary=new(){P(a,b),P(c,b),P(c,d),P(a,d)}};
 static GeneratedGalaxyViewport View(int w,int h){var v=new GeneratedGalaxyViewport();v.SetMap(100,100);v.Resize(w,h);v.Fit();return v;}
 [Fact] public void DefaultBytesStayExactAndFullyChartedViewportHasNoNewVignette()
 {
  var regions=new[]{Box(0,0,100,100)};var view=View(400,300);
  var old=GeneratedGalaxyChartedMask.Build(regions,view,400,300,200,150);
  var zero=GeneratedGalaxyChartedMask.Build(regions,view,400,300,200,150,inwardFeatherUiPixels:0);
  Assert.Equal(old.Pixels,zero.Pixels);Assert.Equal(old.Work,zero.Work);
  view.ZoomAt(10,200,150);var full=GeneratedGalaxyChartedMask.Build(regions,view,400,300,200,150,inwardFeatherUiPixels:8);
  Assert.True(full.Succeeded,full.Reason);Assert.All(full.Pixels,b=>Assert.Equal(255,b));
  var empty=GeneratedGalaxyChartedMask.Build(Array.Empty<GeneratedGalaxyRegionDto>(),view,400,300,200,150,inwardFeatherUiPixels:8);
  Assert.True(empty.Succeeded);Assert.All(empty.Pixels,b=>Assert.Equal(0,b));
 }
 [Theory] [InlineData(-1)] [InlineData(65)] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)]
 public void InvalidFeatherFailsClosed(double feather)
 {
  var result=GeneratedGalaxyChartedMask.Build(new[]{Box(0,0,100,100)},View(400,300),400,300,200,150,inwardFeatherUiPixels:feather);
  Assert.False(result.Succeeded);Assert.Equal("invalid-mask-feather",result.Reason);Assert.All(result.Pixels,b=>Assert.Equal(0,b));
 }
 [Fact] public void AnisotropicDistanceMatchesIndependentBruteForceNearestZeroSamples()
 {
  const int w=80,h=100,uw=640,uh=400;var view=View(uw,uh);
  var shape=new GeneratedGalaxyRegionDto{NodeId="diagonal",Boundary=new(){P(0,0),P(100,0),P(0,100)}};
  var binary=GeneratedGalaxyChartedMask.Build(new[]{shape},view,uw,uh,w,h);
  var soft=GeneratedGalaxyChartedMask.Build(new[]{shape},view,uw,uh,w,h,inwardFeatherUiPixels:8);Assert.True(soft.Succeeded,soft.Reason);
  var zeros=Enumerable.Range(0,w*h).Where(i=>binary.Pixels[i]==0).ToArray();Assert.NotEmpty(zeros);
  int partial=0,opaque=0;
  for(int i=0;i<w*h;i++)
  {
   Assert.True(soft.Pixels[i]<=binary.Pixels[i]);if(binary.Pixels[i]==0){Assert.Equal(0,soft.Pixels[i]);continue;}
   double d2=zeros.Min(j=>Math.Pow(((i%w)-(j%w))*(double)uw/w,2)+Math.Pow(((i/w)-(j/w))*(double)uh/h,2));
   double t=Math.Min(1,Math.Sqrt(d2)/24);int expected=(int)Math.Floor(255*t*t*(3-2*t));
   Assert.InRange((int)soft.Pixels[i],Math.Max(0,expected-1),Math.Min(255,expected+1)); // Independent distance oracle; one byte rounding allowance only here.
   if(soft.Pixels[i]>0&&soft.Pixels[i]<255)partial++;if(soft.Pixels[i]==255)opaque++;
  }
  Assert.True(partial>10,$"partial={partial}");Assert.True(opaque>10,$"opaque={opaque}");Assert.True(soft.Pixels.Where(b=>b>0&&b<255).Distinct().Count()>=4);
 }
 [Fact] public void HorizontalContourHasSeveralMonotonicLevelsAndNoSharedCellSeam()
 {
  var view=View(400,300);var shapes=new[]{Box(0,0,50,100),Box(50,0,100,40),Box(50,40,100,100)};
  var result=GeneratedGalaxyChartedMask.Build(shapes,view,400,300,400,300,inwardFeatherUiPixels:8);Assert.True(result.Succeeded,result.Reason);
  Assert.Equal(255,result.Pixels[200+150*400]);
  var row=Enumerable.Range(0,200).Select(x=>result.Pixels[x+150*400]).ToArray();
  Assert.True(row.Where(b=>b>0&&b<255).Distinct().Count()>=6);
  for(int i=1;i<row.Length;i++)Assert.True(row[i]>=row[i-1]);
  var hole=new[]{Box(0,0,100,30),Box(0,70,100,100),Box(0,30,30,70),Box(70,30,100,70)};
  var ring=GeneratedGalaxyChartedMask.Build(hole,view,400,300,400,300,inwardFeatherUiPixels:8);
  Assert.True(ring.Succeeded);Assert.Equal(0,ring.Pixels[200+150*400]);Assert.Contains((byte)255,ring.Pixels);
 }
 [Fact] public void CornerTouchesAndNarrowIslandsNeverGainSupport()
 {
  var view=View(400,300);var cells=new[]{Box(0,0,40,40),Box(40,40,80,80),Box(90,0,95,100)};
  var binary=GeneratedGalaxyChartedMask.Build(cells,view,400,300,400,300);
  var soft=GeneratedGalaxyChartedMask.Build(cells,view,400,300,400,300,inwardFeatherUiPixels:8);
  Assert.True(soft.Succeeded,soft.Reason);Assert.Equal(0,soft.Pixels[(int)view.X(40)+(int)view.Y(40)*400]);
  for(int i=0;i<soft.Pixels.Length;i++)Assert.True(soft.Pixels[i]<=binary.Pixels[i]);
  Assert.Contains(soft.Pixels,b=>b>0&&b<255);Assert.Contains((byte)255,soft.Pixels);
 }
 [Fact] public void FailureDuringNewPassClearsAlreadyCertifiedInterior()
 {
  var regions=new[]{Box(0,0,100,100)};var view=View(400,300);var old=GeneratedGalaxyChartedMask.Build(regions,view,400,300,200,150);Assert.True(old.Succeeded);
  int limit=old.Work+200*150+100;
  var failed=GeneratedGalaxyChartedMask.Build(regions,view,400,300,200,150,operationLimit:limit,inwardFeatherUiPixels:8);
  Assert.False(failed.Succeeded);Assert.Equal("mask-work-budget",failed.Reason);Assert.True(failed.Work>old.Work);Assert.All(failed.Pixels,b=>Assert.Equal(0,b));
 }
 [Theory] [InlineData(false)] [InlineData(true)]
 public void Actual1000SolidAndFragmentedGeometryStillFitsAggregateBudget(bool fragmented)
 {
  var codec=new ReviewNewtonsoftCodec();var map=codec.DecodeNaturalGalaxy(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"galaxy-samples/natural/seed-42-systems-1000.natural.json")));
  var regions=fragmented?map.Layout.Regions.Where((_,i)=>i%3==0).ToArray():map.Layout.Regions.ToArray();
  var view=new GeneratedGalaxyViewport();view.SetMap(map.Layout.Width,map.Layout.Height);view.Resize(1920,1080);view.Fit();
  var result=GeneratedGalaxyChartedMask.Build(regions,view,1920,1080,960,540,inwardFeatherUiPixels:8);
  output.WriteLine($"Feather1000 fragmented={fragmented} regions={regions.Length} success={result.Succeeded} work={result.Work} reason={result.Reason} partial={result.Pixels.Count(b=>b>0&&b<255)} opaque={result.Pixels.Count(b=>b==255)}");
  Assert.True(result.Succeeded,result.Reason);Assert.InRange(result.Work,1,10000000);Assert.Contains((byte)255,result.Pixels);Assert.Contains(result.Pixels,b=>b>0&&b<255);
 }
}

