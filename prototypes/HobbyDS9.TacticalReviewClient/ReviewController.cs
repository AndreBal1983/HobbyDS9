using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    // Headless local interaction controller. It never determines gameplay outcomes.
    public sealed partial class ReviewController
    {
        private ITacticalClientHost host;
        private readonly Guid id=Guid.NewGuid();
        private readonly Dictionary<string,FleetEdit> draft=new Dictionary<string,FleetEdit>(StringComparer.Ordinal);
        private ReviewWork? pending;
        private long request;
        public ReviewFrame Frame { get; private set; }
        public ReviewMode Mode { get; private set; }
        public CampaignTacticalError? LastBackendError { get; private set; }
        public bool RefreshRequired { get; private set; }
        public bool Dirty => draft.Count != 0 || travelDraft != null;
        public IReadOnlyList<FleetEdit> Draft => draft.Values.OrderBy(o=>o.Fleet,StringComparer.Ordinal).ToList().AsReadOnly();
        public bool HasPendingWork => pending != null;
        public string? SelectedShip { get; private set; }
        public bool DraftInvalidated { get; private set; }
        public ReviewController(ReviewHost host, string actor)
            : this((ITacticalClientHost)host,actor) { }
        public ReviewController(PhysicalReviewHost host,string actor)
            : this((ITacticalClientHost)host,actor) { }
        private ReviewController(ITacticalClientHost host,string actor)
        { this.host=host; Frame=host.Read(actor); Mode=ModeFor(Frame); }
        private static ReviewMode ModeFor(ReviewFrame frame)
            => frame.Battle == null ? (frame.Status.Phase == SessionPhase.Planning ? ReviewMode.Planning : ReviewMode.EncounterSelection)
                : frame.Battle.Complete ? ReviewMode.Complete
                : frame.Battle.OwnCommitmentAccepted ? ReviewMode.Waiting : ReviewMode.Orders;
        public bool SelectEncounter(string encounter, bool discardDraft = false)
        {
            if(pending != null || Dirty && !discardDraft || !Frame.Encounters.Contains(encounter,StringComparer.Ordinal)) return false;
            var next=host.Read(Frame.Actor,encounter);
            if(next.Battle == null || !SameHostGeneration(next) || next.Status.CampaignRevision < Frame.Status.CampaignRevision) return false;
            draft.Clear(); travelDraft=null; SelectedShip=null; Frame=next; Mode=ModeFor(next); RefreshRequired=false; LastBackendError=null;
            return true;
        }
        public bool InspectShip(string ship)
        {
            if(!Frame.OwnShips.Any(s=>s.Condition.ShipId == ship)
                && !(Frame.Battle?.VisibleEnemies.Any(s=>s.ShipId == ship) ?? false)) return false;
            SelectedShip=ship; return true;
        }
        public bool EditFleet(string fleet, CampaignFleetManeuver maneuver, string? focus)
        {
            var b=Frame.Battle;
            var standing=b?.StandingOrders.SingleOrDefault(o=>o.FleetId == fleet && o.CanOrder);
            if(Mode != ReviewMode.Orders || RefreshRequired || b == null
                || standing == null
                || !b.LegalManeuvers.Contains(maneuver)
                || focus != null && focus != standing.FocusFleetId
                    && !b.LegalFocusFleetIds.Contains(focus,StringComparer.Ordinal)) return false;
            draft[fleet]=new FleetEdit(fleet,maneuver,focus); DraftInvalidated=false; return true;
        }
        public bool ReviewRound()
        {
            if(Mode != ReviewMode.Orders || RefreshRequired || Frame.Battle == null || Frame.Battle.Complete
                || Frame.Battle.OwnCommitmentAccepted) return false;
            Mode=ReviewMode.RoundReview; return true;
        }
        public bool CancelReview()
        {
            if(Mode != ReviewMode.RoundReview && Mode != ReviewMode.CompletionReview && Mode != ReviewMode.TravelReview) return false;
            Mode=ModeFor(Frame); return true;
        }
        public ReviewWork? ConfirmRound(string commandId)
        {
            if(Mode != ReviewMode.RoundReview || pending != null || RefreshRequired) return null;
            pending=new ReviewWork(Frame,id,checked(++request),ReviewAction.Round,commandId,Draft);
            Mode=ReviewMode.Sending; return pending;
        }
        public bool ReviewCompletion()
        {
            if(Mode != ReviewMode.Complete || RefreshRequired || !Frame.Status.CanFinalizeBatch) return false;
            Mode=ReviewMode.CompletionReview; return true;
        }
        public ReviewWork? ConfirmCompletion()
        {
            if(Mode != ReviewMode.CompletionReview || pending != null || RefreshRequired) return null;
            pending=new ReviewWork(Frame,id,checked(++request),ReviewAction.Finalize,"",Array.Empty<FleetEdit>());
            Mode=ReviewMode.Finalizing; return pending;
        }
        private bool SameHostGeneration(ReviewFrame next)
            => next.HostId == Frame.HostId && next.Generation == Frame.Generation && next.Actor == Frame.Actor;
        private bool Monotone(ReviewFrame next)
        {
            if(!SameHostGeneration(next) || next.Status.Turn < Frame.Status.Turn
                || next.Status.CampaignRevision < Frame.Status.CampaignRevision) return false;
            var old=Frame.Battle; var b=next.Battle;
            if(old == null) return b == null;
            if(b == null) return next.Status.Phase == SessionPhase.Planning && next.Status.Turn > Frame.Status.Turn;
            if(b.EncounterId != old.EncounterId || b.ActorId != Frame.Actor || b.Turn != old.Turn
                || b.Round < old.Round || b.BoundaryRevision < old.BoundaryRevision || old.Complete && !b.Complete) return false;
            if(b.Round == old.Round && (b.BoundaryRevision != old.BoundaryRevision
                || old.OwnCommitmentAccepted && !b.OwnCommitmentAccepted)) return false;
            return true;
        }
        public bool Receive(ReviewReceipt receipt)
        {
            // Request identity + generation also reject another controller's legitimate response.
            if(pending == null || !ReferenceEquals(pending,receipt.Work)
                || receipt.Work.ControllerId != id || receipt.Work.Generation != Frame.Generation) return false;
            if(!host.IsCurrent(receipt.Work))
            { pending=null; RefreshRequired=true; Mode=ModeFor(Frame); return false; }
            pending=null; LastBackendError=receipt.Result.Error;
            if(!receipt.Result.Accepted)
            { Mode=ModeFor(Frame); RefreshRequired=true; return true; }
            if(!Monotone(receipt.Frame) || receipt.Frame.Status.CampaignRevision < receipt.Result.CampaignRevision)
            { Mode=ModeFor(Frame); RefreshRequired=true; return true; }
            Frame=receipt.Frame; draft.Clear(); travelDraft=null; Mode=ModeFor(Frame); RefreshRequired=false; LastBackendError=null; DraftInvalidated=false;
            return true;
        }
        public bool Refresh()
        {
            if(pending != null) return false;
            var next=host.Read(Frame.Actor,Frame.Battle?.EncounterId);
            if(!Monotone(next)) { RefreshRequired=true; return false; }
            bool changedBoundary=Frame.Battle != null && (next.Battle == null
                || next.Battle.Round != Frame.Battle.Round || next.Battle.BoundaryRevision != Frame.Battle.BoundaryRevision);
            // A changed authoritative boundary invalidates local edits rather than resubmitting them.
            if(changedBoundary || next.Battle?.OwnCommitmentAccepted == true
                || travelDraft != null && (next.Status.CampaignRevision != Frame.Status.CampaignRevision || next.Status.Turn != Frame.Status.Turn))
            { DraftInvalidated=Dirty; draft.Clear(); travelDraft=null; }
            Frame=next; Mode=ModeFor(next); RefreshRequired=false; return true;
        }
        public ReviewSaveHandle SaveCommitted()
        {
            if(pending != null) throw new InvalidOperationException("Wait for the pending operation before saving.");
            return host.SaveCommitted(); // Local unsent edits intentionally remain outside the save.
        }
        public bool LoadCommitted(ReviewSaveHandle handle, bool discardDraft = false)
        {
            if(pending != null || Dirty && !discardDraft) return false;
            var next=host.LoadCommitted(handle,Frame.Actor,Frame.Battle?.EncounterId);
            Frame=next; draft.Clear(); travelDraft=null; SelectedShip=null; pending=null; Mode=ModeFor(next); RefreshRequired=false; LastBackendError=null; DraftInvalidated=false;
            return true;
        }
        public bool ResetDraft()
        {
            if(pending != null || Mode != ReviewMode.Orders && Mode != ReviewMode.RoundReview) return false;
            draft.Clear(); Mode=ModeFor(Frame); DraftInvalidated=false; return true;
        }
    }
}
