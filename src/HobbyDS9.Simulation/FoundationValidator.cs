using System;
using System.Collections.Generic;
using System.Globalization;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Non-mutating structural/identity validation only; gameplay validation comes
    // with future schemas. Diagnostics retain input paths and stable error codes.
    public static partial class FoundationValidator
    {
        public static ValidationReport ValidateRngState(RngStateDto? dto)
        {
            var issues = new List<ValidationIssue>();
            ValidateRng(dto, "$.RngState", issues);
            return new ValidationReport(issues);
        }

        public static ValidationReport ValidateAllocationState(EntityAllocationStateDto? dto)
        {
            var issues = new List<ValidationIssue>();
            if (dto == null) Add(issues, ValidationCode.MissingField, "$.Allocation");
            else if (dto.NextEntitySequence == 0) Add(issues, ValidationCode.InvalidCounter, "$.Allocation.NextEntitySequence");
            return new ValidationReport(issues);
        }

        public static ValidationReport ValidateGameState(GameStateDto? dto, DefinitionCatalogDto? definitions)
        {
            var issues = new List<ValidationIssue>();
            var ids = ValidateDefinitions(definitions, "$.Definitions", issues);
            ValidateState(dto, ids, false, "$.GameState", issues);
            ValidateTypedState(dto, definitions, "$.GameState", issues);
            return new ValidationReport(issues);
        }

        public static ValidationReport ValidateScenario(ScenarioDto? dto)
        {
            var issues = new List<ValidationIssue>();
            if (dto == null) Add(issues, ValidationCode.MissingField, "$.Scenario");
            else
            {
                ValidateMetadata(dto.ScenarioId, dto.RulesVersion, dto.ContentHash, "$", issues);
                var ids = ValidateDefinitions(dto.Definitions, "$.Definitions", issues);
                ValidateState(dto.InitialState, ids, true, "$.InitialState", issues);
                ValidateTypedState(dto.InitialState, dto.Definitions, "$.InitialState", issues);
                if (dto.InitialState != null && (dto.InitialState.TurnNumber != 1 || dto.InitialState.StateRevision != 0
                    || dto.InitialState.NextEntitySequence != 1)) Add(issues, ValidationCode.InvalidCounter, "$.InitialState");
                if (dto.InitialState?.AcceptedCommandIds != null && dto.InitialState.AcceptedCommandIds.Count != 0)
                    Add(issues, ValidationCode.InvalidCounter, "$.InitialState.AcceptedCommandIds");
            }
            return new ValidationReport(issues);
        }

        public static ValidationReport ValidateSave(SaveEnvelopeDto? dto, DefinitionCatalogDto? definitions, SaveCompatibility expected)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            var issues = new List<ValidationIssue>();
            if (dto == null) Add(issues, ValidationCode.MissingField, "$.Save");
            else
            {
                if (dto.SaveFormatVersion != FoundationVersions.SaveFormatVersion)
                    Add(issues, ValidationCode.UnsupportedSaveFormat, "$.SaveFormatVersion");
                ValidateMetadata(dto.ScenarioId, dto.RulesVersion, dto.ContentHash, "$", issues);
                if (!StringComparer.Ordinal.Equals(dto.ScenarioId, expected.ScenarioId)) Add(issues, ValidationCode.ScenarioMismatch, "$.ScenarioId");
                if (!StringComparer.Ordinal.Equals(dto.RulesVersion, expected.RulesVersion)) Add(issues, ValidationCode.RulesMismatch, "$.RulesVersion");
                if (!StringComparer.Ordinal.Equals(dto.ContentHash, expected.ContentHash)) Add(issues, ValidationCode.ContentMismatch, "$.ContentHash");
                var ids = ValidateDefinitions(definitions, "$.Definitions", issues);
                ValidateState(dto.GameState, ids, false, "$.GameState", issues);
                ValidateTypedState(dto.GameState, definitions, "$.GameState", issues);
            }
            return new ValidationReport(issues);
        }

        internal static bool IsContentHash(string? value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (var c in value) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return true;
        }

        private static void ValidateMetadata(string scenario, string rules, string hash, string path, List<ValidationIssue> issues)
        {
            if (!EntityId.TryParse(scenario, out var id)) Add(issues, ValidationCode.InvalidId, path + ".ScenarioId");
            else if (id.UsesAllocationNamespace) Add(issues, ValidationCode.ReservedIdNamespace, path + ".ScenarioId");
            if (string.IsNullOrWhiteSpace(rules)) Add(issues, ValidationCode.MissingField, path + ".RulesVersion");
            if (!IsContentHash(hash)) Add(issues, ValidationCode.InvalidContentHash, path + ".ContentHash");
        }

        private static void ValidateRng(RngStateDto? dto, string path, List<ValidationIssue> issues)
        {
            if (dto == null) { Add(issues, ValidationCode.MissingField, path); return; }
            if (dto.AlgorithmId != FoundationVersions.RandomAlgorithmId) Add(issues, ValidationCode.UnsupportedRngAlgorithm, path + ".AlgorithmId");
            if (dto.AlgorithmVersion != FoundationVersions.RandomAlgorithmVersion) Add(issues, ValidationCode.UnsupportedRngVersion, path + ".AlgorithmVersion");
            if (!dto.State.HasValue) Add(issues, ValidationCode.MissingField, path + ".State");
        }

        private static HashSet<DefinitionId> ValidateDefinitions(DefinitionCatalogDto? dto, string path, List<ValidationIssue> issues)
        {
            var ids = new HashSet<DefinitionId>();
            if (dto == null || dto.DefinitionIds == null) { Add(issues, ValidationCode.MissingField, path); return ids; }
            for (var i = 0; i < dto.DefinitionIds.Count; i++)
            {
                var value = dto.DefinitionIds[i];
                var itemPath = path + ".DefinitionIds[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (!DefinitionId.TryParse(value, out var id))
                {
                    var code = EntityId.TryParse(value, out var entityId) && entityId.UsesAllocationNamespace
                        ? ValidationCode.ReservedIdNamespace : ValidationCode.InvalidId;
                    Add(issues, code, itemPath);
                }
                else if (!ids.Add(id)) Add(issues, ValidationCode.DuplicateId, itemPath);
            }
            ValidateTypedDefinitions(dto, ids, path, issues);
            return ids;
        }

        private static void ValidateState(GameStateDto? dto, HashSet<DefinitionId> definitions, bool authored,
            string path, List<ValidationIssue> issues)
        {
            if (dto == null) { Add(issues, ValidationCode.MissingField, path); return; }
            if (!dto.Phase.HasValue) Add(issues, ValidationCode.MissingField, path + ".Phase");
            else if (dto.Phase != SessionPhase.Planning && dto.Phase != SessionPhase.AwaitingCombat)
                Add(issues, ValidationCode.InvalidSessionPhase, path + ".Phase");
            else if (authored && dto.Phase != SessionPhase.Planning)
                Add(issues, ValidationCode.InvalidSessionPhase, path + ".Phase");
            if (dto.TurnNumber <= 0) Add(issues, ValidationCode.InvalidCounter, path + ".TurnNumber");
            if (dto.StateRevision < 0) Add(issues, ValidationCode.InvalidCounter, path + ".StateRevision");
            if (dto.NextEntitySequence == 0) Add(issues, ValidationCode.InvalidCounter, path + ".NextEntitySequence");
            ValidateRng(dto.RngState, path + ".RngState", issues);
            if (dto.AcceptedCommandIds == null) Add(issues, ValidationCode.MissingField, path + ".AcceptedCommandIds");
            else
            {
                var commands = new HashSet<EntityId>();
                for (var i = 0; i < dto.AcceptedCommandIds.Count; i++)
                {
                    var itemPath = path + ".AcceptedCommandIds[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                    if (!EntityId.TryParse(dto.AcceptedCommandIds[i], out var commandId)) Add(issues, ValidationCode.InvalidId, itemPath);
                    else if (!commands.Add(commandId)) Add(issues, ValidationCode.DuplicateId, itemPath);
                }
                if (dto.StateRevision >= 0 && dto.AcceptedCommandIds.Count > dto.StateRevision) Add(issues, ValidationCode.InvalidCounter, path + ".AcceptedCommandIds");
            }
            if (dto.EntityIdentities == null) { Add(issues, ValidationCode.MissingField, path + ".EntityIdentities"); return; }
            var entities = new HashSet<EntityId>();
            var allocatedSequences = new HashSet<ulong>();
            for (var i = 0; i < dto.EntityIdentities.Count; i++)
            {
                var entity = dto.EntityIdentities[i];
                var itemPath = path + ".EntityIdentities[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (entity == null) { Add(issues, ValidationCode.MissingField, itemPath); continue; }
                if (!EntityId.TryParse(entity.EntityId, out var id)) Add(issues, ValidationCode.InvalidId, itemPath + ".EntityId");
                else
                {
                    if (!entities.Add(id)) Add(issues, ValidationCode.DuplicateId, itemPath + ".EntityId");
                    if (id.UsesAllocationNamespace)
                    {
                        if (authored) Add(issues, ValidationCode.ReservedIdNamespace, itemPath + ".EntityId");
                        if (!id.TryGetAllocationSequence(out var sequence)) Add(issues, ValidationCode.InvalidAllocatedId, itemPath + ".EntityId");
                        else
                        {
                            if (!allocatedSequences.Add(sequence)) Add(issues, ValidationCode.DuplicateId, itemPath + ".EntityId");
                            if (sequence >= dto.NextEntitySequence) Add(issues, ValidationCode.AllocationSequenceMismatch, itemPath + ".EntityId");
                        }
                    }
                }
                if (entity.DefinitionId != null)
                {
                    if (!DefinitionId.TryParse(entity.DefinitionId, out var definitionId)) Add(issues, ValidationCode.InvalidId, itemPath + ".DefinitionId");
                    else if (!definitions.Contains(definitionId)) Add(issues, ValidationCode.MissingDefinition, itemPath + ".DefinitionId");
                }
            }
        }

        private static void Add(List<ValidationIssue> issues, ValidationCode code, string path) => issues.Add(new ValidationIssue(code, path));
    }
}
