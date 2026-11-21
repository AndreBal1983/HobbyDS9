#nullable enable
using System;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private EngineeringRefinementInteractionWork? refinementWork;
        private ReviewController? refinementWorkController;
        public bool SupportsEngineeringRefinement=>host.SupportsEngineeringRefinement;

        public EngineeringRefinementSnapshot? CaptureEngineeringRefinement(string actor)
        {
            if(!SupportsEngineeringRefinement||Busy||Closed||Current.RefreshRequired||Current.HasPendingWork
                ||!string.Equals(actor,Current.Frame.Actor,StringComparison.Ordinal))return null;
            try{return host.CaptureEngineeringRefinement(Current.Frame);}
            catch(InvalidOperationException){return null;}
        }

        // Admission uses the command exactly as reviewed. The host validates
        // capability and stamps; this wrapper never refreshes a stale command.
        public EngineeringRefinementInteractionWork? BeginEngineeringRefinementInteraction(
            EngineeringRefinementInteractionKind kind,EngineeringRefinementCommandDto command)
        {
            if(!SupportsEngineeringRefinement||Busy||Closed||Current.HasPendingWork||Current.RefreshRequired
                ||kind!=EngineeringRefinementInteractionKind.Preview&&Current.Dirty)return null;
            try
            {
                var work=host.PrepareEngineeringRefinementInteraction(Current.Frame,kind,command);
                refinementWork=work;refinementWorkController=Current;Busy=true;return work;
            }
            catch(InvalidOperationException)
            {Message="Engineering observation changed or refinement is unavailable.";return null;}
        }

        public bool CompleteEngineeringRefinementInteraction(EngineeringRefinementInteractionWork work,
            EngineeringRefinementInteractionCompletion completion)
        {
            if(!ReferenceEquals(refinementWork,work))return false;
            if(!ReferenceEquals(Current,refinementWorkController)||!ReferenceEquals(Current.Frame,work.SourceFrame)||Closed)
            {AbandonEngineeringRefinementInteraction(work);return false;}
            try
            {
                if(Current.RefreshRequired){Message="Engineering observation changed.";return false;}
                EngineeringRefinementControllerUpdate? update=null;var research=ResearchDraft;
                if(completion.Result?.Accepted==true)
                {
                    update=Current.PrepareEngineeringRefinementInteractionReceive(completion);
                    // Reconcile drafts before the host atomically publishes its
                    // candidate. A failed publication leaves the source intact.
                    if(research!=null)research=research.PreserveUnsubmittedEdits(completion.PreparedFrame!);
                }
                if(!host.CompleteEngineeringRefinementInteraction(work,completion,update))
                {Message="Engineering interaction cancelled or unavailable.";return false;}
                if(completion.Committed)ResearchDraft=research;
                Message=work.Kind==EngineeringRefinementInteractionKind.Preview
                    ?"Engineering goal checked at the observed campaign state."
                    :completion.Result?.Accepted==true?"Engineering decision accepted.":"Engineering decision unavailable: "+completion.Result?.Error;
                return true;
            }
            catch(Exception)
            {Message="Engineering interaction unavailable; campaign publication did not complete.";return false;}
            finally{AbandonEngineeringRefinementInteraction(work);}
        }

        public void AbandonEngineeringRefinementInteraction(EngineeringRefinementInteractionWork work)
        {
            host.AbandonEngineeringRefinementInteraction(work);
            if(!ReferenceEquals(refinementWork,work)||!work.Settled)return;
            // A cancelled worker may still hold a source read lease. Busy stays
            // set until that worker drains, including while the view is disabled.
            refinementWork=null;refinementWorkController=null;Busy=false;
        }
    }
}
