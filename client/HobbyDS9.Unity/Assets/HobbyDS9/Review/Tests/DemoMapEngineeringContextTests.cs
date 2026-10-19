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
    // Real actor-filtered session + arranged UI events, not native input acceptance.
    public sealed class DemoMapEngineeringContextTests
    {
        [UnityTest] public IEnumerator ActualMapScopeAndEngineeringLimitsAtThreeSizes()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-demo-render-tests")<0)Assert.Ignore("Dedicated demo graphics bootstrap required.");
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            void Invoke(string name)=>typeof(ReviewEntry).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(entry,null);
            void Click(string id)
            {
                var button=doc.rootVisualElement.Q<Button>(id);Assert.That(button,Is.Not.Null,id);Assert.That(button.enabledInHierarchy,Is.True,id);
                using(var evt=NavigationSubmitEvent.GetPooled()){evt.target=button;button.SendEvent(evt);}
            }
            var output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/demo-engineering-map-context/rendered"));Directory.CreateDirectory(output);
            void Readable(Label label,VisualElement viewport)
            {
                Assert.That(label.worldBound.width,Is.GreaterThan(0));Assert.That(label.worldBound.height,Is.GreaterThan(0));
                Assert.That(label.worldBound.xMin,Is.GreaterThanOrEqualTo(viewport.worldBound.xMin-1));Assert.That(label.worldBound.xMax,Is.LessThanOrEqualTo(viewport.worldBound.xMax+1));
                Assert.That(label.worldBound.yMin,Is.GreaterThanOrEqualTo(viewport.worldBound.yMin-1));Assert.That(label.worldBound.yMax,Is.LessThanOrEqualTo(viewport.worldBound.yMax+1));
                var height=label.MeasureTextSize(label.text,label.contentRect.width,VisualElement.MeasureMode.Exactly,0,VisualElement.MeasureMode.Undefined).y;
                Assert.That(label.contentRect.height+1,Is.GreaterThanOrEqualTo(height),label.name+" complete wrapped text");
            }
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
            Click("demo-start");yield return null;var model=entry.Presentation!.Model;var original=model.Current.Frame;
            void CheckMap(string? selectedId)
            {
                var root=doc.rootVisualElement;var facts=model.Current.Frame.Strategy!;
                var context=root.Q<ScrollView>(className:"console-aside");var scope=root.Q<Label>("map-worlds-scope");Readable(scope,context.contentViewport);
                Assert.That(root.Q<Label>("map-own-fleets-heading").text,Is.EqualTo("Your fleets"));
                Assert.That(root.Query<Button>().ToList().Count(b=>b.name.StartsWith("map-fleet-orders-")),Is.EqualTo(facts.Fleets.Count));
                Assert.That(root.Q("integrated-known-map").Query<Button>().ToList().Count,Is.EqualTo(facts.Navigation.Nodes.Count));
                var selected=facts.Navigation.Nodes.FirstOrDefault(n=>n.NodeId.Value==selectedId);
                Assert.That(scope.text,Is.EqualTo(selected==null?"Your worlds — no sector selected":"Selected sector: "+selected.DisplayName));
                var worlds=facts.Colonies.Where(c=>selected==null||c.SystemId==selected.AssociatedSystemId).ToList();
                Assert.That(root.Query<Button>().ToList().Count(b=>b.name.StartsWith("world-inspect-")),Is.EqualTo(worlds.Count));
                foreach(var world in worlds)Assert.That(root.Q("world-inspect-"+world.ColonyId),Is.Not.Null);
                var main=root.Q<ScrollView>(className:"console-work");Readable(root.Q<Label>("map-legend-ordinary"),main.contentViewport);Readable(root.Q<Label>("map-legend-wormhole"),main.contentViewport);
                Assert.That(model.Current.Frame,Is.SameAs(original),"Selection/rendering does not submit orders or advance state");
            }
            Click("review-nav-Map");yield return null;yield return Capture("Map-all-owned-worlds",()=>CheckMap(null));
            var sector=original.Strategy!.Navigation.Nodes.First(n=>original.Strategy.Colonies.Any(c=>c.SystemId==n.AssociatedSystemId));
            Click("map-node-"+sector.NodeId);yield return null;yield return Capture("Map-selected-sector",()=>CheckMap(sector.NodeId.Value));
            Click("review-nav-Research");yield return null;Click("research-tab-engineering");yield return null;
            void CheckEngineering()
            {
                var root=doc.rootVisualElement;var facts=model.Current.Frame.Strategy!;var project=facts.Research.Engineering.Single();
                var card=root.Q("engineering-project-"+project.Definition.DefinitionId);var qualifier=card.Q<Label>("engineering-demo-construction");var progress=card.Q<ProgressBar>();
                Readable(qualifier,root.Q<ScrollView>(className:"console-work").contentViewport);
                Assert.That(qualifier.worldBound.yMin,Is.GreaterThanOrEqualTo(card.Q("engineering-project-heading").worldBound.yMax));
                Assert.That(qualifier.worldBound.yMax,Is.LessThanOrEqualTo(progress.worldBound.yMin),"Construction limit before progress");
                foreach(var option in facts.Construction)Assert.That(qualifier.text,Does.Contain(option.Design.DefinitionId.Value=="ship-design.source-cruiser"?"Cruiser":option.Design.DisplayName));
                Assert.That(card.Q<Label>("engineering-project-cost").text,Does.Contain(((decimal)project.Cost/ResearchUnits.Scale).ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)).And.Contain("Engineering points"));
                Assert.That(root.Q<Label>("engineering-reserve").text,Does.Contain(facts.Research.EngineeringReserveConceptual.ToString()).And.Contain("Engineering points"));
                var details=card.Q<Foldout>("engineering-project-details");Assert.That(details.value,Is.False);
                Assert.That(details.Q<Label>("engineering-authored-effect").text,Is.EqualTo(project.Definition.EffectSummary));
                Assert.That(details.Q("engineering-effect-qualifier").parent,Is.SameAs(details.Q("engineering-authored-effect").parent));
                if(project.Band==EngineeringMaturityBand.Mature)Assert.That(card.Q<Label>("engineering-project-state").text,Is.EqualTo("Completed"));
            }
            yield return Capture("Engineering-initial-limit",CheckEngineering);
            // Earn actual source prerequisites through supported commands, without save edits or fixtures granting eligibility.
            Assert.That(model.AllocateResearch(ResearchUnits.Scale,ReviewUiModel.NewCommandId()),Is.True);
            for(int step=0;step<8&&!model.Current.Frame.Strategy!.Research.Engineering.Single().CanTarget;step++)
            {
                var project=model.Current.Frame.Strategy!.Research.Engineering.Single();var index=Enumerable.Range(0,project.ScienceSatisfied.Count).First(i=>!project.ScienceSatisfied[i]);var discipline=project.Definition.ScienceRequirements[index].Discipline;
                if(model.Current.Frame.Strategy.Research.Science.Any(s=>s.AllocationWeight!=(s.Discipline==discipline?ResearchUnits.Scale:0)))
                    Assert.That(model.AllocateScience(model.Current.Frame.Strategy.Research.Science.Select(s=>new ScienceAllocationDto{Discipline=s.Discipline,Weight=s.Discipline==discipline?ResearchUnits.Scale:0}).ToArray(),ReviewUiModel.NewCommandId()),Is.True);
                Assert.That(model.Advance(),Is.True);
            }
            var available=model.Current.Frame.Strategy!.Research.Engineering.Single();Assert.That(available.CanTarget,Is.True);
            Assert.That(model.Engineering(available.Definition.DefinitionId.Value,ReviewUiModel.NewCommandId()),Is.True);Assert.That(model.AllocateResearch(0,ReviewUiModel.NewCommandId()),Is.True);
            Assert.That(model.Advance(),Is.True);Assert.That(model.Current.Frame.Strategy!.Research.Engineering.Single().Band,Is.EqualTo(EngineeringMaturityBand.Mature));
            Invoke("Build");Click("review-nav-Research");yield return null;Click("research-tab-engineering");yield return null;yield return Capture("Engineering-completed-limit",CheckEngineering);
            var fold=doc.rootVisualElement.Q<Foldout>("engineering-project-details");fold.value=true;yield return null;
            yield return Capture("Engineering-qualified-effect-details",()=>{
                Assert.That(doc.rootVisualElement.Q<Foldout>("engineering-project-details").value,Is.True);
                Assert.That(doc.rootVisualElement.Q<Label>("engineering-effect-qualifier").text,Does.Contain("demo construction limits"));
                Assert.That(doc.rootVisualElement.Q<Label>("engineering-authored-effect").text,Is.EqualTo(model.Current.Frame.Strategy!.Research.Engineering.Single().Definition.EffectSummary));
            });
            for(int step=0;step<12&&model.Current.Frame.Demo!.Outcome==CampaignDemoOutcome.Active;step++)Assert.That(model.Advance(),Is.True);
            Assert.That(model.Current.Frame.Demo!.Outcome,Is.EqualTo(CampaignDemoOutcome.Defeat));Invoke("Build");Click("review-nav-Research");yield return null;Click("research-tab-engineering");yield return null;
            yield return Capture("Engineering-terminal-inspection",()=>{
                CheckEngineering();Assert.That(doc.rootVisualElement.Q<Label>("research-readonly-reason").text,Does.Contain("Campaign complete").And.Not.Contain("until combat"));
                Assert.That(doc.rootVisualElement.Query<Button>().ToList().Where(b=>b.name.StartsWith("integrated-develop-")).All(b=>!b.enabledInHierarchy),Is.True);
            });
            yield return new ExitPlayMode();
        }
    }
}
