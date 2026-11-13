#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    public static class ColonisationCyclePlaytestOptions
    {
        public const string PolicyAssetHash="b496e0b6e7ea4ddaa7d789ee43f38511b7b363fb47c8856d4dcfe2e20c1779e9";
        public static CampaignColonisationCyclePackageDto CreatePackage(CampaignColonisationExpansionPackageDto growth,byte[] bytes,ReviewNewtonsoftCodec codec)
        {
            using(var sha=SHA256.Create())
                if(bytes==null||string.Concat(sha.ComputeHash(bytes).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture)))!=PolicyAssetHash)throw new ArgumentException("Different pinned cycle policy.");
            var actual=codec.DecodeColonisationCyclePolicy(bytes);
            var package=CampaignColonisationCycleApplication.CreateCyclePackage(growth);var expected=package.Policy!;
            if(actual.Algorithm!=expected.Algorithm||actual.ActiveMissions!=expected.ActiveMissions)throw new ArgumentException("Different closed cycle policy.");
            return package;
        }
    }
}
