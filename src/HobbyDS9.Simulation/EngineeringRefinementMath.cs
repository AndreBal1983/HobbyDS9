using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
namespace HobbyDS9.Simulation
{
    // Versioned provisional integer policy. No campaign RNG, I/O or live catalogue mutation.
    internal static class EngineeringRefinementMath
    {
        internal sealed class Candidate
        {
            internal readonly ComponentVersionDto Component;internal readonly EngineeringRefinementGoal? Downside;internal int Weight;
            internal Candidate(ComponentVersionDto c,EngineeringRefinementGoal? downside,int weight){Component=c;Downside=downside;Weight=weight;}
        }
        internal sealed class Options
        {
            internal readonly List<Candidate> Clean=new List<Candidate>(),Mixed=new List<Candidate>();
            internal bool Feasible=>Clean.Count>0&&Mixed.Count>0;
        }
        internal static (int Numerator,int Denominator) Factor(int maturity)
            =>maturity<0||maturity>=150000?(0,1):maturity<=100000?(1,1):(150000-maturity,50000);
        internal static int MaturityGain(int maturity)
        {var f=Factor(maturity);return 5000*f.Numerator/f.Denominator;}
        internal static long Value(ComponentVersionDto c,EngineeringRefinementGoal goal)
            =>goal==EngineeringRefinementGoal.ReduceCapacity?c.Capacity:goal==EngineeringRefinementGoal.IncreasePowerSupply?c.PowerSupply:c.Cost!.Industry;
        private static void Set(ComponentVersionDto c,EngineeringRefinementGoal goal,long value)
        {if(goal==EngineeringRefinementGoal.ReduceCapacity)c.Capacity=value;else if(goal==EngineeringRefinementGoal.IncreasePowerSupply)c.PowerSupply=value;else c.Cost!.Industry=value;}
        private static IEnumerable<(long Delta,int Weight)> Quanta(long value,int maturity,bool industryTarget,bool downside)
        {
            var f=Factor(maturity);if(f.Numerator==0||value<=0)yield break;
            var points=downside?new[]{200,400,600,1000,1500}:industryTarget?new[]{1000,1250,1500,2000,2500}:new[]{800,900,1000,1100,1200};
            var weights=new[]{1,2,3,2,1};var scale=(BigInteger)10000*f.Denominator;
            var low=((BigInteger)value*points[0]*f.Numerator+scale-1)/scale;
            var high=(BigInteger)value*points[4]*f.Numerator/scale;
            var choices=new SortedDictionary<long,int>();
            for(int i=0;i<points.Length;i++)
            {
                var delta=(BigInteger)value*points[i]*f.Numerator/scale;
                if(delta<low||delta>high||delta<=0||delta>long.MaxValue)continue;
                var d=(long)delta;choices[d]=choices.TryGetValue(d,out var w)?w+weights[i]:weights[i];
            }
            foreach(var c in choices)yield return(c.Key,c.Value);
        }
        private static bool Envelope(ComponentVersionDto c,ComponentVersionDto root)
            =>c.Capacity>=Math.Max(1,(root.Capacity+1)/2)&&c.Capacity<=checked(root.Capacity*2)
                &&c.PowerSupply>=Math.Max(1,(root.PowerSupply+1)/2)&&c.PowerSupply<=checked(root.PowerSupply*2)
                &&c.Cost!.Industry>=Math.Max(1,(root.Cost!.Industry+1)/2)&&c.Cost.Industry<=checked(root.Cost.Industry*2);
        private static ShipDesignPreview Evaluate(EngineeringRefinementProjectDto p,ComponentVersionDto vector)
        {
            var catalog=BlueprintCopies.Copy(p.Context!.FrozenCatalogue!);
            var install=p.Context.Fit!.Installations!.Single(i=>i.InstallationId==p.InstallationId);
            var component=catalog.Components!.Single(c=>c.VersionId==install.ComponentVersionId);
            // Counterfactual evaluation only: retain the frozen installation identity and every other field.
            component.Capacity=vector.Capacity;component.PowerSupply=vector.PowerSupply;component.Cost!.Industry=vector.Cost!.Industry;
            return CampaignBlueprints.Compiler(catalog).Preview(p.Context.Fit,CampaignBlueprints.FrozenAccess(catalog));
        }
        private static bool Useful(EngineeringRefinementProjectDto p,ComponentVersionDto c,ShipDesignPreview before)
        {
            if(!Envelope(c,p.GenerationRoot!))return false;
            var after=Evaluate(p,c);if(!after.IsValid||after.Cost==null)return false;
            foreach(var prior in before.Profiles)
            {
                var next=after.Profiles.Single(x=>x.Kind==prior.Kind);
                if(next.AvailablePower<prior.AllocatedPower)return false;
                if(prior.Outputs.Any(o=>next.Output(o.Kind)<o.Amount))return false;
            }
            return p.Goal==EngineeringRefinementGoal.ReduceCapacity?after.CapacityUsed<before.CapacityUsed:
                p.Goal==EngineeringRefinementGoal.ReduceIndustry?after.Cost.Industry<before.Cost!.Industry:
                after.Profiles.Any(x=>x.AvailablePower>before.Profiles.Single(b=>b.Kind==x.Kind).AvailablePower);
        }
        internal static Options Enumerate(EngineeringRefinementProjectDto p)
        {
            var result=new Options();if(p.Before?.Kind!=ComponentKind.Reactor||p.GenerationRoot==null||p.Context==null
                ||!Enum.IsDefined(typeof(EngineeringRefinementGoal),p.Goal)||MaturityGain(p.ParentMaturityMilli)<100)return result;
            var before=Evaluate(p,p.Before);if(!before.IsValid)return result;
            foreach(var gain in Quanta(Value(p.Before,p.Goal),p.ParentMaturityMilli,p.Goal==EngineeringRefinementGoal.ReduceIndustry,false))
            {
                var improved=BlueprintCopies.Copy(p.Before);Set(improved,p.Goal,checked(Value(improved,p.Goal)+(p.Goal==EngineeringRefinementGoal.IncreasePowerSupply?gain.Delta:-gain.Delta)));
                if(Useful(p,improved,before))result.Clean.Add(new Candidate(improved,null,gain.Weight));
                foreach(var other in Enum.GetValues(typeof(EngineeringRefinementGoal)).Cast<EngineeringRefinementGoal>().Where(g=>g!=p.Goal))
                foreach(var loss in Quanta(Value(p.Before,other),p.ParentMaturityMilli,false,true))
                {
                    var mixed=BlueprintCopies.Copy(improved);Set(mixed,other,checked(Value(mixed,other)+(other==EngineeringRefinementGoal.IncreasePowerSupply?-loss.Delta:loss.Delta)));
                    if(Useful(p,mixed,before))result.Mixed.Add(new Candidate(mixed,other,gain.Weight*loss.Weight));
                }
            }
            return result;
        }
        internal static Candidate Resolve(EngineeringRefinementProjectDto p,string seed,out string draw)
        {
            var options=Enumerate(p);if(!options.Feasible)throw new InvalidOperationException("No representable contextual refinement outcomes.");
            draw=CampaignActivitiesData.Identity(EngineeringRefinementIdentity.Policy,seed,p.ActorId,p.ProjectId,p.ParentVersionId,
                ((int)p.Goal).ToString(CultureInfo.InvariantCulture),p.Context!.SourceContentHash,p.Context.BlueprintId,p.Context.Revision.ToString(CultureInfo.InvariantCulture));
            var rng=new SplitMix64Random(ulong.Parse(draw.Substring(0,16),NumberStyles.HexNumber,CultureInfo.InvariantCulture));
            var selected=rng.NextBelow(100)<70?options.Clean:options.Mixed;
            var pick=rng.NextBelow((ulong)selected.Sum(c=>c.Weight));
            foreach(var c in selected){if(pick<(ulong)c.Weight)return c;pick-=(ulong)c.Weight;}
            throw new InvalidOperationException("Closed refinement outcome distribution.");
        }
    }
}
