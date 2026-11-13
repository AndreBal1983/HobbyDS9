#nullable enable
using System;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    // Explicit composition-root content pin. Never read this approval from a save.
    public static class PublishedConstructionPlaytestOptions
    {
        public const string SupportSha256="05417b3bf41f2b134077f6130f0c71dd2ffc27d3ec1f40593b8dfac4a2da45b3";
        public static CampaignPublishedConstructionPackageDto CreatePackage(CampaignPlayableFleetPackageDto template,byte[] bytes,ReviewNewtonsoftCodec codec)
        {
            if(bytes==null)throw new ArgumentNullException(nameof(bytes));
            using(var sha=SHA256.Create())
                if(string.Concat(sha.ComputeHash(bytes).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture)))!=SupportSha256)
                    throw new ArgumentException("Different provisional playtest support asset.");
            var owned=codec.CopyPackage(template);owned.ReplayOperationLimit=65536;
            return CampaignPublishedConstructionApplication.CreatePackage(owned,codec.DecodePublishedSupport(bytes),512);
        }
    }
}
