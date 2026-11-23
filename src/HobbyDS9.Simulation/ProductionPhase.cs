using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class ProductionPhase
    {
        internal static void Execute(GameStateDto state, DefinitionCatalogDto definitions)
        {
            foreach (var colony in state.Colonies!.OrderBy(c => c.ColonyId, StringComparer.Ordinal))
            {
                if (colony.ProductionQueue!.Count == 0) continue;
                var head = colony.ProductionQueue[0];
                var industry = WorldEconomy.Project(colony, state, definitions).Industry;
                var remaining = head.RequiredIndustry - head.CompletedIndustry;
                if (industry < remaining) { head.CompletedIndustry += industry; continue; }
                var design = CampaignShipDesignResolver.Queue(state, definitions, head);
                Spawn(state,definitions,colony,head.ShipDesignDefinitionId,head.Blueprint,design);
                state.PopulationStatistics!.ResourceConsumed = checked(state.PopulationStatistics.ResourceConsumed + PopulationRules.Sum(head.PaidResources!.Select(r => r.Amount)));
                state.PopulationStatistics!.ProductionCompleted = checked(state.PopulationStatistics.ProductionCompleted + 1);
                colony.ProductionQueue.RemoveAt(0); state.EntityIdentities.RemoveAll(e => e.EntityId == head.QueueItemId);
            }
        }
        // Shared physical effect only: no queue admission, payment, material accounting or completion receipt.
        internal static ShipStateDto Spawn(GameStateDto state,DefinitionCatalogDto definitions,ColonyStateDto colony,string staticId,BlueprintReferenceDto? blueprint,ShipDesignDefinitionDto design)
        {
                // Ship first, then fleet (only if none stationary); one global sequence.
                var shipId = CampaignAllocation.Allocate(state, "ship", blueprint == null ? design.DefinitionId : null);
                var location=new WeightedNavigationGraph(definitions).NodeForSystem(colony.SystemId)!;
                var fleet = state.Fleets!.Where(f => f.OwnerCivilizationId == colony.OwnerCivilizationId && f.LocationNodeId == location && f.TransitTotalCost==0 && f.RouteNodeIds!.Count == 0)
                    .OrderBy(f => f.FleetId, StringComparer.Ordinal).FirstOrDefault();
                if (fleet == null)
                {
                    fleet = new FleetStateDto
                    {
                        FleetId = CampaignAllocation.Allocate(state, "fleet", null),
                        OwnerCivilizationId = colony.OwnerCivilizationId!,
                        LocationNodeId = location,
                        ShipIds = new List<string>(),
                        RouteNodeIds = new List<string>()
                    };
                    state.Fleets!.Add(fleet);
                }
                fleet.ShipIds!.Add(shipId);
                var ship=new ShipStateDto
                {
                    ShipId = shipId,
                    ShipDesignDefinitionId = staticId,
                    Blueprint = blueprint == null ? null : BlueprintCopies.Copy(blueprint),
                    OwnerCivilizationId = colony.OwnerCivilizationId!,
                    FleetId = fleet.FleetId,
                    CurrentHull = design.MaxHull
                };
                state.Ships!.Add(ship);return ship;
        }
    }
}
