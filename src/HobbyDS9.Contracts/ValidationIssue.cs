namespace HobbyDS9.Contracts
{
    public enum ValidationCode
    {
        MissingField, InvalidId, ReservedIdNamespace, DuplicateId, MissingDefinition,
        InvalidCounter, InvalidAllocatedId, AllocationSequenceMismatch, InvalidContentHash,
        UnsupportedSaveFormat, UnsupportedRngAlgorithm, UnsupportedRngVersion,
        RulesMismatch, ScenarioMismatch, ContentMismatch
    }

    public sealed class ValidationIssue
    {
        public ValidationCode Code { get; }
        public string Path { get; }
        public ValidationIssue(ValidationCode code, string path) { Code = code; Path = path; }
    }
}
