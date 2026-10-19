#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    // Actual controls/public UI dispatch, not native input. Requires exclusive receiving lease.
    public sealed class FrontierPreviewControlTests
    {
        private readonly List<string> temporaryDirectories=new List<string>();
        private static void RequireRun()
        {if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-frontier-render-tests")<0)Assert.Ignore("Dedicated frontier menu/resources bootstrap required.");}
        private static object? Invoke(ReviewEntry e,string method,params object[] args)=>typeof(ReviewEntry).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(e,args);
        private static Button Confirm(VisualElement root)=>root.Q("review-one-use-confirmation").Query<Button>().ToList().Single(b=>b.text=="Confirm");
        private static void Click(Button b)
        {Assert.That(b,Is.Not.Null);Assert.That(b.enabledInHierarchy,Is.True,b.name);using(var evt=NavigationSubmitEvent.GetPooled()){evt.target=b;b.SendEvent(evt);}}
        private static void Id(UIDocument doc,string id)=>Click(doc.rootVisualElement.Q<Button>(id));
        private static void Key(VisualElement dispatcher,VisualElement target,KeyCode code,bool up=false)
        {
            var source=new Event{type=up?EventType.KeyUp:EventType.KeyDown,keyCode=code};
            if(up){using(var e=KeyUpEvent.GetPooled(source)){e.target=target;dispatcher.SendEvent(e);}}
            else{using(var e=KeyDownEvent.GetPooled(source)){e.target=target;dispatcher.SendEvent(e);}}
        }
        private ReviewUiModel Isolated(ReviewEntry entry,Action<CampaignFrontierPackageDto>? configure=null)
        {
            byte[] Bytes(string n)=>Resources.Load<TextAsset>(FrontierPreviewOptions.ResourceRoot+n).bytes;
            var codec=new ReviewNewtonsoftCodec();var package=ReviewExactPackageLoader.LoadFrontier(Bytes("frontier"),Bytes("package"),Bytes("physical"),Bytes("core"),Bytes("scenario"),codec);
            configure?.Invoke(package);
            var directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Frontier-UI-"+Guid.NewGuid().ToString("N"));temporaryDirectories.Add(directory);
            var model=new ReviewUiModel(package,codec,new ReviewFrontierSlotStore(directory,codec));
            Invoke(entry,"PublishPlayerProfile",model,"frontier-1");return model;
        }
        [TearDown] public void RemoveOnlyFixtureDirectories()
        {
            foreach(var directory in temporaryDirectories)
            {
                var full=Path.GetFullPath(directory);var parent=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
                if(!string.Equals(Path.GetDirectoryName(full),parent,StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(full).StartsWith("HobbyDS9-Frontier-UI-",StringComparison.Ordinal))throw new InvalidOperationException("Unsafe fixture cleanup.");
                if(Directory.Exists(full))Directory.Delete(full,true);
            }
            temporaryDirectories.Clear();
        }
        [UnityTest] public IEnumerator MenuOptInRetainsActualOriginWithoutStartingAMilitaryObjective()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            Assert.That(entry.Presentation!.Model.SingleplayerDemo,Is.True);Assert.That(entry.Presentation.Model.FrontierPreview,Is.False);
            var root=doc.rootVisualElement;var panel=root.panel.visualTree;var start=root.Q<Button>("frontier-start");
            Assert.That(root.Q<Button>("demo-start").enabledInHierarchy,Is.True);start.Focus();Key(start,start,KeyCode.Return);yield return null;
            var model=entry.Presentation.Model;root=doc.rootVisualElement;
            Assert.That(model.FrontierPreview,Is.True);Assert.That(model.SingleplayerDemo,Is.False);Assert.That(model.Current.Frame.Demo,Is.Null);
            Assert.That(root.Q("demo-objective"),Is.Null);Assert.That(root.Q("frontier-known-map"),Is.Not.Null);
            Assert.That(root.panel.visualTree,Is.SameAs(panel));Assert.That(start.panel,Is.SameAs(root.panel));Assert.That(start.enabledInHierarchy,Is.False);Assert.That(start.focusable,Is.False);
            var before=model.Current.Frame;Id(doc,"player-primary-action");var confirm=Confirm(root);Key(confirm,confirm,KeyCode.Return);
            Assert.That(model.Current.Frame,Is.SameAs(before),"held profile-start cannot confirm End turn");
            Key(panel,start,KeyCode.Return,true);Assert.That(start.panel,Is.Null);Key(confirm,confirm,KeyCode.Return);
            Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(before.Status.Turn+1));Key(panel,confirm,KeyCode.Return,true);
            Id(doc,"demo-menu");Assert.That(doc.rootVisualElement.Q<Button>("demo-continue-demo-1"),Is.Not.Null);
            Assert.That(doc.rootVisualElement.Q<Button>("frontier-continue-frontier-1"),Is.Not.Null);
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator RoutePreviewPartialTravelAndArrivalRenderOnlyCurrentHumanKnowledge()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();var model=Isolated(entry);yield return null;
            var first=model.Current.Frame;var initial=first.Frontier!;Assert.That(initial.ActorId,Is.EqualTo(first.Actor));
            Assert.That(initial.Nodes.Any(n=>n.NodeId=="nav.badlands"),Is.False);Assert.That(doc.rootVisualElement.Q("frontier-node-nav.badlands"),Is.Null);
            yield return Capture(entry,doc,"initial");Id(doc,"frontier-node-nav.bajor");Id(doc,"frontier-preview-route");
            var offer=entry.Workspace!.RoutePreview;Assert.That(offer,Is.Not.Null);Assert.That(model.Current.Frame,Is.SameAs(first));
            Id(doc,"review-nav-Research");Id(doc,"review-nav-Map");Assert.That(entry.Workspace.RoutePreview,Is.SameAs(offer));
            Assert.That(entry.Workspace.SelectedNode,Is.EqualTo("nav.bajor"));Assert.That(entry.Workspace.SelectedFleet,Is.EqualTo(offer!.Fleet));
            Id(doc,"frontier-send-route");Key(Confirm(doc.rootVisualElement),Confirm(doc.rootVisualElement),KeyCode.Escape);
            Assert.That(model.Current.Frame,Is.SameAs(first));Id(doc,"frontier-send-route");Click(Confirm(doc.rootVisualElement));
            Id(doc,"player-primary-action");Click(Confirm(doc.rootVisualElement));yield return null;
            var partial=model.Current.Frame;Assert.That(partial.Frontier!.Nodes.Select(n=>n.NodeId),Is.EqualTo(initial.Nodes.Select(n=>n.NodeId)));
            Assert.That(partial.Frontier.Receipts,Is.Empty);var travel=partial.Travel!.OwnReceipts.Single(r=>r.FleetId==offer.Fleet);
            Assert.That(travel.TransitProgress,Is.GreaterThan(0).And.LessThan(travel.TransitTotalCost));Assert.That(doc.rootVisualElement.Q("frontier-node-nav.badlands"),Is.Null);
            Assert.That(entry.Workspace.RoutePreview,Is.Null,"advance invalidates the old stamped route preview");yield return Capture(entry,doc,"partial");
            Id(doc,"player-primary-action");Click(Confirm(doc.rootVisualElement));yield return null;
            var arrived=model.Current.Frame;Assert.That(arrived.Frontier!.Receipts.Any(r=>r.ArrivalNodeId=="nav.bajor"),Is.True);
            Assert.That(arrived.Frontier.Nodes.Count,Is.GreaterThan(initial.Nodes.Count));
            var ids=doc.rootVisualElement.Query<Button>().ToList().Where(b=>b.name.StartsWith("frontier-node-",StringComparison.Ordinal)).Select(b=>b.name.Substring("frontier-node-".Length)).OrderBy(x=>x).ToArray();
            Assert.That(ids,Is.EqualTo(arrived.Frontier.Nodes.Select(n=>n.NodeId).OrderBy(x=>x).ToArray()));
            yield return Capture(entry,doc,"arrival-branches");
            var branch=arrived.Frontier.Nodes.First(n=>!initial.Nodes.Any(old=>old.NodeId==n.NodeId)).NodeId;
            Id(doc,"frontier-node-"+branch);Id(doc,"frontier-preview-route");var branchOffer=entry.Workspace.RoutePreview;
            Assert.That(branchOffer,Is.Not.Null);Assert.That(branchOffer!.Destination,Is.EqualTo(branch));Assert.That(model.Current.Frame,Is.SameAs(arrived));
            Id(doc,"frontier-send-route");Click(Confirm(doc.rootVisualElement));
            Assert.That(model.Current.Frame.Travel!.OwnOrders.Any(o=>o.FleetId==branchOffer.Fleet&&o.RouteNodeIds.Contains(branch)),Is.True,"newly charted branch uses the same real stamped command path");
            Assert.That(model.Current.Frame.Frontier!.Receipts.Count,Is.EqualTo(arrived.Frontier.Receipts.Count),"sending another previewed branch does not itself chart anything");
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator FailedFrontierLoadPreservesResearchDesignerAndTravelDrafts()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();var model=Isolated(entry);yield return null;
            var controller=model.Current;var frame=controller.Frame;
            void FailLoad()
            {
                Id(doc,"review-nav-Saves");Assert.That(doc.rootVisualElement.Q("save-slot-frontier-1"),Is.Not.Null);Assert.That(doc.rootVisualElement.Q("save-slot-demo-1"),Is.Null);
                Id(doc,"player-load");Click(Confirm(doc.rootVisualElement));
                Assert.That(model.Current,Is.SameAs(controller));Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(model.HasLocalEdits,Is.True);
                Assert.That(doc.rootVisualElement.Q<Label>("save-load-result").text,Does.Contain("NotFound").And.Contain("unsent edits are unchanged"));
            }
            Id(doc,"review-nav-Research");var plus=doc.rootVisualElement.Query<Button>().ToList().First(b=>b.name.StartsWith("science-plus-",StringComparison.Ordinal));Click(plus);
            var research=model.ResearchDraft;FailLoad();Assert.That(model.ResearchDraft,Is.SameAs(research));Assert.That(research!.Dirty,Is.True);Assert.That(model.Reset(),Is.True);
            Id(doc,"review-nav-Designer");var name=doc.rootVisualElement.Q<TextField>("designer-name");Assert.That(name,Is.Not.Null);name.value="Frontier local draft";
            Assert.That(model.LocalDesignDirty,Is.True);FailLoad();Assert.That(model.LocalDesignDirty,Is.True);
            Id(doc,"review-nav-Designer");Assert.That(doc.rootVisualElement.Q<TextField>("designer-name").value,Is.EqualTo("Frontier local draft"));Assert.That(model.Reset(),Is.True);
            Assert.That(model.DraftTravel(frame.OwnStrategicFleets.First().FleetId.Value,CampaignTravelIntent.Leave,"nav.bajor",null),Is.True);
            var travel=model.Current.TravelDraft;FailLoad();Assert.That(model.Current.TravelDraft,Is.SameAs(travel));Assert.That(model.Reset(),Is.True);
            Id(doc,"player-save");Assert.That(doc.rootVisualElement.Q<Label>("save-load-result").text,Does.Contain("saved"));
            var saved=Path.Combine(temporaryDirectories.Single(),"unity-frontier-review-v5-frontier-1.json");Assert.That(File.Exists(saved),Is.True);
            Assert.That(new ReviewNewtonsoftCodec().DecodeFrontierRoot(File.ReadAllBytes(saved)).FormatVersion,Is.EqualTo(5));
            Id(doc,"player-load");Click(Confirm(doc.rootVisualElement));Assert.That(model.Current,Is.Not.SameAs(controller));Assert.That(model.Current.Frame.Frontier!.Nodes.Select(n=>n.NodeId),Is.EqualTo(frame.Frontier!.Nodes.Select(n=>n.NodeId)));
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator EveryExposedPageAndEndTurnSupportsNullDemoAndNoActorSwitch()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();var model=Isolated(entry);yield return null;
            var frame=model.Current.Frame;
            foreach(var page in new[]{ReviewWorkspacePage.Map,ReviewWorkspacePage.Worlds,ReviewWorkspacePage.Research,ReviewWorkspacePage.Production,ReviewWorkspacePage.FleetOrders,ReviewWorkspacePage.Combat,ReviewWorkspacePage.Saves,ReviewWorkspacePage.Audio,ReviewWorkspacePage.Display,ReviewWorkspacePage.Designer,ReviewWorkspacePage.Tools})
            {
                Id(doc,"review-nav-"+page);yield return null;Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(model.Current.Frame.Demo,Is.Null);
                Assert.That(doc.rootVisualElement.Query<Button>().ToList().Any(b=>b.name.StartsWith("player-switch-",StringComparison.Ordinal)),Is.False);
            }
            Assert.That(model.SwitchActor("civ.cardassian",true),Is.False);Assert.That(model.Current.Frame,Is.SameAs(frame));
            Id(doc,"review-nav-Map");Id(doc,"player-primary-action");Click(Confirm(doc.rootVisualElement));
            Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(frame.Status.Turn+1));Assert.That(model.Current.Frame.Actor,Is.EqualTo(frame.Actor));Assert.That(model.Current.Frame.Frontier,Is.Not.Null);
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator SwitchingProfilesInvalidatesOldPermissionAndRouteGeneration()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();var model=Isolated(entry);yield return null;
            Id(doc,"frontier-node-nav.bajor");Id(doc,"frontier-preview-route");var offer=entry.Workspace!.RoutePreview!;
            Id(doc,"frontier-send-route");var obsolete=Confirm(doc.rootVisualElement);Key(obsolete,obsolete,KeyCode.Escape);
            var old=model.Current.Frame;Id(doc,"demo-menu");Id(doc,"demo-start");var replacement=Confirm(doc.rootVisualElement);
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=obsolete;doc.rootVisualElement.panel.visualTree.SendEvent(e);}
            Assert.That(entry.Presentation!.Model.Current.Frame,Is.SameAs(old));Click(replacement);yield return null;
            var bajor=entry.Presentation.Model;Assert.That(bajor.SingleplayerDemo,Is.True);Assert.That(bajor.FrontierPreview,Is.False);Assert.That(bajor.Current.Frame.Demo,Is.Not.Null);Assert.That(bajor.Current.Frame.Frontier,Is.Null);
            var before=bajor.Current.Frame;Assert.That(bajor.SendPreviewedTravel(offer,ReviewUiModel.NewCommandId()),Is.False);Assert.That(bajor.Current.Frame,Is.SameAs(before));
            Id(doc,"review-nav-Saves");Assert.That(doc.rootVisualElement.Q("save-slot-demo-1"),Is.Not.Null);Assert.That(doc.rootVisualElement.Q("save-slot-frontier-1"),Is.Null);
            Id(doc,"review-nav-Research");Click(doc.rootVisualElement.Query<Button>().ToList().First(b=>b.name.StartsWith("science-plus-",StringComparison.Ordinal)));
            var controller=bajor.Current;var research=bajor.ResearchDraft;Id(doc,"demo-menu");
            // Unique absent name: exercise the real cross-profile handler without
            // reading/overwriting any owner's fixed Frontier1/2/3 save slot.
            Invoke(entry,"ContinuePlayerProfile",true,"missing-ui-"+Guid.NewGuid().ToString("N").Substring(0,8));
            Assert.That(entry.Presentation.Model,Is.SameAs(bajor));Assert.That(bajor.Current,Is.SameAs(controller));
            Assert.That(bajor.Current.Frame,Is.SameAs(before));Assert.That(bajor.ResearchDraft,Is.SameAs(research));Assert.That(research!.Dirty,Is.True);
            Assert.That(doc.rootVisualElement.Q<Label>("profile-load-result").text,Does.Contain("unchanged"));
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator RealPendingFrontierCombatKeepsRoundAndAftermathConfirmationsReachable()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            var model=Isolated(entry,package=>{
                // Same validated combat fixture as Primary's CPU case. Initial
                // content only; no outcomes, stocks, RNG or live saves assigned.
                var physical=package.Integrated!.Physical!;var scenario=physical.CorePackage!.Campaign!;
                scenario.InitialState.Fleets!.Single(f=>f.FleetId=="fleet.cardassian-scout").LocationNodeId="nav.bajor";
                scenario.Definitions.NavigationConnections!.Single(c=>c.ConnectionId=="connection.sol-bajor").BaseTraversalCost=1;
                var otherKnowledge=scenario.InitialState.NavigationKnowledge!.Single(k=>k.CivilizationId=="civ.cardassian");
                foreach(var node in otherKnowledge.Nodes!)node.Level=NavigationKnowledgeLevel.Charted;
                foreach(var lane in otherKnowledge.Connections!)lane.Level=NavigationKnowledgeLevel.Charted;
                physical.InitialOrders!.Single(o=>o.FleetId=="fleet.cardassian-scout").Intent=CampaignTravelIntent.StayEngage;
                physical.InitialContacts!.Add(new CampaignContactKnowledgeDto{ActorId="civ.cardassian",FleetId="fleet.federation-scout",LastSeenNodeId="nav.sol",LastSeenTurn=1,TrackingNodeIds=otherKnowledge.Nodes!.Select(n=>n.Id).ToList()});
            });yield return null;
            Id(doc,"frontier-node-nav.bajor");Id(doc,"frontier-preview-route");Id(doc,"frontier-send-route");Click(Confirm(doc.rootVisualElement));
            Id(doc,"player-primary-action");Click(Confirm(doc.rootVisualElement));yield return null;
            Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.AwaitingCombat));Assert.That(model.Current.Frame.Battle,Is.Not.Null);
            Id(doc,"review-nav-Combat");yield return null;
            Assert.That(doc.rootVisualElement.Q("review-central-content").ClassListContains("demo-combat"),Is.True,"Frontier uses the readable player combat presentation");
            var copy=string.Join("\n",doc.rootVisualElement.Query<Label>().ToList().Select(l=>l.text));
            Assert.That(copy,Does.Not.Contain("Each human actor").And.Not.Contain("No opponent AI is invented"));
            Assert.That(copy.IndexOf("computer",StringComparison.OrdinalIgnoreCase)>=0||copy.IndexOf("AI",StringComparison.OrdinalIgnoreCase)>=0,Is.True,"player copy acknowledges actual automatic opponent");
            Button Action(string text)=>doc.rootVisualElement.Q("central-combat-footer").Query<Button>().ToList().Single(b=>b.text==text);
            int committed=0;
            while(!model.Current.Frame.Battle!.Complete)
            {
                Assert.That(++committed,Is.LessThanOrEqualTo(3));
                if(entry.Presentation!.Stage==ReviewCombatStage.RoundResult)Click(Action("Plan next round"));
                var before=model.Current.Frame;var round=before.Battle!.Round;
                yield return Capture(entry,doc,"combat-round-"+round,true);
                Click(Action("Review / commit round "+round));yield return Capture(entry,doc,"combat-round-"+round+"-confirm",true,true);
                Click(doc.rootVisualElement.Q("review-one-use-confirmation").Query<Button>().ToList().Single(b=>b.text=="Cancel"));
                Assert.That(model.Current.Frame,Is.SameAs(before),"cancel submits no joint round");
                Click(Action("Review / commit round "+round));var confirm=Confirm(doc.rootVisualElement);Click(confirm);yield return null;
                Assert.That(model.Current.Frame,Is.Not.SameAs(before));Assert.That(model.Current.Frame.Battle!.Reports.Any(r=>r.Round==round),Is.True);
                var accepted=model.Current.Frame;using(var e=NavigationSubmitEvent.GetPooled()){e.target=confirm;doc.rootVisualElement.panel.visualTree.SendEvent(e);}
                Assert.That(model.Current.Frame,Is.SameAs(accepted),"obsolete confirmation cannot commit twice");Assert.That(model.Current.Frame.Frontier!.Receipts,Is.Empty);
            }
            yield return Capture(entry,doc,"combat-aftermath-pending",true);
            var pending=model.Current.Frame;Click(Action("Confirm campaign aftermath"));yield return Capture(entry,doc,"combat-aftermath-confirm",true,true);
            Click(Confirm(doc.rootVisualElement));yield return null;
            Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning));Assert.That(model.Current.Frame,Is.Not.SameAs(pending));
            Assert.That(model.Current.Frame.Frontier!.Receipts.Count,Is.EqualTo(1));var final=model.Current.Frame;
            var summary=entry.Presentation!.AftermathSummary;Assert.That(summary,Is.Not.Null,"confirmed own-only session summary is available in Frontier");
            Assert.That(summary!.LostShipIds.Count,Is.EqualTo(pending.Battle!.OwnShips.Count(s=>s.Condition.Hull==0)));
            foreach(var survivor in summary.Survivors)
            {
                var actual=final.OwnShips.Single(s=>s.Condition.ShipId==survivor.Condition.ShipId);
                Assert.That(survivor.Condition.Hull,Is.EqualTo(actual.Condition.Hull));Assert.That(survivor.Condition.Shields,Is.EqualTo(actual.Condition.Shields));
                foreach(var weapon in survivor.Weapons.Where(w=>w.Class==TacticalWeaponClass.Torpedo))
                {Assert.That(weapon.Ammunition,Is.EqualTo(actual.Weapons.Single(w=>w.WeaponId==weapon.WeaponId).Ammunition));Assert.That(weapon.Ammunition,Is.LessThanOrEqualTo(weapon.MagazineCapacity));}
            }
            Assert.That(model.FinalizeBatch(),Is.False);Assert.That(model.Current.Frame,Is.SameAs(final));Assert.That(final.Demo,Is.Null);
            yield return new ExitPlayMode();
        }
        private static IEnumerator Capture(ReviewEntry entry,UIDocument doc,string state,bool combat=false,bool modal=false)
        {
            var directory=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/frontier-player-preview/rendered"));Directory.CreateDirectory(directory);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
            {
                var frame=entry.Presentation!.Model.Current.Frame;var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);target.Create();
                var prior=RenderTexture.active;Texture2D? pixels=null;
                try
                {
                    doc.panelSettings.targetTexture=target;var root=doc.rootVisualElement;root.style.width=size.x;root.style.height=size.y;yield return null;yield return null;yield return null;
                    Assert.That(root.Q("player-footer").worldBound.yMax,Is.LessThanOrEqualTo(size.y+1));Assert.That(root.Q("demo-objective"),Is.Null);
                    if(combat)
                    {
                        var actions=root.Q("central-combat-footer");Assert.That(actions,Is.Not.Null);Assert.That(actions.worldBound.yMax,Is.LessThanOrEqualTo(root.Q("player-footer").worldBound.yMin));
                        Assert.That(root.Q("central-order-dock").worldBound.yMax,Is.LessThanOrEqualTo(actions.worldBound.yMin+1));
                        foreach(var action in actions.Query<Button>().ToList()){Assert.That(action.worldBound.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(action.worldBound.xMax,Is.LessThanOrEqualTo(size.x));Assert.That(action.worldBound.yMax,Is.LessThanOrEqualTo(size.y));}
                        if(modal)foreach(var action in root.Q("review-one-use-confirmation").Query<Button>().ToList())
                        {Assert.That(action.text,Is.EqualTo("Confirm").Or.EqualTo("Cancel"));Assert.That(action.enabledInHierarchy,Is.True);Assert.That(action.worldBound.yMin,Is.GreaterThanOrEqualTo(0));Assert.That(action.worldBound.yMax,Is.LessThanOrEqualTo(size.y));Assert.That(action.worldBound.xMax,Is.LessThanOrEqualTo(size.x));}
                        Assert.That(root.Query<Button>().ToList().Any(b=>b.name.StartsWith("player-switch-",StringComparison.Ordinal)),Is.False);
                    }
                    else
                    {
                        var map=root.Q("frontier-known-map");Assert.That(map.worldBound.yMax,Is.LessThanOrEqualTo(root.Q("player-footer").worldBound.yMin));
                        foreach(var marker in map.Query<Button>().ToList()){Assert.That(marker.worldBound.xMin,Is.GreaterThanOrEqualTo(map.worldBound.xMin));Assert.That(marker.worldBound.xMax,Is.LessThanOrEqualTo(map.worldBound.xMax));Assert.That(marker.worldBound.yMax,Is.LessThanOrEqualTo(map.worldBound.yMax));}
                    }
                    Assert.That(entry.Presentation.Model.Current.Frame,Is.SameAs(frame));RenderTexture.active=target;pixels=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(directory,size.x+"x"+size.y+"-"+state+".png"),pixels.EncodeToPNG());
                }
                finally{doc.panelSettings.targetTexture=null;RenderTexture.active=prior;target.Release();UnityEngine.Object.Destroy(target);if(pixels!=null)UnityEngine.Object.Destroy(pixels);}
            }
        }
    }
}
