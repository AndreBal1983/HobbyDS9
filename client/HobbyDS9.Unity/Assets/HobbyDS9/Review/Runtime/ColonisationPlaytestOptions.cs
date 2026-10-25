#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    // Composition-root pins are independent of all save-supplied metadata.
    public static class ColonisationPlaytestOptions
    {
        private static readonly string[] AssetHashes={"1dc65fab5ec4c7392c1909e8917dff4af13376efcee3f5f16d58b55b02f572a5","c4289397f869f02f63324b29444b57ef8dd43ec24edf2e79111baf457a83eafc","fcba9d70f0e966e8d693ada1a363b43cd6a71f2e040e2e5f2edfc6b673c8b8a7","2145be41fbab731b1d0823ada361519d528b278da36fbf1393848b5ad21b987c"};
        public static CampaignColonisationPackageDto CreatePackage(CampaignActivitiesPackageDto activities,byte[][] bytes,ReviewNewtonsoftCodec codec)
        {
            if(bytes==null||bytes.Length!=4)throw new ArgumentException("Four pinned colonisation assets required.");
            using(var sha=SHA256.Create())for(int i=0;i<bytes.Length;i++)
                if(bytes[i]==null||string.Concat(sha.ComputeHash(bytes[i]).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture)))!=AssetHashes[i])throw new ArgumentException("Different provisional colonisation content.");
            return CampaignColonisationApplication.CreatePackage(activities,codec.DecodeColonisationCatalogue(bytes[0]),codec.DecodeColonisationReadyClasses(bytes[1]),codec.DecodeColonisationSupport(bytes[2]),codec.DecodeColonisationPolicy(bytes[3]));
        }
    }
}
