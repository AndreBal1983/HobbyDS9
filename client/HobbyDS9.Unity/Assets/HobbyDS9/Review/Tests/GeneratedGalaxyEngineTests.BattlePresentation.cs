#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    public sealed partial class GeneratedGalaxyEngineTests
    {
        private static ReviewBattleStage BattleStage(UIDocument doc)=>(ReviewBattleStage)typeof(ReviewBattlePanel).GetField("stage",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(doc.rootVisualElement.Q<ReviewBattlePanel>())!;
        private static void BattleCameraContainment(ReviewBattleStage stage,string label)
        {
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(ReviewBattleStage);
            var camera=(Camera)type.GetField("camera",flags)!.GetValue(stage)!;var root=(GameObject)type.GetField("root",flags)!.GetValue(stage)!;
            var ships=(System.Collections.Generic.Dictionary<string,GameObject>)type.GetField("ships",flags)!.GetValue(stage)!;
            var poses=(System.Collections.Generic.Dictionary<string,Tuple<Vector3,Quaternion,Vector3,Quaternion>>)type.GetField("poses",flags)!.GetValue(stage)!;
            var round=(CampaignRoundPlayback?)type.GetField("round",flags)!.GetValue(stage);int checkedPoints=0,cues=0;
            void Inside(Vector3 world,string reason)
            {
                var projected=camera.WorldToViewportPoint(world);checkedPoints++;
                foreach(float n in new[]{projected.x,projected.y,projected.z})Assert.That(float.IsNaN(n)||float.IsInfinity(n),Is.False,label+reason);
                Assert.That(projected.x,Is.InRange(0f,1f),label+reason+" horizontal");Assert.That(projected.y,Is.InRange(0f,1f),label+reason+" vertical");
                Assert.That(projected.z,Is.GreaterThan(camera.nearClipPlane),label+reason+" near");Assert.That(projected.z,Is.LessThan(camera.farClipPlane),label+reason+" far");
            }
            foreach(var pair in ships)
            {
                var group=pair.Value.transform;var localCorners=new System.Collections.Generic.List<Vector3>();float radius=0;
                foreach(var renderer in pair.Value.GetComponentsInChildren<Renderer>(true))
                {
                    var bounds=renderer.localBounds;
                    foreach(int corner in Enumerable.Range(0,8))
                    {
                        var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                        var world=renderer.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,sign));
                        Inside(world,pair.Key+" actualLOD");localCorners.Add(group.InverseTransformPoint(world));radius=Math.Max(radius,Vector3.Distance(world,group.position));
                    }
                }
                foreach(float t in new[]{0f,.25f,.5f,.75f,1f})
                {
                    Vector3 centre;Matrix4x4 matrix;
                    if(poses.TryGetValue(pair.Key,out var pose))
                    {var position=Vector3.Lerp(pose.Item1,pose.Item3,t);centre=root.transform.TransformPoint(position);matrix=root.transform.localToWorldMatrix*Matrix4x4.TRS(position,Quaternion.Slerp(pose.Item2,pose.Item4,t),group.localScale);}
                    else{centre=group.position;matrix=group.localToWorldMatrix;}
                    foreach(var corner in localCorners)Inside(matrix.MultiplyPoint3x4(corner),pair.Key+" independent interpolatedLOD t="+t);
                    // Independently project a rotation-invariant sphere's enclosing cube.
                    // This tests actual Camera projection, not the production fit inequalities.
                    foreach(int corner in Enumerable.Range(0,8))Inside(centre+radius*new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1),pair.Key+" rotationEnvelope t="+t);
                }
            }
            if(round!=null)foreach(var e in round.Events.Where(e=>e.Kind==CampaignPlaybackEventKind.IncomingTorpedo&&(e.SourceShipId==null||!ships.ContainsKey(e.SourceShipId))&&e.TargetShipId!=null&&ships.ContainsKey(e.TargetShipId)))
            {
                var target=ships[e.TargetShipId!].transform;
                foreach(float t in new[]{0f,.25f,.5f,.75f,1f})
                {float z=poses.TryGetValue(e.TargetShipId!,out var p)?Mathf.Lerp(p.Item1.z,p.Item3.z,t):target.localPosition.z;Inside(root.transform.TransformPoint(new Vector3(24,2,z+5))," anonymous recorded incoming cue");cues++;}
            }
            Assert.That(checkedPoints,Is.GreaterThan(0));
            foreach(float value in new[]{camera.fieldOfView,camera.aspect,camera.nearClipPlane,camera.farClipPlane,camera.orthographicSize})Assert.That(float.IsNaN(value)||float.IsInfinity(value),Is.False);
            Assert.That(camera.aspect,Is.EqualTo(1280f/600));
            if(!camera.orthographic)Assert.That(camera.fieldOfView,Is.EqualTo(35f));
            File.AppendAllText(Path.Combine(CampaignOutput(),"battle-camera-containment.jsonl"),JsonConvert.SerializeObject(new{label,camera.orthographic,camera.fieldOfView,camera.nearClipPlane,camera.farClipPlane,checkedPoints,cues,claim="actual Unity WorldToViewport of everyLOD/before/after/intermediate/sphere envelope; no pixel readability claim"})+Environment.NewLine);
        }
        private static void BattleAnonymousCameraProbe(CampaignRoundPlayback knownRound)
        {
            var target=knownRound.Before.OwnShips.First(s=>s.Hull>0);
            var incoming=new CampaignPlaybackEvent("component-anonymous-cue",knownRound.Round,0,CampaignPlaybackEventKind.IncomingTorpedo,null,null,target.ShipId,target.FleetId,TacticalWeaponClass.Torpedo,null,null,0);
            var synthetic=new CampaignRoundPlayback(knownRound.Round,knownRound.Before,knownRound.After,new[]{incoming});
            using(var stage=new ReviewBattleStage(synthetic.Before,synthetic,ReviewBattleVisualBinding.Identity))
            {
                var camera=(Camera)typeof(ReviewBattleStage).GetField("camera",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(stage)!;
                for(int mode=0;mode<2;mode++)
                {
                    Assert.That(camera.orthographic,Is.EqualTo(mode==1));BattleCameraContainment(stage,"COMPONENT synthetic anonymous cue mode"+mode);
                    var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView,near=camera.nearClipPlane,far=camera.farClipPlane,ortho=camera.orthographicSize;
                    stage.Paused=true;stage.Tick(.1f);stage.Paused=false;
                    for(int tick=0;tick<3;tick++)
                    {stage.Tick(.1f);Assert.That(camera.transform.position,Is.EqualTo(position));Assert.That(camera.transform.rotation,Is.EqualTo(rotation));Assert.That(camera.fieldOfView,Is.EqualTo(fov));Assert.That(camera.nearClipPlane,Is.EqualTo(near));Assert.That(camera.farClipPlane,Is.EqualTo(far));Assert.That(camera.orthographicSize,Is.EqualTo(ortho));}
                    BattleCameraContainment(stage,"COMPONENT synthetic anonymous cue after controlledTick mode"+mode);if(mode==0)stage.ToggleCamera();
                }
            }
        }
        private static void BattleLayoutAudit(UIDocument doc,Vector2Int size,string state)
        {
            try
            {
                object Box(Rect r)=>new{x=r.x,y=r.y,width=r.width,height=r.height};
                var chain=new System.Collections.Generic.List<object>();
                for(VisualElement? element=doc.rootVisualElement.Q<Image>("battle-3d-image");element!=null;element=element.parent)
                {
                    var s=element.resolvedStyle;chain.Add(new{element.name,classes=string.Join(" ",element.GetClasses()),layout=Box(element.layout),content=Box(element.contentRect),world=Box(element.worldBound),
                        s.flexGrow,s.flexShrink,basis=s.flexBasis.ToString(),height=s.height.ToString(),minHeight=s.minHeight.ToString(),display=s.display.ToString(),
                        padding=new{s.paddingLeft,s.paddingRight,s.paddingTop,s.paddingBottom},border=new{s.borderLeftWidth,s.borderRightWidth,s.borderTopWidth,s.borderBottomWidth},margin=new{s.marginTop,s.marginBottom}});
                }
                var controls=doc.rootVisualElement.Query<Button>().ToList().Where(b=>b.name.StartsWith("combat-",StringComparison.Ordinal)||b.text.StartsWith("Review / commit round ",StringComparison.Ordinal))
                    .Select(b=>new{b.name,b.text,b.enabledInHierarchy,world=Box(b.worldBound),display=b.resolvedStyle.display.ToString()}).ToArray();
                File.AppendAllText(Path.Combine(CampaignOutput(),"battle-ancestor-layout.jsonl"),JsonConvert.SerializeObject(new{state,size.x,size.y,chain,controls})+Environment.NewLine);
            }
            catch(Exception error){CampaignDiagnosticError("battle-layout-audit",error);}
        }
        private static void BattleVisible(VisualElement control,Vector2Int size)
        {
            Assert.That(control.panel,Is.Not.Null,control.name);var r=control.worldBound;
            foreach(var n in new[]{r.x,r.y,r.width,r.height})Assert.That(float.IsNaN(n)||float.IsInfinity(n),Is.False,control.name);
            Assert.That(control.resolvedStyle.display,Is.Not.EqualTo(DisplayStyle.None));Assert.That(r.width,Is.GreaterThan(1));Assert.That(r.height,Is.GreaterThan(1));
            Assert.That(r.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(r.yMin,Is.GreaterThanOrEqualTo(0));Assert.That(r.xMax,Is.LessThanOrEqualTo(size.x));Assert.That(r.yMax,Is.LessThanOrEqualTo(size.y));
            var hit=control.panel.Pick(r.center);Assert.That(hit==control||(hit!=null&&control.Contains(hit)),Is.True,"actual panel hit at "+control.name);
        }
        private static IEnumerator BattleSized(ReviewEntry entry,UIDocument doc,Vector2Int size,string state,bool scene,bool? desiredOverview=null,bool? desiredPaused=null)
        {
            var old=doc.panelSettings.targetTexture;var w=doc.rootVisualElement.style.width;var h=doc.rootVisualElement.style.height;var active=RenderTexture.active;
            var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);Texture2D? pixels=null;bool complete=false;
            try
            {
                Assert.That(target.Create(),Is.True);doc.panelSettings.targetTexture=target;doc.rootVisualElement.style.width=size.x;doc.rootVisualElement.style.height=size.y;
                yield return null;yield return null;yield return null;
                if(scene&&(desiredOverview.HasValue||desiredPaused.HasValue))
                {
                    // Geometry rebuilds the panel. Operate ONLY on fresh actual controls
                    // after that sized surface is laid out, never a retained old stage.
                    var current=BattleStage(doc);var camera=(Camera)typeof(ReviewBattleStage).GetField("camera",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(current)!;
                    if(desiredOverview.HasValue&&camera.orthographic!=desiredOverview.Value)Click(CampaignButton(doc,"Overview / cinematic"));
                    if(desiredPaused.HasValue&&BattleStage(doc).Paused!=desiredPaused.Value)Click(doc.rootVisualElement.Q<Button>("battle-playback-pause"));
                    yield return null;yield return null;
                }
                BattleLayoutAudit(doc,size,state);
                // Preserve this SAME intended state before asserting its bounds.
                // A failed layout remains failed; outcome metadata distinguishes the PNG.
                RenderTexture.active=target;pixels=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"battle-v2-"+state+"-"+size.x+"x"+size.y+".png"),pixels.EncodeToPNG());
                var rail=doc.rootVisualElement.Q("central-combat-surface").Q(className:"review-order-dock");BattleVisible(rail,size);
                foreach(var id in new[]{"combat-tab-battle","combat-tab-stocks","combat-tab-report","combat-tab-inspection","combat-toggle-fleet-details"})
                {var item=doc.rootVisualElement.Q(id);if(item!=null)BattleVisible(item,size);}
                foreach(var action in doc.rootVisualElement.Query<Button>().ToList().Where(b=>b.text.StartsWith("Review / commit round ",StringComparison.Ordinal)||b.name=="combat-skip-pending-playback"))BattleVisible(action,size);
                if(scene)
                {
                    var current=BattleStage(doc);var camera=(Camera)typeof(ReviewBattleStage).GetField("camera",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(current)!;
                    if(desiredOverview.HasValue)Assert.That(camera.orthographic,Is.EqualTo(desiredOverview.Value),"actual mode on current sized panel");
                    if(desiredPaused.HasValue)Assert.That(current.Paused,Is.EqualTo(desiredPaused.Value),"actual pause on current sized panel");
                    BattleCameraContainment(current,state+"-"+size.x);
                    var image=doc.rootVisualElement.Q<Image>("battle-3d-image");Assert.That(image,Is.Not.Null);BattleVisible(image,size);
                    Assert.That(image.image,Is.TypeOf<RenderTexture>());Assert.That(image.image.width,Is.EqualTo(1280));Assert.That(image.image.height,Is.EqualTo(600));
                    float scale=Math.Min(image.contentRect.width/1280,image.contentRect.height/600);float paintedWidth=1280*scale,paintedHeight=600*scale;
                    File.AppendAllText(Path.Combine(CampaignOutput(),"battle-layout.jsonl"),JsonConvert.SerializeObject(new{state,size.x,size.y,Widget=new{image.worldBound.x,image.worldBound.y,image.worldBound.width,image.worldBound.height},paintedWidth,paintedHeight,scaleMode=image.scaleMode.ToString()})+Environment.NewLine);
                    if(size.x==1280)Assert.That(paintedHeight,Is.GreaterThanOrEqualTo(220),"actual ScaleToFit scene, not black surround");
                    Assert.That(image.worldBound.Overlaps(rail.worldBound),Is.False,"scene/order rail nonoverlap");
                    BattleVisible(doc.rootVisualElement.Q("battle-playback-pause"),size);
                    foreach(var control in doc.rootVisualElement.Q("resolved-battle-presentation").Query<Button>().ToList())BattleVisible(control,size);
                }
                complete=true;
            }
            finally
            {
                try{File.AppendAllText(Path.Combine(CampaignOutput(),"battle-capture-outcomes.jsonl"),JsonConvert.SerializeObject(new{state,size.x,size.y,assertionsPassed=complete,imageWritten=pixels!=null,claim="Same intended normal state; PNG is not a passing-layout claim"})+Environment.NewLine);}catch(Exception error){CampaignDiagnosticError("battle-capture-outcome",error);}
                if(!complete)V3Audit(entry,"battle-capture-failed-"+state);
                doc.panelSettings.targetTexture=old;doc.rootVisualElement.style.width=w;doc.rootVisualElement.style.height=h;RenderTexture.active=active;
                target.Release();UnityEngine.Object.Destroy(target);if(pixels!=null)UnityEngine.Object.Destroy(pixels);
            }
            yield return null;yield return null;yield return null;
        }
        private static void BattleMaterialProbe(ReviewBattleStage stage)
        {
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;var shader=Resources.Load<Shader>("ArtDemo/PreviewStudio");Assert.That(shader,Is.Not.Null);
            var a=new Material(shader);var b=new Material(shader);var host=new GameObject("test-only source material probe");host.SetActive(false);
            var renderer=host.AddComponent<MeshRenderer>();var hdr=new Color(2.4f,.7f,.2f,1);var colour=new Color(.3f,.4f,.5f,1);Material? nativeEffectBaseline=null;
            try
            {
                a.SetColor("_BaseColor",colour);a.SetColor("_EmissionColor",hdr);a.SetFloat("_Metallic",.8f);a.SetFloat("_Smoothness",.15f);
                b.SetColor("_BaseColor",colour);b.SetColor("_EmissionColor",Color.black);b.SetFloat("_Metallic",.1f);b.SetFloat("_Smoothness",.85f);
                float[] Channels(Color c)=>new[]{c.r,c.g,c.b,c.a};
                string Snapshot(Material m)=>JsonConvert.SerializeObject(new{Base=Channels(m.GetColor("_BaseColor")),Emission=Channels(m.GetColor("_EmissionColor")),Metallic=m.GetFloat("_Metallic"),Smoothness=m.GetFloat("_Smoothness"),Shader=m.shader.name});
                var beforeA=Snapshot(a);var beforeB=Snapshot(b);renderer.sharedMaterials=new[]{a,b,a};
                typeof(ReviewBattleStage).GetMethod("Materials",flags)!.Invoke(stage,new object[]{host,false});var clones=renderer.sharedMaterials;
                Assert.That(clones[0],Is.Not.SameAs(a));Assert.That(clones[0],Is.SameAs(clones[2]));Assert.That(clones[0],Is.Not.SameAs(clones[1]));
                Assert.That(clones[0].GetColor("_EmissionColor"),Is.EqualTo(hdr));Assert.That(clones[0].GetFloat("_Metallic"),Is.EqualTo(.8f));Assert.That(clones[0].GetFloat("_Smoothness"),Is.EqualTo(.15f));
                Assert.That(clones[1].GetFloat("_Smoothness"),Is.EqualTo(.85f));Assert.That(Snapshot(a),Is.EqualTo(beforeA));Assert.That(Snapshot(b),Is.EqualTo(beforeB));
                var surface=typeof(ReviewBattleStage).GetMethod("Surface",BindingFlags.Static|BindingFlags.NonPublic)!;
                Assert.That(surface.Invoke(null,new object?[]{null,"_Metallic",.3f}),Is.EqualTo(.3f));Assert.That(surface.Invoke(null,new object[]{a,"_Missing",.4f}),Is.EqualTo(.4f));
                a.SetFloat("_Metallic",float.NaN);Assert.That(surface.Invoke(null,new object[]{a,"_Metallic",.3f}),Is.EqualTo(.3f));
                a.SetFloat("_Metallic",2);Assert.That(surface.Invoke(null,new object[]{a,"_Metallic",.3f}),Is.EqualTo(1f));
                a.SetColor("_BaseColor",new Color(float.NaN,0,0,1));a.SetColor("_EmissionColor",new Color(float.PositiveInfinity,0,0,1));a.SetFloat("_Smoothness",float.NaN);
                var invalidBefore=Snapshot(a);renderer.sharedMaterials=new[]{a};typeof(ReviewBattleStage).GetMethod("Materials",flags)!.Invoke(stage,new object[]{host,false});
                var fallback=renderer.sharedMaterial;Assert.That(fallback.GetColor("_BaseColor"),Is.EqualTo(new Color(.62f,.67f,.72f)));Assert.That(fallback.GetColor("_EmissionColor"),Is.EqualTo(Color.black));
                Assert.That(fallback.GetFloat("_Smoothness"),Is.EqualTo(.4f));Assert.That(Snapshot(a),Is.EqualTo(invalidBefore));
                var effect=typeof(ReviewBattleStage).GetMethod("Material",flags,null,new[]{typeof(Color),typeof(bool)},null)!.Invoke(stage,new object[]{colour,true}) as Material;
                var effectShader=Resources.Load<Shader>("BattlePresentation/Effect");nativeEffectBaseline=new Material(effectShader);nativeEffectBaseline.SetColor("_BaseColor",colour);
                var baselineColour=nativeEffectBaseline.GetColor("_BaseColor");var actualColour=effect!.GetColor("_BaseColor");
                int[] Bits(Color value)=>Channels(value).Select(n=>BitConverter.ToInt32(BitConverter.GetBytes(n),0)).ToArray();
                object Exact(Color value)=>new{roundTrip=Channels(value).Select(n=>n.ToString("R",System.Globalization.CultureInfo.InvariantCulture)).ToArray(),rawBits=Bits(value).Select(n=>n.ToString("X8",System.Globalization.CultureInfo.InvariantCulture)).ToArray()};
                File.WriteAllText(Path.Combine(CampaignOutput(),"battle-effect-native-colour-roundtrip.json"),JsonConvert.SerializeObject(new{requested=Exact(colour),baseline=Exact(baselineColour),actual=Exact(actualColour),shader=effectShader.name,property="_BaseColor",baselineHasProperty=nativeEffectBaseline.HasProperty("_BaseColor"),actualHasProperty=effect.HasProperty("_BaseColor"),claim="Independent fresh Material native Set/Get oracle; exact RGBA float bits, no epsilon"}));
                Assert.That(effect.shader,Is.SameAs(effectShader));CollectionAssert.AreEqual(Bits(baselineColour),Bits(actualColour),"adapter must preserve native property roundtrip exactly");
                Assert.That(effect.HasProperty("_EmissionColor"),Is.False,"existing unlit effect consumes BaseColor directly");
                File.WriteAllText(Path.Combine(CampaignOutput(),"battle-material-probe.json"),JsonConvert.SerializeObject(new{authoredHdr=hdr.r,cacheDistinct=true,sourceUnchanged=true,nullMissingNonfiniteFallback=true,effectsPreserved=true,claim="isolated component receiving, source assets read only"}));
            }
            finally{UnityEngine.Object.Destroy(host);UnityEngine.Object.Destroy(a);UnityEngine.Object.Destroy(b);if(nativeEffectBaseline!=null)UnityEngine.Object.Destroy(nativeEffectBaseline);}
        }
        [UnityTest,Timeout(600000)] public IEnumerator PlayableBattleLayoutAndAuthoredMaterialsUseActualControls()
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Battle-V2-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var storePath=Path.Combine(directory,"campaign");typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,storePath);
            var codec=new ReviewNewtonsoftCodec();var store=new ReviewStaticAstronomyCampaignSlotStore(storePath,codec);
            var pending=V3Fixture("HOBBYDS9_V3_COMBAT","e3be33bf1fae4bcf9de27a73aef4b628c8419e0c5194f806cf39684ab32e46bc");store.Write("generated-3",codec.DecodeStaticAstronomyCampaignRoot(pending),false);
            try
            {
                using(var surface=new GalaxyInputSurface(doc))
                {
                    Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-3"));Confirm(doc);yield return WaitCampaign(entry,"battle-v2-labelled-pending68");
                    var model=CampaignModel(entry);Assert.That(model.StaticAstronomyCampaign,Is.True);Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.AwaitingCombat));
                    CampaignPage(doc,ReviewWorkspacePage.Combat);yield return null;yield return null;
                    Assert.That(doc.rootVisualElement.Q("combat-tab-battle").ClassListContains("selected-tab"),Is.True);
                    Assert.That(doc.rootVisualElement.Q("central-fleet-rail").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
                    Assert.That(((Camera)typeof(ReviewBattleStage).GetField("camera",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(BattleStage(doc))!).orthographic,Is.False,"new encounter defaults to Cinematic");
                    foreach(var size in V3Sizes)yield return BattleSized(entry,doc,size,"initial",true,false); //3
                    var boundary=codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model));
                    var panel=doc.rootVisualElement.Q<ReviewBattlePanel>();var stage=(ReviewBattleStage)typeof(ReviewBattlePanel).GetField("stage",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(panel)!;BattleMaterialProbe(stage);
                    var oldResources=((System.Collections.Generic.List<UnityEngine.Object>)typeof(ReviewBattleStage).GetField("owned",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(stage)!).ToArray();
                    // Actual local draft, then inspection/roster redraws must preserve it and authority.
                    var maneuver=doc.rootVisualElement.Q<DropdownField>("combat-maneuver-picker");int standingManeuver=maneuver.index;maneuver.index=(maneuver.index+1)%maneuver.choices.Count;
                    Click(CampaignButton(doc,"Apply to local draft"));var draft=JsonConvert.SerializeObject(model.Current.Draft);
                    Click(doc.rootVisualElement.Q<Button>("combat-toggle-fleet-details"));yield return null;yield return null;
                    var oldCache=(System.Collections.IDictionary)typeof(ReviewBattleStage).GetField("materialCache",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(stage)!;
                    Assert.That(oldCache.Count,Is.Zero,"replaced presentation disposes its material cache");
                    Assert.That(oldResources.All(item=>item==null),Is.True,"owned material/effect resources destroyed after detach");
                    var ship=doc.rootVisualElement.Q<DropdownField>("combat-ship-picker");Assert.That(ship,Is.Not.Null);if(ship.choices.Count>1)ship.index=1;
                    Click(CampaignButton(doc,"Inspect ship"));yield return null;yield return null;
                    Assert.That(JsonConvert.SerializeObject(model.Current.Draft),Is.EqualTo(draft));Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model)),Is.EqualTo(boundary));
                    yield return BattleSized(entry,doc,V3Sizes[0],"inspection-expanded",false); //1
                    Click(doc.rootVisualElement.Q<Button>("combat-tab-stocks"));Click(doc.rootVisualElement.Q<Button>("combat-toggle-fleet-details"));yield return null;yield return null;
                    Assert.That(doc.rootVisualElement.Q("combat-tab-stocks").ClassListContains("selected-tab"),Is.True,"same boundary redraw retains user tab");
                    Assert.That(JsonConvert.SerializeObject(model.Current.Draft),Is.EqualTo(draft));yield return BattleSized(entry,doc,V3Sizes[0],"status-retained",false); //1
                    CampaignPage(doc,ReviewWorkspacePage.Saves);Click(doc.rootVisualElement.Q<Button>("save-slot-generated-3"));yield return null;yield return null;
                    var filePath=Path.Combine(storePath,"unity-generated-static-astronomy-v2-generated-3.json");var fileBefore=File.ReadAllBytes(filePath);
                    var frameBefore=model.Current.Frame;var rootBefore=codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model));var draftBefore=JsonConvert.SerializeObject(model.Current.Draft);
                    var promptWatch=System.Diagnostics.Stopwatch.StartNew();Click(doc.rootVisualElement.Q<Button>("player-save"));var prompt=doc.rootVisualElement.Q("review-one-use-confirmation");promptWatch.Stop();
                    Assert.That(prompt,Is.Not.Null);Assert.That(prompt.Query<Label>().ToList().Any(l=>l.text.Contains("Replace the save in generated-3?")),Is.True,"captured requested slot, not the empty default slot1");
                    File.WriteAllText(Path.Combine(CampaignOutput(),"battle-occupied-slot-prompt.json"),JsonConvert.SerializeObject(new{elapsedMs=promptWatch.Elapsed.TotalMilliseconds,slot="generated-3",measurement="synchronous Click to actual modal query ONLY; private audit comparisons outside timer",exportCounter="No shipping test-only counter added; no-export path separately source/CPU reviewed"}));
                    Assert.That(promptWatch.Elapsed.TotalSeconds,Is.LessThan(1),"occupied-slot advisory must not run the expensive export/replay before asking");
                    Confirm(doc,false);yield return null;yield return null;
                    Assert.That(model.Current.Frame,Is.SameAs(frameBefore));Assert.That(JsonConvert.SerializeObject(model.Current.Draft),Is.EqualTo(draftBefore));
                    Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model)),Is.EqualTo(rootBefore));Assert.That(File.ReadAllBytes(filePath),Is.EqualTo(fileBefore));
                    Assert.That(File.Exists(Path.Combine(storePath,"unity-generated-static-astronomy-v2-generated-1.json")),Is.False,"no accidental save to default slot");
                    CampaignPage(doc,ReviewWorkspacePage.Combat);yield return null;yield return null;
                    // Restore the accepted fixture's standing maneuver through the actual UI;
                    // the inspection regression above still exercised a genuinely edited draft.
                    doc.rootVisualElement.Q<DropdownField>("combat-maneuver-picker").index=standingManeuver;Click(CampaignButton(doc,"Apply to local draft"));
                    Click(doc.rootVisualElement.Q<Button>("combat-tab-battle"));int rounds=0;
                    while(!model.Current.Frame.Battle!.Complete)
                    {
                        Assert.That(++rounds,Is.LessThanOrEqualTo(16));int round=model.Current.Frame.Battle.Round;
                        Click(CampaignButton(doc,"Review / commit round "+round));Confirm(doc);yield return null;yield return null;
                        byte[]? rootBeforeCamera=null;
                        if(rounds==1)
                        {
                            rootBeforeCamera=codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model));
                            BattleAnonymousCameraProbe(entry.Presentation!.PendingPlayback!);
                            for(int i=0;i<V3Sizes.Length;i++)
                                yield return BattleSized(entry,doc,V3Sizes[i],"resolved-playback",true,i==1,true); //3: Cinematic/Overview/Cinematic, actual fresh controls after each layout
                            Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model)),Is.EqualTo(rootBeforeCamera),"actual camera/pause controls cannot change accepted campaign");
                            // Returning to the live surface can rebuild once more. Reacquire
                            // and exercise resume on that current stage through the UI.
                            if(!BattleStage(doc).Paused)Click(doc.rootVisualElement.Q<Button>("battle-playback-pause"));Assert.That(BattleStage(doc).Paused,Is.True);
                            Click(doc.rootVisualElement.Q<Button>("battle-playback-pause"));Assert.That(BattleStage(doc).Paused,Is.False);
                        }
                        var accepted=model.Current.Frame;Click(doc.rootVisualElement.Q<Button>("combat-skip-pending-playback"));Assert.That(model.Current.Frame,Is.SameAs(accepted));yield return null;yield return null;
                        if(rootBeforeCamera!=null)Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model)),Is.EqualTo(rootBeforeCamera),"actual skip/acknowledgment cannot change accepted campaign");
                        if(rounds==1)yield return BattleSized(entry,doc,V3Sizes[0],"round-report",false); //1
                        if(!model.Current.Frame.Battle.Complete)Click(CampaignButton(doc,"Plan next round"));
                    }
                    Assert.That(rounds,Is.EqualTo(3),"accepted fixed B68 standing-order fixture");
                    yield return BattleSized(entry,doc,V3Sizes[1],"final-report",false); //1
                    Click(CampaignButton(doc,"Confirm campaign aftermath"));Confirm(doc);yield return null;yield return null;
                    Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning));Assert.That(model.History().Count,Is.GreaterThan(0));yield return BattleSized(entry,doc,V3Sizes[0],"aftermath",false); //1
                    CampaignPage(doc,ReviewWorkspacePage.Saves);Click(doc.rootVisualElement.Q<Button>("save-slot-generated-3"));Click(doc.rootVisualElement.Q<Button>("player-save"));if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);
                    Assert.That(model.Message,Is.EqualTo("Checkpoint result: None"));var saved=codec.EncodeStaticAstronomyCampaignRoot(store.Read("generated-3"));Click(doc.rootVisualElement.Q<Button>("demo-menu"));Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-3"));Confirm(doc);yield return WaitCampaign(entry,"battle-v2-continue-aftermath");
                    Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(CampaignModel(entry))),Is.EqualTo(saved));CampaignPage(doc,ReviewWorkspacePage.Combat);Click(doc.rootVisualElement.Q<Button>("combat-tab-report"));yield return null;
                    Assert.That(doc.rootVisualElement.Query<Label>().ToList().Any(l=>l.text.StartsWith("Battle statistics",StringComparison.Ordinal)&&l.text.Contains("turn 68")),Is.True,"actual reloaded historical statistics, not merely a reachable empty tab");
                    yield return BattleSized(entry,doc,V3Sizes[1],"continued-history",false); //1 =>12
                    Assert.That(Directory.GetFiles(CampaignOutput(),"battle-v2-*.png").Length,Is.EqualTo(12));
                    Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;yield return null;
                    Assert.That(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Any(c=>c.name=="Battle camera"),Is.False,"detached stages release even their normally inactive owned objects after deferred destruction");
                }
            }
            finally{V3Audit(entry,"battle-finally");}
            yield return new ExitPlayMode();
        }
    }
}
