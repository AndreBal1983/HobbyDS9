using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class ResearchAutoApplyTests
{
    [Fact]public void AcceptedScienceThenRejectedSplitKeepsTheAcceptedLayerAndDoesNotRetryEither()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var d=m.ResearchInputs()!;var frame=m.Current.Frame;var split=d.ScienceUnits;
        d.SetWeight(ScienceDiscipline.Construction,360);Assert.True(m.QueueScienceAllocation(d));d.ScienceUnits=601;Assert.True(m.QueueResearchSplit(d));
        Assert.False(m.FlushResearchAllocations());Assert.Equal(frame.Status.CampaignRevision+1,m.Current.Frame.Status.CampaignRevision);
        Assert.Equal(360,m.ResearchInputs()!.Weight(ScienceDiscipline.Construction));Assert.Equal(split,m.ResearchInputs()!.ScienceUnits);Assert.False(m.HasPlanningEdits);
        var accepted=m.Current.Frame;Assert.True(m.FlushResearchAllocations());Assert.Same(accepted,m.Current.Frame);
    }
    [Fact]public void CancellationRestoresAcceptedInputAndLocksSurviveTurnRefresh()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var d=m.ResearchInputs()!;var split=d.ScienceUnits;
        d.SetLocked(ScienceDiscipline.Computing,true);var lockedLabel=d.PercentLabel(ScienceDiscipline.Computing);d.SetWeight(ScienceDiscipline.Construction,400);
        Assert.True(m.QueueScienceAllocation(d));Assert.True(m.FlushResearchAllocations());
        d=m.ResearchInputs()!;d.ScienceUnits=300;Assert.True(m.QueueResearchSplit(d));m.CancelResearchAllocations();
        Assert.Equal(split,m.ResearchInputs()!.ScienceUnits);Assert.False(m.HasPlanningEdits);Assert.True(m.Advance());Assert.True(m.ResearchInputs()!.IsLocked(ScienceDiscipline.Computing));Assert.Equal(lockedLabel,m.ResearchInputs()!.PercentLabel(ScienceDiscipline.Computing));
    }
    [Fact]public void RapidCommittedChangesCoalesceToLastCompletePlanAndKeepLocks()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var frame=m.Current.Frame;var d=m.ResearchInputs()!;
        d.SetLocked(ScienceDiscipline.Computing,true);var locked=d.Weight(ScienceDiscipline.Computing);
        for(int i=1;i<=50;i++){d.SetWeight(ScienceDiscipline.Construction,i*6);Assert.True(m.QueueScienceAllocation(d));}
        var desired=d.Values().Select(v=>v.Weight).ToArray();Assert.Same(frame,m.Current.Frame);Assert.True(m.ResearchCommitPending);
        Assert.True(m.FlushResearchAllocations());Assert.Equal(frame.Status.CampaignRevision+1,m.Current.Frame.Status.CampaignRevision);
        Assert.Equal(desired,m.Current.Frame.Strategy!.Research.Science.OrderBy(s=>s.Discipline).Select(s=>s.AllocationWeight));
        Assert.Equal(600,desired.Sum());Assert.Equal(locked,m.ResearchInputs()!.Weight(ScienceDiscipline.Computing));Assert.True(m.ResearchInputs()!.IsLocked(ScienceDiscipline.Computing));
        Assert.False(m.HasPlanningEdits);var accepted=m.Current.Frame;Assert.True(m.FlushResearchAllocations());Assert.Same(accepted,m.Current.Frame);
    }
    [Fact]public void BothLayersSerializeAgainstTheirAcceptedStampsWithoutLosingLastValues()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var frame=m.Current.Frame;var d=m.ResearchInputs()!;
        d.SetWeight(ScienceDiscipline.Construction,360);Assert.True(m.QueueScienceAllocation(d));
        var final=d.ScienceUnits==480?450:480;d.ScienceUnits=450;Assert.True(m.QueueResearchSplit(d));d.ScienceUnits=final;Assert.True(m.QueueResearchSplit(d));
        Assert.True(m.FlushResearchAllocations());Assert.Equal(frame.Status.CampaignRevision+2,m.Current.Frame.Status.CampaignRevision);
        Assert.Equal(final,m.Current.Frame.Strategy!.Research.ScienceAllocationUnits);Assert.Equal(360,m.ResearchInputs()!.Weight(ScienceDiscipline.Construction));Assert.False(m.HasLocalEdits);
    }
    [Fact]public void StaleQueuedObservationRestoresAcceptedValuesAndNeverRetries()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var d=m.ResearchInputs()!;
        var original=d.Values().Select(v=>v.Weight).ToArray();d.SetWeight(ScienceDiscipline.Construction,360);Assert.True(m.QueueScienceAllocation(d));
        Assert.True(m.Refresh());var refreshed=m.Current.Frame;Assert.False(m.FlushResearchAllocations());
        Assert.Same(refreshed,m.Current.Frame);Assert.Equal(original,m.ResearchInputs()!.Values().Select(v=>v.Weight));Assert.False(m.ResearchCommitPending);Assert.False(m.HasPlanningEdits);
        Assert.Contains("changed",m.ResearchCommitNotice);Assert.True(m.FlushResearchAllocations());Assert.Same(refreshed,m.Current.Frame);
    }
    [Fact]public void RejectedSplitRestoresAcceptedValuesAndStopsTheBatch()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var frame=m.Current.Frame;var d=m.ResearchInputs()!;
        var original=d.ScienceUnits;d.ScienceUnits=601;Assert.True(m.QueueResearchSplit(d));Assert.False(m.FlushResearchAllocations());
        Assert.Same(frame,m.Current.Frame);Assert.Equal(original,m.ResearchInputs()!.ScienceUnits);Assert.False(m.ResearchCommitPending);Assert.False(m.HasPlanningEdits);
        Assert.Contains("restored",m.ResearchCommitNotice);Assert.True(m.FlushResearchAllocations());Assert.Same(frame,m.Current.Frame);
    }
    [Fact]public void EndTurnWaitsOnlyForThePendingCommitThenPreservesSessionDesign()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var d=m.ResearchInputs()!;var controller=m.Current;int turn=controller.Frame.Status.Turn;
        m.LocalDesignDirty=true;bool reset=false;m.ResetLocalDesign=()=>reset=true;
        d.ScienceUnits=420;Assert.True(m.QueueResearchSplit(d));Assert.False(m.Advance());
        Assert.True(m.FlushResearchAllocations());Assert.False(m.HasPlanningEdits);Assert.True(m.Advance());
        Assert.Same(controller,m.Current);Assert.Equal(turn+1,m.Current.Frame.Status.Turn);Assert.True(m.LocalDesignDirty);Assert.False(reset);
        Assert.True(m.Save("local-design"));Assert.True(m.LocalDesignDirty);Assert.False(m.Load("local-design"));Assert.False(m.Close());
    }
    [Fact]public void EqualizeCommitsDirectlyAndRetainsExactLockedShare()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var d=m.ResearchInputs()!;
        d.SetWeight(ScienceDiscipline.Construction,360);Assert.True(m.QueueScienceAllocation(d));Assert.True(m.FlushResearchAllocations());
        d=m.ResearchInputs()!;d.SetLocked(ScienceDiscipline.Construction,true);d.Equalize();Assert.True(m.QueueScienceAllocation(d));Assert.True(m.FlushResearchAllocations());
        Assert.Equal(360,m.ResearchInputs()!.Weight(ScienceDiscipline.Construction));Assert.Equal(600,m.ResearchInputs()!.Values().Sum(v=>v.Weight));Assert.False(m.HasPlanningEdits);
    }
    [Fact]public void ExplicitActorReplacementCancelsOldQueueAndCannotApplyToNewActor()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var d=m.ResearchInputs()!;d.ScienceUnits=420;Assert.True(m.QueueResearchSplit(d));
        Assert.False(m.SwitchActor(UnityReviewFixture.B));Assert.True(m.SwitchActor(UnityReviewFixture.B,true));var frame=m.Current.Frame;
        Assert.True(m.FlushResearchAllocations());Assert.Same(frame,m.Current.Frame);Assert.False(m.HasLocalEdits);
    }
}
