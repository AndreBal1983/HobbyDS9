#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    // Exact 1/1024-unit vertices until projection. This is presentation, never travel cost/search.
    public readonly struct GeneratedGalaxyDisplayPoint : IEquatable<GeneratedGalaxyDisplayPoint>
    {
        public long X1024 { get; }
        public long Y1024 { get; }
        public double X=>X1024/1024.0;
        public double Y=>Y1024/1024.0;
        public GeneratedGalaxyDisplayPoint(long x,long y){X1024=x;Y1024=y;}
        public bool Equals(GeneratedGalaxyDisplayPoint other)=>X1024==other.X1024&&Y1024==other.Y1024;
        public override bool Equals(object? other)=>other is GeneratedGalaxyDisplayPoint p&&Equals(p);
        public override int GetHashCode()=>X1024.GetHashCode()^Y1024.GetHashCode();
    }
    public sealed class GeneratedGalaxyCertifiedPath
    {
        public IReadOnlyList<GeneratedGalaxyDisplayPoint> Points { get; }
        public bool Certified { get; }
        public bool UsedFallback { get; }
        public string Reason { get; }
        public int Operations { get; }
        internal GeneratedGalaxyCertifiedPath(IEnumerable<GeneratedGalaxyDisplayPoint> points,bool certified,bool fallback,string reason,int operations)
        {Points=Array.AsReadOnly(points.ToArray());Certified=certified;UsedFallback=fallback;Reason=reason;Operations=operations;}
    }
    public static class GeneratedGalaxyCorridorPath
    {
        public const int MaximumOperations=100000;
        private sealed class Budget
        {
            public int Used;private readonly int limit;
            public Budget(int limit){this.limit=limit;}
            public void Tick(){if(++Used>limit)throw new InvalidOperationException("geometry operation budget");}
        }
        private readonly struct Portal
        {
            public readonly GeneratedGalaxyDisplayPoint A,B,Left,Right;
            public Portal(GeneratedGalaxyPointDto a,GeneratedGalaxyPointDto b)
            {
                A=Point(a);B=Point(b);
                // The accepted cell boundary is CCW: viewed outwards, b is left and a is right.
                Left=new GeneratedGalaxyDisplayPoint(1023L*b.X+a.X,1023L*b.Y+a.Y);
                Right=new GeneratedGalaxyDisplayPoint(1023L*a.X+b.X,1023L*a.Y+b.Y);
            }
        }
        private readonly struct Fraction : IComparable<Fraction>
        {
            public readonly BigInteger N,D;
            public Fraction(BigInteger n,BigInteger d){if(d.Sign<0){n=-n;d=-d;}N=n;D=d;}
            public int CompareTo(Fraction other)=>(N*other.D).CompareTo(other.N*D);
        }
        private readonly struct Progress : IComparable<Progress>
        {
            public readonly int Segment;public readonly Fraction T;
            public Progress(int segment,Fraction t){if(t.N==t.D){segment++;t=new Fraction(0,1);}Segment=segment;T=t;}
            public int CompareTo(Progress other)=>Segment!=other.Segment?Segment.CompareTo(other.Segment):T.CompareTo(other.T);
        }
        private readonly struct RationalPoint
        {
            public readonly BigInteger X,Y,D;
            public RationalPoint(BigInteger x,BigInteger y,BigInteger d){X=x;Y=y;D=d;}
            public static RationalPoint At(GeneratedGalaxyDisplayPoint p)=>new RationalPoint(p.X1024,p.Y1024,1);
            public static RationalPoint Mid(RationalPoint p,RationalPoint q)=>new RationalPoint(p.X*q.D+q.X*p.D,p.Y*q.D+q.Y*p.D,2*p.D*q.D);
        }
        private static GeneratedGalaxyDisplayPoint Point(GeneratedGalaxyPointDto p)=>new GeneratedGalaxyDisplayPoint(1024L*p.X,1024L*p.Y);
        private static BigInteger Cross(long ax,long ay,long bx,long by)=>(BigInteger)ax*by-(BigInteger)ay*bx;
        private static BigInteger Turn(GeneratedGalaxyDisplayPoint a,GeneratedGalaxyDisplayPoint b,GeneratedGalaxyDisplayPoint c,Budget budget)
        {budget.Tick();return Cross(b.X1024-a.X1024,b.Y1024-a.Y1024,c.X1024-a.X1024,c.Y1024-a.Y1024);}

        public static GeneratedGalaxyCertifiedPath Build(IReadOnlyDictionary<string,GeneratedGalaxyRegionDto> regions,string origin,IReadOnlyList<string> steps,int operationLimit=MaximumOperations)
        {
            var budget=new Budget(Math.Max(0,Math.Min(operationLimit,MaximumOperations)));string failure="";
            var ids=new[]{origin}.Concat(steps).ToArray();
            if(ids.Length>2000)return new GeneratedGalaxyCertifiedPath(Array.Empty<GeneratedGalaxyDisplayPoint>(),false,false,"channel limit",0);
            Portal[] portals;var extractionBudget=new Budget(MaximumOperations);
            try{portals=Enumerable.Range(0,steps.Count).Select(i=>Shared(regions[ids[i]],regions[ids[i+1]],extractionBudget)).ToArray();}
            catch(ArgumentException){return new GeneratedGalaxyCertifiedPath(Array.Empty<GeneratedGalaxyDisplayPoint>(),false,false,"no positive shared edge",extractionBudget.Used);}
            catch(InvalidOperationException){return new GeneratedGalaxyCertifiedPath(Array.Empty<GeneratedGalaxyDisplayPoint>(),false,false,"portal extraction operation budget; no certified route line",extractionBudget.Used);}
            var start=Point(regions[origin].Center);var end=Point(regions[ids[ids.Length-1]].Center);
            try
            {
                if(ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length)failure="non-simple channel";
                else
                {
                    var path=Pull(start,end,portals,budget);
                    if(Certify(regions,ids,portals,path,budget,out failure))return new GeneratedGalaxyCertifiedPath(path,true,false,"inset-portal certificate",extractionBudget.Used+budget.Used);
                }
            }
            catch(InvalidOperationException){failure="geometry operation budget";}
            // A separately bounded, linear certificate for raw midpoints; never presume strictness.
            var fallbackBudget=new Budget(MaximumOperations);var raw=new List<GeneratedGalaxyDisplayPoint>{start};
            raw.AddRange(portals.Select(p=>new GeneratedGalaxyDisplayPoint((p.A.X1024+p.B.X1024)/2,(p.A.Y1024+p.B.Y1024)/2)));
            if(steps.Count>0)raw.Add(end);
            try
            {
                if(CertifyMidpoints(regions,ids,raw,fallbackBudget))return new GeneratedGalaxyCertifiedPath(raw,true,true,failure,extractionBudget.Used+budget.Used+fallbackBudget.Used);
            }
            catch(InvalidOperationException){failure+="; fallback operation budget";}
            return new GeneratedGalaxyCertifiedPath(Array.Empty<GeneratedGalaxyDisplayPoint>(),false,false,failure+"; no certified route line",extractionBudget.Used+budget.Used+fallbackBudget.Used);
        }
        public static bool CertifyCandidate(IReadOnlyDictionary<string,GeneratedGalaxyRegionDto> regions,string origin,IReadOnlyList<string> steps,
            IReadOnlyList<GeneratedGalaxyDisplayPoint> points,out string reason)
        {
            var budget=new Budget(MaximumOperations);reason="candidate bounds";
            if(steps.Count>=2000||points.Count>2*steps.Count+2)return false;
            try
            {
                var ids=new[]{origin}.Concat(steps).ToArray();var portals=Enumerable.Range(0,steps.Count).Select(i=>Shared(regions[ids[i]],regions[ids[i+1]],budget)).ToArray();
                return Certify(regions,ids,portals,points,budget,out reason);
            }
            catch(ArgumentException){reason="no positive shared edge";return false;}
            catch(InvalidOperationException){reason="candidate operation budget";return false;}
        }
        private static Portal Shared(GeneratedGalaxyRegionDto from,GeneratedGalaxyRegionDto to,Budget budget)
        {
            for(int i=0;i<from.Boundary.Count;i++)
            {
                var a=from.Boundary[i];var b=from.Boundary[(i+1)%from.Boundary.Count];if(a.X==b.X&&a.Y==b.Y)continue;
                for(int j=0;j<to.Boundary.Count;j++)
                {
                    budget.Tick();
                    var c=to.Boundary[j];var d=to.Boundary[(j+1)%to.Boundary.Count];
                    if(a.X==c.X&&a.Y==c.Y&&b.X==d.X&&b.Y==d.Y||a.X==d.X&&a.Y==d.Y&&b.X==c.X&&b.Y==c.Y)return new Portal(a,b);
                }
            }
            throw new ArgumentException("No shared edge.");
        }
        private static List<GeneratedGalaxyDisplayPoint> Pull(GeneratedGalaxyDisplayPoint start,GeneratedGalaxyDisplayPoint end,Portal[] portals,Budget budget)
        {
            var output=new List<GeneratedGalaxyDisplayPoint>{start};var apex=start;var left=start;var right=start;
            int apexIndex=0,leftIndex=0,rightIndex=0;
            for(int i=1;i<=portals.Length+1;i++)
            {
                budget.Tick();var nextLeft=i>portals.Length?end:portals[i-1].Left;var nextRight=i>portals.Length?end:portals[i-1].Right;
                if(Turn(apex,right,nextRight,budget)>=0)
                {
                    if(apex.Equals(right)||Turn(apex,left,nextRight,budget)<0){right=nextRight;rightIndex=i;}
                    else{if(!output[output.Count-1].Equals(left))output.Add(left);apex=left;apexIndex=leftIndex;left=right=apex;leftIndex=rightIndex=apexIndex;i=apexIndex;continue;}
                }
                if(Turn(apex,left,nextLeft,budget)<=0)
                {
                    if(apex.Equals(left)||Turn(apex,right,nextLeft,budget)>0){left=nextLeft;leftIndex=i;}
                    else{if(!output[output.Count-1].Equals(right))output.Add(right);apex=right;apexIndex=rightIndex;left=right=apex;leftIndex=rightIndex=apexIndex;i=apexIndex;}
                }
            }
            if(!output[output.Count-1].Equals(end))output.Add(end);return output;
        }
        private static RationalPoint At(IReadOnlyList<GeneratedGalaxyDisplayPoint> path,Progress progress)
        {
            if(progress.Segment>=path.Count-1)return RationalPoint.At(path[path.Count-1]);
            var a=path[progress.Segment];var b=path[progress.Segment+1];var t=progress.T;
            return new RationalPoint((BigInteger)a.X1024*t.D+(BigInteger)(b.X1024-a.X1024)*t.N,
                (BigInteger)a.Y1024*t.D+(BigInteger)(b.Y1024-a.Y1024)*t.N,t.D);
        }
        private static bool Inside(GeneratedGalaxyRegionDto cell,RationalPoint p,bool strict,Budget budget)
        {
            for(int i=0;i<cell.Boundary.Count;i++)
            {
                budget.Tick();var a=Point(cell.Boundary[i]);var b=Point(cell.Boundary[(i+1)%cell.Boundary.Count]);
                if(a.Equals(b))continue;
                var cross=(BigInteger)(b.X1024-a.X1024)*(p.Y-a.Y1024*p.D)-(BigInteger)(b.Y1024-a.Y1024)*(p.X-a.X1024*p.D);
                if(strict?cross.Sign<=0:cross.Sign<0)return false;
            }
            return true;
        }
        private static bool Piece(GeneratedGalaxyRegionDto cell,RationalPoint a,RationalPoint b,Budget budget)
        {
            if(a.X*b.D==b.X*a.D&&a.Y*b.D==b.Y*a.D)return false;
            return Inside(cell,a,false,budget)&&Inside(cell,b,false,budget)&&Inside(cell,RationalPoint.Mid(a,b),true,budget);
        }
        private static bool CertifyMidpoints(IReadOnlyDictionary<string,GeneratedGalaxyRegionDto> regions,string[] ids,IReadOnlyList<GeneratedGalaxyDisplayPoint> path,Budget budget)
        {
            if(ids.Length==1)return Inside(regions[ids[0]],RationalPoint.At(path[0]),true,budget);
            if(!Inside(regions[ids[0]],RationalPoint.At(path[0]),true,budget)||!Inside(regions[ids[ids.Length-1]],RationalPoint.At(path[path.Count-1]),true,budget))return false;
            for(int i=0;i<ids.Length;i++)if(!Piece(regions[ids[i]],RationalPoint.At(path[i]),RationalPoint.At(path[i+1]),budget))return false;
            return true;
        }
        private static bool Certify(IReadOnlyDictionary<string,GeneratedGalaxyRegionDto> regions,string[] ids,Portal[] portals,IReadOnlyList<GeneratedGalaxyDisplayPoint> path,Budget budget,out string failure)
        {
            failure="ordered corridor certificate rejected";
            if(path.Count==0||!path[0].Equals(Point(regions[ids[0]].Center))||!path[path.Count-1].Equals(Point(regions[ids[ids.Length-1]].Center)))
            {failure="explicit endpoint changed";return false;}
            if(portals.Length==0)return path.Count==1&&Inside(regions[ids[0]],RationalPoint.At(path[0]),true,budget);
            var crossings=new List<Progress>{new Progress(0,new Fraction(0,1))};
            foreach(var gate in portals)
            {
                Progress? found=null;
                for(int segment=0;segment<path.Count-1;segment++)
                {
                    budget.Tick();var p=path[segment];var q=path[segment+1];long rx=q.X1024-p.X1024,ry=q.Y1024-p.Y1024;
                    long sx=gate.B.X1024-gate.A.X1024,sy=gate.B.Y1024-gate.A.Y1024;
                    var den=Cross(rx,ry,sx,sy);var offsetX=gate.A.X1024-p.X1024;var offsetY=gate.A.Y1024-p.Y1024;
                    if(den.IsZero)
                    {
                        // Collinear support is not an unambiguous interior crossing. Reject even tangency.
                        if(Cross(offsetX,offsetY,rx,ry).IsZero)
                        {
                            long p0=rx!=0?p.X1024:p.Y1024,q0=rx!=0?q.X1024:q.Y1024;
                            long a0=rx!=0?gate.A.X1024:gate.A.Y1024,b0=rx!=0?gate.B.X1024:gate.B.Y1024;
                            if(Math.Min(p0,q0)<=Math.Max(a0,b0)&&Math.Min(a0,b0)<=Math.Max(p0,q0)){failure="collinear portal support";return false;}
                        }
                        continue;
                    }
                    var t=new Fraction(Cross(offsetX,offsetY,sx,sy),den);var u=new Fraction(Cross(offsetX,offsetY,rx,ry),den);
                    if(t.N<0||t.N>t.D||u.N<0||u.N>u.D)continue;
                    if(u.N*1024<u.D||u.N*1024>1023*u.D){failure="corner or inset clearance";return false;}
                    var progress=new Progress(segment,t);
                    if(found.HasValue&&found.Value.CompareTo(progress)!=0){failure="repeated portal crossing";return false;}found=progress;
                }
                if(!found.HasValue||crossings[crossings.Count-1].CompareTo(found.Value)>=0)return false;
                crossings.Add(found.Value);
            }
            var finish=new Progress(path.Count-1,new Fraction(0,1));if(crossings[crossings.Count-1].CompareTo(finish)>=0)return false;crossings.Add(finish);
            if(!Inside(regions[ids[0]],RationalPoint.At(path[0]),true,budget)||!Inside(regions[ids[ids.Length-1]],RationalPoint.At(path[path.Count-1]),true,budget))return false;
            for(int region=0;region<ids.Length;region++)
            {
                var begin=crossings[region];var end=crossings[region+1];var previous=At(path,begin);var cell=regions[ids[region]];
                for(int vertex=begin.Segment+1;vertex<path.Count;vertex++)
                {
                    var progress=new Progress(vertex,new Fraction(0,1));if(progress.CompareTo(end)>=0)break;
                    var point=RationalPoint.At(path[vertex]);if(!Inside(cell,point,true,budget)||!Piece(cell,previous,point,budget))return false;previous=point;
                }
                if(!Piece(cell,previous,At(path,end),budget))return false;
            }
            failure="";return true;
        }
    }
}
