#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
#if UNITY_EDITOR
        private string? generatedCampaignTestDirectory=null; // Dedicated engine fixture only; never a player path option.
#endif
        private string GeneratedCampaignDirectory(bool legacy=false,bool published=false,bool activities=false,bool streamed=false)
        {
#if UNITY_EDITOR
            if(generatedCampaignTestDirectory!=null)return generatedCampaignTestDirectory;
#endif
            return Path.Combine(UnityEngine.Application.persistentDataPath,streamed?ReviewStreamedActivitiesSlotStore.Namespace:activities?ReviewActivitiesSlotStore.Namespace:published?ReviewPublishedConstructionSlotStore.Namespace:legacy?GeneratedCampaignOptions.SaveDirectory:ReviewStaticAstronomyCampaignSlotStore.Namespace);
        }
        private string generatedCampaignSeed="42";
        private int generatedCampaignSize=250,generatedCampaignQuiet=2000;
        private bool generatedCampaignBusy;
        private VisualElement? generatedCampaignControls;
        private VisualElement? legacyGeneratedCampaignControls;
        private Label? generatedCampaignNotice;
        private void DrawGeneratedCampaignMenu(VisualElement parent)
        {
            TextField seed;DropdownField size,quiet;
            var controls=new VisualElement{name="generated-campaign-controls"};generatedCampaignControls=controls;parent.Add(controls);
            controls.Add(Readout("Your galaxy"));
            controls.Add(Readout("Two empires. Explore unknown space, research technology and build your fleet."));
            var row=new VisualElement();row.AddToClassList("review-field-row");controls.Add(row);var epoch=viewEpoch;
            seed=new TextField("Seed"){name="generated-campaign-seed",value=generatedCampaignSeed,maxLength=20};seed.AddToClassList("generated-campaign-seed");row.Add(seed);
            size=new DropdownField("Systems",new List<string>{"250","1000"},generatedCampaignSize.ToString(CultureInfo.InvariantCulture)){name="generated-campaign-size"};row.Add(size);
            var values=GeneratedGalaxyWorkspace.QuietTargetChoices(true);var choices=new List<string>();foreach(var v in values)choices.Add(GeneratedGalaxyWorkspace.QuietTargetLabel(v));
            quiet=new DropdownField("Quiet area",choices,GeneratedGalaxyWorkspace.QuietTargetLabel(generatedCampaignQuiet)){name="generated-campaign-quiet"};row.Add(quiet);
            seed.RegisterValueChangedCallback(e=>{if(epoch==viewEpoch)generatedCampaignSeed=e.newValue;});
            size.RegisterValueChangedCallback(e=>{if(epoch==viewEpoch)generatedCampaignSize=int.Parse(e.newValue,CultureInfo.InvariantCulture);});
            quiet.RegisterValueChangedCallback(_=>{if(epoch==viewEpoch&&quiet.index>=0)generatedCampaignQuiet=values[quiet.index];});
            DrawStreamedActivitiesMenu(controls);
            var earlier=new Foldout{text="Earlier campaigns · original rules",value=false,name="earlier-generated-campaign-menu"};controls.Add(earlier);
            var start=AddButton(earlier,"New earlier campaign · original rules",()=>{
                void Start(){RunGeneratedCampaignOperation(null);}
                if(demoStarted||model!.HasLocalEdits)Confirm("Start a generated campaign? Your current unsaved progress is replaced only after the new game validates. Existing saves remain intact.",Start);else Start();
            });start.name="generated-campaign-start";
            earlier.Add(Readout("Continue an earlier campaign without changing its rules"));
            var saved=new VisualElement();saved.AddToClassList("review-field-row");earlier.Add(saved);
            foreach(var slot in GeneratedCampaignOptions.Slots())
            {
                var captured=slot;
                var load=AddButton(saved,"Earlier campaign · Continue "+slot.Substring(slot.Length-1),()=>Confirm("Continue this earlier campaign under its original rules? Unsaved progress is replaced only after the save is validated.",()=>RunGeneratedCampaignOperation(captured)));
                load.name="generated-campaign-continue-"+slot;
            }
            generatedCampaignNotice=Readout(generatedCampaignBusy?"Preparing and validating campaign…":profileMenuNotice);generatedCampaignNotice.name="generated-campaign-notice";controls.Add(generatedCampaignNotice);
            DrawPublishedConstructionMenu(earlier);
            DrawActivitiesMenu(earlier);
            controls.SetEnabled(!generatedCampaignBusy&&PlayableResourcesAvailable());
        }
        private void DrawLegacyGeneratedCampaignMenu(VisualElement parent)
        {
            var controls=new VisualElement{name="legacy-generated-campaign-controls"};legacyGeneratedCampaignControls=controls;parent.Add(controls);
            controls.Add(Readout("Earlier generated campaigns · original saves"));
            foreach(var slot in GeneratedCampaignOptions.Slots())
            {
                var captured=slot;
                var load=AddButton(controls,"Continue earlier galaxy "+slot.Substring(slot.Length-1),()=>Confirm("Continue an earlier generated campaign? Its original save is loaded without conversion; unsaved progress is replaced only after validation.",()=>RunGeneratedCampaignOperation(captured,true)));
                load.name="generated-campaign-v1-continue-"+slot;
            }
            controls.SetEnabled(!generatedCampaignBusy&&PlayableResourcesAvailable());
        }
        private object? generatedCampaignOperationToken;
        private async void RunGeneratedCampaignOperation(string? slot,bool legacy=false,bool published=false,bool activities=false,bool streamed=false)
        {
            if(generatedCampaignBusy)return;
            generatedCampaignBusy=true;var epoch=viewEpoch;var prior=model;
            var operationToken=new object();generatedCampaignOperationToken=operationToken;
            var seed=generatedCampaignSeed;var size=generatedCampaignSize;var quiet=generatedCampaignQuiet;
            generatedCampaignControls?.SetEnabled(false);legacyGeneratedCampaignControls?.SetEnabled(false);if(generatedCampaignNotice!=null)generatedCampaignNotice.text=slot==null?"Generating and validating campaign…":"Loading the saved galaxy and campaign…";
            ShowCampaignBusy(operationToken,slot==null?"Generating galaxy and validating campaign…":"Loading and validating campaign…");
            try
            {
                // Resource access is main-thread only. The portable factory/load and trusted file IO run away from the UI.
                var template=LoadPlayableFleetTemplate();var codec=new ReviewNewtonsoftCodec();
                var directory=GeneratedCampaignDirectory(legacy,published,activities,streamed);
                byte[][]? activityBytes=null;
                if(activities||streamed)
                {
                    activityBytes=new byte[4][];var names=new[]{"catalogue","ready-classes","support","activities"};
                    for(int i=0;i<names.Length;i++){var asset=Resources.Load<TextAsset>("ActivitiesV1/"+names[i]);if(asset==null)throw new InvalidOperationException("Activity campaign content unavailable.");activityBytes[i]=asset.bytes;}
                }
                byte[]? supportBytes=null;
                if(published){var supportAsset=Resources.Load<TextAsset>("PublishedConstructionV1/support");if(supportAsset==null)throw new InvalidOperationException("Published playtest support unavailable.");supportBytes=supportAsset.bytes;}
                var settings=new NaturalGalaxySettingsDto{Map=new ScaledGalaxySettingsDto{Seed=seed,SystemCount=size,EmptyRegionCount=size,
                    Width=size==1000?5000000:2500000,Height=size==1000?4000000:2000000,QuietEmptyAreaTargetPermille=quiet,
                    HomeCivilizationDefinitionIds=new List<string>{"civilization.federation","civilization.cardassian"}}};
                var next=await Task.Run(()=>{
                    if(streamed)
                    {
                        if(legacy||published||activities)throw new InvalidOperationException("Distinct streamed campaign profile required.");
                        var content=ActivitiesPlaytestOptions.CreatePackage(template,activityBytes!,codec);
                        var package=HobbyDS9.Application.CampaignStreamedActivitiesApplication.CreatePackage(content);var activityStore=new ReviewStreamedActivitiesSlotStore(directory,codec);
                        return slot==null?new ReviewUiModel(package,settings,codec,activityStore):ReviewUiModel.ContinueStreamedActivities(package,codec,activityStore,slot);
                    }
                    if(activities)
                    {
                        if(legacy||published)throw new InvalidOperationException("Distinct activity campaign profile required.");
                        var package=ActivitiesPlaytestOptions.CreatePackage(template,activityBytes!,codec);var activityStore=new ReviewActivitiesSlotStore(directory,codec);
                        return slot==null?new ReviewUiModel(package,settings,codec,activityStore):ReviewUiModel.ContinueActivities(package,codec,activityStore,slot);
                    }
                    if(published)
                    {
                        if(legacy)throw new InvalidOperationException("Distinct campaign profiles required.");
                        var package=PublishedConstructionPlaytestOptions.CreatePackage(template,supportBytes!,codec);
                        var publishedStore=new ReviewPublishedConstructionSlotStore(directory,codec);
                        return slot==null?new ReviewUiModel(package,settings,codec,publishedStore):ReviewUiModel.ContinuePublishedConstruction(package,codec,publishedStore,slot);
                    }
                    if(legacy)
                    {
                        if(slot==null)throw new InvalidOperationException("An earlier campaign requires an explicit saved slot.");
                        return ReviewUiModel.ContinueGeneratedCampaign(template,codec,new ReviewGeneratedCampaignSlotStore(directory,codec),slot);
                    }
                    var store=new ReviewStaticAstronomyCampaignSlotStore(directory,codec);
                    return slot==null?new ReviewUiModel(template,settings,codec,store):ReviewUiModel.ContinueStaticAstronomyCampaign(template,codec,store,slot);
                });
                if(this==null||!isActiveAndEnabled||viewEpoch!=epoch||!ReferenceEquals(model,prior)||generatedGalaxyOpen)return;
                HideCampaignBusy(operationToken);
                PublishPlayerProfile(next,slot??GeneratedCampaignOptions.Slots()[0]);
            }
            catch(Exception)
            {
                if(this!=null&&viewEpoch==epoch){profileMenuNotice="Generated campaign unavailable. Your current game, unsent edits and saves are kept.";if(generatedCampaignNotice!=null)generatedCampaignNotice.text=profileMenuNotice;}
            }
            finally
            {
                var owned=ReferenceEquals(generatedCampaignOperationToken,operationToken);
                if(owned)
                {generatedCampaignOperationToken=null;generatedCampaignBusy=false;}
                var hidden=HideCampaignBusy(operationToken);
                if(owned&&this!=null&&viewEpoch==epoch&&campaignBusyToken==null){generatedCampaignControls?.SetEnabled(PlayableResourcesAvailable());legacyGeneratedCampaignControls?.SetEnabled(PlayableResourcesAvailable());if(hidden)Draw();}
                else if(owned&&this!=null&&demoLanding&&!generatedGalaxyOpen&&campaignBusyToken==null)Draw();
            }
        }
    }
}
