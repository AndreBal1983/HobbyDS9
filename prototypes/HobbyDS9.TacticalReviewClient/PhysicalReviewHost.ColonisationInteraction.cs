using System;
using System.Threading;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public enum ColonisationInteractionKind { Preview, Submission, PlanningAdvance }
    public sealed class ColonisationInteractionCompletion
    {
        internal readonly ColonisationInteractionWork Work;
        internal readonly ICampaignColonisationSession? Candidate;
        internal ReviewFrame? Frame;
        internal GeneratedCampaignPresentation? Presentation;
        internal FrontierCommissionSnapshot? Commission;
        internal bool Published;
        internal ColonisationCampaignSnapshot? Observation;
        internal System.Collections.Generic.IReadOnlyList<ColonisationPlanetSnapshot>? Planets;
        internal CampaignActivityObservationDto? Activities;
        public ColonisationEligibilitySnapshot? ColonisationEligibility {get;internal set;}
        public CampaignActivityEligibilityDto? Eligibility {get;internal set;}
        public CampaignTacticalMutationResult? Result {get;internal set;}
        public bool Failed {get;internal set;}
        public ColonisationCampaignSnapshot? PreparedObservation=>Observation;
        public System.Collections.Generic.IReadOnlyList<ColonisationPlanetSnapshot>? PreparedPlanets=>Planets;
        public bool Committed=>Published;
        public ReviewFrame? PreparedFrame=>Frame;
        internal ColonisationInteractionCompletion(ColonisationInteractionWork work,ICampaignColonisationSession? candidate=null){Work=work;Candidate=candidate;}
    }
    public sealed class ColonisationInteractionWork
    {
        internal readonly PhysicalReviewHost Host;
        internal readonly ICampaignColonisationSession Source;
        public ReviewFrame SourceFrame {get;}
        public ColonisationInteractionKind Kind {get;}
        private readonly CampaignActivityCommandDto? command;
        private readonly ColonisationCommandDto? colonyCommand;
        private int executed,cancelled,finished;
        internal ColonisationInteractionWork(PhysicalReviewHost host,ICampaignColonisationSession source,ReviewFrame frame,ColonisationInteractionKind kind,CampaignActivityCommandDto? command,ColonisationCommandDto? colonyCommand)
        {Host=host;Source=source;SourceFrame=frame;Kind=kind;this.command=command==null?null:Copy(command);this.colonyCommand=colonyCommand==null?null:ColonisationCommandCopy.Copy(colonyCommand);}
        public CampaignActivityCommandDto? Command=>command==null?null:Copy(command);
        public ColonisationCommandDto? ColonisationCommand=>colonyCommand==null?null:ColonisationCommandCopy.Copy(colonyCommand);
        internal static CampaignActivityCommandDto Copy(CampaignActivityCommandDto c)=>new CampaignActivityCommandDto{ActorId=c.ActorId,ExpectedTurn=c.ExpectedTurn,ExpectedRevision=c.ExpectedRevision,CommandId=c.CommandId,Kind=c.Kind,TargetId=c.TargetId,DefinitionId=c.DefinitionId,BlueprintRevision=c.BlueprintRevision,ShipId=c.ShipId};
        public bool Cancelled=>Volatile.Read(ref cancelled)!=0;
        public bool Settled=>Volatile.Read(ref executed)==0||Volatile.Read(ref finished)!=0;
        internal void Cancel()=>Interlocked.Exchange(ref cancelled,1);
        public ColonisationInteractionCompletion Execute()
        {
            if(Interlocked.Exchange(ref executed,1)!=0)throw new InvalidOperationException("Interaction already executed.");
            try{if(Cancelled)return new ColonisationInteractionCompletion(this){Failed=true};return Host.ExecuteColonisationInteraction(this);}
            catch(Exception){return new ColonisationInteractionCompletion(this){Failed=true};}
            finally{Volatile.Write(ref finished,1);Host.ColonisationInteractionSettled(this);}
        }
    }
    public sealed partial class PhysicalReviewHost
    {
        private ColonisationInteractionWork? colonisationInteraction;
        private GeneratedCampaignPresentation? colonisationCommittedPresentation;
        private ICampaignColonisationSession? colonisationPresentationSource;
        private FrontierCommissionSnapshot? colonisationCommittedCommission;
        private ReviewFrame? colonisationCommissionFrame;
        internal Action<string>? ColonisationInteractionProbe {get;set;}
        public ColonisationInteractionWork PrepareColonisationInteraction(ReviewFrame frame,ColonisationInteractionKind kind,CampaignActivityCommandDto? command=null,ColonisationCommandDto? colonyCommand=null)
        {
            lock(gate)
            {
                RequireNoStreamedInteraction();RequireCurrent(frame.HostId,frame.Generation);
                var status=session.CaptureStatus();
                if(colonisationPackage==null||colonisationCodec is not IBackgroundColonisationCheckpointCodec||!Enum.IsDefined(typeof(ColonisationInteractionKind),kind)
                    ||status.Turn!=frame.Status.Turn||status.CampaignRevision!=frame.Status.CampaignRevision
                    ||kind==ColonisationInteractionKind.PlanningAdvance&&(command!=null||colonyCommand!=null)
                    ||kind!=ColonisationInteractionKind.PlanningAdvance&&((command==null)==(colonyCommand==null)
                        ||command!=null&&(command.ActorId!=frame.Actor||command.ExpectedTurn!=frame.Status.Turn||command.ExpectedRevision!=frame.Status.CampaignRevision)
                        ||colonyCommand!=null&&(colonyCommand.ActorId!=frame.Actor||colonyCommand.Turn!=frame.Status.Turn||colonyCommand.Revision!=frame.Status.CampaignRevision)))
                    throw new InvalidOperationException("Invalid streamed interaction observation.");
                return colonisationInteraction=new ColonisationInteractionWork(this,(ICampaignColonisationSession)session,frame,kind,command,colonyCommand);
            }
        }
        internal ColonisationInteractionCompletion ExecuteColonisationInteraction(ColonisationInteractionWork work)
        {
            // No host gate, source mutation, Unity objects, serialization or replay.
            ColonisationInteractionProbe?.Invoke("before-fork");
            var candidate=work.Source.ForkInteraction(work.SourceFrame.Status.Turn,work.SourceFrame.Status.CampaignRevision);
            var completion=new ColonisationInteractionCompletion(work,candidate);
            if(work.Cancelled){completion.Failed=true;return completion;}
            ColonisationInteractionProbe?.Invoke("before-action");
            if(work.Kind==ColonisationInteractionKind.Preview)
            {if(work.ColonisationCommand!=null)completion.ColonisationEligibility=candidate.EvaluateColonisation(work.ColonisationCommand);else completion.Eligibility=candidate.EvaluateActivity(work.Command!);}
            else completion.Result=work.Kind==ColonisationInteractionKind.Submission?(work.ColonisationCommand!=null?candidate.SubmitColonisation(work.ColonisationCommand):candidate.SubmitActivity(work.Command!)):candidate.AdvanceTurn(work.SourceFrame.Status.Turn,work.SourceFrame.Status.CampaignRevision);
            if(work.Kind!=ColonisationInteractionKind.Preview&&completion.Result!.Accepted)
            {
                var frame=Capture(candidate,work.SourceFrame.Actor,work.SourceFrame.Battle?.EncounterId,work.SourceFrame.Generation);
                if(work.Kind==ColonisationInteractionKind.PlanningAdvance&&frame.Encounters.Count==1)frame=Capture(candidate,frame.Actor,frame.Encounters[0],frame.Generation);
                completion.Frame=frame;completion.Presentation=CaptureColonisationCandidate(candidate,frame);completion.Commission=candidate.CaptureFrontierCommission(frame.Actor);completion.Observation=candidate.CaptureColonisation(frame.Actor);completion.Planets=candidate.CaptureColonisationPlanets(frame.Actor).Select(p=>new ColonisationPlanetSnapshot(p)).ToList().AsReadOnly();completion.Activities=candidate.CaptureActivities(frame.Actor);
            }
            ColonisationInteractionProbe?.Invoke("after-capture");return completion;
        }
        public bool CompleteColonisationInteraction(ColonisationInteractionWork work,ColonisationInteractionCompletion completion,ColonisationControllerUpdate? update=null)
        {
            lock(gate)
            {
                if(!ReferenceEquals(colonisationInteraction,work)||!ReferenceEquals(completion.Work,work)||work.Cancelled||completion.Failed
                    ||!ReferenceEquals(session,work.Source)||work.SourceFrame.HostId!=id||work.SourceFrame.Generation!=generation)return false;
                var status=session.CaptureStatus();
                if(status.Turn!=work.SourceFrame.Status.Turn||status.CampaignRevision!=work.SourceFrame.Status.CampaignRevision)return false;
                if(work.Kind!=ColonisationInteractionKind.Preview&&completion.Result?.Accepted==true)
                {
                    if(completion.Candidate==null||completion.Frame==null||completion.Presentation==null||update==null||!update.Current(work,completion))return false;
                    // All throwing work precedes the live publication assignments.
                    ColonisationInteractionProbe?.Invoke("before-publication");
                    if(!ReferenceEquals(colonisationInteraction,work)||work.Cancelled||!ReferenceEquals(session,work.Source)||!update.Current(work,completion))return false;
                    session=completion.Candidate;colonisationCommittedPresentation=completion.Presentation;colonisationPresentationSource=completion.Candidate;colonisationCommittedCommission=completion.Commission;colonisationCommissionFrame=completion.Frame;update.Apply();completion.Published=true;
                }
                colonisationInteraction=null;return true;
            }
        }
        public void AbandonColonisationInteraction(ColonisationInteractionWork work)
        {lock(gate){if(!ReferenceEquals(work.Host,this))return;work.Cancel();if(work.Settled&&ReferenceEquals(colonisationInteraction,work))colonisationInteraction=null;}}
        internal void ColonisationInteractionSettled(ColonisationInteractionWork work)
        {lock(gate){if(work.Cancelled&&ReferenceEquals(colonisationInteraction,work))colonisationInteraction=null;}}
    }
    // Prepared on the owning thread. Application is plain assignment only; no
    // host read, callback, allocation or validation can fail after publication.
    public sealed class ColonisationControllerUpdate
    {
        private readonly ReviewController controller;
        private readonly ColonisationInteractionCompletion completion;
        internal ColonisationControllerUpdate(ReviewController controller,ColonisationInteractionCompletion completion){this.controller=controller;this.completion=completion;}
        internal bool Current(ColonisationInteractionWork work,ColonisationInteractionCompletion value)=>ReferenceEquals(completion,value)&&controller.CanReceiveColonisationInteraction(work,value);
        internal void Apply()=>controller.ApplyColonisationInteraction(completion.Frame!,completion.Work.Kind);
    }
    public sealed partial class ReviewController
    {
        internal bool CanReceiveColonisationInteraction(ColonisationInteractionWork work,ColonisationInteractionCompletion completion)
            =>ReferenceEquals(host,work.Host)&&ReferenceEquals(Frame,work.SourceFrame)&&!RefreshRequired&&!Dirty&&pending==null&&completion.Frame!=null
                &&completion.Frame.HostId==Frame.HostId&&completion.Frame.Generation==Frame.Generation&&completion.Frame.Actor==Frame.Actor
                &&completion.Frame.Status.Turn>=Frame.Status.Turn&&completion.Frame.Status.CampaignRevision>=Frame.Status.CampaignRevision;
        public ColonisationControllerUpdate PrepareColonisationInteractionReceive(ColonisationInteractionCompletion completion)
        {
            if(!CanReceiveColonisationInteraction(completion.Work,completion))throw new InvalidOperationException("Obsolete streamed controller.");
            return new ColonisationControllerUpdate(this,completion);
        }
        internal void ApplyColonisationInteraction(ReviewFrame frame,ColonisationInteractionKind kind)
        {Frame=frame;Mode=ModeFor(frame);RefreshRequired=false;LastBackendError=null;if(kind==ColonisationInteractionKind.PlanningAdvance&&frame.Encounters.Count==1)SelectedShip=null;}
    }
}
