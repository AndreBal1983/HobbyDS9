#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
namespace HobbyDS9.UnityReview
{
    // Desired local inputs only; authoritative spending/progress/eligibility remain in Application.
    public sealed class ReviewResearchDraft
    {
        private readonly ReviewFrame observed;
        private readonly int[] original,weights;
        private readonly bool[] locked;
        private readonly int[] lockedDisplay;
        private readonly int originalSplit;
        public int ScienceUnits { get; set; }
        public bool SplitChanged=>ScienceUnits!=originalSplit;
        public bool ScienceChanged=>!weights.SequenceEqual(original);
        public bool Dirty=>SplitChanged||ScienceChanged;
        public ReviewResearchDraft(ReviewFrame frame)
        {
            observed=frame;var research=frame.Strategy!.Research;
            ScienceUnits=originalSplit=research.ScienceAllocationUnits;
            original=research.Science.OrderBy(s=>s.Discipline).Select(s=>s.AllocationWeight).ToArray();weights=(int[])original.Clone();locked=new bool[weights.Length];lockedDisplay=new int[weights.Length];
        }
        public bool Current(ReviewFrame frame)=>frame.Actor==observed.Actor&&frame.Generation==observed.Generation&&frame.Status.Turn==observed.Status.Turn&&frame.Status.CampaignRevision==observed.Status.CampaignRevision;
        public int Weight(ScienceDiscipline discipline)=>weights[(int)discipline-1];
        public bool IsLocked(ScienceDiscipline discipline)=>locked[(int)discipline-1];
        public void SetLocked(ScienceDiscipline discipline,bool value)
        {
            var index=(int)discipline-1;
            if(value&&!locked[index])lockedDisplay[index]=DisplayPercent(discipline);
            locked[index]=value;
        }
        public bool CanAdjust(ScienceDiscipline discipline)=>!IsLocked(discipline)&&locked.Count(v=>!v)>1;
        // Only the local complete-plan draft is redistributed. Simulation still validates the submitted plan.
        public void Adjust(ScienceDiscipline discipline,int delta)
            =>SetWeight(discipline,(int)Math.Max(0,Math.Min(ResearchUnits.Scale,(long)Weight(discipline)+delta)));
        public void SetWeight(ScienceDiscipline discipline,int desired)
        {
            var index=(int)discipline-1;if(index<0||index>=weights.Length||!CanAdjust(discipline))return;
            var others=Enumerable.Range(0,weights.Length).Where(i=>i!=index&&!locked[i]).ToArray();
            var available=ResearchUnits.Scale-Enumerable.Range(0,weights.Length).Where(i=>locked[i]).Sum(i=>weights[i]);
            desired=Math.Max(0,Math.Min(available,desired));
            if(desired==weights[index])return;
            var redistributed=Apportion(others.Select(i=>weights[i]).ToArray(),available-desired);
            weights[index]=desired;
            for(int i=0;i<others.Length;i++)weights[others[i]]=redistributed[i];
        }
        public void Equalize()
        {
            var indices=Enumerable.Range(0,weights.Length).Where(i=>!locked[i]).ToArray();
            if(indices.Length<2)return;
            var shares=Apportion(indices.Select(_=>1).ToArray(),indices.Sum(i=>weights[i]));
            for(int i=0;i<indices.Length;i++)weights[indices[i]]=shares[i];
        }
        // Largest remainder, stable discipline order for ties. Display rounding never changes authoritative units.
        public int DisplayHundredths(ScienceDiscipline discipline)=>Apportion(weights,10000)[(int)discipline-1];
        public int DisplayPercent(ScienceDiscipline discipline)
        {
            var index=(int)discipline-1;
            if(locked[index])return lockedDisplay[index];
            var indices=Enumerable.Range(0,weights.Length).Where(i=>!locked[i]).ToArray();
            var budget=100-Enumerable.Range(0,weights.Length).Where(i=>locked[i]).Sum(i=>lockedDisplay[i]);
            return Apportion(indices.Select(i=>weights[i]).ToArray(),budget)[Array.IndexOf(indices,index)];
        }
        public string PercentLabel(ScienceDiscipline discipline)=>DisplayPercent(discipline).ToString(System.Globalization.CultureInfo.InvariantCulture)+"%";
        public void ResetScience()
        {
            Array.Copy(original,weights,weights.Length);
            var display=Apportion(weights,100);
            Array.Copy(display,lockedDisplay,display.Length);
        }
        private static int[] Apportion(int[] shares,int total)
        {
            var sum=shares.Sum();
            if(sum==0){shares=shares.Select(_=>1).ToArray();sum=shares.Length;}
            var result=shares.Select(v=>(int)((long)v*total/sum)).ToArray();
            var remainder=total-result.Sum();
            foreach(var index in Enumerable.Range(0,shares.Length).OrderByDescending(i=>(long)shares[i]*total%sum).ThenBy(i=>i).Take(remainder))result[index]++;
            return result;
        }
        public IReadOnlyList<ScienceAllocationDto> Values()=>weights.Select((w,i)=>new ScienceAllocationDto{Discipline=(ScienceDiscipline)(i+1),Weight=w}).ToList().AsReadOnly();
        public ReviewResearchDraft Acknowledge(ReviewFrame frame,CommandKind submitted)
        {
            var next=new ReviewResearchDraft(frame);
            if(submitted!=CommandKind.SetResearchAllocation)next.ScienceUnits=ScienceUnits;
            if(submitted!=CommandKind.SetScienceAllocation)Array.Copy(weights,next.weights,weights.Length);
            Array.Copy(locked,next.locked,locked.Length);
            Array.Copy(lockedDisplay,next.lockedDisplay,lockedDisplay.Length);
            return next;
        }
    }
}
