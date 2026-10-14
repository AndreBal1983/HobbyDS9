using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewStorage;

public static class PhysicalReviewPackageFiles
{
    public static CampaignTacticalTravelPackageDto Load(string profilePath,string corePath,string scenarioPath)
    {
        var options=new JsonSerializerOptions { UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow };
        var profile=File.ReadAllBytes(profilePath); var core=File.ReadAllBytes(corePath); var scenario=File.ReadAllBytes(scenarioPath);
        var p=JsonSerializer.Deserialize<CampaignTacticalTravelPackageDto>(profile,options)!;
        p.CorePackage=JsonSerializer.Deserialize<CampaignTacticalReviewPackageDto>(core,options)!;
        p.CorePackage.Campaign=JsonSerializer.Deserialize<ScenarioDto>(scenario,options)!;
        p.CorePackage.Campaign.ContentHash=Hash(scenario);
        p.CorePackage.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-tactical-review-package-v1\n"+Hash(core)+"\n"+Hash(scenario)+"\n"));
        p.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-physical-review-package-v2\n"+Hash(profile)+"\n"+p.CorePackage.PackageHash+"\n"));
        return p;
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
