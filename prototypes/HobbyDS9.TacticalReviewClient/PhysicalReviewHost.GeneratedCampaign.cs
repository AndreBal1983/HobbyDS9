using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public interface IGeneratedCampaignReviewHostCodec : IPlayableFleetReviewHostCodec
    {
        CampaignGeneratedSaveDto CopySave(CampaignGeneratedSaveDto root);
    }
    public interface IGeneratedCampaignReviewCheckpointStore
    {
        void Write(string slot,CampaignGeneratedSaveDto root,bool overwrite);
        CampaignGeneratedSaveDto Read(string slot);
    }
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignPlayableFleetPackageDto? generatedTemplate;
        private readonly IGeneratedCampaignReviewHostCodec? generatedCodec;
        private readonly IGeneratedCampaignReviewCheckpointStore? generatedStore;
        private readonly Dictionary<long,CampaignGeneratedSaveDto> generatedSaves=new Dictionary<long,CampaignGeneratedSaveDto>();
        private PhysicalReviewHost(CampaignPlayableFleetPackageDto template,IGeneratedCampaignReviewHostCodec codec,
            IGeneratedCampaignReviewCheckpointStore? store,NaturalGalaxySettingsDto? settings,CampaignGeneratedSaveDto? saved)
        {
            this.codec=codec;generatedCodec=codec;generatedTemplate=codec.CopyPackage(template);generatedStore=store;
            // Continue goes straight through complete frozen-map validation; never StartGame/Generate.
            session=saved!=null?CampaignGeneratedApplication.LoadSave(generatedTemplate,codec.CopySave(saved))
                :CampaignGeneratedApplication.StartGame(generatedTemplate,settings??throw new ArgumentNullException(nameof(settings)));
        }
        public static PhysicalReviewHost StartGeneratedCampaign(CampaignPlayableFleetPackageDto template,NaturalGalaxySettingsDto settings,
            IGeneratedCampaignReviewHostCodec codec,IGeneratedCampaignReviewCheckpointStore? store=null)
            =>new PhysicalReviewHost(template,codec,store,settings,null);
        public CampaignGeneratedMapDto CaptureGeneratedMap(ReviewFrame observed)
        {
            lock(gate){RequireCurrent(observed.HostId,observed.Generation);
                var view=((ICampaignGeneratedSession)session).CaptureGeneratedMap(observed.Actor);
                if(view.ObservationTurn!=observed.Status.Turn||view.CampaignRevision!=observed.Status.CampaignRevision)throw new InvalidOperationException("Generated map observation is stale.");
                return view;}
        }
        private CampaignGeneratedSaveDto ExportGenerated()
        {
            var save=generatedCodec!.CopySave(((ICampaignGeneratedSession)session).ExportGeneratedSave());
            CampaignGeneratedApplication.LoadSave(generatedTemplate!,save);return save;
        }
        private ReviewSaveHandle SaveGeneratedCommitted()
        {
            var save=ExportGenerated();var number=checked(nextSave+1);var handle=new ReviewSaveHandle(id,number,session.CaptureStatus());
            generatedSaves.Add(number,save);nextSave=number;return handle;
        }
        private ReviewFrame LoadGeneratedCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            if(handle.HostId!=id||!generatedSaves.TryGetValue(handle.Id,out var save))throw new ArgumentException("Unknown generated checkpoint.");
            var candidate=CampaignGeneratedApplication.LoadSave(generatedTemplate!,generatedCodec!.CopySave(save));
            var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);
            session=candidate;generation=epoch;return frame;
        }
        private ReviewCheckpointResult SaveGeneratedToSlot(string slot,bool overwrite)
        {
            if(generatedStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignGeneratedSaveDto save;
            try{save=ExportGenerated();}catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
            var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
            try{generatedStore.Write(slot,save,overwrite);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
            return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);
        }
        private ReviewCheckpointResult LoadGeneratedFromSlot(string slot,string actor,string? encounter)
        {
            if(generatedStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignGeneratedSaveDto save;
            try{save=generatedStore.Read(slot);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed);}
            try
            {
                var candidate=CampaignGeneratedApplication.LoadSave(generatedTemplate!,generatedCodec!.CopySave(save));
                var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);
                var metadata=new ReviewSlotCheckpoint(slot,frame.Status);session=candidate;generation=epoch;
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);
            }
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        public static PhysicalHostOpenResult OpenGeneratedCampaignFromSlot(CampaignPlayableFleetPackageDto template,
            IGeneratedCampaignReviewHostCodec codec,IGeneratedCampaignReviewCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try
            {
                var saved=store.Read(slot);
                var candidate=new PhysicalReviewHost(template,codec,store,null,saved);
                var frame=candidate.Read(actor,encounter);
                return new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,frame);
            }
            catch(ReviewCheckpointStoreException e){return new PhysicalHostOpenResult(e.Error);}
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
    }
}
