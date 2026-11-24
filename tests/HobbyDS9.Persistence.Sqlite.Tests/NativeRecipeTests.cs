using System.Diagnostics;
using System.Security.Cryptography;
using HobbyDS9.Persistence.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;
using Xunit.Abstractions;

namespace HobbyDS9.Persistence.Sqlite.Tests;

public sealed class NativeRecipeTests
{
    private readonly ITestOutputHelper output;
    public NativeRecipeTests(ITestOutputHelper output) { this.output = output; }
    [Fact] public void ReportActualRuntimeAndRejectOldFallbackWhenR2WasExplicitlyStaged()
    {
        SqliteProviderBootstrap.EnsureInitialized();
        using var sql = new SqliteConnection("Data Source=:memory:;Pooling=False"); sql.Open();
        using var c = sql.CreateCommand(); c.CommandText = "SELECT sqlite_version() || ' ' || sqlite_source_id()";
        var version = (string)c.ExecuteScalar()!; output.WriteLine(version);
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "hobbyds9-native-recipe.txt")))
        {
            Assert.StartsWith("3.54.1 ", version);
            var module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().Single(m => m.ModuleName.Equals("e_sqlite3.dll", StringComparison.OrdinalIgnoreCase));
            output.WriteLine(module.FileName);
            Assert.Equal("4b59d3cded9dc1608544160f3b4b10ac974c6ac0d6ffc23064e5f4c8dbd05aea", Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(module.FileName))).ToLowerInvariant());
        }
    }
}
