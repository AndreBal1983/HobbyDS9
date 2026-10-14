using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public interface IIntegratedReviewHostCodec : IPhysicalReviewHostCodec
    {
        CampaignIntegratedPlaytestPackageDto CopyPackage(CampaignIntegratedPlaytestPackageDto package);
        CampaignIntegratedPlaytestSaveDto CopySave(CampaignIntegratedPlaytestSaveDto save);
    }
    public interface IIntegratedReviewCheckpointStore
    {
        void Write(string slot,CampaignIntegratedPlaytestSaveDto privateRoot,bool overwrite);
        CampaignIntegratedPlaytestSaveDto Read(string slot);
    }
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignIntegratedPlaytestPackageDto? integratedPackage;
        private readonly IIntegratedReviewHostCodec? integratedCodec;
        private readonly IIntegratedReviewCheckpointStore? integratedStore;
        private readonly Dictionary<long,CampaignIntegratedPlaytestSaveDto> integratedSaves=new Dictionary<long,CampaignIntegratedPlaytestSaveDto>();
        private PhysicalReviewHost(CampaignIntegratedPlaytestPackageDto package,IIntegratedReviewHostCodec codec,IIntegratedReviewCheckpointStore? store)
        {
            this.codec=codec;integratedCodec=codec;integratedPackage=codec.CopyPackage(package);integratedStore=store;
            session=CampaignIntegratedPlaytestApplication.StartPackage(integratedPackage);
        }
        public static PhysicalReviewHost StartIntegrated(CampaignIntegratedPlaytestPackageDto package,IIntegratedReviewHostCodec codec,IIntegratedReviewCheckpointStore? store=null)
            =>new PhysicalReviewHost(package,codec,store);
        public CampaignTacticalMutationResult SubmitStrategicCommand(ReviewFrame observed,CommandEnvelope command)
        {
            lock(gate)
            {
                RequireCurrent(observed.HostId,observed.Generation);
                if(command.ActorCivilizationId!=observed.Actor)return new CampaignTacticalMutationResult(CampaignTacticalError.ActorNotControlled,session.CaptureStatus().CampaignRevision);
                return session.SubmitStrategicCommand(command);
            }
        }
        private CampaignIntegratedPlaytestSaveDto ExportIntegrated()
            =>integratedCodec!.CopySave(((ICampaignIntegratedPlaytestSession)session).ExportIntegratedSave());
        private ReviewSaveHandle SaveIntegratedCommitted()
        {
            var save=ExportIntegrated();var number=checked(nextSave+1);var handle=new ReviewSaveHandle(id,number,session.CaptureStatus());
            integratedSaves.Add(number,save);nextSave=number;return handle;
        }
        private ReviewFrame LoadIntegratedCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            if(handle.HostId!=id||!integratedSaves.TryGetValue(handle.Id,out var save))throw new ArgumentException("Unknown integrated checkpoint.");
            var candidate=CampaignIntegratedPlaytestApplication.LoadSave(integratedPackage!,integratedCodec!.CopySave(save));
            var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);
            session=candidate;generation=epoch;return frame;
        }
        private ReviewCheckpointResult SaveIntegratedToSlot(string slot,bool overwrite)
        {
            if(integratedStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignIntegratedPlaytestSaveDto save;
            try{save=ExportIntegrated();}catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
            var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
            try{integratedStore.Write(slot,save,overwrite);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
            return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);
        }
        private ReviewCheckpointResult LoadIntegratedFromSlot(string slot,string actor,string? encounter)
        {
            if(integratedStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignIntegratedPlaytestSaveDto save;
            try{save=integratedStore.Read(slot);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed);}
            try
            {
                var candidate=CampaignIntegratedPlaytestApplication.LoadSave(integratedPackage!,integratedCodec!.CopySave(save));
                var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);
                var metadata=new ReviewSlotCheckpoint(slot,frame.Status);session=candidate;generation=epoch;
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);
            }
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        public static PhysicalHostOpenResult OpenIntegratedFromSlot(CampaignIntegratedPlaytestPackageDto package,IIntegratedReviewHostCodec codec,
            IIntegratedReviewCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try
            {
                var candidate=StartIntegrated(package,codec,store);var loaded=candidate.LoadIntegratedFromSlot(slot,actor,encounter);
                return loaded.Succeeded?new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,loaded.Frame):new PhysicalHostOpenResult(loaded.Error);
            }
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
    }
}
