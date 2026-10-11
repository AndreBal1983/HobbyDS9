using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
public sealed class ScienceCoreTests
{
    internal static ScienceEngineeringStateDto Initial(ulong seed=0) => new(){ResearchDiscoverySeed=seed,ScienceAllocationBps=7000,ScienceFocus=ScienceDiscipline.Construction,Disciplines=Enum.GetValues<ScienceDiscipline>().Select(d=>new ScienceDisciplineStateDto{Discipline=d,Level=1}).ToList(),EngineeringProgress=new(),MatureEngineeringApplicationDefinitionIds=new(),Statistics=new()};
    [Theory] [InlineData(5000,50)] [InlineData(6700,67)] [InlineData(10000,100)] [InlineData(15000,150)] [InlineData(30000,300)]
    public void PaceAndCeilingAreExact(int pace,long expected){Assert.Equal(expected,ScienceRules.IntervalCost(1,pace));Assert.Equal((3L*pace+9999)/10000,ScienceRules.AdjustCost(3,pace));}
    [Fact] public void CostOverflowIsExplicit(){Assert.Throws<OverflowException>(()=>ScienceRules.AdjustCost(long.MaxValue,30000));Assert.Equal(long.MaxValue,ScienceRules.AdjustCost(long.MaxValue,10000));}
    [Fact] public void EveryScheduleIsPositiveExactDeterministicPermutation()
    {foreach(int pace in new[]{5000,6700,10000,15000,30000})foreach(var d in Enum.GetValues<ScienceDiscipline>())for(int level=1;level<=9;level++)for(ulong seed=0;seed<30;seed++){
        var a=ScienceRules.Schedule(seed,d,level,pace);var b=ScienceRules.Schedule(seed,d,level,pace);Assert.InRange(a.Count,3,5);Assert.All(a,p=>Assert.True(p.ChunkCost>0));Assert.Equal(ScienceRules.IntervalCost(level,pace),a.Sum(p=>p.ChunkCost));Assert.Equal(Enumerable.Range(1,a.Count),a.Select(p=>p.Ordinal).Order());Assert.Equal(a.Select(p=>(p.Ordinal,p.Threshold,p.ChunkCost)),b.Select(p=>(p.Ordinal,p.Threshold,p.ChunkCost)));}}
    [Fact] public void SeedsVarySchedulesWithoutCampaignDraws()
    {var rng=new SplitMix64Random(42);var before=rng.State;var variants=Enumerable.Range(0,100).Select(seed=>string.Join(",",ScienceRules.Schedule((ulong)seed,ScienceDiscipline.Weapons,1,10000).Select(p=>$"{p.Ordinal}:{p.ChunkCost}"))).Distinct().Count();Assert.True(variants>20);Assert.Equal(before,rng.State);Assert.NotEqual(ScienceRules.DiscoverySeed(1,"civ.alpha"),ScienceRules.DiscoverySeed(1,"civ.beta"));Assert.NotEqual(ScienceRules.DiscoverySeed(1,"civ.alpha"),ScienceRules.DiscoverySeed(2,"civ.alpha"));}
    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(3333)] [InlineData(7000)] [InlineData(10000)]
    public void SplitConservesEveryPointAndBanksEngineering(int bps)
    {var s=Initial();s.ScienceAllocationBps=bps;ScienceRules.Allocate(s,17);Assert.Equal(17,s.ScienceReserve+s.EngineeringReserve);Assert.Equal(17*bps/10000,s.ScienceReserve);Assert.Equal(17,s.Statistics!.TotalResearchGenerated);}
    [Fact] public void ExactThresholdAndMultiLevelCarryAreConserved()
    {var s=Initial();var receipt=new List<ResearchReceiptDto>();var first=ScienceRules.Schedule(0,s.ScienceFocus!.Value,1,10000)[0];s.ScienceReserve=first.Threshold;ScienceRules.SpendScience(s,10000,"civ.alpha",receipt);Assert.Single(receipt,r=>r.Category=="science-breakthrough");Assert.Equal(first.Threshold,s.Disciplines!.Single(d=>d.Discipline==s.ScienceFocus).ProgressTowardNextLevel);
     long extra=100+130+170+7-first.Threshold;s.ScienceReserve=extra;ScienceRules.SpendScience(s,10000,"civ.alpha",receipt);var focus=s.Disciplines!.Single(d=>d.Discipline==s.ScienceFocus);Assert.Equal(4,focus.Level);Assert.Equal(7,focus.ProgressTowardNextLevel);Assert.Equal(407,s.Statistics!.TotalScienceSpent);}
    [Fact] public void MaximumLevelNeverRedirectsOrLosesReserve()
    {var s=Initial();s.ScienceReserve=10000;ScienceRules.SpendScience(s,10000,"civ.alpha",new());Assert.Equal(10,s.Disciplines!.Single(d=>d.Discipline==s.ScienceFocus).Level);Assert.Equal(10000-3290,s.ScienceReserve);Assert.Equal(0,s.Disciplines!.Single(d=>d.Discipline==s.ScienceFocus).ProgressTowardNextLevel);Assert.Equal(9,s.Statistics!.TotalScienceLevelsCompleted);}
    [Fact] public void ReorderedDisciplinesAndReloadProduceSameProgress()
    {var a=Initial(12);var b=System.Text.Json.JsonSerializer.Deserialize<ScienceEngineeringStateDto>(System.Text.Json.JsonSerializer.Serialize(a))!;b.Disciplines!.Reverse();a.ScienceReserve=b.ScienceReserve=450;var ar=new List<ResearchReceiptDto>();var br=new List<ResearchReceiptDto>();ScienceRules.SpendScience(a,10000,"civ.alpha",ar);ScienceRules.SpendScience(b,10000,"civ.alpha",br);Assert.Equal(System.Text.Json.JsonSerializer.Serialize(ar),System.Text.Json.JsonSerializer.Serialize(br));Assert.Equal(a.Disciplines!.Single(d=>d.Discipline==a.ScienceFocus).Level,b.Disciplines.Single(d=>d.Discipline==b.ScienceFocus).Level);}
    [Fact] public void MalformedStatesAreRejected()
    {var s=Initial();Assert.Empty(ScienceRules.Validate(s,10000));s.Disciplines!.RemoveAt(0);Assert.NotEmpty(ScienceRules.Validate(s,10000));s=Initial();s.Disciplines![0].ProgressTowardNextLevel=100;Assert.NotEmpty(ScienceRules.Validate(s,10000));s=Initial();s.ResearchDiscoverySeed=null;Assert.NotEmpty(ScienceRules.Validate(s,10000));Assert.NotEmpty(ScienceRules.Validate(Initial(),1));}
}
