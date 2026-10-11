using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static partial class PopulationData
    {
        internal static HabitatDefinitionDto Copy(HabitatDefinitionDto x) => new HabitatDefinitionDto
        {
            Kind = x.Kind,
            ShareBps = x.ShareBps,
            NaturalCapacity = x.NaturalCapacity,
        };
        internal static bool Same(HabitatDefinitionDto? a, HabitatDefinitionDto? b) => a == null ? b == null : b != null && a.Kind == b.Kind
            && a.ShareBps == b.ShareBps
            && a.NaturalCapacity == b.NaturalCapacity;
        internal static HabitatSuitabilityDto Copy(HabitatSuitabilityDto x) => new HabitatSuitabilityDto
        {
            Kind = x.Kind,
            SuitabilityBps = x.SuitabilityBps,
        };
        internal static bool Same(HabitatSuitabilityDto? a, HabitatSuitabilityDto? b) => a == null ? b == null : b != null && a.Kind == b.Kind
            && a.SuitabilityBps == b.SuitabilityBps;
        internal static SpeciesDefinitionDto Copy(SpeciesDefinitionDto x) => new SpeciesDefinitionDto
        {
            DefinitionId = x.DefinitionId,
            DisplayName = x.DisplayName,
            Suitabilities = x.Suitabilities!.OrderBy(v => v.Kind).Select(Copy).ToList(),
            MinimumGravity = x.MinimumGravity,
            MaximumGravity = x.MaximumGravity,
            MinimumPressure = x.MinimumPressure,
            MaximumPressure = x.MaximumPressure,
            MaximumRadiation = x.MaximumRadiation,
            Atmospheres = x.Atmospheres!.OrderBy(v => v, StringComparer.Ordinal).ToList(),
            TraitTags = x.TraitTags!.OrderBy(v => v, StringComparer.Ordinal).ToList(),
        };
        internal static bool Same(SpeciesDefinitionDto? a, SpeciesDefinitionDto? b) => a == null ? b == null : b != null && a.DefinitionId == b.DefinitionId
            && a.DisplayName == b.DisplayName
            && (a.Suitabilities == null ? b.Suitabilities == null : b.Suitabilities != null && a.Suitabilities.Count == b.Suitabilities.Count && a.Suitabilities.Zip(b.Suitabilities, (x, y) => Same(x, y)).All(v => v))
            && a.MinimumGravity == b.MinimumGravity
            && a.MaximumGravity == b.MaximumGravity
            && a.MinimumPressure == b.MinimumPressure
            && a.MaximumPressure == b.MaximumPressure
            && a.MaximumRadiation == b.MaximumRadiation
            && (a.Atmospheres == null ? b.Atmospheres == null : b.Atmospheres != null && a.Atmospheres.Count == b.Atmospheres.Count && a.Atmospheres.SequenceEqual(b.Atmospheres))
            && (a.TraitTags == null ? b.TraitTags == null : b.TraitTags != null && a.TraitTags.Count == b.TraitTags.Count && a.TraitTags.SequenceEqual(b.TraitTags));
        internal static PlanetDefinitionDto Copy(PlanetDefinitionDto x) => new PlanetDefinitionDto
        {
            DefinitionId = x.DefinitionId,
            DisplayName = x.DisplayName,
            SystemDefinitionId = x.SystemDefinitionId,
            PlanetClass = x.PlanetClass,
            Gravity = x.Gravity,
            Pressure = x.Pressure,
            Radiation = x.Radiation,
            Atmosphere = x.Atmosphere,
            Habitats = x.Habitats!.OrderBy(v => v.Kind).Select(Copy).ToList(),
            Deposits = x.Deposits!.OrderBy(v => v.ResourceDefinitionId, StringComparer.Ordinal).Select(Copy).ToList(),
        };
        internal static bool Same(PlanetDefinitionDto? a, PlanetDefinitionDto? b) => a == null ? b == null : b != null && a.DefinitionId == b.DefinitionId
            && a.DisplayName == b.DisplayName
            && a.SystemDefinitionId == b.SystemDefinitionId
            && a.PlanetClass == b.PlanetClass
            && a.Gravity == b.Gravity
            && a.Pressure == b.Pressure
            && a.Radiation == b.Radiation
            && a.Atmosphere == b.Atmosphere
            && (a.Habitats == null ? b.Habitats == null : b.Habitats != null && a.Habitats.Count == b.Habitats.Count && a.Habitats.Zip(b.Habitats, (x, y) => Same(x, y)).All(v => v))
            && (a.Deposits == null ? b.Deposits == null : b.Deposits != null && a.Deposits.Count == b.Deposits.Count && a.Deposits.Zip(b.Deposits, (x, y) => Same(x, y)).All(v => v));
        internal static PlanetStateDto Copy(PlanetStateDto x) => new PlanetStateDto
        {
            PlanetId = x.PlanetId,
            DefinitionId = x.DefinitionId,
            SystemId = x.SystemId,
            RemainingDeposits = x.RemainingDeposits!.OrderBy(v => v.ResourceDefinitionId, StringComparer.Ordinal).Select(Copy).ToList(),
        };
        internal static bool Same(PlanetStateDto? a, PlanetStateDto? b) => a == null ? b == null : b != null && a.PlanetId == b.PlanetId
            && a.DefinitionId == b.DefinitionId
            && a.SystemId == b.SystemId
            && (a.RemainingDeposits == null ? b.RemainingDeposits == null : b.RemainingDeposits != null && a.RemainingDeposits.Count == b.RemainingDeposits.Count && a.RemainingDeposits.Zip(b.RemainingDeposits, (x, y) => Same(x, y)).All(v => v));
        internal static ResourceDefinitionDto Copy(ResourceDefinitionDto x) => new ResourceDefinitionDto
        {
            DefinitionId = x.DefinitionId,
            DisplayName = x.DisplayName,
        };
        internal static bool Same(ResourceDefinitionDto? a, ResourceDefinitionDto? b) => a == null ? b == null : b != null && a.DefinitionId == b.DefinitionId
            && a.DisplayName == b.DisplayName;
        internal static ResourceAmountDto Copy(ResourceAmountDto x) => new ResourceAmountDto
        {
            ResourceDefinitionId = x.ResourceDefinitionId,
            Amount = x.Amount,
        };
        internal static bool Same(ResourceAmountDto? a, ResourceAmountDto? b) => a == null ? b == null : b != null && a.ResourceDefinitionId == b.ResourceDefinitionId
            && a.Amount == b.Amount;
        internal static InfrastructureDefinitionDto Copy(InfrastructureDefinitionDto x) => new InfrastructureDefinitionDto
        {
            DefinitionId = x.DefinitionId,
            DisplayName = x.DisplayName,
            FoodOutput = x.FoodOutput,
            Industry = x.Industry,
            PowerSupply = x.PowerSupply,
            PowerDemand = x.PowerDemand,
            Research = x.Research,
            LogisticsCapacity = x.LogisticsCapacity,
            Shipbuilding = x.Shipbuilding,
            Extraction = x.Extraction!.OrderBy(v => v.ResourceDefinitionId, StringComparer.Ordinal).Select(Copy).ToList(),
            AccessibleHabitats = x.AccessibleHabitats!.OrderBy(v => v).ToList(),
            ArtificialCapacity = x.ArtificialCapacity,
            ArtificialHabitability = x.ArtificialHabitability,
        };
        internal static bool Same(InfrastructureDefinitionDto? a, InfrastructureDefinitionDto? b) => a == null ? b == null : b != null && a.DefinitionId == b.DefinitionId
            && a.DisplayName == b.DisplayName
            && a.FoodOutput == b.FoodOutput
            && a.Industry == b.Industry
            && a.PowerSupply == b.PowerSupply
            && a.PowerDemand == b.PowerDemand
            && a.Research == b.Research
            && a.LogisticsCapacity == b.LogisticsCapacity
            && a.Shipbuilding == b.Shipbuilding
            && (a.Extraction == null ? b.Extraction == null : b.Extraction != null && a.Extraction.Count == b.Extraction.Count && a.Extraction.Zip(b.Extraction, (x, y) => Same(x, y)).All(v => v))
            && (a.AccessibleHabitats == null ? b.AccessibleHabitats == null : b.AccessibleHabitats != null && a.AccessibleHabitats.Count == b.AccessibleHabitats.Count && a.AccessibleHabitats.SequenceEqual(b.AccessibleHabitats))
            && a.ArtificialCapacity == b.ArtificialCapacity
            && a.ArtificialHabitability == b.ArtificialHabitability;
        internal static InfrastructureStateDto Copy(InfrastructureStateDto x) => new InfrastructureStateDto
        {
            DefinitionId = x.DefinitionId,
            Count = x.Count,
        };
        internal static bool Same(InfrastructureStateDto? a, InfrastructureStateDto? b) => a == null ? b == null : b != null && a.DefinitionId == b.DefinitionId
            && a.Count == b.Count;
        internal static ExternalAffinityDto Copy(ExternalAffinityDto x) => new ExternalAffinityDto
        {
            CivilizationId = x.CivilizationId,
            Rating = x.Rating,
        };
        internal static bool Same(ExternalAffinityDto? a, ExternalAffinityDto? b) => a == null ? b == null : b != null && a.CivilizationId == b.CivilizationId
            && a.Rating == b.Rating;
        internal static PopulationMemoryDto Copy(PopulationMemoryDto x) => new PopulationMemoryDto
        {
            Kind = x.Kind,
            Magnitude = x.Magnitude,
            SourceEntityId = x.SourceEntityId,
            StartTurn = x.StartTurn,
            ExpiryTurn = x.ExpiryTurn,
            DecayPerTurn = x.DecayPerTurn,
        };
        internal static bool Same(PopulationMemoryDto? a, PopulationMemoryDto? b) => a == null ? b == null : b != null && a.Kind == b.Kind
            && a.Magnitude == b.Magnitude
            && a.SourceEntityId == b.SourceEntityId
            && a.StartTurn == b.StartTurn
            && a.ExpiryTurn == b.ExpiryTurn
            && a.DecayPerTurn == b.DecayPerTurn;
        internal static PopulationCohortDto Copy(PopulationCohortDto x) => new PopulationCohortDto
        {
            CohortId = x.CohortId,
            SpeciesDefinitionId = x.SpeciesDefinitionId,
            Population = x.Population,
            LegalStatus = x.LegalStatus,
            LegalStatusEligibleTurns = x.LegalStatusEligibleTurns,
            OriginCivilizationId = x.OriginCivilizationId,
            Satisfaction = x.Satisfaction,
            Integration = x.Integration,
            CivicAlignment = x.CivicAlignment,
            Radicalisation = x.Radicalisation,
            ExternalAffinities = x.ExternalAffinities!.OrderBy(v => v.CivilizationId, StringComparer.Ordinal).Select(Copy).ToList(),
            Memories = x.Memories!.OrderBy(v => v.Kind + "|" + v.SourceEntityId, StringComparer.Ordinal).Select(Copy).ToList(),
        };
        internal static bool Same(PopulationCohortDto? a, PopulationCohortDto? b) => a == null ? b == null : b != null && a.CohortId == b.CohortId
            && a.SpeciesDefinitionId == b.SpeciesDefinitionId
            && a.Population == b.Population
            && a.LegalStatus == b.LegalStatus
            && a.LegalStatusEligibleTurns == b.LegalStatusEligibleTurns
            && a.OriginCivilizationId == b.OriginCivilizationId
            && a.Satisfaction == b.Satisfaction
            && a.Integration == b.Integration
            && a.CivicAlignment == b.CivicAlignment
            && a.Radicalisation == b.Radicalisation
            && (a.ExternalAffinities == null ? b.ExternalAffinities == null : b.ExternalAffinities != null && a.ExternalAffinities.Count == b.ExternalAffinities.Count && a.ExternalAffinities.Zip(b.ExternalAffinities, (x, y) => Same(x, y)).All(v => v))
            && (a.Memories == null ? b.Memories == null : b.Memories != null && a.Memories.Count == b.Memories.Count && a.Memories.Zip(b.Memories, (x, y) => Same(x, y)).All(v => v));
        internal static DisplacementDto Copy(DisplacementDto x) => new DisplacementDto
        {
            DisplacementId = x.DisplacementId,
            SourcePlanetId = x.SourcePlanetId,
            CohortId = x.CohortId,
            Population = x.Population,
            Cause = x.Cause,
            ResponsibleEntityId = x.ResponsibleEntityId,
        };
        internal static bool Same(DisplacementDto? a, DisplacementDto? b) => a == null ? b == null : b != null && a.DisplacementId == b.DisplacementId
            && a.SourcePlanetId == b.SourcePlanetId
            && a.CohortId == b.CohortId
            && a.Population == b.Population
            && a.Cause == b.Cause
            && a.ResponsibleEntityId == b.ResponsibleEntityId;
        internal static LogisticsDeliveryDto Copy(LogisticsDeliveryDto x) => new LogisticsDeliveryDto
        {
            OriginPlanetId = x.OriginPlanetId,
            DestinationPlanetId = x.DestinationPlanetId,
            ResourceDefinitionId = x.ResourceDefinitionId,
            RouteSystemIds = x.RouteSystemIds!.ToList(),
            ImportNeed = x.ImportNeed,
            ReachableSurplus = x.ReachableSurplus,
            RouteCapacity = x.RouteCapacity,
            StabilityFactor = x.StabilityFactor,
            RouteIntegrity = x.RouteIntegrity,
            Delivered = x.Delivered,
        };
        internal static bool Same(LogisticsDeliveryDto? a, LogisticsDeliveryDto? b) => a == null ? b == null : b != null && a.OriginPlanetId == b.OriginPlanetId
            && a.DestinationPlanetId == b.DestinationPlanetId
            && a.ResourceDefinitionId == b.ResourceDefinitionId
            && (a.RouteSystemIds == null ? b.RouteSystemIds == null : b.RouteSystemIds != null && a.RouteSystemIds.Count == b.RouteSystemIds.Count && a.RouteSystemIds.SequenceEqual(b.RouteSystemIds))
            && a.ImportNeed == b.ImportNeed
            && a.ReachableSurplus == b.ReachableSurplus
            && a.RouteCapacity == b.RouteCapacity
            && a.StabilityFactor == b.StabilityFactor
            && a.RouteIntegrity == b.RouteIntegrity
            && a.Delivered == b.Delivered;
        internal static PopulationStatisticsDto Copy(PopulationStatisticsDto x) => new PopulationStatisticsDto
        {
            Births = x.Births,
            Deaths = x.Deaths,
            Migrated = x.Migrated,
            RefugeesMoved = x.RefugeesMoved,
            FoodDelivered = x.FoodDelivered,
            ResourceExtracted = x.ResourceExtracted,
            ResourceDelivered = x.ResourceDelivered,
            ResourceConsumed = x.ResourceConsumed,
            ProductionCompleted = x.ProductionCompleted,
        };
        internal static bool Same(PopulationStatisticsDto? a, PopulationStatisticsDto? b) => a == null ? b == null : b != null && a.Births == b.Births
            && a.Deaths == b.Deaths
            && a.Migrated == b.Migrated
            && a.RefugeesMoved == b.RefugeesMoved
            && a.FoodDelivered == b.FoodDelivered
            && a.ResourceExtracted == b.ResourceExtracted
            && a.ResourceDelivered == b.ResourceDelivered
            && a.ResourceConsumed == b.ResourceConsumed
            && a.ProductionCompleted == b.ProductionCompleted;
        internal static ColonyStateDto Copy(ColonyStateDto x) => new ColonyStateDto
        {
            Politics = Copy(x.Politics!),
            ColonyId = x.ColonyId,
            SystemId = x.SystemId,
            OwnerCivilizationId = x.OwnerCivilizationId,
            PlanetId = x.PlanetId,
            Cohorts = x.Cohorts!.OrderBy(v => v.CohortId, StringComparer.Ordinal).Select(Copy).ToList(),
            Infrastructure = x.Infrastructure!.OrderBy(v => v.DefinitionId, StringComparer.Ordinal).Select(Copy).ToList(),
            Stockpiles = x.Stockpiles!.OrderBy(v => v.ResourceDefinitionId, StringComparer.Ordinal).Select(Copy).ToList(),
            DesiredStockpiles = x.DesiredStockpiles!.OrderBy(v => v.ResourceDefinitionId, StringComparer.Ordinal).Select(Copy).ToList(),
            Stability = x.Stability,
            Prosperity = x.Prosperity,
            Automation = x.Automation,
            FoodShortageTurns = x.FoodShortageTurns,
            AcceptsRefugees = x.AcceptsRefugees,
            Food = x.Food,
            ProductionQueue = x.ProductionQueue!.Select(q => new ProductionQueueItemDto { QueueItemId = q.QueueItemId, ShipDesignDefinitionId = q.ShipDesignDefinitionId, RequiredIndustry = q.RequiredIndustry, CompletedIndustry = q.CompletedIndustry, PaidCredits = q.PaidCredits, PaidResources = q.PaidResources!.OrderBy(v => v.ResourceDefinitionId, StringComparer.Ordinal).Select(Copy).ToList() }).ToList(),
        };
        internal static bool Same(ColonyStateDto? a, ColonyStateDto? b) => a == null ? b == null : b != null && Same(a.Politics,b.Politics) && a.ColonyId == b.ColonyId
            && a.SystemId == b.SystemId
            && a.OwnerCivilizationId == b.OwnerCivilizationId
            && a.PlanetId == b.PlanetId
            && (a.Cohorts == null ? b.Cohorts == null : b.Cohorts != null && a.Cohorts.Count == b.Cohorts.Count && a.Cohorts.Zip(b.Cohorts, (x, y) => Same(x, y)).All(v => v))
            && (a.Infrastructure == null ? b.Infrastructure == null : b.Infrastructure != null && a.Infrastructure.Count == b.Infrastructure.Count && a.Infrastructure.Zip(b.Infrastructure, (x, y) => Same(x, y)).All(v => v))
            && (a.Stockpiles == null ? b.Stockpiles == null : b.Stockpiles != null && a.Stockpiles.Count == b.Stockpiles.Count && a.Stockpiles.Zip(b.Stockpiles, (x, y) => Same(x, y)).All(v => v))
            && (a.DesiredStockpiles == null ? b.DesiredStockpiles == null : b.DesiredStockpiles != null && a.DesiredStockpiles.Count == b.DesiredStockpiles.Count && a.DesiredStockpiles.Zip(b.DesiredStockpiles, (x, y) => Same(x, y)).All(v => v))
            && a.Stability == b.Stability
            && a.Prosperity == b.Prosperity
            && a.Automation == b.Automation
            && a.FoodShortageTurns == b.FoodShortageTurns
            && a.AcceptsRefugees == b.AcceptsRefugees
            && a.Food == b.Food
            && (a.ProductionQueue == null ? b.ProductionQueue == null : b.ProductionQueue != null && a.ProductionQueue.Count == b.ProductionQueue.Count && a.ProductionQueue.Zip(b.ProductionQueue, (x, y) => x.QueueItemId == y.QueueItemId && x.ShipDesignDefinitionId == y.ShipDesignDefinitionId && x.RequiredIndustry == y.RequiredIndustry && x.CompletedIndustry == y.CompletedIndustry && x.PaidCredits == y.PaidCredits && x.PaidResources!.Count == y.PaidResources!.Count && x.PaidResources.Zip(y.PaidResources, (r, s) => Same(r, s)).All(v => v)).All(v => v));
    }
}
