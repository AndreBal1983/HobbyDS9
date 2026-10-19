using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class FrontierCombatPresentationTests
{
    [Fact] public void ActualFrontierBattleHasOwnedFiniteAftermathWithoutDemoVictorySemantics()
    {
        var codec=new ReviewNewtonsoftCodec();
        var package=FrontierPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/frontier-reconnaissance-v1"));
        var physical=package.Integrated!.Physical!;var scenario=physical.CorePackage!.Campaign!;
        scenario.InitialState.Fleets!.Single(f=>f.FleetId=="fleet.cardassian-scout").LocationNodeId="nav.bajor";
        scenario.Definitions.NavigationConnections!.Single(c=>c.ConnectionId=="connection.sol-bajor").BaseTraversalCost=1;
        var knowledge=scenario.InitialState.NavigationKnowledge!.Single(k=>k.CivilizationId=="civ.cardassian");
        foreach(var node in knowledge.Nodes!)node.Level=NavigationKnowledgeLevel.Charted;
        foreach(var lane in knowledge.Connections!)lane.Level=NavigationKnowledgeLevel.Charted;
        physical.InitialOrders!.Single(o=>o.FleetId=="fleet.cardassian-scout").Intent=CampaignTravelIntent.StayEngage;
        physical.InitialContacts!.Add(new(){ActorId="civ.cardassian",FleetId="fleet.federation-scout",LastSeenNodeId="nav.sol",LastSeenTurn=1,TrackingNodeIds=knowledge.Nodes!.Select(n=>n.Id).ToList()});
        // The store is never written; only an injected test-owned path is supplied.
        var model=new ReviewUiModel(package,codec,new ReviewFrontierSlotStore(Path.Combine(Path.GetTempPath(),"hobby-frontier-flow-"+Guid.NewGuid().ToString("N")),codec));
        var flow=new ReviewCombatFlow(model);
        Assert.Equal("Combat — strategic Planning",flow.Heading);Assert.False(flow.CampaignComplete);
        Assert.True(flow.DraftNextOrder("fleet.federation-scout",CampaignTravelIntent.Leave,"nav.bajor"));
        Assert.True(flow.SubmitNextOrder(ReviewUiModel.NewCommandId(),true));Assert.True(flow.AdvancePlanning(true));
        Assert.NotNull(model.Current.Frame.Battle);Assert.Null(model.Current.Frame.Demo);
        var rounds=0;
        while(!model.Current.Frame.Battle!.Complete)
        {
            Assert.InRange(++rounds,1,3);
            if(flow.Stage==ReviewCombatStage.RoundResult)Assert.True(flow.PlanNextRound());
            Assert.True(flow.CommitRound(ReviewUiModel.NewCommandId(),true));
        }
        var completed=model.Current.Frame.Battle;
        Assert.Null(flow.AftermathSummary);Assert.False(flow.FinalizeAftermath(false));
        Assert.True(flow.FinalizeAftermath(true));var summary=Assert.IsType<ReviewAftermathSummary>(flow.AftermathSummary);
        Assert.Equal(ReviewCombatStage.AftermathConfirmed,flow.Stage);Assert.False(flow.CampaignComplete);Assert.Null(model.Current.Frame.Demo);
        foreach(var survivor in summary.Survivors)
        {
            var staged=completed.OwnShips.Single(s=>s.Condition.ShipId==survivor.Condition.ShipId);
            Assert.Equal(staged.Condition.Hull,survivor.Condition.Hull);Assert.Equal(staged.Condition.Shields,survivor.Condition.Shields);
            foreach(var weapon in staged.Weapons.Where(w=>w.Class==TacticalWeaponClass.Torpedo))
                Assert.Equal(weapon.Ammunition,survivor.Weapons.Single(w=>w.WeaponId==weapon.WeaponId).Ammunition);
        }
        Assert.Equal(completed.OwnShips.Where(s=>s.Condition.Hull==0).Select(s=>s.Condition.ShipId).OrderBy(x=>x),summary.LostShipIds.OrderBy(x=>x));
        Assert.DoesNotContain("civ.cardassian",JsonSerializer.Serialize(summary));Assert.DoesNotContain("VisibleEnemies",JsonSerializer.Serialize(summary));
        Assert.Contains("Selected encounter only",new ReviewDemoCombatText().Aftermath(summary));
        var frame=model.Current.Frame;Assert.False(flow.FinalizeAftermath(true));Assert.Same(frame,model.Current.Frame);Assert.Same(summary,flow.AftermathSummary);
    }
}
