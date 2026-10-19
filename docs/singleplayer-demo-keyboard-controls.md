# Demo keyboard control fallback

19 October 2026. **CPU-compiled candidate; receiving Editor and native acceptance pending.** Isolated from frozen R2 source `39bfedd6fa21304466cf198b544522cbf7ef0ac3` and its package. No simulation, persistence, project input configuration or default-profile activation change.

## Observed failure and scope

The coordinator's actual R2 player log at `E:/HobbyDS9/artifacts/native-singleplayer-acceptance-20261019-r2/player-1280.log` records native UI Toolkit Enter and Tab key events reaching a focused Button with the correct focused panel, initialized/enabled InputSystem provider, enabled shared project/provider Submit action and one resolved control. Raw Enter/Tab edges and Submit/navigation counts remained zero. This establishes a split between delivered UI key events and action-driven navigation; it does not establish how a physical keyboard behaves.

Installed InputSystem1.20 `InputSystem/Runtime/Plugins/InputForUI/InputSystemProvider.cs` calls the engine's `InputEventPartialProvider.Update()` at line138 before dispatching queued action navigation at line150. Inspection of the installed Unity6000.6.5f1 engine assembly shows the partial provider reading `UnityEngine.Event` keyboard events separately; Submit comes from the action callback (line513), and fixed Tab navigation polls `IsPressed()` (line259). Button activation handles NavigationSubmit, and the focus ring handles NavigationMove Next/Previous. The installed automation skill describes SendInput, but the helper implementation is unavailable locally: exact injection semantics and physical keyboard behavior remain unverified.

## Control behavior

Only actual single-player demo `AddButton` controls register the extra KeyDown handler. An unmodified Return, keypad Enter or Space must target that exact focused, attached, enabled button. It invokes the **same guarded callback** as the normal click: existing epoch, active component, busy/closed session and modal permission checks remain authoritative. It does not find a guessed action by caption or invoke gameplay directly.

If the actual project UI Submit action already performed this frame, KeyDown does nothing and the ordinary queued NavigationSubmit owns activation. This is necessary because the provider delivers KeyDown before its already queued navigation; otherwise opening one modal could allow the subsequent Submit to confirm it. A Return/Space latch survives view rebuilds until KeyUp or application focus loss/disable, preventing OS repeated KeyDown from activating newly opened modals. No action is enabled or disabled by this code.

One bubbling callback on the demo document handles unconsumed Tab/Shift-Tab only when a live descendant actually owns focus and the current keyboard has neither a held Tab nor a current-frame Tab press. TextField/DropdownField targets and descendants keep their own keys. It sends one standard Next/Previous NavigationMove to that focused element and lets Unity's focus ring select the next control. There is no global navigation dispatcher, Update loop, private FocusController call or provider replacement. Menu primary focus is scheduled only for the current attached epoch and cannot steal an existing valid focus or an open modal.

The review runtime gains an explicit `Unity.InputSystem` assembly reference to read public action/key signals. The split compiler and boundary checker retain explicit allowlists; no Simulation or host-storage dependency is added. Legacy review controls and default startup do not register this behavior.

## Verification boundaries

Five authored actual-UI Editor fixtures cover focused/disabled/detached/modifier/held keys; sequential one-use modals and obsolete controls; local Tab traversal and field/popup/raw-input ownership; a real performed InputAction plus explicitly arranged standard NavigationSubmit to prove no double callback; and initial menu focus versus a scheduled focus/modal race. They deliberately inject direct UI events for control testing. They are **compiled, not executed**, and do not claim OS keyboard, automatic provider delivery or native acceptance. The earlier failing actual InputSystem-provider fixture remains unchanged and must not be relabeled passed.

CPU receipts live under `artifacts/demo-keyboard-controls`: full portable adapter, binding/integrated static checks and pinned split API compiler. The initial overlapping compiler/portable invocation collided over shared DLL outputs; the serial portable retry is the meaningful result. A fresh-worktree static check initially detected CRLF resource copies; normal default/demo Prepare restored authored raw resource bytes before its rerun. Preserve those receipts as setup failures, not runtime failures or hidden passes.

Receiving gates: independent exact-source review; coordinator-authorized Editor execution of the five control cases without weakening assertions; actual native Enter/Tab/Space, both modifier directions and repeated/modal input; ordinary action path duplicate suppression; text fields/dropdowns; save and campaign continuation. Physical keyboard behavior remains unverified until directly exercised. No Unity, GPU, native input or frozen package mutation was performed during implementation.
