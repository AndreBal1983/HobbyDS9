using System;
using System.Collections.Generic;

namespace HobbyDS9.Persistence
{
    public enum RepositoryError { InvalidRequest, WriterInUse, IncompatibleSchema, IncompatibleEdition, Conflict, OperationIdConflict, NotFound, Unavailable, Corrupt, ValidationFailed, WriteFailed, OutcomeUnknown, StorageFull, ReadOnly, Busy }
    public sealed class RepositoryException : Exception
    {
        public RepositoryError Error { get; }
        public string? OperationId { get; }
        public RepositoryException(RepositoryError error, string? operationId = null, Exception? inner = null) : base(error.ToString(), inner) { Error = error; OperationId = operationId; }
    }
    public enum SaveReferenceKind { Manual = 1, Auto = 2, Milestone = 3 }
    public sealed class ReferenceUpdate
    {
        public string Id { get; }
        public long ExpectedGeneration { get; }
        public SaveReferenceKind Kind { get; }
        public string Name { get; }
        public string Notes { get; }
        public ReferenceUpdate(string id, long expectedGeneration, SaveReferenceKind kind, string name, string notes = "")
        { Id = id; ExpectedGeneration = expectedGeneration; Kind = kind; Name = name; Notes = notes; }
    }
    public sealed class CheckpointCommit
    {
        public string OperationId { get; }
        public string BranchId { get; }
        public long ExpectedBranchGeneration { get; }
        public string? ExpectedHead { get; }
        public CheckpointImage Image { get; }
        public ReferenceUpdate? Reference { get; }
        public string SourceEpoch { get; }
        public CheckpointCommit(string operationId, string branchId, long expectedBranchGeneration, string? expectedHead, CheckpointImage image, ReferenceUpdate? reference = null, string sourceEpoch = "")
        { OperationId = operationId; BranchId = branchId; ExpectedBranchGeneration = expectedBranchGeneration; ExpectedHead = expectedHead; Image = image; Reference = reference; SourceEpoch = sourceEpoch; }
    }
    public sealed class BranchInfo
    {
        public string Id { get; }
        public string? Head { get; }
        public long Generation { get; }
        public BranchInfo(string id, string? head, long generation) { Id = id; Head = head; Generation = generation; }
    }
    public sealed class SaveReference
    {
        public string Id { get; }
        public string CheckpointId { get; }
        public long Generation { get; }
        public SaveReferenceKind Kind { get; }
        public string Name { get; }
        public string Notes { get; }
        public int Turn { get; }
        public long Revision { get; }
        public string Phase { get; }
        public long StorageSequence { get; }
        public string UpdatedUtc { get; }
        // Metadata only, not proof of successful current game validation.
        public bool PayloadAvailable { get; }
        public string CheckpointBranchId { get; }
        public bool Quarantined { get; }
        public SaveReference(string id, string checkpointId, long generation, SaveReferenceKind kind, string name, string notes,
            int turn, long revision, string phase, long storageSequence, string updatedUtc, bool payloadAvailable, string checkpointBranchId = "")
            : this(id, checkpointId, generation, kind, name, notes, turn, revision, phase, storageSequence, updatedUtc, payloadAvailable, checkpointBranchId, false) { }
        public SaveReference(string id, string checkpointId, long generation, SaveReferenceKind kind, string name, string notes,
            int turn, long revision, string phase, long storageSequence, string updatedUtc, bool payloadAvailable, string checkpointBranchId, bool quarantined)
        { Id = id; CheckpointId = checkpointId; Generation = generation; Kind = kind; Name = name; Notes = notes; Turn = turn; Revision = revision; Phase = phase; StorageSequence = storageSequence; UpdatedUtc = updatedUtc; PayloadAvailable = payloadAvailable; CheckpointBranchId = checkpointBranchId; Quarantined = quarantined; }
    }
    public sealed class RepositoryUsage
    {
        public long LogicalPayloadBytes { get; }
        public long AutomaticPayloadBytes { get; }
        public long ProtectedPayloadBytes { get; }
        public long QuarantinedPayloadBytes { get; }
        public long PhysicalDatabaseBytes { get; }
        public long WalBytes { get; }
        public long SoftAutomaticBudgetBytes { get; }
        public bool AutomaticBudgetExceeded => AutomaticPayloadBytes > SoftAutomaticBudgetBytes;
        public RepositoryUsage(long logical, long automatic, long protectedBytes, long physical, long wal, long softBudget)
            : this(logical, automatic, protectedBytes, physical, wal, softBudget, 0) { }
        public RepositoryUsage(long logical, long automatic, long protectedBytes, long physical, long wal, long softBudget, long quarantined)
        { LogicalPayloadBytes = logical; AutomaticPayloadBytes = automatic; ProtectedPayloadBytes = protectedBytes; PhysicalDatabaseBytes = physical; WalBytes = wal; SoftAutomaticBudgetBytes = softBudget; QuarantinedPayloadBytes = quarantined; }
    }
    public sealed class DurableReceipt
    {
        public string OperationId { get; }
        public string Fingerprint { get; }
        public long StorageSequence { get; }
        public string? CheckpointId { get; }
        public long BranchGeneration { get; }
        public long ReferenceGeneration { get; }
        public string SourceEpoch { get; }
        public string CommittedUtc { get; }
        public DurableReceipt(string operationId, string fingerprint, long sequence, string? checkpointId, long branchGeneration, long referenceGeneration, string sourceEpoch, string committedUtc)
        { OperationId = operationId; Fingerprint = fingerprint; StorageSequence = sequence; CheckpointId = checkpointId; BranchGeneration = branchGeneration; ReferenceGeneration = referenceGeneration; SourceEpoch = sourceEpoch; CommittedUtc = committedUtc; }
    }
    public interface ICampaignRepository : IDisposable
    {
        DurableReceipt CommitCheckpoint(CheckpointCommit request);
        DurableReceipt ForkBranch(string operationId, string branchId, string originCheckpointId);
        DurableReceipt SetReference(string operationId, ReferenceUpdate reference, string checkpointId);
        DurableReceipt DeleteReference(string operationId, string referenceId, long expectedGeneration);
        DurableReceipt? FindOperation(string operationId);
        BranchInfo GetBranch(string branchId);
        IReadOnlyList<SaveReference> ListReferences(string afterId = "", int limit = 50);
        ReferencePage QueryReferences(ReferenceQuery query);
        CheckpointImage ReadCheckpoint(string checkpointId);
        DurableReceipt ImportJson(string absoluteSourceFile);
        string ExportJson(string checkpointId, string exportId, bool overwrite = false);
        string Backup(string backupId);
        DurableReceipt ApplyRetention(string operationId, BranchInfo activeBranch, int rolling = 10, int milestones = 3, long softAutoBytes = 512L * 1024 * 1024);
        RepositoryUsage GetStorageUsage(BranchInfo activeBranch, long softAutoBytes = 512L * 1024 * 1024);
    }
}
