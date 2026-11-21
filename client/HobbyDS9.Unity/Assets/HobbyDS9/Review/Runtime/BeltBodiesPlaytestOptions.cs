#nullable enable
using HobbyDS9.Application;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    public static class BeltBodiesPlaytestOptions
    {
        public const string DisplayName="Belt mining campaign";
        public const string SaveDirectorySuffix=BeltBodiesEditionIdentity.SaveDirectorySuffix;
        public static CampaignBeltBodiesPackageDto CreatePackage(CampaignColonisationExpansionPackageDto growth)
            =>CampaignColonisationCycleApplication.CreateBeltBodiesPackage(growth);
    }
}
