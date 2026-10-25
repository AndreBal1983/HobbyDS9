using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private sealed class StreamedActivitiesContext
        {
            internal readonly string PackageHash;
            internal List<StreamedProductionItemDto> Production=new List<StreamedProductionItemDto>();
            internal List<StreamedProductionReceiptDto> History=new List<StreamedProductionReceiptDto>();
            internal StreamedActivitiesContext(string hash){PackageHash=hash;}
            internal StreamedActivitiesContext Copy()=>new StreamedActivitiesContext(PackageHash){Production=Production.Select(CampaignStreamedActivitiesData.Copy).ToList(),History=History.Select(CampaignStreamedActivitiesData.Copy).ToList()};
        }
        private StreamedActivitiesContext? Streams=>activities?.Streamed;
        public static CampaignStreamedActivitiesPackageDto CreateStreamedActivitiesPackage(CampaignActivitiesPackageDto source)
        {
            RequireActivitiesPackage(source);
            // Caller content is validated and deeply retained by Start, not trusted by a hash.
            var p=new CampaignStreamedActivitiesPackageDto{ContentPackage=source};p.PackageHash=CampaignStreamedActivitiesData.PackageHash(p);return p;
        }
        private static void RequireStreamedPackage(CampaignStreamedActivitiesPackageDto p)
        {Require(p!=null&&p.PackageHash==CampaignStreamedActivitiesData.PackageHash(p),"Different streamed activity package.");}
        public static CampaignTacticalReviewSession StartStreamedActivities(CampaignStreamedActivitiesPackageDto p,NaturalGalaxySettingsDto settings)
        {
            RequireStreamedPackage(p);var source=p.ContentPackage!;
            var catalog=source.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
            var map=NaturalGalaxyGenerator.Generate(settings,catalog);
            return StartActivitiesFrozen(source,GeneratedCampaignFactory.Frozen(map,catalog),p);
        }
        public CampaignStreamedActivitiesSaveDto ExportStreamedActivitiesSave()
        {
            lock(gate)
            {
                Require(Streams!=null,"Explicit streamed root required.");ValidateActivityState();
                return new CampaignStreamedActivitiesSaveDto{TemplatePackageHash=Streams!.PackageHash,PackageHash=playableHash!,SupportHash=published!.SupportHash,
                    Map=NaturalGalaxyData.Copy(generatedMap!),Campaign=ExportPlayableCore(),Discoveries=discoveries.Select(CopyDiscovery).ToList(),
                    PublishedBuiltShips=published.Records.OrderBy(r=>r.ShipId,StringComparer.Ordinal).Select(PublishedBuiltShipValidation.Copy).ToList(),
                    Activities=CampaignActivitiesData.Copy(activities!.State),Production=Streams.Production.Select(CampaignStreamedActivitiesData.Copy).ToList(),
                    ProductionHistory=Streams.History.Select(CampaignStreamedActivitiesData.Copy).ToList(),Journal=activities.Journal.Select(CopyActivityOperation).ToList()};
            }
        }
        public static CampaignTacticalReviewSession LoadStreamedActivities(CampaignStreamedActivitiesPackageDto p,CampaignStreamedActivitiesSaveDto root)
        {
            RequireStreamedPackage(p);
            Require(root!=null&&root.FormatVersion==CampaignStreamedActivitiesIdentity.Format&&root.ProfileId==CampaignStreamedActivitiesIdentity.Profile
                &&root.PolicyId==CampaignStreamedActivitiesIdentity.Policy&&root.TemplatePackageHash==p.PackageHash&&root.Map!=null&&root.Campaign!=null
                &&root.Activities!=null&&root.Production!=null&&root.ProductionHistory!=null&&root.Discoveries!=null&&root.PublishedBuiltShips!=null
                &&root.Journal!=null&&root.Journal.Count<=p.ContentPackage!.BaseTemplate!.ReplayOperationLimit,"Complete streamed root required; no conversion.");
            Require(root!.Campaign!.Integrated?.Physical?.Core.ProfileId==CampaignStreamedActivitiesIdentity.EmbeddedCoreProfile,"Streamed embedded marker required.");
            var map=GeneratedCampaignFactory.Frozen(root.Map!,p.ContentPackage!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!);
            var replay=StartActivitiesFrozen(p.ContentPackage!,map,p);
            Require(root.PackageHash==replay.playableHash&&root.SupportHash==replay.published!.SupportHash,"Different streamed geometry/support identity.");
            foreach(var entry in root.Journal!)
            {
                Require(entry!=null&&(entry.Activity==null)!=(entry.Common==null),"Closed streamed operation.");
                var expected=CopyActivityOperation(entry!);
                var accepted=entry!.Activity!=null?replay.SubmitActivity(entry.Activity).Accepted:ReplayActivityCommon(replay,entry.Common!);
                Require(accepted&&FrontierSame(expected,replay.activities!.Journal.Last()),"Illegal streamed operation.");
            }
            Require(FrontierSame(replay.ExportStreamedActivitiesSave(),root),"Streamed root differs from complete ordered replay.");return replay;
        }
        public CampaignStreamedActivityObservationDto CaptureStreamedActivities(string actor)
        {
            lock(gate)
            {
                Require(Streams!=null,"Explicit streamed observation required.");var observation=CaptureActivities(actor);
                var game=state.Campaign.GameState;var own=game.Colonies!.Where(c=>c.OwnerCivilizationId==actor).ToList();
                // The ordinary immutable class valuation remains in authority; normal production costs zero credits here.
                foreach(var option in observation.BuildOptions)option.Cost.Credits=0;
                var result=new CampaignStreamedActivityObservationDto{Activities=observation,Construction=CaptureStreamedConstruction(own,game),
                    Production=Streams!.Production.Where(q=>q.OwnerId==actor).Select(CampaignStreamedActivitiesData.Copy).ToList(),
                    Supply=own.Select(c=>new StreamedOwnColonySupplyDto{ColonyId=c.ColonyId,Stocks=c.Stockpiles!.Select(PopulationData.Copy).ToList(),
                        Deliveries=game.LastDeliveries!.Where(d=>d.DestinationPlanetId==c.PlanetId).Select(PopulationData.Copy).ToList()}).ToList()};
                foreach(var group in Streams.Production.Where(q=>q.OwnerId==actor).GroupBy(q=>q.ColonyId,StringComparer.Ordinal))
                {
                    var head=group.First();var colony=own.Single(c=>c.ColonyId==head.ColonyId);var industry=WorldEconomy.Project(colony,game,definitions).Industry;
                    var plan=StreamedMaterialWork.Plan(head.RequiredResources!,colony.Stockpiles!,head.RequiredIndustry,head.CompletedIndustry,industry);
                    result.Availability.Add(new StreamedProductionAvailabilityDto{QueueItemId=head.QueueItemId,WorkWithCurrentStocks=plan.CompletedWork-head.CompletedIndustry,
                        Status=industry<=0?"Awaiting Industry":plan.CompletedWork==head.CompletedIndustry?"Awaiting materials":
                            plan.CompletedWork-head.CompletedIndustry<Math.Min(industry,head.RequiredIndustry-head.CompletedIndustry)?"Limited by local materials":"Ready with current stocks"});
                }
                return result;
            }
        }
    }
}
