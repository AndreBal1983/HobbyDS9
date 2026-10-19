using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Contracts;

internal static class FrontierPackageReader
{
    internal static CampaignFrontierPackageDto Read(string folder)
    {
        var bytes=File.ReadAllBytes(Path.Combine(folder,"frontier.json"));
        var package=JsonSerializer.Deserialize<CampaignFrontierPackageDto>(bytes)!;
        package.Integrated=IntegratedPlaytestPackageReader.Read(folder);
        string Hash(byte[] b)=>Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();
        package.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-frontier-review-package-v5\n"+Hash(bytes)+"\n"+package.Integrated.PackageHash+"\n"));
        return package;
    }
}
