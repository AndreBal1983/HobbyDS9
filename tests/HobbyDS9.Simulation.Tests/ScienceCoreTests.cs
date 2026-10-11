using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
public sealed class ScienceCoreTests
{
    internal static List<ScienceAllocationDto> Equal()=>Enum.GetValues<ScienceDiscipline>().Select(d=>new ScienceAllocationDto{Discipline=d,Weight=100}).ToList();
    internal static ScienceEngineeringStateDto Initial(ulong seed=0)=>new(){ResearchDiscoverySeed=seed,ScienceAllocationUnits=420,ScienceAllocations=Equal(),EngineeringParameters=new(),Disciplines=Enum.GetValues<ScienceDiscipline>().Select(d=>new ScienceDisciplineStateDto{Discipline=d,Level=1}).ToList(),EngineeringProgress=new(),MatureEngineeringApplicationDefinitionIds=new(),Statistics=new()};
    [Theory][InlineData(1,600)][InlineData(4,2400)]
    public void ConversionOccursExactlyOnce(long conceptual,long expected)=>Assert.Equal(expected,ScienceRules.InternalUnits(conceptual));
    [Theory][InlineData(5000,30000)][InlineData(6700,40200)][InlineData(10000,60000)][InlineData(15000,90000)][InlineData(30000,180000)]
    public void PaceAndScaledCostsAreExact(int pace,long expected){Assert.Equal(expected,ScienceRules.IntervalCost(1,pace));Assert.Equal((3L*pace+9999)/10000,ScienceRules.AdjustCost(3,pace));}
    [Fact] public void OverflowIsExplicit(){Assert.Throws<OverflowException>(()=>ScienceRules.InternalUnits(long.MaxValue));Assert.Throws<OverflowException>(()=>ScienceRules.AdjustCost(long.MaxValue,30000));Assert.Equal(long.MaxValue,ScienceRules.AdjustCost(long.MaxValue,10000));}
    [Fact] public void EveryScheduleIsPositiveExactDeterministicPermutation()
    {
        foreach(int pace in new[]{5000,6700,10000,15000,30000})foreach(var d in Enum.GetValues<ScienceDiscipline>())for(int level=1;level<=9;level++)for(ulong seed=0;seed<30;seed++)
        {var a=ScienceRules.Schedule(seed,d,level,pace);var b=ScienceRules.Schedule(seed,d,level,pace);Assert.InRange(a.Count,3,5);Assert.All(a,p=>Assert.True(p.ChunkCost>0));Assert.Equal(ScienceRules.IntervalCost(level,pace),a.Sum(p=>p.ChunkCost));Assert.Equal(Enumerable.Range(1,a.Count),a.Select(p=>p.Ordinal).Order());Assert.Equal(a.Select(p=>(p.Ordinal,p.Threshold,p.ChunkCost)),b.Select(p=>(p.Ordinal,p.Threshold,p.ChunkCost)));}
    }
    [Fact] public void SeedsVarySchedulesWithoutCampaignDraws(){var rng=new SplitMix64Random(42);var variants=Enumerable.Range(0,100).Select(seed=>string.Join(",",ScienceRules.Schedule((ulong)seed,ScienceDiscipline.Weapons,1,10000).Select(p=>$"{p.Ordinal}:{p.ChunkCost}"))).Distinct().Count();Assert.True(variants>20);Assert.Equal(42UL,rng.State);Assert.NotEqual(ScienceRules.DiscoverySeed(1,"civ.alpha"),ScienceRules.DiscoverySeed(1,"civ.beta"));}
    [Theory][InlineData(0)][InlineData(1)][InlineData(199)][InlineData(420)][InlineData(600)]
    public void SplitConservesEveryInternalUnit(int units){var state=Initial();state.ScienceAllocationUnits=units;ScienceRules.Allocate(state,4);Assert.Equal(2400,state.ScienceReserve+state.EngineeringReserve);Assert.Equal(4*units,state.ScienceReserve);Assert.Equal(2400,state.Statistics!.TotalResearchGenerated);}
    [Fact] public void SeventyThirtyAndEqualSixWayAreExact(){var s=Initial();ScienceRules.Allocate(s,4);Assert.Equal(1680,s.ScienceReserve);Assert.Equal(720,s.EngineeringReserve);Assert.All(ScienceRules.Distribute(s.ScienceReserve,s.ScienceAllocations!),a=>Assert.Equal(280,a.Amount));ScienceRules.SpendScience(s,10000,"civ.alpha",new());Assert.All(s.Disciplines!,d=>{Assert.Equal(280,d.ProgressTowardNextLevel);Assert.Equal(280,d.TotalSpent);});Assert.Equal(1680,s.Statistics!.TotalScienceSpent);Assert.Equal(0,s.ScienceReserve);}
    [Fact] public void LargestRemainderTiesUseEnumOrderRegardlessOfInputOrder(){var weights=Equal();var result=ScienceRules.Distribute(8,weights);Assert.Equal(new long[]{2,2,1,1,1,1},result.Select(r=>r.Amount));weights.Reverse();Assert.Equal(result,ScienceRules.Distribute(8,weights));for(long amount=0;amount<1000;amount++){weights[0].Weight=105;weights[1].Weight=95;Assert.Equal(amount,ScienceRules.Distribute(amount,weights).Sum(r=>r.Amount));}}
    [Fact] public void ExactThresholdMultiLevelCarryAndSixWayCrossingsAreConserved()
    {
        var s=Initial();var receipts=new List<ResearchReceiptDto>();long perDiscipline=60000+78000+102000+7;s.ScienceReserve=6*perDiscipline;ScienceRules.SpendScience(s,10000,"civ.alpha",receipts);
        Assert.All(s.Disciplines!,d=>{Assert.Equal(4,d.Level);Assert.Equal(7,d.ProgressTowardNextLevel);Assert.Equal(perDiscipline,d.TotalSpent);});Assert.Equal(18,s.Statistics!.TotalScienceLevelsCompleted);Assert.Equal(6*perDiscipline,s.Statistics.TotalScienceSpent);Assert.Equal(0,s.ScienceReserve);Assert.True(receipts.Count(r=>r.Category=="science-breakthrough")>=54);
    }
    [Fact] public void ExactThresholdIsCrossedOnce(){var s=Initial();var point=ScienceRules.Schedule(0,ScienceDiscipline.Biotech,1,10000)[0];s.ScienceAllocations!.ForEach(a=>a.Weight=a.Discipline==ScienceDiscipline.Biotech?600:0);s.ScienceReserve=point.Threshold;var receipts=new List<ResearchReceiptDto>();ScienceRules.SpendScience(s,10000,"civ.alpha",receipts);Assert.Single(receipts,r=>r.Category=="science-breakthrough");ScienceRules.SpendScience(s,10000,"civ.alpha",receipts);Assert.Single(receipts,r=>r.Category=="science-breakthrough");}
    [Fact] public void MaximumLevelBanksAssignedShareAndRedistributesBankOnlyOnceNextPhase()
    {
        var s=Initial();s.Disciplines![0].Level=10;s.ScienceReserve=600;ScienceRules.SpendScience(s,10000,"civ.alpha",new());Assert.Equal(100,s.ScienceReserve);Assert.Equal(500,s.Statistics!.TotalScienceSpent);Assert.Equal(0,s.Disciplines[0].TotalSpent);
        ScienceRules.SpendScience(s,10000,"civ.alpha",new());Assert.Equal(17,s.ScienceReserve);Assert.Equal(583,s.Statistics.TotalScienceSpent);Assert.Equal(600,s.ScienceReserve+s.Statistics.TotalScienceSpent);
    }
    [Fact] public void TerminalOverflowBanksWithoutPointLoss(){var s=Initial();long allCosts=3290L*600;s.ScienceReserve=6*(allCosts+100);ScienceRules.SpendScience(s,10000,"civ.alpha",new());Assert.All(s.Disciplines!,d=>{Assert.Equal(10,d.Level);Assert.Equal(0,d.ProgressTowardNextLevel);});Assert.Equal(600,s.ScienceReserve);Assert.Equal(54,s.Statistics!.TotalScienceLevelsCompleted);}
    [Fact] public void ReorderedAllocationsDisciplinesAndReloadContinueIdentically(){var a=Initial(12);var b=JsonSerializer.Deserialize<ScienceEngineeringStateDto>(JsonSerializer.Serialize(a))!;b.Disciplines!.Reverse();b.ScienceAllocations!.Reverse();a.ScienceReserve=b.ScienceReserve=1000000;var ar=new List<ResearchReceiptDto>();var br=new List<ResearchReceiptDto>();ScienceRules.SpendScience(a,10000,"civ.alpha",ar);ScienceRules.SpendScience(b,10000,"civ.alpha",br);Assert.Equal(JsonSerializer.Serialize(ar),JsonSerializer.Serialize(br));}
    [Fact] public void MalformedStateRejects(){var s=Initial();Assert.Empty(ScienceRules.Validate(s,10000));s.Disciplines!.RemoveAt(0);Assert.NotEmpty(ScienceRules.Validate(s,10000));s=Initial();s.Disciplines![0].ProgressTowardNextLevel=60000;Assert.NotEmpty(ScienceRules.Validate(s,10000));s=Initial();s.ResearchDiscoverySeed=null;Assert.NotEmpty(ScienceRules.Validate(s,10000));Assert.NotEmpty(ScienceRules.Validate(Initial(),1));}
}
