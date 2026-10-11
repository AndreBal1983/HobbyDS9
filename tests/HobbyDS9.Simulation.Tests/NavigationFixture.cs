using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation.Tests;
internal static class NavigationFixture
{
    internal static DefinitionCatalogDto Definitions(DefinitionCatalogDto d, params string[] systems)
    {
        d.NavigationRegions = systems.Select(id => new NavigationRegionDefinitionDto { NodeId = id, DisplayName = id, AssociatedSystemId = id, PhenomenonDefinitionIds = new() }).ToList();
        d.NavigationConnections = d.GalaxyEdges!.Select((e,i) => new NavigationConnectionDto { ConnectionId = "connection." + i, EndpointA = e.EndpointA, EndpointB = e.EndpointB, Kind = e.Kind ?? GalaxyEdgeKind.Normal, BaseTraversalCost = 1 }).ToList();
        d.Phenomena = new(); return d;
    }
    internal static GameStateDto State(GameStateDto s, DefinitionCatalogDto d)
    {
        foreach(var civilization in s.Civilizations!) {
            if(civilization.Disciplines==null){civilization.Disciplines=ScienceCoreTests.Initial().Disciplines;civilization.ScienceFocus=ScienceDiscipline.Construction;civilization.ResearchDiscoverySeed=0;civilization.EngineeringProgress=new();civilization.Statistics=new();}
        }
        s.ResearchPaceFactor ??= 10000; s.LastResearchReceipts ??= new();
        s.NavigationKnowledge = s.Civilizations!.Select(c => new CivilizationNavigationKnowledgeDto { CivilizationId = c.CivilizationId,
            Nodes = d.NavigationRegions!.Select(n => new NavigationKnowledgeEntryDto { Id=n.NodeId, Level=NavigationKnowledgeLevel.Surveyed }).ToList(),
            Connections = d.NavigationConnections!.Select(e => new NavigationKnowledgeEntryDto { Id=e.ConnectionId, Level=NavigationKnowledgeLevel.Surveyed }).ToList() }).ToList();
        s.TraversalEffects = new(); s.NavigationStatistics = new(); return s;
    }
    internal static SaveEnvelopeDto Save(SaveEnvelopeDto s, DefinitionCatalogDto d) { State(s.GameState,d); return s; }
}
