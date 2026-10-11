using System.Collections.Generic;
using System.Linq;
namespace HobbyDS9.Contracts
{
    public sealed class GovernancePolicyDefinitionSnapshot
    {
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int SatisfactionChange { get; }
        public int IntegrationChange { get; }
        public int AlignmentChange { get; }
        public int RadicalisationChange { get; }
        public int CreditsFactor { get; }
        public int IndustryFactor { get; }
        public int ResearchFactor { get; }
        public int LogisticsFactor { get; }
        public int Suppression { get; }
        public bool AcceptsRefugees { get; }
        public IReadOnlyList<LegalTransitionDefinitionSnapshot> LegalTransitions { get; }
        public GovernancePolicyDefinitionSnapshot(GovernancePolicyDefinitionDto x)
        {
            DefinitionId = x.DefinitionId;
            DisplayName = x.DisplayName;
            Description = x.Description;
            SatisfactionChange = x.SatisfactionChange;
            IntegrationChange = x.IntegrationChange;
            AlignmentChange = x.AlignmentChange;
            RadicalisationChange = x.RadicalisationChange;
            CreditsFactor = x.CreditsFactor;
            IndustryFactor = x.IndustryFactor;
            ResearchFactor = x.ResearchFactor;
            LogisticsFactor = x.LogisticsFactor;
            Suppression = x.Suppression;
            AcceptsRefugees = x.AcceptsRefugees;
            LegalTransitions = x.LegalTransitions!.Select(v=>new LegalTransitionDefinitionSnapshot(v)).ToList().AsReadOnly();
        }
    }
    public sealed class LegalTransitionDefinitionSnapshot
    {
        public LegalStatus From { get; }
        public LegalStatus To { get; }
        public int RequiredIntegration { get; }
        public int RequiredSatisfaction { get; }
        public int RequiredAlignment { get; }
        public int RequiredStability { get; }
        public int RequiredTurns { get; }
        public LegalTransitionDefinitionSnapshot(LegalTransitionDefinitionDto x)
        {
            From = x.From;
            To = x.To;
            RequiredIntegration = x.RequiredIntegration;
            RequiredSatisfaction = x.RequiredSatisfaction;
            RequiredAlignment = x.RequiredAlignment;
            RequiredStability = x.RequiredStability;
            RequiredTurns = x.RequiredTurns;
        }
    }
    public sealed class PoliticalMemoryEffectSnapshot
    {
        public string DefinitionId { get; }
        public string MemoryKind { get; }
        public int SatisfactionChange { get; }
        public int IntegrationChange { get; }
        public int AlignmentChange { get; }
        public int RadicalisationChange { get; }
        public PoliticalMemoryEffectSnapshot(PoliticalMemoryEffectDto x)
        {
            DefinitionId = x.DefinitionId;
            MemoryKind = x.MemoryKind;
            SatisfactionChange = x.SatisfactionChange;
            IntegrationChange = x.IntegrationChange;
            AlignmentChange = x.AlignmentChange;
            RadicalisationChange = x.RadicalisationChange;
        }
    }
    public sealed class PoliticalReasonSnapshot
    {
        public string Code { get; }
        public string? SourceEntityId { get; }
        public int SatisfactionChange { get; }
        public int IntegrationChange { get; }
        public int AlignmentChange { get; }
        public int RadicalisationChange { get; }
        public PoliticalReasonSnapshot(PoliticalReasonDto x)
        {
            Code = x.Code;
            SourceEntityId = x.SourceEntityId;
            SatisfactionChange = x.SatisfactionChange;
            IntegrationChange = x.IntegrationChange;
            AlignmentChange = x.AlignmentChange;
            RadicalisationChange = x.RadicalisationChange;
        }
    }
    public sealed class CohortPoliticalReportSnapshot
    {
        public string CohortId { get; }
        public LegalStatus PreviousStatus { get; }
        public LegalStatus ResultStatus { get; }
        public int TransitionProgress { get; }
        public int SatisfactionChange { get; }
        public int IntegrationChange { get; }
        public int AlignmentChange { get; }
        public int RadicalisationChange { get; }
        public IReadOnlyList<PoliticalReasonSnapshot> Reasons { get; }
        public CohortPoliticalReportSnapshot(CohortPoliticalReportDto x)
        {
            CohortId = x.CohortId;
            PreviousStatus = x.PreviousStatus;
            ResultStatus = x.ResultStatus;
            TransitionProgress = x.TransitionProgress;
            SatisfactionChange = x.SatisfactionChange;
            IntegrationChange = x.IntegrationChange;
            AlignmentChange = x.AlignmentChange;
            RadicalisationChange = x.RadicalisationChange;
            Reasons = x.Reasons!.Select(v=>new PoliticalReasonSnapshot(v)).ToList().AsReadOnly();
        }
    }
    public sealed class WorldPoliticalStateSnapshot
    {
        public string GovernancePolicyDefinitionId { get; }
        public ResistanceState Resistance { get; }
        public int Pressure { get; }
        public int PreviousPressure { get; }
        public int EscalationTurns { get; }
        public int RecoveryTurns { get; }
        public int OrganizedTurns { get; }
        public int DisruptionRemainingTurns { get; }
        public long Disruptions { get; }
        public int LastUpdateTurn { get; }
        public IReadOnlyList<CohortPoliticalReportSnapshot> CohortReports { get; }
        public IReadOnlyList<string> Events { get; }
        public WorldPoliticalStateSnapshot(WorldPoliticalStateDto x)
        {
            GovernancePolicyDefinitionId = x.GovernancePolicyDefinitionId;
            Resistance = x.Resistance;
            Pressure = x.Pressure;
            PreviousPressure = x.PreviousPressure;
            EscalationTurns = x.EscalationTurns;
            RecoveryTurns = x.RecoveryTurns;
            OrganizedTurns = x.OrganizedTurns;
            DisruptionRemainingTurns = x.DisruptionRemainingTurns;
            Disruptions = x.Disruptions;
            LastUpdateTurn = x.LastUpdateTurn;
            CohortReports = x.CohortReports!.Select(v=>new CohortPoliticalReportSnapshot(v)).ToList().AsReadOnly();
            Events = x.Events!.ToList().AsReadOnly();
        }
    }
}
