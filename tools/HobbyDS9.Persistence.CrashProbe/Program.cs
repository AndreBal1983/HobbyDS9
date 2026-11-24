using System.Text;
using HobbyDS9.Persistence;
using HobbyDS9.Persistence.Sqlite;
using Microsoft.Data.Sqlite;

// Test fixture codec only; never presented as gameplay/edition validation.
var path = args[0]; var mode = args[1];
SqliteProviderBootstrap.EnsureInitialized();
if (File.Exists(Path.Combine(AppContext.BaseDirectory, "hobbyds9-native-recipe.txt")))
{
    using var verifyNative = new SqliteConnection("Data Source=:memory:;Pooling=False"); verifyNative.Open();
    using var version = verifyNative.CreateCommand(); version.CommandText = "SELECT sqlite_version()";
    if ((string?)version.ExecuteScalar() != "3.54.1") throw new InvalidOperationException("R2 native fallback detected.");
    var module = System.Diagnostics.Process.GetCurrentProcess().Modules.Cast<System.Diagnostics.ProcessModule>().Single(m => m.ModuleName.Equals("e_sqlite3.dll", StringComparison.OrdinalIgnoreCase));
    if (Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(module.FileName))).ToLowerInvariant() != "4b59d3cded9dc1608544160f3b4b10ac974c6ac0d6ffc23064e5f4c8dbd05aea") throw new InvalidOperationException("Loaded R2 native hash mismatch.");
}
if (mode == "info" || mode == "smoke")
{
    SqliteProviderBootstrap.EnsureInitialized();
    using var sql = new SqliteConnection("Data Source=:memory:;Pooling=False"); sql.Open();
    using var query = sql.CreateCommand(); query.CommandText = "SELECT sqlite_version() || ' ' || sqlite_source_id()";
    var runtime = (string)query.ExecuteScalar()!;
    query.CommandText = "PRAGMA compile_options"; var options = new List<string>();
    using (var rows = query.ExecuteReader()) while (rows.Read()) options.Add(rows.GetString(0));
    var assemblies = new[] { typeof(SqliteCampaignRepository).Assembly, typeof(CheckpointImage).Assembly, typeof(SqliteConnection).Assembly, typeof(SQLitePCL.raw).Assembly, typeof(SQLitePCL.SQLite3Provider_e_sqlite3).Assembly }
        .Select(a => new { a.FullName, a.Location, Framework = a.GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false).Cast<System.Runtime.Versioning.TargetFrameworkAttribute>().Single().FrameworkName,
            Sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(a.Location))).ToLowerInvariant() }).ToArray();
    if (mode == "smoke")
    {
        string checkpoint;
        using (var repo = new SqliteCampaignRepository(path, "campaign", "test-edition-v1", new FixtureValidator()))
        {
            checkpoint = repo.CommitCheckpoint(new("smoke", "main", 0, null, new CheckpointImage(Encoding.UTF8.GetBytes("1"), "test-edition-v1", 1, 0, "Planning"))).CheckpointId!;
            repo.ForkBranch("fork", "other", checkpoint); repo.SetReference("pin", new("manual", 0, SaveReferenceKind.Manual, "Exact netstandard 🖖"), checkpoint);
            repo.Backup("backup");
        }
        using var reopened = new SqliteCampaignRepository(path, "campaign", "test-edition-v1", new FixtureValidator());
        if (Encoding.UTF8.GetString(reopened.ReadCheckpoint(checkpoint).ReadPayload()) != "1" || reopened.FindOperation("smoke") == null) throw new InvalidOperationException("Smoke mismatch.");
    }
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { runtime, options, assemblies, mode, succeeded = true }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
try
{
    using var repo = new SqliteCampaignRepository(path, "campaign", "test-edition-v1", new FixtureValidator(), stage =>
    {
        if (mode == "before" && stage == RepositoryWriteStage.BeforeCommit || mode == "after" && stage == RepositoryWriteStage.AfterCommit)
        { Console.WriteLine("READY"); Console.Out.Flush(); Console.ReadLine(); }
    });
    if (mode == "lease") { Console.WriteLine("READY"); Console.Out.Flush(); Console.ReadLine(); }
    else
    {
        var first = repo.GetBranch("main");
        repo.CommitCheckpoint(new CheckpointCommit("child", "main", first.Generation, first.Head,
            new CheckpointImage(Encoding.UTF8.GetBytes("2"), "test-edition-v1", 2, 0, "Planning")));
    }
}
catch (RepositoryException e) when (e.Error == RepositoryError.WriterInUse) { return 23; }
return 0;

sealed class FixtureValidator : ICheckpointValidator
{
    public CheckpointImage Validate(byte[] bytes) => new(bytes, "test-edition-v1", int.Parse(Encoding.UTF8.GetString(bytes), System.Globalization.CultureInfo.InvariantCulture), 0, "Planning");
}
