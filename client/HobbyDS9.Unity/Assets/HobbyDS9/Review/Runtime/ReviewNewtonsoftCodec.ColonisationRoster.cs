#nullable enable
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec:IColonisationRosterReviewHostCodec
    {
        public CampaignColonisationRosterPackageDto CopyRosterPackage(CampaignColonisationRosterPackageDto package)
        {
            loadDiagnostic?.Record(ReviewLoadPhase.RosterCopy,ReviewLoadEvent.Begin);
            var copied=Decode<CampaignColonisationRosterPackageDto>(Encode(package));
            loadDiagnostic?.Record(ReviewLoadPhase.RosterCopy,ReviewLoadEvent.NormalReturn);
            return copied;
        }
        public ColonisationStartingRosterDto DecodeColonisationRoster(byte[] bytes)=>DecodeActivityResource<ColonisationStartingRosterDto>(bytes);
    }
}
