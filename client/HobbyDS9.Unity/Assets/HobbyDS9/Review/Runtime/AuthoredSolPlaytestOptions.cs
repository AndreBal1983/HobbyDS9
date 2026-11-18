#nullable enable
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    // GUI selects this source only for the explicitly named new-campaign edition.
    public static class AuthoredSolPlaytestOptions
    {
        public const string DisplayName="Sol belt campaign";
        public const string SaveDirectorySuffix=CampaignAuthoredSolIdentity.SaveDirectorySuffix;
        public static CampaignAuthoredSolPackageDto CreatePackage(CampaignColonisationExpansionPackageDto growth)
            =>CampaignColonisationCycleApplication.CreateAuthoredSolPackage(growth);
    }
}
