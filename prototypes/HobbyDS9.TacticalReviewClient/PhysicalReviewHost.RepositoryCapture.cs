using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public sealed class CampaignRepositoryCaptureWork
    {
        private Func<Func<bool>,CampaignRepositoryImageResult>? execute;
        public CampaignRepositoryStamp Source {get;}
        public Guid WorkToken {get;}=Guid.NewGuid();
        internal CampaignRepositoryCaptureWork(CampaignRepositoryStamp source,Func<Func<bool>,CampaignRepositoryImageResult> execute)
        {Source=source;this.execute=execute;}
        public CampaignRepositoryImageResult Execute(Func<bool> cancelled)
        {
            if(cancelled==null)throw new ArgumentNullException(nameof(cancelled));
            var work=Interlocked.Exchange(ref execute,null)??throw new InvalidOperationException("Capture work is one-use.");
            return work(cancelled);
        }
    }
    public sealed class CampaignRepositoryCapturePreparation
    {
        public CampaignRepositoryError Error {get;}
        public CampaignRepositoryCaptureWork? Work {get;}
        internal CampaignRepositoryCapturePreparation(CampaignRepositoryError error,CampaignRepositoryCaptureWork? work=null){Error=error;Work=work;}
    }
    public sealed class CampaignRepositoryRestoreWork
    {
        private Func<Func<bool>,CampaignRepositoryRestoreResult>? execute;
        public CampaignRepositoryStamp Source {get;}
        public Guid WorkToken {get;}=Guid.NewGuid();
        internal CampaignRepositoryRestoreWork(CampaignRepositoryStamp source,Func<Func<bool>,CampaignRepositoryRestoreResult> execute)
        {Source=source;this.execute=execute;}
        public CampaignRepositoryRestoreResult Execute(Func<bool> cancelled)
        {
            if(cancelled==null)throw new ArgumentNullException(nameof(cancelled));
            return (Interlocked.Exchange(ref execute,null)??throw new InvalidOperationException("Restore work is one-use."))(cancelled);
        }
    }
    public sealed class CampaignRepositoryRestoreResult
    {
        internal ICampaignColonisationCycleSession? Candidate;
        internal CampaignRepositoryRestoreWork? Work;
        private int settled;
        public CampaignRepositoryError Error {get;}
        public CampaignRepositoryImage? Image {get;}
        public CampaignRepositoryStamp? PublishedStamp {get;internal set;}
        public bool Settled=>Volatile.Read(ref settled)!=0;
        internal CampaignRepositoryRestoreResult(CampaignRepositoryError error,CampaignRepositoryImage? image=null,ICampaignColonisationCycleSession? candidate=null)
        {Error=error;Image=image;Candidate=candidate;}
        internal bool Settle()=>Interlocked.Exchange(ref settled,1)==0;
        public void Abandon(){if(Settle())Candidate=null;}
    }
    public sealed class CampaignRepositoryRestorePreparation
    {
        public CampaignRepositoryError Error {get;}
        public CampaignRepositoryRestoreWork? Work {get;}
        internal CampaignRepositoryRestorePreparation(CampaignRepositoryError error,CampaignRepositoryRestoreWork? work=null){Error=error;Work=work;}
    }

    public sealed partial class PhysicalReviewHost
    {
        private bool RepositoryBusy=>streamedInteraction!=null||colonisationInteraction!=null||engineeringRefinementInteraction!=null||beltBodyInteraction!=null;
        private string RepositoryCombatBoundary(string actor)=>RepositoryCombatBoundary(session,actor);
        private static string RepositoryCombatBoundary(ICampaignTacticalTravelSession source,string actor)
            =>string.Join("|",source.GetEncounters(actor).OrderBy(x=>x,StringComparer.Ordinal).Select(x=>{
                var b=source.Observe(x,actor);
                return x.Length.ToString(CultureInfo.InvariantCulture)+":"+x+":"+b.Round.ToString(CultureInfo.InvariantCulture)+":"+b.BoundaryRevision.ToString(CultureInfo.InvariantCulture);
            }));
        private CampaignRepositoryStamp RepositoryStamp(ReviewFrame frame)
        {
            var s=session.CaptureStatus();
            return new CampaignRepositoryStamp(id,generation,s.Turn,s.CampaignRevision,s.Phase,frame.Actor,RepositoryCombatBoundary(frame.Actor));
        }
        private CampaignRepositoryError RepositoryFrameError(ReviewFrame frame)
        {
            if(colonisationCyclePackage==null)return CampaignRepositoryError.Unavailable;
            if(RepositoryBusy)return CampaignRepositoryError.Busy;
            var s=session.CaptureStatus();
            if(frame.HostId!=id||frame.Generation!=generation||frame.Status.Turn!=s.Turn||frame.Status.CampaignRevision!=s.CampaignRevision||frame.Status.Phase!=s.Phase)
                return CampaignRepositoryError.Stale;
            if(frame.Battle!=null)
            {
                if(!session.GetEncounters(frame.Actor).Contains(frame.Battle.EncounterId,StringComparer.Ordinal))return CampaignRepositoryError.Stale;
                var b=session.Observe(frame.Battle.EncounterId,frame.Actor);
                if(b.Round!=frame.Battle.Round||b.BoundaryRevision!=frame.Battle.BoundaryRevision)return CampaignRepositoryError.Stale;
            }
            return CampaignRepositoryError.None;
        }
        public bool IsRepositorySessionCurrent(CampaignRepositoryStamp source)
        {if(source==null)return false;lock(gate)return source.HostId==id&&source.SessionEpoch==generation;}
        public bool IsRepositoryStateCurrent(CampaignRepositoryStamp source)
        {
            if(source==null)return false;
            lock(gate)
            {
                if(RepositoryBusy||!IsRepositorySessionCurrent(source))return false;
                var s=session.CaptureStatus();
                return source.Turn==s.Turn&&source.Revision==s.CampaignRevision&&source.Phase==s.Phase&&source.CombatBoundary==RepositoryCombatBoundary(source.Actor);
            }
        }
        public CampaignRepositoryError CaptureRepositoryBoundary(ReviewFrame observed,out CampaignRepositoryStamp? stamp)
        {
            stamp=null;if(observed==null)throw new ArgumentNullException(nameof(observed));
            lock(gate)
            {
                var error=RepositoryFrameError(observed);if(error!=CampaignRepositoryError.None)return error;
                stamp=RepositoryStamp(observed);return CampaignRepositoryError.None;
            }
        }
        public CampaignRepositoryCapturePreparation PrepareRepositoryCheckpoint(ReviewFrame observed,ICampaignRepositoryCodec codec)
        {
            if(observed==null||codec==null)throw new ArgumentNullException();
            lock(gate)
            {
                var error=RepositoryFrameError(observed);if(error!=CampaignRepositoryError.None)return new CampaignRepositoryCapturePreparation(error);
                try
                {
                    // Full detached export under the existing session owner. Nothing
                    // in the worker queries a live host or reads actor projections.
                    var root=((ICampaignColonisationCycleSession)session).ExportCycleSave();
                    var adapter=new CycleCampaignRepositoryAdapter(colonisationCyclePackage!,colonisationCycleCodec!,codec);
                    var stamp=RepositoryStamp(observed);
                    var work=new CampaignRepositoryCaptureWork(stamp,cancelled=>{
                        if(cancelled())return new CampaignRepositoryImageResult(CampaignRepositoryError.Cancelled);
                        CampaignRepositoryImageResult result;
                        try{result=adapter.Validate(codec.Encode(root));}catch(Exception){return new CampaignRepositoryImageResult(CampaignRepositoryError.InvalidPayload);}
                        return cancelled()?new CampaignRepositoryImageResult(CampaignRepositoryError.Cancelled):result;
                    });
                    return new CampaignRepositoryCapturePreparation(CampaignRepositoryError.None,work);
                }
                catch(Exception){return new CampaignRepositoryCapturePreparation(CampaignRepositoryError.InvalidPayload);}
            }
        }
        public CampaignRepositoryRestorePreparation PrepareRepositoryRestore(ReviewFrame observed,byte[] payload,string expectedEditionIdentity,ICampaignRepositoryCodec codec)
        {
            if(observed==null||codec==null)throw new ArgumentNullException();
            lock(gate)
            {
                var error=RepositoryFrameError(observed);if(error!=CampaignRepositoryError.None)return new CampaignRepositoryRestorePreparation(error);
                if(payload==null||payload.Length==0||payload.Length>CampaignRepositoryImage.MaximumBytes)return new CampaignRepositoryRestorePreparation(CampaignRepositoryError.InvalidPayload);
                if(string.IsNullOrEmpty(expectedEditionIdentity)||expectedEditionIdentity.Length>CampaignRepositoryEdition.MaximumIdentityCharacters)return new CampaignRepositoryRestorePreparation(CampaignRepositoryError.Incompatible);
                var owned=(byte[])payload.Clone();var stamp=RepositoryStamp(observed);
                var adapter=new CycleCampaignRepositoryAdapter(colonisationCyclePackage!,colonisationCycleCodec!,codec);
                CampaignRepositoryRestoreWork? work=null;
                work=new CampaignRepositoryRestoreWork(stamp,cancelled=>{
                    if(cancelled())return new CampaignRepositoryRestoreResult(CampaignRepositoryError.Cancelled){Work=work};
                    var result=adapter.Load(owned,expectedEditionIdentity,out var candidate);
                    return cancelled()?new CampaignRepositoryRestoreResult(CampaignRepositoryError.Cancelled){Work=work}
                        :new CampaignRepositoryRestoreResult(result.Error,result.Image,candidate){Work=work};
                });
                return new CampaignRepositoryRestorePreparation(CampaignRepositoryError.None,work);
            }
        }
        public CampaignRepositoryRestorePreparation PrepareRepositoryRestore(ReviewFrame observed,Func<CampaignRepositoryStoredPayload> readPayload,ICampaignRepositoryCodec codec)
        {
            if(observed==null||readPayload==null||codec==null)throw new ArgumentNullException();
            lock(gate)
            {
                var error=RepositoryFrameError(observed);if(error!=CampaignRepositoryError.None)return new CampaignRepositoryRestorePreparation(error);
                var stamp=RepositoryStamp(observed);var adapter=new CycleCampaignRepositoryAdapter(colonisationCyclePackage!,colonisationCycleCodec!,codec);
                CampaignRepositoryRestoreWork? work=null;
                work=new CampaignRepositoryRestoreWork(stamp,cancelled=>{
                    if(cancelled())return new CampaignRepositoryRestoreResult(CampaignRepositoryError.Cancelled){Work=work};
                    try
                    {
                        var stored=readPayload();
                        if(cancelled())return new CampaignRepositoryRestoreResult(CampaignRepositoryError.Cancelled){Work=work};
                        var result=adapter.Load(stored.ReadPayload(),stored.EditionIdentity,out var candidate);
                        if(!result.Succeeded)return new CampaignRepositoryRestoreResult(result.Error){Work=work};
                        if(!stored.Matches(result.Image!))return new CampaignRepositoryRestoreResult(CampaignRepositoryError.InvalidPayload){Work=work};
                        return cancelled()?new CampaignRepositoryRestoreResult(CampaignRepositoryError.Cancelled){Work=work}
                            :new CampaignRepositoryRestoreResult(CampaignRepositoryError.None,result.Image,candidate){Work=work};
                    }
                    catch(Exception){return new CampaignRepositoryRestoreResult(CampaignRepositoryError.InvalidPayload){Work=work};}
                });
                return new CampaignRepositoryRestorePreparation(CampaignRepositoryError.None,work);
            }
        }
        public CampaignRepositoryError CompleteRepositoryRestore(CampaignRepositoryRestoreWork work,CampaignRepositoryRestoreResult result,out ReviewFrame? frame)
        {
            frame=null;if(work==null||result==null)throw new ArgumentNullException();
            lock(gate)
            {
                if(!ReferenceEquals(result.Work,work)||result.Settled)return CampaignRepositoryError.Stale;
                if(RepositoryBusy)return CampaignRepositoryError.Busy;
                if(!result.Settle())return CampaignRepositoryError.Stale;
                var candidate=result.Candidate;result.Candidate=null;
                if(result.Error!=CampaignRepositoryError.None)return result.Error;
                if(candidate==null||!IsRepositoryStateCurrent(work.Source))return CampaignRepositoryError.Stale;
                try
                {
                    var epoch=checked(generation+1);
                    var next=Capture(candidate,work.Source.Actor,null,epoch);
                    CaptureColonisationCandidate(candidate,next);
                    var status=candidate.CaptureStatus();
                    var published=new CampaignRepositoryStamp(id,epoch,status.Turn,status.CampaignRevision,status.Phase,next.Actor,RepositoryCombatBoundary(candidate,next.Actor));
                    session=candidate;generation=epoch;frame=next;
                    result.PublishedStamp=published;
                    return CampaignRepositoryError.None;
                }
                catch(Exception){return CampaignRepositoryError.InvalidPayload;}
            }
        }
    }
}
