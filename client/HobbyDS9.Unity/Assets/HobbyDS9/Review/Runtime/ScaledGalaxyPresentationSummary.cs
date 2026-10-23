#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    // Read-only measurements of the exported document under its declared sampled-site classification.
    // Never changes placement, generation pressure, topology or campaign state.
    public sealed class ScaledGalaxyPresentationSummary
    {
        public IReadOnlyCollection<string> QuietNodes { get; }
        public IReadOnlyCollection<string> DisturbedNodes { get; }
        public int QuietComponentCount { get; }
        public double? QuietToSystemMeanArea { get; }
        public double EmptyAreaShare { get; }
        public ScaledGalaxyPresentationSummary(ScaledGalaxyDocumentDto document)
        {
            var features=document.Features.ToDictionary(f=>f.FeatureId,StringComparer.Ordinal);
            var influences=document.RegionInfluences.ToDictionary(i=>i.NodeId,StringComparer.Ordinal);
            var anchors=new HashSet<string>(document.Features.Where(f=>f.Kind==GalaxyEnvironmentFeatureKind.BlackHole).Select(f=>f.NodeId!),StringComparer.Ordinal);
            bool Disturbed(GeneratedGalaxyRegionDto r)=>anchors.Contains(r.NodeId)||influences[r.NodeId].Contributions.Any(c=>
                features[c.FeatureId].Kind!=GalaxyEnvironmentFeatureKind.Star&&c.Amount>=document.Tuning.QuietDisturbanceThreshold);
            var systems=document.Layout.Regions.Where(r=>r.SystemId!=null).ToArray();
            var quiet=document.Layout.Regions.Where(r=>r.SystemId==null&&!Disturbed(r)).ToArray();
            var disturbed=document.Layout.Regions.Where(r=>r.SystemId==null&&Disturbed(r)).ToArray();
            QuietNodes=Array.AsReadOnly(quiet.Select(r=>r.NodeId).OrderBy(n=>n,StringComparer.Ordinal).ToArray());
            DisturbedNodes=Array.AsReadOnly(disturbed.Select(r=>r.NodeId).OrderBy(n=>n,StringComparer.Ordinal).ToArray());
            QuietToSystemMeanArea=quiet.Length==0?null:quiet.Average(r=>(double)Area2(r))/systems.Average(r=>(double)Area2(r));
            EmptyAreaShare=quiet.Concat(disturbed).Sum(Area2)/(2.0*document.Layout.Width*document.Layout.Height);
            var unseen=new HashSet<string>(QuietNodes,StringComparer.Ordinal);
            var neighbors=QuietNodes.ToDictionary(n=>n,_=>new List<string>(),StringComparer.Ordinal);
            foreach(var edge in document.Layout.Connections)if(neighbors.ContainsKey(edge.EndpointA)&&neighbors.ContainsKey(edge.EndpointB))
            {neighbors[edge.EndpointA].Add(edge.EndpointB);neighbors[edge.EndpointB].Add(edge.EndpointA);}
            int components=0;
            foreach(var start in QuietNodes)
            {
                if(!unseen.Remove(start))continue;components++;var queue=new Queue<string>();queue.Enqueue(start);
                while(queue.Count>0)foreach(var next in neighbors[queue.Dequeue()])if(unseen.Remove(next))queue.Enqueue(next);
            }
            QuietComponentCount=components;
        }
        private static long Area2(GeneratedGalaxyRegionDto region)
        {
            long sum=0;for(int i=0;i<region.Boundary.Count;i++)
            {var a=region.Boundary[i];var b=region.Boundary[(i+1)%region.Boundary.Count];sum=checked(sum+(long)a.X*b.Y-(long)a.Y*b.X);}
            return sum;
        }
    }
}
