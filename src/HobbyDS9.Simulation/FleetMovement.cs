using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal sealed class SetFleetRouteHandler : ICommandHandler
    {
        private readonly WeightedNavigationGraph graph;
        internal SetFleetRouteHandler(WeightedNavigationGraph graph){this.graph=graph;}
        public CommandErrorCode Execute(GameStateDto state,CommandEnvelope command)
        {
            var payload=command.SetFleetRoute!;var fleet=state.Fleets!.SingleOrDefault(f=>f.FleetId==payload.FleetId);
            if(fleet==null)return CommandErrorCode.UnknownFleet;
            if(fleet.OwnerCivilizationId!=command.ActorCivilizationId)return CommandErrorCode.FleetNotOwned;
            if(fleet.TransitTotalCost>0)return CommandErrorCode.FleetInTransit;
            if(!graph.ValidateRoute(state,fleet.OwnerCivilizationId,fleet.LocationNodeId,payload.RouteNodeIds))return CommandErrorCode.InvalidRoute;
            fleet.RouteNodeIds=new List<string>(payload.RouteNodeIds!);return CommandErrorCode.None;
        }
    }
    internal static class MovementPhase
    {
        internal static void Execute(GameStateDto state,DefinitionCatalogDto definitions)
        {
            var graph=new WeightedNavigationGraph(definitions);
            var fleets=state.Fleets!.OrderBy(f=>f.FleetId,StringComparer.Ordinal).ToList();
            var budgets=fleets.ToDictionary(f=>f.FleetId,f=>graph.Budget(state,f,definitions,out _),StringComparer.Ordinal);
            var blocked=new HashSet<string>(StringComparer.Ordinal);
            void FreezeEncounters()
            {
                foreach(var group in fleets.Where(f=>f.TransitTotalCost==0).GroupBy(f=>f.LocationNodeId,StringComparer.Ordinal))
                    if(group.Select(f=>f.OwnerCivilizationId).Distinct(StringComparer.Ordinal).Count()>1)blocked.Add(group.Key);
                foreach(var fleet in fleets.Where(f=>f.TransitTotalCost==0&&blocked.Contains(f.LocationNodeId)))budgets[fleet.FleetId]=0;
            }
            FreezeEncounters();
            while(true)
            {
                var moving=fleets.Where(f=>budgets[f.FleetId]>0&&f.RouteNodeIds!.Count>0).ToList();if(moving.Count==0)break;
                foreach(var fleet in moving)if(fleet.TransitTotalCost==0)fleet.TransitTotalCost=graph.Cost(state,fleet.LocationNodeId,fleet.RouteNodeIds![0]).EffectiveCost;
                // Jump to the next arrival/budget exhaustion; never loop over huge integer costs.
                var step=moving.Min(f=>Math.Min(budgets[f.FleetId],f.TransitTotalCost-f.TransitProgress));
                foreach(var fleet in moving)
                {
                    fleet.TransitProgress=checked(fleet.TransitProgress+step);budgets[fleet.FleetId]-=step;
                    state.NavigationStatistics!.TraversalCostPaid=checked(state.NavigationStatistics.TraversalCostPaid+step);
                    if(fleet.TransitProgress==fleet.TransitTotalCost)
                    {
                        fleet.LocationNodeId=fleet.RouteNodeIds![0];fleet.RouteNodeIds.RemoveAt(0);fleet.TransitTotalCost=0;fleet.TransitProgress=0;
                        state.NavigationStatistics.EdgesCompleted=checked(state.NavigationStatistics.EdgesCompleted+1);
                    }
                }
                FreezeEncounters();
            }
            state.NavigationStatistics!.PartialTraversalTurns=checked(state.NavigationStatistics.PartialTraversalTurns+fleets.Count(f=>f.TransitTotalCost>0));
        }
    }
}
