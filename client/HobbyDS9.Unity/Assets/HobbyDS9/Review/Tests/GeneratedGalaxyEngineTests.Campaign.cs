#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    public sealed partial class GeneratedGalaxyEngineTests
    {
        private static ReviewUiModel CampaignModel(ReviewEntry entry)=>(ReviewUiModel)typeof(ReviewEntry).GetField("model",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry)!;
        private static void CampaignPage(UIDocument doc,ReviewWorkspacePage page)=>Click(doc.rootVisualElement.Q<Button>("review-nav-"+page));
        private static Button CampaignButton(UIDocument doc,string text)=>doc.rootVisualElement.Query<Button>().ToList().Single(b=>b.text==text);
        private static string CampaignOutput()
        {var path=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/scaled-galaxy/engine-rendered"));Directory.CreateDirectory(path);return path;}
        private static IEnumerator WaitCampaign(ReviewEntry entry,string phase)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();int frames=0;
            while((bool)typeof(ReviewEntry).GetField("generatedCampaignBusy",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry)!)
            {Assert.That(watch.Elapsed.TotalSeconds,Is.LessThan(90),phase);frames++;yield return null;}
            File.AppendAllText(Path.Combine(CampaignOutput(),"generated-operation-timing.txt"),phase+" elapsedMs="+watch.Elapsed.TotalMilliseconds+" yieldedFrames="+frames+" (not FPS)"+Environment.NewLine);
            yield return null;yield return null;
        }
        private static IEnumerator CampaignPointerRight(UIDocument doc,ReviewEntry entry,Button intended,int button=1)
        {
            Assert.That(intended,Is.Not.Null);var frame=CampaignModel(entry).Current.Frame;var point=intended.worldBound.center;
            var panel=doc.rootVisualElement.panel;var down=panel.Pick(point);
            Assert.That(down,Is.Not.Null,"actual panel hit at "+point);
            var map=doc.rootVisualElement.Q("playable-known-map");var known=CampaignModel(entry).GeneratedMap();var local=map.WorldToLocal(point);
            VisualElement? hit=down;while(hit!=null&&hit!=map&&!hit.name.StartsWith("map-",StringComparison.Ordinal))hit=hit.parent;
            bool contact=hit?.name.StartsWith("map-contact-",StringComparison.Ordinal)==true;
            Assert.That(hit?.name.StartsWith("map-ring-",StringComparison.Ordinal)==true||hit?.name.StartsWith("map-moving-fleet-",StringComparison.Ordinal)==true,Is.False,"actual fleet/ring target keeps priority; not a node test point");
            var anchors=known.Regions.Select(r=>{var star=known.Features.FirstOrDefault(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star&&f.NodeId==r.NodeId);var at=star?.Position??r.Center;
                return new GeneratedCampaignNodeAnchor(r.NodeId,(float)CampaignModel(entry).GeneratedViewport.X(at.X),(float)CampaignModel(entry).GeneratedViewport.Y(at.Y));});
            var expected=contact?frame.Travel!.KnownContacts.Single(c=>"map-contact-"+c.FleetId==hit!.name).LastSeenNodeId:GeneratedCampaignNodePicker.Resolve(anchors,local.x,local.y);
            Assert.That(expected,Is.Not.Null);
            int id;
            using(var e=PointerDownEvent.GetPooled(new Event{type=EventType.MouseDown,button=button,mousePosition=point}))
            {id=e.pointerId;e.target=down;down.SendEvent(e);}
            Assert.That(CampaignModel(entry).Current.Frame,Is.SameAs(frame),"pointer down never submits");
            var capture=panel.GetCapturingElement(id) as VisualElement;
            var up=capture??panel.Pick(point);Assert.That(up,Is.Not.Null);Assert.That(up.panel,Is.SameAs(panel));
            if(capture!=null)Assert.That(contact?capture==hit||hit!.Contains(capture):capture==map,Is.True,"actual priority target or generated map owns capture");
            using(var e=PointerUpEvent.GetPooled(new Event{type=EventType.MouseUp,button=button,mousePosition=point}))
            {Assert.That(e.pointerId,Is.EqualTo(id));e.target=up;up.SendEvent(e);}
            yield return null;
            if(button==1){Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Not.Null,"actual right-click review");Assert.That(entry.Workspace!.RoutePreview!.Destination,Is.EqualTo(expected));}
            else Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Null);
            Assert.That(entry.Workspace!.SelectedNode,Is.EqualTo(expected),"same projected nearest-node resolution for left/right");
            Assert.That(panel.GetCapturingElement(id),Is.Null,"pointer capture released");
            Assert.That(CampaignModel(entry).Current.Frame,Is.SameAs(frame),"review is not submission");
            TestContext.WriteLine("Campaign pointer "+intended.name+" point="+point+" down="+down.name+" up="+up.name+" capture="+(capture?.name??"none")+" resolved="+expected+" button="+button);
        }
        private static IEnumerator CampaignPriorityPointer(UIDocument doc,ReviewEntry entry,VisualElement intended,Vector2 point,int button)
        {
            var model=CampaignModel(entry);var frame=model.Current.Frame;var node=entry.Workspace!.SelectedNode;var fleet=entry.Workspace.SelectedFleet;
            var panel=doc.rootVisualElement.panel;var down=panel.Pick(point);var ancestor=down;
            while(ancestor!=null&&ancestor!=intended)ancestor=ancestor.parent;
            Assert.That(ancestor,Is.SameAs(intended),"actual priority target pick "+intended.name+" at "+point);
            int id;using(var e=PointerDownEvent.GetPooled(new Event{type=EventType.MouseDown,button=button,mousePosition=point}))
            {id=e.pointerId;e.target=down;down.SendEvent(e);}
            var capture=panel.GetCapturingElement(id) as VisualElement;var up=capture??panel.Pick(point);
            Assert.That(up,Is.Not.Null);Assert.That(up.panel,Is.SameAs(panel));
            if(capture!=null)Assert.That(capture==intended||intended.Contains(capture),Is.True,"priority widget retains capture");
            using(var e=PointerUpEvent.GetPooled(new Event{type=EventType.MouseUp,button=button,mousePosition=point}))
            {Assert.That(e.pointerId,Is.EqualTo(id));e.target=up;up.SendEvent(e);}yield return null;
            Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Null);
            if(button==0&&intended.name.StartsWith("map-ring-",StringComparison.Ordinal))
                Assert.That(entry.Workspace.SelectedFleet,Is.EqualTo(frame.OwnStrategicFleets.First(f=>f.LocationNodeId.Value==intended.name.Substring("map-ring-".Length)).FleetId.Value));
            else
            {
                Assert.That(entry.Workspace.SelectedNode,Is.EqualTo(node));Assert.That(entry.Workspace.SelectedFleet,Is.EqualTo(fleet));
                if(button==0)Assert.That(doc.rootVisualElement.Q<Label>("map-selection-notice").text,Does.Contain("Observed hostile contact"));
            }
            Assert.That(panel.GetCapturingElement(id),Is.Null);TestContext.WriteLine("Priority pointer "+intended.name+" button="+button+" point="+point+" actual="+down.name);
        }
        private static IEnumerator CampaignCapture(ReviewEntry entry,UIDocument doc,string state)
        {
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
            {
                var priorTarget=doc.panelSettings.targetTexture;var active=RenderTexture.active;
                var w=doc.rootVisualElement.style.width;var h=doc.rootVisualElement.style.height;
                var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);Assert.That(target.Create(),Is.True);Texture2D? pixels=null;
                try
                {
                    doc.panelSettings.targetTexture=target;doc.rootVisualElement.style.width=size.x;doc.rootVisualElement.style.height=size.y;
                    yield return null;yield return null;yield return null;
                    var fit=doc.rootVisualElement.Q<Button>("map-fit");if(fit!=null){Click(fit);yield return null;yield return null;}
                    Assert.That(doc.rootVisualElement.Q("generated-galaxy-review"),Is.Null,"this is an actual campaign, not map review");
                    var model=CampaignModel(entry);
                    var map=doc.rootVisualElement.Q("playable-known-map");
                    if(map!=null)
                    {
                        var observed=model.GeneratedMap();var nodes=observed.Regions.Select(r=>r.NodeId).ToHashSet(StringComparer.Ordinal);
                        var buttons=map.Query<Button>().ToList().Where(b=>b.name.StartsWith("map-star-",StringComparison.Ordinal)).ToArray();
                        CollectionAssert.AreEquivalent(nodes,buttons.Select(b=>b.name.Substring("map-star-".Length)));
                        Assert.That(map.Query<Button>().ToList().Any(b=>b.name.StartsWith("galaxy-candidate-",StringComparison.Ordinal)),Is.False);
                        foreach(var feature in observed.Features.Where(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star))
                        {
                            var marker=doc.rootVisualElement.Q<Button>("map-star-"+feature.NodeId);
                            var expected=map.LocalToWorld(new Vector2((float)model.GeneratedViewport.X(feature.Position.X),(float)model.GeneratedViewport.Y(feature.Position.Y)));
                            Assert.That(Vector2.Distance(expected,marker.worldBound.center),Is.LessThan(2),"actual known star anchor "+feature.FeatureId);
                            Assert.That(marker.worldBound.center.x,Is.InRange(map.worldBound.xMin,map.worldBound.xMax));
                            Assert.That(marker.worldBound.center.y,Is.InRange(map.worldBound.yMin,map.worldBound.yMax));
                        }
                    }
                    foreach(var id in new[]{"generated-campaign-seed","generated-campaign-size","generated-campaign-quiet","generated-campaign-start","map-fit","player-primary-action","player-save"})
                    {
                        var control=doc.rootVisualElement.Q(id);if(control==null||control.resolvedStyle.display==DisplayStyle.None)continue;
                        Assert.That(control.worldBound.xMin,Is.GreaterThanOrEqualTo(-1),id);Assert.That(control.worldBound.xMax,Is.LessThanOrEqualTo(size.x+1),id);
                        Assert.That(control.worldBound.yMin,Is.GreaterThanOrEqualTo(-1),id);Assert.That(control.worldBound.yMax,Is.LessThanOrEqualTo(size.y+1),id);
                    }
                    RenderTexture.active=target;pixels=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                    File.WriteAllBytes(Path.Combine(CampaignOutput(),size.x+"x"+size.y+"-campaign-"+state+".png"),pixels.EncodeToPNG());
                }
                finally{doc.panelSettings.targetTexture=priorTarget;doc.rootVisualElement.style.width=w;doc.rootVisualElement.style.height=h;RenderTexture.active=active;target.Release();UnityEngine.Object.Destroy(target);if(pixels!=null)UnityEngine.Object.Destroy(pixels);}
                yield return null;yield return null;
                var restoreFit=doc.rootVisualElement.Q<Button>("map-fit");if(restoreFit!=null){Click(restoreFit);yield return null;}
            }
        }
        [UnityTest] public IEnumerator GeneratedCampaignNewGamePaidFleetExplorationBattleAndContinueUseActualControls()
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,Path.Combine(directory,"campaign"));
            var before=CampaignModel(entry);
            using(var surface=new GalaxyInputSurface(doc))
            {
                yield return null;yield return null;
                CollectionAssert.AreEqual(new[]{"0.50×","0.75×","1.00×","1.25×","1.50×","1.75×","2.00×"},doc.rootVisualElement.Q<DropdownField>("generated-campaign-quiet").choices);
                yield return CampaignCapture(entry,doc,"settings");
                Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-1"));Confirm(doc);yield return WaitCampaign(entry,"absent-continue");
                Assert.That(CampaignModel(entry),Is.SameAs(before));
                doc.rootVisualElement.Q<TextField>("generated-campaign-seed").value="invalid";
                Click(doc.rootVisualElement.Q<Button>("generated-campaign-start"));yield return WaitCampaign(entry,"invalid-seed");Assert.That(CampaignModel(entry),Is.SameAs(before));
                doc.rootVisualElement.Q<TextField>("generated-campaign-seed").value="42";
                Click(doc.rootVisualElement.Q<Button>("generated-campaign-start"));Click(doc.rootVisualElement.Q<Button>("generated-galaxy-entry"));
                yield return WaitCampaign(entry,"stale-generation");Assert.That(CampaignModel(entry),Is.SameAs(before));
                Click(doc.rootVisualElement.Q<Button>("galaxy-back"));yield return null;
                Click(doc.rootVisualElement.Q<Button>("generated-campaign-start"));yield return WaitCampaign(entry,"new-game250");
                var model=CampaignModel(entry);Assert.That(model.GeneratedCampaign,Is.True);Assert.That(model,Is.Not.SameAs(before));
                Assert.That(model.GeneratedMap().Regions.Count,Is.InRange(2,20));var mapHash=model.GeneratedMap().MapContentHash;
                Assert.That(model.GeneratedMap().Features.All(f=>f.Kind!=GalaxyEnvironmentFeatureKind.Nebula),Is.True,"no undisclosed non-node nebulae in policy v1");
                yield return CampaignCapture(entry,doc,"known-map");
                Vector2 RingPoint(VisualElement ring)
                {
                    foreach(var delta in new[]{new Vector2(31,0),new Vector2(-31,0),new Vector2(0,31),new Vector2(0,-31)})
                    {var at=ring.worldBound.center+delta;var picked=ring.panel.Pick(at);for(var v=picked;v!=null;v=v.parent)if(v==ring)return at;}
                    Assert.Fail("No actual hollow-ring hit point");return default;
                }
                var ringNode=model.Current.Frame.OwnStrategicFleets.First().LocationNodeId.Value;
                var priorityRing=doc.rootVisualElement.Q<Button>("map-ring-"+ringNode);
                yield return CampaignPriorityPointer(doc,entry,priorityRing,RingPoint(priorityRing),1);
                yield return CampaignPriorityPointer(doc,entry,priorityRing,RingPoint(priorityRing),0);
                // Deliberately expose overlap at full-galaxy scale. Use the real panel
                // pick, not an intended-button event fallback or a test-only zoom fix.
                Click(doc.rootVisualElement.Q<Button>("map-fleet-"+model.Current.Frame.OwnStrategicFleets.First().FleetId.Value));
                Button CrowdedPoint()
                {
                    var map=doc.rootVisualElement.Q("playable-known-map");
                    var found=map.Query<Button>().ToList().Where(b=>b.name.StartsWith("map-star-",StringComparison.Ordinal)).FirstOrDefault(b=>{
                        var actual=map.panel.Pick(b.worldBound.center);while(actual!=null&&actual!=map&&!actual.name.StartsWith("map-",StringComparison.Ordinal))actual=actual.parent;
                        return actual!=null&&actual!=b&&actual.name.StartsWith("map-star-",StringComparison.Ordinal);});
                    Assert.That(found,Is.Not.Null,"actual generated crowded-node point at1280");return found!;
                }
                Click(doc.rootVisualElement.Q<Button>("map-fit-galaxy"));yield return null;yield return null;
                yield return CampaignPointerRight(doc,entry,CrowdedPoint(),0);
                Click(doc.rootVisualElement.Q<Button>("map-fit-galaxy"));yield return null;yield return null;
                yield return CampaignPointerRight(doc,entry,CrowdedPoint());Confirm(doc,false);Click(doc.rootVisualElement.Q<Button>("map-fit"));yield return null;
                var initialShips=model.Current.Frame.Strategy!.Ships.Select(s=>s.ShipId.Value).ToHashSet(StringComparer.Ordinal);
                CampaignPage(doc,ReviewWorkspacePage.Research);doc.rootVisualElement.Q<SliderInt>("integrated-research-split").value=10;
                Click(doc.rootVisualElement.Q<Button>("integrated-apply-split"));Confirm(doc);
                CampaignPage(doc,ReviewWorkspacePage.Production);
                var colony=model.Current.Frame.Strategy!.Construction.First(c=>c.CanEnqueue&&c.Design.DefinitionId.Value=="ship-design.source-cruiser").ColonyId.Value;
                Click(doc.rootVisualElement.Q<Button>("production-colony-"+colony));
                for(int i=0;i<2;i++){Click(doc.rootVisualElement.Q<Button>("integrated-queue-ship-design.source-cruiser"));Confirm(doc);}
                for(int i=0;i<4;i++){Click(doc.rootVisualElement.Q<Button>("player-primary-action"));Confirm(doc);yield return null;}
                var produced=model.Current.Frame.Strategy!.Ships.Select(s=>s.ShipId.Value).Where(s=>!initialShips.Contains(s)).ToArray();Assert.That(produced.Length,Is.GreaterThanOrEqualTo(2));
                CampaignPage(doc,ReviewWorkspacePage.FleetOrders);
                foreach(var id in model.Current.Frame.Strategy.Ships.Select(s=>s.ShipId.Value).ToArray())doc.rootVisualElement.Q<Toggle>("fleet-member-"+id).value=true;
                doc.rootVisualElement.Q<TextField>("fleet-organization-name").value="Generated task force";
                Click(doc.rootVisualElement.Q<Button>("fleet-create"));Confirm(doc);Assert.That(model.NamedFleets().Any(f=>produced.All(id=>f.Fleet.ShipIds.Any(s=>s.Value==id))),Is.True);
                yield return CampaignCapture(entry,doc,"paid-fleet");
                var visited=new HashSet<string>(StringComparer.Ordinal);bool travelCaptured=false,contactPriorityChecked=false;
                for(int turn=0;turn<128&&model.Current.Frame.Status.Phase==SessionPhase.Planning;turn++)
                {
                    CampaignPage(doc,ReviewWorkspacePage.Map);yield return null;
                    foreach(var fleet in model.Current.Frame.OwnStrategicFleets.ToArray())
                    {
                        var node=fleet.LocationNodeId.Value;visited.Add(node);if(fleet.TransitTotalCost>0||fleet.RouteNodeIds.Count>0)continue;
                        var known=model.GeneratedMap();var neighbors=known.Regions.ToDictionary(r=>r.NodeId,_=>new List<string>(),StringComparer.Ordinal);
                        foreach(var edge in known.Connections){neighbors[edge.EndpointA].Add(edge.EndpointB);neighbors[edge.EndpointB].Add(edge.EndpointA);}
                        var distance=new Dictionary<string,int>(StringComparer.Ordinal){{node,0}};var queue=new Queue<string>();queue.Enqueue(node);
                        while(queue.Count>0){var at=queue.Dequeue();foreach(var next in neighbors[at])if(!distance.ContainsKey(next)){distance[next]=distance[at]+1;queue.Enqueue(next);}}
                        var target=distance.Where(d=>d.Value>0&&!visited.Contains(d.Key)).OrderBy(d=>d.Value).ThenBy(d=>d.Key,StringComparer.Ordinal).Select(d=>d.Key).FirstOrDefault();
                        var contact=model.Current.Frame.Travel!.KnownContacts.Where(c=>c.Fresh).Select(c=>(Contact:c,Offer:model.Current.FindKnownRoute(fleet.FleetId.Value,c.LastSeenNodeId)))
                            .Where(c=>c.Offer?.Route.Success==true).OrderBy(c=>c.Offer!.Route.TotalCost).ThenBy(c=>c.Contact.FleetId,StringComparer.Ordinal).FirstOrDefault();
                        Assert.That(target!=null||contact.Contact!=null,Is.True,"bounded known-space driver needs a lawful next target");
                        Click(doc.rootVisualElement.Q<Button>("map-fleet-"+fleet.FleetId.Value));Click(doc.rootVisualElement.Q<Button>("map-fit"));yield return null;yield return null;
                        var marker=doc.rootVisualElement.Q<Button>(contact.Contact==null?"map-star-"+target:"map-contact-"+contact.Contact.FleetId);
                        if(contact.Contact!=null&&!contactPriorityChecked){yield return CampaignPriorityPointer(doc,entry,marker,marker.worldBound.center,0);contactPriorityChecked=true;}
                        yield return CampaignPointerRight(doc,entry,marker);var resolvedNode=entry.Workspace!.SelectedNode;Confirm(doc);yield return null;
                        var actualOrder=model.Current.Frame.Travel!.OwnOrders.Single(o=>o.FleetId==fleet.FleetId.Value);
                        Assert.That(actualOrder.RouteNodeIds.LastOrDefault()??fleet.LocationNodeId.Value,Is.EqualTo(resolvedNode),"confirmed command uses resolved destination");
                    }
                    if(!travelCaptured){yield return CampaignCapture(entry,doc,"travel");travelCaptured=true;}
                    Click(doc.rootVisualElement.Q<Button>("player-primary-action"));Confirm(doc);yield return null;
                }
                Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.AwaitingCombat),"real exploration meets lawful AI within128turns");
                CampaignPage(doc,ReviewWorkspacePage.Combat);yield return null;
                Assert.That(produced.All(id=>model.Current.Frame.Battle!.OwnShips.Any(s=>s.Condition.ShipId==id)),Is.True,"paid produced fleet participates in the battle");
                yield return CampaignCapture(entry,doc,"orders");bool firstRound=true;int rounds=0;
                while(!model.Current.Frame.Battle!.Complete)
                {
                    Assert.That(++rounds,Is.LessThanOrEqualTo(16));var round=model.Current.Frame.Battle.Round;
                    var maneuver=doc.rootVisualElement.Q<DropdownField>("combat-maneuver-picker");maneuver.index=round%maneuver.choices.Count;
                    Click(CampaignButton(doc,"Apply to local draft"));Click(CampaignButton(doc,"Review / commit round "+round));Confirm(doc);yield return null;
                    var boundary=model.Current.Frame;
                    if(firstRound)yield return CampaignCapture(entry,doc,"playback");
                    Click(doc.rootVisualElement.Q<Button>("combat-skip-pending-playback"));Assert.That(model.Current.Frame,Is.SameAs(boundary),"playback acknowledgment never resolves authority");
                    if(firstRound){yield return CampaignCapture(entry,doc,"round-report");firstRound=false;}
                    if(!model.Current.Frame.Battle.Complete)Click(CampaignButton(doc,"Plan next round"));
                }
                Click(CampaignButton(doc,"Confirm campaign aftermath"));Confirm(doc);yield return null;
                Assert.That(model.History().Count,Is.GreaterThanOrEqualTo(1));Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning));
                CampaignPage(doc,ReviewWorkspacePage.Saves);Click(doc.rootVisualElement.Q<Button>("player-save"));Assert.That(model.Message,Is.EqualTo("Checkpoint result: None"));
                var savedPath=Path.Combine(directory,"campaign","unity-generated-campaign-v7-generated-1.json");var savedBytes=File.ReadAllBytes(savedPath);
                var codec=new ReviewNewtonsoftCodec();var saved=codec.DecodeGeneratedCampaignRoot(savedBytes);Assert.That(saved.Map.ContentHash,Is.EqualTo(mapHash));
                Click(doc.rootVisualElement.Q<Button>("demo-menu"));Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-1"));Confirm(doc);yield return WaitCampaign(entry,"continue-aftermath");
                var continued=CampaignModel(entry);Assert.That(continued,Is.Not.SameAs(model));Assert.That(continued.GeneratedMap().MapContentHash,Is.EqualTo(mapHash));
                Assert.That(codec.EncodeGeneratedCampaignRoot(new ReviewGeneratedCampaignSlotStore(Path.Combine(directory,"campaign"),codec).Read("generated-1")),Is.EqualTo(savedBytes));
                Assert.That(continued.History().Count,Is.EqualTo(model.History().Count));Assert.That(continued.Current.Frame.Status.Turn,Is.EqualTo(model.Current.Frame.Status.Turn));
                yield return CampaignCapture(entry,doc,"continued-aftermath");
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"generated-whole-campaign-final.json"),savedBytes);
                File.WriteAllText(Path.Combine(CampaignOutput(),"generated-journey-summary.json"),"{\"visited\":"+visited.Count+",\"rounds\":"+rounds+",\"finalTurn\":"+continued.Current.Frame.Status.Turn+",\"knownRegions\":"+continued.GeneratedMap().Regions.Count+",\"managedBytes\":"+GC.GetTotalMemory(false)+"}");
                Assert.That(Directory.GetFiles(CampaignOutput(),"*-campaign-*.png").Length,Is.LessThanOrEqualTo(24));
            }
            yield return new ExitPlayMode();
        }
    }
}
