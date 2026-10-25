#nullable enable
using System;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private void DrawActivitiesMenu(VisualElement parent)
        {
            var fold=new Foldout{text="Earlier activities prototype · upfront-paid rules",value=false,name="activities-campaign-menu"};parent.Add(fold);
            fold.Add(Readout("Continue an earlier prototype checkpoint under its original upfront-paid construction rules. These saves remain separate from streamed activities campaigns."));
            foreach(var slot in GeneratedCampaignOptions.Slots())
            {var captured=slot;var load=AddButton(fold,"Continue activities "+slot.Substring(slot.Length-1),()=>Confirm("Continue this activities campaign? Unsaved changes are replaced only after validation.",()=>RunGeneratedCampaignOperation(captured,activities:true)));load.name="activities-campaign-continue-"+slot;}
        }
        private void DrawActivitiesPanel(VisualElement parent)
        {
            var owner=model!;var controller=owner.Current;var frame=controller.Frame;
            var observation=owner.Activities();
            Func<HobbyDS9.Contracts.CampaignActivityCommandDto,HobbyDS9.Contracts.CampaignActivityEligibilityDto> evaluate=command=>{
                if(!ReferenceEquals(model,owner)||!ReferenceEquals(owner.Current,controller)||!ReferenceEquals(controller.Frame,frame))throw new InvalidOperationException("Activity observation changed.");
                return owner.ActivityEligibility(command);
            };
            Action<HobbyDS9.Contracts.CampaignActivityCommandDto> submit=command=>{
                if(ReferenceEquals(model,owner)&&ReferenceEquals(owner.Current,controller)&&ReferenceEquals(controller.Frame,frame))Do(()=>owner.SubmitActivity(command));
            };
            var panel=owner.StreamedActivitiesCampaign?new ReviewActivitiesPanel(owner.StreamedActivities(),evaluate,submit):new ReviewActivitiesPanel(observation,evaluate,submit);
            parent.Add(panel);
        }
    }
}
