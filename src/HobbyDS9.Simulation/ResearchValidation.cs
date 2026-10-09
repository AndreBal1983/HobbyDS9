using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public static partial class FoundationValidator
    {
        private static void ValidateResearchDefinitions(DefinitionCatalogDto dto, HashSet<DefinitionId> definitions,
            HashSet<string> typed, string path, List<ValidationIssue> issues)
        {
            if (dto.ResearchNodes == null) { Add(issues, ValidationCode.MissingField, path + ".ResearchNodes"); return; }
            var nodes = dto.ResearchNodes.Where(n => n != null && DefinitionId.TryParse(n.DefinitionId, out _))
                .GroupBy(n => n.DefinitionId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            for (var i = 0; i < dto.ResearchNodes.Count; i++)
            {
                var node = dto.ResearchNodes[i]; var p = path + ".ResearchNodes[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (node == null) { Add(issues, ValidationCode.MissingField, p); continue; }
                if (!DefinitionId.TryParse(node.DefinitionId, out var id)) Add(issues, ValidationCode.InvalidId, p + ".DefinitionId");
                else if (!definitions.Contains(id)) Add(issues, ValidationCode.MissingDefinition, p + ".DefinitionId");
                if (node.DefinitionId != null && !typed.Add(node.DefinitionId)) Add(issues, ValidationCode.DuplicateId, p + ".DefinitionId");
                if (string.IsNullOrWhiteSpace(node.DisplayName)) Add(issues, ValidationCode.MissingField, p + ".DisplayName");
                if (node.ResearchCost <= 0) Add(issues, ValidationCode.InvalidResearchValue, p + ".ResearchCost");
                if (node.PrerequisiteDefinitionIds == null) { Add(issues, ValidationCode.MissingField, p + ".PrerequisiteDefinitionIds"); continue; }
                var unique = new HashSet<string>(StringComparer.Ordinal);
                for (var j = 0; j < node.PrerequisiteDefinitionIds.Count; j++)
                {
                    var prerequisite = node.PrerequisiteDefinitionIds[j]; var item = p + ".PrerequisiteDefinitionIds[" + j.ToString(CultureInfo.InvariantCulture) + "]";
                    if (!DefinitionId.TryParse(prerequisite, out _)) { Add(issues, ValidationCode.InvalidId, item); continue; }
                    if (!unique.Add(prerequisite)) Add(issues, ValidationCode.DuplicateId, item);
                    if (!nodes.ContainsKey(prerequisite)) Add(issues, ValidationCode.MissingDefinition, item);
                    if (prerequisite == node.DefinitionId) Add(issues, ValidationCode.SelfPrerequisite, item);
                }
            }
            // Kahn's algorithm: iterative even for a large/deep content graph.
            var indegrees = nodes.ToDictionary(p => p.Key, p => (p.Value.PrerequisiteDefinitionIds ?? new List<string>())
                .Where(id => id != null && nodes.ContainsKey(id)).Distinct(StringComparer.Ordinal).Count(), StringComparer.Ordinal);
            var dependents = nodes.Keys.ToDictionary(id => id, _ => new SortedSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
            foreach (var pair in nodes.OrderBy(p => p.Key, StringComparer.Ordinal))
            foreach (var prerequisite in (pair.Value.PrerequisiteDefinitionIds ?? new List<string>()).Where(id => id != null && nodes.ContainsKey(id)).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal))
                dependents[prerequisite].Add(pair.Key);
            var ready = new SortedSet<string>(indegrees.Where(p => p.Value == 0).Select(p => p.Key), StringComparer.Ordinal);
            var visited = 0;
            while (ready.Count != 0)
            {
                var next = ready.Min!; ready.Remove(next); visited++;
                foreach (var dependent in dependents[next]) if (--indegrees[dependent] == 0) ready.Add(dependent);
            }
            if (visited != nodes.Count) Add(issues, ValidationCode.ResearchCycle, path + ".ResearchNodes");
        }

        private static void ValidateResearchState(GameStateDto dto, DefinitionCatalogDto? catalog, string path, List<ValidationIssue> issues)
        {
            var nodes = (catalog?.ResearchNodes ?? new List<ResearchNodeDefinitionDto>()).Where(n => n != null && DefinitionId.TryParse(n.DefinitionId, out _))
                .GroupBy(n => n.DefinitionId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            if (dto.Civilizations == null) return;
            for (var i = 0; i < dto.Civilizations.Count; i++)
            {
                var civ = dto.Civilizations[i]; var p = path + ".Civilizations[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (civ == null) continue;
                if (civ.ResearchReserve < 0) Add(issues, ValidationCode.InvalidResearchValue, p + ".ResearchReserve");
                var unlocked = new HashSet<string>(StringComparer.Ordinal);
                if (civ.UnlockedResearchDefinitionIds == null) Add(issues, ValidationCode.MissingField, p + ".UnlockedResearchDefinitionIds");
                else for (var j = 0; j < civ.UnlockedResearchDefinitionIds.Count; j++)
                {
                    var id = civ.UnlockedResearchDefinitionIds[j]; var item = p + ".UnlockedResearchDefinitionIds[" + j.ToString(CultureInfo.InvariantCulture) + "]";
                    if (!DefinitionId.TryParse(id, out _)) { Add(issues, ValidationCode.InvalidId, item); continue; }
                    if (!unlocked.Add(id)) Add(issues, ValidationCode.DuplicateId, item);
                    if (!nodes.ContainsKey(id)) Add(issues, ValidationCode.MissingDefinition, item);
                }
                if (civ.ResearchTargetDefinitionId == null) continue;
                var target = civ.ResearchTargetDefinitionId;
                if (!DefinitionId.TryParse(target, out _)) Add(issues, ValidationCode.InvalidId, p + ".ResearchTargetDefinitionId");
                else if (!nodes.TryGetValue(target, out var node)) Add(issues, ValidationCode.MissingDefinition, p + ".ResearchTargetDefinitionId");
                else
                {
                    if (unlocked.Contains(target)) Add(issues, ValidationCode.ResearchAlreadyUnlocked, p + ".ResearchTargetDefinitionId");
                    if (node.PrerequisiteDefinitionIds != null && node.PrerequisiteDefinitionIds.Any(id => id == null || !unlocked.Contains(id)))
                        Add(issues, ValidationCode.ResearchPrerequisiteMissing, p + ".ResearchTargetDefinitionId");
                }
            }
        }
    }
}
