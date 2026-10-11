using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    public sealed partial class SimulationSession
    {
        public StrategicRouteSnapshot FindNavigationRoute(string civilization,string origin,string destination)
        {lock(gate)return Navigation.FindRoute(state,civilization,origin,destination);}
        public FleetTravelSnapshot CaptureFleetTravel(string fleetId,System.Collections.Generic.IReadOnlyList<string>? preview=null)
        {lock(gate)return Navigation.Travel(state,state.Fleets!.Single(f=>f.FleetId==fleetId),definitions,preview);}
        public NavigationViewSnapshot CaptureNavigation(string civilization)
        {
            lock(gate)
            {
                var nodes=Navigation.Regions.Where(n=>Navigation.KnownNode(state,civilization,n.NodeId)).ToList();
                var edges=Navigation.Connections.Where(e=>Navigation.KnownEdge(state,civilization,e.ConnectionId)&&nodes.Any(n=>n.NodeId==e.EndpointA)&&nodes.Any(n=>n.NodeId==e.EndpointB))
                    .Select(e=>Navigation.Cost(state,e.EndpointA,e.EndpointB));
                return new NavigationViewSnapshot(nodes,edges,definitions.Phenomena!.Select(p=>new PhenomenonDefinitionSnapshot(p)));
            }
        }
    }
}
