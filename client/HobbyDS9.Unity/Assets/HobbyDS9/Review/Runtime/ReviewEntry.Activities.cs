#nullable enable
using System;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private void DrawActivitiesMenu(VisualElement parent)
        {
            var fold=new Foldout{text="Activities campaign · provisional playtest",value=false,name="activities-campaign-menu"};parent.Add(fold);
            fold.Add(Readout("Explore finite discoveries, meet independent societies and build ships or colony facilities in one campaign. Constitution Explorer, Galaxy Cruiser and Defiant Combat are ready to build. Tuning is provisional; earlier saves keep their original rules."));
            var start=AddButton(fold,"New activities campaign",()=>Confirm("Start the activities playtest? Your current game is replaced only after validation; earlier saves remain separate.",()=>RunGeneratedCampaignOperation(null,activities:true)));
            start.name="activities-campaign-start";
            foreach(var slot in GeneratedCampaignOptions.Slots())
            {var captured=slot;var load=AddButton(fold,"Continue activities "+slot.Substring(slot.Length-1),()=>Confirm("Continue this activities campaign? Unsaved changes are replaced only after validation.",()=>RunGeneratedCampaignOperation(captured,activities:true)));load.name="activities-campaign-continue-"+slot;}
        }
        private void DrawActivitiesPanel(VisualElement parent)
        {
            var owner=model!;var controller=owner.Current;var frame=controller.Frame;
            var observation=owner.Activities();
            var panel=new ReviewActivitiesPanel(observation,command=>{
                if(!ReferenceEquals(model,owner)||!ReferenceEquals(owner.Current,controller)||!ReferenceEquals(controller.Frame,frame))throw new InvalidOperationException("Activity observation changed.");
                return owner.ActivityEligibility(command);
            },command=>{
                if(ReferenceEquals(model,owner)&&ReferenceEquals(owner.Current,controller)&&ReferenceEquals(controller.Frame,frame))Do(()=>owner.SubmitActivity(command));
            });
            parent.Add(panel);
        }
    }
}
