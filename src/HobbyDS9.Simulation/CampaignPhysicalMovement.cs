using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    internal sealed class PhysicalMeeting
    {
        internal string Node="";
        internal StrategicTravelTime Time=StrategicTravelTime.Zero;
        internal readonly HashSet<string> Fleets=new HashSet<string>(StringComparer.Ordinal);
    }
    internal sealed class PhysicalMovementResult
    {
        internal readonly List<PhysicalMeeting> Meetings=new List<PhysicalMeeting>();
        internal readonly List<CampaignFleetTravelReceiptDto> Receipts=new List<CampaignFleetTravelReceiptDto>();
    }
    internal static class CampaignPhysicalMovement
    {
        private sealed class Leg
        {
            internal string Node="";
            internal long Cost,InitialProgress;
            internal StrategicTravelTime Departure=StrategicTravelTime.Zero,Arrival=StrategicTravelTime.Zero;
        }
        private sealed class Visit
        {
            internal string Node="";
            internal StrategicTravelTime Arrival=StrategicTravelTime.Zero;
            internal bool Dwell;
        }
        private sealed class Plan
        {
            internal FleetStateDto Fleet=null!;
            internal long Budget;
            internal readonly List<Leg> Legs=new List<Leg>();
            internal readonly List<Visit> Visits=new List<Visit>();
            internal StrategicTravelTime? Stop;
        }
        internal static bool Hostile(IEnumerable<CampaignDeclaredHostilityDto> rows,string a,string b)
            =>a!=b&&rows.Any(h=>h.CivilizationA==a&&h.CivilizationB==b||h.CivilizationA==b&&h.CivilizationB==a);
        internal static PhysicalMovementResult Execute(GameStateDto game,DefinitionCatalogDto definitions,
            IReadOnlyList<CampaignTravelOrderDto> orders,IReadOnlyList<CampaignContactKnowledgeDto> contacts,
            IReadOnlyList<CampaignDeclaredHostilityDto> hostilities,int freshness)
        {
            var graph=new WeightedNavigationGraph(definitions);
            var plans=new Dictionary<string,Plan>(StringComparer.Ordinal);
            foreach(var fleet in game.Fleets!.OrderBy(f=>f.FleetId,StringComparer.Ordinal))
            {
                var plan=new Plan { Fleet=fleet,Budget=graph.Budget(game,fleet,definitions,out _) };
                var time=StrategicTravelTime.Zero;var previous=fleet.LocationNodeId;
                if(fleet.TransitTotalCost==0)plan.Visits.Add(new Visit { Node=previous,Dwell=fleet.RouteNodeIds!.Count==0 });
                for(var i=0;i<fleet.RouteNodeIds!.Count;i++)
                {
                    var node=fleet.RouteNodeIds[i];
                    var cost=i==0&&fleet.TransitTotalCost>0?fleet.TransitTotalCost:graph.Cost(game,previous,node).EffectiveCost;
                    var paid=i==0?fleet.TransitProgress:0;
                    var arrival=time+new StrategicTravelTime(cost-paid,plan.Budget);
                    plan.Legs.Add(new Leg { Node=node,Cost=cost,InitialProgress=paid,Departure=time,Arrival=arrival });
                    if(arrival>StrategicTravelTime.One)break;
                    plan.Visits.Add(new Visit { Node=node,Arrival=arrival,Dwell=i==fleet.RouteNodeIds.Count-1||arrival.Equals(StrategicTravelTime.One) });
                    time=arrival;previous=node;
                    if(time.Equals(StrategicTravelTime.One))break;
                }
                plans.Add(fleet.FleetId,plan);
            }
            bool Wants(Plan own,Plan other,string node)
            {
                var order=orders.Single(o=>o.FleetId==own.Fleet.FleetId);
                if(order.Intent!=CampaignTravelIntent.StayEngage
                    &&!(order.Intent==CampaignTravelIntent.Follow&&order.ContactFleetId==other.Fleet.FleetId))return false;
                return contacts.Any(k=>k.ActorId==own.Fleet.OwnerCivilizationId&&k.FleetId==other.Fleet.FleetId
                    &&k.LastSeenTurn<=game.TurnNumber&&(long)game.TurnNumber-k.LastSeenTurn<=freshness&&k.TrackingNodeIds.Contains(node,StringComparer.Ordinal));
            }
            var result=new PhysicalMovementResult();
            var assigned=new Dictionary<string,PhysicalMeeting>(StringComparer.Ordinal);
            var times=plans.Values.SelectMany(p=>p.Visits.Select(v=>v.Arrival)).Append(StrategicTravelTime.Zero).Append(StrategicTravelTime.One)
                .Distinct().OrderBy(t=>t).ToList();
            foreach(var time in times)
            {
                var occupancy=new Dictionary<string,List<Plan>>(StringComparer.Ordinal);
                foreach(var plan in plans.Values)
                {
                    string? node=null;
                    if(assigned.TryGetValue(plan.Fleet.FleetId,out var existing))node=existing.Node;
                    else
                    {
                        var visit=plan.Visits.FirstOrDefault(v=>v.Arrival<=time&&(v.Dwell||v.Arrival.Equals(time))
                            &&(!time.Equals(StrategicTravelTime.Zero)||v.Dwell));
                        node=visit?.Node; // departure-first at shared start; no edge-interior occupancy.
                    }
                    if(node==null)continue;
                    if(!occupancy.TryGetValue(node,out var at))occupancy.Add(node,at=new List<Plan>());
                    at.Add(plan);
                }
                foreach(var location in occupancy.OrderBy(p=>p.Key,StringComparer.Ordinal))
                {
                    var links=new List<(string A,string B)>();
                    var at=location.Value.OrderBy(p=>p.Fleet.FleetId,StringComparer.Ordinal).ToList();
                    for(var i=0;i<at.Count;i++)for(var j=i+1;j<at.Count;j++)
                        if(Hostile(hostilities,at[i].Fleet.OwnerCivilizationId,at[j].Fleet.OwnerCivilizationId)
                            &&(Wants(at[i],at[j],location.Key)||Wants(at[j],at[i],location.Key)))
                            links.Add((at[i].Fleet.FleetId,at[j].Fleet.FleetId));
                    // Existing participants remain an indivisible pending component when reinforcements arrive.
                    foreach(var pending in result.Meetings.Where(m=>m.Node==location.Key))
                    {
                        var ids=pending.Fleets.OrderBy(id=>id,StringComparer.Ordinal).ToList();
                        for(var i=1;i<ids.Count;i++)links.Add((ids[0],ids[i]));
                    }
                    var unseen=links.SelectMany(l=>new[]{l.A,l.B}).ToHashSet(StringComparer.Ordinal);
                    while(unseen.Count>0)
                    {
                        var component=new HashSet<string>(StringComparer.Ordinal) { unseen.OrderBy(id=>id,StringComparer.Ordinal).First() };
                        bool changed;
                        do { changed=false;foreach(var link in links)if(component.Contains(link.A)||component.Contains(link.B))
                            { changed|=component.Add(link.A);changed|=component.Add(link.B); } } while(changed);
                        foreach(var id in component)unseen.Remove(id);
                        var old=result.Meetings.Where(m=>m.Fleets.Overlaps(component)).ToList();
                        var owners=component.Select(id=>plans[id].Fleet.OwnerCivilizationId).Distinct(StringComparer.Ordinal).ToList();
                        // Core currently equates owner and tactical side. Reject a non-clique instead of firing on neutral allies.
                        Require(owners.All(a=>owners.All(b=>a==b||Hostile(hostilities,a,b))),"Connected non-clique sides require a separately reviewed owner/side contract.");
                        var meeting=old.Count>0?old.OrderBy(m=>m.Time).ThenBy(m=>m.Node,StringComparer.Ordinal).First()
                            :new PhysicalMeeting { Node=location.Key,Time=time };
                        if(old.Count==0)result.Meetings.Add(meeting);
                        foreach(var other in old.Where(m=>m!=meeting))result.Meetings.Remove(other);
                        foreach(var id in component)
                        {
                            if(!assigned.ContainsKey(id))plans[id].Stop=time;
                            meeting.Fleets.Add(id);assigned[id]=meeting;
                        }
                    }
                }
            }
            foreach(var plan in plans.Values.OrderBy(p=>p.Fleet.FleetId,StringComparer.Ordinal))
            {
                var fleet=plan.Fleet;var end=plan.Stop??StrategicTravelTime.One;
                long paid=0,completed=0;var node=fleet.LocationNodeId;string? destination=null;
                long total=0,progress=0;
                foreach(var leg in plan.Legs)
                {
                    if(leg.Arrival<=end)
                    { paid=checked(paid+leg.Cost-leg.InitialProgress);completed++;node=leg.Node;continue; }
                    if(end>leg.Departure)
                    {
                        var current=(end-leg.Departure)*plan.Budget+new StrategicTravelTime(leg.InitialProgress,1);
                        Require(current.D==BigInteger.One&&current.N>=0&&current.N<=long.MaxValue,"Nonintegral/overflowing full-week edge progress.");
                        destination=leg.Node;total=leg.Cost;progress=(long)current.N;paid=checked(paid+progress-leg.InitialProgress);
                    }
                    break;
                }
                if(assigned.TryGetValue(fleet.FleetId,out var meeting))
                { node=meeting.Node;destination=null;total=progress=0;fleet.RouteNodeIds!.Clear(); }
                else fleet.RouteNodeIds!.RemoveRange(0,(int)completed);
                fleet.LocationNodeId=node;fleet.TransitTotalCost=total;fleet.TransitProgress=progress;
                game.NavigationStatistics!.TraversalCostPaid=checked(game.NavigationStatistics.TraversalCostPaid+paid);
                game.NavigationStatistics.EdgesCompleted=checked(game.NavigationStatistics.EdgesCompleted+completed);
                result.Receipts.Add(new CampaignFleetTravelReceiptDto { FleetId=fleet.FleetId,OwnerId=fleet.OwnerCivilizationId,Turn=game.TurnNumber,
                    EndNodeId=node,TransitDestinationId=destination,TransitTotalCost=total,TransitProgress=progress,CostPaid=paid,EdgesCompleted=completed,
                    StoppedByEncounter=plan.Stop.HasValue,StopTimeNumerator=end.Numerator,StopTimeDenominator=end.Denominator });
            }
            game.NavigationStatistics!.PartialTraversalTurns=checked(game.NavigationStatistics.PartialTraversalTurns+plans.Values.Count(p=>p.Fleet.TransitTotalCost>0));
            return result;
        }
    }
}
