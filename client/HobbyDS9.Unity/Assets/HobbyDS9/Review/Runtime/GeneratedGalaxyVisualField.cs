#nullable enable
using System;

namespace HobbyDS9.UnityReview
{
    // Cosmetic version/seed only: never a generated-map field or campaign RNG stream.
    public static class GeneratedGalaxyVisualField
    {
        public const int Version=1,Resolution=48,MaximumClouds=8,RaySamples=32;
        public const double CameraDistance=4,HalfDepth=.32;
        public static uint Seed(string galaxySeed,string featureId)
        {
            uint h=2166136261;
            foreach(char c in "galaxy-visual-1/"+galaxySeed+"/"+featureId)h=unchecked((h^c)*16777619);
            return h;
        }
        public static double Guard(double x,double y,double z)
        {
            double Fade(double a){a=Math.Max(0,Math.Min(1,(1-Math.Abs(a))/.18));return a*a*(3-2*a);}
            return Fade(x)*Fade(y)*Fade(z);
        }
        public static double Perspective(double z)=>CameraDistance/(CameraDistance-Math.Max(-HalfDepth,Math.Min(HalfDepth,z)));
        public static double Noise(double x,double y,double z,uint seed)
        {
            int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y),iz=(int)Math.Floor(z);
            double a=x-ix,b=y-iy,c=z-iz;a=a*a*(3-2*a);b=b*b*(3-2*b);c=c*c*(3-2*c);
            double H(int dx,int dy,int dz)
            {
                uint h=unchecked(seed^(uint)(ix+dx)*374761393u^(uint)(iy+dy)*668265263u^(uint)(iz+dz)*2246822519u);
                h=unchecked((h^(h>>13))*1274126177u);return (h^(h>>16))/4294967295.0;
            }
            double L(double p,double q,double t)=>p+(q-p)*t;
            return L(L(L(H(0,0,0),H(1,0,0),a),L(H(0,1,0),H(1,1,0),a),b),
                L(L(H(0,0,1),H(1,0,1),a),L(H(0,1,1),H(1,1,1),a),b),c);
        }
        // Continuous asymmetric lobes/ribbons and cavities. All six faces go to zero,
        // including the depth end caps that remained nonzero in the browser proof.
        public static double Density(double x,double y,double z,uint seed)
        {
            if(Math.Abs(x)>=1||Math.Abs(y)>=1||Math.Abs(z)>=1)return 0;
            double n=Noise(x*2.8+5,y*2.8+7,z*2.8+3,seed);
            double fine=Noise(x*8+2,y*8+4,z*8+6,seed^7919);
            double wx=x+.20*Math.Sin(3*y+2*z),wy=y+.17*Math.Sin(4*x-z);
            double lobe=Math.Exp(-5*((wx+.26)*(wx+.26)+.8*(wy-.11)*(wy-.11)+1.4*z*z));
            lobe+=.7*Math.Exp(-9*((wx-.43)*(wx-.43)+1.6*(wy+.22)*(wy+.22)+z*z));
            double ribbon=Math.Exp(-55*Math.Pow(wy-.27*Math.Sin(4*wx+z),2)-2.5*wx*wx-3*z*z);
            double cavity=Math.Exp(-35*((x-.06)*(x-.06)+(y+.03)*(y+.03)+z*z));
            return Math.Max(0,Math.Min(1,(.55*lobe+.55*ribbon-.55*cavity)*(.3+.9*n+.25*fine)))*Guard(x,y,z);
        }
        public static (int width,int height) TargetSize(double width,double height)
        {
            if(double.IsNaN(width)||double.IsInfinity(width)||double.IsNaN(height)||double.IsInfinity(height)||width<=0||height<=0)return (1,1);
            double scale=Math.Min(.5,Math.Min(960/width,540/height));
            return (Math.Max(1,(int)Math.Ceiling(width*scale)),Math.Max(1,(int)Math.Ceiling(height*scale)));
        }
    }
}
