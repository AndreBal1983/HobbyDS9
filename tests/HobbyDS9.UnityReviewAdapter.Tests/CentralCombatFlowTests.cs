using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;
using static HobbyDS9.UnityReviewAdapter.Tests.UnityReviewFixture;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class CentralCombatFlowTests
{
    private static void Travel(ReviewCombatFlow flow,string actor,CampaignTravelIntent intent,string? destination=null,string? contact=null,string? fleet=null)
    {
        Assert.True(flow.Model.SwitchActor(actor));
        Assert.True(flow.DraftNextOrder(fleet??(actor==A?FA:FB),intent,destination,contact));
        Assert.True(flow.SubmitNextOrder("command.physical."+actor+"."+flow.Model.Current.Frame.Status.Turn+(fleet==null?"":"."+fleet),true));
    }
    private static void Pursue(ReviewCombatFlow flow)
    {
        Assert.True(flow.Model.SetCloak(FA,true,"command.physical.cloak"));
        Travel(flow,A,CampaignTravelIntent.Follow,"nav.bajor",FB);Travel(flow,B,CampaignTravelIntent.Leave,"nav.bajoran-wormhole");
        Assert.True(flow.AdvancePlanning(true));
    }
    private static void Commit(ReviewCombatFlow flow,string actor)
    {
        var selected=flow.Model.Current.Frame.Battle?.EncounterId;
        Assert.True(flow.Model.SwitchActor(actor));
        if(selected!=null&&flow.Model.Current.Frame.Battle?.EncounterId!=selected)Assert.True(flow.Model.SelectEncounter(selected));
        if(flow.Stage==ReviewCombatStage.RoundResult)Assert.True(flow.PlanNextRound());
        var b=flow.Model.Current.Frame.Battle!;
        foreach(var own in b.StandingOrders.Where(o=>o.CanOrder))Assert.True(flow.DraftFleet(own.FleetId,b.Round==2?CampaignFleetManeuver.Flank:b.Round==3?CampaignFleetManeuver.Screen:CampaignFleetManeuver.Hold,null,true));
        Assert.True(flow.CommitRound("command.physical.round."+b.EncounterId+"."+actor+"."+b.Round,true));
    }
    private static void Complete(ReviewCombatFlow flow)
    {
        var selected=flow.Model.Current.Frame.Battle?.EncounterId;
        Assert.True(flow.Model.SwitchActor(A));
        if(selected!=null&&flow.Model.Current.Frame.Battle?.EncounterId!=selected)Assert.True(flow.Model.SelectEncounter(selected));
        while(!flow.Model.Current.Frame.Battle!.Complete){Commit(flow,A);Commit(flow,B);Assert.True(flow.Model.SwitchActor(A));if(selected!=null&&flow.Model.Current.Frame.Battle?.EncounterId!=selected)Assert.True(flow.Model.SelectEncounter(selected));}
    }
    [Fact]
    public void CentralActualFlowTwoBattlesSaveResumeRetainedStocksAndExistingVector()
    {
        using var f=new UnityReviewFixture();var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);var flow=new ReviewCombatFlow(ui);
        Pursue(flow);Assert.Equal(ReviewCombatStage.Orders,flow.Stage);Commit(flow,A);
        Assert.Equal(ReviewCombatStage.Waiting,flow.Stage);Assert.False(flow.CommitRound("command.duplicate",true));
        Assert.True(ui.Save("waiting"));Assert.True(ui.Load("waiting",true));Assert.Equal(ReviewCombatStage.Waiting,flow.Stage);
        Commit(flow,B);Assert.Equal(ReviewCombatStage.RoundResult,flow.Stage);Assert.False(flow.CommitRound("command.future",true));
        Assert.True(flow.PlanNextRound());Assert.False(flow.PlanNextRound());
        Complete(flow);Assert.Equal(ReviewCombatStage.AftermathPending,flow.Stage);
        Assert.False(flow.DraftNextOrder(FA,CampaignTravelIntent.StayEngage));Assert.False(flow.FinalizeAftermath(false));
        Assert.True(flow.FinalizeAftermath(true));Assert.Equal(ReviewCombatStage.AftermathConfirmed,flow.Stage);
        var turn=ui.Current.Frame.Status.Turn;Assert.False(flow.FinalizeAftermath(true));Assert.Equal(turn,ui.Current.Frame.Status.Turn);
        Assert.Equal(3,ui.Current.Frame.OwnShips.Single().Weapons.Single(w=>w.Class==TacticalWeaponClass.Torpedo).Ammunition);
        var shields=ui.Current.Frame.OwnShips.Single().Condition.Shields.ToArray();
        Travel(flow,A,CampaignTravelIntent.StayEngage);Travel(flow,B,CampaignTravelIntent.StayEngage);Assert.True(flow.AdvancePlanning(true));
        Assert.True(ui.SwitchActor(A));Assert.Equal(shields,ui.Current.Frame.OwnShips.Single().Condition.Shields);
        Assert.Equal(3,ui.Current.Frame.OwnShips.Single().Weapons.Single(w=>w.Class==TacticalWeaponClass.Torpedo).Ammunition);
        Complete(flow);Assert.True(flow.FinalizeAftermath(true));Assert.True(ui.Save("final"));
        var vector=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(f.Store.Read("final")))).ToLowerInvariant();
        Assert.Equal("a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177",vector);
        Assert.Contains("ammunition 0/12",flow.Conditions());
    }
    [Fact]
    public void PendingDraftDismissResumeAndSaveNeverCommitOrDiscard()
    {
        using var f=new UnityReviewFixture();var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);var flow=new ReviewCombatFlow(ui);Pursue(flow);ui.SwitchActor(A);
        Assert.True(flow.DraftFleet(FA,CampaignFleetManeuver.Screen,null));var frame=ui.Current.Frame;
        flow.Dismiss();Assert.False(flow.SurfaceOpen);flow.Open();Assert.True(flow.SurfaceOpen);
        Assert.Same(frame,ui.Current.Frame);Assert.True(ui.Current.Dirty);Assert.False(ui.Current.Frame.Battle!.OwnCommitmentAccepted);
        Assert.True(ui.Save("draft"));Assert.True(ui.Current.Dirty);Assert.Single(ui.Current.Draft);
        Assert.False(ui.Load("missing",true));Assert.Same(frame,ui.Current.Frame);Assert.True(ui.Current.Dirty);
        Assert.True(ui.Load("draft",true));Assert.False(ui.Current.Dirty);Assert.Equal(ReviewCombatStage.Orders,flow.Stage);
    }
    [Theory]
    [InlineData(CampaignTravelIntent.Stay)][InlineData(CampaignTravelIntent.StayEngage)][InlineData(CampaignTravelIntent.Leave)][InlineData(CampaignTravelIntent.Follow)]
    public void DraftAndReviewNextOrderDoNotAdvanceOrClaimAcceptance(CampaignTravelIntent intent)
    {
        using var f=new UnityReviewFixture();var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);var flow=new ReviewCombatFlow(ui);var status=ui.Current.Frame.Status;
        Assert.True(flow.DraftNextOrder(FA,intent,intent==CampaignTravelIntent.Leave||intent==CampaignTravelIntent.Follow?"nav.bajor":null,intent==CampaignTravelIntent.Follow?FB:null));
        Assert.Equal(status.Turn,ui.Current.Frame.Status.Turn);Assert.Equal(status.CampaignRevision,ui.Current.Frame.Status.CampaignRevision);
        Assert.False(flow.SubmitNextOrder("command.unconfirmed",false));Assert.Equal(CampaignTravelIntent.Stay,Assert.Single(ui.Current.Frame.Travel!.OwnOrders).Intent);
        Assert.True(flow.SubmitNextOrder("command.next",true));Assert.Equal(intent,Assert.Single(ui.Current.Frame.Travel!.OwnOrders).Intent);
        Assert.Equal(status.Turn,ui.Current.Frame.Status.Turn);Assert.False(ui.Current.Dirty);
    }
    [Theory]
    [InlineData("unknown-route")][InlineData("unknown-contact")][InlineData("stale-contact")]
    public void UnavailableRouteOrContactKeepsExistingDraftAndAcceptedOrder(string problem)
    {
        using var f=new UnityReviewFixture(adjust:p=>{if(problem=="stale-contact")p.ContactFreshnessTurns=0;});
        var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);var flow=new ReviewCombatFlow(ui);
        if(problem=="stale-contact"){Assert.True(flow.AdvancePlanning(true));Assert.False(ui.Current.Frame.Travel!.KnownContacts.Single().Fresh);}
        Assert.True(flow.DraftNextOrder(FA,CampaignTravelIntent.StayEngage));var draft=ui.Current.TravelDraft;
        Assert.False(flow.DraftNextOrder(FA,problem=="unknown-route"?CampaignTravelIntent.Leave:CampaignTravelIntent.Follow,problem=="unknown-route"?"nav.unknown":"nav.bajor",problem=="unknown-contact"?"fleet.unknown":problem=="stale-contact"?FB:null));
        Assert.Same(draft,ui.Current.TravelDraft);Assert.Equal(CampaignTravelIntent.Stay,Assert.Single(ui.Current.Frame.Travel!.OwnOrders).Intent);
    }
    [Fact]
    public void LateStaleFinalizationFailureKeepsStagedViewAndNeverAppliesTwice()
    {
        using var f=new UnityReviewFixture();var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);var flow=new ReviewCombatFlow(ui);Pursue(flow);Complete(flow);
        var staged=ui.Current.Frame;Assert.True(flow.CanFinalize);
        // A second real controller on the actual host simulates another transaction winning.
        var host=(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(ui)!;
        var other=new ReviewController(host,A);Assert.True(other.SelectEncounter(staged.Battle!.EncounterId));Assert.True(other.ReviewCompletion());
        Assert.True(host.Execute(other.ConfirmCompletion()!).Result.Accepted);
        Assert.False(flow.FinalizeAftermath(true));Assert.Same(staged,ui.Current.Frame);Assert.Equal(ReviewCombatStage.AftermathPending,flow.Stage);Assert.True(ui.Current.RefreshRequired);
        Assert.True(ui.Save("after-stale"));var turn=f.Store.Read("after-stale").Core.Campaign.GameState.TurnNumber;
        Assert.False(flow.FinalizeAftermath(true));Assert.True(ui.Save("after-repeat"));Assert.Equal(turn,f.Store.Read("after-repeat").Core.Campaign.GameState.TurnNumber);
        Assert.True(ui.Refresh());Assert.Equal(SessionPhase.Planning,ui.Current.Frame.Status.Phase);
    }
    [Fact]
    public void NoOwnedFleetDoesNotOfferMovementOrInventLossHistory()
    {
        using var f=new UnityReviewFixture(adjust:p=>{
            var g=p.CorePackage!.Campaign!.InitialState;var a=g.Civilizations!.Single(c=>c.CivilizationId==A);
            a.CivilizationId="civ.empty";g.Civilizations!.Add(JsonSerializer.Deserialize<CivilizationStateDto>(JsonSerializer.Serialize(a))!);a.CivilizationId=A;
            var e=JsonSerializer.Deserialize<EntityIdentityDto>(JsonSerializer.Serialize(g.EntityIdentities.Single(i=>i.EntityId==A)))!;e.EntityId="civ.empty";g.EntityIdentities.Add(e);
            var nav=JsonSerializer.Deserialize<CivilizationNavigationKnowledgeDto>(JsonSerializer.Serialize(g.NavigationKnowledge!.Single(n=>n.CivilizationId==A)))!;nav.CivilizationId="civ.empty";g.NavigationKnowledge!.Add(nav);
        });
        var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,"civ.empty");var flow=new ReviewCombatFlow(ui);
        Assert.Equal(ReviewCombatStage.NoOwnedFleet,flow.Stage);Assert.False(flow.DraftNextOrder(FA,CampaignTravelIntent.Stay));Assert.Empty(flow.OwnLosses);
        Assert.Contains("No owned ships",flow.Conditions());
    }
    [Fact]
    public void HiddenEnemyNeverGetsStockReadinessOrdersOrPhantomIdentityInCentralText()
    {
        using var f=new UnityReviewFixture();var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);var flow=new ReviewCombatFlow(ui);Pursue(flow);ui.SwitchActor(B);
        Assert.Empty(ui.Current.Frame.Battle!.VisibleEnemies);var text=flow.StageDiagram();Assert.Contains("No visible enemy",text);
        Assert.DoesNotContain("ship.equipment-cruiser",text);Assert.DoesNotContain("Ammunition",text);Assert.DoesNotContain("Powered",text);Assert.DoesNotContain("Random",text);
        ui.SwitchActor(A);Assert.Empty(flow.OwnLosses);Assert.DoesNotContain("ship.cardassian-scout",flow.Conditions());
    }
    [Fact]
    public void MultipleRealEncountersRequireWholeBatchCompletionBeforeFinalization()
    {
        using var f=new UnityReviewFixture(adjust:p=>{
            var game=p.CorePackage!.Campaign!.InitialState;var catalogue=p.CorePackage.Campaign.Definitions.AuthoredTacticalEquipment!;
            foreach(var id in new[]{FA,FB})
            {
                var originalFleet=game.Fleets!.Single(x=>x.FleetId==id);var original=game.Ships!.Single(s=>s.FleetId==id);
                var fleet=Clone(originalFleet);fleet.FleetId=id+".second";fleet.LocationNodeId="nav.cardassia";fleet.RouteNodeIds!.Clear();
                var ship=Clone(original);ship.ShipId+=".second";ship.FleetId=fleet.FleetId;fleet.ShipIds=new List<string>{ship.ShipId};
                game.Ships!.Add(ship);game.Fleets!.Add(fleet);
                foreach(var pair in new[]{(id,fleet.FleetId),(original.ShipId,ship.ShipId)}){var identity=Clone(game.EntityIdentities.Single(e=>e.EntityId==pair.Item1));identity.EntityId=pair.Item2;game.EntityIdentities.Add(identity);}
                var provision=Clone(catalogue.InitialShipProvisions!.Single(s=>s.ShipId==original.ShipId));provision.ShipId=ship.ShipId;catalogue.InitialShipProvisions!.Add(provision);
                p.InitialOrders!.Add(new CampaignTravelOrderDto{FleetId=fleet.FleetId,Intent=CampaignTravelIntent.StayEngage,RouteNodeIds=new List<string>()});
            }
            foreach(var order in p.InitialOrders!)order.Intent=CampaignTravelIntent.StayEngage;
            p.InitialContacts!.Add(new CampaignContactKnowledgeDto{ActorId=A,FleetId=FB+".second",LastSeenNodeId="nav.cardassia",LastSeenTurn=1,TrackingNodeIds=new List<string>{"nav.cardassia"}});
            p.InitialContacts.Add(new CampaignContactKnowledgeDto{ActorId=B,FleetId=FA+".second",LastSeenNodeId="nav.cardassia",LastSeenTurn=1,TrackingNodeIds=new List<string>{"nav.cardassia"}});
        });
        var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);var flow=new ReviewCombatFlow(ui);Assert.True(flow.AdvancePlanning(true));
        Assert.Equal(ReviewCombatStage.SelectEncounter,flow.Stage);Assert.Equal(2,ui.Current.Frame.Encounters.Count);
        var encounters=ui.Current.Frame.Encounters.ToArray();Assert.True(ui.SelectEncounter(encounters[0]));Complete(flow);
        Assert.Equal(ReviewCombatStage.AftermathPending,flow.Stage);Assert.False(flow.CanFinalize);Assert.False(flow.FinalizeAftermath(true));
        Assert.False(flow.DraftNextOrder(FA,CampaignTravelIntent.Leave,"nav.bajor"));var turn=ui.Current.Frame.Status.Turn;
        Assert.True(ui.SelectEncounter(encounters[1]));Complete(flow);Assert.True(flow.CanFinalize);Assert.True(flow.FinalizeAftermath(true));
        Assert.Equal(turn+1,ui.Current.Frame.Status.Turn);Assert.Equal(ReviewCombatStage.AftermathConfirmed,flow.Stage);Assert.False(flow.FinalizeAftermath(true));
    }
    [Fact]
    public void ActualDefeatShowsOwnLossThenNoMovementControlsWithoutRecreatingShips()
    {
        using var f=new UnityReviewFixture(adjust:p=>{
            var ship=p.CorePackage!.Campaign!.InitialState.Ships!.Single(s=>s.FleetId==FA);ship.CurrentHull=1;
            foreach(var shield in p.CorePackage.Campaign.Definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.Single(s=>s.ShipId==ship.ShipId).InitialShields!)shield.Current=0;
            foreach(var order in p.InitialOrders!)order.Intent=CampaignTravelIntent.StayEngage;
        });
        var ui=new ReviewUiModel(f.Package,f.Codec,f.Store,A);var flow=new ReviewCombatFlow(ui);Assert.True(flow.AdvancePlanning(true));Complete(flow);
        Assert.Equal(ReviewCombatStage.AftermathPending,flow.Stage);Assert.Contains("ship.equipment-cruiser",flow.OwnLosses);
        Assert.True(flow.FinalizeAftermath(true));Assert.Equal(ReviewCombatStage.NoOwnedFleet,flow.Stage);Assert.False(flow.CanDraftNextOrder);
        Assert.False(flow.DraftNextOrder(FA,CampaignTravelIntent.Leave,"nav.bajor"));Assert.Empty(ui.Current.Frame.OwnShips);
        Assert.True(ui.Save("defeated"));Assert.True(ui.Load("defeated",true));Assert.Empty(ui.Current.Frame.OwnShips);Assert.Equal(ReviewCombatStage.NoOwnedFleet,flow.Stage);
    }
    private static T Clone<T>(T value)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
}
