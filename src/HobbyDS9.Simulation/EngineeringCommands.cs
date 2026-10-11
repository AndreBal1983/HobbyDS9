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
            if(command.Kind==CommandKind.SetResearchAllocation){int value=command.SetResearchAllocation!.ScienceAllocationUnits;if(value<0||value>ResearchUnits.Scale)return CommandErrorCode.InvalidResearchAllocation;if(value==civ.ScienceAllocationUnits)return CommandErrorCode.ResearchAllocationUnchanged;civ.ScienceAllocationUnits=value;return CommandErrorCode.None;}
            var desired=command.SetScienceAllocation!.Allocations;
            if(!ScienceRules.ValidAllocations(desired))return CommandErrorCode.InvalidScienceAllocation;
            var ordered=desired!.OrderBy(a=>a.Discipline).ToList();
            if(ordered.Select(a=>(a.Discipline,a.Weight)).SequenceEqual(civ.ScienceAllocations!.OrderBy(a=>a.Discipline).Select(a=>(a.Discipline,a.Weight))))return CommandErrorCode.ScienceAllocationUnchanged;
            civ.ScienceAllocations=ordered.Select(a=>new ScienceAllocationDto{Discipline=a.Discipline,Weight=a.Weight}).ToList();return CommandErrorCode.None;
        }
    }
}
