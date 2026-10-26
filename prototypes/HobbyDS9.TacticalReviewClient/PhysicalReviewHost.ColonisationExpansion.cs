using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public interface IColonisationExpansionReviewHostCodec:IColonisationReviewHostCodec
    {
        CampaignColonisationExpansionPackageDto CopyExpansionPackage(CampaignColonisationExpansionPackageDto package);
        CampaignColonisationExpansionSaveDto CopyExpansionSave(CampaignColonisationExpansionSaveDto root);
    }
    public interface IColonisationExpansionCheckpointStore
    {
        void Write(string slot,CampaignColonisationExpansionSaveDto root,bool overwrite);
        CampaignColonisationExpansionSaveDto Read(string slot);
    }
    public interface IBackgroundColonisationExpansionCheckpointCodec:IColonisationExpansionReviewHostCodec { }
    public interface IBackgroundColonisationExpansionCheckpointStore:IColonisationExpansionCheckpointStore { }
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignColonisationExpansionPackageDto? colonisationExpansionPackage;
        private readonly IColonisationExpansionReviewHostCodec? colonisationExpansionCodec;
        private readonly IColonisationExpansionCheckpointStore? colonisationExpansionStore;
        private readonly Dictionary<long,CampaignColonisationExpansionSaveDto> colonisationExpansionSaves=new Dictionary<long,CampaignColonisationExpansionSaveDto>();
        private PhysicalReviewHost(CampaignColonisationExpansionPackageDto package,IColonisationExpansionReviewHostCodec codec,
            IColonisationExpansionCheckpointStore? store,NaturalGalaxySettingsDto? settings,CampaignColonisationExpansionSaveDto? saved)
        {
            this.codec=codec;colonisationExpansionCodec=codec;colonisationExpansionStore=store;colonisationExpansionPackage=codec.CopyExpansionPackage(package);
            colonisationPackage=colonisationExpansionPackage.BaseRosterV2!.BaseV1;colonisationCodec=codec;
            session=saved==null?CampaignColonisationExpansionApplication.StartGame(colonisationExpansionPackage,settings??throw new ArgumentNullException(nameof(settings)))
                :CampaignColonisationExpansionApplication.LoadSave(colonisationExpansionPackage,codec.CopyExpansionSave(saved));
        }
        public static PhysicalReviewHost StartColonisationExpansion(CampaignColonisationExpansionPackageDto package,NaturalGalaxySettingsDto settings,
            IColonisationExpansionReviewHostCodec codec,IColonisationExpansionCheckpointStore? store=null)=>new PhysicalReviewHost(package,codec,store,settings,null);
        public static PhysicalHostOpenResult OpenColonisationExpansionFromSlot(CampaignColonisationExpansionPackageDto package,
            IColonisationExpansionReviewHostCodec codec,IColonisationExpansionCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try{var candidate=new PhysicalReviewHost(package,codec,store,null,store.Read(slot));var frame=candidate.Read(actor,encounter);
                candidate.CaptureColonisationCandidate((ICampaignColonisationExpansionSession)candidate.session,frame);
                return new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,frame);}
            catch(ReviewCheckpointStoreException e){return new PhysicalHostOpenResult(e.Error);}
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        private ICampaignColonisationExpansionSession LoadColonisationExpansionSource(CampaignColonisationExpansionSaveDto root)
            =>CampaignColonisationExpansionApplication.LoadSave(colonisationExpansionPackage!,root);
        private CampaignColonisationExpansionSaveDto ExportColonisationExpansion()
        {
            var root=colonisationExpansionCodec!.CopyExpansionSave(((ICampaignColonisationExpansionSession)session).ExportColonisationExpansionSave());
            LoadColonisationExpansionSource(root);return root;
        }
        private ReviewSaveHandle SaveColonisationExpansionCommitted()
        {var root=ExportColonisationExpansion();var number=checked(nextSave+1);var result=new ReviewSaveHandle(id,number,session.CaptureStatus());colonisationExpansionSaves.Add(number,root);nextSave=number;return result;}
        private ReviewFrame LoadColonisationExpansionCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            if(handle.HostId!=id||!colonisationExpansionSaves.TryGetValue(handle.Id,out var root))throw new ArgumentException("Unknown colonisation checkpoint.");
            var candidate=LoadColonisationExpansionSource(colonisationExpansionCodec!.CopyExpansionSave(root));
            var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureColonisationCandidate(candidate,frame);
            session=candidate;generation=epoch;return frame;
        }
        private ReviewCheckpointResult SaveColonisationExpansionToSlot(string slot,bool overwrite)
        {
            if(colonisationExpansionStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignColonisationExpansionSaveDto root;
            try{root=ExportColonisationExpansion();}catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
            var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
            try{colonisationExpansionStore.Write(slot,root,overwrite);return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
        }
        private ReviewCheckpointResult LoadColonisationExpansionFromSlot(string slot,string actor,string? encounter)
        {
            if(colonisationExpansionStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignColonisationExpansionSaveDto root;
            try{root=colonisationExpansionStore.Read(slot);}catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed);}
            try{var candidate=LoadColonisationExpansionSource(colonisationExpansionCodec!.CopyExpansionSave(root));
                var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureColonisationCandidate(candidate,frame);
                var metadata=new ReviewSlotCheckpoint(slot,frame.Status);session=candidate;generation=epoch;
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
    }
}

