using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public interface IPlayableFleetReviewHostCodec : IIntegratedReviewHostCodec
    {
        CampaignPlayableFleetPackageDto CopyPackage(CampaignPlayableFleetPackageDto package);
        CampaignPlayableFleetSaveDto CopySave(CampaignPlayableFleetSaveDto save);
    }
    public interface IPlayableFleetReviewCheckpointStore
    {
        void Write(string slot,CampaignPlayableFleetSaveDto privateRoot,bool overwrite);
        CampaignPlayableFleetSaveDto Read(string slot);
    }
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignPlayableFleetPackageDto? playableFleetPackage;
        private readonly IPlayableFleetReviewHostCodec? playableFleetCodec;
        private readonly IPlayableFleetReviewCheckpointStore? playableFleetStore;
        private readonly Dictionary<long,CampaignPlayableFleetSaveDto> playableFleetSaves=new Dictionary<long,CampaignPlayableFleetSaveDto>();
        private PhysicalReviewHost(CampaignPlayableFleetPackageDto package,IPlayableFleetReviewHostCodec codec,IPlayableFleetReviewCheckpointStore? store)
        {
            this.codec=codec;playableFleetCodec=codec;playableFleetPackage=codec.CopyPackage(package);playableFleetStore=store;
            session=CampaignPlayableFleetApplication.StartPackage(playableFleetPackage);
        }
        public static PhysicalReviewHost StartPlayableFleet(CampaignPlayableFleetPackageDto package,IPlayableFleetReviewHostCodec codec,IPlayableFleetReviewCheckpointStore? store=null)
            =>new PhysicalReviewHost(package,codec,store);
        private CampaignPlayableFleetSaveDto ExportPlayableFleet()
        {
            var save=playableFleetCodec!.CopySave(((ICampaignPlayableFleetSession)session).ExportPlayableSave());
            // An adapter that silently drops wrapper5 authority must fail before
            // replacing a previously valid opaque/slot checkpoint.
            CampaignPlayableFleetApplication.LoadSave(playableFleetPackage!,save);
            return save;
        }
        private ReviewSaveHandle SavePlayableFleetCommitted()
        {
            var save=ExportPlayableFleet();var number=checked(nextSave+1);var handle=new ReviewSaveHandle(id,number,session.CaptureStatus());
            playableFleetSaves.Add(number,save);nextSave=number;return handle;
        }
        private ReviewFrame LoadPlayableFleetCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            if(handle.HostId!=id||!playableFleetSaves.TryGetValue(handle.Id,out var save))throw new ArgumentException("Unknown playableFleet checkpoint.");
            var candidate=CampaignPlayableFleetApplication.LoadSave(playableFleetPackage!,playableFleetCodec!.CopySave(save));
            var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);
            session=candidate;generation=epoch;return frame;
        }
        private ReviewCheckpointResult SavePlayableFleetToSlot(string slot,bool overwrite)
        {
            if(playableFleetStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignPlayableFleetSaveDto save;
            try{save=ExportPlayableFleet();}catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
            var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
            try{playableFleetStore.Write(slot,save,overwrite);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
            return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);
        }
        private ReviewCheckpointResult LoadPlayableFleetFromSlot(string slot,string actor,string? encounter)
        {
            if(playableFleetStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignPlayableFleetSaveDto save;
            try{save=playableFleetStore.Read(slot);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed);}
            try
            {
                var candidate=CampaignPlayableFleetApplication.LoadSave(playableFleetPackage!,playableFleetCodec!.CopySave(save));
                var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);
                var metadata=new ReviewSlotCheckpoint(slot,frame.Status);session=candidate;generation=epoch;
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);
            }
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        public static PhysicalHostOpenResult OpenPlayableFleetFromSlot(CampaignPlayableFleetPackageDto package,IPlayableFleetReviewHostCodec codec,
            IPlayableFleetReviewCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try
            {
                var candidate=StartPlayableFleet(package,codec,store);var loaded=candidate.LoadPlayableFleetFromSlot(slot,actor,encounter);
                return loaded.Succeeded?new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,loaded.Frame):new PhysicalHostOpenResult(loaded.Error);
            }
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
    }
}
