#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.UnityReview
{
    public readonly struct GeneratedLabelRect
    {
        public readonly double X,Y,Width,Height;
        public GeneratedLabelRect(double x,double y,double width,double height){X=x;Y=y;Width=width;Height=height;}
        public bool Intersects(GeneratedLabelRect other)=>X<other.X+other.Width&&X+Width>other.X&&Y<other.Y+other.Height&&Y+Height>other.Y;
    }
    public readonly struct GeneratedLabelCandidate
    {
        public readonly string NodeId;
        public readonly double X,Y,Width,Height,Gap;
        public readonly int Priority;
        public GeneratedLabelCandidate(string node,double x,double y,double width,double height,int priority,double gap=12)
        {NodeId=node;X=x;Y=y;Width=width;Height=height;Priority=priority;Gap=gap;}
    }
    public static class GeneratedCampaignLabelLayout
    {
        // Presentation-only bounded greedy packing. No map identities or coordinates change.
        public static IReadOnlyDictionary<string,GeneratedLabelRect> Place(IEnumerable<GeneratedLabelCandidate> candidates,
            IReadOnlyList<GeneratedLabelRect> glyphs,double width,double height)
        {
            var placed=new Dictionary<string,GeneratedLabelRect>(StringComparer.Ordinal);
            bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
            // On an exceptional visual-obstacle overload, keep tooltips/selection
            // rather than silently omit obstacles and paint misleading overlaps.
            if(!Finite(width)||!Finite(height)||width<=0||height<=0||glyphs.Count>4096)return placed;
            foreach(var c in candidates.Where(c=>!string.IsNullOrEmpty(c.NodeId)&&Finite(c.X)&&Finite(c.Y)&&Finite(c.Width)&&Finite(c.Height)&&Finite(c.Gap)
                &&c.Width>0&&c.Height>0&&c.Gap>=0&&c.X>=0&&c.X<=width&&c.Y>=0&&c.Y<=height)
                .OrderByDescending(c=>c.Priority).ThenBy(c=>c.NodeId,StringComparer.Ordinal).Take(512))
            {
                if(placed.Count>=64)break;
                if(placed.ContainsKey(c.NodeId))continue;
                var options=new[]{new GeneratedLabelRect(c.X+c.Gap,c.Y-c.Height/2,c.Width,c.Height),
                    new GeneratedLabelRect(c.X-c.Gap-c.Width,c.Y-c.Height/2,c.Width,c.Height),
                    new GeneratedLabelRect(c.X-c.Width/2,c.Y-c.Gap-c.Height,c.Width,c.Height),
                    new GeneratedLabelRect(c.X-c.Width/2,c.Y+c.Gap,c.Width,c.Height)};
                foreach(var r in options)
                {
                    if(r.X<0||r.Y<0||r.X+r.Width>width||r.Y+r.Height>height)continue;
                    if(placed.Values.Any(p=>r.Intersects(p))||glyphs.Any(p=>r.Intersects(p)))continue;
                    placed.Add(c.NodeId,r);break;
                }
            }
            return placed;
        }
    }
}
