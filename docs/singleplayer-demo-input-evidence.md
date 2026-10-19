# Bounded native keyboard observation candidate

## Current R5 follow-up candidate

19 October 2026. The extension on `codex/demo-tab-focus-route` remains behind the existing explicit `--hobbyds9-native-input-evidence` flag, off by default. It runs at most **1800 seconds / 512 total rows**, with at most 16 current document/panel bindings and four named keyboard-state records. The historical 90-second observer below expired before a later R5 Return failure. That failure has no current event/latch evidence and may involve external window-focus steering; it is not attributed to a latch or provider defect.

The extension follows each live document's current public panel visual tree, including attach/detach/replacement. Obsolete panel callbacks are removed, current shared panels are bound once, and stopped/disabled/disposed/quit observers remove their subscriptions. Public trickle-down callbacks observe only Return, keypad Enter, Space and Tab KeyDown/KeyUp, Submit, next/previous navigation, focus and application-focus changes. Named raw controls remain individually read; no characters, composition, arbitrary keys, raw buffers, text-field values, device identities, enemy facts or save/order content are collected.

Rows add frame/panel identity, target ID/type, propagation phase, attached/enabled/in-document/panel-root/focused booleans, modifiers, Submit's public performed-this-frame status, and before/after-update context. Read-only reflection is narrowly cached for the project's own `ReviewEntry.returnHeld` and `spaceHeld` flags only. The existing global `HobbyDS9.UnityReview preserve="all"` entry retains those fields; no new linker or application assembly dependency is introduced. The prior approved Unity provider-context reads below remain unchanged, with no new Unity-internal reflection or private focus access added. Public `focusController.focusedElement` supplies focused-element identity.

The observer never consumes events, dispatches navigation, focuses a control, invokes component methods, enables/disables actions, changes settings/providers, or alters UI/gameplay state. Its own counters/subscriptions/cache and normal log output are the only mutations. KeyDown is observed during panel propagation, then the coalesced after-update row records the resulting own latch state; KeyUp may already have passed the product's earlier-registered release callback. These are timestamped observations, not assertions about missing events between bindings or physical OS delivery. Diagnostic logging allocates and can affect timing; performance acceptance must run without the flag.

Two additional actual-control fixtures are authored, not executed: a Return-confirmed failed load from a unique absent slot preserves real dirty combat orders through Draw, then a current-panel KeyUp and navigation rebuild allow a later round confirmation; without release, the later modal must remain open until release. They introduce no Return runtime change and do not prove the cause of the later native failure. The two Tab fixtures and earlier keyboard fixtures retain their separate receiving gates. Frozen R5, its build checkout and owner slots remain untouched.

CPU evidence and independent review are recorded with the candidate. The standalone observer compiler project targets .NET Standard 2.1 against pinned Unity6000.6.5f1 and the actual InputSystem DLL, with warnings as errors. Initial compiler setup failures (missing `Unity.Scripting` reference and an incorrect combined-engine reference), plus a test call's missing contact argument, are preserved as failed receipts. No Unity/GPU/native process runs while the coordinator owns the R5 lease. Graphics receiving, stripped-player reflection/lifecycle verification and actual native behavior remain pending.

## Historical original observer baseline

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
