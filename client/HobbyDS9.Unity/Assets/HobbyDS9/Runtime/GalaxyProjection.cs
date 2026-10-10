#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using EntityId = HobbyDS9.Contracts.EntityId;

namespace HobbyDS9.UnityClient
{
    // Pure client projection. No Unity objects and no campaign mutation.
    public sealed class GalaxyNodeView
    {
        public NavigationNodeId NodeId { get; }
        public EntityId? SystemId { get; }
        public string Label { get; }
        public IReadOnlyList<FleetSnapshot> Fleets { get; }
        public GalaxyNodeView(NavigationNodeId id, EntityId? system, string label, IEnumerable<FleetSnapshot> fleets)
        { NodeId = id; SystemId = system; Label = label; Fleets = fleets.ToList().AsReadOnly(); }
    }
    public static class GalaxyProjection
    {
        public static IReadOnlyList<GalaxyNodeView> Nodes(CampaignSnapshot snapshot) => Nodes(snapshot.Navigation.Nodes, snapshot.Campaign, snapshot.Content);
        public static IReadOnlyList<GalaxyNodeView> Nodes(IEnumerable<NavigationNodeSnapshot> nodes, SessionSnapshot campaign, ScenarioContentSnapshot content)
        {
            var names = content.StarSystems.ToDictionary(d => d.DefinitionId, d => d.DisplayName);
            var systems = campaign.StarSystems.ToDictionary(s => s.SystemId);
            return nodes.OrderBy(n => n.NodeId.Value, StringComparer.Ordinal).Select(n =>
            {
                var label = "Transit region";
                if (n.AssociatedSystemId.HasValue && systems.TryGetValue(n.AssociatedSystemId.Value, out var system))
                    label = names.TryGetValue(system.DefinitionId, out var name) ? name : "Star system";
                var fleets = campaign.Fleets.Where(f => n.AssociatedSystemId.HasValue && f.LocationSystemId == n.AssociatedSystemId.Value)
                    .OrderBy(f => f.FleetId.Value, StringComparer.Ordinal);
                return new GalaxyNodeView(n.NodeId, n.AssociatedSystemId, label, fleets);
            }).ToList().AsReadOnly();
        }
    }
    // Persistent only for the lifetime of the view, never exported in campaign saves.
    public sealed class GalaxySelection
    {
        public NavigationNodeId? Node { get; private set; }
        public EntityId? Fleet { get; private set; }
        public void SelectNode(NavigationNodeId id) { Node = id; Fleet = null; }
        public void SelectFleet(EntityId id) { Fleet = id; Node = null; }
        public void Reconcile(CampaignSnapshot snapshot)
        {
            if (Node.HasValue && !snapshot.Navigation.Nodes.Any(n => n.NodeId == Node.Value)) Node = null;
            if (Fleet.HasValue && !snapshot.Campaign.Fleets.Any(f => f.FleetId == Fleet.Value)) Fleet = null;
        }
    }
}
