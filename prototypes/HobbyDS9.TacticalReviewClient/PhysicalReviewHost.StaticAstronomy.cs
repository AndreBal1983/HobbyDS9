using System;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public sealed partial class PhysicalReviewHost
    {
        private sealed class StaticStoreAdapter : IGeneratedCampaignReviewCheckpointStore,IReviewCheckpointWriteAdmission
        {
            private readonly IStaticAstronomyReviewCheckpointStore store;
            internal IStaticAstronomyReviewCheckpointStore Original=>store;
            internal StaticStoreAdapter(IStaticAstronomyReviewCheckpointStore store){this.store=store;}
            public void Write(string slot,CampaignGeneratedSaveDto root,bool overwrite)=>store.Write(slot,root,overwrite);
            public CampaignGeneratedSaveDto Read(string slot)=>store.Read(slot);
            public bool TryCheckWriteAdmission(string slot,out ReviewCheckpointError error)
            {
                if(store is IReviewCheckpointWriteAdmission admission)return admission.TryCheckWriteAdmission(slot,out error);
                error=ReviewCheckpointError.None;return false;
            }
        }
        public static PhysicalReviewHost StartStaticAstronomyCampaign(CampaignPlayableFleetPackageDto template,NaturalGalaxySettingsDto settings,
            IStaticAstronomyReviewHostCodec codec,IStaticAstronomyReviewCheckpointStore? store=null)
            =>new PhysicalReviewHost(template,codec,store==null?null:new StaticStoreAdapter(store),settings,null,true);
        public static PhysicalHostOpenResult OpenStaticAstronomyCampaignFromSlot(CampaignPlayableFleetPackageDto template,
            IStaticAstronomyReviewHostCodec codec,IStaticAstronomyReviewCheckpointStore store,string slot,string actor,string? encounter=null)
        {
            try
            {
                var candidate=new PhysicalReviewHost(template,codec,new StaticStoreAdapter(store),null,store.Read(slot),true);
                var frame=candidate.Read(actor,encounter);
                candidate.CaptureGeneratedPresentation(frame);
                return new PhysicalHostOpenResult(ReviewCheckpointError.None,candidate,frame);
            }
            catch(ReviewCheckpointStoreException e){return new PhysicalHostOpenResult(e.Error);}
            catch(Exception){return new PhysicalHostOpenResult(ReviewCheckpointError.InvalidCheckpoint);}
        }
        public GeneratedCampaignPresentation CaptureGeneratedPresentation(ReviewFrame observed)
        {
            lock(gate)
            {
                RequireCurrent(observed.HostId,observed.Generation);
                if(activitiesPackage!=null)return CaptureActivitiesCandidate((ICampaignActivitiesSession)session,observed);
                if(publishedPackage!=null)return CapturePublishedCandidate((ICampaignPublishedConstructionSession)session,observed);
                if(!staticAstronomy || session is not ICampaignStaticAstronomySession current)
                    throw new InvalidOperationException("Static astronomy is not available in this campaign profile.");
                return CaptureStaticCandidate(current,observed);
            }
        }
        private GeneratedCampaignPresentation CaptureStaticCandidate(ICampaignGeneratedSession candidate,ReviewFrame frame)
        {
            var current=(ICampaignStaticAstronomySession)candidate;
            return new GeneratedCampaignPresentation(frame,current.CaptureGeneratedMap(frame.Actor),
                current.CaptureKnownAstronomy(frame.Actor),(IStaticAstronomyReviewHostCodec)generatedCodec!);
        }
        public void ValidateGeneratedPresentation(ReviewFrame observed,GeneratedCampaignPresentation presentation)
        {
            lock(gate)
            {
                RequireCurrent(observed.HostId,observed.Generation);
                RequireCurrent(presentation.HostId,presentation.Generation);
                if(!staticAstronomy&&publishedPackage==null&&activitiesPackage==null)throw new InvalidOperationException("Wrong campaign profile.");
                var status=session.CaptureStatus();
                if(presentation.Actor!=observed.Actor || presentation.Frame.Status.Turn!=status.Turn
                    || presentation.Frame.Status.CampaignRevision!=status.CampaignRevision
                    || observed.Status.Turn!=status.Turn || observed.Status.CampaignRevision!=status.CampaignRevision)
                    throw new InvalidOperationException("Stale generated presentation.");
            }
        }
    }
}
