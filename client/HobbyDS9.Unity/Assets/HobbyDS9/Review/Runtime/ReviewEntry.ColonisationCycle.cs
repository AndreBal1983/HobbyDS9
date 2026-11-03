#nullable enable
using System.IO;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private string ColonisationCycleCampaignDirectory()
        {
#if UNITY_EDITOR
            if(generatedCampaignTestDirectory!=null)return generatedCampaignTestDirectory;
#endif
            return Path.Combine(UnityEngine.Application.persistentDataPath,ReviewColonisationCycleSlotStore.Namespace);
        }
        private void DrawColonisationCycleMenu(VisualElement parent)
        {
            var fold=new Foldout{text="Campaign",value=true,name="colonisation-cycle-campaign-menu"};parent.Add(fold);
            fold.Add(Readout("Found and grow your colonies. Rival empires can prepare successive resource-limited frontier expeditions while their colonies remain viable, using real ships, settlers and supplies. Provisional playtest; earlier campaigns keep their separate saves."));
            var start=AddButton(fold,"New Campaign",()=>{void Start()=>RunGeneratedCampaignOperation(null,colonisationCycle:true);
                if(demoStarted||model!.HasLocalEdits)Confirm("Start a new continuous expansion campaign? Unsaved progress is replaced only after validation. Earlier saves stay in their own slots.",Start);else Start();});start.name="colonisation-cycle-campaign-start";
            foreach(var slot in GeneratedCampaignOptions.Slots()){var captured=slot;var load=AddButton(fold,"Continue "+slot.Substring(slot.Length-1),()=>Confirm("Continue this continuous expansion campaign? Unsaved progress is replaced only after complete replay validation.",()=>RunGeneratedCampaignOperation(captured,colonisationCycle:true)));load.name="colonisation-cycle-campaign-continue-"+slot;}
        }
    }
}
