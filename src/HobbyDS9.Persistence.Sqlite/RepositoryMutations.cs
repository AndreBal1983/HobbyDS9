using System;
using Microsoft.Data.Sqlite;

namespace HobbyDS9.Persistence.Sqlite
{
    public sealed partial class SqliteCampaignRepository
    {
        private DurableReceipt Mutate(string operationId, string fingerprint, string sourceEpoch, Func<SqliteTransaction, long, (string? Checkpoint, long BranchGeneration, long ReferenceGeneration)> apply)
        {
            Id(operationId);
            lock (gate)
            {
                Alive();
                // Receipt lookup precedes payload insertion and every generation check.
                // A failed lookup throws; only a completed successful lookup can mean absent.
                var known = Receipt(operationId);
                if (known != null)
                {
                    if (known.Fingerprint != fingerprint) throw Error(RepositoryError.OperationIdConflict, operationId);
                    return known;
                }
                var committing = false;
                try
                {
                    using var tx = connection.BeginTransaction(deferred: false);
                    var sequence = Number("SELECT NextSequence FROM RepositoryMeta WHERE Singleton=1", tx);
                    if (sequence == long.MaxValue) throw Error(RepositoryError.InvalidRequest, operationId);
                    var result = apply(tx, sequence);
                    var utc = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
                    Exec("UPDATE RepositoryMeta SET NextSequence=$next WHERE Singleton=1", tx, ("$next", sequence + 1));
                    Exec("INSERT INTO PersistenceOperation VALUES($id,$fp,$seq,$cp,$bg,$rg,$epoch,$utc)", tx,
                        ("$id", operationId), ("$fp", fingerprint), ("$seq", sequence), ("$cp", result.Checkpoint), ("$bg", result.BranchGeneration), ("$rg", result.ReferenceGeneration), ("$epoch", sourceEpoch), ("$utc", utc));
                    fault?.Invoke(RepositoryWriteStage.BeforeCommit);
                    committing = true;
                    tx.Commit();
                    fault?.Invoke(RepositoryWriteStage.AfterCommit);
                    return new DurableReceipt(operationId, fingerprint, sequence, result.Checkpoint, result.BranchGeneration, result.ReferenceGeneration, sourceEpoch, utc);
                }
                catch (Exception e)
                {
                    // Even Commit throwing does not prove rollback. Caller must reopen/query
                    // this operation before retrying with the same ID and exact request.
                    if (committing) throw Error(RepositoryError.OutcomeUnknown, operationId, e);
                    if (e is RepositoryException) throw;
                    if (e is SqliteException sqlite)
                    {
                        var error = sqlite.SqliteErrorCode == 13 ? RepositoryError.StorageFull : sqlite.SqliteErrorCode == 8 ? RepositoryError.ReadOnly
                            : sqlite.SqliteErrorCode == 5 || sqlite.SqliteErrorCode == 6 ? RepositoryError.Busy : RepositoryError.WriteFailed;
                        throw Error(error, operationId, e);
                    }
                    throw Error(RepositoryError.WriteFailed, operationId, e);
                }
            }
        }
        public DurableReceipt CommitCheckpoint(CheckpointCommit request)
        {
            if (request == null || request.Image == null) throw Error(RepositoryError.InvalidRequest);
            Id(request.OperationId); Id(request.BranchId); if (request.ExpectedHead != null) Id(request.ExpectedHead);
            if (request.ExpectedBranchGeneration < 0) throw Error(RepositoryError.InvalidRequest);
            Text(request.SourceEpoch, 256);
            if (request.Reference != null) CheckReference(request.Reference);
            var image = request.Image;
            var reference = request.Reference;
            var fingerprint = Fingerprint("checkpoint", request.BranchId, N(request.ExpectedBranchGeneration), request.ExpectedHead,
                image.PayloadHash, N(image.PayloadLength), image.EditionIdentity, N(image.Turn), N(image.Revision), image.Phase, request.SourceEpoch,
                reference?.Id, reference == null ? null : N(reference.ExpectedGeneration), reference == null ? null : N((int)reference.Kind), reference?.Name, reference?.Notes);
            return Mutate(request.OperationId, fingerprint, request.SourceEpoch, (tx, sequence) =>
            {
                // The image has already been validated before opening this transaction.
                var branch = Branch(request.BranchId, tx);
                if (branch == null)
                {
                    if (request.ExpectedBranchGeneration != 0 || request.ExpectedHead != null) throw Error(RepositoryError.Conflict);
                    Exec("INSERT INTO Branch VALUES($id,NULL,NULL,0)", tx, ("$id", request.BranchId));
                    branch = new BranchInfo(request.BranchId, null, 0);
                }
                if (branch.Generation != request.ExpectedBranchGeneration || branch.Head != request.ExpectedHead || branch.Generation == long.MaxValue) throw Error(RepositoryError.Conflict);
                var checkpoint = "checkpoint-" + Guid.NewGuid().ToString("N");
                Exec("INSERT INTO Checkpoint VALUES($id,$branch,$parent,$seq,$turn,$rev,$phase,$payload,$hash,$length,1)", tx,
                    ("$id", checkpoint), ("$branch", branch.Id), ("$parent", branch.Head), ("$seq", sequence), ("$turn", image.Turn), ("$rev", image.Revision), ("$phase", image.Phase),
                    ("$payload", image.ReadPayload()), ("$hash", image.PayloadHash), ("$length", image.PayloadLength));
                fault?.Invoke(RepositoryWriteStage.AfterPayload);
                Exec("UPDATE Branch SET Head=$cp,Generation=Generation+1 WHERE Id=$id", tx, ("$cp", checkpoint), ("$id", branch.Id));
                var rg = reference == null ? 0 : ApplyReference(reference, checkpoint, tx);
                return (checkpoint, branch.Generation + 1, rg);
            }, image);
        }

        // Validate once before opening the short SQL write transaction, after checking
        // an existing receipt. A retained callback array cannot mutate the request image.
        private DurableReceipt Mutate(string operationId, string fingerprint, string epoch,
            Func<SqliteTransaction, long, (string? Checkpoint, long BranchGeneration, long ReferenceGeneration)> apply, CheckpointImage image)
        {
            lock (gate)
            {
                Alive(); var known = Receipt(operationId);
                if (known != null)
                {
                    if (known.Fingerprint != fingerprint) throw Error(RepositoryError.OperationIdConflict, operationId);
                    return known;
                }
                var verified = Validate(image.ReadPayload());
                if (verified.Turn != image.Turn || verified.Revision != image.Revision || verified.Phase != image.Phase || verified.EditionIdentity != image.EditionIdentity) throw Error(RepositoryError.ValidationFailed);
                return Mutate(operationId, fingerprint, epoch, apply);
            }
        }
        public DurableReceipt ForkBranch(string operationId, string branchId, string originCheckpointId)
        {
            Id(branchId); Id(originCheckpointId);
            return Mutate(operationId, Fingerprint("fork", branchId, originCheckpointId), "", (tx, sequence) =>
            {
                RequireCheckpoint(originCheckpointId, tx);
                if (Branch(branchId, tx) != null) throw Error(RepositoryError.Conflict);
                Exec("INSERT INTO Branch VALUES($id,$origin,$origin,0)", tx, ("$id", branchId), ("$origin", originCheckpointId));
                return (originCheckpointId, 0, 0);
            });
        }
        public DurableReceipt SetReference(string operationId, ReferenceUpdate reference, string checkpointId)
        {
            CheckReference(reference); Id(checkpointId);
            return Mutate(operationId, Fingerprint("reference", reference.Id, N(reference.ExpectedGeneration), N((int)reference.Kind), reference.Name, reference.Notes, checkpointId), "", (tx, sequence) =>
            { RequireCheckpoint(checkpointId, tx); return (checkpointId, 0, ApplyReference(reference, checkpointId, tx)); });
        }
        public DurableReceipt DeleteReference(string operationId, string referenceId, long expectedGeneration)
        {
            Id(referenceId); if (expectedGeneration < 1 || expectedGeneration == long.MaxValue) throw Error(RepositoryError.InvalidRequest);
            return Mutate(operationId, Fingerprint("delete-reference", referenceId, N(expectedGeneration)), "", (tx, sequence) =>
            {
                using var c = Command("UPDATE SaveReference SET Deleted=1,Generation=Generation+1 WHERE Id=$id AND Generation=$gen AND Deleted=0", tx, ("$id", referenceId), ("$gen", expectedGeneration));
                if (c.ExecuteNonQuery() != 1) throw Error(RepositoryError.Conflict);
                return (null, 0, expectedGeneration + 1);
            });
        }
        private static void CheckReference(ReferenceUpdate r)
        {
            if (r == null || r.ExpectedGeneration < 0 || r.ExpectedGeneration == long.MaxValue || !Enum.IsDefined(typeof(SaveReferenceKind), r.Kind)) throw Error(RepositoryError.InvalidRequest);
            Id(r.Id); Text(r.Name, 120, false); Text(r.Notes, 1000);
        }
        private void RequireCheckpoint(string checkpoint, SqliteTransaction tx)
        { if (Number("SELECT COUNT(*) FROM Checkpoint WHERE Id=$id AND Available=1", tx, ("$id", checkpoint)) != 1) throw Error(RepositoryError.Unavailable); }
        private long ApplyReference(ReferenceUpdate r, string checkpoint, SqliteTransaction tx)
        {
            var count = Number("SELECT COUNT(*) FROM SaveReference WHERE Id=$id", tx, ("$id", r.Id));
            if (count == 0)
            {
                if (r.ExpectedGeneration != 0) throw Error(RepositoryError.Conflict);
                Exec("INSERT INTO SaveReference VALUES($id,$cp,1,$kind,$name,$notes,0,$utc)", tx,
                    ("$id", r.Id), ("$cp", checkpoint), ("$kind", (int)r.Kind), ("$name", r.Name), ("$notes", r.Notes), ("$utc", DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture))); return 1;
            }
            using var command = Command("UPDATE SaveReference SET CheckpointId=$cp,Generation=Generation+1,Name=$name,Notes=$notes,UpdatedUtc=$utc WHERE Id=$id AND Generation=$gen AND Deleted=0 AND Kind=$kind", tx,
                ("$cp", checkpoint), ("$name", r.Name), ("$notes", r.Notes), ("$id", r.Id), ("$gen", r.ExpectedGeneration), ("$kind", (int)r.Kind), ("$utc", DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture)));
            if (command.ExecuteNonQuery() != 1) throw Error(RepositoryError.Conflict);
            return r.ExpectedGeneration + 1;
        }
    }
}
