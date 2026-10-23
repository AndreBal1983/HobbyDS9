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
        private string GeneratedCampaignDirectory()
        {
#if UNITY_EDITOR
            if(generatedCampaignTestDirectory!=null)return generatedCampaignTestDirectory;
#endif
            return Path.Combine(UnityEngine.Application.persistentDataPath,GeneratedCampaignOptions.SaveDirectory);
        }
        private string generatedCampaignSeed="42";
        private int generatedCampaignSize=250,generatedCampaignQuiet=2000;
        private bool generatedCampaignBusy;
        private VisualElement? generatedCampaignControls;
        private Label? generatedCampaignNotice;
        private void DrawGeneratedCampaignMenu(VisualElement parent)
        {
            TextField seed;DropdownField size,quiet;
            var controls=new VisualElement{name="generated-campaign-controls"};generatedCampaignControls=controls;parent.Add(controls);
            controls.Add(Readout("New generated campaign · explore, build and fight"));
            controls.Add(Readout("Two active empires in a seeded galaxy. Only known space is shown. Research, ships, battles and the exact galaxy are retained together in campaign saves."));
            var row=new VisualElement();row.AddToClassList("review-field-row");controls.Add(row);var epoch=viewEpoch;
            seed=new TextField("Seed"){name="generated-campaign-seed",value=generatedCampaignSeed,maxLength=20};row.Add(seed);
            size=new DropdownField("Systems",new List<string>{"250","1000"},generatedCampaignSize.ToString(CultureInfo.InvariantCulture)){name="generated-campaign-size"};row.Add(size);
            var values=GeneratedGalaxyWorkspace.QuietTargetChoices(true);var choices=new List<string>();foreach(var v in values)choices.Add(GeneratedGalaxyWorkspace.QuietTargetLabel(v));
            quiet=new DropdownField("Quiet area",choices,GeneratedGalaxyWorkspace.QuietTargetLabel(generatedCampaignQuiet)){name="generated-campaign-quiet"};row.Add(quiet);
            seed.RegisterValueChangedCallback(e=>{if(epoch==viewEpoch)generatedCampaignSeed=e.newValue;});
            size.RegisterValueChangedCallback(e=>{if(epoch==viewEpoch)generatedCampaignSize=int.Parse(e.newValue,CultureInfo.InvariantCulture);});
            quiet.RegisterValueChangedCallback(_=>{if(epoch==viewEpoch&&quiet.index>=0)generatedCampaignQuiet=values[quiet.index];});
            var start=AddButton(controls,"New generated game",()=>{
                void Start(){RunGeneratedCampaignOperation(null);}
                if(demoStarted||model!.HasLocalEdits)Confirm("Start a generated campaign? Your current unsaved progress is replaced only after the new game validates. Existing saves remain intact.",Start);else Start();
            });start.name="generated-campaign-start";start.AddToClassList("primary-action");
            foreach(var slot in GeneratedCampaignOptions.Slots())
            {
                var captured=slot;
                var load=AddButton(controls,"Continue generated campaign "+slot.Substring(slot.Length-1),()=>Confirm("Continue this generated campaign? Its saved galaxy and accepted state replace unsaved progress only after complete validation.",()=>RunGeneratedCampaignOperation(captured)));
                load.name="generated-campaign-continue-"+slot;
            }
            generatedCampaignNotice=Readout(generatedCampaignBusy?"Preparing and validating campaign…":profileMenuNotice);generatedCampaignNotice.name="generated-campaign-notice";controls.Add(generatedCampaignNotice);
            controls.SetEnabled(!generatedCampaignBusy&&PlayableResourcesAvailable());
        }
        private async void RunGeneratedCampaignOperation(string? slot)
        {
            if(generatedCampaignBusy)return;
            generatedCampaignBusy=true;var epoch=viewEpoch;var prior=model;
            var seed=generatedCampaignSeed;var size=generatedCampaignSize;var quiet=generatedCampaignQuiet;
            generatedCampaignControls?.SetEnabled(false);if(generatedCampaignNotice!=null)generatedCampaignNotice.text=slot==null?"Generating and validating campaign…":"Loading the saved galaxy and campaign…";
            try
            {
                // Resource access is main-thread only. The portable factory/load and trusted file IO run away from the UI.
                var template=LoadPlayableFleetTemplate();var codec=new ReviewNewtonsoftCodec();
                var store=new ReviewGeneratedCampaignSlotStore(GeneratedCampaignDirectory(),codec);
                var settings=new NaturalGalaxySettingsDto{Map=new ScaledGalaxySettingsDto{Seed=seed,SystemCount=size,EmptyRegionCount=size,
                    Width=size==1000?5000000:2500000,Height=size==1000?4000000:2000000,QuietEmptyAreaTargetPermille=quiet,
                    HomeCivilizationDefinitionIds=new List<string>{"civilization.federation","civilization.cardassian"}}};
                var next=await Task.Run(()=>slot==null?new ReviewUiModel(template,settings,codec,store):ReviewUiModel.ContinueGeneratedCampaign(template,codec,store,slot));
                if(this==null||!isActiveAndEnabled||viewEpoch!=epoch||!ReferenceEquals(model,prior)||generatedGalaxyOpen)return;
                PublishPlayerProfile(next,slot??GeneratedCampaignOptions.Slots()[0]);
            }
            catch(Exception)
            {
                if(this!=null&&viewEpoch==epoch){profileMenuNotice="Generated campaign unavailable. Your current game, unsent edits and saves are kept.";if(generatedCampaignNotice!=null)generatedCampaignNotice.text=profileMenuNotice;}
            }
            finally
            {
                generatedCampaignBusy=false;
                if(this!=null&&viewEpoch==epoch)generatedCampaignControls?.SetEnabled(PlayableResourcesAvailable());
                else if(this!=null&&demoLanding&&!generatedGalaxyOpen)Draw();
            }
        }
    }
}
