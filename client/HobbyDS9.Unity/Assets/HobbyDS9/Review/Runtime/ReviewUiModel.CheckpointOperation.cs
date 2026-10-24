#nullable enable
using System;
using System.Threading;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    // Begin/Complete/Abandon are UI-thread owned; Execute is the only worker entry.
    // No mutable model, controller or Unity object is accessed by Execute.
    public sealed class ReviewCheckpointOperation
    {
        private readonly Func<Func<bool>,ReviewCheckpointCompletion> execute;
        private int started,cancelled;
        public bool Loading { get; }
        public string Slot { get; }
        internal ReviewController Source { get; }
        internal ReviewFrame Frame { get; }
        internal ReviewCheckpointOperation(ReviewController source,string slot,bool loading,Func<Func<bool>,ReviewCheckpointCompletion> execute)
        {Source=source;Frame=source.Frame;Slot=slot;Loading=loading;this.execute=execute;}
        internal void Cancel()=>Interlocked.Exchange(ref cancelled,1);
        public ReviewCheckpointCompletion Execute()
        {
            if(Interlocked.Exchange(ref started,1)!=0)throw new InvalidOperationException("Checkpoint operation is one-use.");
            return execute(()=>Volatile.Read(ref cancelled)!=0);
        }
    }
    public sealed class ReviewCheckpointCompletion
    {
        internal ReviewCheckpointOperation? Operation;
        internal ReviewCheckpointError Error { get; }
        internal PhysicalReviewHost? Host { get; }
        internal ReviewController? Controller { get; }
        internal StaticCheckpointSaveWork? SaveWork { get; }
        internal ReviewCheckpointCompletion(ReviewCheckpointError error,PhysicalReviewHost? host=null,ReviewController? controller=null,StaticCheckpointSaveWork? saveWork=null)
        {Error=error;Host=host;Controller=controller;SaveWork=saveWork;}
    }
    public sealed partial class ReviewUiModel
    {
        private ReviewCheckpointOperation? checkpointOperation;
        public ReviewCheckpointOperation? BeginStaticCheckpointOperation(string slot,bool loading,bool overwrite=false,bool discard=false)
        {
            if(!StaticAstronomyCampaign||Busy||Closed||Current.HasPendingWork)return null;
            if(loading&&HasLocalEdits&&!discard){Message="Explicit discard required before load.";return null;}
            var source=Current;var sourceHost=host;var frame=source.Frame;
            Busy=true;
            try
            {
                var save=loading?null:sourceHost.PrepareStaticCheckpointSave(frame,slot,overwrite);
                // These are private immutable/owned adapter inputs, not live UI state.
                var template=generatedCampaignTemplate!;var workerCodec=codec;var store=staticAstronomySlots!;
                ReviewCheckpointOperation? operation=null;
                operation=new ReviewCheckpointOperation(source,slot,loading,cancelled=>{
                    ReviewCheckpointCompletion result;
                    try
                    {
                        if(cancelled())result=new ReviewCheckpointCompletion(ReviewCheckpointError.Busy);
                        else if(!loading)
                        {
                            var saved=save!.Execute(cancelled);
                            result=new ReviewCheckpointCompletion(saved.Error,saveWork:save);
                        }
                        else
                        {
                            var opened=PhysicalReviewHost.OpenStaticAstronomyCampaignFromSlot(template,workerCodec,store,slot,frame.Actor,frame.Battle?.EncounterId);
                            if(!opened.Succeeded)result=new ReviewCheckpointCompletion(opened.Error);
                            else
                            {
                                var next=Controller(opened.Host!,frame.Actor);
                                opened.Host!.CaptureGeneratedPresentation(next.Frame);
                                result=new ReviewCheckpointCompletion(ReviewCheckpointError.None,opened.Host,next);
                            }
                        }
                    }
                    catch(Exception){result=new ReviewCheckpointCompletion(loading?ReviewCheckpointError.InvalidCheckpoint:ReviewCheckpointError.SerializationFailed);}
                    result.Operation=operation;return result;
                });
                checkpointOperation=operation;Message=loading?"Loading and validating campaign…":"Saving and validating campaign…";return operation;
            }
            catch(Exception){Busy=false;Message="Checkpoint result: SerializationFailed";return null;}
        }
        public bool CompleteStaticCheckpointOperation(ReviewCheckpointOperation operation,ReviewCheckpointCompletion completion)
        {
            if(!ReferenceEquals(checkpointOperation,operation)||!ReferenceEquals(completion.Operation,operation))return false;
            checkpointOperation=null;Busy=false;
            if(Closed||!ReferenceEquals(Current,operation.Source)||!ReferenceEquals(Current.Frame,operation.Frame)
                ||!host.IsCurrentStaticCheckpointSource(operation.Frame))return false;
            if(!operation.Loading)
            {
                if(completion.SaveWork!=null&&!Current.ReceiveStaticCheckpointSave(completion.SaveWork))return false;
                Message="Checkpoint result: "+completion.Error;return completion.Error==ReviewCheckpointError.None;
            }
            if(completion.Error!=ReviewCheckpointError.None){Message="Checkpoint result: "+completion.Error;return false;}
            // Candidate frame, controller and presentation have ALL been built before publication.
            host=completion.Host!;Current=completion.Controller!;GeneratedViewportNeedsFit=true;
            ResearchDraft=null;LocalDesignDirty=false;ResetLocalDesign=null;ResetOrganization();
            Message="Validated complete-root checkpoint loaded.";return true;
        }
        public void AbandonStaticCheckpointOperation(ReviewCheckpointOperation operation)
        {
            operation.Cancel();
            if(!ReferenceEquals(checkpointOperation,operation))return;
            checkpointOperation=null;Busy=false;
        }
    }
}
