#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    public sealed class GeneratedGalaxyMaskResult
    {
        public byte[] Pixels { get; }
        public bool Succeeded { get; }
        public string Reason { get; }
        public int Work { get; }
        internal GeneratedGalaxyMaskResult(byte[] pixels,bool succeeded,string reason,int work){Pixels=pixels;Succeeded=succeeded;Reason=reason;Work=work;}
    }
    // Conservative UNION mask. Edge cancellation precedes erosion, so internal
    // charted borders do not produce invented gaps. All stages share one budget.
    public static class GeneratedGalaxyChartedMask
    {
        private sealed class Limit:Exception { internal Limit(string reason):base(reason){} }
        private sealed class Budget
        {internal int Used;private readonly int maximum;internal Budget(int maximum){this.maximum=maximum;}internal void Add(int n=1){if(n<0||Used>maximum-n)throw new Limit("mask-work-budget");Used+=n;}}
        private readonly struct Point
        {internal readonly int X,Y;internal Point(GeneratedGalaxyPointDto p){X=p.X;Y=p.Y;}}
        private readonly struct Event
        {internal readonly int T,Delta;internal readonly Point P;internal Event(int t,int delta,Point p){T=t;Delta=delta;P=p;}}
        private readonly struct Segment
        {internal readonly double X0,Y0,X1,Y1;internal Segment(double x0,double y0,double x1,double y1){X0=x0;Y0=y0;X1=x1;Y1=y1;}}
        private readonly struct Line:IEquatable<Line>
        {
            private readonly long a,b;private readonly BigInteger c;
            internal Line(Point p,Point q)
            {long x=(long)q.Y-p.Y,y=(long)p.X-q.X,g=Gcd(Math.Abs(x),Math.Abs(y));if(g==0)throw new Limit("zero-edge");x/=g;y/=g;if(x<0||x==0&&y<0){x=-x;y=-y;}a=x;b=y;c=-(new BigInteger(a)*p.X+new BigInteger(b)*p.Y);}
            private static long Gcd(long a,long b){while(b!=0){long n=a%b;a=b;b=n;}return a;}
            public bool Equals(Line other)=>a==other.a&&b==other.b&&c==other.c;
            public override bool Equals(object? other)=>other is Line line&&Equals(line);
            public override int GetHashCode()=>a.GetHashCode()^b.GetHashCode()^c.GetHashCode();
        }
        private sealed class Lines:IEqualityComparer<Line>
        {private readonly Budget budget;internal Lines(Budget budget){this.budget=budget;}public bool Equals(Line x,Line y){budget.Add();return x.Equals(y);}public int GetHashCode(Line x){budget.Add();return x.GetHashCode();}}
        public static GeneratedGalaxyMaskResult Build(IReadOnlyList<GeneratedGalaxyRegionDto> regions,GeneratedGalaxyViewport view,
            double uiWidth,double uiHeight,int width,int height,double guardX=3,double guardY=3,int operationLimit=10000000)
        {
            if(width<1||width>960||height<1||height>540)throw new ArgumentOutOfRangeException(nameof(width));
            var pixels=new byte[width*height];var budget=new Budget(Math.Max(0,operationLimit));
            try
            {
                if(!(uiWidth>=2&&uiHeight>=2&&uiWidth<=100000&&uiHeight<=100000&&guardX>=3&&guardY>=3&&guardX<=64&&guardY<=64))throw new Limit("invalid-mask-view");
                if(regions.Count>2000)throw new Limit("region-cap");
                var groups=new Dictionary<Line,List<Event>>(new Lines(budget));int edges=0;
                foreach(var region in regions)
                {
                    if(region==null)throw new Limit("null-region");
                    var points=region.Boundary;if(points==null||points.Count<3)throw new Limit("invalid-polygon");
                    if(edges>20000-points.Count)throw new Limit("edge-cap");edges+=points.Count;BigInteger area=0;
                    for(int i=0;i<points.Count;i++)
                    {
                        budget.Add(4);if(points[i]==null||points[(i+1)%points.Count]==null)throw new Limit("null-point");
                        var p=new Point(points[i]);var q=new Point(points[(i+1)%points.Count]);
                        area+=(BigInteger)p.X*q.Y-(BigInteger)p.Y*q.X;
                        // Every vertex must lie in every directed edge's closed
                        // halfplane; local triple turns alone can admit star polygons.
                        foreach(var point in points)
                        {budget.Add();if(point==null)throw new Limit("null-point");if(((BigInteger)q.X-p.X)*(point.Y-(long)p.Y)-((BigInteger)q.Y-p.Y)*(point.X-(long)p.X)<0)throw new Limit("nonconvex-polygon");}
                        var line=new Line(p,q);bool vertical=p.X==q.X;int t0=vertical?p.Y:p.X,t1=vertical?q.Y:q.X,sign=t1>t0?1:-1;
                        if(!groups.TryGetValue(line,out var list)){list=new List<Event>();groups.Add(line,list);}
                        list.Add(new Event(Math.Min(t0,t1),sign,t0<t1?p:q));list.Add(new Event(Math.Max(t0,t1),-sign,t0<t1?q:p));
                    }
                    if(area<=0)throw new Limit("polygon-orientation");
                }
                var segments=new List<Segment>();
                foreach(var list in groups.Values)
                {
                    list.Sort((a,b)=>{budget.Add();return a.T.CompareTo(b.T);});int count=0,index=0;Point prior=default;
                    while(index<list.Count)
                    {
                        budget.Add();var e=list[index];
                        if(count!=0)
                        {
                            if(Math.Abs(count)!=1)throw new Limit("overlapping-charted-polygons");
                            if(segments.Count>=40000)throw new Limit("boundary-cap");
                            double x0=view.X(prior.X)*width/uiWidth,y0=view.Y(prior.Y)*height/uiHeight,x1=view.X(e.P.X)*width/uiWidth,y1=view.Y(e.P.Y)*height/uiHeight;
                            if(!(Math.Abs(x0)<=1e9&&Math.Abs(y0)<=1e9&&Math.Abs(x1)<=1e9&&Math.Abs(y1)<=1e9))throw new Limit("projection-range");
                            segments.Add(new Segment(x0,y0,x1,y1));
                        }
                        do{budget.Add();count+=list[index].Delta;index++;}while(index<list.Count&&list[index].T==e.T);
                        prior=e.P;
                    }
                    if(count!=0)throw new Limit("open-union-boundary");
                }
                // Buckets contain each edge twice, not every edge×every row.
                var starts=new List<int>?[height];var ends=new List<int>?[height+1];
                for(int i=0;i<segments.Count;i++)
                {
                    budget.Add();var s=segments[i];if(s.Y0==s.Y1)continue;
                    int first=Math.Max(0,(int)Math.Ceiling(Math.Min(s.Y0,s.Y1)-.5)),last=Math.Min(height-1,(int)Math.Ceiling(Math.Max(s.Y0,s.Y1)-.5)-1);
                    if(first>last)continue;(starts[first]??=new List<int>()).Add(i);(ends[last+1]??=new List<int>()).Add(i);
                }
                var active=new HashSet<int>();var xs=new List<double>();
                for(int y=0;y<height;y++)
                {
                    budget.Add();if(ends[y]!=null)foreach(int i in ends[y]!){budget.Add();active.Remove(i);}if(starts[y]!=null)foreach(int i in starts[y]!){budget.Add();active.Add(i);}
                    xs.Clear();foreach(int i in active){budget.Add();var s=segments[i];xs.Add(s.X0+(y+.5-s.Y0)/(s.Y1-s.Y0)*(s.X1-s.X0));}
                    xs.Sort((a,b)=>{budget.Add();return a.CompareTo(b);});if(xs.Count%2!=0)throw new Limit("unclosed-scanline");
                    for(int k=0;k<xs.Count;k+=2)
                    {
                        int first=Math.Max(0,(int)Math.Ceiling(xs[k]-.5)),last=Math.Min(width-1,(int)Math.Floor(xs[k+1]-.5));
                        for(int x=first;x<=last;x++){budget.Add();pixels[x+y*width]=255;}
                    }
                }
                // Any footprint crossing from inside to outside must intersect a
                // union boundary. Zero its conservative axis-aligned supercover.
                foreach(var s in segments)
                {
                    int first=Math.Max(0,(int)Math.Floor(Math.Min(s.Y0,s.Y1)-guardY-.5)),last=Math.Min(height-1,(int)Math.Ceiling(Math.Max(s.Y0,s.Y1)+guardY-.5));
                    for(int y=first;y<=last;y++)
                    {
                        budget.Add();double xa=s.X0,xb=s.X1;
                        if(s.Y0!=s.Y1)
                        {double lo=Math.Max(Math.Min(s.Y0,s.Y1),y+.5-guardY),hi=Math.Min(Math.Max(s.Y0,s.Y1),y+.5+guardY);if(lo>hi)continue;xa=s.X0+(lo-s.Y0)/(s.Y1-s.Y0)*(s.X1-s.X0);xb=s.X0+(hi-s.Y0)/(s.Y1-s.Y0)*(s.X1-s.X0);}
                        int x0=Math.Max(0,(int)Math.Floor(Math.Min(xa,xb)-guardX-.5)),x1=Math.Min(width-1,(int)Math.Ceiling(Math.Max(xa,xb)+guardX-.5));
                        for(int x=x0;x<=x1;x++){budget.Add();pixels[x+y*width]=0;}
                    }
                }
                return new GeneratedGalaxyMaskResult(pixels,true,"certified-union-support",budget.Used);
            }
            catch(Exception e) when(e is Limit||e.InnerException is Limit)
            {Array.Clear(pixels,0,pixels.Length);return new GeneratedGalaxyMaskResult(pixels,false,(e is Limit?e:e.InnerException!).Message,budget.Used);}
        }
    }
}
