using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HobbyDS9.Persistence;
using Microsoft.Data.Sqlite;

namespace HobbyDS9.Persistence.Sqlite
{
    public sealed partial class SqliteCampaignRepository
    {
        private const string IsQuarantinedSql = "EXISTS(SELECT 1 FROM CheckpointQuarantine q WHERE q.CheckpointId=c.Id)";
        private const string QuarantineColumns = "CheckpointId,Kind,DetectedUtc,ObservedPayloadBytes,ObservedPayloadHash";
        private const string BoundedPayloadSql = "CASE WHEN typeof(Payload)='blob' AND length(Payload) BETWEEN 1 AND 67108864 THEN Payload ELSE NULL END";
        private readonly HashSet<string> pendingQuarantine = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyList<QuarantinedCheckpoint> ListQuarantinedCheckpoints(string afterId = "", int limit = 50)
        {
            if (afterId != "") Id(afterId);
            if (limit < 1 || limit > 500) throw Error(RepositoryError.InvalidRequest);
            lock (gate)
            {
                Alive();
                var result = new List<QuarantinedCheckpoint>();
                using var command = Command("SELECT " + QuarantineReadColumns + " FROM CheckpointQuarantine WHERE CheckpointId>$after ORDER BY CheckpointId COLLATE BINARY LIMIT $limit",
                    null, ("$after", afterId), ("$limit", limit));
                using var reader = command.ExecuteReader();
                while (reader.Read()) result.Add(ReadQuarantineRow(reader));
                return result.AsReadOnly();
            }
        }

        public string ExportQuarantinedPayload(string checkpointId, string exportId)
        {
            Id(checkpointId);
            Id(exportId);
            lock (gate)
            {
                Alive();
                var record = ReadQuarantine(checkpointId) ?? throw Error(RepositoryError.NotFound);
                if (!record.CanExport) throw Error(RepositoryError.Unavailable);
                var observed = ReadEvidence(checkpointId, null);
                if (observed.Bytes == null || observed.Length != record.ObservedPayloadBytes ||
                    CheckpointImage.Hash(observed.Bytes) != record.ObservedPayloadHash)
                    throw Error(RepositoryError.Corrupt);
                var destination = Path.Combine(SubDirectory("quarantine-exports"), exportId + ".bin");
                AtomicWrite(destination, observed.Bytes, false);
                return destination;
            }
        }

        private static string QuarantineReadColumns => Field("CheckpointId", 96) + ",Kind," + Field("DetectedUtc", 64)
            + ",ObservedPayloadBytes," + Field("ObservedPayloadHash", 64) + ",ObservedPayloadHash IS NULL";

        private QuarantinedCheckpoint? ReadQuarantine(string checkpointId, SqliteTransaction? tx = null)
        {
            using var command = Command("SELECT " + QuarantineReadColumns + " FROM CheckpointQuarantine WHERE CheckpointId=$id",
                tx, ("$id", checkpointId));
            using var reader = command.ExecuteReader();
            return reader.Read() ? ReadQuarantineRow(reader) : null;
        }

        private static QuarantinedCheckpoint ReadQuarantineRow(SqliteDataReader reader)
        {
            var kind = (CheckpointCorruptionKind)reader.GetInt32(1);
            if (!Enum.IsDefined(typeof(CheckpointCorruptionKind), kind)) throw Error(RepositoryError.Corrupt);
            return new QuarantinedCheckpoint(ReadText(reader, 0, 96), kind, ReadText(reader, 2, 64),
                reader.GetInt64(3), reader.GetInt32(5) == 1 ? null : ReadText(reader, 4, 64));
        }

        private (byte[]? Bytes, long Length) ReadEvidence(string checkpointId, SqliteTransaction? tx)
        {
            using var command = Command("SELECT " + BoundedPayloadSql + ",CASE WHEN typeof(Payload)='blob' THEN length(Payload) ELSE 0 END,Available FROM Checkpoint WHERE Id=$id",
                tx, ("$id", checkpointId));
            using var reader = command.ExecuteReader();
            if (!reader.Read() || reader.GetInt32(2) == 0) throw Error(RepositoryError.Unavailable);
            return (reader.IsDBNull(0) ? null : (byte[])reader.GetValue(0), reader.GetInt64(1));
        }

        private void ProtectCorruption(string checkpointId, CheckpointCorruptionKind kind, long dataVersion, CheckpointImage? stored = null)
        {
            // If the marker cannot be committed, expose that storage failure and
            // fence retention on this open repository until the evidence is confirmed.
            pendingQuarantine.Add(checkpointId);
            var committing = false;
            try
            {
                (string? Hash, long Length) observed = stored == null
                    ? ObserveEvidence(checkpointId)
                    : (stored.PayloadHash, stored.PayloadLength);
                using var tx = connection.BeginTransaction(deferred: false);
                if (Number("PRAGMA data_version", tx) != dataVersion) throw Error(RepositoryError.Conflict);
                if (ReadQuarantine(checkpointId, tx) == null)
                {
                    var sequence = Number("SELECT NextSequence FROM RepositoryMeta WHERE Singleton=1", tx);
                    if (sequence == long.MaxValue) throw Error(RepositoryError.WriteFailed);
                    var utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                    Exec("INSERT INTO CheckpointQuarantine(" + QuarantineColumns + ") VALUES($id,$kind,$utc,$length,$hash)",
                        tx, ("$id", checkpointId), ("$kind", (int)kind), ("$utc", utc), ("$length", observed.Length), ("$hash", observed.Hash));
                    Exec("UPDATE RepositoryMeta SET NextSequence=$next WHERE Singleton=1", tx, ("$next", sequence + 1));
                    fault?.Invoke(RepositoryWriteStage.BeforeCommit);
                    committing = true;
                    tx.Commit();
                    fault?.Invoke(RepositoryWriteStage.AfterCommit);
                }
                pendingQuarantine.Remove(checkpointId);
            }
            catch (Exception error)
            {
                if (committing) throw Error(RepositoryError.OutcomeUnknown, null, error);
                if (error is RepositoryException) throw;
                throw Error(ClassifyWriteError(error), null, error);
            }
        }

        private (string? Hash, long Length) ObserveEvidence(string checkpointId)
        {
            var observed = ReadEvidence(checkpointId, null);
            return (observed.Bytes == null ? null : CheckpointImage.Hash(observed.Bytes), observed.Length);
        }
    }
}
