using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    internal static class EngineeringRefinementEditionData
    {
        internal static CampaignEngineeringRefinementPackageDto Create(CampaignColonisationExpansionPackageDto growth)
        {
            Require(growth!=null&&growth.PackageHash==ColonisationData.GrowthHash(growth),"An exact growth source is required.");
            var previous=growth!.BaseRosterV2!;var old=previous.BaseV1!;
            // Only the support edition changes. The old package graph remains untouched.
            var colony=new CampaignColonisationPackageDto{Activities=old.Activities,Catalogue=old.Catalogue,ReadyClasses=old.ReadyClasses,
                Settlement=old.Settlement,Support=PublishedTacticalSupportCompiler.CreateRefinementSupport(old.Support!)};
            colony.PackageHash=ColonisationData.PackageHash(colony);
            var roster=new CampaignColonisationRosterPackageDto{BaseV1=colony,StartingRoster=previous.StartingRoster};
            roster.PackageHash=ColonisationData.RosterHash(roster);
            var next=new CampaignColonisationExpansionPackageDto{ProfileId=CampaignColonisationGrowthIdentity.PackageProfile,
                BaseRosterV2=roster,PlannerPolicy=ColonisationData.Copy(growth.PlannerPolicy!)};
            next.PackageHash=ColonisationData.GrowthHash(next);
            var result=new CampaignEngineeringRefinementPackageDto{BaseGrowth=next};result.PackageHash=Hash(result);return result;
        }
        internal static string Hash(CampaignEngineeringRefinementPackageDto p)
        {
            Require(p!=null&&p.FormatVersion==4&&p.ProfileId==EngineeringRefinementIdentity.PackageProfile&&p.RefinementPolicyId==EngineeringRefinementIdentity.Policy
                &&p.BaseGrowth?.BaseRosterV2?.BaseV1?.Support!=null,"Different targeted refinement package.");
            var support=p!.BaseGrowth!.BaseRosterV2!.BaseV1!.Support!;
            Require(support.FormatVersion==ColonisationRefinementSupportIdentity.Format&&support.SchemaId==ColonisationRefinementSupportIdentity.Schema
                &&support.CompilerId==ColonisationRefinementSupportIdentity.Compiler,"Explicit refinement support v5 is required.");
            var authored=new CampaignAuthoredSolPackageDto{BaseGrowth=p.BaseGrowth,Policy=p.Policy,Generation=p.Generation};
            return CampaignActivitiesData.Identity(EngineeringRefinementIdentity.Profile,AuthoredSolEditionData.Hash(authored,true),EngineeringRefinementIdentity.Policy,
                "carrier17-v1;projects64;commands1024;single-base-journal-offset-list-order;frozen-source-replay;pending-before-decision;support5;work24000;milli1000;root-maturity0;step5000;minimum-step100;curve100000-150000;gain-bps800,900,1000,1100,1200;industry-gain1000,1250,1500,2000,2500;downside200,400,600,1000,1500;weights1,2,3,2,1;root-envelope-half-to-double;category70,30;protected-frozen-fit-outputs-allocation");
        }
    }
}
