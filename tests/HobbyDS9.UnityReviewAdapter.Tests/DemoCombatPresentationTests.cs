using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class DemoCombatPresentationTests
{
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
