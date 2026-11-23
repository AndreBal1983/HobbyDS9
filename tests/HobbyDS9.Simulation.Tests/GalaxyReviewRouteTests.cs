using System.Reflection;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class GalaxyReviewRouteTests
{
    const string Hash="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    static GeneratedGalaxyRegionDto Node(string id,int x,int y,string? system=null)=>new(){NodeId=id,Center=new(){X=x,Y=y},SystemId=system};
    static NavigationConnectionDto Edge(string a,string b)=>new(){ConnectionId="edge."+a+"."+b,EndpointA=a,EndpointB=b,BaseTraversalCost=1};
    static GalaxyReviewRouteQueryDto Request(List<GeneratedGalaxyRegionDto> nodes,List<NavigationConnectionDto> edges,string origin,params string[] targets)=>new(){
        MapContentHash=Hash,OriginNodeId=origin,Targets=targets.Select(n=>new GalaxyReviewTargetDto{NodeId=n}).ToList(),
        Filter=new(){PermittedNodeIds=nodes.Select(n=>n.NodeId).ToList(),PermittedConnectionIds=edges.Select(e=>e.ConnectionId).ToList()}};
    static GalaxyReviewRoutePlanSnapshot Query(List<GeneratedGalaxyRegionDto> nodes,List<NavigationConnectionDto> edges,GalaxyReviewRouteQueryDto request)
        =>(GalaxyReviewRoutePlanSnapshot)typeof(ScaledGalaxyReviewSession).Assembly.GetType("HobbyDS9.Simulation.GalaxyReviewRouteSearch")!
            .GetMethod("Query",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[]{Hash,nodes,edges,request})!;
    static string Json<T>(T value)=>JsonSerializer.Serialize(value);

    [Fact]
    public void CrossingCountWinsEvenWhenTheLongerRouteSavesOneCrossing()
    {
        var nodes=new List<GeneratedGalaxyRegionDto>{Node("o",0,0),Node("a",30,0),Node("b",70,0),Node("far",0,100000),Node("t",100,0)};
        var edges=new List<NavigationConnectionDto>{Edge("o","a"),Edge("a","b"),Edge("b","t"),Edge("o","far"),Edge("far","t")};
        var plan=Query(nodes,edges,Request(nodes,edges,"o","t"));
        Assert.True(plan.Success);Assert.Equal(2,plan.TotalCost);Assert.Equal(new[]{"far","t"},plan.Steps);Assert.True(plan.GeographicLengthUnits>100000);
    }

    [Fact]
    public void EqualCrossingDirectnessPrecedesOrdinalNodeIds()
    {
        var nodes=new List<GeneratedGalaxyRegionDto>{Node("o",0,0),Node("a",0,1000),Node("z",500,0),Node("t",1000,0)};
        var edges=new List<NavigationConnectionDto>{Edge("o","a"),Edge("a","t"),Edge("o","z"),Edge("z","t")};
        var plan=Query(nodes,edges,Request(nodes,edges,"o","t"));
        Assert.Equal(new[]{"z","t"},plan.Steps);Assert.Equal(2,plan.TotalCost);Assert.Equal(1000,plan.GeographicLengthUnits);
        Assert.Equal(GalaxyReviewRoutePolicy.SecondaryMetric,plan.SecondaryMetric);
    }

    [Fact]
    public void ExactTieUsesWholePathOrdinalOrderRatherThanOnlyLastPredecessor()
    {
        var nodes=new List<GeneratedGalaxyRegionDto>{Node("o",100,100),Node("a",200,100),Node("b",100,200),Node("z",300,100),Node("c",100,300),Node("t",300,300)};
        var edges=new List<NavigationConnectionDto>{Edge("o","a"),Edge("a","z"),Edge("z","t"),Edge("o","b"),Edge("b","c"),Edge("c","t")};
        var plan=Query(nodes,edges,Request(nodes,edges,"o","t"));Assert.Equal(new[]{"a","z","t"},plan.Steps);Assert.Equal(400,plan.GeographicLengthUnits);
        nodes.Reverse();edges.Reverse();var request=Request(nodes,edges,"o","t");request.Filter.PermittedNodeIds.Reverse();request.Filter.PermittedConnectionIds.Reverse();
        Assert.Equal(Json(plan),Json(Query(nodes,edges,request)));
    }

    [Fact]
    public void OrderedLegsRetainTransitAndPinnedSystemIntentsWithoutExecutingActions()
    {
        var nodes=new List<GeneratedGalaxyRegionDto>{Node("o",0,0),Node("a",3,4,"system.a"),Node("b",6,8),Node("t",9,12,"system.t")};
        var edges=new List<NavigationConnectionDto>{Edge("o","a"),Edge("a","b"),Edge("b","t")};
        var request=Request(nodes,edges,"o","b","b","a","t");request.Targets[2].Intent=GalaxyReviewTargetIntent.SystemLocation;request.Targets[2].SystemId="system.a";
        string before=Json(request);var plan=Query(nodes,edges,request);
        Assert.Equal(before,Json(request));Assert.Equal(Hash,plan.MapContentHash);Assert.Equal(4,plan.Legs.Count);
        Assert.Equal(new[]{"a","b","a","b","t"},plan.Steps);Assert.Equal(5,plan.TotalCost);Assert.Equal(25,plan.GeographicLengthUnits);
        Assert.Empty(plan.Legs[1].Route.Steps);Assert.Equal(0,plan.Legs[1].Route.TotalCost);Assert.Equal(GalaxyReviewTargetIntent.SystemLocation,plan.Legs[2].Target.Intent);
        request.Targets[2].NodeId="other";request.Filter.PermittedNodeIds.Clear();Assert.Equal("a",plan.Legs[2].Target.NodeId);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public void EveryLegUsesThePermittedSubgraphAndFailureReturnsNoPrefix(bool blockNode)
    {
        var nodes=new List<GeneratedGalaxyRegionDto>{Node("o",0,0),Node("a",10,0),Node("b",20,0),Node("t",30,0)};
        var edges=new List<NavigationConnectionDto>{Edge("o","a"),Edge("a","b"),Edge("b","t")};var request=Request(nodes,edges,"o","a","t");
        if(blockNode)request.Filter.PermittedNodeIds.Remove("b");else request.Filter.PermittedConnectionIds.Remove("edge.a.b");
        var plan=Query(nodes,edges,request);Assert.Equal(GalaxyReviewPlanError.UnreachableLeg,plan.ErrorCode);Assert.Equal(1,plan.FailedLegIndex);
        Assert.Empty(plan.Legs);Assert.Empty(plan.Steps);Assert.Null(plan.TotalCost);Assert.Null(plan.GeographicLengthUnits);
    }

    [Fact]
    public void HiddenShortcutsCannotParticipateAndAccessLossInvalidatesWithoutMutation()
    {
        var nodes=new List<GeneratedGalaxyRegionDto>{Node("o",0,0),Node("secret",10,0),Node("a",0,20),Node("b",10,20),Node("t",20,0)};
        var edges=new List<NavigationConnectionDto>{Edge("o","secret"),Edge("secret","t"),Edge("o","a"),Edge("a","b"),Edge("b","t")};
        var request=Request(nodes,edges,"o","t");request.Filter.PermittedNodeIds.Remove("secret");
        var original=Query(nodes,edges,request);Assert.Equal(3,original.TotalCost);Assert.DoesNotContain("secret",original.Steps);
        request.Filter.PermittedConnectionIds.Remove("edge.a.b");var rejected=Query(nodes,edges,request);
        Assert.False(rejected.Success);Assert.Empty(rejected.Legs);Assert.Equal(3,original.TotalCost);
        request.Targets[0].NodeId="secret";Assert.Equal(GalaxyReviewPlanError.UnavailableTarget,Query(nodes,edges,request).ErrorCode);
        request.Targets[0].NodeId="foreign";Assert.Equal(GalaxyReviewPlanError.UnavailableTarget,Query(nodes,edges,request).ErrorCode);
    }

    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)]
    public void InvalidOrStaleRequestsFailClosed(int mode)
    {
        var nodes=new List<GeneratedGalaxyRegionDto>{Node("o",0,0),Node("t",3,4)};var edges=new List<NavigationConnectionDto>{Edge("o","t")};var q=Request(nodes,edges,"o","t");
        switch(mode){case 0:q.MapContentHash="other";break;case 1:q.PolicyVersion="unknown";break;case 2:q.Filter=null!;break;case 3:q.Filter.PermittedNodeIds.Clear();break;
            case 4:q.Filter.PermittedConnectionIds.Add("edge.foreign");break;case 5:q.Filter.PermittedNodeIds.Add("t");break;case 6:q.Targets=Enumerable.Repeat(q.Targets[0],65).ToList();break;
            case 7:q.Targets[0].Intent=GalaxyReviewTargetIntent.SystemLocation;q.Targets[0].SystemId="system.nonexistent";break;}
        var p=Query(nodes,edges,q);Assert.False(p.Success);Assert.Empty(p.Legs);Assert.Empty(p.Steps);Assert.Null(p.TotalCost);
        if(mode==0)Assert.Equal(GalaxyReviewPlanError.StaleMap,p.ErrorCode);
    }

    [Fact]
    public void ExhaustiveSmallGraphsAgreeWithTheDeclaredLexicographicObjective()
    {
        for(int seed=0;seed<12;seed++){
            var random=new Random(seed);var nodes=Enumerable.Range(0,6).Select(i=>Node("n"+i,random.Next(0,500),random.Next(0,500))).ToList();var edges=new List<NavigationConnectionDto>();
            for(int i=0;i<6;i++)for(int j=i+1;j<6;j++)if(j==i+1||random.Next(3)==0)edges.Add(Edge("n"+i,"n"+j));
            var paths=new List<List<string>>();var current=new List<string>{"n0"};
            void Visit(string n){if(n=="n5"){paths.Add(new(current));return;}foreach(var e in edges.Where(e=>e.EndpointA==n||e.EndpointB==n)){
                string next=e.EndpointA==n?e.EndpointB:e.EndpointA;if(current.Contains(next))continue;current.Add(next);Visit(next);current.RemoveAt(current.Count-1);}}
            long Span(List<string> path)=>Enumerable.Range(1,path.Count-1).Sum(i=>{var a=nodes.Single(n=>n.NodeId==path[i-1]).Center;var b=nodes.Single(n=>n.NodeId==path[i]).Center;
                long dx=b.X-a.X,dy=b.Y-a.Y;return (long)Math.Sqrt(dx*dx+dy*dy);});
            Visit("n0");var best=paths.OrderBy(p=>p.Count).ThenBy(Span).ThenBy(p=>string.Join("\0",p),StringComparer.Ordinal).First();
            var result=Query(nodes,edges,Request(nodes,edges,"n0","n5"));Assert.Equal(best.Skip(1),result.Steps);Assert.Equal(Span(best),result.GeographicLengthUnits);
        }
    }

    [Fact]
    public void ActualMapLegacyRouteAndExportRemainExactWhileNewQueriesAreStamped()
    {
        var catalog=new List<CivilizationDefinitionDto>{new(){DefinitionId="civilization.federation"},new(){DefinitionId="civilization.cardassian"}};
        var review=ScaledGalaxyApplication.Generate(new(){Seed="42",HomeCivilizationDefinitionIds=catalog.Select(c=>c.DefinitionId).ToList()},catalog);
        var d=review.Export();string before=Json(d);var h=d.Layout.HomeCandidates;string old=Json(review.FindRoute(h[0].NodeId,h[1].NodeId));
        var q=Request(d.Layout.Regions,d.Layout.Connections,h[0].NodeId,h[1].NodeId);q.MapContentHash=d.ContentHash;var plan=review.FindRoutePlan(q);
        Assert.True(plan.Success);Assert.Equal(review.FindRoute(h[0].NodeId,h[1].NodeId).TotalCost,plan.TotalCost);Assert.Equal(before,Json(review.Export()));Assert.Equal(old,Json(review.FindRoute(h[0].NodeId,h[1].NodeId)));
        var reopened=ScaledGalaxyApplication.Open(JsonSerializer.Deserialize<ScaledGalaxyDocumentDto>(before)!,catalog.AsEnumerable().Reverse());
        Assert.Equal(Json(plan),Json(reopened.FindRoutePlan(q)));Assert.Equal("006ab67d08f1cd3c3d9e73da2fa335fb5e4ba1cee1acd824d63dbd44135c1937",d.ContentHash);
    }
}
