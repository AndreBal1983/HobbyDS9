using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Application
{
    public enum ResearchDisplayState { Locked, Available, Current, Completed }

    // Advisory, immutable projections of an observed snapshot. Submission still
    // performs authoritative validation, including stamps, in Simulation.
    public sealed class ResearchOption
    {
        public ResearchDefinitionSnapshot Definition { get; }
        public ResearchDisplayState State { get; }
        public CommandErrorCode UnavailableReason { get; }
        public bool CanSelect => UnavailableReason == CommandErrorCode.None;
        internal ResearchOption(ResearchDefinitionSnapshot definition, ResearchDisplayState state, CommandErrorCode reason)
        { Definition = definition; State = state; UnavailableReason = reason; }
    }

    public sealed class ProductionOption
    {
        public ShipDesignSnapshot Design { get; }
        public CommandErrorCode UnavailableReason { get; }
        public bool CanEnqueue => UnavailableReason == CommandErrorCode.None;
        internal ProductionOption(ShipDesignSnapshot design, CommandErrorCode reason)
        { Design = design; UnavailableReason = reason; }
    }

    public static class StrategicOptions
    {
        public static CommandErrorCode PlanningAccess(CampaignSnapshot snapshot, EntityId actorId)
        {
            if (snapshot.Campaign.Phase != SessionPhase.Planning) return CommandErrorCode.CombatPending;
            var actor = snapshot.Campaign.Civilizations.SingleOrDefault(c => c.CivilizationId == actorId);
            if (actor == null) return CommandErrorCode.UnknownActor;
            return actor.ControllerKind == ControllerKind.Human ? CommandErrorCode.None : CommandErrorCode.ActorNotControlled;
        }

        public static IReadOnlyList<ResearchOption> Research(CampaignSnapshot snapshot, EntityId actorId)
        {
            var actor = snapshot.Campaign.Civilizations.SingleOrDefault(c => c.CivilizationId == actorId);
            var access = PlanningAccess(snapshot, actorId);
            return snapshot.Content.Research.Select(definition =>
            {
                var completed = actor != null && actor.UnlockedResearchDefinitionIds.Contains(definition.DefinitionId);
                var missing = actor == null || definition.Prerequisites.Any(p => !actor.UnlockedResearchDefinitionIds.Contains(p));
                var current = actor != null && actor.ResearchTargetDefinitionId == definition.DefinitionId;
                var state = completed ? ResearchDisplayState.Completed : current ? ResearchDisplayState.Current
                    : missing ? ResearchDisplayState.Locked : ResearchDisplayState.Available;
                var reason = access != CommandErrorCode.None ? access : completed ? CommandErrorCode.ResearchAlreadyUnlocked
                    : missing ? CommandErrorCode.ResearchPrerequisiteMissing : current ? CommandErrorCode.ResearchTargetUnchanged : CommandErrorCode.None;
                return new ResearchOption(definition, state, reason);
            }).ToList().AsReadOnly();
        }

        public static IReadOnlyList<ProductionOption> Production(CampaignSnapshot snapshot, EntityId actorId, EntityId colonyId)
        {
            var access = PlanningAccess(snapshot, actorId);
            var actor = snapshot.Campaign.Civilizations.SingleOrDefault(c => c.CivilizationId == actorId);
            var colony = snapshot.Campaign.Colonies.SingleOrDefault(c => c.ColonyId == colonyId);
            return snapshot.Campaign.ShipDesigns.Select(design =>
            {
                var reason = access != CommandErrorCode.None ? access
                    : colony == null ? CommandErrorCode.UnknownColony
                    : colony.OwnerCivilizationId != actorId ? CommandErrorCode.ColonyNotOwned
                    : design.RequiredResearchDefinitionIds.Any(id => !actor!.UnlockedResearchDefinitionIds.Contains(id)) ? CommandErrorCode.DesignResearchPrerequisiteMissing
                    : actor!.Credits < design.CreditCost ? CommandErrorCode.InsufficientCredits
                    : colony.Materials < design.MaterialCost ? CommandErrorCode.InsufficientMaterials : CommandErrorCode.None;
                return new ProductionOption(design, reason);
            }).ToList().AsReadOnly();
        }
    }
}
