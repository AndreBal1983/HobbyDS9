using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public interface IActivitiesReviewHostCodec : IPlayableFleetReviewHostCodec,IKnownGeneratedMapCodec
    {
        CampaignActivitiesPackageDto CopyPackage(CampaignActivitiesPackageDto package);
        CampaignActivitiesSaveDto CopySave(CampaignActivitiesSaveDto root);
    }
    public interface IActivitiesCheckpointStore
    {
        void Write(string slot,CampaignActivitiesSaveDto root,bool overwrite);
        CampaignActivitiesSaveDto Read(string slot);
    }
    public interface IBackgroundActivitiesCheckpointCodec : IActivitiesReviewHostCodec { }
    public interface IBackgroundActivitiesCheckpointStore : IActivitiesCheckpointStore { }
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignActivitiesPackageDto? activitiesPackage;
        private readonly IActivitiesReviewHostCodec? activitiesCodec;
        private readonly IActivitiesCheckpointStore? activitiesStore;
        private readonly Dictionary<long,CampaignActivitiesSaveDto> activitiesSaves=new Dictionary<long,CampaignActivitiesSaveDto>();
        private PhysicalReviewHost(CampaignActivitiesPackageDto package,IActivitiesReviewHostCodec codec,
            IActivitiesCheckpointStore? store,NaturalGalaxySettingsDto? settings,CampaignActivitiesSaveDto? saved)
        {
            this.codec=codec;activitiesCodec=codec;activitiesStore=store;activitiesPackage=codec.CopyPackage(package);
            session=saved==null?CampaignActivitiesApplication.StartGame(activitiesPackage,settings??throw new ArgumentNullException(nameof(settings)))
                :CampaignActivitiesApplication.LoadSave(activitiesPackage,codec.CopySave(saved));
        }
        public static PhysicalReviewHost StartActivities(CampaignActivitiesPackageDto package,NaturalGalaxySettingsDto settings,
            IActivitiesReviewHostCodec codec,IActivitiesCheckpointStore? store=null)=>new PhysicalReviewHost(package,codec,store,settings,null);
        public static PhysicalHostOpenResult OpenActivitiesFromSlot(CampaignActivitiesPackageDto package,
            IActivitiesReviewHostCodec codec,IActivitiesCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try{var candidate=new PhysicalReviewHost(package,codec,store,null,store.Read(slot));var frame=candidate.Read(actor,encounter);
                candidate.CaptureActivitiesCandidate((ICampaignActivitiesSession)candidate.session,frame);
                return new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,frame);}
            catch(ReviewCheckpointStoreException e){return new PhysicalHostOpenResult(e.Error);}
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        private GeneratedCampaignPresentation CaptureActivitiesCandidate(ICampaignActivitiesSession candidate,ReviewFrame frame)
            =>new GeneratedCampaignPresentation(frame,candidate.CaptureGeneratedMap(frame.Actor),candidate.CaptureKnownAstronomy(frame.Actor),activitiesCodec!);
        public CampaignActivityObservationDto CaptureActivities(ReviewFrame frame)
        {
            lock(gate){RequireCurrent(frame.HostId,frame.Generation);var status=session.CaptureStatus();
                if(activitiesPackage==null||frame.Status.Turn!=status.Turn||frame.Status.CampaignRevision!=status.CampaignRevision)throw new InvalidOperationException("Activity observation changed.");
                return ((ICampaignActivitiesSession)session).CaptureActivities(frame.Actor);}
        }
        public CampaignActivityEligibilityDto EvaluateActivity(ReviewFrame frame,CampaignActivityCommandDto command)
        {
            lock(gate){RequireActivityFrame(frame,command);return ((ICampaignActivitiesSession)session).EvaluateActivity(command);}
        }
        public ReviewFrame SubmitActivity(ReviewFrame frame,CampaignActivityCommandDto command,out CampaignTacticalMutationResult result)
        {
            lock(gate){RequireActivityFrame(frame,command);result=((ICampaignActivitiesSession)session).SubmitActivity(command);return Capture(session,frame.Actor,frame.Battle?.EncounterId,generation);}
        }
        private void RequireActivityFrame(ReviewFrame frame,CampaignActivityCommandDto command)
        {
            RequireCurrent(frame.HostId,frame.Generation);var status=session.CaptureStatus();
            if(activitiesPackage==null||command==null||command.ActorId!=frame.Actor||command.ExpectedTurn!=frame.Status.Turn||command.ExpectedRevision!=frame.Status.CampaignRevision
                ||status.Turn!=frame.Status.Turn||status.CampaignRevision!=frame.Status.CampaignRevision)throw new InvalidOperationException("Activity command observation changed.");
        }
        private CampaignActivitiesSaveDto ExportActivities()
        {
            var root=activitiesCodec!.CopySave(((ICampaignActivitiesSession)session).ExportActivitiesSave());
            CampaignActivitiesApplication.LoadSave(activitiesPackage!,root);return root;
        }
        private ReviewSaveHandle SaveActivitiesCommitted()
        {var root=ExportActivities();var number=checked(nextSave+1);var result=new ReviewSaveHandle(id,number,session.CaptureStatus());activitiesSaves.Add(number,root);nextSave=number;return result;}
        private ReviewFrame LoadActivitiesCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            if(handle.HostId!=id||!activitiesSaves.TryGetValue(handle.Id,out var root))throw new ArgumentException("Unknown activities checkpoint.");
            var candidate=CampaignActivitiesApplication.LoadSave(activitiesPackage!,activitiesCodec!.CopySave(root));
            var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureActivitiesCandidate(candidate,frame);
            session=candidate;generation=epoch;return frame;
        }
        private ReviewCheckpointResult SaveActivitiesToSlot(string slot,bool overwrite)
        {
            if(activitiesStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignActivitiesSaveDto root;
            try{root=ExportActivities();}catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
            var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
            try{activitiesStore.Write(slot,root,overwrite);return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
        }
        private ReviewCheckpointResult LoadActivitiesFromSlot(string slot,string actor,string? encounter)
        {
            if(activitiesStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignActivitiesSaveDto root;
            try{root=activitiesStore.Read(slot);}catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed);}
            try{var candidate=CampaignActivitiesApplication.LoadSave(activitiesPackage!,activitiesCodec!.CopySave(root));
                var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureActivitiesCandidate(candidate,frame);
                var metadata=new ReviewSlotCheckpoint(slot,frame.Status);session=candidate;generation=epoch;
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
    }
}
