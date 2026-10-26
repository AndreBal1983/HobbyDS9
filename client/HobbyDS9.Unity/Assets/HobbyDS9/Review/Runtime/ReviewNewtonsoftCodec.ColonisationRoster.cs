#nullable enable
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec:IColonisationRosterReviewHostCodec
    {
        public CampaignColonisationRosterPackageDto CopyRosterPackage(CampaignColonisationRosterPackageDto package)
            =>Decode<CampaignColonisationRosterPackageDto>(Encode(package));
        public ColonisationStartingRosterDto DecodeColonisationRoster(byte[] bytes)=>DecodeActivityResource<ColonisationStartingRosterDto>(bytes);
    }
}
