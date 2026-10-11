using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static class PopulationMigration
    {
        // Exact capped largest-remainder allocation; ordinal IDs settle ties.
        internal static Dictionary<string, long> Allocate(long total, IEnumerable<(string id, long weight, long capacity)> values)
        {
            var rows = values.Where(v => v.weight > 0 && v.capacity > 0).OrderBy(v => v.id, StringComparer.Ordinal).ToList();
            var result = rows.ToDictionary(v => v.id, v => 0L, StringComparer.Ordinal);
            while (total > 0 && rows.Count > 0)
            {
                var sum = rows.Aggregate(BigInteger.Zero, (a, v) => a + v.weight); var shares = rows.Select(v => (row: v, amount: (long)((BigInteger)total * v.weight / sum), remainder: (BigInteger)total * v.weight % sum)).ToList();
                long used = 0;
                foreach (var share in shares) { var n = Math.Min(share.amount, share.row.capacity - result[share.row.id]); result[share.row.id] += n; used += n; }
                total -= used;
                foreach (var share in shares.OrderByDescending(x => x.remainder).ThenBy(x => x.row.id, StringComparer.Ordinal))
                {
                    if (total == 0) break;
                    if (result[share.row.id] < share.row.capacity) { result[share.row.id]++; total--; used++; }
                }
                rows = rows.Where(v => result[v.id] < v.capacity).ToList();
                if (used == 0) break;
            }
            return result;
        }
        private static long Fit(PopulationCohortDto cohort, ColonyStateDto colony, GameStateDto s, DefinitionCatalogDto d)
        {
            var planet = WorldEconomy.Planet(colony, s, d); var species = d.Species!.Single(x => x.DefinitionId == cohort.SpeciesDefinitionId);
            return (long)(planet.Habitats!.Aggregate(BigInteger.Zero, (a, h) => a + (BigInteger)h.ShareBps * WorldEconomy.Suitability(species, planet, h.Kind)) / 10000);
        }
        private static long Opportunity(ColonyStateDto c, WorldEconomySnapshot f) => Math.Min(10000, PopulationRules.ScaleBy(Math.Min(100, Math.Min(100, f.Industry) + Math.Min(100, f.ResearchOutput)), 100, 1)) + c.Prosperity;
        private static bool Habitable(PopulationCohortDto cohort, ColonyStateDto destination, GameStateDto state, DefinitionCatalogDto definitions) =>
            Fit(cohort, destination, state, definitions) > 0 || WorldEconomy.Installed(destination, definitions, i => i.ArtificialCapacity) > 0;
        internal static void Execute(GameStateDto s, DefinitionCatalogDto d)
        {
            var graph = PopulationLogistics.Graph(s, d); var colonies = s.Colonies!.OrderBy(c => c.ColonyId, StringComparer.Ordinal).ToList();
            var facts = colonies.ToDictionary(c => c.ColonyId, c => WorldEconomy.Project(c, s, d));
            var capacity = colonies.ToDictionary(c => c.ColonyId, c => Math.Max(0, facts[c.ColonyId].EffectiveCapacity - facts[c.ColonyId].Population));
            var identities = colonies.ToDictionary(c => c.ColonyId, c => c.Cohorts!.Select(PopulationRules.Identity).ToHashSet(StringComparer.Ordinal));
            bool HasIdentitySlot(ColonyStateDto to, PopulationCohortDto cohort, LegalStatus status) => identities[to.ColonyId].Contains(cohort.SpeciesDefinitionId + "|" + (int)status + "|" + cohort.OriginCivilizationId) || identities[to.ColonyId].Count < PopulationRules.CohortCeiling;
            var moves = new List<(ColonyStateDto from, ColonyStateDto to, PopulationCohortDto cohort, long amount, bool refugee, DisplacementDto? displacement)>();
            var reserved = s.Displacements!.ToDictionary(r => r.CohortId, r => r.Population, StringComparer.Ordinal);
            foreach (var source in colonies.Where(c => c.OwnerCivilizationId != null))
                foreach (var cohort in source.Cohorts!.OrderBy(c => c.CohortId, StringComparer.Ordinal))
                {
                    var protectedCount = reserved.TryGetValue(cohort.CohortId, out var count) ? count : 0;
                    var available = cohort.Population - protectedCount;
                    var volume = PopulationRules.ScaleBy(available, PopulationRules.MigrationBps);
                    if (volume == 0) continue;
                    var from = facts[source.ColonyId];
                    var sourceScore = Opportunity(source, from) + source.Stability + Fit(cohort, source, s, d) + cohort.Satisfaction
                        - WorldEconomy.LegalPenalty(cohort.LegalStatus) - (from.Crowding > 0 ? 5000 : 0);
                    var choices = new List<(string id, long weight, long capacity)>();
                    foreach (var destination in colonies.Where(c => c != source && c.OwnerCivilizationId == source.OwnerCivilizationId))
                    {
                        if (!Habitable(cohort, destination, s, d) || !HasIdentitySlot(destination, cohort, cohort.LegalStatus) || capacity[destination.ColonyId] == 0) continue;
                        var route = PopulationLogistics.Path(s, graph, source.SystemId, destination.SystemId, source.OwnerCivilizationId);
                        if (route.integrity == 0 || facts[destination.ColonyId].LogisticsCapacity == 0 || from.LogisticsCapacity == 0) continue;
                        var diaspora = destination.Cohorts!.Any(c => c.SpeciesDefinitionId == cohort.SpeciesDefinitionId) ? 1000 : 0;
                        var destScore = Opportunity(destination, facts[destination.ColonyId]) + destination.Stability + Fit(cohort, destination, s, d) + 7000 + diaspora;
                        var pull = Math.Max(0, destScore - sourceScore);
                        if (pull > 0) choices.Add((destination.ColonyId, PopulationRules.ScaleBy(pull, route.integrity) / (route.cost==long.MaxValue?long.MaxValue:route.cost+1), capacity[destination.ColonyId]));
                    }
                    foreach (var allocation in Allocate(volume, choices).Where(a => a.Value > 0).OrderBy(a => a.Key, StringComparer.Ordinal))
                    { var to = colonies.Single(c => c.ColonyId == allocation.Key); identities[to.ColonyId].Add(PopulationRules.Identity(cohort)); moves.Add((source, to, cohort, allocation.Value, false, null)); capacity[to.ColonyId] -= allocation.Value; }
                }
            // Refugees deliberately use a separate policy and keep their status/history.
            foreach (var request in s.Displacements.OrderBy(r => r.DisplacementId, StringComparer.Ordinal))
            {
                var source = colonies.Single(c => c.PlanetId == request.SourcePlanetId); var cohort = source.Cohorts!.Single(c => c.CohortId == request.CohortId);
                var choices = new List<(string id, long weight, long capacity)>();
                foreach (var to in colonies.Where(c => c != source && facts[c.ColonyId].Politics.AcceptsRefugees && c.OwnerCivilizationId != null))
                {
                    if (!Habitable(cohort, to, s, d) || facts[source.ColonyId].LogisticsCapacity == 0 || capacity[to.ColonyId] == 0 || !HasIdentitySlot(to, cohort, LegalStatus.Refugee) || facts[to.ColonyId].LogisticsCapacity == 0) continue;
                    var route = PopulationLogistics.Path(s, graph, source.SystemId, to.SystemId, to.OwnerCivilizationId); if (route.integrity == 0) continue;
                    var diaspora = to.Cohorts!.Any(c => c.SpeciesDefinitionId == cohort.SpeciesDefinitionId) ? 1L : 0;
                    // Radix weights preserve the declared priority ordering.
                    var weight = checked((to.Stability / 100 + 1L) * 1000000000000L + (route.integrity / 100 / ((route.cost==long.MaxValue?long.MaxValue:route.cost+1))) * 100000000L + diaspora * 10000000L + Fit(cohort, to, s, d) * 100 + Math.Min(99999, Opportunity(to, facts[to.ColonyId])));
                    choices.Add((to.ColonyId, weight, capacity[to.ColonyId]));
                }
                foreach (var a in Allocate(request.Population, choices).Where(a => a.Value > 0).OrderBy(a => a.Key, StringComparer.Ordinal))
                { var to = colonies.Single(c => c.ColonyId == a.Key); identities[to.ColonyId].Add(cohort.SpeciesDefinitionId + "|" + (int)LegalStatus.Refugee + "|" + cohort.OriginCivilizationId); moves.Add((source, to, cohort, a.Value, true, request)); capacity[to.ColonyId] -= a.Value; request.Population -= a.Value; }
            }
            foreach (var move in moves)
            {
                move.cohort.Population -= move.amount;
                var arrival = PopulationData.Copy(move.cohort); arrival.CohortId = CampaignAllocation.Allocate(s, "cohort", arrival.SpeciesDefinitionId); arrival.Population = move.amount; arrival.LegalStatusEligibleTurns=0;
                if (move.refugee)
                {
                    arrival.LegalStatus = LegalStatus.Refugee;
                    foreach (var memory in new[] {
                        new PopulationMemoryDto { Kind=move.displacement!.Cause, SourceEntityId=move.displacement.ResponsibleEntityId, Magnitude=2000, StartTurn=s.TurnNumber, ExpiryTurn=checked(s.TurnNumber+40), DecayPerTurn=50 },
                        new PopulationMemoryDto { Kind="displaced-from", SourceEntityId=move.from.PlanetId, Magnitude=1000, StartTurn=s.TurnNumber, ExpiryTurn=checked(s.TurnNumber+40), DecayPerTurn=25 } })
                    {
                        arrival.Memories!.RemoveAll(m => m.Kind == memory.Kind && m.SourceEntityId == memory.SourceEntityId);
                        arrival.Memories.Add(memory);
                    }
                    arrival.Memories!.RemoveAll(m => m.Kind == "accepted-as-refugees" && m.SourceEntityId == move.to.OwnerCivilizationId);
                    arrival.Memories.Add(new PopulationMemoryDto { Kind = "accepted-as-refugees", Magnitude = 1000, SourceEntityId = move.to.OwnerCivilizationId, StartTurn = s.TurnNumber, ExpiryTurn = checked(s.TurnNumber + 20), DecayPerTurn = 50 });
                    s.PopulationStatistics!.RefugeesMoved = checked(s.PopulationStatistics!.RefugeesMoved + move.amount);
                }
                else s.PopulationStatistics!.Migrated = checked(s.PopulationStatistics!.Migrated + move.amount);
                move.to.Cohorts!.Add(arrival);
            }
            foreach (var done in s.Displacements.Where(r => r.Population == 0).ToList()) { s.Displacements!.Remove(done); s.EntityIdentities.RemoveAll(e => e.EntityId == done.DisplacementId); }
            foreach (var colony in colonies) CohortCompaction.Execute(colony, s);
        }
    }
}
