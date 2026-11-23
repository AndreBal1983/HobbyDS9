using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class StreamedMaterialWorkTests
{
    static ResourceAmountDto R(long amount,string id="resource.dilithium")=>new(){ResourceDefinitionId=id,Amount=amount};
    [Theory]
    [InlineData(5,100,0,0)][InlineData(5,100,1,1)][InlineData(5,100,20,1)]
    [InlineData(5,100,21,2)][InlineData(5,100,100,5)][InlineData(10,5,1,2)][InlineData(0,7,7,0)]
    public void ExactCumulativeCeiling(long resource,long work,long completed,long expected)
        =>Assert.Equal(expected,StreamedMaterialWork.Cumulative(resource,work,completed));
    [Fact] public void FirstPositiveWorkRequiresEveryPositiveInputAndThenUsesOnlyNextQuantum()
    {
        var need=new[]{R(5),R(1,"resource.other")};
        Assert.Equal(0,StreamedMaterialWork.Plan(need,new[]{R(5)},100,0,100).CompletedWork);
        var first=StreamedMaterialWork.Plan(need,new[]{R(1),R(1,"resource.other")},100,0,100);
        Assert.Equal(20,first.CompletedWork);Assert.All(first.ConsumedDelta,r=>Assert.Equal(1,r.Amount));
        var blocked=StreamedMaterialWork.Plan(need,Array.Empty<ResourceAmountDto>(),100,20,100);
        Assert.Equal(20,blocked.CompletedWork);Assert.All(blocked.ConsumedDelta,r=>Assert.Equal(0,r.Amount));
        var resumed=StreamedMaterialWork.Plan(need,new[]{R(1)},100,20,100);
        Assert.Equal(40,resumed.CompletedWork);Assert.Equal(1,resumed.ConsumedDelta[0].Amount);Assert.Equal(0,resumed.ConsumedDelta[1].Amount);
    }
    [Fact] public void NoWorkAndNoRequirementsAreHandledWithoutFreeMaterialsOrExtraIndustry()
    {
        var paused=StreamedMaterialWork.Plan(new[]{R(5)},new[]{R(5)},100,0,0);
        Assert.Equal(0,paused.CompletedWork);Assert.Equal(0,Assert.Single(paused.ConsumedDelta).Amount);
        var free=StreamedMaterialWork.Plan(Array.Empty<ResourceAmountDto>(),Array.Empty<ResourceAmountDto>(),100,20,9);
        Assert.Equal(29,free.CompletedWork);Assert.Empty(free.ConsumedDelta);
    }
    [Fact] public void WholeLongDomainDoesNotOverflowAndInsufficientUnitsCannotComplete()
    {
        Assert.Equal(long.MaxValue,StreamedMaterialWork.Cumulative(long.MaxValue,long.MaxValue,long.MaxValue));
        Assert.Equal(1,StreamedMaterialWork.Cumulative(1,long.MaxValue,1));
        var plan=StreamedMaterialWork.Plan(new[]{R(long.MaxValue)},new[]{R(long.MaxValue-1)},long.MaxValue,0,long.MaxValue);
        Assert.Equal(long.MaxValue-1,plan.CompletedWork);Assert.Equal(long.MaxValue-1,Assert.Single(plan.ConsumedDelta).Amount);
        var final=StreamedMaterialWork.Plan(new[]{R(long.MaxValue)},new[]{R(1)},long.MaxValue,long.MaxValue-1,long.MaxValue);
        Assert.Equal(long.MaxValue,final.CompletedWork);Assert.Equal(1,Assert.Single(final.ConsumedDelta).Amount);
    }
    [Fact] public void ReorderedVectorsProduceSameCanonicalPlanWithoutAliasingOrMutation()
    {
        var a=new[]{R(7,"resource.z"),R(3,"resource.a")};var stocks=new[]{R(2,"resource.a"),R(7,"resource.z")};
        var plan=StreamedMaterialWork.Plan(a,stocks,100,0,100);
        var reordered=StreamedMaterialWork.Plan(a.Reverse().ToArray(),stocks.Reverse().ToArray(),100,0,100);
        Assert.Equal(plan.CompletedWork,reordered.CompletedWork);
        Assert.Equal(plan.ConsumedDelta.Select(r=>(r.ResourceDefinitionId,r.Amount)),reordered.ConsumedDelta.Select(r=>(r.ResourceDefinitionId,r.Amount)));
        Assert.Equal(new[]{"resource.a","resource.z"},plan.ConsumedDelta.Select(r=>r.ResourceDefinitionId));
        plan.ConsumedDelta[0].Amount=999;Assert.Equal(3,a[1].Amount);Assert.Equal(2,stocks[0].Amount);
    }
    [Fact] public void RepeatedActualTranchesConserveTheFullRequirementExactlyOnce()
    {
        for(int material=1;material<=25;material++)
        {
            long progress=0,consumed=0;var required=new[]{R(material)};
            while(progress<113)
            {
                var result=StreamedMaterialWork.Plan(required,new[]{R(material-consumed)},113,progress,8);
                Assert.True(result.CompletedWork>progress);progress=result.CompletedWork;
                consumed+=Assert.Single(result.ConsumedDelta).Amount;
                Assert.Equal(StreamedMaterialWork.Cumulative(material,113,progress),consumed);
            }
            Assert.Equal(material,consumed);
        }
    }
    [Fact] public void InvalidVectorsAndWorkFailBeforeAnyInputCanChange()
    {
        var good=new[]{R(1)};
        foreach(var bad in new[]{new[]{R(0)},new[]{R(-1)},new[]{R(1),R(2)},new[]{R(1,"")},new ResourceAmountDto[]{null!}})
            Assert.Throws<ArgumentException>(()=>StreamedMaterialWork.Plan(bad,good,100,0,5));
        Assert.Throws<ArgumentException>(()=>StreamedMaterialWork.Plan(good,new[]{R(-1)},100,0,5));
        Assert.Throws<ArgumentException>(()=>StreamedMaterialWork.Plan(good,new[]{R(1),R(1)},100,0,5));
        Assert.Throws<ArgumentException>(()=>StreamedMaterialWork.Plan(good,good,100,100,5));
        Assert.Throws<ArgumentException>(()=>StreamedMaterialWork.Plan(good,good,100,0,-1));
        Assert.Throws<ArgumentException>(()=>StreamedMaterialWork.Cumulative(1,0,0));
        Assert.Throws<ArgumentException>(()=>StreamedMaterialWork.Cumulative(1,100,101));
        Assert.Equal(1,good[0].Amount);
    }
}
