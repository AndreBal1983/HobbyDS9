#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public enum ReviewWorkspacePage { Map, Research, Production, FleetOrders, Combat, Saves, Audio, Tools, Designer, Worlds, Display, Empire }

    // Navigation owns presentation only. It never creates/replaces a host, refreshes a
    // campaign, drops a controller draft or dispatches authoritative work.
    public sealed class ReviewWorkspace
    {
        public ReviewCombatFlow Combat { get; }
        public ReviewUiModel Model=>Combat.Model;
        private ReviewController context;
        private ReviewWorkspacePage page=ReviewWorkspacePage.Map;
        private string? selectedNode,selectedFleet;
        public string? SelectedNode { get=>selectedNode;set{if(selectedNode!=value){RoutePreview=null;LastMapStarClick=null;LastMapFleetClick=null;}selectedNode=value;} }
        public string? SelectedFleet { get=>selectedFleet;set{if(selectedFleet!=value)RoutePreview=null;selectedFleet=value;} }
        internal (string? SystemId,string Caption)? PendingSystemInspection { get; set; }
        internal (ReviewFrame Frame,string World)? PendingWorldInspection { get; set; }
        internal bool PendingWorldDetails { get; set; }
        internal bool WorldDetailsOpen { get; private set; }
        internal (ReviewFrame Frame,string Node,int PointerId)? LastMapStarClick { get; set; }
        internal (ReviewFrame Frame,string Node,int PointerId)? LastMapFleetClick { get; set; }
        public string? SelectedColony { get; set; }
        public string? SelectedOwnShip { get; set; }
        public KnownRouteOffer? RoutePreview { get; set; }
        public bool DestinationExpanded { get; set; }
        public HobbyDS9.Contracts.ScienceDiscipline? ScienceDetails { get; set; }
        public string ResearchTab { get; set; }="science";
        public string? EngineeringProjectId { get; set; }
        public string EngineeringNewSearch { get; set; }="";
        public string EngineeringRefinementSearch { get; set; }="";
        public string EngineeringNewCategory { get; set; }="All technologies";
        public string EngineeringRefinementCategory { get; set; }="All technologies";
        public bool Compact { get; set; }
        public float MapZoom { get; set; }=1;
        public float MapPanX { get; set; }
        public float MapPanY { get; set; }
        public string FleetSort { get; set; }="Name";
        public bool FleetSortDescending { get; set; }
        public bool FleetOrganizationExpanded { get; set; }
        public string WorldSort { get; set; }="World";
        public bool WorldSortDescending { get; set; }
        public bool WorldColumnsExpanded { get; set; }
        public bool WorldsContextOpen { get; set; }
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
        private readonly List<Location> back=new List<Location>(),forward=new List<Location>();
        private long locationId,nextLocationId;
        internal long NavigationLocationId {get{Context();return locationId;}}
        internal IEnumerable<long> RetainedNavigationLocationIds=>back.Select(l=>l.Id).Concat(forward.Select(l=>l.Id)).Concat(new[]{locationId});
        private const int HistoryLimit=64;
        private sealed class Location
        {
            internal long Id;internal ReviewWorkspacePage Page;internal string? Node,Fleet,Ship,Colony;internal string Tab="science";internal bool WorldContext,WorldDetails;
            internal float Zoom,PanX,PanY;
            internal bool Same(Location other)=>Page==other.Page&&Node==other.Node&&Fleet==other.Fleet&&Ship==other.Ship&&Colony==other.Colony&&Tab==other.Tab&&WorldContext==other.WorldContext&&WorldDetails==other.WorldDetails;
        }
        private Location Capture()=>new Location{Id=locationId,Page=page,Node=SelectedNode,Fleet=SelectedFleet,Ship=SelectedOwnShip,Colony=SelectedColony,Tab=ResearchTab,WorldContext=page==ReviewWorkspacePage.Worlds&&WorldsContextOpen,WorldDetails=page==ReviewWorkspacePage.Worlds&&WorldDetailsOpen,Zoom=MapZoom,PanX=MapPanX,PanY=MapPanY};
        private static void Push(List<Location> stack,Location item){stack.Add(item);if(stack.Count>HistoryLimit)stack.RemoveAt(0);}
        public ReviewWorkspace(ReviewCombatFlow combat){Combat=combat;context=combat.Model.Current;}
        private void Context()
        {
            if(ReferenceEquals(context,Model.Current))return;
            context=Model.Current;page=ReviewWorkspacePage.Map;SelectedNode=null;SelectedFleet=null;SelectedColony=null;PendingSystemInspection=null;PendingWorldInspection=null;LastMapStarClick=null;LastMapFleetClick=null;SelectedOwnShip=null;RoutePreview=null;ScienceDetails=null;ResearchTab="science";MapZoom=1;MapPanX=MapPanY=0;NavigationEpoch=checked(NavigationEpoch+1);
            FleetSort="Name";FleetSortDescending=false;FleetOrganizationExpanded=false;WorldFilters.Clear();CollapsedSystems.Clear();WorldSort="World";WorldSortDescending=false;
            EngineeringProjectId=null;EngineeringNewSearch=EngineeringRefinementSearch="";EngineeringNewCategory=EngineeringRefinementCategory="All technologies";
            WorldColumnsExpanded=false;WorldsContextOpen=false;
            PendingWorldDetails=false;WorldDetailsOpen=false;back.Clear();forward.Clear();locationId=nextLocationId=0;
        }
        public ReviewWorkspacePage Page { get {Context();return page;} }
        public bool CanNavigate=>!Model.Closed&&!Model.Busy&&!Model.Current.HasPendingWork;
        public bool Navigate(ReviewWorkspacePage next)
        {
            Context();if(!Enum.IsDefined(typeof(ReviewWorkspacePage),next)||!CanNavigate)return false;
            if(PendingWorldInspection is var pending&&pending.HasValue&&(!ReferenceEquals(pending.Value.Frame,Model.Current.Frame)||Model.Current.Frame.Strategy?.Colonies.Any(c=>c.ColonyId.Value==pending.Value.World)!=true))
            {PendingWorldInspection=null;PendingWorldDetails=false;return false;}
            // Retain the enum value for source compatibility; Empire has one canonical destination.
            if(next==ReviewWorkspacePage.Empire)next=ReviewWorkspacePage.Worlds;
            var previous=Capture();page=next;
            if(next==ReviewWorkspacePage.Worlds)
            {
                WorldsContextOpen=PendingWorldInspection.HasValue||PendingSystemInspection.HasValue;
                WorldDetailsOpen=PendingWorldInspection.HasValue&&PendingWorldDetails;
            }
            else WorldDetailsOpen=false;
            if(previous.Same(Capture())){LastMapFleetClick=null;return true;}
            Push(back,previous);forward.Clear();
            locationId=checked(++nextLocationId);
            LastMapStarClick=null;LastMapFleetClick=null;NavigationEpoch=checked(NavigationEpoch+1);
            if(next==ReviewWorkspacePage.Combat)Combat.Open();
            return true;
        }
        public bool CanGoBack {get{Context();return CanNavigate&&!Model.Current.RefreshRequired&&back.Count>0;}}
        public bool CanGoForward {get{Context();return CanNavigate&&!Model.Current.RefreshRequired&&forward.Count>0;}}
        public bool GoBack()=>MoveHistory(back,forward);
        public bool GoForward()=>MoveHistory(forward,back);
        private bool MoveHistory(List<Location> source,List<Location> destination)
        {
            Context();if(!CanNavigate||Model.Current.RefreshRequired||source.Count==0)return false;
            var next=source[source.Count-1];source.RemoveAt(source.Count-1);Push(destination,Capture());
            var frame=Model.Current.Frame;page=next.Page;locationId=next.Id;
            SelectedNode=next.Node!=null&&frame.Strategy?.Navigation.Nodes.Any(n=>n.NodeId.Value==next.Node)==true?next.Node:null;
            SelectedFleet=next.Fleet!=null&&frame.OwnStrategicFleets.Any(f=>f.FleetId.Value==next.Fleet)?next.Fleet:null;
            SelectedOwnShip=SelectedFleet!=null&&frame.OwnStrategicFleets.Any(f=>f.FleetId.Value==SelectedFleet&&f.ShipIds.Any(s=>s.Value==next.Ship))?next.Ship:null;
            SelectedColony=next.Colony!=null&&frame.Strategy?.Colonies.Any(c=>c.ColonyId.Value==next.Colony)==true?next.Colony:null;
            ResearchTab=next.Tab;MapZoom=next.Zoom;MapPanX=next.PanX;MapPanY=next.PanY;
            WorldsContextOpen=next.WorldContext;WorldDetailsOpen=next.WorldDetails&&SelectedColony!=null;
            PendingWorldInspection=null;PendingSystemInspection=null;PendingWorldDetails=false;RoutePreview=null;LastMapStarClick=null;LastMapFleetClick=null;
            if(page==ReviewWorkspacePage.Worlds&&WorldsContextOpen&&WorldDetailsOpen&&SelectedColony!=null)
            {PendingWorldInspection=(frame,SelectedColony);PendingWorldDetails=WorldDetailsOpen;}
            NavigationEpoch=checked(NavigationEpoch+1);if(page==ReviewWorkspacePage.Combat)Combat.Open();return true;
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
