using System;

namespace HobbyDS9.Persistence.Sqlite
{
    public sealed partial class SqliteCampaignRepository
    {
        // Explicit opt-in storage migration, never a game-carrier conversion.
        // The online backup is verified through the same complete-payload validator
        // before any DDL. Preserve backups on every failure and never auto-downgrade.
        private void UpgradeSchema1()
        {
            Backup("before-schema-1-" + Guid.NewGuid().ToString("N"));
            var committing = false;
            try
            {
                using var tx = connection.BeginTransaction(deferred: false);
                Exec("ALTER TABLE SaveReference ADD COLUMN UpdatedUtc TEXT NOT NULL DEFAULT '' CHECK(typeof(UpdatedUtc)='text' AND length(CAST(UpdatedUtc AS BLOB))<=64); PRAGMA user_version=2;", tx);
                // Empty means historically unknown, never invented modification time.
                fault?.Invoke(RepositoryWriteStage.BeforeCommit);
                committing = true; tx.Commit();
                fault?.Invoke(RepositoryWriteStage.AfterCommit);
            }
            catch (Exception e) { throw Error(committing ? RepositoryError.OutcomeUnknown : RepositoryError.WriteFailed, null, e); }
        }
    }
}
