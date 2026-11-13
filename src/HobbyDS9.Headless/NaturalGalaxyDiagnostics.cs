using HobbyDS9.Contracts;

// Measurements of actual owned output; never inputs to generation or a campaign.
internal static class NaturalGalaxyDiagnostics
{
    internal static object Measure(NaturalGalaxyDocumentDto d)
    {
        var stars=d.Features.Where(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star).OrderBy(f=>f.FeatureId,StringComparer.Ordinal).ToArray();
        var nearest=new List<object>();var distances=new List<long>();var parent=Enumerable.Range(0,stars.Length).ToArray();
        int Root(int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
        int closePairs=0;const long closeSquared=30000L*30000;
        for(int i=0;i<stars.Length;i++){
            long best=long.MaxValue;string closest="";
            for(int j=0;j<stars.Length;j++){
                if(i==j)continue;long dx=stars[i].Position.X-stars[j].Position.X,dy=stars[i].Position.Y-stars[j].Position.Y,squared=dx*dx+dy*dy;
                if(squared<best||squared==best&&StringComparer.Ordinal.Compare(stars[j].FeatureId,closest)<0){best=squared;closest=stars[j].FeatureId;}
                if(j>i&&squared<=closeSquared){closePairs++;int a=Root(i),b=Root(j);parent[Math.Max(a,b)]=Math.Min(a,b);}
            }
            distances.Add(best);nearest.Add(new{FeatureId=stars[i].FeatureId,NearestFeatureId=closest,DistanceSquared=best});
        }
        var components=Enumerable.Range(0,stars.Length).GroupBy(Root).Select(g=>new{
            ComponentId=stars[g.Min()].FeatureId,NodeIds=g.Select(i=>stars[i].NodeId).ToArray(),Size=g.Count()}).OrderBy(g=>g.ComponentId,StringComparer.Ordinal).ToArray();
        distances.Sort();
        var loose=d.StarGroups.Where(g=>g.Kind==NaturalStarGroupKind.LooseAssociation).ToArray();
        var common=ScaledGalaxyDiagnostics.Measure(new ScaledGalaxyDocumentDto{ContentHash=d.ContentHash,Settings=d.Settings.Map,
            Tuning=new(){Environment=d.Tuning.Environment,QuietDisturbanceThreshold=d.Tuning.QuietDisturbanceThreshold},
            Features=d.Features,Layout=d.Layout,RegionInfluences=d.RegionInfluences});
        return new{d.ContentHash,CoordinateMeaning="Integer map presentation units, not physical distance or ship speed",
            ProvenanceGroupsAreNotNearestNeighbourComponents=true,
            ProvenanceIsolatedCount=d.StarGroups.Count(g=>g.Kind==NaturalStarGroupKind.Isolated),
            ActualStarsWithNearestAtLeastIsolatedSeparation=distances.Count(x=>x>=(long)d.Tuning.IsolatedStarSeparation*d.Tuning.IsolatedStarSeparation),
            NearestDistanceMinimum=Math.Sqrt(distances[0]),NearestDistanceP10=Math.Sqrt(distances[(distances.Count-1)/10]),
            NearestDistanceMedian=Math.Sqrt(distances[distances.Count/2]),NearestDistanceP90=Math.Sqrt(distances[(distances.Count-1)*9/10]),
            NearestDistanceMaximum=Math.Sqrt(distances[^1]),NearestDistanceMean=distances.Average(x=>Math.Sqrt(x)),NearestNeighbours=nearest,
            LooseAssociationArities=loose.GroupBy(g=>g.StarFeatureIds.Count).OrderBy(g=>g.Key).Select(g=>new{MemberCount=g.Key,GroupCount=g.Count()}).ToArray(),
            LooseRadiusMinimum=loose.Min(g=>Math.Min(g.RadiusX,g.RadiusY)),LooseRadiusMaximum=loose.Max(g=>Math.Max(g.RadiusX,g.RadiusY)),
            CloseNeighbourThreshold=30000,ClosePairCount=closePairs,CloseComponents=components,
            CloseComponentSizes=components.GroupBy(c=>c.Size).OrderBy(g=>g.Key).Select(g=>new{Size=g.Key,ComponentCount=g.Count()}).ToArray(),GeometryAndQuietSpace=common};
    }
}
