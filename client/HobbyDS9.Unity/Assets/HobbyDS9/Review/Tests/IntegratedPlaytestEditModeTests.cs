#nullable enable
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview.Tests
{
    public sealed class IntegratedPlaytestEditModeTests
    {
        private GameObject go=null!;private ReviewEntry entry=null!;
        private VisualElement Root=>go.GetComponent<UIDocument>().rootVisualElement;
        private ReviewUiModel Model=>entry.Presentation!.Model;
        private void Invoke(string method,params object[] args)=>typeof(ReviewEntry).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(entry,args);
        [SetUp] public void Setup()
        {
            Assert.That(IntegratedLaunchOptions.TryParse(new[]{"--hobbyds9-integrated-playtest","--hobbyds9-integrated-actor","civ.federation"},out var options),Is.True);
            go=new GameObject("Actual integrated Editor fixture");go.SetActive(false);entry=go.AddComponent<ReviewEntry>();entry.ConfigureIntegrated(options!);go.SetActive(true);Invoke("OnEnable");
            Assert.That(Model.Integrated,Is.True);
        }
        [TearDown] public void Cleanup(){if(entry!=null)Invoke("OnDestroy");if(go!=null)UnityEngine.Object.DestroyImmediate(go);}
        [Test] public void ExplicitIntegratedEntryStartsExistingAudioWithoutCreatingADefaultCampaign()
        {
            // Default ClientEntry.OnEnable is bypassed by the explicit integrated flag.
            // Remove any earlier test's services so they cannot mask missing startup.
            foreach(var owner in UnityEngine.Object.FindObjectsByType<AudioSource>().Select(s=>s.gameObject).Distinct().ToArray())
                if(owner.name=="HobbyDS9 Audio"||owner.name=="HobbyDS9 SFX")UnityEngine.Object.DestroyImmediate(owner);
            var output=GameObject.Find("HobbyDS9 Audio Output");if(output!=null)UnityEngine.Object.DestroyImmediate(output);
            var frame=Model.Current.Frame;Invoke("OnEnable");
            var sources=UnityEngine.Object.FindObjectsByType<AudioSource>();
            Assert.That(sources.Count(s=>s.gameObject.name=="HobbyDS9 Audio"),Is.EqualTo(2));
            Assert.That(sources.Count(s=>s.gameObject.name=="HobbyDS9 SFX"),Is.EqualTo(5));
            Assert.That(UnityEngine.Object.FindObjectsByType<AudioListener>().Count(l=>l.isActiveAndEnabled),Is.EqualTo(1));
            Assert.That(Model.Current.Frame,Is.SameAs(frame));
            Assert.That(GameObject.Find("HobbyDS9 Client"),Is.Null,"Audio must not start another default campaign.");
            Invoke("OnEnable");Assert.That(UnityEngine.Object.FindObjectsByType<AudioSource>().Length,Is.EqualTo(sources.Length));
            foreach(var owner in sources.Select(s=>s.gameObject).Distinct().ToArray())
                if(owner!=null&&(owner.name=="HobbyDS9 Audio"||owner.name=="HobbyDS9 SFX"))UnityEngine.Object.DestroyImmediate(owner);
            output=GameObject.Find("HobbyDS9 Audio Output");if(output!=null)UnityEngine.Object.DestroyImmediate(output);
        }
        [Test] public void ActualIntegratedResourcesStartOneScopedMapWithExplicitIdentity()
        {
            Assert.That(Root.Q<Label>("review-scenario-identity").text,Does.Contain("EARLY CAMPAIGN PREVIEW").And.Contain("Federation"));
            Assert.That(Model.Current.Frame.Strategy!.Civilization.CivilizationId.Value,Is.EqualTo("civ.federation"));
            Assert.That(Model.Current.Frame.Strategy.Civilization.Credits,Is.EqualTo(250));
            Assert.That(Root.Q("integrated-known-map"),Is.Not.Null);Assert.That(Root.Q("player-primary-action"),Is.Not.Null);
            Assert.That(Root.Q("map-fleet-orders-fleet.cardassian-scout"),Is.Null);
        }
        [Test] public void ActualNavigationRendersRealResearchAndProductionWithoutResettingDraft()
        {
            var controller=Model.Current;var frame=controller.Frame;Invoke("Navigate",ReviewWorkspacePage.Research);
            Assert.That(Root.Q("integrated-research-split"),Is.TypeOf<SliderInt>());
            Assert.That(Root.Q<Button>("integrated-apply-split").enabledSelf,Is.False);
            Model.ResearchInputs()!.ScienceUnits=300;Invoke("Navigate",ReviewWorkspacePage.Production);
            Assert.That(Root.Q("integrated-queue-ship-design.source-cruiser"),Is.Not.Null);
            Assert.That(Root.Q<Button>("player-primary-action").enabledSelf,Is.False);
            Invoke("Navigate",ReviewWorkspacePage.Map);Invoke("Navigate",ReviewWorkspacePage.Research);
            Assert.That(Model.Current,Is.SameAs(controller));Assert.That(Model.Current.Frame,Is.SameAs(frame));Assert.That(Model.HasLocalEdits,Is.True);
            Assert.That(Root.Q<Button>("integrated-apply-split").enabledSelf,Is.True);
        }
        [TestCase(1280,720,true)][TestCase(1920,1080,false)][TestCase(3440,1440,false)]
        public void ActualIntegratedGeometryHandlerRetainsSameFrameAndNavigation(int width,int height,bool compact)
        {
            var frame=Model.Current.Frame;
            using(var e=GeometryChangedEvent.GetPooled(Rect.zero,new Rect(0,0,width,height)))Invoke("Geometry",e);
            Assert.That(Root.ClassListContains("player-narrow"),Is.EqualTo(compact));Assert.That(Model.Current.Frame,Is.SameAs(frame));
            Assert.That(Root.Q("review-workspace-navigation"),Is.Not.Null);
            // Actual handler/tree/class checks; no runtime event dispatch/native pixel proof.
        }
        [Test] public void ActualUnityCodecRoundTripsFullIntegratedRootAndRejectsBarePhysical()
        {
            var codec=new ReviewNewtonsoftCodec();byte[] Bytes(string name)=>Resources.Load<TextAsset>("IntegratedPackages/v1/"+name).bytes;
            var package=ReviewExactPackageLoader.LoadIntegrated(Bytes("package"),Bytes("physical"),Bytes("core"),Bytes("scenario"),codec);
            var app=HobbyDS9.Application.CampaignIntegratedPlaytestApplication.StartPackage(package);
            var root=app.ExportIntegratedSave();var restored=codec.DecodeIntegratedRoot(codec.EncodeIntegratedRoot(root));
            Assert.That(restored.FormatVersion,Is.EqualTo(3));Assert.That(restored.BuiltShips,Is.Not.Null);
            Assert.That(restored.Physical!.Core.Campaign.GameState.TurnNumber,Is.EqualTo(root.Physical!.Core.Campaign.GameState.TurnNumber));
            Assert.Throws<InvalidOperationException>(()=>app.ExportSave());
            Assert.Throws<System.IO.InvalidDataException>(()=>codec.DecodeIntegratedRoot(codec.EncodeRoot(root.Physical!)));
        }
        [UnityTest] public IEnumerator RealPaidProducedShipThroughCentralCombatAndAftermathInSameEntry()
        {
            var initial=Model.Current.Frame.Strategy!.Ships.Select(s=>s.ShipId.Value).ToArray();
            Assert.That(Model.AllocateResearch(600,ReviewUiModel.NewCommandId()),Is.True);
            Assert.That(Model.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()),Is.True);
            Assert.That(Model.Current.Frame.Strategy!.Civilization.Credits,Is.EqualTo(50));
            for(int i=0;i<13;i++)Assert.That(Model.Advance(),Is.True);
            var produced=Model.Current.Frame.Strategy!.Ships.Single(s=>!initial.Contains(s.ShipId.Value));
            Invoke("Build");Invoke("Navigate",ReviewWorkspacePage.Production);yield return null;
            Assert.That(Root.Q("player-footer"),Is.Not.Null);Assert.That(Model.Current.Frame.Strategy.Ships.Any(s=>s.ShipId==produced.ShipId),Is.True);
            foreach(var actor in new[]{"civ.federation","civ.cardassian"})
            {
                Assert.That(Model.SwitchActor(actor),Is.True);var fleet=actor=="civ.federation"?"fleet.federation-scout":"fleet.cardassian-scout";
                Assert.That(Model.DraftTravel(fleet,CampaignTravelIntent.Leave,"nav.bajor",null),Is.True);Assert.That(Model.SendTravel(ReviewUiModel.NewCommandId()),Is.True);
            }
            int limit=0;while(Model.Current.Frame.Status.Phase==SessionPhase.Planning&&Model.Current.Frame.OwnStrategicFleets.Single().LocationNodeId.Value!="nav.bajor"&&limit++<5)Assert.That(Model.Advance(),Is.True);
            if(Model.Current.Frame.Status.Phase==SessionPhase.Planning)
            {
                Assert.That(Model.SwitchActor("civ.federation"),Is.True);Assert.That(Model.DraftTravel("fleet.federation-scout",CampaignTravelIntent.StayEngage,null,null),Is.True);
                Assert.That(Model.SendTravel(ReviewUiModel.NewCommandId()),Is.True);Assert.That(Model.Advance(),Is.True);
            }
            Assert.That(Model.SwitchActor("civ.federation"),Is.True);Invoke("Build");Invoke("Navigate",ReviewWorkspacePage.Combat);yield return null;
            Assert.That(Root.Q("central-combat-surface"),Is.Not.Null);Assert.That(Model.Current.Frame.Battle!.OwnShips.Any(s=>s.Condition.ShipId==produced.ShipId.Value),Is.True);
            var flow=entry.Presentation!;
            while(!Model.Current.Frame.Battle!.Complete)
                foreach(var actor in new[]{"civ.federation","civ.cardassian"})
                {Assert.That(Model.SwitchActor(actor),Is.True);if(flow.Stage==ReviewCombatStage.RoundResult)Assert.That(flow.PlanNextRound(),Is.True);Assert.That(flow.CommitRound(ReviewUiModel.NewCommandId(),true),Is.True);}
            Assert.That(flow.FinalizeAftermath(true),Is.True);Assert.That(flow.FinalizeAftermath(true),Is.False);
            Assert.That(Model.SwitchActor("civ.federation"),Is.True);Invoke("Build");Invoke("Navigate",ReviewWorkspacePage.Map);yield return null;
            Assert.That(Root.Q("integrated-known-map"),Is.Not.Null);Assert.That(Model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning));
            Assert.That(Model.Current.Frame.OwnShips.Single(s=>s.Condition.ShipId==produced.ShipId.Value).Weapons.Single(w=>w.Class==TacticalWeaponClass.Torpedo).Ammunition,Is.LessThan(7));
        }
    }
}
