#nullable enable
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private void DrawColonisationGrowthMenu(VisualElement parent)
        {
            var fold=new Foldout{text="Colonisation & Growth",value=true,name="colonisation-growth-campaign-menu"};parent.Add(fold);
            fold.Add(Readout("Found and support new worlds, then grow established colonies by carrying real passengers without a settlement kit. Rival empires use the same population and supply rules. Provisional playtest; earlier campaigns keep their separate saves."));
            var start=AddButton(fold,"New growth campaign",()=>{void Start()=>RunGeneratedCampaignOperation(null,colonisationGrowth:true);
                if(demoStarted||model!.HasLocalEdits)Confirm("Start a new growth campaign? Unsaved progress is replaced only after validation. Earlier saves stay in their own slots.",Start);else Start();});start.name="colonisation-growth-campaign-start";
            foreach(var slot in GeneratedCampaignOptions.Slots()){var captured=slot;var load=AddButton(fold,"Continue growth "+slot.Substring(slot.Length-1),()=>Confirm("Continue this growth campaign? Unsaved progress is replaced only after complete replay validation.",()=>RunGeneratedCampaignOperation(captured,colonisationGrowth:true)));load.name="colonisation-growth-campaign-continue-"+slot;}
        }
    }
}
