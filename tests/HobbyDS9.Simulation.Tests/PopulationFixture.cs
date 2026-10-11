using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
namespace HobbyDS9.Simulation.Tests;

internal static class PopulationFixture
{
    internal static DefinitionCatalogDto Content(DefinitionCatalogDto definitions)
    {
        var source = VerticalSliceFixture.Scenario().Definitions;
        definitions.Species = new() { PopulationData.Copy(source.Species!.Single(s => s.DefinitionId == "species.human")) };
        var planet = PopulationData.Copy(source.Planets!.Single(p => p.DefinitionId == "planet-definition.sol"));
        planet.DefinitionId = "planet-definition.fixture"; planet.SystemDefinitionId = "system.fixture"; planet.Deposits = new();
        foreach (var h in planet.Habitats!) h.NaturalCapacity = 0;
        planet.Habitats.Single(h => h.Kind == HabitatKind.Temperate).NaturalCapacity = 2000;
        definitions.Planets = new() { planet }; definitions.Resources = new();
        definitions.Infrastructure = new(){new(){DefinitionId="infrastructure.fixture",DisplayName="Fixture",FoodOutput=12,Industry=8,PowerSupply=10,Research=1,LogisticsCapacity=20,Shipbuilding=true,
            AccessibleHabitats=Enum.GetValues<HabitatKind>().ToList(),Extraction=new()}};
        definitions.DefinitionIds.AddRange(new[] { "species.human", "planet-definition.fixture", "infrastructure.fixture" }); return definitions;
    }
    internal static ColonyStateDto Colony(string id, string system, string? owner, long population = 1000) => new()
    {
        ColonyId = id,
        SystemId = system,
        OwnerCivilizationId = owner,
        PlanetId = "planet." + id,
        Food = 20,
        ProductionQueue = new(),
        Stability = 10000,
        Prosperity = 10000,
        Automation = 0,
        AcceptsRefugees = true,
        Stockpiles = new(),
        DesiredStockpiles = new(),
        Infrastructure = new() { new() { DefinitionId = "infrastructure.fixture", Count = 1 } },
        Cohorts = new() { new() { CohortId = "cohort." + id, SpeciesDefinitionId = "species.human", Population = population, LegalStatus = LegalStatus.Citizen, Satisfaction = 10000, Integration = 10000, CivicAlignment = 10000, Radicalisation = 0, ExternalAffinities = new(), Memories = new() } }
    };
    internal static SaveEnvelopeDto Save(SaveEnvelopeDto save) { foreach (var c in save.GameState.Colonies!) RegisterWorld(save.GameState, c); return save; }
    internal static void RegisterWorld(GameStateDto state, ColonyStateDto colony)
    {
        state.Planets!.Add(new() { PlanetId = colony.PlanetId, DefinitionId = "planet-definition.fixture", SystemId = colony.SystemId, RemainingDeposits = new() });
        state.EntityIdentities.Add(new() { EntityId = colony.PlanetId, DefinitionId = "planet-definition.fixture" });
        foreach (var c in colony.Cohorts!) state.EntityIdentities.Add(new() { EntityId = c.CohortId, DefinitionId = c.SpeciesDefinitionId });
    }
    // Isolate older combat/production fixtures from the new second-world migration.
    internal static void RemoveMars(ScenarioDto scenario)
    {
        var state = scenario.InitialState; var colony = state.Colonies!.Single(c => c.ColonyId == "colony.mars");
        var ids = colony.Cohorts!.Select(c => c.CohortId).Append(colony.ColonyId).Append(colony.PlanetId).ToHashSet();
        state.Colonies!.Remove(colony); state.Planets!.RemoveAll(p => p.PlanetId == colony.PlanetId); state.EntityIdentities.RemoveAll(e => ids.Contains(e.EntityId));
    }
    internal static long Dilithium(ColonyStateDto colony) => PopulationRules.Stock(colony.Stockpiles!, "resource.dilithium");
    internal static long Dilithium(ColonySnapshot colony) => colony.State.Stockpiles.SingleOrDefault(r => r.ResourceDefinitionId == "resource.dilithium")?.Amount ?? 0;
    internal static void SetDilithium(ColonyStateDto colony, long amount)
    { colony.Stockpiles!.RemoveAll(r => r.ResourceDefinitionId == "resource.dilithium"); colony.Stockpiles.Add(new() { ResourceDefinitionId = "resource.dilithium", Amount = amount }); }
}
