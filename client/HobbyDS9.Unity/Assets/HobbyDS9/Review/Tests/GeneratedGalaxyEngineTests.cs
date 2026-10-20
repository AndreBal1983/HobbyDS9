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
    public sealed class GeneratedGalaxyEngineTests
    {
        private string? directory;
        [SetUp] public void DedicatedRunOnly()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-generated-galaxy-tests")<0)
                Assert.Ignore("Dedicated graphics receiving run required.");
        }
        [TearDown] public void Cleanup()
        {
            if(directory==null)return;
            var full=Path.GetFullPath(directory);
            if(Path.GetDirectoryName(full)!=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
                ||!Path.GetFileName(full).StartsWith("HobbyDS9-Galaxy-Engine-",StringComparison.Ordinal))throw new InvalidOperationException();
            if(Directory.Exists(full))Directory.Delete(full,true);directory=null;
        }
        private static void Click(Button button)
        {
            Assert.That(button,Is.Not.Null);Assert.That(button.enabledInHierarchy,Is.True,button.name);
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=button;button.SendEvent(e);}
        }
        private static void Confirm(UIDocument doc,bool yes=true)
        {
            var dialog=doc.rootVisualElement.Q("review-one-use-confirmation");Assert.That(dialog,Is.Not.Null);
            Click(dialog.Query<Button>().ToList().Single(b=>b.text==(yes?"Confirm":"Cancel")));
        }
        [UnityTest] public IEnumerator VisibleGenerationInspectionRoutesAndReopenUseRealPortableData()
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(entry,Is.Not.Null);
            var doc=entry.GetComponent<UIDocument>();var campaignBefore=entry.Presentation!.Model.Current.Frame;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-"+Guid.NewGuid().ToString("N"));
            var scenario=new ReviewNewtonsoftCodec().DecodeScenario(Resources.Load<TextAsset>(PlayableFleetOptions.ResourceRoot+"scenario").bytes);
            var workspace=new GeneratedGalaxyWorkspace(scenario.Definitions.Civilizations!,new GeneratedGalaxyMapStore(directory));
            typeof(ReviewEntry).GetField("galaxy",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,workspace);
            Click(doc.rootVisualElement.Q<Button>("generated-galaxy-entry"));yield return null;
            Assert.That(doc.rootVisualElement.Q("galaxy-empty"),Is.Not.Null);
            Assert.That(workspace.Document,Is.Null,"opening review must not generate or replace campaign");
            foreach(var item in new[]{("42",40),("12345",30),("20261020",50)})
            {
                var root=doc.rootVisualElement;
                root.Q<TextField>("galaxy-seed").value=item.Item1;root.Q<DropdownField>("galaxy-size").value=item.Item2.ToString();
                Click(root.Q<Button>("galaxy-generate"));
                if(root.Q("review-one-use-confirmation")!=null)Confirm(doc);
                yield return null;yield return null;
                var map=workspace.Document!;Assert.That(map.Systems.Count,Is.EqualTo(item.Item2));
                Assert.That(root.Query<Button>().ToList().Count(b=>b.name.StartsWith("galaxy-star-",StringComparison.Ordinal)),Is.EqualTo(map.Systems.Count));
                Assert.That(root.Query<Button>().ToList().Count(b=>b.name.StartsWith("galaxy-candidate-",StringComparison.Ordinal)),Is.EqualTo(2));
                yield return Capture(doc,"seed-"+item.Item1+"-overview");

                var first=map.Systems[0];Click(root.Q<Button>("galaxy-star-"+first.SystemId));yield return null;
                Assert.That(root.Q<Label>("galaxy-selection-identity").text,Does.Contain(first.SystemId));
                Assert.That(root.Q<Label>("galaxy-selection-kind").text,Is.EqualTo("STAR SYSTEM"));
                Click(root.Q<Button>("galaxy-zoom-in"));Click(root.Q<Button>("galaxy-zoom-in"));yield return null;
                Assert.That(root.Q<GeneratedGalaxyMapView>("generated-galaxy-map").Zoom,Is.GreaterThan(1.6));
                yield return Capture(doc,"seed-"+item.Item1+"-star-mid");

                var empty=map.Regions.First(r=>r.SystemId==null);
                var picker=root.Q<DropdownField>("galaxy-region-picker");
                picker.index=map.Regions.OrderBy(r=>r.NodeId,StringComparer.Ordinal).ToList().FindIndex(r=>r.NodeId==empty.NodeId);yield return null;
                Assert.That(root.Q<Label>("galaxy-selection-identity").text,Is.EqualTo(empty.NodeId));
                Assert.That(root.Query<Label>().ToList().Any(l=>l.text=="Empty navigation region · no star system"),Is.True);
                Click(root.Q<Button>("galaxy-route-start"));yield return null;
                // Actual right-click event handler on a displayed star target.
                var destination=map.Systems.Last();var star=root.Q<Button>("galaxy-star-"+destination.SystemId);
                Click(root.Q<Button>("galaxy-fit"));yield return null;
                using(var e=PointerUpEvent.GetPooled(new Event{type=EventType.MouseUp,button=1,mousePosition=star.worldBound.center}))
                {e.target=star;star.SendEvent(e);}
                yield return null;
                var expected=workspace.FindRoute(empty.NodeId,destination.NodeId);
                Assert.That(root.Q<Label>("galaxy-route-cost").text,Does.StartWith("Graph cost "+expected.TotalCost+" ·"));
                Assert.That(root.Q<Label>("galaxy-route-target-label").text,Is.EqualTo("To "+destination.DisplayName));
                yield return Capture(doc,"seed-"+item.Item1+"-empty-route");

                Click(root.Q<Button>("galaxy-candidate-"+map.HomeCandidates[0].CivilizationDefinitionId));yield return null;
                Assert.That(root.Q<Label>("galaxy-selection-kind").text,Is.EqualTo("STARTING CANDIDATE"));
                Assert.That(root.Q<Label>("galaxy-selection-identity").text,Does.Contain(map.HomeCandidates[0].NodeId));
                yield return Capture(doc,"seed-"+item.Item1+"-candidate");
                Assert.That(entry.Presentation.Model.Current.Frame,Is.SameAs(campaignBefore));
            }
            var current=workspace.Document!;
            Click(doc.rootVisualElement.Q<Button>("galaxy-generate"));Confirm(doc,false);Assert.That(workspace.Document,Is.SameAs(current));
            Click(doc.rootVisualElement.Q<Button>("galaxy-export"));Assert.That(workspace.Unexported,Is.False);
            doc.rootVisualElement.Q<TextField>("galaxy-seed").value="77";Click(doc.rootVisualElement.Q<Button>("galaxy-generate"));yield return null;
            Assert.That(workspace.Document!.ContentHash,Is.Not.EqualTo(current.ContentHash));
            Click(doc.rootVisualElement.Q<Button>("galaxy-open"));Confirm(doc);yield return null;
            Assert.That(workspace.Document!.ContentHash,Is.EqualTo(current.ContentHash));
            Assert.That(doc.rootVisualElement.Q<TextField>("galaxy-seed").value,Is.EqualTo(current.Settings.Seed));
            var restored=workspace.Document;doc.rootVisualElement.Q<DropdownField>("galaxy-slot").value="3";
            Click(doc.rootVisualElement.Q<Button>("galaxy-open"));Assert.That(workspace.Document,Is.SameAs(restored));
            Assert.That(doc.rootVisualElement.Q<Label>("galaxy-status").text,Does.Contain("empty"));
            doc.rootVisualElement.Q<TextField>("galaxy-seed").value="-1";Click(doc.rootVisualElement.Q<Button>("galaxy-generate"));
            Assert.That(workspace.Document,Is.SameAs(restored));
            yield return Capture(doc,"invalid-input-retains-map");
            Click(doc.rootVisualElement.Q<Button>("galaxy-back"));yield return null;
            Assert.That(doc.rootVisualElement.Q<Button>("playable-fleet-start"),Is.Not.Null);
            Assert.That(entry.Presentation.Model.Current.Frame,Is.SameAs(campaignBefore));
            Click(doc.rootVisualElement.Q<Button>("generated-galaxy-entry"));yield return null;
            Assert.That(workspace.Document,Is.SameAs(restored));
            yield return new ExitPlayMode();
        }
        private static IEnumerator Capture(UIDocument doc,string state)
        {
            var directory=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/generated-galaxy/engine-rendered"));
            Directory.CreateDirectory(directory);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
            {
                var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);target.Create();
                var prior=RenderTexture.active;Texture2D? pixels=null;
                try
                {
                    doc.panelSettings.targetTexture=target;var root=doc.rootVisualElement;root.style.width=size.x;root.style.height=size.y;
                    yield return null;yield return null;yield return null;
                    foreach(var name in new[]{"galaxy-seed","galaxy-size","galaxy-slot","galaxy-generate","galaxy-export","galaxy-open","galaxy-back","galaxy-fit"})
                    {
                        var control=root.Q(name);Assert.That(control,Is.Not.Null,name);
                        Assert.That(control.worldBound.xMin,Is.GreaterThanOrEqualTo(0),name);
                        Assert.That(control.worldBound.xMax,Is.LessThanOrEqualTo(size.x+1),name);
                        Assert.That(control.worldBound.yMin,Is.GreaterThanOrEqualTo(0),name);
                        Assert.That(control.worldBound.yMax,Is.LessThanOrEqualTo(size.y+1),name);
                    }
                    var map=root.Q("generated-galaxy-map");Assert.That(map.worldBound.height,Is.GreaterThan(250));
                    Assert.That(map.worldBound.width,Is.GreaterThan(600));
                    var inspector=root.Q<ScrollView>("galaxy-inspector");
                    Assert.That(inspector.contentViewport.worldBound.height,Is.GreaterThan(250));
                    Assert.That(map.worldBound.xMax,Is.LessThanOrEqualTo(inspector.worldBound.xMin));
                    RenderTexture.active=target;pixels=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);
                    pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                    File.WriteAllBytes(Path.Combine(directory,size.x+"x"+size.y+"-"+state+".png"),pixels.EncodeToPNG());
                }
                finally{doc.panelSettings.targetTexture=null;RenderTexture.active=prior;target.Release();UnityEngine.Object.Destroy(target);if(pixels!=null)UnityEngine.Object.Destroy(pixels);}
            }
        }
    }
}
