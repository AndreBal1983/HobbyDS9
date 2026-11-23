#nullable enable
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private void DrawColonisationRosterMenu(VisualElement parent)
        {
            var fold=new Foldout{text="Colonisation V2 (provisional playtest)",value=false,name="colonisation-roster-campaign-menu"};parent.Add(fold);
            fold.Add(Readout("Both empires start with their own Constitution Explorer. Manufacture transports and settlement equipment, carry real settlers and supplies, then establish and support a discovered world. New V2 save slots; earlier V1 saves keep their original starting fleets. AI expansion planning is not included."));
            var start=AddButton(fold,"New colonisation V2 campaign",()=>{void Start()=>RunGeneratedCampaignOperation(null,colonisationRoster:true);
                if(demoStarted||model!.HasLocalEdits)Confirm("Start a new colonisation V2 campaign? Unsaved progress is replaced only after validation. Earlier saves stay in their own slots.",Start);else Start();});start.name="colonisation-roster-campaign-start";
            foreach(var slot in GeneratedCampaignOptions.Slots()){var captured=slot;var load=AddButton(fold,"Continue colonisation V2 "+slot.Substring(slot.Length-1),()=>Confirm("Continue this V2 campaign? Unsaved progress is replaced only after complete replay validation.",()=>RunGeneratedCampaignOperation(captured,colonisationRoster:true)));load.name="colonisation-roster-campaign-continue-"+slot;}
        }
    }
}
