using System;
using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public sealed class ColonyStateDto
    {
        public string ColonyId { get; set; } = string.Empty;
        public string SystemId { get; set; } = string.Empty;
        public string? OwnerCivilizationId { get; set; }
        public string PlanetId { get; set; } = string.Empty;
        public List<PopulationCohortDto>? Cohorts { get; set; }
        public List<InfrastructureStateDto>? Infrastructure { get; set; }
        public List<ResourceAmountDto>? Stockpiles { get; set; }
        public List<ResourceAmountDto>? DesiredStockpiles { get; set; }
        public int Stability { get; set; }
        public int Prosperity { get; set; }
        public int Automation { get; set; }
        public int FoodShortageTurns { get; set; }
        public bool AcceptsRefugees { get; set; }
        public long Food { get; set; }
        public List<ProductionQueueItemDto>? ProductionQueue { get; set; }
    }

}
