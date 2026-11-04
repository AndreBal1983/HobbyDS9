#nullable enable
using System;
using System.IO;
using HobbyDS9.TacticalReviewClient;
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
            DrawEarlierCampaignSaves(fold);
        }

        private void DrawEarlierCampaignSaves(VisualElement parent)
        {
            var earlier = new Foldout { text = "Earlier campaign saves", value = false, name = "earlier-campaign-saves" };
            var codec = new ReviewNewtonsoftCodec();
            var slots = GeneratedCampaignOptions.Slots();

            void AddProfile(string label, string profile, Func<IReviewCheckpointWriteAdmission> openStore, Action<string> continueSlot)
            {
                IReviewCheckpointWriteAdmission store;
                try { store = openStore(); }
                catch (ReviewCheckpointStoreException) { return; }

                foreach (var slot in slots)
                {
                    // The admission check is read-only. The typed loader still validates the full save.
                    if (!store.TryCheckWriteAdmission(slot, out var status) || status != ReviewCheckpointError.SlotOccupied)
                        continue;

                    if (earlier.parent == null)
                        parent.Add(earlier);

                    var load = AddButton(earlier, label + " · Continue " + slot.Substring(slot.Length - 1), () =>
                    {
                        void Continue() => continueSlot(slot);
                        if (demoStarted || model!.HasLocalEdits)
                            Confirm("Continue this earlier campaign under its original rules? Unsaved progress is replaced only after the save is validated.", Continue);
                        else
                            Continue();
                    });
                    load.name = "earlier-campaign-continue-" + profile + "-" + slot;
                }
            }

            AddProfile("Repeat Expansion", "repeat",
                () => new ReviewColonisationRepeatSlotStore(GeneratedCampaignDirectory(colonisationRepeat: true), codec),
                slot => RunGeneratedCampaignOperation(slot, colonisationRepeat: true));
            AddProfile("Colony Growth", "growth",
                () => new ReviewColonisationGrowthSlotStore(GeneratedCampaignDirectory(colonisationGrowth: true), codec),
                slot => RunGeneratedCampaignOperation(slot, colonisationGrowth: true));
            AddProfile("Colony Expansion", "expansion",
                () => new ReviewColonisationExpansionSlotStore(GeneratedCampaignDirectory(colonisationExpansion: true), codec),
                slot => RunGeneratedCampaignOperation(slot, colonisationExpansion: true));
            AddProfile("Colonisation V2", "roster",
                () => ReviewColonisationSlotStore.CreateRosterV2(GeneratedCampaignDirectory(colonisationRoster: true), codec),
                slot => RunGeneratedCampaignOperation(slot, colonisationRoster: true));
            AddProfile("Colonisation", "colonisation",
                () => new ReviewColonisationSlotStore(GeneratedCampaignDirectory(colonisation: true), codec),
                slot => RunGeneratedCampaignOperation(slot, colonisation: true));
            AddProfile("Streamed Activities", "streamed",
                () => new ReviewStreamedActivitiesSlotStore(GeneratedCampaignDirectory(streamed: true), codec),
                slot => RunGeneratedCampaignOperation(slot, streamed: true));
            AddProfile("Activities", "activities",
                () => new ReviewActivitiesSlotStore(GeneratedCampaignDirectory(activities: true), codec),
                slot => RunGeneratedCampaignOperation(slot, activities: true));
            AddProfile("Published Construction", "published",
                () => new ReviewPublishedConstructionSlotStore(GeneratedCampaignDirectory(published: true), codec),
                slot => RunGeneratedCampaignOperation(slot, published: true));
            AddProfile("Static Astronomy", "astronomy",
                () => new ReviewStaticAstronomyCampaignSlotStore(GeneratedCampaignDirectory(), codec),
                slot => RunGeneratedCampaignOperation(slot));
            AddProfile("Original Generated Galaxy", "generated",
                () => new ReviewGeneratedCampaignSlotStore(GeneratedCampaignDirectory(legacy: true), codec),
                slot => RunGeneratedCampaignOperation(slot, legacy: true));
        }
    }
}
