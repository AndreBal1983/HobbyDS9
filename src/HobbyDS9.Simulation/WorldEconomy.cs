using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static class PopulationRules
    {
        internal const int Scale = 10000, StabilityStep = 250, CohortCeiling = 32, MergeTolerance = 250;
        internal const int PeoplePerFood = 100, PeoplePerCredit = 100, PeoplePerResearch = 500;
        internal const int GrowthBps = 50, MigrationBps = 100, ShortageGrace = 3;
        internal static long ScaleBy(long value, long factor, long scale = Scale) => checked((long)((BigInteger)value * factor / scale));
        internal static long Sum(IEnumerable<long> values) => checked((long)values.Aggregate(BigInteger.Zero, (a, b) => a + b));
        internal static int Rating(long value) => (int)Math.Max(0, Math.Min(Scale, value));
        internal static long Population(ColonyStateDto c) => Sum(c.Cohorts!.Select(p => p.Population));
        internal static long Stock(IEnumerable<ResourceAmountDto> stocks, string resource) => stocks.FirstOrDefault(r => r.ResourceDefinitionId == resource)?.Amount ?? 0;
        internal static void AddStock(List<ResourceAmountDto> stocks, string resource, long amount)
        {
            var stock = stocks.FirstOrDefault(r => r.ResourceDefinitionId == resource);
            if (stock == null) { stock = new ResourceAmountDto { ResourceDefinitionId = resource }; stocks.Add(stock); }
            stock.Amount = checked(stock.Amount + amount);
            if (stock.Amount < 0) throw new InvalidOperationException("Negative physical resource stock.");
            stocks.Sort((a, b) => StringComparer.Ordinal.Compare(a.ResourceDefinitionId, b.ResourceDefinitionId));
        }
        internal static string Identity(PopulationCohortDto c) => c.SpeciesDefinitionId + "|" + (int)c.LegalStatus + "|" + c.OriginCivilizationId;
        internal static bool CanReceive(ColonyStateDto c, PopulationCohortDto cohort, LegalStatus status) =>
            c.Cohorts!.Any(p => p.SpeciesDefinitionId == cohort.SpeciesDefinitionId && p.LegalStatus == status && p.OriginCivilizationId == cohort.OriginCivilizationId)
            || c.Cohorts.Select(Identity).Distinct(StringComparer.Ordinal).Count() < CohortCeiling;
    }
    internal static class WorldEconomy
    {
        internal static PlanetDefinitionDto Planet(ColonyStateDto colony, GameStateDto state, DefinitionCatalogDto definitions) =>
            definitions.Planets!.Single(p => p.DefinitionId == state.Planets!.Single(p => p.PlanetId == colony.PlanetId).DefinitionId);
        internal static IEnumerable<(InfrastructureDefinitionDto definition, int count)> Infrastructure(ColonyStateDto c, DefinitionCatalogDto d) =>
            c.Infrastructure!.OrderBy(i => i.DefinitionId, StringComparer.Ordinal).Select(i => (d.Infrastructure!.Single(x => x.DefinitionId == i.DefinitionId), i.Count));
        internal static long Installed(ColonyStateDto c, DefinitionCatalogDto d, Func<InfrastructureDefinitionDto, long> quantity) =>
            PopulationRules.Sum(Infrastructure(c, d).Select(i => checked(quantity(i.definition) * i.count)));
        internal static int Suitability(SpeciesDefinitionDto species, PlanetDefinitionDto planet, HabitatKind kind)
        {
            var compatible = planet.Gravity >= species.MinimumGravity && planet.Gravity <= species.MaximumGravity
                && planet.Pressure >= species.MinimumPressure && planet.Pressure <= species.MaximumPressure
                && planet.Radiation <= species.MaximumRadiation && species.Atmospheres!.Contains(planet.Atmosphere, StringComparer.Ordinal);
            return compatible ? species.Suitabilities!.Single(s => s.Kind == kind).SuitabilityBps : 0;
        }
        internal static WorldEconomySnapshot Project(ColonyStateDto colony, GameStateDto state, DefinitionCatalogDto definitions)
        {
            var planet = Planet(colony, state, definitions);
            var installed = Infrastructure(colony, definitions).ToList();
            var accessible = installed.SelectMany(i => i.definition.AccessibleHabitats!).ToHashSet();
            var natural = planet.Habitats!.ToDictionary(h => h.Kind, h => accessible.Contains(h.Kind) ? h.NaturalCapacity : 0);
            var remaining = colony.Cohorts!.ToDictionary(c => c.CohortId, c => c.Population, StringComparer.Ordinal);
            var choices = colony.Cohorts.SelectMany(c => planet.Habitats.Select(h => (cohort: c, habitat: (HabitatKind?)h.Kind,
                rating: Suitability(definitions.Species!.Single(s => s.DefinitionId == c.SpeciesDefinitionId), planet, h.Kind))))
                .Where(c => c.rating > 0).ToList();
            var artificial = installed.Select(i => (capacity: checked(i.definition.ArtificialCapacity * i.count), rating: i.definition.ArtificialHabitability)).Where(i => i.capacity > 0).ToList();
            var artificialCapacity = PopulationRules.Sum(artificial.Select(i => i.capacity));
            var artificialRating = artificialCapacity == 0 ? 0 : (int)(artificial.Aggregate(BigInteger.Zero, (sum, i) => sum + (BigInteger)i.capacity * i.rating) / artificialCapacity);
            if (artificialCapacity > 0) choices.AddRange(colony.Cohorts.Select(c => (c, (HabitatKind?)null, artificialRating)));
            var allocations = new List<HabitatAllocationSnapshot>(); var artificialRemaining = artificialCapacity;
            foreach (var choice in choices.OrderByDescending(c => c.rating).ThenBy(c => c.habitat).ThenBy(c => c.cohort.SpeciesDefinitionId, StringComparer.Ordinal).ThenBy(c => c.cohort.CohortId, StringComparer.Ordinal))
            {
                var available = choice.habitat.HasValue ? natural[choice.habitat.Value] : artificialRemaining;
                var used = Math.Min(remaining[choice.cohort.CohortId], available);
                if (used == 0) continue;
                remaining[choice.cohort.CohortId] -= used;
                if (choice.habitat.HasValue) natural[choice.habitat.Value] -= used; else artificialRemaining -= used;
                allocations.Add(new HabitatAllocationSnapshot(choice.cohort.CohortId, choice.habitat, used, choice.rating));
            }
            var population = PopulationRules.Population(colony);
            var effective = checked(PopulationRules.Sum(planet.Habitats.Where(h => accessible.Contains(h.Kind) && definitions.Species!.Any(s => Suitability(s, planet, h.Kind) > 0)).Select(h => h.NaturalCapacity)) + artificialCapacity);
            var average = population == 0 ? 0 : (int)(allocations.Aggregate(BigInteger.Zero, (sum, a) => sum + (BigInteger)a.Population * a.Habitability) / population);
            var power = Installed(colony, definitions, i => i.PowerSupply); var demandPower = Installed(colony, definitions, i => i.PowerDemand);
            var powerFactor = demandPower == 0 || power >= demandPower ? 10000 : PopulationRules.ScaleBy(power, 10000, demandPower);
            var industry = PopulationRules.ScaleBy(PopulationRules.ScaleBy(Installed(colony, definitions, i => i.Industry), 10000L + colony.Automation), powerFactor);
            var credits = PopulationRules.ScaleBy(PopulationRules.ScaleBy(population / PopulationRules.PeoplePerCredit, colony.Prosperity), 5000 + colony.Stability / 2);
            var research = PopulationRules.ScaleBy(checked(Installed(colony, definitions, i => i.Research) + population / PopulationRules.PeoplePerResearch), 5000 + colony.Stability / 2);
            var politics = PoliticalEconomy.Project(colony, definitions);
            industry = PopulationRules.ScaleBy(industry, politics.IndustryFactor);
            credits = PopulationRules.ScaleBy(credits, politics.CreditsFactor);
            research = PopulationRules.ScaleBy(research, politics.ResearchFactor);
            var foodDemand = population / PopulationRules.PeoplePerFood + (population % PopulationRules.PeoplePerFood == 0 ? 0 : 1);
            var weighted = colony.Cohorts.Aggregate(BigInteger.Zero, (sum, c) => sum + (BigInteger)c.Population *
                (4L * c.Satisfaction + 4L * c.CivicAlignment + 2L * (10000 - c.Radicalisation)));
            var legal = colony.Cohorts.Aggregate(BigInteger.Zero, (sum, c) => sum + (BigInteger)c.Population * LegalPenalty(c.LegalStatus));
            var target = population == 0 ? colony.Stability : PopulationRules.Rating((long)(weighted / population / 10) - (long)(legal / population)
                + (colony.Prosperity - 5000) / 10 - (colony.FoodShortageTurns > 0 ? 1000 : 0));
            return new WorldEconomySnapshot(population, PopulationRules.Sum(planet.Habitats.Select(h => h.NaturalCapacity)), effective,
                PopulationRules.Sum(remaining.Values), average, target, industry, credits, research, Installed(colony, definitions, i => i.FoodOutput), foodDemand,
                PopulationRules.ScaleBy(Installed(colony, definitions, i => i.LogisticsCapacity), politics.LogisticsFactor), installed.Any(i => i.definition.Shipbuilding), allocations, politics);
        }
        internal static int LegalPenalty(LegalStatus status) => status == LegalStatus.Occupied ? 1500 : status == LegalStatus.Subject ? 1000 : status == LegalStatus.Refugee ? 500 : 0;
    }
}
