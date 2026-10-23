#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using Newtonsoft.Json.Linq;
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
        private static IEnumerator WaitForGalaxy(ReviewEntry entry)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();int frames=0;
            while((bool)typeof(ReviewEntry).GetField("galaxyBusy",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry)!)
            {Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(90),"bounded asynchronous map operation");frames++;yield return null;}
            if(frames>0)
            {
                var output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/scaled-galaxy/engine-rendered"));Directory.CreateDirectory(output);
                File.AppendAllText(Path.Combine(output,"operation-timing.txt"),"ElapsedMs="+timer.Elapsed.TotalMilliseconds+" responsiveFrames="+frames+Environment.NewLine);
            }
            yield return null;yield return null;
        }
        [UnityTest] public IEnumerator OrderedWaypointDraftsNaturalMapsAndCompanionsUseActualControls()
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-"+Guid.NewGuid().ToString("N"));
            var codec=new ReviewNewtonsoftCodec();var scenario=codec.DecodeScenario(Resources.Load<TextAsset>(PlayableFleetOptions.ResourceRoot+"scenario").bytes);
            var workspace=new GeneratedGalaxyWorkspace(scenario.Definitions.Civilizations!,new GeneratedGalaxyMapStore(Path.Combine(directory,"maps"))){NaturalGeneration=true};
            typeof(ReviewEntry).GetField("galaxy",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,workspace);
            typeof(ReviewEntry).GetField("galaxyDraftStore",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,new GeneratedGalaxyRouteDraftStore(Path.Combine(directory,"drafts")));
            GeneratedGalaxyRouteDraft Draft()=>(GeneratedGalaxyRouteDraft)typeof(ReviewEntry).GetField("galaxyDraft",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry)!;
            GalaxyReviewRoutePlanSnapshot Plan()=>(GalaxyReviewRoutePlanSnapshot)typeof(ReviewEntry).GetField("galaxyPlan",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry)!;
            Click(doc.rootVisualElement.Q<Button>("generated-galaxy-entry"));yield return null;var root=doc.rootVisualElement;
            using(var inputSurface=new GalaxyInputSurface(doc))
            {
            yield return null;yield return null;
            foreach(var size in new[]{250,1000})
            {
                root.Q<TextField>("galaxy-seed").value="42";root.Q<DropdownField>("galaxy-size").value=size.ToString();
                Click(root.Q<Button>("galaxy-generate"));yield return WaitForGalaxy(entry);
                if(root.Q("review-one-use-confirmation")!=null)Confirm(doc);yield return WaitForGalaxy(entry);
                var map=workspace.Document!;Assert.That(map.FormatVersion,Is.EqualTo(4));
                Assert.That(map.ContentHash,Is.EqualTo(size==250?"4ea9b0d4581edb60460413e9fee0203257bd47e5b6cb43fc044ee5b2cda01976":"265f4247468e493e0cab64edd7c28970865ce955efe950880742313a8d857ac4"));
                var original=workspace.ExportBytes();yield return Capture(doc,"natural-"+size+"-overview");
                var a=map.Systems[0];var b=map.Systems[1];
                IEnumerator Target(GeneratedGalaxySystemDto system,bool shift)
                {
                    var star=root.Q<Button>("galaxy-star-"+system.SystemId);
                    yield return RightClick(root.Q<GeneratedGalaxyMapView>("generated-galaxy-map"),star.worldBound.center,shift);
                    Assert.That(Draft().Targets[Draft().Targets.Count-1].Kind,Is.EqualTo(GeneratedGalaxyDraftAnchorKind.StarLocation));
                }
                Click(root.Q<Button>("galaxy-star-"+a.SystemId));yield return null;
                Assert.That(root.Q<Button>("galaxy-route-target").enabledInHierarchy,Is.False);
                Click(root.Q<Button>("galaxy-route-start"));yield return null;
                Assert.That(root.Q<Button>("galaxy-route-target").enabledInHierarchy,Is.True,"explicit start must enable existing destination control immediately");
                yield return Target(b,false);
                var view=root.Q<GeneratedGalaxyMapView>("generated-galaxy-map");
                var background=map.Regions.Where(r=>r.SystemId==null).First(r=>{
                    var local=view.Position(r.NodeId);var world=view.LocalToWorld(local);
                    return view.contentRect.Contains(local)&&view.NearestStar(local)==null&&ReferenceEquals(view.panel.Pick(world),view);});
                yield return RightClick(view,view.LocalToWorld(view.Position(background.NodeId)),true);
                Assert.That(Draft().Targets.Count,Is.EqualTo(2));Assert.That(Draft().Targets[1].Kind,Is.EqualTo(GeneratedGalaxyDraftAnchorKind.RegionTransit));
                Assert.That(Draft().Targets[1].NodeId,Is.EqualTo(background.NodeId));
                yield return Target(b,false);yield return Target(b,true);yield return Target(a,true);
                Assert.That(Draft().Targets.Count,Is.EqualTo(3));Assert.That(Plan().Success,Is.True);
                Assert.That(Plan().Legs.Count,Is.EqualTo(3));Assert.That(Plan().Legs[1].Route.Steps.Count,Is.Zero);
                Assert.That(Plan().Legs[0].Target.NodeId,Is.EqualTo(b.NodeId));Assert.That(Plan().Legs[2].OriginNodeId,Is.EqualTo(b.NodeId));
                yield return Capture(doc,"natural-"+size+"-backtrack-zero-leg",plan:Plan());
                if(size==250)
                {
                    Click(root.Q<Button>("galaxy-route-save"));Click(root.Q<Button>("galaxy-route-clear"));Assert.That(Draft().Targets.Count,Is.Zero);
                    Click(root.Q<Button>("galaxy-route-restore"));yield return null;Assert.That(Draft().Targets.Count,Is.EqualTo(3));
                    yield return Capture(doc,"natural-250-restored-companion",plan:Plan());
                    for(int i=3;i<GalaxyReviewRoutePolicy.MaximumTargets;i++)yield return Target(a,true);
                    Assert.That(Draft().Targets.Count,Is.EqualTo(64));yield return Target(a,true);Assert.That(Draft().Targets.Count,Is.EqualTo(64));
                    var targetList=root.Q<ScrollView>("galaxy-route-target-list");Assert.That(targetList,Is.Not.Null);yield return null;
                    var costPosition=root.Q<Label>("galaxy-route-cost").worldBound;
                    var wheelPoint=targetList.contentViewport.worldBound.center;var picked=targetList.panel.Pick(wheelPoint);Assert.That(picked,Is.Not.Null);
                    using(var e=WheelEvent.GetPooled(new Event{type=EventType.ScrollWheel,mousePosition=wheelPoint,delta=new Vector2(0,80)}))
                    {e.target=picked;picked.SendEvent(e);}
                    yield return null;yield return null;
                    Assert.That(targetList.scrollOffset.y,Is.GreaterThan(0),"target list receives its own scroll event");
                    Assert.That(root.Q<Label>("galaxy-route-cost").worldBound,Is.EqualTo(costPosition),"target scrolling cannot move total/actions");
                    yield return Capture(doc,"natural-250-64-pins-fixed-controls");
                    Click(root.Q<Button>("galaxy-route-remove-last"));Assert.That(Draft().Targets.Count,Is.EqualTo(63));
                    yield return Target(b,false);Assert.That(Draft().Targets.Count,Is.EqualTo(1));
                }
                else
                {
                    Click(root.Q<Button>("galaxy-zoom-in"));Click(root.Q<Button>("galaxy-pan-left"));
                    yield return Capture(doc,"natural-1000-waypoints-pan-zoom",plan:Plan());
                    Click(root.Q<Button>("galaxy-fit"));
                }
                CollectionAssert.AreEqual(original,workspace.ExportBytes(),"draft/input/camera never change generated geography");
            }
            // Actual f8cost22 fixed topology/query. Raw pictures are explicitly diagnostic baselines,
            // using retained midpoint vertices; they are never normal-game certified output.
            var repo=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../.."));
            new GeneratedGalaxyMapStore(Path.Combine(directory,"maps")).Write(1,File.ReadAllBytes(Path.Combine(repo,"docs/samples/scaled-galaxy-v4/seed-42-systems-250.scaled.json")),false);
            Click(root.Q<Button>("galaxy-open"));if(root.Q("review-one-use-confirmation")!=null)Confirm(doc);yield return WaitForGalaxy(entry);
            var fixture=JObject.Parse(File.ReadAllText(Path.Combine(repo,"docs/samples/galaxy-route-intent-v1/examples.json")));
            var query=fixture["Comparison"]!["Query"]!.ToObject<GalaxyReviewRouteQueryDto>()!;var expected=workspace.FindRoutePlan(query);
            var currentMap=workspace.Document!;var originSystem=currentMap.Systems.Single(s=>s.NodeId==query.OriginNodeId);
            Click(root.Q<Button>("galaxy-star-"+originSystem.SystemId));Click(root.Q<Button>("galaxy-route-start"));yield return null;
            var routeView=root.Q<GeneratedGalaxyMapView>("generated-galaxy-map");var destination=query.Targets.Single();
            yield return RightClick(routeView,routeView.LocalToWorld(routeView.Position(destination.NodeId)),false);yield return null;
            CollectionAssert.AreEqual(expected.Steps,Plan().Steps);Assert.That(Plan().TotalCost,Is.EqualTo(22));
            var beforeFixture=workspace.ExportBytes();
            foreach(var camera in new[]{"fit","zoom"})
            {
                var field=typeof(GeneratedGalaxyMapView).GetField("planPaths",BindingFlags.Instance|BindingFlags.NonPublic)!;
                var certified=field.GetValue(routeView);var raw=routeView.CertifiedRouteGeometry!.Legs.Select(l=>(System.Collections.Generic.IReadOnlyList<GeneratedGalaxyDisplayPoint>)l.RawMidpoints).ToArray();
                try{field.SetValue(routeView,raw);routeView.MarkDirtyRepaint();yield return Capture(doc,"f8-"+camera+"-raw-midpoint-diagnostic");}
                finally{field.SetValue(routeView,certified);routeView.MarkDirtyRepaint();}
                yield return Capture(doc,"f8-"+camera+"-certified",plan:Plan());
            }
            CollectionAssert.AreEqual(beforeFixture,workspace.ExportBytes());
            }
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator ExactEightCrossingPortalRouteAndScaledMapsUseActualControls()
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-"+Guid.NewGuid().ToString("N"));
            var codec=new ReviewNewtonsoftCodec();var scenario=codec.DecodeScenario(Resources.Load<TextAsset>(PlayableFleetOptions.ResourceRoot+"scenario").bytes);
            var workspace=new GeneratedGalaxyWorkspace(scenario.Definitions.Civilizations!,new GeneratedGalaxyMapStore(directory));
            typeof(ReviewEntry).GetField("galaxy",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,workspace);
            Click(doc.rootVisualElement.Q<Button>("generated-galaxy-entry"));yield return null;var root=doc.rootVisualElement;
            root.Q<TextField>("galaxy-seed").value="42";root.Q<DropdownField>("galaxy-size").value="50";
            Click(root.Q<Button>("galaxy-generate"));yield return WaitForGalaxy(entry);
            var ids=new[]{39,52,26,25,5,13,84,21,44}.Select(i=>"nav.generated."+i.ToString("000")).ToArray();
            var map=workspace.Document!;var original=workspace.ExportBytes();var route=workspace.FindRoute(ids[0],ids[ids.Length-1]);
            CollectionAssert.AreEqual(ids.Skip(1),route.Steps);Assert.That(route.TotalCost,Is.EqualTo(8));
            Click(root.Q<Button>("galaxy-star-"+map.Systems.Single(s=>s.NodeId==ids[0]).SystemId));Click(root.Q<Button>("galaxy-route-start"));
            Click(root.Q<Button>("galaxy-star-"+map.Systems.Single(s=>s.NodeId==ids[ids.Length-1]).SystemId));Click(root.Q<Button>("galaxy-route-target"));
            yield return Capture(doc,"exact-eight-crossing-portals-fit",ids[0],route);
            Click(root.Q<Button>("galaxy-zoom-in"));Click(root.Q<Button>("galaxy-pan-left"));yield return null;
            yield return Capture(doc,"exact-eight-crossing-portals-pan-zoom",ids[0],route);
            Click(root.Q<Button>("galaxy-fit"));yield return null;
            CollectionAssert.AreEqual(original,workspace.ExportBytes());Assert.That(workspace.FindRoute(ids[0],ids[ids.Length-1]).TotalCost,Is.EqualTo(8));
            yield return Capture(doc,"exact-eight-crossing-portals-fit-restored",ids[0],route);
            foreach(var size in new[]{250,1000})
            {
                root.Q<DropdownField>("galaxy-size").value=size.ToString();Click(root.Q<Button>("galaxy-generate"));yield return WaitForGalaxy(entry);
                if(root.Q("review-one-use-confirmation")!=null)Confirm(doc);yield return WaitForGalaxy(entry);
                map=workspace.Document!;Assert.That(map.FormatVersion,Is.EqualTo(3));Assert.That(map.Systems.Count,Is.EqualTo(size));
                Assert.That(map.ContentHash,Is.EqualTo(size==250?"006ab67d08f1cd3c3d9e73da2fa335fb5e4ba1cee1acd824d63dbd44135c1937":"b7a10c946954cfdf51a28e90bccda98ee8a1d4dcbd6e35c23c811fd94878a834"));
                Assert.That(root.Q<Label>("galaxy-area-summary").text,Does.Contain("Quiet empty "+map.ScaleSummary!.QuietNodes.Count));
                var start=map.HomeCandidates[0];var end=map.HomeCandidates[1];route=workspace.FindRoute(start.NodeId,end.NodeId);
                Click(root.Q<Button>("galaxy-star-"+start.SystemId));Click(root.Q<Button>("galaxy-route-start"));
                Click(root.Q<Button>("galaxy-star-"+end.SystemId));Click(root.Q<Button>("galaxy-route-target"));
                yield return Capture(doc,"scaled-"+size+"-route",start.NodeId,route);
                var before=workspace.ExportBytes();Click(root.Q<Button>("galaxy-features"));Click(root.Q<Button>("galaxy-influence-areas"));
                CollectionAssert.AreEqual(before,workspace.ExportBytes());yield return Capture(doc,"scaled-"+size+"-influence");
                Click(root.Q<Button>("galaxy-features"));
                var blackHole=map.Features.First(f=>f.Kind==GalaxyEnvironmentFeatureKind.BlackHole);
                Click(root.Q<Button>("galaxy-feature-"+blackHole.FeatureId));yield return null;
                Assert.That(root.Q<Label>("galaxy-feature-identity").text,Is.EqualTo(blackHole.FeatureId));
                yield return Capture(doc,"scaled-"+size+"-feature-inspector");
                Click(root.Q<Button>("galaxy-export"));if(root.Q("review-one-use-confirmation")!=null)Confirm(doc);yield return WaitForGalaxy(entry);
                Click(root.Q<Button>("galaxy-open"));yield return WaitForGalaxy(entry);CollectionAssert.AreEqual(before,workspace.ExportBytes());
            }
            yield return new ExitPlayMode();
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
                Click(root.Q<Button>("galaxy-generate"));yield return WaitForGalaxy(entry);
                if(root.Q("review-one-use-confirmation")!=null)Confirm(doc);yield return WaitForGalaxy(entry);
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
                Assert.That(root.Q<Label>("galaxy-route-cost").text,Does.StartWith("Total cost "+expected.TotalCost+" ·"));
                Assert.That(root.Q<Button>("galaxy-route-row-1").text,Does.Contain(destination.DisplayName));
                yield return Capture(doc,"seed-"+item.Item1+"-empty-route",empty.NodeId,expected);

                Click(root.Q<Button>("galaxy-candidate-"+map.HomeCandidates[0].CivilizationDefinitionId));yield return null;
                Assert.That(root.Q<Label>("galaxy-selection-kind").text,Is.EqualTo("STARTING CANDIDATE"));
                Assert.That(root.Q<Label>("galaxy-selection-identity").text,Does.Contain(map.HomeCandidates[0].NodeId));
                yield return Capture(doc,"seed-"+item.Item1+"-candidate");
                var candidate=map.HomeCandidates[0];
                Click(root.Q<Button>("galaxy-star-"+candidate.SystemId));yield return null;
                Assert.That(root.Q<Label>("galaxy-selection-kind").text,Is.EqualTo("STAR SYSTEM"));
                Click(root.Q<Button>("galaxy-inspect-candidate-"+candidate.CivilizationDefinitionId));yield return null;
                Assert.That(root.Q<Label>("galaxy-selection-kind").text,Is.EqualTo("STARTING CANDIDATE"),"candidate remains reachable from its star inspector");
                Assert.That(entry.Presentation.Model.Current.Frame,Is.SameAs(campaignBefore));
            }
            var current=workspace.Document!;
            Click(doc.rootVisualElement.Q<Button>("galaxy-generate"));yield return WaitForGalaxy(entry);Confirm(doc,false);Assert.That(workspace.Document,Is.SameAs(current));
            Click(doc.rootVisualElement.Q<Button>("galaxy-export"));Assert.That(workspace.Unexported,Is.False);
            doc.rootVisualElement.Q<TextField>("galaxy-seed").value="77";Click(doc.rootVisualElement.Q<Button>("galaxy-generate"));yield return WaitForGalaxy(entry);yield return null;
            Assert.That(workspace.Document!.ContentHash,Is.Not.EqualTo(current.ContentHash));
            Click(doc.rootVisualElement.Q<Button>("galaxy-open"));yield return WaitForGalaxy(entry);Confirm(doc);yield return WaitForGalaxy(entry);yield return null;
            Assert.That(workspace.Document!.ContentHash,Is.EqualTo(current.ContentHash));
            Assert.That(doc.rootVisualElement.Q<TextField>("galaxy-seed").value,Is.EqualTo(current.Settings.Seed));
            var restored=workspace.Document;doc.rootVisualElement.Q<DropdownField>("galaxy-slot").value="3";
            Click(doc.rootVisualElement.Q<Button>("galaxy-open"));yield return WaitForGalaxy(entry);Assert.That(workspace.Document,Is.SameAs(restored));
            Assert.That(doc.rootVisualElement.Q<Label>("galaxy-status").text,Does.Contain("empty"));
            doc.rootVisualElement.Q<TextField>("galaxy-seed").value="-1";Click(doc.rootVisualElement.Q<Button>("galaxy-generate"));yield return WaitForGalaxy(entry);
            Assert.That(workspace.Document,Is.SameAs(restored));
            yield return Capture(doc,"invalid-input-retains-map");
            Click(doc.rootVisualElement.Q<Button>("galaxy-back"));yield return null;
            Assert.That(doc.rootVisualElement.Q<Button>("playable-fleet-start"),Is.Not.Null);
            Assert.That(entry.Presentation.Model.Current.Frame,Is.SameAs(campaignBefore));
            Click(doc.rootVisualElement.Q<Button>("generated-galaxy-entry"));yield return null;
            Assert.That(workspace.Document,Is.SameAs(restored));
            yield return new ExitPlayMode();
        }
        private static void Key(VisualElement target,KeyCode key,bool up=false)
        {
            var source=new Event{type=up?EventType.KeyUp:EventType.KeyDown,keyCode=key};
            if(up){using(var e=KeyUpEvent.GetPooled(source)){e.target=target;target.SendEvent(e);}}
            else{using(var e=KeyDownEvent.GetPooled(source)){e.target=target;target.SendEvent(e);}}
        }
        private static void Pointer(VisualElement target,EventType type,Vector2 position)
        {
            var source=new Event{type=type,button=0,mousePosition=position};
            if(type==EventType.MouseDown){using(var e=PointerDownEvent.GetPooled(source)){e.target=target;target.SendEvent(e);}}
            else if(type==EventType.MouseUp){using(var e=PointerUpEvent.GetPooled(source)){e.target=target;target.SendEvent(e);}}
            else{using(var e=PointerMoveEvent.GetPooled(source)){e.target=target;target.SendEvent(e);}}
        }
        private static void Wheel(VisualElement target,Vector2 position)
        {using(var e=WheelEvent.GetPooled(new Event{type=EventType.ScrollWheel,delta=new Vector2(0,-3),mousePosition=position})){e.target=target;target.SendEvent(e);}}
        // Batch receiving has no dependable desktop input surface. Keep one live panel render target
        // for real picking; screenshot captures temporarily replace it and restore it exactly.
        private sealed class GalaxyInputSurface : IDisposable
        {
            private readonly UIDocument doc;private readonly RenderTexture? priorTarget,priorActive;
            private readonly StyleLength priorWidth,priorHeight;private readonly RenderTexture target;
            public GalaxyInputSurface(UIDocument document)
            {
                doc=document;priorTarget=doc.panelSettings.targetTexture;priorActive=RenderTexture.active;
                priorWidth=doc.rootVisualElement.style.width;priorHeight=doc.rootVisualElement.style.height;
                target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);target.Create();
                doc.panelSettings.targetTexture=target;doc.rootVisualElement.style.width=1280;doc.rootVisualElement.style.height=720;
            }
            public void Dispose()
            {
                doc.panelSettings.targetTexture=priorTarget;doc.rootVisualElement.style.width=priorWidth;doc.rootVisualElement.style.height=priorHeight;
                RenderTexture.active=priorActive;target.Release();UnityEngine.Object.Destroy(target);
            }
        }
        private static IEnumerator RightClick(GeneratedGalaxyMapView view,Vector2 point,bool shift)
        {
            var panel=view.panel;Assert.That(panel,Is.Not.Null,"attached input panel");
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();
            object? Field(string name)=>typeof(ReviewEntry).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry);
            var draft=(GeneratedGalaxyRouteDraft)Field("galaxyDraft")!;var priorTargets=draft.Targets.ToArray();
            var priorPlan=Field("galaxyPlan");var priorDocument=typeof(GeneratedGalaxyMapView).GetField("document",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(view);
            var local=view.WorldToLocal(point);var node=view.NearestStar(local);
            var kind=node==null?GeneratedGalaxyDraftAnchorKind.RegionTransit:GeneratedGalaxyDraftAnchorKind.StarLocation;
            if(node==null)node=((GeneratedGalaxyMapData)priorDocument!).Regions.Single(r=>IsProjectedCentre(local,view,r)).NodeId;
            var down=panel.Pick(point);Assert.That(down,Is.Not.Null,"actual panel down target at "+point+" map "+view.worldBound);
            int pointerId;
            using(var e=PointerDownEvent.GetPooled(new Event{type=EventType.MouseDown,button=1,mousePosition=point,modifiers=shift?EventModifiers.Shift:EventModifiers.None}))
            {pointerId=e.pointerId;e.target=down;down.SendEvent(e);}
            CollectionAssert.AreEqual(priorTargets,draft.Targets,"Down must not edit the draft");Assert.That(Field("galaxyPlan"),Is.SameAs(priorPlan));
            Assert.That(typeof(GeneratedGalaxyMapView).GetField("document",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(view),Is.SameAs(priorDocument));
            var capture=panel.GetCapturingElement(pointerId);VisualElement up;
            if(capture!=null)
            {
                var captured=capture as VisualElement;Assert.That(captured,Is.Not.Null,"capturer is a visual element");up=captured!;
                Assert.That(up.panel,Is.SameAs(panel),"capturer stays on input panel");
                Assert.That(up,Is.SameAs(view),"star arbitration capture belongs to the map");
            }
            else{up=panel.Pick(point);Assert.That(up,Is.Not.Null,"fresh uncaptured panel up target at "+point+" map "+view.worldBound);}
            TestContext.WriteLine("RightClick point="+point+" bounds="+view.worldBound+" pointer="+pointerId+" down="+down.name+" capture="+(capture==null?"none":up.name)+" up="+up.name+" shift="+shift);
            using(var e=PointerUpEvent.GetPooled(new Event{type=EventType.MouseUp,button=1,mousePosition=point,modifiers=shift?EventModifiers.Shift:EventModifiers.None}))
            {Assert.That(e.pointerId,Is.EqualTo(pointerId));e.target=up;up.SendEvent(e);}
            yield return null;
            Assert.That(panel.GetCapturingElement(pointerId),Is.Null,"Up releases map capture");
            var rejected=shift&&priorTargets.Length==GalaxyReviewRoutePolicy.MaximumTargets;
            Assert.That(draft.Targets.Count,Is.EqualTo(rejected?priorTargets.Length:shift?priorTargets.Length+1:1));
            if(rejected)CollectionAssert.AreEqual(priorTargets,draft.Targets,"limit preserves queue");
            else{Assert.That(draft.Targets.Last().NodeId,Is.EqualTo(node));Assert.That(draft.Targets.Last().Kind,Is.EqualTo(kind));}
        }
        private static bool IsProjectedCentre(Vector2 local,GeneratedGalaxyMapView view,GeneratedGalaxyRegionDto region)
        {
            // This helper is used only at the already selected empty-region centre.
            return Vector2.Distance(local,view.Position(region.NodeId))<.01f;
        }

        [UnityTest] public IEnumerator PointerKeyboardModalAndCampaignDraftRemainIsolated()
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            Click(doc.rootVisualElement.Q<Button>("playable-fleet-start"));yield return null;
            Click(doc.rootVisualElement.Q<Button>("review-nav-Research"));yield return null;
            Click(doc.rootVisualElement.Q<Button>("science-plus-Computing"));yield return null;
            var campaign=entry.Presentation!.Model;var accepted=campaign.Current.Frame;
            var draft=campaign.ResearchInputs()!;var weight=draft.Weight(ScienceDiscipline.Computing);
            Assert.That(campaign.HasLocalEdits,Is.True);
            Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-"+Guid.NewGuid().ToString("N"));
            var scenario=new ReviewNewtonsoftCodec().DecodeScenario(Resources.Load<TextAsset>(PlayableFleetOptions.ResourceRoot+"scenario").bytes);
            var workspace=new GeneratedGalaxyWorkspace(scenario.Definitions.Civilizations!,new GeneratedGalaxyMapStore(directory));
            typeof(ReviewEntry).GetField("galaxy",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,workspace);
            Click(doc.rootVisualElement.Q<Button>("generated-galaxy-entry"));yield return null;
            var root=doc.rootVisualElement;root.Q<TextField>("galaxy-seed").value="42";
            root.Q<TextField>("galaxy-seed").Focus();Key(root.Q<TextField>("galaxy-seed"),KeyCode.Return);Key(root.panel.visualTree,KeyCode.Return,true);
            Assert.That(workspace.Document,Is.Null,"Return in seed field cannot generate");
            Click(root.Q<Button>("galaxy-generate"));yield return WaitForGalaxy(entry);yield return null;yield return null;
            var document=workspace.Document!;var map=root.Q<GeneratedGalaxyMapView>("generated-galaxy-map");
            Assert.That(map.ShowGraphLinks,Is.False);Click(root.Q<Button>("galaxy-graph-links"));Assert.That(map.ShowGraphLinks,Is.True);
            Click(root.Q<Button>("galaxy-graph-links"));Assert.That(map.ShowGraphLinks,Is.False);
            var empty=document.Regions.First(r=>r.SystemId==null);var point=map.LocalToWorld(map.Position(empty.NodeId));
            Pointer(map,EventType.MouseDown,point);Pointer(map,EventType.MouseUp,point);yield return null;
            Assert.That(root.Q<Label>("galaxy-selection-identity").text,Is.EqualTo(empty.NodeId),"actual polygon hit-test selects empty region");
            var before=map.Position(empty.NodeId);
            Pointer(map,EventType.MouseDown,point);Pointer(map,EventType.MouseDrag,point+new Vector2(45,20));Pointer(map,EventType.MouseUp,point+new Vector2(45,20));yield return null;
            Assert.That(Vector2.Distance(before,map.Position(empty.NodeId)),Is.GreaterThan(20));
            Assert.That(root.Q<Label>("galaxy-selection-identity").text,Is.EqualTo(empty.NodeId),"drag must not select another region");
            Wheel(map,map.worldBound.center);yield return null;Assert.That(map.Zoom,Is.GreaterThan(1));
            Click(root.Q<Button>("galaxy-fit"));yield return null;Assert.That(map.Zoom,Is.EqualTo(1));
            Assert.That(Vector2.Distance(before,map.Position(empty.NodeId)),Is.LessThan(.1f),"Fit resets actual transformed position");
            var retained=workspace.Document;
            var generate=root.Q<Button>("galaxy-generate");generate.Focus();Key(generate,KeyCode.Return);
            var confirm=root.Q("review-one-use-confirmation").Query<Button>().ToList().Single(b=>b.text=="Confirm");
            var zoom=map.Zoom;Wheel(map,map.worldBound.center);Assert.That(map.Zoom,Is.EqualTo(zoom),"modal disables map input");
            confirm.Focus();Key(confirm,KeyCode.Return);Assert.That(workspace.Document,Is.SameAs(retained),"held Return cannot confirm replacement");
            Key(root.panel.visualTree,KeyCode.Return,true);Key(confirm,KeyCode.Escape);yield return null;
            Assert.That(root.Q("review-one-use-confirmation"),Is.Null);Assert.That(workspace.Document,Is.SameAs(retained));
            root.Q<TextField>("galaxy-seed").value="43";Click(root.Q<Button>("galaxy-generate"));yield return WaitForGalaxy(entry);Confirm(doc);yield return WaitForGalaxy(entry);yield return null;
            Assert.That(workspace.Document!.ContentHash,Is.Not.EqualTo(retained!.ContentHash));
            Assert.That(root.Q<Label>("galaxy-selection-identity"),Is.Null,"map-scoped selection clears");
            Assert.That(root.Q<Label>("galaxy-route-origin-label").text,Is.EqualTo("Choose a starting region."));
            var replaced=workspace.Document;
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=generate;generate.SendEvent(e);}
            Assert.That(workspace.Document,Is.SameAs(replaced));Assert.That(root.Q("review-one-use-confirmation"),Is.Null,"detached old control cannot open a new modal");
            yield return Capture(doc,"pointer-keyboard-isolation");
            Click(root.Q<Button>("galaxy-back"));yield return null;
            Click(root.Q<Button>("demo-resume-current"));yield return null;
            Assert.That(entry.Presentation.Model,Is.SameAs(campaign));Assert.That(campaign.Current.Frame,Is.SameAs(accepted));
            Assert.That(campaign.ResearchInputs(),Is.SameAs(draft));Assert.That(draft.Weight(ScienceDiscipline.Computing),Is.EqualTo(weight));
            Assert.That(campaign.HasLocalEdits,Is.True);Assert.That(root.Q<Button>("integrated-apply-science").enabledInHierarchy,Is.True);
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator EnvironmentFeaturesAndOldMapFormatsUseDisplayedControls()
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();var root=doc.rootVisualElement;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-"+Guid.NewGuid().ToString("N"));
            var codec=new ReviewNewtonsoftCodec();var scenario=codec.DecodeScenario(Resources.Load<TextAsset>(PlayableFleetOptions.ResourceRoot+"scenario").bytes);
            var workspace=new GeneratedGalaxyWorkspace(scenario.Definitions.Civilizations!,new GeneratedGalaxyMapStore(directory));
            typeof(ReviewEntry).GetField("galaxy",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,workspace);
            Click(root.Q<Button>("generated-galaxy-entry"));yield return null;
            root.Q<TextField>("galaxy-seed").value="42";Click(root.Q<Button>("galaxy-generate"));yield return WaitForGalaxy(entry);yield return null;yield return null;
            var map=workspace.Document!;var environment=map.Environment!;Assert.That(environment,Is.Not.Null);
            var bytes=workspace.ExportBytes();var view=root.Q<GeneratedGalaxyMapView>("generated-galaxy-map");
            Assert.That(view.Query<Button>().ToList().Count(b=>b.name.StartsWith("galaxy-feature-",StringComparison.Ordinal)),Is.EqualTo(5));
            foreach(var kind in new[]{GalaxyEnvironmentFeatureKind.BlackHole,GalaxyEnvironmentFeatureKind.Nebula})
            {
                var feature=environment.Features.First(f=>f.Kind==kind);
                Click(root.Q<Button>("galaxy-feature-"+feature.FeatureId));yield return null;
                Assert.That(root.Q<Label>("galaxy-feature-identity").text,Is.EqualTo(feature.FeatureId));
                yield return Capture(doc,"environment-"+kind);
            }
            Click(root.Q<Button>("galaxy-influence-areas"));Assert.That(view.ShowInfluenceAreas,Is.True);
            Click(root.Q<Button>("galaxy-density-causes"));
            var system=map.Systems[0];Click(root.Q<Button>("galaxy-star-"+system.SystemId));yield return null;
            var influence=environment.RegionInfluences.Single(i=>i.NodeId==system.NodeId);
            Assert.That(root.Q<Label>("galaxy-density-values").text,Does.Contain("sampled target "+influence.TargetDensity+" /"));
            foreach(var contribution in influence.Contributions)
                Assert.That(root.Q<Button>("galaxy-contribution-"+contribution.FeatureId).text,Does.EndWith("+"+contribution.Amount));
            Click(root.Q<Button>("galaxy-inspect-star-influence"));yield return null;
            Assert.That(root.Q<Label>("galaxy-feature-identity").text,Is.EqualTo(environment.Features.Single(f=>f.SystemId==system.SystemId).FeatureId));
            yield return Capture(doc,"environment-influence-density");
            Click(root.Q<Button>("galaxy-features"));Assert.That(view.ShowFeatures,Is.False);
            Assert.That(view.Query<Button>().ToList().Where(b=>b.name.StartsWith("galaxy-feature-",StringComparison.Ordinal)).All(b=>b.style.display.value==DisplayStyle.None),Is.True);
            Assert.That(workspace.Document,Is.SameAs(map));CollectionAssert.AreEqual(bytes,workspace.ExportBytes(),"display toggles cannot change persisted authority");
            yield return Capture(doc,"environment-features-hidden");
            Click(root.Q<Button>("galaxy-features"));Click(root.Q<Button>("galaxy-influence-areas"));
            Click(root.Q<Button>("galaxy-export"));Assert.That(workspace.Unexported,Is.False);
            var currentPath=Path.Combine(directory,"generated-map-1.json");var saved=File.ReadAllBytes(currentPath);
            var corrupted=codec.DecodeEnvironmentGalaxy(saved);corrupted.Features[0].Position.X++;
            File.WriteAllBytes(currentPath,codec.EncodeEnvironmentGalaxy(corrupted));
            Click(root.Q<Button>("galaxy-open"));yield return WaitForGalaxy(entry);Assert.That(workspace.Document,Is.SameAs(map));
            Assert.That(root.Q<Label>("galaxy-feature-identity"),Is.Not.Null,"failed Open preserves selected feature");
            File.WriteAllBytes(currentPath,saved);
            foreach(var version in new[]{"v1","v2"})
            {
                var source=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","docs/samples/generated-galaxy-"+version,"seed-42-systems-40.galaxy.json"));
                var original=File.ReadAllBytes(source);var expected=codec.DecodeGeneratedGalaxy(original);
                File.WriteAllBytes(Path.Combine(directory,"generated-map-2.json"),original);
                root.Q<DropdownField>("galaxy-slot").value="2";Click(root.Q<Button>("galaxy-open"));yield return WaitForGalaxy(entry);yield return null;yield return null;
                Assert.That(workspace.Document!.ContentHash,Is.EqualTo(expected.ContentHash));Assert.That(workspace.Document.Environment,Is.Null);
                Assert.That(root.Q<Button>("galaxy-features"),Is.Null);Assert.That(root.Q<Label>("galaxy-feature-identity"),Is.Null);
                CollectionAssert.AreEqual(original,File.ReadAllBytes(Path.Combine(directory,"generated-map-2.json")));
                yield return Capture(doc,"retained-format1-"+version);
            }
            root.Q<DropdownField>("galaxy-slot").value="1";Click(root.Q<Button>("galaxy-open"));yield return WaitForGalaxy(entry);yield return null;yield return null;
            Assert.That(workspace.Document!.ContentHash,Is.EqualTo(map.ContentHash));CollectionAssert.AreEqual(bytes,workspace.ExportBytes());
            yield return Capture(doc,"environment-reopened");
            yield return new ExitPlayMode();
        }
        private static IEnumerator Capture(UIDocument doc,string state,string? origin=null,StrategicRouteSnapshot? route=null,GalaxyReviewRoutePlanSnapshot? plan=null)
        {
            var directory=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/scaled-galaxy/engine-rendered"));
            Directory.CreateDirectory(directory);
            if(plan==null&&origin!=null&&route!=null)
            {
                var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();
                var current=typeof(ReviewEntry).GetField("galaxyPlan",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry) as GalaxyReviewRoutePlanSnapshot;
                if(current?.Success==true){Assert.That(current.TotalCost,Is.EqualTo(route.TotalCost),"legacy and current policy retain minimum primary cost");plan=current;}
            }
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
            {
                var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);target.Create();
                var prior=RenderTexture.active;var priorPanelTarget=doc.panelSettings.targetTexture;
                var priorWidth=doc.rootVisualElement.style.width;var priorHeight=doc.rootVisualElement.style.height;Texture2D? pixels=null;
                try
                {
                    doc.panelSettings.targetTexture=target;var root=doc.rootVisualElement;root.style.width=size.x;root.style.height=size.y;
                    yield return null;yield return null;yield return null;
                    if(state.StartsWith("f8-",StringComparison.Ordinal))
                    {
                        var fixedView=root.Q<GeneratedGalaxyMapView>("generated-galaxy-map");fixedView.Fit();
                        if(state.StartsWith("f8-zoom-",StringComparison.Ordinal)){fixedView.ZoomBy(1.3);fixedView.ZoomBy(1.3);fixedView.Pan(90,0);}
                        yield return null;yield return null;
                    }
                    RenderTexture.active=target;pixels=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);
                    pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                    File.WriteAllBytes(Path.Combine(directory,size.x+"x"+size.y+"-"+state+".png"),pixels.EncodeToPNG());
                    foreach(var name in new[]{"galaxy-seed","galaxy-size","galaxy-slot","galaxy-generate","galaxy-export","galaxy-open","galaxy-back","galaxy-fit","galaxy-graph-links"})
                    {
                        var control=root.Q(name);Assert.That(control,Is.Not.Null,name);
                        Assert.That(control.worldBound.xMin,Is.GreaterThanOrEqualTo(0),name);
                        Assert.That(control.worldBound.xMax,Is.LessThanOrEqualTo(size.x+1),name);
                        Assert.That(control.worldBound.yMin,Is.GreaterThanOrEqualTo(0),name);
                        Assert.That(control.worldBound.yMax,Is.LessThanOrEqualTo(size.y+1),name);
                    }
                    foreach(var name in new[]{"galaxy-features","galaxy-influence-areas","galaxy-density-causes"})
                    {
                        var control=root.Q(name);if(control==null)continue;
                        Assert.That(control.worldBound.xMin,Is.GreaterThanOrEqualTo(0),name);Assert.That(control.worldBound.xMax,Is.LessThanOrEqualTo(size.x+1),name);
                        Assert.That(control.worldBound.yMax,Is.LessThanOrEqualTo(size.y+1),name);
                    }
                    var map=root.Q("generated-galaxy-map");Assert.That(map.worldBound.height,Is.GreaterThan(250));
                    Assert.That(map.worldBound.width,Is.GreaterThan(600));
                    var inspector=root.Q<ScrollView>("galaxy-inspector");
                    Assert.That(inspector.contentViewport.worldBound.height,Is.GreaterThan(80),"general inspection remains locally scrollable beneath fixed route controls");
                    foreach(var name in new[]{"galaxy-route-cost","galaxy-route-remove-last","galaxy-route-clear","galaxy-route-save","galaxy-route-restore"})
                    {
                        var control=root.Q(name);if(control==null)continue;
                        Assert.That(control.worldBound.xMin,Is.GreaterThanOrEqualTo(0),name);Assert.That(control.worldBound.xMax,Is.LessThanOrEqualTo(size.x+1),name);
                        Assert.That(control.worldBound.yMin,Is.GreaterThanOrEqualTo(0),name);Assert.That(control.worldBound.yMax,Is.LessThanOrEqualTo(size.y+1),name);
                        if(control is Button)
                        {
                            var hit=control.panel.Pick(control.worldBound.center);
                            Assert.That(hit!=null&&(ReferenceEquals(hit,control)||control.Contains(hit)),Is.True,"fixed route action must remain hittable: "+name);
                        }
                    }
                    var routePanel=root.Q("galaxy-route-preview");
                    Assert.That(routePanel.worldBound.yMax,Is.LessThanOrEqualTo(inspector.worldBound.yMin),"route and general inspector must not overlap");
                    Assert.That(map.worldBound.xMax,Is.LessThanOrEqualTo(inspector.worldBound.xMin));
                    var featureView=root.Q<GeneratedGalaxyMapView>("generated-galaxy-map");
                    foreach(var feature in featureView.Query<Button>().ToList().Where(b=>b.name.StartsWith("galaxy-feature-",StringComparison.Ordinal)&&b.style.display.value!=DisplayStyle.None))
                    {
                        var expected=featureView.LocalToWorld(featureView.FeaturePosition(feature.name.Substring("galaxy-feature-".Length)));
                        Assert.That(Vector2.Distance(feature.worldBound.center,expected),Is.LessThan(2),feature.name);
                    }
                    if(state=="environment-influence-density"||state.StartsWith("scaled-",StringComparison.Ordinal)&&state.EndsWith("-influence",StringComparison.Ordinal))
                    {
                        var data=(GeneratedGalaxyMapData)typeof(GeneratedGalaxyMapView).GetField("document",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(featureView)!;
                        var project=typeof(GeneratedGalaxyMapView).GetMethod("Point",BindingFlags.NonPublic|BindingFlags.Instance)!;
                        Vector2 Project(int x,int y)=>featureView.LocalToWorld((Vector2)project.Invoke(featureView,new object[]{new GeneratedGalaxyPointDto{X=x,Y=y}})!);
                        var low=Project(0,0);var high=Project(data.Width,data.Height);int checkedPixels=0;
                        var controls=featureView.Children().Where(c=>c.resolvedStyle.display!=DisplayStyle.None).Select(c=>c.worldBound).ToArray();
                        var anchors=data.Features.Select(f=>featureView.LocalToWorld(featureView.FeaturePosition(f.FeatureId))).ToArray();
                        foreach(var fraction in Enumerable.Range(1,19).Select(i=>i/20f))
                        foreach(var sample in new[]{new Vector2(Mathf.Lerp(low.x,high.x,fraction),low.y-15),new Vector2(Mathf.Lerp(low.x,high.x,fraction),high.y+15),
                            new Vector2(low.x-15,Mathf.Lerp(low.y,high.y,fraction)),new Vector2(high.x+15,Mathf.Lerp(low.y,high.y,fraction))})
                        {
                            if(!featureView.worldBound.Contains(sample)||controls.Any(c=>c.Contains(sample))||anchors.Any(a=>Vector2.Distance(a,sample)<22))continue;
                            var color=pixels.GetPixel(Mathf.RoundToInt(sample.x),size.y-1-Mathf.RoundToInt(sample.y));
                            Assert.That(Mathf.Abs(color.r-8/255f)+Mathf.Abs(color.g-18/255f)+Mathf.Abs(color.b-30/255f),Is.LessThan(.025f),
                                "feature influence must not paint outside exported map extent at "+sample);
                            checkedPixels++;
                        }
                        Assert.That(checkedPixels,Is.GreaterThan(0),"actual exterior clipping samples");
                    }
                    if(origin!=null&&route!=null||plan?.Success==true)
                    {
                        var view=root.Q<GeneratedGalaxyMapView>("generated-galaxy-map");
                        var points=(plan==null?view.RoutePositions():view.RouteLegPositions().SelectMany(p=>p).ToArray()).Select(p=>view.LocalToWorld(p)).ToArray();
                        if(plan==null)Assert.That(points.Length,Is.EqualTo(route!.Steps.Count==0?1:2+route.Steps.Count),"raw diagnostic path count");
                        if(plan!=null)
                        {
                            var legs=view.RouteLegPositions();Assert.That(legs.Count,Is.EqualTo(plan.Legs.Count));
                            var certified=view.CertifiedRouteGeometry!;Assert.That(certified.Legs.Count,Is.EqualTo(plan.Legs.Count));
                            Assert.That(certified.HiddenCount,Is.Zero,"actual fixture needs certified lines");
                            Assert.That(certified.Operations,Is.LessThanOrEqualTo(GeneratedGalaxyCorridorPath.MaximumPlanOperations));
                            for(int leg=0;leg<legs.Count;leg++)
                            {
                                Assert.That(certified.Legs[leg].Certified,Is.True,certified.Legs[leg].Reason);
                                Assert.That(legs[leg].Count,Is.EqualTo(certified.Legs[leg].Points.Count),"render exactly certified vertices, not crossing count");
                                Assert.That(certified.Legs[leg].RawMidpoints.Count,Is.EqualTo(plan.Legs[leg].Route.Steps.Count==0?1:2+plan.Legs[leg].Route.Steps.Count),"retain one raw portal per graph crossing separately");
                                Assert.That(Vector2.Distance(legs[leg][0],view.Position(plan.Legs[leg].OriginNodeId)),Is.LessThan(.001f));
                                Assert.That(Vector2.Distance(legs[leg][legs[leg].Count-1],view.Position(plan.Legs[leg].Target.NodeId)),Is.LessThan(.001f));
                            }
                            File.AppendAllText(Path.Combine(directory,"route-certified-display.txt"),size+" "+state+" map="+plan.MapContentHash+
                                " cost="+plan.TotalCost+" work="+certified.Operations+" fallback="+certified.FallbackCount+" hidden="+certified.HiddenCount+
                                " legs="+string.Join(" | ",certified.Legs.Select(l=>l.Reason+":"+string.Join(";",l.Points.Select(p=>p.X1024+","+p.Y1024))))+Environment.NewLine);
                        }
                        Vector2[] CurrentPoints()=>(plan==null?view.RoutePositions():view.RouteLegPositions().SelectMany(p=>p).ToArray()).Select(p=>view.LocalToWorld(p)).ToArray();
                        // Normal screenshots and marker assertions retain the actual opaque star controls.
                        // A subpixel portal half-segment may be wholly covered by one of those glyphs.
                        // Probe the same Painter2D underlay with child visibility hidden, never changing
                        // display/layout, route geometry, viewport, costs or normal-game presentation.
                        var priorVisibility=view.Children().Select(c=>(control:c,visibility:c.style.visibility)).ToArray();
                        var priorBounds=view.worldBound;var priorZoom=view.Zoom;
                        var priorSteps=(plan?.Steps??route!.Steps).ToArray();var priorCost=plan?.TotalCost??route!.TotalCost;
                        Texture2D? routePixels=null;
                        try
                        {
                            foreach(var item in priorVisibility)item.control.style.visibility=Visibility.Hidden;
                            yield return null;yield return null;yield return null;
                            Assert.That(view.worldBound,Is.EqualTo(priorBounds));Assert.That(view.Zoom,Is.EqualTo(priorZoom));
                            CollectionAssert.AreEqual(points,CurrentPoints());
                            CollectionAssert.AreEqual(priorSteps,plan?.Steps??route!.Steps);Assert.That(plan?.TotalCost??route!.TotalCost,Is.EqualTo(priorCost));
                            RenderTexture.active=target;routePixels=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);
                            routePixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);routePixels.Apply();
                            File.AppendAllText(Path.Combine(directory,"route-underlay-probes.txt"),size+" "+state+
                                " diagnostic-only hidden-child underlay; unchanged points="+points.Length+" cost="+priorCost+Environment.NewLine);
                        var paintedLegs=plan==null?new[]{points}:view.RouteLegPositions().Select(leg=>leg.Select(p=>view.LocalToWorld(p)).ToArray()).ToArray();
                        foreach(var legPoints in paintedLegs)for(int i=1;i<legPoints.Length;i++)
                        {
                            if(legPoints[i]==legPoints[i-1])
                            {
                                File.AppendAllText(Path.Combine(directory,"route-underlay-probes.txt"),size+" "+state+" zero projected extent; no pixel-line claim; segment="+i+Environment.NewLine);
                                continue;
                            }
                            bool painted=false,visible=false;
                            foreach(var fraction in new[]{.25f,.5f,.75f})
                            {
                                var p=Vector2.Lerp(legPoints[i-1],legPoints[i],fraction);
                                if(!view.worldBound.Contains(p))continue;visible=true;
                                for(int dx=-3;dx<=3;dx++)for(int dy=-3;dy<=3;dy++)
                                {
                                    var color=routePixels.GetPixel(Mathf.Clamp(Mathf.RoundToInt(p.x)+dx,0,size.x-1),Mathf.Clamp(size.y-1-Mathf.RoundToInt(p.y)+dy,0,size.y-1));
                                    painted|=color.r>.8f&&color.g>.5f&&color.g<.8f&&color.b<.6f;
                                }
                            }
                            if(visible)Assert.That(painted,Is.True,"actual Painter2D underlay must follow visible corridor segment "+(i-1));
                        }
                        }
                        finally
                        {
                            foreach(var item in priorVisibility)item.control.style.visibility=item.visibility;
                            if(routePixels!=null)UnityEngine.Object.Destroy(routePixels);
                        }
                        yield return null;yield return null;yield return null;
                        Assert.That(view.worldBound,Is.EqualTo(priorBounds));Assert.That(view.Zoom,Is.EqualTo(priorZoom));
                        CollectionAssert.AreEqual(points,CurrentPoints());
                        CollectionAssert.AreEqual(priorSteps,plan?.Steps??route!.Steps);Assert.That(plan?.TotalCost??route!.TotalCost,Is.EqualTo(priorCost));
                        foreach(var button in root.Query<Button>().ToList().Where(b=>b.name.StartsWith("galaxy-star-",StringComparison.Ordinal)&&b.resolvedStyle.display!=DisplayStyle.None))
                        {
                            var systemId=button.name.Substring("galaxy-star-".Length);
                            // The actual star control position must match the declared generator system/region association.
                            var document=(GeneratedGalaxyMapData)typeof(GeneratedGalaxyMapView).GetField("document",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(view)!;
                            var node=document.Systems.Single(s=>s.SystemId==systemId).NodeId;
                            var expected=view.LocalToWorld(view.SystemPosition(systemId));var actual=button.worldBound.center;var delta=actual-expected;
                            var diagnostic=size+" "+state+" system="+systemId+" node="+node+" actual="+actual.ToString("F5")+" expected="+expected.ToString("F5")
                                +" delta="+delta.ToString("F5")+" mapLayout="+view.layout+" content="+view.contentRect
                                +" borders="+view.resolvedStyle.borderLeftWidth+","+view.resolvedStyle.borderTopWidth
                                +" padding="+view.resolvedStyle.paddingLeft+","+view.resolvedStyle.paddingTop+" button="+button.layout;
                            File.AppendAllText(Path.Combine(directory,"marker-alignment.txt"),diagnostic+Environment.NewLine);
                            Assert.That(delta.magnitude,Is.LessThan(2),diagnostic);
                        }
                        // Exercise real panel picking with a candidate bracket deliberately overlapping its star.
                        var candidateButton=view.Query<Button>().ToList().First(b=>b.name.StartsWith("galaxy-candidate-",StringComparison.Ordinal));
                        var starButton=view.Query<Button>().ToList().First(b=>b.name.StartsWith("galaxy-star-",StringComparison.Ordinal)&&b.resolvedStyle.display!=DisplayStyle.None&&view.worldBound.Contains(b.worldBound.center));
                        var oldLeft=candidateButton.style.left;var oldTop=candidateButton.style.top;var oldDisplay=candidateButton.style.display;
                        candidateButton.style.display=DisplayStyle.Flex;
                        candidateButton.style.left=starButton.style.left;candidateButton.style.top=starButton.style.top;
                        yield return null;
                        var pickedSystemId=starButton.name.Substring("galaxy-star-".Length);
                        var starPoint=view.LocalToWorld(view.SystemPosition(pickedSystemId));
                        var picked=view.panel.Pick(starPoint);Assert.That(picked,Is.Not.Null);
                        Pointer(picked,EventType.MouseDown,starPoint);Pointer(picked,EventType.MouseUp,starPoint);yield return null;
                        Assert.That(root.Q<Label>("galaxy-selection-identity").text,Does.Contain(pickedSystemId),"nearest actual star wins pointer arbitration despite overlapping controls");
                        candidateButton.style.left=oldLeft;candidateButton.style.top=oldTop;candidateButton.style.display=oldDisplay;
                        yield return null;
                    }
                }
                finally{doc.panelSettings.targetTexture=priorPanelTarget;doc.rootVisualElement.style.width=priorWidth;doc.rootVisualElement.style.height=priorHeight;RenderTexture.active=prior;target.Release();UnityEngine.Object.Destroy(target);if(pixels!=null)UnityEngine.Object.Destroy(pixels);}
                yield return null;yield return null;
            }
        }
    }
}
