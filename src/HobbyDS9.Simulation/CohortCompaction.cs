using System;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static class CohortCompaction
    {
        internal static void Execute(ColonyStateDto colony, GameStateDto state)
        {
            var protectedIds = state.Displacements!.Select(r => r.CohortId).ToHashSet(StringComparer.Ordinal);
            foreach (var dead in colony.Cohorts!.Where(c => c.Population == 0 && !protectedIds.Contains(c.CohortId)).ToList())
            { colony.Cohorts!.Remove(dead); state.EntityIdentities.RemoveAll(e => e.EntityId == dead.CohortId); }
            while (true)
            {
                var rows = colony.Cohorts!.OrderBy(c => c.CohortId, StringComparer.Ordinal).ToList();
                var pairs = rows.SelectMany((a, i) => rows.Skip(i + 1).Where(b => PopulationRules.Identity(a) == PopulationRules.Identity(b) && !(protectedIds.Contains(a.CohortId) && protectedIds.Contains(b.CohortId)))
                    .Select(b => (a, b, distance: Math.Abs(a.Satisfaction - b.Satisfaction) + Math.Abs(a.Integration - b.Integration) + Math.Abs(a.CivicAlignment - b.CivicAlignment) + Math.Abs(a.Radicalisation - b.Radicalisation))));
                var pair = pairs.Where(p => colony.Cohorts!.Count > PopulationRules.CohortCeiling || (p.distance <= PopulationRules.MergeTolerance && p.a.ExternalAffinities!.OrderBy(a => a.CivilizationId, StringComparer.Ordinal).Select(a => (a.CivilizationId, a.Rating)).SequenceEqual(p.b.ExternalAffinities!.OrderBy(a => a.CivilizationId, StringComparer.Ordinal).Select(a => (a.CivilizationId, a.Rating)))))
                    .OrderBy(p => p.distance).ThenBy(p => p.a.CohortId, StringComparer.Ordinal).ThenBy(p => p.b.CohortId, StringComparer.Ordinal).FirstOrDefault();
                if (pair.a == null) break;
                var a = protectedIds.Contains(pair.b.CohortId) ? pair.b : pair.a; var b = a == pair.a ? pair.b : pair.a; var total = checked(a.Population + b.Population);
                int Weight(int x, int y) => total == 0 ? 0 : (int)(((BigInteger)a.Population * x + (BigInteger)b.Population * y) / total);
                a.Satisfaction = Weight(a.Satisfaction, b.Satisfaction); a.Integration = Weight(a.Integration, b.Integration); a.CivicAlignment = Weight(a.CivicAlignment, b.CivicAlignment); a.Radicalisation = Weight(a.Radicalisation, b.Radicalisation);
                a.ExternalAffinities = a.ExternalAffinities!.Concat(b.ExternalAffinities!).Select(v => v.CivilizationId).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal)
                    .Select(id => new ExternalAffinityDto { CivilizationId = id, Rating = Weight(a.ExternalAffinities.SingleOrDefault(v => v.CivilizationId == id)?.Rating ?? 0, b.ExternalAffinities!.SingleOrDefault(v => v.CivilizationId == id)?.Rating ?? 0) }).ToList();
                a.Memories = a.Memories!.Concat(b.Memories!).GroupBy(m => m.Kind + "|" + m.SourceEntityId, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g =>
                {
                    var first = g.First(); return new PopulationMemoryDto
                    {
                        Kind = first.Kind,
                        SourceEntityId = first.SourceEntityId,
                        Magnitude = Weight(a.Memories!.SingleOrDefault(m => m.Kind == first.Kind && m.SourceEntityId == first.SourceEntityId)?.Magnitude ?? 0, b.Memories!.SingleOrDefault(m => m.Kind == first.Kind && m.SourceEntityId == first.SourceEntityId)?.Magnitude ?? 0),
                        StartTurn = g.Min(m => m.StartTurn),
                        ExpiryTurn = g.Max(m => m.ExpiryTurn),
                        DecayPerTurn = g.Min(m => m.DecayPerTurn)
                    };
                }).ToList();
                a.Population = total; colony.Cohorts!.Remove(b); state.EntityIdentities.RemoveAll(e => e.EntityId == b.CohortId);
            }
            colony.Cohorts!.Sort((a, b) => StringComparer.Ordinal.Compare(a.CohortId, b.CohortId));
            if (colony.Cohorts!.Count > PopulationRules.CohortCeiling) throw new InvalidOperationException("Cohort ceiling cannot be satisfied without discarding distinct histories.");
        }
    }
}
