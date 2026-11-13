# TURN-POPUP-01 — existing standardized popup reference

13 November 2026. **Recovery/reference handoff; no new popup design or runtime implementation.** Same isolated worktree/branch as SYS-VIS-01: `C:/HobbyDS9/artifacts/worktrees/SYS-VIS-01-art`, `codex/sys-vis-01-art-direction`, migration source `b9401ce175c02ed512e9ae0e675e456b19b2a371`.

## Exact reusable component

The migration already has a shared campaign dialog, rather than requiring a new visual system:

- `client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewEntry.CampaignMenus.cs`, `ShowCampaignDialog`, lines23 onward: one backdrop, title, vertically scrolling body, separate fixed footer, safe dismissal and supplied choices. Uses `ReviewModalPermission` plus exact model/controller/frame/epoch guards.
- `client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/PlayerConsole.uss`, lines13–25: `.campaign-dialog-backdrop`, `.campaign-dialog`, `.campaign-menu-title`, `.campaign-dialog-body`, `.campaign-dialog-actions`.
- `Review/Runtime/ReviewEntry.cs`, confirmation adapter and `ClearConfirmation`; `Review/Runtime/ReviewModalPermission.cs`; documented lifetime contract in `docs/unity-review-modal-permission.md`.

Exact source styling: centred630logical-pixel dialog, max width100%, max height88%, padding18×22, amber14px left/5px top frame, rounded left corners30/24, dark `#080f19` surface, lavender26px title,16px scrollable body, footer actions≥44px; narrow footer stacks vertically. Backdrop fills the console with `rgba(2,5,12,.92)`. Reuse these classes/component boundaries; do not introduce a separate toast, arbitrary full-screen result page or third modal palette for each event type.

This source is implemented for campaign dialogs/confirmations. Source presence is not proof that Research/Engineering/Production start-turn notifications already use it. The current inspected call sites are campaign navigation/save/review actions; no generalized completion-popup binding was recovered in this audit.

## Inspected original design references

Open draft PR60 at `b3496b6b1da31634d9beb8a7357b2480f37a9a72` preserves the original standard popup grammar in `tools/interface-mockups/app.js`, `modalPage`, and `.modal-veil/.modal/.modal-scroll/.actions` in `style.css`. `docs/art/coherent-interface/design-handoff.md` requires a scrolling body with fixed actions and focus distinct from selection.

Three source screens were reproduced unchanged with installed headless Chrome/GPU disabled, then individually visually inspected:

| Published reference | Meaning / status |
| --- | --- |
| `artifacts/coordination/TURN-POPUP-01/coherent-science-review-1920.png` | Existing centred Science review dialog: amber heading/action, blue-dark panel, dimmed context, fixed footer and visibly focused safe action. **Pre-action concept**, not a research-completed report. |
| `artifacts/coordination/TURN-POPUP-01/coherent-endturn-review-1920.png` | Same standard dialog for a turn review. **Pre-advance concept**, not a completion event. |
| `artifacts/coordination/TURN-POPUP-01/coherent-notifications-1920.png` | Existing proposed lawful activity/receipt vocabulary, including Research prerequisite reached and Queue completed. It is a notification-list concept, not evidence that a turn-start popup queue is implemented. |

These are fresh browser source reproductions, not recovered historical capture bytes or native Unity screenshots. Old E: gallery and any dedicated later completion-event render are unavailable here. No such later event-specific illustration or approval record was recovered from the searched Git art history/available prior chat evidence. The reusable live-source dialog above is more current than the browser's700px gradient modal; prefer its native classes while retaining the same title/body/footer hierarchy.

## Applying the owner's completion-event instruction

The owner now explicitly requires this existing standardized popup for applicable start-turn completion events. Use the shared shell and actor-lawful accepted result facts. Bind the title/body to the actual completed Science threshold, Engineering project or constructed item and world/class/revision as exposed by authority. A suggested title such as Research completed is valid only when the actual receipt means completion; prerequisite reached, reserve earned and maturity progress are different facts. Completion artwork, if available and legally mapped, is subordinate to the same readable text and never creates an unlock/unit.

One safe Continue/Close action dismisses a completed factual notice. Optional Inspect research/Engineering/world navigates to the existing exact context; it must not Develop, enqueue, apply a plan, resolve or Advance again. Do not reuse a pre-action Confirm caption as if it completes an already-completed event. For multiple events, the UI should sequence the same component with stable event identities and an explicit count where lawful, without opening stacked overlapping dialogs. Ordering/deduplication/acknowledgment behavior must be specified from actual available receipts; this handoff invents no saved event ledger or migration. Rebuilding/resizing/re-entering must not redispatch a completion mutation, and load must not fabricate a new event from an old latest-turn receipt.

Retain existing focus trap, initially focused safe action, Esc dismissal, one-use callback permission, stale-host rejection and focus restoration. A held opening key cannot acknowledge the next notice. Long event text scrolls inside the body while actions remain visible at720p; event notices remain readable muted and with reduced motion. Do not block the thread on animation or introduce a fake processing delay.

## Receiving gate and limits

Unity owns component reuse and event binding; any missing actor-safe completion fact requires the narrow shared contract discussion rather than inference from state deltas/names. QA should exercise one real research completion, Engineering completion and production completion, multiple same-turn completions, no-completion turns, resize, dismissal/Inspect, held-key/repeated callback and save/load/re-entry. Verify exact accepted turn/world/class context, unchanged campaign state on acknowledgment/navigation, no repeated completion and no hidden opponent event. Use the existing component at all three target sizes; no new modal design is requested.

Executed by Art: exact current component/USS and permission-contract reads; three original-source popup/notification browser captures, zero page errors; each image inspected. Event binding, native focus/input and actual completion behavior were not executed or accepted here.
