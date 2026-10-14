#nullable enable
using System;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public enum ReviewWorkspacePage { Map, Research, Production, FleetOrders, Combat }

    // Navigation owns presentation only. It never creates/replaces a host, refreshes a
    // campaign, drops a controller draft or dispatches authoritative work.
    public sealed class ReviewWorkspace
    {
        public ReviewCombatFlow Combat { get; }
        public ReviewUiModel Model=>Combat.Model;
        private ReviewController context;
        private ReviewWorkspacePage page=ReviewWorkspacePage.Map;
        public long NavigationEpoch { get; private set; }
        public ReviewWorkspace(ReviewCombatFlow combat){Combat=combat;context=combat.Model.Current;}
        private void Context()
        {
            if(ReferenceEquals(context,Model.Current))return;
            context=Model.Current;page=ReviewWorkspacePage.Map;NavigationEpoch=checked(NavigationEpoch+1);
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
