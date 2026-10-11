using System;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class ProductionFleetGuards
    {
        internal static void Require(GameStateDto before, GameStateDto after, bool production, bool movement, bool economy = false)
        {
            void Check(bool condition) { if (!condition) throw new InvalidOperationException("Phase changed protected production/fleet facts."); }
            foreach (var colony in before.Colonies!)
            {
                var queue = colony.ProductionQueue!;
                var next = after.Colonies!.Single(c => c.ColonyId == colony.ColonyId).ProductionQueue!;
                if (!production) Check(queue.Select(Key).SequenceEqual(next.Select(Key)));
                else
                {
                    Check(next.Count == queue.Count || (queue.Count > 0 && next.Count == queue.Count - 1));
                    var offset = queue.Count - next.Count;
                    for (var i = 0; i < next.Count; i++)
                    {
                        var old = queue[i + offset]; var current = next[i];
                        Check((old.QueueItemId, old.ShipDesignDefinitionId, old.RequiredIndustry, old.PaidCredits, ResourceKey(old))
                            == (current.QueueItemId, current.ShipDesignDefinitionId, current.RequiredIndustry, current.PaidCredits, ResourceKey(current)));
                        Check(i + offset == 0 ? current.CompletedIndustry >= old.CompletedIndustry : current.CompletedIndustry == old.CompletedIndustry);
                    }
                }
            }
            Check(production ? before.Ships!.All(s => after.Ships!.Any(n => ShipKey(s) == ShipKey(n)))
                : before.Ships!.Select(ShipKey).SequenceEqual(after.Ships!.Select(ShipKey)));
            Check(production ? before.Fleets!.All(f => after.Fleets!.Any(n => n.FleetId == f.FleetId))
                : before.Fleets!.Select(f => f.FleetId).SequenceEqual(after.Fleets!.Select(f => f.FleetId)));
            foreach (var fleet in before.Fleets!)
            {
                var next = after.Fleets!.Single(f => f.FleetId == fleet.FleetId);
                Check(fleet.OwnerCivilizationId == next.OwnerCivilizationId);
                Check(production ? next.ShipIds!.Take(fleet.ShipIds!.Count).SequenceEqual(fleet.ShipIds) : fleet.ShipIds!.SequenceEqual(next.ShipIds!));
                if(!movement) Check(fleet.LocationNodeId==next.LocationNodeId&&fleet.RouteNodeIds!.SequenceEqual(next.RouteNodeIds!)&&fleet.TransitTotalCost==next.TransitTotalCost&&fleet.TransitProgress==next.TransitProgress);
                else
                {
                    var consumed=fleet.RouteNodeIds!.Count-next.RouteNodeIds!.Count;
                    Check(consumed>=0&&fleet.RouteNodeIds.Skip(consumed).SequenceEqual(next.RouteNodeIds));
                    Check(next.LocationNodeId==(consumed==0?fleet.LocationNodeId:fleet.RouteNodeIds[consumed-1]));
                    Check(consumed!=0||next.TransitProgress>=fleet.TransitProgress);
                }
            }
            if (production)
            {
                var queueIds = before.Colonies!.SelectMany(c => c.ProductionQueue!).Select(q => q.QueueItemId).ToHashSet(StringComparer.Ordinal);
                if (economy) queueIds.UnionWith(before.Colonies!.SelectMany(c => c.Cohorts!).Select(c => c.CohortId).Concat(before.Displacements!.Select(r => r.DisplacementId)));
                Check(before.EntityIdentities.Where(e => !queueIds.Contains(e.EntityId)).All(e => after.EntityIdentities.Any(n => (n.EntityId, n.DefinitionId) == (e.EntityId, e.DefinitionId))));
            }
        }
        private static string ResourceKey(ProductionQueueItemDto q) => string.Join(";", q.PaidResources!.OrderBy(r => r.ResourceDefinitionId, StringComparer.Ordinal).Select(r => r.ResourceDefinitionId + ":" + r.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        private static (string, string, long, long, string, long) Key(ProductionQueueItemDto q) =>
            (q.QueueItemId, q.ShipDesignDefinitionId, q.RequiredIndustry, q.CompletedIndustry, ResourceKey(q), q.PaidCredits);
        private static (string, string, string, string, long) ShipKey(ShipStateDto s) =>
            (s.ShipId, s.ShipDesignDefinitionId, s.OwnerCivilizationId, s.FleetId, s.CurrentHull);
    }
}
