using System.Linq;
using System.Reflection;
using HobbyDS9.UnityClient;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient.Tests
{
    public sealed class StrategicPrototypeTests
    {
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Set(ClientEntry entry, string name, object value) => typeof(ClientEntry).GetField(name, Private).SetValue(entry, value);
        private static void Call(ClientEntry entry, string name, params object[] args) => typeof(ClientEntry).GetMethod(name, Private).Invoke(entry, args);

        [Test]
        public void InitialMarsWorldUsesLiveFactsAndVisibleProductionWithoutChangingCampaign()
        {
            var go = new GameObject("Strategic prototype test"); go.SetActive(false);
            try
            {
                var entry = go.AddComponent<ClientEntry>();
                var host = new CampaignHost(Resources.Load<TextAsset>("ds9-slice").bytes);
                var shell = new LcarsShell(new GalaxyMap(), new ProductionOverviewView(), _ => {}, () => {}, () => {});
                Set(entry, "host", host); Set(entry, "shell", shell); Set(entry, "details", shell.Details); Set(entry, "message", shell.Message);
                var before = host.SaveJson();
                Call(entry, "SelectInitialStrategicWorld");
                Assert.That(typeof(ClientEntry).GetField("selectedPlanet", Private).GetValue(entry).ToString(), Is.EqualTo("planet.mars"));
                var mars = StrategicScreenPresentation.InitialWorld(host.Snapshot);
                Call(entry, "ShowStrategicWorld", host.Snapshot, mars);
                Assert.That(shell.DetailsFrame.Heading.text, Is.EqualTo("MARS / WORLD INSPECTOR"));
                Assert.That(shell.Q<VisualElement>("selected-world-production"), Is.Not.Null);
                Assert.That(shell.Q<Button>("queue-selected-design").enabledSelf, Is.False); // Real missing local resource.
                Assert.That(shell.Q<Foldout>("world-details").value, Is.False);
                Assert.That(shell.Query<Label>().ToList().Any(label => label.text.Contains("unavailable locally")), Is.True);
                Assert.That(shell.Query<Label>().ToList().Any(label => label.text.Contains("[InsufficientStrategicResource]")), Is.False);
                Assert.That(host.SaveJson(), Is.EqualTo(before));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test]
        public void MapDecorationCannotPickAndResizeKeepsStrategicControls()
        {
            var shell = new LcarsShell(new GalaxyMap(),new ProductionOverviewView(),_=>{},()=>{},()=>{});
            shell.ProductionChoice.Choose(false);
            foreach (var width in new[] {1920,2580})
            {
                shell.ApplyLogicalSize(width,1080);
                Assert.That(shell.Q<Label>("map-legend").pickingMode,Is.EqualTo(PickingMode.Ignore));
                Assert.That(shell.Q<LcarsElbow>().pickingMode,Is.EqualTo(PickingMode.Ignore));
                Assert.That(shell.ProductionOpen,Is.False);
                Assert.That(shell.Header.Credits,Is.Not.Null); Assert.That(shell.Header.Phase,Is.Not.Null);
                Assert.That(shell.DetailsOpen,Is.True);
            }
        }
    }
}
