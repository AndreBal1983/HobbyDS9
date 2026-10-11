using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    public static class EngineeringRules
    {
        public static int Maturity(long progress,long cost) => (int)BigInteger.Min(100,(BigInteger)progress*100/cost);
        public static EngineeringMaturityBand Band(int maturity) => maturity==100?EngineeringMaturityBand.Mature:maturity>=50?EngineeringMaturityBand.Operational:maturity>=25?EngineeringMaturityBand.Prototype:EngineeringMaturityBand.Development;
        public static bool ScienceSatisfied(ScienceEngineeringStateDto state,EngineeringScienceRequirementDto requirement,int pace)
        {
            var discipline=state.Disciplines!.Single(d=>d.Discipline==requirement.Discipline);
            return discipline.Level>requirement.IntervalStartLevel||(discipline.Level==requirement.IntervalStartLevel&&ScienceRules.Schedule(state.ResearchDiscoverySeed!.Value,requirement.Discipline,requirement.IntervalStartLevel,pace).Single(p=>p.Ordinal==requirement.RequiredBreakthroughOrdinal).Threshold<=discipline.ProgressTowardNextLevel);
        }
        public static CommandErrorCode Availability(ScienceEngineeringStateDto state,EngineeringApplicationDefinitionDto application,int pace)
        {
            if(state.MatureEngineeringApplicationDefinitionIds!.Contains(application.DefinitionId,StringComparer.Ordinal))return CommandErrorCode.EngineeringAlreadyMature;
            if(application.ScienceRequirements!.Any(r=>!ScienceSatisfied(state,r,pace)))return CommandErrorCode.EngineeringScienceRequirementMissing;
            if(application.PrerequisiteDefinitionIds!.Any(id=>!state.MatureEngineeringApplicationDefinitionIds.Contains(id,StringComparer.Ordinal)))return CommandErrorCode.EngineeringPrerequisiteMissing;
            return CommandErrorCode.None;
        }
        internal static void Spend(ScienceEngineeringStateDto state,int pace,DefinitionCatalogDto definitions,string civilizationId,List<ResearchReceiptDto> receipts)
        {
            if(state.EngineeringTargetDefinitionId==null)return;
            var app=definitions.EngineeringApplications!.Single(a=>a.DefinitionId==state.EngineeringTargetDefinitionId);
            if(Availability(state,app,pace)!=CommandErrorCode.None)throw new InvalidOperationException("Ineligible engineering target.");
            var progress=state.EngineeringProgress!.SingleOrDefault(p=>p.DefinitionId==app.DefinitionId);
            if(progress==null){progress=new EngineeringProgressDto{DefinitionId=app.DefinitionId};state.EngineeringProgress!.Add(progress);}
            long cost=ScienceRules.AdjustCost(app.BaseEngineeringCost,pace);long spent=Math.Min(state.EngineeringReserve,cost-progress.Progress);var before=Band(Maturity(progress.Progress,cost));
            progress.Progress=checked(progress.Progress+spent);state.EngineeringReserve-=spent;state.Statistics!.TotalEngineeringSpent=checked(state.Statistics.TotalEngineeringSpent+spent);
            if(spent>0)receipts.Add(new ResearchReceiptDto{CivilizationId=civilizationId,Category="engineering-spent",ApplicationDefinitionId=app.DefinitionId,Amount=spent});
            var after=Band(Maturity(progress.Progress,cost));for(int band=(int)before+1;band<=(int)after;band++)receipts.Add(new ResearchReceiptDto{CivilizationId=civilizationId,Category="engineering-band",ApplicationDefinitionId=app.DefinitionId,Amount=band});
            if(progress.Progress==cost){if(app.Kind==EngineeringProjectKind.Refinement){var change=app.ParameterChange!;state.EngineeringParameters!.Single(p=>p.DefinitionId==change.ParameterDefinitionId).Value=change.Value;}state.MatureEngineeringApplicationDefinitionIds!.Add(app.DefinitionId);state.MatureEngineeringApplicationDefinitionIds!.Sort(StringComparer.Ordinal);state.EngineeringTargetDefinitionId=null;state.Statistics.TotalEngineeringApplicationsMatured=checked(state.Statistics.TotalEngineeringApplicationsMatured+1);receipts.Add(new ResearchReceiptDto{CivilizationId=civilizationId,Category="engineering-mature",ApplicationDefinitionId=app.DefinitionId,Amount=cost});}
        }
    }
}
