using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Contracts;
internal static class SingleplayerDemoPackageReader
{
    internal static CampaignSingleplayerDemoPackageDto Read(string folder)
    {
        var bytes=File.ReadAllBytes(Path.Combine(folder,"demo.json"));
        var p=JsonSerializer.Deserialize<CampaignSingleplayerDemoPackageDto>(bytes)!;
        p.Integrated=IntegratedPlaytestPackageReader.Read(folder);
        string Hash(byte[] b)=>Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();
        p.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-singleplayer-demo-package-v4\n"+Hash(bytes)+"\n"+p.Integrated.PackageHash+"\n"));
        return p;
    }
}
