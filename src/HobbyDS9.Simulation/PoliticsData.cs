using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static partial class PopulationData
    {
        internal static GovernancePolicyDefinitionDto Copy(GovernancePolicyDefinitionDto x) => new GovernancePolicyDefinitionDto
        {
            DefinitionId = x.DefinitionId,
            DisplayName = x.DisplayName,
            Description = x.Description,
            SatisfactionChange = x.SatisfactionChange,
            IntegrationChange = x.IntegrationChange,
            AlignmentChange = x.AlignmentChange,
            RadicalisationChange = x.RadicalisationChange,
            CreditsFactor = x.CreditsFactor,
            IndustryFactor = x.IndustryFactor,
            ResearchFactor = x.ResearchFactor,
            LogisticsFactor = x.LogisticsFactor,
            Suppression = x.Suppression,
            AcceptsRefugees = x.AcceptsRefugees,
            LegalTransitions = x.LegalTransitions!.OrderBy(t=>t.From).Select(Copy).ToList(),
        };
        internal static bool Same(GovernancePolicyDefinitionDto? a,GovernancePolicyDefinitionDto? b) => a==null ? b==null : b!=null && a.DefinitionId==b.DefinitionId
            && a.DisplayName==b.DisplayName
            && a.Description==b.Description
            && a.SatisfactionChange==b.SatisfactionChange
            && a.IntegrationChange==b.IntegrationChange
            && a.AlignmentChange==b.AlignmentChange
            && a.RadicalisationChange==b.RadicalisationChange
            && a.CreditsFactor==b.CreditsFactor
            && a.IndustryFactor==b.IndustryFactor
            && a.ResearchFactor==b.ResearchFactor
            && a.LogisticsFactor==b.LogisticsFactor
            && a.Suppression==b.Suppression
            && a.AcceptsRefugees==b.AcceptsRefugees
            && (a.LegalTransitions==null ? b.LegalTransitions==null : b.LegalTransitions!=null && a.LegalTransitions.Count==b.LegalTransitions.Count && a.LegalTransitions.Zip(b.LegalTransitions,(x,y)=>Same(x,y)).All(v=>v));
        internal static LegalTransitionDefinitionDto Copy(LegalTransitionDefinitionDto x) => new LegalTransitionDefinitionDto
        {
            From = x.From,
            To = x.To,
            RequiredIntegration = x.RequiredIntegration,
            RequiredSatisfaction = x.RequiredSatisfaction,
            RequiredAlignment = x.RequiredAlignment,
            RequiredStability = x.RequiredStability,
            RequiredTurns = x.RequiredTurns,
        };
        internal static bool Same(LegalTransitionDefinitionDto? a,LegalTransitionDefinitionDto? b) => a==null ? b==null : b!=null && a.From==b.From
            && a.To==b.To
            && a.RequiredIntegration==b.RequiredIntegration
            && a.RequiredSatisfaction==b.RequiredSatisfaction
            && a.RequiredAlignment==b.RequiredAlignment
            && a.RequiredStability==b.RequiredStability
            && a.RequiredTurns==b.RequiredTurns;
        internal static PoliticalMemoryEffectDto Copy(PoliticalMemoryEffectDto x) => new PoliticalMemoryEffectDto
        {
            DefinitionId = x.DefinitionId,
            MemoryKind = x.MemoryKind,
            SatisfactionChange = x.SatisfactionChange,
            IntegrationChange = x.IntegrationChange,
            AlignmentChange = x.AlignmentChange,
            RadicalisationChange = x.RadicalisationChange,
        };
        internal static bool Same(PoliticalMemoryEffectDto? a,PoliticalMemoryEffectDto? b) => a==null ? b==null : b!=null && a.DefinitionId==b.DefinitionId
            && a.MemoryKind==b.MemoryKind
            && a.SatisfactionChange==b.SatisfactionChange
            && a.IntegrationChange==b.IntegrationChange
            && a.AlignmentChange==b.AlignmentChange
            && a.RadicalisationChange==b.RadicalisationChange;
        internal static PoliticalReasonDto Copy(PoliticalReasonDto x) => new PoliticalReasonDto
        {
            Code = x.Code,
            SourceEntityId = x.SourceEntityId,
            SatisfactionChange = x.SatisfactionChange,
            IntegrationChange = x.IntegrationChange,
            AlignmentChange = x.AlignmentChange,
            RadicalisationChange = x.RadicalisationChange,
        };
        internal static bool Same(PoliticalReasonDto? a,PoliticalReasonDto? b) => a==null ? b==null : b!=null && a.Code==b.Code
            && a.SourceEntityId==b.SourceEntityId
            && a.SatisfactionChange==b.SatisfactionChange
            && a.IntegrationChange==b.IntegrationChange
            && a.AlignmentChange==b.AlignmentChange
            && a.RadicalisationChange==b.RadicalisationChange;
        internal static CohortPoliticalReportDto Copy(CohortPoliticalReportDto x) => new CohortPoliticalReportDto
        {
            CohortId = x.CohortId,
            PreviousStatus = x.PreviousStatus,
            ResultStatus = x.ResultStatus,
            TransitionProgress = x.TransitionProgress,
            SatisfactionChange = x.SatisfactionChange,
            IntegrationChange = x.IntegrationChange,
            AlignmentChange = x.AlignmentChange,
            RadicalisationChange = x.RadicalisationChange,
            Reasons = x.Reasons!.Select(Copy).ToList(),
        };
        internal static bool Same(CohortPoliticalReportDto? a,CohortPoliticalReportDto? b) => a==null ? b==null : b!=null && a.CohortId==b.CohortId
            && a.PreviousStatus==b.PreviousStatus
            && a.ResultStatus==b.ResultStatus
            && a.TransitionProgress==b.TransitionProgress
            && a.SatisfactionChange==b.SatisfactionChange
            && a.IntegrationChange==b.IntegrationChange
            && a.AlignmentChange==b.AlignmentChange
            && a.RadicalisationChange==b.RadicalisationChange
            && (a.Reasons==null ? b.Reasons==null : b.Reasons!=null && a.Reasons.Count==b.Reasons.Count && a.Reasons.Zip(b.Reasons,(x,y)=>Same(x,y)).All(v=>v));
        internal static WorldPoliticalStateDto Copy(WorldPoliticalStateDto x) => new WorldPoliticalStateDto
        {
            GovernancePolicyDefinitionId = x.GovernancePolicyDefinitionId,
            Resistance = x.Resistance,
            Pressure = x.Pressure,
            PreviousPressure = x.PreviousPressure,
            EscalationTurns = x.EscalationTurns,
            RecoveryTurns = x.RecoveryTurns,
            OrganizedTurns = x.OrganizedTurns,
            DisruptionRemainingTurns = x.DisruptionRemainingTurns,
            Disruptions = x.Disruptions,
            LastUpdateTurn = x.LastUpdateTurn,
            CohortReports = x.CohortReports!.Select(Copy).ToList(),
            Events = x.Events!.ToList(),
        };
        internal static bool Same(WorldPoliticalStateDto? a,WorldPoliticalStateDto? b) => a==null ? b==null : b!=null && a.GovernancePolicyDefinitionId==b.GovernancePolicyDefinitionId
            && a.Resistance==b.Resistance
            && a.Pressure==b.Pressure
            && a.PreviousPressure==b.PreviousPressure
            && a.EscalationTurns==b.EscalationTurns
            && a.RecoveryTurns==b.RecoveryTurns
            && a.OrganizedTurns==b.OrganizedTurns
            && a.DisruptionRemainingTurns==b.DisruptionRemainingTurns
            && a.Disruptions==b.Disruptions
            && a.LastUpdateTurn==b.LastUpdateTurn
            && (a.CohortReports==null ? b.CohortReports==null : b.CohortReports!=null && a.CohortReports.Count==b.CohortReports.Count && a.CohortReports.Zip(b.CohortReports,(x,y)=>Same(x,y)).All(v=>v))
            && (a.Events==null ? b.Events==null : b.Events!=null && a.Events.Count==b.Events.Count && a.Events.SequenceEqual(b.Events));
    }
}
