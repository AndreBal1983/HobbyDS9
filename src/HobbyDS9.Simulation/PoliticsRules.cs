using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static class PoliticsRules
    {
        internal const int EscalationDelay=3, RecoveryDelay=5, DisruptionPeriod=4, DisruptionDuration=2;
        internal static GovernancePolicyDefinitionDto Policy(ColonyStateDto world,DefinitionCatalogDto definitions) => definitions.GovernancePolicies!.Single(p=>p.DefinitionId==world.Politics!.GovernancePolicyDefinitionId);
        internal static int Pressure(ColonyStateDto world)
        {
            var population=PopulationRules.Population(world);if(population==0)return 0;
            var sum=world.Cohorts!.Aggregate(BigInteger.Zero,(total,c)=>total+(BigInteger)c.Population*
                ((10000-c.Satisfaction)*3L+(10000-c.CivicAlignment)*3L+c.Radicalisation*4L+WorldEconomy.LegalPenalty(c.LegalStatus)*5L));
            return PopulationRules.Rating((long)(sum/population/10)+(world.FoodShortageTurns>0?500:0)+(10000-world.Stability)/10);
        }
        internal static void ExecuteWorld(ColonyStateDto world,GameStateDto state,DefinitionCatalogDto definitions,bool sufficient)
        {
            var politics=world.Politics!;var policy=Policy(world,definitions);var facts=WorldEconomy.Project(world,state,definitions);
            politics.CohortReports!.Clear();politics.Events!.Clear();politics.LastUpdateTurn=state.TurnNumber;
            foreach(var cohort in world.Cohorts!.OrderBy(c=>c.CohortId,StringComparer.Ordinal))
            {
                var old=(cohort.Satisfaction,cohort.Integration,cohort.CivicAlignment,cohort.Radicalisation,cohort.LegalStatus);
                var reasons=new List<PoliticalReasonDto>();
                void Reason(string code,int satisfaction=0,int integration=0,int alignment=0,int radicalisation=0,string? source=null)
                {reasons.Add(new PoliticalReasonDto{Code=code,SourceEntityId=source,SatisfactionChange=satisfaction,IntegrationChange=integration,AlignmentChange=alignment,RadicalisationChange=radicalisation});}
                Reason("basic-needs",sufficient?25:-300,integration:sufficient&&cohort.Satisfaction>=5000&&world.Stability>=5000?10:0);
                Reason("prosperity",(world.Prosperity-5000)/100);
                Reason("environment",-(10000-facts.AverageHabitability)/100-(facts.Crowding>0?100:0));
                var legal=WorldEconomy.LegalPenalty(cohort.LegalStatus);
                Reason("legal-treatment",-legal/25,alignment:-legal/50,radicalisation:legal/50);
                Reason("stability",(world.Stability-5000)/250,radicalisation:world.Stability<2500?20:0);
                Reason("civic-conditions",alignment:(cohort.Satisfaction-5000)/100+(cohort.Integration-5000)/200,
                    radicalisation:cohort.Satisfaction<3000?60:cohort.Satisfaction>=7000&&cohort.Integration>=5000?-40:0);
                Reason("governance-policy",policy.SatisfactionChange,policy.IntegrationChange,policy.AlignmentChange,policy.RadicalisationChange);
                foreach(var memory in cohort.Memories!.OrderBy(m=>m.Kind,StringComparer.Ordinal).ThenBy(m=>m.SourceEntityId,StringComparer.Ordinal))
                {
                    if(memory.ExpiryTurn<=state.TurnNumber||memory.Magnitude==0)continue;
                    var effect=definitions.PoliticalMemoryEffects!.SingleOrDefault(e=>e.MemoryKind==memory.Kind);if(effect==null)continue;
                    int Scale(int value)=>(int)PopulationRules.ScaleBy(value,memory.Magnitude);
                    Reason("memory."+memory.Kind,Scale(effect.SatisfactionChange),Scale(effect.IntegrationChange),Scale(effect.AlignmentChange),Scale(effect.RadicalisationChange),memory.SourceEntityId);
                }
                cohort.Satisfaction=PopulationRules.Rating(cohort.Satisfaction+reasons.Sum(r=>(long)r.SatisfactionChange));
                cohort.Integration=PopulationRules.Rating(cohort.Integration+Math.Max(-25,Math.Min(25,reasons.Sum(r=>(long)r.IntegrationChange))));
                cohort.CivicAlignment=PopulationRules.Rating(cohort.CivicAlignment+reasons.Sum(r=>(long)r.AlignmentChange));
                cohort.Radicalisation=PopulationRules.Rating(cohort.Radicalisation+reasons.Sum(r=>(long)r.RadicalisationChange));
                var transition=policy.LegalTransitions!.SingleOrDefault(t=>t.From==cohort.LegalStatus);
                var eligible=transition!=null&&cohort.Integration>=transition.RequiredIntegration&&cohort.Satisfaction>=transition.RequiredSatisfaction
                    &&cohort.CivicAlignment>=transition.RequiredAlignment&&world.Stability>=transition.RequiredStability;
                cohort.LegalStatusEligibleTurns=eligible?Math.Min(transition!.RequiredTurns,checked(cohort.LegalStatusEligibleTurns+1)):0;
                var progress=cohort.LegalStatusEligibleTurns;
                if(eligible&&progress>=transition!.RequiredTurns){cohort.LegalStatus=transition.To;cohort.LegalStatusEligibleTurns=0;politics.Events.Add("legal-transition."+cohort.CohortId);}
                politics.CohortReports.Add(new CohortPoliticalReportDto{CohortId=cohort.CohortId,PreviousStatus=old.LegalStatus,ResultStatus=cohort.LegalStatus,TransitionProgress=progress,
                    SatisfactionChange=cohort.Satisfaction-old.Satisfaction,IntegrationChange=cohort.Integration-old.Integration,AlignmentChange=cohort.CivicAlignment-old.CivicAlignment,
                    RadicalisationChange=cohort.Radicalisation-old.Radicalisation,Reasons=reasons.OrderBy(r=>r.Code,StringComparer.Ordinal).ThenBy(r=>r.SourceEntityId,StringComparer.Ordinal).ToList()});
            }
            politics.PreviousPressure=politics.Pressure;
            politics.Pressure=PopulationRules.Rating(Pressure(world)-policy.Suppression);
            var level=(int)politics.Resistance;var nextThreshold=level==0?2500:level==1?4000:6000;
            var recoveryThreshold=level==1?1750:level==2?3250:5250;
            politics.EscalationTurns=level<3&&politics.Pressure>=nextThreshold?politics.EscalationTurns+1:0;
            politics.RecoveryTurns=level>0&&politics.Pressure<recoveryThreshold?politics.RecoveryTurns+1:0;
            if(politics.EscalationTurns>=EscalationDelay){politics.Resistance=(ResistanceState)(level+1);politics.EscalationTurns=0;politics.Events.Add("resistance-escalated");}
            else if(politics.RecoveryTurns>=RecoveryDelay){politics.Resistance=(ResistanceState)(level-1);politics.RecoveryTurns=0;politics.Events.Add("resistance-receded");}
            politics.OrganizedTurns=politics.Resistance==ResistanceState.OrganizedResistance?checked(politics.OrganizedTurns+1):0;
            politics.DisruptionRemainingTurns=Math.Max(0,politics.DisruptionRemainingTurns-1);
            if(politics.OrganizedTurns>0&&politics.OrganizedTurns%DisruptionPeriod==0)
            {politics.DisruptionRemainingTurns=DisruptionDuration;politics.Disruptions=checked(politics.Disruptions+1);politics.Events.Add("organized-disruption");}
        }
    }
    internal sealed class GovernancePolicyHandler : ICommandHandler
    {
        private readonly DefinitionCatalogDto definitions;
        internal GovernancePolicyHandler(DefinitionCatalogDto definitions){this.definitions=definitions;}
        public CommandErrorCode Execute(GameStateDto state,CommandEnvelope command)
        {
            var payload=command.SetWorldGovernancePolicy!;
            var world=state.Colonies!.SingleOrDefault(c=>c.ColonyId==payload.ColonyId);
            if(world==null)return CommandErrorCode.UnknownColony;
            if(world.OwnerCivilizationId!=command.ActorCivilizationId)return CommandErrorCode.ColonyNotOwned;
            if(!definitions.GovernancePolicies!.Any(p=>p.DefinitionId==payload.PolicyDefinitionId))return CommandErrorCode.UnknownGovernancePolicy;
            if(world.Politics!.GovernancePolicyDefinitionId==payload.PolicyDefinitionId)return CommandErrorCode.GovernancePolicyUnchanged;
            world.Politics.GovernancePolicyDefinitionId=payload.PolicyDefinitionId;
            foreach(var cohort in world.Cohorts!)cohort.LegalStatusEligibleTurns=0;
            return CommandErrorCode.None;
        }
    }
}
