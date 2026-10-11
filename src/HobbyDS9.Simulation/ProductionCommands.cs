using System;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class RefundArithmetic
    {
        // Intermediate precision only; the result is bounded by paid.
        internal static long Unused(long paid, long required, long completed)
        {
            if (paid < 0 || required <= 0 || completed < 0 || completed >= required) throw new ArgumentOutOfRangeException(nameof(paid));
            return (long)((BigInteger)paid * (required - completed) / required);
        }
    }
    internal static class CampaignAllocation
    {
        internal static string Allocate(GameStateDto state, string category, string? definition)
        {
            var allocator = new EntityIdAllocator(state.NextEntitySequence);
            var id = allocator.Allocate(category).Value;
            state.NextEntitySequence = allocator.NextEntitySequence;
            state.EntityIdentities.Add(new EntityIdentityDto { EntityId = id, DefinitionId = definition });
            return id;
        }
    }
    internal sealed class EnqueueProductionHandler : ICommandHandler
    {
        private readonly DefinitionCatalogDto definitions;
        internal EnqueueProductionHandler(DefinitionCatalogDto definitions) { this.definitions = definitions; }
        public CommandErrorCode Execute(GameStateDto state, CommandEnvelope command)
        {
            var payload = command.EnqueueProduction!;
            var colony = state.Colonies!.SingleOrDefault(c => c.ColonyId == payload.ColonyId);
            if (colony == null) return CommandErrorCode.UnknownColony;
            if (colony.OwnerCivilizationId != command.ActorCivilizationId) return CommandErrorCode.ColonyNotOwned;
            var design = definitions.ShipDesigns!.SingleOrDefault(d => d.DefinitionId == payload.DefinitionId);
            if (design == null) return CommandErrorCode.UnknownShipDesign;
            var owner = state.Civilizations!.Single(c => c.CivilizationId == command.ActorCivilizationId);
            if (design.RequiredResearchDefinitionIds!.Any(id => !owner.UnlockedResearchDefinitionIds!.Contains(id, StringComparer.Ordinal))) return CommandErrorCode.DesignResearchPrerequisiteMissing;
            if (owner.Credits < design.CreditCost) return CommandErrorCode.InsufficientCredits;
            if (!WorldEconomy.Infrastructure(colony, definitions).Any(i => i.definition.Shipbuilding)) return CommandErrorCode.ShipbuildingUnavailable;
            if (design.ResourceCosts!.Any(r => PopulationRules.Stock(colony.Stockpiles!, r.ResourceDefinitionId) < r.Amount)) return CommandErrorCode.InsufficientStrategicResource;
            var id = CampaignAllocation.Allocate(state, "queue-item", design.DefinitionId);
            owner.Credits -= design.CreditCost;
            foreach (var cost in design.ResourceCosts!) PopulationRules.AddStock(colony.Stockpiles!, cost.ResourceDefinitionId, -cost.Amount);
            colony.ProductionQueue!.Add(new ProductionQueueItemDto
            {
                QueueItemId = id,
                ShipDesignDefinitionId = design.DefinitionId,
                RequiredIndustry = design.IndustryCost,
                PaidCredits = design.CreditCost,
                PaidResources = design.ResourceCosts!.OrderBy(r => r.ResourceDefinitionId, StringComparer.Ordinal).Select(PopulationData.Copy).ToList()
            });
            return CommandErrorCode.None;
        }
    }
    internal sealed class CancelProductionHandler : ICommandHandler
    {
        public CommandErrorCode Execute(GameStateDto state, CommandEnvelope command)
        {
            var payload = command.CancelProduction!;
            var colony = state.Colonies!.SingleOrDefault(c => c.ColonyId == payload.ColonyId);
            if (colony == null) return CommandErrorCode.UnknownColony;
            if (colony.OwnerCivilizationId != command.ActorCivilizationId) return CommandErrorCode.ColonyNotOwned;
            var item = colony.ProductionQueue!.SingleOrDefault(q => q.QueueItemId == payload.QueueItemId);
            if (item == null) return CommandErrorCode.UnknownQueueItem;
            var owner = state.Civilizations!.Single(c => c.CivilizationId == command.ActorCivilizationId);
            // Check both totals before changing either. Capacity overflow is a transaction fault.
            var credits = checked(owner.Credits + RefundArithmetic.Unused(item.PaidCredits, item.RequiredIndustry, item.CompletedIndustry));
            foreach (var resource in item.PaidResources!)
            {
                var refund = RefundArithmetic.Unused(resource.Amount, item.RequiredIndustry, item.CompletedIndustry);
                PopulationRules.AddStock(colony.Stockpiles!, resource.ResourceDefinitionId, refund);
                state.PopulationStatistics!.ResourceConsumed = checked(state.PopulationStatistics.ResourceConsumed + resource.Amount - refund);
            }
            owner.Credits = credits;
            colony.ProductionQueue!.Remove(item); state.EntityIdentities.RemoveAll(e => e.EntityId == item.QueueItemId);
            return CommandErrorCode.None;
        }
    }
}
