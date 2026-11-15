#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.UnityReview
{
    // Decorative layout only, never orbital physics or a source of body instances.
    public static class SolSchematicLayout
    {
        private static readonly string[] order={"planet.mercury","planet.venus","planet.sol","planet.mars","planet.jupiter","planet.saturn","planet.uranus","planet.neptune"};
        public static int Ordinal(string id)=>Array.IndexOf(order,id);
        public static double Radius(int index)=>18+4*index;
        public static double Angle(int index)=>-22+index*137.508;
        public static double Au(long kilometres)=>(double)kilometres/149600000;
        public static double GlyphFraction(string id,bool namedSol)=>namedSol?(Ordinal(id)>=4?.062:.05):.05+(PlanetVisualProfile.StableSeed(id)%3)*.004;
        public readonly struct Point
        {
            public string Id {get;} public double X {get;} public double Y {get;}
            public Point(string id,double x,double y){Id=id;X=x;Y=y;}
        }
        public readonly struct Label
        {
            public string Id {get;} public double X {get;} public double Y {get;}
            public Label(string id,double x,double y){Id=id;X=x;Y=y;}
        }
        // Balanced side rails keep every disclosed label visible, without covering the plot.
        public static IReadOnlyList<Label> Labels(IEnumerable<Point> source,double width,double height)
        {
            var points=source.OrderBy(p=>p.X).ThenBy(p=>p.Id,StringComparer.Ordinal).ToArray();var result=new List<Label>();
            var half=(points.Length+1)/2;
            void Rail(IEnumerable<Point> values,double x)
            {
                var rows=values.OrderBy(p=>p.Y).ThenBy(p=>p.Id,StringComparer.Ordinal).ToArray();
                var ys=rows.Select(p=>Math.Max(6,Math.Min(height-50,p.Y-22))).ToArray();
                for(int i=1;i<ys.Length;i++)ys[i]=Math.Max(ys[i],ys[i-1]+46);
                if(ys.Length>0)ys[ys.Length-1]=Math.Min(ys[ys.Length-1],height-50);
                for(int i=ys.Length-2;i>=0;i--)ys[i]=Math.Min(ys[i],ys[i+1]-46);
                for(int i=0;i<rows.Length;i++)result.Add(new Label(rows[i].Id,x,ys[i]));
            }
            Rail(points.Take(half),6);Rail(points.Skip(half),Math.Max(6,width-114));return result.AsReadOnly();
        }
    }
}
