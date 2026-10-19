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
    // Actual campaign/controls arranged with direct UI events, not OS input evidence.
    public sealed class DemoAftermathSummaryRenderTests
    {
        [UnityTest] public IEnumerator ConfirmedOwnAftermathAndSessionBoundaryAtThreeSizes()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-demo-render-tests")<0)Assert.Ignore("Dedicated demo graphics bootstrap required.");
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            void Invoke(string name)=>typeof(ReviewEntry).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(entry,null);
            void Click(Button button){Assert.That(button,Is.Not.Null);Assert.That(button.enabledInHierarchy,Is.True,button.text);using(var e=NavigationSubmitEvent.GetPooled()){e.target=button;button.SendEvent(e);}}
            void Id(string id)=>Click(doc.rootVisualElement.Q<Button>(id));
            void Caption(string caption)=>Click(doc.rootVisualElement.Query<Button>().ToList().Single(b=>b.text==caption));
            var output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/demo-aftermath-summary/rendered"));Directory.CreateDirectory(output);
            IEnumerator Capture(string name,Action verify)
            {
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
                {
                    var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);target.Create();doc.panelSettings.targetTexture=target;
                    var root=doc.rootVisualElement;root.style.width=size.x;root.style.height=size.y;yield return null;yield return null;yield return null;
                    var prior=RenderTexture.active;RenderTexture.active=target;var png=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,size.x,size.y),0,0);png.Apply();File.WriteAllBytes(Path.Combine(output,size.x+"x"+size.y+"-"+name+".png"),png.EncodeToPNG());RenderTexture.active=prior;
                    verify();Assert.That(root.Q("player-footer").worldBound.yMax,Is.LessThanOrEqualTo(size.y+1));
                    doc.panelSettings.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(png);
                }
            }
            Id("demo-start");yield return null;var model=entry.Presentation!.Model;
            Assert.That(model.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()),Is.True);
            Assert.That(model.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()),Is.True);
            Assert.That(model.AllocateResearch(450,ReviewUiModel.NewCommandId()),Is.True);
            while(model.Current.Frame.Status.Turn<5)Assert.That(model.Advance(),Is.True);
            Assert.That(model.Current.Frame.OwnShips.Count,Is.EqualTo(3));var fleet=model.Current.Frame.OwnStrategicFleets.Single().FleetId.Value;
            Assert.That(model.DraftTravel(fleet,CampaignTravelIntent.Leave,"nav.bajor",null),Is.True);Assert.That(model.SendTravel(ReviewUiModel.NewCommandId()),Is.True);Assert.That(model.Advance(),Is.True);
            while(!model.Current.Frame.Battle!.Complete)
            {
                var b=model.Current.Frame.Battle;
                foreach(var order in b.StandingOrders.Where(o=>o.CanOrder))Assert.That(model.DraftFleet(order.FleetId,CampaignFleetManeuver.Charge,b.LegalFocusFleetIds.FirstOrDefault(),false),Is.True);
                Assert.That(model.CommitRound(ReviewUiModel.NewCommandId()),Is.True);
            }
            var completed=model.Current.Frame.Battle!;Assert.That(completed.OwnShips.Count(s=>s.Condition.Hull==0),Is.EqualTo(1));
            Invoke("Build");Id("review-nav-Combat");yield return null;Caption("Confirm campaign aftermath");yield return null;Caption("Confirm");yield return null;
            var flow=(ReviewCombatFlow)typeof(ReviewEntry).GetField("flow",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(entry)!;
            var summary=flow.AftermathSummary;Assert.That(summary,Is.Not.Null);var frame=model.Current.Frame;Assert.That(frame.Battle,Is.Null);
            void SummaryBounds()
            {
                var root=doc.rootVisualElement;var text=root.Q<Label>("combat-current-detail").text;
                Assert.That(text,Does.Contain("aftermath confirmed").And.Contain("Own ships lost: 1").And.Contain("own survivors: 2").And.Contain("Selected encounter only").And.Contain("not saved"));
                foreach(var ship in summary!.Survivors)
                {
                    Assert.That(text,Does.Contain("Hull "+ship.Condition.Hull+"/"+ship.Condition.MaxHull));
                    foreach(var w in ship.Weapons.Where(w=>w.Class==TacticalWeaponClass.Torpedo))Assert.That(text,Does.Contain("Torpedoes "+w.Ammunition+"/"+w.MagazineCapacity));
                }
                Assert.That(text,Does.Not.Contain("ship.").And.Not.Contain("commit one round").And.Not.Contain("civ.cardassian"));
                Assert.That(root.Query<Button>().ToList().Any(b=>b.text=="Battle summary"),Is.True);
                var detail=root.Q<ScrollView>("central-combat-details");Assert.That(detail.contentViewport.worldBound.height,Is.GreaterThanOrEqualTo(80));
                Assert.That(detail.contentViewport.worldBound.yMax,Is.LessThanOrEqualTo(root.Q("central-combat-footer").worldBound.yMin));
                Assert.That(detail.horizontalScroller.highValue,Is.LessThanOrEqualTo(1),"No horizontal text clipping/scroll");
                if(root.resolvedStyle.width<1600)Assert.That(detail.verticalScroller.highValue,Is.GreaterThan(0),"Compact summary reachable by vertical scroll");
                Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(flow.AftermathSummary,Is.SameAs(summary));
            }
            yield return Capture("Confirmed-selected-aftermath",SummaryBounds);
            Caption("Ship status");yield return null;
            Assert.That(doc.rootVisualElement.Q("combat-own-current-stocks"),Is.Not.Null,"Ship status keeps its selected own-stock readout");
            Assert.That(doc.rootVisualElement.Q("combat-schematic"),Is.Not.Null);
            Caption("Battle summary");yield return null;
            Assert.That(doc.rootVisualElement.Q("combat-own-current-stocks"),Is.Null,"Summary contains all survivor stocks without a duplicate block");
            Assert.That(doc.rootVisualElement.Q("combat-schematic"),Is.Null);
            Id("review-nav-Map");yield return null;Id("review-nav-Combat");yield return null;yield return Capture("Aftermath-kept-on-navigation",SummaryBounds);
            Id("review-nav-Saves");yield return null;typeof(ReviewEntry).GetField("slotName",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(entry,"missing-"+Guid.NewGuid().ToString("N").Substring(0,8));Invoke("Draw");
            Id("player-load");yield return null;Caption("Confirm");yield return null;Assert.That(flow.AftermathSummary,Is.SameAs(summary));
            Id("review-nav-Combat");yield return null;yield return Capture("Aftermath-kept-on-failed-load",SummaryBounds);
            Invoke("StartFreshDemo");yield return null;Id("review-nav-Combat");yield return null;Caption("Round report");yield return null;
            yield return Capture("Replacement-session-no-history",()=>{
                Assert.That(doc.rootVisualElement.Q<Label>("combat-current-detail").text,Does.Contain("No battle summary is retained").And.Not.Contain("commit one round"));
                Assert.That(entry.Presentation!.Model.Current.Frame.OwnShips,Is.Not.Empty);
            });
            yield return new ExitPlayMode();
        }
    }
}
