using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public interface IColonisationReviewHostCodec:IPlayableFleetReviewHostCodec,IKnownGeneratedMapCodec
    {
        CampaignColonisationPackageDto CopyPackage(CampaignColonisationPackageDto package);
        CampaignColonisationSaveDto CopySave(CampaignColonisationSaveDto root);
    }
    public interface IColonisationCheckpointStore
    {
        void Write(string slot,CampaignColonisationSaveDto root,bool overwrite);
        CampaignColonisationSaveDto Read(string slot);
    }
    public interface IBackgroundColonisationCheckpointCodec:IColonisationReviewHostCodec { }
    public interface IBackgroundColonisationCheckpointStore:IColonisationCheckpointStore { }
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignColonisationPackageDto? colonisationPackage;
        private readonly IColonisationReviewHostCodec? colonisationCodec;
        private readonly IColonisationCheckpointStore? colonisationStore;
        private readonly Dictionary<long,CampaignColonisationSaveDto> colonisationSaves=new Dictionary<long,CampaignColonisationSaveDto>();
        private PhysicalReviewHost(CampaignColonisationPackageDto package,IColonisationReviewHostCodec codec,
            IColonisationCheckpointStore? store,NaturalGalaxySettingsDto? settings,CampaignColonisationSaveDto? saved)
        {
            this.codec=codec;colonisationCodec=codec;colonisationStore=store;colonisationPackage=codec.CopyPackage(package);
            session=saved==null?CampaignColonisationApplication.StartGame(colonisationPackage,settings??throw new ArgumentNullException(nameof(settings)))
                :CampaignColonisationApplication.LoadSave(colonisationPackage,codec.CopySave(saved));
        }
        public static PhysicalReviewHost StartColonisation(CampaignColonisationPackageDto package,NaturalGalaxySettingsDto settings,
            IColonisationReviewHostCodec codec,IColonisationCheckpointStore? store=null)=>new PhysicalReviewHost(package,codec,store,settings,null);
        public static PhysicalHostOpenResult OpenColonisationFromSlot(CampaignColonisationPackageDto package,
            IColonisationReviewHostCodec codec,IColonisationCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try{var candidate=new PhysicalReviewHost(package,codec,store,null,store.Read(slot));var frame=candidate.Read(actor,encounter);
                candidate.CaptureColonisationCandidate((ICampaignColonisationSession)candidate.session,frame);
                return new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,frame);}
            catch(ReviewCheckpointStoreException e){return new PhysicalHostOpenResult(e.Error);}
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        private GeneratedCampaignPresentation CaptureColonisationCandidate(ICampaignColonisationOperations candidate,ReviewFrame frame)
            =>new GeneratedCampaignPresentation(frame,candidate.CaptureGeneratedMap(frame.Actor),candidate.CaptureKnownAstronomy(frame.Actor),colonisationCodec!);
        public ColonisationCampaignSnapshot CaptureColonisation(ReviewFrame frame)
        {
            lock(gate){RequireCurrent(frame.HostId,frame.Generation);var status=session.CaptureStatus();
                if(colonisationPackage==null||frame.Status.Turn!=status.Turn||frame.Status.CampaignRevision!=status.CampaignRevision)throw new InvalidOperationException("Activity observation changed.");
                return ((ICampaignColonisationOperations)session).CaptureColonisation(frame.Actor);}
        }
        public System.Collections.Generic.IReadOnlyList<ColonisationPlanetObservationDto> CaptureColonisationPlanets(ReviewFrame frame)
        {lock(gate){RequireColonisationFrame(frame);return ((ICampaignColonisationOperations)session).CaptureColonisationPlanets(frame.Actor);}}
        public CampaignActivityObservationDto CaptureColonisationActivities(ReviewFrame frame)
        {lock(gate){RequireColonisationFrame(frame);return ((ICampaignColonisationOperations)session).CaptureActivities(frame.Actor);}}
        private void RequireColonisationFrame(ReviewFrame frame)
        {RequireCurrent(frame.HostId,frame.Generation);var status=session.CaptureStatus();
         if(colonisationPackage==null||status.Turn!=frame.Status.Turn||status.CampaignRevision!=frame.Status.CampaignRevision)throw new InvalidOperationException("Colonisation observation changed.");}
        public ColonisationEligibilitySnapshot EvaluateColonisation(ReviewFrame frame,ColonisationCommandDto command)
        {lock(gate){RequireColonisationCommandFrame(frame,command);return ((ICampaignColonisationOperations)session).EvaluateColonisation(command);}}
        public ReviewFrame SubmitColonisation(ReviewFrame frame,ColonisationCommandDto command,out CampaignTacticalMutationResult result)
        {lock(gate){RequireColonisationCommandFrame(frame,command);result=((ICampaignColonisationOperations)session).SubmitColonisation(command);return Capture(session,frame.Actor,frame.Battle?.EncounterId,generation);}}
        private void RequireColonisationCommandFrame(ReviewFrame frame,ColonisationCommandDto command)
        {RequireColonisationFrame(frame);if(command==null||command.ActorId!=frame.Actor||command.Turn!=frame.Status.Turn||command.Revision!=frame.Status.CampaignRevision)throw new InvalidOperationException("Colonisation command observation changed.");}
        public CampaignActivityEligibilityDto EvaluateColonisationActivity(ReviewFrame frame,CampaignActivityCommandDto command)
        {
            lock(gate){RequireColonisationActivityFrame(frame,command);return ((ICampaignColonisationOperations)session).EvaluateActivity(command);}
        }
        public ReviewFrame SubmitColonisationActivity(ReviewFrame frame,CampaignActivityCommandDto command,out CampaignTacticalMutationResult result)
        {
            lock(gate){RequireColonisationActivityFrame(frame,command);result=((ICampaignColonisationOperations)session).SubmitActivity(command);return Capture(session,frame.Actor,frame.Battle?.EncounterId,generation);}
        }
        private void RequireColonisationActivityFrame(ReviewFrame frame,CampaignActivityCommandDto command)
        {
            RequireCurrent(frame.HostId,frame.Generation);var status=session.CaptureStatus();
            if(colonisationPackage==null||command==null||command.ActorId!=frame.Actor||command.ExpectedTurn!=frame.Status.Turn||command.ExpectedRevision!=frame.Status.CampaignRevision
                ||status.Turn!=frame.Status.Turn||status.CampaignRevision!=frame.Status.CampaignRevision)throw new InvalidOperationException("Activity command observation changed.");
        }
        private CampaignColonisationSaveDto ExportColonisation()
        {
            var root=colonisationCodec!.CopySave(((ICampaignColonisationSession)session).ExportColonisationSave());
            LoadColonisationSource(root);return root;
        }
        private ReviewSaveHandle SaveColonisationCommitted()
        {var root=ExportColonisation();var number=checked(nextSave+1);var result=new ReviewSaveHandle(id,number,session.CaptureStatus());colonisationSaves.Add(number,root);nextSave=number;return result;}
        private ReviewFrame LoadColonisationCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            if(handle.HostId!=id||!colonisationSaves.TryGetValue(handle.Id,out var root))throw new ArgumentException("Unknown colonisation checkpoint.");
            var candidate=LoadColonisationSource(colonisationCodec!.CopySave(root));
            var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureColonisationCandidate(candidate,frame);
            session=candidate;generation=epoch;return frame;
        }
        private ReviewCheckpointResult SaveColonisationToSlot(string slot,bool overwrite)
        {
            if(colonisationStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignColonisationSaveDto root;
            try{root=ExportColonisation();}catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
            var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
            try{colonisationStore.Write(slot,root,overwrite);return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
        }
        private ReviewCheckpointResult LoadColonisationFromSlot(string slot,string actor,string? encounter)
        {
            if(colonisationStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignColonisationSaveDto root;
            try{root=colonisationStore.Read(slot);}catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed);}
            try{var candidate=LoadColonisationSource(colonisationCodec!.CopySave(root));
                var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureColonisationCandidate(candidate,frame);
                var metadata=new ReviewSlotCheckpoint(slot,frame.Status);session=candidate;generation=epoch;
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
    }
}
