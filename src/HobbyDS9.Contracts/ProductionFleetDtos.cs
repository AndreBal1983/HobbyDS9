using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public sealed class ShipDesignDefinitionDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public long MovementBudget { get; set; }
        public long IndustryCost { get; set; }
        public List<ResourceAmountDto>? ResourceCosts { get; set; }
        public long CreditCost { get; set; }
        public long MaxHull { get; set; }
        public ShipCombatProfileDto? CombatProfile { get; set; }
        public List<string>? RequiredResearchDefinitionIds { get; set; }
    }
    public sealed class ProductionQueueItemDto
    {
        public string QueueItemId { get; set; } = string.Empty;
        public string ShipDesignDefinitionId { get; set; } = string.Empty;
        public long RequiredIndustry { get; set; }
        public long CompletedIndustry { get; set; }
        public List<ResourceAmountDto>? PaidResources { get; set; }
        public long PaidCredits { get; set; }
    }
    public sealed class ShipStateDto
    {
        public string ShipId { get; set; } = string.Empty;
        public string ShipDesignDefinitionId { get; set; } = string.Empty;
        public string OwnerCivilizationId { get; set; } = string.Empty;
        public string FleetId { get; set; } = string.Empty;
        public long CurrentHull { get; set; }
    }
    public sealed class FleetStateDto
    {
        public long TransitTotalCost { get; set; }
        public long TransitProgress { get; set; }
        public string FleetId { get; set; } = string.Empty;
        public string OwnerCivilizationId { get; set; } = string.Empty;
        public string LocationNodeId { get; set; } = string.Empty;
        public List<string>? ShipIds { get; set; }
        public List<string>? RouteNodeIds { get; set; }
    }
    public sealed class ProductionQueueItemSnapshot
    {
        public EntityId QueueItemId { get; }
        public DefinitionId ShipDesignDefinitionId { get; }
        public long RequiredIndustry { get; }
        public long CompletedIndustry { get; }
        public IReadOnlyList<ResourceAmountSnapshot> PaidResources { get; }
        public long PaidCredits { get; }
        public ProductionQueueItemSnapshot(ProductionQueueItemDto item)
        {
            QueueItemId = new EntityId(item.QueueItemId); ShipDesignDefinitionId = new DefinitionId(item.ShipDesignDefinitionId);
            RequiredIndustry = item.RequiredIndustry; CompletedIndustry = item.CompletedIndustry;
            PaidResources = ResourceAmountSnapshot.Copy(item.PaidResources!); PaidCredits = item.PaidCredits;
        }
    }
    public sealed class ShipSnapshot
    {
        public EntityId ShipId { get; }
        public DefinitionId ShipDesignDefinitionId { get; }
        public EntityId OwnerCivilizationId { get; }
        public EntityId FleetId { get; }
        public long CurrentHull { get; }
        public ShipSnapshot(ShipStateDto ship)
        {
            ShipId = new EntityId(ship.ShipId); ShipDesignDefinitionId = new DefinitionId(ship.ShipDesignDefinitionId);
            OwnerCivilizationId = new EntityId(ship.OwnerCivilizationId); FleetId = new EntityId(ship.FleetId); CurrentHull = ship.CurrentHull;
        }
    }
    public sealed class FleetSnapshot
    {
        public EntityId FleetId { get; }
        public EntityId OwnerCivilizationId { get; }
        public long TransitTotalCost { get; }
        public long TransitProgress { get; }
        public EntityId LocationNodeId { get; }
        public IReadOnlyList<EntityId> ShipIds { get; }
        public IReadOnlyList<EntityId> RouteNodeIds { get; }
        public FleetSnapshot(FleetStateDto fleet)
        {
            FleetId = new EntityId(fleet.FleetId); OwnerCivilizationId = new EntityId(fleet.OwnerCivilizationId);
            TransitTotalCost=fleet.TransitTotalCost;TransitProgress=fleet.TransitProgress;LocationNodeId = new EntityId(fleet.LocationNodeId);
            ShipIds = fleet.ShipIds!.ConvertAll(id => new EntityId(id)).AsReadOnly();
            RouteNodeIds = fleet.RouteNodeIds!.ConvertAll(id => new EntityId(id)).AsReadOnly();
        }
    }
    public sealed class ShipDesignSnapshot
    {
        public DefinitionId DefinitionId { get; }
        public string DisplayName { get; }
        public long MovementBudget { get; }
        public long IndustryCost { get; }
        public IReadOnlyList<ResourceAmountSnapshot> ResourceCosts { get; }
        public long CreditCost { get; }
        public long MaxHull { get; }
        public ShipCombatProfileSnapshot CombatProfile { get; }
        public IReadOnlyList<DefinitionId> RequiredResearchDefinitionIds { get; }
        public ShipDesignSnapshot(ShipDesignDefinitionDto design)
        {
            DefinitionId = new DefinitionId(design.DefinitionId); DisplayName = design.DisplayName;
            MovementBudget=design.MovementBudget;IndustryCost = design.IndustryCost; ResourceCosts = ResourceAmountSnapshot.Copy(design.ResourceCosts!); CreditCost = design.CreditCost; MaxHull = design.MaxHull; CombatProfile = new ShipCombatProfileSnapshot(design.CombatProfile!);
            RequiredResearchDefinitionIds = design.RequiredResearchDefinitionIds!.ConvertAll(id => new DefinitionId(id)).AsReadOnly();
        }
    }
}
