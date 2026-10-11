using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal sealed class EngineeringTargetHandler : ICommandHandler
    {
        private readonly DefinitionCatalogDto definitions;
        internal EngineeringTargetHandler(DefinitionCatalogDto definitions){this.definitions=definitions;}
        public CommandErrorCode Execute(GameStateDto staging,CommandEnvelope command)
        {
            var target=command.SetEngineeringTarget!.TargetDefinitionId;var civ=staging.Civilizations!.Single(c=>c.CivilizationId==command.ActorCivilizationId);
            if(target==null){if(civ.EngineeringTargetDefinitionId==null)return CommandErrorCode.EngineeringTargetUnchanged;civ.EngineeringTargetDefinitionId=null;return CommandErrorCode.None;}
            var app=definitions.EngineeringApplications!.SingleOrDefault(a=>a.DefinitionId==target);if(app==null)return CommandErrorCode.UnknownEngineeringApplication;
            var error=EngineeringRules.Availability(civ,app,staging.ResearchPaceFactor!.Value);if(error!=CommandErrorCode.None)return error;
            if(civ.EngineeringTargetDefinitionId==target)return CommandErrorCode.EngineeringTargetUnchanged;civ.EngineeringTargetDefinitionId=target;return CommandErrorCode.None;
        }
    }
    internal sealed class ScienceCommandHandler : ICommandHandler
    {
        public CommandErrorCode Execute(GameStateDto staging,CommandEnvelope command)
        {
            var civ=staging.Civilizations!.Single(c=>c.CivilizationId==command.ActorCivilizationId);
            if(command.Kind==CommandKind.SetResearchAllocation){int value=command.SetResearchAllocation!.ScienceAllocationBps;if(value<0||value>10000)return CommandErrorCode.InvalidResearchAllocation;if(value==civ.ScienceAllocationBps)return CommandErrorCode.ResearchAllocationUnchanged;civ.ScienceAllocationBps=value;return CommandErrorCode.None;}
            var focus=command.SetScienceFocus!.Discipline;if(!ScienceRules.ValidDiscipline(focus))return CommandErrorCode.InvalidScienceDiscipline;
            if(civ.Disciplines!.Single(d=>d.Discipline==focus).Level==10)return CommandErrorCode.ScienceFocusMaxed;
            if(civ.ScienceFocus==focus)return CommandErrorCode.ScienceFocusUnchanged;civ.ScienceFocus=focus;return CommandErrorCode.None;
        }
    }
}
