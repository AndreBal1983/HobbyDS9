using System;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal sealed class ResearchTargetHandler : ICommandHandler
    {
        private readonly DefinitionCatalogDto definitions;
        internal ResearchTargetHandler(DefinitionCatalogDto definitions) { this.definitions = definitions; }
        public CommandErrorCode Execute(GameStateDto stagingState, CommandEnvelope command)
        {
            var target = command.SetResearchTarget!.TargetDefinitionId;
            var node = definitions.ResearchNodes!.SingleOrDefault(n => n.DefinitionId == target);
            if (node == null) return CommandErrorCode.UnknownResearchNode;
            var civilization = stagingState.Civilizations!.Single(c => c.CivilizationId == command.ActorCivilizationId);
            if (civilization.UnlockedResearchDefinitionIds!.Contains(target, StringComparer.Ordinal)) return CommandErrorCode.ResearchAlreadyUnlocked;
            if (node.PrerequisiteDefinitionIds!.Any(id => !civilization.UnlockedResearchDefinitionIds.Contains(id, StringComparer.Ordinal)))
                return CommandErrorCode.ResearchPrerequisiteMissing;
            if (civilization.ResearchTargetDefinitionId == target) return CommandErrorCode.ResearchTargetUnchanged;
            civilization.ResearchTargetDefinitionId = target;
            return CommandErrorCode.None;
        }
    }
}
