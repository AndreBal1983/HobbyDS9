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
            var profiles = definitions.EconomicProfiles!.ToDictionary(p => p.DefinitionId, StringComparer.Ordinal);
            var designs = definitions.ShipDesigns!.ToDictionary(d => d.DefinitionId, StringComparer.Ordinal);
            foreach (var colony in state.Colonies!.OrderBy(c => c.ColonyId, StringComparer.Ordinal))
            {
                if (colony.ProductionQueue!.Count == 0) continue;
                var head = colony.ProductionQueue[0]; var profile = profiles[colony.EconomicProfileDefinitionId];
                var industry = checked(profile.BaseIndustry + colony.Population / profile.PopulationPerIndustry);
                var remaining = head.RequiredIndustry - head.CompletedIndustry;
                if (industry < remaining) { head.CompletedIndustry += industry; continue; }
                var design = designs[head.ShipDesignDefinitionId];
                // Ship first, then fleet (only if none stationary); one global sequence.
                var shipId = CampaignAllocation.Allocate(state, "ship", design.DefinitionId);
                var fleet = state.Fleets!.Where(f => f.OwnerCivilizationId == colony.OwnerCivilizationId && f.LocationSystemId == colony.SystemId && f.RouteSystemIds!.Count == 0)
                    .OrderBy(f => f.FleetId, StringComparer.Ordinal).FirstOrDefault();
                if (fleet == null)
                {
                    fleet = new FleetStateDto { FleetId = CampaignAllocation.Allocate(state, "fleet", null), OwnerCivilizationId = colony.OwnerCivilizationId!,
                        LocationSystemId = colony.SystemId, ShipIds = new List<string>(), RouteSystemIds = new List<string>() };
                    state.Fleets!.Add(fleet);
                }
                fleet.ShipIds!.Add(shipId);
                state.Ships!.Add(new ShipStateDto { ShipId = shipId, ShipDesignDefinitionId = design.DefinitionId,
                    OwnerCivilizationId = colony.OwnerCivilizationId!, FleetId = fleet.FleetId, CurrentHull = design.MaxHull });
                colony.ProductionQueue.RemoveAt(0); state.EntityIdentities.RemoveAll(e => e.EntityId == head.QueueItemId);
            }
        }
    }
}
