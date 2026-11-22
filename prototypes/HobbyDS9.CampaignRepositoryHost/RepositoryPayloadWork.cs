using System;
using System.Threading;
using HobbyDS9.Contracts;
using HobbyDS9.Persistence;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.CampaignRepositoryHost
{
    public sealed class RepositoryPayloadSummary
    {
        public string EditionIdentity {get;}
        public int Turn {get;}
        public long Revision {get;}
        public SessionPhase Phase {get;}
        public string PayloadHash {get;}
        public int PayloadLength {get;}
        internal RepositoryPayloadSummary(CampaignRepositoryImage image)
        {EditionIdentity=image.Edition.Identity;Turn=image.Turn;Revision=image.Revision;Phase=image.Phase;PayloadHash=image.PayloadHash;PayloadLength=image.Length;}
    }
    public sealed class RepositoryLoadedCheckpoint
    {
        public string CheckpointId {get;}
        public CampaignRepositoryStamp Source {get;}
        public RepositoryPayloadSummary Summary {get;}
        internal RepositoryLoadedCheckpoint(string checkpoint,CampaignRepositoryStamp source,RepositoryPayloadSummary summary)
        {CheckpointId=checkpoint;Source=source;Summary=summary;}
    }
    public sealed class RepositoryPayloadResult
    {
        internal CampaignRepositoryRestoreResult? Restore;
        public CampaignRepositoryError Error {get;}
        public RepositoryError? StorageError {get;}
        public RepositoryPayloadSummary? Summary {get;}
        public RepositoryLoadedCheckpoint? Loaded {get;internal set;}
        internal RepositoryPayloadResult(CampaignRepositoryRestoreResult restore,RepositoryError? storageError)
        {Restore=restore;Error=restore.Error;StorageError=storageError;Summary=restore.Image==null?null:new RepositoryPayloadSummary(restore.Image);}
        internal void Release(){Restore?.Abandon();Restore=null;}
    }
    public sealed class RepositoryPayloadWork
    {
        internal readonly CampaignRepositoryRestoreWork Restore;
        internal readonly object AdoptionGate=new object();
        private readonly Func<RepositoryError?> storageError;
        private int started,cancelled;
        private RepositoryPayloadResult? result;
        public Guid Token {get;}=Guid.NewGuid();
        public string CheckpointId {get;}
        public CampaignRepositoryStamp Source=>Restore.Source;
        internal RepositoryPayloadWork(string checkpoint,CampaignRepositoryRestoreWork restore,Func<RepositoryError?> storageError)
        {CheckpointId=checkpoint;Restore=restore;this.storageError=storageError;}
        public bool CancellationRequested=>Volatile.Read(ref cancelled)!=0;
        public void Cancel(){lock(AdoptionGate)Interlocked.Exchange(ref cancelled,1);}
        public RepositoryPayloadResult Execute()
        {
            if(Interlocked.Exchange(ref started,1)!=0)throw new InvalidOperationException("Payload worker is one-use.");
            var done=new RepositoryPayloadResult(Restore.Execute(()=>Volatile.Read(ref cancelled)!=0),storageError());
            Volatile.Write(ref result,done);return done;
        }
        internal RepositoryPayloadResult? Result=>Volatile.Read(ref result);
    }
    public sealed class RepositoryPayloadAdmission
    {
        public SaveRequestStatus Status {get;}
        public RepositoryPayloadWork? Work {get;}
        internal RepositoryPayloadAdmission(SaveRequestStatus status,RepositoryPayloadWork? work=null){Status=status;Work=work;}
    }
}
