using HobbyDS9.Application;
using HobbyDS9.Contracts;

internal static class GalaxyReviewRouteExamples
{
    internal static object Run(IEnumerable<CivilizationDefinitionDto> catalog)
    {
        var review=ScaledGalaxyApplication.Generate(new ScaledGalaxySettingsDto {Seed="42",
            HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}},catalog);
        var d=review.Export();var homes=d.Layout.HomeCandidates;string a=homes[0].NodeId,c=homes[1].NodeId;
        var legacy=review.FindRoute(a,c);string b=legacy.Steps[legacy.Steps.Count/2];
        GalaxyReviewTargetDto Transit(string node)=>new(){NodeId=node};
        GalaxyReviewTargetDto Location(string node)=>new(){NodeId=node,Intent=GalaxyReviewTargetIntent.SystemLocation,SystemId=d.Layout.Regions.Single(r=>r.NodeId==node).SystemId};
        GalaxyReviewRouteQueryDto Query(params GalaxyReviewTargetDto[] targets)=>new(){MapContentHash=d.ContentHash,OriginNodeId=a,Targets=targets.ToList(),
            Filter=new(){PermittedNodeIds=d.Layout.Regions.Select(r=>r.NodeId).ToList(),PermittedConnectionIds=d.Layout.Connections.Select(e=>e.ConnectionId).ToList()}};
        var direct=Query(Location(c));var directPlan=review.FindRoutePlan(direct);
        var legacyQuery=Query(Location(c));legacyQuery.Filter.PermittedConnectionIds=legacy.Costs.Select(e=>e.ConnectionId).ToList();
        var legacyMetric=review.FindRoutePlan(legacyQuery);
        if(!legacyMetric.Success||!legacyMetric.Steps.SequenceEqual(legacy.Steps)||directPlan.TotalCost!=legacy.TotalCost)
            throw new InvalidOperationException("Route comparison changed primary cost or legacy-reference sequence.");
        var ordered=Query(Transit(b),Location(c),Transit(d.Layout.Regions.First(r=>r.SystemId==null&&r.NodeId!=b).NodeId));
        var returning=Query(Transit(b),Location(a));var zero=Query(Transit(b),Transit(b),Location(c));
        var blocked=Query(Transit(b),Location(c));
        blocked.Filter.PermittedConnectionIds=d.Layout.Connections.Where(e=>e.EndpointA!=c&&e.EndpointB!=c).Select(e=>e.ConnectionId).ToList();
        var blockedPlan=review.FindRoutePlan(blocked);
        if(blockedPlan.Success||blockedPlan.FailedLegIndex!=1||blockedPlan.Legs.Count!=0||blockedPlan.TotalCost!=null)
            throw new InvalidOperationException("Blocked middle-leg fixture did not reject atomically.");
        return new {MapReference="docs/samples/scaled-galaxy-v4/seed-42-systems-250.scaled.json",d.ContentHash,
            Policy=GalaxyReviewRoutePolicy.MinimumCrossingsDirectV1,Metric=GalaxyReviewRoutePolicy.SecondaryMetric,
            Scope="Full-map review query examples only; not campaign orders, actions or actor knowledge",
            Comparison=new {LegacyRoute=legacy,LegacyCentreSpanUnits=legacyMetric.GeographicLengthUnits,Query=direct,Result=directPlan},
            Cases=new object[]{new{Name="ordered-mixed-targets",Query=ordered,Result=review.FindRoutePlan(ordered)},
                new{Name="return-a-b-a",Query=returning,Result=review.FindRoutePlan(returning)},
                new{Name="zero-leg-a-b-b-c",Query=zero,Result=review.FindRoutePlan(zero)},
                new{Name="blocked-middle-leg",Query=blocked,Result=blockedPlan}}};
    }
}
