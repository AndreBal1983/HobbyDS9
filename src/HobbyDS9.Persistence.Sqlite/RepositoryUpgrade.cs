using System;
using Microsoft.Data.Sqlite;

namespace HobbyDS9.Persistence.Sqlite
{
    public sealed partial class SqliteCampaignRepository
    {
        // Explicit opt-in storage migration, never a game-carrier conversion.
        // The online backup is verified through the same complete-payload validator
        // before any DDL. Preserve backups on every failure and never auto-downgrade.
        private void UpgradeStorage(long schema)
        {
            BackupVerified("before-schema-" + N(schema) + "-" + Guid.NewGuid().ToString("N"), hasQuarantine: false);
            var committing = false;
            try
            {
                using var tx = connection.BeginTransaction(deferred: false);
                if (schema == 1)
                    Exec("ALTER TABLE SaveReference ADD COLUMN UpdatedUtc TEXT NOT NULL DEFAULT '' CHECK(typeof(UpdatedUtc)='text' AND length(CAST(UpdatedUtc AS BLOB))<=64)", tx);
                CreateQuarantineTable(tx);
                fault?.Invoke(RepositoryWriteStage.BeforeCommit);
                committing = true; tx.Commit();
                fault?.Invoke(RepositoryWriteStage.AfterCommit);
            }
            catch (Exception e) { throw Error(committing ? RepositoryError.OutcomeUnknown : RepositoryError.WriteFailed, null, e); }
        }

        private void CreateQuarantineTable(SqliteTransaction tx)
        {
            Exec(@"CREATE TABLE CheckpointQuarantine(
CheckpointId TEXT PRIMARY KEY NOT NULL REFERENCES Checkpoint(Id),
Kind INTEGER NOT NULL CHECK(Kind IN(1,2,3)),
DetectedUtc TEXT NOT NULL CHECK(typeof(DetectedUtc)='text' AND length(CAST(DetectedUtc AS BLOB))<=64),
ObservedPayloadBytes INTEGER NOT NULL CHECK(typeof(ObservedPayloadBytes)='integer' AND ObservedPayloadBytes>=0),
ObservedPayloadHash TEXT NULL CHECK(ObservedPayloadHash IS NULL OR
 (typeof(ObservedPayloadHash)='text' AND length(CAST(ObservedPayloadHash AS BLOB))=64
 AND ObservedPayloadBytes BETWEEN 1 AND 67108864)));
PRAGMA user_version=3;", tx);
        }
    }
}
