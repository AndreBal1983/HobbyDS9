#nullable enable
using System;
using System.Threading.Tasks;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private StreamedInteractionWork? streamedUiWork;
        private ReviewUiModel? streamedUiOwner;
        private string streamedUiTitle="";
        private void CancelStreamedUiWork()
        {
            if(streamedUiWork!=null)streamedUiOwner?.AbandonStreamedInteraction(streamedUiWork);
            // Keep ownership until the cancelled source-reading worker drains.
            // A re-enabled view shows the same busy state instead of querying it.
        }
        private void RestoreStreamedBusy()
        {
            if(streamedUiWork!=null&&ReferenceEquals(model,streamedUiOwner)&&model!.Busy&&campaignBusyToken==null)
            {campaignBusyToken=streamedUiWork;campaignBusyTitle=streamedUiTitle;}
        }
        private async void RunStreamedInteraction(ReviewUiModel owner,ReviewController controller,ReviewFrame frame,
            StreamedInteractionKind kind,CampaignActivityCommandDto? command=null,string caption="",ReviewActivitiesPanel.Selection? selection=null)
        {
            if(!ReferenceEquals(model,owner)||!ReferenceEquals(owner.Current,controller)||!ReferenceEquals(controller.Frame,frame)||!isActiveAndEnabled)return;
            var work=owner.BeginStreamedInteraction(kind,command);
            if(work==null)
            {
                // A rejected Begin can mean the source stamp changed. Show the
                // current owned notice without trying to recapture that stale
                // frame, and never overwrite a running newer operation's UI.
                if(kind==StreamedInteractionKind.Submission&&(command?.Kind==CampaignActivityCommandKind.EnqueueShip||command?.Kind==CampaignActivityCommandKind.EnqueueStructure)
                    &&!owner.Busy&&streamedUiWork==null&&ReferenceEquals(activitiesPanelOwner,owner)&&ReferenceEquals(activitiesPanelController,controller))
                {root?.Q<ReviewActivitiesPanel>()?.RefreshConstructionStatus();return;}
                Draw();return;
            }
            var epoch=viewEpoch;streamedUiWork=work;streamedUiOwner=owner;
            streamedUiTitle=kind==StreamedInteractionKind.Preview?"Checking order…":kind==StreamedInteractionKind.Submission?"Applying order…":"Advancing week…";
            try
            {
                ShowCampaignBusy(work,streamedUiTitle);
                var completion=await Task.Run(work.Execute);
                if(this==null||!isActiveAndEnabled||viewEpoch!=epoch||!ReferenceEquals(model,owner)
                    ||!ReferenceEquals(streamedUiWork,work)||!ReferenceEquals(streamedUiOwner,owner))return;
                var finished=owner.CompleteStreamedInteraction(work,completion);
                HideCampaignBusy(work);streamedUiWork=null;streamedUiOwner=null;
                if(!ReferenceEquals(frame,owner.Current.Frame)){viewEpoch++;Build();}else Draw();
                var panel=root?.Q<ReviewActivitiesPanel>();panel?.RestoreSelection(selection);
                if(finished&&kind==StreamedInteractionKind.Preview&&completion.Eligibility!=null&&ReferenceEquals(owner.Current.Frame,frame))
                    panel?.PresentReviewedOrder(work.Command!,caption,completion.Eligibility);
            }
            finally
            {
                owner.AbandonStreamedInteraction(work);HideCampaignBusy(work);
                if(ReferenceEquals(streamedUiWork,work))
                {
                    streamedUiWork=null;streamedUiOwner=null;
                    if(this!=null&&isActiveAndEnabled&&ReferenceEquals(model,owner))Draw();
                }
            }
        }
    }
}
