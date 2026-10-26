#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    public static class ColonisationRepeatPlaytestOptions
    {
        public const string PolicyAssetHash="fecceffdff09938656ae0d580ccc8d8792e0966f27dc1a3ea8b14bae7e55374c";
        public static CampaignColonisationRepeatPackageDto CreatePackage(CampaignColonisationExpansionPackageDto growth,byte[] bytes,ReviewNewtonsoftCodec codec)
        {
            using(var sha=SHA256.Create())
                if(bytes==null||string.Concat(sha.ComputeHash(bytes).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture)))!=PolicyAssetHash)throw new ArgumentException("Different pinned repeat policy.");
            var actual=codec.DecodeColonisationRepeatPolicy(bytes);
            var package=CampaignColonisationRepeatApplication.CreateRepeatPackage(growth);var expected=package.Policy!;
            if(actual.Algorithm!=expected.Algorithm||actual.AdditionalColonies!=expected.AdditionalColonies||actual.SettlementAttempts!=expected.SettlementAttempts)throw new ArgumentException("Different closed repeat policy.");
            return package;
        }
    }
}
