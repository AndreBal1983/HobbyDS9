using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = HobbyDS9.Contracts.EntityId;

namespace HobbyDS9.UnityClient.Editor
{
    // Optional real Play-mode acceptance runner. Uses the normal scenario and
    // commands; never changes economy, availability, compiled values or saves.
    [InitializeOnLoad]
    public static class CampaignBlueprintCapture
    {
        private const string Key="HobbyDS9.CampaignBlueprintCapture";
        private static readonly Vector2Int[] Sizes={new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)};
        private static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        private static readonly EntityId Actor=new EntityId("civ.federation"),Colony=new EntityId("colony.sol");
        private static ClientEntry entry;
        private static CampaignHost host;
        private static CampaignDesigner campaign;
        private static ShipDesignerView view;
        private static double deadline;
        private static string Output=>Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/unity/campaign-blueprints"));
        static CampaignBlueprintCapture()
        {
            if(SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update+=Start;}
        }
        public static void CaptureForAcceptance()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var game=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView",true));game.Show();game.Focus();
            UnityEngine.Application.runInBackground=true;SessionState.SetBool(Key,true);
            deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update-=Start;EditorApplication.update+=Start;
            EditorApplication.delayCall+=()=>EditorApplication.isPlaying=true;
        }
        private static void Invoke(string method,params object[] args)=>typeof(ClientEntry).GetMethod(method,Flags).Invoke(entry,args);
        private static void Section(string name)=>Invoke("RequestStrategicSection",name);
        private static void Click(Button button)
        {
            Require(button!=null && button.enabledInHierarchy,"Acceptance action disabled.");
            typeof(Clickable).GetMethod("Invoke",Flags).Invoke(button.clickable,new object[]{null});
        }
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        private static void Start()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup>deadline){Fail("Acceptance startup timed out.");return;}
            if(!EditorApplication.isPlaying || Time.frameCount<20)return;
            entry=UnityEngine.Object.FindAnyObjectByType<ClientEntry>();if(entry==null)return;
            EditorApplication.update-=Start;SessionState.SetBool(Key,false);
            deadline=EditorApplication.timeSinceStartup+900;EditorApplication.update+=Watch;
            try
            {
                host=(CampaignHost)typeof(ClientEntry).GetField("host",Flags).GetValue(entry);
                Section("Designer");campaign=(CampaignDesigner)typeof(ClientEntry).GetField("campaignDesigner",Flags).GetValue(entry);
                view=entry.GetComponent<UIDocument>().rootVisualElement.Q<ShipDesignerView>();
                var draft=view.Draft;
                // Complete test-owned input built from this campaign's empty fit.
                foreach(var id in new[]{"component.command.v1","component.life-support.v1","component.propulsion.v1","component.reactor.v1"})draft.Add(id);
                draft.SetCrew(30);draft.Rename("Capture Courier");
                foreach(DesignerProfile profile in Enum.GetValues(typeof(DesignerProfile)))
                {
                    draft.SelectProfile(profile);draft.SetPower("component.command.v1",4,4);draft.SetPower("component.life-support.v1",6,6);
                    var propulsion=profile==DesignerProfile.Cruise?30:profile==DesignerProfile.Combat?12:8;
                    draft.SetPower("component.propulsion.v1",propulsion,propulsion);
                }
                draft.SelectProfile(DesignerProfile.Cruise);Require(campaign.CanPublish,"Live fit must validate.");
                foreach(var system in ShipDesignerView.Systems){var fit=draft.Current;view.SelectSystem(system);Require(draft.Current.SameAs(fit),"System navigation lost the draft.");}
                view.SelectSystem("Overview");Capture("designer",Validation,Invalid);
            }
            catch(Exception error){Fail(error.ToString());}
        }
        private static void Invalid()
        {
            view.Draft.SetPower("component.propulsion.v1",30,100);view.SelectSystem("Validation");
            Require(!campaign.CanPublish,"Invalid power must disable publication.");
            Capture("invalid-power",Validation,Stale);
        }
        private static void Stale()
        {
            view.Draft.SetPower("component.propulsion.v1",30,30);
            Require(host.Client.Enqueue(host.Snapshot,Actor,Colony,new DefinitionId("ship-design.scout")).Accepted,"Real legacy command failed.");
            var before=host.SaveJson();var fit=view.Draft.Current;
            Click(view.Q<Button>("designer-construction"));view.SelectSystem("Validation");
            Require(campaign.RefreshRequired && campaign.Notice.Contains("[StaleRevision]") && host.SaveJson()==before && view.Draft.Current.SameAs(fit),"Rejected publication altered state or draft.");
            Capture("publication-rejected",Validation,Publish);
        }
        private static void Publish()
        {
            var item=host.Snapshot.Campaign.Colonies.Single(c=>c.ColonyId==Colony).ProductionQueue.Single();
            Require(host.Client.Cancel(host.Snapshot,Actor,Colony,item.QueueItemId).Accepted,"Legacy refund failed.");
            Click(view.Q<Button>("designer-refresh-campaign"));Click(view.Q<Button>("designer-construction"));view.SelectSystem("Validation");
            Require(campaign.History.Count==1 && campaign.EditingRevision==1,"Publication failed.");
            Capture("publication-success",Validation,History);
        }
        private static void History()
        {
            view.Draft.Rename("Capture Courier II");Click(view.Q<Button>("designer-construction"));view.SelectSystem("Validation");
            Require(campaign.History.Count==2,"Second revision failed.");
            Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"native-start.json"),host.SaveJson());
            Capture("immutable-history",Validation,Options);
        }
        private static void ConfigureProduction()
        {
            var root=entry.GetComponent<UIDocument>().rootVisualElement;
            var production=root.Q<ProductionOverviewView>();var selector=production.Q<DropdownField>("production-owned-colony");
            selector.value=selector.choices.Single(c=>c.Contains("colony.sol"));
            var world=production.Q<Foldout>("colony.sol");
            foreach(var foldout in world.Query<Foldout>().ToList())if(foldout.text=="Ship designs")foldout.value=false;
            production.scrollOffset=Vector2.zero;
        }
        private static void Options()
        {
            Section("Production");ConfigureProduction();Capture("construction-options",ProductionValidation,Queue);
        }
        private static void ProductionValidation(VisualElement root)
        {
            ConfigureProduction();var production=root.Q<ProductionOverviewView>();
            var button=production.Q<Foldout>("blueprint-options-colony.sol").Q<Button>("queue-blueprint-"+campaign.BlueprintId+"-r1");
            production.ScrollTo(button);
            Require(button!=null,"Exact revision action missing.");
        }
        private static void Queue()
        {
            var root=entry.GetComponent<UIDocument>().rootVisualElement;
            Click(root.Q<ProductionOverviewView>().Q<Foldout>("blueprint-options-colony.sol").Q<Button>("queue-blueprint-"+campaign.BlueprintId+"-r1"));
            Require(host.Snapshot.Campaign.Colonies.Single(c=>c.ColonyId==Colony).ProductionQueue.Single().Blueprint.Revision==1,"Queue did not pin revision 1.");
            ConfigureProduction();Capture("queued-blueprint",r=>{ConfigureProduction();},Finished);
        }
        private static void Finished()
        {
            var combat=new CombatOrchestrator(host.Client);
            for(var i=0;i<20 && !host.Snapshot.Campaign.Ships.Any(s=>s.Blueprint!=null);i++)
            {
                Require(host.EndTurn().Status!=TurnStatus.Rejected,"Production turn rejected.");combat.Pump();Require(combat.Error==null && !combat.RequiresPlayer,"Unexpected combat decision in capture scenario.");
            }
            var ship=host.Snapshot.Campaign.Ships.Single(s=>s.Blueprint!=null);
            Require(ship.Blueprint.Revision==1 && ship.ResolvedDesign.DisplayName=="Capture Courier","Finished ship was retroactively upgraded.");
            var saved=host.SaveJson();host.LoadJson(saved);Require(saved==host.SaveJson(),"Save/load changed the campaign.");
            File.WriteAllText(Path.Combine(Output,"finished-campaign.json"),saved);
            Section("Selection");var shell=entry.GetComponent<UIDocument>().rootVisualElement.Q<LcarsShell>();shell.ProductionChoice.Choose(false);
            var selection=(GalaxySelection)typeof(ClientEntry).GetField("selection",Flags).GetValue(entry);selection.SelectFleet(ship.FleetId);Invoke("Refresh");
            Capture("finished-ship",r=>{
                var details=r.Q<LcarsShell>().Details;var block=details.Query<LcarsInspectorBlock>().ToList().Single(b=>b.tooltip==ship.ShipId.Value);details.ScrollTo(block);
                Require(r.Query<Label>().ToList().Any(l=>l.text.Contains(campaign.BlueprintId+" • revision 1")),"Inspector provenance missing.");
            },()=>{EditorApplication.update-=Watch;Debug.Log("Campaign blueprint acceptance: 24 real Game-view captures complete; real commands, frozen revision, production and save/load verified.");EditorApplication.Exit(0);});
        }
        private static void Validation(VisualElement root)
        {
            Require(root.Q<LcarsShell>().DesignerOpen,"Designer unexpectedly closed.");
            foreach(var button in new[]{view.CloseButton,view.ResetButton,view.Q<Button>("designer-construction")})
            {
                Require(button.worldBound.width>=80 && button.worldBound.height>=32 && view.worldBound.Contains(button.worldBound.min) && view.worldBound.Contains(button.worldBound.max),"Primary action clipped.");
            }
            Require(view.Q<Label>("designer-provider-status").text.Contains("LIVE CAMPAIGN"),"Live provenance missing.");
            if(view.SelectedSystem=="Overview")Require(view.Q<ShipSystemGraphic>().HasImage,"Overview artwork missing.");
        }
        private static void Capture(string name,Action<VisualElement> validate,Action next)
        {
            StrategicPrototypeCapture.CaptureSequence("campaign-blueprints",name,Sizes,validate,()=>{try{next();}catch(Exception error){Fail(error.ToString());}});
        }
        private static void Watch(){EditorApplication.QueuePlayerLoopUpdate();if(EditorApplication.timeSinceStartup>deadline)Fail("Capture timed out; inspect first exception.");}
        private static void Fail(string message){EditorApplication.update-=Start;EditorApplication.update-=Watch;SessionState.SetBool(Key,false);Debug.LogError(message);EditorApplication.Exit(1);}
    }
}
