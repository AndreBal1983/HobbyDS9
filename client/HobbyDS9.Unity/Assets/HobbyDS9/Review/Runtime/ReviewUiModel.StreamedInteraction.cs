#nullable enable
using System;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private StreamedInteractionWork? streamedWork;
        private ReviewController? streamedWorkController;
        // Owning-thread admission only; no map copy, serialization or full replay.
        public StreamedInteractionWork? BeginStreamedInteraction(StreamedInteractionKind kind,CampaignActivityCommandDto? command=null)
        {
            if(!StreamedActivitiesCampaign||Busy||Closed||Current.HasPendingWork||Current.RefreshRequired
                ||kind!=StreamedInteractionKind.Preview&&Current.Dirty||kind==StreamedInteractionKind.PlanningAdvance&&HasLocalEdits)return null;
            try
            {
                var work=host.PrepareStreamedInteraction(Current.Frame,kind,command);
                streamedWork=work;streamedWorkController=Current;Busy=true;return work;
            }
            catch(InvalidOperationException){Message="Activity observation changed.";return null;}
        }
        public bool CompleteStreamedInteraction(StreamedInteractionWork work,StreamedInteractionCompletion completion)
        {
            if(!ReferenceEquals(streamedWork,work))return false;
            if(!ReferenceEquals(Current,streamedWorkController)||!ReferenceEquals(Current.Frame,work.SourceFrame)||Closed){AbandonStreamedInteraction(work);return false;}
            try
            {
                if(Current.RefreshRequired){Message="Activity observation changed.";return false;}
                StreamedControllerUpdate? update=null;var research=ResearchDraft;
                if(completion.Result?.Accepted==true)
                {
                    update=Current.PrepareStreamedInteractionReceive(completion);
                    // Preserve current unsubmitted allocation values; source frame
                    // remains untouched until every reconciliation is ready.
                    if(research!=null)research=research.PreserveUnsubmittedEdits(completion.PreparedFrame!);
                }
                if(!host.CompleteStreamedInteraction(work,completion,update)){Message="Interaction cancelled or unavailable.";return false;}
                if(completion.Committed)ResearchDraft=research;
                Message=completion.Failed?"Interaction unavailable.":work.Kind==StreamedInteractionKind.Preview?"Order checked at the observed campaign state.":completion.Result?.Accepted==true?"Activity order accepted.":"Activity order unavailable: "+completion.Result?.Error;
                return true;
            }
            catch(Exception){Message="Interaction unavailable; campaign publication did not complete.";return false;}
            finally{AbandonStreamedInteraction(work);}
        }
        public void AbandonStreamedInteraction(StreamedInteractionWork work)
        {
            host.AbandonStreamedInteraction(work);
            if(!ReferenceEquals(streamedWork,work))return;
            // A cancelled clone may still own the source read lock. Keep Busy
            // until it has drained, so a re-enabled UI never waits on that lock.
            if(!work.Settled)return;
            streamedWork=null;streamedWorkController=null;Busy=false;
        }
    }
}
