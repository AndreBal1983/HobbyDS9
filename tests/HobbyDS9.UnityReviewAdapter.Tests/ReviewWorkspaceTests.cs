using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;
using static HobbyDS9.UnityReviewAdapter.Tests.UnityReviewFixture;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class ReviewWorkspaceTests
{
    [Fact]
    public void EveryPanelUsesSameControllerAndFrameWithoutResetOrAuthorityRefresh()
    {
        using var f=new UnityReviewFixture();var model=new ReviewUiModel(f.Package,f.Codec,f.Store,A);
        var workspace=new ReviewWorkspace(new ReviewCombatFlow(model));var controller=model.Current;var frame=controller.Frame;
        Assert.Equal(ReviewWorkspacePage.Map,workspace.Page);
        foreach(var page in Enum.GetValues<ReviewWorkspacePage>())
        {
            Assert.True(workspace.Navigate(page));Assert.Equal(page,workspace.Page);
            Assert.Same(controller,model.Current);Assert.Same(frame,model.Current.Frame);
        }
        Assert.Contains(A,workspace.BoundaryIdentity);Assert.Contains(frame.Status.Turn.ToString(),workspace.BoundaryIdentity);
    }
    [Fact]
    public void NavigationAndCommittedSaveKeepTravelDraftWhileFailedLoadKeepsSession()
    {
        using var f=new UnityReviewFixture();var model=new ReviewUiModel(f.Package,f.Codec,f.Store,A);
        var workspace=new ReviewWorkspace(new ReviewCombatFlow(model));
        Assert.True(model.DraftTravel(FA,CampaignTravelIntent.StayEngage,null,null));
        var draft=model.Current.TravelDraft;var controller=model.Current;var frame=controller.Frame;
        foreach(var page in Enum.GetValues<ReviewWorkspacePage>())Assert.True(workspace.Navigate(page));
        Assert.True(model.Save("navigation"));Assert.Same(draft,model.Current.TravelDraft);Assert.Same(frame,model.Current.Frame);
        Assert.False(model.Load("missing",true));Assert.Same(controller,model.Current);Assert.Same(draft,model.Current.TravelDraft);
        Assert.True(model.Load("navigation",true));Assert.NotSame(controller,model.Current);Assert.False(model.Current.Dirty);
        Assert.Equal(ReviewWorkspacePage.Map,workspace.Page);
        Assert.Equal(CampaignTravelIntent.Stay,Assert.Single(model.Current.Frame.Travel!.OwnOrders).Intent);
    }
    [Fact]
    public void BattleEditsRemainUnsentAcrossPanelsAndWaitingAllowsReadonlyNavigation()
    {
        using var f=new UnityReviewFixture();var model=new ReviewUiModel(f.Package,f.Codec,f.Store,A);
        var workspace=new ReviewWorkspace(new ReviewCombatFlow(model));
        foreach(var actor in new[]{A,B})
        {
            Assert.True(model.SwitchActor(actor));Assert.True(model.DraftTravel(actor==A?FA:FB,CampaignTravelIntent.StayEngage,null,null));
            Assert.True(model.SendTravel("command.navigation.start."+actor));
        }
        Assert.True(model.Advance());Assert.True(model.SwitchActor(A));
        Assert.True(model.DraftFleet(FA,CampaignFleetManeuver.Screen,null,true));var draft=model.Current.Draft.Single();var frame=model.Current.Frame;
        foreach(var page in Enum.GetValues<ReviewWorkspacePage>())Assert.True(workspace.Navigate(page));
        Assert.Same(frame,model.Current.Frame);Assert.Same(draft,model.Current.Draft.Single());Assert.False(frame.Battle!.OwnCommitmentAccepted);
        Assert.True(model.CommitRound("command.navigation.commit"));Assert.Equal(ReviewCombatStage.Waiting,workspace.Combat.Stage);
        frame=model.Current.Frame;foreach(var page in Enum.GetValues<ReviewWorkspacePage>())Assert.True(workspace.Navigate(page));
        Assert.Same(frame,model.Current.Frame);Assert.Equal(ReviewCombatStage.Waiting,workspace.Combat.Stage);
    }
    [Fact]
    public void PendingWorkAndClosedModelsRejectNavigationWithoutDroppingDraft()
    {
        using var f=new UnityReviewFixture();var model=new ReviewUiModel(f.Package,f.Codec,f.Store,A);
        var workspace=new ReviewWorkspace(new ReviewCombatFlow(model));
        Assert.True(model.DraftTravel(FA,CampaignTravelIntent.StayEngage,null,null));var draft=model.Current.TravelDraft;
        Assert.True(model.Current.ReviewTravel());Assert.NotNull(model.Current.ConfirmTravel("command.navigation.pending"));
        Assert.False(workspace.CanNavigate);Assert.False(workspace.Navigate(ReviewWorkspacePage.Research));
        Assert.Equal(ReviewWorkspacePage.Map,workspace.Page);Assert.Same(draft,model.Current.TravelDraft);
        var closed=new ReviewUiModel(f.Package,f.Codec,f.Store,A);var other=new ReviewWorkspace(new ReviewCombatFlow(closed));
        Assert.True(closed.Close());Assert.False(other.CanNavigate);Assert.False(other.Navigate(ReviewWorkspacePage.Combat));
    }
    [Fact]
    public void ActorChangeResetsPresentationContextAndRejectsMalformedPanel()
    {
        using var f=new UnityReviewFixture();var model=new ReviewUiModel(f.Package,f.Codec,f.Store,A);
        var workspace=new ReviewWorkspace(new ReviewCombatFlow(model));Assert.True(workspace.Navigate(ReviewWorkspacePage.Combat));
        var epoch=workspace.NavigationEpoch;Assert.False(workspace.Navigate((ReviewWorkspacePage)999));
        Assert.Equal(epoch,workspace.NavigationEpoch);Assert.True(model.SwitchActor(B));Assert.Equal(ReviewWorkspacePage.Map,workspace.Page);
        Assert.True(workspace.NavigationEpoch>epoch);Assert.Contains(B,workspace.BoundaryIdentity);
    }
}
