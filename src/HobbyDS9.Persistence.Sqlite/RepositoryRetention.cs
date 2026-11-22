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
                var before = Branch(activeBranch.Id);
                if (before == null || before.Head != activeBranch.Head || before.Generation != activeBranch.Generation) throw Error(RepositoryError.Conflict);
                if (before.Head == null) throw Error(RepositoryError.Unavailable);
                // Complete host validation holds the repository gate, not a DB writer.
                var verified = ReadCheckpoint(before.Head);
                return Mutate(operationId, fingerprint, "", (tx, sequence) =>
                {
                    var current = Branch(activeBranch.Id, tx);
                    if (current == null || current.Head != activeBranch.Head || current.Generation != activeBranch.Generation) throw Error(RepositoryError.Conflict);
                    if (current.Head == null) throw Error(RepositoryError.Unavailable);
                    // Recheck bounded stored bytes/hash/metadata in the write snapshot.
                    // No game replay occurs in this short maintenance transaction.
                    var stored = ReadStoredCheckpoint(current.Head, tx);
                    if (stored.PayloadHash != verified.PayloadHash || stored.PayloadLength != verified.PayloadLength || stored.Turn != verified.Turn || stored.Revision != verified.Revision || stored.Phase != verified.Phase)
                        throw Error(RepositoryError.Conflict);
                    var refs = new List<(string Id, string Checkpoint, int Kind, long Sequence)>();
                    using (var cmd = Command("SELECT " + Field("r.Id", 96) + "," + Field("r.CheckpointId", 96) + ",r.Kind,c.Sequence FROM SaveReference r JOIN Checkpoint c ON c.Id=r.CheckpointId WHERE r.Deleted=0 ORDER BY c.Sequence DESC,r.Id COLLATE BINARY", tx))
                    using (var r = cmd.ExecuteReader()) while (r.Read()) refs.Add((ReadText(r, 0, 96), ReadText(r, 1, 96), r.GetInt32(2), r.GetInt64(3)));
                    var sizes = new Dictionary<string, long>(StringComparer.Ordinal);
                    using (var cmd = Command("SELECT " + Field("Id", 96) + ",PayloadLength FROM Checkpoint WHERE Available=1", tx))
                    using (var r = cmd.ExecuteReader()) while (r.Read()) sizes.Add(ReadText(r, 0, 96), r.GetInt64(1));
                    var manual = refs.Where(r => r.Kind == 1).Select(r => r.Checkpoint).ToHashSet(StringComparer.Ordinal);
                    var keep = refs.Where(r => r.Kind == 1).Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
                    foreach (var r in refs.Where(r => r.Kind == 2).Take(rolling).Concat(refs.Where(r => r.Kind == 3).Take(milestones))) keep.Add(r.Id);
                    long AutomaticBytes()
                    {
                        var ids = refs.Where(r => r.Kind != 1 && keep.Contains(r.Id)).Select(r => r.Checkpoint).ToHashSet(StringComparer.Ordinal);
                        if (current.Head != null) ids.Add(current.Head);
                        return ids.Where(id => !manual.Contains(id) && sizes.ContainsKey(id)).Sum(id => sizes[id]);
                    }
                    foreach (var old in refs.Where(r => r.Kind != 1).OrderBy(r => r.Sequence).ThenBy(r => r.Id, StringComparer.Ordinal))
                    {
                        if (AutomaticBytes() <= softAutoBytes) break;
                        if (old.Checkpoint == current.Head || manual.Contains(old.Checkpoint)) continue;
                        foreach (var r in refs.Where(r => r.Kind != 1 && r.Checkpoint == old.Checkpoint)) keep.Remove(r.Id);
                    }
                    foreach (var r in refs.Where(r => r.Kind != 1 && !keep.Contains(r.Id)))
                        Exec("UPDATE SaveReference SET Deleted=1,Generation=Generation+1 WHERE Id=$id", tx, ("$id", r.Id));
                    var pins = refs.Where(r => keep.Contains(r.Id)).Select(r => r.Checkpoint).ToHashSet(StringComparer.Ordinal);
                    if (current.Head != null) pins.Add(current.Head);
                    foreach (var cp in sizes.Keys.Where(id => !pins.Contains(id)))
                        Exec("UPDATE Checkpoint SET Payload=NULL,Available=0 WHERE Id=$id", tx, ("$id", cp));
                    return (current.Head, current.Generation, 0);
                });
            }
        }
        public long LogicalPayloadBytes
        {
            get { lock (gate) { Alive(); return Number("SELECT COALESCE(SUM(PayloadLength),0) FROM Checkpoint WHERE Available=1"); } }
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
                var protectedBytes = Number("SELECT COALESCE(SUM(PayloadLength),0) FROM Checkpoint c WHERE Available=1 AND (c.Id=$head OR " + manual + ")", null, ("$head", branch.Head));
                var automaticBytes = Number("SELECT COALESCE(SUM(PayloadLength),0) FROM Checkpoint c WHERE Available=1 AND NOT " + manual + " AND (c.Id=$head OR " + automatic + ")", null, ("$head", branch.Head));
                var db = new System.IO.FileInfo(SafeFile("campaign.sqlite")); var wal = new System.IO.FileInfo(SafeFile("campaign.sqlite-wal"));
                return new RepositoryUsage(LogicalPayloadBytes, automaticBytes, protectedBytes, db.Exists ? db.Length : 0, wal.Exists ? wal.Length : 0, softAutoBytes);
            }
        }
        public void CheckpointWal()
        {
            lock (gate) { Alive(); Exec("PRAGMA wal_checkpoint(PASSIVE)"); }
        }
    }
}
