using System;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId=HobbyDS9.Contracts.EntityId;

namespace HobbyDS9.UnityClient.Tests
{
    public sealed class CampaignDesignerTests
    {
        private static readonly EntityId Actor=new EntityId("civ.federation"),Colony=new EntityId("colony.sol");
        private static CampaignHost Start()=>new CampaignHost(Resources.Load<TextAsset>("ds9-slice").bytes);
        private static CampaignDesigner Workspace(CampaignHost host)
        {
            var w=new CampaignDesigner(host.Client,Actor);
            // Test-owned input only; live controller/catalogue/validation are real.
            var input=new ShipDesignDraft(ShipDesignFixtureLoader.Load());input.RequestTemplate("class.escort");
            var fit=input.Current.ToDto();fit.ClassId="class."+w.BlueprintId;
            w.Draft.StartFit(new DesignerFit(fit));return w;
        }
        private static void Click(Button button)
        {
            Assert.That(button.enabledSelf,Is.True);
            typeof(Clickable).GetMethod("Invoke",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(button.clickable,new object[]{null});
        }
        [Test]
        public void LiveEntryUsesCampaignProviderAndNeverOfflineLoader()
        {
            var go=new GameObject("campaign test");
            try
            {
                var entry=go.AddComponent<ClientEntry>();
                typeof(ClientEntry).GetMethod("OnEnable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(entry,null);
                typeof(ClientEntry).GetMethod("RequestStrategicSection",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(entry,new object[]{"Designer"});
                var view=entry.GetComponent<UIDocument>().rootVisualElement.Q<ShipDesignerView>();
                Assert.That(view,Is.Not.Null);Assert.That(view.Draft.Provider,Is.TypeOf<ApplicationShipDesignPreviewProvider>());
                Assert.That(view.Q<Label>("designer-provider-status").text,Does.Contain("LIVE CAMPAIGN"));
                Assert.That(view.Q<Button>("designer-construction").enabledSelf,Is.False);
                Assert.That(view.Draft.Current.Installed,Is.Empty);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test]
        public void PublicationButtonRunsLiveCommandShowsHistoryAndPreventsDuplicateClick()
        {
            var host=Start();var w=Workspace(host);
            using(var view=new ShipDesignerView(w.Draft,()=>{},campaign:w))
            {
                view.Draft.Rename("Live UI class");Click(view.Q<Button>("designer-construction"));
                Assert.That(host.Client.Blueprints().Single().Blueprint.Fit.DisplayName,Is.EqualTo("Live UI class"));
                Assert.That(view.Q<Button>("designer-construction").enabledSelf,Is.False);
                view.SelectSystem("Validation");Assert.That(view.Q<Label>("designer-command-feedback").text,Does.Contain("Published"));
                Assert.That(view.Q<Button>("designer-edit-"+w.BlueprintId+"-r1"),Is.Not.Null);
                var saved=host.SaveJson();view.SelectSystem("Profiles");view.Draft.SelectProfile(DesignerProfile.Combat);
                Assert.That(view.Q<LcarsPowerSlider>(),Is.Not.Null);Assert.That(host.SaveJson(),Is.EqualTo(saved));
            }
        }
        [Test]
        public void StalePublicationShowsBackendCodeRetainsDraftAndRequiresExplicitRefresh()
        {
            var host=Start();var w=Workspace(host);
            using(var view=new ShipDesignerView(w.Draft,()=>{},campaign:w))
            {
                var fit=view.Draft.Current;
                Assert.That(host.Client.Enqueue(host.Snapshot,Actor,Colony,new DefinitionId("ship-design.scout")).Accepted,Is.True);
                var before=host.SaveJson();Click(view.Q<Button>("designer-construction"));view.SelectSystem("Validation");
                Assert.That(w.RefreshRequired,Is.True);Assert.That(view.Draft.Current.SameAs(fit),Is.True);Assert.That(host.SaveJson(),Is.EqualTo(before));
                Assert.That(view.Q<Label>("designer-command-feedback").text,Does.Contain("[StaleRevision]"));
                Assert.That(view.Q<Button>("designer-construction").enabledSelf,Is.False);
                Click(view.Q<Button>("designer-refresh-campaign"));Assert.That(w.RefreshRequired,Is.False);
            }
        }
        [Test]
        public void NeutralProviderFactsAndEngineeringGateControlBothAddButtons()
        {
            var host=Start();var source=host.Client.ShipDesignCatalogue(Actor);
            var design=HobbyDS9.Application.ShipDesignApplication.FromCampaignCatalogue(source.Catalogue);
            var fit=Workspace(host).Draft.Current;
            var provider=new ApplicationShipDesignPreviewProvider(design,source.AvailableVersionIds.Where(id=>id!="component.energy-bank.v1"),
                new[]{new DesignerTemplate("test","Test input",fit)},"Live test access");
            var draft=new ShipDesignDraft(provider);
            using(var view=new ShipDesignerView(draft,()=>{}))
            {
                view.SelectSystem("Weapons");Click(view.Q<Button>("select-component-component.energy-bank.v1"));
                Assert.That(view.Q<Button>("designer-add").enabledSelf,Is.False);
                Assert.That(view.Q<Button>("quantity-plus-component.energy-bank.v1").enabledSelf,Is.False);
                Assert.That(view.Query<Label>().ToList().Any(l=>l.text=="Version unavailable"),Is.True);
                Assert.That(view.Query<Label>().ToList().Any(l=>l.text=="Full / minimum power"),Is.True);
            }
        }
        [Test]
        public void ProductionButtonsQueueExactHistoryAndRefreshAuthoritativeState()
        {
            var host=Start();var w=Workspace(host);Assert.That(w.Publish().Accepted,Is.True);
            w.Draft.Rename("Second UI revision");Assert.That(w.Publish().Accepted,Is.True);
            var before=host.Snapshot.Campaign.Civilizations.Single(c=>c.CivilizationId==Actor).Credits;
            var model=new ProductionOverview(host.Client,host.Snapshot);var view=new ProductionOverviewView();
            view.Present(model,op=>op());
            var button=view.Q<Foldout>("blueprint-options-colony.sol").Q<Button>("queue-blueprint-"+w.BlueprintId+"-r1");Assert.That(button,Is.Not.Null);Click(button);
            Assert.That(button.enabledSelf,Is.False);
            Assert.That(host.Snapshot.Campaign.Colonies.Single(c=>c.ColonyId==Colony).ProductionQueue.Single().Blueprint.Revision,Is.EqualTo(1));
            Assert.That(host.Snapshot.Campaign.Civilizations.Single(c=>c.CivilizationId==Actor).Credits,Is.EqualTo(before-w.History[0].Blueprint.Performance.Cost.Credits));
            view.Present(new ProductionOverview(host.Client,host.Snapshot),op=>op());
            Assert.That(view.Query<Label>().ToList().Any(l=>l.text.Contains(w.BlueprintId+" • revision 1")),Is.True);
        }
        [Test]
        public void LoadedCampaignBlocksOldPublicationWithExplicitReplacementAction()
        {
            var host=Start();var w=Workspace(host);
            using(var view=new ShipDesignerView(w.Draft,()=>{},campaign:w))
            {
                view.Draft.Rename("Keep for review");var fit=view.Draft.Current;host.LoadJson(host.SaveJson());w.Refresh();view.RefreshCampaignState();
                Assert.That(view.Q<Button>("designer-construction").enabledSelf,Is.False);
                Assert.That(view.Q<Button>("designer-use-loaded-campaign").style.display.value,Is.EqualTo(DisplayStyle.Flex));
                Assert.That(view.Draft.Current.SameAs(fit),Is.True);
                view.SelectSystem("Validation");Assert.That(view.Q<Label>("designer-command-feedback").text,Does.Contain("Campaign replaced"));
            }
        }
        [TestCase(1100)]
        [TestCase(1700)]
        [TestCase(2580)]
        public void LiveDraftPreservesEverySystemAndProfileAcrossResponsiveModes(float width)
        {
            var host=Start();var w=Workspace(host);
            using(var view=new ShipDesignerView(w.Draft,()=>{},campaign:w))
            {
                view.ApplyLogicalWidth(width);view.Draft.Rename("Retained");view.Draft.SelectProfile(DesignerProfile.Survey);var fit=view.Draft.Current;
                foreach(var system in ShipDesignerView.Systems) {view.SelectSystem(system);Assert.That(view.Draft.Current.SameAs(fit),Is.True);Assert.That(view.Draft.Profile,Is.EqualTo(DesignerProfile.Survey));}
                Assert.That(view.ResetButton,Is.Not.Null);Assert.That(view.CloseButton,Is.Not.Null);
                view.SelectSystem("Profiles");Assert.That(view.Q<ScrollView>("designer-profile-results"),Is.Not.Null);
            }
        }
    }
}
