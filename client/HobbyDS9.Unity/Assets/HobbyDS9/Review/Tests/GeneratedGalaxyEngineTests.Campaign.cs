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
        private static int campaignFractionalOnlySelections;
        private static ReviewUiModel CampaignModel(ReviewEntry entry)=>(ReviewUiModel)typeof(ReviewEntry).GetField("model",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry)!;
        private static void CampaignPage(UIDocument doc,ReviewWorkspacePage page)=>Click(doc.rootVisualElement.Q<Button>("review-nav-"+page));
        private static Button CampaignButton(UIDocument doc,string text)=>doc.rootVisualElement.Query<Button>().ToList().Single(b=>b.text==text);
        private static string CampaignOutput()
        {var path=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/scaled-galaxy/engine-rendered"));Directory.CreateDirectory(path);return path;}
        private static void CampaignDiagnosticError(string phase,Exception error)
        {
            // Diagnostics must never replace the original assertion/exception.
            try{File.AppendAllText(Path.Combine(CampaignOutput(),"generated-diagnostic-errors.txt"),phase+": "+error+Environment.NewLine);}catch{}
        }
        private static void CampaignTrace(ReviewEntry entry,string phase,object? intent=null)
        {
            try
            {
                var model=CampaignModel(entry);if(!model.GeneratedCampaign)return;
                var frame=model.Current.Frame;var known=model.GeneratedMap();
                var line=Newtonsoft.Json.JsonConvert.SerializeObject(new{phase,intent,frame.Actor,frame.Status.Turn,frame.Status.CampaignRevision,
                    Phase=frame.Status.Phase.ToString(),known.MapContentHash,known.ObservationTurn,MapRevision=known.CampaignRevision,
                    KnownRegions=known.Regions.Count,known.ObservedNodeIds,OwnFleets=frame.OwnStrategicFleets,
                    OwnOrders=frame.Travel?.OwnOrders,Contacts=frame.Travel?.KnownContacts,model.Message});
                File.AppendAllText(Path.Combine(CampaignOutput(),"generated-player-turn-trace.jsonl"),line+Environment.NewLine);
            }
            catch(Exception error){CampaignDiagnosticError(phase,error);}
        }
        private static void CampaignDiagnosticSnapshot(ReviewEntry entry,string phase)
        {
            CampaignTrace(entry,phase);
            try
            {
                var model=CampaignModel(entry);if(!model.GeneratedCampaign||model.Busy||model.Current.HasPendingWork)return;
                // Private audit export only. Never supplied to renderer, route selection or player input.
                var host=(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(model)!;
                var session=(HobbyDS9.Application.ICampaignGeneratedSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(host)!;
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"generated-private-audit-"+phase+".json"),new ReviewNewtonsoftCodec().EncodeGeneratedCampaignRoot(session.ExportGeneratedSave()));
                File.WriteAllText(Path.Combine(CampaignOutput(),"generated-actor-map-"+phase+".json"),Newtonsoft.Json.JsonConvert.SerializeObject(model.GeneratedMap()));
            }
            catch(Exception error){CampaignDiagnosticError(phase,error);}
        }
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
            Assert.That(intended,Is.Not.Null);var frame=CampaignModel(entry).Current.Frame;
            var panel=doc.rootVisualElement.panel;var map=doc.rootVisualElement.Q("playable-known-map");var known=CampaignModel(entry).GeneratedMap();
            Assert.That(intended.enabledInHierarchy&&intended.visible&&intended.resolvedStyle.display!=DisplayStyle.None&&map.enabledInHierarchy&&map.visible,Is.True,"intended marker and map must be available");
            bool contact=intended.name.StartsWith("map-contact-",StringComparison.Ordinal);
            var intendedNode=contact?frame.Travel!.KnownContacts.Single(c=>"map-contact-"+c.FleetId==intended.name).LastSeenNodeId:intended.name.Substring("map-star-".Length);
            var anchors=known.Regions.Select(r=>{var star=known.Features.FirstOrDefault(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star&&f.NodeId==r.NodeId);var at=star?.Position??r.Center;
                return new GeneratedCampaignNodeAnchor(r.NodeId,(float)CampaignModel(entry).GeneratedViewport.X(at.X),(float)CampaignModel(entry).GeneratedViewport.Y(at.Y));}).ToArray();
            Vector2 point=default;VisualElement? down=null,hit=null;var attempts=new List<string>();
            // Pixel rounding can move a button centre out of its nearest-anchor area.
            // Try the actual integer pixel and exact painted anchor before the bounded grid.
            var offsets=new[]{0f,-.2f,.2f,-.4f,.4f};var bounds=intended.worldBound;
            var anchor=anchors.Single(a=>a.NodeId==intendedNode);
            var exact=contact?bounds.center:map.LocalToWorld(new Vector2((float)anchor.X,(float)anchor.Y));
            var candidates=new List<(Vector2 Point,string Kind)>{(new Vector2(Mathf.Round(exact.x),Mathf.Round(exact.y)),"rounded-anchor"),(exact,"exact-anchor")};
            foreach(var y in offsets)foreach(var x in offsets)
                if(candidates.Count<25)candidates.Add((bounds.center+new Vector2(x*bounds.width,y*bounds.height),"interior-grid"));
            string chosen="none";
            foreach(var sample in candidates)
            {
                    var candidate=sample.Point;
                    if(!map.worldBound.Contains(candidate)||!bounds.Contains(candidate)||!intended.ContainsPoint(intended.WorldToLocal(candidate)))
                    {attempts.Add(sample.Kind+" "+candidate+" clipped");continue;}
                    var picked=panel.Pick(candidate);var ancestor=picked;
                    while(ancestor!=null&&ancestor!=map&&!ancestor.name.StartsWith("map-",StringComparison.Ordinal))ancestor=ancestor.parent;
                    var local=map.WorldToLocal(candidate);var resolved=GeneratedCampaignNodePicker.Resolve(anchors,local.x,local.y);
                    bool allowed=contact?picked!=null&&(picked==intended||intended.Contains(picked)):
                        (ancestor==map||ancestor?.name.StartsWith("map-star-",StringComparison.Ordinal)==true)&&resolved==intendedNode;
                    attempts.Add(sample.Kind+" "+candidate+" hit="+(picked?.name??"null")+" ancestor="+(ancestor?.name??"null")+" resolved="+(resolved??"null"));
                    if(!allowed)continue;
                    point=candidate;down=picked;hit=ancestor;chosen=sample.Kind;break;
            }
            if(chosen=="exact-anchor")campaignFractionalOnlySelections++;
            var diagnostic=intended.name+" chosen="+chosen+" fractionalOnlyTotal="+campaignFractionalOnlySelections+"; "+string.Join("; ",attempts);
            File.AppendAllText(Path.Combine(CampaignOutput(),"generated-pointer-diagnostics.txt"),diagnostic+Environment.NewLine);
            TestContext.WriteLine(diagnostic);
            Assert.That(down,Is.Not.Null,"No exposed marker point: "+intended.name+" bounds="+bounds+" map="+map.worldBound+"; "+string.Join("; ",attempts));
            var expected=intendedNode;
            TestContext.WriteLine("Exposed campaign pointer "+intended.name+" point="+point+" actual="+down!.name+" resolved="+expected+" candidates="+attempts.Count);
            int id;
            using(var e=PointerDownEvent.GetPooled(new Event{type=EventType.MouseDown,button=button,mousePosition=point}))
            {id=e.pointerId;e.target=down;down!.SendEvent(e);}
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
            campaignFractionalOnlySelections=0;
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,Path.Combine(directory,"campaign"));
            var before=CampaignModel(entry);
            try
            {
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
                CampaignTrace(entry,"new-game");
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
                CampaignTrace(entry,"research-split-confirmed");
                CampaignPage(doc,ReviewWorkspacePage.Production);
                var colony=model.Current.Frame.Strategy!.Construction.First(c=>c.CanEnqueue&&c.Design.DefinitionId.Value=="ship-design.source-cruiser").ColonyId.Value;
                Click(doc.rootVisualElement.Q<Button>("production-colony-"+colony));
                for(int i=0;i<2;i++){Click(doc.rootVisualElement.Q<Button>("integrated-queue-ship-design.source-cruiser"));Confirm(doc);CampaignTrace(entry,"construction-confirmed",new{QueueIndex=i});}
                for(int i=0;i<4;i++){Click(doc.rootVisualElement.Q<Button>("player-primary-action"));Confirm(doc);yield return null;CampaignTrace(entry,"production-turn-advanced");}
                var produced=model.Current.Frame.Strategy!.Ships.Select(s=>s.ShipId.Value).Where(s=>!initialShips.Contains(s)).ToArray();Assert.That(produced.Length,Is.GreaterThanOrEqualTo(2));
                CampaignPage(doc,ReviewWorkspacePage.FleetOrders);
                foreach(var id in model.Current.Frame.Strategy.Ships.Select(s=>s.ShipId.Value).ToArray())doc.rootVisualElement.Q<Toggle>("fleet-member-"+id).value=true;
                doc.rootVisualElement.Q<TextField>("fleet-organization-name").value="Generated task force";
                Click(doc.rootVisualElement.Q<Button>("fleet-create"));Confirm(doc);Assert.That(model.NamedFleets().Any(f=>produced.All(id=>f.Fleet.ShipIds.Any(s=>s.Value==id))),Is.True);
                CampaignTrace(entry,"fleet-merged",new{Produced=produced});
                yield return CampaignCapture(entry,doc,"paid-fleet");
                Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(5),"four paid-production setup turns");
                var ownSystem=model.Current.Frame.Strategy!.Colonies.Where(c=>c.OwnerCivilizationId?.Value==model.Current.Frame.Actor)
                    .OrderBy(c=>c.ColonyId.Value,StringComparer.Ordinal).First().SystemId.Value;
                var ownHome=model.GeneratedMap().Regions.Single(r=>r.SystemId==ownSystem).NodeId;
                var guardIssued=new HashSet<string>(StringComparer.Ordinal);
                var visited=new HashSet<string>(StringComparer.Ordinal);bool travelCaptured=false,contactPriorityChecked=false;
                for(int turn=0;turn<128&&model.Current.Frame.Status.Phase==SessionPhase.Planning;turn++)
                {
                    CampaignTrace(entry,"turn-start");
                    CampaignPage(doc,ReviewWorkspacePage.Map);yield return null;
                    foreach(var fleet in model.Current.Frame.OwnStrategicFleets.ToArray())
                    {
                        var node=fleet.LocationNodeId.Value;visited.Add(node);if(fleet.TransitTotalCost>0)continue;
                        if(turn>=8&&node==ownHome)
                        {
                            if(!guardIssued.Contains(fleet.FleetId.Value))
                            {
                                Click(doc.rootVisualElement.Q<Button>("map-fleet-"+fleet.FleetId.Value));CampaignPage(doc,ReviewWorkspacePage.FleetOrders);
                                var defend=doc.rootVisualElement.Q<Button>("player-stay-engage");Assert.That(defend,Is.Not.Null);Assert.That(defend.enabledInHierarchy,Is.True);
                                Click(defend);Confirm(doc);guardIssued.Add(fleet.FleetId.Value);
                                Assert.That(model.Current.Frame.Travel!.OwnOrders.Single(o=>o.FleetId==fleet.FleetId.Value).Intent,Is.EqualTo(CampaignTravelIntent.StayEngage));
                                CampaignTrace(entry,"own-colony-guard-confirmed",new{Fleet=fleet.FleetId.Value,OwnColonyNode=ownHome});
                            }
                            continue;
                        }
                        if(fleet.RouteNodeIds.Count>0)continue;
                        var known=model.GeneratedMap();var neighbors=known.Regions.ToDictionary(r=>r.NodeId,_=>new List<string>(),StringComparer.Ordinal);
                        foreach(var edge in known.Connections){neighbors[edge.EndpointA].Add(edge.EndpointB);neighbors[edge.EndpointB].Add(edge.EndpointA);}
                        var distance=new Dictionary<string,int>(StringComparer.Ordinal){{node,0}};var queue=new Queue<string>();queue.Enqueue(node);
                        while(queue.Count>0){var at=queue.Dequeue();foreach(var next in neighbors[at])if(!distance.ContainsKey(next)){distance[next]=distance[at]+1;queue.Enqueue(next);}}
                        var target=distance.Where(d=>d.Value>0&&!visited.Contains(d.Key)).OrderBy(d=>d.Value).ThenBy(d=>d.Key,StringComparer.Ordinal).Select(d=>d.Key).FirstOrDefault();
                        var contact=model.Current.Frame.Travel!.KnownContacts.Where(c=>c.Fresh).Select(c=>(Contact:c,Offer:model.Current.FindKnownRoute(fleet.FleetId.Value,c.LastSeenNodeId)))
                            .Where(c=>c.Offer?.Route.Success==true).OrderBy(c=>c.Offer!.Route.TotalCost).ThenBy(c=>c.Contact.FleetId,StringComparer.Ordinal).FirstOrDefault();
                        if(turn>=8){target=ownHome;contact=default;} // Deliberate defence; no hidden enemy target or Follow override.
                        CampaignTrace(entry,"route-choice",new{Fleet=fleet.FleetId.Value,Origin=node,Target=target,Contact=contact.Contact?.FleetId,
                            ContactNode=contact.Contact?.LastSeenNodeId,ContactTurn=contact.Contact?.LastSeenTurn,Intent=contact.Contact==null?"Leave":"Follow"});
                        Assert.That(target!=null||contact.Contact!=null,Is.True,"bounded known-space driver needs a lawful next target");
                        Click(doc.rootVisualElement.Q<Button>("map-fleet-"+fleet.FleetId.Value));Click(doc.rootVisualElement.Q<Button>("map-fit"));yield return null;yield return null;
                        var marker=doc.rootVisualElement.Q<Button>(contact.Contact==null?"map-star-"+target:"map-contact-"+contact.Contact.FleetId);
                        if(contact.Contact!=null&&!contactPriorityChecked){yield return CampaignPriorityPointer(doc,entry,marker,marker.worldBound.center,0);contactPriorityChecked=true;}
                        yield return CampaignPointerRight(doc,entry,marker);var resolvedNode=entry.Workspace!.SelectedNode;Confirm(doc);yield return null;
                        var actualOrder=model.Current.Frame.Travel!.OwnOrders.Single(o=>o.FleetId==fleet.FleetId.Value);
                        Assert.That(actualOrder.RouteNodeIds.LastOrDefault()??fleet.LocationNodeId.Value,Is.EqualTo(resolvedNode),"confirmed command uses resolved destination");
                        CampaignTrace(entry,"order-confirmed");
                    }
                    if(!travelCaptured){yield return CampaignCapture(entry,doc,"travel");travelCaptured=true;}
                    Click(doc.rootVisualElement.Q<Button>("player-primary-action"));Confirm(doc);yield return null;
                    CampaignTrace(entry,"turn-advanced");
                    if(turn==7)Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(13),"eight actor-known exploration turns");
                    var observed=model.Current.Frame.Travel!.KnownContacts.FirstOrDefault(c=>c.Fresh);
                    if(observed!=null&&!contactPriorityChecked)
                    {
                        CampaignPage(doc,ReviewWorkspacePage.Map);yield return null;yield return null;
                        var marker=doc.rootVisualElement.Q<Button>("map-contact-"+observed.FleetId);
                        yield return CampaignPriorityPointer(doc,entry,marker,marker.worldBound.center,0);
                        if(model.Current.Frame.Status.Phase==SessionPhase.Planning){yield return CampaignPointerRight(doc,entry,marker);Confirm(doc,false);}
                        else yield return CampaignPriorityPointer(doc,entry,marker,marker.worldBound.center,1);
                        foreach(var id in guardIssued)Assert.That(model.Current.Frame.Travel!.OwnOrders.Single(o=>o.FleetId==id).Intent,Is.EqualTo(CampaignTravelIntent.StayEngage),"contact inspection never replaces deliberate guard");
                        contactPriorityChecked=true;CampaignTrace(entry,"guard-contact-inspected-without-replacing-order");
                    }
                }
                CampaignDiagnosticSnapshot(entry,"before-encounter-bound");
                Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.AwaitingCombat),"real exploration meets lawful AI within128turns");
                Assert.That(guardIssued,Is.Not.Empty);Assert.That(contactPriorityChecked,Is.True,"actual observed-contact pointer priority checked");
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
                File.WriteAllText(Path.Combine(CampaignOutput(),"generated-journey-summary.json"),"{\"visited\":"+visited.Count+",\"rounds\":"+rounds+",\"finalTurn\":"+continued.Current.Frame.Status.Turn+",\"knownRegions\":"+continued.GeneratedMap().Regions.Count+",\"fractionalOnlySelections\":"+campaignFractionalOnlySelections+",\"managedBytes\":"+GC.GetTotalMemory(false)+"}");
                Assert.That(Directory.GetFiles(CampaignOutput(),"*-campaign-*.png").Length,Is.LessThanOrEqualTo(24));
            }
            }
            finally{CampaignDiagnosticSnapshot(entry,"finally");}
            yield return new ExitPlayMode();
        }
    }
}
