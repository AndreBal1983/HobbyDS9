#nullable enable
using System;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    // Trusted composition-root content, never a support catalogue supplied by a save.
    public static class ActivitiesPlaytestOptions
    {
        private static readonly string[] AssetHashes={
            "945bd888471cc38ddd6307ff7245072e61131752a1f55d22d7155b8d4178b17f",
            "bb3433febe69f351e536dc3ccc1ad3190ea344aa55c2e877bde4d4174fef36da",
            "807c508e1968ce52c95a5b220cdf8f8f58d7610838c3000085aedc9ec5af4485",
            "0a8df4fd704a3fef4cb795392b4ec5566fdcb42f7cf24aed48a696cc3abba920"};
        public static CampaignActivitiesPackageDto CreatePackage(CampaignPlayableFleetPackageDto template,byte[][] bytes,ReviewNewtonsoftCodec codec)
        {
            if(bytes==null||bytes.Length!=4)throw new ArgumentException("Four pinned activity assets required.");
            using(var sha=SHA256.Create())for(int i=0;i<bytes.Length;i++)
                if(bytes[i]==null||string.Concat(sha.ComputeHash(bytes[i]).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture)))!=AssetHashes[i])throw new ArgumentException("Different provisional activity content.");
            var owned=codec.CopyPackage(template);owned.ReplayOperationLimit=65536;
            return CampaignActivitiesApplication.CreatePackage(owned,codec.DecodeActivityCatalogue(bytes[0]),codec.DecodeActivityReadyClasses(bytes[1]),
                codec.DecodeActivitySupport(bytes[2]),codec.DecodeActivityContent(bytes[3]),2048);
        }
    }
}
