using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public static partial class FoundationValidator
    {
        private static void ValidateEconomicProfiles(DefinitionCatalogDto dto, HashSet<DefinitionId> ids,
            HashSet<string> typed, string path, List<ValidationIssue> issues)
        {
            if (dto.EconomicProfiles == null) { Add(issues, ValidationCode.MissingField, path + ".EconomicProfiles"); return; }
            for (var i = 0; i < dto.EconomicProfiles.Count; i++)
            {
                var profile = dto.EconomicProfiles[i]; var p = path + ".EconomicProfiles[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (profile == null) { Add(issues, ValidationCode.MissingField, p); continue; }
                if (!DefinitionId.TryParse(profile.DefinitionId, out var id)) Add(issues, ValidationCode.InvalidId, p + ".DefinitionId");
                else if (!ids.Contains(id)) Add(issues, ValidationCode.MissingDefinition, p + ".DefinitionId");
                if (profile.DefinitionId != null && !typed.Add(profile.DefinitionId)) Add(issues, ValidationCode.DuplicateId, p + ".DefinitionId");
                if (string.IsNullOrWhiteSpace(profile.DisplayName)) Add(issues, ValidationCode.MissingField, p + ".DisplayName");
                void Nonnegative(long value, string field) { if (value < 0) Add(issues, ValidationCode.InvalidEconomicValue, p + "." + field); }
                void Positive(long value, string field) { if (value <= 0) Add(issues, ValidationCode.InvalidEconomicValue, p + "." + field); }
                Nonnegative(profile.FoodOutput, nameof(profile.FoodOutput));
                Nonnegative(profile.MaterialOutput, nameof(profile.MaterialOutput));
                Nonnegative(profile.BaseIndustry, nameof(profile.BaseIndustry));
                Nonnegative(profile.GrowthPerTurn, nameof(profile.GrowthPerTurn));
                Nonnegative(profile.StarvationLossPerTurn, nameof(profile.StarvationLossPerTurn));
                Nonnegative(profile.ResearchOutput, nameof(profile.ResearchOutput));
                Positive(profile.PopulationPerFood, nameof(profile.PopulationPerFood));
                Positive(profile.PopulationPerCredit, nameof(profile.PopulationPerCredit));
                Positive(profile.PopulationPerIndustry, nameof(profile.PopulationPerIndustry));
            }
        }

        private static void ValidateColonies(GameStateDto state, DefinitionCatalogDto? catalog,
            Dictionary<string, EntityIdentityDto> headers, HashSet<string> typedIds, HashSet<string> civilizations,
            string path, List<ValidationIssue> issues)
        {
            if (state.Colonies == null) { Add(issues, ValidationCode.MissingField, path + ".Colonies"); return; }
            var profiles = new HashSet<string>(catalog?.EconomicProfiles?.Where(p => p != null).Select(p => p.DefinitionId) ?? Array.Empty<string>(), StringComparer.Ordinal);
            var systems = (state.StarSystems ?? new List<StarSystemStateDto>()).Where(n => n != null && n.SystemId != null)
                .GroupBy(n => n.SystemId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            var occupied = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < state.Colonies.Count; i++)
            {
                var colony = state.Colonies[i]; var p = path + ".Colonies[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (colony == null) { Add(issues, ValidationCode.MissingField, p); continue; }
                if (!EntityId.TryParse(colony.ColonyId, out _)) Add(issues, ValidationCode.InvalidId, p + ".ColonyId");
                else
                {
                    if (!typedIds.Add(colony.ColonyId)) Add(issues, ValidationCode.DuplicateId, p + ".ColonyId");
                    if (!headers.TryGetValue(colony.ColonyId, out var header)) Add(issues, ValidationCode.MissingEntity, p + ".ColonyId");
                    else if (header.DefinitionId != colony.EconomicProfileDefinitionId) Add(issues, ValidationCode.IdentityMismatch, p + ".ColonyId");
                }
                if (!DefinitionId.TryParse(colony.EconomicProfileDefinitionId, out _)) Add(issues, ValidationCode.InvalidId, p + ".EconomicProfileDefinitionId");
                else if (!profiles.Contains(colony.EconomicProfileDefinitionId)) Add(issues, ValidationCode.MissingDefinition, p + ".EconomicProfileDefinitionId");
                if (!EntityId.TryParse(colony.SystemId, out _)) Add(issues, ValidationCode.InvalidId, p + ".SystemId");
                else
                {
                    if (!occupied.Add(colony.SystemId)) Add(issues, ValidationCode.DuplicateColonySystem, p + ".SystemId");
                    if (!systems.TryGetValue(colony.SystemId, out var system)) Add(issues, ValidationCode.MissingSystem, p + ".SystemId");
                    else if (system.OwnerCivilizationId != colony.OwnerCivilizationId) Add(issues, ValidationCode.OwnershipMismatch, p + ".OwnerCivilizationId");
                }
                if (colony.OwnerCivilizationId != null)
                {
                    if (!EntityId.TryParse(colony.OwnerCivilizationId, out _)) Add(issues, ValidationCode.InvalidId, p + ".OwnerCivilizationId");
                    else if (!civilizations.Contains(colony.OwnerCivilizationId)) Add(issues, ValidationCode.MissingCivilization, p + ".OwnerCivilizationId");
                }
                if (colony.Population < 1 || colony.Population > colony.PopulationCapacity) Add(issues, ValidationCode.InvalidEconomicValue, p + ".Population");
                if (colony.PopulationCapacity <= 0) Add(issues, ValidationCode.InvalidEconomicValue, p + ".PopulationCapacity");
                if (colony.Food < 0) Add(issues, ValidationCode.InvalidEconomicValue, p + ".Food");
                if (colony.Materials < 0) Add(issues, ValidationCode.InvalidEconomicValue, p + ".Materials");
            }
        }
    }
}
