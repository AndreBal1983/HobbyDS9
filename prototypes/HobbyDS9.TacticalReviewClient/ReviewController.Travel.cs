using System;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public sealed partial class ReviewController
    {
        private TravelEdit? travelDraft;
        public TravelEdit? TravelDraft => travelDraft;
        public KnownRouteOffer? FindKnownRoute(string fleet,string destination)
        {
            if(pending != null || Mode != ReviewMode.Planning || !(host is PhysicalReviewHost physical)
                || !Frame.OwnStrategicFleets.Any(f=>f.FleetId.Value == fleet)) return null;
            return physical.FindKnownRoute(Frame,fleet,destination);
        }
        public bool EditTravel(string fleet,CampaignTravelIntent intent,KnownRouteOffer? offer = null,string? contact = null)
        {
            if(Mode != ReviewMode.Planning || RefreshRequired || Frame.Travel == null
                || !Frame.OwnStrategicFleets.Any(f=>f.FleetId.Value == fleet) || !Enum.IsDefined(typeof(CampaignTravelIntent),intent)) return false;
            if(offer != null && (offer.HostId != Frame.HostId || offer.Generation != Frame.Generation || offer.Actor != Frame.Actor
                || offer.Fleet != fleet || offer.Turn != Frame.Status.Turn || offer.CampaignRevision != Frame.Status.CampaignRevision || !offer.Route.Success)) return false;
            // These are menu/input guards only. No graph, hostility, transit or pursuit rules are calculated.
            if((intent == CampaignTravelIntent.Stay || intent == CampaignTravelIntent.StayEngage) && (offer != null || contact != null)) return false;
            if(intent == CampaignTravelIntent.Leave && (offer == null || contact != null)) return false;
            var known=contact == null ? null : Frame.Travel.KnownContacts.SingleOrDefault(k=>k.FleetId == contact);
            if(intent == CampaignTravelIntent.Follow && (known == null || !known.Fresh)) return false;
            if(intent != CampaignTravelIntent.Follow && contact != null) return false;
            travelDraft=new TravelEdit(fleet,intent,contact,known?.LastSeenTurn,offer?.Route.Steps ?? Array.Empty<string>());
            DraftInvalidated=false; return true;
        }
        public bool ReviewTravel()
        {
            if(Mode != ReviewMode.Planning || travelDraft == null || RefreshRequired) return false;
            Mode=ReviewMode.TravelReview; return true;
        }
        public ReviewWork? ConfirmTravel(string commandId)
        {
            if(Mode != ReviewMode.TravelReview || travelDraft == null || pending != null || RefreshRequired) return null;
            pending=new ReviewWork(Frame,id,checked(++request),commandId,travelDraft);
            Mode=ReviewMode.Sending; return pending;
        }
        public bool ResetTravelDraft()
        {
            if(pending != null || Mode != ReviewMode.Planning && Mode != ReviewMode.TravelReview) return false;
            travelDraft=null; DraftInvalidated=false; Mode=ModeFor(Frame); return true;
        }
    }
}
