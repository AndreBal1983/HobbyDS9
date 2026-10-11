using System.Collections.Generic;
using System.Linq;
namespace HobbyDS9.Contracts
{
    public sealed class HabitatAllocationSnapshot
    {
        public string CohortId { get; }
        public HabitatKind? Habitat { get; } // null = artificial habitat
        public long Population { get; }
        public int Habitability { get; }
        public HabitatAllocationSnapshot(string cohortId, HabitatKind? habitat, long population, int habitability)
        { CohortId = cohortId; Habitat = habitat; Population = population; Habitability = habitability; }
    }
    public sealed class WorldEconomySnapshot
    {
        public PoliticalEconomySnapshot Politics { get; }
        public long Population { get; }
        public long NaturalCapacity { get; }
        public long EffectiveCapacity { get; }
        public long Crowding { get; }
        public int AverageHabitability { get; }
        public int TargetStability { get; }
        public long Industry { get; }
        public long CreditOutput { get; }
        public long ResearchOutput { get; }
        public long FoodOutput { get; }
        public long FoodDemand { get; }
        public long LogisticsCapacity { get; }
        public bool Shipbuilding { get; }
        public IReadOnlyList<HabitatAllocationSnapshot> Allocations { get; }
        public WorldEconomySnapshot(long population, long natural, long effective, long crowding, int habitability,
            int targetStability, long industry, long credits, long research, long food, long demand, long logistics, bool shipbuilding,
            IEnumerable<HabitatAllocationSnapshot> allocations, PoliticalEconomySnapshot? politics = null)
        {
            Politics = politics ?? new PoliticalEconomySnapshot(10000,10000,10000,10000,10000,true);
            Population = population; NaturalCapacity = natural; EffectiveCapacity = effective; Crowding = crowding;
            AverageHabitability = habitability; TargetStability = targetStability; Industry = industry; CreditOutput = credits;
            ResearchOutput = research; FoodOutput = food; FoodDemand = demand; LogisticsCapacity = logistics;
            Shipbuilding = shipbuilding; Allocations = allocations.ToList().AsReadOnly();
        }
    }
    public sealed class ColonySnapshot
    {
        public EntityId ColonyId { get; }
        public EntityId SystemId { get; }
        public EntityId? OwnerCivilizationId { get; }
        public EntityId PlanetId { get; }
        public ColonyStateSnapshot State { get; }
        public PlanetDefinitionSnapshot Environment { get; }
        public WorldEconomySnapshot Economy { get; }
        public long Population => Economy.Population;
        public long PopulationCapacity => Economy.EffectiveCapacity;
        public long Food => State.Food;
        public IReadOnlyList<ProductionQueueItemSnapshot> ProductionQueue => State.ProductionQueue;
        public ColonySnapshot(ColonyStateDto state, PlanetDefinitionDto planet, WorldEconomySnapshot economy)
        {
            ColonyId = new EntityId(state.ColonyId); SystemId = new EntityId(state.SystemId); PlanetId = new EntityId(state.PlanetId);
            OwnerCivilizationId = state.OwnerCivilizationId == null ? (EntityId?)null : new EntityId(state.OwnerCivilizationId);
            State = new ColonyStateSnapshot(state); Environment = new PlanetDefinitionSnapshot(planet); Economy = economy;
        }
    }
}
