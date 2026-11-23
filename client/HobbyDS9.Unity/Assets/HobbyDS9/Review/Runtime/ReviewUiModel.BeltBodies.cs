#nullable enable
using System;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private BeltBodyInteractionWork? beltWork;
        private ReviewController? beltWorkController;
        public bool SupportsBeltBodies=>host.SupportsBeltBodies;

        public BeltBodiesSnapshot? CaptureBeltBodies(string actor)
        {
            if(!SupportsBeltBodies||Busy||Closed||Current.RefreshRequired||Current.HasPendingWork
                ||!string.Equals(actor,Current.Frame.Actor,StringComparison.Ordinal))return null;
            try{return host.CaptureBeltBodies(Current.Frame);}
            catch(InvalidOperationException){return null;}
        }
        public BeltBodyInteractionWork? BeginBeltBodyInteraction(BeltBodyInteractionKind kind,BeltBodyCommandDto command)
        {
            if(!SupportsBeltBodies||Busy||Closed||Current.HasPendingWork||Current.RefreshRequired
                ||kind!=BeltBodyInteractionKind.Preview&&Current.Dirty)return null;
            try
            {
                var work=host.PrepareBeltBodyInteraction(Current.Frame,kind,command);
                beltWork=work;beltWorkController=Current;Busy=true;return work;
            }
            catch(InvalidOperationException)
            {Message="The body observation changed or this action is unavailable.";return null;}
        }
        public bool CompleteBeltBodyInteraction(BeltBodyInteractionWork work,BeltBodyInteractionCompletion completion)
        {
            if(!ReferenceEquals(beltWork,work))return false;
            if(!ReferenceEquals(Current,beltWorkController)||!ReferenceEquals(Current.Frame,work.SourceFrame)||Closed)
            {AbandonBeltBodyInteraction(work);return false;}
            try
            {
                if(Current.RefreshRequired){Message="The body observation changed. Review the action again.";return false;}
                BeltBodyControllerUpdate? update=null;var research=ResearchDraft;
                if(completion.Result?.Accepted==true)
                {
                    update=Current.PrepareBeltBodyInteractionReceive(completion);
                    if(research!=null)research=research.PreserveUnsubmittedEdits(completion.PreparedFrame!);
                }
                if(!host.CompleteBeltBodyInteraction(work,completion,update))
                {Message="Body interaction cancelled or unavailable.";return false;}
                if(completion.Committed)ResearchDraft=research;
                Message=work.Kind==BeltBodyInteractionKind.Preview?"Body action checked at the observed state."
                    :completion.Result?.Accepted==true?"Body action accepted.":"Body action unavailable: "+completion.Result?.Error;
                return true;
            }
            catch(Exception){Message="Body interaction unavailable; campaign publication did not complete.";return false;}
            finally{AbandonBeltBodyInteraction(work);}
        }
        public void AbandonBeltBodyInteraction(BeltBodyInteractionWork work)
        {
            host.AbandonBeltBodyInteraction(work);
            if(!ReferenceEquals(beltWork,work)||!work.Settled)return;
            beltWork=null;beltWorkController=null;Busy=false;
        }
    }
}
