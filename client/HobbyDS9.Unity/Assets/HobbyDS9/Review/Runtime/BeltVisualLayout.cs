#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    // Actor-filtered input only. Display radii and scatter have no simulation authority.
    public sealed class BeltVisualSlot
    {
        public SystemAsteroidBeltSnapshot Feature {get;}
        public double Inner {get;} public double Outer {get;} public double Envelope=>Outer+.0208*(Inner+Outer)/2;
        internal BeltVisualSlot(SystemAsteroidBeltSnapshot feature){Feature=feature;Inner=12+feature.InnerRadiusMilliAu*.004;Outer=12+feature.OuterRadiusMilliAu*.004;}
    }
    public sealed class BeltRockPlacement
    {
        public double Turns {get;} public double Radial {get;} public double Height {get;} public double Radius {get;}
        public int Prototype {get;} public double Rx {get;} public double Ry {get;} public double Rz {get;}
        public double Sx {get;} public double Sy {get;} public double Sz {get;} public double Tone {get;}
        internal BeltRockPlacement(int index,double[] a)
        {
            var turns=index*.6180339887498949+.12*(a[0]-.5);Turns=turns-Math.Floor(turns);Radial=(a[1]+a[2])/2;
            Height=.012*(a[3]-.5);Radius=.006+.010*a[4]*a[4];Prototype=Math.Min(3,(int)(a[5]*4));
            Rx=2*Math.PI*a[6];Ry=2*Math.PI*a[7];Rz=2*Math.PI*a[8];Sx=.8+.5*a[9];Sy=.7+.4*a[10];Sz=.75+.35*a[2];Tone=.68+.28*a[10];
        }
    }
    public static class BeltVisualLayout
    {
        public const int MaximumFeatures=8,MaximumRocks=384;
        public static IReadOnlyList<BeltVisualSlot> ForSystem(CampaignStaticAstronomySnapshot? sky,string actor,int turn,long revision,string system)
        {
            if(sky==null||sky.ActorId!=actor||sky.ObservationTurn!=turn||sky.CampaignRevision!=revision||sky.SystemBeltGeneration!=CampaignSystemRichnessIdentity.Generation)return Array.Empty<BeltVisualSlot>();
            var features=sky.SystemBelts.Where(b=>b.SystemId==system).OrderBy(b=>b.FeatureId,StringComparer.Ordinal).ToArray();
            if(features.Length>MaximumFeatures)throw new ArgumentException("Disclosed belt rendering limit exceeded; use the feature list.");
            foreach(var f in features)
                if(!EntityId.FromAuthored(f.SystemId).IsValid||!EntityId.FromAuthored(f.FeatureId).IsValid||f.InnerRadiusMilliAu<2200||f.InnerRadiusMilliAu>4199||f.OuterRadiusMilliAu-f.InnerRadiusMilliAu<300||f.OuterRadiusMilliAu-f.InnerRadiusMilliAu>999||f.VisualSeed.Length!=64||f.VisualSeed.Any(c=>!(c>='0'&&c<='9'||c>='a'&&c<='f')))throw new ArgumentException("Invalid disclosed belt visual contract.");
            return features.Select(f=>new BeltVisualSlot(f)).ToList().AsReadOnly();
        }
        public static uint Seed(string system,string feature,string? persistedSeed=null)
        {
            if(system.Contains("\n")||feature.Contains("\n")||persistedSeed?.Contains("\n")==true)throw new ArgumentException("Ambiguous belt seed.");
            uint state=2166136261;foreach(var b in Encoding.UTF8.GetBytes("belt-v1\n"+system+"\n"+feature+(persistedSeed==null?"":"\n"+persistedSeed)))state=unchecked((state^b)*16777619);
            return state==0?1:state;
        }
        public static IReadOnlyList<BeltRockPlacement> Rocks(uint state,int count)
        {
            if(count<0||count>MaximumRocks)throw new ArgumentOutOfRangeException(nameof(count));if(state==0)state=1;
            var result=new List<BeltRockPlacement>();
            for(var i=0;i<count;i++)
            {
                var a=new double[11];for(var j=0;j<a.Length;j++){state^=state<<13;state^=state>>17;state^=state<<5;a[j]=(state>>8)/16777216.0;}
                result.Add(new BeltRockPlacement(i,a));
            }
            return result.AsReadOnly();
        }
        public static int RockBudget(int index,int features,bool low)
        {
            if(features<1||features>MaximumFeatures||index<0||index>=features)throw new ArgumentOutOfRangeException();
            var total=low?120:features==1?320:MaximumRocks;return total/features+(index<total%features?1:0);
        }
        public static int Detail(double pixels,int previous)
        {
            if(previous==0&&pixels>=187||previous==1&&pixels>=76.5&&pixels<=253||previous==2&&pixels<=103.5)return previous;
            return pixels>220?0:pixels>=90?1:2;
        }
    }
}
