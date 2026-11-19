#nullable enable
using HobbyDS9.Application;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    // Explicit successor activation; GUI chooses this source for a new campaign.
    public static class EngineeringRefinementPlaytestOptions
    {
        public const string SaveDirectorySuffix=EngineeringRefinementIdentity.SaveDirectorySuffix;
        public static CampaignEngineeringRefinementPackageDto CreatePackage(CampaignColonisationExpansionPackageDto growth)
            =>CampaignColonisationCycleApplication.CreateEngineeringRefinementPackage(growth);
    }
}
