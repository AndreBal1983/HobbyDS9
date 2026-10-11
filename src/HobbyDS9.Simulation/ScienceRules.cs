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
        { if(level<1||level>9)throw new ArgumentOutOfRangeException(nameof(level));return AdjustCost(checked(Costs[level-1]*ResearchUnits.Scale),pace); }
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
        public static long InternalUnits(long conceptual) => checked((long)((BigInteger)conceptual*ResearchUnits.Scale));
        public static bool ValidAllocations(IReadOnlyList<ScienceAllocationDto>? allocations) => allocations!=null&&allocations.Count==6
            &&allocations.All(a=>a!=null&&ValidDiscipline(a.Discipline)&&a.Weight>=0&&a.Weight<=ResearchUnits.Scale)
            &&allocations.Select(a=>a.Discipline).Distinct().Count()==6&&allocations.Sum(a=>a.Weight)==ResearchUnits.Scale;
        public static IReadOnlyList<(ScienceDiscipline Discipline,long Amount)> Distribute(long amount,IReadOnlyList<ScienceAllocationDto> allocations)
        {
            if(amount<0||!ValidAllocations(allocations))throw new ArgumentException("Invalid Science distribution.");
            var ordered=allocations.OrderBy(a=>a.Discipline).ToList();var shares=new long[6];var remainder=new BigInteger[6];long assigned=0;
            for(int i=0;i<6;i++){shares[i]=(long)BigInteger.DivRem((BigInteger)amount*ordered[i].Weight,ResearchUnits.Scale,out remainder[i]);assigned=checked(assigned+shares[i]);}
            foreach(var i in Enumerable.Range(0,6).OrderByDescending(i=>remainder[i]).ThenBy(i=>ordered[i].Discipline).Take(checked((int)(amount-assigned))))shares[i]++;
            return ordered.Select((a,i)=>(a.Discipline,shares[i])).ToList().AsReadOnly();
        }
        public static void Allocate(ScienceEngineeringStateDto research,long conceptualOutput)
        {
            if(conceptualOutput<0||research.ScienceAllocationUnits<0||research.ScienceAllocationUnits>ResearchUnits.Scale)throw new ArgumentOutOfRangeException(nameof(conceptualOutput));
            long output=InternalUnits(conceptualOutput);long science=(long)((BigInteger)output*research.ScienceAllocationUnits/ResearchUnits.Scale);long engineering=output-science;
            checked { research.ScienceReserve+=science;research.EngineeringReserve+=engineering;
                research.Statistics!.TotalResearchGenerated+=output;research.Statistics.TotalScienceAllocated+=science;research.Statistics.TotalEngineeringAllocated+=engineering; }
        }
        public static void SpendScience(ScienceEngineeringStateDto research,int pace,string civilizationId,List<ResearchReceiptDto> receipts)
        {
            // Distribute the whole available bank once. A maxed share or excess at level 10 stays banked.
            // No recursive redistribution within this phase and no inter-turn rounding debt.
            var budgets=Distribute(research.ScienceReserve,research.ScienceAllocations!);
            foreach(var budget in budgets)
            {
                var discipline=research.Disciplines!.Single(d=>d.Discipline==budget.Discipline);long remaining=budget.Amount;
                while(discipline.Level<10&&remaining>0)
                {
                    long cost=IntervalCost(discipline.Level,pace);long spent=Math.Min(remaining,cost-discipline.ProgressTowardNextLevel);
                    long before=discipline.ProgressTowardNextLevel;discipline.ProgressTowardNextLevel=checked(before+spent);remaining-=spent;research.ScienceReserve-=spent;
                    discipline.TotalSpent=checked(discipline.TotalSpent+spent);research.Statistics!.TotalScienceSpent=checked(research.Statistics.TotalScienceSpent+spent);
                    receipts.Add(new ResearchReceiptDto{CivilizationId=civilizationId,Category="science-spent",Discipline=discipline.Discipline,IntervalStartLevel=discipline.Level,Amount=spent});
                    foreach(var point in Schedule(research.ResearchDiscoverySeed!.Value,discipline.Discipline,discipline.Level,pace))
                        if(point.Threshold>before&&point.Threshold<=discipline.ProgressTowardNextLevel)
                        {research.Statistics.TotalBreakthroughs=checked(research.Statistics.TotalBreakthroughs+1);receipts.Add(new ResearchReceiptDto{CivilizationId=civilizationId,Category="science-breakthrough",Discipline=discipline.Discipline,IntervalStartLevel=discipline.Level,BreakthroughOrdinal=point.Ordinal,Amount=point.Threshold});}
                    if(discipline.ProgressTowardNextLevel<cost)break;
                    discipline.ProgressTowardNextLevel=0;discipline.Level++;research.Statistics.TotalScienceLevelsCompleted=checked(research.Statistics.TotalScienceLevelsCompleted+1);
                    receipts.Add(new ResearchReceiptDto{CivilizationId=civilizationId,Category="science-level",Discipline=discipline.Discipline,IntervalStartLevel=discipline.Level-1,Amount=discipline.Level});
                }
            }
        }
        public static IReadOnlyList<string> Validate(ScienceEngineeringStateDto? state,int pace)
        {
            var errors=new List<string>();if(state==null){errors.Add("MissingResearch");return errors.AsReadOnly();}
            if(!ValidPace(pace))errors.Add("InvalidPace");
            if(!state.ResearchDiscoverySeed.HasValue)errors.Add("MissingDiscoverySeed");
            if(state.ScienceAllocationUnits<0||state.ScienceAllocationUnits>ResearchUnits.Scale||state.ScienceReserve<0||state.EngineeringReserve<0)errors.Add("InvalidReserveOrAllocation");
            if(!ValidAllocations(state.ScienceAllocations))errors.Add("InvalidScienceAllocations");
            if(state.EngineeringParameters==null)errors.Add("MissingEngineeringParameters");
            if(state.Disciplines==null||state.Disciplines.Count!=6||state.Disciplines.Any(d=>d==null)||state.Disciplines.Where(d=>d!=null).Select(d=>d.Discipline).Distinct().Count()!=6)errors.Add("MissingOrDuplicateDisciplines");
            if(state.Disciplines!=null)foreach(var d in state.Disciplines.Where(d=>d!=null))
                if(!ValidDiscipline(d.Discipline)||d.Level<1||d.Level>10||d.ProgressTowardNextLevel<0||d.TotalSpent<0||(d.Level==10&&d.ProgressTowardNextLevel!=0)||(d.Level<10&&d.Level>=1&&ValidPace(pace)&&d.ProgressTowardNextLevel>=IntervalCost(d.Level,pace)))errors.Add("InvalidDisciplineState");
            if(state.Statistics==null||state.EngineeringProgress==null)errors.Add("MissingResearchCollections");
            if(state.Statistics!=null){var s=state.Statistics;if(new[]{s.TotalResearchGenerated,s.TotalScienceAllocated,s.TotalEngineeringAllocated,s.TotalScienceSpent,s.TotalEngineeringSpent,s.TotalBreakthroughs,s.TotalScienceLevelsCompleted,s.TotalEngineeringApplicationsMatured}.Any(v=>v<0)||(BigInteger)s.TotalScienceAllocated+s.TotalEngineeringAllocated!=s.TotalResearchGenerated)errors.Add("InvalidResearchCounters");}
            return errors.AsReadOnly();
        }
    }
}
