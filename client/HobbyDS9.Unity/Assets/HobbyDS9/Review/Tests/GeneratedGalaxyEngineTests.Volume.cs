#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    public sealed partial class GeneratedGalaxyEngineTests
    {
        [UnityTest] public IEnumerator ContinuousGalaxyFieldPreservesMapRoutesPickingAndLifecycle()
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            int originalInstances=GeneratedGalaxyVolumeBackground.LiveInstances;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-"+Guid.NewGuid().ToString("N"));
            var codec=new ReviewNewtonsoftCodec();var scenario=codec.DecodeScenario(Resources.Load<TextAsset>(PlayableFleetOptions.ResourceRoot+"scenario").bytes);
            var repo=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../.."));
            var store=new GeneratedGalaxyMapStore(Path.Combine(directory,"maps"));
            foreach(var item in new[]{(250,1),(1000,2)})store.Write(item.Item2,File.ReadAllBytes(Path.Combine(repo,"docs/samples/natural-galaxy-v5/seed-42-systems-"+item.Item1+".natural.json")),false);
            var workspace=new GeneratedGalaxyWorkspace(scenario.Definitions.Civilizations!,store){Slot=1};Assert.That(workspace.Open(),Is.True);
            typeof(ReviewEntry).GetField("galaxy",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,workspace);
            typeof(ReviewEntry).GetField("galaxyDraftStore",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,new GeneratedGalaxyRouteDraftStore(Path.Combine(directory,"drafts")));
            Click(doc.rootVisualElement.Q<Button>("generated-galaxy-entry"));yield return null;
            using(var surface=new GalaxyInputSurface(doc))
            {
                yield return null;yield return null;yield return null;
                var root=doc.rootVisualElement;var view=root.Q<GeneratedGalaxyMapView>();
                Assert.That(view.BackgroundRenderer,Is.Not.Null,view.BackgroundStatus);
                Assert.That(view.BackgroundRenderer!.Texture!.IsCreated(),Is.True);
                Assert.That(view.BackgroundRenderer.Texture.format,Is.EqualTo(RenderTextureFormat.ARGBHalf),"Dark gradients require the supported half-float output target");
                var bytes=workspace.ExportBytes();var data=workspace.Document!;string hash=data.ContentHash;
                var star=data.Systems[0];Click(root.Q<Button>("galaxy-star-"+star.SystemId));Click(root.Q<Button>("galaxy-route-start"));yield return null;
                view=root.Q<GeneratedGalaxyMapView>();var next=data.Systems[1];
                yield return RightClick(view,view.LocalToWorld(view.SystemPosition(next.SystemId)),false);
                var plan=(GalaxyReviewRoutePlanSnapshot)typeof(ReviewEntry).GetField("galaxyPlan",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry)!;
                Assert.That(plan.Success,Is.True);var steps=plan.Steps.ToArray();var cost=plan.TotalCost;
                yield return Capture(doc,"volume-250-overview",plan:plan); // three normal sizes
                yield return null;yield return null;yield return null;
                view=root.Q<GeneratedGalaxyMapView>();view.ZoomBy(3);yield return null;
                yield return Capture(doc,"volume-250-regional",plan:plan); // three sizes
                yield return null;yield return null;yield return null;
                var renderer=view.BackgroundRenderer!;int renders=renderer.RenderCount;
                yield return null;yield return null;yield return null;
                Assert.That(renderer.RenderCount,Is.EqualTo(renders),"No GPU submission on stationary frames");
                var beforeStars=data.Systems.Take(5).Select(s=>view.SystemPosition(s.SystemId)).ToArray();
                var path=view.RouteLegPositions().Select(l=>l.ToArray()).ToArray();
                view.ToggleBackgroundDepth();yield return null;
                Assert.That(view.BackgroundDepth,Is.False);
                CollectionAssert.AreEqual(beforeStars,data.Systems.Take(5).Select(s=>view.SystemPosition(s.SystemId)).ToArray());
                CollectionAssert.AreEqual(path.SelectMany(p=>p),view.RouteLegPositions().SelectMany(p=>p));
                yield return VolumeFrame(doc,"volume-regional-depth-off");
                view.ToggleBackgroundDepth();yield return null;
                yield return VolumeFrame(doc,"volume-regional-depth-on");
                // Keep an actual large nebula on screen for consecutive motion
                // evidence; an empty clamped edge alone cannot prove cloud quality.
                var cloud=data.Features.Where(f=>f.Kind==GalaxyEnvironmentFeatureKind.Nebula).OrderByDescending(f=>(long)f.RadiusX*f.RadiusY).First();
                var cloudPoint=view.FeaturePosition(cloud.FeatureId);view.Pan(view.contentRect.center.x-cloudPoint.x,view.contentRect.center.y-cloudPoint.y);yield return null;
                for(int i=0;i<6;i++){view.Pan(-12,0);yield return null;yield return VolumeFrame(doc,"volume-visible-cloud-motion-"+i);}
                view.Pan(100000,0);yield return null;yield return VolumeFrame(doc,"volume-extreme-clamp");
                view.Fit();yield return null;
                var position=view.LocalToWorld(view.SystemPosition(star.SystemId));var picked=view.panel.Pick(position);
                Assert.That(picked,Is.Not.Null);int pointerId;
                using(var down=PointerDownEvent.GetPooled(new Event{type=EventType.MouseDown,button=0,mousePosition=position}))
                {pointerId=down.pointerId;down.target=picked;picked!.SendEvent(down);}
                var capturer=view.panel.GetCapturingElement(pointerId) as VisualElement;
                Assert.That(capturer,Is.SameAs(view));Pointer(capturer!,EventType.MouseUp,position);yield return null;
                Assert.That(typeof(ReviewEntry).GetField("galaxySelected",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry),Is.EqualTo(star.NodeId));
                Assert.That(typeof(ReviewEntry).GetField("galaxySelectionKind",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry),Is.EqualTo(GeneratedGalaxySelectionKind.System));
                CollectionAssert.AreEqual(steps,plan.Steps);Assert.That(plan.TotalCost,Is.EqualTo(cost));
                Assert.That(workspace.Document!.ContentHash,Is.EqualTo(hash));Assert.That(workspace.ExportBytes(),Is.EqualTo(bytes));
                view.ToggleFeatures();Assert.That(view.BackgroundRenderer!.VisibleCloudCount,Is.EqualTo(0));view.ToggleFeatures();
                view.ToggleBackground();yield return null;
                Assert.That(view.BackgroundRenderer,Is.Null);Assert.That(GeneratedGalaxyVolumeBackground.LiveInstances,Is.EqualTo(originalInstances));
                yield return VolumeFrame(doc,"volume-flat-fallback");
                view.ToggleBackground();yield return null;Assert.That(view.BackgroundRenderer,Is.Not.Null);
                var parent=view.parent;int index=parent.IndexOf(view);view.RemoveFromHierarchy();yield return null;
                Assert.That(GeneratedGalaxyVolumeBackground.LiveInstances,Is.EqualTo(originalInstances));parent.Insert(index,view);yield return null;yield return null;
                Assert.That(view.BackgroundRenderer,Is.Not.Null);
                root.Q<DropdownField>("galaxy-slot").value="2";Click(root.Q<Button>("galaxy-open"));yield return WaitForGalaxy(entry);
                view=root.Q<GeneratedGalaxyMapView>();Assert.That(workspace.Size,Is.EqualTo(1000));Assert.That(view.BackgroundRenderer,Is.Not.Null);
                Assert.That(GeneratedGalaxyVolumeBackground.LiveInstances,Is.EqualTo(originalInstances+1));
                yield return Capture(doc,"volume-1000-overview"); // three sizes
                yield return null;yield return null;yield return null;view.ZoomBy(3);yield return null;
                yield return VolumeFrame(doc,"volume-1000-regional"); // twentieth normal frame
                Click(root.Q<Button>("galaxy-back"));yield return null;yield return null;
                Assert.That(GeneratedGalaxyVolumeBackground.LiveInstances,Is.EqualTo(originalInstances));
            }
            yield return new ExitPlayMode();
        }
        private static IEnumerator VolumeFrame(UIDocument doc,string name)
        {
            yield return null;yield return null;
            var view=doc.rootVisualElement.Q<GeneratedGalaxyMapView>();var renderer=view.BackgroundRenderer;
            var directory=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/scaled-galaxy/engine-rendered"));Directory.CreateDirectory(directory);
            var prior=RenderTexture.active;Texture2D? pixels=null;
            try
            {
                var rt=doc.panelSettings.targetTexture;Assert.That(rt,Is.Not.Null);
                RenderTexture.active=rt;pixels=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
                pixels.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(directory,name+".png"),pixels.EncodeToPNG());
                FrameTimingManager.CaptureFrameTimings();var timing=new FrameTiming[1];uint timingCount=FrameTimingManager.GetLatestTimings(1,timing);
                File.WriteAllText(Path.Combine(directory,name+".json"),JsonConvert.SerializeObject(new{
                    view.Zoom,view.BackgroundStatus,view.BackgroundDepth,
                    renderer?.RenderCount,renderer?.VisibleCloudCount,renderer?.OmittedCloudCount,
                    renderer?.LastSubmissionMilliseconds,CPUOnly="Submission time is NOT GPU timing",timingCount,
                    textureFormat=renderer?.Texture?.format.ToString(),
                    gpuWholeFrameMilliseconds=timingCount==0?0:timing[0].gpuFrameTime,
                    timingScope="Whole-engine timing if nonzero; zero/unavailable is not a GPU performance pass",
                    SystemInfo.graphicsDeviceName,SystemInfo.graphicsDeviceType},Formatting.Indented));
            }
            finally{RenderTexture.active=prior;if(pixels!=null)UnityEngine.Object.Destroy(pixels);}
        }
    }
}
