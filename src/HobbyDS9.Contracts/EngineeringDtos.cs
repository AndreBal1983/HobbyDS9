using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public sealed class EngineeringScienceRequirementDto
    {
        public ScienceDiscipline Discipline { get; set; }
        public int IntervalStartLevel { get; set; }
        public int RequiredBreakthroughOrdinal { get; set; }
    }
    public enum EngineeringProjectKind { NewCapability = 1, Refinement = 2, CivilApplication = 3 }
    public sealed class EngineeringParameterChangeDto { public string ParameterDefinitionId { get; set; } = string.Empty; public long Value { get; set; } }
    public sealed class EngineeringApplicationDefinitionDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public long BaseEngineeringCost { get; set; }
        public EngineeringProjectKind Kind { get; set; }
        public string EffectSummary { get; set; } = string.Empty;
        public EngineeringParameterChangeDto? ParameterChange { get; set; }
        public List<EngineeringScienceRequirementDto>? ScienceRequirements { get; set; }
        public List<string>? PrerequisiteDefinitionIds { get; set; }
    }
}
