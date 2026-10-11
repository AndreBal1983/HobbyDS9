using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    // Immutable authored topology. Dynamic cost and knowledge come from owned state.
    public sealed class WeightedNavigationGraph
    {
        private readonly Dictionary<string,NavigationRegionDefinitionDto> nodes;
        private readonly Dictionary<string,NavigationConnectionDto> edges;
        private readonly Dictionary<string,PhenomenonDefinitionDto> phenomena;
        public IReadOnlyList<NavigationRegionDefinitionSnapshot> Regions { get; }
        public IReadOnlyList<NavigationConnectionSnapshot> Connections { get; }
        public WeightedNavigationGraph(DefinitionCatalogDto definitions)
        {
            Validate(definitions).ThrowIfInvalid(nameof(definitions));
            nodes=definitions.NavigationRegions!.ToDictionary(n=>n.NodeId,PopulationData.Copy,StringComparer.Ordinal);
            edges=definitions.NavigationConnections!.ToDictionary(e=>e.ConnectionId,PopulationData.Copy,StringComparer.Ordinal);
            phenomena=definitions.Phenomena!.ToDictionary(p=>p.DefinitionId,PopulationData.Copy,StringComparer.Ordinal);
            Regions=nodes.Values.OrderBy(n=>n.NodeId,StringComparer.Ordinal).Select(n=>new NavigationRegionDefinitionSnapshot(n)).ToList().AsReadOnly();
            Connections=edges.Values.OrderBy(e=>e.ConnectionId,StringComparer.Ordinal).Select(e=>new NavigationConnectionSnapshot(e)).ToList().AsReadOnly();
        }
        public string? NodeForSystem(string system)=>Regions.SingleOrDefault(n=>n.AssociatedSystemId==system)?.NodeId;
        public string? SystemForNode(string node)=>nodes.TryGetValue(node,out var region)?region.AssociatedSystemId:null;
        public IReadOnlyList<string> Neighbors(string node)=>Connections.Where(e=>e.EndpointA==node||e.EndpointB==node).Select(e=>e.EndpointA==node?e.EndpointB:e.EndpointA).OrderBy(n=>n,StringComparer.Ordinal).ToList().AsReadOnly();
        internal NavigationConnectionDto? Edge(string from,string to)=>edges.Values.FirstOrDefault(e=>(e.EndpointA==from&&e.EndpointB==to)||(e.EndpointB==from&&e.EndpointA==to));
        internal bool KnownNode(GameStateDto state,string actor,string node)=>state.NavigationKnowledge!.Single(k=>k.CivilizationId==actor).Nodes!.Any(k=>k.Id==node&&k.Level!=NavigationKnowledgeLevel.Unknown);
        internal bool KnownEdge(GameStateDto state,string actor,string connection)=>state.NavigationKnowledge!.Single(k=>k.CivilizationId==actor).Connections!.Any(k=>k.Id==connection&&k.Level!=NavigationKnowledgeLevel.Unknown);
        internal IEnumerable<TraversalEffectDto> Effects(GameStateDto state,long? atTurn=null)=>state.TraversalEffects!.Where(e=>e.StartTurn<=(atTurn??state.TurnNumber)&&(atTurn??state.TurnNumber)<=e.EndTurn).OrderBy(e=>e.EffectId,StringComparer.Ordinal);
        internal NavigationCostSnapshot Cost(GameStateDto state,string from,string to,long? atTurn=null)
        {
            var edge=Edge(from,to)??throw new ArgumentException("Nonadjacent navigation route.");
            var natural=PopulationRules.Sum(nodes[from].PhenomenonDefinitionIds!.Concat(nodes[to].PhenomenonDefinitionIds!).Distinct(StringComparer.Ordinal).Select(id=>phenomena[id].TraversalCostIncrease));
            var artificial=PopulationRules.Sum(Effects(state,atTurn).Where(e=>e.ConnectionId==edge.ConnectionId||e.NodeId==from||e.NodeId==to).Select(e=>e.TraversalCostIncrease));
            return new NavigationCostSnapshot(edge.ConnectionId,from,to,edge.Kind,edge.BaseTraversalCost,natural,artificial);
        }
        internal long Budget(GameStateDto state,FleetStateDto fleet,DefinitionCatalogDto definitions,out string limiting,long? atTurn=null)
        {
            var ships=state.Ships!.Where(s=>s.FleetId==fleet.FleetId).OrderBy(s=>definitions.ShipDesigns!.Single(d=>d.DefinitionId==s.ShipDesignDefinitionId).MovementBudget).ThenBy(s=>s.ShipId,StringComparer.Ordinal).ToList();
            limiting=ships[0].ShipId;var basis=definitions.ShipDesigns!.Single(d=>d.DefinitionId==ships[0].ShipDesignDefinitionId).MovementBudget;
            var penalty=PopulationRules.Sum(nodes[fleet.LocationNodeId].PhenomenonDefinitionIds!.Select(id=>phenomena[id].MovementBudgetPenalty)
                .Concat(Effects(state,atTurn).Where(e=>e.NodeId==fleet.LocationNodeId).Select(e=>e.MovementBudgetPenalty)));
            return Math.Max(1,basis-Math.Min(basis,penalty));
        }
        internal int LogisticsFactor(string node)=>nodes[node].PhenomenonDefinitionIds!.Select(id=>phenomena[id].LogisticsFactor).DefaultIfEmpty(10000).Min();
        public StrategicRouteSnapshot FindRoute(GameStateDto state,string? civilization,string origin,string destination)
        {
            StrategicRouteSnapshot Fail(RouteErrorCode code)=>new StrategicRouteSnapshot(code,Array.Empty<string>(),Array.Empty<NavigationCostSnapshot>(),null);
            if(!EntityId.TryParse(origin,out _)||!EntityId.TryParse(destination,out _))return Fail(RouteErrorCode.InvalidSystemId);
            if(!nodes.ContainsKey(origin)||!nodes.ContainsKey(destination)||(civilization!=null&&(!KnownNode(state,civilization,origin)||!KnownNode(state,civilization,destination))))return Fail(RouteErrorCode.UnknownSystem);
            var distance=new Dictionary<string,BigInteger>(StringComparer.Ordinal){{origin,BigInteger.Zero}};
            var previous=new Dictionary<string,string>(StringComparer.Ordinal);var settled=new HashSet<string>(StringComparer.Ordinal);
            while(true)
            {
                var current=distance.Where(p=>!settled.Contains(p.Key)).OrderBy(p=>p.Value).ThenBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key).FirstOrDefault();
                if(current==null)return Fail(RouteErrorCode.Unreachable);
                if(current==destination)
                {
                    if(distance[current]>long.MaxValue)return Fail(RouteErrorCode.CostOverflow);
                    var route=new List<string>();while(current!=origin){route.Add(current);current=previous[current];}route.Reverse();
                    var costs=new List<NavigationCostSnapshot>();current=origin;foreach(var next in route){costs.Add(Cost(state,current,next));current=next;}
                    return new StrategicRouteSnapshot(RouteErrorCode.None,route,costs,(long)distance[destination]);
                }
                settled.Add(current);
                foreach(var next in Neighbors(current))
                {
                    if(settled.Contains(next)||civilization!=null&&(!KnownNode(state,civilization,next)||!KnownEdge(state,civilization,Edge(current,next)!.ConnectionId)))continue;
                    var candidate=distance[current]+Cost(state,current,next).EffectiveCost;
                    if(!distance.TryGetValue(next,out var known)||candidate<known){distance[next]=candidate;previous[next]=current;}
                }
            }
        }
        internal bool ValidateRoute(GameStateDto state,string actor,string origin,IEnumerable<string>? steps,long lockedRemainingCost=0)
        {
            if(steps==null||!nodes.ContainsKey(origin)||!KnownNode(state,actor,origin))return false;
            BigInteger total=0;var first=true;
            foreach(var next in steps)
            {
                var edge=Edge(origin,next);if(edge==null||!KnownNode(state,actor,next)||!KnownEdge(state,actor,edge.ConnectionId))return false;
                total+=first&&lockedRemainingCost>0?lockedRemainingCost:Cost(state,origin,next).EffectiveCost;
                if(total>long.MaxValue)return false;first=false;origin=next;
            }
            return true;
        }
        internal FleetTravelSnapshot Travel(GameStateDto state,FleetStateDto fleet,DefinitionCatalogDto definitions,IReadOnlyList<string>? preview=null)
        {
            var route=preview??fleet.RouteNodeIds!;
            long projectedTurn=(long)state.TurnNumber+(state.Phase==SessionPhase.AwaitingCombat?1:0);
            var initialBudget=Budget(state,fleet,definitions,out var limiting,projectedTurn);
            // A private projection state advances only the known effect clock. No campaign mutation.
            var stationed=new FleetStateDto { FleetId=fleet.FleetId, LocationNodeId=fleet.LocationNodeId };
            long remainingTotal=0,turns=route.Count==0?0:1,left=initialBudget;
            for(var index=0;index<route.Count;index++)
            {
                var cost=index==0&&preview==null&&fleet.TransitTotalCost>0?fleet.TransitTotalCost-fleet.TransitProgress:Cost(state,stationed.LocationNodeId,route[index],projectedTurn).EffectiveCost;
                remainingTotal=checked(remainingTotal+cost);
                while(cost>left)
                {
                    cost-=left;
                    turns=checked(turns+1);
                    projectedTurn=checked(projectedTurn+1);
                    var budget=Budget(state,stationed,definitions,out _,projectedTurn);
                    // Jump whole turns until arrival or the next modifier start/expiry.
                    var boundary=state.TraversalEffects!.SelectMany(e=>new[]{(long)e.StartTurn,(long)e.EndTurn+1})
                        .Where(turn=>turn>projectedTurn).DefaultIfEmpty(long.MaxValue).Min();
                    var skip=Math.Min((cost-1)/budget,boundary-projectedTurn);
                    if(skip>0)
                    {
                        cost-=checked(skip*budget);turns=checked(turns+skip);
                        projectedTurn=checked(projectedTurn+skip);
                        budget=Budget(state,stationed,definitions,out _,projectedTurn);
                    }
                    left=budget;
                }
                left-=cost;stationed.LocationNodeId=route[index];
                if(left==0&&index+1<route.Count)
                {turns=checked(turns+1);projectedTurn=checked(projectedTurn+1);left=Budget(state,stationed,definitions,out _,projectedTurn);}
            }
            return new FleetTravelSnapshot(fleet.FleetId,fleet.LocationNodeId,limiting,initialBudget,fleet.TransitTotalCost,fleet.TransitProgress,remainingTotal,turns);
        }

        public static ValidationReport Validate(DefinitionCatalogDto d)
        {
            var issues=new List<ValidationIssue>();void Check(bool ok,string path){if(!ok)issues.Add(new ValidationIssue(ValidationCode.InvalidFleetRoute,path));}
            if(d.NavigationRegions==null||d.NavigationConnections==null||d.Phenomena==null){issues.Add(new ValidationIssue(ValidationCode.MissingField,"$.Navigation"));return new ValidationReport(issues);}
            var nodes=new HashSet<string>(StringComparer.Ordinal);var phenomenonIds=new HashSet<string>(StringComparer.Ordinal);
            foreach(var p in d.Phenomena){if(p==null){Check(false,"$.Phenomena");continue;}Check(DefinitionId.TryParse(p.DefinitionId,out _)&&phenomenonIds.Add(p.DefinitionId)&&!string.IsNullOrWhiteSpace(p.DisplayName)&&!string.IsNullOrWhiteSpace(p.Description)&&p.TraversalCostIncrease>=0&&p.MovementBudgetPenalty>=0&&p.LogisticsFactor>=0&&p.LogisticsFactor<=10000,"$.Phenomena");}
            foreach(var n in d.NavigationRegions){if(n==null){Check(false,"$.NavigationRegions");continue;}Check(EntityId.TryParse(n.NodeId,out _)&&nodes.Add(n.NodeId)&&!string.IsNullOrWhiteSpace(n.DisplayName)&&n.PhenomenonDefinitionIds!=null&&n.PhenomenonDefinitionIds.All(id=>phenomenonIds.Contains(id))&&n.PhenomenonDefinitionIds.Distinct(StringComparer.Ordinal).Count()==n.PhenomenonDefinitionIds.Count,"$.NavigationRegions");}
            Check(d.NavigationRegions.Where(n=>n!=null&&n.AssociatedSystemId!=null).Select(n=>n.AssociatedSystemId).Distinct(StringComparer.Ordinal).Count()==d.NavigationRegions.Count(n=>n!=null&&n.AssociatedSystemId!=null),"$.NavigationAssociations");
            var pairs=new HashSet<string>(StringComparer.Ordinal);var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var e in d.NavigationConnections){if(e==null){Check(false,"$.NavigationConnections");continue;}var pair=StringComparer.Ordinal.Compare(e.EndpointA,e.EndpointB)<0?e.EndpointA+"|"+e.EndpointB:e.EndpointB+"|"+e.EndpointA;
                Check(EntityId.TryParse(e.ConnectionId,out _)&&ids.Add(e.ConnectionId)&&nodes.Contains(e.EndpointA)&&nodes.Contains(e.EndpointB)&&e.EndpointA!=e.EndpointB&&pairs.Add(pair)&&e.BaseTraversalCost>0&&Enum.IsDefined(typeof(GalaxyEdgeKind),e.Kind),"$.NavigationConnections");}
            return new ValidationReport(issues);
        }
    }
}
