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
        private readonly int originalSplit;
        public int ScienceUnits { get; set; }
        public bool SplitChanged=>ScienceUnits!=originalSplit;
        public bool ScienceChanged=>!weights.SequenceEqual(original);
        public bool Dirty=>SplitChanged||ScienceChanged;
        public ReviewResearchDraft(ReviewFrame frame)
        {
            observed=frame;var research=frame.Strategy!.Research;
            ScienceUnits=originalSplit=research.ScienceAllocationUnits;
            original=research.Science.OrderBy(s=>s.Discipline).Select(s=>s.AllocationWeight).ToArray();weights=(int[])original.Clone();
        }
        public bool Current(ReviewFrame frame)=>frame.Actor==observed.Actor&&frame.Generation==observed.Generation&&frame.Status.Turn==observed.Status.Turn&&frame.Status.CampaignRevision==observed.Status.CampaignRevision;
        public int Weight(ScienceDiscipline discipline)=>weights[(int)discipline-1];
        public void Adjust(ScienceDiscipline discipline,int delta)
        {
            var index=(int)discipline-1;if(index<0||index>=weights.Length)return;
            var desired=Math.Max(0,Math.Min(ResearchUnits.Scale,weights[index]+delta));delta=desired-weights[index];
            while(delta!=0)for(int i=0;i<weights.Length&&delta!=0;i++)
            {
                if(i==index)continue;
                if(delta>0&&weights[i]>0){weights[i]--;weights[index]++;delta--;}
                else if(delta<0&&weights[i]<ResearchUnits.Scale){weights[i]++;weights[index]--;delta++;}
            }
        }
        public void Equalize(){for(int i=0;i<weights.Length;i++)weights[i]=ResearchUnits.Scale/weights.Length;}
        public IReadOnlyList<ScienceAllocationDto> Values()=>weights.Select((w,i)=>new ScienceAllocationDto{Discipline=(ScienceDiscipline)(i+1),Weight=w}).ToList().AsReadOnly();
        public ReviewResearchDraft Acknowledge(ReviewFrame frame,CommandKind submitted)
        {
            var next=new ReviewResearchDraft(frame);
            if(submitted!=CommandKind.SetResearchAllocation)next.ScienceUnits=ScienceUnits;
            if(submitted!=CommandKind.SetScienceAllocation)Array.Copy(weights,next.weights,weights.Length);
            return next;
        }
    }
}
