#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using EntityId = HobbyDS9.Contracts.EntityId;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    public sealed class GalaxyMap : VisualElement
    {
        private readonly Dictionary<NavigationNodeId, Vector2> positions = new Dictionary<NavigationNodeId, Vector2>();
        private readonly List<(VisualElement view, NavigationNodeId node, Vector2 offset)> markers = new List<(VisualElement, NavigationNodeId, Vector2)>();
        private IReadOnlyList<NavigationLinkSnapshot> links = Array.Empty<NavigationLinkSnapshot>();
        private Vector2 pan;
        private float zoom = 1;
        private int dragPointer = -1;
        private Vector2 previous;
        public GalaxyMap()
        {
            style.flexGrow = 1; style.minWidth = 0; style.overflow = Overflow.Hidden;
            style.backgroundColor = new Color(.025f, .04f, .065f);
            generateVisualContent += DrawLinks;
            RegisterCallback<GeometryChangedEvent>(_ => Place());
            RegisterCallback<WheelEvent>(e =>
            {
                var oldZoom = zoom; zoom = Mathf.Clamp(zoom * Mathf.Exp(-e.delta.y * .045f), .35f, 3.5f);
                var center = contentRect.center;
                pan = e.localMousePosition - center - (e.localMousePosition - center - pan) * (zoom / oldZoom);
                Place(); e.StopPropagation();
            });
            RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 && e.button != 2) return;
                if (e.button == 0 && e.target != this) return;
                dragPointer = e.pointerId; previous = e.position; this.CapturePointer(e.pointerId); e.StopPropagation();
            });
            RegisterCallback<PointerMoveEvent>(e =>
            {
                if (dragPointer != e.pointerId) return;
                Vector2 current = e.position; pan += current - previous; previous = current; Place();
            });
            RegisterCallback<PointerUpEvent>(e =>
            { if (dragPointer == e.pointerId) { this.ReleasePointer(e.pointerId); dragPointer = -1; } });
            RegisterCallback<PointerCaptureOutEvent>(_ => dragPointer = -1);
        }
        public void ResetView() { pan = Vector2.zero; zoom = 1; Place(); }
        public void Present(CampaignSnapshot snapshot, GalaxySelection selection, Action<NavigationNodeId> nodeSelected, Action<EntityId> fleetSelected)
        {
            Clear(); markers.Clear(); positions.Clear(); links = snapshot.Navigation.Links;
            var nodes = GalaxyProjection.Nodes(snapshot);
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i]; var angle = 2 * Mathf.PI * i / Math.Max(1, nodes.Count) - Mathf.PI / 2;
                positions[node.NodeId] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var wormhole = links.Any(l => l.Kind == GalaxyEdgeKind.Wormhole && (l.EndpointA == node.NodeId || l.EndpointB == node.NodeId));
                var marker = new Button(() => nodeSelected(node.NodeId)) { text = (node.SystemId.HasValue ? "● " : "◇ ") + node.Label };
                marker.style.backgroundColor = selection.Node == node.NodeId ? new Color(.25f, .45f, .6f) : wormhole ? new Color(.30f, .14f, .42f) : new Color(.10f, .19f, .28f);
                marker.style.color = Color.white; marker.style.width = 170; marker.style.height = 30;
                AddMarker(marker, node.NodeId, new Vector2(-85, -15));
                for (var j = 0; j < node.Fleets.Count; j++)
                {
                    var fleet = node.Fleets[j];
                    var owner = snapshot.Campaign.Civilizations.Single(c => c.CivilizationId == fleet.OwnerCivilizationId);
                    var name = snapshot.Content.Civilizations.Single(c => c.DefinitionId == owner.DefinitionId).DisplayName;
                    var button = new Button(() => fleetSelected(fleet.FleetId)) { text = "▸ " + name + " • " + fleet.ShipIds.Count };
                    button.tooltip = fleet.FleetId.Value;
                    button.style.color = Color.white; button.style.backgroundColor = selection.Fleet == fleet.FleetId ? new Color(.3f, .42f, .24f) : new Color(.12f, .16f, .19f);
                    button.style.width = 180; button.style.height = 25;
                    AddMarker(button, node.NodeId, new Vector2(-90, 19 + j * 27));
                }
            }
            foreach (var edge in links)
            {
                var label = new Label((edge.Kind == GalaxyEdgeKind.Wormhole ? "Wormhole · " : "") + "cost " + edge.TraversalCost);
                label.style.position = Position.Absolute; label.style.color = new Color(.65f, .72f, .8f); label.pickingMode = PickingMode.Ignore;
                Add(label);
                // Cost labels are positioned separately because they belong to links.
                label.userData = edge;
            }
            Place();
        }
        private void AddMarker(VisualElement view, NavigationNodeId node, Vector2 offset)
        { view.style.position = Position.Absolute; Add(view); markers.Add((view, node, offset)); }
        private Vector2 Screen(NavigationNodeId id)
        {
            var radius = Mathf.Max(30, Mathf.Min(contentRect.width * .32f, contentRect.height * .30f));
            return contentRect.center + pan + positions[id] * radius * zoom;
        }
        private void Place()
        {
            foreach (var marker in markers)
            { var p = Screen(marker.node) + marker.offset; marker.view.style.left = p.x; marker.view.style.top = p.y; }
            foreach (var child in Children())
                if (child.userData is NavigationLinkSnapshot edge)
                { var p = (Screen(edge.EndpointA) + Screen(edge.EndpointB)) / 2; child.style.left = p.x + 5; child.style.top = p.y + 5; }
            MarkDirtyRepaint();
        }
        private void DrawLinks(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            foreach (var edge in links)
            {
                painter.strokeColor = edge.Kind == GalaxyEdgeKind.Wormhole ? new Color(.8f, .45f, 1f) : new Color(.23f, .36f, .45f);
                painter.lineWidth = edge.Kind == GalaxyEdgeKind.Wormhole ? 4 : 2;
                painter.BeginPath(); painter.MoveTo(Screen(edge.EndpointA)); painter.LineTo(Screen(edge.EndpointB)); painter.Stroke();
            }
        }
    }
}
