using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
namespace HobbyDS9.TacticalReviewClient
{
    public interface ISingleplayerDemoHostCodec : IIntegratedReviewHostCodec
    {
        CampaignSingleplayerDemoPackageDto CopyPackage(CampaignSingleplayerDemoPackageDto package);
        CampaignSingleplayerDemoSaveDto CopySave(CampaignSingleplayerDemoSaveDto save);
    }
    public interface ISingleplayerDemoCheckpointStore
    {
        void Write(string slot,CampaignSingleplayerDemoSaveDto privateRoot,bool overwrite);
        CampaignSingleplayerDemoSaveDto Read(string slot);
    }
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignSingleplayerDemoPackageDto? demoPackage;
        private readonly ISingleplayerDemoHostCodec? demoCodec;
        private readonly ISingleplayerDemoCheckpointStore? demoStore;
        private readonly Dictionary<long,CampaignSingleplayerDemoSaveDto> demoSaves=new Dictionary<long,CampaignSingleplayerDemoSaveDto>();
        private PhysicalReviewHost(CampaignSingleplayerDemoPackageDto package,ISingleplayerDemoHostCodec codec,ISingleplayerDemoCheckpointStore? store)
        {
            this.codec=codec;demoCodec=codec;demoPackage=codec.CopyPackage(package);demoStore=store;
            session=CampaignSingleplayerDemoApplication.StartPackage(demoPackage);
        }
        public static PhysicalReviewHost StartDemo(CampaignSingleplayerDemoPackageDto package,ISingleplayerDemoHostCodec codec,ISingleplayerDemoCheckpointStore? store=null)
            =>new PhysicalReviewHost(package,codec,store);
        private CampaignSingleplayerDemoSaveDto ExportDemo()=>demoCodec!.CopySave(((ICampaignSingleplayerDemoSession)session).ExportDemoSave());
        private ReviewSaveHandle SaveDemoCommitted()
        {
            var save=ExportDemo();var number=checked(nextSave+1);var handle=new ReviewSaveHandle(id,number,session.CaptureStatus());
            demoSaves.Add(number,save);nextSave=number;return handle;
        }
        private ReviewFrame LoadDemoCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            if(handle.HostId!=id||!demoSaves.TryGetValue(handle.Id,out var save))throw new ArgumentException("Unknown demo checkpoint.");
            var candidate=CampaignSingleplayerDemoApplication.LoadSave(demoPackage!,demoCodec!.CopySave(save));
            var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);
            session=candidate;generation=epoch;return frame;
        }
        private ReviewCheckpointResult SaveDemoToSlot(string slot,bool overwrite)
        {
            if(demoStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignSingleplayerDemoSaveDto save;
            try{save=ExportDemo();}catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
            var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
            try{demoStore.Write(slot,save,overwrite);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
            return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);
        }
        private ReviewCheckpointResult LoadDemoFromSlot(string slot,string actor,string? encounter)
        {
            if(demoStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignSingleplayerDemoSaveDto save;
            try{save=demoStore.Read(slot);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed);}
            try
            {
                var candidate=CampaignSingleplayerDemoApplication.LoadSave(demoPackage!,demoCodec!.CopySave(save));
                var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);
                var metadata=new ReviewSlotCheckpoint(slot,frame.Status);session=candidate;generation=epoch;
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);
            }
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        public static PhysicalHostOpenResult OpenDemoFromSlot(CampaignSingleplayerDemoPackageDto package,ISingleplayerDemoHostCodec codec,
            ISingleplayerDemoCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try
            {
                var candidate=StartDemo(package,codec,store);var loaded=candidate.LoadDemoFromSlot(slot,actor,encounter);
                return loaded.Succeeded?new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,loaded.Frame):new PhysicalHostOpenResult(loaded.Error);
            }
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
    }
}
