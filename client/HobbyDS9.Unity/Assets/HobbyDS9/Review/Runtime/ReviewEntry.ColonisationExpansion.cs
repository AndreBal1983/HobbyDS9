#nullable enable
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private void DrawColonisationExpansionMenu(VisualElement parent)
        {
            var fold=new Foldout{text="Colonisation & Expansion",value=true,name="colonisation-expansion-campaign-menu"};parent.Add(fold);
            fold.Add(Readout("Build transports and settlement equipment, carry settlers and supplies, then establish and support a discovered world. Rival empires can build, survey and establish their own colonies using the same settlers and supply rules. Provisional playtest; separate saves keep earlier campaigns unchanged."));
            var start=AddButton(fold,"New expansion campaign",()=>{void Start()=>RunGeneratedCampaignOperation(null,colonisationExpansion:true);
                if(demoStarted||model!.HasLocalEdits)Confirm("Start a new expansion campaign? Unsaved progress is replaced only after validation. Earlier saves stay in their own slots.",Start);else Start();});start.name="colonisation-expansion-campaign-start";
            foreach(var slot in GeneratedCampaignOptions.Slots()){var captured=slot;var load=AddButton(fold,"Continue expansion "+slot.Substring(slot.Length-1),()=>Confirm("Continue this expansion campaign? Unsaved progress is replaced only after complete replay validation.",()=>RunGeneratedCampaignOperation(captured,colonisationExpansion:true)));load.name="colonisation-expansion-campaign-continue-"+slot;}
        }
    }
}
