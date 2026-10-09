using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public static partial class FoundationValidator
    {
        private static void ValidateTypedDefinitions(DefinitionCatalogDto dto, HashSet<DefinitionId> ids,
            string path, List<ValidationIssue> issues)
        {
            if (dto.ContentFormatVersion != FoundationVersions.ContentFormatVersion)
                Add(issues, ValidationCode.UnsupportedContentFormat, path + ".ContentFormatVersion");
            var typed = new HashSet<string>(StringComparer.Ordinal);
            void Definition(string? id, string? label, string itemPath)
            {
                if (!DefinitionId.TryParse(id, out var parsed)) Add(issues, ValidationCode.InvalidId, itemPath + ".DefinitionId");
                else if (!ids.Contains(parsed)) Add(issues, ValidationCode.MissingDefinition, itemPath + ".DefinitionId");
                if (id != null && !typed.Add(id)) Add(issues, ValidationCode.DuplicateId, itemPath + ".DefinitionId");
                if (string.IsNullOrWhiteSpace(label)) Add(issues, ValidationCode.MissingField, itemPath + ".DisplayName");
            }
            if (dto.Civilizations == null) Add(issues, ValidationCode.MissingField, path + ".Civilizations");
            else for (var i = 0; i < dto.Civilizations.Count; i++)
            {
                var item = dto.Civilizations[i]; var p = path + ".Civilizations[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
                if (item == null) Add(issues, ValidationCode.MissingField, p);
                else Definition(item.DefinitionId, item.DisplayName, p);
            }
            if (dto.StarSystems == null) Add(issues, ValidationCode.MissingField, path + ".StarSystems");
            else for (var i = 0; i < dto.StarSystems.Count; i++)
            {
                var item = dto.StarSystems[i]; var p = path + ".StarSystems[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
                if (item == null) Add(issues, ValidationCode.MissingField, p);
                else Definition(item.DefinitionId, item.DisplayName, p);
            }
            ValidateEconomicProfiles(dto, ids, typed, path, issues);
            ValidateResearchDefinitions(dto, ids, typed, path, issues);
            if (dto.GalaxyEdges == null) Add(issues, ValidationCode.MissingField, path + ".GalaxyEdges");
        }

        private static void ValidateTypedState(GameStateDto? dto, DefinitionCatalogDto? catalog,
            string path, List<ValidationIssue> issues)
        {
            if (dto == null) return;
            var civDefinitions = new HashSet<string>(catalog?.Civilizations?.Where(c => c != null).Select(c => c.DefinitionId) ?? Array.Empty<string>(), StringComparer.Ordinal);
            var systemDefinitions = new HashSet<string>(catalog?.StarSystems?.Where(c => c != null).Select(c => c.DefinitionId) ?? Array.Empty<string>(), StringComparer.Ordinal);
            var headers = (dto.EntityIdentities ?? new List<EntityIdentityDto>()).Where(e => e != null && EntityId.TryParse(e.EntityId, out _))
                .GroupBy(e => e.EntityId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            var typedIds = new HashSet<string>(StringComparer.Ordinal);
            void Identity(string? id, string? definition, HashSet<string> definitions, string p)
            {
                if (!EntityId.TryParse(id, out _)) Add(issues, ValidationCode.InvalidId, p);
                else
                {
                    if (!typedIds.Add(id!)) Add(issues, ValidationCode.DuplicateId, p);
                    if (!headers.TryGetValue(id!, out var header)) Add(issues, ValidationCode.MissingEntity, p);
                    else if (header.DefinitionId != definition) Add(issues, ValidationCode.IdentityMismatch, p);
                }
                if (!DefinitionId.TryParse(definition, out _)) Add(issues, ValidationCode.InvalidId, p + ".DefinitionId");
                else if (!definitions.Contains(definition!)) Add(issues, ValidationCode.MissingDefinition, p + ".DefinitionId");
            }
            var civilizationIds = new HashSet<string>(StringComparer.Ordinal);
            if (dto.Civilizations == null) Add(issues, ValidationCode.MissingField, path + ".Civilizations");
            else for (var i = 0; i < dto.Civilizations.Count; i++)
            {
                var civ = dto.Civilizations[i]; var p = path + ".Civilizations[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
                if (civ == null) { Add(issues, ValidationCode.MissingField, p); continue; }
                Identity(civ.CivilizationId, civ.DefinitionId, civDefinitions, p);
                if (civ.Credits < 0) Add(issues, ValidationCode.InvalidEconomicValue, p + ".Credits");
                if (civ.CivilizationId != null) civilizationIds.Add(civ.CivilizationId);
                if (!civ.ControllerKind.HasValue) Add(issues, ValidationCode.MissingField, p + ".ControllerKind");
                else if (civ.ControllerKind != ControllerKind.Human && civ.ControllerKind != ControllerKind.AI)
                    Add(issues, ValidationCode.InvalidControllerKind, p + ".ControllerKind");
            }
            if (dto.StarSystems == null) Add(issues, ValidationCode.MissingField, path + ".StarSystems");
            else for (var i = 0; i < dto.StarSystems.Count; i++)
            {
                var system = dto.StarSystems[i]; var p = path + ".StarSystems[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
                if (system == null) { Add(issues, ValidationCode.MissingField, p); continue; }
                Identity(system.SystemId, system.DefinitionId, systemDefinitions, p);
                if (system.OwnerCivilizationId != null)
                {
                    if (!EntityId.TryParse(system.OwnerCivilizationId, out _)) Add(issues, ValidationCode.InvalidId, p + ".OwnerCivilizationId");
                    else if (!civilizationIds.Contains(system.OwnerCivilizationId)) Add(issues, ValidationCode.MissingCivilization, p + ".OwnerCivilizationId");
                }
            }
            ValidateColonies(dto, catalog, headers, typedIds, civilizationIds, path, issues);
            ValidateResearchState(dto, catalog, path, issues);
            var graphReport = GalaxyGraph.Validate(dto.StarSystems?.Select(s => s?.SystemId ?? string.Empty), catalog?.GalaxyEdges);
            foreach (var issue in graphReport.Issues)
                Add(issues, issue.Code, issue.Path.StartsWith("$.Edges", StringComparison.Ordinal)
                    ? "$.Definitions.GalaxyEdges" + issue.Path.Substring(7) : path + ".StarSystems");
        }
    }
}
