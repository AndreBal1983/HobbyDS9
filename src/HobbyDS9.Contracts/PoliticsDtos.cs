using System.Collections.Generic;
namespace HobbyDS9.Contracts
{
    public enum ResistanceState { Quiet = 0, Tense = 1, Unrest = 2, OrganizedResistance = 3 }
    public sealed class GovernancePolicyDefinitionDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SatisfactionChange { get; set; }
        public int IntegrationChange { get; set; }
        public int AlignmentChange { get; set; }
        public int RadicalisationChange { get; set; }
        public int CreditsFactor { get; set; }
        public int IndustryFactor { get; set; }
        public int ResearchFactor { get; set; }
        public int LogisticsFactor { get; set; }
        public int Suppression { get; set; }
        public bool AcceptsRefugees { get; set; }
        public List<LegalTransitionDefinitionDto>? LegalTransitions { get; set; }
    }
    public sealed class LegalTransitionDefinitionDto
    {
        public LegalStatus From { get; set; }
        public LegalStatus To { get; set; }
        public int RequiredIntegration { get; set; }
        public int RequiredSatisfaction { get; set; }
        public int RequiredAlignment { get; set; }
        public int RequiredStability { get; set; }
        public int RequiredTurns { get; set; }
    }
    public sealed class PoliticalMemoryEffectDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string MemoryKind { get; set; } = string.Empty;
        public int SatisfactionChange { get; set; }
        public int IntegrationChange { get; set; }
        public int AlignmentChange { get; set; }
        public int RadicalisationChange { get; set; }
    }
    public sealed class PoliticalReasonDto
    {
        public string Code { get; set; } = string.Empty;
        public string? SourceEntityId { get; set; }
        public int SatisfactionChange { get; set; }
        public int IntegrationChange { get; set; }
        public int AlignmentChange { get; set; }
        public int RadicalisationChange { get; set; }
    }
    public sealed class CohortPoliticalReportDto
    {
        public string CohortId { get; set; } = string.Empty;
        public LegalStatus PreviousStatus { get; set; }
        public LegalStatus ResultStatus { get; set; }
        public int TransitionProgress { get; set; }
        public int SatisfactionChange { get; set; }
        public int IntegrationChange { get; set; }
        public int AlignmentChange { get; set; }
        public int RadicalisationChange { get; set; }
        public List<PoliticalReasonDto>? Reasons { get; set; }
    }
    public sealed class WorldPoliticalStateDto
    {
        public string GovernancePolicyDefinitionId { get; set; } = string.Empty;
        public ResistanceState Resistance { get; set; }
        public int Pressure { get; set; }
        public int PreviousPressure { get; set; }
        public int EscalationTurns { get; set; }
        public int RecoveryTurns { get; set; }
        public int OrganizedTurns { get; set; }
        public int DisruptionRemainingTurns { get; set; }
        public long Disruptions { get; set; }
        public int LastUpdateTurn { get; set; }
        public List<CohortPoliticalReportDto>? CohortReports { get; set; }
        public List<string>? Events { get; set; }
    }
    public sealed class SetWorldGovernancePolicyPayload
    {
        public string ColonyId { get; set; } = string.Empty;
        public string PolicyDefinitionId { get; set; } = string.Empty;
    }
}
