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
        // Invoke the actual registered Clickable callback; synthetic navigation
        // events in EditMode do not exercise player input. Native checks are separate.
        private static void Click(Button button)
        {
            Assert.That(button.enabledSelf,Is.True);
            var invoke=typeof(Clickable).GetMethod("Invoke",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Assert.That(invoke,Is.Not.Null);invoke.Invoke(button.clickable,new object[]{null});
        }
        [Test]
        public void SystemSwitchingFiltersCataloguePreservesDraftAndSeparatesProfileControls()
        {
            var draft=new ShipDesignDraft(ShipDesignFixtureLoader.Load());draft.Rename("Local survey class");
            using(var view=new ShipDesignerView(draft,()=>{}))
            {
                Assert.That(view.Q<ShipSystemGraphic>().HasImage,Is.True);
                foreach(var id in new[]{"Weapons","Power","Crew","Sensors","Science","Command","Shields","Propulsion","Support"})
                {
                    view.SelectSystem(id);
                    var expected=draft.Provider.Catalogue.Where(c=>c.SystemId==id).Select(c=>"component-row-"+c.Id).ToArray();
                    Assert.That(view.Q("designer-component-list").Query<VisualElement>().ToList().Where(e=>e.name?.StartsWith("component-row-")==true).Select(e=>e.name),Is.EquivalentTo(expected));
                    Assert.That(view.Query<LongField>().ToList().Any(f=>f.name.StartsWith("power-")),Is.False);
                    Assert.That(draft.Current.Name,Is.EqualTo("Local survey class"));
                }
                view.SelectSystem("Profiles");Assert.That(view.Q<LongField>("power-allocated-component.command.v1"),Is.Not.Null);
                view.SelectSystem("Overview");Assert.That(view.Q<LongField>("power-allocated-component.command.v1"),Is.Null);
            }
        }
        [Test]
        public void MissingArtUsesSafePlaceholderAndCatalogueCanReplaceIt()
        {
            var catalogue=ScriptableObject.CreateInstance<ShipDesignerGraphics>();var image=new Texture2D(2,2);
            try
            {
                var missing=new ShipSystemGraphic("Power","Power",catalogue);Assert.That(missing.HasImage,Is.False);Assert.That(missing.Q<Label>("designer-graphic-placeholder"),Is.Not.Null);
                catalogue.Slots.Add(new ShipDesignerGraphics.Slot {SystemId="Power",Image=image});
                var supplied=new ShipSystemGraphic("Power","Power",catalogue);Assert.That(supplied.Art.image,Is.SameAs(image));Assert.That(supplied.Q<Label>("designer-graphic-placeholder"),Is.Null);
            }
            finally {UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(catalogue);}
        }
        [Test]
        public void AttachedComponentActionsAddReplaceAndRemoveThroughLocalDraft()
        {
            var draft=new ShipDesignDraft(ShipDesignFixtureLoader.Load());draft.RequestTemplate("class.escort");
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();
            using(var view=new ShipDesignerView(draft,()=>{}))
            {
                try
                {
                    window.Show();window.rootVisualElement.Add(view);view.SelectSystem("Shields");
                    Click(view.Q<Button>("select-component-component.small-shield.v1"));
                    Click(view.Q<Button>("designer-replace"));
                    Assert.That(draft.Current.Installed.ContainsKey("component.escort-shield.v1"),Is.False);
                    Assert.That(draft.Current.Installed["component.small-shield.v1"],Is.EqualTo(1));
                    Click(view.Q<Button>("designer-add"));
                    Assert.That(draft.Current.Installed["component.small-shield.v1"],Is.EqualTo(2));
                    Click(view.Q<Button>("remove-component.small-shield.v1"));
                    Assert.That(draft.Current.Installed.ContainsKey("component.small-shield.v1"),Is.False);
                }
                finally{window.Close();}
            }
        }
        [Test]
        public void ProposedEditUsesSameDraftApisWithoutChangingSourceOrCampaign()
        {
            var host=new CampaignHost(Resources.Load<TextAsset>("ds9-slice").bytes);var save=host.SaveJson();
            var draft=new ShipDesignDraft(ShipDesignFixtureLoader.Load());var original=draft.Current;
            var proposed=draft.PreviewCopy();proposed.Add("component.energy-bank.v1");proposed.Replace("component.energy-bank.v1","component.launcher.v1");proposed.Remove("component.launcher.v1");
            Assert.That(draft.Current.SameAs(original),Is.True);Assert.That(host.SaveJson(),Is.EqualTo(save));
        }
        [Test]
        public void BackendMountFailureRoutesToWeaponsAndIsDisplayedThere()
        {
            var provider=ShipDesignFixtureLoader.Load();var draft=new ShipDesignDraft(provider);draft.SetQuantity("component.energy-bank.v1",5);
            using(var view=new ShipDesignerView(draft,()=>{}))
            {
                var issue=draft.Preview().Issues.First(i=>i.Explanation.ToLowerInvariant().Contains("mount"));
                Assert.That(view.IssueSystem(issue),Is.EqualTo("Weapons"));view.SelectSystem("Weapons");Assert.That(view.Query<Label>().ToList().Any(l=>l.text.Contains(issue.Explanation)),Is.True);
                view.SelectSystem("Validation");Assert.That(view.Query<Button>().ToList().Any(b=>b.text=="Review Weapons"),Is.True);
            }
        }
        [TestCase(1100)]
        [TestCase(1700)]
        [TestCase(2580)]
        public void LayoutChangesNeverDuplicateEditingControls(float width)
        {
            var draft=new ShipDesignDraft(ShipDesignFixtureLoader.Load());using(var view=new ShipDesignerView(draft,()=>{}))
            {
                view.ApplyLogicalWidth(width);view.SelectSystem("Weapons");view.ApplyLogicalWidth(1700);view.ApplyLogicalWidth(width);
                Assert.That(view.Query<Button>("designer-add").ToList().Count,Is.EqualTo(1));Assert.That(view.Query<Button>("designer-close").ToList().Count,Is.EqualTo(1));
                view.SelectSystem("Profiles");Assert.That(view.Query<LongField>("power-allocated-component.command.v1").ToList().Count,Is.EqualTo(1));
            }
        }
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
                    window.Show(); window.rootVisualElement.Add(view); view.SelectSystem("Profiles");
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
                Assert.That(view.Q<VisualElement>("designer-body"),Is.Not.Null); Assert.That(view.CloseButton,Is.Not.Null);
                Assert.That(view.Q<ShipSystemGraphic>().pickingMode,Is.EqualTo(PickingMode.Ignore)); Assert.That(view.Q<Image>("designer-image"),Is.Not.Null);
                shell.SelectSection("Selection"); Assert.That(shell.DesignerOpen,Is.False);
                Assert.That(shell.Q<VisualElement>("galaxy-map-frame").style.display.value,Is.EqualTo(DisplayStyle.Flex));
                Assert.That(shell.Header.FitMap.enabledSelf,Is.True);
            }
        }
    }
}
