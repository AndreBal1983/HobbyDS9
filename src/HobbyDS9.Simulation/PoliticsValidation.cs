using System;
using System.Linq;
using System.Collections.Generic;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    public static partial class FoundationValidator
    {
        private static void ValidatePoliticsDefinitions(DefinitionCatalogDto d,HashSet<DefinitionId> ids,HashSet<string> typed,string path,List<ValidationIssue> issues)
        {
            void Check(bool value,string name){if(!value)Add(issues,ValidationCode.InvalidEconomicValue,path+"."+name);}
            bool Identity(string id)=>DefinitionId.TryParse(id,out var value)&&ids.Contains(value)&&typed.Add(id);
            if(d.GovernancePolicies==null||d.PoliticalMemoryEffects==null){Add(issues,ValidationCode.MissingField,path+".Politics");return;}
            foreach(var p in d.GovernancePolicies)
            {
                if(p==null){Check(false,"GovernancePolicies");continue;}
                Check(Identity(p.DefinitionId)&&!string.IsNullOrWhiteSpace(p.DisplayName)&&!string.IsNullOrWhiteSpace(p.Description),p.DefinitionId);
                Check(new[]{p.SatisfactionChange,p.AlignmentChange,p.RadicalisationChange}.All(x=>x>=-500&&x<=500)&&p.IntegrationChange>=-25&&p.IntegrationChange<=25,p.DefinitionId+".Rates");
                Check(new[]{p.CreditsFactor,p.IndustryFactor,p.ResearchFactor,p.LogisticsFactor}.All(x=>x>=0&&x<=20000)&&p.Suppression>=0&&p.Suppression<=10000,p.DefinitionId+".Factors");
                Check(p.LegalTransitions!=null&&p.LegalTransitions.All(t=>t!=null&&Enum.IsDefined(typeof(LegalStatus),t.From)&&Enum.IsDefined(typeof(LegalStatus),t.To)&&t.From!=t.To&&t.RequiredTurns>0&&t.RequiredTurns<=10000
                    &&new[]{t.RequiredAlignment,t.RequiredIntegration,t.RequiredSatisfaction,t.RequiredStability}.All(x=>x>=0&&x<=10000))
                    &&p.LegalTransitions.Select(t=>t.From).Distinct().Count()==p.LegalTransitions.Count,p.DefinitionId+".Transitions");
                // Reject status cycles, not just duplicate source statuses.
                if(p.LegalTransitions!=null&&p.LegalTransitions.All(t=>t!=null))
                    foreach(var start in p.LegalTransitions)
                    {
                        var seen=new HashSet<LegalStatus>();var status=start.From;
                        while(true)
                        {
                            if(!seen.Add(status)){Check(false,p.DefinitionId+".Cycles");break;}
                            var next=p.LegalTransitions.FirstOrDefault(t=>t.From==status);
                            if(next==null)break;
                            status=next.To;
                        }
                    }
            }
            foreach(var e in d.PoliticalMemoryEffects)
            {
                if(e==null){Check(false,"MemoryEffects");continue;}
                Check(Identity(e.DefinitionId)&&DefinitionId.TryParse(e.MemoryKind,out _)&&new[]{e.SatisfactionChange,e.AlignmentChange,e.RadicalisationChange}.All(x=>x>=-500&&x<=500)&&e.IntegrationChange>=-25&&e.IntegrationChange<=25,e.DefinitionId);
            }
            Check(d.PoliticalMemoryEffects.Where(e=>e!=null).Select(e=>e.MemoryKind).Distinct(StringComparer.Ordinal).Count()==d.PoliticalMemoryEffects.Count,"MemoryEffectKinds");
        }
        private static void ValidatePolitics(ColonyStateDto world,GameStateDto state,DefinitionCatalogDto? definitions,string path,List<ValidationIssue> issues)
        {
            var p=world.Politics;if(p==null){Add(issues,ValidationCode.MissingField,path+".Politics");return;}
            void Check(bool valid){if(!valid)Add(issues,ValidationCode.InvalidEconomicValue,path+".Politics");}
            Check(definitions?.GovernancePolicies?.Any(d=>d!=null&&d.DefinitionId==p.GovernancePolicyDefinitionId)==true&&Enum.IsDefined(typeof(ResistanceState),p.Resistance)
                &&new[]{p.Pressure,p.PreviousPressure}.All(x=>x>=0&&x<=10000)&&p.EscalationTurns>=0&&p.EscalationTurns<PoliticsRules.EscalationDelay
                &&p.RecoveryTurns>=0&&p.RecoveryTurns<PoliticsRules.RecoveryDelay&&p.OrganizedTurns>=0&&p.DisruptionRemainingTurns>=0&&p.DisruptionRemainingTurns<=PoliticsRules.DisruptionDuration
                &&p.Disruptions>=0&&p.LastUpdateTurn>=0&&p.LastUpdateTurn<=state.TurnNumber);
            Check(p.CohortReports!=null&&p.Events!=null);
            if(p.CohortReports!=null)
            {
                Check(p.CohortReports.All(r=>r!=null)&&p.CohortReports.Where(r=>r!=null).Select(r=>r.CohortId).Distinct(StringComparer.Ordinal).Count()==p.CohortReports.Count);
                foreach(var r in p.CohortReports.Where(r=>r!=null))Check(EntityId.TryParse(r.CohortId,out _)&&Enum.IsDefined(typeof(LegalStatus),r.PreviousStatus)&&Enum.IsDefined(typeof(LegalStatus),r.ResultStatus)&&r.TransitionProgress>=0
                    &&new[]{r.SatisfactionChange,r.IntegrationChange,r.AlignmentChange,r.RadicalisationChange}.All(x=>x>=-10000&&x<=10000)&&r.Reasons!=null
                    &&r.Reasons.All(reason=>reason!=null&&DefinitionId.TryParse(reason.Code,out _)&&(reason.SourceEntityId==null||EntityId.TryParse(reason.SourceEntityId,out _))));
            }
            if(p.Events!=null)Check(p.Events.All(e=>DefinitionId.TryParse(e,out _)));
            if(world.Cohorts!=null)
                foreach(var cohort in world.Cohorts.Where(c=>c!=null))
                {
                    var policy=definitions?.GovernancePolicies?.FirstOrDefault(d=>d!=null&&d.DefinitionId==p.GovernancePolicyDefinitionId);
                    var transition=policy?.LegalTransitions?.FirstOrDefault(t=>t!=null&&t.From==cohort.LegalStatus);
                    Check(cohort.LegalStatusEligibleTurns>=0&&(cohort.LegalStatusEligibleTurns==0||transition!=null&&cohort.LegalStatusEligibleTurns<transition.RequiredTurns));
                }
        }
    }
}
