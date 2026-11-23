using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class EngineeringRefinementPresentationTests
{
    static ComponentVersionDto Module(string id,long space,long power,long industry)=>new(){VersionId=id,Capacity=space,PowerSupply=power,
        CompatibleHullFamilyIds=new(),RequiredHullTags=new(),Tags=new(),RequiredComponentTags=new(),Mounts=new(),Dependencies=new(),Modes=new(),Cost=new(){Industry=industry,Resources=new()}};
    static EngineeringRefinementProjectSnapshot Project(EngineeringRefinementGoal goal,ComponentVersionDto before,ComponentVersionDto after,EngineeringRefinementGoal? hint=null)
        =>new(new(){ProjectId="project.test",Goal=goal,Stage=EngineeringRefinementStage.Pending,Before=before,ParentMaturityMilli=100000,EffectiveWork=24000,Downside=hint,Outcome=new(){Component=after,MaturityMilli=105000}},24000);
    [Fact]public void MaturityAndEngineeringWorkRemainDistinctAndNullIsNotZero()
    {
        Assert.Equal("Not tracked",EngineeringRefinementPresentation.Maturity(null));Assert.Equal("100",EngineeringRefinementPresentation.Maturity(100000));Assert.Equal("125.125",EngineeringRefinementPresentation.Maturity(125125));
        Assert.Equal("40",EngineeringRefinementPresentation.Work(24000));Assert.Equal("0.5",EngineeringRefinementPresentation.Work(300));
    }
    [Fact]public void SideEffectsUseActualNonTargetVectorChangesRatherThanTheCategoryHint()
    {
        var before=Module("component.before",12,60,100);var clean=Project(EngineeringRefinementGoal.ReduceCapacity,before,Module("component.after",11,60,100),EngineeringRefinementGoal.ReduceIndustry);
        Assert.Equal("None",EngineeringRefinementPresentation.SideEffects(clean));Assert.False(EngineeringRefinementPresentation.HasDownside(clean));Assert.True(EngineeringRefinementPresentation.Improved(clean.Before,clean.Outcome!,clean.Goal));
        var tradeoff=Project(EngineeringRefinementGoal.ReduceCapacity,before,Module("component.after",11,60,104));
        Assert.Equal("Industry cost 100 → 104",EngineeringRefinementPresentation.SideEffects(tradeoff));Assert.True(EngineeringRefinementPresentation.HasDownside(tradeoff));Assert.Equal("12 → 11 hull space",EngineeringRefinementPresentation.Improvement(tradeoff));
        var otherBenefit=Project(EngineeringRefinementGoal.ReduceCapacity,before,Module("component.after",11,62,100));
        Assert.Equal("Reactor output 60 → 62",EngineeringRefinementPresentation.SideEffects(otherBenefit));Assert.False(EngineeringRefinementPresentation.HasDownside(otherBenefit));
    }
    [Fact]public void SupplyAndConsumptionHaveOppositeImprovementDirections()
    {
        var before=Module("component.before",12,60,100);var after=Module("component.after",11,64,105);var b=new EngineeringRefinementVectorSnapshot(before,100000);var a=new EngineeringRefinementVectorSnapshot(after,105000);
        Assert.True(EngineeringRefinementPresentation.Improved(b,a,EngineeringRefinementGoal.ReduceCapacity));Assert.True(EngineeringRefinementPresentation.Improved(b,a,EngineeringRefinementGoal.IncreasePowerSupply));Assert.False(EngineeringRefinementPresentation.Improved(b,a,EngineeringRefinementGoal.ReduceIndustry));
        Assert.Contains("limit",EngineeringRefinementPresentation.Reason(EngineeringRefinementError.Exhausted));Assert.Contains("Reopen",EngineeringRefinementPresentation.Reason(EngineeringRefinementError.StaleRevision));
    }
}
