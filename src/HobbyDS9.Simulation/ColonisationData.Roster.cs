using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    internal static partial class ColonisationData
    {
        internal static string RosterHash(CampaignColonisationRosterPackageDto p)
        {
            Require(p!=null&&p.FormatVersion==1&&p.ProfileId==CampaignColonisationRosterIdentity.PackageProfile&&p.BaseV1!=null
                &&p.BaseV1.PackageHash==PackageHash(p.BaseV1),"Different colonisation roster base.");
            var roster=p!.StartingRoster;
            Require(roster!=null&&roster.Identity==CampaignColonisationRosterIdentity.Policy&&roster.ClassId=="class.constitution-explorer.v1"
                &&roster.HullVersionId=="hull.constitution-explorer.v1"&&roster.Revision==1,"Unsupported starting-roster content.");
            return CampaignActivitiesData.Identity(p.ProfileId,p.BaseV1!.PackageHash,roster!.Identity,roster.ClassId,roster.HullVersionId,"R1;each-existing-actor-starter;actor-owned-publication;no-built-receipt");
        }
    }
}
