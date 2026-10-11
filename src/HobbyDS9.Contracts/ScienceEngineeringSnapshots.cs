using System;
using System.Collections.Generic;
using System.Linq;
namespace HobbyDS9.Contracts
{
    public sealed class ScienceAllocationSnapshot
    { public ScienceDiscipline Discipline { get; } public int Weight { get; } public ScienceAllocationSnapshot(ScienceAllocationDto a){Discipline=a.Discipline;Weight=a.Weight;} }
    public sealed class EngineeringParameterSnapshot
    { public DefinitionId DefinitionId { get; } public long Value { get; } public EngineeringParameterSnapshot(EngineeringParameterDto p){DefinitionId=new DefinitionId(p.DefinitionId);Value=p.Value;} }
    public sealed class ResearchReceiptSnapshot
    {
        public EntityId CivilizationId { get; } public string Category { get; } public ScienceDiscipline? Discipline { get; }
        public int IntervalStartLevel { get; } public int BreakthroughOrdinal { get; } public DefinitionId? ApplicationDefinitionId { get; } public long Amount { get; }
        public ResearchReceiptSnapshot(ResearchReceiptDto d){CivilizationId=new EntityId(d.CivilizationId);Category=d.Category;Discipline=d.Discipline;IntervalStartLevel=d.IntervalStartLevel;BreakthroughOrdinal=d.BreakthroughOrdinal;ApplicationDefinitionId=d.ApplicationDefinitionId==null?(DefinitionId?)null:new DefinitionId(d.ApplicationDefinitionId);Amount=d.Amount;}
    }
    public sealed class ScienceDisciplineSnapshot
    {
        public ScienceDiscipline Discipline { get; } public int Level { get; } public long Progress { get; } public long TotalSpent { get; }
        public ScienceDisciplineSnapshot(ScienceDisciplineStateDto dto){Discipline=dto.Discipline;Level=dto.Level;Progress=dto.ProgressTowardNextLevel;TotalSpent=dto.TotalSpent;}
    }
    public sealed class EngineeringProgressSnapshot
    {
        public DefinitionId DefinitionId { get; } public long Progress { get; }
        public EngineeringProgressSnapshot(EngineeringProgressDto dto){DefinitionId=new DefinitionId(dto.DefinitionId);Progress=dto.Progress;}
    }
    public sealed class ResearchStatisticsSnapshot
    {
        public long TotalResearchGenerated { get; } public long TotalScienceAllocated { get; } public long TotalEngineeringAllocated { get; }
        public long TotalScienceSpent { get; } public long TotalEngineeringSpent { get; } public long TotalBreakthroughs { get; }
        public long TotalScienceLevelsCompleted { get; } public long TotalEngineeringApplicationsMatured { get; }
        public ResearchStatisticsSnapshot(ResearchStatisticsDto s){TotalResearchGenerated=s.TotalResearchGenerated;TotalScienceAllocated=s.TotalScienceAllocated;TotalEngineeringAllocated=s.TotalEngineeringAllocated;TotalScienceSpent=s.TotalScienceSpent;TotalEngineeringSpent=s.TotalEngineeringSpent;TotalBreakthroughs=s.TotalBreakthroughs;TotalScienceLevelsCompleted=s.TotalScienceLevelsCompleted;TotalEngineeringApplicationsMatured=s.TotalEngineeringApplicationsMatured;}
    }
    public sealed class ScienceEngineeringSnapshot
    {
        public ulong ResearchDiscoverySeed { get; } public IReadOnlyList<ScienceAllocationSnapshot> ScienceAllocations { get; } public IReadOnlyList<EngineeringParameterSnapshot> EngineeringParameters { get; } public int ScienceAllocationUnits { get; }
        public long ScienceReserve { get; } public long EngineeringReserve { get; } public DefinitionId? EngineeringTarget { get; }
        public IReadOnlyList<ScienceDisciplineSnapshot> Disciplines { get; } public IReadOnlyList<EngineeringProgressSnapshot> EngineeringProgress { get; }
        public IReadOnlyList<DefinitionId> MatureApplications { get; } public ResearchStatisticsSnapshot Statistics { get; }
        public ScienceEngineeringSnapshot(ScienceEngineeringStateDto s){ResearchDiscoverySeed=s.ResearchDiscoverySeed!.Value;ScienceAllocations=s.ScienceAllocations!.OrderBy(a=>a.Discipline).Select(a=>new ScienceAllocationSnapshot(a)).ToList().AsReadOnly();EngineeringParameters=s.EngineeringParameters!.OrderBy(p=>p.DefinitionId,StringComparer.Ordinal).Select(p=>new EngineeringParameterSnapshot(p)).ToList().AsReadOnly();ScienceAllocationUnits=s.ScienceAllocationUnits;ScienceReserve=s.ScienceReserve;EngineeringReserve=s.EngineeringReserve;EngineeringTarget=s.EngineeringTargetDefinitionId==null?(DefinitionId?)null:new DefinitionId(s.EngineeringTargetDefinitionId);Disciplines=s.Disciplines!.OrderBy(d=>d.Discipline).Select(d=>new ScienceDisciplineSnapshot(d)).ToList().AsReadOnly();EngineeringProgress=s.EngineeringProgress!.OrderBy(p=>p.DefinitionId,StringComparer.Ordinal).Select(p=>new EngineeringProgressSnapshot(p)).ToList().AsReadOnly();MatureApplications=s.MatureEngineeringApplicationDefinitionIds!.OrderBy(id=>id,StringComparer.Ordinal).Select(id=>new DefinitionId(id)).ToList().AsReadOnly();Statistics=new ResearchStatisticsSnapshot(s.Statistics!);}
    }
}
