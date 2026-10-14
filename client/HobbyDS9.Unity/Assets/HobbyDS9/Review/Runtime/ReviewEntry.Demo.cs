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
            var panel=PagePanel("HobbyDS9 — Single-player demo");var demo=model!.Current.Frame.Demo!;
            panel.Add(Readout(demo.ObjectiveText));
            panel.Add(Readout("Review your worlds and R&D, build an available ship, and plot a known route. Issue fleet orders in battle, then confirm the aftermath. Hull, shields and ammunition persist."));
            panel.Add(Readout("The computer chooses its own orders. Changes stay unsent until you confirm them. Save whenever you want to return later."));
            if(demoStarted){var resume=AddButton(panel,"Resume current campaign",()=>{demoLanding=false;viewEpoch++;Build();});resume.name="demo-resume-current";resume.AddToClassList("primary-action");}
            var start=AddButton(panel,"Start new demo",()=>{
                if(demoStarted)Confirm("Start a new demo? Unsaved progress and local edits will be lost; your saved slots remain intact.",StartFreshDemo);
                else StartFreshDemo();
            });start.name="demo-start";start.AddToClassList("primary-action");
            panel.Add(Readout("Continue a saved demo"));
            foreach(var slot in new[]{"demo-1","demo-2","demo-3"})
            {
                var load=AddButton(panel,"Continue Demo "+slot.Substring(slot.Length-1),()=>Confirm("Load this saved demo? Only a validated compatible format-4 save can replace your current campaign.",()=>{
                    if(model.Load(slot,true)){slotName=slot;demoLanding=false;demoStarted=true;viewEpoch++;Build();}else Draw();
                }));load.name="demo-continue-"+slot;
            }
            panel.Add(Readout("Demo saves are separate from classic and previous review saves. Empty or incompatible slots leave this session unchanged."));
            AddButton(panel,"Audio",()=>OpenDemoSettings(ReviewWorkspacePage.Audio));
            AddButton(panel,"Display",()=>OpenDemoSettings(ReviewWorkspacePage.Display));
            AddButton(panel,"Quit",()=>Confirm("Quit without automatic saving? Any unsaved progress will be lost.",()=>{if(model.Close(true))UnityEngine.Application.Quit();}));
        }
        private void StartFreshDemo()
        {
            // Prepare a valid replacement before publishing it; a failed factory keeps the old campaign.
            var codec=new ReviewNewtonsoftCodec();var next=new ReviewUiModel(demoSource!,codec,new ReviewDemoSlotStore(Path.Combine(UnityEngine.Application.persistentDataPath,"HobbyDS9SingleplayerDemoOnly-v4"),codec));
            viewEpoch++;modalPermission.Invalidate();model=next;flow=new ReviewCombatFlow(next);workspace=new ReviewWorkspace(flow);
            central=null;centralContext=null;designState=null;designContext=null;demoLanding=false;demoStarted=true;Build();
        }
        private void OpenDemoSettings(ReviewWorkspacePage page)
        {demoLanding=false;workspace!.Navigate(page);viewEpoch++;Build();}
        private void OpenDemoMenu(){demoLanding=true;viewEpoch++;Build();}
        private void DrawDisplay()
        {
            var panel=PagePanel("Display preferences");panel.Add(Readout("Window size and fullscreen mode affect presentation only. They do not change your campaign or Windows display settings."));
            panel.Add(Readout(Screen.width+" × "+Screen.height+" · "+(Screen.fullScreen?"Fullscreen":"Windowed")));
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
                AddButton(panel,size.x+" × "+size.y,()=>Screen.SetResolution(size.x,size.y,Screen.fullScreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed));
            AddButton(panel,Screen.fullScreen?"Use windowed mode":"Use borderless fullscreen",()=>Screen.SetResolution(Screen.width,Screen.height,Screen.fullScreen?FullScreenMode.Windowed:FullScreenMode.FullScreenWindow));
            panel.Add(Readout("VSync follows the established game configuration. No desktop resolution or account setting is changed."));
        }
    }
}
