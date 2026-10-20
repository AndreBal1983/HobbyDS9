#nullable enable
using System;
using System.Linq;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private ReviewPlayerView? player;
        private VisualElement? playerFooter;
        private Label? playerStatus,playerTitle,playerGuidance;
        private object? designState;private HobbyDS9.TacticalReviewClient.ReviewController? designContext;
        private void BuildPlayerShell()
        {
            root!.AddToClassList("client-root");root.AddToClassList("player-console");root.style.fontSize=18;
            var spine=new VisualElement();spine.AddToClassList("console-spine");root.Add(spine);
            var header=new VisualElement{name="player-header"};header.AddToClassList("console-header");root.Add(header);
            var names=new VisualElement();header.Add(names);
            var identity=Readout((model!.PlayableFleet?"HOBBYDS9 · FLEET CAMPAIGN · ":model.FrontierPreview?"HOBBYDS9 · FRONTIER EXPLORATION PREVIEW · ":model.SingleplayerDemo?"HOBBYDS9 · SINGLE PLAYER DEMO · ":"EARLY CAMPAIGN PREVIEW · ")+OwnerName(model.Current.Frame.Actor));identity.name="review-scenario-identity";identity.AddToClassList("console-kicker");names.Add(identity);
            playerTitle=Readout(Title(workspace!.Page));playerTitle.AddToClassList("console-title");names.Add(playerTitle);
            if(model.Current.Frame.Demo!=null){var objective=Readout("");objective.name="demo-objective";objective.AddToClassList("console-kicker");names.Add(objective);}
            playerStatus=Readout("");playerStatus.AddToClassList("console-status");header.Add(playerStatus);
            playerGuidance=Readout("");playerGuidance.name="player-guidance";playerGuidance.AddToClassList("console-guidance");root.Add(playerGuidance);
            var body=new VisualElement();body.AddToClassList("console-body");root.Add(body);
            var nav=new ScrollView(ScrollViewMode.Vertical){name="review-workspace-navigation"};nav.AddToClassList("console-nav");body.Add(nav);if(PlayerProfile&&(demoLanding||!demoStarted))nav.style.display=DisplayStyle.None;
            if(PlayerProfile){var menu=AddButton(nav,"Menu",OpenDemoMenu);menu.name="demo-menu";}
            foreach(var page in new[]{ReviewWorkspacePage.Map,ReviewWorkspacePage.Empire,ReviewWorkspacePage.Worlds,ReviewWorkspacePage.Research,ReviewWorkspacePage.Production,ReviewWorkspacePage.FleetOrders,ReviewWorkspacePage.Combat,ReviewWorkspacePage.Saves,ReviewWorkspacePage.Audio,ReviewWorkspacePage.Display,ReviewWorkspacePage.Designer,ReviewWorkspacePage.Tools}.Where(p=>p!=ReviewWorkspacePage.Empire||model!.PlayableFleet))
            {
                var b=AddButton(nav,(page==ReviewWorkspacePage.Map?"Map":Title(page))+(workspace.Page==page?"  ›":""),()=>Navigate(page));b.name="review-nav-"+page;b.SetEnabled(workspace.CanNavigate);b.EnableInClassList("selected-nav",workspace.Page==page);
            }
            content=new VisualElement{name="review-central-content"};content.AddToClassList("console-main");body.Add(content);
            if(central==null||!ReferenceEquals(centralContext,model.Current))
            {central=new ReviewCentralView(content,flow!,(p,t,a)=>AddButton(p,t,a),Do,Confirm);centralContext=model.Current;}
            else central.Bind(content);
            player=new ReviewPlayerView(model,workspace,(p,t,a)=>AddButton(p,t,a),Do,Confirm,Navigate,(fleet,node,ship)=>{workspace.SelectedFleet=fleet;central.SelectStrategicFleet(fleet,node,ship);Navigate(ReviewWorkspacePage.FleetOrders);},(b,a)=>BindButton(b,a));
            playerFooter=new VisualElement{name="player-footer"};playerFooter.AddToClassList("console-footer");root.Add(playerFooter);
        }
        private static string OwnerName(string actor)=>actor=="civ.federation"?"Federation":actor=="civ.cardassian"?"Cardassian Union":actor;
        private static string Title(ReviewWorkspacePage page)
        {
            switch(page)
            {
                case ReviewWorkspacePage.Map:return "Strategic Map";
                case ReviewWorkspacePage.Research:return "Research";
                case ReviewWorkspacePage.Production:return "Production";
                case ReviewWorkspacePage.FleetOrders:return "Fleets";
                case ReviewWorkspacePage.Combat:return "Combat";
                case ReviewWorkspacePage.Saves:return "Save / Load";
                case ReviewWorkspacePage.Audio:return "Audio";
                case ReviewWorkspacePage.Designer:return "Ship Design";
                case ReviewWorkspacePage.Worlds:return "Worlds";
                case ReviewWorkspacePage.Empire:return "Empire";
                case ReviewWorkspacePage.Display:return "Display";
                default:return "Test Tools";
            }
        }
        private void DrawPlayer()
        {
            var frame=model!.Current.Frame;playerTitle!.text=Title(workspace!.Page);
            playerStatus!.text="Turn "+frame.Status.Turn+" · "+(frame.Status.Phase==SessionPhase.Planning?"Planning":"Combat pending")+"\nCredits "+frame.Strategy!.Civilization.Credits;
            if(frame.Demo!=null){var goal=frame.Strategy.Navigation.Nodes.FirstOrDefault(n=>n.NodeId.Value==frame.Demo.ObjectiveNodeId)?.DisplayName??"Objective sector";root!.Q<Label>("demo-objective").text="Objective: "+goal+" · Through turn "+frame.Demo.LastTurn+" · "+frame.Demo.Outcome;}
            playerGuidance!.text=PlayerMessage();
            if(PlayerProfile&&demoLanding){playerTitle.text=model.PlayableFleet?"Fleet campaign":model.FrontierPreview?"Frontier preview":"HobbyDS9 Demo";DrawDemoLanding();}
            else if(workspace.Page==ReviewWorkspacePage.Combat)central!.Render();
            else if(workspace.Page==ReviewWorkspacePage.Saves)DrawSaves();
            else if(workspace.Page==ReviewWorkspacePage.Audio)DrawAudio();
            else if(workspace.Page==ReviewWorkspacePage.Display)DrawDisplay();
            else if(workspace.Page==ReviewWorkspacePage.Tools)DrawTools();
            else if(workspace.Page==ReviewWorkspacePage.Designer)DrawDesigner();
            else player!.Render(content!,workspace.Page);
            DrawPlayerFooter();
        }
        private string PlayerMessage()
        {
            if(model!.HasLocalEdits)return model.Current.RefreshRequired?"Unsent changes kept · refresh your view before applying. Saving committed state keeps these edits locally.":"Unsent changes · apply or discard them before ending the turn. Navigation keeps your edits.";
            var demo=model.Current.Frame.Demo;
            if(demo!=null&&demo.Outcome!=CampaignDemoOutcome.Active)return "Demo outcome: "+demo.Outcome+". You may inspect or save the completed campaign, or start a new demo from Menu.";
            if(model!.Message.Contains("Stale"))return "Your view changed. Open Test Tools to refresh it before trying again. Your edits are kept.";
            if(model.Current.RefreshRequired)return "That action could not be applied. Your campaign and edits are kept; refresh the view in Test Tools.";
            if(workspace!.Page==ReviewWorkspacePage.Saves||demoLanding){
                if(model.Message.Contains("Checkpoint result: None"))return "Campaign saved or loaded successfully.";
                if(model.Message=="Validated complete-root checkpoint loaded.")return "Campaign loaded. Accepted state is restored; previous local edits were discarded.";
                if(model.Message.Contains("NotFound"))return "That save slot is empty. Choose another slot or save your current campaign.";
                if(model.Message.Contains("InvalidCheckpoint"))return "This save cannot be loaded by the current scenario. Your campaign is unchanged.";
                if(model.Message.Contains("WriteFailed"))return "Saving failed. Your previous save and current campaign are kept.";
            }
            if(flow!.Stage==ReviewCombatStage.Waiting)return PlayerProfile?"Your orders are accepted. Waiting for the current battle boundary; refresh your view if needed. The computer owns its orders.":"Your orders are submitted. This early scenario uses two human sides: open Test Tools to switch sides and provide their orders.";
            if(flow.Stage==ReviewCombatStage.AftermathPending)return "Battle complete. Confirm the campaign aftermath in Combat, then give survivors fresh fleet orders.";
            if(flow.Stage==ReviewCombatStage.RoundResult)return "Round resolved. Review the report and retained stocks in Combat, then plan the next round.";
            if(flow.Stage==ReviewCombatStage.AftermathConfirmed)return "Aftermath confirmed. Return to the map or give surviving fleets fresh strategic orders.";
            if(model.HasLocalEdits)return "Unsent changes · apply or discard them before ending the turn. Navigation keeps your edits.";
            if(model.Current.Frame.Status.Phase!=SessionPhase.Planning)return "A battle needs your attention. Open Combat, resolve its rounds, then confirm the aftermath.";
            if(model.FrontierPreview&&workspace!.Page==ReviewWorkspacePage.Map)return "Frontier preview: select your fleet and a known node, preview/review the route, then end turns. Actual arrivals chart branches at the next Planning boundary.";
            return workspace!.Page==ReviewWorkspacePage.Map?"Select a fleet to plot a course, visit Research, or build a ship in Production.":workspace.Page==ReviewWorkspacePage.Research?"Science and Engineering progress when you end a turn. Changes apply only when you confirm them.":workspace.Page==ReviewWorkspacePage.Production?"Choose a shipyard, inspect the cost, then build. Industry progresses on End turn.":"Inspection and navigation never submit an order.";
        }
        private void DrawPlayerFooter()
        {
            playerFooter!.Clear();var left=new VisualElement();left.AddToClassList("review-field-row");playerFooter.Add(left);
            if(PlayerProfile&&(demoLanding||!demoStarted)){AddButton(left,"Back to menu",OpenDemoMenu);return;}
            AddButton(left,"‹ Return to map",()=>Navigate(ReviewWorkspacePage.Map));
            AddButton(left,"Save",()=>Navigate(ReviewWorkspacePage.Saves));
            if(model!.HasLocalEdits)AddButton(left,"Discard changes",()=>Confirm("Discard your unsent edits? Your accepted orders and campaign remain unchanged.",()=>Do(()=>model.Reset())));
            if(workspace!.Page==ReviewWorkspacePage.Combat)return; // Central view owns the battle primary action.
            if(workspace.Page==ReviewWorkspacePage.Research)
            {
                var d=model.ResearchInputs()!;
                if(d.ScienceChanged){Primary("Apply Science allocation",()=>Confirm("Apply this complete Science plan?\n"+string.Join("\n",model.Current.Frame.Strategy!.Research.Science.Select(c=>c.Discipline+": "+d.PercentLabel(c.Discipline)))+"\nTotal 100%.",()=>Do(()=>model.AllocateScience(d.Values(),ReviewUiModel.NewCommandId()))),d.Current(model.Current.Frame)&&CanPlan());return;}
                if(d.SplitChanged){Primary("Apply R&D split",()=>Confirm("Apply this Science / Engineering split?",()=>Do(()=>model.AllocateResearch(d.ScienceUnits,ReviewUiModel.NewCommandId()))),d.Current(model.Current.Frame)&&CanPlan());return;}
            }
            if(model.Current.Frame.Status.Phase!=SessionPhase.Planning){Primary("Open combat",()=>Navigate(ReviewWorkspacePage.Combat));return;}
            Primary("End turn →",()=>Confirm("End turn with your accepted orders? Research and Industry will progress. Unsent edits must be applied or discarded first.",()=>Do(()=>model.Advance())),CanPlan()&&!model.HasLocalEdits);
        }
        private bool CanPlan()=>model!.Current.Frame.Status.Phase==SessionPhase.Planning&&(model.Current.Frame.Demo==null||model.Current.Frame.Demo.Outcome==CampaignDemoOutcome.Active)&&!model.Current.RefreshRequired&&!model.Current.HasPendingWork&&!model.Busy&&!model.Closed;
        private Button Primary(string label,Action action,bool enabled=true){var b=AddButton(playerFooter!,label,action);b.AddToClassList("primary-action");b.name="player-primary-action";b.SetEnabled(enabled);return b;}
        private VisualElement PagePanel(string heading)
        {content!.Clear();var p=new ScrollView(ScrollViewMode.Vertical);p.AddToClassList("console-panel");content.Add(p);var h=Readout(heading);h.AddToClassList("console-section");p.Add(h);return p;}
        private void DrawSaves()
        {
            var p=PagePanel("Save & resume");p.Add(Readout("Save your current campaign, or load a compatible save. Saving keeps unsent edits locally; successful loading replaces them."));
            var result=SaveLoadResult();
            if(result.Length>0){var banner=Readout(result);banner.name="save-load-result";banner.AddToClassList("console-queue");p.Add(banner);}
            var slots=new VisualElement();slots.AddToClassList("review-field-row");p.Add(slots);
            foreach(var slot in model!.PlayableFleet?PlayableFleetOptions.Slots():model.FrontierPreview?FrontierPreviewOptions.Slots():model.SingleplayerDemo?new[]{"demo-1","demo-2","demo-3"}:new[]{"campaign-1","campaign-2","campaign-3"})
            {var b=AddButton(slots,(model.FrontierPreview?"Frontier · format 5 · ":model.SingleplayerDemo?"Demo ":"Campaign ")+slot.Substring(slot.Length-1)+(slotName==slot?" · selected":""),()=>{slotName=slot;Draw();});b.name="save-slot-"+slot;}
            p.Add(Readout("Selected slot: "+slotName));
            var save=AddButton(p,"Save current campaign",()=>Do(()=>{if(!model!.Save(slotName)&&model.Message=="Checkpoint result: SlotOccupied")Confirm("Replace the save in "+slotName+"? Your current campaign will be saved; unsent edits stay local.",()=>Do(()=>model.Save(slotName,true)));}));save.name="player-save";save.AddToClassList("primary-action");
            var load=AddButton(p,"Load selected campaign",()=>Confirm("Load "+slotName+"? Only a validated compatible save can replace this campaign. Unsent edits will be discarded after a successful load.",()=>Do(()=>model!.Load(slotName,true))));load.name="player-load";
            p.Add(Readout(model.PlayableFleet?"Fleet campaign saves are separate from other profiles. Saved state includes named fleets, current ship stocks and completed battle reports. Unsent edits remain local; a successful load replaces them.":model.FrontierPreview?"Frontier format-5 saves use their own frontier-1/2/3 slots and directory. Bajor format-4 and older saves are separate; no import or conversion. Failed loads keep your current session.":model.SingleplayerDemo?"Format-4 demo saves remain separate from classic, physical and integrated-review saves. Failed loads keep your current session.":"Early campaign saves remain separate from classic and earlier physical-review saves. Failed loads keep your current session."));
        }
        private string SaveLoadResult()
        {
            var message=model!.Message;
            if(message=="Checkpoint result: None")return "Last save result: committed campaign state saved. Unsent edits stay local and are not in the save.";
            if(message=="Validated complete-root checkpoint loaded.")return "Last load result: campaign loaded. Accepted state is restored; previous local edits were discarded.";
            const string prefix="Checkpoint result: ";
            if(!message.StartsWith(prefix,StringComparison.Ordinal))return "";
            var code=message.Substring(prefix.Length);string reason;
            switch(code)
            {
                case "NotFound":reason="This save slot is empty (NotFound).";break;
                case "InvalidCheckpoint":reason="The save is invalid or incompatible with this campaign (InvalidCheckpoint).";break;
                case "WriteFailed":reason="The save could not be written (WriteFailed). Your previous valid save is kept.";break;
                case "SerializationFailed":reason="The campaign could not be prepared for saving (SerializationFailed). Your previous valid save is kept.";break;
                case "SlotOccupied":reason="This slot already contains a save. Confirm replacement to overwrite it.";break;
                default:reason="The operation did not complete ("+code+").";break;
            }
            return "Last save/load result: "+reason+(model.HasLocalEdits?" Your current campaign and unsent edits are unchanged.":" Your current campaign is unchanged.");
        }
        private void DrawAudio()
        {
            var p=PagePanel("Audio preferences");
            try{var t=Type.GetType("HobbyDS9.UnityClient.IntegratedAudioPanelBridge, HobbyDS9.UnityClient",false);if(t==null)throw new InvalidOperationException();t.GetMethod("Present")!.Invoke(null,new object[]{p});}
            catch(Exception){p.Add(Readout("Audio controls are currently unavailable. Your campaign remains playable."));}
        }
        private void DrawTools()
        {
            var p=PagePanel("Developer / Test Tools");p.Add(Readout("This early scenario uses two human sides. These host controls are not an opponent AI or an account security boundary."));
            if(PlayerProfile){p.Clear();p.Add(Readout(model!.PlayableFleet?"HOBBYDS9 · FLEET CAMPAIGN · ":model.FrontierPreview?"Frontier preview: one package-defined human player. The computer owns its decisions. No opponent actor switching is exposed.":"Single-player demo: the computer owns its decisions. No opponent actor switching is exposed."));AddButton(p,"Refresh current view",()=>Do(()=>model.Refresh()));p.Add(Readout(model.Message));return;}
            foreach(var actor in new[]{"civ.federation","civ.cardassian"}){var b=AddButton(p,"Switch human side to "+OwnerName(actor),()=>{if(model!.HasLocalEdits)Confirm("Switch side and discard unsent edits?",()=>Do(()=>model.SwitchActor(actor,true)));else Do(()=>model.SwitchActor(actor));});b.name="player-switch-"+actor;}
            AddButton(p,"Refresh view",()=>Do(()=>model!.Refresh()));
            p.Add(Readout("Profile integrated-playtest-v1 · proposed numerical policies · default activation disabled"));p.Add(Readout(workspace!.BoundaryIdentity));p.Add(Readout(model!.Message));
            AddButton(p,"Exit early campaign",()=>Confirm("Exit without automatic saving? Unsent edits will be lost.",()=>Do(()=>model.Close(true))));
        }
        private void DrawDesigner()
        {
            content!.Clear();
            var bridge=Type.GetType("HobbyDS9.UnityClient.IntegratedDesignerPanelBridge, HobbyDS9.UnityClient",true)!;
            if(designState==null||!ReferenceEquals(designContext,model!.Current))
            {
                designContext=model!.Current;var owner=designContext;
                designState=bridge.GetMethod("Create")!.Invoke(null,new object[]{model.DesignLibrary(),new Action<bool>(dirty=>{if(!ReferenceEquals(model.Current,owner)||model.Closed)return;model.LocalDesignDirty=dirty;playerGuidance!.text=PlayerMessage();DrawPlayerFooter();}),new Action(()=>{if(ReferenceEquals(model.Current,owner)&&confirmation==null&&!model.Busy&&!model.Closed)Navigate(ReviewWorkspacePage.Map);})});
                var state=designState;model.ResetLocalDesign=()=>bridge.GetMethod("Reset")!.Invoke(null,new[]{state});
            }
            bridge.GetMethod("Present")!.Invoke(null,new[]{content,designState});
        }
    }
}
