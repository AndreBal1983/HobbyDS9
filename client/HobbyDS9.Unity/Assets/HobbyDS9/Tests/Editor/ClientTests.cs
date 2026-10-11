using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient.Tests
{
    public sealed class ClientTests
    {
        private static CampaignHost Start() => new CampaignHost(Resources.Load<TextAsset>("ds9-slice").bytes);
        [Test]
        public void ProjectStartsRealScenarioThroughApplication()
        {
            Assert.That(UnityEngine.Application.unityVersion, Is.EqualTo("6000.6.5f1"));
            Assert.That(Resources.Load<ThemeStyleSheet>("HobbyTheme"), Is.Not.Null);
            Assert.That(Resources.Load<StyleSheet>("HobbyClient"), Is.Not.Null);
            var host = Start(); var snapshot = host.Snapshot;
            Assert.That(snapshot.Campaign.ScenarioId, Is.EqualTo("ds9-strategic-slice-v1"));
            Assert.That(snapshot.Navigation.Nodes.Count, Is.EqualTo(7));
            Assert.That(snapshot.Campaign.Fleets.Count, Is.EqualTo(3));
            Assert.That(typeof(ClientEntry).Assembly.GetReferencedAssemblies().Any(a => a.Name == "HobbyDS9.Simulation"), Is.False);
        }
        [Test]
        public void EmptyNavigationNodeNeverInheritsSystemFromMatchingId()
        {
            var snapshot = Start().Snapshot;
            var node = GalaxyProjection.Nodes(new[] { new NavigationNodeSnapshot(new NavigationNodeId("system.sol"), null) }, snapshot.Campaign, snapshot.Content).Single();
            Assert.That(node.SystemId, Is.Null); Assert.That(node.Fleets, Is.Empty);
        }
        [Test]
        public void HumanOrdersUseRealApplicationAndNewtonsoftSaveRoundTrip()
        {
            var host = Start(); var client = host.Client;
            var actor = new HobbyDS9.Contracts.EntityId("civ.federation");
            var colony = new HobbyDS9.Contracts.EntityId("colony.sol");
            Assert.That(client.Research(host.Snapshot, actor, new DefinitionId("research.advanced-engineering")).Accepted, Is.True);
            Assert.That(client.Enqueue(host.Snapshot, actor, colony, new DefinitionId("ship-design.scout")).Accepted, Is.True);
            var json = host.SaveJson(); var saved = host.Snapshot;
            var item = saved.Campaign.Colonies.Single(c => c.ColonyId == colony).ProductionQueue.Single();
            Assert.That(client.Cancel(host.Snapshot, actor, colony, item.QueueItemId).Accepted, Is.True);
            host.LoadJson(json);
            Assert.That(host.SaveJson(), Is.EqualTo(json));
            Assert.That(host.Snapshot, Is.Not.SameAs(saved));
            Assert.That(host.SaveSummary(json), Does.Contain("Turn 1"));
            Assert.That(host.Snapshot.Campaign.Colonies.Single(c => c.ColonyId == colony).ProductionQueue.Count, Is.EqualTo(1));
        }
        [Test]
        public void FailedJsonAndIncompatibleLoadPreserveLiveHost()
        {
            var host = Start(); var snapshot = host.Snapshot; var json = host.SaveJson();
            Assert.That(() => host.LoadJson("{bad-json"), Throws.Exception);
            Assert.That(() => host.LoadJson(json.Replace("ds9-strategic-slice-v1", "different-scenario")), Throws.Exception);
            Assert.That(host.Snapshot, Is.SameAs(snapshot)); Assert.That(host.SaveJson(), Is.EqualTo(json));
            Assert.That(host.EndTurn().Status, Is.Not.EqualTo(TurnStatus.Rejected));
        }
        [Test]
        public void FleetPreviewStaysLocalUntilConfirmation()
        {
            var host = Start(); var before = host.SaveJson();
            var preview = host.Client.Preview(host.Snapshot, new HobbyDS9.Contracts.EntityId("fleet.federation-scout"), new NavigationNodeId("nav.bajor"));
            Assert.That(preview.Route.TraversalCost, Is.EqualTo(7)); Assert.That(host.SaveJson(), Is.EqualTo(before));
            var map = new GalaxyMap();
            map.Present(host.Snapshot, new GalaxySelection(), _ => { }, _ => { }); map.SetRoute(preview);
            Assert.That(map.Query<Button>().ToList().Count, Is.EqualTo(10));
            Assert.That(host.SaveJson(), Is.EqualTo(before));
            Assert.That(host.Client.SubmitRoute(preview).Accepted, Is.True);
            Assert.That(host.Snapshot.Campaign.Fleets.Single(f => f.FleetId.Value == "fleet.federation-scout").RouteNodeIds.Select(n => n.Value), Is.EqualTo(new[] { "nav.bajor" }));
            Assert.That(host.SaveJson(), Is.Not.EqualTo(before));
        }
        [Test]
        public void SlotOverwriteUsesTheRealHostSerializer()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hobby-unity-save-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var host = Start(); var store = new SaveSlotStore(directory);
                store.Write(1, host.SaveJson(), false); host.EndTurn(); var changed = host.SaveJson();
                Assert.That(() => store.Write(1, changed, false), Throws.TypeOf<System.IO.IOException>());
                store.Write(1, changed, true); host.EndTurn(); host.LoadJson(store.Read(1));
                Assert.That(host.SaveJson(), Is.EqualTo(changed));
            }
            finally { if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true); }
        }
        [Test]
        public void NewtonsoftRoundTripsPendingAndBufferedCombatCheckpoints()
        {
            var host = Start();
            for (var turn = 0; turn < 10 && host.Snapshot.Campaign.Phase == SessionPhase.Planning; turn++) host.EndTurn();
            Assert.That(host.Snapshot.Campaign.Phase, Is.EqualTo(SessionPhase.AwaitingCombat));
            var pending = host.SaveJson(); host.LoadJson(pending); Assert.That(host.SaveJson(), Is.EqualTo(pending));
            foreach (var request in host.Pending()) Assert.That(host.Resolve(request).Status, Is.EqualTo(CombatSubmissionStatus.Buffered));
            var buffered = host.SaveJson(); host.LoadJson(buffered);
            Assert.That(host.Pending(), Is.Empty); Assert.That(host.SaveJson(), Is.EqualTo(buffered));
            Assert.That(host.FinalizeCombats().Status, Is.EqualTo(TurnStatus.Completed));
        }
        [TestCase(false)] [TestCase(true)]
        public void ClientHostCompletesTwentyFiveTurnsAndCombatCheckpoints(bool reverse)
        {
            var host = Start(); var encounters = 0;
            for (var i = 0; i < 25; i++)
            {
                var result = host.EndTurn(); Assert.That(result.Status, Is.Not.EqualTo(TurnStatus.Rejected));
                if (result.Status == TurnStatus.AwaitingCombat)
                {
                    Assert.That(host.EndTurn().ErrorCode, Is.EqualTo(TurnErrorCode.CombatPending));
                    var pending = host.Pending();
                    foreach (var request in reverse ? pending.Reverse() : pending)
                    { Assert.That(host.Resolve(request).Status, Is.EqualTo(CombatSubmissionStatus.Buffered)); encounters++; }
                    Assert.That(host.Snapshot.Campaign.Phase, Is.EqualTo(SessionPhase.AwaitingCombat));
                    Assert.That(host.FinalizeCombats().Status, Is.EqualTo(TurnStatus.Completed));
                }
            }
            var state = host.Snapshot.Campaign;
            Assert.That(encounters, Is.EqualTo(17)); Assert.That(state.TurnNumber, Is.EqualTo(26));
            Assert.That(state.StateRevision, Is.EqualTo(187)); Assert.That(state.Ships.Count, Is.EqualTo(9));
            Assert.That(state.NextEntitySequence, Is.EqualTo(97UL)); Assert.That(state.RngState.State, Is.EqualTo(0xd8919406d6e01cf1UL));
        }

        [Test]
        public void MixedWorldSnapshotsAndNamedResourceCostsReachTheClient()
        {
            var host=Start();var snapshot=host.Snapshot;
            var worlds=snapshot.Campaign.Colonies.Where(c=>c.SystemId.Value=="system.sol").ToList();
            Assert.That(worlds.Count,Is.EqualTo(2));
            var earth=worlds.Single(c=>c.PlanetId.Value=="planet.sol");
            Assert.That(earth.State.Cohorts.Count,Is.EqualTo(3));
            Assert.That(earth.Environment.Habitats.Sum(h=>h.ShareBps),Is.EqualTo(10000));
            Assert.That(earth.Economy.Industry,Is.GreaterThan(0));
            Assert.That(snapshot.Campaign.ShipDesigns.All(d=>d.ResourceCosts.Any(r=>r.ResourceDefinitionId=="resource.dilithium")),Is.True);
            var json=host.SaveJson();host.EndTurn();host.LoadJson(json);Assert.That(host.SaveJson(),Is.EqualTo(json));
        }
        [TestCase("Stability")]
        [TestCase("Prosperity")]
        [TestCase("AcceptsRefugees")]
        public void MissingAuthoritativeScalarRejectsLoadWithoutReplacingSession(string field)
        {
            var host=Start();var before=host.SaveJson();var json=Newtonsoft.Json.Linq.JObject.Parse(before);
            ((Newtonsoft.Json.Linq.JObject)json["GameState"]["Colonies"][0]).Remove(field);
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(()=>host.LoadJson(json.ToString()));
            Assert.That(host.SaveJson(),Is.EqualTo(before));
        }
        [Test]
        public void ObsoleteMaterialsFieldRejectsLoadWithoutReplacingSession()
        {
            var host=Start();var before=host.SaveJson();var json=Newtonsoft.Json.Linq.JObject.Parse(before);
            json["GameState"]["Colonies"][0]["Materials"]=99;
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(()=>host.LoadJson(json.ToString()));
            Assert.That(host.SaveJson(),Is.EqualTo(before));
        }
        [Test]
        public void GovernanceCommandRefreshesDetachedPoliticsAndPreservesStaleRejection()
        {
            var host=Start();var before=host.Snapshot;
            var result=host.Client.Governance(before,new HobbyDS9.Contracts.EntityId("civ.federation"),new HobbyDS9.Contracts.EntityId("colony.sol"),new DefinitionId("governance.autonomy"));
            Assert.That(result.Accepted,Is.True);
            Assert.That(before.Campaign.Colonies.Single(c=>c.ColonyId.Value=="colony.sol").State.Politics.GovernancePolicyDefinitionId,Is.EqualTo("governance.integration"));
            var saved=host.SaveJson();
            Assert.That(host.Client.Governance(before,new HobbyDS9.Contracts.EntityId("civ.federation"),new HobbyDS9.Contracts.EntityId("colony.sol"),new DefinitionId("governance.security")).ErrorCode,Is.EqualTo(CommandErrorCode.StaleRevision));
            Assert.That(host.SaveJson(),Is.EqualTo(saved));
            host.EndTurn();Assert.That(host.Snapshot.Campaign.Colonies.Single(c=>c.ColonyId.Value=="colony.sol").State.Politics.CohortReports,Is.Not.Empty);
        }
        [Test]
        public void WeightedPreviewAndPartialTravelRoundTripThroughRealHost()
        {
            var host=Start();var before=host.SaveJson();var preview=host.Client.Preview(host.Snapshot,new HobbyDS9.Contracts.EntityId("fleet.federation-scout"),new NavigationNodeId("nav.bajor"));
            Assert.That(preview.Route.TraversalCost,Is.EqualTo(7));Assert.That(preview.Travel.EstimatedTurns,Is.EqualTo(3));Assert.That(host.SaveJson(),Is.EqualTo(before));
            Assert.That(host.Client.SubmitRoute(preview).Accepted,Is.True);host.EndTurn();var fleet=host.Snapshot.Campaign.Fleets.Single(f=>f.FleetId.Value=="fleet.federation-scout");
            Assert.That(fleet.TransitProgress,Is.EqualTo(3));Assert.That(fleet.TransitTotalCost,Is.EqualTo(7));
            var saved=host.SaveJson();host.LoadJson(saved);Assert.That(host.SaveJson(),Is.EqualTo(saved));
        }
        [Test]
        public void IndependentEmptyHazardIsPresentedWithoutInferringSystem()
        {
            var snapshot=Start().Snapshot;var node=snapshot.Navigation.Nodes.Single(n=>n.NodeId.Value=="nav.badlands");
            Assert.That(node.AssociatedSystemId,Is.Null);Assert.That(node.PhenomenonDefinitionIds,Does.Contain("phenomenon.plasma-storm"));
            var sol=snapshot.Navigation.Nodes.Single(n=>n.NodeId.Value=="nav.sol");Assert.That(sol.AssociatedSystemId.Value.Value,Is.EqualTo("system.sol"));
            Assert.That(GalaxyProjection.Nodes(snapshot).Single(n=>n.NodeId==node.NodeId).Label,Is.EqualTo("Badlands transit"));
        }
        [Test]
        public void UnknownConnectionIsExcludedFromMapAndRoutePreviewAfterReload()
        {
            var host=Start();var json=Newtonsoft.Json.Linq.JObject.Parse(host.SaveJson());
            var knowledge=json["GameState"]["NavigationKnowledge"].Single(k=>(string)k["CivilizationId"]=="civ.federation");
            knowledge["Connections"].Single(e=>(string)e["Id"]=="connection.sol-bajor")["Level"]=0;
            host.LoadJson(json.ToString());var preview=host.Client.Preview(host.Snapshot,new HobbyDS9.Contracts.EntityId("fleet.federation-scout"),new NavigationNodeId("nav.bajor"));
            Assert.That(preview.Route.Success,Is.False);Assert.That(host.Snapshot.Navigation.Links.Any(e=>e.EndpointA.Value=="nav.sol"||e.EndpointB.Value=="nav.sol"),Is.False);
        }
    }
}
