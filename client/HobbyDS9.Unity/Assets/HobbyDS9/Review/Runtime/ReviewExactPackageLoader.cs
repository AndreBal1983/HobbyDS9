#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    public static class ReviewExactPackageLoader
    {
        // Bytes arrive from TextAsset.bytes in Unity. Never hash reserialized objects.
        public static CampaignTacticalTravelPackageDto Load(byte[] profile,byte[] core,byte[] scenario,ReviewNewtonsoftCodec codec)
        {
            var package=codec.DecodeProfile(profile);
            package.CorePackage=codec.DecodeCoreProfile(core);
            package.CorePackage.Campaign=codec.DecodeScenario(scenario);
            package.CorePackage.Campaign.ContentHash=Hash(scenario);
            package.CorePackage.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-tactical-review-package-v1\n"+Hash(core)+"\n"+Hash(scenario)+"\n"));
            package.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-physical-review-package-v2\n"+Hash(profile)+"\n"+package.CorePackage.PackageHash+"\n"));
            return package;
        }
        private static string Hash(byte[] bytes)
        { using(var sha=SHA256.Create()) return string.Concat(sha.ComputeHash(bytes).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture))); }
        public static CampaignIntegratedPlaytestPackageDto LoadIntegrated(byte[] outer,byte[] physical,byte[] core,byte[] scenario,ReviewNewtonsoftCodec codec)
        {
            var package=codec.DecodeIntegratedProfile(outer);
            package.Physical=Load(physical,core,scenario,codec);
            package.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-integrated-playtest-package-v3\n"+Hash(outer)+"\n"+package.Physical.PackageHash+"\n"));
            return package;
        }
    }
}
