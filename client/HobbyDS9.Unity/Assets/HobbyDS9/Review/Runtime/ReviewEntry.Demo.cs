#nullable enable
using System;
using System.IO;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public static class SingleplayerDemoLaunch
    {
        public static void Launch(string[] args)
        {
            if(!SingleplayerDemoLaunchOptions.TryParse(args,out var options))throw new InvalidOperationException("Explicit demo launch selection required.");
            var go=new GameObject("HobbyDS9 Single-player Demo");go.SetActive(false);UnityEngine.Object.DontDestroyOnLoad(go);
            var entry=go.AddComponent<ReviewEntry>();entry.ConfigureDemo(options!);go.SetActive(true);
        }
    }
    public sealed partial class ReviewEntry
    {
        private CampaignSingleplayerDemoPackageDto? demoSource;
        private bool demoLanding,demoStarted;
        private void DrawDemoLanding()
        {
            var panel=PagePanel("HobbyDS9 — Start / Continue");var demo=model!.Current.Frame.Demo;
            panel.Add(Readout(demo?.ObjectiveText??(model.PlayableFleet?"Fleet campaign: build, organize, follow and fight. No deadline or campaign victory objective is defined.":"Current profile: Frontier exploration preview. Arrival charting only; no military victory or turn deadline is defined by this preview.")));
            panel.Add(Readout("Review your worlds and R&D, build an available ship, and plot a known route. Issue fleet orders in battle, then confirm the aftermath. Hull, shields and ammunition persist."));
            panel.Add(Readout("The computer chooses its own orders. Changes stay unsent until you confirm them. Save whenever you want to return later."));
            if(demoStarted){var resume=AddButton(panel,"Resume current campaign",()=>{demoLanding=false;viewEpoch++;Build();});resume.name="demo-resume-current";resume.AddToClassList("primary-action");ScheduleDemoMenuFocus(resume);}
            var generated=AddButton(panel,"Generated galaxy review",OpenGeneratedGalaxy);generated.name="generated-galaxy-entry";
            panel.Add(Readout("Create and inspect a seeded map. Full-map review only; your current campaign stays intact."));
            DrawPlayableFleetMenu(panel);
            panel.Add(Readout("Other campaign profiles"));
            var start=AddButton(panel,"Start new demo",()=>{
                if(demoStarted)Confirm("Start a new demo? Unsaved progress and local edits will be lost; your saved slots remain intact.",StartFreshDemo);
                else StartFreshDemo();
            });start.name="demo-start";start.AddToClassList("primary-action");
            if(!demoStarted&&!PlayableResourcesAvailable())ScheduleDemoMenuFocus(start);
            panel.Add(Readout("Continue a saved demo"));
            foreach(var slot in new[]{"demo-1","demo-2","demo-3"})
            {
                var load=AddButton(panel,"Continue Demo "+slot.Substring(slot.Length-1),()=>Confirm("Load this saved demo? Only a validated compatible format-4 save can replace your current campaign.",()=>{
                    ContinuePlayerProfile(false,slot);
                }));load.name="demo-continue-"+slot;
            }
            panel.Add(Readout("Demo saves are separate from classic and previous review saves. Empty or incompatible slots leave this session unchanged."));
            DrawFrontierMenu(panel);
            AddButton(panel,"Audio",()=>OpenDemoSettings(ReviewWorkspacePage.Audio));
            AddButton(panel,"Display",()=>OpenDemoSettings(ReviewWorkspacePage.Display));
            AddButton(panel,"Quit",()=>Confirm("Quit without automatic saving? Any unsaved progress will be lost.",()=>{if(model.Close(true))UnityEngine.Application.Quit();}));
        }
        private void StartFreshDemo()
        {
            // Prepare a valid replacement before publishing it; a failed factory keeps the old campaign.
            var codec=new ReviewNewtonsoftCodec();var next=new ReviewUiModel(demoSource!,codec,new ReviewDemoSlotStore(Path.Combine(UnityEngine.Application.persistentDataPath,"HobbyDS9SingleplayerDemoOnly-v4"),codec));
            PublishPlayerProfile(next,model?.SingleplayerDemo!=true?"demo-1":slotName);
        }
        private void OpenDemoSettings(ReviewWorkspacePage page)
        {demoLanding=false;workspace!.Navigate(page);viewEpoch++;Build();}
        private void OpenDemoMenu(){demoLanding=true;viewEpoch++;Build();}
        private void DrawDisplay()
        {
            var panel=PagePanel("Display preferences");panel.Add(Readout("Window size and fullscreen mode affect presentation only. They do not change your campaign or Windows display settings."));
            var status=Readout(CurrentDisplayStatus());status.name="display-current-status";panel.Add(status);
            // SetResolution is asynchronous. The element's own scheduler pauses
            // when detached; only actual applied window state is displayed.
            status.schedule.Execute(()=>{var value=CurrentDisplayStatus();if(status.text!=value)status.text=value;}).Every(100);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
                AddButton(panel,size.x+" × "+size.y,()=>Screen.SetResolution(size.x,size.y,Screen.fullScreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed));
            AddButton(panel,Screen.fullScreen?"Use windowed mode":"Use borderless fullscreen",()=>Screen.SetResolution(Screen.width,Screen.height,Screen.fullScreen?FullScreenMode.Windowed:FullScreenMode.FullScreenWindow));
            panel.Add(Readout("VSync follows the established game configuration. No desktop resolution or account setting is changed."));
            panel.Add(Readout("A window as large as your desktop can extend beyond its edges. Use borderless fullscreen to fit the display."));
        }
        private static string CurrentDisplayStatus()=>Screen.width+" × "+Screen.height+" · "+(Screen.fullScreenMode==FullScreenMode.FullScreenWindow?"Borderless fullscreen":Screen.fullScreen?"Fullscreen":"Windowed");
    }
}
