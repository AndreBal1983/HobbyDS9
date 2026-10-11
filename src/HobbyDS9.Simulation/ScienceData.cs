using System;
using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static class ScienceData
    {
        internal static ResearchStatisticsDto Copy(ResearchStatisticsDto s) => new ResearchStatisticsDto{TotalResearchGenerated=s.TotalResearchGenerated,TotalScienceAllocated=s.TotalScienceAllocated,TotalEngineeringAllocated=s.TotalEngineeringAllocated,TotalScienceSpent=s.TotalScienceSpent,TotalEngineeringSpent=s.TotalEngineeringSpent,TotalBreakthroughs=s.TotalBreakthroughs,TotalScienceLevelsCompleted=s.TotalScienceLevelsCompleted,TotalEngineeringApplicationsMatured=s.TotalEngineeringApplicationsMatured};
        internal static CivilizationStateDto Copy(CivilizationStateDto c) => new CivilizationStateDto{CivilizationId=c.CivilizationId,DefinitionId=c.DefinitionId,ControllerKind=c.ControllerKind,Credits=c.Credits,
            ResearchDiscoverySeed=c.ResearchDiscoverySeed,ScienceFocus=c.ScienceFocus,ScienceAllocationBps=c.ScienceAllocationBps,ScienceReserve=c.ScienceReserve,EngineeringReserve=c.EngineeringReserve,EngineeringTargetDefinitionId=c.EngineeringTargetDefinitionId,
            Disciplines=c.Disciplines!.OrderBy(d=>d.Discipline).Select(d=>new ScienceDisciplineStateDto{Discipline=d.Discipline,Level=d.Level,ProgressTowardNextLevel=d.ProgressTowardNextLevel}).ToList(),
            EngineeringProgress=c.EngineeringProgress!.OrderBy(p=>p.DefinitionId,StringComparer.Ordinal).Select(p=>new EngineeringProgressDto{DefinitionId=p.DefinitionId,Progress=p.Progress}).ToList(),MatureEngineeringApplicationDefinitionIds=c.MatureEngineeringApplicationDefinitionIds!.OrderBy(id=>id,StringComparer.Ordinal).ToList(),Statistics=Copy(c.Statistics!)};
        internal static ResearchReceiptDto Copy(ResearchReceiptDto r)=>new ResearchReceiptDto{CivilizationId=r.CivilizationId,Category=r.Category,Discipline=r.Discipline,IntervalStartLevel=r.IntervalStartLevel,BreakthroughOrdinal=r.BreakthroughOrdinal,ApplicationDefinitionId=r.ApplicationDefinitionId,Amount=r.Amount};
        private static (long,long,long,long,long,long,long,long) Statistics(ResearchStatisticsDto s)=>(s.TotalResearchGenerated,s.TotalScienceAllocated,s.TotalEngineeringAllocated,s.TotalScienceSpent,s.TotalEngineeringSpent,s.TotalBreakthroughs,s.TotalScienceLevelsCompleted,s.TotalEngineeringApplicationsMatured);
        internal static bool Same(GameStateDto a,GameStateDto b,bool allowResearch)
        {
            if(a.ResearchPaceFactor!=b.ResearchPaceFactor)return false;
            if(!a.Civilizations!.Select(c=>(c.CivilizationId,c.ResearchDiscoverySeed,c.ScienceAllocationBps,c.ScienceFocus)).SequenceEqual(b.Civilizations!.Select(c=>(c.CivilizationId,c.ResearchDiscoverySeed,c.ScienceAllocationBps,c.ScienceFocus))))return false;
            if(allowResearch)return true;
            return a.Civilizations.Zip(b.Civilizations,(x,y)=>x.ScienceReserve==y.ScienceReserve&&x.EngineeringReserve==y.EngineeringReserve&&x.EngineeringTargetDefinitionId==y.EngineeringTargetDefinitionId&&Statistics(x.Statistics!)==Statistics(y.Statistics!)
                &&x.Disciplines!.Select(d=>(d.Discipline,d.Level,d.ProgressTowardNextLevel)).SequenceEqual(y.Disciplines!.Select(d=>(d.Discipline,d.Level,d.ProgressTowardNextLevel)))
                &&x.EngineeringProgress!.Select(p=>(p.DefinitionId,p.Progress)).SequenceEqual(y.EngineeringProgress!.Select(p=>(p.DefinitionId,p.Progress)))&&x.MatureEngineeringApplicationDefinitionIds!.SequenceEqual(y.MatureEngineeringApplicationDefinitionIds!)).All(equal=>equal)
                &&a.LastResearchReceipts!.Select(r=>(r.CivilizationId,r.Category,r.Discipline,r.IntervalStartLevel,r.BreakthroughOrdinal,r.ApplicationDefinitionId,r.Amount)).SequenceEqual(b.LastResearchReceipts!.Select(r=>(r.CivilizationId,r.Category,r.Discipline,r.IntervalStartLevel,r.BreakthroughOrdinal,r.ApplicationDefinitionId,r.Amount)));
        }
    }
}
