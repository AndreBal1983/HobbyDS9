using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;
using static HobbyDS9.UnityReviewAdapter.Tests.UnityReviewFixture;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class DemoAftermathSummaryTests
{
    private static void CompleteDemoBattle(ReviewUiModel model,ReviewCombatFlow flow)
    {
        Assert.True(model.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()));
        Assert.True(model.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()));
        Assert.True(model.AllocateResearch(450,ReviewUiModel.NewCommandId()));
        while(model.Current.Frame.Status.Turn<5)Assert.True(model.Advance());
        Assert.Equal(3,model.Current.Frame.OwnShips.Count);var fleet=model.Current.Frame.OwnStrategicFleets.Single().FleetId.Value;
        Assert.True(flow.DraftNextOrder(fleet,CampaignTravelIntent.Leave,"nav.bajor"));Assert.True(flow.SubmitNextOrder(ReviewUiModel.NewCommandId(),true));Assert.True(flow.AdvancePlanning(true));
        Assert.NotNull(model.Current.Frame.Battle);
        while(!model.Current.Frame.Battle!.Complete)
        {
            if(flow.Stage==ReviewCombatStage.RoundResult)Assert.True(flow.PlanNextRound());
            var b=model.Current.Frame.Battle;
            foreach(var order in b.StandingOrders.Where(o=>o.CanOrder))Assert.True(flow.DraftFleet(order.FleetId,CampaignFleetManeuver.Charge,b.LegalFocusFleetIds.FirstOrDefault(),false));
            Assert.True(flow.CommitRound(ReviewUiModel.NewCommandId(),true));
        }
    }
    [Fact] public void RealPaidThreeShipBattlePublishesOnlyConfirmedOwnLossAndSurvivorStocksWithoutChangingSavedBytes()
    {
        using var f=new DemoUiFixture();var m=f.Model;var flow=new ReviewCombatFlow(m);CompleteDemoBattle(m,flow);
        var completed=m.Current.Frame.Battle!;var lost=completed.OwnShips.Where(s=>s.Condition.Hull==0).Select(s=>s.Condition.ShipId).ToArray();Assert.Single(lost);
        Assert.Null(flow.AftermathSummary);Assert.False(flow.FinalizeAftermath(false));Assert.Null(flow.AftermathSummary);
        Assert.True(flow.FinalizeAftermath(true));var summary=Assert.IsType<ReviewAftermathSummary>(flow.AftermathSummary);
        Assert.Equal(completed.EncounterId,summary.EncounterId);Assert.Equal(completed.Turn,summary.BattleTurn);Assert.Equal(completed.Turn+1,summary.PlanningTurn);Assert.Equal(lost,summary.LostShipIds);
        Assert.Equal(2,summary.Survivors.Count);Assert.Null(m.Current.Frame.Battle);Assert.Equal(ReviewCombatStage.AftermathConfirmed,flow.Stage);
        foreach(var own in summary.Survivors)
        {
            var current=m.Current.Frame.OwnShips.Single(s=>s.Condition.ShipId==own.Condition.ShipId);Assert.Same(current,own);
            Assert.Equal(3,own.Weapons.Single(w=>w.Class==TacticalWeaponClass.Torpedo).Ammunition);
            Assert.Equal(completed.OwnShips.Single(s=>s.Condition.ShipId==own.Condition.ShipId).Condition.Shields,own.Condition.Shields);
        }
        Assert.True(m.Save("before-display"));var before=f.Codec.EncodeDemoRoot(f.Store.Read("before-display"));var frame=m.Current.Frame;
        var text=new ReviewDemoCombatText().Aftermath(summary);Assert.Contains("Own ships lost: 1",text);Assert.Contains("own survivors: 2",text);Assert.Contains("Torpedoes 3/12",text);Assert.Contains("Selected encounter only",text);
        Assert.Throws<NotSupportedException>(()=>((IList<string>)summary.LostShipIds).Clear());
        Assert.Throws<NotSupportedException>(()=>((IList<CampaignTacticalOwnShip>)summary.Survivors).Clear());
        Assert.DoesNotContain("civ.cardassian",JsonSerializer.Serialize(summary));Assert.DoesNotContain("VisibleEnemies",JsonSerializer.Serialize(summary));Assert.DoesNotContain("Reports",JsonSerializer.Serialize(summary));
        foreach(var own in completed.OwnShips)Assert.DoesNotContain(own.Condition.ShipId,text);
        Assert.Same(frame,m.Current.Frame);Assert.False(flow.FinalizeAftermath(true));Assert.Same(summary,flow.AftermathSummary);
        Assert.True(m.Save("after-display"));Assert.Equal(before,f.Codec.EncodeDemoRoot(f.Store.Read("after-display")));
    }
    [Fact] public void NavigationLocalOrdersSaveAndFailedLoadKeepSummaryButSuccessfulLoadDoesNotInventHistory()
    {
        bool fail=false;using var f=new DemoUiFixture(stage=>{if(fail&&stage==UnityReviewWriteStage.BeforeReplace)throw new IOException("test-only");});var m=f.Model;var flow=new ReviewCombatFlow(m);CompleteDemoBattle(m,flow);Assert.True(flow.FinalizeAftermath(true));
        var summary=flow.AftermathSummary;Assert.NotNull(summary);var workspace=new ReviewWorkspace(flow);
        foreach(var page in new[]{ReviewWorkspacePage.Map,ReviewWorkspacePage.Research,ReviewWorkspacePage.Combat}){Assert.True(workspace.Navigate(page));Assert.Same(summary,flow.AftermathSummary);}
        Assert.True(flow.DraftNextOrder(m.Current.Frame.OwnStrategicFleets.First().FleetId.Value,CampaignTravelIntent.StayEngage));Assert.Same(summary,flow.AftermathSummary);
        Assert.True(flow.SubmitNextOrder(ReviewUiModel.NewCommandId(),true));Assert.Same(summary,flow.AftermathSummary);
        Assert.True(flow.DraftNextOrder(m.Current.Frame.OwnStrategicFleets.First().FleetId.Value,CampaignTravelIntent.Stay));
        Assert.True(m.Save("boundary"));var bytes=f.Codec.EncodeDemoRoot(f.Store.Read("boundary"));
        Assert.False(m.Load("missing",true));Assert.Same(summary,flow.AftermathSummary);Assert.NotNull(m.Current.TravelDraft);
        fail=true;Assert.False(m.Save("boundary",true));Assert.Same(summary,flow.AftermathSummary);Assert.Equal(bytes,f.Codec.EncodeDemoRoot(f.Store.Read("boundary")));fail=false;
        Assert.True(m.Load("boundary",true));Assert.Null(flow.AftermathSummary);Assert.Null(new ReviewCombatFlow(m).AftermathSummary);
        Assert.Contains("No battle summary is retained",new ReviewDemoCombatText().LatestRound(m.Current.Frame));Assert.DoesNotContain("commit one round",new ReviewDemoCombatText().LatestRound(m.Current.Frame));
        Assert.True(m.Save("resumed"));Assert.Equal(bytes,f.Codec.EncodeDemoRoot(f.Store.Read("resumed")));
    }
    [Theory][InlineData(false)][InlineData(true)] public void DirectAdvanceOrCloseInvalidatesSessionReceipt(bool close)
    {
        using var f=new DemoUiFixture();var m=f.Model;var flow=new ReviewCombatFlow(m);CompleteDemoBattle(m,flow);Assert.True(flow.FinalizeAftermath(true));Assert.NotNull(flow.AftermathSummary);
        Assert.False(m.SwitchActor(B));Assert.NotNull(flow.AftermathSummary);
        if(close)Assert.True(m.Close(true));else Assert.True(m.Advance());
        Assert.Null(flow.AftermathSummary);
    }
    [Fact] public void AnotherRealControllerFinalizingFirstCannotPublishAFalseAppliedReceipt()
    {
        using var f=new DemoUiFixture();var m=f.Model;var flow=new ReviewCombatFlow(m);CompleteDemoBattle(m,flow);var staged=m.Current.Frame;
        var host=(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(m)!;
        var other=new ReviewController(host,A);Assert.True(other.SelectEncounter(staged.Battle!.EncounterId));Assert.True(other.ReviewCompletion());var receipt=other.ExecutePending(other.ConfirmCompletion()!);Assert.True(other.Receive(receipt));Assert.True(receipt.Result.Accepted);
        Assert.False(flow.FinalizeAftermath(true));Assert.True(m.Current.RefreshRequired);Assert.Same(staged,m.Current.Frame);Assert.Null(flow.AftermathSummary);
        Assert.True(m.Refresh());Assert.Null(m.Current.Frame.Battle);Assert.Null(flow.AftermathSummary);Assert.DoesNotContain("aftermath confirmed",new ReviewDemoCombatText().LatestRound(m.Current.Frame));
    }
    [Fact] public void MultiEncounterReceiptIsOnlyTheSelectedOwnEncounterAndActorSwitchClearsIt()
    {
        using var f=new UnityReviewFixture(adjust:p=>{
            var g=p.CorePackage!.Campaign!.InitialState;var catalogue=p.CorePackage.Campaign.Definitions.AuthoredTacticalEquipment!;
            foreach(var id in new[]{FA,FB})
            {
                var original=g.Fleets!.Single(x=>x.FleetId==id);var originalShip=g.Ships!.Single(s=>s.FleetId==id);var fleet=Clone(original);fleet.FleetId=id+".second";fleet.LocationNodeId="nav.cardassia";fleet.RouteNodeIds!.Clear();
                var ship=Clone(originalShip);ship.ShipId+=".second";ship.FleetId=fleet.FleetId;fleet.ShipIds=new(){ship.ShipId};g.Ships!.Add(ship);g.Fleets!.Add(fleet);
                foreach(var pair in new[]{(id,fleet.FleetId),(originalShip.ShipId,ship.ShipId)}){var identity=Clone(g.EntityIdentities.Single(i=>i.EntityId==pair.Item1));identity.EntityId=pair.Item2;g.EntityIdentities.Add(identity);}
                var provision=Clone(catalogue.InitialShipProvisions!.Single(s=>s.ShipId==originalShip.ShipId));provision.ShipId=ship.ShipId;catalogue.InitialShipProvisions!.Add(provision);
                p.InitialOrders!.Add(new(){FleetId=fleet.FleetId,Intent=CampaignTravelIntent.StayEngage,RouteNodeIds=new()});
            }
            foreach(var order in p.InitialOrders!)order.Intent=CampaignTravelIntent.StayEngage;
            p.InitialContacts!.Add(new(){ActorId=A,FleetId=FB+".second",LastSeenNodeId="nav.cardassia",LastSeenTurn=1,TrackingNodeIds=new(){"nav.cardassia"}});
            p.InitialContacts.Add(new(){ActorId=B,FleetId=FA+".second",LastSeenNodeId="nav.cardassia",LastSeenTurn=1,TrackingNodeIds=new(){"nav.cardassia"}});
        });
        var m=new ReviewUiModel(f.Package,f.Codec,f.Store,A);var flow=new ReviewCombatFlow(m);Assert.True(m.Advance());var encounters=m.Current.Frame.Encounters.ToArray();Assert.Equal(2,encounters.Length);
        foreach(var encounter in encounters)
        {
            Assert.True(m.SelectEncounter(encounter));
            while(!m.Current.Frame.Battle!.Complete)
            {
                foreach(var actor in new[]{A,B})
                {Assert.True(m.SwitchActor(actor));Assert.True(m.SelectEncounter(encounter));Assert.True(m.CommitRound(ReviewUiModel.NewCommandId()));}
                Assert.True(m.SwitchActor(A));Assert.True(m.SelectEncounter(encounter));
            }
            Assert.Null(flow.AftermathSummary);
        }
        var selected=m.Current.Frame.Battle!;
        var host=typeof(ReviewUiModel).GetField("host",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(m)!;
        var session=typeof(PhysicalReviewHost).GetField("session",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(host)!;
        session=session.GetType().GetField("session",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(session)!;
        var probe=session.GetType().GetProperty("FinalizationProbe",BindingFlags.NonPublic|BindingFlags.Instance)!;
        try
        {
            probe.SetValue(session,(Action<int>)(index=>{if(index==1)throw new InvalidOperationException("Later encounter rejection fixture");}));
            Assert.False(flow.FinalizeAftermath(true));Assert.Null(flow.AftermathSummary);Assert.Same(selected,m.Current.Frame.Battle);
        }
        finally {probe.SetValue(session,null);}
        Assert.True(m.Refresh());Assert.True(flow.FinalizeAftermath(true));var summary=Assert.IsType<ReviewAftermathSummary>(flow.AftermathSummary);
        Assert.Equal(selected.EncounterId,summary.EncounterId);Assert.Equal(selected.OwnShips.Count,summary.LostShipIds.Count+summary.Survivors.Count);
        Assert.True(m.Current.Frame.OwnShips.Count>summary.Survivors.Count);Assert.Contains("Selected encounter only",new ReviewDemoCombatText().Aftermath(summary));
        Assert.True(m.SwitchActor(B));Assert.Null(flow.AftermathSummary);
    }
    private static T Clone<T>(T value)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
}
