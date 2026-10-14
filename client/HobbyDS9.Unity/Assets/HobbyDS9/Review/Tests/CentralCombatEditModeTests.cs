#nullable enable
using System;
using System.Collections;
using System.Reflection;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    // Actual graphics-enabled Editor/UIElements source and real Application resources, no native input.
    public sealed class CentralCombatEditModeTests
    {
        private GameObject go=null!;
        private ReviewEntry entry=null!;
        private VisualElement Root=>go.GetComponent<UIDocument>().rootVisualElement;
        [SetUp] public void Setup()
        {
            Assert.That(ReviewLaunchOptions.TryParse(new[]{"--hobbyds9-physical-review","--hobbyds9-review-profile","faster","--hobbyds9-review-actor","civ.federation"},out var options),Is.True);
            go=new GameObject("Central combat editor test");go.SetActive(false);entry=go.AddComponent<ReviewEntry>();entry.Configure(options!);go.SetActive(true);
            // Ordinary MonoBehaviours do not receive their runtime OnEnable in EditMode.
            InvokeEntry("OnEnable");
            Assert.That(entry.Presentation,Is.Not.Null);
        }
        private void InvokeEntry(string method,params object[] args)=>typeof(ReviewEntry).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(entry,args);
        private void ShowCombat(){Assert.That(entry.Workspace!.Navigate(ReviewWorkspacePage.Combat),Is.True);InvokeEntry("Build");}
        [TearDown] public void Cleanup(){if(entry!=null)InvokeEntry("OnDestroy");if(go!=null)UnityEngine.Object.DestroyImmediate(go);}
        [Test] public void ActualOptInEntryExposesCentralSurfaceScenarioIdentityAndPersistentFooter()
        {
            ShowCombat();
            Assert.That(Root.Q<Label>("review-scenario-identity").text,Does.Contain("OPT-IN PHYSICAL TACTICAL V2").And.Contain("faster"));
            Assert.That(Root.Q("central-combat-surface"),Is.Not.Null);Assert.That(Root.Q("central-combat-footer"),Is.Not.Null);
            Assert.That(Root.Q("central-order-dock"),Is.Not.Null);
            Assert.That(Root.Q("central-combat-details"),Is.TypeOf<ScrollView>());
            for(var parent=Root.Q("central-combat-footer").parent;parent!=null;parent=parent.parent)
                Assert.That(parent,Is.Not.TypeOf<ScrollView>(),"Primary footer must not be buried in scrollable combat details.");
        }
        [TestCase(1280,720,true)][TestCase(1920,1080,false)][TestCase(3440,1440,false)]
        public void ActualGeometryPolicySelectsResponsiveClassWithoutCommands(int width,int height,bool compact)
        {
            ShowCombat();
            var frame=entry.Presentation!.Model.Current.Frame;
            // The EditMode UIDocument has no runtime panel event loop. Invoke the registered
            // actual geometry handler with an actual pooled event; do not claim live dispatch.
            using(var e=GeometryChangedEvent.GetPooled(Rect.zero,new Rect(0,0,width,height)))
                typeof(ReviewEntry).GetMethod("Geometry",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(entry,new object[]{e});
            Assert.That(Root.ClassListContains("review-compact"),Is.EqualTo(compact));
            Assert.That(entry.Presentation.Model.Current.Frame,Is.SameAs(frame));
            Assert.That(Root.Q("central-combat-footer"),Is.Not.Null);
            // Handler/class/tree proof only; no native resolution screenshot/readability claim.
        }
        [Test] public void ActualRebuildKeepsUnsentPlanningDraftWithoutSubmittingOrAdvancing()
        {
            var flow=entry.Presentation!;var model=flow.Model;var frame=model.Current.Frame;
            Assert.That(flow.DraftNextOrder("fleet.federation-scout",CampaignTravelIntent.StayEngage),Is.True);
            var draft=model.Current.TravelDraft;
            ShowCombat();InvokeEntry("Draw");InvokeEntry("Draw");
            Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(model.Current.TravelDraft,Is.SameAs(draft));
            Assert.That(model.Current.Dirty,Is.True);
            Assert.That(Root.Q("central-fleet-rail"),Is.TypeOf<ScrollView>());
            Assert.That(Root.Q("central-combat-footer"),Is.Not.Null);
        }
        [Test] public void ActualShellNavigationKeepsSameCampaignDraftAndInvalidatesViewEpoch()
        {
            var model=entry.Presentation!.Model;var controller=model.Current;var frame=controller.Frame;
            Assert.That(entry.Workspace!.Page,Is.EqualTo(ReviewWorkspacePage.Map));
            Assert.That(Root.Q("review-workspace-navigation"),Is.Not.Null);
            Assert.That(model.DraftTravel("fleet.federation-scout",CampaignTravelIntent.StayEngage,null,null),Is.True);var draft=controller.TravelDraft;
            var epoch=(long)typeof(ReviewEntry).GetField("viewEpoch",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry)!;
            foreach(var page in new[]{ReviewWorkspacePage.Research,ReviewWorkspacePage.Production,ReviewWorkspacePage.FleetOrders,ReviewWorkspacePage.Combat,ReviewWorkspacePage.Map})
            {
                InvokeEntry("Navigate",page);Assert.That(entry.Workspace.Page,Is.EqualTo(page));
                Assert.That(model.Current,Is.SameAs(controller));Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(model.Current.TravelDraft,Is.SameAs(draft));
            }
            Assert.That((long)typeof(ReviewEntry).GetField("viewEpoch",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry)!,Is.GreaterThan(epoch));
            Assert.That(Root.Q<Label>("review-workspace-heading").text,Is.EqualTo("Map"));
        }
        [UnityTest] public IEnumerator RealJointRoundMovesCentralViewThroughWaitingResultAndStagedAftermath()
        {
            var flow=entry.Presentation!;var model=flow.Model;
            Assert.That(flow.DraftNextOrder("fleet.federation-scout",CampaignTravelIntent.StayEngage),Is.True);
            Assert.That(flow.SubmitNextOrder("command.editor.central.a",true),Is.True);
            Assert.That(model.SwitchActor("civ.cardassian"),Is.True);
            Assert.That(flow.DraftNextOrder("fleet.cardassian-scout",CampaignTravelIntent.StayEngage),Is.True);
            Assert.That(flow.SubmitNextOrder("command.editor.central.b",true),Is.True);Assert.That(flow.AdvancePlanning(true),Is.True);
            Assert.That(model.SwitchActor("civ.federation"),Is.True);Assert.That(flow.CommitRound("command.editor.central.round.a",true),Is.True);
            ShowCombat();yield return null;
            Assert.That(Root.Q<Label>("combat-stage-heading").text,Does.Contain("waiting"));
            Assert.That(flow.CommitRound("command.editor.central.duplicate",true),Is.False);
            Assert.That(model.SwitchActor("civ.cardassian"),Is.True);Assert.That(flow.CommitRound("command.editor.central.round.b",true),Is.True);
            ShowCombat();yield return null;
            Assert.That(flow.Stage,Is.EqualTo(ReviewCombatStage.RoundResult));Assert.That(flow.PlanNextRound(),Is.True);
            while(!model.Current.Frame.Battle!.Complete)
            {
                var round=model.Current.Frame.Battle.Round;
                foreach(var actor in new[]{"civ.federation","civ.cardassian"})
                {
                    Assert.That(model.SwitchActor(actor),Is.True);if(flow.Stage==ReviewCombatStage.RoundResult)Assert.That(flow.PlanNextRound(),Is.True);
                    Assert.That(flow.CommitRound("command.editor.central."+actor+"."+round,true),Is.True);
                }
            }
            ShowCombat();yield return null;
            Assert.That(flow.Stage,Is.EqualTo(ReviewCombatStage.AftermathPending));Assert.That(flow.CanDraftNextOrder,Is.False);
            Assert.That(flow.FinalizeAftermath(true),Is.True);ShowCombat();yield return null;
            Assert.That(flow.Stage,Is.EqualTo(ReviewCombatStage.AftermathConfirmed));Assert.That(flow.FinalizeAftermath(true),Is.False);
            Assert.That(Root.Q<Label>("combat-stage-heading").text,Does.Contain("Aftermath confirmed"));
        }
    }
}
