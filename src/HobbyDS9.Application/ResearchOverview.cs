using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public sealed class EngineeringRequirementSnapshot
    {
        public ScienceDiscipline Discipline { get; }
        public int IntervalStartLevel { get; }
        public int BreakthroughOrdinal { get; }
        internal EngineeringRequirementSnapshot(EngineeringScienceRequirementDto r)
        { Discipline=r.Discipline;IntervalStartLevel=r.IntervalStartLevel;BreakthroughOrdinal=r.RequiredBreakthroughOrdinal; }
    }
    public sealed class ScienceBreakpointSnapshot
    {
        public int Ordinal { get; } public long ChunkCost { get; } public long Threshold { get; } public bool Achieved { get; }
        internal ScienceBreakpointSnapshot(ScienceBreakpoint p,long progress){Ordinal=p.Ordinal;ChunkCost=p.ChunkCost;Threshold=p.Threshold;Achieved=progress>=p.Threshold;}
    }
    public sealed class ScienceCardSnapshot
    {
        public ScienceDiscipline Discipline { get; } public int Level { get; } public long Progress { get; } public long IntervalCost { get; }
        public int MaxLevel=>10; public decimal ProgressPercent=>Level==10?100m:100m*Progress/IntervalCost; public int BreakthroughCount=>Breakpoints.Count;
        public int AllocationWeight { get; } public decimal AllocationPercent=>100m*AllocationWeight/ResearchUnits.Scale;
        public bool Maxed=>Level==10; public bool AllocatedWhileMaxed=>Maxed&&AllocationWeight>0;
        public IReadOnlyList<ScienceBreakpointSnapshot> Breakpoints { get; }
        internal ScienceCardSnapshot(ScienceDisciplineSnapshot d,ScienceEngineeringSnapshot state,int pace,CommandErrorCode access)
        {
            Discipline=d.Discipline;Level=d.Level;Progress=d.Progress;AllocationWeight=state.ScienceAllocations.Single(a=>a.Discipline==d.Discipline).Weight;
            IntervalCost=d.Level==10?0:ScienceRules.IntervalCost(d.Level,pace);
            Breakpoints=(d.Level==10?new List<ScienceBreakpointSnapshot>():ScienceRules.Schedule(state.ResearchDiscoverySeed,d.Discipline,d.Level,pace).Select(p=>new ScienceBreakpointSnapshot(p,d.Progress)).ToList()).AsReadOnly();

        }
    }
    public sealed class EngineeringCardSnapshot
    {
        public EngineeringDefinitionSnapshot Definition { get; } public long Cost { get; } public long Progress { get; }
        public int MaturityPercent { get; } public EngineeringMaturityBand Band { get; } public bool IsTarget { get; }
        public IReadOnlyList<bool> ScienceSatisfied { get; } public IReadOnlyList<bool> PrerequisitesSatisfied { get; }
        public CommandErrorCode UnavailableReason { get; } public bool CanTarget=>UnavailableReason==CommandErrorCode.None;
        internal EngineeringCardSnapshot(EngineeringDefinitionSnapshot d,ScienceEngineeringSnapshot state,int pace,CommandErrorCode access)
        {
            Definition=d;Cost=ScienceRules.AdjustCost(d.BaseEngineeringCost,pace);Progress=state.EngineeringProgress.SingleOrDefault(p=>p.DefinitionId==d.DefinitionId)?.Progress??0;
            MaturityPercent=EngineeringRules.Maturity(Progress,Cost);Band=EngineeringRules.Band(MaturityPercent);IsTarget=state.EngineeringTarget==d.DefinitionId;
            ScienceSatisfied=d.ScienceRequirements.Select(q=>
            {
                var discipline=state.Disciplines.Single(s=>s.Discipline==q.Discipline);
                return discipline.Level>q.IntervalStartLevel||(discipline.Level==q.IntervalStartLevel&&ScienceRules.Schedule(state.ResearchDiscoverySeed,q.Discipline,q.IntervalStartLevel,pace).Single(p=>p.Ordinal==q.BreakthroughOrdinal).Threshold<=discipline.Progress);
            }).ToList().AsReadOnly();
            PrerequisitesSatisfied=d.Prerequisites.Select(p=>state.MatureApplications.Contains(p)).ToList().AsReadOnly();
            UnavailableReason=access!=CommandErrorCode.None?access:Band==EngineeringMaturityBand.Mature?CommandErrorCode.EngineeringAlreadyMature
                :ScienceSatisfied.Any(s=>!s)?CommandErrorCode.EngineeringScienceRequirementMissing:PrerequisitesSatisfied.Any(s=>!s)?CommandErrorCode.EngineeringPrerequisiteMissing:IsTarget?CommandErrorCode.EngineeringTargetUnchanged:CommandErrorCode.None;
        }
    }
    public sealed class ResearchOverviewSnapshot
    {
        public long ResearchOutput { get; } public long ScienceAllocation { get; } public long EngineeringAllocation { get; }
        public int ScienceAllocationUnits { get; } public int PaceFactor { get; } public long ScienceReserve { get; } public long EngineeringReserve { get; }
        public DefinitionId? Target { get; }
        public decimal SciencePercent=>100m*ScienceAllocationUnits/ResearchUnits.Scale; public decimal EngineeringPercent=>100m-SciencePercent;
        public decimal ScienceReserveConceptual=>(decimal)ScienceReserve/ResearchUnits.Scale; public decimal EngineeringReserveConceptual=>(decimal)EngineeringReserve/ResearchUnits.Scale;
        public IReadOnlyList<ScienceCardSnapshot> Science { get; } public IReadOnlyList<EngineeringCardSnapshot> Engineering { get; }
        public IReadOnlyList<ResearchReceiptSnapshot> Receipts { get; }
        private ResearchOverviewSnapshot(CampaignSnapshot snapshot,CivilizationSnapshot actor,bool planningAllowed)
        {
            var state=actor.Research!;var access=planningAllowed?StrategicOptions.PlanningAccess(snapshot,actor.CivilizationId):CommandErrorCode.CombatPending;
            PaceFactor=snapshot.Campaign.ResearchPaceFactor;ScienceAllocationUnits=state.ScienceAllocationUnits;
            ResearchOutput=snapshot.Campaign.Colonies.Where(c=>c.OwnerCivilizationId==actor.CivilizationId).Aggregate(0L,(sum,c)=>checked(sum+c.Economy.ResearchOutput));
            ScienceAllocation=(long)((BigInteger)ResearchOutput*ScienceAllocationUnits);EngineeringAllocation=checked(ResearchOutput*ResearchUnits.Scale)-ScienceAllocation;
            ScienceReserve=state.ScienceReserve;EngineeringReserve=state.EngineeringReserve;Target=state.EngineeringTarget;
            Science=state.Disciplines.Select(d=>new ScienceCardSnapshot(d,state,PaceFactor,access)).ToList().AsReadOnly();
            Engineering=snapshot.Content.Engineering.Select(d=>new EngineeringCardSnapshot(d,state,PaceFactor,access)).ToList().AsReadOnly();
            Receipts=snapshot.Campaign.LastResearchReceipts.Where(r=>r.CivilizationId==actor.CivilizationId).ToList().AsReadOnly();
        }
        public static ResearchOverviewSnapshot For(CampaignSnapshot snapshot,EntityId actorId)=>For(snapshot,actorId,true);
        internal static ResearchOverviewSnapshot For(CampaignSnapshot snapshot,EntityId actorId,bool planningAllowed)=>new ResearchOverviewSnapshot(snapshot,snapshot.Campaign.Civilizations.Single(c=>c.CivilizationId==actorId),planningAllowed);
    }
}
