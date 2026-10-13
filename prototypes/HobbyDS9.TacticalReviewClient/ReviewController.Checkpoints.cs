namespace HobbyDS9.TacticalReviewClient
{
    public sealed partial class ReviewController
    {
        public ReviewCheckpointError LastCheckpointError { get; private set; }
        public ReviewCheckpointResult SaveCommittedToSlot(string slot, bool overwrite = false)
        {
            var result=pending == null ? host.SaveCommittedToSlot(slot,overwrite)
                : new ReviewCheckpointResult(ReviewCheckpointError.Busy);
            LastCheckpointError=result.Error;
            return result; // Unsent local edits are never handed to the store.
        }
        public ReviewCheckpointResult LoadCommittedFromSlot(string slot, bool discardDraft = false)
        {
            ReviewCheckpointResult result;
            if(pending != null) result=new ReviewCheckpointResult(ReviewCheckpointError.Busy);
            else if(Dirty && !discardDraft) result=new ReviewCheckpointResult(ReviewCheckpointError.DirtyDraft);
            else result=host.LoadCommittedFromSlot(slot,Frame.Actor,Frame.Battle?.EncounterId);
            LastCheckpointError=result.Error;
            if(result.Succeeded) ReplaceCheckpointFrame(result.Frame!);
            return result;
        }
        public ReviewCheckpointResult ReopenFromSlot(string slot, bool discardDraft = false)
        {
            if(pending != null || Dirty && !discardDraft)
            {
                LastCheckpointError=pending != null ? ReviewCheckpointError.Busy : ReviewCheckpointError.DirtyDraft;
                return new ReviewCheckpointResult(LastCheckpointError);
            }
            var opened=host.Reopen(slot,Frame.Actor,Frame.Battle?.EncounterId);
            LastCheckpointError=opened.Error;
            if(!opened.Succeeded) return new ReviewCheckpointResult(opened.Error);
            host=opened.Host!; ReplaceCheckpointFrame(opened.Frame!);
            return new ReviewCheckpointResult(ReviewCheckpointError.None,frame:Frame);
        }
        private void ReplaceCheckpointFrame(ReviewFrame next)
        {
            Frame=next; draft.Clear(); travelDraft=null; SelectedShip=null; pending=null;
            Mode=ModeFor(next); RefreshRequired=false; LastBackendError=null; DraftInvalidated=false;
        }
    }
}
