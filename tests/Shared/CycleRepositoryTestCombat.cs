using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Xunit;

namespace HobbyDS9.RepositoryTestFixtures;

internal static class CycleRepositoryTestCombat
{
    private const string Human = "civ.federation";

    public static void ReachAwaitingCombat(PhysicalReviewHost host, ICampaignColonisationCycleSession s, bool useFormat18Itinerary)
    {
        var frame=host.Read(Human);
        var fleet=Assert.Single(frame.OwnStrategicFleets).FleetId.Value;
        var stamp=s.CaptureStatus();Assert.True(s.SubmitActivity(new(){ActorId=Human,CommandId="command.repository.combat.build",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,
            Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId="blueprint.ready.defiant-combat",BlueprintRevision=1}).Accepted);
        var advances=0;
        while(!s.CaptureColonisation(Human).OwnShipSources.Any(b=>b.BlueprintId=="blueprint.ready.defiant-combat"&&b.Revision==1)&&advances<80)
        {
            frame=host.Read(Human);
            Assert.True(host.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.repository.combat.prepare."+advances,ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
                Order=new(){FleetId=fleet,Intent=CampaignTravelIntent.StayEngage,RouteNodeIds=new()}}).Accepted);
            Advance(host);advances++;
        }
        var built=Assert.Single(s.CaptureColonisation(Human).OwnShipSources,b=>b.BlueprintId=="blueprint.ready.defiant-combat"&&b.Revision==1);
        Assert.Contains(host.Read(Human).OwnStrategicFleets.Single(f=>f.FleetId.Value==fleet).ShipIds,ship=>ship.Value==built.ShipId);
        Assert.Equal(SessionPhase.Planning,s.CaptureStatus().Phase);
        if(useFormat18Itinerary)
        {
            // Fixed seed-42 test itinerary. Each destination becomes public
            // before we issue the next single-edge order.
            var itinerary=new[]{"0016","0015","0042","0038","0039","0054","0036","0002"};
            foreach(var suffix in itinerary)
            {
                frame=host.Read(Human);
                var destination="nav.natural."+suffix;
                Assert.Contains(s.CaptureGeneratedMap(Human).Regions,r=>r.NodeId==destination);
                var travel=host.FindKnownRoute(frame,fleet,destination,false);
                Assert.True(travel.Route.Success);
                Assert.Equal(destination,Assert.Single(travel.Route.Steps));
                Assert.True(host.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.repository.combat.expedition."+suffix,ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
                    Order=new(){FleetId=fleet,Intent=CampaignTravelIntent.Leave,RouteNodeIds=travel.Route.Steps.ToList()}}).Accepted);
                for(var elapsed=0;elapsed<4;elapsed++)
                {
                    Advance(host);advances++;
                    Assert.Equal(SessionPhase.Planning,s.CaptureStatus().Phase);
                    var moved=host.Read(Human).OwnStrategicFleets.Single(f=>f.FleetId.Value==fleet);
                    if(moved.LocationNodeId.Value==destination&&moved.TransitTotalCost==0)break;
                }
                Assert.Equal(destination,host.Read(Human).OwnStrategicFleets.Single(f=>f.FleetId.Value==fleet).LocationNodeId.Value);
            }
            frame=host.Read(Human);
            Assert.Contains(frame.Travel!.KnownContacts,c=>c.Fresh&&c.LastSeenNodeId=="nav.natural.0002");
        }
        while(s.CaptureStatus().Phase==SessionPhase.Planning&&advances<80)
        {
            frame=host.Read(Human);
            var contact=frame.Travel!.KnownContacts.Where(c=>c.Fresh).OrderBy(c=>c.FleetId,StringComparer.Ordinal)
                .FirstOrDefault(c=>host.FindKnownRoute(frame,fleet,c.LastSeenNodeId,false).Route.Success);
            var route=contact==null?new List<string>():host.FindKnownRoute(frame,fleet,contact.LastSeenNodeId,false).Route.Steps.ToList();
            Assert.True(host.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.repository.combat.reach."+advances,ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
                Order=new(){FleetId=fleet,Intent=contact==null?CampaignTravelIntent.StayEngage:CampaignTravelIntent.Follow,ContactFleetId=contact?.FleetId,ExpectedContactSeenTurn=contact?.LastSeenTurn,RouteNodeIds=route}}).Accepted);
            Advance(host);advances++;
        }
        Assert.Equal(SessionPhase.AwaitingCombat,s.CaptureStatus().Phase);
        Assert.NotEmpty(s.GetEncounters(Human));
    }

    private static void Advance(PhysicalReviewHost host)
    {
        var frame = host.Read(Human);
        Assert.True(host.AdvancePlanning(frame).Accepted);
    }
}
