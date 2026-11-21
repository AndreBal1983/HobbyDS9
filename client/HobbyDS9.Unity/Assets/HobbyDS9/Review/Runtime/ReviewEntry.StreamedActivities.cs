#nullable enable
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private void DrawStreamedActivitiesMenu(VisualElement parent)
        {
            var fold=new Foldout{text="Campaign · construction and exploration",value=true,name="streamed-activities-campaign-menu"};parent.Add(fold);
            fold.Add(Readout("Explore, meet independent societies and build three ready ship classes. Normal construction costs Industry and gradually consumed local materials, without a credit charge. Delivery interruptions pause work. This playtest uses separate saves; earlier campaigns retain their rules."));
            fold.Add(Readout("Your Frontier Commission: establish capacity, launch an own-built ship, file a scientific result and assess an independent society. Follow the Commission report as you play, then keep exploring."));
            var start=AddButton(fold,"New campaign",()=>Confirm("Start a new campaign? The current game is replaced only after validation.",()=>RunGeneratedCampaignOperation(null,streamed:true)));
            start.name="streamed-activities-campaign-start";
            start.AddToClassList("primary-action");if(!demoStarted&&PlayableResourcesAvailable())ScheduleDemoMenuFocus(start);
            foreach(var slot in GeneratedCampaignOptions.Slots())
            {var selected=slot;var load=AddButton(fold,"Continue "+slot.Substring(slot.Length-1),()=>Confirm("Continue this campaign? Unsent edits are replaced only after validation.",()=>RunGeneratedCampaignOperation(selected,streamed:true)));load.name="streamed-activities-campaign-continue-"+slot;}
        }
    }
}
