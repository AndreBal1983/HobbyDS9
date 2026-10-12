using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Simulation
{
    public static partial class FoundationValidator
    {
        private static void ValidateBlueprintCatalogue(DefinitionCatalogDto content, string path, List<ValidationIssue> issues)
        {
            var gates = content.ShipDesignVersionAccess;
            if (gates == null) { Add(issues, ValidationCode.MissingField, path + ".ShipDesignVersionAccess"); return; }
            if (content.ShipDesignCatalogue == null)
            { if (gates.Count > 0) Add(issues, ValidationCode.MissingDefinition, path + ".ShipDesignCatalogue"); return; }
            if (ShipDesignCatalogueValidation.Validate(content.ShipDesignCatalogue).Count > 0)
            { Add(issues, ValidationCode.InvalidShipDesignCatalogue, path + ".ShipDesignCatalogue"); return; }
            var catalogue = content.ShipDesignCatalogue;
            var versions = catalogue.Hulls!.Select(h => h.VersionId).Concat(catalogue.Components!.Select(c => c.VersionId)).OrderBy(id => id, StringComparer.Ordinal);
            if (gates.Any(g => g == null) || !versions.SequenceEqual(gates.Where(g => g != null).Select(g => g.VersionId).OrderBy(id => id, StringComparer.Ordinal)))
                Add(issues, ValidationCode.MissingDefinition, path + ".ShipDesignVersionAccess");
            var engineering = (content.EngineeringApplications ?? new List<EngineeringApplicationDefinitionDto>()).Where(e => e != null).Select(e => e.DefinitionId).ToHashSet(StringComparer.Ordinal);
            foreach (var gate in gates.Where(g => g != null))
                if (gate.RequiredEngineeringApplicationDefinitionIds == null || gate.RequiredEngineeringApplicationDefinitionIds.Distinct(StringComparer.Ordinal).Count() != gate.RequiredEngineeringApplicationDefinitionIds.Count
                    || gate.RequiredEngineeringApplicationDefinitionIds.Any(id => !engineering.Contains(id)))
                    Add(issues, ValidationCode.MissingDefinition, path + ".ShipDesignVersionAccess." + gate.VersionId);
            var resources = (content.Resources ?? new List<ResourceDefinitionDto>()).Where(r => r != null).Select(r => r.DefinitionId).ToHashSet(StringComparer.Ordinal);
            if (catalogue.Hulls.SelectMany(h => h.Cost!.Resources!).Concat(catalogue.Components.SelectMany(c => c.Cost!.Resources!)).Any(r => !resources.Contains(r.ResourceDefinitionId)))
                Add(issues, ValidationCode.MissingDefinition, path + ".ShipDesignCatalogue.Resources");
        }

        private static void ValidateBlueprintRegistry(GameStateDto state, DefinitionCatalogDto? content, string path, List<ValidationIssue> issues)
        {
            path += ".Blueprints";
            if (state.Blueprints == null) { Add(issues, ValidationCode.MissingField, path); return; }
            var keys = new HashSet<(string, int)>();
            foreach (var record in state.Blueprints)
            {
                if (record == null) { Add(issues, ValidationCode.MissingField, path); continue; }
                var p = path + "." + record.BlueprintId + "." + record.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (!DefinitionId.TryParse(record.BlueprintId, out _) || record.Revision <= 0 || !keys.Add((record.BlueprintId, record.Revision)))
                    Add(issues, ValidationCode.DuplicateId, p);
                if (state.Civilizations?.Any(c => c != null && c.CivilizationId == record.OwnerCivilizationId) != true)
                    Add(issues, ValidationCode.MissingCivilization, p);
                if (content?.DefinitionIds.Contains(record.BlueprintId) == true || record.CompilerVersion != ShipDesignVersions.Compiler
                    || string.IsNullOrWhiteSpace(record.RulesVersion) || !IsContentHash(record.SourceContentHash))
                    Add(issues, ValidationCode.BlueprintIdentityMismatch, p);
                if (record.Fit == null || record.Cost == null || record.CompiledDesign == null || ShipDesignCatalogueValidation.Validate(record.FrozenCatalogue).Count > 0)
                { Add(issues, ValidationCode.InvalidBlueprint, p); continue; }
                var publication = CampaignBlueprints.Compiler(record.FrozenCatalogue!).Publish(record.Fit,
                    CampaignBlueprints.FrozenAccess(record.FrozenCatalogue!), record.BlueprintId, Math.Max(1, record.Revision));
                if (!publication.Published) { Add(issues, ValidationCode.InvalidBlueprint, p); continue; }
                var expected = CampaignBlueprints.Design(publication.Blueprint!);
                var cost = publication.Blueprint!.Performance.Cost!;
                if (expected.MovementBudget <= 0 || !BlueprintCopies.Same(BlueprintCopies.Copy(expected), BlueprintCopies.Copy(record.CompiledDesign))
                    || cost.Credits != record.Cost.Credits || cost.Industry != record.Cost.Industry || cost.Maintenance != record.Cost.Maintenance
                    || !SameResources(record.Cost.Resources, expected.ResourceCosts))
                    Add(issues, ValidationCode.InvalidBlueprint, p);
                ValidateResources(record.Cost.Resources, content, p + ".Resources", issues);
            }
            foreach (var history in state.Blueprints.Where(b => b != null).GroupBy(b => b.BlueprintId, StringComparer.Ordinal))
            {
                var revisions = history.OrderBy(b => b.Revision).ToList();
                if (!revisions.Select(b => b.Revision).SequenceEqual(Enumerable.Range(1, revisions.Count))
                    || revisions.Any(b => b.OwnerCivilizationId != revisions[0].OwnerCivilizationId || b.Fit?.ClassId != revisions[0].Fit?.ClassId))
                    Add(issues, ValidationCode.InvalidCounter, path + "." + history.Key);
            }
        }
    }
}
