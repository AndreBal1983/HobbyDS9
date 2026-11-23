using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public interface IColonisationGrowthReviewHostCodec:IColonisationReviewHostCodec
    {
        CampaignColonisationExpansionPackageDto CopyGrowthPackage(CampaignColonisationExpansionPackageDto package);
        CampaignColonisationExpansionSaveDto CopyGrowthSave(CampaignColonisationExpansionSaveDto root);
    }
    public interface IColonisationGrowthCheckpointStore
    {
        void Write(string slot,CampaignColonisationExpansionSaveDto root,bool overwrite);
        CampaignColonisationExpansionSaveDto Read(string slot);
    }
    public interface IBackgroundColonisationGrowthCheckpointCodec:IColonisationGrowthReviewHostCodec { }
    public interface IBackgroundColonisationGrowthCheckpointStore:IColonisationGrowthCheckpointStore { }
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignColonisationExpansionPackageDto? colonisationGrowthPackage;
        private readonly IColonisationGrowthReviewHostCodec? colonisationGrowthCodec;
        private readonly IColonisationGrowthCheckpointStore? colonisationGrowthStore;
        private readonly Dictionary<long,CampaignColonisationExpansionSaveDto> colonisationGrowthSaves=new Dictionary<long,CampaignColonisationExpansionSaveDto>();
        private PhysicalReviewHost(CampaignColonisationExpansionPackageDto package,IColonisationGrowthReviewHostCodec codec,
            IColonisationGrowthCheckpointStore? store,NaturalGalaxySettingsDto? settings,CampaignColonisationExpansionSaveDto? saved)
        {
            this.codec=codec;colonisationGrowthCodec=codec;colonisationGrowthStore=store;colonisationGrowthPackage=codec.CopyGrowthPackage(package);
            colonisationPackage=colonisationGrowthPackage.BaseRosterV2!.BaseV1;colonisationCodec=codec;
            session=saved==null?CampaignColonisationGrowthApplication.StartGrowthGame(colonisationGrowthPackage,settings??throw new ArgumentNullException(nameof(settings)))
                :CampaignColonisationGrowthApplication.LoadGrowthSave(colonisationGrowthPackage,codec.CopyGrowthSave(saved));
        }
        public static PhysicalReviewHost StartColonisationGrowth(CampaignColonisationExpansionPackageDto package,NaturalGalaxySettingsDto settings,
            IColonisationGrowthReviewHostCodec codec,IColonisationGrowthCheckpointStore? store=null)=>new PhysicalReviewHost(package,codec,store,settings,null);
        public static PhysicalHostOpenResult OpenColonisationGrowthFromSlot(CampaignColonisationExpansionPackageDto package,
            IColonisationGrowthReviewHostCodec codec,IColonisationGrowthCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try{var candidate=new PhysicalReviewHost(package,codec,store,null,store.Read(slot));var frame=candidate.Read(actor,encounter);
                candidate.CaptureColonisationCandidate((ICampaignColonisationGrowthSession)candidate.session,frame);
                return new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,frame);}
            catch(ReviewCheckpointStoreException e){return new PhysicalHostOpenResult(e.Error);}
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        private ICampaignColonisationGrowthSession LoadColonisationGrowthSource(CampaignColonisationExpansionSaveDto root)
            =>CampaignColonisationGrowthApplication.LoadGrowthSave(colonisationGrowthPackage!,root);
        private CampaignColonisationExpansionSaveDto ExportColonisationGrowth()
        {
            var root=colonisationGrowthCodec!.CopyGrowthSave(((ICampaignColonisationGrowthSession)session).ExportGrowthSave());
            LoadColonisationGrowthSource(root);return root;
        }
        private ReviewSaveHandle SaveColonisationGrowthCommitted()
        {var root=ExportColonisationGrowth();var number=checked(nextSave+1);var result=new ReviewSaveHandle(id,number,session.CaptureStatus());colonisationGrowthSaves.Add(number,root);nextSave=number;return result;}
        private ReviewFrame LoadColonisationGrowthCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            if(handle.HostId!=id||!colonisationGrowthSaves.TryGetValue(handle.Id,out var root))throw new ArgumentException("Unknown colonisation checkpoint.");
            var candidate=LoadColonisationGrowthSource(colonisationGrowthCodec!.CopyGrowthSave(root));
            var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureColonisationCandidate(candidate,frame);
            session=candidate;generation=epoch;return frame;
        }
        private ReviewCheckpointResult SaveColonisationGrowthToSlot(string slot,bool overwrite)
        {
            if(colonisationGrowthStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignColonisationExpansionSaveDto root;
            try{root=ExportColonisationGrowth();}catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
            var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
            try{colonisationGrowthStore.Write(slot,root,overwrite);return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
        }
        private ReviewCheckpointResult LoadColonisationGrowthFromSlot(string slot,string actor,string? encounter)
        {
            if(colonisationGrowthStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignColonisationExpansionSaveDto root;
            try{root=colonisationGrowthStore.Read(slot);}catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed);}
            try{var candidate=LoadColonisationGrowthSource(colonisationGrowthCodec!.CopyGrowthSave(root));
                var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CaptureColonisationCandidate(candidate,frame);
                var metadata=new ReviewSlotCheckpoint(slot,frame.Status);session=candidate;generation=epoch;
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
    }
}
