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
    // Real session state, actual UI rendering/direct UI-event arrangement.
    // This is deliberately NOT OS keyboard/mouse or input-provider acceptance.
    public sealed class DemoCombatRenderTests
    {
        [UnityTest] public IEnumerator ReadableCombatSaveFeedbackAndBoundedDialogsAtThreeSizes()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-demo-render-tests")<0)Assert.Ignore("Dedicated demo graphics bootstrap required.");
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            var output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/singleplayer-demo-r2/rendered"));Directory.CreateDirectory(output);
            void Invoke(string name,params object[] args)=>typeof(ReviewEntry).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(entry,args);
            void Click(Button button){Assert.That(button.enabledInHierarchy,Is.True,button.text);using(var evt=NavigationSubmitEvent.GetPooled()){evt.target=button;button.SendEvent(evt);}}
            void Id(string id){var b=doc.rootVisualElement.Q<Button>(id);Assert.That(b,Is.Not.Null,id);Click(b);}
            void Caption(string text)=>Click(doc.rootVisualElement.Query<Button>().ToList().Single(b=>b.text==text));
            IEnumerator Capture(string name,Action verify)
            {
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
                {
                    var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);target.Create();doc.panelSettings.targetTexture=target;
                    var root=doc.rootVisualElement;root.style.width=size.x;root.style.height=size.y;yield return null;yield return null;yield return null;
                    verify();Assert.That(root.Q("player-footer").worldBound.yMax,Is.LessThanOrEqualTo(size.y+1));
                    var prior=RenderTexture.active;RenderTexture.active=target;var png=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,size.x,size.y),0,0);png.Apply();File.WriteAllBytes(Path.Combine(output,size.x+"x"+size.y+"-"+name+".png"),png.EncodeToPNG());RenderTexture.active=prior;
                    doc.panelSettings.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(png);
                }
            }
            Id("demo-start");yield return null;var model=entry.Presentation!.Model;Assert.That(model.SingleplayerDemo,Is.True);
            Id("review-nav-Combat");yield return null;
            Caption("Draft selected next-turn order");yield return null;Caption("Review / submit next-turn draft");yield return null;
            yield return Capture("Planning-order-confirmation",()=>{
                var box=doc.rootVisualElement.Q("player-confirmation-summary");var body=doc.rootVisualElement.Q<ScrollView>("player-confirmation-details");Assert.That(body,Is.Not.Null);
                var confirm=box.Query<Button>().ToList().Single(b=>b.text=="Confirm");Assert.That(confirm.worldBound.yMax,Is.LessThanOrEqualTo(box.worldBound.yMax+1));
                Assert.That(box.Query<Label>().ToList().Any(l=>l.text.Contains("ACTOR-SCOPED")||l.text.Contains("fleet.federation")),Is.False);
            });
            Caption("Cancel");yield return null;Assert.That(model.Reset(),Is.True);
            Invoke("Confirm","Layout stress fixture, not campaign results.\n"+string.Concat(Enumerable.Repeat("Long review text must scroll without pushing away Cancel or Confirm.\n",80)),(Action)(()=>{}));yield return null;
            yield return Capture("Long-dialog-layout-fixture",()=>{
                var box=doc.rootVisualElement.Q("player-confirmation-summary");var body=doc.rootVisualElement.Q<ScrollView>("player-confirmation-details");
                var confirm=box.Query<Button>().ToList().Single(b=>b.text=="Confirm");
                Assert.That(body.verticalScroller.highValue,Is.GreaterThan(0));
                Assert.That(body.contentViewport.worldBound.yMax,Is.LessThanOrEqualTo(confirm.worldBound.yMin));
                Assert.That(confirm.worldBound.yMax,Is.LessThanOrEqualTo(box.worldBound.yMax+1));
            });Caption("Cancel");yield return null;
            var own=model.Current.Frame.OwnStrategicFleets.First().FleetId.Value;
            Assert.That(model.DraftTravel(own,CampaignTravelIntent.Leave,"nav.bajor",null),Is.True);Assert.That(model.SendTravel(ReviewUiModel.NewCommandId()),Is.True);
            int limit=0;while(model.Current.Frame.Status.Phase==SessionPhase.Planning&&model.Current.Frame.OwnStrategicFleets.First().LocationNodeId.Value!="nav.bajor"&&limit++<5)Assert.That(model.Advance(),Is.True);
            if(model.Current.Frame.Status.Phase==SessionPhase.Planning){Assert.That(model.DraftTravel(own,CampaignTravelIntent.StayEngage,null,null),Is.True);Assert.That(model.SendTravel(ReviewUiModel.NewCommandId()),Is.True);Assert.That(model.Advance(),Is.True);}
            Assert.That(model.Current.Frame.Battle,Is.Not.Null);Invoke("Build");Id("review-nav-Combat");yield return null;
            void CombatBounds()
            {
                var root=doc.rootVisualElement;var rail=root.Q("central-fleet-rail");var dock=root.Q("central-order-dock");
                Assert.That(rail.worldBound.xMax,Is.LessThan(dock.worldBound.xMin));
                Assert.That(root.Q<DropdownField>("combat-fleet-picker").choices.Any(s=>s.Contains("fleet.")),Is.False);
                Assert.That(root.Q<DropdownField>("combat-ship-picker").choices.Any(s=>s.Contains("ship.")||s.Contains("PROVISIONAL")),Is.False);
                Assert.That(root.Q<Foldout>("combat-technical-details").value,Is.False);
                Assert.That(root.Q<Label>("combat-own-current-stocks").text,Does.Contain("Hull").And.Contain("Fore").And.Contain("Port").And.Contain("Starboard").And.Contain("Aft").And.Contain("Torpedoes"));
            }
            yield return Capture("Battle-orders",CombatBounds);
            Caption("Review / commit round "+model.Current.Frame.Battle!.Round);yield return null;Caption("Confirm");yield return null;
            Assert.That(model.Current.Frame.Battle!.Reports.Count,Is.GreaterThan(0));
            yield return Capture("Resolved-round",CombatBounds);
            model.ResearchInputs()!.ScienceUnits=600;Assert.That(model.HasLocalEdits,Is.True);Id("review-nav-Saves");yield return null;
            // Unique absent slot: never read/overwrite an owner's three real saves.
            typeof(ReviewEntry).GetField("slotName",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,"missing-"+Guid.NewGuid().ToString("N").Substring(0,8));Invoke("Draw");
            var before=model.Current;Id("player-load");yield return null;Caption("Confirm");yield return null;
            Assert.That(model.Current,Is.SameAs(before));Assert.That(model.HasLocalEdits,Is.True);
            yield return Capture("Failed-load-draft-kept",()=>{
                Assert.That(doc.rootVisualElement.Q<Label>("save-load-result").text,Does.Contain("empty").And.Contain("unsent edits are unchanged"));
                Assert.That(doc.rootVisualElement.Q<Label>("player-guidance").text,Does.Contain("Unsent"));
            });
            yield return new ExitPlayMode();
        }
    }
}
