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
        private sealed class ConstructionFeedback
        {
            internal readonly WeakReference<ReviewController> Controller;
            internal readonly WeakReference<ReviewFrame> Frame;
            internal readonly string Colony,OperationId,Text;
            internal ConstructionFeedback(ReviewController controller,CampaignActivityCommandDto command,string text)
            {Controller=new WeakReference<ReviewController>(controller);Frame=new WeakReference<ReviewFrame>(controller.Frame);Colony=command.TargetId;OperationId=command.CommandId;Text=text;}
        }
        private ConstructionFeedback? constructionFeedback;
        private static bool IsConstruction(StreamedInteractionKind kind,CampaignActivityCommandDto? command)
            =>kind==StreamedInteractionKind.Submission&&(command?.Kind==CampaignActivityCommandKind.EnqueueShip||command?.Kind==CampaignActivityCommandKind.EnqueueStructure);
        private void ConstructionFeedbackFor(CampaignActivityCommandDto command,string text)
            =>constructionFeedback=new ConstructionFeedback(Current,command,text);
        private void ConstructionFeedbackFor(StreamedInteractionWork work,CampaignActivityCommandDto command,string text)
        {if(ReferenceEquals(streamedWork,work)&&ReferenceEquals(Current,streamedWorkController)&&!Closed)ConstructionFeedbackFor(command,text);}
        // Weak identity binding cannot retain an old session or detached frame.
        public string? ConstructionStatusFor(ReviewController controller,ReviewFrame frame,string colony)
        {
            // A stale panel cannot clear a newer notice by querying it.
            if(!ReferenceEquals(controller,Current)||!ReferenceEquals(frame,Current.Frame))return null;
            if(constructionFeedback!=null&&(!constructionFeedback.Controller.TryGetTarget(out var owner)||!ReferenceEquals(owner,Current)
                ||!constructionFeedback.Frame.TryGetTarget(out var observed)||!ReferenceEquals(observed,Current.Frame)))constructionFeedback=null;
            return !Busy&&!Closed&&constructionFeedback?.Colony==colony?constructionFeedback.Text:null;
        }
        // Owning-thread admission only; no map copy, serialization or full replay.
        public StreamedInteractionWork? BeginStreamedInteraction(StreamedInteractionKind kind,CampaignActivityCommandDto? command=null)
        {
            if(!StreamedActivitiesCampaign||Busy||Closed||Current.HasPendingWork||Current.RefreshRequired
                ||kind!=StreamedInteractionKind.Preview&&Current.Dirty||kind==StreamedInteractionKind.PlanningAdvance&&HasLocalEdits)
            {
                if(StreamedActivitiesCampaign&&!Busy&&!Closed&&IsConstruction(kind,command))
                    ConstructionFeedbackFor(command!,Current.RefreshRequired?"The campaign changed. Refresh before adding construction.":"Finish or discard the pending order before adding construction.");
                return null;
            }
            try
            {
                var work=host.PrepareStreamedInteraction(Current.Frame,kind,command);
                constructionFeedback=null;streamedWork=work;streamedWorkController=Current;Busy=true;return work;
            }
            catch(InvalidOperationException)
            {Message="Activity observation changed.";if(!Busy&&!Closed&&IsConstruction(kind,command))ConstructionFeedbackFor(command!,"The campaign changed. Refresh before adding construction.");return null;}
        }
        public bool CompleteStreamedInteraction(StreamedInteractionWork work,StreamedInteractionCompletion completion)
        {
            if(!ReferenceEquals(streamedWork,work))return false;
            if(!ReferenceEquals(Current,streamedWorkController)||!ReferenceEquals(Current.Frame,work.SourceFrame)||Closed){AbandonStreamedInteraction(work);return false;}
            var command=work.Command;
            try
            {
                if(Current.RefreshRequired){Message="Activity observation changed.";if(IsConstruction(work.Kind,command))ConstructionFeedbackFor(work,command!,"The campaign changed. Refresh before adding construction.");return false;}
                StreamedControllerUpdate? update=null;var research=ResearchDraft;
                if(completion.Result?.Accepted==true)
                {
                    update=Current.PrepareStreamedInteractionReceive(completion);
                    // Preserve current unsubmitted allocation values; source frame
                    // remains untouched until every reconciliation is ready.
                    if(research!=null)research=research.PreserveUnsubmittedEdits(completion.PreparedFrame!);
                }
                if(!host.CompleteStreamedInteraction(work,completion,update))
                {Message="Interaction cancelled or unavailable.";if(IsConstruction(work.Kind,command))ConstructionFeedbackFor(work,command!,"The construction request was not applied. No order was added.");return false;}
                if(completion.Committed)ResearchDraft=research;
                Message=completion.Failed?"Interaction unavailable.":work.Kind==StreamedInteractionKind.Preview?"Order checked at the observed campaign state.":completion.Result?.Accepted==true?"Activity order accepted.":"Activity order unavailable: "+completion.Result?.Error;
                if(IsConstruction(work.Kind,command))
                {
                    if(completion.Result?.Accepted==false)Message=ConstructionRejection(completion.Result.Error);
                    ConstructionFeedbackFor(work,command!,completion.Committed?"Construction order added.":Message);
                }
                return true;
            }
            catch(Exception)
            {Message="Interaction unavailable; campaign publication did not complete.";if(IsConstruction(work.Kind,command))ConstructionFeedbackFor(work,command!,"The construction request could not be completed. Refresh before trying again.");return false;}
            finally{AbandonStreamedInteraction(work);}
        }
        private static string ConstructionRejection(CampaignTacticalError error)
        {
            switch(error)
            {
                case CampaignTacticalError.StaleTurn:case CampaignTacticalError.StaleRevision:case CampaignTacticalError.StaleBoundary:
                    return "The campaign changed. Choose the construction order again.";
                case CampaignTacticalError.UnsupportedSource:return "That class or construction site is not supported here.";
                case CampaignTacticalError.WrongPhase:return "Construction orders are available during Planning.";
                case CampaignTacticalError.Overflow:return "The campaign has no safe room for another construction order.";
                case CampaignTacticalError.DuplicateCommand:return "This construction order was already submitted.";
                default:return "This construction order is no longer available at the selected world.";
            }
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
