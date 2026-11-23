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
                if(colonisationInteraction!=null)
                {
                    if(ReferenceEquals(session,colonisationInteraction.Source)&&ReferenceEquals(session,colonisationPresentationSource)&&ReferenceEquals(observed,colonisationInteraction.SourceFrame)&&colonisationCommittedPresentation!=null&&ReferenceEquals(colonisationCommittedPresentation.Frame,observed))return colonisationCommittedPresentation;
                    throw new InvalidOperationException("Wait for the current colonisation interaction.");
                }
                if(streamedInteraction!=null)
                {
                    if(ReferenceEquals(session,streamedInteraction.Source)&&ReferenceEquals(session,streamedPresentationSource)&&ReferenceEquals(observed,streamedInteraction.SourceFrame)&&streamedCommittedPresentation!=null&&ReferenceEquals(streamedCommittedPresentation.Frame,observed))return streamedCommittedPresentation;
                    throw new InvalidOperationException("Wait for the current streamed interaction.");
                }
                RequireCurrent(observed.HostId,observed.Generation);
                if(streamedActivitiesPackage!=null)
                {
                    var status=session.CaptureStatus();
                    if(observed.Status.Turn!=status.Turn||observed.Status.CampaignRevision!=status.CampaignRevision)throw new InvalidOperationException("Stale generated presentation.");
                }
                if(streamedActivitiesPackage!=null&&ReferenceEquals(session,streamedPresentationSource)&&streamedCommittedPresentation!=null&&ReferenceEquals(streamedCommittedPresentation.Frame,observed))return streamedCommittedPresentation;
                if(streamedActivitiesPackage!=null){streamedPresentationSource=(ICampaignStreamedActivitiesSession)session;return streamedCommittedPresentation=CaptureStreamedActivitiesCandidate((ICampaignStreamedActivitiesSession)session,observed);}
                if(colonisationPackage!=null){RequireColonisationFrame(observed);
                    if(ReferenceEquals(session,colonisationPresentationSource)&&colonisationCommittedPresentation!=null&&ReferenceEquals(colonisationCommittedPresentation.Frame,observed))return colonisationCommittedPresentation;
                    colonisationPresentationSource=(ICampaignColonisationOperations)session;return colonisationCommittedPresentation=CaptureColonisationCandidate((ICampaignColonisationOperations)session,observed);}
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
                if(colonisationInteraction!=null)
                {
                    if(ReferenceEquals(session,colonisationInteraction.Source)&&ReferenceEquals(session,colonisationPresentationSource)&&ReferenceEquals(presentation,colonisationCommittedPresentation)&&ReferenceEquals(observed,colonisationInteraction.SourceFrame)&&ReferenceEquals(presentation.Frame,observed)&&presentation.HostId==id&&presentation.Generation==generation)return;
                    throw new InvalidOperationException("Stale generated presentation.");
                }
                if(streamedInteraction!=null)
                {
                    if(ReferenceEquals(session,streamedInteraction.Source)&&ReferenceEquals(session,streamedPresentationSource)&&ReferenceEquals(presentation,streamedCommittedPresentation)&&ReferenceEquals(observed,streamedInteraction.SourceFrame)&&ReferenceEquals(presentation.Frame,observed)&&presentation.HostId==id&&presentation.Generation==generation)return;
                    throw new InvalidOperationException("Stale generated presentation.");
                }
                RequireCurrent(observed.HostId,observed.Generation);
                RequireCurrent(presentation.HostId,presentation.Generation);
                if(!staticAstronomy&&publishedPackage==null&&activitiesPackage==null&&streamedActivitiesPackage==null&&colonisationPackage==null)throw new InvalidOperationException("Wrong campaign profile.");
                var status=session.CaptureStatus();
                if(presentation.Actor!=observed.Actor || presentation.Frame.Status.Turn!=status.Turn
                    || presentation.Frame.Status.CampaignRevision!=status.CampaignRevision
                    || observed.Status.Turn!=status.Turn || observed.Status.CampaignRevision!=status.CampaignRevision)
                    throw new InvalidOperationException("Stale generated presentation.");
            }
        }
    }
}
