using System;
using System.Collections.Generic;
namespace HobbyDS9.Contracts
{
    public enum NavigationKnowledgeLevel { Unknown=0, Charted=1, Surveyed=2 }
    public sealed class NavigationRegionDefinitionDto
    {
        public string NodeId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AssociatedSystemId { get; set; }
        public List<string>? PhenomenonDefinitionIds { get; set; }
    }
    public sealed class NavigationConnectionDto
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string EndpointA { get; set; } = string.Empty;
        public string EndpointB { get; set; } = string.Empty;
        public GalaxyEdgeKind Kind { get; set; }
        public long BaseTraversalCost { get; set; }
    }
    public sealed class PhenomenonDefinitionDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long TraversalCostIncrease { get; set; }
        public long MovementBudgetPenalty { get; set; }
        public int LogisticsFactor { get; set; }
    }
    public sealed class TraversalEffectDto
    {
        public string EffectId { get; set; } = string.Empty;
        public string? NodeId { get; set; }
        public string? ConnectionId { get; set; }
        public long TraversalCostIncrease { get; set; }
        public long MovementBudgetPenalty { get; set; }
        public int StartTurn { get; set; }
        public int EndTurn { get; set; }
    }
    public sealed class NavigationKnowledgeEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public NavigationKnowledgeLevel Level { get; set; }
    }
    public sealed class CivilizationNavigationKnowledgeDto
    {
        public string CivilizationId { get; set; } = string.Empty;
        public List<NavigationKnowledgeEntryDto>? Nodes { get; set; }
        public List<NavigationKnowledgeEntryDto>? Connections { get; set; }
    }
    public sealed class NavigationStatisticsDto
    {
        public long TraversalCostPaid { get; set; }
        public long EdgesCompleted { get; set; }
        public long PartialTraversalTurns { get; set; }
    }
    public sealed class NavigationCostSnapshot
    {
        public string ConnectionId { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public GalaxyEdgeKind Kind { get; }
        public long BaseCost { get; }
        public long PhenomenonCost { get; }
        public long ArtificialCost { get; }
        public long EffectiveCost { get; }
        public NavigationCostSnapshot(string connection,string from,string to,GalaxyEdgeKind kind,long basis,long phenomena,long artificial)
        {ConnectionId=connection;FromNodeId=from;ToNodeId=to;Kind=kind;BaseCost=basis;PhenomenonCost=phenomena;ArtificialCost=artificial;EffectiveCost=checked(basis+phenomena+artificial);}
    }
    public sealed class StrategicRouteSnapshot
    {
        public RouteErrorCode ErrorCode { get; }
        public bool Success => ErrorCode==RouteErrorCode.None;
        public IReadOnlyList<string> Steps { get; }
        public IReadOnlyList<NavigationCostSnapshot> Costs { get; }
        public long? TotalCost { get; }
        public StrategicRouteSnapshot(RouteErrorCode error,IEnumerable<string> steps,IEnumerable<NavigationCostSnapshot> costs,long? total)
        {ErrorCode=error;Steps=new List<string>(steps).AsReadOnly();Costs=new List<NavigationCostSnapshot>(costs).AsReadOnly();TotalCost=total;}
    }
    public sealed class FleetTravelSnapshot
    {
        public string FleetId { get; }
        public string LocationNodeId { get; }
        public string LimitingShipId { get; }
        public long MovementBudget { get; }
        public long TransitTotalCost { get; }
        public long TransitProgress { get; }
        public long RemainingCost { get; }
        public long EstimatedTurns { get; }
        public FleetTravelSnapshot(string fleet,string node,string limiting,long budget,long total,long progress,long remaining,long eta)
        {FleetId=fleet;LocationNodeId=node;LimitingShipId=limiting;MovementBudget=budget;TransitTotalCost=total;TransitProgress=progress;RemainingCost=remaining;EstimatedTurns=eta;}
    }
}
namespace HobbyDS9.Contracts
{
    public sealed class NavigationViewSnapshot
    {
        public IReadOnlyList<NavigationRegionDefinitionSnapshot> Nodes { get; }
        public IReadOnlyList<NavigationCostSnapshot> Connections { get; }
        public IReadOnlyList<PhenomenonDefinitionSnapshot> Phenomena { get; }
        public NavigationViewSnapshot(IEnumerable<NavigationRegionDefinitionSnapshot> nodes,IEnumerable<NavigationCostSnapshot> connections,IEnumerable<PhenomenonDefinitionSnapshot> phenomena)
        {Nodes=new List<NavigationRegionDefinitionSnapshot>(nodes).AsReadOnly();Connections=new List<NavigationCostSnapshot>(connections).AsReadOnly();Phenomena=new List<PhenomenonDefinitionSnapshot>(phenomena).AsReadOnly();}
    }
}
