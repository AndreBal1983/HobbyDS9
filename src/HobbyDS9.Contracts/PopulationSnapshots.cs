using System;
using System.Collections.Generic;
using System.Linq;
namespace HobbyDS9.Contracts
{
    public sealed class HabitatDefinitionSnapshot
    {
        public HabitatKind Kind { get; }
        public int ShareBps { get; }
        public long NaturalCapacity { get; }
        public HabitatDefinitionSnapshot(HabitatDefinitionDto x)
        { Kind = x.Kind; ShareBps = x.ShareBps; NaturalCapacity = x.NaturalCapacity; }
    }
    public sealed class HabitatSuitabilitySnapshot
    {
        public HabitatKind Kind { get; }
        public int SuitabilityBps { get; }
        public HabitatSuitabilitySnapshot(HabitatSuitabilityDto x)
        { Kind = x.Kind; SuitabilityBps = x.SuitabilityBps; }
    }
    public sealed class SpeciesDefinitionSnapshot
    {
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<HabitatSuitabilitySnapshot> Suitabilities { get; }
        public int MinimumGravity { get; }
        public int MaximumGravity { get; }
        public int MinimumPressure { get; }
        public int MaximumPressure { get; }
        public int MaximumRadiation { get; }
        public IReadOnlyList<string> Atmospheres { get; }
        public IReadOnlyList<string> TraitTags { get; }
        public SpeciesDefinitionSnapshot(SpeciesDefinitionDto x)
        { DefinitionId = x.DefinitionId; DisplayName = x.DisplayName; Suitabilities = x.Suitabilities!.Select(v => new HabitatSuitabilitySnapshot(v)).ToList().AsReadOnly(); MinimumGravity = x.MinimumGravity; MaximumGravity = x.MaximumGravity; MinimumPressure = x.MinimumPressure; MaximumPressure = x.MaximumPressure; MaximumRadiation = x.MaximumRadiation; Atmospheres = x.Atmospheres!.ToList().AsReadOnly(); TraitTags = x.TraitTags!.ToList().AsReadOnly(); }
    }
    public sealed class PlanetDefinitionSnapshot
    {
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public string SystemDefinitionId { get; }
        public string PlanetClass { get; }
        public int Gravity { get; }
        public int Pressure { get; }
        public int Radiation { get; }
        public string Atmosphere { get; }
        public IReadOnlyList<HabitatDefinitionSnapshot> Habitats { get; }
        public IReadOnlyList<ResourceAmountSnapshot> Deposits { get; }
        public PlanetDefinitionSnapshot(PlanetDefinitionDto x)
        { DefinitionId = x.DefinitionId; DisplayName = x.DisplayName; SystemDefinitionId = x.SystemDefinitionId; PlanetClass = x.PlanetClass; Gravity = x.Gravity; Pressure = x.Pressure; Radiation = x.Radiation; Atmosphere = x.Atmosphere; Habitats = x.Habitats!.Select(v => new HabitatDefinitionSnapshot(v)).ToList().AsReadOnly(); Deposits = x.Deposits!.Select(v => new ResourceAmountSnapshot(v)).ToList().AsReadOnly(); }
    }
    public sealed class PlanetStateSnapshot
    {
        public string PlanetId { get; }
        public string DefinitionId { get; }
        public string SystemId { get; }
        public IReadOnlyList<ResourceAmountSnapshot> RemainingDeposits { get; }
        public PlanetStateSnapshot(PlanetStateDto x)
        { PlanetId = x.PlanetId; DefinitionId = x.DefinitionId; SystemId = x.SystemId; RemainingDeposits = x.RemainingDeposits!.Select(v => new ResourceAmountSnapshot(v)).ToList().AsReadOnly(); }
    }
    public sealed class ResourceDefinitionSnapshot
    {
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public ResourceDefinitionSnapshot(ResourceDefinitionDto x)
        { DefinitionId = x.DefinitionId; DisplayName = x.DisplayName; }
    }
    public sealed class ResourceAmountSnapshot
    {
        public string ResourceDefinitionId { get; }
        public long Amount { get; }
        public ResourceAmountSnapshot(ResourceAmountDto x)
        { ResourceDefinitionId = x.ResourceDefinitionId; Amount = x.Amount; }
        public static IReadOnlyList<ResourceAmountSnapshot> Copy(IEnumerable<ResourceAmountDto> values) => values.Select(v => new ResourceAmountSnapshot(v)).ToList().AsReadOnly();
    }
    public sealed class InfrastructureDefinitionSnapshot
    {
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public long FoodOutput { get; }
        public long Industry { get; }
        public long PowerSupply { get; }
        public long PowerDemand { get; }
        public long Research { get; }
        public long LogisticsCapacity { get; }
        public bool Shipbuilding { get; }
        public IReadOnlyList<ResourceAmountSnapshot> Extraction { get; }
        public IReadOnlyList<HabitatKind> AccessibleHabitats { get; }
        public long ArtificialCapacity { get; }
        public int ArtificialHabitability { get; }
        public InfrastructureDefinitionSnapshot(InfrastructureDefinitionDto x)
        { DefinitionId = x.DefinitionId; DisplayName = x.DisplayName; FoodOutput = x.FoodOutput; Industry = x.Industry; PowerSupply = x.PowerSupply; PowerDemand = x.PowerDemand; Research = x.Research; LogisticsCapacity = x.LogisticsCapacity; Shipbuilding = x.Shipbuilding; Extraction = x.Extraction!.Select(v => new ResourceAmountSnapshot(v)).ToList().AsReadOnly(); AccessibleHabitats = x.AccessibleHabitats!.ToList().AsReadOnly(); ArtificialCapacity = x.ArtificialCapacity; ArtificialHabitability = x.ArtificialHabitability; }
    }
    public sealed class InfrastructureStateSnapshot
    {
        public string DefinitionId { get; }
        public int Count { get; }
        public InfrastructureStateSnapshot(InfrastructureStateDto x)
        { DefinitionId = x.DefinitionId; Count = x.Count; }
    }
    public sealed class ExternalAffinitySnapshot
    {
        public string CivilizationId { get; }
        public int Rating { get; }
        public ExternalAffinitySnapshot(ExternalAffinityDto x)
        { CivilizationId = x.CivilizationId; Rating = x.Rating; }
    }
    public sealed class PopulationMemorySnapshot
    {
        public string Kind { get; }
        public int Magnitude { get; }
        public string? SourceEntityId { get; }
        public int StartTurn { get; }
        public int ExpiryTurn { get; }
        public int DecayPerTurn { get; }
        public PopulationMemorySnapshot(PopulationMemoryDto x)
        { Kind = x.Kind; Magnitude = x.Magnitude; SourceEntityId = x.SourceEntityId; StartTurn = x.StartTurn; ExpiryTurn = x.ExpiryTurn; DecayPerTurn = x.DecayPerTurn; }
    }
    public sealed class PopulationCohortSnapshot
    {
        public string CohortId { get; }
        public string SpeciesDefinitionId { get; }
        public long Population { get; }
        public int LegalStatusEligibleTurns { get; }
        public LegalStatus LegalStatus { get; }
        public string? OriginCivilizationId { get; }
        public int Satisfaction { get; }
        public int Integration { get; }
        public int CivicAlignment { get; }
        public int Radicalisation { get; }
        public IReadOnlyList<ExternalAffinitySnapshot> ExternalAffinities { get; }
        public IReadOnlyList<PopulationMemorySnapshot> Memories { get; }
        public PopulationCohortSnapshot(PopulationCohortDto x)
        { CohortId = x.CohortId; SpeciesDefinitionId = x.SpeciesDefinitionId; Population = x.Population; LegalStatus = x.LegalStatus; LegalStatusEligibleTurns = x.LegalStatusEligibleTurns; OriginCivilizationId = x.OriginCivilizationId; Satisfaction = x.Satisfaction; Integration = x.Integration; CivicAlignment = x.CivicAlignment; Radicalisation = x.Radicalisation; ExternalAffinities = x.ExternalAffinities!.Select(v => new ExternalAffinitySnapshot(v)).ToList().AsReadOnly(); Memories = x.Memories!.Select(v => new PopulationMemorySnapshot(v)).ToList().AsReadOnly(); }
    }
    public sealed class DisplacementSnapshot
    {
        public string DisplacementId { get; }
        public string SourcePlanetId { get; }
        public string CohortId { get; }
        public long Population { get; }
        public string Cause { get; }
        public string? ResponsibleEntityId { get; }
        public DisplacementSnapshot(DisplacementDto x)
        { DisplacementId = x.DisplacementId; SourcePlanetId = x.SourcePlanetId; CohortId = x.CohortId; Population = x.Population; Cause = x.Cause; ResponsibleEntityId = x.ResponsibleEntityId; }
    }
    public sealed class LogisticsDeliverySnapshot
    {
        public string OriginPlanetId { get; }
        public string DestinationPlanetId { get; }
        public string ResourceDefinitionId { get; }
        public IReadOnlyList<string> RouteSystemIds { get; }
        public long ImportNeed { get; }
        public long ReachableSurplus { get; }
        public long RouteCapacity { get; }
        public int StabilityFactor { get; }
        public int RouteIntegrity { get; }
        public long Delivered { get; }
        public LogisticsDeliverySnapshot(LogisticsDeliveryDto x)
        { OriginPlanetId = x.OriginPlanetId; DestinationPlanetId = x.DestinationPlanetId; ResourceDefinitionId = x.ResourceDefinitionId; RouteSystemIds = x.RouteSystemIds!.ToList().AsReadOnly(); ImportNeed = x.ImportNeed; ReachableSurplus = x.ReachableSurplus; RouteCapacity = x.RouteCapacity; StabilityFactor = x.StabilityFactor; RouteIntegrity = x.RouteIntegrity; Delivered = x.Delivered; }
    }
    public sealed class PopulationStatisticsSnapshot
    {
        public long Births { get; }
        public long Deaths { get; }
        public long Migrated { get; }
        public long RefugeesMoved { get; }
        public long FoodDelivered { get; }
        public long ResourceExtracted { get; }
        public long ResourceDelivered { get; }
        public long ResourceConsumed { get; }
        public long ProductionCompleted { get; }
        public PopulationStatisticsSnapshot(PopulationStatisticsDto x)
        { Births = x.Births; Deaths = x.Deaths; Migrated = x.Migrated; RefugeesMoved = x.RefugeesMoved; FoodDelivered = x.FoodDelivered; ResourceExtracted = x.ResourceExtracted; ResourceDelivered = x.ResourceDelivered; ResourceConsumed = x.ResourceConsumed; ProductionCompleted = x.ProductionCompleted; }
    }
    public sealed class ColonyStateSnapshot
    {
        public WorldPoliticalStateSnapshot Politics { get; }
        public string ColonyId { get; }
        public string SystemId { get; }
        public string? OwnerCivilizationId { get; }
        public string PlanetId { get; }
        public IReadOnlyList<PopulationCohortSnapshot> Cohorts { get; }
        public IReadOnlyList<InfrastructureStateSnapshot> Infrastructure { get; }
        public IReadOnlyList<ResourceAmountSnapshot> Stockpiles { get; }
        public IReadOnlyList<ResourceAmountSnapshot> DesiredStockpiles { get; }
        public int Stability { get; }
        public int Prosperity { get; }
        public int Automation { get; }
        public int FoodShortageTurns { get; }
        public bool AcceptsRefugees { get; }
        public long Food { get; }
        public IReadOnlyList<ProductionQueueItemSnapshot> ProductionQueue { get; }
        public ColonyStateSnapshot(ColonyStateDto x)
        { Politics = new WorldPoliticalStateSnapshot(x.Politics!); ColonyId = x.ColonyId; SystemId = x.SystemId; OwnerCivilizationId = x.OwnerCivilizationId; PlanetId = x.PlanetId; Cohorts = x.Cohorts!.Select(v => new PopulationCohortSnapshot(v)).ToList().AsReadOnly(); Infrastructure = x.Infrastructure!.Select(v => new InfrastructureStateSnapshot(v)).ToList().AsReadOnly(); Stockpiles = x.Stockpiles!.Select(v => new ResourceAmountSnapshot(v)).ToList().AsReadOnly(); DesiredStockpiles = x.DesiredStockpiles!.Select(v => new ResourceAmountSnapshot(v)).ToList().AsReadOnly(); Stability = x.Stability; Prosperity = x.Prosperity; Automation = x.Automation; FoodShortageTurns = x.FoodShortageTurns; AcceptsRefugees = x.AcceptsRefugees; Food = x.Food; ProductionQueue = x.ProductionQueue!.Select(v => new ProductionQueueItemSnapshot(v)).ToList().AsReadOnly(); }
    }
}
