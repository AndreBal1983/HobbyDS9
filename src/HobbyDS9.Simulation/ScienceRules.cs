using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    public sealed class ScienceBreakpoint
    {
        public int Ordinal { get; }
        public long ChunkCost { get; }
        public long Threshold { get; }
        internal ScienceBreakpoint(int ordinal,long chunk,long threshold){Ordinal=ordinal;ChunkCost=chunk;Threshold=threshold;}
    }
    // Portable pure functions. Discovery uses an isolated stream, never campaign RNG.
    public static class ScienceRules
    {
        private static readonly long[] Costs = {100,130,170,220,290,380,500,650,850};
        public static bool ValidPace(int pace) => pace==5000||pace==6700||pace==10000||pace==15000||pace==30000;
        public static long AdjustCost(long cost,int pace)
        { if(cost<=0||!ValidPace(pace))throw new ArgumentOutOfRangeException(nameof(cost));return checked((long)BigInteger.Max(1,((BigInteger)cost*pace+9999)/10000)); }
        public static long IntervalCost(int level,int pace)
        { if(level<1||level>9)throw new ArgumentOutOfRangeException(nameof(level));return AdjustCost(Costs[level-1],pace); }
        public static bool ValidDiscipline(ScienceDiscipline discipline) => (int)discipline>=1&&(int)discipline<=6;
        public static ulong DiscoverySeed(ulong campaignStartSeed,string civilizationId)
        {
            _ = new EntityId(civilizationId);
            // FNV-1a over canonical ASCII IDs, mixed with the initial campaign seed.
            ulong hash=14695981039346656037UL;
            unchecked { foreach(var c in civilizationId){hash^=(byte)c;hash*=1099511628211UL;} }
            return new SplitMix64Random(hash^campaignStartSeed).NextUInt64();
        }
        public static IReadOnlyList<ScienceBreakpoint> Schedule(ulong seed,ScienceDiscipline discipline,int level,int pace)
        {
            if(!ValidDiscipline(discipline))throw new ArgumentOutOfRangeException(nameof(discipline));
            var cost=IntervalCost(level,pace);
            var random=new SplitMix64Random(unchecked(seed^((ulong)discipline*0xd6e8feb86659fd93UL)^((ulong)level*0xa0761d6478bd642fUL)));
            int count=3+(int)random.NextBelow(3);var ordinals=Enumerable.Range(1,count).ToArray();
            for(int i=count-1;i>0;i--){int j=(int)random.NextBelow((ulong)(i+1));int t=ordinals[i];ordinals[i]=ordinals[j];ordinals[j]=t;}
            var weights=new int[count];for(int i=0;i<count;i++){int delta=10+(int)random.NextBelow(6);weights[i]=100+(random.NextBelow(2)==0?-delta:delta);}
            int denominator=weights.Sum();var shares=new long[count];var remainders=new BigInteger[count];long assigned=0;
            for(int i=0;i<count;i++){var numerator=(BigInteger)cost*weights[i];shares[i]=(long)BigInteger.DivRem(numerator,denominator,out remainders[i]);assigned=checked(assigned+shares[i]);}
            foreach(var i in Enumerable.Range(0,count).OrderByDescending(i=>remainders[i]).ThenBy(i=>i).Take(checked((int)(cost-assigned))))shares[i]++;
            long threshold=0;var result=new List<ScienceBreakpoint>();for(int i=0;i<count;i++){threshold=checked(threshold+shares[i]);result.Add(new ScienceBreakpoint(ordinals[i],shares[i],threshold));}
            return result.AsReadOnly();
        }
        public static void Allocate(ScienceEngineeringStateDto research,long output)
        {
            if(output<0||research.ScienceAllocationBps<0||research.ScienceAllocationBps>10000)throw new ArgumentOutOfRangeException(nameof(output));
            long science=(long)((BigInteger)output*research.ScienceAllocationBps/10000);long engineering=output-science;
            checked { research.ScienceReserve+=science;research.EngineeringReserve+=engineering;
                research.Statistics!.TotalResearchGenerated+=output;research.Statistics.TotalScienceAllocated+=science;research.Statistics.TotalEngineeringAllocated+=engineering; }
        }
        public static void SpendScience(ScienceEngineeringStateDto research,int pace,string civilizationId,List<ResearchReceiptDto> receipts)
        {
            if(research.ScienceFocus==null)return;
            var focus=research.Disciplines!.Single(d=>d.Discipline==research.ScienceFocus);
            while(focus.Level<10&&research.ScienceReserve>0)
            {
                long cost=IntervalCost(focus.Level,pace);long spent=Math.Min(research.ScienceReserve,cost-focus.ProgressTowardNextLevel);
                long before=focus.ProgressTowardNextLevel;focus.ProgressTowardNextLevel=checked(before+spent);research.ScienceReserve-=spent;
                research.Statistics!.TotalScienceSpent=checked(research.Statistics.TotalScienceSpent+spent);
                receipts.Add(new ResearchReceiptDto{CivilizationId=civilizationId,Category="science-spent",Discipline=focus.Discipline,IntervalStartLevel=focus.Level,Amount=spent});
                foreach(var point in Schedule(research.ResearchDiscoverySeed!.Value,focus.Discipline,focus.Level,pace))
                    if(point.Threshold>before&&point.Threshold<=focus.ProgressTowardNextLevel)
                    {research.Statistics.TotalBreakthroughs=checked(research.Statistics.TotalBreakthroughs+1);receipts.Add(new ResearchReceiptDto{CivilizationId=civilizationId,Category="science-breakthrough",Discipline=focus.Discipline,IntervalStartLevel=focus.Level,BreakthroughOrdinal=point.Ordinal,Amount=point.Threshold});}
                if(focus.ProgressTowardNextLevel<cost)break;
                focus.ProgressTowardNextLevel=0;focus.Level++;research.Statistics.TotalScienceLevelsCompleted=checked(research.Statistics.TotalScienceLevelsCompleted+1);
                receipts.Add(new ResearchReceiptDto{CivilizationId=civilizationId,Category="science-level",Discipline=focus.Discipline,IntervalStartLevel=focus.Level-1,Amount=focus.Level});
            }
        }
        public static IReadOnlyList<string> Validate(ScienceEngineeringStateDto? state,int pace)
        {
            var errors=new List<string>();if(state==null){errors.Add("MissingResearch");return errors.AsReadOnly();}
            if(!ValidPace(pace))errors.Add("InvalidPace");
            if(!state.ResearchDiscoverySeed.HasValue)errors.Add("MissingDiscoverySeed");
            if(state.ScienceAllocationBps<0||state.ScienceAllocationBps>10000||state.ScienceReserve<0||state.EngineeringReserve<0)errors.Add("InvalidReserveOrAllocation");
            if(state.ScienceFocus.HasValue&&!ValidDiscipline(state.ScienceFocus.Value))errors.Add("InvalidFocus");
            if(state.Disciplines==null||state.Disciplines.Count!=6||state.Disciplines.Any(d=>d==null)||state.Disciplines.Where(d=>d!=null).Select(d=>d.Discipline).Distinct().Count()!=6)errors.Add("MissingOrDuplicateDisciplines");
            if(state.Disciplines!=null)foreach(var d in state.Disciplines.Where(d=>d!=null))
                if(!ValidDiscipline(d.Discipline)||d.Level<1||d.Level>10||d.ProgressTowardNextLevel<0||(d.Level==10&&d.ProgressTowardNextLevel!=0)||(d.Level<10&&d.Level>=1&&ValidPace(pace)&&d.ProgressTowardNextLevel>=IntervalCost(d.Level,pace)))errors.Add("InvalidDisciplineState");
            if(state.Statistics==null||state.EngineeringProgress==null)errors.Add("MissingResearchCollections");
            if(state.Statistics!=null){var s=state.Statistics;if(new[]{s.TotalResearchGenerated,s.TotalScienceAllocated,s.TotalEngineeringAllocated,s.TotalScienceSpent,s.TotalEngineeringSpent,s.TotalBreakthroughs,s.TotalScienceLevelsCompleted,s.TotalEngineeringApplicationsMatured}.Any(v=>v<0)||(BigInteger)s.TotalScienceAllocated+s.TotalEngineeringAllocated!=s.TotalResearchGenerated)errors.Add("InvalidResearchCounters");}
            return errors.AsReadOnly();
        }
    }
}
