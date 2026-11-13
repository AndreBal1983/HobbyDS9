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
        private ColonisationInteractionWork? colonisationUiWork;
        private ReviewUiModel? colonisationUiOwner;
        private string colonisationUiTitle="";
        private void CancelColonisationUiWork()
        {
            if(colonisationUiWork!=null)colonisationUiOwner?.AbandonColonisationInteraction(colonisationUiWork);
            // Keep ownership until the cancelled source-reading worker drains.
            // A re-enabled view shows the same busy state instead of querying it.
        }
        private void RestoreColonisationBusy()
        {
            if(colonisationUiWork!=null&&ReferenceEquals(model,colonisationUiOwner)&&model!.Busy&&campaignBusyToken==null)
            {campaignBusyToken=colonisationUiWork;campaignBusyTitle=colonisationUiTitle;}
        }
        private async void RunColonisationInteraction(ReviewUiModel owner,ReviewController controller,ReviewFrame frame,
            ColonisationInteractionKind kind,CampaignActivityCommandDto? command=null,ColonisationCommandDto? colonyCommand=null,string caption="",ReviewColonisationPanel.Selection? selection=null)
        {
            if(!ReferenceEquals(model,owner)||!ReferenceEquals(owner.Current,controller)||!ReferenceEquals(controller.Frame,frame)||!isActiveAndEnabled)return;
            var work=owner.BeginColonisationInteraction(kind,command,colonyCommand);
            if(work==null)
            {
                if(!owner.Busy&&colonisationUiWork==null)root?.Q<ReviewColonisationPanel>()?.PresentAdmissionRejected(owner.ColonisationActionStatus()??"The order is unavailable at the observed campaign state.");
                return;
            }
            var epoch=viewEpoch;colonisationUiWork=work;colonisationUiOwner=owner;
            colonisationRebuildSelection=selection??(workspace!=null&&(workspace.Page==ReviewWorkspacePage.Production||workspace.Page==ReviewWorkspacePage.Worlds)
                &&ReferenceEquals(owner,colonisationPanelOwner)&&ReferenceEquals(controller,colonisationPanelController)?root?.Q<ReviewColonisationPanel>()?.CaptureSelection():null);
            colonisationUiTitle=kind==ColonisationInteractionKind.Preview?"Checking order…":kind==ColonisationInteractionKind.Submission?"Applying order…":"Advancing week…";
            try
            {
                ShowCampaignBusy(work,colonisationUiTitle);
                var completion=await Task.Run(work.Execute);
                if(this==null||!isActiveAndEnabled||viewEpoch!=epoch||!ReferenceEquals(model,owner)
                    ||!ReferenceEquals(colonisationUiWork,work)||!ReferenceEquals(colonisationUiOwner,owner))return;
                var finished=owner.CompleteColonisationInteraction(work,completion);
                HideCampaignBusy(work);colonisationUiWork=null;colonisationUiOwner=null;
                if(!ReferenceEquals(frame,owner.Current.Frame)){viewEpoch++;Build();}else Draw();
                if(finished&&completion.Committed)CapturePublishedCompletions(owner,frame);
                var panel=root?.Q<ReviewColonisationPanel>();panel?.RestoreSelection(selection);
                if(finished&&kind==ColonisationInteractionKind.Preview&&ReferenceEquals(owner.Current.Frame,frame))
                {
                    if(completion.ColonisationEligibility!=null)panel?.PresentReviewedOrder(work.ColonisationCommand!,caption,completion.ColonisationEligibility);
                    else if(completion.Eligibility!=null)panel?.PresentReviewedActivity(work.Command!,caption,completion.Eligibility);
                }
            }
            finally
            {
                owner.AbandonColonisationInteraction(work);HideCampaignBusy(work);
                if(ReferenceEquals(colonisationUiWork,work))
                {
                    colonisationUiWork=null;colonisationUiOwner=null;
                    if(this!=null&&isActiveAndEnabled&&ReferenceEquals(model,owner))Draw();
                }
            }
        }
    }
}
