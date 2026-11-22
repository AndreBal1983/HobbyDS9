using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;

namespace HobbyDS9.Persistence.Sqlite
{
    public enum RepositoryWriteStage { AfterPayload, BeforeCommit, AfterCommit }
    // Per-campaign, cooperative local writer. No game or Unity dependency.
    public sealed partial class SqliteCampaignRepository : ICampaignRepository
    {
        public const int StorageSchemaVersion = 1;
        private const int ApplicationId = 1213416497; // HDS1
        private readonly object gate = new object();
        private readonly string directory;
        private readonly string editionIdentity;
        private readonly ICheckpointValidator validator;
        private readonly Action<RepositoryWriteStage>? fault;
        private readonly FileStream lease;
        private readonly SqliteConnection connection;
        private bool disposed;

        public SqliteCampaignRepository(string campaignDirectory, string campaignId, string editionIdentity, ICheckpointValidator validator, Action<RepositoryWriteStage>? fault = null)
        {
            Id(campaignId); Text(editionIdentity, 16384, false);
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
            this.editionIdentity = editionIdentity; this.fault = fault;
            directory = SafeDirectory(campaignDirectory);
            Directory.CreateDirectory(directory); CheckAncestors(directory);
            var lockPath = SafeFile("writer.lock");
            try { lease = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException e) { throw Error(RepositoryError.WriterInUse, null, e); }
            try
            {
                SqliteProviderBootstrap.EnsureInitialized();
                var path = SafeFile("campaign.sqlite");
                var exists = File.Exists(path);
                CheckSidecars();
                connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false, Mode = SqliteOpenMode.ReadWriteCreate, DefaultTimeout = 1 }.ToString());
                try
                {
                    connection.Open();
                    if (exists)
                    {
                        if (Number("PRAGMA application_id") != ApplicationId || Number("PRAGMA user_version") != StorageSchemaVersion) throw Error(RepositoryError.IncompatibleSchema);
                        if (TextScalar("CampaignId", 96) != campaignId) throw Error(RepositoryError.Conflict);
                        if (TextScalar("EditionIdentity", 16384) != editionIdentity) throw Error(RepositoryError.IncompatibleEdition);
                    }
                    Exec("PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA synchronous=FULL; PRAGMA busy_timeout=1000; PRAGMA wal_autocheckpoint=1000;");
                    if (!exists) Initialize(campaignId);
                    if ((string?)Scalar("PRAGMA quick_check") != "ok") throw Error(RepositoryError.Corrupt);
                }
                catch { connection.Dispose(); throw; }
            }
            catch { lease.Dispose(); throw; }
        }

        private void Initialize(string campaignId)
        {
            using var tx = connection.BeginTransaction(deferred: false);
            Exec(@"CREATE TABLE RepositoryMeta(Singleton INTEGER PRIMARY KEY CHECK(Singleton=1),CampaignId TEXT NOT NULL CHECK(typeof(CampaignId)='text' AND length(CAST(CampaignId AS BLOB))<=96),EditionIdentity TEXT NOT NULL CHECK(typeof(EditionIdentity)='text' AND length(CAST(EditionIdentity AS BLOB))<=65536),NextSequence INTEGER NOT NULL CHECK(NextSequence>0));
CREATE TABLE Branch(Id TEXT PRIMARY KEY,Origin TEXT NULL,Head TEXT NULL,Generation INTEGER NOT NULL CHECK(Generation>=0));
CREATE TABLE Checkpoint(Id TEXT PRIMARY KEY,BranchId TEXT NOT NULL REFERENCES Branch(Id),Parent TEXT NULL,Sequence INTEGER NOT NULL UNIQUE,Turn INTEGER NOT NULL CHECK(Turn>0),Revision INTEGER NOT NULL CHECK(Revision>=0),Phase TEXT NOT NULL,Payload BLOB NULL,PayloadHash TEXT NOT NULL,PayloadLength INTEGER NOT NULL CHECK(PayloadLength>0 AND PayloadLength<=67108864),Available INTEGER NOT NULL DEFAULT 1 CHECK(Available IN(0,1)),CHECK((Available=0 AND Payload IS NULL) OR (Available=1 AND typeof(Payload)='blob' AND length(Payload)=PayloadLength)));
CREATE TABLE SaveReference(Id TEXT PRIMARY KEY CHECK(typeof(Id)='text' AND length(CAST(Id AS BLOB))<=96),CheckpointId TEXT NOT NULL REFERENCES Checkpoint(Id),Generation INTEGER NOT NULL CHECK(Generation>0),Kind INTEGER NOT NULL CHECK(Kind IN(1,2,3)),Name TEXT NOT NULL CHECK(typeof(Name)='text' AND length(Name)<=120 AND length(CAST(Name AS BLOB))<=480),Notes TEXT NOT NULL CHECK(typeof(Notes)='text' AND length(Notes)<=1000 AND length(CAST(Notes AS BLOB))<=4000),Deleted INTEGER NOT NULL DEFAULT 0 CHECK(Deleted IN(0,1)));
CREATE TABLE PersistenceOperation(Id TEXT PRIMARY KEY,Fingerprint TEXT NOT NULL,Sequence INTEGER NOT NULL UNIQUE,CheckpointId TEXT NULL,BranchGeneration INTEGER NOT NULL,ReferenceGeneration INTEGER NOT NULL,SourceEpoch TEXT NOT NULL,CommittedUtc TEXT NOT NULL);
CREATE INDEX ReferenceListing ON SaveReference(Deleted,Id);
PRAGMA application_id=1213416497; PRAGMA user_version=1;", tx);
            Exec("INSERT INTO RepositoryMeta VALUES(1,$id,$edition,1)", tx, ("$id", campaignId), ("$edition", editionIdentity));
            tx.Commit();
        }

        public BranchInfo GetBranch(string branchId)
        {
            Id(branchId);
            lock (gate) { Alive(); return Branch(branchId) ?? throw Error(RepositoryError.NotFound); }
        }
        private BranchInfo? Branch(string id, SqliteTransaction? tx = null)
        {
            using var c = Command("SELECT " + Field("Head", 96) + ",Generation,Head IS NULL FROM Branch WHERE Id=$id", tx, ("$id", id));
            using var r = c.ExecuteReader();
            return r.Read() ? new BranchInfo(id, r.GetInt32(2) == 1 ? null : ReadText(r, 0, 96), r.GetInt64(1)) : null;
        }
        public DurableReceipt? FindOperation(string operationId)
        {
            Id(operationId);
            lock (gate)
            {
                try { Alive(); return Receipt(operationId); }
                catch (ObjectDisposedException) { throw; }
                catch (Exception e) { throw Error(RepositoryError.OutcomeUnknown, operationId, e); }
            }
        }
        private DurableReceipt? Receipt(string id, SqliteTransaction? tx = null)
        {
            using var c = Command("SELECT " + Field("Fingerprint", 64) + ",Sequence," + Field("CheckpointId", 96) + ",BranchGeneration,ReferenceGeneration," + Field("SourceEpoch", 256) + "," + Field("CommittedUtc", 64) + ",CheckpointId IS NULL FROM PersistenceOperation WHERE Id=$id", tx, ("$id", id));
            using var r = c.ExecuteReader();
            return r.Read() ? new DurableReceipt(id, ReadText(r, 0, 64), r.GetInt64(1), r.GetInt32(7) == 1 ? null : ReadText(r, 2, 96), r.GetInt64(3), r.GetInt64(4), ReadText(r, 5, 256), ReadText(r, 6, 64)) : null;
        }
        public IReadOnlyList<SaveReference> ListReferences(string afterId = "", int limit = 50)
        {
            if (afterId != "") Id(afterId);
            if (limit < 1 || limit > 500) throw Error(RepositoryError.InvalidRequest);
            lock (gate)
            {
                Alive(); var result = new List<SaveReference>();
                using var c = Command("SELECT " + Field("Id", 96) + "," + Field("CheckpointId", 96) + ",Generation,Kind," + Field("Name", 120) + "," + Field("Notes", 1000) + " FROM SaveReference WHERE Deleted=0 AND Id>$after ORDER BY Id COLLATE BINARY LIMIT $limit", null, ("$after", afterId), ("$limit", limit));
                using var r = c.ExecuteReader();
                while (r.Read())
                {
                    var kind = (SaveReferenceKind)r.GetInt32(3);
                    if (r.GetInt64(2) < 1 || !Enum.IsDefined(typeof(SaveReferenceKind), kind)) throw Error(RepositoryError.Corrupt);
                    result.Add(new SaveReference(ReadText(r, 0, 96), ReadText(r, 1, 96), r.GetInt64(2), kind, ReadText(r, 4, 120), ReadText(r, 5, 1000)));
                }
                return result.AsReadOnly();
            }
        }
        public CheckpointImage ReadCheckpoint(string checkpointId)
        {
            Id(checkpointId);
            lock (gate)
            {
                Alive(); byte[] bytes; int turn; long revision; string phase; string hash;
                using (var c = Command("SELECT Turn,Revision," + Field("Phase", 64) + "," + Field("PayloadHash", 64) + ",PayloadLength,CASE WHEN typeof(Payload)='blob' AND length(Payload)<=67108864 THEN Payload ELSE NULL END,Available,length(Payload) FROM Checkpoint WHERE Id=$id", null, ("$id", checkpointId)))
                using (var r = c.ExecuteReader())
                {
                    if (!r.Read()) throw Error(RepositoryError.NotFound);
                    if (r.GetInt32(6) == 0) throw Error(RepositoryError.Unavailable);
                    if (r.IsDBNull(5) || r.GetInt64(4) < 1 || r.GetInt64(4) > CheckpointImage.MaximumPayloadBytes || r.GetInt64(7) != r.GetInt64(4)) throw Error(RepositoryError.Corrupt);
                    turn = r.GetInt32(0); revision = r.GetInt64(1); phase = ReadText(r, 2, 64); hash = ReadText(r, 3, 64); bytes = (byte[])r.GetValue(5);
                }
                if (CheckpointImage.Hash(bytes) != hash) throw Error(RepositoryError.Corrupt);
                var image = Validate(bytes);
                if (image.Turn != turn || image.Revision != revision || image.Phase != phase || image.PayloadHash != hash) throw Error(RepositoryError.Corrupt);
                return image;
            }
        }
        private CheckpointImage Validate(byte[] bytes)
        {
            var hash = CheckpointImage.Hash(bytes);
            CheckpointImage image;
            try { image = validator.Validate((byte[])bytes.Clone()); }
            catch (Exception e) { throw Error(RepositoryError.ValidationFailed, null, e); }
            if (image == null || image.PayloadHash != hash || image.PayloadLength != bytes.Length) throw Error(RepositoryError.ValidationFailed);
            if (image.EditionIdentity != editionIdentity) throw Error(RepositoryError.IncompatibleEdition);
            return image;
        }
        private SqliteCommand Command(string sql, SqliteTransaction? tx = null, params (string Name, object? Value)[] values)
        {
            var c = connection.CreateCommand(); c.CommandText = sql; c.Transaction = tx;
            foreach (var v in values) c.Parameters.AddWithValue(v.Name, v.Value ?? DBNull.Value);
            return c;
        }
        private object? Scalar(string sql, SqliteTransaction? tx = null, params (string Name, object? Value)[] values) { using var c = Command(sql, tx, values); return c.ExecuteScalar(); }
        private long Number(string sql, SqliteTransaction? tx = null, params (string Name, object? Value)[] values) => Convert.ToInt64(Scalar(sql, tx, values), CultureInfo.InvariantCulture);
        private void Exec(string sql, SqliteTransaction? tx = null, params (string Name, object? Value)[] values) { using var c = Command(sql, tx, values); c.ExecuteNonQuery(); }
        private void Alive() { if (disposed) throw new ObjectDisposedException(nameof(SqliteCampaignRepository)); CheckAncestors(directory); CheckSidecars(); }
        public void Dispose() { lock (gate) { if (disposed) return; disposed = true; try { connection.Dispose(); } finally { lease.Dispose(); } } }
        private static RepositoryException Error(RepositoryError error, string? op = null, Exception? e = null) => new RepositoryException(error, op, e);
        private static void Id(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 96) throw Error(RepositoryError.InvalidRequest);
            foreach (var c in id) if (!(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '-' || c == '_')) throw Error(RepositoryError.InvalidRequest);
        }
        private static void Text(string s, int max, bool empty = true)
        {
            if (s == null || s.Length > max || !empty && string.IsNullOrWhiteSpace(s) || s.IndexOf('\0') >= 0) throw Error(RepositoryError.InvalidRequest);
            try { new UTF8Encoding(false, true).GetByteCount(s); }
            catch (EncoderFallbackException) { throw Error(RepositoryError.InvalidRequest); }
        }
        // Check type and UTF8 byte length inside SQLite before allocating any managed
        // value. Then decode strictly and enforce UTF16 bounds used by the public API.
        private static string Field(string column, int max) => "CASE WHEN typeof(" + column + ")='text' AND length(CAST(" + column + " AS BLOB))<=" + N(max * 4L) + " THEN CAST(" + column + " AS BLOB) ELSE NULL END";
        private static string ReadText(SqliteDataReader reader, int ordinal, int max)
        {
            if (reader.IsDBNull(ordinal)) throw Error(RepositoryError.Corrupt);
            try
            {
                // Field() already bounds the native value. GetBytes/GetStream on a
                // computed column invokes unsafe provider metadata paths; do not use it.
                var bytes = (byte[])reader.GetValue(ordinal);
                if (bytes.Length > max * 4L) throw Error(RepositoryError.Corrupt);
                var text = new UTF8Encoding(false, true).GetString(bytes);
                if (text.Length > max || text.IndexOf('\0') >= 0) throw Error(RepositoryError.Corrupt);
                return text;
            }
            catch (DecoderFallbackException e) { throw Error(RepositoryError.Corrupt, null, e); }
        }
        private string TextScalar(string column, int max)
        {
            using var c = Command("SELECT " + Field(column, max) + " FROM RepositoryMeta WHERE Singleton=1");
            using var r = c.ExecuteReader(); if (!r.Read()) throw Error(RepositoryError.Corrupt); return ReadText(r, 0, max);
        }
        private static string SafeDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || !Path.IsPathFullyQualified(path)) throw Error(RepositoryError.InvalidRequest);
            var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (full == "" || full == Path.GetPathRoot(full)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) throw Error(RepositoryError.InvalidRequest);
            CheckAncestors(full); return full;
        }
        private static void CheckAncestors(string path)
        {
            for (var p = new DirectoryInfo(path); p != null; p = p.Parent)
                if (p.Exists && (p.Attributes & FileAttributes.ReparsePoint) != 0 || File.Exists(p.FullName)) throw Error(RepositoryError.InvalidRequest);
        }
        private string SafeFile(string name)
        {
            var path = Path.Combine(directory, name);
            CheckRegularFile(path);
            return path;
        }
        private void CheckSidecars() { SafeFile("campaign.sqlite"); SafeFile("campaign.sqlite-wal"); SafeFile("campaign.sqlite-shm"); SafeFile("writer.lock"); }
        private static string Fingerprint(params string?[] fields)
        {
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), true);
            writer.Write("hobbyds9-persistence-operation-v1");
            foreach (var field in fields) { writer.Write(field != null); if (field != null) writer.Write(field); }
            writer.Flush(); return CheckpointImage.Hash(stream.ToArray());
        }
        private static string N(long value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
