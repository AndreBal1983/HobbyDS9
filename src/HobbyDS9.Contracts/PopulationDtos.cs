using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public enum HabitatKind { Oceanic = 1, Polar = 2, Tundra = 3, Temperate = 4, Arid = 5, Tropical = 6, Wetland = 7, Highland = 8, Volcanic = 9 }
    public enum LegalStatus { Citizen = 1, Resident = 2, ForeignNational = 3, Refugee = 4, Subject = 5, Occupied = 6 }
    public sealed class HabitatDefinitionDto
    {
        public HabitatKind Kind { get; set; }
        public int ShareBps { get; set; }
        public long NaturalCapacity { get; set; }
    }
    public sealed class HabitatSuitabilityDto
    {
        public HabitatKind Kind { get; set; }
        public int SuitabilityBps { get; set; }
    }
    public sealed class SpeciesDefinitionDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<HabitatSuitabilityDto>? Suitabilities { get; set; }
        public int MinimumGravity { get; set; }
        public int MaximumGravity { get; set; }
        public int MinimumPressure { get; set; }
        public int MaximumPressure { get; set; }
        public int MaximumRadiation { get; set; }
        public List<string>? Atmospheres { get; set; }
        public List<string>? TraitTags { get; set; }
    }
    public sealed class PlanetDefinitionDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SystemDefinitionId { get; set; } = string.Empty;
        public string PlanetClass { get; set; } = string.Empty;
        public int Gravity { get; set; }
        public int Pressure { get; set; }
        public int Radiation { get; set; }
        public string Atmosphere { get; set; } = string.Empty;
        public List<HabitatDefinitionDto>? Habitats { get; set; }
        public List<ResourceAmountDto>? Deposits { get; set; }
    }
    public sealed class PlanetStateDto
    {
        public string PlanetId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public string SystemId { get; set; } = string.Empty;
        public List<ResourceAmountDto>? RemainingDeposits { get; set; }
    }
    public sealed class ResourceDefinitionDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
    public sealed class ResourceAmountDto
    {
        public string ResourceDefinitionId { get; set; } = string.Empty;
        public long Amount { get; set; }
    }
    public sealed class InfrastructureDefinitionDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public long FoodOutput { get; set; }
        public long Industry { get; set; }
        public long PowerSupply { get; set; }
        public long PowerDemand { get; set; }
        public long Research { get; set; }
        public long LogisticsCapacity { get; set; }
        public bool Shipbuilding { get; set; }
        public List<ResourceAmountDto>? Extraction { get; set; }
        public List<HabitatKind>? AccessibleHabitats { get; set; }
        public long ArtificialCapacity { get; set; }
        public int ArtificialHabitability { get; set; }
    }
    public sealed class InfrastructureStateDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public int Count { get; set; }
    }
    public sealed class ExternalAffinityDto
    {
        public string CivilizationId { get; set; } = string.Empty;
        public int Rating { get; set; }
    }
    public sealed class PopulationMemoryDto
    {
        public string Kind { get; set; } = string.Empty;
        public int Magnitude { get; set; }
        public string? SourceEntityId { get; set; }
        public int StartTurn { get; set; }
        public int ExpiryTurn { get; set; }
        public int DecayPerTurn { get; set; }
    }
    public sealed class PopulationCohortDto
    {
        public string CohortId { get; set; } = string.Empty;
        public string SpeciesDefinitionId { get; set; } = string.Empty;
        public long Population { get; set; }
        public int LegalStatusEligibleTurns { get; set; }
        public LegalStatus LegalStatus { get; set; }
        public string? OriginCivilizationId { get; set; }
        public int Satisfaction { get; set; }
        public int Integration { get; set; }
        public int CivicAlignment { get; set; }
        public int Radicalisation { get; set; }
        public List<ExternalAffinityDto>? ExternalAffinities { get; set; }
        public List<PopulationMemoryDto>? Memories { get; set; }
    }
    public sealed class DisplacementDto
    {
        public string DisplacementId { get; set; } = string.Empty;
        public string SourcePlanetId { get; set; } = string.Empty;
        public string CohortId { get; set; } = string.Empty;
        public long Population { get; set; }
        public string Cause { get; set; } = string.Empty;
        public string? ResponsibleEntityId { get; set; }
    }
    // Completed flow receipts cannot be reconstructed from post-delivery stocks.
    public sealed class LogisticsDeliveryDto
    {
        public string OriginPlanetId { get; set; } = string.Empty;
        public string DestinationPlanetId { get; set; } = string.Empty;
        public string ResourceDefinitionId { get; set; } = string.Empty; // resource.food is reserved for local food.
        public List<string>? RouteNodeIds { get; set; }
        public long ImportNeed { get; set; }
        public long ReachableSurplus { get; set; }
        public long RouteCapacity { get; set; }
        public int StabilityFactor { get; set; }
        public int RouteIntegrity { get; set; }
        public long Delivered { get; set; }
    }
    public sealed class PopulationStatisticsDto
    {
        public long Births { get; set; }
        public long Deaths { get; set; }
        public long Migrated { get; set; }
        public long RefugeesMoved { get; set; }
        public long FoodDelivered { get; set; }
        public long ResourceExtracted { get; set; }
        public long ResourceDelivered { get; set; }
        public long ResourceConsumed { get; set; }
        public long ProductionCompleted { get; set; }
    }
}
