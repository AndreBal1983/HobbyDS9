using System.Diagnostics;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using Xunit.Abstractions;

namespace HobbyDS9.Simulation.Tests;

public sealed class CampaignPhysicalStressTests
{
    private const string A="civ.federation",B="civ.cardassian";
    private readonly ITestOutputHelper output;
    public CampaignPhysicalStressTests(ITestOutputHelper output){this.output=output;}
    private static string Json<T>(T value)=>JsonSerializer.Serialize(value);
    private static T Copy<T>(T value)=>JsonSerializer.Deserialize<T>(Json(value))!;
    private sealed class Fixture
    {
        internal readonly CampaignTacticalTravelPackageDto P=CampaignPhysicalTravelTests.Package();
        private readonly ShipStateDto armed,unarmed;
        private readonly FleetStateDto fleet;
        private readonly AuthoredTacticalShipProvisionDto armedPool,unarmedPool;
        internal Fixture()
        {
            var g=P.CorePackage!.Campaign!.InitialState;var d=P.CorePackage.Campaign.Definitions;
            armed=Copy(g.Ships!.First(s=>s.ShipDesignDefinitionId=="ship-design.source-cruiser"));
            unarmed=Copy(g.Ships!.Single(s=>s.ShipId=="ship.equipment-surveyor"));fleet=Copy(g.Fleets!.First());
            armedPool=Copy(d.AuthoredTacticalEquipment!.InitialShipProvisions!.Single(s=>s.ShipId==armed.ShipId));
            unarmedPool=Copy(d.AuthoredTacticalEquipment.InitialShipProvisions!.Single(s=>s.ShipId==unarmed.ShipId));
            var ids=g.Fleets!.Select(f=>f.FleetId).Concat(g.Ships!.Select(s=>s.ShipId)).ToHashSet();
            g.EntityIdentities.RemoveAll(e=>ids.Contains(e.EntityId));g.Fleets!.Clear();g.Ships!.Clear();
            d.AuthoredTacticalEquipment.InitialShipProvisions!.Clear();P.InitialOrders!.Clear();P.InitialContacts!.Clear();
            foreach(var design in d.ShipDesigns!)design.MovementBudget=design.DefinitionId==unarmed.ShipDesignDefinitionId?19:6;
            foreach(var n in d.NavigationRegions!)n.PhenomenonDefinitionIds!.Clear();
            g.TraversalEffects!.Clear();
            foreach(var edge in d.NavigationConnections!)edge.BaseTraversalCost=edge.ConnectionId=="connection.sol-bajor"?3:2;
        }
        internal string Add(string actor,string name,string node,bool weapon=true)
        {
            var g=P.CorePackage!.Campaign!.InitialState;var s=Copy(weapon?armed:unarmed);var f=Copy(fleet);var pool=Copy(weapon?armedPool:unarmedPool);
            s.ShipId="ship.stress."+name;s.FleetId="fleet.stress."+name;s.OwnerCivilizationId=actor;
            f.FleetId=s.FleetId;f.OwnerCivilizationId=actor;f.LocationNodeId=node;f.RouteNodeIds=new();f.ShipIds=new(){s.ShipId};f.TransitTotalCost=f.TransitProgress=0;
            pool.ShipId=s.ShipId;pool.OwnerCivilizationId=actor;g.Ships!.Add(s);g.Fleets!.Add(f);
            g.EntityIdentities.Add(new(){EntityId=s.ShipId,DefinitionId=s.ShipDesignDefinitionId});g.EntityIdentities.Add(new(){EntityId=f.FleetId});
            P.CorePackage.Campaign.Definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.Add(pool);
            P.InitialOrders!.Add(new(){FleetId=f.FleetId,Intent=CampaignTravelIntent.Stay,RouteNodeIds=new()});return f.FleetId;
        }
        internal void Contact(string actor,string target)
        {
            if(P.InitialContacts!.Any(k=>k.ActorId==actor&&k.FleetId==target))return;
            P.InitialContacts!.Add(new(){ActorId=actor,FleetId=target,LastSeenNodeId="nav.sol",LastSeenTurn=1,
                TrackingNodeIds=P.CorePackage!.Campaign!.Definitions.NavigationRegions!.Select(n=>n.NodeId).ToList()});
        }
    }
    private sealed record Intent(string Actor,string Fleet,CampaignTravelIntent Kind,string[] Route,string? Target=null);
    private static readonly string[] LongRoute=Enumerable.Range(0,4).SelectMany(_=>new[]{"nav.bajor","nav.bajoran-wormhole","nav.gamma-exit","nav.bajoran-wormhole","nav.bajor","nav.sol"}).ToArray();
    private static CampaignTacticalTravelPackageDto Reordered(CampaignTacticalTravelPackageDto original,bool reverse)
    {
        var p=Copy(original);if(!reverse)return p;
        p.InitialContacts!.Reverse();p.InitialOrders!.Reverse();p.Hostilities!.Reverse();
        var g=p.CorePackage!.Campaign!.InitialState;g.Fleets!.Reverse();g.Ships!.Reverse();g.EntityIdentities.Reverse();
        var d=p.CorePackage.Campaign.Definitions;d.NavigationConnections!.Reverse();d.ShipDesigns!.Reverse();d.AuthoredTacticalEquipment!.InitialShipProvisions!.Reverse();return p;
    }
    private static ICampaignTacticalTravelSession Start(CampaignTacticalTravelPackageDto p)=>CampaignTacticalTravelApplication.StartReviewPackage(p);
    private static ICampaignTacticalTravelSession Reload(CampaignTacticalTravelPackageDto p,ICampaignTacticalTravelSession app)=>CampaignTacticalTravelApplication.LoadReviewSave(p,Copy(app.ExportSave()));
    private static void Orders(ICampaignTacticalTravelSession app,IEnumerable<Intent> intents)
    {
        foreach(var i in intents)
        {
            var status=app.CaptureStatus();var dto=new SubmitCampaignTravelOrderDto {ActorId=i.Actor,CommandId="command.stress."+i.Fleet,
                ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,Order=new(){FleetId=i.Fleet,Intent=i.Kind,RouteNodeIds=i.Route.ToList(),ContactFleetId=i.Target,
                    ExpectedContactSeenTurn=i.Target==null?null:app.GetKnownContacts(i.Actor).Single(k=>k.FleetId==i.Target).LastSeenTurn}};
            Assert.True(app.SubmitTravelOrder(dto).Accepted);
            dto.Order.RouteNodeIds.Clear();dto.Order.ContactFleetId="fleet.mutated-input"; // owned command copy must survive.
        }
    }
    private static void Advance(ICampaignTacticalTravelSession app)
    {var s=app.CaptureStatus();Assert.True(app.AdvanceTurn(s.Turn,s.CampaignRevision).Accepted);}
    private static void TrafficOracle(CampaignTacticalTravelPackageDto p,ICampaignTacticalTravelSession app,IEnumerable<string> ids,int turn)
    {
        // Independent integer ledger: 24 authored edges total 56; spend at most 19 each turn.
        Assert.Equal(24,LongRoute.Length);var cumulative=0L;var completed=0;var from="nav.sol";
        var spent=Math.Min(19L*turn,56);var node=from;long transit=0,progress=0;
        foreach(var next in LongRoute)
        {
            var edge=p.CorePackage!.Campaign!.Definitions.NavigationConnections!.Single(e=>e.EndpointA==from&&e.EndpointB==next||e.EndpointB==from&&e.EndpointA==next);
            if(cumulative+edge.BaseTraversalCost<=spent){cumulative+=edge.BaseTraversalCost;completed++;node=next;from=next;}
            else {transit=edge.BaseTraversalCost;progress=spent-cumulative;break;}
        }
        if(progress==0)transit=0;
        foreach(var id in ids)
        {
            var r=app.GetOwnTravelReceipts(A).Single(r=>r.FleetId==id);var f=app.GetOwnStrategicFleets(A).Single(f=>f.FleetId.Value==id);
            Assert.False(r.StoppedByEncounter);Assert.Equal(Math.Min(19,56-19L*(turn-1)),r.CostPaid);
            Assert.Equal(node,r.EndNodeId);Assert.Equal(transit,r.TransitTotalCost);Assert.Equal(progress,r.TransitProgress);
            Assert.Equal(LongRoute.Skip(completed),f.RouteNodeIds.Select(n=>n.Value));
        }
    }
    private ICampaignTacticalTravelSession Resolve(CampaignTacticalTravelPackageDto p,ICampaignTacticalTravelSession app,bool reverse,bool reload)
    {
        var ids=app.GetEncounters(A).ToArray();if(reverse)Array.Reverse(ids);
        foreach(var id in ids)
            while(!app.Observe(id,A).Complete)
            {
                var commands=new[]{A,B}.Select(actor=>{var v=app.Observe(id,actor);return new CommitCampaignTacticalRoundDto {
                    ActorId=actor,EncounterId=id,CommandId="command.stress.round."+id+"."+actor+"."+v.Round,
                    ExpectedTurn=v.Turn,ExpectedRound=v.Round,ExpectedBoundaryRevision=v.BoundaryRevision,Replacements=new()};}).ToArray();
                if(reverse)Array.Reverse(commands);
                foreach(var c in commands){Assert.True(app.CommitRound(c).Accepted);if(reload)app=Reload(p,app);}
            }
        var s=app.CaptureStatus();Assert.True(s.CanFinalizeBatch);
        if(!reverse&&!reload)
        {
            var session=(CampaignTacticalReviewSession)app.GetType().GetField("session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(app)!;
            var before=Json(app.ExportSave());session.FinalizationProbe=index=>{if(index==ids.Length-1)throw new InvalidOperationException("Late batch failure");};
            Assert.False(app.FinalizeBatch(s.Turn,s.CampaignRevision).Accepted);Assert.Equal(before,Json(app.ExportSave()));session.FinalizationProbe=null;
        }
        Assert.True(app.FinalizeBatch(s.Turn,s.CampaignRevision).Accepted);return Reload(p,app);
    }
    [Fact]
    public void DozensOfIndependentBattlesAndLongRouteTrafficMatchEveryBoundaryAndFinalRoot()
    {
        var f=new Fixture();var intents=new List<Intent>();var pairs=new List<(string A,string B,string Node,string N,string D)>();var traffic=new List<string>();
        foreach(var(origin,node,n,d)in new[]{("nav.sol","nav.bajor","1","2"),("nav.cardassia","nav.badlands","1","3"),("nav.bajoran-wormhole","nav.gamma-exit","1","3")})
            for(var i=0;i<4;i++)
            {
                var a=f.Add(A,"a."+node+"."+i,origin);var b=f.Add(B,"b."+node+"."+i,node);f.Contact(A,b);f.Contact(B,a);
                intents.Add(new(A,a,CampaignTravelIntent.Follow,new[]{node},b));intents.Add(new(B,b,CampaignTravelIntent.Follow,Array.Empty<string>(),a));pairs.Add((a,b,node,n,d));
            }
        for(var i=0;i<24;i++){var id=f.Add(A,"traffic."+i,"nav.sol",false);traffic.Add(id);intents.Add(new(A,id,CampaignTravelIntent.Leave,LongRoute));}
        string? baseline=null;string? boundary=null;
        foreach(var variant in new[]{"canonical","reversed","repeated","resumed","reordered"})
        {
            var watch=Stopwatch.StartNew();var p=Reordered(f.P,variant=="reordered");var input=Copy(p);var app=Start(input);
            input.CorePackage!.Campaign!.InitialState.Fleets!.Clear();input.CorePackage.Campaign.Definitions.NavigationConnections!.Clear();input.InitialContacts!.Clear();
            Orders(app,variant=="reordered"?intents.AsEnumerable().Reverse():intents);Advance(app);
            var save=app.ExportSave();Assert.Equal(12,save.Requests.Count);
            Assert.Equal(24,save.Requests.SelectMany(r=>r.Participants).Select(v=>v.FleetId).Distinct().Count());
            foreach(var(a,b,node,n,d)in pairs)
            {
                var request=Assert.Single(save.Requests,r=>r.Participants.Any(v=>v.FleetId==a));
                Assert.Equal(new[]{a,b}.OrderBy(v=>v,StringComparer.Ordinal),request.Participants.Select(v=>v.FleetId).OrderBy(v=>v,StringComparer.Ordinal));
                Assert.Equal(node,request.LocationNodeId);
                foreach(var actor in new[]{A,B})
                {var r=app.GetOwnTravelReceipts(actor).Single(r=>r.FleetId==(actor==A?a:b));Assert.Equal(n,r.StopTimeNumerator);Assert.Equal(d,r.StopTimeDenominator);Assert.Equal(actor==A?(d=="2"?3:2):0,r.CostPaid);}
            }
            TrafficOracle(p,app,traffic,1);boundary??=Json(save);Assert.Equal(boundary,Json(save));
            // Corrupt the last request after earlier components are valid: reload cannot partially install a batch.
            var bad=Copy(save);bad.Requests[^1].Participants[0].Ships[0].Hull--;
            Assert.Throws<ArgumentException>(()=>CampaignTacticalTravelApplication.LoadReviewSave(p,bad));Assert.Equal(boundary,Json(app.ExportSave()));
            save.MovementProof!.Start.Fleets!.Clear();save.LastMovement.Clear();Assert.Equal(boundary,Json(app.ExportSave()));
            app=Resolve(p,app,variant=="reversed",variant=="resumed");
            // Two distant torpedo opportunities on rounds 1 and 3 consume four of the authored seven rounds.
            foreach(var actor in new[]{A,B})
            {var torpedoes=app.InspectOwnShips(actor).SelectMany(s=>s.Weapons).Where(w=>w.Class==TacticalWeaponClass.Torpedo).ToList();
                Assert.Equal(12,torpedoes.Count);Assert.All(torpedoes,w=>Assert.Equal(3,w.Ammunition));}
            foreach(var id in traffic)
                Assert.Equal(new long[]{5,10,0,8},app.InspectOwnShips(A).Single(s=>s.Condition.FleetId==id).Condition.Shields);
            baseline??=Json(app.ExportSave());Assert.Equal(baseline,Json(app.ExportSave()));
            output.WriteLine("48 fleets/48 ships, 12 encounters across 3 sectors, 24 traffic routes x24 legs, 36 joint rounds; "+variant+" elapsed-ms="+watch.ElapsedMilliseconds);
        }
    }
    [Fact]
    public void LargerNoncombatTrafficConservesWeightedBudgetAcrossThreeTurnsAndPermutations()
    {
        var f=new Fixture();var traffic=Enumerable.Range(0,160).Select(i=>f.Add(A,"large."+i,"nav.sol",false)).ToArray();
        var intents=traffic.Select(id=>new Intent(A,id,CampaignTravelIntent.Leave,LongRoute)).ToArray();string? baseline=null;
        foreach(var variant in new[]{"canonical","repeated","resumed","reordered"})
        {
            var watch=Stopwatch.StartNew();var p=Reordered(f.P,variant=="reordered");var app=Start(p);Orders(app,variant=="reordered"?intents.Reverse():intents);
            for(var turn=1;turn<=3;turn++){Advance(app);Assert.Empty(app.GetEncounters(A));TrafficOracle(p,app,traffic,turn);if(variant=="resumed")app=Reload(p,app);}
            var save=app.ExportSave();Assert.Equal(160*56,save.Core.Campaign.GameState.NavigationStatistics!.TraversalCostPaid);
            Assert.Equal(160*24,save.Core.Campaign.GameState.NavigationStatistics.EdgesCompleted);
            Assert.All(app.GetOwnStrategicFleets(A),v=>Assert.Empty(v.RouteNodeIds));baseline??=Json(save);Assert.Equal(baseline,Json(save));
            output.WriteLine("160 fleets/160 ships, 3840 route legs, 3 turns, no combat; "+variant+" elapsed-ms="+watch.ElapsedMilliseconds);
        }
    }
    [Fact]
    public void LaterBridgeMergesEarlierComponentsWithoutAliasingOrChangingAnyStopTime()
    {
        var f=new Fixture();var a1=f.Add(A,"merge.a1","nav.cardassia");var b1=f.Add(B,"merge.b1","nav.badlands");
        var a2=f.Add(A,"merge.a2","nav.badlands");var b2=f.Add(B,"merge.b2","nav.badlands");var bridge=f.Add(A,"merge.bridge","nav.bajor");
        f.P.CorePackage!.Campaign!.Definitions.NavigationConnections!.Single(e=>e.ConnectionId=="connection.badlands-bajor").BaseTraversalCost=3;
        foreach(var target in new[]{b1,b2})f.Contact(A,target);foreach(var target in new[]{a1,a2,bridge})f.Contact(B,target);
        var intents=new[]{new Intent(A,a1,CampaignTravelIntent.Follow,new[]{"nav.badlands"},b1),new(B,b1,CampaignTravelIntent.Follow,Array.Empty<string>(),a1),
            new(A,a2,CampaignTravelIntent.Follow,Array.Empty<string>(),b2),new(B,b2,CampaignTravelIntent.Follow,Array.Empty<string>(),bridge),
            new(A,bridge,CampaignTravelIntent.Follow,new[]{"nav.badlands"},b1)};
        string? baseline=null;
        foreach(var reverse in new[]{false,true})
        {
            var p=Reordered(f.P,reverse);var app=Start(p);Orders(app,reverse?intents.Reverse():intents);Advance(app);
            var save=app.ExportSave();Assert.Equal(5,Assert.Single(save.Requests).Participants.Count);Assert.All(save.LastMovement,r=>Assert.True(r.StoppedByEncounter));
            foreach(var(id,n,d,cost)in new[]{(a1,"1","3",2),(b1,"1","3",0),(a2,"0","1",0),(b2,"0","1",0),(bridge,"1","2",3)})
            {var r=save.LastMovement.Single(r=>r.FleetId==id);Assert.Equal(n,r.StopTimeNumerator);Assert.Equal(d,r.StopTimeDenominator);Assert.Equal(cost,r.CostPaid);}
            baseline??=Json(save);Assert.Equal(baseline,Json(save));Assert.Equal(baseline,Json(Reload(p,app).ExportSave()));
        }
    }
    [Fact]
    public void LateUnsupportedOwnerComponentRollsBackEvenAfterEarlierLawfulContacts()
    {
        var f=new Fixture();var a=f.Add(A,"late.a","nav.badlands");var b=f.Add(B,"late.b","nav.badlands");var third=f.Add("civ.dominion","late.third","nav.bajor");
        f.P.Hostilities!.Add(new(){CivilizationA=B,CivilizationB="civ.dominion"});f.Contact(A,b);f.Contact(B,a);f.Contact("civ.dominion",b);
        f.P.CorePackage!.Campaign!.Definitions.NavigationConnections!.Single(e=>e.ConnectionId=="connection.badlands-bajor").BaseTraversalCost=3;
        var app=Start(f.P);Orders(app,new[]{new Intent(A,a,CampaignTravelIntent.Follow,Array.Empty<string>(),b),new(B,b,CampaignTravelIntent.Follow,Array.Empty<string>(),a),
            new("civ.dominion",third,CampaignTravelIntent.Follow,new[]{"nav.badlands"},b)});
        var before=Json(app.ExportSave());var s=app.CaptureStatus();Assert.Equal(CampaignTacticalError.InvalidBatch,app.AdvanceTurn(s.Turn,s.CampaignRevision).Error);Assert.Equal(before,Json(app.ExportSave()));
        Assert.Empty(app.GetEncounters(A));Assert.Empty(app.GetOwnTravelReceipts(A));
    }
}
