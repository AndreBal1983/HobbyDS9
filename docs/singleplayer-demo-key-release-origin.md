# Demo held-key release lifecycle

19 October 2026. **CPU/source candidate; revised Unity fixtures and native behavior unrun.** Branch `codex/demo-key-release-origin` starts at frozen R6 `d64209dfc75a5c53747599dbd1ea6cce8ccc66df`. Existing R6 packages, build checkouts and owner saves are preserved.

## Evidence and rejected approach

The owner's R6 `player-input-1280.log` records Start Return at 106.912 seconds, followed by a replacement panel with `ownReturnHeld=true`. At 228.151 seconds an unmodified Return reached focused, enabled modal Confirm with no performed Submit action and the old held flag still true; activation was suppressed. Its KeyUp cleared the latch and the next distinct Return confirmed once. No initial Start release was observed on the new panel. This proves the suppression cause, not the initial release's exact route or absence from Windows.

A first button-local release candidate compiled, but its direct `SendEvent` on a detached Start could not dispatch. It was rejected before commit. Its exact source/tests and compiler/static receipts remain in `artifacts/demo-key-release-origin/failed-origin-only` and the original `api-compile` directory, not active runtime code. They are not engine passes.

Read-only PE/IL inspection of installed Unity 6000.6.5f1 `UnityEngine.UIElementsModule.dll` (SHA256 `b1176fe6d880c8d9988a7d3e6c160f73d8f6ec5de270119a132746bee84a7eb6`) establishes:

- `VisualElement.SendEvent` immediately returns when its elementPanel is null.
- Callback dispatch rejects a target whose elementPanel differs from the dispatch panel.
- `DefaultEventSystem.SendFocusBasedEvent` assigns the cached previous focused element, sends via the previous panel visual tree, then updates focus. Merely retaining the panel while discarding the cached target is insufficient.

Inspection uses `PEReader`, never loads/invokes Unity, and lives under the ignored investigation artifacts. Its initial C# language-version setup failure is retained separately. These source facts constrain the candidate; they do not reproduce the actual native event route.

## Revised behavior

The demo now keeps its owned PanelSettings/document root across Build. Other profiles keep their panel-replacement behavior. Every rebuild still invalidates modal permission, unregisters old geometry/keyboard subscriptions and disposes/rebinds audio. Existing caller epoch changes remain. Disable/destroy release the appropriate resources.

When a redraw removes a held-key initiating button, retain that **actual control** in a hidden container on the original panel visual tree, outside the visible document. The control is disabled and nonfocusable; the container is undisplayed and ignores picking. This keeps the cached original target attached to its original panel while preventing obsolete activation, focus, visible duplicate controls or inclusion in document queries. There are at most two distinct retained controls: the accepted Return-group origin and Space origin. Shared-origin dual-key holds retain one control until both releases. Repeated rebuilds cannot accumulate historical controls.

Retention runs after Build's root clear, after integrated Draw, and finally after guarded button actions, covering direct central-tab Render callbacks as well as footer/content clearing and ClearConfirmation → Draw. It neither executes gameplay nor changes order/save state. Normal visible origins stay in place. No renderer/model/art change is required.

The existing current-panel trickle KeyUp observer can receive an event actually addressed to the original retained control. Release must match the accepted physical key and have a public event timestamp at least as new as the accepted KeyDown. Old different-origin, nonfocused targets cannot clear a newer hold. A valid current focused descendant or panel-root release remains supported. Removed retained controls detach immediately once no held key owns them; focus-loss, disable and destroy remove the container and clear origins. Main Return/keypad Enter share activation suppression but have distinct release codes; Space remains independent.

Both ordinary fallback KeyDown and real already-performed Submit record the accepted origin/code/timestamp. The latter still defers activation to ordinary queued NavigationSubmit. Repeat/duplicate and guarded-click/modal protections remain. Public UI events have no OS press ID; this is bounded route/correlation protection, not perfect identification of arbitrary fabricated events.

There is no hold reset on Build, expiry timer, polling, provider/action configuration change, private engine reflection or synthetic runtime input. Observer field identities remain. The [existing fallback](singleplayer-demo-keyboard-controls.md) and [panel-release explanation](singleplayer-demo-keyboard-release-route.md) are historical foundations; their replaced-panel behavior is intentionally superseded for the demo only.

## Verification and remaining gates

CPU receipts: `artifacts/demo-key-release-origin`.

- Portable PrepareUnity and PrepareUnityDemo targets passed; no Unity process launched.
- Binding/integrated source checks passed. Binding static adversarial tests: **22/22** (14 retained and eight new); integrated static tests: **6/6**.
- Eight pinned split compiler outputs and assembly/bootstrap metadata passed in `api-compile-revised`: actual Player/Editor review runtime and test sources compile against pinned Unity/InputSystem/NUnit assemblies. Review runtime/editor have no warnings. Inherited default obsolete-API warnings and `DemoRuntimeAcceptanceTests.cs:69` nullable warning remain; none originates in changed source.
- Final compiler/source hashes, fixture inventory, documentation links and diff checked before delivery.

The 11 existing keyboard cases remain; demo panel-identity assertions change to intentional stable lifetime. Their held/no-release, disabled/detached/modifier, duplicate Submit, stale control, binding/unbinding, document-detach, Tab and failed-load behavior checks remain. Unrelated-panel tests now use a real separate test-owned UIDocument rather than treating the retained demo panel as obsolete. Failed-load cases release through the actual removed Confirm on the original panel, proving the proposed route rather than substituting a new panel-root release.

Thirteen additional authored fixtures cover actual Start Return/keypad Enter/Space followed by release through the **original panel's** public SendEvent with target set to the **actual retained Start**; held Start; stale released Start versus a newer retained actual End-turn Confirm; attached disabled stale controls; exact key separation; focus-loss/disable cleanup; queued earlier timestamp; shared/dual-origin bounds; direct CentralView tab rendering; and real performed Submit → explicitly arranged NavigationSubmit → Start rebuild → retained-origin release. Successful command paths assert exactly one real End-turn transition. Fixtures assert same original-panel membership, disabled/nonfocusable hidden retention, no activation of retired controls and detachment after release. The keyboard fixture now contains 24 cases; the previous six other receiving cases are unchanged.

**Not executed:** the revised 24 Unity keyboard fixtures, combined receiving selection (30 cases if the prior six are selected), actual Editor import/asmdef/test execution, IL2CPP build, GPU/native or physical keyboard acceptance. The real InputAction fixture arranges navigation explicitly and does not claim automatic provider delivery. Unchanged portable gameplay suites were not repeated for this input-only change.

Independent exact-source review must precede receiving. The owner retains the sole Unity/GPU/native lease. Then run all retained/new fixtures, strict build and fresh native Start → released key → End turn → released key → subsequent confirmation checks, including real-action duplicate prevention, held repeats, modifiers, focus loss and stale controls. If the initial native release still never reaches the retained original-panel route, this candidate remains insufficient. Preserve failed receipts and investigate before another lifecycle change; do not call compiler success a verified native fix or overwrite frozen R6.
