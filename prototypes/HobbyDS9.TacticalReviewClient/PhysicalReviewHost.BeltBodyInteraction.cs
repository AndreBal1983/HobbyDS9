using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public enum BeltBodyInteractionKind { Preview, Submission }
    public sealed class BeltBodyInteractionCompletion
    {
        internal readonly BeltBodyInteractionWork Work;
        internal readonly ICampaignColonisationOperations? Candidate;
        internal ReviewFrame? Frame;
        internal GeneratedCampaignPresentation? Presentation;
        internal FrontierCommissionSnapshot? Commission;
        internal ColonisationCampaignSnapshot? Observation;
        internal IReadOnlyList<ColonisationPlanetSnapshot>? Planets;
        internal bool Published;
        public BeltBodyCommandPreview? Preview {get;internal set;}
        public BeltBodyCommandResult? Result {get;internal set;}
        public BeltBodiesSnapshot? PreparedBeltBodies {get;internal set;}
        public ColonisationCampaignSnapshot? PreparedObservation=>Observation;
        public IReadOnlyList<ColonisationPlanetSnapshot>? PreparedPlanets=>Planets;
        public bool Failed {get;internal set;}
        public bool Committed=>Published;
        public ReviewFrame? PreparedFrame=>Frame;
        internal BeltBodyInteractionCompletion(BeltBodyInteractionWork work,ICampaignColonisationOperations? candidate=null){Work=work;Candidate=candidate;}
    }
    public sealed class BeltBodyInteractionWork
    {
        internal readonly PhysicalReviewHost Host;
        internal readonly ICampaignColonisationOperations Source;
        private readonly BeltBodyCommandDto command;
        private int executed,cancelled,finished;
        public ReviewFrame SourceFrame {get;}
        public BeltBodyInteractionKind Kind {get;}
        public BeltBodyCommandDto Command=>Copy(command);
        public bool Cancelled=>Volatile.Read(ref cancelled)!=0;
        public bool Settled=>Volatile.Read(ref executed)==0||Volatile.Read(ref finished)!=0;
        internal BeltBodyInteractionWork(PhysicalReviewHost host,ICampaignColonisationOperations source,ReviewFrame frame,BeltBodyInteractionKind kind,BeltBodyCommandDto command)
        {Host=host;Source=source;SourceFrame=frame;Kind=kind;this.command=Copy(command);}
        private static BeltBodyCommandDto Copy(BeltBodyCommandDto c)=>new BeltBodyCommandDto
        {ActorId=c.ActorId,CommandId=c.CommandId,ExpectedTurn=c.ExpectedTurn,ExpectedRevision=c.ExpectedRevision,Action=c.Action,BodyId=c.BodyId,ShipId=c.ShipId,ColonyId=c.ColonyId,SurveyId=c.SurveyId,MineId=c.MineId};
        internal void Cancel()=>Interlocked.Exchange(ref cancelled,1);
        public BeltBodyInteractionCompletion Execute()
        {
            if(Interlocked.Exchange(ref executed,1)!=0)throw new InvalidOperationException("Interaction already executed.");
            try{if(Cancelled)return new BeltBodyInteractionCompletion(this){Failed=true};return Host.ExecuteBeltBodyInteraction(this);}
            catch(Exception){return new BeltBodyInteractionCompletion(this){Failed=true};}
            finally{Volatile.Write(ref finished,1);Host.BeltBodyInteractionSettled(this);}
        }
    }
    public sealed partial class PhysicalReviewHost
    {
        private BeltBodyInteractionWork? beltBodyInteraction;
        public bool SupportsBeltBodies=>colonisationCyclePackage is CampaignBeltBodiesPackageDto;
        internal Action<string>? BeltBodyInteractionProbe {get;set;}
        public BeltBodiesSnapshot? CaptureBeltBodies(ReviewFrame frame)
        {lock(gate){RequireCurrent(frame.HostId,frame.Generation);var status=session.CaptureStatus();
            if(status.Turn!=frame.Status.Turn||status.CampaignRevision!=frame.Status.CampaignRevision)throw new InvalidOperationException("Engineering observation changed.");
            return (session as ICampaignBeltBodiesSession)?.CaptureBeltBodies(frame.Actor);}}
        public BeltBodyInteractionWork PrepareBeltBodyInteraction(ReviewFrame frame,BeltBodyInteractionKind kind,BeltBodyCommandDto command)
        {
            lock(gate)
            {
                RequireNoStreamedInteraction();RequireCurrent(frame.HostId,frame.Generation);var status=session.CaptureStatus();
                if(colonisationPackage==null||(colonisationCyclePackage!=null?colonisationCycleCodec is not IBackgroundColonisationCycleCheckpointCodec:colonisationRepeatPackage!=null?colonisationRepeatCodec is not IBackgroundColonisationRepeatCheckpointCodec:colonisationGrowthPackage!=null?colonisationGrowthCodec is not IBackgroundColonisationGrowthCheckpointCodec:colonisationExpansionPackage!=null?colonisationExpansionCodec is not IBackgroundColonisationExpansionCheckpointCodec:colonisationCodec is not IBackgroundColonisationCheckpointCodec)
                    ||session is not ICampaignColonisationOperations source||session is not ICampaignBeltBodiesSession
                    ||!Enum.IsDefined(typeof(BeltBodyInteractionKind),kind)||command==null||command.ActorId!=frame.Actor
                    ||command.ExpectedTurn!=frame.Status.Turn||command.ExpectedRevision!=frame.Status.CampaignRevision
                    ||status.Turn!=frame.Status.Turn||status.CampaignRevision!=frame.Status.CampaignRevision)
                    throw new InvalidOperationException("Invalid belt interaction observation.");
                return beltBodyInteraction=new BeltBodyInteractionWork(this,source,frame,kind,command);
            }
        }
        internal BeltBodyInteractionCompletion ExecuteBeltBodyInteraction(BeltBodyInteractionWork work)
        {
            BeltBodyInteractionProbe?.Invoke("before-fork");
            var candidate=work.Source.ForkOperations(work.SourceFrame.Status.Turn,work.SourceFrame.Status.CampaignRevision);
            var completion=new BeltBodyInteractionCompletion(work,candidate);
            if(work.Cancelled||candidate is not ICampaignBeltBodiesSession bodies){completion.Failed=true;return completion;}
            BeltBodyInteractionProbe?.Invoke("before-action");
            if(work.Kind==BeltBodyInteractionKind.Preview)completion.Preview=bodies.PreviewBeltCommand(work.Command);
            else completion.Result=bodies.SubmitBeltCommand(work.Command);
            completion.PreparedBeltBodies=bodies.CaptureBeltBodies(work.SourceFrame.Actor);
            if(completion.Result?.Accepted==true)
            {
                var frame=Capture(candidate,work.SourceFrame.Actor,work.SourceFrame.Battle?.EncounterId,work.SourceFrame.Generation);
                completion.Frame=frame;completion.Presentation=CaptureColonisationCandidate(candidate,frame);completion.Commission=candidate.CaptureFrontierCommission(frame.Actor);
                completion.Observation=candidate.CaptureColonisation(frame.Actor);completion.Planets=candidate.CaptureColonisationPlanets(frame.Actor).Select(p=>new ColonisationPlanetSnapshot(p)).ToList().AsReadOnly();
            }
            BeltBodyInteractionProbe?.Invoke("after-capture");return completion;
        }
        public bool CompleteBeltBodyInteraction(BeltBodyInteractionWork work,BeltBodyInteractionCompletion completion,BeltBodyControllerUpdate? update=null)
        {
            lock(gate)
            {
                if(!ReferenceEquals(beltBodyInteraction,work)||!ReferenceEquals(completion.Work,work)||work.Cancelled||completion.Failed
                    ||!ReferenceEquals(session,work.Source)||work.SourceFrame.HostId!=id||work.SourceFrame.Generation!=generation)return false;
                var status=session.CaptureStatus();if(status.Turn!=work.SourceFrame.Status.Turn||status.CampaignRevision!=work.SourceFrame.Status.CampaignRevision)return false;
                if(completion.Result?.Accepted==true)
                {
                    if(completion.Candidate==null||completion.Frame==null||completion.Presentation==null||update==null||!update.Current(work,completion))return false;
                    BeltBodyInteractionProbe?.Invoke("before-publication");
                    if(!ReferenceEquals(beltBodyInteraction,work)||work.Cancelled||!ReferenceEquals(session,work.Source)||!update.Current(work,completion))return false;
                    session=completion.Candidate;colonisationCommittedPresentation=completion.Presentation;colonisationPresentationSource=completion.Candidate;
                    colonisationCommittedCommission=completion.Commission;colonisationCommissionFrame=completion.Frame;update.Apply();completion.Published=true;
                }
                beltBodyInteraction=null;return true;
            }
        }
        public void AbandonBeltBodyInteraction(BeltBodyInteractionWork work)
        {lock(gate){if(!ReferenceEquals(work.Host,this))return;work.Cancel();if(work.Settled&&ReferenceEquals(beltBodyInteraction,work))beltBodyInteraction=null;}}
        internal void BeltBodyInteractionSettled(BeltBodyInteractionWork work)
        {lock(gate){if(work.Cancelled&&ReferenceEquals(beltBodyInteraction,work))beltBodyInteraction=null;}}
    }
    public sealed class BeltBodyControllerUpdate
    {
        private readonly ReviewController controller;
        private readonly BeltBodyInteractionCompletion completion;
        internal BeltBodyControllerUpdate(ReviewController controller,BeltBodyInteractionCompletion completion){this.controller=controller;this.completion=completion;}
        internal bool Current(BeltBodyInteractionWork work,BeltBodyInteractionCompletion value)=>ReferenceEquals(completion,value)&&controller.CanReceiveBeltBodyInteraction(work,value);
        internal void Apply()=>controller.ApplyColonisationInteraction(completion.PreparedFrame!,ColonisationInteractionKind.Submission);
    }
    public sealed partial class ReviewController
    {
        internal bool CanReceiveBeltBodyInteraction(BeltBodyInteractionWork work,BeltBodyInteractionCompletion completion)
            =>ReferenceEquals(host,work.Host)&&ReferenceEquals(Frame,work.SourceFrame)&&!RefreshRequired&&!Dirty&&pending==null&&completion.PreparedFrame!=null
                &&completion.PreparedFrame.HostId==Frame.HostId&&completion.PreparedFrame.Generation==Frame.Generation&&completion.PreparedFrame.Actor==Frame.Actor
                &&completion.PreparedFrame.Status.Turn==Frame.Status.Turn&&completion.PreparedFrame.Status.CampaignRevision>Frame.Status.CampaignRevision;
        public BeltBodyControllerUpdate PrepareBeltBodyInteractionReceive(BeltBodyInteractionCompletion completion)
        {if(!CanReceiveBeltBodyInteraction(completion.Work,completion))throw new InvalidOperationException("Obsolete belt controller.");return new BeltBodyControllerUpdate(this,completion);}
    }
}
