using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Persistence.Sqlite;
using HobbyDS9.StateRepositoryProbe;

internal static class Program
{
    private static int Main(string[] args)
    {
        if(args.Length!=3)throw new ArgumentException("Arguments: generated-root run-guid-N phase");
        var result=SqliteNativeProbe.Execute(args[0],args[1],args[2],SqliteProviderBootstrap.EnsureInitialized);
        using var process=System.Diagnostics.Process.GetCurrentProcess();
        var native=process.Modules.Cast<System.Diagnostics.ProcessModule>().Single(m=>string.Equals(m.ModuleName,"e_sqlite3.dll",StringComparison.OrdinalIgnoreCase));
        var nativeHash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(native.FileName))).ToLowerInvariant();
        if(nativeHash!="4b59d3cded9dc1608544160f3b4b10ac974c6ac0d6ffc23064e5f4c8dbd05aea")throw new InvalidDataException("Loaded engine differs from R2 pin.");
        var assemblies=new[]{typeof(Microsoft.Data.Sqlite.SqliteConnection).Assembly,typeof(SQLitePCL.raw).Assembly,typeof(SQLitePCL.SQLite3Provider_e_sqlite3).Assembly,typeof(SqliteProviderBootstrap).Assembly};
        var pins=assemblies.Select(a=>new{Name=a.GetName().Name,Path=a.Location,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(a.Location))).ToLowerInvariant()}).ToArray();
        Console.WriteLine(JsonSerializer.Serialize(new{HeadlessOnly=true,Framework=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,NativePath=native.FileName,NativeSha256=nativeHash,ManagedAssets=pins,Exercise=result},
            new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));return result.Success?0:1;
    }
}
