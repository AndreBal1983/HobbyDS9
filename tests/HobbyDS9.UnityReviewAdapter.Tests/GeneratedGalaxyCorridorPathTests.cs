using System.Numerics;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class GeneratedGalaxyCorridorPathTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper output;
    public GeneratedGalaxyCorridorPathTests(Xunit.Abstractions.ITestOutputHelper output){this.output=output;}
    private static GeneratedGalaxyRegionDto Region(string id,int x,int y,params (int X,int Y)[] boundary)=>new(){NodeId=id,
        Center=new(){X=x,Y=y},Boundary=boundary.Select(p=>new GeneratedGalaxyPointDto{X=p.X,Y=p.Y}).ToList()};
    private static Dictionary<string,GeneratedGalaxyRegionDto> Cells(params GeneratedGalaxyRegionDto[] regions)=>regions.ToDictionary(r=>r.NodeId);
    [Fact] public void StraightCorridorBecomesOneStraightSegmentAndForcedBudgetUsesCertifiedMidpoints()
    {
        var regions=Cells(Region("a",5,5,(0,0),(10,0),(10,10),(0,10)),Region("b",15,5,(10,0),(20,0),(20,10),(10,10)),Region("c",25,5,(20,0),(30,0),(30,10),(20,10)));
        var path=GeneratedGalaxyCorridorPath.Build(regions,"a",new[]{"b","c"});Assert.True(path.Certified,path.Reason);Assert.False(path.UsedFallback,path.Reason);Assert.Equal(2,path.Points.Count);
        Prove(regions,new[]{"a","b","c"},path.Points);
        var fallback=GeneratedGalaxyCorridorPath.Build(regions,"a",new[]{"b","c"},0);Assert.True(fallback.Certified,fallback.Reason);Assert.True(fallback.UsedFallback);Assert.Contains("budget",fallback.Reason);
        Prove(regions,new[]{"a","b","c"},fallback.Points);
    }
    [Fact] public void BentChannelAndReversalRetainStrictOrderedCrossings()
    {
        var regions=Cells(Region("a",5,5,(0,0),(10,0),(10,10),(0,10)),Region("b",15,5,(10,0),(20,0),(20,10),(10,10)),Region("c",15,15,(10,10),(20,10),(20,20),(10,20)));
        var path=GeneratedGalaxyCorridorPath.Build(regions,"a",new[]{"b","c"});Assert.True(path.Certified,path.Reason);Assert.False(path.UsedFallback,path.Reason);
        Assert.True(path.Points.Count>2);Prove(regions,new[]{"a","b","c"},path.Points);
        var reverse=GeneratedGalaxyCorridorPath.Build(regions,"c",new[]{"b","a"});Assert.True(reverse.Certified,reverse.Reason);Prove(regions,new[]{"c","b","a"},reverse.Points);
        Assert.Equal(Length(path.Points),Length(reverse.Points),10);
    }
    [Fact] public void OneUnitCrookedGateNearFiveMillionCoordinatesRetainsInteriorClearance()
    {
        const int n=4999900;
        var regions=Cells(Region("a",n+2,n+2,(n,n),(n+10,n),(n+10,n+1),(n,n+10)),
            Region("b",n+18,n+2,(n+10,n),(n+20,n),(n+20,n+10),(n+10,n+1)));
        var path=GeneratedGalaxyCorridorPath.Build(regions,"a",new[]{"b"});Assert.True(path.Certified,path.Reason);Prove(regions,new[]{"a","b"},path.Points);
        Assert.True(path.Points.All(p=>p.X>=n&&p.X<=n+20));
    }
    [Fact] public void RepeatedRegionWithZeroInteriorProgressNeverPaintsAFakeTransitAndZeroHopRemainsAnchor()
    {
        var regions=Cells(Region("a",5,5,(0,0),(10,0),(10,10),(0,10)),Region("b",15,5,(10,0),(20,0),(20,10),(10,10)));
        var path=GeneratedGalaxyCorridorPath.Build(regions,"a",new[]{"b","a"});Assert.False(path.Certified);Assert.Empty(path.Points);Assert.Contains("non-simple",path.Reason);
        var zero=GeneratedGalaxyCorridorPath.Build(regions,"a",Array.Empty<string>());Assert.True(zero.Certified);Assert.Single(zero.Points);
        var outbound=GeneratedGalaxyCorridorPath.Build(regions,"a",new[]{"b"});var inbound=GeneratedGalaxyCorridorPath.Build(regions,"b",new[]{"a"});
        Assert.Equal(outbound.Points[^1],inbound.Points[0]);Assert.Equal(15,outbound.Points[^1].X);
    }
    [Fact] public void SelfStepCannotMatchItsOwnPolygonEdgeOrInventOneCrossing()
    {
        var regions=Cells(Region("a",5,5,(0,0),(10,0),(10,10),(0,10)));
        var invalid=GeneratedGalaxyCorridorPath.Build(regions,"a",new[]{"a"});Assert.False(invalid.Certified);Assert.Empty(invalid.Points);Assert.Empty(invalid.RawMidpoints);Assert.Contains("self-step",invalid.Reason);
    }
    private static GalaxyReviewRoutePlanSnapshot SyntheticPlan(IEnumerable<(string from,string to,string[] steps)> routes)
    {
        var legs=routes.Select((r,i)=>new GalaxyReviewLegSnapshot(i,r.from,new GalaxyReviewTargetSnapshot(r.to,GalaxyReviewTargetIntent.RegionTransit,null),
            new StrategicRouteSnapshot(RouteErrorCode.None,r.steps,Array.Empty<NavigationCostSnapshot>(),r.steps.Length),0)).ToArray();
        return new GalaxyReviewRoutePlanSnapshot(GalaxyReviewRoutePolicy.MinimumCrossingsDirectV1,new string('a',64),legs[0].OriginNodeId,GalaxyReviewPlanError.None,null,legs,legs.SelectMany(l=>l.Route.Steps),legs.Sum(l=>l.Route.TotalCost),0);
    }
    [Fact] public void AllSixtyFourLegsShareOneBudgetAndExhaustionRetainsLegDiagnostics()
    {
        var regions=Cells(Region("a",5,5,(0,0),(10,0),(10,10),(0,10)),Region("b",15,5,(10,0),(20,0),(20,10),(10,10)));
        var plan=SyntheticPlan(Enumerable.Range(0,64).Select(i=>(i%2==0?"a":"b",i%2==0?"b":"a",new[]{i%2==0?"b":"a"})));
        var full=GeneratedGalaxyCorridorPath.BuildPlan(regions,plan);Assert.False(full.BudgetExhausted);Assert.Equal(64,full.Legs.Count);Assert.Equal(0,full.HiddenCount);
        var bounded=GeneratedGalaxyCorridorPath.BuildPlan(regions,plan,100);Assert.True(bounded.BudgetExhausted);Assert.Equal(100,bounded.Operations);Assert.Equal(64,bounded.Legs.Count);Assert.True(bounded.HiddenCount>0);
        Assert.Equal(bounded.Operations,bounded.Legs.Sum(p=>p.Operations));Assert.All(bounded.Legs.Where(p=>!p.Certified),p=>Assert.Contains("plan operation budget",p.Reason));
        Assert.Equal(64,plan.TotalCost);Assert.Equal(64,plan.Legs.Count);
    }
    [Fact] public void SharedBudgetAlsoChargesCertifiedFallbackWorkAndCanStopItSafely()
    {
        var regions=Cells(Region("a",1,3,(0,0),(10,0),(-5,10)),Region("b",-3,0,(0,0),(-5,10),(-5,-10)),Region("c",1,-3,(0,0),(-5,-10),(10,0)));
        var plan=SyntheticPlan(new[]{("a","a",new[]{"b","c","a"})});var full=GeneratedGalaxyCorridorPath.BuildPlan(regions,plan);
        Assert.Equal(1,full.FallbackCount);Assert.Equal(0,full.HiddenCount);Prove(regions,new[]{"a","b","c","a"},full.Legs[0].Points);
        var limited=GeneratedGalaxyCorridorPath.BuildPlan(regions,plan,full.Operations-1);Assert.True(limited.BudgetExhausted);Assert.Equal(1,limited.HiddenCount);Assert.Empty(limited.Legs[0].Points);
        Assert.Contains("during fallback",limited.Legs[0].Reason);Assert.Equal(full.Operations-1,limited.Operations);
    }
    [Fact] public void ActualTwentyTwoCrossingDoglegFixtureShortensWithoutChangingMapStepsOrCost()
    {
        var codec=new ReviewNewtonsoftCodec();var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"galaxy-samples/scaled/seed-42-systems-250.scaled.json"));
        var map=codec.DecodeScaledGalaxy(bytes);var before=codec.EncodeScaledGalaxy(map);
        var scenario=codec.DecodeScenario(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1/scenario.json")));
        var review=ScaledGalaxyApplication.Open(map,scenario.Definitions.Civilizations!);
        var fixture=JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"galaxy-samples/route-intent/examples.json")));
        var query=fixture["Comparison"]!["Query"]!.ToObject<GalaxyReviewRouteQueryDto>()!;var plan=review.FindRoutePlan(query);
        Assert.True(plan.Success);Assert.Equal(22,plan.TotalCost);Assert.Equal(fixture["Comparison"]!["Result"]!["Steps"]!.Values<string>(),plan.Steps);
        var regions=map.Layout.Regions.ToDictionary(r=>r.NodeId);var leg=Assert.Single(plan.Legs);var ids=new[]{leg.OriginNodeId}.Concat(leg.Route.Steps).ToArray();
        var raw=GeneratedGalaxyRoutePath.Build(regions,leg.OriginNodeId,leg.Route.Steps).Select(p=>new GeneratedGalaxyDisplayPoint(p.X2*512,p.Y2*512)).ToArray();
        var path=GeneratedGalaxyCorridorPath.Build(regions,leg.OriginNodeId,leg.Route.Steps);Assert.True(path.Certified,path.Reason);Assert.False(path.UsedFallback,path.Reason);
        Prove(regions,ids,path.Points);Assert.True(Length(path.Points)<Length(raw));Assert.True(path.Points.Count<raw.Length);
        output.WriteLine("Same map {0}; cost22; midpoint length {1:F6}/vertices{2}; certified inset-funnel length {3:F6}/vertices{4}; fallback {5}; operations {6}",map.ContentHash,Length(raw),raw.Length,Length(path.Points),path.Points.Count,path.UsedFallback,path.Operations);
        Assert.Equal(before,codec.EncodeScaledGalaxy(map));Assert.Equal(plan.Steps,review.FindRoutePlan(query).Steps);Assert.Equal(22,review.FindRoutePlan(query).TotalCost);
    }
    [Fact] public void CornerTangencyRepeatedCrossingsAndBoundaryOnlyCellHaveExplicitSafeFailure()
    {
        var regions=Cells(Region("a",5,5,(0,0),(10,0),(10,10),(0,10)),Region("b",15,5,(10,0),(20,0),(20,10),(10,10)));
        GeneratedGalaxyDisplayPoint P(int x,int y)=>new(1024L*x,1024L*y);
        Assert.False(GeneratedGalaxyCorridorPath.CertifyCandidate(regions,"a",new[]{"b"},new[]{P(5,5),P(10,10),P(15,5)},out var corner));Assert.Contains("corner",corner);
        Assert.False(GeneratedGalaxyCorridorPath.CertifyCandidate(regions,"a",new[]{"b"},new[]{P(5,5),P(12,5),P(8,5),P(15,5)},out var repeat));Assert.Contains("repeated",repeat);
        Assert.False(GeneratedGalaxyCorridorPath.CertifyCandidate(regions,"a",new[]{"b"},new[]{P(6,5),P(15,5)},out var endpoint));Assert.Contains("endpoint",endpoint);
        var collinear=Cells(Region("a",5,5,(0,0),(10,0),(10,10),(0,10)),
            Region("b",10,15,(0,10),(10,10),(20,10),(20,20),(0,20)),Region("c",15,5,(10,0),(20,0),(20,10),(10,10)));
        var hidden=GeneratedGalaxyCorridorPath.Build(collinear,"a",new[]{"b","c"});Assert.False(hidden.Certified);Assert.Empty(hidden.Points);Assert.Contains("no certified route line",hidden.Reason);
    }
    [Fact] public void EverySuppliedSuccessfulOrderedFixtureKeepsEachAnchorIncludingZeroAndReturnLegs()
    {
        var codec=new ReviewNewtonsoftCodec();var map=codec.DecodeScaledGalaxy(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"galaxy-samples/scaled/seed-42-systems-250.scaled.json")));
        var scenario=codec.DecodeScenario(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1/scenario.json")));
        var review=ScaledGalaxyApplication.Open(map,scenario.Definitions.Civilizations!);var regions=map.Layout.Regions.ToDictionary(r=>r.NodeId);
        var fixture=JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"galaxy-samples/route-intent/examples.json")));int legs=0;
        foreach(var item in fixture["Cases"]!)
        {
            var query=item["Query"]!.ToObject<GalaxyReviewRouteQueryDto>()!;var plan=review.FindRoutePlan(query);
            Assert.Equal(item["Result"]!["Success"]!.Value<bool>(),plan.Success);if(!plan.Success){Assert.Empty(plan.Legs);continue;}
            foreach(var leg in plan.Legs)
            {
                var path=GeneratedGalaxyCorridorPath.Build(regions,leg.OriginNodeId,leg.Route.Steps);Assert.True(path.Certified,path.Reason);Assert.False(path.UsedFallback,path.Reason);
                Prove(regions,new[]{leg.OriginNodeId}.Concat(leg.Route.Steps).ToArray(),path.Points);
                Assert.Equal(1024L*regions[leg.Target.NodeId].Center.X,path.Points[^1].X1024);
                output.WriteLine("Fixture{0} leg{1} cost{2} vertices{3} fallback{4} certified{5}",item["Name"],leg.Index,leg.Route.TotalCost,path.Points.Count,path.UsedFallback,path.Certified);legs++;
            }
        }
        Assert.Equal(8,legs);
    }
    [Fact] public void OriginalSeed42FiftySystemEightCrossingCorridorRemainsTheSameAcceptedBaseline()
    {
        var codec=new ReviewNewtonsoftCodec();var scenario=codec.DecodeScenario(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1/scenario.json")));
        var workspace=new GeneratedGalaxyWorkspace(scenario.Definitions.Civilizations!,new GeneratedGalaxyMapStore(Path.GetTempPath())){Seed="42",Size=50};Assert.True(workspace.Generate());
        var map=workspace.Document!;var before=workspace.ExportBytes();Assert.Equal("a3d42067ca95f8453f465faf1381dd77f151eb6d70a0c4cca3cb8486755fb3d3",map.ContentHash);
        var ids=new[]{39,52,26,25,5,13,84,21,44}.Select(i=>$"nav.generated.{i:000}").ToArray();var route=workspace.FindRoute(ids[0],ids[^1]);
        Assert.Equal(8,route.TotalCost);Assert.Equal(ids.Skip(1),route.Steps);var regions=map.Regions.ToDictionary(r=>r.NodeId);
        var path=GeneratedGalaxyCorridorPath.Build(regions,ids[0],route.Steps);Assert.True(path.Certified,path.Reason);Assert.False(path.UsedFallback,path.Reason);Prove(regions,ids,path.Points);
        output.WriteLine("Oldseed42/50 map{0} unchanged8crossings vertices{1} operations{2}",map.ContentHash,path.Points.Count,path.Operations);Assert.Equal(before,workspace.ExportBytes());
    }
    [Fact] public void UChannelRetainsAllSixRegionsRatherThanJumpingAcrossTheGap()
    {
        var cells=new List<GeneratedGalaxyRegionDto>();
        foreach(var item in new[]{("a",0,0),("b",10,0),("c",20,0),("d",20,10),("e",10,10),("f",0,10)})
            cells.Add(Region(item.Item1,item.Item2+5,item.Item3+5,(item.Item2,item.Item3),(item.Item2+10,item.Item3),(item.Item2+10,item.Item3+10),(item.Item2,item.Item3+10)));
        var regions=Cells(cells.ToArray());var ids=cells.Select(c=>c.NodeId).ToArray();
        var path=GeneratedGalaxyCorridorPath.Build(regions,"a",ids.Skip(1).ToArray());Assert.True(path.Certified,path.Reason);Prove(regions,ids,path.Points);
    }
    [Theory][InlineData("42",250)][InlineData("43",250)][InlineData("42",1000)]
    public void BoundedTwentyPairNaturalProbeReportsActualFallbackRatesAndPreservesEachRoute(string seed,int size)
    {
        var codec=new ReviewNewtonsoftCodec();var map=codec.DecodeNaturalGalaxy(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"galaxy-samples/natural/seed-"+seed+"-systems-"+size+".natural.json")));
        var before=codec.EncodeNaturalGalaxy(map);var scenario=codec.DecodeScenario(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1/scenario.json")));
        var review=NaturalGalaxyApplication.Open(map,scenario.Definitions.Civilizations!);var regions=map.Layout.Regions.ToDictionary(r=>r.NodeId);
        var systems=map.Layout.Systems.OrderBy(s=>s.NodeId,StringComparer.Ordinal).ToArray();int fallbacks=0,hidden=0;
        for(int sample=0;sample<20;sample++)
        {
            var query=new GalaxyReviewRouteQueryDto{MapContentHash=map.ContentHash,OriginNodeId=systems[sample*37%size].NodeId,
                Targets=new(){new(){NodeId=systems[(sample*91+17)%size].NodeId}},Filter=new(){PermittedNodeIds=regions.Keys.ToList(),PermittedConnectionIds=map.Layout.Connections.Select(e=>e.ConnectionId).ToList()}};
            var plan=review.FindRoutePlan(query);Assert.True(plan.Success);var leg=Assert.Single(plan.Legs);
            var raw=GeneratedGalaxyRoutePath.Build(regions,leg.OriginNodeId,leg.Route.Steps).Select(p=>new GeneratedGalaxyDisplayPoint(p.X2*512,p.Y2*512)).ToArray();
            var path=GeneratedGalaxyCorridorPath.Build(regions,leg.OriginNodeId,leg.Route.Steps);if(path.UsedFallback)fallbacks++;if(!path.Certified)hidden++;
            if(path.Certified)Prove(regions,new[]{leg.OriginNodeId}.Concat(leg.Route.Steps).ToArray(),path.Points);
            output.WriteLine("{0}/{1} sample{2} cost{3} steps{4} oldLength{5:F3} newLength{6:F3} vertices{7} fallback={8} certified={9} reason={10}",seed,size,sample,plan.TotalCost,string.Join(",",plan.Steps),Length(raw),path.Points.Count==0?0:Length(path.Points),path.Points.Count,path.UsedFallback,path.Certified,path.Reason);
            Assert.Equal(plan.Steps,review.FindRoutePlan(query).Steps);Assert.Equal(plan.TotalCost,review.FindRoutePlan(query).TotalCost);
        }
        output.WriteLine("Map{0} bounded20pairs fallback{1}/20 hidden{2}/20",map.ContentHash,fallbacks,hidden);
        Assert.Equal(0,hidden);Assert.Equal(before,codec.EncodeNaturalGalaxy(map));
    }
    private static double Length(IReadOnlyList<GeneratedGalaxyDisplayPoint> path)=>Enumerable.Range(1,path.Count-1).Sum(i=>Math.Sqrt(Math.Pow(path[i].X-path[i-1].X,2)+Math.Pow(path[i].Y-path[i-1].Y,2)));
    private readonly struct F : IComparable<F>
    {
        public readonly BigInteger N,D;
        public F(BigInteger n,BigInteger d){if(d.Sign<0){n=-n;d=-d;}N=n;D=d;}
        public int CompareTo(F q)=>(N*q.D).CompareTo(q.N*D);
    }
    // Independent convex clipping oracle: partitions EACH drawn segment against every intended cell,
    // with exact rational half-plane intervals. It never calls the funnel or production certificate.
    private static void Prove(IReadOnlyDictionary<string,GeneratedGalaxyRegionDto> regions,string[] ids,IReadOnlyList<GeneratedGalaxyDisplayPoint> points)
    {
        if(points.Count==1){Assert.Single(ids);Assert.Equal(1024L*regions[ids[0]].Center.X,points[0].X1024);Assert.Equal(1024L*regions[ids[0]].Center.Y,points[0].Y1024);return;}
        var owners=new List<string>();
        for(int segment=1;segment<points.Count;segment++)
        {
            var p=points[segment-1];var q=points[segment];if(p.Equals(q))continue;
            var spans=new List<(F lo,F hi,string id)>();
            foreach(var id in ids.Distinct(StringComparer.Ordinal))
            {
                var cell=regions[id];var lo=new F(0,1);var hi=new F(1,1);bool possible=true;
                for(int edge=0;edge<cell.Boundary.Count;edge++)
                {
                    var a=cell.Boundary[edge];var b=cell.Boundary[(edge+1)%cell.Boundary.Count];
                    BigInteger dx=(long)b.X-a.X,dy=(long)b.Y-a.Y;
                    var start=dx*(p.Y1024-1024L*a.Y)-dy*(p.X1024-1024L*a.X);
                    var delta=dx*(q.Y1024-p.Y1024)-dy*(q.X1024-p.X1024);
                    if(delta.IsZero){if(start.Sign<=0){possible=false;break;}}
                    else{var crossing=new F(-start,delta);if(delta.Sign>0){if(crossing.CompareTo(lo)>0)lo=crossing;}else if(crossing.CompareTo(hi)<0)hi=crossing;}
                }
                if(possible&&lo.CompareTo(hi)<0)spans.Add((lo,hi,id));
            }
            spans.Sort((a,b)=>a.lo.CompareTo(b.lo));Assert.NotEmpty(spans);
            Assert.Equal(0,spans[0].lo.CompareTo(new F(0,1)));Assert.Equal(0,spans[^1].hi.CompareTo(new F(1,1)));
            for(int i=0;i<spans.Count;i++)
            {
                if(i>0)Assert.Equal(0,spans[i-1].hi.CompareTo(spans[i].lo));
                if(owners.Count==0||owners[^1]!=spans[i].id)
                {
                    if(owners.Count>0)AssertSharedInterior(regions[owners[^1]],regions[spans[i].id],p,q,spans[i].lo);
                    owners.Add(spans[i].id);
                }
            }
        }
        Assert.Equal(ids,owners);
    }
    private static void AssertSharedInterior(GeneratedGalaxyRegionDto from,GeneratedGalaxyRegionDto to,
        GeneratedGalaxyDisplayPoint p,GeneratedGalaxyDisplayPoint q,F t)
    {
        var x=(BigInteger)p.X1024*t.D+(q.X1024-p.X1024)*t.N;var y=(BigInteger)p.Y1024*t.D+(q.Y1024-p.Y1024)*t.N;
        bool proved=false;
        for(int i=0;i<from.Boundary.Count;i++)
        {
            var a=from.Boundary[i];var b=from.Boundary[(i+1)%from.Boundary.Count];
            bool shared=Enumerable.Range(0,to.Boundary.Count).Any(j=>{
                var c=to.Boundary[j];var d=to.Boundary[(j+1)%to.Boundary.Count];return a.X==c.X&&a.Y==c.Y&&b.X==d.X&&b.Y==d.Y||a.X==d.X&&a.Y==d.Y&&b.X==c.X&&b.Y==c.Y;});
            if(!shared)continue;
            BigInteger dx=1024L*(b.X-a.X),dy=1024L*(b.Y-a.Y),px=x-1024L*a.X*t.D,py=y-1024L*a.Y*t.D;
            var dot=px*dx+py*dy;var length2=dx*dx+dy*dy;
            if(dx*py-dy*px==0&&dot>0&&dot<length2*t.D)proved=true;
        }
        Assert.True(proved,$"No exact positive interior crossing from{from.NodeId} to{to.NodeId}");
    }
}
