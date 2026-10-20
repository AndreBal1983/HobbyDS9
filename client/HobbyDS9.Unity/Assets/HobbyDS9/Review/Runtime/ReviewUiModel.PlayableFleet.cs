#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private readonly CampaignPlayableFleetPackageDto? playableFleetPackage;
        private readonly ReviewPlayableFleetSlotStore? playableFleetSlots;
        public HashSet<string> OrganizationShips { get; }=new HashSet<string>(StringComparer.Ordinal);
        public string OrganizationName { get; set; }="";
        public bool OrganizationDirty=>OrganizationShips.Count>0||OrganizationName.Length>0;
        public void ResetOrganization(){OrganizationShips.Clear();OrganizationName="";}
        public ReviewUiModel(CampaignPlayableFleetPackageDto package,ReviewNewtonsoftCodec codec,ReviewPlayableFleetSlotStore slots)
        {
            playableFleetPackage=codec.CopyPackage(package);this.codec=codec;playableFleetSlots=slots;
            host=PhysicalReviewHost.StartPlayableFleet(playableFleetPackage,codec,slots);
            var humans=playableFleetPackage.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Civilizations!
                .Where(c=>c.ControllerKind==ControllerKind.Human).ToList();
            if(humans.Count!=1)throw new ArgumentException("Playable fleet requires exactly one human controller.",nameof(package));
            Current=Controller(host,humans[0].CivilizationId);
        }
        public IReadOnlyList<CampaignNamedFleet> NamedFleets()=>host.CaptureFleetOrganization(Current.Frame);
        public CampaignBattlePlayback Playback(string encounter)=>host.CaptureBattlePlayback(Current.Frame,encounter);
        public IReadOnlyList<CampaignBattlePlayback> History()=>host.CaptureBattleHistory(Current.Frame);
        public bool OrganizeFleet(ReviewFrame reviewed,CampaignFleetOrganizationKind kind,IReadOnlyList<string> ships,
            string? target,string? name,string command)=>Run(()=>{
                // A modal's accepted intent belongs to its exact observation, never a silently refreshed one.
                if(!PlayableFleet||!ReferenceEquals(reviewed,Current.Frame)||Current.Dirty||(ResearchDraft?.Dirty??false)||LocalDesignDirty||Current.HasPendingWork||Current.RefreshRequired)
                    return Unavailable();
                var result=host.OrganizeFleet(reviewed,kind,ships,target,name,command);
                Message=result.Accepted?"Fleet updated. Ship condition and equipment are retained.":OrganizationFailure(result.Error);
                if(!result.Accepted)return false;
                var refreshed=Current.Refresh();
                if(refreshed)ResetOrganization();
                return refreshed;
            });
        private static string OrganizationFailure(CampaignFleetOrganizationError error)
        {
            switch(error)
            {
                case CampaignFleetOrganizationError.StaleTurn:
                case CampaignFleetOrganizationError.StaleRevision:return "The campaign changed. Refresh, review the selected ships and try again.";
                case CampaignFleetOrganizationError.FleetHasOrders:return "Use Remain to clear every affected fleet's travel orders first.";
                case CampaignFleetOrganizationError.FleetInTransit:return "Wait for the affected fleets to finish travelling.";
                case CampaignFleetOrganizationError.NotColocated:return "All selected ships and the destination fleet must share one location.";
                case CampaignFleetOrganizationError.WrongPhase:return "Fleet membership can change during strategic Planning.";
                case CampaignFleetOrganizationError.InvalidName:return "Enter a fleet name of 1–80 characters, without control characters.";
                case CampaignFleetOrganizationError.FleetNotOwned:
                case CampaignFleetOrganizationError.ShipNotOwned:return "Only your own ships and fleets can be organized.";
                case CampaignFleetOrganizationError.UnknownFleet:
                case CampaignFleetOrganizationError.UnknownShip:return "A selected ship or fleet is no longer available. Review the current roster.";
                default:return "Fleet change unavailable. Your campaign and unsent organization draft are unchanged.";
            }
        }
    }
}
