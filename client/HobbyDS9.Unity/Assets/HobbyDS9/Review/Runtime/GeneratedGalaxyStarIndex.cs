#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    // Invisible presentation index. Exact persisted star positions; no displaced/merged markers.
    public sealed class GeneratedGalaxyStarIndex
    {
        private readonly double cellSize;
        private readonly Dictionary<(int,int),List<(string id,GeneratedGalaxyPointDto point)>> cells=new Dictionary<(int,int),List<(string,GeneratedGalaxyPointDto)>>();
        public GeneratedGalaxyStarIndex(IEnumerable<(string id,GeneratedGalaxyPointDto point)> stars,double worldWidth,double worldHeight)
        {
            cellSize=Math.Max(worldWidth,worldHeight)/32;
            if(cellSize<=0)throw new ArgumentOutOfRangeException(nameof(worldWidth));
            foreach(var star in stars.OrderBy(s=>s.id,StringComparer.Ordinal))
            {
                var key=Cell(star.point.X,star.point.Y);
                if(!cells.TryGetValue(key,out var entries))cells.Add(key,entries=new List<(string,GeneratedGalaxyPointDto)>());
                entries.Add(star);
            }
        }
        public string? Nearest(double x,double y,double radius)
        {
            if(double.IsNaN(x)||double.IsInfinity(x)||double.IsNaN(y)||double.IsInfinity(y)||radius<0||double.IsInfinity(radius)||double.IsNaN(radius))return null;
            var low=Cell(x-radius,y-radius);var high=Cell(x+radius,y+radius);
            string? result=null;double best=radius*radius;
            foreach(var cell in cells.Where(c=>c.Key.Item1>=low.Item1&&c.Key.Item1<=high.Item1&&c.Key.Item2>=low.Item2&&c.Key.Item2<=high.Item2))
            foreach(var star in cell.Value)
            {
                double dx=star.point.X-x,dy=star.point.Y-y,distance=dx*dx+dy*dy;
                if(distance<best||distance==best&&(result==null||StringComparer.Ordinal.Compare(star.id,result)<0))
                {best=distance;result=star.id;}
            }
            return result;
        }
        private (int,int) Cell(double x,double y)=>((int)Math.Floor(x/cellSize),(int)Math.Floor(y/cellSize));
    }
}
