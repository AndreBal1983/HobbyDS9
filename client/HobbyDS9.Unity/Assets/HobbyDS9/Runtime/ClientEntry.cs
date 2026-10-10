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
    public sealed class ClientEntry : MonoBehaviour
    {
        private CampaignHost host = null!;
        private readonly GalaxySelection selection = new GalaxySelection();
        private GalaxyMap map = null!;
        private Label status = null!, message = null!;
        private Button endTurn = null!;
        private ScrollView details = null!;
        private PanelSettings panel = null!;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("HobbyDS9 Client");
            DontDestroyOnLoad(go); go.AddComponent<ClientEntry>();
        }
        private void OnEnable()
        {
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; panel.match = 1;
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("HobbyTheme");
            var document = gameObject.AddComponent<UIDocument>(); document.panelSettings = panel;
            var root = document.rootVisualElement;
            root.AddToClassList("client-root");
            root.styleSheets.Add(Resources.Load<StyleSheet>("HobbyClient"));
            var header = new VisualElement(); header.AddToClassList("top-bar");
            header.Add(new Label("HobbyDS9 • Strategic prototype"));
            status = new Label(); status.AddToClassList("campaign-status"); header.Add(status);
            endTurn = new Button(() => Run(() =>
            {
                var result = host.EndTurn();
                if (result.Status == TurnStatus.Rejected) throw new InvalidOperationException(result.ErrorCode.ToString());
                return result.Status == TurnStatus.AwaitingCombat ? "Encounter checkpoint reached. Resolve the pending battles." : "Turn completed.";
            })) { text = "End Turn" };
            header.Add(endTurn);
            var reset = new Button(() => map.ResetView()) { text = "Fit map" }; header.Add(reset); root.Add(header);
            var body = new VisualElement(); body.AddToClassList("workspace");
            map = new GalaxyMap(); body.Add(map);
            details = new ScrollView(); details.AddToClassList("details-panel");
            body.Add(details); root.Add(body);
            message = new Label("Drag empty map to pan • Scroll to zoom • Select nodes or fleet markers");
            message.AddToClassList("message-bar"); root.Add(message);
            try
            {
                var asset = Resources.Load<TextAsset>("ds9-slice");
                if (asset == null) throw new InvalidOperationException("Scenario missing. Run dotnet msbuild scripts/PrepareUnity.proj before opening Unity.");
                host = new CampaignHost(asset.bytes); Refresh();
            }
            catch (Exception error) { endTurn.SetEnabled(false); message.text = error.Message; Debug.LogException(error); }
        }
        private void OnDestroy() { if (panel != null) Destroy(panel); }
        private void Run(Func<string> operation)
        {
            try { message.text = operation(); }
            catch (Exception error) { message.text = error.Message; Debug.LogException(error); }
            finally { if (host != null) { host.Refresh(); Refresh(); } }
        }
        private void Refresh()
        {
            var snapshot = host.Snapshot; var state = snapshot.Campaign;
            selection.Reconcile(snapshot);
            status.text = $"Turn {state.TurnNumber}  •  {state.Phase}  •  Revision {state.StateRevision}";
            endTurn.SetEnabled(state.Phase == SessionPhase.Planning);
            map.Present(snapshot, selection, id => { selection.SelectNode(id); Refresh(); }, id => { selection.SelectFleet(id); Refresh(); });
            details.Clear();
            if (state.Phase == SessionPhase.AwaitingCombat) { ShowEncounters(snapshot); return; }
            if (selection.Fleet.HasValue)
            {
                var fleet = state.Fleets.Single(f => f.FleetId == selection.Fleet.Value);
                Text("Fleet", true); Text(fleet.FleetId.Value); Text("Owner: " + OwnerName(snapshot, fleet.OwnerCivilizationId));
                Text("Location: " + SystemName(snapshot, fleet.LocationSystemId));
                Text("Queued route steps: " + fleet.RouteSystemIds.Count);
                foreach (var ship in state.Ships.Where(s => s.FleetId == fleet.FleetId))
                {
                    var design = state.ShipDesigns.Single(d => d.DefinitionId == ship.ShipDesignDefinitionId);
                    Text(design.DisplayName + " • Hull " + ship.CurrentHull + "/" + design.MaxHull);
                    Text(ship.ShipId.Value);
                }
                return;
            }
            if (selection.Node.HasValue)
            {
                var node = GalaxyProjection.Nodes(snapshot).Single(n => n.NodeId == selection.Node.Value);
                Text(node.Label, true); Text("Navigation node: " + node.NodeId.Value);
                if (node.SystemId.HasValue)
                {
                    var system = state.StarSystems.Single(s => s.SystemId == node.SystemId.Value);
                    Text(system.OwnerCivilizationId.HasValue ? "Owner: " + OwnerName(snapshot, system.OwnerCivilizationId.Value) : "Unowned");
                    foreach (var colony in state.Colonies.Where(c => c.SystemId == system.SystemId))
                        Text($"Population {colony.Population} / {colony.PopulationCapacity}\nFood {colony.Food} • Materials {colony.Materials}\nProduction queue: {colony.ProductionQueue.Count}");
                }
                else Text("Empty-space navigation region; no associated star system.");
                foreach (var link in snapshot.Navigation.Links.Where(l => l.EndpointA == node.NodeId || l.EndpointB == node.NodeId))
                    Text($"{link.Kind} link • traversal cost {link.TraversalCost}");
                foreach (var fleet in node.Fleets)
                    details.Add(new Button(() => { selection.SelectFleet(fleet.FleetId); Refresh(); }) { text = "Select " + fleet.FleetId.Value });
                return;
            }
            Text("Galaxy", true); Text("Select a node to inspect its associated system, or select a fleet marker.");
            Text("Purple links mark wormholes. Costs come from the application snapshot; they are not travel-time estimates.");
            Text("Federation is human-controlled. Cardassian and Dominion turns are automated by the existing simulation.");
        }
        private void ShowEncounters(CampaignSnapshot snapshot)
        {
            var requests = host.Pending();
            Text("Awaiting combat", true);
            Text("Strategic processing is paused. Each result is buffered before the turn can be finalized.");
            Text($"Pending: {requests.Count} • Buffered: {snapshot.Campaign.Combats.Count(c => c.IsResolved)}");
            if (requests.Count == 0)
            {
                details.Add(new Button(() => Run(() =>
                {
                    var result = host.FinalizeCombats();
                    if (result.Status != TurnStatus.Completed) throw new InvalidOperationException(result.ErrorCode.ToString());
                    return "Combat finalized. Next planning turn opened.";
                })) { text = "Finalize combat and complete turn" });
                return;
            }
            var request = requests[0]; Text(SystemName(snapshot, request.LocationSystemId), true);
            var plans = new List<FleetCombatPlanDto>();
            var destinations = snapshot.Campaign.StarSystems.OrderBy(s => s.SystemId.Value, StringComparer.Ordinal).ToList();
            foreach (var participant in request.Participants)
            {
                Text(OwnerName(snapshot, new EntityId(participant.CivilizationId)), true); Text(participant.FleetId);
                foreach (var ship in participant.Ships) Text($"{ship.ShipId}\nHull {ship.Hull}/{ship.MaxHull} • Shields {ship.CombatProfile!.ShieldCapacity}");
                var plan = new FleetCombatPlanDto { FleetId = participant.FleetId, LongRangeTactic = CombatTactic.FocusFire,
                    MediumRangeTactic = CombatTactic.Standard, CloseRangeTactic = CombatTactic.Overcharge, TargetPriority = CombatTargetPriority.LowestHullRatio };
                plans.Add(plan);
                Tactic("Long range", plan.LongRangeTactic, value => plan.LongRangeTactic = value);
                Tactic("Medium range", plan.MediumRangeTactic, value => plan.MediumRangeTactic = value);
                Tactic("Close range", plan.CloseRangeTactic, value => plan.CloseRangeTactic = value);
                var priorities = Enum.GetValues(typeof(CombatTargetPriority)).Cast<CombatTargetPriority>().ToList();
                var priority = new DropdownField("Target priority", priorities.Select(v => v.ToString()).ToList(), priorities.IndexOf(plan.TargetPriority));
                priority.AddToClassList("combat-plan-field");
                priority.RegisterValueChangedCallback(_ => plan.TargetPriority = priorities[priority.index]); details.Add(priority);
                var retreat = new IntegerField("Retreat hull % (0 disables)") { value = 0 };
                var destination = new DropdownField("Retreat system", destinations.Select(s => SystemName(snapshot, s.SystemId)).ToList(), 0);
                destination.AddToClassList("combat-plan-field");
                retreat.AddToClassList("combat-plan-field");
                destination.SetEnabled(false);
                void UpdateRetreat()
                {
                    plan.RetreatHullPercent = Mathf.Clamp(retreat.value, 0, 100); retreat.SetValueWithoutNotify(plan.RetreatHullPercent);
                    destination.SetEnabled(plan.RetreatHullPercent > 0);
                    plan.RetreatSystemId = plan.RetreatHullPercent == 0 ? null : destinations[destination.index].SystemId.Value;
                }
                retreat.RegisterValueChangedCallback(_ => UpdateRetreat()); destination.RegisterValueChangedCallback(_ => UpdateRetreat());
                details.Add(retreat); details.Add(destination);
            }
            Text("Retreat destinations must pass campaign adjacency/occupancy validation. Plan controls apply to this encounter only.");
            details.Add(new Button(() => Resolve(request, null)) { text = "Resolve with defaults" });
            details.Add(new Button(() => Resolve(request, plans)) { text = "Resolve with selected plans" });
        }
        private void Resolve(CombatRequestDto request, IReadOnlyList<FleetCombatPlanDto>? plans) => Run(() =>
        {
            var result = host.Resolve(request, plans);
            if (result.Status != CombatSubmissionStatus.Buffered) throw new InvalidOperationException("Combat rejected: " + result.ErrorCode);
            return "Combat result buffered. Resolve remaining encounters, then finalize.";
        });
        private void Tactic(string label, CombatTactic initial, Action<CombatTactic> set)
        {
            var values = Enum.GetValues(typeof(CombatTactic)).Cast<CombatTactic>().ToList();
            var field = new DropdownField(label, values.Select(v => v.ToString()).ToList(), values.IndexOf(initial));
            field.AddToClassList("combat-plan-field");
            field.RegisterValueChangedCallback(_ => set(values[field.index])); details.Add(field);
        }
        private void Text(string text, bool heading = false)
        {
            var label = new Label(text); label.style.whiteSpace = WhiteSpace.Normal; label.style.marginBottom = 12;
            if (heading) { label.style.fontSize = 23; label.style.unityFontStyleAndWeight = FontStyle.Bold; }
            details.Add(label);
        }
        private static string OwnerName(CampaignSnapshot snapshot, EntityId id)
        {
            var actor = snapshot.Campaign.Civilizations.Single(c => c.CivilizationId == id);
            return snapshot.Content.Civilizations.Single(c => c.DefinitionId == actor.DefinitionId).DisplayName;
        }
        private static string SystemName(CampaignSnapshot snapshot, EntityId id)
        {
            var system = snapshot.Campaign.StarSystems.Single(s => s.SystemId == id);
            return snapshot.Content.StarSystems.Single(s => s.DefinitionId == system.DefinitionId).DisplayName;
        }
        private static string SystemName(CampaignSnapshot snapshot, string id) => SystemName(snapshot, new EntityId(id));
    }
}
