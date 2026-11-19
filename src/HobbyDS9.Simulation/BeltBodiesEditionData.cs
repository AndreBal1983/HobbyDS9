using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    internal static class BeltBodiesEditionData
    {
        internal static CampaignBeltBodiesPackageDto Create(CampaignColonisationExpansionPackageDto growth)
        {
            var refinement=EngineeringRefinementEditionData.Create(growth);
            var result=new CampaignBeltBodiesPackageDto{BaseGrowth=refinement.BaseGrowth,Policy=refinement.Policy,Generation=refinement.Generation};
            result.PackageHash=Hash(result);return result;
        }
        internal static string Hash(CampaignBeltBodiesPackageDto p)
        {
            Require(p!=null&&p.FormatVersion==BeltBodiesEditionIdentity.PackageFormat&&p.ProfileId==BeltBodiesEditionIdentity.PackageProfile
                &&p.BeltBodiesPolicyId==BeltBodiesEditionIdentity.Policy,"Different belt-body source.");
            var refinement=new CampaignEngineeringRefinementPackageDto{BaseGrowth=p!.BaseGrowth,Policy=p.Policy,Generation=p.Generation,RefinementPolicyId=p.RefinementPolicyId};
            return CampaignActivitiesData.Identity(BeltBodiesEditionIdentity.Profile,EngineeringRefinementEditionData.Hash(refinement),BeltBodiesEditionIdentity.Policy,
                "carrier18-v1;body1000;observations4000;surveys4096;mines2048;commands4096;shipments1000;separate-belt-not-planet;preserve-feature-radii-seed;cross-type-offset-revision-order;support5",BeltMiningPolicy.Tuning);
        }
    }
}
