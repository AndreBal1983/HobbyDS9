using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    internal enum ColonisationSourceEdition { Original=0,Growth=1,Repeat=2,Cycle=3 }
    internal static partial class ColonisationData
    {
        internal static string GrowthHash(CampaignColonisationExpansionPackageDto p)
        {
            Require(p!=null&&p.FormatVersion==1&&p.ProfileId==CampaignColonisationGrowthIdentity.PackageProfile&&p.PlannerPolicy!=null
                &&p.PlannerPolicy.Algorithm==CampaignColonisationGrowthIdentity.Planner,"Different closed growth package.");
            var original=new CampaignColonisationExpansionPackageDto{BaseRosterV2=p!.BaseRosterV2,PlannerPolicy=Copy(p.PlannerPolicy!)};
            original.PlannerPolicy.Algorithm=CampaignColonisationExpansionIdentity.Planner;
            return CampaignActivitiesData.Identity(CampaignColonisationGrowthIdentity.PackageProfile,ExpansionHash(original),CampaignColonisationGrowthIdentity.Profile,
                CampaignColonisationGrowthIdentity.Protocol,CampaignColonisationGrowthIdentity.Planner,CampaignColonisationGrowthIdentity.Budget,"one-extra-immigration-trip;100;kit-free;terminal-records2;journal1;future-ids0");
        }
    }
}
