using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public interface IFrontierReviewHostCodec : IIntegratedReviewHostCodec
    {
        CampaignFrontierPackageDto CopyPackage(CampaignFrontierPackageDto package);
        CampaignFrontierSaveDto CopySave(CampaignFrontierSaveDto save);
    }
    public interface IFrontierReviewCheckpointStore
    {
        void Write(string slot,CampaignFrontierSaveDto privateRoot,bool overwrite);
        CampaignFrontierSaveDto Read(string slot);
    }
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignFrontierPackageDto? frontierPackage;
        private readonly IFrontierReviewHostCodec? frontierCodec;
        private readonly IFrontierReviewCheckpointStore? frontierStore;
        private readonly Dictionary<long,CampaignFrontierSaveDto> frontierSaves=new Dictionary<long,CampaignFrontierSaveDto>();
        private PhysicalReviewHost(CampaignFrontierPackageDto package,IFrontierReviewHostCodec codec,IFrontierReviewCheckpointStore? store)
        {
            this.codec=codec;frontierCodec=codec;frontierPackage=codec.CopyPackage(package);frontierStore=store;
            session=CampaignFrontierApplication.StartPackage(frontierPackage);
        }
        public static PhysicalReviewHost StartFrontier(CampaignFrontierPackageDto package,IFrontierReviewHostCodec codec,IFrontierReviewCheckpointStore? store=null)
            =>new PhysicalReviewHost(package,codec,store);
        private CampaignFrontierSaveDto ExportFrontier()
        {
            var save=frontierCodec!.CopySave(((ICampaignFrontierSession)session).ExportFrontierSave());
            // An adapter that silently drops wrapper5 authority must fail before
            // replacing a previously valid opaque/slot checkpoint.
            CampaignFrontierApplication.LoadSave(frontierPackage!,save);
            return save;
        }
        private ReviewSaveHandle SaveFrontierCommitted()
        {
            var save=ExportFrontier();var number=checked(nextSave+1);var handle=new ReviewSaveHandle(id,number,session.CaptureStatus());
            frontierSaves.Add(number,save);nextSave=number;return handle;
        }
        private ReviewFrame LoadFrontierCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            if(handle.HostId!=id||!frontierSaves.TryGetValue(handle.Id,out var save))throw new ArgumentException("Unknown frontier checkpoint.");
            var candidate=CampaignFrontierApplication.LoadSave(frontierPackage!,frontierCodec!.CopySave(save));
            var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);
            session=candidate;generation=epoch;return frame;
        }
        private ReviewCheckpointResult SaveFrontierToSlot(string slot,bool overwrite)
        {
            if(frontierStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignFrontierSaveDto save;
            try{save=ExportFrontier();}catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
            var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
            try{frontierStore.Write(slot,save,overwrite);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
            return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);
        }
        private ReviewCheckpointResult LoadFrontierFromSlot(string slot,string actor,string? encounter)
        {
            if(frontierStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignFrontierSaveDto save;
            try{save=frontierStore.Read(slot);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed);}
            try
            {
                var candidate=CampaignFrontierApplication.LoadSave(frontierPackage!,frontierCodec!.CopySave(save));
                var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);
                var metadata=new ReviewSlotCheckpoint(slot,frame.Status);session=candidate;generation=epoch;
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);
            }
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        public static PhysicalHostOpenResult OpenFrontierFromSlot(CampaignFrontierPackageDto package,IFrontierReviewHostCodec codec,
            IFrontierReviewCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try
            {
                var candidate=StartFrontier(package,codec,store);var loaded=candidate.LoadFrontierFromSlot(slot,actor,encounter);
                return loaded.Succeeded?new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,loaded.Frame):new PhysicalHostOpenResult(loaded.Error);
            }
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
    }
}
