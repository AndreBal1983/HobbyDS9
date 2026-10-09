using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal sealed class SetFleetRouteHandler : ICommandHandler
    {
        private readonly GalaxyGraph graph;
        internal SetFleetRouteHandler(GalaxyGraph graph) { this.graph = graph; }
        public CommandErrorCode Execute(GameStateDto state, CommandEnvelope command)
        {
            var payload = command.SetFleetRoute!;
            var fleet = state.Fleets!.SingleOrDefault(f => f.FleetId == payload.FleetId);
            if (fleet == null) return CommandErrorCode.UnknownFleet;
            if (fleet.OwnerCivilizationId != command.ActorCivilizationId) return CommandErrorCode.FleetNotOwned;
            if (!graph.ValidateRoute(new EntityId(fleet.LocationSystemId), payload.RouteSystemIds!.Select(id => new EntityId(id))).Success) return CommandErrorCode.InvalidRoute;
            fleet.RouteSystemIds = new List<string>(payload.RouteSystemIds);
            return CommandErrorCode.None;
        }
    }
    internal static class MovementPhase
    {
        internal static void Execute(GameStateDto state)
        {
            // Freeze membership at phase entry; no second movement from later effects.
            var fleetIds = state.Fleets!.Select(f => f.FleetId).OrderBy(id => id, StringComparer.Ordinal).ToList();
            var fleets = state.Fleets.ToDictionary(f => f.FleetId, StringComparer.Ordinal);
            foreach (var id in fleetIds)
            {
                var fleet = fleets[id];
                if (fleet.RouteSystemIds!.Count == 0) continue;
                fleet.LocationSystemId = fleet.RouteSystemIds[0]; fleet.RouteSystemIds.RemoveAt(0);
            }
        }
    }
}
