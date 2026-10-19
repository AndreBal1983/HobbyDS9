# Combat order-pane readability

19 October 2026. **CPU-compiled presentation candidate; receiving/native layout verification pending.** Separate branch `codex/demo-combat-order-pane` from received PR #61 source `cb80ec167ffae4954e7ea27d0771ee9f5d79f4f7`. The received checkout, keyboard/observer source, owner saves and frozen packages remain intact.

The [1280 × 720 receiving capture](C:/Users/ambal/.codex/worktrees/demo-tab-focus-route/HobbyDS9/artifacts/demo-tab-focus-route/receiving/combined-2-captures/demo-aftermath-summary/1280x720-Confirmed-selected-aftermath.png) shows horizontal overflow in the right fresh-order pane. Long choices are clipped, and the Draft action requires finding the bottom of a long form. That source's 17 passing cases/51 captures do not establish readability of these descendants.

## Presentation change

Demo Combat constrains the order content to its own viewport, stacks field groups/choices vertically, and permits full button labels, field labels and selected values to wrap. The horizontal bar is hidden **alongside** those width/wrapping constraints, not used as a substitute for fitting the content. Vertical scrolling remains available; type sizes and full labels are retained. The known-destination field now uses the same vertical label/value treatment as the other fields.

The existing **Draft selected next-turn order** action moves beside Review in the persistent Combat footer. Its callback, selected IDs, legal intent choices, enabled guard, confirmation, command stamps and once-only submission are unchanged. Drafting still creates only a local draft; Review/Confirm still submits it. Footer actions can wrap within their allotted width. The three panels, meaningful own aftermath summary and selected-ship inspection remain intact. Non-demo order layout is retained.

Only `ReviewCentralView`, scoped `PlayerConsole.uss`, two existing render fixtures, a shared test helper/metadata and this note change. No model, backend, persistence, keyboard, diagnostic or art changes.

## Checks and remaining gates

Both source/boundary checks and **14 + 6 adversarial static tests passed**. Default/demo portable preparation passed. Eight pinned Unity 6000.6.5f1 split compiler outputs/metadata checks passed, including the new helper. Review runtime Player/Editor and review Editor have zero warnings; inherited default deprecations and the existing `DemoRuntimeAcceptanceTests.cs:69` nullable warning remain. Reference/source hashes and logs live under `artifacts/demo-combat-order-pane/`.

The existing capture loops call the new `DemoCombatOrderPaneChecks` helper when an actual demo dock is present and no modal obscures it. This preserves the existing 17-case/51-capture inventory and all prior assertions. The added checks are **authored/compiler-checked, not run**:

- Exact dock content, text, field and button horizontal bounds must fit its viewport, and horizontal scroll range must be zero within tolerance even with the bar hidden.
- Full captions must fit or wrap to their measured height; every real dropdown choice is selected without notifying callbacks, checked, then restored.
- Every legal choice/control, including disabled choices, must be reachable within the vertical viewport after scrolling. All four strategic intents remain present.
- Draft must remain discoverable in the fixed footer. Draft/review/advance action bounds and caption heights must fit that footer.

The existing real-session fixtures still exercise actual local drafting/review, battles, aftermath, navigation, failed loads, terminal states and all three resolutions. Their new pane checks run at 1280 × 720, 1920 × 1080 and 3440 × 1440. No unchanged portable gameplay suite was repeated for this view/layout change. No Unity/GPU/native process or build was launched. Independent exact-source review, coordinated receiving/manual images and native scrolling/click/focus checks remain required before a new package.
