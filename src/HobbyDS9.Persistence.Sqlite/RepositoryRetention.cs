using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.Persistence.Sqlite
{
    public sealed partial class SqliteCampaignRepository
    {
        // Explicit maintenance after a successful replacement commit. Full payloads
        // have no parent restore dependency; ancestry and operation receipts remain.
        // Physical pages/WAL can exceed the logical soft budget until idle maintenance.
        public DurableReceipt ApplyRetention(string operationId, BranchInfo activeBranch, int rolling = 10, int milestones = 3, long softAutoBytes = 512L * 1024 * 1024)
        {
            if (activeBranch == null || rolling < 1 || rolling > 10000 || milestones < 0 || milestones > 10000 || softAutoBytes < 1) throw Error(RepositoryError.InvalidRequest);
            Id(activeBranch.Id);
            Id(operationId);
            var fingerprint = Fingerprint("retention-v1", activeBranch.Id, N(activeBranch.Generation), activeBranch.Head, N(rolling), N(milestones), N(softAutoBytes));
            lock (gate)
            {
                Alive(); var known = Receipt(operationId);
                if (known != null)
                {
                    if (known.Fingerprint != fingerprint) throw Error(RepositoryError.OperationIdConflict, operationId);
                    return known;
                }
                if (pendingQuarantine.Count != 0) throw Error(RepositoryError.WriteFailed);
                var before = Branch(activeBranch.Id);
                if (before == null || before.Head != activeBranch.Head || before.Generation != activeBranch.Generation) throw Error(RepositoryError.Conflict);
                if (before.Head == null) throw Error(RepositoryError.Unavailable);
                // Complete host validation holds the repository gate, not a DB writer.
                var verified = RetentionStamp(ReadCheckpoint(before.Head));
                var dataVersion = Number("PRAGMA data_version");
                var plan = PrepareRetention(before.Head, rolling, milestones, softAutoBytes, dataVersion);
                return Mutate(operationId, fingerprint, "", (tx, sequence) =>
                {
                    var current = Branch(activeBranch.Id, tx);
                    if (current == null || current.Head != activeBranch.Head || current.Generation != activeBranch.Generation) throw Error(RepositoryError.Conflict);
                    if (current.Head == null) throw Error(RepositoryError.Unavailable);
                    // The gate excludes cooperating edits. Refuse to apply this
                    // plan if another SQLite connection committed during preflight.
                    if (Number("PRAGMA data_version", tx) != dataVersion) throw Error(RepositoryError.Conflict);
                    // Recheck bounded stored bytes/hash/metadata in the write snapshot.
                    // No game replay occurs in this short maintenance transaction.
                    var stored = ReadStoredCheckpoint(current.Head, tx);
                    if (RetentionStamp(stored) != verified) throw Error(RepositoryError.Conflict);
                    foreach (var reference in plan.References)
                        Exec("UPDATE SaveReference SET Deleted=1,Generation=Generation+1 WHERE Id=$id", tx, ("$id", reference));
                    foreach (var checkpoint in plan.Payloads)
                    {
                        using var remove = Command("UPDATE Checkpoint AS c SET Payload=NULL,Available=0 WHERE Id=$id AND Available=1 AND NOT " + IsQuarantinedSql,
                            tx, ("$id", checkpoint));
                        if (remove.ExecuteNonQuery() != 1) throw Error(RepositoryError.Conflict);
                    }
                    return (current.Head, current.Generation, 0);
                });
            }
        }

        private static (string Hash, int Length, int Turn, long Revision, string Phase) RetentionStamp(CheckpointImage image) =>
            (image.PayloadHash, image.PayloadLength, image.Turn, image.Revision, image.Phase);

        private (string[] References, string[] Payloads) PrepareRetention(string head, int rolling, int milestones, long softAutoBytes, long dataVersion)
        {
            // A prior process may have died before a quarantine marker committed.
            // Recheck actual deletion candidates through the matching host. Only
            // IDs are retained between reads, so payload memory is bounded.
            var verified = new HashSet<string>(StringComparer.Ordinal);
            while (true)
            {
                if (Number("PRAGMA data_version") != dataVersion) throw Error(RepositoryError.Conflict);
                var plan = PlanRetention(head, rolling, milestones, softAutoBytes);
                var pinned = false;
                foreach (var checkpoint in plan.Payloads)
                {
                    if (verified.Contains(checkpoint)) continue;
                    try { ReadCheckpoint(checkpoint); }
                    catch (RepositoryException error) when (error.Error == RepositoryError.Corrupt && ReadQuarantine(checkpoint) != null)
                    {
                        pinned = true;
                        continue;
                    }
                    verified.Add(checkpoint);
                }
                // A newly pinned checkpoint uses no automatic retention slot.
                // Recalculate the healthy window before committing removals.
                if (!pinned) return plan;
            }
        }

        private (string[] References, string[] Payloads) PlanRetention(string head, int rolling, int milestones, long softAutoBytes)
        {
            var refs = new List<(string Id, string Checkpoint, int Kind, long Sequence)>();
            using (var command = Command("SELECT " + Field("r.Id", 96) + "," + Field("r.CheckpointId", 96)
                + ",r.Kind,c.Sequence FROM SaveReference r JOIN Checkpoint c ON c.Id=r.CheckpointId WHERE r.Deleted=0 ORDER BY c.Sequence DESC,r.Id COLLATE BINARY"))
            using (var rows = command.ExecuteReader())
                while (rows.Read()) refs.Add((ReadText(rows, 0, 96), ReadText(rows, 1, 96), rows.GetInt32(2), rows.GetInt64(3)));

            var sizes = new Dictionary<string, long>(StringComparer.Ordinal);
            using (var command = Command("SELECT " + Field("c.Id", 96) + ",PayloadLength FROM Checkpoint c WHERE Available=1 AND NOT " + IsQuarantinedSql))
            using (var rows = command.ExecuteReader())
                while (rows.Read()) sizes.Add(ReadText(rows, 0, 96), rows.GetInt64(1));
            if (!sizes.ContainsKey(head)) throw Error(RepositoryError.Conflict);

            var manual = refs.Where(row => row.Kind == 1).Select(row => row.Checkpoint).ToHashSet(StringComparer.Ordinal);
            var keep = refs.Where(row => row.Kind == 1).Select(row => row.Id).ToHashSet(StringComparer.Ordinal);
            var viable = refs.Where(row => sizes.ContainsKey(row.Checkpoint));
            foreach (var row in viable.Where(row => row.Kind == 2).Take(rolling).Concat(viable.Where(row => row.Kind == 3).Take(milestones)))
                keep.Add(row.Id);

            var automatic = refs.Where(row => row.Kind != 1 && keep.Contains(row.Id)).Select(row => row.Checkpoint).ToHashSet(StringComparer.Ordinal);
            automatic.Add(head);
            automatic.ExceptWith(manual);
            var automaticBytes = automatic.Sum(id => sizes[id]);
            foreach (var group in refs.Where(row => row.Kind != 1).OrderBy(row => row.Sequence).ThenBy(row => row.Id, StringComparer.Ordinal).GroupBy(row => row.Checkpoint))
            {
                if (automaticBytes <= softAutoBytes) break;
                if (group.Key == head || !automatic.Remove(group.Key)) continue;
                automaticBytes -= sizes[group.Key];
                foreach (var row in group) keep.Remove(row.Id);
            }

            var pins = refs.Where(row => keep.Contains(row.Id)).Select(row => row.Checkpoint).ToHashSet(StringComparer.Ordinal);
            pins.Add(head);
            return (refs.Where(row => row.Kind != 1 && !keep.Contains(row.Id)).Select(row => row.Id).ToArray(),
                sizes.Keys.Where(id => !pins.Contains(id)).OrderBy(id => id, StringComparer.Ordinal).ToArray());
        }
        public long LogicalPayloadBytes
        {
            get
            {
                lock (gate)
                {
                    Alive();
                    return Number("SELECT COALESCE(SUM(COALESCE(q.ObservedPayloadBytes,c.PayloadLength)),0) FROM Checkpoint c LEFT JOIN CheckpointQuarantine q ON q.CheckpointId=c.Id WHERE c.Available=1");
                }
            }
        }
        public RepositoryUsage GetStorageUsage(BranchInfo activeBranch, long softAutoBytes = 512L * 1024 * 1024)
        {
            if (activeBranch == null || softAutoBytes < 1) throw Error(RepositoryError.InvalidRequest);
            Id(activeBranch.Id);
            lock (gate)
            {
                Alive(); var branch = Branch(activeBranch.Id);
                if (branch == null || branch.Head != activeBranch.Head || branch.Generation != activeBranch.Generation) throw Error(RepositoryError.Conflict);
                if (branch.Head == null) throw Error(RepositoryError.Unavailable);
                if (Number("SELECT COUNT(*) FROM Checkpoint WHERE Id=$head AND Available=1", null, ("$head", branch.Head)) != 1) throw Error(RepositoryError.Unavailable);
                const string manual = "EXISTS(SELECT 1 FROM SaveReference r WHERE r.Deleted=0 AND r.Kind=1 AND r.CheckpointId=c.Id)";
                const string automatic = "EXISTS(SELECT 1 FROM SaveReference r WHERE r.Deleted=0 AND r.Kind IN(2,3) AND r.CheckpointId=c.Id)";
                var quarantineBytes = Number("SELECT COALESCE(SUM(ObservedPayloadBytes),0) FROM CheckpointQuarantine");
                var protectedBytes = quarantineBytes + Number("SELECT COALESCE(SUM(PayloadLength),0) FROM Checkpoint c WHERE Available=1 AND NOT " + IsQuarantinedSql + " AND (c.Id=$head OR " + manual + ")", null, ("$head", branch.Head));
                var automaticBytes = Number("SELECT COALESCE(SUM(PayloadLength),0) FROM Checkpoint c WHERE Available=1 AND NOT " + IsQuarantinedSql + " AND NOT " + manual + " AND (c.Id=$head OR " + automatic + ")", null, ("$head", branch.Head));
                var db = new System.IO.FileInfo(SafeFile("campaign.sqlite")); var wal = new System.IO.FileInfo(SafeFile("campaign.sqlite-wal"));
                return new RepositoryUsage(LogicalPayloadBytes, automaticBytes, protectedBytes, db.Exists ? db.Length : 0, wal.Exists ? wal.Length : 0, softAutoBytes, quarantineBytes);
            }
        }
        public void CheckpointWal()
        {
            lock (gate) { Alive(); Exec("PRAGMA wal_checkpoint(PASSIVE)"); }
        }
    }
}
