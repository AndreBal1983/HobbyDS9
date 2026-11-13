#nullable enable
using System;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    public enum PlanetSurfaceKind { Unknown, Unclassified, Temperate, Arid, Oceanic, Frozen, Airless, Wetland, Volcanic, GasGiant }

    // Presentation only. Never receives a package, hidden definition, or simulation RNG.
    public sealed class PlanetVisualProfile
    {
        public const int VisualVersion = 1;
        public PlanetSurfaceKind Kind { get; }
        public uint Seed { get; }
        public bool HasAtmosphere { get; }
        public string Caption { get; }
        public string? AuthoredMap { get; }
        public float CloudCoverage { get; }
        public float AtmosphereStrength { get; }
        private PlanetVisualProfile(PlanetSurfaceKind kind, uint seed, bool atmosphere, string caption, string? map)
        {
            Kind=kind;Seed=seed;HasAtmosphere=atmosphere;Caption=caption;AuthoredMap=map;
            CloudCoverage=!atmosphere?0:kind==PlanetSurfaceKind.Oceanic?.52f:kind==PlanetSurfaceKind.Wetland?.44f:kind==PlanetSurfaceKind.Temperate?.32f:kind==PlanetSurfaceKind.Frozen?.12f:.07f;
            AtmosphereStrength=atmosphere?.7f:0;
        }
        public static PlanetVisualProfile From(ColonisationPlanetSnapshot planet)
        {
            if(planet==null)throw new ArgumentNullException(nameof(planet));
            // Survey is the disclosure boundary, even if malformed input contains hidden fields.
            if(!planet.Surveyed || string.IsNullOrWhiteSpace(planet.PlanetClass))
                return new PlanetVisualProfile(PlanetSurfaceKind.Unknown,0,false,"Surface unresolved",null);
            var type=planet.PlanetClass!.Trim().ToLowerInvariant();
            var kind=type=="temperate"||type=="m"?PlanetSurfaceKind.Temperate:
                type=="arid"||type=="desert"?PlanetSurfaceKind.Arid:
                type=="oceanic"||type=="ocean"?PlanetSurfaceKind.Oceanic:
                type=="frozen"||type=="ice"?PlanetSurfaceKind.Frozen:
                type=="vacuum"||type=="airless"||type=="barren"?PlanetSurfaceKind.Airless:
                type=="wetland"?PlanetSurfaceKind.Wetland:type=="volcanic"?PlanetSurfaceKind.Volcanic:
                type=="gas giant"||type=="gasgiant"||type=="gas-giant"?PlanetSurfaceKind.GasGiant:PlanetSurfaceKind.Unclassified;
            var air=!string.IsNullOrWhiteSpace(planet.Atmosphere) &&
                !planet.Atmosphere!.EndsWith("vacuum",StringComparison.OrdinalIgnoreCase) &&
                !planet.Atmosphere.Equals("none",StringComparison.OrdinalIgnoreCase) && planet.Pressure>0 && kind!=PlanetSurfaceKind.Airless;
            // Exact canonical IDs, never a localized display-name match.
            string? map=planet.PlanetId=="planet.sol"?"earth_daymap":planet.PlanetId=="planet.mars"?"mars":null;
            return new PlanetVisualProfile(kind,StableSeed(planet.SystemId+"/"+planet.BodyId+"/"+planet.PlanetId),air,
                kind==PlanetSurfaceKind.Airless?"Airless world":kind==PlanetSurfaceKind.Unclassified?"Class "+planet.PlanetClass:kind.ToString()+" world",map);
        }
        public static uint StableSeed(string value)
        {
            unchecked {uint hash=2166136261;foreach(var c in value){hash^=c;hash*=16777619;}return hash;}
        }
    }

    public readonly struct PlanetSurfaceSample
    {
        public readonly float R,G,B,Water;
        public PlanetSurfaceSample(float r,float g,float b,float water=0){R=r;G=g;B=b;Water=water;}
    }

    public static class PlanetSurfaceGenerator
    {
        // Sampling a 3D unit sphere makes the longitude seam and poles continuous.
        public static PlanetSurfaceSample Sample(PlanetVisualProfile p,double x,double y,double z)
        {
            var seed=p.Seed;
            var height=Fbm(x*2.7,y*2.7,z*2.7,seed);
            var detail=Fbm(x*17,y*17,z*17,seed+91);
            var latitude=Math.Abs(y);
            var sea=p.Kind==PlanetSurfaceKind.Oceanic?.69:p.Kind==PlanetSurfaceKind.Wetland?.53:.46;
            var variation=(float)(.78+detail*.36);
            PlanetSurfaceSample color;
            switch(p.Kind)
            {
                case PlanetSurfaceKind.Temperate:case PlanetSurfaceKind.Wetland:case PlanetSurfaceKind.Oceanic:
                    if(height<sea)color=Mix(new PlanetSurfaceSample(.015f,.055f,.14f,1),new PlanetSurfaceSample(.025f,.36f,.45f,1),(height/(sea+.001))*.9);
                    else if(height<sea+.022)color=new PlanetSurfaceSample(.50f,.55f,.35f);
                    else color=Mix(new PlanetSurfaceSample(.035f,.16f,.105f),new PlanetSurfaceSample(.32f,.41f,.20f),detail);
                    if(latitude+height*.12>.94)color=Mix(color,new PlanetSurfaceSample(.79f,.88f,.87f),.9);
                    break;
                case PlanetSurfaceKind.Arid:
                    color=Mix(new PlanetSurfaceSample(.30f,.12f,.065f),new PlanetSurfaceSample(.80f,.51f,.24f),height*.7+detail*.3);break;
                case PlanetSurfaceKind.Frozen:
                    color=Mix(new PlanetSurfaceSample(.12f,.29f,.40f),new PlanetSurfaceSample(.83f,.91f,.92f),height*.75+detail*.25);
                    if(Math.Abs(Fbm(x*6,y*6,z*6,seed+79)-.5)<.022)color=Mix(color,new PlanetSurfaceSample(.08f,.21f,.29f),.65);
                    break;
                case PlanetSurfaceKind.Volcanic:
                    var lava=Math.Abs(Fbm(x*5,y*5,z*5,seed+37)-.5)<.035;
                    color=lava?new PlanetSurfaceSample(.95f,.22f,.025f):Mix(new PlanetSurfaceSample(.065f,.055f,.055f),new PlanetSurfaceSample(.24f,.16f,.12f),detail);break;
                case PlanetSurfaceKind.GasGiant:
                    var bands=(Math.Sin(y*65+height*8)+1)*.5;
                    color=Mix(new PlanetSurfaceSample(.34f,.23f,.18f),new PlanetSurfaceSample(.88f,.71f,.47f),bands*.6+detail*.4);break;
                case PlanetSurfaceKind.Airless:
                    color=Mix(new PlanetSurfaceSample(.14f,.15f,.16f),new PlanetSurfaceSample(.50f,.47f,.43f),height*.4+detail*.6);
                    color=Crater(p,x,y,z,color);break;
                default:color=Mix(new PlanetSurfaceSample(.14f,.19f,.25f),new PlanetSurfaceSample(.29f,.36f,.43f),height);break;
            }
            return new PlanetSurfaceSample(color.R*variation,color.G*variation,color.B*variation,color.Water);
        }
        public static float Clouds(PlanetVisualProfile p,double x,double y,double z)
        {
            if(p.CloudCoverage<=0)return 0;
            var n=Fbm(x*4+2,y*5,z*4-1,p.Seed+199);
            return (float)Math.Max(0,Math.Min(1,(n-(.75-p.CloudCoverage*.45))*7));
        }
        private static PlanetSurfaceSample Crater(PlanetVisualProfile p,double x,double y,double z,PlanetSurfaceSample c)
        {
            for(uint i=0;i<25;i++)
            {
                var lat=Hash((int)i,9,3,p.Seed)*2-1;var angle=Hash((int)i,17,3,p.Seed)*Math.PI*2;
                var r=Math.Sqrt(1-lat*lat);var d=1-(x*r*Math.Cos(angle)+y*lat+z*r*Math.Sin(angle));
                var size=.0015+Hash((int)i,8,1,p.Seed)*.014;
                if(d<size)c=Mix(c,new PlanetSurfaceSample(.08f,.09f,.105f),(1-d/size)*.55);
                else if(d<size*1.2)c=Mix(c,new PlanetSurfaceSample(.62f,.60f,.57f),.24);
            }
            return c;
        }
        private static PlanetSurfaceSample Mix(PlanetSurfaceSample a,PlanetSurfaceSample b,double t)
        {var f=(float)Math.Max(0,Math.Min(1,t));return new PlanetSurfaceSample(a.R+(b.R-a.R)*f,a.G+(b.G-a.G)*f,a.B+(b.B-a.B)*f,a.Water+(b.Water-a.Water)*f);}
        private static double Fbm(double x,double y,double z,uint seed)
        {double result=0,weight=.55,total=0;for(int i=0;i<5;i++){result+=Noise(x,y,z,seed+(uint)i*137)*weight;total+=weight;x=x*2.03+9.1;y=y*2.03+1.7;z=z*2.03-4.8;weight*=.48;}return result/total;}
        private static double Noise(double x,double y,double z,uint seed)
        {
            var ix=(int)Math.Floor(x);var iy=(int)Math.Floor(y);var iz=(int)Math.Floor(z);
            x-=ix;y-=iy;z-=iz;x=x*x*(3-2*x);y=y*y*(3-2*y);z=z*z*(3-2*z);
            double sum=0;for(int a=0;a<2;a++)for(int b=0;b<2;b++)for(int c=0;c<2;c++)
                sum+=Hash(ix+a,iy+b,iz+c,seed)*(a==0?1-x:x)*(b==0?1-y:y)*(c==0?1-z:z);
            return sum;
        }
        private static double Hash(int x,int y,int z,uint seed)
        {unchecked{uint n=seed^(uint)x*374761393U^(uint)y*668265263U^(uint)z*2147483647U;n=(n^(n>>13))*1274126177U;return (n^(n>>16))/(double)uint.MaxValue;}}
    }
}
