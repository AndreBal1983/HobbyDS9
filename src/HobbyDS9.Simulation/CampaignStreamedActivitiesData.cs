using System;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;

namespace HobbyDS9.Simulation
{
    internal static class CampaignStreamedActivitiesData
    {
        internal static string PackageHash(CampaignStreamedActivitiesPackageDto p)
        {
            CampaignActivitiesData.Require(p!=null&&p.FormatVersion==1&&p.ProfileId==CampaignStreamedActivitiesIdentity.PackageProfile
                &&p.ContentPackage!=null,"Explicit streamed content package required.");
            var source=CampaignActivitiesData.PackageHash(p!.ContentPackage!);
            CampaignActivitiesData.Require(source==p.ContentPackage!.PackageHash,"Different frozen activity content.");
            return CampaignActivitiesData.Identity(CampaignStreamedActivitiesIdentity.PackageProfile,CampaignStreamedActivitiesIdentity.Policy,
                "ceil-material-apportionment-v1","head-tranche-same-economy-freight-v1","terminal-liability-next-boundary-v1",source);
        }
        internal static StreamedProductionItemDto Copy(StreamedProductionItemDto p)=>new StreamedProductionItemDto{
            QueueItemId=p.QueueItemId,ColonyId=p.ColonyId,OwnerId=p.OwnerId,Kind=p.Kind,DefinitionId=p.DefinitionId,
            Blueprint=p.Blueprint==null?null:BlueprintCopies.Copy(p.Blueprint),RequiredIndustry=p.RequiredIndustry,CompletedIndustry=p.CompletedIndustry,
            RequiredResources=p.RequiredResources?.Select(PopulationData.Copy).ToList(),ConsumedResources=p.ConsumedResources?.Select(PopulationData.Copy).ToList()};
        internal static StreamedProductionReceiptDto Copy(StreamedProductionReceiptDto r)=>new StreamedProductionReceiptDto{
            Order=r.Order==null?null:Copy(r.Order),Completed=r.Completed,ResolvedTurn=r.ResolvedTurn,ShipId=r.ShipId};
    }
}
