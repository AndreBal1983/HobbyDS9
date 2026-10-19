#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

[assembly: AlwaysLinkAssembly]

namespace HobbyDS9.NativeInputEvidence
{
    // Operator-only observation. Never enables actions, marks input handled,
    // synthesizes events, replaces a provider, or invokes gameplay/UI commands.
    [Preserve]
    public sealed class NativeInputEvidence : MonoBehaviour
    {
        private const int MaxRecords = 128, MaxRoots = 16, MaxKeyboards = 4;
        private const double DurationSeconds = 90;
        private const BindingFlags StaticFields = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static bool installed;
        private readonly List<VisualElement> roots = new List<VisualElement>();
        private readonly Dictionary<int, bool[]> keyboardStates = new Dictionary<int, bool[]>();
        private readonly Dictionary<string, Type?> reflectedTypes = new Dictionary<string, Type?>();
        private InputAction? projectSubmit, providerSubmit;
        private double started, nextBind;
        private int records, enterDown, enterUp, tabDown, tabUp, projectSubmits, providerSubmits, panelKeys, panelSubmits, panelMoves;
        private bool stopped, dirty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (installed || Array.IndexOf(Environment.GetCommandLineArgs(), "--hobbyds9-native-input-evidence") < 0) return;
            installed = true;
            var owner = new GameObject("HobbyDS9 Native Input Evidence");
            DontDestroyOnLoad(owner);
            owner.AddComponent<NativeInputEvidence>();
        }

        private void Awake()
        {
            started = Time.realtimeSinceStartupAsDouble;
            InputSystem.onEvent += InputEvent;
            Emit("installed");
        }

        private void Update()
        {
            if (stopped) return;
            var now = Time.realtimeSinceStartupAsDouble;
            if (now - started >= DurationSeconds) { Stop("timeout"); return; }
            if (now < nextBind) return;
            nextBind = now + 1;
            foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude))
            {
                var root = doc.rootVisualElement;
                if (root == null || roots.Contains(root) || roots.Count >= MaxRoots) continue;
                roots.Add(root);
                root.RegisterCallback<KeyDownEvent>(PanelKey, TrickleDown.TrickleDown);
                root.RegisterCallback<NavigationSubmitEvent>(PanelSubmit, TrickleDown.TrickleDown);
                root.RegisterCallback<NavigationMoveEvent>(PanelMove, TrickleDown.TrickleDown);
                root.RegisterCallback<FocusInEvent>(PanelFocus, TrickleDown.TrickleDown);
                dirty = true;
            }
            var project = InputSystem.actions?.FindAction("UI/Submit", false);
            var provider = ReadField(ReadProvider(), "m_SubmitAction") as InputAction;
            if (!ReferenceEquals(projectSubmit, project))
            {
                if (projectSubmit != null) projectSubmit.performed -= ProjectSubmit;
                projectSubmit = project;
                if (projectSubmit != null) projectSubmit.performed += ProjectSubmit;
                dirty = true;
            }
            if (!ReferenceEquals(providerSubmit, provider))
            {
                if (providerSubmit != null) providerSubmit.performed -= ProviderSubmit;
                providerSubmit = provider;
                if (providerSubmit != null) providerSubmit.performed += ProviderSubmit;
                dirty = true;
            }
        }

        private void LateUpdate()
        {
            if (!stopped && dirty) { dirty = false; Emit("after-update"); }
        }

        private void InputEvent(InputEventPtr inputEvent, InputDevice device)
        {
            if (stopped || !(device is Keyboard keyboard) ||
                (inputEvent.type != StateEvent.Type && inputEvent.type != DeltaStateEvent.Type)) return;
            if (!keyboardStates.TryGetValue(device.deviceId, out var previous))
            {
                if (keyboardStates.Count >= MaxKeyboards) return;
                previous = new bool[2]; keyboardStates.Add(device.deviceId, previous);
            }
            NamedEdge(keyboard.enterKey, inputEvent, previous, 0, "Enter");
            NamedEdge(keyboard.tabKey, inputEvent, previous, 1, "Tab");
        }

        private void NamedEdge(ButtonControl control, InputEventPtr inputEvent, bool[] previous, int index, string key)
        {
            // Read only these two named controls, never a raw buffer or typed text.
            if (!control.ReadValueFromEvent(inputEvent, out float value)) return;
            bool down = value >= control.pressPointOrDefault;
            if (down == previous[index]) return;
            previous[index] = down;
            if (index == 0) { if (down) enterDown++; else enterUp++; }
            else { if (down) tabDown++; else tabUp++; }
            dirty = true; Emit(down ? "raw-down" : "raw-up", key);
        }

        private void ProjectSubmit(InputAction.CallbackContext _) { if (!stopped) { projectSubmits++; dirty = true; Emit("project-submit", "Submit"); } }
        private void ProviderSubmit(InputAction.CallbackContext _) { if (!stopped) { providerSubmits++; dirty = true; Emit("provider-submit", "Submit"); } }
        private void PanelKey(KeyDownEvent evt)
        {
            if (stopped || (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.Tab)) return;
            panelKeys++; dirty = true; Emit("panel-key", evt.keyCode == KeyCode.Return ? "Enter" : "Tab");
        }
        private void PanelSubmit(NavigationSubmitEvent _) { if (!stopped) { panelSubmits++; dirty = true; Emit("panel-submit", "Submit"); } }
        private void PanelMove(NavigationMoveEvent evt)
        {
            if (stopped || (evt.direction != NavigationMoveEvent.Direction.Next && evt.direction != NavigationMoveEvent.Direction.Previous)) return;
            panelMoves++; dirty = true; Emit("panel-move", evt.direction.ToString());
        }
        private void PanelFocus(FocusInEvent _) { if (!stopped) dirty = true; }
        private void OnApplicationFocus(bool _) { if (!stopped) { dirty = true; Emit("application-focus"); } }
        private void OnApplicationQuit() { Stop("quit"); }
        private void OnDisable() { Stop("disabled"); }
        private void OnDestroy() { Stop("disposed"); installed = false; }

        private void Stop(string reason)
        {
            if (stopped) return;
            stopped = true;
            InputSystem.onEvent -= InputEvent;
            if (projectSubmit != null) projectSubmit.performed -= ProjectSubmit;
            if (providerSubmit != null) providerSubmit.performed -= ProviderSubmit;
            foreach (var root in roots)
            {
                root.UnregisterCallback<KeyDownEvent>(PanelKey, TrickleDown.TrickleDown);
                root.UnregisterCallback<NavigationSubmitEvent>(PanelSubmit, TrickleDown.TrickleDown);
                root.UnregisterCallback<NavigationMoveEvent>(PanelMove, TrickleDown.TrickleDown);
                root.UnregisterCallback<FocusInEvent>(PanelFocus, TrickleDown.TrickleDown);
            }
            EmitFinal(reason); roots.Clear(); keyboardStates.Clear();
        }

        private void Emit(string stage, string key = "")
        {
            if (stopped) return;
            if (records >= MaxRecords) { Stop("record-limit"); return; }
            records++; EmitFinal(stage, key);
        }

        private void EmitFinal(string stage, string key = "")
        {
            try
            {
                var provider = ReadProvider();
                var utility = FindType("UnityEngine.UIElements.UIElementsRuntimeUtility");
                // Read a field, not the lazy defaultEventSystem getter: observation
                // must not instantiate/activate an event system.
                var system = ReadStatic(utility, "s_DefaultEventSystem");
                object? focusedPanel = ReadField(system, "m_FocusedPanel");
                var focused = "none"; bool samePanel = false;
                foreach (var root in roots)
                {
                    if (root.panel == null) continue;
                    if (ReferenceEquals(root.panel, focusedPanel)) samePanel = true;
                    if (root.panel.focusController?.focusedElement is VisualElement element)
                        focused = SafeName(element.name) + ":" + element.GetType().Name;
                }
                var row = new Row
                {
                    stage = stage, key = key, elapsedSeconds = Time.realtimeSinceStartupAsDouble - started,
                    appFocused = UnityEngine.Application.isFocused, focusedElement = focused, globalFocusedPanelMatches = samePanel,
                    provider = provider?.GetType().FullName ?? "unavailable", providerEnabled = Bool(ReadStatic(FindType("UnityEngine.InputForUI.EventProvider"), "m_IsEnabled")),
                    providerInitialized = Bool(ReadStatic(FindType("UnityEngine.InputForUI.EventProvider"), "m_IsInitialized")),
                    inputForUIActive = Bool(ReadField(system, "m_IsInputForUIActive")), inputReady = Bool(ReadField(system, "m_IsInputReady")),
                    providerPendingEvents = (ReadField(provider, "m_Events") as ICollection)?.Count ?? -1,
                    projectSubmitAvailable = projectSubmit != null, projectSubmitEnabled = projectSubmit?.enabled ?? false,
                    providerSubmitAvailable = providerSubmit != null, providerSubmitEnabled = providerSubmit?.enabled ?? false,
                    sameSubmitAction = projectSubmit != null && ReferenceEquals(projectSubmit, providerSubmit),
                    projectSubmitControls = projectSubmit?.controls.Count ?? -1, providerSubmitControls = providerSubmit?.controls.Count ?? -1,
                    keyboardAvailable = Keyboard.current != null, keyboardEnabled = Keyboard.current?.enabled ?? false,
                    enterDown = enterDown, enterUp = enterUp, tabDown = tabDown, tabUp = tabUp,
                    projectSubmits = projectSubmits, providerSubmits = providerSubmits,
                    panelKeys = panelKeys, panelSubmits = panelSubmits, panelMoves = panelMoves
                };
                Debug.Log("HOBBYDS9_NATIVE_INPUT_EVIDENCE " + JsonUtility.ToJson(row));
            }
            catch (Exception ex) { Debug.Log("HOBBYDS9_NATIVE_INPUT_EVIDENCE unavailable:" + ex.GetType().Name); }
        }

        private Type? FindType(string name)
        {
            if (reflectedTypes.TryGetValue(name, out var known)) return known;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var found = assembly.GetType(name, false);
                if (found != null) { reflectedTypes[name] = found; return found; }
            }
            return null;
        }
        private object? ReadProvider() { return ReadStatic(FindType("UnityEngine.InputForUI.EventProvider"), "s_impl"); }
        private static object? ReadStatic(Type? type, string field) { return type?.GetField(field, StaticFields)?.GetValue(null); }
        private static object? ReadField(object? owner, string field) { return owner?.GetType().GetField(field, InstanceFields)?.GetValue(owner); }
        private static string Bool(object? value) { return value is bool flag ? (flag ? "true" : "false") : "unavailable"; }
        private static string SafeName(string value) { return value.Length > 96 ? value.Substring(0, 96) : value; }

        [Serializable, Preserve]
        private sealed class Row
        {
            public string stage = "", key = "", focusedElement = "", provider = "", providerEnabled = "", providerInitialized = "", inputForUIActive = "", inputReady = "";
            public double elapsedSeconds;
            public bool appFocused, globalFocusedPanelMatches, projectSubmitAvailable, projectSubmitEnabled, providerSubmitAvailable, providerSubmitEnabled, sameSubmitAction, keyboardAvailable, keyboardEnabled;
            public int providerPendingEvents, projectSubmitControls, providerSubmitControls, enterDown, enterUp, tabDown, tabUp, projectSubmits, providerSubmits, panelKeys, panelSubmits, panelMoves;
        }
    }
}
