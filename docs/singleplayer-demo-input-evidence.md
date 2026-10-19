# Bounded native keyboard observation candidate

19 October 2026. **Optional diagnostic source, CPU-compiled only; not a keyboard fix or native acceptance.** Separate branch `codex/singleplayer-demo-input-evidence` starts at reviewed frontend `fbf78198ab6684698b7d74ecdaeb711f1aaea80c`. Frozen PR #45 and the delivered Windows review package remain unchanged. The coordinator owns desktop/native/graphics use; no Unity process or input was launched for this investigation.

The actual packaged player accepted mouse Start/purchase and Escape cancellation, but primary Return and Tab did not activate/navigate. The batch Input System fixture also failed Enter-start. Source inspection found normal UI Toolkit Button click handlers and no Submit/Tab suppression in the application. The pinned InputSystemProvider registers `UI/Submit.performed` to queue a navigation event; Tab instead uses a separately enabled fixed `<Keyboard>/tab` action. A bad authored Submit binding alone therefore cannot explain both failures. Provider activation, key delivery and global panel focus are still unobserved runtime facts, not established causes.

## Observer boundary

New independent assembly `HobbyDS9.NativeInputEvidence` depends on Unity engine modules and `Unity.InputSystem` only. It has no Application, Simulation, host, save, campaign or existing NativeAcceptanceProbe dependency. Bootstrap returns immediately unless `--hobbyds9-native-input-evidence` is present. Existing launch/default/build flags are unchanged. The new assembly uses AlwaysLinkAssembly and targeted linker metadata so a later diagnostic IL2CPP build can retain its bootstrap and the observed fields.

The reflected provider is actually declared in the separate `Unity.InputSystem.ForUI` assembly. Its field-preservation entry names that assembly; `Unity.InputSystem` is only the observer's public typed-input dependency. Independent review caught the initial incorrect provider assembly name before any diagnostic build; the corrected source still needs actual linker/native verification.

Only primary Enter and Tab state edges are inspected from Input System events; no raw buffers, arbitrary keys, text/composition, physical device names/serials or campaign data are logged. Project and actual-provider Submit actions receive separate passive callbacks; if they are the same action, both counters observe the same performance, **not two submissions**. Public UI Toolkit trickle-down callbacks record named key events, Submit and next/previous navigation arrivals and focused element names. No event is consumed or marked handled.

Read-only reflection inspects the pinned provider's name, enabled/initialized flags, current provider Submit identity, pending-event **count**, DefaultEventSystem input-ready/InputForUI flags and global focused-panel match. It reads `s_DefaultEventSystem` directly rather than invoking the lazy getter, so the observer cannot instantiate an event system. Missing metadata is reported as unavailable. No private provider method is invoked; no provider is replaced, action enabled/disabled, setting changed, focus set, event synthesized, or UI/gameplay command submitted.

InputForUI's EventProvider/EventConsumer types are internal in the actual pinned engine metadata. A direct global event-stream subscription would require an unsupported bridge; this candidate deliberately avoids one. Provider queue counts are contextual samples and do **not** prove global navigation dispatch or distinguish queued pointer events. The actual UI Toolkit NavigationSubmit/Move callbacks prove arrival at the observed panel; zero arrivals remain a diagnostic gap, not evidence of a particular engine bug.

The observer runs for at most 90 seconds with 128 ordinary rows plus one final row, at most 16 UI roots and four keyboard states. Roots/action subscriptions are checked once a second, not every frame. All subscriptions are removed at timeout, record cap, disable, disposal or quit. Output is only `HOBBYDS9_NATIVE_INPUT_EVIDENCE` JSON lines in the normal player log. It writes no separate files or preferences. Logging/reflection allocates and can affect frame timing, so native performance acceptance must run without this flag.

## CPU verification and next experiment

The new source compiled against pinned Unity 6000.6.5f1 engine references and the receiving project's actual `Unity.InputSystem.dll`, targeting .NET Standard 2.1/C# 9 with warnings as errors: **zero warnings/errors**. This verifies the concrete APIs, not Unity asmdef import, callback behavior, reflection under IL2CPP, linker execution or native input. The metadata probe reads PE tables without loading/executing Unity assemblies; ignored investigation artifacts are under `artifacts/singleplayer-demo-ui/input-public-metadata` and `input-observer-compile`.

After independent source review and an explicitly coordinated graphics/build slot, create a new diagnostic native package; never modify the frozen package. Launch the dedicated demo with the optional evidence flag and a fresh external log path. Within 90 seconds, arrange actual window/button focus using normal UI input, then primary Enter and Tab once each. Do not bypass the UI, submit backend commands or infer keyboard success from direct NavigationSubmit injection.

Interpret the same short timeline:

| Observation | Next investigation; not an automatic production fix |
| --- | --- |
| No named raw edges | Check actual OS/injection delivery and Input System device/update visibility; panel dispatch cannot explain an absent lower-layer edge |
| Raw Enter edge, no actual-provider Submit callback | Inspect recorded provider action availability/enabled state, identity and configured control resolution; no blind Enable/Enter shortcut |
| Submit callback but no panel Submit | Inspect actual provider-ready/global panel-focus states and source dispatch path; queue count alone is insufficient |
| Panel Submit arrives but no action | Inspect focused element, enabled hierarchy and existing guarded button/modal lifetime; retain once-only authority checks |
| Tab raw edges with no next/previous panel move | Inspect fixed-action polling/update timing and active provider/global panel focus; a custom Submit binding is irrelevant to this fixed Tab action |

No current data settles these branches. Preserve the three failing fixtures and real native evidence until the actual defect or automation limitation is identified and the normal player path is proven.
