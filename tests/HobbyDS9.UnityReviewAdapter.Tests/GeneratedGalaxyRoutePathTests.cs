using System.Numerics;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class GeneratedGalaxyRoutePathTests
{
    [Fact]
    public void ReportedSeed42RouteCrossesOnlyItsEightAcceptedBoundariesWithoutChangingExportOrCost()
    {
        var codec=new ReviewNewtonsoftCodec();
        var catalog=codec.DecodeScenario(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "scenarios/playable-fleet-v1/scenario.json"))).Definitions.Civilizations!;
        var workspace=new GeneratedGalaxyWorkspace(catalog,new GeneratedGalaxyMapStore(Path.GetTempPath()));
        workspace.Seed="42";workspace.Size=50;Assert.True(workspace.Generate(),workspace.Notice);
        var document=workspace.Document!;var original=workspace.ExportBytes();
        Assert.Equal("a3d42067ca95f8453f465faf1381dd77f151eb6d70a0c4cca3cb8486755fb3d3",document.ContentHash);
        var ids=new[]{39,52,26,25,5,13,84,21,44}.Select(i=>$"nav.generated.{i:000}").ToArray();
        var route=workspace.FindRoute(ids[0],ids[^1]);Assert.True(route.Success);
        Assert.Equal(ids.Skip(1),route.Steps);Assert.Equal(8,route.TotalCost);
        var regions=document.Regions.ToDictionary(r=>r.NodeId);
        var path=GeneratedGalaxyRoutePath.Build(regions,ids[0],route.Steps);
        Assert.Equal(10,path.Count);
        AssertCorridor(regions,ids,path);
        Assert.DoesNotContain(ids.Skip(1).Take(ids.Length-2),id=>path.Any(p=>
            p.X2==2L*regions[id].Center.X&&p.Y2==2L*regions[id].Center.Y));
        for(int i=0;i<ids.Length-1;i++)AssertCrossing(regions,ids[i],ids[i+1]);
        // Regression witness: the old centre chord actually enters a non-route cell.
        var oldA=regions[ids[1]].Center;var oldB=regions[ids[2]].Center;
        Assert.Contains(Enumerable.Range(1,99),t=>StrictlyInside(regions["nav.generated.077"],
            oldA.X+(oldB.X-oldA.X)*t/100.0,oldA.Y+(oldB.Y-oldA.Y)*t/100.0));
        // Exercise every real edge, including the 266-unit near-junction edge in this map.
        foreach(var edge in document.Connections)AssertCrossing(regions,edge.EndpointA,edge.EndpointB);
        Assert.Equal(original,workspace.ExportBytes());
        Assert.Equal(route.TotalCost,workspace.FindRoute(ids[0],ids[^1]).TotalCost);
        Assert.Equal(route.Steps,workspace.FindRoute(ids[0],ids[^1]).Steps);
    }

    [Fact]
    public void EveryRetainedLegacyMapUsesExactPortalsWithoutMutatingItsDocument()
    {
        var codec=new ReviewNewtonsoftCodec();
        var samples=Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"galaxy-samples"),"*.galaxy.json",SearchOption.AllDirectories);
        Assert.Equal(7,samples.Length);
        foreach(var sample in samples)
        {
            var document=codec.DecodeGeneratedGalaxy(File.ReadAllBytes(sample));
            var before=codec.EncodeGeneratedGalaxy(document);var regions=document.Regions.ToDictionary(r=>r.NodeId);
            foreach(var edge in document.Connections)AssertCrossing(regions,edge.EndpointA,edge.EndpointB);
            Assert.Equal(before,codec.EncodeGeneratedGalaxy(document));
        }
    }

    [Fact]
    public void ScaledSampleCorridorsContainAllTransitSegmentsWithoutChangingMapBytes()
    {
        var codec=new ReviewNewtonsoftCodec();
        foreach(var sample in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"galaxy-samples/scaled"),"*.scaled.json"))
        {
            var document=new GeneratedGalaxyMapData(codec.DecodeScaledGalaxy(File.ReadAllBytes(sample)));
            var before=codec.EncodeScaledGalaxy(document.Scaled!);
            var report=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(sample.Replace(".scaled.json",".report.json")));
            string origin=report["OriginNodeId"]!.Value<string>()!;
            var ids=new[]{origin}.Concat(report["Route"]!["Steps"]!.Values<string>().Select(s=>s!)).ToArray();
            var regions=document.Regions.ToDictionary(r=>r.NodeId);
            var path=GeneratedGalaxyRoutePath.Build(regions,origin,ids.Skip(1).ToArray());
            AssertCorridor(regions,ids,path);
            var reverse=GeneratedGalaxyRoutePath.Build(regions,ids[^1],ids.Reverse().Skip(1).ToArray());
            Assert.Equal(path.Select(p=>(p.X2,p.Y2)).Reverse(),reverse.Select(p=>(p.X2,p.Y2)));
            Assert.Equal(before,codec.EncodeScaledGalaxy(document.Scaled!));
        }
    }

    [Fact]
    public void OneUnitNearCornerBoundaryKeepsItsHalfUnitPortalAndIsReversible()
    {
        var a=Region("a",2,2,(0,0),(10,0),(10,1),(0,10));
        var b=Region("b",18,2,(10,0),(20,0),(20,10),(10,1));
        var regions=new Dictionary<string,GeneratedGalaxyRegionDto>{{"a",a},{"b",b}};
        var path=GeneratedGalaxyRoutePath.Build(regions,"a",new[]{"b"});
        Assert.Equal(20,path[1].X2);Assert.Equal(1,path[1].Y2);Assert.Equal(.5,path[1].Y);
        AssertCrossing(regions,"a","b");AssertCrossing(regions,"b","a");
        var reverse=GeneratedGalaxyRoutePath.Build(regions,"b",new[]{"a"});
        Assert.Equal(path.Select(p=>(p.X2,p.Y2)).Reverse(),reverse.Select(p=>(p.X2,p.Y2)));
        Assert.Single(GeneratedGalaxyRoutePath.Build(regions,"a",Array.Empty<string>()));
    }

    [Fact]
    public void BacktrackingAndZeroLengthLegsKeepEveryExplicitAnchorAndFailuresPaintNothing()
    {
        var regions=new Dictionary<string,GeneratedGalaxyRegionDto>{
            {"a",Region("a",2,5,(0,0),(10,0),(10,10),(0,10))},
            {"b",Region("b",18,5,(10,0),(20,0),(20,10),(10,10))}};
        GalaxyReviewLegSnapshot Leg(int index,string from,string to,params string[] steps)=>new(index,from,
            new GalaxyReviewTargetSnapshot(to,GalaxyReviewTargetIntent.RegionTransit,null),
            new StrategicRouteSnapshot(RouteErrorCode.None,steps,Array.Empty<NavigationCostSnapshot>(),steps.Length),0);
        var legs=new[]{Leg(0,"a","b","b"),Leg(1,"b","b"),Leg(2,"b","a","a")};
        var plan=new GalaxyReviewRoutePlanSnapshot("policy","hash","a",GalaxyReviewPlanError.None,null,legs,new[]{"b","a"},2,0);
        var paths=GeneratedGalaxyRoutePath.BuildPlan(regions,plan);Assert.Equal(3,paths.Count);
        Assert.Equal(36,paths[0][^1].X2);Assert.Equal(36,Assert.Single(paths[1]).X2);Assert.Equal(36,paths[2][0].X2);
        Assert.Equal(4,paths[2][^1].X2);
        AssertCorridor(regions,new[]{"a","b"},paths[0]);AssertCorridor(regions,new[]{"b","a"},paths[2]);
        var failed=new GalaxyReviewRoutePlanSnapshot("policy","hash","a",GalaxyReviewPlanError.UnreachableLeg,1,
            Array.Empty<GalaxyReviewLegSnapshot>(),Array.Empty<string>(),null,null);
        Assert.Empty(GeneratedGalaxyRoutePath.BuildPlan(regions,failed));
    }

    [Fact]
    public void CornerOnlyContactDoesNotInventARouteSegment()
    {
        var regions=new Dictionary<string,GeneratedGalaxyRegionDto>{
            {"a",Region("a",1,1,(0,0),(2,0),(2,2),(0,2))},
            {"b",Region("b",3,3,(2,2),(4,2),(4,4),(2,4))}};
        Assert.Throws<ArgumentException>(()=>GeneratedGalaxyRoutePath.Build(regions,"a",new[]{"b"}));
    }

    private static void AssertCrossing(IReadOnlyDictionary<string,GeneratedGalaxyRegionDto> regions,string from,string to)
    {
        var path=GeneratedGalaxyRoutePath.Build(regions,from,new[]{to});Assert.Equal(3,path.Count);
        AssertContained(regions[from],path[0]);AssertContained(regions[from],path[1]);
        AssertContained(regions[to],path[1]);AssertContained(regions[to],path[2]);
        // Convexity + exact endpoint half-plane containment proves the entire segment is inside.
        // The portal is strictly inside a positive shared edge, so no other region is crossed.
        var portal=path[1];
        foreach(var region in new[]{regions[from],regions[to]})
        {
            Assert.Contains(Enumerable.Range(0,region.Boundary.Count),i=>{
                var a=region.Boundary[i];var b=region.Boundary[(i+1)%region.Boundary.Count];
                return (a.X!=b.X||a.Y!=b.Y)&&portal.X2==(long)a.X+b.X&&portal.Y2==(long)a.Y+b.Y;
            });
        }
        // Independent interior samples exclude every unrelated region, including near-corner cells.
        for(int half=0;half<2;half++)foreach(var fraction in new[]{.25,.5,.75})
        {
            double x=path[half].X+(path[half+1].X-path[half].X)*fraction;
            double y=path[half].Y+(path[half+1].Y-path[half].Y)*fraction;
            var owners=regions.Values.Where(r=>StrictlyInside(r,x,y)).Select(r=>r.NodeId).ToArray();
            Assert.Equal(new[]{half==0?from:to},owners);
        }
    }

    private static void AssertContained(GeneratedGalaxyRegionDto region,GeneratedGalaxyRoutePoint p)
    {
        for(int i=0;i<region.Boundary.Count;i++)
        {
            var a=region.Boundary[i];var b=region.Boundary[(i+1)%region.Boundary.Count];
            var cross=(BigInteger)(b.X-a.X)*(p.Y2-2L*a.Y)-(BigInteger)(b.Y-a.Y)*(p.X2-2L*a.X);
            Assert.True(cross>=0,$"Point outside {region.NodeId}");
        }
    }
    private static void AssertCorridor(IReadOnlyDictionary<string,GeneratedGalaxyRegionDto> regions,
        IReadOnlyList<string> ids,IReadOnlyList<GeneratedGalaxyRoutePoint> path)
    {
        Assert.Equal(ids.Count+1,path.Count);
        for(int segment=0;segment<ids.Count;segment++)
        {
            var region=regions[ids[segment]];
            AssertContained(region,path[segment]);AssertContained(region,path[segment+1]);
            foreach(var fraction in new[]{.25,.5,.75})
            {
                double x=path[segment].X+(path[segment+1].X-path[segment].X)*fraction;
                double y=path[segment].Y+(path[segment+1].Y-path[segment].Y)*fraction;
                Assert.Equal(new[]{ids[segment]},regions.Values.Where(r=>StrictlyInside(r,x,y)).Select(r=>r.NodeId));
            }
        }
    }
    private static bool StrictlyInside(GeneratedGalaxyRegionDto r,double x,double y)
        => Enumerable.Range(0,r.Boundary.Count).All(i=>{
            var a=r.Boundary[i];var b=r.Boundary[(i+1)%r.Boundary.Count];
            return (b.X-a.X)*(y-a.Y)-(b.Y-a.Y)*(x-a.X)>0;
        });
    private static GeneratedGalaxyRegionDto Region(string id,int x,int y,params (int X,int Y)[] points)
        => new(){NodeId=id,Center=new(){X=x,Y=y},Boundary=points.Select(p=>new GeneratedGalaxyPointDto{X=p.X,Y=p.Y}).ToList()};
}
