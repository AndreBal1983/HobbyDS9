using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static partial class PopulationData
    {
        internal static NavigationRegionDefinitionDto Copy(NavigationRegionDefinitionDto x) => new NavigationRegionDefinitionDto
        {
            NodeId = x.NodeId,
            DisplayName = x.DisplayName,
            AssociatedSystemId = x.AssociatedSystemId,
            PhenomenonDefinitionIds = x.PhenomenonDefinitionIds!.OrderBy(id=>id,System.StringComparer.Ordinal).ToList(),
        };
        internal static bool Same(NavigationRegionDefinitionDto? a,NavigationRegionDefinitionDto? b) => a==null ? b==null : b!=null && a.NodeId==b.NodeId
            && a.DisplayName==b.DisplayName
            && a.AssociatedSystemId==b.AssociatedSystemId
            && (a.PhenomenonDefinitionIds==null ? b.PhenomenonDefinitionIds==null : b.PhenomenonDefinitionIds!=null && a.PhenomenonDefinitionIds.Count==b.PhenomenonDefinitionIds.Count && a.PhenomenonDefinitionIds.SequenceEqual(b.PhenomenonDefinitionIds));
        internal static NavigationConnectionDto Copy(NavigationConnectionDto x) => new NavigationConnectionDto
        {
            ConnectionId = x.ConnectionId,
            EndpointA = System.StringComparer.Ordinal.Compare(x.EndpointA,x.EndpointB)<0?x.EndpointA:x.EndpointB,
            EndpointB = System.StringComparer.Ordinal.Compare(x.EndpointA,x.EndpointB)<0?x.EndpointB:x.EndpointA,
            Kind = x.Kind,
            BaseTraversalCost = x.BaseTraversalCost,
        };
        internal static bool Same(NavigationConnectionDto? a,NavigationConnectionDto? b) => a==null ? b==null : b!=null && a.ConnectionId==b.ConnectionId
            && a.EndpointA==b.EndpointA
            && a.EndpointB==b.EndpointB
            && a.Kind==b.Kind
            && a.BaseTraversalCost==b.BaseTraversalCost;
        internal static PhenomenonDefinitionDto Copy(PhenomenonDefinitionDto x) => new PhenomenonDefinitionDto
        {
            DefinitionId = x.DefinitionId,
            DisplayName = x.DisplayName,
            Description = x.Description,
            TraversalCostIncrease = x.TraversalCostIncrease,
            MovementBudgetPenalty = x.MovementBudgetPenalty,
            LogisticsFactor = x.LogisticsFactor,
        };
        internal static bool Same(PhenomenonDefinitionDto? a,PhenomenonDefinitionDto? b) => a==null ? b==null : b!=null && a.DefinitionId==b.DefinitionId
            && a.DisplayName==b.DisplayName
            && a.Description==b.Description
            && a.TraversalCostIncrease==b.TraversalCostIncrease
            && a.MovementBudgetPenalty==b.MovementBudgetPenalty
            && a.LogisticsFactor==b.LogisticsFactor;
        internal static TraversalEffectDto Copy(TraversalEffectDto x) => new TraversalEffectDto
        {
            EffectId = x.EffectId,
            NodeId = x.NodeId,
            ConnectionId = x.ConnectionId,
            TraversalCostIncrease = x.TraversalCostIncrease,
            MovementBudgetPenalty = x.MovementBudgetPenalty,
            StartTurn = x.StartTurn,
            EndTurn = x.EndTurn,
        };
        internal static bool Same(TraversalEffectDto? a,TraversalEffectDto? b) => a==null ? b==null : b!=null && a.EffectId==b.EffectId
            && a.NodeId==b.NodeId
            && a.ConnectionId==b.ConnectionId
            && a.TraversalCostIncrease==b.TraversalCostIncrease
            && a.MovementBudgetPenalty==b.MovementBudgetPenalty
            && a.StartTurn==b.StartTurn
            && a.EndTurn==b.EndTurn;
        internal static NavigationKnowledgeEntryDto Copy(NavigationKnowledgeEntryDto x) => new NavigationKnowledgeEntryDto
        {
            Id = x.Id,
            Level = x.Level,
        };
        internal static bool Same(NavigationKnowledgeEntryDto? a,NavigationKnowledgeEntryDto? b) => a==null ? b==null : b!=null && a.Id==b.Id
            && a.Level==b.Level;
        internal static CivilizationNavigationKnowledgeDto Copy(CivilizationNavigationKnowledgeDto x) => new CivilizationNavigationKnowledgeDto
        {
            CivilizationId = x.CivilizationId,
            Nodes = x.Nodes!.OrderBy(n=>n.Id,System.StringComparer.Ordinal).Select(Copy).ToList(),
            Connections = x.Connections!.OrderBy(n=>n.Id,System.StringComparer.Ordinal).Select(Copy).ToList(),
        };
        internal static bool Same(CivilizationNavigationKnowledgeDto? a,CivilizationNavigationKnowledgeDto? b) => a==null ? b==null : b!=null && a.CivilizationId==b.CivilizationId
            && (a.Nodes==null ? b.Nodes==null : b.Nodes!=null && a.Nodes.Count==b.Nodes.Count && a.Nodes.Zip(b.Nodes,(x,y)=>Same(x,y)).All(v=>v))
            && (a.Connections==null ? b.Connections==null : b.Connections!=null && a.Connections.Count==b.Connections.Count && a.Connections.Zip(b.Connections,(x,y)=>Same(x,y)).All(v=>v));
        internal static NavigationStatisticsDto Copy(NavigationStatisticsDto x) => new NavigationStatisticsDto
        {
            TraversalCostPaid = x.TraversalCostPaid,
            EdgesCompleted = x.EdgesCompleted,
            PartialTraversalTurns = x.PartialTraversalTurns,
        };
        internal static bool Same(NavigationStatisticsDto? a,NavigationStatisticsDto? b) => a==null ? b==null : b!=null && a.TraversalCostPaid==b.TraversalCostPaid
            && a.EdgesCompleted==b.EdgesCompleted
            && a.PartialTraversalTurns==b.PartialTraversalTurns;
    }
}
