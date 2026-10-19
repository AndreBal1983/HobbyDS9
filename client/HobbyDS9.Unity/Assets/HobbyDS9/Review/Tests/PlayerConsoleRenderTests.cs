#nullable enable
using System;
using System.Collections;
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
    // Run separately with explicit integrated CLI flags, graphics enabled. No OS input.
    public sealed class PlayerConsoleRenderTests
    {
        [UnityTest] public IEnumerator DesignerCaptionStyleAndPixelProbe()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-player-render-tests")<0)Assert.Ignore("Dedicated graphics bootstrap required.");
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            typeof(ReviewEntry).GetMethod("Navigate",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(entry,new object[]{ReviewWorkspacePage.Designer});
            var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);target.Create();doc.panelSettings.targetTexture=target;
            var root=doc.rootVisualElement;root.style.width=1280;root.style.height=720;yield return null;yield return null;yield return null;
            foreach(var id in new[]{"designer-close","designer-reset","designer-global-issues","designer-construction"})
            {
                var b=root.Q<Button>(id);Debug.Log("DESIGN CAPTION "+id+" text="+b.text+" color="+b.resolvedStyle.color+" background="+b.resolvedStyle.backgroundColor+" size="+b.resolvedStyle.fontSize+" font="+b.resolvedStyle.unityFont+" fontDefinition="+b.resolvedStyle.unityFontDefinition+" bounds="+b.worldBound);
            }
            var back=root.Q<Button>("designer-close");
            Assert.That(root.Q<Label>("designer-state").text,Is.EqualTo("Incomplete local preview"));
            yield return null;yield return null;
            var prior=RenderTexture.active;RenderTexture.active=target;var png=new Texture2D(1280,720,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,1280,720),0,0);png.Apply();
            var output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/early-game-player-ui/designer-probe.png"));File.WriteAllBytes(output,png.EncodeToPNG());RenderTexture.active=prior;
            var bounds=back.worldBound;int ink=0;
            for(int x=(int)bounds.xMin+12;x<(int)bounds.xMax-12;x++)for(int y=(int)bounds.yMin+8;y<(int)bounds.yMax-8;y++)
            {var pixel=png.GetPixel(x,719-y);if(pixel.r<.2f&&pixel.g<.2f&&pixel.b<.2f)ink++;}
            Debug.Log("DESIGN CAPTION actual dark glyph pixels="+ink);Assert.That(ink,Is.GreaterThan(15),"Enabled Back needs actual readable glyph pixels, not only text in hierarchy");
            doc.panelSettings.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(png);
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator RenderActualRuntimeConsoleAndExercisePlayerActions()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-player-render-tests")<0)Assert.Ignore("Dedicated render run requires explicit integrated bootstrap flags.");
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(entry,Is.Not.Null);
            var document=entry.GetComponent<UIDocument>();var root=document.rootVisualElement;var model=entry.Presentation!.Model;
            Assert.That(model.Integrated,Is.True);Assert.That(GameObject.Find("HobbyDS9 Client"),Is.Null);
            var output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/early-game-player-ui/rendered"));Directory.CreateDirectory(output);
            void Invoke(string method,params object[] args)=>typeof(ReviewEntry).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(entry,args);
            void Click(string id)
            {
                root=document.rootVisualElement;
                var b=root.Q<Button>(id);Assert.That(b,Is.Not.Null,"Missing player action "+id);Assert.That(b.enabledInHierarchy,Is.True,"Disabled player action "+id);
                b.Focus();using(var e=NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}
            }
            void Modal(string caption)
            {
                root=document.rootVisualElement;
                var b=root.Q("review-one-use-confirmation").Query<Button>().ToList().Single(x=>x.text==caption);
                b.Focus();using(var e=NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}
            }
            void EscapeModal()
            {
                root=document.rootVisualElement;var modal=root.Q("review-one-use-confirmation");
                using(var e=KeyDownEvent.GetPooled(new Event{type=EventType.KeyDown,keyCode=KeyCode.Escape})){e.target=modal;modal.SendEvent(e);}
            }
            void Caption(string caption)
            {
                root=document.rootVisualElement;var b=root.Query<Button>().ToList().Single(x=>x.text==caption);
                Assert.That(b.enabledInHierarchy,Is.True,caption);b.Focus();using(var e=NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}
            }
            void Visible(VisualElement element,Rect bounds,string name)
            {
                Assert.That(element,Is.Not.Null,name);var actual=element.worldBound;
                Assert.That(actual.width,Is.GreaterThan(0),name+" has no width");
                Assert.That(actual.height,Is.GreaterThan(0),name+" has no height");
                Assert.That(actual.xMin,Is.GreaterThanOrEqualTo(bounds.xMin-1),name+" left clipped");
                Assert.That(actual.xMax,Is.LessThanOrEqualTo(bounds.xMax+1),name+" right clipped");
                Assert.That(actual.yMin,Is.GreaterThanOrEqualTo(bounds.yMin-1),name+" top clipped");
                Assert.That(actual.yMax,Is.LessThanOrEqualTo(bounds.yMax+1),name+" bottom clipped");
            }
            IEnumerator Capture(string state)
            {
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
                {
                    var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);target.Create();document.panelSettings.targetTexture=target;
                    root=document.rootVisualElement;root.style.width=size.x;root.style.height=size.y;
                    yield return null;yield return null;yield return null;
                    var summary=root.Q("player-confirmation-summary");
                    if(summary!=null)Visible(summary,new Rect(0,0,size.x,size.y),"Confirmation summary");
                    Visible(root.Q("player-footer"),new Rect(0,0,size.x,size.y),"Persistent footer");
                    var prior=RenderTexture.active;RenderTexture.active=target;var png=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);
                    png.ReadPixels(new Rect(0,0,size.x,size.y),0,0);png.Apply();File.WriteAllBytes(Path.Combine(output,size.x+"x"+size.y+"-"+state+".png"),png.EncodeToPNG());RenderTexture.active=prior;
                    document.panelSettings.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(png);
                }
            }
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
            {
                var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);target.Create();document.panelSettings.targetTexture=target;
                root.style.width=size.x;root.style.height=size.y;root.EnableInClassList("player-narrow",size.x<1600||size.y<850);
                foreach(var page in new[]{ReviewWorkspacePage.Map,ReviewWorkspacePage.Worlds,ReviewWorkspacePage.Research,ReviewWorkspacePage.Production,ReviewWorkspacePage.FleetOrders,ReviewWorkspacePage.Saves,ReviewWorkspacePage.Audio,ReviewWorkspacePage.Designer})
                {
                    Invoke("Navigate",page);document.panelSettings.targetTexture=target;root=document.rootVisualElement;root.style.width=size.x;root.style.height=size.y;root.EnableInClassList("player-narrow",size.x<1600||size.y<850);
                    yield return null;yield return null;yield return null;
                    if(page==ReviewWorkspacePage.Production){Click("production-colony-colony.sol");yield return null;yield return null;}
                    Assert.That(root.Q("player-footer"),Is.Not.Null);Assert.That(root.Q("review-workspace-navigation"),Is.Not.Null);
                    Assert.That(root.Q("player-footer").worldBound.yMax,Is.LessThanOrEqualTo(size.y+1),"Footer clipped");
                    if(page==ReviewWorkspacePage.Map)Assert.That(root.Q<Image>("map-space-backdrop").image,Is.Not.Null,"Live map decorative asset must load");
                    if(page==ReviewWorkspacePage.Research)
                    {
                        var cards=root.Query<VisualElement>().ToList().Where(e=>e.name.StartsWith("science-card-")).ToList();Assert.That(cards.Count,Is.EqualTo(6));
                        var viewport=root.Q("six-science-cards").GetFirstAncestorOfType<ScrollView>().contentViewport.worldBound;
                        foreach(var card in cards)Visible(card,viewport,card.name);
                        Click("science-plus-Construction");yield return null;yield return null;
                        Visible(root.Q("player-primary-action"),new Rect(0,0,size.x,size.y),"Apply Science action");
                        Assert.That(root.Q<Button>("player-primary-action").text,Is.EqualTo("Apply Science allocation"));
                        Assert.That(model.HasLocalEdits,Is.True);
                        Click("player-primary-action");yield return null;EscapeModal();yield return null;
                        Assert.That(model.HasLocalEdits,Is.True,"Cancel must preserve the research draft");
                        Assert.That(document.rootVisualElement.Q("review-one-use-confirmation"),Is.Null,"Escape closes only the confirmation");
                        model.Reset();Invoke("Draw");yield return null;yield return null;
                    }
                    var prior=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,size.x+"x"+size.y+"-"+page+".png"),image.EncodeToPNG());RenderTexture.active=prior;UnityEngine.Object.Destroy(image);
                    if(page==ReviewWorkspacePage.Research)
                    {
                        Click("research-tab-engineering");yield return null;yield return null;yield return null;
                        Assert.That(root.Query<VisualElement>().ToList().Any(e=>e.name.StartsWith("engineering-project-")),Is.True);
                        prior=RenderTexture.active;RenderTexture.active=target;image=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,size.x+"x"+size.y+"-Engineering.png"),image.EncodeToPNG());RenderTexture.active=prior;UnityEngine.Object.Destroy(image);
                        Click("research-tab-science");yield return null;
                    }
                    if(page==ReviewWorkspacePage.Designer&&size.x==1280)
                    {
                        var oldName=root.Q<TextField>("designer-name");oldName.value="Edited preview";yield return null;
                        Assert.That(model.LocalDesignDirty,Is.True);Assert.That(root.Q<Button>("player-primary-action").enabledInHierarchy,Is.False);
                        Click("review-nav-Map");yield return null;Click("review-nav-Designer");yield return null;
                        Assert.That(document.rootVisualElement.Q<TextField>("designer-name").value,Is.EqualTo("Edited preview"));
                        Click("review-nav-Tools");yield return null;Click("player-switch-civ.cardassian");yield return null;Modal("Confirm");yield return null;
                        Assert.That(model.LocalDesignDirty,Is.False);oldName.value="Detached former actor edit";yield return null;
                        Assert.That(model.LocalDesignDirty,Is.False,"Detached designer must not dirty a replacement actor");
                        Click("review-nav-Tools");yield return null;Click("player-switch-civ.federation");yield return null;
                    }
                }
                document.panelSettings.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);
            }
            Invoke("Navigate",ReviewWorkspacePage.Research);yield return null;root=document.rootVisualElement;var frame=model.Current.Frame;
            Click("science-plus-Construction");yield return null;Assert.That(model.HasLocalEdits,Is.True);Assert.That(model.Current.Frame,Is.SameAs(frame));
            Click("player-primary-action");yield return null;Assert.That(root.Q("player-confirmation-summary"),Is.Not.Null);
            Modal("Cancel");yield return null;
            Assert.That(model.HasLocalEdits,Is.True);Assert.That(model.Current.Frame,Is.SameAs(frame));
            Click("player-primary-action");yield return null;Modal("Confirm");yield return null;
            Assert.That(model.HasLocalEdits,Is.False);Assert.That(model.Current.Frame,Is.Not.SameAs(frame));
            root=document.rootVisualElement;root.Q<SliderInt>("integrated-research-split").value=20;yield return null;
            Click("player-primary-action");yield return null;Modal("Confirm");yield return null;
            Invoke("Navigate",ReviewWorkspacePage.Production);yield return null;root=document.rootVisualElement;
            Click("production-colony-colony.sol");yield return null;
            Click("integrated-queue-ship-design.source-cruiser");yield return null;
            var summary=root.Q("player-confirmation-summary").Query<Label>().ToList().Select(l=>l.text);var content=string.Join("\n",summary);
            Assert.That(content,Does.Contain("Cruiser").And.Contain("200").And.Contain("Dilithium").And.Contain("100"));Assert.That(content,Does.Not.Contain("YOUR ORDERS ONLY"));
            Assert.That(content,Does.Contain("Earth"));yield return Capture("Purchase-confirmation");
            Modal("Cancel");yield return null;
            Assert.That(model.Current.Frame.Strategy!.Civilization.Credits,Is.EqualTo(250));
            var originalShips=model.Current.Frame.Strategy.Ships.Select(s=>s.ShipId.Value).ToArray();
            Click("integrated-queue-ship-design.source-cruiser");yield return null;Modal("Confirm");yield return null;
            Assert.That(model.Current.Frame.Strategy.Civilization.Credits,Is.EqualTo(50));
            Assert.That(model.Current.Frame.Strategy.Colonies.Single(c=>c.ColonyId.Value=="colony.sol").ProductionQueue.Count,Is.EqualTo(1));
            yield return Capture("Paid-queue");
            for(int turn=0;turn<13;turn++){Click("player-primary-action");yield return null;Modal("Confirm");yield return null;}
            var produced=model.Current.Frame.Strategy.Ships.Single(s=>!originalShips.Contains(s.ShipId.Value));
            Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(14));
            Click("review-nav-FleetOrders");yield return null;yield return Capture("Produced-ship");
            foreach(var actor in new[]{"civ.federation","civ.cardassian"})
            {
                Click("review-nav-Tools");yield return null;Click("player-switch-"+actor);yield return null;
                Click("review-nav-FleetOrders");yield return null;root=document.rootVisualElement;var destinationPicker=root.Q<Foldout>("fleet-route-destination");Assert.That(destinationPicker,Is.Not.Null);destinationPicker.value=true;yield return null;Click("route-destination-nav.bajor");yield return null;
                Click("player-preview-route");yield return null;
                if(actor=="civ.federation")
                {
                    Click("review-nav-Map");yield return null;Click("map-node-nav.cardassia");yield return null;
                    Click("map-fleet-orders-fleet.federation-scout");yield return null;
                    Assert.That(document.rootVisualElement.Q("player-confirm-route"),Is.Null,"Map C must invalidate preview B");
                    Click("review-nav-Map");yield return null;Click("map-node-nav.bajor");yield return null;
                    Click("map-fleet-orders-fleet.federation-scout");yield return null;Click("player-preview-route");yield return null;
                }
                yield return Capture("Route-preview-"+actor);
                root=document.rootVisualElement;Visible(root.Q("player-confirm-route"),root.Q("player-confirm-route").GetFirstAncestorOfType<ScrollView>().contentViewport.worldBound,"Confirm route");
                Click("player-confirm-route");yield return null;Modal("Confirm");yield return null;
                Assert.That(model.Current.Frame.OwnStrategicFleets.Single().RouteNodeIds.Count,Is.GreaterThan(0));
            }
            int travelLimit=0;
            while(model.Current.Frame.Status.Phase==SessionPhase.Planning&&model.Current.Frame.OwnStrategicFleets.Single().LocationNodeId.Value!="nav.bajor"&&travelLimit++<5)
            {Click("player-primary-action");yield return null;Modal("Confirm");yield return null;}
            Click("review-nav-Tools");yield return null;Click("player-switch-civ.federation");yield return null;
            if(model.Current.Frame.Status.Phase==SessionPhase.Planning)
            {
                Click("review-nav-FleetOrders");yield return null;Click("player-stay-engage");yield return null;Modal("Confirm");yield return null;
                Click("player-primary-action");yield return null;Modal("Confirm");yield return null;
            }
            Assert.That(model.Current.Frame.Battle,Is.Not.Null);Click("review-nav-Combat");yield return null;
            Assert.That(model.Current.Frame.Battle!.OwnShips.Any(s=>s.Condition.ShipId==produced.ShipId.Value),Is.True);
            yield return Capture("Combat-orders");
            int roundLimit=0;
            while(!model.Current.Frame.Battle!.Complete&&roundLimit++<3)
                foreach(var actor in new[]{"civ.federation","civ.cardassian"})
                {
                    Click("review-nav-Tools");yield return null;Click("player-switch-"+actor);yield return null;Click("review-nav-Combat");yield return null;
                    if(entry.Presentation!.Stage==ReviewCombatStage.RoundResult){Caption("Plan next round");yield return null;}
                    Caption("Review / commit round "+model.Current.Frame.Battle!.Round);yield return null;Modal("Confirm");yield return null;
                    if(actor=="civ.federation"){Assert.That(entry.Presentation!.Stage,Is.EqualTo(ReviewCombatStage.Waiting));yield return Capture("Combat-waiting-"+roundLimit);}
                    else yield return Capture("Combat-result-"+roundLimit);
                }
            Assert.That(model.Current.Frame.Battle!.Complete,Is.True);yield return Capture("Aftermath-pending");
            Caption("Confirm campaign aftermath");yield return null;Modal("Confirm");yield return null;
            Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning));yield return Capture("Aftermath-confirmed");
            Click("review-nav-Tools");yield return null;Click("player-switch-civ.federation");yield return null;
            var survivor=model.Current.Frame.OwnShips.Single(s=>s.Condition.ShipId==produced.ShipId.Value);
            var ammunition=survivor.Weapons.Single(w=>w.Class==TacticalWeaponClass.Torpedo).Ammunition;Assert.That(ammunition,Is.LessThan(7));
            var savedTurn=model.Current.Frame.Status.Turn;
            typeof(ReviewEntry).GetField("slotName",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,"render-"+Guid.NewGuid().ToString("N").Substring(0,24));
            Click("review-nav-Saves");yield return null;Click("player-save");yield return null;
            Assert.That(model.Message,Is.EqualTo("Checkpoint result: None"));yield return Capture("Saved-campaign");
            Click("player-load");yield return null;Modal("Confirm");yield return null;
            Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(savedTurn));
            Assert.That(model.Current.Frame.OwnShips.Single(s=>s.Condition.ShipId==produced.ShipId.Value).Weapons.Single(w=>w.Class==TacticalWeaponClass.Torpedo).Ammunition,Is.EqualTo(ammunition));
            Click("review-nav-FleetOrders");yield return null;yield return Capture("Resumed-survivor");
            yield return new ExitPlayMode();
        }
    }
}
