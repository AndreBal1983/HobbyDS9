#nullable enable
using System;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public enum ReviewWorkspacePage { Map, Research, Production, FleetOrders, Combat, Saves, Audio, Tools, Designer, Worlds, Display }

    // Navigation owns presentation only. It never creates/replaces a host, refreshes a
    // campaign, drops a controller draft or dispatches authoritative work.
    public sealed class ReviewWorkspace
    {
        public ReviewCombatFlow Combat { get; }
        public ReviewUiModel Model=>Combat.Model;
        private ReviewController context;
        private ReviewWorkspacePage page=ReviewWorkspacePage.Map;
        private string? selectedNode,selectedFleet;
        public string? SelectedNode { get=>selectedNode;set{if(selectedNode!=value)RoutePreview=null;selectedNode=value;} }
        public string? SelectedFleet { get=>selectedFleet;set{if(selectedFleet!=value)RoutePreview=null;selectedFleet=value;} }
        public string? SelectedColony { get; set; }
        public string? SelectedOwnShip { get; set; }
        public KnownRouteOffer? RoutePreview { get; set; }
        public bool DestinationExpanded { get; set; }
        public HobbyDS9.Contracts.ScienceDiscipline? ScienceDetails { get; set; }
        public string ResearchTab { get; set; }="science";
        public bool Compact { get; set; }
        public float MapZoom { get; set; }=1;
        public float MapPanX { get; set; }
        public float MapPanY { get; set; }
        public string WorldSort { get; set; }="World";
        public bool WorldSortDescending { get; set; }
        public System.Collections.Generic.Dictionary<string,string> WorldFilters { get; }=new System.Collections.Generic.Dictionary<string,string>(StringComparer.Ordinal);
        public System.Collections.Generic.HashSet<string> CollapsedSystems { get; }=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        public bool RoutePreviewMatchesCurrent
        {
            get
            {
                Context();var offer=RoutePreview;var frame=Model.Current.Frame;
                if(offer==null||offer.Actor!=frame.Actor||offer.Fleet!=SelectedFleet||offer.Destination!=SelectedNode
                    ||offer.Turn!=frame.Status.Turn||offer.CampaignRevision!=frame.Status.CampaignRevision)return false;
                foreach(var fleet in frame.OwnStrategicFleets)if(fleet.FleetId.Value==offer.Fleet)return fleet.LocationNodeId.Value==offer.Origin;
                return false;
            }
        }
        public long NavigationEpoch { get; private set; }
        public ReviewWorkspace(ReviewCombatFlow combat){Combat=combat;context=combat.Model.Current;}
        private void Context()
        {
            if(ReferenceEquals(context,Model.Current))return;
            context=Model.Current;page=ReviewWorkspacePage.Map;SelectedNode=null;SelectedFleet=null;SelectedColony=null;SelectedOwnShip=null;RoutePreview=null;ScienceDetails=null;ResearchTab="science";MapZoom=1;MapPanX=MapPanY=0;NavigationEpoch=checked(NavigationEpoch+1);
            WorldFilters.Clear();CollapsedSystems.Clear();WorldSort="World";WorldSortDescending=false;
        }
        public ReviewWorkspacePage Page { get {Context();return page;} }
        public bool CanNavigate=>!Model.Closed&&!Model.Busy&&!Model.Current.HasPendingWork;
        public bool Navigate(ReviewWorkspacePage next)
        {
            Context();if(!Enum.IsDefined(typeof(ReviewWorkspacePage),next)||!CanNavigate)return false;
            if(page==next)return true;
            page=next;NavigationEpoch=checked(NavigationEpoch+1);
            if(next==ReviewWorkspacePage.Combat)Combat.Open();
            return true;
        }
        public string BoundaryIdentity
        {
            get
            {
                Context();var frame=Model.Current.Frame;
                return "Turn "+frame.Status.Turn+" | "+frame.Status.Phase+" | Owner view "+frame.Actor;
            }
        }
    }
}
