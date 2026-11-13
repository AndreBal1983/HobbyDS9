using HobbyDS9.Contracts;

// Host-only measured observations, never trusted input to generation or campaign state.
internal static class ScaledGalaxyDiagnostics
{
    internal static object Measure(ScaledGalaxyDocumentDto d)
    {
        var l = d.Layout;
        var features = d.Features.ToDictionary(f => f.FeatureId);
        var influences = d.RegionInfluences.ToDictionary(r => r.NodeId);
        var blackHoleNodes = d.Features.Where(f => f.Kind == GalaxyEnvironmentFeatureKind.BlackHole).Select(f => f.NodeId).ToHashSet();
        bool Disturbed(GeneratedGalaxyRegionDto r) => blackHoleNodes.Contains(r.NodeId) || influences[r.NodeId].Contributions.Any(c =>
            features[c.FeatureId].Kind != GalaxyEnvironmentFeatureKind.Star && c.Amount >= d.Tuning.QuietDisturbanceThreshold);
        long Area(GeneratedGalaxyRegionDto r)
        {
            long sum = 0;
            for (int i = 0; i < r.Boundary.Count; i++) {
                var a = r.Boundary[i]; var b = r.Boundary[(i + 1) % r.Boundary.Count];
                sum = checked(sum + (long)a.X * b.Y - (long)a.Y * b.X);
            }
            return sum;
        }
        var systems = l.Regions.Where(r => r.SystemId != null).ToArray();
        var quiet = l.Regions.Where(r => r.SystemId == null && !Disturbed(r)).ToArray();
        var disturbed = l.Regions.Where(r => r.SystemId == null && Disturbed(r)).ToArray();
        var neighbors = l.Regions.ToDictionary(r => r.NodeId, _ => new List<string>());
        foreach (var e in l.Connections) { neighbors[e.EndpointA].Add(e.EndpointB); neighbors[e.EndpointB].Add(e.EndpointA); }
        foreach (var list in neighbors.Values) list.Sort(StringComparer.Ordinal);
        Dictionary<string,int> Distances(string start, HashSet<string>? allowed = null) {
            var distances = new Dictionary<string,int> { [start] = 0 }; var queue = new Queue<string>(); queue.Enqueue(start);
            while (queue.Count > 0) { var node = queue.Dequeue(); foreach (var next in neighbors[node])
                if ((allowed == null || allowed.Contains(next)) && distances.TryAdd(next, distances[node] + 1)) queue.Enqueue(next); }
            return distances;
        }
        var quietIds = quiet.Select(r => r.NodeId).ToHashSet(); var visited = new HashSet<string>(); var components = new List<object>();
        foreach (var node in quietIds.Order(StringComparer.Ordinal)) {
            if (visited.Contains(node)) continue;
            var component = Distances(node, quietIds).Keys.ToHashSet(); visited.UnionWith(component);
            int diameter = component.Max(n => Distances(n, component).Values.Max());
            components.Add(new { ComponentId = node, NodeIds = component.Order(StringComparer.Ordinal).ToArray(), MaxCrossings = diameter,
                AdjacentSystemCount = component.SelectMany(n => neighbors[n]).Distinct().Count(n => l.Regions.Single(r => r.NodeId == n).SystemId != null) });
        }
        var pairDistances = new List<int>();
        for (int i = 0; i < systems.Length; i++) { var distances = Distances(systems[i].NodeId);
            for (int j = i + 1; j < systems.Length; j++) pairDistances.Add(distances[systems[j].NodeId]); }
        pairDistances.Sort();
        var edges = new Dictionary<(int,int,int,int),int>();
        foreach (var r in l.Regions) for (int i = 0; i < r.Boundary.Count; i++) {
            var a=r.Boundary[i]; var b=r.Boundary[(i+1)%r.Boundary.Count];
            var key = a.X < b.X || a.X == b.X && a.Y < b.Y ? (a.X,a.Y,b.X,b.Y) : (b.X,b.Y,a.X,a.Y);
            edges[key]=edges.GetValueOrDefault(key)+1;
        }
        var lengths = edges.Where(e=>e.Value==2).Select(e=>(long)(e.Key.Item1-e.Key.Item3)*(e.Key.Item1-e.Key.Item3)+(long)(e.Key.Item2-e.Key.Item4)*(e.Key.Item2-e.Key.Item4)).Order().ToArray();
        double? Mean(GeneratedGalaxyRegionDto[] rows) => rows.Length == 0 ? null : rows.Average(r => (double)Area(r)) / 2;
        double systemMean = Mean(systems)!.Value;
        return new { d.ContentHash, Classification = "Site-pressure sample; not whole-cell environmental coverage or campaign knowledge",
            QuietEmptyNodeIds = quiet.Select(r=>r.NodeId).Order(StringComparer.Ordinal).ToArray(),
            DisturbedEmptyNodeIds = disturbed.Select(r=>r.NodeId).Order(StringComparer.Ordinal).ToArray(),
            QuietEmptyMeanArea = Mean(quiet), DisturbedEmptyMeanArea = Mean(disturbed), SystemMeanArea = systemMean,
            QuietEmptySystemMeanAreaRatio = Mean(quiet) / systemMean, AllEmptyAreaShare = (quiet.Concat(disturbed).Sum(Area)/2.0) / ((long)l.Width*l.Height),
            QuietComponents = components, QuietComponentCount = components.Count, SystemPairCount = pairDistances.Count,
            SystemPairCrossingsMedian = pairDistances[pairDistances.Count/2], SystemPairCrossingsP90 = pairDistances[(pairDistances.Count-1)*9/10],
            SystemPairCrossingsMaximum = pairDistances[^1], InteriorEdgeCount = lengths.Length,
            MinimumSharedEdgeLengthSquared = lengths[0], MedianSharedEdgeLengthSquared = lengths[lengths.Length/2] };
    }
}
