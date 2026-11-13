#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    public static class ColonisationExpansionPlaytestOptions
    {
        public const string PlannerAssetHash="1863794d2fdbdcbfc444c23031bae7ff2bed0197e1feb72f8059166377f1bf48";
        public static CampaignColonisationExpansionPackageDto CreatePackage(CampaignColonisationRosterPackageDto roster,byte[] bytes,ReviewNewtonsoftCodec codec)
        {
            using(var sha=SHA256.Create())
                if(bytes==null||string.Concat(sha.ComputeHash(bytes).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture)))!=PlannerAssetHash)
                    throw new ArgumentException("Different pinned expansion policy.");
            var p=codec.DecodeColonisationPlanner(bytes);var package=CampaignColonisationExpansionApplication.CreatePackage(roster);var expected=package.PlannerPolicy!;
            if(p==null||p.Algorithm!=expected.Algorithm||p.ActiveGoals!=expected.ActiveGoals||p.MaximumAttempts!=expected.MaximumAttempts||p.MaximumAccepted!=expected.MaximumAccepted||p.BodyCandidates!=expected.BodyCandidates||p.RouteCandidates!=expected.RouteCandidates||p.FailedTargets!=expected.FailedTargets||p.BfsTraversals!=expected.BfsTraversals||p.KnownNodes!=expected.KnownNodes||p.KnownEdges!=expected.KnownEdges||p.HomeDeficitWeeks!=expected.HomeDeficitWeeks||p.ContingencyWeeks!=expected.ContingencyWeeks||p.DisputedWeeks!=expected.DisputedWeeks)throw new ArgumentException("Different closed expansion policy.");
            return package;
        }
    }
}
