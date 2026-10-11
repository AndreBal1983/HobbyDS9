using System.Collections.Generic;
namespace HobbyDS9.Contracts
{
    public static class ScienceEngineeringVersions { public const int Save = 13; public const int Content = 9; public const string Rules = "science-engineering-v1"; }
    public enum ScienceDiscipline { Biotech = 1, Computing = 2, Construction = 3, Energy = 4, Propulsion = 5, Weapons = 6 }
    public enum EngineeringMaturityBand { Development = 0, Prototype = 1, Operational = 2, Mature = 3 }
    public sealed class ScienceDisciplineStateDto
    {
        public ScienceDiscipline Discipline { get; set; }
        public int Level { get; set; }
        public long ProgressTowardNextLevel { get; set; }
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
    public sealed class ScienceEngineeringStateDto
    {
        public ulong? ResearchDiscoverySeed { get; set; }
        public ScienceDiscipline? ScienceFocus { get; set; }
        public int ScienceAllocationBps { get; set; }
        public long ScienceReserve { get; set; }
        public long EngineeringReserve { get; set; }
        public List<ScienceDisciplineStateDto>? Disciplines { get; set; }
        public string? EngineeringTargetDefinitionId { get; set; }
        public List<EngineeringProgressDto>? EngineeringProgress { get; set; }
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
    public sealed class SetResearchAllocationPayload { public int ScienceAllocationBps { get; set; } }
    public sealed class SetScienceFocusPayload { public ScienceDiscipline Discipline { get; set; } }
}
