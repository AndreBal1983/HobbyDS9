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
        private const int MaxRecords = 512, MaxRoots = 16, MaxKeyboards = 4;
        private const double DurationSeconds = 1800;
        private const BindingFlags StaticFields = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static bool installed;
        private readonly List<DocumentBinding> documents = new List<DocumentBinding>();
        private readonly Dictionary<VisualElement,int> panels = new Dictionary<VisualElement,int>();
        private readonly Dictionary<Type,OwnFlags> ownFields = new Dictionary<Type,OwnFlags>();
        private readonly Dictionary<int, bool[]> keyboardStates = new Dictionary<int, bool[]>();
        private readonly Dictionary<string, Type?> reflectedTypes = new Dictionary<string, Type?>();
        private InputAction? projectSubmit, providerSubmit;
        private double started, nextBind;
        private int records, nextPanelId, enterDown, enterUp, tabDown, tabUp, keypadDown, keypadUp, spaceDown, spaceUp, projectSubmits, providerSubmits, panelKeys, panelKeyUps, panelSubmits, panelMoves;
        private bool stopped, dirty;

        private sealed class DocumentBinding
        {
            public UIDocument Document=null!;
            public VisualElement Root=null!;
            public VisualElement? PanelRoot;
            public MonoBehaviour? Entry;
            public EventCallback<AttachToPanelEvent> Attached=null!;
            public EventCallback<DetachFromPanelEvent> Detached=null!;
        }
        private sealed class OwnFlags
        {
            public FieldInfo? ReturnHeld,SpaceHeld;
        }

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
            var active=FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude);
            for(int i=documents.Count-1;i>=0;i--)
            {
                var binding=documents[i];
                if(binding.Document==null||!binding.Document.isActiveAndEnabled||Array.IndexOf(active,binding.Document)<0
                    ||!ReferenceEquals(binding.Document.rootVisualElement,binding.Root))
                {UnbindDocument(binding);documents.RemoveAt(i);}
            }
            foreach(var doc in active)
            {
                if(!doc.isActiveAndEnabled||doc.rootVisualElement==null||documents.Count>=MaxRoots)continue;
                bool bound=false;foreach(var binding in documents)if(ReferenceEquals(binding.Root,doc.rootVisualElement)){bound=true;break;}
                if(!bound)BindDocument(doc);
            }
            RefreshPanelBindings();
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

        private void BindDocument(UIDocument doc)
        {
            var binding=new DocumentBinding{Document=doc,Root=doc.rootVisualElement,PanelRoot=doc.rootVisualElement.panel?.visualTree};
            foreach(var component in doc.GetComponents<MonoBehaviour>())
                if(component!=null&&component.GetType().FullName=="HobbyDS9.UnityReview.ReviewEntry"){binding.Entry=component;break;}
            binding.Attached=e=>{if(!stopped&&ReferenceEquals(e.target,binding.Root)){binding.PanelRoot=e.destinationPanel.visualTree;RefreshPanelBindings();}};
            binding.Detached=e=>{if(ReferenceEquals(e.target,binding.Root)){binding.PanelRoot=null;RefreshPanelBindings();}};
            binding.Root.RegisterCallback(binding.Attached);binding.Root.RegisterCallback(binding.Detached);
            documents.Add(binding);dirty=true;
        }
        private static void UnbindDocument(DocumentBinding binding)
        {
            binding.Root.UnregisterCallback(binding.Attached);binding.Root.UnregisterCallback(binding.Detached);binding.PanelRoot=null;
        }
        private void RefreshPanelBindings()
        {
            var current=new HashSet<VisualElement>();
            if(!stopped)foreach(var binding in documents)if(binding.PanelRoot!=null)current.Add(binding.PanelRoot);
            var obsolete=new List<VisualElement>();foreach(var panel in panels.Keys)if(!current.Contains(panel))obsolete.Add(panel);
            foreach(var panel in obsolete){PanelCallbacks(panel,false);panels.Remove(panel);dirty=true;}
            foreach(var panel in current)if(!panels.ContainsKey(panel)){panels.Add(panel,++nextPanelId);PanelCallbacks(panel,true);dirty=true;}
        }
        private void PanelCallbacks(VisualElement panel,bool bind)
        {
            if(bind)
            {
                panel.RegisterCallback<KeyDownEvent>(PanelKey,TrickleDown.TrickleDown);
                panel.RegisterCallback<KeyUpEvent>(PanelKeyUp,TrickleDown.TrickleDown);
                panel.RegisterCallback<NavigationSubmitEvent>(PanelSubmit,TrickleDown.TrickleDown);
                panel.RegisterCallback<NavigationMoveEvent>(PanelMove,TrickleDown.TrickleDown);
                panel.RegisterCallback<FocusInEvent>(PanelFocus,TrickleDown.TrickleDown);
            }
            else
            {
                panel.UnregisterCallback<KeyDownEvent>(PanelKey,TrickleDown.TrickleDown);
                panel.UnregisterCallback<KeyUpEvent>(PanelKeyUp,TrickleDown.TrickleDown);
                panel.UnregisterCallback<NavigationSubmitEvent>(PanelSubmit,TrickleDown.TrickleDown);
                panel.UnregisterCallback<NavigationMoveEvent>(PanelMove,TrickleDown.TrickleDown);
                panel.UnregisterCallback<FocusInEvent>(PanelFocus,TrickleDown.TrickleDown);
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
                previous = new bool[4]; keyboardStates.Add(device.deviceId, previous);
            }
            NamedEdge(keyboard.enterKey, inputEvent, previous, 0, "Enter");
            NamedEdge(keyboard.tabKey, inputEvent, previous, 1, "Tab");
            NamedEdge(keyboard.numpadEnterKey, inputEvent, previous, 2, "KeypadEnter");
            NamedEdge(keyboard.spaceKey, inputEvent, previous, 3, "Space");
        }

        private void NamedEdge(ButtonControl control, InputEventPtr inputEvent, bool[] previous, int index, string key)
        {
            // Read only these four named controls, never a raw buffer or typed text.
            if (!control.ReadValueFromEvent(inputEvent, out float value)) return;
            bool down = value >= control.pressPointOrDefault;
            if (down == previous[index]) return;
            previous[index] = down;
            if (index == 0) { if (down) enterDown++; else enterUp++; }
            else if(index==1) { if (down) tabDown++; else tabUp++; }
            else if(index==2) { if (down) keypadDown++; else keypadUp++; }
            else { if (down) spaceDown++; else spaceUp++; }
            dirty = true; Emit(down ? "raw-down" : "raw-up", key);
        }

        private void ProjectSubmit(InputAction.CallbackContext _) { if (!stopped) { projectSubmits++; dirty = true; Emit("project-submit", "Submit"); } }
        private void ProviderSubmit(InputAction.CallbackContext _) { if (!stopped) { providerSubmits++; dirty = true; Emit("provider-submit", "Submit"); } }
        private void PanelKey(KeyDownEvent evt)
        {
            var key=NamedKey(evt.keyCode);if(stopped||key==null||!CurrentPanel(evt))return;
            panelKeys++;dirty=true;Emit("panel-key",key,evt);
        }
        private void PanelKeyUp(KeyUpEvent evt)
        {
            var key=NamedKey(evt.keyCode);if(stopped||key==null||!CurrentPanel(evt))return;
            panelKeyUps++;dirty=true;Emit("panel-key-up",key,evt);
        }
        private static string? NamedKey(KeyCode key)=>key==KeyCode.Return?"Enter":key==KeyCode.KeypadEnter?"KeypadEnter":key==KeyCode.Space?"Space":key==KeyCode.Tab?"Tab":null;
        private bool CurrentPanel(EventBase evt)=>evt.currentTarget is VisualElement panel&&panels.ContainsKey(panel);
        private void PanelSubmit(NavigationSubmitEvent evt) { if (!stopped&&CurrentPanel(evt)) { panelSubmits++; dirty = true; Emit("panel-submit", "Submit",evt); } }
        private void PanelMove(NavigationMoveEvent evt)
        {
            if (stopped ||!CurrentPanel(evt)|| (evt.direction != NavigationMoveEvent.Direction.Next && evt.direction != NavigationMoveEvent.Direction.Previous)) return;
            panelMoves++; dirty = true; Emit("panel-move", evt.direction.ToString(),evt);
        }
        private void PanelFocus(FocusInEvent evt) { if (!stopped&&CurrentPanel(evt)) {dirty = true;Emit("panel-focus","",evt);} }
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
            foreach(var binding in documents)UnbindDocument(binding);
            RefreshPanelBindings();
            if(records<MaxRecords){records++;EmitFinal(reason);}
            documents.Clear();keyboardStates.Clear();ownFields.Clear();
        }

        private void Emit(string stage, string key = "",EventBase? evt=null)
        {
            if (stopped) return;
            if (records >= MaxRecords-1) { Stop("record-limit"); return; }
            records++; EmitFinal(stage, key,evt);
        }

        private void EmitFinal(string stage, string key = "",EventBase? evt=null)
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
                var eventPanel=evt?.currentTarget as VisualElement;var target=evt?.target as VisualElement;
                DocumentBinding? observed=null;bool inDocument=false;
                foreach (var binding in documents)
                {
                    var root=binding.Root;
                    if (root.panel == null) continue;
                    if (ReferenceEquals(root.panel, focusedPanel)) samePanel = true;
                    if (root.panel.focusController?.focusedElement is VisualElement element)
                        focused = SafeName(element.name) + ":" + element.GetType().Name;
                    if(target!=null&&root.Contains(target))inDocument=true;
                    if(binding.Entry!=null&&(observed==null||ReferenceEquals(binding.PanelRoot,eventPanel)))observed=binding;
                }
                var panelRoot=eventPanel??observed?.PanelRoot;var keys=evt as IKeyboardEvent;
                var row = new Row
                {
                    stage = stage, key = key, elapsedSeconds = Time.realtimeSinceStartupAsDouble - started,
                    frame=Time.frameCount,panelId=panelRoot!=null&&panels.TryGetValue(panelRoot,out var panelId)?panelId:0,
                    eventPhase=evt?.propagationPhase.ToString()??"",eventTarget=ElementName(target),
                    eventTargetIsPanelRoot=target!=null&&ReferenceEquals(target,panelRoot),eventTargetInDocument=inDocument,
                    eventTargetAttached=target?.panel!=null,eventTargetEnabled=target?.enabledInHierarchy??false,
                    eventTargetFocused=target!=null&&ReferenceEquals(target,panelRoot?.panel?.focusController.focusedElement),
                    alt=keys?.altKey??false,ctrl=keys?.ctrlKey??false,command=keys?.commandKey??false,shift=keys?.shiftKey??false,
                    ownReturnHeld=ReadOwnFlag(observed,true),ownSpaceHeld=ReadOwnFlag(observed,false),
                    boundDocuments=documents.Count,boundPanels=panels.Count,
                    appFocused = UnityEngine.Application.isFocused, focusedElement = focused, globalFocusedPanelMatches = samePanel,
                    provider = provider?.GetType().FullName ?? "unavailable", providerEnabled = Bool(ReadStatic(FindType("UnityEngine.InputForUI.EventProvider"), "m_IsEnabled")),
                    providerInitialized = Bool(ReadStatic(FindType("UnityEngine.InputForUI.EventProvider"), "m_IsInitialized")),
                    inputForUIActive = Bool(ReadField(system, "m_IsInputForUIActive")), inputReady = Bool(ReadField(system, "m_IsInputReady")),
                    providerPendingEvents = (ReadField(provider, "m_Events") as ICollection)?.Count ?? -1,
                    projectSubmitAvailable = projectSubmit != null, projectSubmitEnabled = projectSubmit?.enabled ?? false,
                    providerSubmitAvailable = providerSubmit != null, providerSubmitEnabled = providerSubmit?.enabled ?? false,
                    sameSubmitAction = projectSubmit != null && ReferenceEquals(projectSubmit, providerSubmit),
                    projectSubmitPerformed=projectSubmit?.WasPerformedThisFrame()??false,providerSubmitPerformed=providerSubmit?.WasPerformedThisFrame()??false,
                    projectSubmitControls = projectSubmit?.controls.Count ?? -1, providerSubmitControls = providerSubmit?.controls.Count ?? -1,
                    keyboardAvailable = Keyboard.current != null, keyboardEnabled = Keyboard.current?.enabled ?? false,
                    enterDown = enterDown, enterUp = enterUp, tabDown = tabDown, tabUp = tabUp,
                    keypadDown=keypadDown,keypadUp=keypadUp,spaceDown=spaceDown,spaceUp=spaceUp,
                    projectSubmits = projectSubmits, providerSubmits = providerSubmits,
                    panelKeys = panelKeys,panelKeyUps=panelKeyUps, panelSubmits = panelSubmits, panelMoves = panelMoves
                };
                Debug.Log("HOBBYDS9_NATIVE_INPUT_EVIDENCE " + JsonUtility.ToJson(row));
            }
            catch (Exception ex) { Debug.Log("HOBBYDS9_NATIVE_INPUT_EVIDENCE unavailable:" + ex.GetType().Name); }
        }

        private string ReadOwnFlag(DocumentBinding? binding,bool enter)
        {
            var entry=binding?.Entry;if(entry==null)return "unavailable";
            var type=entry.GetType();if(type.FullName!="HobbyDS9.UnityReview.ReviewEntry")return "unavailable";
            if(!ownFields.TryGetValue(type,out var fields))
            {
                fields=new OwnFlags{ReturnHeld=type.GetField("returnHeld",InstanceFields),SpaceHeld=type.GetField("spaceHeld",InstanceFields)};
                ownFields.Add(type,fields);
            }
            // Only our two named latch flags. Never read private focus/input data
            // or invoke a component method; existing ReviewEntry preservation applies.
            return Bool((enter?fields.ReturnHeld:fields.SpaceHeld)?.GetValue(entry));
        }
        private static string ElementName(VisualElement? element)=>element==null?"none":SafeName(element.name)+":"+element.GetType().Name;

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
            public string eventPhase="",eventTarget="",ownReturnHeld="unavailable",ownSpaceHeld="unavailable";
            public double elapsedSeconds;
            public bool appFocused, globalFocusedPanelMatches, projectSubmitAvailable, projectSubmitEnabled, providerSubmitAvailable, providerSubmitEnabled, sameSubmitAction, keyboardAvailable, keyboardEnabled;
            public bool eventTargetIsPanelRoot,eventTargetInDocument,eventTargetAttached,eventTargetEnabled,eventTargetFocused,alt,ctrl,command,shift,projectSubmitPerformed,providerSubmitPerformed;
            public int providerPendingEvents, projectSubmitControls, providerSubmitControls, enterDown, enterUp, tabDown, tabUp, projectSubmits, providerSubmits, panelKeys, panelSubmits, panelMoves;
            public int frame,panelId,boundDocuments,boundPanels,panelKeyUps,keypadDown,keypadUp,spaceDown,spaceUp;
        }
    }
}
