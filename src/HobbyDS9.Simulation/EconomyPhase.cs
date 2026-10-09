using System;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class EconomyPhase
    {
        internal static void Execute(GameStateDto staging, DefinitionCatalogDto definitions)
        {
            var profiles = definitions.EconomicProfiles!.ToDictionary(p => p.DefinitionId, StringComparer.Ordinal);
            var civilizations = staging.Civilizations!.ToDictionary(c => c.CivilizationId, StringComparer.Ordinal);
            foreach (var colony in staging.Colonies!.OrderBy(c => c.ColonyId, StringComparer.Ordinal))
            {
                var profile = profiles[colony.EconomicProfileDefinitionId];
                var population = colony.Population; // Demand and credits use pre-growth population.
                checked
                {
                    colony.Food += profile.FoodOutput;
                    colony.Materials += profile.MaterialOutput;
                    // Ceiling division without the potentially overflowing n+d-1.
                    var demand = population / profile.PopulationPerFood
                        + (population % profile.PopulationPerFood == 0 ? 0 : 1);
                    var sufficient = colony.Food >= demand;
                    colony.Food -= Math.Min(colony.Food, demand);
                    colony.Population = sufficient
                        ? population + Math.Min(profile.GrowthPerTurn, colony.PopulationCapacity - population)
                        : population - Math.Min(profile.StarvationLossPerTurn, population - 1);
                    if (colony.OwnerCivilizationId != null)
                        civilizations[colony.OwnerCivilizationId].Credits += population / profile.PopulationPerCredit;
                }
            }
        }
    }
}
