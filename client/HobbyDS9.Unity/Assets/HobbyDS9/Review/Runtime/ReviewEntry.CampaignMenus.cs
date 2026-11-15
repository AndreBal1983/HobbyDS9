#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HobbyDS9.TacticalReviewClient;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private sealed class CampaignDialogAction
        {
            internal readonly string Label; internal readonly Action Run;
            internal CampaignDialogAction(string label,Action run){Label=label;Run=run;}
        }
        private VisualElement? campaignDialogInvoker;
        private string campaignDialogInvokerName="";
        private Func<bool>? campaignDialogCurrent;
        private void ShowEngineeringDialog(ReviewEngineeringDialog project)
        {
            if(confirmation!=null)return;
            var choices=new List<CampaignDialogAction>();
            if(project.Commit!=null)choices.Add(new CampaignDialogAction(project.ActionLabel,project.Commit));
            if(project.Pause!=null)choices.Add(new CampaignDialogAction("Pause project",project.Pause));
            ShowCampaignDialog(project.Title,project.Detail,"Close",null,choices.ToArray());
            AppendEngineeringOutputs(project.Outputs);
        }
        private void AppendEngineeringOutputs(IReadOnlyList<HobbyDS9.Application.EngineeringOutputSnapshot> outputs)
        {
            var body=confirmation?.Q<ScrollView>("player-confirmation-details");if(body==null)return;
            var bridge=Type.GetType("HobbyDS9.UnityClient.LcarsCapacityDetails, HobbyDS9.UnityClient");
            foreach(var output in outputs)
            {
                VisualElement? widget=null;
                if(output.Component!=null)
                    widget=bridge?.GetMethod("Module")?.Invoke(null,new object?[]{output.Component,output.ParentComponent,HobbyDS9.Contracts.ShipDesign.OperatingProfileKind.Cruise}) as VisualElement;
                else if(output.Hull!=null)widget=bridge?.GetMethod("Hull")?.Invoke(null,new object?[]{output.Hull,output.ParentHull}) as VisualElement;
                if(widget!=null)body.Add(widget);
                else if(output.Component!=null)body.Add(Readout(output.DisplayName+" · capacity blocks unavailable · module space "+output.Component.Capacity+" · required crew "+output.Component.CrewRequired+" · raw power supply "+output.Component.PowerSupply+" · Industry "+output.Component.Cost.Industry+" · catalogue credits "+output.Component.Cost.Credits));
                else if(output.Hull!=null)body.Add(Readout(output.DisplayName+" · capacity blocks unavailable · hull space "+output.Hull.Capacity+" · distribution "+output.Hull.PowerDistributionLimit+" · minimum crew "+output.Hull.MinimumCrew+" · Industry "+output.Hull.Cost.Industry+" · catalogue credits "+output.Hull.Cost.Credits));
                else body.Add(Readout("Static design · "+output.DisplayName+" · module capacity details unavailable."));
            }
        }
        private void ShowCampaignDialog(string title,string detail,string safeLabel,Action? safe,
            params CampaignDialogAction[] choices)
        {
            ReviewLcarsSelector.CloseOpen();
            if(confirmation!=null||campaignBusyToken!=null||model==null||model.Busy||model.Closed)return;
            var owner=model;var controller=owner.Current;var frame=controller.Frame;var epoch=viewEpoch;
            Action? selected=null;
            bool Current()=>this!=null&&isActiveAndEnabled&&viewEpoch==epoch&&ReferenceEquals(model,owner)
                &&ReferenceEquals(owner.Current,controller)&&ReferenceEquals(controller.Frame,frame)&&!owner.Busy&&!owner.Closed;
            var callbacks=modalPermission.TryOpen(()=>{
                var valid=Current();ClearConfirmation();if(valid)selected?.Invoke();
            },()=>{ClearConfirmation();safe?.Invoke();});
            if(callbacks==null)return;
            campaignDialogCurrent=Current;
            campaignDialogInvoker=root!.panel?.focusController.focusedElement as VisualElement;
            campaignDialogInvokerName=campaignDialogInvoker?.name??"";
            foreach(var child in root.Children())child.SetEnabled(false);
            confirmation=new VisualElement{name="review-one-use-confirmation"};
            confirmation.AddToClassList("review-confirmation");confirmation.AddToClassList("campaign-dialog-backdrop");
            var box=new VisualElement{name="player-confirmation-summary"};box.AddToClassList("campaign-dialog");
            var heading=Readout(title);heading.name="campaign-dialog-title";heading.AddToClassList("campaign-menu-title");box.Add(heading);
            var body=new ScrollView(ScrollViewMode.Vertical){name="player-confirmation-details"};
            body.AddToClassList("campaign-dialog-body");
            foreach(var paragraph in detail.Split(new[]{"\n\n"},StringSplitOptions.RemoveEmptyEntries))
            {
                if(paragraph.StartsWith("Travel details\n",StringComparison.Ordinal))
                {var travel=new Foldout{text="Travel details",value=false,name="campaign-dialog-travel"};travel.Add(Readout(paragraph.Substring("Travel details\n".Length)));body.Add(travel);}
                else
                {
                    var fact=Readout(paragraph);
                    if(title=="Interrupt survey?"&&body.childCount==0)
                    {fact.name="campaign-dialog-survey-targets";fact.style.unityFontStyleAndWeight=FontStyle.Bold;fact.style.marginBottom=10;}
                    body.Add(fact);
                }
            }
            box.Add(body);
            var footer=new VisualElement();footer.AddToClassList("campaign-dialog-actions");box.Add(footer);
            var dismiss=AddButton(footer,safeLabel,callbacks.Cancel,true);dismiss.name="campaign-dialog-safe";
            var footerButtons=new List<Button>{dismiss};
            foreach(var choice in choices)
            {
                var captured=choice;
                var button=AddButton(footer,captured.Label,()=>{selected=captured.Run;callbacks.Confirm();},true);
                button.AddToClassList("primary-action");
                footerButtons.Add(button);
            }
            confirmation.RegisterCallback<KeyDownEvent>(e=>{
                if(e.keyCode==KeyCode.Escape){e.StopImmediatePropagation();callbacks.Cancel();return;}
                if(e.keyCode!=KeyCode.Tab||e.altKey||e.ctrlKey||e.commandKey)return;
                // The documented FocusController hook suppresses its default Tab
                // traversal; the detail disclosure and footer actions own this ring.
                root?.panel?.focusController.IgnoreEvent(e);e.StopImmediatePropagation();
                var controls=body.Query<Toggle>().ToList().Cast<VisualElement>().Concat(footerButtons).Where(CampaignCanFocus).ToList();
                if(controls.Count==0)return;
                var focused=root?.panel?.focusController.focusedElement as VisualElement;
                var index=focused!=null?controls.IndexOf(focused):-1;
                var next=e.shiftKey?(index<=0?controls.Count-1:index-1):(index+1)%controls.Count;
                e.StopImmediatePropagation();controls[next].Focus();
            },TrickleDown.TrickleDown);
            confirmation.Add(box);root.Add(confirmation);dismiss.Focus();
        }
        private bool CampaignCanFocus(VisualElement? element)
        {
            if(element==null||root?.panel==null||element.panel!=root.panel||!root.Contains(element)
                ||!element.focusable||element.tabIndex<0||!element.enabledInHierarchy)return false;
            var bounds=element.worldBound;
            if(bounds.width<=0||bounds.height<=0)return false;
            for(var ancestor=element;ancestor!=null;ancestor=ancestor.parent)
            {
                if(!ancestor.enabledInHierarchy||ancestor.resolvedStyle.display==DisplayStyle.None
                    ||ancestor.resolvedStyle.visibility!=Visibility.Visible||ancestor.resolvedStyle.opacity<=0)return false;
                if(ancestor is ScrollView scroll&&!bounds.Overlaps(scroll.contentViewport.worldBound))return false;
                if(ancestor.style.overflow.value==Overflow.Hidden&&!bounds.Overlaps(ancestor.worldBound))return false;
            }
            return bounds.Overlaps(root.worldBound);
        }
        private void RestoreCampaignDialogFocus(VisualElement? previous,string name)
        {
            var epoch=viewEpoch;
            root!.schedule.Execute(()=>{
                if(epoch!=viewEpoch||confirmation!=null||root?.panel==null)return;
                var target=CampaignCanFocus(previous)?previous:name.Length>0?root.Q<VisualElement>(name):null;
                if(!CampaignCanFocus(target))target=root.Q<Button>("demo-menu");
                if(!CampaignCanFocus(target))target=root.Q<Button>("campaign-menu-resume");
                if(!CampaignCanFocus(target))target=root.Query<Button>().ToList().FirstOrDefault(CampaignCanFocus);
                target?.Focus();
            });
        }

        private enum CampaignMenuPage { Start, Campaign, New, Archive, Settings, Combat }
        private CampaignMenuPage campaignMenuPage;
        private CampaignMenuPage campaignArchiveReturn=CampaignMenuPage.Campaign;
        private bool campaignArchiveSaving=true;
        private bool campaignArchivePreferEmpty;
        private string campaignMenuNotice="";
        private Action? campaignAfterSave;
        private string campaignContinuationDescription="";
        private string campaignContinuationLabel="",campaignProtectedLoadSlot="";
        private ReviewUiModel? campaignSavedOwner;
        private long campaignSavedRevision;
        private int campaignSavedTurn;
        private bool CampaignNeedsSave=>demoStarted&&(!ReferenceEquals(model,campaignSavedOwner)
            ||model!.Current.Frame.Status.CampaignRevision!=campaignSavedRevision);
        private void MarkCampaignSaved(ReviewUiModel owner)
        {
            campaignSavedOwner=owner;campaignSavedRevision=owner.Current.Frame.Status.CampaignRevision;
            campaignSavedTurn=owner.Current.Frame.Status.Turn;
        }
        private void ShowCampaignMenuPage(CampaignMenuPage page)
        {campaignMenuPage=page;demoLanding=true;campaignArchiveNeedsRefresh=true;viewEpoch++;Build();}
        private void ResumeCampaign()
        {
            campaignAfterSave=null;campaignContinuationLabel="";campaignProtectedLoadSlot="";
            campaignArchiveOverrideProfile=null;
            demoLanding=false;viewEpoch++;Build();
        }
        private void OpenCampaignArchive(bool saving,string? profile=null)
        {
            campaignAfterSave=null;campaignContinuationLabel="";campaignProtectedLoadSlot="";
            campaignArchiveOverrideProfile=profile;
            campaignArchiveSaving=saving;campaignArchivePreferEmpty=saving;campaignArchiveRows=null;
            campaignArchiveReturn=demoLanding?campaignMenuPage:CampaignMenuPage.Campaign;
            ShowCampaignMenuPage(CampaignMenuPage.Archive);
        }
        private void RequestCampaignDeparture(string title,string verb,Action proceed,string? protectedSlot=null,string? destination=null)
        {
            if(!demoStarted||!CampaignNeedsSave&&!model!.HasLocalEdits){proceed();return;}
            var detail=CampaignNeedsSave?"Accepted campaign changes have not been saved in this menu session.":"Your accepted campaign is saved, but you have unsubmitted edits.";
            if(model!.HasLocalEdits)detail+=" Unsubmitted edits are not included in a campaign save and will be discarded if you continue.";
            detail+=" Existing saves are kept unless you explicitly choose to replace one.";
            ShowCampaignDialog(title,detail,"Keep playing",ResumeCampaign,
                new CampaignDialogAction(verb+" without saving",proceed),
                new CampaignDialogAction("Save and "+verb.ToLowerInvariant(),()=>{
                    campaignAfterSave=proceed;campaignContinuationLabel=verb.ToLowerInvariant();
                    campaignContinuationDescription=destination??title;
                    campaignArchiveOverrideProfile=null;
                    campaignProtectedLoadSlot=protectedSlot??"";campaignArchiveSaving=true;campaignArchivePreferEmpty=true;
                    campaignArchiveReturn=CampaignMenuPage.Campaign;campaignArchiveRows=null;
                    ShowCampaignMenuPage(CampaignMenuPage.Archive);
                }));
        }
        private void QuitCampaignPlayer()
        {
            ReviewShutdownLifecycle.Mark("ReviewEntry",shutdownLabel,"close.begin");
            if(!model!.Close(true)){campaignMenuNotice="The campaign cannot close while an operation is pending.";Draw();return;}
            ReviewShutdownLifecycle.Mark("ReviewEntry",shutdownLabel,"close.return.true");
            ReviewShutdownLifecycle.Mark("ReviewEntry",shutdownLabel,"quit.call");
            UnityEngine.Application.Quit();
            ReviewShutdownLifecycle.Mark("ReviewEntry",shutdownLabel,"quit.return");
        }
        private void DrawCampaignMenus()
        {
            if(campaignMenuPage==CampaignMenuPage.Archive){DrawCampaignArchive();return;}
            if(campaignMenuPage==CampaignMenuPage.New){DrawCampaignNew();return;}
            if(campaignMenuPage==CampaignMenuPage.Settings){DrawCampaignSettings();return;}
            if(campaignMenuPage==CampaignMenuPage.Combat){DrawCombatPlaytestMenu();return;}
            var menuClock=System.Diagnostics.Stopwatch.StartNew();
            var paused=campaignMenuPage==CampaignMenuPage.Campaign&&demoStarted;
            var panel=PagePanel(paused?"Campaign menu":"HobbyDS9");panel.AddToClassList("campaign-menu");
            panel.Add(Readout("GALACTIC CAMPAIGNS"));
#if HOBBYDS9_PB_20261113
            var buildIdentity=Readout(UnityEngine.Application.version+" · Playtest candidate · Unity "+UnityEngine.Application.unityVersion);
            buildIdentity.name="playtest-build-identity";panel.Add(buildIdentity);
#endif
            var summary=new VisualElement();summary.AddToClassList("campaign-menu-summary");panel.Add(summary);
            if(demoStarted)
            {
                var frame=model!.Current.Frame;
                summary.Add(Readout(OwnerName(frame.Actor)+" · Week "+frame.Status.Turn));
                if(model.CombatPlaytest)summary.Add(Readout("Combat playtest · Authored Bajor fixture · Separate test saves"));
                summary.Add(Readout(frame.Strategy!.Colonies.Count+" owned colonies · "+frame.Strategy.Ships.Count+" owned ships"));
                summary.Add(Readout(CampaignNeedsSave?"Accepted progress has not been saved since the last successful menu save/load.":"Accepted campaign saved at week "+campaignSavedTurn+"."));
                if(model.HasLocalEdits)summary.Add(Readout("Unsubmitted edits are held in memory. Saving does not include them."));
            }
            else
            {
                EnsureCampaignArchive();
                var latest=NewestCampaignSave();
                var saveSummary=Readout(latest==null?"Explore a galaxy, research technology and build your fleet.":latest.Summary);saveSummary.name="campaign-archive-summary";summary.Add(saveSummary);
                var archiveStatus=Readout(campaignArchiveRows==null||campaignArchiveScanQueued||campaignArchiveScanToken!=null?"Checking saved slots…":"Save compatibility is validated when loading.");archiveStatus.name="campaign-archive-status";summary.Add(archiveStatus);
            }
            var layout=new VisualElement();layout.AddToClassList("campaign-menu-layout");panel.Add(layout);
            var commands=new VisualElement();commands.AddToClassList("campaign-menu-commands");layout.Add(commands);
            var context=new VisualElement();context.AddToClassList("campaign-menu-context");layout.Add(context);
            summary.RemoveFromHierarchy();context.Add(summary);
            var art=ConstructionCardArt.Ship("hull.galaxy-cruiser.v1");
            if(art!=null)
            {
                var image=new Image{image=art,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
                image.AddToClassList("campaign-menu-art");context.Add(image);
            }
            if(demoStarted)
            {
                var resume=AddButton(commands,"Resume campaign",ResumeCampaign);resume.name="campaign-menu-resume";
                resume.AddToClassList("primary-action");ScheduleDemoMenuFocus(resume);
            }
            else
            {
                var latest=NewestCampaignSave();
                var next=AddButton(commands,latest==null?"Continue · Checking saved slots…":"Continue · "+SlotLabel(latest.Slot),()=>{var selected=NewestCampaignSave();if(selected!=null)LoadCampaignMenuSlot(selected.Slot);});
                next.name="campaign-menu-continue";next.SetEnabled(latest!=null);if(latest!=null)next.AddToClassList("primary-action");
            }
            if(!paused)
            {
                var start=AddButton(commands,"New campaign",()=>ShowCampaignMenuPage(CampaignMenuPage.New));
                start.name="campaign-menu-new";if(!demoStarted&&NewestCampaignSave()==null)start.AddToClassList("primary-action");
                AddButton(commands,"Combat playtest",OpenCombatPlaytestMenu).name="campaign-menu-combat";
            }
            if(paused)AddButton(commands,"Save game",()=>OpenCampaignArchive(true)).name="campaign-menu-save";
            AddButton(commands,"Load game",()=>OpenCampaignArchive(false)).name="campaign-menu-load";
            AddButton(commands,"Settings",()=>{campaignSettingsMenuOrigin=campaignMenuPage;ShowCampaignMenuPage(CampaignMenuPage.Settings);}).name="campaign-menu-settings";
            if(paused)AddButton(commands,"Main menu",()=>ShowCampaignMenuPage(CampaignMenuPage.Start)).name="campaign-menu-main";
            AddButton(commands,"Quit to desktop",()=>RequestCampaignDeparture("Quit to desktop?","Quit",QuitCampaignPlayer)).name="campaign-menu-quit";
            if(campaignMenuNotice.Length>0)panel.Add(Readout(campaignMenuNotice));
            if(profileMenuNotice.Length>0)panel.Add(Readout(profileMenuNotice));
            if(Environment.GetCommandLineArgs().Contains("--hobbyds9-command-timing"))
                Debug.Log(FormattableString.Invariant($"HOBBYDS9_MENU_READY startupMs={Time.realtimeSinceStartupAsDouble*1000:F2} buildMs={menuClock.Elapsed.TotalMilliseconds:F2} archivePending={campaignArchiveScanQueued||campaignArchiveScanToken!=null}"));
        }
        private void DrawCampaignNew()
        {
            var panel=PagePanel("New campaign");panel.AddToClassList("campaign-menu");
            panel.AddToClassList("campaign-setup");
            var epoch=viewEpoch;var owner=model;
            bool CurrentForm()=>epoch==viewEpoch&&ReferenceEquals(owner,model);
            panel.Add(Readout("Federation · Two empires. Existing save slots remain intact."));
            if(campaignMenuNotice.Length>0)panel.Add(Readout(campaignMenuNotice));
            var seed=new TextField("Galaxy seed"){value=generatedCampaignSeed,maxLength=20,name="generated-campaign-seed"};
            seed.AddToClassList("generated-campaign-seed");panel.Add(seed);
            seed.RegisterValueChangedCallback(e=>{if(CurrentForm())generatedCampaignSeed=e.newValue;});
            var advanced=new Foldout{text="Advanced galaxy settings",value=false};panel.Add(advanced);
            var size=new DropdownField("Systems",new List<string>{"250","1000"},generatedCampaignSize.ToString(CultureInfo.InvariantCulture));advanced.Add(size);
            size.RegisterValueChangedCallback(e=>{if(CurrentForm()&&int.TryParse(e.newValue,out var value))generatedCampaignSize=value;});
            var values=GeneratedGalaxyWorkspace.QuietTargetChoices(true);
            var quiet=new DropdownField("Quiet area",values.Select(GeneratedGalaxyWorkspace.QuietTargetLabel).ToList(),GeneratedGalaxyWorkspace.QuietTargetLabel(generatedCampaignQuiet));advanced.Add(quiet);
            quiet.RegisterValueChangedCallback(_=>{if(CurrentForm()&&quiet.index>=0)generatedCampaignQuiet=values[quiet.index];});
            AddButton(panel,"Back",()=>ShowCampaignMenuPage(demoStarted?CampaignMenuPage.Campaign:CampaignMenuPage.Start));
            var start=AddButton(panel,"Start campaign",()=>RequestCampaignDeparture("Replace the open campaign?","Start",()=>
                RunGeneratedCampaignOperation(null,colonisationCycle:true,completed:ok=>{
                    if(ok){campaignSavedOwner=null;campaignArchiveRows=null;campaignMenuNotice="";}
                    else{campaignMenuNotice="The new campaign could not be prepared. Your current session and saves are kept.";Draw();}
                })));
            start.name="colonisation-cycle-campaign-start";start.AddToClassList("primary-action");start.SetEnabled(PlayableResourcesAvailable());
        }
        private Action? campaignSettingsReturn;
        private ReviewWorkspacePage campaignSettingsPriorPage;
        private CampaignMenuPage campaignSettingsMenuOrigin;
        private void DrawCampaignSettings()
        {
            var panel=PagePanel("Settings");panel.AddToClassList("campaign-menu");
            panel.Add(Readout("Audio and Display use the game's existing controls. Changes apply immediately; Done does not roll them back."));
            var groups=new VisualElement();groups.AddToClassList("settings-groups");panel.Add(groups);
            var audio=new VisualElement();audio.AddToClassList("settings-group");groups.Add(audio);
            audio.Add(Readout("Sound aboard the console"));audio.Add(Readout("Music playback, master volume and independent button sounds."));
            AddButton(audio,"Audio",()=>OpenDemoSettings(ReviewWorkspacePage.Audio));
            var display=new VisualElement();display.AddToClassList("settings-group");groups.Add(display);
            display.Add(Readout("Window & display"));display.Add(Readout("Current size, windowed mode and borderless fullscreen."));
            AddButton(display,"Display",()=>OpenDemoSettings(ReviewWorkspacePage.Display));
            AddButton(panel,"Done",()=>ShowCampaignMenuPage(campaignSettingsMenuOrigin));
        }
        private void DrawCampaignSettingsReturn()
        {
            if(campaignSettingsReturn==null)return;
            var back=AddButton(content!,"Done · Back to menu",ReturnFromCampaignSettings);back.name="campaign-settings-done";
        }
        private void ReturnFromCampaignSettings()
        {
            var back=campaignSettingsReturn;campaignSettingsReturn=null;
            workspace!.Navigate(campaignSettingsPriorPage);back?.Invoke();
        }

        private sealed class CampaignArchiveRow
        {
            internal readonly ReviewArchiveFile File;
            internal string Slot=>File.Slot;
            internal bool Occupied=>File.Occupied;
            internal bool CanAttemptLoad=>Occupied&&!File.Unavailable&&!Rejected;
            internal DateTime ModifiedUtc=>File.ModifiedUtc;
            internal readonly bool Rejected;
            internal string Summary=>SlotLabel(Slot)+(!Occupied?" · Empty":File.Unavailable?" · Unavailable":Rejected?" · Unavailable or incompatible":" · Saved file · "+ModifiedUtc.ToLocalTime().ToString("g",CultureInfo.CurrentCulture)+" · Validate on load");
            internal CampaignArchiveRow(ReviewArchiveFile file,bool rejected=false){File=file;Rejected=rejected;}
        }
        private List<CampaignArchiveRow>? campaignArchiveRows;
        private bool campaignArchiveScanQueued;
        private object? campaignArchiveScanToken;
        private string campaignArchiveProfile="";
        private readonly Dictionary<string,List<CampaignArchiveRow>> campaignArchiveCache=new Dictionary<string,List<CampaignArchiveRow>>(StringComparer.Ordinal);
        private bool campaignArchiveNeedsRefresh=true;
        private int campaignArchiveGeneration;
        private ReviewUiModel? campaignArchiveScanOwner;
        private Action<string>? campaignArchiveMetadataProbe=null; // Dedicated receiving hold; no player delay.
        private void CancelCampaignArchiveScan()
        {
            campaignArchiveGeneration++;campaignArchiveScanQueued=false;campaignArchiveNeedsRefresh=true;
            if(campaignArchiveScanToken is CancellationTokenSource cancellation)cancellation.Cancel();
            campaignArchiveScanToken=null;campaignArchiveScanOwner=null;
        }
        private bool ObservingCampaignArchive=>demoLanding&&(campaignMenuPage==CampaignMenuPage.Start||campaignMenuPage==CampaignMenuPage.Archive||campaignMenuPage==CampaignMenuPage.Combat)
            ||!demoLanding&&workspace?.Page==ReviewWorkspacePage.Saves;
        private string? campaignArchiveOverrideProfile;
        private string CampaignArchiveProfile()=>campaignArchiveSaving?CurrentCampaignArchiveProfile():campaignArchiveOverrideProfile??CurrentCampaignArchiveProfile();
        private string CurrentCampaignArchiveProfile()=>!demoStarted||model!.ColonisationCycleCampaign?"cycle":model.ColonisationRepeatCampaign?"repeat":model.ColonisationGrowthCampaign?"growth":model.ColonisationExpansionCampaign?"expansion":model.ColonisationRosterCampaign?"roster":model.ColonisationCampaign?"colony":model.StreamedActivitiesCampaign?"streamed":model.ActivitiesCampaign?"activities":model.PublishedConstructionCampaign?"published":model.GeneratedCampaign?(model.StaticAstronomyCampaign?"astronomy":"generated"):model.FrontierPreview?"frontier":model.CombatPlaytest?"combat":model.SingleplayerDemo?"demo":"fleet";
        private string[] CampaignArchiveSlots(string profile)=>profile=="combat"?CombatPlaytestOptions.Slots():profile=="frontier"?FrontierPreviewOptions.Slots():profile=="demo"?new[]{"demo-1","demo-2","demo-3"}:profile=="fleet"?PlayableFleetOptions.Slots():GeneratedCampaignOptions.Slots();
        private static string SlotLabel(string slot)=>(slot.StartsWith("combat-",StringComparison.Ordinal)?"Combat playtest":"Campaign")+" · Slot "+slot.Substring(slot.Length-1);
        private CampaignArchiveRow? NewestCampaignSave()=>campaignArchiveRows?.Where(r=>r.CanAttemptLoad)
            .OrderByDescending(r=>r.ModifiedUtc).ThenBy(r=>r.Slot,StringComparer.Ordinal).FirstOrDefault();
        private string CampaignArchivePath(string profile,string slot)
        {
            string directory,prefix;
            switch(profile)
            {
                case "cycle":directory=ColonisationCycleCampaignDirectory();prefix="unity-campaign-colonisation-cycle-v1-";break;
                case "repeat":directory=GeneratedCampaignDirectory(colonisationRepeat:true);prefix="unity-campaign-colonisation-repeat-v1-";break;
                case "growth":directory=GeneratedCampaignDirectory(colonisationGrowth:true);prefix="unity-campaign-colonisation-growth-v2-";break;
                case "expansion":directory=GeneratedCampaignDirectory(colonisationExpansion:true);prefix="unity-campaign-colonisation-expansion-v1-";break;
                case "roster":directory=GeneratedCampaignDirectory(colonisationRoster:true);prefix="unity-campaign-colonisation-v2-";break;
                case "colony":directory=GeneratedCampaignDirectory(colonisation:true);prefix="unity-campaign-colonisation-v1-";break;
                case "streamed":directory=GeneratedCampaignDirectory(streamed:true);prefix="unity-campaign-streamed-activities-v1-";break;
                case "activities":directory=GeneratedCampaignDirectory(activities:true);prefix="unity-campaign-activities-v1-";break;
                case "published":directory=GeneratedCampaignDirectory(published:true);prefix="unity-published-construction-v1-";break;
                case "astronomy":directory=GeneratedCampaignDirectory();prefix="unity-generated-static-astronomy-v2-";break;
                case "generated":directory=GeneratedCampaignDirectory(legacy:true);prefix="unity-generated-campaign-v7-";break;
                case "frontier":directory=Path.Combine(UnityEngine.Application.persistentDataPath,FrontierPreviewOptions.SaveDirectory);prefix="unity-frontier-review-v5-";break;
                case "demo":directory=Path.Combine(UnityEngine.Application.persistentDataPath,"HobbyDS9SingleplayerDemoOnly-v4");prefix="unity-singleplayer-demo-";break;
                case "combat":directory=Path.Combine(UnityEngine.Application.persistentDataPath,CombatPlaytestOptions.SaveDirectory);prefix="unity-singleplayer-demo-";break;
                default:directory=Path.Combine(UnityEngine.Application.persistentDataPath,PlayableFleetOptions.SaveDirectory);prefix="unity-playable-fleet-review-v6-";break;
            }
            if(!CampaignArchiveSlots(profile).Contains(slot))throw new InvalidOperationException("Unknown fixed slot.");
            // Metadata only. Actual reads/writes retain their original confined typed stores.
            return Path.Combine(directory,prefix+slot+".json");
        }
        private async Task<ReviewUiModel> BuildMenuCandidateAsync(string profile,string slot)
        {
            if(profile!="combat"&&profile!="demo"&&profile!="frontier"&&profile!="fleet")
                return await BuildGeneratedCampaignCandidateAsync(slot,legacy:profile=="generated",published:profile=="published",
                    activities:profile=="activities",streamed:profile=="streamed",colonisation:profile=="colony",
                    colonisationRoster:profile=="roster",colonisationExpansion:profile=="expansion",colonisationGrowth:profile=="growth",
                    colonisationRepeat:profile=="repeat",colonisationCycle:profile=="cycle");
            var codec=new ReviewNewtonsoftCodec();
            var candidate=profile=="combat"?NewCombatPlaytestModel():profile=="frontier"?NewFrontierModel():profile=="fleet"?NewPlayableFleetModel()
                :new ReviewUiModel(demoSource!,codec,new ReviewDemoSlotStore(Path.Combine(UnityEngine.Application.persistentDataPath,"HobbyDS9SingleplayerDemoOnly-v4"),codec));
            try{if(await Task.Run(()=>candidate.Load(slot,true)))return candidate;throw new InvalidOperationException("Unavailable campaign.");}
            catch{CloseUnpublishedPlayer(candidate);throw;}
        }
        private void EnsureCampaignArchive()
        {
            var profile=CampaignArchiveProfile();
            if(campaignArchiveProfile!=profile)
            {
                CancelCampaignArchiveScan();campaignArchiveProfile=profile;
                campaignArchiveRows=campaignArchiveCache.TryGetValue(profile,out var cached)?cached:null;
            }
            if(campaignArchiveRows!=null&&campaignArchiveRows.Count!=CampaignArchiveSlots(profile).Length)campaignArchiveNeedsRefresh=true;
            if((campaignArchiveRows!=null&&!campaignArchiveNeedsRefresh)||campaignArchiveScanQueued||campaignArchiveScanToken!=null)return;
            campaignArchiveScanQueued=true;var generation=campaignArchiveGeneration;
            root!.schedule.Execute(()=>{
                if(generation!=campaignArchiveGeneration)return;
                campaignArchiveScanQueued=false;
                if(this!=null&&isActiveAndEnabled&&confirmation==null&&campaignBusyToken==null&&ObservingCampaignArchive)ScanCampaignArchive();
            });
        }
        private async void ScanCampaignArchive(string[]? slots=null,string? requestedProfile=null)
        {
            var owner=model;var profile=requestedProfile??CampaignArchiveProfile();
            CancelCampaignArchiveScan();var generation=campaignArchiveGeneration;
            var cancellation=new CancellationTokenSource();var token=cancellation.Token;
            campaignArchiveScanToken=cancellation;campaignArchiveScanOwner=owner;campaignArchiveNeedsRefresh=false;
            // Resolve Unity-owned directories/profile data on the owning thread.
            var paths=Array.Empty<KeyValuePair<string,string>>();
            var probe=campaignArchiveMetadataProbe;var clock=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                paths=(slots??CampaignArchiveSlots(profile)).Select(slot=>new KeyValuePair<string,string>(slot,CampaignArchivePath(profile,slot))).ToArray();
                var files=await Task.Run(()=>ReviewArchiveMetadata.Read(paths,token,probe));
                if(this==null||!isActiveAndEnabled||token.IsCancellationRequested||generation!=campaignArchiveGeneration
                    ||!ReferenceEquals(model,owner)||!ReferenceEquals(campaignArchiveScanToken,cancellation))return;
                var rows=campaignArchiveCache.TryGetValue(profile,out var cached)?new List<CampaignArchiveRow>(cached):new List<CampaignArchiveRow>();
                foreach(var file in files)
                {
                    var previous=rows.FirstOrDefault(row=>row.Slot==file.Slot);
                    var replacement=previous!=null&&previous.File.SameObservation(file)?previous:new CampaignArchiveRow(file);
                    rows.RemoveAll(row=>row.Slot==file.Slot);rows.Add(replacement);
                }
                rows=rows.OrderBy(row=>row.Slot,StringComparer.Ordinal).ToList();campaignArchiveCache[profile]=rows;
                if(CampaignArchiveProfile()==profile){campaignArchiveProfile=profile;campaignArchiveRows=rows;}
                if(Environment.GetCommandLineArgs().Contains("--hobbyds9-command-timing"))
                    Debug.Log(FormattableString.Invariant($"HOBBYDS9_ARCHIVE_TIMING profile={profile} slots={paths.Length} elapsedMs={clock.Elapsed.TotalMilliseconds:F2} payloadLoads=0"));
            }
            catch(OperationCanceledException){} // Superseded scans never publish partial results.
            catch(Exception)
            {
                if(generation==campaignArchiveGeneration&&ReferenceEquals(model,owner))campaignMenuNotice="Saved slots could not be listed. You can start a new campaign or refresh the archive.";
            }
            finally
            {
                var current=ReferenceEquals(campaignArchiveScanToken,cancellation);
                if(current){campaignArchiveScanToken=null;campaignArchiveScanOwner=null;}
                cancellation.Dispose();
                if(current&&this!=null&&isActiveAndEnabled&&ReferenceEquals(model,owner)&&CampaignArchiveProfile()==profile
                    &&confirmation==null&&campaignBusyToken==null&&!owner!.Busy&&!owner.Closed)RefreshCampaignArchivePresentation();
            }
        }
        private void RefreshCampaignArchivePresentation()
        {
            if(!ObservingCampaignArchive)return;
            if(campaignMenuPage==CampaignMenuPage.Archive||!demoLanding&&workspace?.Page==ReviewWorkspacePage.Saves){Draw();return;}
            // Keep New/Settings and the focused menu control mounted throughout discovery.
            var latest=NewestCampaignSave();
            var summary=root?.Q<Label>("campaign-archive-summary");if(summary!=null)summary.text=latest?.Summary??"Explore a galaxy, research technology and build your fleet.";
            var status=root?.Q<Label>("campaign-archive-status");if(status!=null)status.text=campaignMenuNotice.Length>0?campaignMenuNotice:"Save compatibility is validated when loading.";
            var next=root?.Q<Button>("campaign-menu-continue");if(next!=null){next.text=latest==null?"Continue · No saved slot":"Continue · "+SlotLabel(latest.Slot);next.SetEnabled(latest!=null);next.EnableInClassList("primary-action",latest!=null);}
            var combat=root?.Q<Button>("combat-playtest-continue");if(combat!=null)combat.SetEnabled(latest!=null);
        }
        private void RefreshSavedCampaignSlot(string profile,string slot)
        {
            // An accepted write invalidates only this row; unrelated saves are not replayed.
            if(campaignArchiveCache.TryGetValue(profile,out var rows))
                campaignArchiveCache[profile]=rows.Where(row=>row.Slot!=slot).ToList();
            ScanCampaignArchive(new[]{slot},profile);
        }
        private void RejectCampaignArchiveRow(string profile,CampaignArchiveRow? observed)
        {
            if(observed==null||!campaignArchiveCache.TryGetValue(profile,out var rows))return;
            var index=rows.FindIndex(row=>ReferenceEquals(row,observed));if(index<0)return;
            var changed=new List<CampaignArchiveRow>(rows);changed[index]=new CampaignArchiveRow(observed.File,true);campaignArchiveCache[profile]=changed;
            if(CampaignArchiveProfile()==profile)campaignArchiveRows=changed;
        }
        private void DrawCampaignArchive()
        {
            EnsureCampaignArchive();
            var panel=PagePanel(campaignArchiveSaving?"Save game":"Load game");panel.AddToClassList("campaign-menu");
            panel.Add(Readout("Archive: "+(CampaignArchiveProfile()=="combat"?"Combat playtest":CampaignArchiveProfile()=="cycle"?"Generated campaign":CampaignArchiveProfile()=="demo"?"Bajor demo":"Separate campaign profile")));
            panel.Add(Readout("Three fixed slots. Compatibility is validated only when loading. Slot selection never saves or loads."));
            if(campaignAfterSave!=null)panel.Add(Readout("After a successful save: "+campaignContinuationDescription));
            if(campaignArchiveRows==null){panel.Add(Readout("Checking saved slots…"));AddButton(panel,"Back",()=>ShowCampaignMenuPage(campaignArchiveReturn));return;}
            if(campaignArchiveScanQueued||campaignArchiveScanToken!=null)panel.Add(Readout("Refreshing saved slots…"));
            var selectable=campaignArchiveRows.Where(r=>!campaignArchiveSaving||r.Slot!=campaignProtectedLoadSlot).ToList();
            if(campaignArchiveSaving&&campaignArchivePreferEmpty)
            {
                slotName=selectable.FirstOrDefault(r=>!r.Occupied&&r.Summary.EndsWith(" · Empty",StringComparison.Ordinal))?.Slot
                    ??selectable.FirstOrDefault()?.Slot??"";
                campaignArchivePreferEmpty=false;
            }
            if(!selectable.Any(r=>r.Slot==slotName))slotName=(campaignArchiveSaving?selectable.FirstOrDefault(r=>!r.Occupied):selectable.FirstOrDefault(r=>r.CanAttemptLoad))?.Slot??selectable.FirstOrDefault()?.Slot??"";
            foreach(var row in campaignArchiveRows)
            {
                var captured=row;
                var select=AddButton(panel,(slotName==row.Slot?"Selected · ":"")+row.Summary,()=>{slotName=captured.Slot;campaignMenuNotice="";Draw();});
                select.name="save-slot-"+row.Slot;select.AddToClassList("campaign-save-row");
                select.EnableInClassList("selected-nav",slotName==row.Slot);
                select.SetEnabled(row.Slot!=campaignProtectedLoadSlot&&(campaignArchiveSaving||row.CanAttemptLoad));
                if(row.Slot==campaignProtectedLoadSlot)panel.Add(Readout("This is the requested load target. It cannot be overwritten before loading."));
            }
            var selected=campaignArchiveRows.FirstOrDefault(r=>r.Slot==slotName);
            panel.Add(Readout(selected?.Summary??"Select a slot."));
            panel.Add(Readout("Saving includes accepted campaign state. Unsubmitted orders, allocations and design edits stay local and are not included."));
            if(campaignMenuNotice.Length>0)panel.Add(Readout(campaignMenuNotice));
            var actions=new VisualElement();actions.AddToClassList("campaign-dialog-actions");panel.Add(actions);
            AddButton(actions,campaignAfterSave!=null?"Keep playing":"Back",()=>{
                if(campaignAfterSave!=null)ResumeCampaign();else ShowCampaignMenuPage(campaignArchiveReturn);
            });
            AddButton(actions,"Refresh slots",()=>{CancelCampaignArchiveScan();campaignArchiveCache.Remove(CampaignArchiveProfile());campaignArchiveRows=null;Draw();});
            var commit=AddButton(actions,campaignArchiveSaving?(campaignAfterSave==null?"Save selected slot":"Save and "+campaignContinuationLabel):"Load selected campaign",()=>{
                if(campaignArchiveSaving)SaveCampaignMenuSlot(slotName);
                else
                {
                    var target=slotName;var profile=CampaignArchiveProfile();
                    RequestCampaignDeparture("Load another campaign?","Load",()=>LoadCampaignMenuSlot(target,profile),
                        profile==CurrentCampaignArchiveProfile()?target:null,selected?.Summary??SlotLabel(target));
                }
            });
            commit.name=campaignArchiveSaving?"player-save":"player-load";commit.AddToClassList("primary-action");
            commit.SetEnabled(selected!=null&&selected.Slot!=campaignProtectedLoadSlot&&(campaignArchiveSaving&&demoStarted||!campaignArchiveSaving&&selected.CanAttemptLoad));
            if(!campaignArchiveSaving)
            {
                var earlier=new Foldout{text="Earlier campaign saves · original rules",value=false};panel.Add(earlier);
                earlier.Add(Readout("Each archive retains its original typed loader and separate save slots. Selecting an archive does not load a campaign."));
                var profiles=new[]{"cycle","repeat","growth","expansion","roster","colony","streamed","activities","published","astronomy","generated","fleet","frontier","demo","combat"};
                var labels=new List<string>{"Current Campaign","Repeat Expansion","Colony Growth","Colony Expansion","Colonisation V2","Colonisation","Streamed Activities","Activities","Published Construction","Static Astronomy","Original Generated Galaxy","Fleet Campaign","Frontier Preview","Bajor Demo","Combat playtest"};
                var profile=CampaignArchiveProfile();var index=Array.IndexOf(profiles,profile);
                var picker=new DropdownField("Archive",labels,index<0?0:index);earlier.Add(picker);var epoch=viewEpoch;
                picker.RegisterValueChangedCallback(_=>{
                    if(epoch!=viewEpoch||picker.index<0)return;
                    campaignArchiveOverrideProfile=profiles[picker.index];campaignArchiveRows=null;campaignMenuNotice="";Draw();
                });
            }
        }
        private void SaveCampaignMenuSlot(string requestedSlot,bool overwrite=false)
        {
            if(!demoStarted||requestedSlot==campaignProtectedLoadSlot)return;
            var owner=model!;
            void Replace()=>ShowCampaignDialog("Replace "+SlotLabel(requestedSlot)+"?",
                "Previous: "+(campaignArchiveRows?.FirstOrDefault(r=>r.Slot==requestedSlot)?.Summary??SlotLabel(requestedSlot))
                    +"\nCurrent: "+OwnerName(owner.Current.Frame.Actor)+" · Week "+owner.Current.Frame.Status.Turn
                    +". The previous save is replaced only after writing succeeds. Unsubmitted edits are not saved.",
                "Keep existing save",null,new CampaignDialogAction("Replace save",()=>SaveCampaignMenuSlot(requestedSlot,true)));
            if(!overwrite&&owner.TryCheckWriteAdmission(requestedSlot,out var error)&&error==ReviewCheckpointError.SlotOccupied){Replace();return;}
            void Finished(bool accepted)
            {
                if(!ReferenceEquals(model,owner))return;
                if(!accepted)
                {
                    if(!overwrite&&owner.Message=="Checkpoint result: SlotOccupied"){Replace();return;}
                    campaignMenuNotice="Saving did not complete. Your current campaign, previous save and requested next action are kept.";Draw();return;
                }
                MarkCampaignSaved(owner);RefreshSavedCampaignSlot(CurrentCampaignArchiveProfile(),requestedSlot);campaignMenuNotice="Accepted campaign saved. Unsubmitted edits remain local.";
                var next=campaignAfterSave;campaignAfterSave=null;campaignContinuationLabel="";campaignProtectedLoadSlot="";
                if(next!=null)next();else Draw();
            }
            if(owner.StaticAstronomyCampaign)RunPlayerCheckpoint(false,requestedSlot,overwrite,completed:Finished);
            else Finished(owner.Save(requestedSlot,overwrite));
        }
        private async void LoadCampaignMenuSlot(string requestedSlot,string? requestedProfile=null)
        {
            var owner=model;var epoch=viewEpoch;var profile=requestedProfile??CampaignArchiveProfile();
            var observed=campaignArchiveRows?.FirstOrDefault(row=>row.Slot==requestedSlot);CancelCampaignArchiveScan();
            var token=new object();ReviewUiModel? candidate=null;ShowCampaignBusy(token,"Loading and validating selected campaign…");
            try
            {
                candidate=await BuildMenuCandidateAsync(profile,requestedSlot);
                if(this==null||!isActiveAndEnabled||viewEpoch!=epoch||!ReferenceEquals(model,owner))return;
                HideCampaignBusy(token);var accepted=candidate;
                PublishPlayerProfile(accepted,requestedSlot);candidate=null;
                MarkCampaignSaved(accepted);campaignMenuNotice="";
                campaignArchiveOverrideProfile=null;
                campaignAfterSave=null;campaignContinuationLabel="";campaignProtectedLoadSlot="";
            }
            catch(Exception)
            {
                if(this!=null&&viewEpoch==epoch&&ReferenceEquals(model,owner))
                    {RejectCampaignArchiveRow(profile,observed);campaignMenuNotice="This selected save could not be loaded. Your current campaign and unsubmitted edits are kept. No other save was loaded.";}
            }
            finally
            {
                CloseUnpublishedPlayer(candidate);var hidden=HideCampaignBusy(token);
                if(this!=null&&isActiveAndEnabled&&(hidden||viewEpoch==epoch&&ReferenceEquals(model,owner)))Draw();
            }
        }
    }
}
