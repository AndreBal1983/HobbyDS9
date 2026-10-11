using System.Collections.Generic;
using System.Linq;
namespace HobbyDS9.Contracts
{
    public sealed class NavigationRegionDefinitionSnapshot
    {
        public string NodeId { get; }
        public string DisplayName { get; }
        public string? AssociatedSystemId { get; }
        public IReadOnlyList<string> PhenomenonDefinitionIds { get; }
        public NavigationRegionDefinitionSnapshot(NavigationRegionDefinitionDto x)
        {
            NodeId = x.NodeId;
            DisplayName = x.DisplayName;
            AssociatedSystemId = x.AssociatedSystemId;
            PhenomenonDefinitionIds = x.PhenomenonDefinitionIds!.ToList().AsReadOnly();
        }
    }
    public sealed class NavigationConnectionSnapshot
    {
        public string ConnectionId { get; }
        public string EndpointA { get; }
        public string EndpointB { get; }
        public GalaxyEdgeKind Kind { get; }
        public long BaseTraversalCost { get; }
        public NavigationConnectionSnapshot(NavigationConnectionDto x)
        {
            ConnectionId = x.ConnectionId;
            EndpointA = x.EndpointA;
            EndpointB = x.EndpointB;
            Kind = x.Kind;
            BaseTraversalCost = x.BaseTraversalCost;
        }
    }
    public sealed class PhenomenonDefinitionSnapshot
    {
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public long TraversalCostIncrease { get; }
        public long MovementBudgetPenalty { get; }
        public int LogisticsFactor { get; }
        public PhenomenonDefinitionSnapshot(PhenomenonDefinitionDto x)
        {
            DefinitionId = x.DefinitionId;
            DisplayName = x.DisplayName;
            Description = x.Description;
            TraversalCostIncrease = x.TraversalCostIncrease;
            MovementBudgetPenalty = x.MovementBudgetPenalty;
            LogisticsFactor = x.LogisticsFactor;
        }
    }
    public sealed class TraversalEffectSnapshot
    {
        public string EffectId { get; }
        public string? NodeId { get; }
        public string? ConnectionId { get; }
        public long TraversalCostIncrease { get; }
        public long MovementBudgetPenalty { get; }
        public int StartTurn { get; }
        public int EndTurn { get; }
        public TraversalEffectSnapshot(TraversalEffectDto x)
        {
            EffectId = x.EffectId;
            NodeId = x.NodeId;
            ConnectionId = x.ConnectionId;
            TraversalCostIncrease = x.TraversalCostIncrease;
            MovementBudgetPenalty = x.MovementBudgetPenalty;
            StartTurn = x.StartTurn;
            EndTurn = x.EndTurn;
        }
    }
    public sealed class NavigationKnowledgeEntrySnapshot
    {
        public string Id { get; }
        public NavigationKnowledgeLevel Level { get; }
        public NavigationKnowledgeEntrySnapshot(NavigationKnowledgeEntryDto x)
        {
            Id = x.Id;
            Level = x.Level;
        }
    }
    public sealed class CivilizationNavigationKnowledgeSnapshot
    {
        public string CivilizationId { get; }
        public IReadOnlyList<NavigationKnowledgeEntrySnapshot> Nodes { get; }
        public IReadOnlyList<NavigationKnowledgeEntrySnapshot> Connections { get; }
        public CivilizationNavigationKnowledgeSnapshot(CivilizationNavigationKnowledgeDto x)
        {
            CivilizationId = x.CivilizationId;
            Nodes = x.Nodes!.Select(v=>new NavigationKnowledgeEntrySnapshot(v)).ToList().AsReadOnly();
            Connections = x.Connections!.Select(v=>new NavigationKnowledgeEntrySnapshot(v)).ToList().AsReadOnly();
        }
    }
    public sealed class NavigationStatisticsSnapshot
    {
        public long TraversalCostPaid { get; }
        public long EdgesCompleted { get; }
        public long PartialTraversalTurns { get; }
        public NavigationStatisticsSnapshot(NavigationStatisticsDto x)
        {
            TraversalCostPaid = x.TraversalCostPaid;
            EdgesCompleted = x.EdgesCompleted;
            PartialTraversalTurns = x.PartialTraversalTurns;
        }
    }
}
