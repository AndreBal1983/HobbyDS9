#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        [UnityTest,Timeout(720000)]
        public IEnumerator ColonisationCycleSavedVisibleFamilyRenderingAndPresentationPurity()
        {
            var t=new CycleReceivingState();var s=t.Case;var stack=new Stack<IEnumerator>();
            stack.Push(VisibleFamilyBody(t));Exception? failure=null;
            while(stack.Count>0)
            {
                bool next=false;object? value=null;var current=stack.Peek();
                try
                {
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Cycle visible-family deadline before "+t.Phase);
                    next=current.MoveNext();if(next)value=current.Current;CycleDiscover(t);
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Cycle visible-family deadline after "+t.Phase);
                    if(t.Flushed<s.Shared.Timings.Count)t.Flush();
                }
                catch(Exception error)
                {
                    failure=error;
                    try{CycleDiscover(t);}catch(Exception ownershipError){try{CampaignDiagnosticError("cycle-visible-family-ownership",ownershipError);}catch{}}
                }
                if(failure!=null)break;
                if(!next){stack.Pop();try{(current as IDisposable)?.Dispose();}catch(Exception error){failure=error;break;}continue;}
                if(value is IEnumerator nested){stack.Push(nested);continue;}
                yield return value;
            }
            t.Productive.Stop();
            void Cleanup(string phase,Action action)
            {try{action();}catch(Exception error){failure=failure??error;try{CampaignDiagnosticError(phase,error);}catch{}}}
            Cleanup("cycle-visible-family-release-all",()=>{var error=s.ReleaseAll();failure=failure??error;});
            Cleanup("cycle-visible-family-shared-release",()=>s.Shared.Release.Set());
            if(s.Shared.Entry!=null)Cleanup("cycle-visible-family-disable",()=>s.Shared.Entry.enabled=false);
            while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){failure=failure??error;}
            bool settled=true;var drain=System.Diagnostics.Stopwatch.StartNew();
            bool Pending(){bool pending=true;Cleanup("cycle-visible-family-drain",()=>pending=CyclePending(t));return pending;}
            while(Pending())
            {
                if(drain.Elapsed.TotalSeconds>=90)
                {settled=false;directory=null;failure=failure??new TimeoutException("Cycle visible-family owners unsettled; preserve temporary override/namespace.");break;}
                yield return null;
            }
            Cleanup("cycle-visible-family-timings",t.Flush);bool overrideRestored=false;
            if(settled&&s.Shared.Entry!=null&&s.PriorDirectoryCaptured&&t.OverrideAssigned)
                Cleanup("cycle-visible-family-restore-override",()=>{typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!.SetValue(s.Shared.Entry,s.Shared.PriorDirectory);overrideRestored=true;});
            bool directoryRetained=!settled||t.OverrideAssigned&&!overrideRestored;
            if(directoryRetained)directory=null;
            Cleanup("cycle-visible-family-cleanup-receipt",()=>File.WriteAllText(Path.Combine(CampaignOutput(),"cycle-visible-family-cleanup.json"),JsonConvert.SerializeObject(new{
                settled,overrideRestored,directoryRetained,priorOverrideCaptured=s.PriorDirectoryCaptured,overrideAssigned=t.OverrideAssigned,
                trackedModels=t.Models.Count,trackedHosts=s.Hosts.Count,trackedContexts=s.Contexts.Count,
                allReleasesSet=s.Holds.All(h=>h.Release.IsSet),entryDisabled=s.Shared.Entry==null||!s.Shared.Entry.enabled,
                productiveMs=t.Productive.Elapsed.TotalMilliseconds,cleanupMs=drain.Elapsed.TotalMilliseconds,
                productiveLimitSeconds=600,drainLimitSeconds=90,outerFrameworkSeconds=720,failed=failure!=null,lastPhase=t.Phase})));
            if(settled)foreach(var host in s.Hosts)Cleanup("cycle-visible-family-remove-probe",()=>ColonyProbe(host,null));
            if(!s.Pending)foreach(var h in s.Holds){Cleanup("cycle-visible-family-dispose-entered",()=>h.Entered.Dispose());Cleanup("cycle-visible-family-dispose-release",()=>h.Release.Dispose());}
            if(s.Shared.Holder==null||s.Shared.Holder.IsCompleted)
            {Cleanup("cycle-visible-family-dispose-shared-entered",()=>s.Shared.Entered.Dispose());Cleanup("cycle-visible-family-dispose-shared-release",()=>s.Shared.Release.Dispose());}
            yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }


        private IEnumerator VisibleFamilyBody(CycleReceivingState t)
        {
            var s=t.Case;yield return new EnterPlayMode();yield return null;yield return null;
            var bytes=V3Fixture("HOBBYDS9_COLONISATION_CYCLE_COMBAT_WAITING",CycleCombatFixtureSha);
            Assert.That(bytes.Length,Is.EqualTo(1358829),"whole genuine fixture checked before decode");
            var codec=new ReviewNewtonsoftCodec();var fixture=codec.DecodeCycleRoot(bytes);Assert.That(codec.EncodeCycleRoot(fixture),Is.EqualTo(bytes));
            Assert.That(fixture.FormatVersion,Is.EqualTo(14));Assert.That(fixture.ProfileId,Is.EqualTo("generated-colonisation-cycle-v1"));
            Assert.That(fixture.PackageHash,Is.EqualTo("34aba41a024a8c3e37c3bd24922ad2b0b47fcab3dffb5cd9d049d99e300dae75"));
            s.Shared.Entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(s.Shared.Entry,Is.Not.Null);var entry=s.Shared.Entry!;
            CycleDiscover(t);var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!;
            s.Shared.PriorDirectory=(string?)field.GetValue(entry);s.PriorDirectoryCaptured=true;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Cycle-VisibleFamily-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var storePath=Path.Combine(directory,"campaign");field.SetValue(entry,storePath);t.OverrideAssigned=true;
            var resolved=(string)typeof(ReviewEntry).GetMethod("ColonisationCycleCampaignDirectory",HullPrivate)!.Invoke(entry,null)!;
            Assert.That(Path.GetFullPath(resolved),Is.EqualTo(Path.GetFullPath(storePath)));
            var store=new ReviewColonisationCycleSlotStore(resolved,codec);store.Write("generated-2",fixture,false);
            Assert.That(File.ReadAllBytes(Path.Combine(resolved,"unity-campaign-colonisation-cycle-v1-generated-2.json")),Is.EqualTo(bytes));
            var doc=entry.GetComponent<UIDocument>();
            using(var surface=new GalaxyInputSurface(doc))
            {
                var prior=ColonyModel(s);var oldController=prior.Current;var oldHost=StreamHost(prior);
                doc.rootVisualElement.Q<Foldout>("colonisation-cycle-campaign-menu").value=true;yield return null;
                var load=doc.rootVisualElement.Q<Button>("colonisation-cycle-campaign-continue-generated-2");yield return ColonyReach(doc,load,target:"visible family saved Cycle Continue2");
                yield return CycleOperation(t,doc,"visible-family-continue","Loading",()=>{Click(load);Confirm(doc);});
                var model=ColonyModel(s);var actor=model.Current.Frame.Actor;
                Assert.That(model,Is.Not.SameAs(prior));Assert.That(model.Current,Is.Not.SameAs(oldController));Assert.That(StreamHost(model),Is.Not.SameAs(oldHost));
                Assert.That(model.ColonisationCycleCampaign,Is.True);Assert.That(actor,Is.EqualTo("civ.federation"));
                Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(69));Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.AwaitingCombat));
                Assert.That(CycleBytes(model),Is.EqualTo(bytes),"full public typed replay ingress");
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-visible-family-ingress-private.json"),CycleBytes(model));
                var imported=HullImportedMaterials();
                // This is a separately labelled COMPONENT probe, never the real enemy render proof.
                t.Mark("visible-family-component-probe");VisibleFamilyStagePreservesConstructorAbiMembershipUnknownFallbackAndLifetime();
                Assert.That(CycleBytes(model),Is.EqualTo(bytes));HullSourcesUnchanged(imported);
                CycleActivate(t,()=>CampaignPage(doc,ReviewWorkspacePage.Combat));yield return null;yield return null;
                Assert.That(model.Current.Frame.Battle,Is.Not.Null);var battle=model.Current.Frame.Battle!;
                Assert.That(model.Current.Frame.Encounters,Does.Contain(battle.EncounterId));Assert.That(battle.OwnShips,Is.Not.Empty);
                // Complete roots are assertions only; the action comes from this actual public own battle.
                var physical=CycleAudit(model).BaseCampaign!.Campaign!.Integrated!.Physical!;
                var request=physical.Requests.Single(r=>r.EncounterId==battle.EncounterId);
                Assert.That(request.Participants.Any(p=>p.CivilizationId==actor),Is.True);
                Assert.That(request.Participants.Any(p=>physical.Core.Campaign.GameState.Civilizations!.Single(c=>c.CivilizationId==p.CivilizationId).ControllerKind==ControllerKind.AI),Is.True);
                var order=battle.StandingOrders.Where(o=>o.CanOrder).ToArray();Assert.That(order.Length,Is.EqualTo(1));
                var maneuver=doc.rootVisualElement.Q<DropdownField>("combat-maneuver-picker");yield return ColonyReach(doc,maneuver);
                int hold=battle.LegalManeuvers.ToList().IndexOf(CampaignFleetManeuver.Hold);
                Assert.That(hold,Is.GreaterThanOrEqualTo(0));Assert.That(hold,Is.LessThan(maneuver.choices.Count));
                Assert.That(maneuver.choices[hold],Is.EqualTo(ReviewDemoCombatText.Maneuver(CampaignFleetManeuver.Hold.ToString())));
                CycleActivate(t,()=>maneuver.index=hold);yield return null;
                yield return CycleCombatClick(t,doc,CampaignButton(doc,"Apply to local draft"));
                Assert.That(model.Current.Draft.Single(d=>d.Fleet==order[0].FleetId).Maneuver,Is.EqualTo(CampaignFleetManeuver.Hold));
                Assert.That(entry.Presentation!.CanCommitRound,Is.True);
                yield return CycleCombatClick(t,doc,CampaignButton(doc,"Review / commit round "+battle.Round),true);
                Assert.That(model.Current.Frame.Actor,Is.EqualTo(actor));Assert.That(entry.Presentation.PendingPlayback,Is.Not.Null);
                Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(69));Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.AwaitingCombat));
                var pending=entry.Presentation.PendingPlayback!;var accepted=CycleBytes(model);var frame=model.Current.Frame;
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-visible-family-committed-private.json"),accepted);
                yield return CycleCombatClick(t,doc,doc.rootVisualElement.Q<Button>("combat-tab-battle"));
                yield return VisibleFamilySized(t,doc,pending.Before,accepted,model,imported);
                Assert.That(CycleBytes(model),Is.EqualTo(accepted));
                yield return CycleCombatClick(t,doc,doc.rootVisualElement.Q<Button>("combat-skip-pending-playback"));
                Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(model.Current.Frame.Actor,Is.EqualTo(actor));Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(69));
                Assert.That(CycleBytes(model),Is.EqualTo(accepted),"public playback acknowledgment is presentation/flow only");
                HullSourcesUnchanged(imported);
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-visible-family-after-presentation-private.json"),CycleBytes(model));
                File.WriteAllText(Path.Combine(CampaignOutput(),"cycle-visible-family-summary.json"),JsonConvert.SerializeObject(new{
                    actor,fixtureSha=CycleCombatFixtureSha,fixtureBytes=bytes.Length,turn=model.Current.Frame.Status.Turn,ordinaryAdvances=0,committedRounds=1,
                    ingressWholeEqual=true,presentationWholeEqual=true,componentProbe="separate synthetic component case invoked once",renderedCapture="cycle-visible-family-1920.png",
                    renderedScope="new Unity family projection; unchanged Windows has not received it",foreignEngineeringVersionDisclosed=false,
                    representativeArtHullKeyMeaning="presentation selection only, never foreign engineering version",newlyBuiltEngineeringV2Proof="OPEN"}));
            }
        }

        private static void VisibleFamilyModels(ReviewBattleStage stage,CampaignPlaybackBoundary expected,Dictionary<Material,string> imported,string label)
        {
            var boundary=(CampaignPlaybackBoundary)typeof(ReviewBattleStage).GetField("boundary",HullPrivate)!.GetValue(stage)!;
            Assert.That(boundary,Is.SameAs(expected),"fresh current stage uses pending BEFORE, not a future or union boundary");
            var living=boundary.OwnShips.Concat(boundary.VisibleEnemies).Where(s=>s.Hull>0).ToArray();
            Assert.That(boundary.VisibleEnemies.Any(s=>s.Hull>0),Is.True,"nonvacuous real visible enemy");
            var models=HullInstances(stage);Assert.That(models.Keys.Cast<string>(),Is.EquivalentTo(living.Select(s=>s.ShipId)));
            var ships=(Dictionary<string,GameObject>)typeof(ReviewBattleStage).GetField("ships",HullPrivate)!.GetValue(stage)!;
            Assert.That(ships.Keys,Is.EquivalentTo(living.Select(s=>s.ShipId)));var facts=new List<object>();
            foreach(var ship in living)
            {
                bool own=boundary.OwnShips.Any(s=>s.ShipId==ship.ShipId);
                var identity=boundary.ShipIdentities.Single(i=>i.ShipId==ship.ShipId);
                string? art=own?ReviewBattleVisualBinding.OwnHullVersion(boundary,ship.ShipId):ReviewBattleVisualBinding.FamilyArtHull(ReviewBattleVisualBinding.VisibleHullFamily(boundary,ship.ShipId));
                if(!own){Assert.That(identity.OwnHullVersionId,Is.Null);Assert.That(identity.VisibleHullFamilyId,Is.Not.Null);}
                Assert.That(art,Is.Not.Null,"actual supported disclosed family/exact own hull, no alias");
                var instance=models[ship.ShipId]!;var definition=HullProperty(instance,"Definition");var root=(GameObject)HullProperty(instance,"Root");
                // Definition.HullVersionId below is representativeArtHullKey, NOT a foreign engineering version.
                Assert.That((string)HullProperty(definition,"HullVersionId"),Is.EqualTo(art));
                var catalogue=Type.GetType("HobbyDS9.ArtDemo.ActivityHullCatalog, HobbyDS9.ArtDemo",true)!;
                var approved=catalogue.GetMethod("Find")!.Invoke(null,new object[]{art!});Assert.That(approved,Is.Not.Null);
                Assert.That((string)HullProperty(definition,"VisualKey"),Is.EqualTo((string)HullProperty(approved!,"VisualKey")));
                Assert.That((string)HullProperty(definition,"ResourcePath"),Is.EqualTo((string)HullProperty(approved!,"ResourcePath")));
                Assert.That((string)HullProperty(instance,"MissingArt"),Is.Empty);Assert.That(ships[ship.ShipId].transform.GetChild(0).gameObject,Is.SameAs(root));
                var prefab=Resources.Load<GameObject>((string)HullProperty(definition,"ResourcePath"));Assert.That(prefab,Is.Not.Null);
                var importedMeshes=prefab.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();
                var actualMeshes=root.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();
                Assert.That(actualMeshes,Is.Not.Empty);Assert.That(actualMeshes.All(m=>m!=null&&m.vertexCount>0),Is.True);
                Assert.That(actualMeshes,Is.EquivalentTo(importedMeshes),"actual imported mesh instances, not presentation markers");
                var renderers=root.GetComponentsInChildren<MeshRenderer>(true);Assert.That(renderers,Is.Not.Empty);
                foreach(var renderer in renderers)
                {
                    Assert.That(renderer.sharedMaterials,Is.Not.Empty);
                    foreach(var material in renderer.sharedMaterials)
                    {
                        Assert.That(material,Is.Not.Null);Assert.That(material.shader,Is.SameAs(Resources.Load<Shader>("ArtDemo/PreviewStudio")));
                        if(art=="hull.defiant-combat.v1")
                        {
                            const string path="ArtDemo/ActivityHulls/Defiant/Textures/Defiant_Image_";
                            Assert.That(material.GetTexture("_BaseMap"),Is.SameAs(Resources.Load<Texture2D>(path+"0")));
                            Assert.That(material.GetTexture("_BumpMap"),Is.SameAs(Resources.Load<Texture2D>(path+"2")));
                            Assert.That(material.GetTexture("_MetallicRoughnessMap"),Is.SameAs(Resources.Load<Texture2D>(path+"1")));
                            Assert.That(material.GetFloat("_UseNormalMap"),Is.EqualTo(1));Assert.That(material.GetFloat("_UseMetallicRoughnessMap"),Is.EqualTo(1));
                        }
                    }
                }
                facts.Add(new{ship.ShipId,own,identity.VisibleHullFamilyId,representativeArtHullKey=art,visualKey=HullProperty(definition,"VisualKey"),meshCount=actualMeshes.Length,rendererCount=renderers.Length});
            }
            Assert.That(stage.GenericMarkers,Is.Zero);HullSourcesUnchanged(imported);
            File.AppendAllText(Path.Combine(CampaignOutput(),"cycle-visible-family-models.jsonl"),JsonConvert.SerializeObject(new{label,claim="public boundary visual family; representative art key is not foreign engineering identity",facts})+Environment.NewLine);
        }

        private static IEnumerator VisibleFamilySized(CycleReceivingState t,UIDocument doc,CampaignPlaybackBoundary expected,byte[] accepted,ReviewUiModel model,Dictionary<Material,string> imported)
        {
            var size=new Vector2Int(1920,1080);var previous=doc.panelSettings.targetTexture;var width=doc.rootVisualElement.style.width;var height=doc.rootVisualElement.style.height;var active=RenderTexture.active;
            var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);Texture2D? pixels=null;bool complete=false;
            try
            {
                Assert.That(target.Create(),Is.True);doc.panelSettings.targetTexture=target;doc.rootVisualElement.style.width=size.x;doc.rootVisualElement.style.height=size.y;
                yield return null;yield return null;yield return null;
                if(!BattleStage(doc).Paused)yield return CycleCombatClick(t,doc,doc.rootVisualElement.Q<Button>("battle-playback-pause"));
                Assert.That(BattleStage(doc).Paused,Is.True);
                yield return CycleCombatClick(t,doc,CampaignButton(doc,"Overview / cinematic"));
                Assert.That(CycleBytes(model),Is.EqualTo(accepted));
                ReviewBattleStage InspectCurrent()
                {
                    BattleLayoutAudit(doc,size,"visible-family");var current=BattleStage(doc);BattleCameraContainment(current,"visible-family1920");
                    var image=doc.rootVisualElement.Q<Image>("battle-3d-image");BattleVisible(image,size);
                    Assert.That(current.Texture.IsCreated(),Is.True);Assert.That(image.image,Is.SameAs(current.Texture));
                    VisibleFamilyModels(current,expected,imported,"current sized stage immediately before pixels");return current;
                }
                var stage=InspectCurrent();
                // No yield/action between final geometry/model audit and this read. Re-audit any unexpected replacement.
                if(!ReferenceEquals(stage,BattleStage(doc)))stage=InspectCurrent();
                Assert.That(BattleStage(doc),Is.SameAs(stage));
                RenderTexture.active=target;pixels=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-visible-family-1920.png"),pixels.EncodeToPNG());
                Assert.That(CycleBytes(model),Is.EqualTo(accepted));complete=true;
            }
            finally
            {
                try{File.WriteAllText(Path.Combine(CampaignOutput(),"cycle-visible-family-capture.json"),JsonConvert.SerializeObject(new{size.x,size.y,assertionsPassed=complete,imageWritten=pixels!=null,claim="current-stage model/layout checked before fresh pixels; written image alone is not PASS"}));}catch(Exception error){CampaignDiagnosticError("visible-family-capture-receipt",error);}
                doc.panelSettings.targetTexture=previous;doc.rootVisualElement.style.width=width;doc.rootVisualElement.style.height=height;RenderTexture.active=active;
                target.Release();UnityEngine.Object.Destroy(target);if(pixels!=null)UnityEngine.Object.Destroy(pixels);
            }
            yield return null;yield return null;yield return null;
            Assert.That(CycleBytes(model),Is.EqualTo(accepted));
        }
    }
}
