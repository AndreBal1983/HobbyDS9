#nullable enable
using System;
using System.Diagnostics;
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
            {routinePendingToken=colonisationUiWork;PresentRoutinePending(colonisationUiTitle);}
        }
        private async void RunColonisationInteraction(ReviewUiModel owner,ReviewController controller,ReviewFrame frame,
            ColonisationInteractionKind kind,CampaignActivityCommandDto? command=null,ColonisationCommandDto? colonyCommand=null,string caption="",ReviewColonisationPanel.Selection? selection=null)
        {
            if(!ReferenceEquals(model,owner)||!ReferenceEquals(owner.Current,controller)||!ReferenceEquals(controller.Frame,frame)||!isActiveAndEnabled)return;
            var timing=Stopwatch.StartNew();
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
            var saved=new RoutineViewState();double beginMs=0;
            try
            {
                saved=ShowRoutinePending(work,colonisationUiTitle);beginMs=timing.Elapsed.TotalMilliseconds;
                var completion=await Task.Run(work.Execute);
                var workerMs=timing.Elapsed.TotalMilliseconds-beginMs;
                if(this==null||!isActiveAndEnabled||viewEpoch!=epoch||!ReferenceEquals(model,owner)
                    ||!ReferenceEquals(colonisationUiWork,work)||!ReferenceEquals(colonisationUiOwner,owner))return;
                var finished=owner.CompleteColonisationInteraction(work,completion);
                var receiveMs=timing.Elapsed.TotalMilliseconds-beginMs-workerMs;
                HideRoutinePending(work);colonisationUiWork=null;colonisationUiOwner=null;
                ReceiveRoutineView(saved,!ReferenceEquals(frame,owner.Current.Frame));
                if(finished&&completion.Committed)CapturePublishedCompletions(owner,frame);
                var panel=root?.Q<ReviewColonisationPanel>();panel?.PresentStatus(owner.ColonisationActionStatus());
                if(kind==ColonisationInteractionKind.Submission&&ReferenceEquals(owner.Current.Frame,frame))panel?.CompleteSubmission(owner.ColonisationActionStatus()??owner.Message);
                if(finished&&kind==ColonisationInteractionKind.Preview&&ReferenceEquals(owner.Current.Frame,frame))
                {
                    if(completion.ColonisationEligibility!=null)panel?.PresentReviewedOrder(work.ColonisationCommand!,caption,completion.ColonisationEligibility);
                    else if(completion.Eligibility!=null)panel?.PresentReviewedActivity(work.Command!,caption,completion.Eligibility);
                }
                ReportRoutineTiming("colonisation."+kind,timing,beginMs,workerMs,receiveMs,completion.Committed);
            }
            finally
            {
                owner.AbandonColonisationInteraction(work);HideRoutinePending(work);
                if(ReferenceEquals(colonisationUiWork,work))
                {
                    colonisationUiWork=null;colonisationUiOwner=null;
                    if(this!=null&&isActiveAndEnabled&&ReferenceEquals(model,owner))
                    {if(viewEpoch==epoch)ReceiveRoutineView(saved,false);else Draw();}
                }
            }
        }
    }
}
