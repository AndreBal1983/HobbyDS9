#nullable enable
using System;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private ColonisationInteractionWork? colonisationWork;
        private ReviewController? colonisationWorkController;
        private sealed class ColonisationFeedback
        {
            internal readonly WeakReference<ReviewController> Controller;
            internal readonly WeakReference<ReviewFrame> Frame;
            internal readonly string Colony,OperationId,Text;
            internal ColonisationFeedback(ReviewController controller,CampaignActivityCommandDto command,string text)
            {Controller=new WeakReference<ReviewController>(controller);Frame=new WeakReference<ReviewFrame>(controller.Frame);Colony=command.TargetId;OperationId=command.CommandId;Text=text;}
        }
        private ColonisationFeedback? colonisationFeedback;
        private WeakReference<ReviewController>? colonisationStatusController;
        private WeakReference<ReviewFrame>? colonisationStatusFrame;
        private string? colonisationStatus;
        public string? ColonisationActionStatus()
        {
            if(colonisationStatusController==null||!colonisationStatusController.TryGetTarget(out var owner)||!ReferenceEquals(owner,Current)
                ||colonisationStatusFrame==null||!colonisationStatusFrame.TryGetTarget(out var frame)||!ReferenceEquals(frame,Current.Frame))
            {colonisationStatus=null;colonisationStatusController=null;colonisationStatusFrame=null;return null;}
            return !Busy&&!Closed?colonisationStatus:null;
        }
        private void SetColonisationStatus(string text)
        {colonisationStatus=text;colonisationStatusController=new WeakReference<ReviewController>(Current);colonisationStatusFrame=new WeakReference<ReviewFrame>(Current.Frame);}
        private static bool IsColonisationConstruction(ColonisationInteractionKind kind,CampaignActivityCommandDto? command)
            =>kind==ColonisationInteractionKind.Submission&&(command?.Kind==CampaignActivityCommandKind.EnqueueShip||command?.Kind==CampaignActivityCommandKind.EnqueueStructure);
        private void ColonisationFeedbackFor(CampaignActivityCommandDto command,string text)
            =>colonisationFeedback=new ColonisationFeedback(Current,command,text);
        private void ColonisationFeedbackFor(ColonisationInteractionWork work,CampaignActivityCommandDto command,string text)
        {if(ReferenceEquals(colonisationWork,work)&&ReferenceEquals(Current,colonisationWorkController)&&!Closed)ColonisationFeedbackFor(command,text);}
        // Weak identity binding cannot retain an old session or detached frame.
        public string? ColonisationStatusFor(ReviewController controller,ReviewFrame frame,string colony)
        {
            // A stale panel cannot clear a newer notice by querying it.
            if(!ReferenceEquals(controller,Current)||!ReferenceEquals(frame,Current.Frame))return null;
            if(colonisationFeedback!=null&&(!colonisationFeedback.Controller.TryGetTarget(out var owner)||!ReferenceEquals(owner,Current)
                ||!colonisationFeedback.Frame.TryGetTarget(out var observed)||!ReferenceEquals(observed,Current.Frame)))colonisationFeedback=null;
            return !Busy&&!Closed&&colonisationFeedback?.Colony==colony?colonisationFeedback.Text:null;
        }
        // Owning-thread admission only; no map copy, serialization or full replay.
        public ColonisationInteractionWork? BeginColonisationInteraction(ColonisationInteractionKind kind,CampaignActivityCommandDto? command=null,ColonisationCommandDto? colonyCommand=null)
        {
            if(!ColonisationCampaign||Busy||Closed||Current.HasPendingWork||Current.RefreshRequired
                ||kind!=ColonisationInteractionKind.Preview&&Current.Dirty||kind==ColonisationInteractionKind.PlanningAdvance&&HasLocalEdits)
            {
                if(ColonisationCampaign&&!Busy&&!Closed&&IsColonisationConstruction(kind,command))
                    ColonisationFeedbackFor(command!,Current.RefreshRequired?"The campaign changed. Refresh before adding construction.":"Finish or discard the pending order before adding construction.");
                return null;
            }
            try
            {
                var work=host.PrepareColonisationInteraction(Current.Frame,kind,command,colonyCommand);
                colonisationFeedback=null;colonisationStatus=null;colonisationWork=work;colonisationWorkController=Current;Busy=true;return work;
            }
            catch(InvalidOperationException)
            {Message="Activity observation changed.";if(!Busy&&!Closed)SetColonisationStatus("The campaign changed. Refresh before submitting the order.");if(!Busy&&!Closed&&IsColonisationConstruction(kind,command))ColonisationFeedbackFor(command!,"The campaign changed. Refresh before adding construction.");return null;}
        }
        public bool CompleteColonisationInteraction(ColonisationInteractionWork work,ColonisationInteractionCompletion completion)
        {
            if(!ReferenceEquals(colonisationWork,work))return false;
            if(!ReferenceEquals(Current,colonisationWorkController)||!ReferenceEquals(Current.Frame,work.SourceFrame)||Closed){AbandonColonisationInteraction(work);return false;}
            var command=work.Command;
            try
            {
                if(Current.RefreshRequired){Message="Activity observation changed.";if(IsColonisationConstruction(work.Kind,command))ColonisationFeedbackFor(work,command!,"The campaign changed. Refresh before adding construction.");return false;}
                ColonisationControllerUpdate? update=null;var research=ResearchDraft;
                if(completion.Result?.Accepted==true)
                {
                    update=Current.PrepareColonisationInteractionReceive(completion);
                    // Preserve current unsubmitted allocation values; source frame
                    // remains untouched until every reconciliation is ready.
                    if(research!=null)research=research.PreserveUnsubmittedEdits(completion.PreparedFrame!);
                }
                if(!host.CompleteColonisationInteraction(work,completion,update))
                {Message="Interaction cancelled or unavailable.";if(IsColonisationConstruction(work.Kind,command))ColonisationFeedbackFor(work,command!,"The construction request was not applied. No order was added.");return false;}
                if(completion.Committed){ResearchDraft=research;colonisationObservation=completion.PreparedObservation;colonisationPlanets=completion.PreparedPlanets;colonisationObservationFrame=Current.Frame;}
                Message=completion.Failed?"Interaction unavailable.":work.Kind==ColonisationInteractionKind.Preview?"Order checked at the observed campaign state.":completion.Result?.Accepted==true?"Activity order accepted.":"Activity order unavailable: "+completion.Result?.Error;
                if(IsColonisationConstruction(work.Kind,command))
                {
                    if(completion.Result?.Accepted==false)Message=ColonisationConstructionRejection(completion.Result.Error);
                    ColonisationFeedbackFor(work,command!,completion.Committed?"Construction order added.":Message);
                }
                if(work.Kind==ColonisationInteractionKind.Submission&&completion.Result?.Accepted==false&&!IsColonisationConstruction(work.Kind,command))Message=ColonisationEligibilitySnapshot.Describe(completion.Result.Error);
                SetColonisationStatus(Message);return true;
            }
            catch(Exception)
            {Message="Interaction unavailable; campaign publication did not complete.";if(IsColonisationConstruction(work.Kind,command))ColonisationFeedbackFor(work,command!,"The construction request could not be completed. Refresh before trying again.");return false;}
            finally{AbandonColonisationInteraction(work);}
        }
        private static string ColonisationConstructionRejection(CampaignTacticalError error)
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
        public void AbandonColonisationInteraction(ColonisationInteractionWork work)
        {
            host.AbandonColonisationInteraction(work);
            if(!ReferenceEquals(colonisationWork,work))return;
            // A cancelled clone may still own the source read lock. Keep Busy
            // until it has drained, so a re-enabled UI never waits on that lock.
            if(!work.Settled)return;
            colonisationWork=null;colonisationWorkController=null;Busy=false;
        }
    }
}
