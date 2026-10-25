using System;
using System.Collections.Generic;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public interface IPublishedConstructionReviewHostCodec : IPlayableFleetReviewHostCodec,IKnownGeneratedMapCodec
    {
        CampaignPublishedConstructionPackageDto CopyPackage(CampaignPublishedConstructionPackageDto package);
        CampaignPublishedConstructionSaveDto CopySave(CampaignPublishedConstructionSaveDto root);
    }
    public interface IPublishedConstructionCheckpointStore
    {
        void Write(string slot,CampaignPublishedConstructionSaveDto root,bool overwrite);
        CampaignPublishedConstructionSaveDto Read(string slot);
    }
    public interface IBackgroundPublishedCheckpointCodec : IPublishedConstructionReviewHostCodec { }
    public interface IBackgroundPublishedCheckpointStore : IPublishedConstructionCheckpointStore { }
    public sealed partial class PhysicalReviewHost
    {
        private readonly CampaignPublishedConstructionPackageDto? publishedPackage;
        private readonly IPublishedConstructionReviewHostCodec? publishedCodec;
        private readonly IPublishedConstructionCheckpointStore? publishedStore;
        private readonly Dictionary<long,CampaignPublishedConstructionSaveDto> publishedSaves=new Dictionary<long,CampaignPublishedConstructionSaveDto>();
        private PhysicalReviewHost(CampaignPublishedConstructionPackageDto package,IPublishedConstructionReviewHostCodec codec,
            IPublishedConstructionCheckpointStore? store,NaturalGalaxySettingsDto? settings,CampaignPublishedConstructionSaveDto? saved)
        {
            this.codec=codec;publishedCodec=codec;publishedStore=store;publishedPackage=codec.CopyPackage(package);
            session=saved==null?CampaignPublishedConstructionApplication.StartGame(publishedPackage,settings??throw new ArgumentNullException(nameof(settings)))
                :CampaignPublishedConstructionApplication.LoadSave(publishedPackage,codec.CopySave(saved));
        }
        public static PhysicalReviewHost StartPublishedConstruction(CampaignPublishedConstructionPackageDto package,NaturalGalaxySettingsDto settings,
            IPublishedConstructionReviewHostCodec codec,IPublishedConstructionCheckpointStore? store=null)=>new PhysicalReviewHost(package,codec,store,settings,null);
        public static PhysicalHostOpenResult OpenPublishedConstructionFromSlot(CampaignPublishedConstructionPackageDto package,
            IPublishedConstructionReviewHostCodec codec,IPublishedConstructionCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try{var candidate=new PhysicalReviewHost(package,codec,store,null,store.Read(slot));var frame=candidate.Read(actor,encounter);
                candidate.CapturePublishedCandidate((ICampaignPublishedConstructionSession)candidate.session,frame);
                return new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,frame);}
            catch(ReviewCheckpointStoreException e){return new PhysicalHostOpenResult(e.Error);}
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        private GeneratedCampaignPresentation CapturePublishedCandidate(ICampaignPublishedConstructionSession candidate,ReviewFrame frame)
            =>new GeneratedCampaignPresentation(frame,candidate.CaptureGeneratedMap(frame.Actor),candidate.CaptureKnownAstronomy(frame.Actor),publishedCodec!);
        public CampaignPublishedConstructionEligibility CaptureConstructionEligibility(ReviewFrame frame,string blueprint,int revision,string colony)
        {
            lock(gate){RequireCurrent(frame.HostId,frame.Generation);var status=session.CaptureStatus();
                if(publishedPackage==null||frame.Status.Turn!=status.Turn||frame.Status.CampaignRevision!=status.CampaignRevision)
                    throw new InvalidOperationException("Construction observation changed.");
                return ((ICampaignPublishedConstructionSession)session).CaptureConstructionEligibility(frame.Actor,blueprint,revision,colony);}
        }
        private CampaignPublishedConstructionSaveDto ExportPublished()
        {
            var root=publishedCodec!.CopySave(((ICampaignPublishedConstructionSession)session).ExportPublishedConstructionSave());
            CampaignPublishedConstructionApplication.LoadSave(publishedPackage!,root);return root;
        }
        private ReviewSaveHandle SavePublishedCommitted()
        {var root=ExportPublished();var number=checked(nextSave+1);var result=new ReviewSaveHandle(id,number,session.CaptureStatus());publishedSaves.Add(number,root);nextSave=number;return result;}
        private ReviewFrame LoadPublishedCommitted(ReviewSaveHandle handle,string actor,string? encounter)
        {
            if(handle.HostId!=id||!publishedSaves.TryGetValue(handle.Id,out var root))throw new ArgumentException("Unknown published checkpoint.");
            var candidate=CampaignPublishedConstructionApplication.LoadSave(publishedPackage!,publishedCodec!.CopySave(root));
            var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CapturePublishedCandidate(candidate,frame);
            session=candidate;generation=epoch;return frame;
        }
        private ReviewCheckpointResult SavePublishedToSlot(string slot,bool overwrite)
        {
            if(publishedStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignPublishedConstructionSaveDto root;
            try{root=ExportPublished();}catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
            var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
            try{publishedStore.Write(slot,root,overwrite);return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);}
            catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
        }
        private ReviewCheckpointResult LoadPublishedFromSlot(string slot,string actor,string? encounter)
        {
            if(publishedStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
            CampaignPublishedConstructionSaveDto root;
            try{root=publishedStore.Read(slot);}catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed);}
            try{var candidate=CampaignPublishedConstructionApplication.LoadSave(publishedPackage!,publishedCodec!.CopySave(root));
                var epoch=checked(generation+1);var frame=Capture(candidate,actor,encounter,epoch);CapturePublishedCandidate(candidate,frame);
                var metadata=new ReviewSlotCheckpoint(slot,frame.Status);session=candidate;generation=epoch;
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);}
            catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
    }
}
