#nullable enable
using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private ReviewFrame? researchQueuedFrame;
        private ReviewController? researchQueuedController,researchNoticeController;
        private IReadOnlyList<ScienceAllocationDto>? researchQueuedScience;
        private int? researchQueuedSplit;
        private bool researchFlushing;
        private string researchNotice="";
        public bool ResearchCommitPending=>ReferenceEquals(researchQueuedController,Current)&&(researchQueuedScience!=null||researchQueuedSplit.HasValue);
        public string ResearchCommitNotice=>ReferenceEquals(researchNoticeController,Current)?researchNotice:"";
        // Planning work and session-replacement protection deliberately have different scopes.
        public bool HasPlanningEdits=>Current.Dirty||OrganizationDirty||(ResearchDraft?.Dirty??false)||ResearchCommitPending;
        public bool HasOrderDraft=>Current.Dirty||OrganizationDirty;
        public bool QueueScienceAllocation(ReviewResearchDraft draft)
        {
            if(!QueueResearch(draft))return false;
            researchQueuedScience=draft.Values();return true;
        }
        public bool QueueResearchSplit(ReviewResearchDraft draft)
        {
            if(!QueueResearch(draft))return false;
            researchQueuedSplit=draft.ScienceUnits;return true;
        }
        private bool QueueResearch(ReviewResearchDraft draft)
        {
            if(Busy||Closed||researchFlushing||Current.RefreshRequired||!ReferenceEquals(draft,ResearchDraft)||!draft.Current(Current.Frame))return false;
            if(researchQueuedFrame!=null&&(!ReferenceEquals(researchQueuedFrame,Current.Frame)||!ReferenceEquals(researchQueuedController,Current)))
            {RejectResearchCommit("The campaign changed. Accepted allocations restored; change the control again after refreshing.");return false;}
            researchQueuedFrame=Current.Frame;researchQueuedController=Current;
            researchNoticeController=Current;researchNotice="Applying allocation…";return true;
        }
        // Owning UI thread only. One scheduled drain coalesces each layer to its latest full plan.
        // Commands are synchronous and serialized. An accepted first layer gives the second its
        // new stamp; a rejection stops the batch. Stale observations are never automatically retried.
        public bool FlushResearchAllocations()
        {
            if(researchFlushing)return false;
            if(researchQueuedScience==null&&!researchQueuedSplit.HasValue)return true;
            if(Busy||Closed||!ReferenceEquals(researchQueuedController,Current)||!ReferenceEquals(researchQueuedFrame,Current.Frame)||Current.RefreshRequired)
            {RejectResearchCommit("The campaign changed or is busy. Accepted allocations restored; refresh before trying again.");return false;}
            var science=researchQueuedScience;var split=researchQueuedSplit;
            ClearResearchQueue();researchFlushing=true;
            try
            {
                if(science!=null&&ResearchDraft!.ScienceChanged&&!AllocateScience(science,NewCommandId()))
                {RejectResearchCommit(Message+" Accepted Science and R&D allocations restored. Refresh before trying again.");return false;}
                if(split.HasValue&&split.Value!=Current.Frame.Strategy!.Research.ScienceAllocationUnits&&!AllocateResearch(split.Value,NewCommandId()))
                {RejectResearchCommit(Message+" Accepted Science and R&D allocations restored. Refresh before trying again.");return false;}
                researchNoticeController=Current;researchNotice="Allocation accepted.";return true;
            }
            finally {researchFlushing=false;}
        }
        private void ClearResearchQueue()
        {researchQueuedScience=null;researchQueuedSplit=null;researchQueuedFrame=null;researchQueuedController=null;researchNotice="";}
        public void CancelResearchAllocations(string reason="Allocation input cancelled when the view closed. Accepted allocations restored.")
        {if(ResearchCommitPending)RejectResearchCommit(reason);}
        private void RejectResearchCommit(string reason)
        {
            ClearResearchQueue();
            if(Current.Frame.Strategy!=null)ResearchDraft=ResearchDraft?.RestoreAccepted(Current.Frame)??new ReviewResearchDraft(Current.Frame);
            researchNoticeController=Current;researchNotice=reason;
        }
    }
}
