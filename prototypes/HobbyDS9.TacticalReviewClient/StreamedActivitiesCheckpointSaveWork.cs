using System;
using System.Threading;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public sealed class StreamedActivitiesCheckpointSaveWork
    {
        private readonly Func<Func<bool>,ReviewCheckpointResult> execute;
        private int started;
        private ReviewCheckpointResult? result;
        public ReviewFrame Source { get; }
        internal StreamedActivitiesCheckpointSaveWork(ReviewFrame source,Func<Func<bool>,ReviewCheckpointResult> execute){Source=source;this.execute=execute;}
        public ReviewCheckpointResult Execute(Func<bool> cancelled)
        {if(cancelled==null)throw new ArgumentNullException(nameof(cancelled));if(Interlocked.Exchange(ref started,1)!=0)throw new InvalidOperationException("Checkpoint work is one-use.");
         var done=execute(cancelled);Volatile.Write(ref result,done);return done;}
        internal ReviewCheckpointResult? Result=>Volatile.Read(ref result);
    }
    public sealed partial class PhysicalReviewHost
    {
        public bool IsCurrentStreamedActivitiesCheckpointSource(ReviewFrame observed)
        {lock(gate){if(streamedInteraction!=null||colonisationInteraction!=null||streamedActivitiesPackage==null||observed.HostId!=id||observed.Generation!=generation)return false;
            var status=session.CaptureStatus();return status.Turn==observed.Status.Turn&&status.CampaignRevision==observed.Status.CampaignRevision;}}
        public StreamedActivitiesCheckpointSaveWork PrepareStreamedActivitiesCheckpointSave(ReviewFrame observed,string slot,bool overwrite)
        {
            lock(gate)
            {
                if(!IsCurrentStreamedActivitiesCheckpointSource(observed)||streamedActivitiesCodec is not IBackgroundStreamedActivitiesCheckpointCodec
                    ||streamedActivitiesStore is not IBackgroundStreamedActivitiesCheckpointStore)throw new InvalidOperationException("Background streamedActivities checkpoint unavailable.");
                var root=((ICampaignStreamedActivitiesSession)session).ExportStreamedActivitiesSave();
                var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());var package=streamedActivitiesPackage!;var workerCodec=streamedActivitiesCodec;var workerStore=streamedActivitiesStore;
                return new StreamedActivitiesCheckpointSaveWork(observed,cancelled=>{
                    if(cancelled()||!IsCurrentStreamedActivitiesCheckpointSource(observed))return new ReviewCheckpointResult(ReviewCheckpointError.Busy);
                    CampaignStreamedActivitiesSaveDto copy;
                    try{copy=workerCodec.CopySave(root);CampaignStreamedActivitiesApplication.LoadSave(package,copy);}
                    catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
                    if(cancelled()||!IsCurrentStreamedActivitiesCheckpointSource(observed))return new ReviewCheckpointResult(ReviewCheckpointError.Busy);
                    try{workerStore.Write(slot,copy,overwrite);return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);}
                    catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
                    catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
                });
            }
        }
    }
    public sealed partial class ReviewController
    {
        public bool ReceiveStreamedActivitiesCheckpointSave(StreamedActivitiesCheckpointSaveWork work)
        {if(work==null||!ReferenceEquals(Frame,work.Source)||pending!=null||work.Result==null)return false;LastCheckpointError=work.Result.Error;return true;}
    }
}
