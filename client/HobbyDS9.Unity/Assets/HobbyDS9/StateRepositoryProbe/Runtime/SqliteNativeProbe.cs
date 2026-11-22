#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace HobbyDS9.StateRepositoryProbe
{
    // Generated probe bytes only. No game session, codec, slot store or owner-save path.
    public static class SqliteNativeProbe
    {
        [Serializable] public sealed class Result
        {
            public string Phase="",Directory="",SQLiteVersion="",SQLiteSourceId="",PayloadSha256="";
            public string JournalMode="";public int Synchronous;public int PayloadBytes;
            public bool Success,RollbackVerified,BackupVerified,RestoreVerified;
            public long ElapsedMilliseconds;public string[] CompileOptions=Array.Empty<string>();
        }
        public static string ProbeDirectory(string root,string run)
        {
            if(!Guid.TryParseExact(run,"N",out var id))throw new ArgumentException("Probe run must be a new 32-digit GUID.");
            return Path.Combine(Path.GetFullPath(root),"StateRepositoryProbe-v1",id.ToString("N"),"Étoile-日本語");
        }
        public static Result Execute(string root,string run,string phase,Action initialize)
        {
            if(phase!="create"&&phase!="reopen"&&phase!="verify-backup"&&phase!="restore"&&phase!="verify-restored")throw new ArgumentException("Unknown probe phase.");
            initialize();var timer=Stopwatch.StartNew();var folder=ProbeDirectory(root,run);
            var database=Path.Combine(folder,phase=="verify-backup"||phase=="restore"?"backup.sqlite":phase=="verify-restored"?"restored.sqlite":"probe.sqlite");
            if(phase=="create")
            {Directory.CreateDirectory(folder);if(File.Exists(Path.Combine(folder,"backup.sqlite")))throw new IOException("Run already exists; refusing overwrite.");Reserve(database);}
            else if(!File.Exists(database))throw new IOException("Create phase has not produced this probe file.");
            var result=new Result{Phase=phase,Directory=folder};
            var payload=Payload();result.PayloadBytes=payload.Length;result.PayloadSha256=Hash(payload);
            using(var connection=Open(database,phase=="create"))
            {
                result.SQLiteVersion=Scalar(connection,"select sqlite_version()");result.SQLiteSourceId=Scalar(connection,"select sqlite_source_id()");
                if(result.SQLiteVersion!="3.54.1"||result.SQLiteSourceId!="2026-10-16 14:45:20 555e07ef33f69a0957d24d8e555b702464a131d6f1607f4c3c1e4551e8e6ca76")
                    throw new InvalidOperationException("Runtime SQLite differs from the reviewed R2 engine; no fallback accepted.");
                var options=new List<string>();using(var cmd=connection.CreateCommand())
                {cmd.CommandText="pragma compile_options";using(var reader=cmd.ExecuteReader())while(reader.Read())options.Add(reader.GetString(0));}
                options.Sort(StringComparer.Ordinal);result.CompileOptions=options.ToArray();
                if(!options.Contains("THREADSAFE=1")||!options.Contains("ENABLE_COLUMN_METADATA"))throw new InvalidOperationException("Required R2 engine compile options absent.");
                result.JournalMode=Scalar(connection,"pragma journal_mode");result.Synchronous=int.Parse(Scalar(connection,"pragma synchronous"),System.Globalization.CultureInfo.InvariantCulture);
                if(phase=="create")
                {
                    if(result.JournalMode!="wal"||result.Synchronous!=2)throw new InvalidOperationException("WAL/FULL was not applied.");
                    NonQuery(connection,"create table Payload(Id integer primary key, Name text not null, Image blob not null, Digest text not null)");
                    using(var transaction=connection.BeginTransaction())
                    {Insert(connection,transaction,1,payload,result.PayloadSha256);transaction.Commit();}
                    using(var transaction=connection.BeginTransaction())
                    {Insert(connection,transaction,2,payload,result.PayloadSha256);transaction.Rollback();}
                }
                Verify(connection,payload,result.PayloadSha256);result.RollbackVerified=true;
                if(phase=="create")
                {
                    // SQLite online backup includes committed WAL; no live-file copying.
                    var backupPath=Path.Combine(folder,"backup.sqlite");Reserve(backupPath);
                    using(var backup=Open(backupPath,true))
                    {connection.BackupDatabase(backup);Verify(backup,payload,result.PayloadSha256);}
                    result.BackupVerified=true;
                }
                if(phase=="verify-backup"||phase=="restore")result.BackupVerified=true;
                if(phase=="restore")
                {
                    var restoredPath=Path.Combine(folder,"restored.sqlite");Reserve(restoredPath);
                    using(var restored=Open(restoredPath,true))
                    {connection.BackupDatabase(restored);Verify(restored,payload,result.PayloadSha256);}
                    result.RestoreVerified=true;
                }
                if(phase=="verify-restored")result.RestoreVerified=true;
            }
            result.Success=true;result.ElapsedMilliseconds=timer.ElapsedMilliseconds;return result;
        }
        private static SqliteConnection Open(string path,bool create)
        {
            var connection=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Mode=create?SqliteOpenMode.ReadWriteCreate:SqliteOpenMode.ReadWrite,Pooling=false}.ToString());
            try
            {
                connection.Open();NonQuery(connection,"pragma busy_timeout=1500; pragma foreign_keys=ON;");
                if(create)NonQuery(connection,"pragma journal_mode=WAL;");
                NonQuery(connection,"pragma synchronous=FULL;");return connection;
            }
            catch{connection.Dispose();throw;}
        }
        private static byte[] Payload()
        {var bytes=new byte[1024*1024];for(var i=0;i<bytes.Length;i++)bytes[i]=(byte)((i*37+11)%251);return bytes;}
        private static void Reserve(string path)
        {using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))file.Flush(true);}
        private static void Insert(SqliteConnection c,SqliteTransaction t,int id,byte[] image,string digest)
        {
            using(var cmd=c.CreateCommand())
            {cmd.Transaction=t;cmd.CommandText="insert into Payload(Id,Name,Image,Digest) values($id,$name,$image,$digest)";
                cmd.Parameters.AddWithValue("$id",id);cmd.Parameters.AddWithValue("$name","Étoile 日本語 — generated probe");
                cmd.Parameters.AddWithValue("$image",image);cmd.Parameters.AddWithValue("$digest",digest);cmd.ExecuteNonQuery();}
        }
        private static void Verify(SqliteConnection c,byte[] expected,string digest)
        {
            if(Scalar(c,"pragma integrity_check")!="ok"||Scalar(c,"select count(*) from Payload")!="1")throw new InvalidDataException("Integrity or rollback/count proof failed.");
            using(var cmd=c.CreateCommand())
            {cmd.CommandText="select Name,Image,Digest from Payload where Id=1";using(var r=cmd.ExecuteReader())
                {if(!r.Read()||r.GetString(0)!="Étoile 日本語 — generated probe"||r.GetString(2)!=digest)throw new InvalidDataException("Metadata mismatch.");
                    var bytes=(byte[])r.GetValue(1);if(bytes.Length!=expected.Length||Hash(bytes)!=digest)throw new InvalidDataException("Payload byte mismatch.");
                    for(var i=0;i<bytes.Length;i++)if(bytes[i]!=expected[i])throw new InvalidDataException("Payload byte mismatch.");}}
        }
        private static string Scalar(SqliteConnection c,string sql)
        {using(var cmd=c.CreateCommand()){cmd.CommandText=sql;return Convert.ToString(cmd.ExecuteScalar(),System.Globalization.CultureInfo.InvariantCulture)??"";}}
        private static void NonQuery(SqliteConnection c,string sql)
        {using(var cmd=c.CreateCommand()){cmd.CommandText=sql;cmd.ExecuteNonQuery();}}
        private static string Hash(byte[] bytes)
        {using(var sha=SHA256.Create()){var b=sha.ComputeHash(bytes);var text=new StringBuilder(64);foreach(var v in b)text.Append(v.ToString("x2"));return text.ToString();}}
    }
}
