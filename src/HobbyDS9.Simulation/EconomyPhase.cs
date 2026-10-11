using System;
using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static class EconomyPhase
    {
        internal static void Execute(GameStateDto s, DefinitionCatalogDto d)
        {
            foreach (var c in s.Colonies!.OrderBy(c => c.ColonyId, StringComparer.Ordinal))
            {
                c.Food = checked(c.Food + WorldEconomy.Project(c, s, d).FoodOutput);
                var planet = s.Planets!.Single(p => p.PlanetId == c.PlanetId);
                foreach (var installed in WorldEconomy.Infrastructure(c, d))
                    foreach (var rate in installed.definition.Extraction!.OrderBy(r => r.ResourceDefinitionId, StringComparer.Ordinal))
                    {
                        var amount = Math.Min(PopulationRules.Stock(planet.RemainingDeposits!, rate.ResourceDefinitionId), checked(rate.Amount * installed.count));
                        PopulationRules.AddStock(planet.RemainingDeposits!, rate.ResourceDefinitionId, -amount); PopulationRules.AddStock(c.Stockpiles!, rate.ResourceDefinitionId, amount);
                        s.PopulationStatistics!.ResourceExtracted = checked(s.PopulationStatistics!.ResourceExtracted + amount);
                    }
            }
            PopulationLogistics.Execute(s, d);
            foreach (var c in s.Colonies.OrderBy(c => c.ColonyId, StringComparer.Ordinal))
            {
                var facts = WorldEconomy.Project(c, s, d); var sufficient = c.Food >= facts.FoodDemand;
                c.Food -= Math.Min(c.Food, facts.FoodDemand); c.FoodShortageTurns = sufficient ? 0 : checked(c.FoodShortageTurns + 1);
                foreach (var cohort in c.Cohorts!.OrderBy(p => p.CohortId, StringComparer.Ordinal))
                {
                    foreach (var memory in cohort.Memories!) memory.Magnitude = Math.Max(0, memory.Magnitude - memory.DecayPerTurn);
                    cohort.Memories.RemoveAll(m => m.ExpiryTurn <= s.TurnNumber || m.Magnitude == 0);
                    if (!sufficient && c.FoodShortageTurns >= PopulationRules.ShortageGrace)
                    {
                        var protectedCount = s.Displacements!.SingleOrDefault(r => r.CohortId == cohort.CohortId)?.Population ?? 0;
                        var lost = Math.Min(cohort.Population - protectedCount, Math.Max(1, cohort.Population / 100));
                        cohort.Population -= lost; s.PopulationStatistics!.Deaths = checked(s.PopulationStatistics!.Deaths + lost);
                    }
                }
                if (sufficient)
                {
                    var free = Math.Max(0, facts.EffectiveCapacity - facts.Population);
                    var births = Math.Min(free, PopulationRules.ScaleBy(facts.Population, PopulationRules.GrowthBps));
                    var distribution = PopulationMigration.Allocate(births, c.Cohorts!.Select(p => (p.CohortId, p.Population, births)));
                    foreach (var cohort in c.Cohorts!) if (distribution.TryGetValue(cohort.CohortId, out var count)) cohort.Population = checked(cohort.Population + count);
                    s.PopulationStatistics!.Births = checked(s.PopulationStatistics!.Births + births);
                }
                PoliticsRules.ExecuteWorld(c,s,d,sufficient);
                var target = WorldEconomy.Project(c, s, d).TargetStability;
                c.Stability += Math.Max(-PopulationRules.StabilityStep, Math.Min(PopulationRules.StabilityStep, target - c.Stability));
                if (c.OwnerCivilizationId != null)
                { var owner = s.Civilizations!.Single(v => v.CivilizationId == c.OwnerCivilizationId); owner.Credits = checked(owner.Credits + facts.CreditOutput); }
            }
            PopulationMigration.Execute(s, d);
        }
    }
}
