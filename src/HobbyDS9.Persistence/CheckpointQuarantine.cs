using System;
using System.Collections.Generic;

namespace HobbyDS9.Persistence
{
    // A matching, available host validator may attest that these exact bytes are
    // definitively invalid. Missing content/edition, cancellation, resource and
    // transient failures must propagate separately.
    public sealed class InvalidCheckpointPayloadException : Exception
    {
        public InvalidCheckpointPayloadException(string message, Exception? innerException = null)
            : base(message, innerException) { }
    }

    public enum CheckpointCorruptionKind { StoredIntegrity = 1, InvalidPayload = 2, MetadataMismatch = 3 }

    public sealed class QuarantinedCheckpoint
    {
        public string CheckpointId { get; }
        public CheckpointCorruptionKind Kind { get; }
        public string DetectedUtc { get; }
        public long ObservedPayloadBytes { get; }
        public string? ObservedPayloadHash { get; }
        public bool CanExport => ObservedPayloadHash != null;

        public QuarantinedCheckpoint(string checkpointId, CheckpointCorruptionKind kind, string detectedUtc,
            long observedPayloadBytes, string? observedPayloadHash)
        {
            CheckpointId = checkpointId;
            Kind = kind;
            DetectedUtc = detectedUtc;
            ObservedPayloadBytes = observedPayloadBytes;
            ObservedPayloadHash = observedPayloadHash;
        }
    }

    // Diagnostic capability. Export puts the exact observed bytes in a new owned
    // .bin file and leaves the evidence pinned. It does not produce a playable save.
    public interface IQuarantinedCheckpointRepository
    {
        IReadOnlyList<QuarantinedCheckpoint> ListQuarantinedCheckpoints(string afterId = "", int limit = 50);
        string ExportQuarantinedPayload(string checkpointId, string exportId);
    }
}
