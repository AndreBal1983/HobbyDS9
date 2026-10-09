using System;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class ResearchPhase
    {
        internal static void Execute(GameStateDto staging, DefinitionCatalogDto definitions)
        {
            var profiles = definitions.EconomicProfiles!.ToDictionary(p => p.DefinitionId, StringComparer.Ordinal);
            var nodes = definitions.ResearchNodes!.ToDictionary(n => n.DefinitionId, StringComparer.Ordinal);
            var colonies = staging.Colonies!.OrderBy(c => c.ColonyId, StringComparer.Ordinal).ToList();
            foreach (var civilization in staging.Civilizations!.OrderBy(c => c.CivilizationId, StringComparer.Ordinal))
            {
                checked
                {
                    long output = 0;
                    foreach (var colony in colonies)
                        if (colony.OwnerCivilizationId == civilization.CivilizationId) output += profiles[colony.EconomicProfileDefinitionId].ResearchOutput;
                    civilization.ResearchReserve += output;
                    if (civilization.ResearchTargetDefinitionId == null) continue;
                    var node = nodes[civilization.ResearchTargetDefinitionId];
                    if (civilization.ResearchReserve < node.ResearchCost) continue;
                    civilization.ResearchReserve -= node.ResearchCost;
                    civilization.UnlockedResearchDefinitionIds!.Add(node.DefinitionId);
                    civilization.UnlockedResearchDefinitionIds.Sort(StringComparer.Ordinal);
                    civilization.ResearchTargetDefinitionId = null;
                }
            }
        }
    }
}
