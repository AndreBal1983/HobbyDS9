using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public interface IColonisationRepeatReviewHostCodec:IColonisationReviewHostCodec
    {
        CampaignColonisationRepeatPackageDto CopyRepeatPackage(CampaignColonisationRepeatPackageDto package);
        CampaignColonisationRepeatSaveDto CopyRepeatSave(CampaignColonisationRepeatSaveDto root);
    }
    public interface IColonisationRepeatCheckpointStore
    {
        void Write(string slot,CampaignColonisationRepeatSaveDto root,bool overwrite);
        CampaignColonisationRepeatSaveDto Read(string slot);
    }
    public interface IBackgroundColonisationRepeatCheckpointCodec:IColonisationRepeatReviewHostCodec { }
    public interface IBackgroundColonisationRepeatCheckpointStore:IColonisationRepeatCheckpointStore { }
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignColonisationRepeatPackageDto? colonisationRepeatPackage;
        private readonly IColonisationRepeatReviewHostCodec? colonisationRepeatCodec;
        private readonly IColonisationRepeatCheckpointStore? colonisationRepeatStore;
        private readonly Dictionary<long,CampaignColonisationRepeatSaveDto> colonisationRepeatSaves=new Dictionary<long,CampaignColonisationRepeatSaveDto>();
        private PhysicalReviewHost(CampaignColonisationRepeatPackageDto package,IColonisationRepeatReviewHostCodec codec,
            IColonisationRepeatCheckpointStore? store,NaturalGalaxySettingsDto? settings,CampaignColonisationRepeatSaveDto? saved)
        {
            this.codec=codec;colonisationRepeatCodec=codec;colonisationRepeatStore=store;colonisationRepeatPackage=codec.CopyRepeatPackage(package);
            colonisationPackage=colonisationRepeatPackage.BaseGrowth!.BaseRosterV2!.BaseV1;colonisationCodec=codec;
            session=saved==null?CampaignColonisationRepeatApplication.StartRepeatGame(colonisationRepeatPackage,settings??throw new ArgumentNullException(nameof(settings)))
                :CampaignColonisationRepeatApplication.LoadRepeatSave(colonisationRepeatPackage,codec.CopyRepeatSave(saved));
        }
        public static PhysicalReviewHost StartColonisationRepeat(CampaignColonisationRepeatPackageDto package,NaturalGalaxySettingsDto settings,
            IColonisationRepeatReviewHostCodec codec,IColonisationRepeatCheckpointStore? store=null)=>new PhysicalReviewHost(package,codec,store,settings,null);
        public static PhysicalHostOpenResult OpenColonisationRepeatFromSlot(CampaignColonisationRepeatPackageDto package,
            IColonisationRepeatReviewHostCodec codec,IColonisationRepeatCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try{var candidate=new PhysicalReviewHost(package,codec,store,null,store.Read(slot));var frame=candidate.Read(actor,encounter);
                candidate.CaptureColonisationCandidate((ICampaignColonisationRepeatSession)candidate.session,frame);
                return new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,frame);}
            catch(ReviewCheckpointStoreException e){return new PhysicalHostOpenResult(e.Error);}
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        private ICampaignColonisationRepeatSession LoadColonisationRepeatSource(CampaignColonisationRepeatSaveDto root)
            =>CampaignColonisationRepeatApplication.LoadRepeatSave(colonisationRepeatPackage!,root);
        private CampaignColonisationRepeatSaveDto ExportColonisationRepeat()
        {
            var root=colonisationRepeatCodec!.CopyRepeatSave(((ICampaignColonisationRepeatSession)session).ExportRepeatSave());
            LoadColonisationRepeatSource(root);return root;
        }
        private ReviewSaveHandle SaveColonisationRepeatCommitted()
        {var root=ExportColonisationRepeat();var number=checked(nextSave+1);var result=new ReviewSaveHandle(id,number,session.CaptureStatus());colonisationRepeatSaves.Add(number,root);nextSave=number;return result;}
        private ReviewFrame LoadColonisationRepeatCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            if(handle.HostId!=id||!colonisationRepeatSaves.TryGetValue(handle.Id,out var root))throw new ArgumentException("Unknown colonisation checkpoint.");
            var candidate=LoadColonisationRepeatSource(colonisationRepeatCodec!.CopyRepeatSave(root));
            var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureColonisationCandidate(candidate,frame);
            session=candidate;generation=epoch;return frame;
        }
        private ReviewCheckpointResult SaveColonisationRepeatToSlot(string slot,bool overwrite)
        {
            if(colonisationRepeatStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignColonisationRepeatSaveDto root;
            try{root=ExportColonisationRepeat();}catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
            var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
            try{colonisationRepeatStore.Write(slot,root,overwrite);return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
        }
        private ReviewCheckpointResult LoadColonisationRepeatFromSlot(string slot,string actor,string? encounter)
        {
            if(colonisationRepeatStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignColonisationRepeatSaveDto root;
            try{root=colonisationRepeatStore.Read(slot);}catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed);}
            try{var candidate=LoadColonisationRepeatSource(colonisationRepeatCodec!.CopyRepeatSave(root));
                var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureColonisationCandidate(candidate,frame);
                var metadata=new ReviewSlotCheckpoint(slot,frame.Status);session=candidate;generation=epoch;
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
    }
}
