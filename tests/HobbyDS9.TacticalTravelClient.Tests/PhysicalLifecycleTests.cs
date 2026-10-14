using System.Globalization;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Xunit;
using static HobbyDS9.TacticalTravelClient.Tests.PhysicalFixture;

namespace HobbyDS9.TacticalTravelClient.Tests;

public sealed class PhysicalLifecycleTests
{
    [Fact]
    public async Task FasterIntermediateContactWaitingSaveFreshProcessAndSecondBattleMatchCompleteRootGolden()
    {
        using var control=new PhysicalFixture(); using var resumed=new PhysicalFixture();
        control.StartPursuit(); resumed.StartPursuit();
        var receipt=Assert.Single(resumed.Own.Frame.Travel!.OwnReceipts);
        Assert.Equal("nav.bajor",receipt.EndNodeId); Assert.Equal("3",receipt.StopTimeNumerator); Assert.Equal("4",receipt.StopTimeDenominator);
        Assert.True(receipt.StoppedByEncounter); Assert.Empty(resumed.Own.Frame.OwnStrategicFleets.Single().RouteNodeIds);
        Commit(resumed.Host,resumed.Own); Assert.Equal(ReviewMode.Waiting,resumed.Own.Mode);
        Assert.True(resumed.Other.EditFleet(FB,CampaignFleetManeuver.Screen,null)); // Local only.
        Assert.True(resumed.Own.SaveCommittedToSlot("waiting").Succeeded);
        var saved=resumed.Store.Read("waiting");
        Assert.Equal(2,saved.FormatVersion); Assert.Equal(SessionPhase.AwaitingCombat,saved.Phase);
        Assert.Equal(SessionPhase.Planning,saved.Core.Campaign.GameState.Phase);
        Assert.NotNull(saved.MovementProof); Assert.NotEmpty(saved.Requests);
        var child=await resumed.Child("finish-two-battles","waiting");
        Assert.Equal(0,child.Exit); Assert.Empty(child.Error);
        var firstReport=control.Complete();
        var retained=control.Own.Frame.OwnShips.Single().Weapons.Single(w=>w.Class == TacticalWeaponClass.Torpedo).Ammunition;
        Assert.Equal(3,retained); var turn=control.Own.Frame.Status.Turn;
        Assert.All(control.Own.Frame.Travel!.OwnOrders,o=>Assert.Equal(CampaignTravelIntent.Stay,o.Intent));
        control.Reengage(); Assert.Equal(turn,control.Own.Frame.Status.Turn);
        Assert.Equal(3,control.Own.Frame.OwnShips.Single().Weapons.Single(w=>w.Class == TacticalWeaponClass.Torpedo).Ammunition);
        Assert.Equal("0",Assert.Single(control.Own.Frame.Travel!.OwnReceipts).StopTimeNumerator);
        var secondReport=control.Complete();
        Assert.Equal(0,control.Own.Frame.OwnShips.Single().Weapons.Single(w=>w.Class == TacticalWeaponClass.Torpedo).Ammunition);
        Assert.Equal("a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177",control.Vector("final"));
        Assert.Equal(File.ReadAllBytes(control.PathFor("final")),File.ReadAllBytes(resumed.PathFor("child-final")));
        Assert.Contains(firstReport,child.Output); Assert.Contains(secondReport,child.Output);
        Assert.Equal(ReviewMode.Waiting,resumed.Own.Mode); Assert.True(resumed.Other.Dirty);
        Assert.DoesNotContain("MovementProof",child.Output); Assert.DoesNotContain("RandomState",child.Output);
    }
    [Fact]
    public async Task SlowerFollowMissRetainsExactWeightedProgressAndDoesNotPromoteLastSeenToCurrentEnemyPosition()
    {
        using var f=new PhysicalFixture("slower"); f.StartPursuit();
        Assert.Empty(f.Own.Frame.Encounters); Assert.Equal(2,f.Own.Frame.Status.Turn);
        var own=Assert.Single(f.Own.Frame.Travel!.OwnReceipts);
        Assert.Equal(2,own.TransitProgress); Assert.Equal(3,own.TransitTotalCost);
        Assert.Equal("nav.sol",own.EndNodeId); Assert.Equal("nav.bajor",own.TransitDestinationId); Assert.False(own.StoppedByEncounter);
        var other=Assert.Single(f.Other.Frame.Travel!.OwnReceipts);
        Assert.Equal(1,other.TransitProgress); Assert.Equal(2,other.TransitTotalCost);
        var text=ReviewTextRenderer.Render(f.Own);
        Assert.Contains("LAST SEEN nav.sol, turn 1",text); Assert.Contains("current position and destination unavailable",text);
        Assert.DoesNotContain("nav.bajoran-wormhole",text);
        Assert.Equal("77073cc154b78dbca2e5f6fa1a618cae490d11410f51f4cb2bb9e640a33210c4",f.Vector("slower"));
        var child=await f.Child("inspect","slower");
        Assert.Equal(0,child.Exit); Assert.Equal(text+ReviewTextRenderer.RenderReport(f.Own),child.Output);
    }
    [Theory]
    [InlineData(CampaignTravelIntent.Stay)]
    [InlineData(CampaignTravelIntent.StayEngage)]
    [InlineData(CampaignTravelIntent.Leave)]
    [InlineData(CampaignTravelIntent.Follow)]
    public void SimpleTravelCommandsUseObservedCampaignAndContactStamps(CampaignTravelIntent intent)
    {
        using var f=new PhysicalFixture();
        var route=intent == CampaignTravelIntent.Leave || intent == CampaignTravelIntent.Follow ? f.Own.FindKnownRoute(FA,"nav.bajor") : null;
        var contact=intent == CampaignTravelIntent.Follow ? FB : null;
        Assert.Null(f.Own.ConfirmTravel("command.unreviewed"));
        Assert.True(f.Own.EditTravel(FA,intent,route,contact)); Assert.True(f.Own.ReviewTravel());
        var work=f.Own.ConfirmTravel("command.travel.test")!;
        Assert.Equal(f.Own.Frame.Status.Turn,work.Turn); Assert.Equal(f.Own.Frame.Status.CampaignRevision,work.CampaignRevision);
        if(contact != null) Assert.Equal(f.Own.Frame.Travel!.KnownContacts.Single(k=>k.FleetId == FB).LastSeenTurn,work.Travel!.ContactSeenTurn);
        Assert.Null(f.Own.ConfirmTravel("command.double"));
        var result=f.Host.Execute(work); Assert.True(result.Result.Accepted); Assert.True(f.Own.Receive(result));
        Assert.Equal(intent,Assert.Single(f.Own.Frame.Travel!.OwnOrders).Intent); Assert.False(f.Own.Dirty);
        Assert.Empty(f.Own.Frame.Encounters);
    }
    [Fact]
    public void KnownRouteQueryReturnsBackendCostsAndIssuingItDoesNotMutateCampaignOrCalculatePursuit()
    {
        using var f=new PhysicalFixture(); var before=f.Own.SaveCommitted(); var frame=f.Own.Frame;
        var route=f.Own.FindKnownRoute(FA,"nav.bajor")!;
        Assert.True(route.Route.Success); Assert.Equal(new[]{"nav.bajor"},route.Route.Steps);
        Assert.Equal(3,route.Route.TotalCost); Assert.Equal(3,Assert.Single(route.Route.Costs).EffectiveCost);
        Assert.Equal(frame.Status.CampaignRevision,route.CampaignRevision);
        Assert.Null(f.Own.FindKnownRoute(FB,"nav.bajor"));
        for(int i=0;i<5;i++) { ReviewTextRenderer.Render(f.Own); f.Own.FindKnownRoute(FA,"nav.bajor"); }
        Assert.Equal(frame.Status.CampaignRevision,f.Host.Read(A).Status.CampaignRevision); Assert.False(f.Own.Dirty);
        Assert.True(f.Own.LoadCommitted(before));
    }
    [Fact]
    public void MutualLeaveDoesNotForceCombatAndStationaryEngageCreatesANewBattleOnlyOnStrategicAdvance()
    {
        using var leave=new PhysicalFixture();
        Travel(leave.Host,leave.Own,CampaignTravelIntent.Leave,"nav.bajor");
        Travel(leave.Host,leave.Other,CampaignTravelIntent.Leave,"nav.bajor"); leave.Own.Refresh(); leave.Advance();
        Assert.Empty(leave.Own.Frame.Encounters); Assert.Equal(2,leave.Own.Frame.Status.Turn);
        Assert.Equal("nav.bajor",leave.Own.Frame.OwnStrategicFleets.Single().LocationNodeId.Value);
        Assert.Equal("nav.bajor",leave.Other.Frame.OwnStrategicFleets.Single().LocationNodeId.Value);
        using var engage=new PhysicalFixture();
        Travel(engage.Host,engage.Own,CampaignTravelIntent.StayEngage);
        Travel(engage.Host,engage.Other,CampaignTravelIntent.StayEngage); engage.Own.Refresh();
        Assert.Empty(engage.Own.Frame.Encounters); engage.Advance();
        var id=engage.Own.Frame.Battle!.EncounterId; Assert.Equal("0",Assert.Single(engage.Own.Frame.Travel!.OwnReceipts).StopTimeNumerator);
        engage.Complete(); Assert.Equal(2,engage.Own.Frame.Status.Turn); Assert.Empty(engage.Own.Frame.Encounters);
        Assert.All(engage.Own.Frame.OwnStrategicFleets,f=>Assert.Empty(f.RouteNodeIds));
        Assert.All(engage.Own.Frame.Travel!.OwnOrders,o=>Assert.Equal(CampaignTravelIntent.Stay,o.Intent));
        engage.Advance(); Assert.Equal(3,engage.Own.Frame.Status.Turn); Assert.Empty(engage.Own.Frame.Encounters); // No fresh intent, no rematch.
        Assert.DoesNotContain(id,engage.Own.Frame.Encounters);
    }
    [Theory]
    [InlineData("en-US")]
    [InlineData("nl-NL")]
    [InlineData("tr-TR")]
    public void CanonicalReversedRepeatedResumedAndReorderedCompletePhysicalRootsAgree(string culture)
    {
        var old=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
            using var canonical=new PhysicalFixture(); using var reverse=new PhysicalFixture();
            using var repeated=new PhysicalFixture(); using var resumed=new PhysicalFixture();
            using var reordered=new PhysicalFixture(adjust:p=>{
                p.InitialOrders!.Reverse(); p.InitialContacts!.Reverse(); p.Hostilities!.Reverse();
                p.CorePackage!.Campaign!.InitialState.Fleets!.Reverse();
            });
            var runs=new[]{canonical,reverse,repeated,resumed,reordered};
            foreach(var f in runs) f.StartPursuit(reverse:ReferenceEquals(f,reverse));
            for(int battle=1;battle<=2;battle++)
            {
                string? report=null;
                foreach(var f in runs)
                {
                    var rendered=f.Complete(ReferenceEquals(f,reverse),ReferenceEquals(f,resumed));
                    report ??=rendered; Assert.Equal(report,rendered);
                }
                var vectors=runs.Select((f,i)=>f.Vector("battle-"+battle)).ToArray(); Assert.All(vectors,v=>Assert.Equal(vectors[0],v));
                var own=JsonSerializer.Serialize(canonical.Own.Frame.OwnShips); var positions=JsonSerializer.Serialize(canonical.Own.Frame.OwnStrategicFleets);
                Assert.All(runs,f=>Assert.Equal(own,JsonSerializer.Serialize(f.Own.Frame.OwnShips)));
                Assert.All(runs,f=>Assert.Equal(positions,JsonSerializer.Serialize(f.Own.Frame.OwnStrategicFleets)));
                if(battle == 1) foreach(var f in runs) f.Reengage();
            }
            Assert.Equal("a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177",canonical.Vector("golden"));
        }
        finally { CultureInfo.CurrentCulture=old; }
    }
    [Fact]
    public void UnrelatedFleetContinuesItsSuppliedRouteWhileParticipantsStopAtIntermediateContact()
    {
        using var f=new PhysicalFixture(adjust:p=>{
            var game=p.CorePackage!.Campaign!.InitialState; var defs=p.CorePackage.Campaign.Definitions;
            var original=game.Ships!.Single(s=>s.ShipId == "ship.equipment-cruiser"); var ship=Clone(original);
            ship.ShipId="ship.unrelated"; ship.FleetId="fleet.unrelated"; game.Ships!.Add(ship);
            var fleet=Clone(game.Fleets!.Single(s=>s.FleetId == FA)); fleet.FleetId=ship.FleetId; fleet.ShipIds=new List<string>{ship.ShipId}; game.Fleets!.Add(fleet);
            foreach(var pair in new[]{(original.ShipId,ship.ShipId),(FA,fleet.FleetId)})
            { var identity=Clone(game.EntityIdentities.Single(e=>e.EntityId == pair.Item1)); identity.EntityId=pair.Item2; game.EntityIdentities.Add(identity); }
            var provision=Clone(defs.AuthoredTacticalEquipment!.InitialShipProvisions!.Single(s=>s.ShipId == original.ShipId));
            provision.ShipId=ship.ShipId; defs.AuthoredTacticalEquipment.InitialShipProvisions!.Add(provision);
            p.InitialOrders!.Add(new CampaignTravelOrderDto { FleetId=fleet.FleetId,Intent=CampaignTravelIntent.Stay,RouteNodeIds=new List<string>() });
        });
        Travel(f.Host,f.Own,CampaignTravelIntent.Leave,"nav.bajoran-wormhole",fleet:"fleet.unrelated");
        Travel(f.Host,f.Own,CampaignTravelIntent.Follow,"nav.bajor",FB,fleet:FA);
        Travel(f.Host,f.Other,CampaignTravelIntent.Leave,"nav.bajoran-wormhole"); f.Own.Refresh(); f.Advance();
        var unrelated=f.Own.Frame.Travel!.OwnReceipts.Single(r=>r.FleetId == "fleet.unrelated");
        Assert.False(unrelated.StoppedByEncounter); Assert.Equal("nav.bajoran-wormhole",unrelated.EndNodeId);
        Assert.Equal(5,unrelated.CostPaid); Assert.Equal(2,unrelated.EdgesCompleted);
        Assert.Equal(FA,Assert.Single(f.Own.Frame.Battle!.StandingOrders).FleetId);
        Assert.DoesNotContain(f.Own.Frame.Battle.OwnShips,s=>s.Condition.FleetId == "fleet.unrelated");
        Assert.Contains("Own movement fleet.unrelated",ReviewTextRenderer.Render(f.Own));
    }
}
