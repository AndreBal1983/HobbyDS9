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
    // Actual engine/control/render checks, not physical mouse/keyboard acceptance.
    public sealed class PlayableFleetEngineTests
    {
        private readonly List<string> directories=new List<string>();
        [SetUp] public void RequireDedicatedGraphicsRun()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-playable-fleet-tests")<0)
                Assert.Ignore("Dedicated graphics receiving run required.");
        }
        private static object? Invoke(ReviewEntry entry,string name,params object[] args)=>
            typeof(ReviewEntry).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(entry,args);
        private ReviewUiModel Model(ReviewEntry entry)
        {
            byte[] Bytes(string name)=>Resources.Load<TextAsset>(PlayableFleetOptions.ResourceRoot+name).bytes;
            var codec=new ReviewNewtonsoftCodec();
            var package=ReviewExactPackageLoader.LoadPlayableFleet(Bytes("playable"),Bytes("package"),Bytes("physical"),Bytes("core"),Bytes("scenario"),codec);
            var directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Playable-Engine-"+Guid.NewGuid().ToString("N"));
            directories.Add(directory);
            var model=new ReviewUiModel(package,codec,new ReviewPlayableFleetSlotStore(directory,codec));
            Invoke(entry,"PublishPlayerProfile",model,"fleet-1");return model;
        }
        [TearDown] public void RemoveOnlyOwnedFixtureDirectories()
        {
            foreach(var directory in directories)
            {
                var full=Path.GetFullPath(directory);
                if(Path.GetDirectoryName(full)!=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
                    ||!Path.GetFileName(full).StartsWith("HobbyDS9-Playable-Engine-",StringComparison.Ordinal))
                    throw new InvalidOperationException("Unsafe fixture cleanup.");
                if(Directory.Exists(full))Directory.Delete(full,true);
            }
            directories.Clear();
        }
        private static void Click(Button button)
        {
            Assert.That(button,Is.Not.Null);Assert.That(button.enabledInHierarchy,Is.True,button.name);
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=button;button.SendEvent(e);}
        }
        private static void Key(VisualElement target,KeyCode code,bool up=false,bool shift=false)
        {
            var source=new Event{type=up?EventType.KeyUp:EventType.KeyDown,keyCode=code,shift=shift};
            if(up){using(var e=KeyUpEvent.GetPooled(source)){e.target=target;target.SendEvent(e);}}
            else{using(var e=KeyDownEvent.GetPooled(source)){e.target=target;target.SendEvent(e);}}
        }
        private static Button Confirm(UIDocument doc)=>doc.rootVisualElement.Q("review-one-use-confirmation")
            .Query<Button>().ToList().Single(b=>b.text=="Confirm");
        private static void Draw(ReviewEntry entry)=>Invoke(entry,"Draw");
        private static void Page(ReviewEntry entry,ReviewWorkspacePage page)=>Invoke(entry,"Navigate",page);

        [UnityTest] public IEnumerator MapKeyboardFamiliesUseGuardedActivationAndKeepHeldOrigins()
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var model=Model(entry);
            var doc=entry.GetComponent<UIDocument>();yield return null;
            var initial=model.Current.Frame;var panel=doc.rootVisualElement.panel.visualTree;
            var ring=doc.rootVisualElement.Q<Button>("map-ring-nav.sol");ring.Focus();Key(ring,KeyCode.Return);
            Assert.That(entry.Workspace!.SelectedFleet,Is.Not.Null);
            Assert.That(ring.panel,Is.SameAs(doc.rootVisualElement.panel),"held origin survives the repaint");
            Assert.That(ring.enabledInHierarchy,Is.False);
            var star=doc.rootVisualElement.Q<Button>("map-star-nav.bajor");star.Focus();Key(star,KeyCode.Return);
            Assert.That(entry.Workspace.SelectedNode,Is.EqualTo("nav.sol"),"held key must not select another marker");
            Key(panel,KeyCode.Return,true);star.Focus();Key(star,KeyCode.Return);
            Assert.That(entry.Workspace.SelectedNode,Is.EqualTo("nav.bajor"));Key(panel,KeyCode.Return,true);
            Assert.That(model.Current.Frame,Is.SameAs(initial),"selection never submits an order");

            var contact=doc.rootVisualElement.Query<Button>().ToList().First(b=>b.name.StartsWith("map-contact-",StringComparison.Ordinal));
            contact.Focus();Key(contact,KeyCode.Space);
            Assert.That(doc.rootVisualElement.Q<Label>("map-selection-notice").text,Does.StartWith("Observed hostile contact"));
            Key(panel,KeyCode.Space,true);Key(contact,KeyCode.F10,false,true);
            Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Not.Null);
            var selected=entry.Workspace.SelectedNode;
            Key(contact,KeyCode.Space);Assert.That(entry.Workspace.SelectedNode,Is.EqualTo(selected));
            Key(Confirm(doc),KeyCode.Escape);Assert.That(model.Current.Frame,Is.SameAs(initial));

            var primary=doc.rootVisualElement.Q<Button>("player-primary-action");primary.Focus();Key(primary,KeyCode.Return);
            var confirm=Confirm(doc);confirm.Focus();Key(confirm,KeyCode.Return);
            Assert.That(model.Current.Frame,Is.SameAs(initial),"held origin cannot confirm the next dialog");
            Key(panel,KeyCode.Return,true);confirm.Focus();Key(confirm,KeyCode.Return);
            Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(initial.Status.Turn+1));Key(panel,KeyCode.Return,true);
            // A detached pre-turn control must not remain usable through Submit.
            var after=model.Current.Frame;using(var e=NavigationSubmitEvent.GetPooled()){e.target=primary;primary.SendEvent(e);}
            Assert.That(model.Current.Frame,Is.SameAs(after));

            var fleet=model.NamedFleets().First().Fleet.FleetId.Value;
            var offer=model.Current.FindKnownRoute(fleet,"nav.bajor")!;
            Assert.That(model.SendPreviewedTravel(offer,ReviewUiModel.NewCommandId()),Is.True);
            Assert.That(model.Advance(),Is.True);entry.Workspace.SelectedFleet=null;Draw(entry);yield return null;
            var moving=doc.rootVisualElement.Query<Button>().ToList().First(b=>b.name.StartsWith("map-moving-fleet-",StringComparison.Ordinal));
            moving.Focus();Key(moving,KeyCode.Space);Assert.That(entry.Workspace.SelectedFleet,Is.EqualTo(fleet));Key(panel,KeyCode.Space,true);
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator ActualPaidFleetRoundPanelsRenderAtThreeSizesAndSkipDoesNotResolveAnotherRound()
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var model=Model(entry);
            var doc=entry.GetComponent<UIDocument>();yield return null;
            yield return Capture(entry,doc,"map");
            foreach(var page in new[]{ReviewWorkspacePage.Empire,ReviewWorkspacePage.Research,ReviewWorkspacePage.Production,ReviewWorkspacePage.FleetOrders})
            {Page(entry,page);yield return Capture(entry,doc,page.ToString());}
            Assert.That(model.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()),Is.True);
            Assert.That(model.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()),Is.True);
            for(int i=0;i<4;i++)Assert.That(model.Advance(),Is.True);
            Assert.That(model.OrganizeFleet(model.Current.Frame,CampaignFleetOrganizationKind.Create,
                model.Current.Frame.Strategy!.Ships.Select(s=>s.ShipId.Value).ToArray(),null,"Engine task force",ReviewUiModel.NewCommandId()),Is.True);
            var fleet=model.NamedFleets().Single().Fleet.FleetId.Value;
            var contact=model.Current.Frame.Travel!.KnownContacts.First(c=>c.Fresh);
            Assert.That(model.SendPreviewedFollow(model.Current.FindKnownRoute(fleet,contact.LastSeenNodeId)!,contact.FleetId,
                contact.LastSeenTurn,ReviewUiModel.NewCommandId()),Is.True);
            for(int i=0;i<12&&model.Current.Frame.Status.Phase==SessionPhase.Planning;i++)Assert.That(model.Advance(),Is.True);
            Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.AwaitingCombat));Page(entry,ReviewWorkspacePage.Combat);
            var flow=entry.Presentation!;
            while(!model.Current.Frame.Battle!.Complete)
            {
                yield return Capture(entry,doc,"orders-"+model.Current.Frame.Battle.Round);
                Assert.That(flow.CommitRound(ReviewUiModel.NewCommandId(),true),Is.True);Draw(entry);yield return null;
                var before=model.Current.Frame;var count=flow.Playback!.Rounds.Count;
                Assert.That(flow.CanCommitRound,Is.False);Assert.That(flow.CanFinalize,Is.False);
                yield return Capture(entry,doc,"playback-"+count);
                Click(doc.rootVisualElement.Q<Button>("battle-playback-pause"));
                Click(doc.rootVisualElement.Q<Button>("battle-playback-skip"));
                Assert.That(model.Current.Frame,Is.SameAs(before));Assert.That(flow.Playback.Rounds.Count,Is.EqualTo(count));
                Assert.That(flow.PendingPlayback,Is.Null);
                if(!model.Current.Frame.Battle.Complete){Assert.That(flow.PlanNextRound(),Is.True);Draw(entry);}
            }
            Assert.That(flow.FinalizeAftermath(true),Is.True);Draw(entry);yield return null;
            yield return Capture(entry,doc,"aftermath");
            Assert.That(model.Save("fleet-1"),Is.True);Assert.That(model.Load("fleet-1"),Is.True);Draw(entry);
            Assert.That(model.History().Count,Is.EqualTo(1));Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning));
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator LargeFormationsFitBothCamerasAndAllResourcesAreReleased()
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var rootsBefore=Resources.FindObjectsOfTypeAll<GameObject>().Count(g=>g.name=="Resolved battle presentation");
            foreach(var count in new[]{12,32})
            {
                var own=Enumerable.Range(0,count).Select(i=>new CampaignTacticalCondition("ship.own."+i,"fleet.own","Cruiser",10,10,new long[]{1,1,1,1})).ToArray();
                var enemy=new CampaignTacticalCondition("ship.enemy","fleet.enemy","Cruiser",10,10,new long[]{1,1,1,1});
                var identities=own.Select(s=>new CampaignPlaybackShipIdentity(s.ShipId,"civilization.federation","ship-design.source-cruiser"))
                    .Concat(new[]{new CampaignPlaybackShipIdentity(enemy.ShipId,"civilization.cardassian","ship-design.travel-target")}).ToArray();
                var boundary=new CampaignPlaybackBoundary(own,new[]{enemy},Array.Empty<CampaignTacticalRelation>(),identities);
                RenderTexture texture;
                using(var stage=new ReviewBattleStage(boundary,null,ReviewBattleVisualBinding.Identity))
                {
                    texture=stage.Texture;Assert.That(stage.GenericMarkers,Is.Zero,"both real imported models must load");
                    foreach(var size in new[]{new Vector2Int(1280,600),new Vector2Int(600,1280)})
                    {
                        texture.Release();texture.width=size.x;texture.height=size.y;texture.Create();
                        for(int view=0;view<2;view++)
                        {
                            stage.ToggleCamera();
                            var camera=Resources.FindObjectsOfTypeAll<Camera>().Single(c=>c.name=="Battle camera");
                            var root=camera.transform.parent;
                            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                            for(int corner=0;corner<8;corner++)
                            {
                                var bounds=renderer.localBounds;
                                var point=renderer.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(
                                    (corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                                var projected=camera.WorldToViewportPoint(point);
                                Assert.That(projected.x,Is.InRange(0f,1f));Assert.That(projected.y,Is.InRange(0f,1f));
                                Assert.That(projected.z,Is.InRange(camera.nearClipPlane,camera.farClipPlane));
                            }
                        }
                    }
                }
                yield return null;Assert.That(texture==null,Is.True);
                Assert.That(Resources.FindObjectsOfTypeAll<GameObject>().Count(g=>g.name=="Resolved battle presentation"),Is.EqualTo(rootsBefore));
            }
            yield return new ExitPlayMode();
        }
        private static IEnumerator Capture(ReviewEntry entry,UIDocument doc,string state)
        {
            var directory=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/playable-frontend/engine-rendered"));
            Directory.CreateDirectory(directory);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
            {
                var before=entry.Presentation!.Model.Current.Frame;
                var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);target.Create();
                var prior=RenderTexture.active;Texture2D? pixels=null;
                try
                {
                    doc.panelSettings.targetTexture=target;var root=doc.rootVisualElement;root.style.width=size.x;root.style.height=size.y;
                    yield return null;yield return null;yield return null;
                    var footer=root.Q("player-footer");Assert.That(footer.worldBound.yMax,Is.LessThanOrEqualTo(size.y+1));
                    foreach(var id in new[]{"player-primary-action","battle-playback-skip","battle-playback-pause"})
                    {
                        var control=root.Q<Button>(id);if(control==null)continue;
                        Assert.That(control.worldBound.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(control.worldBound.xMax,Is.LessThanOrEqualTo(size.x+1));
                        Assert.That(control.worldBound.yMin,Is.GreaterThanOrEqualTo(0));Assert.That(control.worldBound.yMax,Is.LessThanOrEqualTo(size.y+1));
                    }
                    var viewport=root.Q<Image>("battle-3d-image");
                    if(viewport!=null)
                    {Assert.That(viewport.image,Is.Not.Null);Assert.That(root.Q<Label>("battle-playback-caption").text,Does.Not.Contain("unavailable"));}
                    Assert.That(entry.Presentation.Model.Current.Frame,Is.SameAs(before));
                    RenderTexture.active=target;pixels=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);
                    pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                    File.WriteAllBytes(Path.Combine(directory,size.x+"x"+size.y+"-"+state+".png"),pixels.EncodeToPNG());
                }
                finally{doc.panelSettings.targetTexture=null;RenderTexture.active=prior;target.Release();UnityEngine.Object.Destroy(target);if(pixels!=null)UnityEngine.Object.Destroy(pixels);}
            }
        }
    }
}
