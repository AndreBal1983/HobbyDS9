#nullable enable
using System;
using System.Collections.Generic;

namespace HobbyDS9.UnityReview
{
    // Cosmetic metric only; never supplies navigation geometry or travel costs.
    public static class GeneratedGalaxyV3Math
    {
        public const double DiskScale=.47,CameraDistance=4,LocalDepthScale=.8;
        public static IReadOnlyList<double> BinEdges { get; }=Array.AsReadOnly(new double[]{-.33,-.165,-.105,-.054,-.0525,-.027,0,.027,.0525,.054,.105,.165,.33});
        private static readonly double C=Math.Cos(48*Math.PI/180),S=Math.Sin(48*Math.PI/180),R=Math.Cos(32*Math.PI/180),T=Math.Sin(32*Math.PI/180);
        public static (double x,double y,double z) DiskToWorld(double u,double v,double z)
        {double a=DiskScale*u,b=DiskScale*(v*C-z*S);return(R*a-T*b,T*a+R*b,DiskScale*(v*S+z*C));}
        public static (double u,double v,double z) WorldToDisk(double x,double y,double z)
        {double a=(R*x+T*y)/DiskScale,b=(-T*x+R*y)/DiskScale,c=z/DiskScale;return(a,C*b+S*c,-S*b+C*c);}
        public static (double x,double y) Project(double x,double y,double z,double cameraX,double cameraY,bool depth)
        {double q=depth?CameraDistance/(CameraDistance-z):1;return(cameraX+(x-cameraX)*q,cameraY+(y-cameraY)*q);}
        public static (double x,double y,double z) Ray(double screenPlaneX,double screenPlaneY,double z,double cameraX,double cameraY,bool depth)
        {double q=depth?1-z/CameraDistance:1;return(cameraX+(screenPlaneX-cameraX)*q,cameraY+(screenPlaneY-cameraY)*q,z);}
        // Abramowitz/Stegun error-function approximation; the analytic CDF weights
        // are evaluated numerically (max erf approximation error about1.5e-7).
        private static double Erf(double x)
        {double sign=x<0?-1:1,a=Math.Abs(x),t=1/(1+.3275911*a);return sign*(1-(((((1.061405429*t-1.453152027)*t)+1.421413741)*t-.284496736)*t+.254829592)*t*Math.Exp(-a*a));}
        public static double Cdf(double z,double h)
        {if(h<=0)throw new ArgumentOutOfRangeException(nameof(h));return .5*(1+Erf(Math.Max(-3,Math.Min(3,z/h)))/Erf(3));}
        public static double Weight(double lo,double hi,double h,double mu)
        {if(mu<=0||double.IsNaN(mu))throw new ArgumentOutOfRangeException(nameof(mu));return Math.Max(0,Cdf(hi,h)-Cdf(lo,h))/mu;}
        public static double SourceRatio(double tau)=>tau<1e-5?1-tau/2+tau*tau/6:(1-Math.Exp(-tau))/tau;
        public static uint Variant(string featureId)
        {uint h=2166136261;foreach(char c in featureId+"/galaxy-nebula-detail-v3"){h^=c;h*=16777619;}return h;}
    }
}
