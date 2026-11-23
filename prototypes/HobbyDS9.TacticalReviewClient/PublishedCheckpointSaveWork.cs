using System;
using System.Threading;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public sealed class PublishedCheckpointSaveWork
    {
        private readonly Func<Func<bool>,ReviewCheckpointResult> execute;
        private int started;
        private ReviewCheckpointResult? result;
        public ReviewFrame Source { get; }
        internal PublishedCheckpointSaveWork(ReviewFrame source,Func<Func<bool>,ReviewCheckpointResult> execute){Source=source;this.execute=execute;}
        public ReviewCheckpointResult Execute(Func<bool> cancelled)
        {if(cancelled==null)throw new ArgumentNullException(nameof(cancelled));if(Interlocked.Exchange(ref started,1)!=0)throw new InvalidOperationException("Checkpoint work is one-use.");
         var done=execute(cancelled);Volatile.Write(ref result,done);return done;}
        internal ReviewCheckpointResult? Result=>Volatile.Read(ref result);
    }
    public sealed partial class PhysicalReviewHost
    {
        public bool IsCurrentPublishedCheckpointSource(ReviewFrame observed)
        {lock(gate){if(publishedPackage==null||observed.HostId!=id||observed.Generation!=generation)return false;
            var status=session.CaptureStatus();return status.Turn==observed.Status.Turn&&status.CampaignRevision==observed.Status.CampaignRevision;}}
        public PublishedCheckpointSaveWork PreparePublishedCheckpointSave(ReviewFrame observed,string slot,bool overwrite)
        {
            lock(gate)
            {
                if(!IsCurrentPublishedCheckpointSource(observed)||publishedCodec is not IBackgroundPublishedCheckpointCodec
                    ||publishedStore is not IBackgroundPublishedCheckpointStore)throw new InvalidOperationException("Background published checkpoint unavailable.");
                var root=((ICampaignPublishedConstructionSession)session).ExportPublishedConstructionSave();
                var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());var package=publishedPackage!;var workerCodec=publishedCodec;var workerStore=publishedStore;
                return new PublishedCheckpointSaveWork(observed,cancelled=>{
                    if(cancelled()||!IsCurrentPublishedCheckpointSource(observed))return new ReviewCheckpointResult(ReviewCheckpointError.Busy);
                    CampaignPublishedConstructionSaveDto copy;
                    try{copy=workerCodec.CopySave(root);CampaignPublishedConstructionApplication.LoadSave(package,copy);}
                    catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
                    if(cancelled()||!IsCurrentPublishedCheckpointSource(observed))return new ReviewCheckpointResult(ReviewCheckpointError.Busy);
                    try{workerStore.Write(slot,copy,overwrite);return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);}
                    catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
                    catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
                });
            }
        }
    }
    public sealed partial class ReviewController
    {
        public bool ReceivePublishedCheckpointSave(PublishedCheckpointSaveWork work)
        {if(work==null||!ReferenceEquals(Frame,work.Source)||pending!=null||work.Result==null)return false;LastCheckpointError=work.Result.Error;return true;}
    }
}
