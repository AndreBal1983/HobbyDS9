using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Xunit;
using static HobbyDS9.TacticalReviewClient.Tests.ActualReviewFixture;

namespace HobbyDS9.TacticalReviewClient.Tests;

public sealed class ActualFleetAndLifecycleTests
{
    private static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    [Theory]
    [InlineData(true,2)]
    [InlineData(false,0)]
    public void SameFleetScreeningUsesRealBackendAttemptsAndSeparateFleetsKeepSeparateControls(bool sameFleet,int attempts)
    {
        var f=new ActualReviewFixture(p=>{
            var g=p.Campaign!.InitialState; g.Ships!.Single(s=>s.ShipId == "ship.equipment-surveyor").CurrentHull=50;
            if(!sameFleet)
            {
                var fleet=Clone(g.Fleets!.Single(x=>x.FleetId == FleetA)); fleet.FleetId="fleet.separate-escort";
                fleet.ShipIds=new List<string>{"ship.equipment-cruiser"}; g.Fleets!.Add(fleet);
                g.Fleets!.Single(x=>x.FleetId == FleetA).ShipIds!.Remove("ship.equipment-cruiser");
                g.Ships!.Single(x=>x.ShipId == "ship.equipment-cruiser").FleetId=fleet.FleetId;
                var identity=Clone(g.EntityIdentities.Single(x=>x.EntityId == FleetA)); identity.EntityId=fleet.FleetId; g.EntityIdentities.Add(identity);
            }
        });
        f.Allocate();
        Assert.Equal(sameFleet ? 1 : 2,f.A.Frame.Battle!.StandingOrders.Count);
        foreach(var order in f.A.Frame.Battle.StandingOrders)
            Assert.True(f.A.EditFleet(order.FleetId,CampaignFleetManeuver.Screen,order.FocusFleetId));
        Assert.True(f.B.EditFleet(FleetB,CampaignFleetManeuver.Hold,FleetA));
        f.Send(f.A); f.Send(f.B); f.A.Refresh();
        var report=f.A.Frame.Battle!.Reports;
        Assert.Equal(attempts,report.Count(r=>r.Kind == "InterceptionAttempt" && r.ShipId == "ship.equipment-cruiser"));
        Assert.Contains("Screening stays within the same fleet",ReviewTextRenderer.Render(f.A));
        Assert.DoesNotContain(typeof(ReviewWork).GetProperties(),p=>p.Name.Contains("Protect",StringComparison.Ordinal));
    }
    [Fact]
    public void DestroyedFocusStaysAsIntentWhileTypedReportExplainsLawfulFallback()
    {
        // Controlled variant of the actual authored package; not a new accepted balance profile.
        var f=new ActualReviewFixture(p=>{
            var g=p.Campaign!.InitialState; var catalogue=p.Campaign.Definitions.AuthoredTacticalEquipment!;
            var ship=Clone(g.Ships!.Single(s=>s.ShipId == "ship.cardassian-scout"));
            ship.ShipId="ship.additional-opponent"; ship.OwnerCivilizationId="civ.dominion"; ship.FleetId="fleet.additional-opponent"; g.Ships!.Add(ship);
            var fleet=Clone(g.Fleets!.Single(s=>s.FleetId == FleetB));
            fleet.FleetId=ship.FleetId; fleet.OwnerCivilizationId=ship.OwnerCivilizationId; fleet.ShipIds=new List<string>{ship.ShipId}; g.Fleets!.Add(fleet);
            foreach(var pair in new[]{("ship.cardassian-scout",ship.ShipId),(FleetB,fleet.FleetId)})
            { var identity=Clone(g.EntityIdentities.Single(e=>e.EntityId == pair.Item1)); identity.EntityId=pair.Item2; g.EntityIdentities.Add(identity); }
            var provision=Clone(catalogue.InitialShipProvisions!.Single(s=>s.ShipId == "ship.cardassian-scout"));
            provision.ShipId=ship.ShipId; provision.OwnerCivilizationId=ship.OwnerCivilizationId; catalogue.InitialShipProvisions!.Add(provision);
            g.Ships!.Single(s=>s.ShipId == "ship.cardassian-scout").CurrentHull=1;
            foreach(var fit in catalogue.Fits!)
            { fit.EvasionBps=0; foreach(var w in fit.Weapons!) { w.AccuracyBps=10000; w.CadenceRounds=1; } }
            p.Rules!.DistantTorpedoHitPenaltyBps=0; p.Rules.HoldDefenseBps=0;
        });
        var id=f.Allocate(); var third=new ReviewController(f.Host,"civ.dominion"); Assert.True(third.SelectEncounter(id));
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Hold,FleetB));
        f.Send(f.A); f.Send(f.B); f.Send(third); f.A.Refresh(); third.Refresh();
        Assert.DoesNotContain(f.A.Frame.Battle!.VisibleEnemies,s=>s.ShipId == "ship.cardassian-scout");
        Assert.False(f.A.EditFleet(FleetA,CampaignFleetManeuver.Screen,"fleet.unknown"));
        Assert.False(f.A.EditFleet(FleetA,CampaignFleetManeuver.Screen,FleetA));
        Assert.True(f.A.EditFleet(FleetA,CampaignFleetManeuver.Screen,FleetB));
        f.Send(f.A); f.Send(third); f.A.Refresh();
        Assert.Equal(FleetB,Assert.Single(f.A.Frame.Battle!.StandingOrders).FocusFleetId);
        Assert.Equal(CampaignFleetManeuver.Screen,Assert.Single(f.A.Frame.Battle.StandingOrders).Maneuver);
        Assert.Contains(f.A.Frame.Battle.Reports,r=>r.Kind == "FocusFallback" && r.TargetShipId == "ship.additional-opponent");
        Assert.Contains("Authoritative focus fallback reported",ReviewTextRenderer.RenderReport(f.A));
        Assert.DoesNotContain("focus-fallback:",ReviewTextRenderer.RenderReport(f.A));
    }
    [Fact]
    public void StaleGlobalFinalizationReturnsBackendRejectionAndKeepsTerminalReports()
    {
        var f=new ActualReviewFixture(); var id=f.Allocate(); f.Complete();
        Assert.True(f.A.ReviewCompletion()); var stale=Assert.IsType<ReviewWork>(f.A.ConfirmCompletion());
        var other=new ReviewController(f.Host,ActorA); Assert.True(other.SelectEncounter(id)); Assert.True(other.ReviewCompletion());
        var work=Assert.IsType<ReviewWork>(other.ConfirmCompletion());
        var completed=f.Host.Execute(work); Assert.True(other.Receive(completed));
        var oldReport=ReviewTextRenderer.RenderReport(f.A);
        var rejected=f.Host.Execute(stale);
        Assert.Equal(CampaignTacticalError.WrongPhase,rejected.Result.Error); Assert.True(f.A.Receive(rejected));
        Assert.Equal(oldReport,ReviewTextRenderer.RenderReport(f.A)); Assert.True(f.A.RefreshRequired);
        Assert.True(f.A.Refresh()); Assert.Equal(ReviewMode.Planning,f.A.Mode); Assert.False(f.A.ReviewRound());
    }
    [Fact]
    public void AReloadByAnotherActorInvalidatesAnOutstandingReceiptWithoutApplyingIt()
    {
        var f=new ActualReviewFixture(); f.Allocate(); var checkpoint=f.A.SaveCommitted();
        var work=Begin(f.A); var receipt=f.Host.Execute(work);
        Assert.True(f.B.LoadCommitted(checkpoint));
        var before=f.A.Frame;
        Assert.False(f.A.Receive(receipt)); Assert.Same(before,f.A.Frame);
        Assert.True(f.A.RefreshRequired); Assert.False(f.A.HasPendingWork);
        Assert.Throws<InvalidOperationException>(()=>f.Host.Execute(work));
        var rebound=new ReviewController(f.Host,ActorA); rebound.SelectEncounter(Assert.Single(rebound.Frame.Encounters));
        Assert.False(rebound.Frame.Battle!.OwnCommitmentAccepted);
    }
    [Fact]
    public void UnknownOrUncontrolledActorCannotCreateAPresenterObservation()
    {
        var f=new ActualReviewFixture();
        Assert.Throws<ArgumentException>(()=>new ReviewController(f.Host,"civ.unknown"));
        var ai=new ActualReviewFixture(p=>p.Campaign!.InitialState.Civilizations!.Single(c=>c.CivilizationId == "civ.dominion").ControllerKind=ControllerKind.AI);
        Assert.Throws<ArgumentException>(()=>new ReviewController(ai.Host,"civ.dominion"));
    }
    [Fact]
    public void ActualObservationCollectionsRemainImmutableAndOldFramesRetainTheirFacts()
    {
        var f=new ActualReviewFixture(); f.Allocate(); var frame=f.A.Frame;
        var json=JsonSerializer.Serialize(frame.Battle);
        Assert.Throws<NotSupportedException>(()=>((IList<long>)frame.OwnShips[0].Condition.Shields)[0]=999);
        Assert.Throws<NotSupportedException>(()=>((IList<CampaignTacticalCondition>)frame.Battle!.VisibleEnemies).Clear());
        f.Round(); Assert.Equal(json,JsonSerializer.Serialize(frame.Battle));
        Assert.NotEqual(frame.Battle!.Round,f.A.Frame.Battle!.Round);
    }
    [Fact]
    public void EnemyConditionRemainsLawfulAfterSavedCompletionAndFinalizationRemovesCurrentBatchReports()
    {
        var f=new ActualReviewFixture(); f.Allocate(); f.Complete();
        var visible=JsonSerializer.Serialize(f.A.Frame.Battle!.VisibleEnemies); var reports=ReviewTextRenderer.RenderReport(f.A);
        var handle=f.A.SaveCommitted(); f.A.LoadCommitted(handle); f.RebindB();
        Assert.Equal(visible,JsonSerializer.Serialize(f.A.Frame.Battle!.VisibleEnemies)); Assert.Equal(reports,ReviewTextRenderer.RenderReport(f.A));
        f.Finalize(); Assert.Null(f.A.Frame.Battle);
        Assert.Contains("No permanent campaign archive",ReviewTextRenderer.RenderReport(f.A));
        Assert.DoesNotContain("Visible enemy",ReviewTextRenderer.Render(f.A));
    }
}
