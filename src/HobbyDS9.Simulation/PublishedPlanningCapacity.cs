using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Concrete private-domain staging input. No callback, public command permission,
    // saved token or substitute AI validator. Shared ExecuteCommand builds candidates.
    internal sealed class PublishedPlanningCapacity
    {
        private readonly CampaignPlayableFleetPackageDto package;
        private readonly CampaignTacticalTravelSaveDto boundary;
        private readonly IReadOnlyList<CampaignIntegratedBuiltShipDto> statics;
        private readonly IReadOnlyList<CampaignPublishedBuiltShipDto> published;
        private readonly int budget,used;
        internal PublishedPlanningCapacity(CampaignPlayableFleetPackageDto package,CampaignTacticalTravelSaveDto boundary,
            IReadOnlyList<CampaignIntegratedBuiltShipDto> statics,IReadOnlyList<CampaignPublishedBuiltShipDto> published,int budget,int used)
        {this.package=package;this.boundary=CampaignTacticalTravelData.Copy(boundary);this.statics=statics;
         this.published=published.Select(PublishedBuiltShipValidation.Copy).ToList();this.budget=budget;this.used=used;}
        internal bool Fits(GameStateDto candidate)
        {
            var next=CampaignTacticalTravelData.Copy(boundary);next.Core.Campaign.GameState=SimulationSession.CopyState(candidate);
            return ConstructionJournalReserve.Evaluate(package,package.PackageHash!,next,statics,published,used,
                ConstructionJournalOperation.InternalAiProcurement,budget).Certified;
        }
    }
}
