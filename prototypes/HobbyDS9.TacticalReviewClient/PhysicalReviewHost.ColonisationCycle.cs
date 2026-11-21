using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public interface IColonisationCycleReviewHostCodec:IColonisationReviewHostCodec
    {
        CampaignColonisationCyclePackageDto CopyCyclePackage(CampaignColonisationCyclePackageDto package);
        CampaignColonisationCycleSaveDto CopyCycleSave(CampaignColonisationCycleSaveDto root);
    }
    public interface IColonisationCycleCheckpointStore
    {
        void Write(string slot,CampaignColonisationCycleSaveDto root,bool overwrite);
        CampaignColonisationCycleSaveDto Read(string slot);
    }
    public interface IBackgroundColonisationCycleCheckpointCodec:IColonisationCycleReviewHostCodec { }
    public interface IBackgroundColonisationCycleCheckpointStore:IColonisationCycleCheckpointStore { }
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignColonisationCyclePackageDto? colonisationCyclePackage;
        private readonly IColonisationCycleReviewHostCodec? colonisationCycleCodec;
        private readonly IColonisationCycleCheckpointStore? colonisationCycleStore;
        private readonly Dictionary<long,CampaignColonisationCycleSaveDto> colonisationCycleSaves=new Dictionary<long,CampaignColonisationCycleSaveDto>();
        private PhysicalReviewHost(CampaignColonisationCyclePackageDto package,IColonisationCycleReviewHostCodec codec,
            IColonisationCycleCheckpointStore? store,NaturalGalaxySettingsDto? settings,CampaignColonisationCycleSaveDto? saved)
        {
            this.codec=codec;colonisationCycleCodec=codec;colonisationCycleStore=store;colonisationCyclePackage=codec.CopyCyclePackage(package);
            colonisationPackage=colonisationCyclePackage.BaseGrowth!.BaseRosterV2!.BaseV1;colonisationCodec=codec;
            session=saved==null?CampaignColonisationCycleApplication.StartCycleGame(colonisationCyclePackage,settings??throw new ArgumentNullException(nameof(settings)))
                :CampaignColonisationCycleApplication.LoadCycleSave(colonisationCyclePackage,codec.CopyCycleSave(saved));
        }
        public static PhysicalReviewHost StartColonisationCycle(CampaignColonisationCyclePackageDto package,NaturalGalaxySettingsDto settings,
            IColonisationCycleReviewHostCodec codec,IColonisationCycleCheckpointStore? store=null)=>new PhysicalReviewHost(package,codec,store,settings,null);
        public static PhysicalHostOpenResult OpenColonisationCycleFromSlot(CampaignColonisationCyclePackageDto package,
            IColonisationCycleReviewHostCodec codec,IColonisationCycleCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try{var candidate=new PhysicalReviewHost(package,codec,store,null,store.Read(slot));var frame=candidate.Read(actor,encounter);
                candidate.CaptureColonisationCandidate((ICampaignColonisationCycleSession)candidate.session,frame);
                return new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,frame);}
            catch(ReviewCheckpointStoreException e){return new PhysicalHostOpenResult(e.Error);}
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        private ICampaignColonisationCycleSession LoadColonisationCycleSource(CampaignColonisationCycleSaveDto root)
            =>CampaignColonisationCycleApplication.LoadCycleSave(colonisationCyclePackage!,root);
        private CampaignColonisationCycleSaveDto ExportColonisationCycle()
        {
            var root=colonisationCycleCodec!.CopyCycleSave(((ICampaignColonisationCycleSession)session).ExportCycleSave());
            LoadColonisationCycleSource(root);return root;
        }
        private ReviewSaveHandle SaveColonisationCycleCommitted()
        {var root=ExportColonisationCycle();var number=checked(nextSave+1);var result=new ReviewSaveHandle(id,number,session.CaptureStatus());colonisationCycleSaves.Add(number,root);nextSave=number;return result;}
        private ReviewFrame LoadColonisationCycleCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            if(handle.HostId!=id||!colonisationCycleSaves.TryGetValue(handle.Id,out var root))throw new ArgumentException("Unknown colonisation checkpoint.");
            var candidate=LoadColonisationCycleSource(colonisationCycleCodec!.CopyCycleSave(root));
            var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureColonisationCandidate(candidate,frame);
            session=candidate;generation=epoch;return frame;
        }
        private ReviewCheckpointResult SaveColonisationCycleToSlot(string slot,bool overwrite)
        {
            if(colonisationCycleStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignColonisationCycleSaveDto root;
            try{root=ExportColonisationCycle();}catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
            var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
            try{colonisationCycleStore.Write(slot,root,overwrite);return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
        }
        private ReviewCheckpointResult LoadColonisationCycleFromSlot(string slot,string actor,string? encounter)
        {
            if(colonisationCycleStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignColonisationCycleSaveDto root;
            try{root=colonisationCycleStore.Read(slot);}catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed);}
            try{var candidate=LoadColonisationCycleSource(colonisationCycleCodec!.CopyCycleSave(root));
                var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureColonisationCandidate(candidate,frame);
                var metadata=new ReviewSlotCheckpoint(slot,frame.Status);session=candidate;generation=epoch;
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
    }
}
