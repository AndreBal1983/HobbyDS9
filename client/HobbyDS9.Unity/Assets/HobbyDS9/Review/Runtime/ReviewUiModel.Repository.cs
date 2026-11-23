#if HOBBYDS9_STATE_REPOSITORY_TRIAL
#nullable enable
using System;
using HobbyDS9.CampaignRepositoryHost;
using HobbyDS9.Contracts;
using HobbyDS9.Persistence;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        internal bool SupportsRepositoryTrial => colonisationCyclePackage is CampaignBeltBodiesPackageDto;

        internal CampaignRepositoryCaptureWork? PrepareRepositoryTrial(out ICampaignRepositoryCodec wire,
            out CycleCampaignRepositoryAdapter? adapter)
        {
            wire = new DelegatedCampaignRepositoryCodec("review-newtonsoft-cycle-v1", codec.EncodeCycleRoot, codec.DecodeCycleRoot);
            adapter = null;
            if (!SupportsRepositoryTrial || Busy || Closed || Current.HasPendingWork || Current.RefreshRequired) return null;
            var prepared = host.PrepareRepositoryCheckpoint(Current.Frame, wire);
            if (prepared.Error != CampaignRepositoryError.None) return null;
            adapter = new CycleCampaignRepositoryAdapter(colonisationCyclePackage!, codec, wire);
            return prepared.Work;
        }

        internal RepositorySaveQueue AttachRepositoryTrial(ICampaignRepository repository, BranchInfo branch,
            ICampaignRepositoryCodec wire) => new RepositorySaveQueue(host, Current.Frame, repository, branch, wire);

        internal bool RestoreRepositoryTrial(RepositorySaveQueue queue, RepositoryPayloadWork work,
            RepositoryPayloadResult result, ICampaignRepository repository, BranchInfo confirmedBranch)
        {
            if (Busy || Closed || Current.HasPendingWork || Current.RefreshRequired || confirmedBranch.Head != work.CheckpointId)
            {
                work.Cancel();
                queue.CompletePayloadValidation(work);
                return false;
            }
            var error = queue.CompletePayloadRestore(work, out var frame);
            if (error == CampaignRepositoryError.Busy)
            {
                work.Cancel();
                queue.CompletePayloadValidation(work);
            }
            if (error != CampaignRepositoryError.None) return false;

            Current = Controller(host, frame!.Actor);
            GeneratedViewportNeedsFit = true;
            ClearResearchQueue();
            ResearchDraft = null;
            LocalDesignDirty = false;
            ResetLocalDesign = null;
            ResetOrganization();
            Message = "Validated complete-root checkpoint loaded.";
            if (!queue.Rebind(host, Current.Frame, repository, confirmedBranch) || !queue.ConfirmRestoredCheckpoint(result.Loaded!))
                throw new InvalidOperationException("The restored session could not be bound to its confirmed branch.");
            return true;
        }
    }
}
#endif
