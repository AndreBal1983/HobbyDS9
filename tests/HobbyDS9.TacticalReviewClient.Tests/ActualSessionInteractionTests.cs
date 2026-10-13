using System.Globalization;
using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Xunit;
using static HobbyDS9.TacticalReviewClient.Tests.ActualReviewFixture;

namespace HobbyDS9.TacticalReviewClient.Tests;

public sealed class ActualSessionInteractionTests
{
    [Fact]
    public void RealPackageStartsDisabledAndAllocatesAnOwnedDistantEncounter()
    {
        var f=new ActualReviewFixture();
        Assert.Equal("5406e3f89c513c8355ceee830421854508c34f24eac32aa1bfa5e6262fd762c5",Package().PackageHash);
        Assert.False(f.A.Frame.Status.ProductionActivationAllowed);
        Assert.Equal(ReviewMode.Planning,f.A.Mode);
        Assert.False(f.A.ReviewRound());
        f.Allocate();
        Assert.Equal(ReviewMode.Orders,f.A.Mode);
        Assert.All(f.A.Frame.Battle!.Relations,r=>Assert.Equal(TacticalRange.Distant,r.Range));
        Assert.Contains("physical-contact integration pending",ReviewTextRenderer.Render(f.A));
    }
    [Theory]
    [InlineData(CampaignFleetManeuver.Hold)]
    [InlineData(CampaignFleetManeuver.Charge)]
    [InlineData(CampaignFleetManeuver.Flank)]
    [InlineData(CampaignFleetManeuver.Screen)]
    [InlineData(CampaignFleetManeuver.FightingWithdrawal)]
    public void FleetMenuSubmitsOnlyActualLegalManeuverAndFleetFocus(CampaignFleetManeuver maneuver)
    {
        var f=new ActualReviewFixture(); f.Allocate();
        Assert.True(f.A.EditFleet(FleetA,maneuver,FleetB));
        var receipt=f.Send(f.A);
        Assert.True(receipt.Frame.Battle!.OwnCommitmentAccepted);
        var committed=Assert.Single(receipt.Frame.Battle.OwnCommittedOrders);
        Assert.Equal(maneuver,committed.Maneuver); Assert.Equal(FleetB,committed.FocusFleetId);
    }
    [Theory]
    [InlineData("foreign-fleet")]
    [InlineData("hidden-focus")]
    [InlineData("own-focus")]
    [InlineData("exit")]
    public void PresentationRejectsChoicesAbsentFromBackendMenu(string defect)
    {
        var f=new ActualReviewFixture(); f.Allocate();
        bool changed=defect switch {
            "foreign-fleet"=>f.A.EditFleet(FleetB,CampaignFleetManeuver.Hold,null),
            "hidden-focus"=>f.A.EditFleet(FleetA,CampaignFleetManeuver.Hold,"fleet.unknown"),
            "own-focus"=>f.A.EditFleet(FleetA,CampaignFleetManeuver.Hold,FleetA),
            _=>f.A.EditFleet(FleetA,(CampaignFleetManeuver)99,null)};
        Assert.False(changed); Assert.False(f.A.Dirty); Assert.False(f.A.Frame.Battle!.OwnCommitmentAccepted);
    }
    [Fact]
    public void AcceptanceWaitsAtTheSameBoundaryAndWithholdsOpposingFreshOrders()
    {
        var f=new ActualReviewFixture(); f.Allocate();
        var before=JsonSerializer.Serialize(f.B.Frame.Battle);
        var boundary=f.A.Frame.Battle!.BoundaryRevision; var global=f.A.Frame.Status.CampaignRevision;
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Flank,FleetB));
        f.Send(f.A); Assert.True(f.B.Refresh());
        Assert.Equal(global+1,f.A.Frame.Status.CampaignRevision);
        Assert.Equal(boundary,f.A.Frame.Battle!.BoundaryRevision);
        Assert.Equal(before,JsonSerializer.Serialize(f.B.Frame.Battle));
        Assert.Equal(ReviewMode.Waiting,f.A.Mode);
        Assert.Contains("Own accepted "+FleetA+": Flank",ReviewTextRenderer.Render(f.A));
        Assert.DoesNotContain("Own accepted "+FleetA,ReviewTextRenderer.Render(f.B));
        Assert.False(f.A.EditFleet(FleetA,CampaignFleetManeuver.Hold,null));
        Assert.False(f.A.ReviewRound());
    }
    [Fact]
    public void SecondCommitmentResolvesExactlyOneRoundAndKeepsStandingIntent()
    {
        var f=new ActualReviewFixture(); f.Allocate();
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Flank,FleetB)); f.Send(f.A); f.Send(f.B); f.A.Refresh();
        Assert.Equal(2,f.A.Frame.Battle!.Round); Assert.Equal(1,f.A.Frame.Battle.BoundaryRevision);
        Assert.Equal(CampaignFleetManeuver.Flank,Assert.Single(f.A.Frame.Battle.StandingOrders).Maneuver);
        Assert.False(f.A.Frame.Battle.Complete);
        f.Round();
        Assert.Equal(3,f.A.Frame.Battle!.Round);
        Assert.Equal(CampaignFleetManeuver.Flank,Assert.Single(f.A.Frame.Battle.StandingOrders).Maneuver);
    }
    [Fact]
    public void InspectionReportReplayAndRepeatedRenderCannotSpendResourcesOrResolveRounds()
    {
        var f=new ActualReviewFixture(); f.Allocate(); f.Round();
        var before=JsonSerializer.Serialize(f.A.Frame.Battle); var revision=f.A.Frame.Status.CampaignRevision;
        for(var i=0;i<10;i++)
        {
            Assert.True(f.A.InspectShip("ship.equipment-cruiser"));
            ReviewTextRenderer.Render(f.A); ReviewTextRenderer.RenderReport(f.A,1); Assert.True(f.A.Refresh());
        }
        Assert.Equal(before,JsonSerializer.Serialize(f.A.Frame.Battle)); Assert.Equal(revision,f.A.Frame.Status.CampaignRevision);
    }
    [Fact]
    public void BackendRejectionRetainsDraftAndRequiresExplicitRefreshWithoutRetry()
    {
        var f=new ActualReviewFixture(); f.Allocate();
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Screen,FleetB));
        var before=JsonSerializer.Serialize(f.A.Frame.Battle);
        var receipt=f.Host.Execute(Begin(f.A,"INVALID ID"));
        Assert.Equal(CampaignTacticalError.InvalidOrder,receipt.Result.Error); Assert.True(f.A.Receive(receipt));
        Assert.True(f.A.Dirty); Assert.True(f.A.RefreshRequired); Assert.False(f.A.ReviewRound());
        Assert.Equal(before,JsonSerializer.Serialize(f.A.Frame.Battle));
        Assert.True(f.A.Refresh()); Assert.True(f.A.Dirty); f.Send(f.A);
    }
    [Fact]
    public void StaleRoundResultCannotRollbackTheControllerOrAutomaticallyResubmitItsDraft()
    {
        var f=new ActualReviewFixture(); var encounter=f.Allocate();
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Screen,FleetB)); var stale=Begin(f.A);
        var other=new ReviewController(f.Host,ActorA); Assert.True(other.SelectEncounter(encounter));
        var first=f.Host.Execute(Begin(other,"command.other")); Assert.True(other.Receive(first));
        f.Send(f.B);
        var rejected=f.Host.Execute(stale); Assert.Equal(CampaignTacticalError.StaleBoundary,rejected.Result.Error);
        Assert.True(f.A.Receive(rejected)); Assert.True(f.A.Dirty); Assert.True(f.A.RefreshRequired);
        Assert.Equal(1,f.A.Frame.Battle!.Round);
        Assert.True(f.A.Refresh()); Assert.Equal(2,f.A.Frame.Battle!.Round);
        Assert.False(f.A.Dirty); Assert.True(f.A.DraftInvalidated);
        Assert.Contains("Draft invalidated",ReviewTextRenderer.Render(f.A));
    }
    [Fact]
    public void WrongControllerCallbackAndDuplicateDeliveryAreIgnored()
    {
        var f=new ActualReviewFixture(); f.Allocate();
        var a=Begin(f.A); var b=Begin(f.B);
        var ar=f.Host.Execute(a);
        Assert.False(f.B.Receive(ar)); Assert.True(f.B.HasPendingWork);
        Assert.True(f.A.Receive(ar)); Assert.False(f.A.Receive(ar));
        var br=f.Host.Execute(b); Assert.True(f.B.Receive(br)); Assert.False(f.A.Receive(br));
        f.A.Refresh(); Assert.Equal(2,f.A.Frame.Battle!.Round);
    }
    [Fact]
    public void DuplicateExecutionIsAnActualBackendErrorAndCannotApplyTwice()
    {
        var f=new ActualReviewFixture(); f.Allocate(); var work=Begin(f.A);
        var receipt=f.Host.Execute(work); Assert.True(f.A.Receive(receipt));
        var duplicate=f.Host.Execute(work); Assert.Equal(CampaignTacticalError.DuplicateCommand,duplicate.Result.Error);
        Assert.False(f.A.Receive(duplicate)); Assert.Equal(receipt.Frame.Status.CampaignRevision,duplicate.Frame.Status.CampaignRevision);
        Assert.Equal(ReviewMode.Waiting,f.A.Mode);
    }
    [Fact]
    public void AnotherControllerAcceptedOwnOrdersCannotBeOverwritten()
    {
        var f=new ActualReviewFixture(); var encounter=f.Allocate();
        var second=new ReviewController(f.Host,ActorA); second.SelectEncounter(encounter);
        f.Send(f.A);
        var receipt=f.Host.Execute(Begin(second,"command.other-controller"));
        Assert.Equal(CampaignTacticalError.AlreadyCommitted,receipt.Result.Error); Assert.True(second.Receive(receipt));
        Assert.True(second.RefreshRequired); second.Refresh(); Assert.Equal(ReviewMode.Waiting,second.Mode);
    }
    [Fact]
    public void AForeignHostCannotExecuteAnOtherwiseValidWorkItem()
    {
        var f=new ActualReviewFixture(); f.Allocate(); var work=Begin(f.A);
        var foreign=new ActualReviewFixture(); foreign.Allocate();
        Assert.Throws<InvalidOperationException>(()=>foreign.Host.Execute(work));
        Assert.False(foreign.A.Frame.Battle!.OwnCommitmentAccepted);
    }
    [Fact]
    public void ExplicitConfirmationAndDoubleClickGuardProtectRoundAndCompletion()
    {
        var f=new ActualReviewFixture(); f.Allocate();
        Assert.Null(f.A.ConfirmRound("command.unreviewed")); Assert.False(f.A.ReviewCompletion());
        var work=Begin(f.A); Assert.Null(f.A.ConfirmRound("command.double"));
        Assert.False(f.A.Refresh()); Assert.False(f.A.ResetDraft());
        Assert.Throws<InvalidOperationException>(()=>f.A.SaveCommitted());
        f.A.Receive(f.Host.Execute(work)); f.Send(f.B); f.A.Refresh(); f.Complete();
        Assert.Null(f.A.ConfirmCompletion()); Assert.True(f.A.ReviewCompletion());
        Assert.True(f.A.CancelReview()); Assert.Equal(ReviewMode.Complete,f.A.Mode);
        Assert.True(f.A.ReviewCompletion()); var end=f.A.ConfirmCompletion(); Assert.NotNull(end); Assert.Null(f.A.ConfirmCompletion());
        f.A.Receive(f.Host.Execute(end!)); Assert.Equal(ReviewMode.Planning,f.A.Mode); Assert.False(f.A.ReviewCompletion());
    }
    [Fact]
    public void AllThreeRoundsCompleteAndFinalizeOnceWithPersistentOwnPools()
    {
        var f=new ActualReviewFixture(); f.Allocate(); f.Complete();
        Assert.True(f.A.Frame.Battle!.Complete); Assert.Equal(4,f.A.Frame.Battle.Round);
        Assert.False(f.A.ReviewRound()); Assert.True(f.A.Frame.Status.CanFinalizeBatch);
        var stocks=JsonSerializer.Serialize(f.A.Frame.OwnShips.Select(x=>new{x.Condition.ShipId,x.Condition.Hull,x.Condition.Shields,Ammo=x.Weapons.Select(w=>w.Ammunition)}));
        var oldTurn=f.A.Frame.Status.Turn; Assert.True(f.A.ReviewCompletion());
        var work=Assert.IsType<ReviewWork>(f.A.ConfirmCompletion());
        var end=f.Host.Execute(work); Assert.True(end.Result.Accepted); Assert.True(f.A.Receive(end));
        Assert.Equal(oldTurn+1,f.A.Frame.Status.Turn); Assert.Empty(f.A.Frame.Encounters);
        Assert.Equal(stocks,JsonSerializer.Serialize(f.A.Frame.OwnShips.Select(x=>new{x.Condition.ShipId,x.Condition.Hull,x.Condition.Shields,Ammo=x.Weapons.Select(w=>w.Ammunition)})));
        var duplicate=f.Host.Execute(work);
        Assert.Equal(CampaignTacticalError.WrongPhase,duplicate.Result.Error); Assert.False(f.A.Receive(duplicate));
    }
}
