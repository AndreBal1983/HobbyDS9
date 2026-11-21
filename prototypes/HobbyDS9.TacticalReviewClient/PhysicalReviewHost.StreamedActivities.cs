using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public interface IStreamedActivitiesReviewHostCodec : IPlayableFleetReviewHostCodec,IKnownGeneratedMapCodec
    {
        CampaignStreamedActivitiesPackageDto CopyPackage(CampaignStreamedActivitiesPackageDto package);
        CampaignStreamedActivitiesSaveDto CopySave(CampaignStreamedActivitiesSaveDto root);
    }
    public interface IStreamedActivitiesCheckpointStore
    {
        void Write(string slot,CampaignStreamedActivitiesSaveDto root,bool overwrite);
        CampaignStreamedActivitiesSaveDto Read(string slot);
    }
    public interface IBackgroundStreamedActivitiesCheckpointCodec : IStreamedActivitiesReviewHostCodec { }
    public interface IBackgroundStreamedActivitiesCheckpointStore : IStreamedActivitiesCheckpointStore { }
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignStreamedActivitiesPackageDto? streamedActivitiesPackage;
        private readonly IStreamedActivitiesReviewHostCodec? streamedActivitiesCodec;
        private readonly IStreamedActivitiesCheckpointStore? streamedActivitiesStore;
        private readonly Dictionary<long,CampaignStreamedActivitiesSaveDto> streamedActivitiesSaves=new Dictionary<long,CampaignStreamedActivitiesSaveDto>();
        private PhysicalReviewHost(CampaignStreamedActivitiesPackageDto package,IStreamedActivitiesReviewHostCodec codec,
            IStreamedActivitiesCheckpointStore? store,NaturalGalaxySettingsDto? settings,CampaignStreamedActivitiesSaveDto? saved)
        {
            this.codec=codec;streamedActivitiesCodec=codec;streamedActivitiesStore=store;streamedActivitiesPackage=codec.CopyPackage(package);
            session=saved==null?CampaignStreamedActivitiesApplication.StartGame(streamedActivitiesPackage,settings??throw new ArgumentNullException(nameof(settings)))
                :CampaignStreamedActivitiesApplication.LoadSave(streamedActivitiesPackage,codec.CopySave(saved));
        }
        public static PhysicalReviewHost StartStreamedActivities(CampaignStreamedActivitiesPackageDto package,NaturalGalaxySettingsDto settings,
            IStreamedActivitiesReviewHostCodec codec,IStreamedActivitiesCheckpointStore? store=null)=>new PhysicalReviewHost(package,codec,store,settings,null);
        public static PhysicalHostOpenResult OpenStreamedActivitiesFromSlot(CampaignStreamedActivitiesPackageDto package,
            IStreamedActivitiesReviewHostCodec codec,IStreamedActivitiesCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try{var candidate=new PhysicalReviewHost(package,codec,store,null,store.Read(slot));var frame=candidate.Read(actor,encounter);
                candidate.CaptureStreamedActivitiesCandidate((ICampaignStreamedActivitiesSession)candidate.session,frame);
                return new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,frame);}
            catch(ReviewCheckpointStoreException e){return new PhysicalHostOpenResult(e.Error);}
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        private GeneratedCampaignPresentation CaptureStreamedActivitiesCandidate(ICampaignStreamedActivitiesSession candidate,ReviewFrame frame)
            =>new GeneratedCampaignPresentation(frame,candidate.CaptureGeneratedMap(frame.Actor),candidate.CaptureKnownAstronomy(frame.Actor),streamedActivitiesCodec!);
        public CampaignStreamedActivityObservationDto CaptureStreamedActivities(ReviewFrame frame)
        {
            lock(gate){RequireCurrent(frame.HostId,frame.Generation);var status=session.CaptureStatus();
                if(streamedActivitiesPackage==null||frame.Status.Turn!=status.Turn||frame.Status.CampaignRevision!=status.CampaignRevision)throw new InvalidOperationException("Activity observation changed.");
                return ((ICampaignStreamedActivitiesSession)session).CaptureStreamedActivities(frame.Actor);}
        }
        public FrontierCommissionSnapshot CaptureFrontierCommission(ReviewFrame frame)
        {
            lock(gate){RequireCurrent(frame.HostId,frame.Generation);var status=session.CaptureStatus();
                if(streamedActivitiesPackage==null&&colonisationPackage==null||frame.Status.Turn!=status.Turn||frame.Status.CampaignRevision!=status.CampaignRevision)throw new InvalidOperationException("Commission observation changed.");
                if(colonisationPackage!=null){if(colonisationCommittedCommission!=null&&ReferenceEquals(colonisationCommissionFrame,frame))return colonisationCommittedCommission;return ((ICampaignColonisationOperations)session).CaptureFrontierCommission(frame.Actor);}
                if(streamedCommittedCommission!=null&&ReferenceEquals(streamedCommissionFrame,frame))return streamedCommittedCommission;
                return ((ICampaignStreamedActivitiesSession)session).CaptureFrontierCommission(frame.Actor);}
        }
        public CampaignActivityEligibilityDto EvaluateStreamedActivity(ReviewFrame frame,CampaignActivityCommandDto command)
        {
            lock(gate){RequireStreamedActivityFrame(frame,command);return ((ICampaignStreamedActivitiesSession)session).EvaluateActivity(command);}
        }
        public ReviewFrame SubmitStreamedActivity(ReviewFrame frame,CampaignActivityCommandDto command,out CampaignTacticalMutationResult result)
        {
            lock(gate){RequireStreamedActivityFrame(frame,command);result=((ICampaignStreamedActivitiesSession)session).SubmitActivity(command);return Capture(session,frame.Actor,frame.Battle?.EncounterId,generation);}
        }
        private void RequireStreamedActivityFrame(ReviewFrame frame,CampaignActivityCommandDto command)
        {
            RequireCurrent(frame.HostId,frame.Generation);var status=session.CaptureStatus();
            if(streamedActivitiesPackage==null||command==null||command.ActorId!=frame.Actor||command.ExpectedTurn!=frame.Status.Turn||command.ExpectedRevision!=frame.Status.CampaignRevision
                ||status.Turn!=frame.Status.Turn||status.CampaignRevision!=frame.Status.CampaignRevision)throw new InvalidOperationException("Activity command observation changed.");
        }
        private CampaignStreamedActivitiesSaveDto ExportStreamedActivities()
        {
            var root=streamedActivitiesCodec!.CopySave(((ICampaignStreamedActivitiesSession)session).ExportStreamedActivitiesSave());
            CampaignStreamedActivitiesApplication.LoadSave(streamedActivitiesPackage!,root);return root;
        }
        private ReviewSaveHandle SaveStreamedActivitiesCommitted()
        {var root=ExportStreamedActivities();var number=checked(nextSave+1);var result=new ReviewSaveHandle(id,number,session.CaptureStatus());streamedActivitiesSaves.Add(number,root);nextSave=number;return result;}
        private ReviewFrame LoadStreamedActivitiesCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            if(handle.HostId!=id||!streamedActivitiesSaves.TryGetValue(handle.Id,out var root))throw new ArgumentException("Unknown streamedActivities checkpoint.");
            var candidate=CampaignStreamedActivitiesApplication.LoadSave(streamedActivitiesPackage!,streamedActivitiesCodec!.CopySave(root));
            var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureStreamedActivitiesCandidate(candidate,frame);
            session=candidate;generation=epoch;return frame;
        }
        private ReviewCheckpointResult SaveStreamedActivitiesToSlot(string slot,bool overwrite)
        {
            if(streamedActivitiesStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignStreamedActivitiesSaveDto root;
            try{root=ExportStreamedActivities();}catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
            var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
            try{streamedActivitiesStore.Write(slot,root,overwrite);return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
        }
        private ReviewCheckpointResult LoadStreamedActivitiesFromSlot(string slot,string actor,string? encounter)
        {
            if(streamedActivitiesStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignStreamedActivitiesSaveDto root;
            try{root=streamedActivitiesStore.Read(slot);}catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed);}
            try{var candidate=CampaignStreamedActivitiesApplication.LoadSave(streamedActivitiesPackage!,streamedActivitiesCodec!.CopySave(root));
                var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureStreamedActivitiesCandidate(candidate,frame);
                var metadata=new ReviewSlotCheckpoint(slot,frame.Status);session=candidate;generation=epoch;
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
    }
}
