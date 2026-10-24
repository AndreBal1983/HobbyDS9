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
        private static IEnumerator DesignerIdle(ReviewEntry entry)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            while((bool)CheckpointField(entry,"generatedCampaignBusy")! || CampaignModel(entry).Busy
                || CheckpointField(entry,"checkpointUiOperation")!=null)
            {Assert.That(clock.Elapsed.TotalSeconds,Is.LessThan(90),"bounded real designer checkpoint operation");yield return null;}
            yield return null;yield return null;
        }
        private static IEnumerator DesignerCapture(UIDocument doc,Vector2Int size,string label)
        {
            var previous=doc.panelSettings.targetTexture;var active=RenderTexture.active;
            var width=doc.rootVisualElement.style.width;var height=doc.rootVisualElement.style.height;
            var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);Texture2D? pixels=null;
            try
            {
                Assert.That(target.Create(),Is.True);doc.panelSettings.targetTexture=target;
                doc.rootVisualElement.style.width=size.x;doc.rootVisualElement.style.height=size.y;
                yield return null;yield return null;yield return null;
                if(label!="starter")
                {
                    doc.rootVisualElement.Q<Foldout>("integrated-design-history").value=true;
                    yield return null;yield return null;yield return null;
                    var scroller=doc.rootVisualElement.Q<ScrollView>("integrated-design-history-scroll");
                    Assert.That(scroller,Is.Not.Null,"expanded revision library has its own bounded scrolling area");
                    var viewport=scroller.contentViewport.worldBound;
                    Assert.That(viewport.height,Is.GreaterThan(30));
                    Assert.That(viewport.yMax,Is.LessThanOrEqualTo(doc.rootVisualElement.Q("player-footer").worldBound.yMin));
                    var revisions=scroller.Query<Button>().ToList().Where(b=>b.name.StartsWith("integrated-design-edit-",StringComparison.Ordinal)).ToArray();
                    Assert.That(revisions.Length,Is.EqualTo(2));
                    foreach(var revision in revisions)
                    {
                        Assert.That(viewport.Contains(revision.worldBound.min+Vector2.one),Is.True,"revision begins inside history viewport: "+revision.name);
                        Assert.That(viewport.Contains(revision.worldBound.max-Vector2.one),Is.True,"revision ends inside history viewport: "+revision.name);
                    }
                }
                var publish=doc.rootVisualElement.Q<Button>("integrated-design-publish");
                Assert.That(publish,Is.Not.Null);Assert.That(publish.worldBound.width,Is.GreaterThan(20));
                Assert.That(publish.worldBound.height,Is.GreaterThan(15));
                var bounds=doc.rootVisualElement.worldBound;
                Assert.That(bounds.Contains(publish.worldBound.min+Vector2.one),Is.True,"publication action begins inside viewport");
                Assert.That(bounds.Contains(publish.worldBound.max-Vector2.one),Is.True,"publication action ends inside viewport");
                var preview=doc.rootVisualElement.Q<Image>("designer-hull-reference-image");
                var overview=doc.rootVisualElement.Q<ScrollView>("designer-overview").contentViewport.worldBound;
                Assert.That(overview.Contains(preview.worldBound.min+Vector2.one),Is.True,"whole model image begins inside overview viewport");
                Assert.That(overview.Contains(preview.worldBound.max-Vector2.one),Is.True,"whole model image ends inside overview viewport");
                RenderTexture.active=target;pixels=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);
                pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"designer-"+label+"-"+size.x+"x"+size.y+".png"),pixels.EncodeToPNG());
            }
            finally
            {
                doc.panelSettings.targetTexture=previous;doc.rootVisualElement.style.width=width;doc.rootVisualElement.style.height=height;
                RenderTexture.active=active;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);
            }
            yield return null;yield return null;
        }
        [UnityTest,Timeout(600000)]
        public IEnumerator CampaignDesignerPublishesImmutableRevisionsAndContinuesExactHistory()
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Designer-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!;
            var prior=(string?)field.GetValue(entry);field.SetValue(entry,Path.Combine(directory,"campaign"));
            Exception? failure=null;var pending=new Stack<IEnumerator>();pending.Push(DesignerBody(entry,doc));
            while(pending.Count>0)
            {
                object? yielded=null;bool next=false;
                var body=pending.Peek();
                try{next=body.MoveNext();if(next)yielded=body.Current;}catch(Exception error){failure=error;}
                if(failure!=null)break;
                if(!next){pending.Pop();(body as IDisposable)?.Dispose();continue;}
                if(yielded is IEnumerator nested){pending.Push(nested);continue;}
                yield return yielded;
            }
            while(pending.Count>0)(pending.Pop() as IDisposable)?.Dispose();
            // Settle real forwarded continuations before disabling the entry or deleting its temp store.
            var cleanup=System.Diagnostics.Stopwatch.StartNew();
            while((bool)CheckpointField(entry,"generatedCampaignBusy")! || CampaignModel(entry).Busy || CheckpointField(entry,"checkpointUiOperation")!=null)
            {if(cleanup.Elapsed.TotalSeconds>=90){directory=null;failure=failure??new TimeoutException("Unsettled designer checkpoint; temporary namespace retained.");break;}yield return null;}
            entry.enabled=false;field.SetValue(entry,prior);yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        private IEnumerator DesignerBody(ReviewEntry entry,UIDocument doc)
        {
            using(var surface=new GalaxyInputSurface(doc))
            {
                yield return null;yield return null;
                doc.rootVisualElement.Q<TextField>("generated-campaign-seed").value="42";
                doc.rootVisualElement.Q<DropdownField>("generated-campaign-size").value="250";
                Click(doc.rootVisualElement.Q<Button>("generated-campaign-start"));yield return DesignerIdle(entry);
                var model=CampaignModel(entry);var codec=new ReviewNewtonsoftCodec();
                CampaignPage(doc,ReviewWorkspacePage.Designer);yield return null;yield return null;
                Assert.That(doc.rootVisualElement.Q<Label>("integrated-design-scope").text,Does.Contain("Building custom classes is not available"));
                Assert.That(doc.rootVisualElement.Q<Button>("integrated-design-publish").enabledInHierarchy,Is.True,"real campaign starter is valid and publishable");
                var modelImage=doc.rootVisualElement.Q<Image>("designer-hull-reference-image");
                Assert.That(modelImage,Is.Not.Null);Assert.That(modelImage.image,Is.TypeOf<RenderTexture>(),"actual bounded 3D model reference rendered");
                var reference=modelImage.image;Assert.That(reference.width,Is.EqualTo(768));Assert.That(reference.height,Is.EqualTo(480));
                Assert.That(doc.rootVisualElement.Q<Label>("designer-hull-reference-caption").text,Does.Contain("Illustrative"));
                var untouched=codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model));
                var templates=doc.rootVisualElement.Q<DropdownField>("designer-template");
                templates.value=templates.choices.Single(c=>c.Contains("advanced"));yield return null;
                Assert.That(doc.rootVisualElement.Q<Button>("integrated-design-publish").enabledInHierarchy,Is.False,"invalid empty hull cannot publish");
                Assert.That(doc.rootVisualElement.Q<Label>("designer-state").text,Does.Contain("HARD ERROR"));
                Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model)),Is.EqualTo(untouched));
                templates=doc.rootVisualElement.Q<DropdownField>("designer-template");templates.value=templates.choices.Single(c=>c.Contains("starter"));yield return null;
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})yield return DesignerCapture(doc,size,"starter");
                reference=doc.rootVisualElement.Q<Image>("designer-hull-reference-image").image;
                doc.rootVisualElement.Q<TextField>("designer-name").value="Nightwatch";yield return null;
                Assert.That(doc.rootVisualElement.Q<Image>("designer-hull-reference-image").image,Is.SameAs(reference),"same hull draft edits retain the rendered stage");
                Assert.That(model.LocalDesignDirty,Is.True);Click(doc.rootVisualElement.Q<Button>("integrated-design-publish"));yield return null;
                var first=model.DesignLibrary().Published.Single();string blueprint=first.Blueprint.BlueprintId;
                Assert.That(first.Blueprint.Revision,Is.EqualTo(1));Assert.That(first.Blueprint.Fit.DisplayName,Is.EqualTo("Nightwatch"));
                var frozen=JsonConvert.SerializeObject(first.Blueprint);Assert.That(model.LocalDesignDirty,Is.False);
                Assert.That(doc.rootVisualElement.Q<Button>("integrated-design-publish").enabledInHierarchy,Is.False,"unchanged fit cannot duplicate publication");
                doc.rootVisualElement.Q<TextField>("designer-name").value="Nightwatch Mk II";yield return null;
                Click(doc.rootVisualElement.Q<Button>("integrated-design-publish"));yield return null;
                var history=model.DesignLibrary().Published.Where(r=>r.Blueprint.BlueprintId==blueprint).OrderBy(r=>r.Blueprint.Revision).ToArray();
                Assert.That(history.Length,Is.EqualTo(2));Assert.That(JsonConvert.SerializeObject(history[0].Blueprint),Is.EqualTo(frozen));
                Assert.That(history[0].IsLatestRevision,Is.False);Assert.That(history[1].IsLatestRevision,Is.True);
                Assert.That(history[1].Blueprint.Fit.DisplayName,Is.EqualTo("Nightwatch Mk II"));
                doc.rootVisualElement.Q<Foldout>("integrated-design-history").value=true;yield return null;
                Click(doc.rootVisualElement.Q<Button>("integrated-design-edit-"+blueprint+"-1"));yield return null;
                Assert.That(doc.rootVisualElement.Q<TextField>("designer-name").value,Is.EqualTo("Nightwatch"));
                Assert.That(doc.rootVisualElement.Q<Button>("integrated-design-publish").enabledInHierarchy,Is.False);
                yield return DesignerCapture(doc,new Vector2Int(1920,1080),"immutable-history");
                var expected=codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model));
                CampaignPage(doc,ReviewWorkspacePage.Saves);Click(doc.rootVisualElement.Q<Button>("save-slot-generated-2"));Click(doc.rootVisualElement.Q<Button>("player-save"));
                yield return DesignerIdle(entry);Assert.That(model.Message,Is.EqualTo("Checkpoint result: None"));
                var save=Path.Combine(directory!,"campaign","unity-generated-static-astronomy-v2-generated-2.json");Assert.That(File.ReadAllBytes(save),Is.EqualTo(expected));
                Click(doc.rootVisualElement.Q<Button>("demo-menu"));Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-2"));Confirm(doc);yield return DesignerIdle(entry);
                var continued=CampaignModel(entry);Assert.That(continued.Current,Is.Not.SameAs(model.Current));
                Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(continued)),Is.EqualTo(expected));
                CampaignPage(doc,ReviewWorkspacePage.Designer);yield return null;yield return null;
                doc.rootVisualElement.Q<Foldout>("integrated-design-history").value=true;
                Assert.That(continued.DesignLibrary().Published.Count,Is.EqualTo(2));
                Click(doc.rootVisualElement.Q<Button>("integrated-design-edit-"+blueprint+"-2"));yield return null;
                Assert.That(doc.rootVisualElement.Q<TextField>("designer-name").value,Is.EqualTo("Nightwatch Mk II"));
                yield return DesignerCapture(doc,new Vector2Int(1280,720),"continued");
                File.WriteAllText(Path.Combine(CampaignOutput(),"designer-results.json"),JsonConvert.SerializeObject(new{passed=true,revisions=2,blankRejected=true,firstImmutable=true,menuContinueExact=true,captures=5,customConstruction=false,nativeInput=false}));
            }
        }
    }
}
