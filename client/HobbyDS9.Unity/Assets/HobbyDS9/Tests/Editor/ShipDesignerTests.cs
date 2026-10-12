using System;
using System.Linq;
using HobbyDS9.UnityClient;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient.Tests
{
    public sealed class ShipDesignerTests
    {
        [Test]
        public void AttachedPowerControlsRetainInvalidInputAndOnlyEditSelectedProfile()
        {
            var provider=ShipDesignFixtureLoader.Load(); var draft=new ShipDesignDraft(provider);
            draft.RequestTemplate("class.escort"); draft.SelectProfile(DesignerProfile.Combat);
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();
            using(var view=new ShipDesignerView(draft,()=>{}))
            {
                try
                {
                    window.Show(); window.rootVisualElement.Add(view);
                    Assert.That(draft.Current.Power("component.energy-bank.v1",DesignerProfile.Combat).AllocatedPower,Is.EqualTo(12));
                    view.Q<LongField>("power-allocated-component.energy-bank.v1").value=100;
                    Assert.That(draft.Current.Power("component.energy-bank.v1",DesignerProfile.Combat).AllocatedPower,Is.EqualTo(100));
                    Assert.That(provider.Evaluate(draft.Current).Issues.Any(i=>i.Code==HobbyDS9.Contracts.ShipDesign.DesignIssueCode.InvalidPowerAllocation),Is.True);
                    Assert.That(draft.Current.Power("component.energy-bank.v1",DesignerProfile.Cruise).AllocatedPower,Is.EqualTo(0));
                    Assert.That(draft.Current.Power("component.energy-bank.v1",DesignerProfile.Survey).AllocatedPower,Is.EqualTo(0));
                    draft.RequestReset(); draft.ConfirmDiscard();
                    Assert.That(draft.Current.Power("component.energy-bank.v1",DesignerProfile.Combat).AllocatedPower,Is.EqualTo(12));
                }
                finally { window.Close(); }
            }
        }
        [Test]
        public void DraftEditsRefreshAffectedSystemErrorsComparisonAndConfirmationWithoutCampaignMutation()
        {
            var host=new CampaignHost(Resources.Load<TextAsset>("ds9-slice").bytes); var before=host.SaveJson();
            var draft=new ShipDesignDraft(ShipDesignFixtureLoader.Load()); var exited=false;
            using(var view=new ShipDesignerView(draft,()=>exited=true))
            {
                Assert.That(view.Q<Label>("designer-provider-status").text,Does.Contain("Backend validated"));
                Assert.That(view.Q<Button>("designer-construction").enabledSelf,Is.False);
                draft.SetQuantity("component.labs.v1",2);
                Assert.That(view.Q<Label>("designer-state").ClassListContains("sd-error"),Is.True);
                Assert.That(view.Query<Label>().ToList().Any(l=>l.text.Contains("capacity")),Is.True);
                view.RequestLeave(()=>exited=true); Assert.That(exited,Is.False);
                Assert.That(view.Q<VisualElement>("designer-confirmation").style.display.value,Is.EqualTo(DisplayStyle.Flex));
                draft.CancelDiscard(); Assert.That(draft.Modified,Is.True);
                view.RequestLeave(()=>exited=true); draft.ConfirmDiscard(); Assert.That(exited,Is.True);
                Assert.That(host.SaveJson(),Is.EqualTo(before));
            }
        }
        [Test]
        public void AttachedControlsEditOnlyLocalNameRoleAndTemplateAndKeepCancelledTemplateSelection()
        {
            var draft=new ShipDesignDraft(ShipDesignFixtureLoader.Load());
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();
            using(var view=new ShipDesignerView(draft,()=>{}))
            {
                try
                {
                    window.Show();window.rootVisualElement.Add(view);
                    view.Q<TextField>("designer-name").value="New local class"; Assert.That(draft.Current.Name,Is.EqualTo("New local class"));
                    view.Q<DropdownField>("designer-role").index=draft.Provider.Roles.ToList().FindIndex(r=>r.Id=="role.escort"); Assert.That(draft.Current.Role,Is.EqualTo("role.escort"));
                    view.Q<DropdownField>("designer-template").index=1; Assert.That(draft.ConfirmationPending,Is.True);
                    Assert.That(draft.TemplateId,Is.EqualTo("class.surveyor"));
                    draft.CancelDiscard(); Assert.That(view.Q<DropdownField>("designer-template").index,Is.EqualTo(0));
                    Assert.That(draft.Current.Name,Is.EqualTo("New local class"));
                }
                finally {window.Close();}
            }
        }
        [TestCase(1920,1080)]
        [TestCase(3440,1440)]
        [TestCase(1280,720)]
        public void DesignerUsesExistingShellAndResponsiveScrollWithoutRecreatingMap(float width,float height)
        {
            var draft=new ShipDesignDraft(ShipDesignFixtureLoader.Load());var map=new GalaxyMap();
            using(var view=new ShipDesignerView(draft,()=>{}))
            {
                var shell=new LcarsShell(map,new ProductionOverviewView(),_=>{},()=>{},()=>{}); shell.AttachDesigner(view);
                var scale=height/1080*ResponsiveLayoutPolicy.PanelScaleMultiplier(height);
                shell.ApplyLogicalSize(width/scale,height/scale); shell.SelectSection("Designer");
                view.ApplyLogicalWidth((float)(width/scale-16-(shell.Plan.HorizontalNavigation?0:shell.Plan.RailWidth+8)));
                Assert.That(view.ClassListContains("sd-narrow"),Is.EqualTo(width==1280));
                Assert.That(shell.DesignerOpen,Is.True); Assert.That(shell.Q<GalaxyMap>(),Is.SameAs(map));
                Assert.That(shell.Q<VisualElement>("galaxy-map-frame").style.display.value,Is.EqualTo(DisplayStyle.None));
                Assert.That(view.Q<ScrollView>("designer-body"),Is.Not.Null); Assert.That(view.CloseButton,Is.Not.Null);
                Assert.That(view.Q<ShipSchematic>().pickingMode,Is.EqualTo(PickingMode.Ignore));
                shell.SelectSection("Selection"); Assert.That(shell.DesignerOpen,Is.False);
                Assert.That(shell.Q<VisualElement>("galaxy-map-frame").style.display.value,Is.EqualTo(DisplayStyle.Flex));
                Assert.That(shell.Header.FitMap.enabledSelf,Is.True);
            }
        }
    }
}
