using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace HobbyDS9.Persistence.Sqlite
{
    public sealed partial class SqliteCampaignRepository
    {
        // Explicit import only. Source files are never opened for write, renamed or removed.
        // Digest+edition derives a stable destination/operation; changing metadata is a
        // later explicit reference operation, not a second import of identical bytes.
        public DurableReceipt ImportJson(string absoluteSourceFile)
        {
            lock (gate)
            {
                Alive();
                if (!Path.IsPathFullyQualified(absoluteSourceFile)) throw Error(RepositoryError.InvalidRequest);
                var source = Path.GetFullPath(absoluteSourceFile);
                CheckAncestors(Path.GetDirectoryName(source)!);
                if ((File.GetAttributes(source) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0) throw Error(RepositoryError.InvalidRequest);
                byte[] bytes;
                using (var stream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length < 1 || stream.Length > CheckpointImage.MaximumPayloadBytes) throw Error(RepositoryError.InvalidRequest);
                    bytes = new byte[(int)stream.Length]; var offset = 0;
                    while (offset < bytes.Length) { var n = stream.Read(bytes, offset, bytes.Length - offset); if (n == 0) throw Error(RepositoryError.Corrupt); offset += n; }
                }
                var image = Validate(bytes);
                var digest = Fingerprint("import-v1", editionIdentity, image.PayloadHash);
                var id = "import-" + digest;
                var backup = Path.Combine(SubDirectory("migration-backups"), digest + ".json");
                if (File.Exists(backup))
                {
                    CheckRegularFile(backup);
                    if (new FileInfo(backup).Length != bytes.Length || CheckpointImage.Hash(File.ReadAllBytes(backup)) != image.PayloadHash) throw Error(RepositoryError.Corrupt);
                }
                else AtomicWrite(backup, bytes, false);
                return CommitCheckpoint(new CheckpointCommit(id, id, 0, null, image, new ReferenceUpdate(id, 0, SaveReferenceKind.Manual, "Imported save"), "json-import-v1"));
            }
        }
        public string ExportJson(string checkpointId, string exportId, bool overwrite = false)
        {
            Id(exportId);
            lock (gate)
            {
                var bytes = ReadCheckpoint(checkpointId).ReadPayload();
                var path = Path.Combine(SubDirectory("exports"), exportId + ".json");
                AtomicWrite(path, bytes, overwrite); return path;
            }
        }
        public string Backup(string backupId)
        {
            Id(backupId);
            lock (gate)
            {
                Alive(); var destination = Path.Combine(SubDirectory("backups"), backupId + ".sqlite");
                CheckRegularFile(destination);
                if (File.Exists(destination)) throw Error(RepositoryError.Conflict);
                var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = temporary, Pooling = false }.ToString()))
                    { target.Open(); connection.BackupDatabase(target); }
                    using (var verify = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = temporary, Pooling = false, Mode = SqliteOpenMode.ReadOnly }.ToString()))
                    {
                        verify.Open(); using var check = verify.CreateCommand(); check.CommandText = "PRAGMA quick_check";
                        if ((string?)check.ExecuteScalar() != "ok") throw Error(RepositoryError.Corrupt);
                        check.CommandText = "SELECT Turn,Revision," + Field("Phase", 64) + "," + Field("PayloadHash", 64) + ",PayloadLength,CASE WHEN typeof(Payload)='blob' AND length(Payload)<=67108864 THEN Payload ELSE NULL END,length(Payload) FROM Checkpoint WHERE Available=1";
                        using var rows = check.ExecuteReader();
                        while (rows.Read())
                        {
                            if (rows.IsDBNull(5) || rows.GetInt64(4) < 1 || rows.GetInt64(4) > CheckpointImage.MaximumPayloadBytes || rows.GetInt64(6) != rows.GetInt64(4)) throw Error(RepositoryError.Corrupt);
                            var image = Validate((byte[])rows.GetValue(5));
                            if (image.Turn != rows.GetInt32(0) || image.Revision != rows.GetInt64(1) || image.Phase != ReadText(rows, 2, 64) || image.PayloadHash != ReadText(rows, 3, 64)) throw Error(RepositoryError.Corrupt);
                        }
                    }
                    using (var stream = new FileStream(temporary, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) stream.Flush(true);
                    CheckAncestors(Path.GetDirectoryName(destination)!); CheckRegularFile(destination);
                    File.Move(temporary, destination); return destination;
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
        }
        private string SubDirectory(string name)
        {
            var path = Path.Combine(directory, name); CheckAncestors(path);
            Directory.CreateDirectory(path); CheckAncestors(path); return path;
        }
        private static void CheckRegularFile(string path)
        {
            try { if ((File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0) throw Error(RepositoryError.InvalidRequest); }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
        private static void AtomicWrite(string destination, byte[] bytes, bool overwrite)
        {
            CheckAncestors(Path.GetDirectoryName(destination)!); CheckRegularFile(destination);
            if (File.Exists(destination) && !overwrite) throw Error(RepositoryError.Conflict);
            var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp"; var created = false;
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { created = true; stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                CheckAncestors(Path.GetDirectoryName(destination)!); CheckRegularFile(destination);
                if (File.Exists(destination))
                { if (!overwrite) throw Error(RepositoryError.Conflict); File.Replace(temporary, destination, null); }
                else File.Move(temporary, destination);
            }
            finally { if (created && File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
