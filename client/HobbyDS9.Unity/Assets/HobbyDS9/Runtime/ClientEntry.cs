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
        private CombatOrchestrator combat = null!;
        private string? lastCombatFocus;
        private GalaxySelection selection = new GalaxySelection();
        private GalaxyMap map = null!;
        private Label status = null!, message = null!;
        private Button endTurn = null!;
        private ScrollView details = null!;
        private PanelSettings panel = null!;
        private ProductionOverviewView production = null!;
        private LcarsShell shell = null!;
        private VisualElement? uiRoot;
        private IVisualElementScheduledItem? scaleWatcher;
        private int previousViewportHeight;
        private void MatchReadableScale()
        {
            var height = Math.Max(1,Screen.height);
            if (previousViewportHeight == height) return;
            previousViewportHeight = height;
            var scale = (float)ResponsiveLayoutPolicy.PanelScaleMultiplier(height);
            if (!Mathf.Approximately(panel.scale,scale)) panel.scale = scale;
        }
        private void OnViewportGeometry(GeometryChangedEvent _) => MatchReadableScale();
        private void OnDisable()
        {
            scaleWatcher?.Pause(); uiRoot?.UnregisterCallback<GeometryChangedEvent>(OnViewportGeometry);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (UnityEngine.Object.FindAnyObjectByType<ClientEntry>(FindObjectsInactive.Include) != null) return;
            var go = new GameObject("HobbyDS9 Client");
            DontDestroyOnLoad(go); go.AddComponent<ClientEntry>();
        }
        private void OnEnable()
        {
            AudioDirector.TryGetOrCreate();
            if (panel != null) Destroy(panel);
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            previousViewportHeight = 0;
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; panel.match = 1;
            panel.themeStyleSheet = UnityEngine.Resources.Load<ThemeStyleSheet>("HobbyTheme");
            var document = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>(); document.panelSettings = panel;
            var root = document.rootVisualElement; root.Clear(); root.styleSheets.Clear();
            root.AddToClassList("client-root");
            root.styleSheets.Add(UnityEngine.Resources.Load<StyleSheet>("HobbyClient"));
            root.styleSheets.Add(UnityEngine.Resources.Load<StyleSheet>("LcarsTokens"));
            root.styleSheets.Add(UnityEngine.Resources.Load<StyleSheet>("Lcars"));
            map = new GalaxyMap(); production = new ProductionOverviewView();
            shell = new LcarsShell(map, production,
                section => { if (host == null) return; activeTab = section; Refresh(); },
                () => Run(() =>
                {
                    ClearRoute(); var result = host.EndTurn();
                    if (result.Status != TurnStatus.Rejected) combat.Pump();
                    if (combat.Error != null) throw new InvalidOperationException(combat.Error);
                    if (result.Status == TurnStatus.Rejected) throw new InvalidOperationException(result.ErrorCode.ToString());
                    return combat.Notice ?? (result.Status == TurnStatus.AwaitingCombat ? "Encounter checkpoint reached. Resolve the pending battles." : "Turn completed.");
                }), () => map.ResetView());
            status = shell.Header.Primary; message = shell.Message; endTurn = shell.Header.EndTurn; details = shell.Details;
            root.Add(shell);
            // Height-based scaling with a readability floor for small windows.
            // Geometry callbacks converge: scale depends only on physical height,
            // layout decisions depend only on panel logical bounds; no Update loop.
            uiRoot = root; MatchReadableScale();
            root.RegisterCallback<GeometryChangedEvent>(OnViewportGeometry);
            // A same-aspect resize can leave reference-panel bounds unchanged.
            // Poll only physical height, at 5Hz; a change to the readability scale
            // then produces the normal logical GeometryChanged layout callback.
            scaleWatcher = root.schedule.Execute(MatchReadableScale).Every(200);
            try
            {
                var asset = UnityEngine.Resources.Load<TextAsset>("ds9-slice");
                if (asset == null) throw new InvalidOperationException("Scenario missing. Run dotnet msbuild scripts/PrepareUnity.proj before opening Unity.");
                host = new CampaignHost(asset.bytes);
                combat = new CombatOrchestrator(host.Client); combat.Pump();
                saves = new SaveSlotStore(System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "CampaignSaves"));
                Refresh();
            }
            catch (Exception error) { endTurn.SetEnabled(false); message.text = error.Message; Debug.LogException(error); }
        }
        private void OnDestroy() { if (panel != null) Destroy(panel); }
        private void Run(Func<string> operation)
        {
            try { message.text = operation(); message.EnableInClassList("lcars-alert",false); }
            catch (Exception error) { message.text = error.Message; message.EnableInClassList("lcars-alert",true); Debug.LogException(error); }
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
                + $"  |  Turn {state.TurnNumber} • {state.Phase}";
            shell.Header.Secondary.text = "Research: " + researchName + " • Reserve " + (actor?.ResearchReserve ?? 0);
            shell.Header.Diagnostic.text = "Authoritative revision " + state.StateRevision;
            endTurn.SetEnabled(state.Phase == SessionPhase.Planning);
            shell.Header.EnableInClassList("combat-attention",state.Phase == SessionPhase.AwaitingCombat && combat.RequiresPlayer);
            var combatFocus = combat.ReportToReview ?? combat.PendingHumanEncounter ?? (combat.RequiresPlayer ? "complete-turn" : null);
            if (state.Phase == SessionPhase.AwaitingCombat && combat.RequiresPlayer &&
                (lastPhase != state.Phase || combatFocus != lastCombatFocus)) { activeTab = "Combat"; ClearRoute(); }
            lastCombatFocus = combatFocus;
            if (lastPhase != state.Phase && state.Phase == SessionPhase.Planning && activeTab == "Combat") activeTab = "Empire";
            lastPhase = state.Phase;
            map.Present(snapshot, selection, SelectNode, id => { ClearRoute(); selection.SelectFleet(id); activeTab = "Selection"; Refresh(); });
            map.SetRoute(routePreview);
            shell.SelectSection(activeTab);
            production.Present(new ProductionOverview(host.Client, snapshot), Run);
            details.Clear();
            switch (activeTab)
            {
                case "Audio": ShowAudio(); break;
                case "Research": ShowResearch(snapshot); break;
                case "Saves": ShowSaves(); break;
                case "Empire": ShowEmpire(snapshot); break;
                case "Combat":
                    ShowEncounters(snapshot);
                    break;
                default: ShowSelection(snapshot); break;
            }
        }
        private void ShowEncounters(CampaignSnapshot snapshot)
        {
            var view = new CombatPanelView();
            view.Present(snapshot, host.Client, combatPlans, Run, combat); details.Add(view);
        }
        private void Text(string text, bool heading = false)
        {
            var label = heading ? new LcarsSectionHeader(text) : new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
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
