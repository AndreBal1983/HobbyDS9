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
            Assert.That(snapshot.Navigation.Nodes.Count, Is.EqualTo(6));
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
            Assert.That(encounters, Is.EqualTo(15)); Assert.That(state.TurnNumber, Is.EqualTo(26));
            Assert.That(state.StateRevision, Is.EqualTo(228)); Assert.That(state.Ships.Count, Is.EqualTo(8));
            Assert.That(state.NextEntitySequence, Is.EqualTo(72UL)); Assert.That(state.RngState.State, Is.EqualTo(0x850d3ff45bd6b85eUL));
        }
    }
}
