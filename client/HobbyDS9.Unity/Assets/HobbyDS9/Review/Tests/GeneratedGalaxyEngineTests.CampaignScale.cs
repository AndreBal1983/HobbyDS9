#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using HobbyDS9.Application;
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
        private static CampaignGeneratedSaveDto CampaignPrivateAudit(ReviewUiModel model)
        {
            // Trusted identity comparison only, never a route/picking input.
            var host=(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(model)!;
            return ((ICampaignGeneratedSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(host)!).ExportGeneratedSave();
        }
        private static IEnumerator CampaignZoomPan(UIDocument doc,ReviewEntry entry)
        {
            var map=doc.rootVisualElement.Q("playable-known-map");var panel=map.panel;var point=map.worldBound.center;
            var picked=panel.Pick(point);Assert.That(picked,Is.Not.Null);var priorZoom=CampaignModel(entry).GeneratedViewport.Zoom;
            using(var e=WheelEvent.GetPooled(new Event{type=EventType.ScrollWheel,mousePosition=point,delta=new Vector2(0,-3)})){e.target=picked;picked.SendEvent(e);}
            yield return null;Assert.That(CampaignModel(entry).GeneratedViewport.Zoom,Is.GreaterThan(priorZoom));
            int id;picked=panel.Pick(point);
            using(var e=PointerDownEvent.GetPooled(new Event{type=EventType.MouseDown,button=2,mousePosition=point})){id=e.pointerId;e.target=picked;picked.SendEvent(e);}
            var captured=panel.GetCapturingElement(id) as VisualElement;Assert.That(captured,Is.SameAs(map));
            var viewport=CampaignModel(entry).GeneratedViewport;var before=viewport.WorldX(0);var end=point+new Vector2(26,17);
            using(var e=PointerMoveEvent.GetPooled(new Event{type=EventType.MouseDrag,button=2,mousePosition=end})){Assert.That(e.pointerId,Is.EqualTo(id));e.target=captured;captured!.SendEvent(e);}
            var up=panel.GetCapturingElement(id) as VisualElement;Assert.That(up,Is.SameAs(map));
            using(var e=PointerUpEvent.GetPooled(new Event{type=EventType.MouseUp,button=2,mousePosition=end})){Assert.That(e.pointerId,Is.EqualTo(id));e.target=up;up!.SendEvent(e);}
            yield return null;Assert.That(viewport.WorldX(0),Is.Not.EqualTo(before));Assert.That(panel.GetCapturingElement(id),Is.Null);
        }
        [UnityTest] public IEnumerator GeneratedCampaign1000AndKnownMapReadabilityUseActualControls()
        {
            campaignFractionalOnlySelections=0;
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var storePath=Path.Combine(directory,"campaign");typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,storePath);
            var codec=new ReviewNewtonsoftCodec();var store=new ReviewGeneratedCampaignSlotStore(storePath,codec);
            var fixturePath=Environment.GetEnvironmentVariable("HOBBYDS9_GENERATED_READABILITY_CHECKPOINT");Assert.That(fixturePath,Is.Not.Null.And.Not.Empty);
            Assert.That(new FileInfo(fixturePath!).Length,Is.InRange(1,ReviewGeneratedCampaignSlotStore.MaximumBytes));
            var fixtureBytes=File.ReadAllBytes(fixturePath!);
            using(var hash=System.Security.Cryptography.SHA256.Create())Assert.That(BitConverter.ToString(hash.ComputeHash(fixtureBytes)).Replace("-","").ToLowerInvariant(),Is.EqualTo("1a4a3ea7395a360f53a4a2bf0f0d949722f532681f6122f824704ff97cb23c92"));
            store.Write("generated-3",codec.DecodeGeneratedCampaignRoot(fixtureBytes),false);
            try
            {
                using(var surface=new GalaxyInputSurface(doc))
                {
                    yield return null;yield return null;
                    Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-3"));Confirm(doc);yield return WaitCampaign(entry,"fixture250-readability-continue");
                    Assert.That(CampaignModel(entry).Current.Frame.Status.Turn,Is.EqualTo(69));Assert.That(CampaignModel(entry).GeneratedMap().Regions.Count,Is.EqualTo(19));
                    IEnumerator SelectAndHover(EnvironmentGalaxyFeatureDto knownStar)
                    {
                        yield return CampaignPointerRight(doc,entry,doc.rootVisualElement.Q<Button>("map-star-"+knownStar.NodeId),0);
                        var fixtureMap=doc.rootVisualElement.Q("playable-known-map");var vp=CampaignModel(entry).GeneratedViewport;
                        var hoverPoint=fixtureMap.LocalToWorld(new Vector2((float)vp.X(knownStar.Position.X),(float)vp.Y(knownStar.Position.Y)));
                        var hoverPick=fixtureMap.panel.Pick(hoverPoint);Assert.That(hoverPick,Is.Not.Null);
                        using(var e=PointerMoveEvent.GetPooled(new Event{type=EventType.MouseMove,mousePosition=hoverPoint})){e.target=hoverPick;hoverPick.SendEvent(e);}
                        yield return null;Assert.That(doc.rootVisualElement.Q<Label>("map-label-"+knownStar.NodeId).resolvedStyle.visibility,Is.EqualTo(Visibility.Visible));
                        TestContext.WriteLine("Actual selected-hover label visible: "+knownStar.NodeId);
                    }
                    var actorMap=CampaignModel(entry).GeneratedMap();yield return SelectAndHover(actorMap.Features.First(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star));
                    var fixtureFrame=CampaignModel(entry).Current.Frame;var view=CampaignModel(entry).GeneratedViewport;
                    var ownedNodes=fixtureFrame.OwnStrategicFleets.Where(f=>fixtureFrame.Travel!.OwnReceipts.LastOrDefault(r=>r.FleetId==f.FleetId.Value)?.TransitDestinationId==null).Select(f=>f.LocationNodeId.Value).ToHashSet(StringComparer.Ordinal);
                    Vector2 Project(string node){var p=actorMap.Features.FirstOrDefault(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star&&f.NodeId==node)?.Position??actorMap.Regions.Single(r=>r.NodeId==node).Center;return new Vector2((float)view.X(p.X),(float)view.Y(p.Y));}
                    var ringPoints=ownedNodes.Select(Project).ToArray();
                    var nearRing=actorMap.Features.Where(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star&&!ownedNodes.Contains(f.NodeId!))
                        .Where(f=>ringPoints.Any(p=>Math.Abs(Project(f.NodeId!).x-p.x)<=37&&Math.Abs(Project(f.NodeId!).y-p.y)<=37))
                        .OrderBy(f=>ringPoints.Min(p=>(Project(f.NodeId!)-p).sqrMagnitude)).ThenBy(f=>f.NodeId,StringComparer.Ordinal).First();
                    yield return SelectAndHover(nearRing);
                    yield return CampaignCapture(entry,doc,"fixture250-aftermath-readability",new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)});
                    Click(doc.rootVisualElement.Q<Button>("demo-menu"));doc.rootVisualElement.Q<DropdownField>("generated-campaign-size").value="1000";
                    doc.rootVisualElement.Q<TextField>("generated-campaign-seed").value="42";doc.rootVisualElement.Q<DropdownField>("generated-campaign-quiet").value="2.00×";
                    Click(doc.rootVisualElement.Q<Button>("generated-campaign-start"));Confirm(doc);yield return WaitCampaign(entry,"new-game1000");
                    var model=CampaignModel(entry);var known=model.GeneratedMap();var initialCount=known.Regions.Count;
                    Assert.That(known.Width,Is.EqualTo(5000000));Assert.That(known.Height,Is.EqualTo(4000000));Assert.That(initialCount,Is.InRange(2,20));
                    Assert.That(known.CampaignRevision,Is.EqualTo(model.Current.Frame.Status.CampaignRevision));Assert.That(known.ObservationTurn,Is.EqualTo(model.Current.Frame.Status.Turn));
                    var ids=known.Regions.Select(r=>r.NodeId).ToHashSet(StringComparer.Ordinal);
                    Assert.That(known.Features.All(f=>f.NodeId!=null&&ids.Contains(f.NodeId)),Is.True);Assert.That(known.Connections.All(e=>ids.Contains(e.EndpointA)&&ids.Contains(e.EndpointB)),Is.True);
                    yield return CampaignCapture(entry,doc,"scale1000-initial");
                    Click(doc.rootVisualElement.Q<Button>("map-fit-galaxy"));yield return null;yield return CampaignZoomPan(doc,entry);
                    Click(doc.rootVisualElement.Q<Button>("map-fit"));yield return null;yield return null;
                    var fleet=model.Current.Frame.OwnStrategicFleets.First();Click(doc.rootVisualElement.Q<Button>("map-fleet-"+fleet.FleetId.Value));yield return null;
                    var target=known.Regions.Select(r=>(Region:r,Offer:model.Current.FindKnownRoute(fleet.FleetId.Value,r.NodeId)))
                        .Where(r=>r.Offer?.Route.Success==true&&r.Offer.Route.TotalCost>0)
                        .OrderBy(r=>r.Offer!.Route.TotalCost).ThenByDescending(r=>r.Region.Boundary.Count-known.Connections.Count(e=>e.EndpointA==r.Region.NodeId||e.EndpointB==r.Region.NodeId))
                        .ThenBy(r=>r.Region.NodeId,StringComparer.Ordinal).First().Region.NodeId;
                    yield return CampaignPointerRight(doc,entry,doc.rootVisualElement.Q<Button>("map-star-"+target),0);
                    yield return CampaignPointerRight(doc,entry,doc.rootVisualElement.Q<Button>("map-star-"+target));Confirm(doc,false);
                    yield return CampaignCapture(entry,doc,"scale1000-route-preview");
                    yield return CampaignPointerRight(doc,entry,doc.rootVisualElement.Q<Button>("map-star-"+target));Confirm(doc);
                    Click(doc.rootVisualElement.Q<Button>("player-primary-action"));Confirm(doc);yield return null;
                    Assert.That(model.GeneratedMap().ObservedNodeIds,Does.Contain(target));Assert.That(model.GeneratedMap().Regions.Count,Is.GreaterThan(initialCount),"arrival charts new neighbor space");
                    CampaignTrace(entry,"scale1000-after-move");
                    CampaignPage(doc,ReviewWorkspacePage.Saves);Click(doc.rootVisualElement.Q<Button>("player-save"));Assert.That(model.Message,Is.EqualTo("Checkpoint result: None"));
                    var saved=store.Read("generated-1");Assert.That(saved.Map.Layout.Systems.Count,Is.EqualTo(1000));var bytes=codec.EncodeGeneratedCampaignRoot(saved);
                    File.WriteAllBytes(Path.Combine(CampaignOutput(),"scale1000-private-saved.json"),bytes);
                    Click(doc.rootVisualElement.Q<Button>("demo-menu"));Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-1"));Confirm(doc);yield return WaitCampaign(entry,"continue1000");
                    var continued=CampaignModel(entry);Assert.That(continued,Is.Not.SameAs(model));Assert.That(codec.EncodeGeneratedCampaignRoot(CampaignPrivateAudit(continued)),Is.EqualTo(bytes));
                    yield return CampaignCapture(entry,doc,"scale1000-continued");
                    var template=(CampaignPlayableFleetPackageDto)typeof(ReviewUiModel).GetField("generatedCampaignTemplate",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(continued)!;
                    var audit=CampaignGeneratedApplication.LoadSave(template,saved);var status=audit.CaptureStatus();Assert.That(audit.AdvanceTurn(status.Turn,status.CampaignRevision).Accepted,Is.True);
                    Click(doc.rootVisualElement.Q<Button>("player-primary-action"));Confirm(doc);yield return null;
                    Assert.That(codec.EncodeGeneratedCampaignRoot(CampaignPrivateAudit(continued)),Is.EqualTo(codec.EncodeGeneratedCampaignRoot(audit.ExportGeneratedSave())),"next actual turn equals trusted audit; root never chooses UI actions");
                    File.WriteAllText(Path.Combine(CampaignOutput(),"scale1000-summary.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{InitialKnown=initialCount,Known=continued.GeneratedMap().Regions.Count,Turn=continued.Current.Frame.Status.Turn,
                        continued.GeneratedMap().MapContentHash,FractionalAnchorUsedAfterRoundedFailure=campaignFractionalOnlySelections,ManagedBytes=GC.GetTotalMemory(false),NativePixelAccessibilityClaim=false}));
                    Assert.That(Directory.GetFiles(CampaignOutput(),"*-campaign-*.png").Length,Is.EqualTo(11));
                }
            }
            finally{CampaignDiagnosticSnapshot(entry,"scale1000-finally");}
            yield return new ExitPlayMode();
        }
    }
}
