#nullable enable
using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    // Half-unit presentation coordinates retain exact shared-edge midpoints, even on one-unit edges.
    public readonly struct GeneratedGalaxyRoutePoint
    {
        public long X2 { get; }
        public long Y2 { get; }
        public double X => X2 / 2.0;
        public double Y => Y2 / 2.0;
        internal GeneratedGalaxyRoutePoint(long x2,long y2){X2=x2;Y2=y2;}
    }

    // Consumes an accepted route; never searches for a route or changes its nodes, costs or polygons.
    public static class GeneratedGalaxyRoutePath
    {
        public static IReadOnlyList<GeneratedGalaxyRoutePoint> Build(
            IReadOnlyDictionary<string,GeneratedGalaxyRegionDto> regions,string origin,IReadOnlyList<string> steps)
        {
            var current=regions[origin];
            var points=new List<GeneratedGalaxyRoutePoint>{Center(current)};
            foreach(var node in steps)
            {
                var next=regions[node];
                points.Add(Portal(current,next));
                points.Add(Center(next));
                current=next;
            }
            return points.AsReadOnly();
        }

        private static GeneratedGalaxyRoutePoint Center(GeneratedGalaxyRegionDto region)
            => new GeneratedGalaxyRoutePoint(2L*region.Center.X,2L*region.Center.Y);

        private static GeneratedGalaxyRoutePoint Portal(GeneratedGalaxyRegionDto from,GeneratedGalaxyRegionDto to)
        {
            for(int i=0;i<from.Boundary.Count;i++)
            {
                var a=from.Boundary[i];var b=from.Boundary[(i+1)%from.Boundary.Count];
                if(Same(a,b))continue;
                for(int j=0;j<to.Boundary.Count;j++)
                {
                    var c=to.Boundary[j];var d=to.Boundary[(j+1)%to.Boundary.Count];
                    if(Same(a,c)&&Same(b,d)||Same(a,d)&&Same(b,c))
                        // A validated region is convex and contains its center. Both half-segments
                        // therefore stay in their own region, meeting only at this shared boundary.
                        return new GeneratedGalaxyRoutePoint((long)a.X+b.X,(long)a.Y+b.Y);
                }
            }
            // Do not silently fall back to a chord across unrelated cells or a corner-only crossing.
            throw new ArgumentException("Route regions do not share a positive-length boundary.",nameof(to));
        }

        private static bool Same(GeneratedGalaxyPointDto a,GeneratedGalaxyPointDto b)=>a.X==b.X&&a.Y==b.Y;
    }
}
