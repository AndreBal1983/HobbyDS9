using System.Linq;
using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static class PopulationGuards
    {
        internal static bool Same(GameStateDto before, GameStateDto after, bool economy, bool production)
        {
            if (after.NextEntitySequence < before.NextEntitySequence) return false;
            var mutable = new HashSet<string>(StringComparer.Ordinal);
            foreach (var state in new[] { before, after })
            {
                if (economy) mutable.UnionWith(state.Colonies!.SelectMany(c => c.Cohorts!).Select(c => c.CohortId).Concat(state.Displacements!.Select(r => r.DisplacementId)));
                if (production) mutable.UnionWith(state.Colonies!.SelectMany(c => c.ProductionQueue!).Select(q => q.QueueItemId).Concat(state.Ships!.Select(s => s.ShipId)).Concat(state.Fleets!.Select(f => f.FleetId)));
            }
            if (!before.EntityIdentities.Where(e => !mutable.Contains(e.EntityId)).Select(e => (e.EntityId, e.DefinitionId))
                .SequenceEqual(after.EntityIdentities.Where(e => !mutable.Contains(e.EntityId)).Select(e => (e.EntityId, e.DefinitionId)))) return false;
            if (before.Colonies!.Count != after.Colonies!.Count || before.Planets!.Count != after.Planets!.Count) return false;
            for (var i = 0; i < before.Colonies.Count; i++)
            {
                var a = PopulationData.Copy(before.Colonies[i]); var b = PopulationData.Copy(after.Colonies[i]);
                // Queues have their own phase guard; colony identities/capabilities never change in economy.
                a.ProductionQueue!.Clear(); b.ProductionQueue!.Clear();
                if (economy) { b.Cohorts = a.Cohorts; b.Stockpiles = a.Stockpiles; b.Stability = a.Stability; b.Food = a.Food; b.FoodShortageTurns = a.FoodShortageTurns; }
                if (!PopulationData.Same(a, b)) return false;
            }
            for (var i = 0; i < before.Planets.Count; i++)
            {
                var a = PopulationData.Copy(before.Planets[i]); var b = PopulationData.Copy(after.Planets[i]);
                if (economy) b.RemainingDeposits = a.RemainingDeposits;
                if (!PopulationData.Same(a, b)) return false;
            }
            return economy ? production || before.PopulationStatistics!.ProductionCompleted == after.PopulationStatistics!.ProductionCompleted && before.PopulationStatistics.ResourceConsumed == after.PopulationStatistics.ResourceConsumed : (before.Displacements!.Count == after.Displacements!.Count && before.Displacements.Zip(after.Displacements, (a, b) => PopulationData.Same(a, b)).All(v => v)
                && before.LastDeliveries!.Count == after.LastDeliveries!.Count && before.LastDeliveries.Zip(after.LastDeliveries, (a, b) => PopulationData.Same(a, b)).All(v => v)
                && SameCounters(before.PopulationStatistics!, after.PopulationStatistics!, production));
        }
        // Production owns completion/consumption counters; all other counters belong to economy.
        private static bool SameCounters(PopulationStatisticsDto a, PopulationStatisticsDto b, bool production)
        {
            var copy = PopulationData.Copy(b); if (production) { copy.ProductionCompleted = a.ProductionCompleted; copy.ResourceConsumed = a.ResourceConsumed; }
            return PopulationData.Same(a, copy);
        }
    }
}
