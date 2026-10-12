using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Simulation
{
    internal static class CampaignBlueprints
    {
        internal static ShipDesignAccessDto Access(DefinitionCatalogDto content, CivilizationStateDto owner) => new ShipDesignAccessDto
        {
            AvailableVersionIds = content.ShipDesignVersionAccess!.Where(g => g.RequiredEngineeringApplicationDefinitionIds!
                .All(id => owner.MatureEngineeringApplicationDefinitionIds!.Contains(id, StringComparer.Ordinal)))
                .Select(g => g.VersionId).OrderBy(id => id, StringComparer.Ordinal).ToList()
        };

        internal static ShipDesignCompiler Compiler(ShipDesignCatalogueDto catalogue) => new ShipDesignCompiler(new ShipDesignCatalogueSnapshot(catalogue));
        internal static ShipDesignAccessDto FrozenAccess(ShipDesignCatalogueDto catalogue) => new ShipDesignAccessDto
        { AvailableVersionIds = catalogue.Hulls!.Select(h => h.VersionId).Concat(catalogue.Components!.Select(c => c.VersionId)).OrderBy(id => id, StringComparer.Ordinal).ToList() };

        internal static ShipDesignDefinitionDto Design(PublishedShipBlueprint blueprint)
        {
            var performance = blueprint.Performance.Compiled!; var cost = blueprint.Performance.Cost!;
            return new ShipDesignDefinitionDto
            {
                DefinitionId = blueprint.BlueprintId, DisplayName = blueprint.Fit.DisplayName,
                MovementBudget = performance.MovementBudget, MaxHull = performance.MaxHull,
                CreditCost = cost.Credits, IndustryCost = cost.Industry,
                ResourceCosts = cost.Resources.Select(r => new ResourceAmountDto { ResourceDefinitionId = r.ResourceDefinitionId, Amount = r.Amount }).ToList(),
                RequiredEngineeringApplicationDefinitionIds = new List<string>(),
                CombatProfile = new ShipCombatProfileDto
                { ShieldCapacity = performance.Combat.ShieldCapacity, LongRangeAttack = performance.Combat.LongRangeAttack,
                    MediumRangeAttack = performance.Combat.MediumRangeAttack, CloseRangeAttack = performance.Combat.CloseRangeAttack,
                    AccuracyBps = performance.Combat.AccuracyBps, EvasionBps = performance.Combat.EvasionBps }
            };
        }

        internal static PublishedShipBlueprint Restore(BlueprintRevisionDto record) => Compiler(record.FrozenCatalogue!)
            .Publish(record.Fit, FrozenAccess(record.FrozenCatalogue!), record.BlueprintId, record.Revision).Blueprint!;

        internal static BlueprintRevisionDto? Find(GameStateDto state, BlueprintReferenceDto reference) => state.Blueprints!
            .SingleOrDefault(b => b.BlueprintId == reference.BlueprintId && b.Revision == reference.Revision);

        internal static CommandErrorCode Eligibility(GameStateDto state, DefinitionCatalogDto definitions, string actor, string colonyId, BlueprintRevisionDto? blueprint)
        {
            if (blueprint == null) return CommandErrorCode.UnknownBlueprint;
            if (blueprint.OwnerCivilizationId != actor) return CommandErrorCode.BlueprintNotOwned;
            var colony = state.Colonies!.SingleOrDefault(c => c.ColonyId == colonyId);
            if (colony == null) return CommandErrorCode.UnknownColony;
            if (colony.OwnerCivilizationId != actor) return CommandErrorCode.ColonyNotOwned;
            var owner = state.Civilizations!.Single(c => c.CivilizationId == actor);
            if (owner.ControllerKind != ControllerKind.Human) return CommandErrorCode.ActorNotControlled;
            if (state.Phase != SessionPhase.Planning) return CommandErrorCode.CombatPending;
            if (!WorldEconomy.Infrastructure(colony, definitions).Any(i => i.definition.Shipbuilding)) return CommandErrorCode.ShipbuildingUnavailable;
            var design = blueprint.CompiledDesign!;
            if (owner.Credits < design.CreditCost) return CommandErrorCode.InsufficientCredits;
            if (design.ResourceCosts!.Any(r => PopulationRules.Stock(colony.Stockpiles!, r.ResourceDefinitionId) < r.Amount)) return CommandErrorCode.InsufficientStrategicResource;
            return CommandErrorCode.None;
        }
    }

    // The only gameplay lookup for ship sources. Published resolution is always exact revision.
    internal static class CampaignShipDesignResolver
    {
        internal static ShipDesignDefinitionDto? Resolve(GameStateDto state, DefinitionCatalogDto? content, string staticId, BlueprintReferenceDto? reference)
        {
            if (reference == null) return content?.ShipDesigns?.FirstOrDefault(d => d != null && d.DefinitionId == staticId);
            if (staticId != string.Empty || state.Blueprints == null) return null;
            return state.Blueprints.FirstOrDefault(b => b != null && b.BlueprintId == reference.BlueprintId && b.Revision == reference.Revision)?.CompiledDesign;
        }
        internal static ShipDesignDefinitionDto Ship(GameStateDto state, DefinitionCatalogDto content, ShipStateDto ship) =>
            Resolve(state, content, ship.ShipDesignDefinitionId, ship.Blueprint) ?? throw new InvalidOperationException("Unresolvable ship provenance.");
        internal static ShipDesignDefinitionDto Queue(GameStateDto state, DefinitionCatalogDto content, ProductionQueueItemDto item) =>
            Resolve(state, content, item.ShipDesignDefinitionId, item.Blueprint) ?? throw new InvalidOperationException("Unresolvable production provenance.");
    }

    internal sealed class PublishShipBlueprintHandler : ICommandHandler
    {
        private readonly DefinitionCatalogDto definitions;
        private readonly SaveCompatibility compatibility;
        internal PublishShipBlueprintHandler(DefinitionCatalogDto definitions, SaveCompatibility compatibility)
        { this.definitions = definitions; this.compatibility = compatibility; }
        public CommandErrorCode Execute(GameStateDto state, CommandEnvelope command)
        {
            var payload = command.PublishShipBlueprint!;
            if (definitions.ShipDesignCatalogue == null) return CommandErrorCode.ShipDesignCatalogueUnavailable;
            var history = state.Blueprints!.Where(b => b.BlueprintId == payload.BlueprintId).ToList();
            if (history.Any(b => b.OwnerCivilizationId != command.ActorCivilizationId)) return CommandErrorCode.BlueprintNotOwned;
            var latest = history.Select(b => b.Revision).DefaultIfEmpty(0).Max();
            if (latest == int.MaxValue || payload.Revision != latest + 1) return CommandErrorCode.BlueprintRevisionConflict;
            if (history.Any(b => b.Fit!.ClassId != payload.Draft!.ClassId) || definitions.DefinitionIds.Contains(payload.BlueprintId, StringComparer.Ordinal)) return CommandErrorCode.InvalidShipBlueprint;
            var owner = state.Civilizations!.Single(c => c.CivilizationId == command.ActorCivilizationId);
            var publication = CampaignBlueprints.Compiler(definitions.ShipDesignCatalogue).Publish(payload.Draft,
                CampaignBlueprints.Access(definitions, owner), payload.BlueprintId, payload.Revision);
            if (!publication.Published) return publication.Preview.Issues.Any(i => i.Code == DesignIssueCode.VersionUnavailable)
                ? CommandErrorCode.ShipDesignVersionUnavailable : CommandErrorCode.InvalidShipBlueprint;
            var blueprint = publication.Blueprint!;
            var cost = blueprint.Performance.Cost!;
            state.Blueprints!.Add(new BlueprintRevisionDto
            {
                OwnerCivilizationId = command.ActorCivilizationId, BlueprintId = payload.BlueprintId, Revision = payload.Revision,
                CompilerVersion = ShipDesignVersions.Compiler, RulesVersion = compatibility.RulesVersion, SourceContentHash = compatibility.ContentHash,
                FrozenCatalogue = BlueprintCopies.Copy(definitions.ShipDesignCatalogue), Fit = BlueprintCopies.Copy(payload.Draft!),
                CompiledDesign = CampaignBlueprints.Design(blueprint),
                Cost = new ShipDesignCostDto { Credits = cost.Credits, Industry = cost.Industry, Maintenance = cost.Maintenance,
                    Resources = cost.Resources.Select(r => new ResourceAmountDto { ResourceDefinitionId = r.ResourceDefinitionId, Amount = r.Amount }).ToList() }
            });
            return CommandErrorCode.None;
        }
    }

    internal sealed class EnqueueBlueprintConstructionHandler : ICommandHandler
    {
        private readonly DefinitionCatalogDto definitions;
        internal EnqueueBlueprintConstructionHandler(DefinitionCatalogDto definitions) { this.definitions = definitions; }
        public CommandErrorCode Execute(GameStateDto state, CommandEnvelope command)
        {
            var payload = command.EnqueueBlueprintConstruction!;
            var reference = new BlueprintReferenceDto { BlueprintId = payload.BlueprintId, Revision = payload.Revision };
            var blueprint = CampaignBlueprints.Find(state, reference);
            var error = CampaignBlueprints.Eligibility(state, definitions, command.ActorCivilizationId, payload.ColonyId, blueprint);
            if (error != CommandErrorCode.None) return error;
            var colony = state.Colonies!.Single(c => c.ColonyId == payload.ColonyId);
            var owner = state.Civilizations!.Single(c => c.CivilizationId == command.ActorCivilizationId);
            var design = blueprint!.CompiledDesign!;
            var id = CampaignAllocation.Allocate(state, "queue-item", null);
            owner.Credits -= design.CreditCost;
            foreach (var cost in design.ResourceCosts!) PopulationRules.AddStock(colony.Stockpiles!, cost.ResourceDefinitionId, -cost.Amount);
            colony.ProductionQueue!.Add(new ProductionQueueItemDto { QueueItemId = id, Blueprint = reference,
                RequiredIndustry = design.IndustryCost, PaidCredits = design.CreditCost,
                PaidResources = design.ResourceCosts.Select(PopulationData.Copy).ToList() });
            return CommandErrorCode.None;
        }
    }
}
