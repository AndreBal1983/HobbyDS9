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
    public sealed partial class ClientEntry : MonoBehaviour
    {
        private CampaignHost host = null!;
        private GalaxySelection selection = new GalaxySelection();
        private GalaxyMap map = null!;
        private Label status = null!, message = null!;
        private Button endTurn = null!;
        private ScrollView details = null!;
        private PanelSettings panel = null!;
        private ProductionOverviewView production = null!;
        private Button productionToggle = null!;
        private bool productionOpen;
        private bool productionVisibilityChosen;
        private void SetProductionVisible(bool open)
        {
            productionOpen = open;
            if (production != null) production.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (productionToggle != null) productionToggle.EnableInClassList("active-tab", open);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("HobbyDS9 Client");
            DontDestroyOnLoad(go); go.AddComponent<ClientEntry>();
        }
        private void OnEnable()
        {
            AudioDirector.TryGetOrCreate();
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; panel.match = 1;
            panel.themeStyleSheet = UnityEngine.Resources.Load<ThemeStyleSheet>("HobbyTheme");
            var document = gameObject.AddComponent<UIDocument>(); document.panelSettings = panel;
            var root = document.rootVisualElement;
            root.AddToClassList("client-root");
            root.styleSheets.Add(UnityEngine.Resources.Load<StyleSheet>("HobbyClient"));
            var header = new VisualElement(); header.AddToClassList("top-bar");
            header.Add(new Label("HobbyDS9"));
            status = new Label(); status.AddToClassList("campaign-status"); header.Add(status);
            endTurn = new Button(() => Run(() =>
            {
                ClearRoute();
                var result = host.EndTurn();
                if (result.Status == TurnStatus.Rejected) throw new InvalidOperationException(result.ErrorCode.ToString());
                return result.Status == TurnStatus.AwaitingCombat ? "Encounter checkpoint reached. Resolve the pending battles." : "Turn completed.";
            })) { text = "End Turn" };
            header.Add(endTurn);
            var reset = new Button(() => map.ResetView()) { text = "Fit map" }; header.Add(reset); root.Add(header);
            AddStrategicTabs(root);
            var body = new VisualElement(); body.AddToClassList("workspace");
            production = new ProductionOverviewView(); body.Add(production);
            // Height-scaled UI: ultrawide layouts have extra logical width. A user
            // toggle always wins over the initial responsive docking preference.
            body.RegisterCallback<GeometryChangedEvent>(e =>
            { if (!productionVisibilityChosen) SetProductionVisible(e.newRect.width >= 2300); });
            SetProductionVisible(false);
            map = new GalaxyMap(); body.Add(map);
            details = new ScrollView(); details.AddToClassList("details-panel");
            body.Add(details); root.Add(body);
            message = new Label("Drag empty map to pan • Scroll to zoom • Select nodes or fleet markers");
            message.AddToClassList("message-bar"); root.Add(message);
            try
            {
                var asset = UnityEngine.Resources.Load<TextAsset>("ds9-slice");
                if (asset == null) throw new InvalidOperationException("Scenario missing. Run dotnet msbuild scripts/PrepareUnity.proj before opening Unity.");
                host = new CampaignHost(asset.bytes);
                saves = new SaveSlotStore(System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "CampaignSaves"));
                Refresh();
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
            var actor = Human(snapshot);
            var research = actor?.ResearchTargetDefinitionId;
            var researchName = research.HasValue ? ResearchName(snapshot, research.Value) : "No target";
            status.text = (actor == null ? "Observer" : OwnerName(snapshot, actor.CivilizationId) + " • Credits " + actor.Credits)
                + $"  |  Turn {state.TurnNumber} • {state.Phase} • Revision {state.StateRevision}"
                + "\nResearch: " + researchName + " • Reserve " + (actor?.ResearchReserve ?? 0);
            endTurn.SetEnabled(state.Phase == SessionPhase.Planning);
            if (lastPhase != state.Phase && state.Phase == SessionPhase.AwaitingCombat) { activeTab = "Combat"; ClearRoute(); }
            if (lastPhase != state.Phase && state.Phase == SessionPhase.Planning && activeTab == "Combat") activeTab = "Empire";
            lastPhase = state.Phase;
            map.Present(snapshot, selection, SelectNode, id => { ClearRoute(); selection.SelectFleet(id); activeTab = "Selection"; Refresh(); });
            map.SetRoute(routePreview);
            foreach (var tab in tabs) tab.Value.EnableInClassList("active-tab", tab.Key == activeTab);
            production.Present(new ProductionOverview(host.Client, snapshot), Run);
            details.Clear();
            switch (activeTab)
            {
                case "Audio": ShowAudio(); break;
                case "Research": ShowResearch(snapshot); break;
                case "Saves": ShowSaves(); break;
                case "Empire": ShowEmpire(snapshot); break;
                case "Combat":
                    if (state.Phase == SessionPhase.AwaitingCombat) ShowEncounters(snapshot);
                    else Text("No pending encounters.");
                    break;
                default: ShowSelection(snapshot); break;
            }
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
                    var result = host.Client.FinalizeCombats(snapshot);
                    if (result.Status == TurnStatus.Completed) combatPlans.Clear();
                    if (result.Status != TurnStatus.Completed) throw new InvalidOperationException(result.ErrorCode.ToString());
                    return "Combat finalized. Next planning turn opened.";
                })) { text = "Finalize combat and complete turn" });
                return;
            }
            var request = requests[0]; Text(NodeName(snapshot,new NavigationNodeId(request.LocationNodeId)), true);
            if (!combatPlans.TryGetValue(request.EncounterId, out var plans))
            { plans = new List<FleetCombatPlanDto>(); combatPlans[request.EncounterId] = plans; }
            var destinations = snapshot.Navigation.Nodes.OrderBy(s => s.NodeId.Value, StringComparer.Ordinal).ToList();
            foreach (var participant in request.Participants)
            {
                Text(OwnerName(snapshot, new EntityId(participant.CivilizationId)), true); Text(participant.FleetId);
                foreach (var ship in participant.Ships) Text($"{ship.ShipId}\nHull {ship.Hull}/{ship.MaxHull} • Shields {ship.CombatProfile!.ShieldCapacity}");
                var plan = plans.SingleOrDefault(p => p.FleetId == participant.FleetId);
                if (plan == null)
                {
                    plan = new FleetCombatPlanDto { FleetId = participant.FleetId, LongRangeTactic = CombatTactic.FocusFire,
                        MediumRangeTactic = CombatTactic.Standard, CloseRangeTactic = CombatTactic.Overcharge, TargetPriority = CombatTargetPriority.LowestHullRatio };
                    plans.Add(plan);
                }
                Tactic("Long range", plan.LongRangeTactic, value => plan.LongRangeTactic = value);
                Tactic("Medium range", plan.MediumRangeTactic, value => plan.MediumRangeTactic = value);
                Tactic("Close range", plan.CloseRangeTactic, value => plan.CloseRangeTactic = value);
                var priorities = Enum.GetValues(typeof(CombatTargetPriority)).Cast<CombatTargetPriority>().ToList();
                var priority = new DropdownField("Target priority", priorities.Select(v => v.ToString()).ToList(), priorities.IndexOf(plan.TargetPriority));
                priority.AddToClassList("combat-plan-field");
                priority.RegisterValueChangedCallback(_ => plan.TargetPriority = priorities[priority.index]); details.Add(priority);
                var retreat = new IntegerField("Retreat hull % (0 disables)") { value = plan.RetreatHullPercent };
                var destination = new DropdownField("Retreat region", destinations.Select(s => NodeName(snapshot,s.NodeId)).ToList(), Math.Max(0, destinations.FindIndex(s => s.NodeId.Value == plan.RetreatNodeId)));
                destination.AddToClassList("combat-plan-field");
                retreat.AddToClassList("combat-plan-field");
                destination.SetEnabled(plan.RetreatHullPercent > 0);
                void UpdateRetreat()
                {
                    plan.RetreatHullPercent = Mathf.Clamp(retreat.value, 0, 100); retreat.SetValueWithoutNotify(plan.RetreatHullPercent);
                    destination.SetEnabled(plan.RetreatHullPercent > 0);
                    plan.RetreatNodeId = plan.RetreatHullPercent == 0 ? null : destinations[destination.index].NodeId.Value;
                }
                retreat.RegisterValueChangedCallback(_ => UpdateRetreat()); destination.RegisterValueChangedCallback(_ => UpdateRetreat());
                details.Add(retreat); details.Add(destination);
            }
            Text("Retreat destinations must pass campaign adjacency/occupancy validation. Plan controls apply to this encounter only.");
            details.Add(new Button(() => Resolve(snapshot, request, null)) { text = "Resolve with defaults" });
            details.Add(new Button(() => Resolve(snapshot, request, plans)) { text = "Resolve with selected plans" });
        }
        private void Resolve(CampaignSnapshot observed, CombatRequestDto request, IReadOnlyList<FleetCombatPlanDto>? plans) => Run(() =>
        {
            var result = host.Client.Resolve(observed, request, plans);
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
