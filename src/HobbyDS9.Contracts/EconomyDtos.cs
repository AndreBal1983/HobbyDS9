namespace HobbyDS9.Contracts
{
    public sealed class EconomicProfileDefinitionDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public long FoodOutput { get; set; }
        public long PopulationPerFood { get; set; }
        public long MaterialOutput { get; set; }
        public long PopulationPerCredit { get; set; }
        public long BaseIndustry { get; set; }
        public long PopulationPerIndustry { get; set; }
        public long GrowthPerTurn { get; set; }
        public long StarvationLossPerTurn { get; set; }
        public long ResearchOutput { get; set; }
    }

    public sealed class ColonyStateDto
    {
        public string ColonyId { get; set; } = string.Empty;
        public string SystemId { get; set; } = string.Empty;
        public string? OwnerCivilizationId { get; set; }
        public string EconomicProfileDefinitionId { get; set; } = string.Empty;
        public long Population { get; set; }
        public long PopulationCapacity { get; set; }
        public long Food { get; set; }
        public long Materials { get; set; }
    }

    public sealed class ColonySnapshot
    {
        public EntityId ColonyId { get; }
        public EntityId SystemId { get; }
        public EntityId? OwnerCivilizationId { get; }
        public DefinitionId EconomicProfileDefinitionId { get; }
        public long Population { get; }
        public long PopulationCapacity { get; }
        public long Food { get; }
        public long Materials { get; }
        public ColonySnapshot(EntityId colonyId, EntityId systemId, EntityId? ownerCivilizationId,
            DefinitionId economicProfileDefinitionId, long population, long populationCapacity, long food, long materials)
        {
            ColonyId = colonyId; SystemId = systemId; OwnerCivilizationId = ownerCivilizationId;
            EconomicProfileDefinitionId = economicProfileDefinitionId; Population = population;
            PopulationCapacity = populationCapacity; Food = food; Materials = materials;
        }
    }
}
