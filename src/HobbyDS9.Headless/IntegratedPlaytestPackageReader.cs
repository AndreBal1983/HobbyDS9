using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Contracts;

// Host-only JSON/raw-resource assembly. Simulation accepts typed, owned inputs.
internal static class IntegratedPlaytestPackageReader
{
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    internal static CampaignIntegratedPlaytestPackageDto Read(string folder)
    {
        var outer=File.ReadAllBytes(Path.Combine(folder,"package.json"));
        var physical=File.ReadAllBytes(Path.Combine(folder,"physical.json"));
        var core=File.ReadAllBytes(Path.Combine(folder,"core.json"));
        var source=File.ReadAllBytes(Path.Combine(folder,"scenario.json"));
        var result=JsonSerializer.Deserialize<CampaignIntegratedPlaytestPackageDto>(outer)!;
        result.Physical=JsonSerializer.Deserialize<CampaignTacticalTravelPackageDto>(physical)!;
        result.Physical.CorePackage=JsonSerializer.Deserialize<CampaignTacticalReviewPackageDto>(core)!;
        result.Physical.CorePackage.Campaign=JsonSerializer.Deserialize<ScenarioDto>(source)!;
        result.Physical.CorePackage.Campaign.ContentHash=Hash(source);
        result.Physical.CorePackage.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-tactical-review-package-v1\n"+Hash(core)+"\n"+Hash(source)+"\n"));
        result.Physical.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-physical-review-package-v2\n"+Hash(physical)+"\n"+result.Physical.CorePackage.PackageHash+"\n"));
        result.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-integrated-playtest-package-v3\n"+Hash(outer)+"\n"+result.Physical.PackageHash+"\n"));
        return result;
    }
}
