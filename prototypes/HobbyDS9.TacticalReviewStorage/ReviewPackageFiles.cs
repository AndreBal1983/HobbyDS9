using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewStorage;

// Explicit caller-supplied source files; no lookup/default scenario/save directory.
public static class ReviewPackageFiles
{
    public static CampaignTacticalReviewPackageDto Load(string profilePath, string scenarioPath)
    {
        var options=new JsonSerializerOptions { UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow };
        var profile=File.ReadAllBytes(profilePath); var scenario=File.ReadAllBytes(scenarioPath);
        var package=JsonSerializer.Deserialize<CampaignTacticalReviewPackageDto>(profile,options)!;
        package.Campaign=JsonSerializer.Deserialize<ScenarioDto>(scenario,options)!;
        package.Campaign.ContentHash=Hash(scenario);
        package.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-tactical-review-package-v1\n"+Hash(profile)+"\n"+Hash(scenario)+"\n"));
        return package;
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
