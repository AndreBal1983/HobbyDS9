#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private BeltBodyInteractionWork? beltUiWork;
        private ReviewUiModel? beltUiOwner;
        private string beltUiTitle="";
        private void CancelBeltUiWork(){if(beltUiWork!=null)beltUiOwner?.AbandonBeltBodyInteraction(beltUiWork);}
        private void RestoreBeltBodiesBusy()
        {
            if(beltUiWork!=null&&ReferenceEquals(model,beltUiOwner)&&model!.Busy&&campaignBusyToken==null)
            {routinePendingToken=beltUiWork;PresentRoutinePending(beltUiTitle);}
        }
        private bool BeltReviewBoundary(ReviewUiModel owner,ReviewController controller,ReviewFrame frame)
        {
            if(this==null||!isActiveAndEnabled||!ReferenceEquals(model,owner)||!ReferenceEquals(owner.Current,controller)
                ||!ReferenceEquals(controller.Frame,frame)||owner.Busy||owner.Closed||!owner.SupportsBeltBodies
                ||controller.RefreshRequired||owner.HasOrderDraft||frame.Status.Phase!=SessionPhase.Planning||frame.Battle!=null)return false;
            if(!owner.FlushResearchAllocations()||!ReferenceEquals(controller.Frame,frame))
            {
                if(confirmation!=null)ClearConfirmation();Draw();
                ShowCampaignDialog("Review belt action again","Allocation or campaign state changed. Reopen the belt to review the current action.","Close",null);
                return false;
            }
            return true;
        }
        private async void RunBeltBodyInteraction(ReviewUiModel owner,ReviewController controller,ReviewFrame frame,
            BeltBodyInteractionKind kind,BeltBodyCommandDto command,string caption)
        {
            if(confirmation!=null||routinePendingToken!=null||!BeltReviewBoundary(owner,controller,frame))return;
            var work=owner.BeginBeltBodyInteraction(kind,command);
            if(work==null){Draw();return;}
            var epoch=viewEpoch;var mounted=root;var mountedPanel=root?.panel;var modal=confirmation;
            beltUiWork=work;beltUiOwner=owner;beltUiTitle=kind==BeltBodyInteractionKind.Preview?"Checking belt action…":"Applying belt action…";
            var saved=new RoutineViewState();
            try
            {
                saved=ShowRoutinePending(work,beltUiTitle);
                var completion=await Task.Run(work.Execute);
                if(this==null||!isActiveAndEnabled||viewEpoch!=epoch||!ReferenceEquals(root,mounted)||mountedPanel==null
                    ||!ReferenceEquals(mounted?.panel,mountedPanel)||!ReferenceEquals(model,owner)||!ReferenceEquals(owner.Current,controller)
                    ||!ReferenceEquals(controller.Frame,frame)||!ReferenceEquals(confirmation,modal)
                    ||!ReferenceEquals(beltUiWork,work)||!ReferenceEquals(beltUiOwner,owner))return;
                var received=owner.CompleteBeltBodyInteraction(work,completion);
                HideRoutinePending(work);beltUiWork=null;beltUiOwner=null;
                ReceiveRoutineView(saved,!ReferenceEquals(controller.Frame,frame));
                if(received&&kind==BeltBodyInteractionKind.Preview&&completion.Preview!=null&&ReferenceEquals(controller.Frame,frame))
                    ShowBeltPreview(owner,controller,frame,work.Command,caption,completion.Preview);
                else if(!completion.Committed&&ReferenceEquals(controller.Frame,frame)&&confirmation==null)
                    ShowCampaignDialog("Belt action",completion.Result!=null?BeltBodyPresentation.Reason(completion.Result.Error):"The interaction did not complete. No action was published.","Close",null);
            }
            finally
            {
                owner.AbandonBeltBodyInteraction(work);HideRoutinePending(work);
                if(ReferenceEquals(beltUiWork,work))
                {
                    beltUiWork=null;beltUiOwner=null;
                    if(this!=null&&isActiveAndEnabled&&ReferenceEquals(model,owner))
                    {if(viewEpoch==epoch)ReceiveRoutineView(saved,false);else Draw();}
                }
            }
        }
        private void ShowBeltPreview(ReviewUiModel owner,ReviewController controller,ReviewFrame frame,
            BeltBodyCommandDto command,string caption,BeltBodyCommandPreview preview)
        {
            if(confirmation!=null||!BeltReviewBoundary(owner,controller,frame))return;
            if(!preview.CanSubmit){ShowCampaignDialog("Belt action unavailable",caption+"\n"+BeltBodyPresentation.Reason(preview.Error),"Close",null);return;}
            var details=caption;var commit="Confirm action";
            if(command.Action==BeltBodyAction.EnqueueMine)
            {
                var recipient=owner.Colonisation().Worlds.FirstOrDefault(w=>w.Id==command.ColonyId)?.Name??"Selected sponsor";
                details+="\nConstruction work · "+preview.Industry+" Industry · "+preview.Credits+" credits"
                    +"\nRecipient · "+recipient+" · shared construction FIFO"
                    +"\nExtraction · up to "+preview.WeeklyExtraction+" units/week into mine storage"
                    +"\nStanding delivery · up to "+preview.WeeklyTransferLimit+" units/week to "+recipient+" using remaining shared freight capacity. Blocked delivery leaves ore at the mine."
                    +"\nUncrewed · power supply "+preview.PowerSupply+" / demand "+preview.PowerDemand
                    +"\nNo housing or settlement capacity. Construction and transfers happen through normal weeks.";
                commit="Queue mining installation";
            }
            else if(command.Action==BeltBodyAction.StartSurvey)
            {details+="\nSurvey work · "+preview.SurveyWeeks+" weeks with the selected eligible ship.";commit="Start belt survey";}
            else if(command.Action==BeltBodyAction.CancelSurvey)commit="Cancel belt survey";
            else if(command.Action==BeltBodyAction.CancelMine)commit="Cancel mining installation";
            ShowCampaignDialog("Review belt action",details,"Keep inspecting",null,
                new CampaignDialogAction(commit,()=>RunBeltBodyInteraction(owner,controller,frame,BeltBodyInteractionKind.Submission,command,caption)));
        }
    }
}
