using System.Collections.Generic;
namespace HobbyDS9.Contracts
{
    public static class ScienceEngineeringVersions { public const int Save = FoundationVersions.SaveFormatVersion; public const int Content = FoundationVersions.ContentFormatVersion; public const string Rules = "science-engineering-v1"; }
    public enum ScienceDiscipline { Biotech = 1, Computing = 2, Construction = 3, Energy = 4, Propulsion = 5, Weapons = 6 }
    public enum EngineeringMaturityBand { Development = 0, Prototype = 1, Operational = 2, Mature = 3 }
    public sealed class ScienceDisciplineStateDto
    {
        public ScienceDiscipline Discipline { get; set; }
        public int Level { get; set; }
        public long ProgressTowardNextLevel { get; set; }
        public long TotalSpent { get; set; }
    }
    public sealed class ResearchStatisticsDto
    {
        public long TotalResearchGenerated { get; set; }
        public long TotalScienceAllocated { get; set; }
        public long TotalEngineeringAllocated { get; set; }
        public long TotalScienceSpent { get; set; }
        public long TotalEngineeringSpent { get; set; }
        public long TotalBreakthroughs { get; set; }
        public long TotalScienceLevelsCompleted { get; set; }
        public long TotalEngineeringApplicationsMatured { get; set; }
    }
    public class ScienceEngineeringStateDto
    {
        public ulong? ResearchDiscoverySeed { get; set; }
        public List<ScienceAllocationDto>? ScienceAllocations { get; set; }
        public List<EngineeringParameterDto>? EngineeringParameters { get; set; }
        public int ScienceAllocationUnits { get; set; }
        public long ScienceReserve { get; set; }
        public long EngineeringReserve { get; set; }
        public List<ScienceDisciplineStateDto>? Disciplines { get; set; }
        public string? EngineeringTargetDefinitionId { get; set; }
        public List<EngineeringProgressDto>? EngineeringProgress { get; set; }
        public List<string>? MatureEngineeringApplicationDefinitionIds { get; set; }
        public ResearchStatisticsDto? Statistics { get; set; }
    }
    public sealed class EngineeringProgressDto { public string DefinitionId { get; set; } = string.Empty; public long Progress { get; set; } }
    public sealed class ResearchReceiptDto
    {
        public string CivilizationId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public ScienceDiscipline? Discipline { get; set; }
        public int IntervalStartLevel { get; set; }
        public int BreakthroughOrdinal { get; set; }
        public string? ApplicationDefinitionId { get; set; }
        public long Amount { get; set; }
    }
    public sealed class SetResearchAllocationPayload { public int ScienceAllocationUnits { get; set; } }
    public sealed class SetScienceAllocationPayload { public List<ScienceAllocationDto>? Allocations { get; set; } }
    public sealed class ScienceAllocationDto { public ScienceDiscipline Discipline { get; set; } public int Weight { get; set; } }
    public sealed class EngineeringParameterDto { public string DefinitionId { get; set; } = string.Empty; public long Value { get; set; } }
    public static class ResearchUnits { public const int Scale = 600; }
}
