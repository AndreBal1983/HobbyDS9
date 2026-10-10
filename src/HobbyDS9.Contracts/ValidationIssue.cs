namespace HobbyDS9.Contracts
{
    public enum ValidationCode
    {
        MissingField, InvalidId, ReservedIdNamespace, DuplicateId, MissingDefinition,
        InvalidCounter, InvalidAllocatedId, AllocationSequenceMismatch, InvalidContentHash,
        UnsupportedSaveFormat, UnsupportedRngAlgorithm, UnsupportedRngVersion,
        RulesMismatch, ScenarioMismatch, ContentMismatch, UnsupportedContentFormat,
        InvalidControllerKind, InvalidEdgeKind, MissingEntity, MissingCivilization,
        IdentityMismatch, SelfEdge, DuplicateEdge, MissingEndpoint, InvalidSessionPhase, InvalidEconomicValue, MissingSystem, OwnershipMismatch, DuplicateColonySystem, InvalidResearchValue, SelfPrerequisite, ResearchCycle,
        ResearchAlreadyUnlocked, ResearchPrerequisiteMissing, InvalidProductionValue, InvalidShipValue, InvalidFleetMembership, InvalidFleetRoute, InvalidCombatCheckpoint, CombatIdentityMismatch, InvalidCombatReference
    }

    public sealed class ValidationIssue
    {
        public ValidationCode Code { get; }
        public string Path { get; }
        public ValidationIssue(ValidationCode code, string path) { Code = code; Path = path; }
    }
}
