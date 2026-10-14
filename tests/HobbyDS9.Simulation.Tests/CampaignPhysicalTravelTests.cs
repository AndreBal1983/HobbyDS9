using System.Globalization;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class CampaignPhysicalTravelTests
{
    private const string A="civ.federation",B="civ.cardassian",FA="fleet.federation-scout",FB="fleet.cardassian-scout";
    private static string Json<T>(T value)=>JsonSerializer.Serialize(value);
    private static T Clone<T>(T value)=>JsonSerializer.Deserialize<T>(Json(value))!;
    internal static CampaignTacticalTravelPackageDto Package()
    {
        var core=CampaignTacticalReviewTests.Package();
        // Explicit unit-cost authored topology for the timing examples; other tests replace costs.
        foreach(var edge in core.Campaign!.Definitions.NavigationConnections!)edge.BaseTraversalCost=1;
        var nodes=core.Campaign!.Definitions.NavigationRegions!.Select(n=>n.NodeId).ToList();
        return new CampaignTacticalTravelPackageDto { FormatVersion=2,ProfileId=CampaignTacticalTravelIdentity.Profile,PackageHash=new string('b',64),
            PolicyStatus="proposed",DefaultActivationAllowed=false,BoundaryPolicy="arrivals-contact-departures-v1",SharedStartPolicy="departure-first-start-v1",
            LaterArrivalPolicy="join-connected-before-allocation-v1",ContactFreshnessTurns=20,CorePackage=core,
            Hostilities=new List<CampaignDeclaredHostilityDto> { new(){CivilizationA=A,CivilizationB=B} },
            InitialContacts=new List<CampaignContactKnowledgeDto> {
                new(){ActorId=A,FleetId=FB,LastSeenNodeId="nav.sol",LastSeenTurn=1,TrackingNodeIds=new List<string>(nodes)},
                new(){ActorId=B,FleetId=FA,LastSeenNodeId="nav.sol",LastSeenTurn=1,TrackingNodeIds=new List<string>(nodes)} },
            InitialOrders=new List<CampaignTravelOrderDto> {
                new(){FleetId=FA,Intent=CampaignTravelIntent.Stay,RouteNodeIds=new List<string>()},
                new(){FleetId=FB,Intent=CampaignTravelIntent.Stay,RouteNodeIds=new List<string>()} } };
    }
    private static ICampaignTacticalTravelSession Start(CampaignTacticalTravelPackageDto p)=>CampaignTacticalTravelApplication.StartReviewPackage(p);
    private static SubmitCampaignTravelOrderDto Order(ICampaignTacticalTravelSession app,string actor,string fleet,CampaignTravelIntent intent,
        string[]? route=null,string? target=null,string suffix="")
    {
        var s=app.CaptureStatus();
        return new SubmitCampaignTravelOrderDto { ActorId=actor,CommandId="command.travel."+actor+"."+s.Turn+"."+s.CampaignRevision+suffix,
            ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,Order=new CampaignTravelOrderDto { FleetId=fleet,Intent=intent,
                RouteNodeIds=route?.ToList()??new List<string>(),ContactFleetId=target,
                ExpectedContactSeenTurn=target==null?null:app.GetKnownContacts(actor).Single(k=>k.FleetId==target).LastSeenTurn } };
    }
    private static void Advance(ICampaignTacticalTravelSession app)
    { var s=app.CaptureStatus();var result=app.AdvanceTurn(s.Turn,s.CampaignRevision);
        var session=(CampaignTacticalReviewSession)app.GetType().GetField("session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(app)!;
        Assert.True(result.Accepted,result.Error+" "+session.LastPhysicalFailure); }
    private static void Complete(ICampaignTacticalTravelSession app)
    {
        foreach(var id in app.GetEncounters(A))
            while(!app.Observe(id,A).Complete)
            {
                var commands=new[]{A,B}.Select(actor=>{
                    var v=app.Observe(id,actor);
                    return new CommitCampaignTacticalRoundDto { ActorId=actor,CommandId="command.round."+actor+"."+id+"."+v.Round,EncounterId=id,
                        ExpectedTurn=v.Turn,ExpectedRound=v.Round,ExpectedBoundaryRevision=v.BoundaryRevision,Replacements=new List<CampaignTacticalFleetOrderDto>() };
                }).ToArray();
                foreach(var c in commands)Assert.True(app.CommitRound(c).Accepted);
            }
        var s=app.CaptureStatus();Assert.True(app.FinalizeBatch(s.Turn,s.CampaignRevision).Accepted);
    }
    [Fact]
    public void PhysicalPresenceWithoutIntentOrDeclaredHostilityDoesNotCreateBattle()
    {
        var p=Package();var app=Start(p);Advance(app);
        Assert.Equal(SessionPhase.Planning,app.CaptureStatus().Phase);Assert.Empty(app.GetEncounters(A));Assert.Equal(2,app.CaptureStatus().Turn);
        p.Hostilities!.Clear();p.InitialContacts!.Clear();p.InitialOrders!.ForEach(o=>o.Intent=CampaignTravelIntent.StayEngage);
        app=Start(p);Advance(app);Assert.Empty(app.GetEncounters(A));
    }
    [Fact]
    public void BothStationaryEngagersAllocateOneNextTurnBattleAndRetainStocks()
    {
        var p=Package();p.InitialOrders!.ForEach(o=>o.Intent=CampaignTravelIntent.StayEngage);
        var app=Start(p);Advance(app);Assert.Single(app.GetEncounters(A));
        Assert.Equal(SessionPhase.AwaitingCombat,app.CaptureStatus().Phase);Assert.Equal("0",app.GetOwnTravelReceipts(A)[0].StopTimeNumerator);
        var id=app.GetEncounters(A)[0];Complete(app);
        var before=Json(app.InspectOwnShips(A));
        app=CampaignTacticalTravelApplication.LoadReviewSave(p,Clone(app.ExportSave()));Assert.Equal(before,Json(app.InspectOwnShips(A)));
        foreach(var (actor,fleet) in new[]{(A,FA),(B,FB)})Assert.True(app.SubmitTravelOrder(Order(app,actor,fleet,CampaignTravelIntent.StayEngage)).Accepted);
        Advance(app);Assert.NotEqual(id,Assert.Single(app.GetEncounters(A)));
        Assert.True(app.Observe(app.GetEncounters(A)[0],A).OwnShips.SelectMany(s=>s.Weapons).Where(w=>w.Class==TacticalWeaponClass.Torpedo).All(w=>w.Ammunition<7));
    }
    [Fact]
    public void DepartureAtSharedStartPrecedesRenewedContactEvenWhenOpponentEngages()
    {
        var p=Package();var app=Start(p);
        Assert.True(app.SubmitTravelOrder(Order(app,A,FA,CampaignTravelIntent.StayEngage)).Accepted);
        Assert.True(app.SubmitTravelOrder(Order(app,B,FB,CampaignTravelIntent.Leave,new[]{"nav.bajor","nav.bajoran-wormhole"})).Accepted);
        Assert.Equal(new[]{"nav.bajor","nav.bajoran-wormhole"},app.GetOwnStrategicFleets(B)[0].RouteNodeIds.Select(n=>n.Value));
        Advance(app);Assert.Empty(app.GetEncounters(A));Assert.True(app.GetOwnStrategicFleets(B)[0].LocationNodeId.Value=="nav.bajoran-wormhole",Json(app.ExportSave().LastMovement));
        Assert.Equal("nav.sol",app.GetOwnStrategicFleets(A)[0].LocationNodeId.Value);
    }
    [Fact]
    public void BothDepartWithoutPursuitDoNotForceAnEncounterAtEqualIntermediateArrival()
    {
        var p=Package();var app=Start(p);
        foreach(var (actor,fleet) in new[]{(A,FA),(B,FB)})Assert.True(app.SubmitTravelOrder(Order(app,actor,fleet,CampaignTravelIntent.Leave,new[]{"nav.bajor"})).Accepted);
        Advance(app);Assert.Empty(app.GetEncounters(A));
    }
    private static void PursuitBudgets(CampaignTacticalTravelPackageDto p,long pursuer,long departing)
    {
        var core=p.CorePackage!;var game=core.Campaign!.InitialState;var defs=core.Campaign.Definitions;
        var surveyor="ship.equipment-surveyor";game.Ships!.RemoveAll(s=>s.ShipId==surveyor);game.EntityIdentities.RemoveAll(i=>i.EntityId==surveyor);
        game.Fleets!.Single(f=>f.FleetId==FA).ShipIds!.Remove(surveyor);defs.AuthoredTacticalEquipment!.InitialShipProvisions!.RemoveAll(s=>s.ShipId==surveyor);
        var original=defs.ShipDesigns!.Single(d=>d.DefinitionId=="ship-design.source-cruiser");original.MovementBudget=pursuer;
        var target=Clone(original);target.DefinitionId="ship-design.travel-target";target.MovementBudget=departing;defs.ShipDesigns!.Add(target);defs.DefinitionIds.Add(target.DefinitionId);
        game.Ships.Single(s=>s.ShipId=="ship.cardassian-scout").ShipDesignDefinitionId=target.DefinitionId;
        defs.AuthoredTacticalEquipment.InitialShipProvisions!.Single(s=>s.ShipId=="ship.cardassian-scout").ShipDesignDefinitionId=target.DefinitionId;
        game.EntityIdentities.Single(s=>s.EntityId=="ship.cardassian-scout").DefinitionId=target.DefinitionId;
    }
    [Theory]
    [InlineData(3,true)]
    [InlineData(1,false)]
    public void FasterActualPursuerWaitsAtKnownSectorAndSlowerOneMisses(long speed,bool catches)
    {
        var p=Package();PursuitBudgets(p,speed,2);var app=Start(p);
        Assert.True(app.SubmitTravelOrder(Order(app,A,FA,CampaignTravelIntent.Follow,new[]{"nav.bajor"},FB)).Accepted);
        Assert.True(app.SubmitTravelOrder(Order(app,B,FB,CampaignTravelIntent.Leave,new[]{"nav.bajor","nav.bajoran-wormhole","nav.gamma-exit"})).Accepted);
        Advance(app);
        Assert.True(app.GetEncounters(A).Count==(catches?1:0),Json(app.ExportSave().LastMovement));
        if(catches)
        {
            foreach(var actor in new[]{A,B}){var r=Assert.Single(app.GetOwnTravelReceipts(actor));Assert.Equal("nav.bajor",r.EndNodeId);Assert.Equal("1",r.StopTimeNumerator);Assert.Equal("2",r.StopTimeDenominator);Assert.True(r.StoppedByEncounter);}
            Assert.All(app.GetOwnStrategicFleets(A).Concat(app.GetOwnStrategicFleets(B)),f=>Assert.Empty(f.RouteNodeIds));
        }
        else{Assert.Equal("nav.bajor",app.GetOwnStrategicFleets(A)[0].LocationNodeId.Value);Assert.Equal("nav.bajoran-wormhole",app.GetOwnStrategicFleets(B)[0].LocationNodeId.Value);}
    }
    [Fact]
    public void OppositeEdgeCrossingNeverTeleportsIntoSectorCombat()
    {
        var p=Package();PursuitBudgets(p,1,1);
        p.CorePackage!.Campaign!.InitialState.Fleets!.Single(f=>f.FleetId==FB).LocationNodeId="nav.bajor";
        var app=Start(p);
        Assert.True(app.SubmitTravelOrder(Order(app,A,FA,CampaignTravelIntent.Follow,new[]{"nav.bajor"},FB)).Accepted);
        Assert.True(app.SubmitTravelOrder(Order(app,B,FB,CampaignTravelIntent.Follow,new[]{"nav.sol"},FA)).Accepted);
        Advance(app);Assert.Empty(app.GetEncounters(A));
    }
    [Fact]
    public void LockedWeightedPartialProgressComesFromActualContentAndSurvivesLoad()
    {
        var p=Package();PursuitBudgets(p,2,1);
        var edge=p.CorePackage!.Campaign!.Definitions.NavigationConnections!.Single(e=>e.ConnectionId=="connection.sol-bajor");edge.BaseTraversalCost=5;
        var f=p.CorePackage.Campaign.InitialState.Fleets!.Single(f=>f.FleetId==FA);f.RouteNodeIds=new List<string>{"nav.bajor"};f.TransitTotalCost=5;f.TransitProgress=1;
        p.InitialOrders!.Single(o=>o.FleetId==FA).Intent=CampaignTravelIntent.Leave;p.InitialOrders!.Single(o=>o.FleetId==FA).RouteNodeIds=new List<string>{"nav.bajor"};
        var app=Start(p);Advance(app);Assert.Empty(app.GetEncounters(A));
        var r=app.GetOwnTravelReceipts(A)[0];Assert.Equal(3,r.TransitProgress);Assert.Equal(5,r.TransitTotalCost);Assert.Equal(2,r.CostPaid);
        Assert.Equal("nav.sol",r.EndNodeId);Assert.Equal("nav.bajor",r.TransitDestinationId);
        var before=Json(app.ExportSave());Assert.Equal(before,Json(CampaignTacticalTravelApplication.LoadReviewSave(p,Clone(app.ExportSave())).ExportSave()));
    }
    [Theory]
    [InlineData("stale-turn")]
    [InlineData("stale-revision")]
    [InlineData("foreign-fleet")]
    [InlineData("unknown-contact")]
    [InlineData("stale-contact")]
    [InlineData("unknown-route")]
    [InlineData("stay-route")]
    public void InvalidTravelRequestsRejectBeforePhaseEconomyMovementRngOrLedger(string defect)
    {
        var p=Package();var app=Start(p);var c=Order(app,A,FA,CampaignTravelIntent.Follow,new[]{"nav.bajor"},FB);
        switch(defect)
        {
            case "stale-turn":c.ExpectedTurn++;break;
            case "stale-revision":c.ExpectedRevision++;break;
            case "foreign-fleet":c.Order!.FleetId=FB;break;
            case "unknown-contact":c.Order!.ContactFleetId="fleet.hidden";break;
            case "stale-contact":c.Order!.ExpectedContactSeenTurn++;break;
            case "unknown-route":c.Order!.RouteNodeIds=new List<string>{"nav.unknown"};break;
            case "stay-route":c.Order!.Intent=CampaignTravelIntent.StayEngage;c.Order.ContactFleetId=null;c.Order.ExpectedContactSeenTurn=null;break;
        }
        var before=Json(app.ExportSave());Assert.False(app.SubmitTravelOrder(c).Accepted);Assert.Equal(before,Json(app.ExportSave()));
    }
    private static string AddFleet(CampaignTacticalTravelPackageDto p,string actor,string suffix,string origin,string templateId="ship.equipment-surveyor")
    {
        var game=p.CorePackage!.Campaign!.InitialState;var defs=p.CorePackage.Campaign.Definitions;
        var original=game.Ships!.Single(s=>s.ShipId==templateId);var ship=Clone(original);
        ship.ShipId="ship."+suffix;ship.FleetId="fleet."+suffix;ship.OwnerCivilizationId=actor;game.Ships!.Add(ship);
        var fleet=Clone(game.Fleets!.First());fleet.FleetId=ship.FleetId;fleet.OwnerCivilizationId=actor;fleet.LocationNodeId=origin;
        fleet.RouteNodeIds=new List<string>();fleet.ShipIds=new List<string>{ship.ShipId};game.Fleets!.Add(fleet);
        foreach(var(oldId,newId)in new[]{(original.ShipId,ship.ShipId),(game.Fleets!.First().FleetId,fleet.FleetId)})
        {var identity=Clone(game.EntityIdentities.Single(e=>e.EntityId==oldId));identity.EntityId=newId;game.EntityIdentities.Add(identity);}
        var provision=Clone(defs.AuthoredTacticalEquipment!.InitialShipProvisions!.Single(s=>s.ShipId==original.ShipId));
        provision.ShipId=ship.ShipId;provision.OwnerCivilizationId=actor;defs.AuthoredTacticalEquipment.InitialShipProvisions!.Add(provision);
        p.InitialOrders!.Add(new CampaignTravelOrderDto { FleetId=fleet.FleetId,Intent=CampaignTravelIntent.Stay,RouteNodeIds=new List<string>() });
        return fleet.FleetId;
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LaterArrivalJoinsOnlyWhenLawfullyConnectedAndUnrelatedFleetContinues(bool engage)
    {
        var p=Package();p.CorePackage!.Campaign!.InitialState.Fleets!.Single(f=>f.FleetId==FA).LocationNodeId="nav.bajor";
        p.InitialOrders!.Single(o=>o.FleetId==FA).Intent=CampaignTravelIntent.StayEngage;
        var extra=AddFleet(p,A,"reinforcement","nav.sol");
        var app=Start(p);
        Assert.True(app.SubmitTravelOrder(Order(app,B,FB,CampaignTravelIntent.Leave,new[]{"nav.bajor","nav.bajoran-wormhole"})).Accepted);
        Assert.True(app.SubmitTravelOrder(Order(app,A,extra,engage?CampaignTravelIntent.Follow:CampaignTravelIntent.Leave,
            engage?new[]{"nav.bajor"}:new[]{"nav.bajor","nav.bajoran-wormhole"},engage?FB:null)).Accepted);
        Advance(app);var id=Assert.Single(app.GetEncounters(A));var owned=app.Observe(id,A).StandingOrders;
        Assert.Equal(engage?2:1,owned.Count);
        var r=app.GetOwnTravelReceipts(A).Single(r=>r.FleetId==extra);
        Assert.Equal(engage,r.StoppedByEncounter);
        if(engage){Assert.Equal("1",r.StopTimeNumerator);Assert.Equal("2",r.StopTimeDenominator);Assert.Equal("nav.bajor",r.EndNodeId);}
        else Assert.Equal("nav.bajoran-wormhole",r.EndNodeId);
        var save=app.ExportSave();Assert.Single(save.Requests);
        Assert.Equal(engage?3:2,save.Requests[0].Participants.Count);
        Assert.Equal(Json(save),Json(CampaignTacticalTravelApplication.LoadReviewSave(p,Clone(save)).ExportSave()));
    }
    [Theory]
    [InlineData("en-US")]
    [InlineData("nl-NL")]
    [InlineData("tr-TR")]
    public void EqualTimeContactsHaveNoOrdinalFirstMoverAndInputPermutationsMatch(string culture)
    {
        var previous=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
            var p=Package();PursuitBudgets(p,2,2);var alternate=Clone(p);
            alternate.CorePackage!.Campaign!.InitialState.Fleets!.Reverse();alternate.Hostilities!.Reverse();alternate.InitialContacts!.Reverse();alternate.InitialOrders!.Reverse();
            var one=Start(p);var two=Start(alternate);
            foreach(var app in new[]{one,two})
                foreach(var actor in (app==one?new[]{A,B}:new[]{B,A}))
                {
                    var c=Order(app,actor,actor==A?FA:FB,CampaignTravelIntent.Follow,new[]{"nav.bajor","nav.bajoran-wormhole"},actor==A?FB:FA);
                    c.CommandId=actor==A?"command.equal.a":"command.equal.b";Assert.True(app.SubmitTravelOrder(c).Accepted);
                }
            Advance(one);Advance(two);Assert.Equal(Json(one.ExportSave()),Json(two.ExportSave()));
            foreach(var actor in new[]{A,B})Assert.Equal("2",one.GetOwnTravelReceipts(actor)[0].StopTimeDenominator);
        }
        finally{CultureInfo.CurrentCulture=previous;}
    }
    [Theory]
    [InlineData("Research")]
    [InlineData("Production")]
    [InlineData("Movement")]
    [InlineData("EncounterCreation")]
    public void FullTurnFailureRollsBackEarlierActualPhasesOrdersRngAndResources(string phase)
    {
        var p=Package();p.InitialOrders!.ForEach(o=>o.Intent=CampaignTravelIntent.StayEngage);
        var session=CampaignTacticalReviewSession.StartTravel(p);
        session.PhysicalPhaseProbe=current=>current.ToString()!=phase;
        var before=Json(session.ExportTravelSave());var status=session.CaptureStatus();
        Assert.False(session.AdvanceTurn(status.Turn,status.CampaignRevision).Accepted);Assert.Equal(before,Json(session.ExportTravelSave()));
        session.PhysicalPhaseProbe=null;Assert.True(session.AdvanceTurn(status.Turn,status.CampaignRevision).Accepted);
    }
    [Theory]
    [InlineData("receipt-time")]
    [InlineData("receipt-cost")]
    [InlineData("location")]
    [InlineData("route")]
    [InlineData("phase")]
    [InlineData("request-owner")]
    [InlineData("knowledge")]
    [InlineData("no-proof")]
    public void ForgedPhysicalSaveCannotImportAnItineraryOrEncounterOutcome(string defect)
    {
        var p=Package();PursuitBudgets(p,3,2);var app=Start(p);
        Assert.True(app.SubmitTravelOrder(Order(app,A,FA,CampaignTravelIntent.Follow,new[]{"nav.bajor"},FB)).Accepted);
        Assert.True(app.SubmitTravelOrder(Order(app,B,FB,CampaignTravelIntent.Leave,new[]{"nav.bajor","nav.bajoran-wormhole"})).Accepted);
        Advance(app);var saved=app.ExportSave();
        switch(defect)
        {
            case "receipt-time":saved.LastMovement[0].StopTimeNumerator="0";break;
            case "receipt-cost":saved.LastMovement[0].CostPaid++;break;
            case "location":saved.Core.Campaign.GameState.Fleets![0].LocationNodeId="nav.sol";break;
            case "route":saved.Orders[0].RouteNodeIds.Add("nav.bajoran-wormhole");break;
            case "phase":saved.Phase=SessionPhase.Planning;break;
            case "request-owner":saved.Requests[0].Participants[0].CivilizationId="civ.dominion";break;
            case "knowledge":saved.Contacts[0].TrackingNodeIds.Clear();break;
            case "no-proof":saved.MovementProof=null;break;
        }
        var before=Json(app.ExportSave());Assert.Throws<ArgumentException>(()=>CampaignTacticalTravelApplication.LoadReviewSave(p,Clone(saved)));
        Assert.Equal(before,Json(app.ExportSave()));
    }
    [Fact]
    public void StaleKnowledgeCannotSubmitPursuitAndUnknownFleetCannotBeDisclosedThroughQueries()
    {
        var p=Package();p.ContactFreshnessTurns=0;var app=Start(p);Advance(app);
        Assert.False(app.GetKnownContacts(A)[0].Fresh);
        var c=Order(app,A,FA,CampaignTravelIntent.Follow,new[]{"nav.bajor"},FB);
        var before=Json(app.ExportSave());Assert.False(app.SubmitTravelOrder(c).Accepted);Assert.Equal(before,Json(app.ExportSave()));
        Assert.Throws<ArgumentException>(()=>app.GetOwnTravelReceipts("civ.unknown"));
        Assert.Single(app.GetOwnStrategicFleets(A));
        Assert.DoesNotContain(typeof(ICampaignTacticalTravelSession).GetMethods(),m=>m.ReturnType==typeof(SessionSnapshot));
    }
    [Fact]
    public void MidEdgeOrdersCannotRewindOrTeleportAnExistingWeightedTransit()
    {
        var p=Package();PursuitBudgets(p,1,1);
        p.CorePackage!.Campaign!.Definitions.NavigationConnections!.Single(e=>e.ConnectionId=="connection.sol-bajor").BaseTraversalCost=5;
        var app=Start(p);Assert.True(app.SubmitTravelOrder(Order(app,A,FA,CampaignTravelIntent.Leave,new[]{"nav.bajor"})).Accepted);
        Advance(app);var before=Json(app.ExportSave());
        Assert.False(app.SubmitTravelOrder(Order(app,A,FA,CampaignTravelIntent.StayEngage)).Accepted);
        Assert.False(app.SubmitTravelOrder(Order(app,A,FA,CampaignTravelIntent.Follow,new[]{"nav.cardassia"},FB)).Accepted);
        Assert.Equal(before,Json(app.ExportSave()));
    }
    private static void Contact(CampaignTacticalTravelPackageDto p,string actor,string fleet)
    {
        p.InitialContacts!.Add(new CampaignContactKnowledgeDto { ActorId=actor,FleetId=fleet,LastSeenNodeId="nav.sol",LastSeenTurn=1,
            TrackingNodeIds=p.CorePackage!.Campaign!.Definitions.NavigationRegions!.Select(n=>n.NodeId).ToList() });
    }
    [Fact]
    public void DisconnectedHostileComponentsAtOneNodeAllocateSeparateBattlesAndEachFleetOnce()
    {
        var p=Package();var extraA=AddFleet(p,A,"second-a","nav.sol");var extraB=AddFleet(p,B,"second-b","nav.sol");
        Contact(p,A,extraB);Contact(p,B,extraA);var app=Start(p);
        foreach(var(actor,fleet,target)in new[]{(A,FA,FB),(B,FB,FA),(A,extraA,extraB),(B,extraB,extraA)})
            Assert.True(app.SubmitTravelOrder(Order(app,actor,fleet,CampaignTravelIntent.Follow,target:target)).Accepted);
        Advance(app);Assert.Equal(2,app.GetEncounters(A).Count);
        Assert.Equal(4,app.ExportSave().Requests.SelectMany(r=>r.Participants).Select(f=>f.FleetId).Distinct().Count());
        var saved=app.ExportSave();app=CampaignTacticalTravelApplication.LoadReviewSave(p,Clone(saved));
        Complete(app);Assert.Empty(app.GetEncounters(A));Assert.Equal(2,app.CaptureStatus().Turn);
        Assert.Equal(Json(app.ExportSave()),Json(CampaignTacticalTravelApplication.LoadReviewSave(p,Clone(app.ExportSave())).ExportSave()));
    }
    [Fact]
    public void NeutralLinkedOwnersAreExplicitlyGatedInsteadOfReceivingAccidentalFriendlyFire()
    {
        var p=Package();const string third="civ.dominion";var extra=AddFleet(p,third,"third","nav.sol");
        p.Hostilities!.Add(new(){CivilizationA=B,CivilizationB=third});Contact(p,B,extra);
        p.InitialOrders!.ForEach(o=>o.Intent=CampaignTravelIntent.StayEngage);
        var session=CampaignTacticalReviewSession.StartTravel(p);var before=Json(session.ExportTravelSave());var status=session.CaptureStatus();
        Assert.Equal(CampaignTacticalError.InvalidBatch,session.AdvanceTurn(status.Turn,status.CampaignRevision).Error);
        Assert.Contains("non-clique sides",session.LastPhysicalFailure);
        Assert.Equal(before,Json(session.ExportTravelSave()));
    }
    [Fact]
    public void WaitingKnownContactUsesOnlyTheDeclaredTrackingGrid()
    {
        var p=Package();p.CorePackage!.Campaign!.InitialState.Fleets!.Single(f=>f.FleetId==FA).LocationNodeId="nav.bajor";
        p.InitialContacts!.Single(k=>k.ActorId==A).TrackingNodeIds=new List<string>{"nav.sol"};
        var app=Start(p);Assert.False(app.SubmitTravelOrder(Order(app,A,FA,CampaignTravelIntent.Follow,target:FB)).Accepted);
        p.InitialContacts!.Single(k=>k.ActorId==A).TrackingNodeIds.Add("nav.bajor");app=Start(p);
        Assert.True(app.SubmitTravelOrder(Order(app,A,FA,CampaignTravelIntent.Follow,target:FB)).Accepted);
        Assert.True(app.SubmitTravelOrder(Order(app,B,FB,CampaignTravelIntent.Leave,new[]{"nav.bajor","nav.bajoran-wormhole"})).Accepted);
        Advance(app);Assert.Single(app.GetEncounters(A));
        Assert.Equal("3",app.GetOwnTravelReceipts(A)[0].StopTimeDenominator);
    }
    [Fact]
    public void PhysicalFinalizationFaultKeepsCompletedRoundsAndAllPoolsUncommitted()
    {
        var p=Package();p.InitialOrders!.ForEach(o=>o.Intent=CampaignTravelIntent.StayEngage);
        var app=Start(p);Advance(app);
        var id=app.GetEncounters(A)[0];
        while(!app.Observe(id,A).Complete)
            foreach(var actor in new[]{A,B})
            {
                var v=app.Observe(id,actor);Assert.True(app.CommitRound(new(){ActorId=actor,EncounterId=id,CommandId="command.final."+actor+"."+v.Round,
                    ExpectedTurn=v.Turn,ExpectedRound=v.Round,ExpectedBoundaryRevision=v.BoundaryRevision,Replacements=new()}).Accepted);
            }
        var session=(CampaignTacticalReviewSession)app.GetType().GetField("session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(app)!;
        var before=Json(app.ExportSave());session.FinalizationProbe=_=>throw new InvalidOperationException("Injected finalization fault");
        var s=app.CaptureStatus();Assert.False(app.FinalizeBatch(s.Turn,s.CampaignRevision).Accepted);Assert.Equal(before,Json(app.ExportSave()));
        session.FinalizationProbe=null;Assert.True(app.FinalizeBatch(s.Turn,s.CampaignRevision).Accepted);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AfterActualAftermathLeaveAndFollowUseNewTurnTravelWithoutResettingResources(bool follows)
    {
        var p=Package();PursuitBudgets(p,3,2);p.InitialOrders!.ForEach(o=>o.Intent=CampaignTravelIntent.StayEngage);
        var app=Start(p);Advance(app);Complete(app);Assert.Equal(2,app.CaptureStatus().Turn);
        var ownBefore=Json(app.InspectOwnShips(A));
        Assert.True(app.SubmitTravelOrder(Order(app,A,FA,follows?CampaignTravelIntent.Follow:CampaignTravelIntent.StayEngage,
            follows?new[]{"nav.bajor"}:null,follows?FB:null)).Accepted);
        Assert.True(app.SubmitTravelOrder(Order(app,B,FB,CampaignTravelIntent.Leave,new[]{"nav.bajor","nav.bajoran-wormhole"})).Accepted);
        Advance(app);Assert.Equal(ownBefore,Json(app.InspectOwnShips(A)));
        if(follows)
        {Assert.Single(app.GetEncounters(A));Assert.Equal("2",app.GetOwnTravelReceipts(A)[0].StopTimeDenominator);Assert.Equal(2,app.CaptureStatus().Turn);}
        else {Assert.Empty(app.GetEncounters(A));Assert.Equal(3,app.CaptureStatus().Turn);Assert.Equal("nav.bajoran-wormhole",app.GetOwnTravelReceipts(B)[0].EndNodeId);}
        Assert.Equal(Json(app.ExportSave()),Json(CampaignTacticalTravelApplication.LoadReviewSave(p,Clone(app.ExportSave())).ExportSave()));
    }
    [Fact]
    public void SlowestCompiledShipAndActiveTraversalEffectsDetermineActualCostAndBudget()
    {
        var p=Package();p.CorePackage!.Campaign!.InitialState.TraversalEffects!.Add(new TraversalEffectDto {
            EffectId="effect.travel-test",NodeId="nav.sol",MovementBudgetPenalty=1,TraversalCostIncrease=3,StartTurn=1,EndTurn=1 });
        var app=Start(p);
        Assert.True(app.SubmitTravelOrder(Order(app,A,FA,CampaignTravelIntent.Leave,new[]{"nav.bajor"})).Accepted);
        Advance(app);var r=app.GetOwnTravelReceipts(A)[0];
        Assert.Equal(4,r.TransitTotalCost);Assert.Equal(1,r.TransitProgress);Assert.Equal(1,r.CostPaid);
        Assert.Equal(Json(app.ExportSave()),Json(CampaignTacticalTravelApplication.LoadReviewSave(p,Clone(app.ExportSave())).ExportSave()));
        Advance(app);r=app.GetOwnTravelReceipts(A)[0];Assert.Equal(4,r.TransitTotalCost);Assert.Equal(3,r.TransitProgress);
    }
    [Theory]
    [InlineData("request")]
    [InlineData("orders")]
    [InlineData("core")]
    [InlineData("proof")]
    public void NullPhysicalSaveCollectionsRejectAsValidationErrorsWithoutMutatingCarrier(string defect)
    {
        var p=Package();p.InitialOrders!.ForEach(o=>o.Intent=CampaignTravelIntent.StayEngage);var app=Start(p);Advance(app);
        var saved=app.ExportSave();
        switch(defect)
        {
            case "request":saved.Requests[0]=null!;break;
            case "orders":saved.Orders[0].RouteNodeIds=null!;break;
            case "core":saved.Core=null!;break;
            case "proof":saved.MovementProof!.Start=null!;break;
        }
        var before=Json(saved);Assert.Throws<ArgumentException>(()=>CampaignTacticalTravelApplication.LoadReviewSave(p,saved));Assert.Equal(before,Json(saved));
    }
    [Theory]
    [InlineData("position")]
    [InlineData("freshness")]
    [InlineData("proof-turn")]
    public void PlanningSaveCannotForgeMovementOrRefreshUnobservedContact(string defect)
    {
        var p=Package();p.ContactFreshnessTurns=0;var app=Start(p);Advance(app);var saved=app.ExportSave();
        switch(defect)
        {
            case "position":saved.Core.Campaign.GameState.Fleets![0].LocationNodeId="nav.bajor";break;
            case "freshness":saved.Contacts[0].LastSeenTurn=2;break;
            case "proof-turn":saved.MovementProof!.Start.TurnNumber=2;break;
        }
        var before=Json(saved);Assert.Throws<ArgumentException>(()=>CampaignTacticalTravelApplication.LoadReviewSave(p,saved));Assert.Equal(before,Json(saved));
    }
    [Fact]
    public void PhysicalAiOnlyContactResolvesPrivatelyAndSurvivesReloadAndAftermath()
    {
        var p=Package();p.InitialOrders!.ForEach(o=>o.Intent=CampaignTravelIntent.StayEngage);
        foreach(var c in p.CorePackage!.Campaign!.InitialState.Civilizations!){c.ControllerKind=ControllerKind.AI;c.Credits=0;}
        // The real planner has no lawful departure route in this authored fixture.
        foreach(var known in p.CorePackage.Campaign.InitialState.NavigationKnowledge!)
            foreach(var edge in known.Connections!)edge.Level=NavigationKnowledgeLevel.Unknown;
        var session=CampaignTacticalReviewSession.StartTravel(p);var s=session.CaptureStatus();
        Assert.True(session.AdvanceTurn(s.Turn,s.CampaignRevision).Accepted,session.LastPhysicalFailure);
        var saved=session.ExportTravelSave();Assert.Single(saved.Requests);Assert.True(session.CaptureStatus().CanFinalizeBatch);
        Assert.All(saved.Core.Battles,b=>Assert.Equal(3,b.ResolvedRounds.Count));
        session=CampaignTacticalReviewSession.LoadTravel(p,Clone(saved));s=session.CaptureStatus();Assert.True(session.FinalizeBatch(s.Turn,s.CampaignRevision).Accepted);
        Assert.Equal(2,session.CaptureStatus().Turn);
        Assert.Equal(Json(session.ExportTravelSave()),Json(CampaignTacticalReviewSession.LoadTravel(p,Clone(session.ExportTravelSave())).ExportTravelSave()));
    }
}
