using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class DemoCombatPresentationTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public void CompletedCampaignBlocksCombatOrdersButKeepsInspectionAndSaveResume(bool pursueVictory)
    {
        using var fixture=new DemoUiFixture();var model=fixture.Model;var flow=new ReviewCombatFlow(model);
        Assert.False(flow.CampaignComplete);Assert.True(flow.CanDraftNextOrder);
        if(pursueVictory)
        {
            Assert.True(model.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()));
            Assert.True(model.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()));
            Assert.True(model.AllocateResearch(450,ReviewUiModel.NewCommandId()));
        }
        for(int step=0;step<30&&model.Current.Frame.Demo!.Outcome==CampaignDemoOutcome.Active;step++)
        {
            var battle=model.Current.Frame.Battle;
            if(battle!=null)
            {
                if(battle.Complete){Assert.True(flow.FinalizeAftermath(true));continue;}
                if(flow.Stage==ReviewCombatStage.RoundResult)Assert.True(flow.PlanNextRound());
                foreach(var order in battle.StandingOrders.Where(o=>o.CanOrder))
                    Assert.True(flow.DraftFleet(order.FleetId,CampaignFleetManeuver.Charge,battle.LegalFocusFleetIds.FirstOrDefault(),false));
                Assert.True(flow.CommitRound(ReviewUiModel.NewCommandId(),true));
            }
            else
            {
                if(pursueVictory&&model.Current.Frame.Status.Turn>=5)
                    foreach(var fleet in model.Current.Frame.OwnStrategicFleets)
                    {
                        var atObjective=fleet.LocationNodeId.Value==model.Current.Frame.Demo!.ObjectiveNodeId;
                        Assert.True(flow.DraftNextOrder(fleet.FleetId.Value,atObjective?CampaignTravelIntent.StayEngage:CampaignTravelIntent.Leave,atObjective?null:model.Current.Frame.Demo.ObjectiveNodeId));
                        Assert.True(flow.SubmitNextOrder(ReviewUiModel.NewCommandId(),true));
                    }
                Assert.True(flow.AdvancePlanning(true));
            }
        }
        Assert.Equal(pursueVictory?CampaignDemoOutcome.Victory:CampaignDemoOutcome.Defeat,model.Current.Frame.Demo!.Outcome);
        Assert.NotEmpty(model.Current.Frame.OwnStrategicFleets);Assert.True(model.Save("completed-before"));
        var before=fixture.Codec.EncodeDemoRoot(fixture.Store.Read("completed-before"));
        void CheckCompleted()
        {
            Assert.True(flow.CampaignComplete);Assert.Equal(ReviewCombatStage.CampaignComplete,flow.Stage);
            Assert.Contains(model.Current.Frame.Demo!.Outcome.ToString(),flow.Heading);Assert.Contains("inspection",flow.Heading);
            Assert.False(flow.CanDraftNextOrder);Assert.False(flow.CanCommitRound);Assert.False(flow.CanFinalize);
            var fleet=model.Current.Frame.OwnStrategicFleets.First().FleetId.Value;
            Assert.Null(flow.PreviewRoute(fleet,"nav.bajor"));Assert.False(flow.DraftNextOrder(fleet,CampaignTravelIntent.StayEngage));
            Assert.False(flow.SubmitNextOrder(ReviewUiModel.NewCommandId(),true));Assert.False(flow.AdvancePlanning(true));
            Assert.False(flow.CommitRound(ReviewUiModel.NewCommandId(),true));Assert.False(flow.FinalizeAftermath(true));
            Assert.Contains("Completed campaign boundary",flow.StageDiagram());Assert.Contains("Campaign complete",new ReviewDemoCombatText().Diagram(model.Current.Frame));
            Assert.DoesNotContain("commit one round",new ReviewDemoCombatText().LatestRound(model.Current.Frame));
            foreach(var ship in model.Current.Frame.OwnShips)Assert.True(model.Inspect(ship.Condition.ShipId));
        }
        CheckCompleted();Assert.True(model.Save("completed-after"));
        Assert.Equal(before,fixture.Codec.EncodeDemoRoot(fixture.Store.Read("completed-after")));
        Assert.True(model.Load("completed-after",true));CheckCompleted();Assert.True(model.Save("completed-reloaded"));
        Assert.Equal(before,fixture.Codec.EncodeDemoRoot(fixture.Store.Read("completed-reloaded")));
    }

    [Fact] public void FocusCaptionsKeepDistinctChoicesAndUseOnlyObservedFleetAliases()
    {
        using var fixture=new DemoUiFixture();var model=fixture.Model;EnterBattle(model);var frame=model.Current.Frame;var text=new ReviewDemoCombatText();
        Assert.Equal("Keep focus",text.Focus(frame,"retain standing focus"));Assert.Equal("No preference",text.Focus(frame,"clear focus"));
        foreach(var id in frame.Battle!.LegalFocusFleetIds)Assert.Equal(text.Fleet(frame,id),text.Focus(frame,id));
        Assert.Equal("Previously selected fleet (unavailable)",text.Focus(frame,"fleet.not-observed"));
    }

    private static void EnterBattle(ReviewUiModel model)
    {
        Assert.True(model.DraftTravel("fleet.federation-scout",CampaignTravelIntent.Leave,"nav.bajor",null));
        Assert.True(model.SendTravel(ReviewUiModel.NewCommandId()));
        int limit=0;
        while(model.Current.Frame.Status.Phase==SessionPhase.Planning&&model.Current.Frame.OwnStrategicFleets.Single().LocationNodeId.Value!="nav.bajor"&&limit++<5)Assert.True(model.Advance());
        if(model.Current.Frame.Status.Phase==SessionPhase.Planning)
        {Assert.True(model.DraftTravel("fleet.federation-scout",CampaignTravelIntent.StayEngage,null,null));Assert.True(model.SendTravel(ReviewUiModel.NewCommandId()));Assert.True(model.Advance());}
        Assert.NotNull(model.Current.Frame.Battle);
    }

    [Fact] public void ReadableJointRoundUsesPermittedFactsWithoutChangingCompleteSave()
    {
        using var fixture=new DemoUiFixture();var model=fixture.Model;EnterBattle(model);
        Assert.True(model.CommitRound(ReviewUiModel.NewCommandId()));
        var frame=model.Current.Frame;Assert.NotEmpty(frame.Battle!.Reports);Assert.True(model.Save("before-text"));
        var before=fixture.Codec.EncodeDemoRoot(fixture.Store.Read("before-text"));var text=new ReviewDemoCombatText();
        var summary=text.LatestRound(frame);var status=text.Stocks(frame);var display=summary+status+text.Diagram(frame)+text.Orders(model.Current);
        Assert.Contains("Round "+frame.Battle.Reports.Max(e=>e.Round)+" resolved",summary);
        foreach(var own in frame.OwnShips)
        {Assert.Contains($"Hull {own.Condition.Hull} / {own.Condition.MaxHull}",status);Assert.DoesNotContain(own.Condition.ShipId,display);Assert.DoesNotContain(own.Condition.FleetId,display);}
        foreach(var enemy in frame.Battle.VisibleEnemies){Assert.DoesNotContain(enemy.ShipId,display);Assert.DoesNotContain(enemy.FleetId,display);}
        Assert.DoesNotContain("PROVISIONAL",display);Assert.DoesNotContain("inert v1",display);
        Assert.DoesNotContain("Random",display);Assert.Contains("filtered report",summary);
        Assert.Same(frame,model.Current.Frame);Assert.True(model.Save("after-text"));
        Assert.Equal(before,fixture.Codec.EncodeDemoRoot(fixture.Store.Read("after-text")));
    }

    [Fact] public void OwnedAliasesRemainStableWhenPaidConstructionAddsShipsAndUnknownIdsStayUnknown()
    {
        using var fixture=new DemoUiFixture();var model=fixture.Model;var text=new ReviewDemoCombatText();
        var id=model.Current.Frame.OwnShips.Single().Condition.ShipId;var original=text.Ship(model.Current.Frame,id);
        Assert.Equal("No current ship observation",text.Ship(model.Current.Frame,"ship.not-observed"));
        Assert.True(model.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()));
        Assert.True(model.Advance());Assert.True(model.Advance());
        Assert.Equal(original,text.Ship(model.Current.Frame,id));
        var added=model.Current.Frame.OwnShips.Single(s=>s.Condition.ShipId!=id);
        Assert.NotEqual(original,text.Ship(model.Current.Frame,added.Condition.ShipId));
        Assert.DoesNotContain("not-observed",text.Stocks(model.Current.Frame));
        Assert.Equal("No current ship observation",text.Ship(model.Current.Frame,"ship.not-observed"));
    }
}
