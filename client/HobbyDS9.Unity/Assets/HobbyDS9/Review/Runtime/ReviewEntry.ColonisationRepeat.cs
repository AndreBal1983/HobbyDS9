#nullable enable
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private void DrawColonisationRepeatMenu(VisualElement parent)
        {
            var fold=new Foldout{text="Colonisation & Repeat Expansion",value=true,name="colonisation-repeat-campaign-menu"};parent.Add(fold);
            fold.Add(Readout("Found and grow your colonies. Rival empires can prepare one further frontier expedition after their first colony is viable, using real ships, settlers and supplies. Provisional playtest; earlier campaigns keep their separate saves."));
            var start=AddButton(fold,"New repeat expansion campaign",()=>{void Start()=>RunGeneratedCampaignOperation(null,colonisationRepeat:true);
                if(demoStarted||model!.HasLocalEdits)Confirm("Start a new repeat expansion campaign? Unsaved progress is replaced only after validation. Earlier saves stay in their own slots.",Start);else Start();});start.name="colonisation-repeat-campaign-start";
            foreach(var slot in GeneratedCampaignOptions.Slots()){var captured=slot;var load=AddButton(fold,"Continue repeat expansion "+slot.Substring(slot.Length-1),()=>Confirm("Continue this repeat expansion campaign? Unsaved progress is replaced only after complete replay validation.",()=>RunGeneratedCampaignOperation(captured,colonisationRepeat:true)));load.name="colonisation-repeat-campaign-continue-"+slot;}
        }
    }
}
