using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class ResearchAllocationLockTests
{
    private static readonly ScienceDiscipline[] Disciplines=Enum.GetValues<ScienceDiscipline>();

    [Fact]
    public void WholePercentageDisplayTotalsOneHundredWithoutQuantizingWeightsOrMovingLockedLabels()
    {
        using var fixture=new IntegratedUiFixture();
        var draft=fixture.Model.ResearchInputs()!;
        var initial=draft.Values().Select(v=>v.Weight).ToArray();
        Assert.Equal(100,Disciplines.Sum(draft.DisplayPercent));
        foreach(var discipline in Disciplines)Assert.Matches(@"^\d+%$",draft.PercentLabel(discipline));
        Assert.Equal(initial,draft.Values().Select(v=>v.Weight));
        draft.SetLocked(Disciplines[5],true);
        var label=draft.PercentLabel(Disciplines[5]);
        for(int n=0;n<601;n++)
        {
            draft.SetWeight(Disciplines[0],n);
            Assert.Equal(label,draft.PercentLabel(Disciplines[5]));
            Assert.Equal(100,Disciplines.Sum(draft.DisplayPercent));
        }
        draft.ResetScience();
        Assert.True(draft.IsLocked(Disciplines[5]));
        Assert.Equal(initial,draft.Values().Select(v=>v.Weight));
        Assert.Equal(100,Disciplines.Sum(draft.DisplayPercent));
    }

    [Fact]
    public void LocksPreserveExactSharesAndDoNotSubmitOrDirtyTheCampaign()
    {
        using var fixture=new IntegratedUiFixture();
        var model=fixture.Model;var frame=model.Current.Frame;var draft=model.ResearchInputs()!;
        draft.SetLocked(Disciplines[1],true);
        Assert.False(draft.Dirty);
        var fixedWeight=draft.Weight(Disciplines[1]);
        draft.SetWeight(Disciplines[0],137);
        Assert.Equal(137,draft.Weight(Disciplines[0]));
        Assert.Equal(fixedWeight,draft.Weight(Disciplines[1]));
        Assert.Equal(ResearchUnits.Scale,draft.Values().Sum(v=>v.Weight));
        Assert.Same(frame,model.Current.Frame);
        Assert.Equal(10000,Disciplines.Sum(draft.DisplayHundredths));
    }

    [Fact]
    public void OneUnlockedOrAllLockedCannotChangeAndUnlockingDoesNotRebalance()
    {
        using var fixture=new IntegratedUiFixture();
        var draft=fixture.Model.ResearchInputs()!;
        foreach(var discipline in Disciplines.Skip(1))draft.SetLocked(discipline,true);
        var before=draft.Values().Select(v=>v.Weight).ToArray();
        draft.SetWeight(Disciplines[0],ResearchUnits.Scale);
        draft.Equalize();
        Assert.Equal(before,draft.Values().Select(v=>v.Weight));
        draft.SetLocked(Disciplines[0],true);
        draft.Adjust(Disciplines[0],int.MaxValue);
        draft.Equalize();
        Assert.Equal(before,draft.Values().Select(v=>v.Weight));
        draft.SetLocked(Disciplines[2],false);
        Assert.Equal(before,draft.Values().Select(v=>v.Weight));
    }

    [Fact]
    public void FullAllocationCanBeReleasedToZeroPeersAndRoundingIsStable()
    {
        using var fixture=new IntegratedUiFixture();
        var draft=fixture.Model.ResearchInputs()!;
        draft.SetWeight(Disciplines[0],600);
        Assert.Equal(new[]{600,0,0,0,0,0},draft.Values().Select(v=>v.Weight));
        draft.SetWeight(Disciplines[0],1);
        Assert.Equal(new[]{1,120,120,120,120,119},draft.Values().Select(v=>v.Weight));
        Assert.Equal(10000,Disciplines.Sum(draft.DisplayHundredths));
        draft.SetLocked(Disciplines[0],true);draft.Equalize();
        Assert.Equal(1,draft.Weight(Disciplines[0]));
        Assert.Equal(600,draft.Values().Sum(v=>v.Weight));
    }

    [Fact]
    public void RepeatedEditsKeepLockedValuesAndExactTotalThenUseNormalBackendValidation()
    {
        using var fixture=new IntegratedUiFixture();
        var model=fixture.Model;var draft=model.ResearchInputs()!;
        draft.SetLocked(Disciplines[5],true);
        var fixedWeight=draft.Weight(Disciplines[5]);
        for(int n=0;n<601;n++)
        {
            draft.SetWeight(Disciplines[n%5],n);
            Assert.Equal(fixedWeight,draft.Weight(Disciplines[5]));
            Assert.Equal(600,draft.Values().Sum(v=>v.Weight));
            Assert.All(draft.Values(),v=>Assert.InRange(v.Weight,0,600));
            Assert.Equal(10000,Disciplines.Sum(draft.DisplayHundredths));
        }
        Assert.True(model.AllocateScience(draft.Values(),HobbyDS9.UnityReview.ReviewUiModel.NewCommandId()));
        Assert.False(model.ResearchDraft!.Dirty);
        Assert.True(model.ResearchDraft.IsLocked(Disciplines[5]));
        Assert.Equal(draft.Values().Select(v=>v.Weight),
            model.Current.Frame.Strategy!.Research.Science.OrderBy(s=>s.Discipline).Select(s=>s.AllocationWeight));
    }
}
