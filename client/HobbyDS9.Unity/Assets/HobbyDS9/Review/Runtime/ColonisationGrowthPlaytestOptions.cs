#nullable enable
using HobbyDS9.Application;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    public static class ColonisationGrowthPlaytestOptions
    {
        public static CampaignColonisationExpansionPackageDto CreatePackage(CampaignColonisationRosterPackageDto roster,byte[] planner,ReviewNewtonsoftCodec codec)
            =>CampaignColonisationGrowthApplication.CreateGrowthPackage(ColonisationExpansionPlaytestOptions.CreatePackage(roster,planner,codec));
    }
}
