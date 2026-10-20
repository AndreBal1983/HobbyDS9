using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Contracts;

internal static class PlayableFleetPackageReader
{
    internal static CampaignPlayableFleetPackageDto Read(string folder)
    {
        var bytes = File.ReadAllBytes(Path.Combine(folder, "playable.json"));
        var package = JsonSerializer.Deserialize<CampaignPlayableFleetPackageDto>(bytes)!;
        package.Integrated = IntegratedPlaytestPackageReader.Read(folder);
        string Hash(byte[] b) => Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();
        package.PackageHash = Hash(Encoding.ASCII.GetBytes("campaign-playable-fleet-package-v6\n"
            + Hash(bytes) + "\n" + package.Integrated.PackageHash + "\n"));
        return package;
    }
}
