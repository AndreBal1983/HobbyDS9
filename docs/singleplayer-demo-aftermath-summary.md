# Selected-encounter aftermath summary

19 October 2026. **CPU-verified presentation candidate; independent review, receiving and native acceptance pending.** Branch `codex/demo-aftermath-summary`, from frozen R4 source `ba152554a2307cf389a621398066ae1a1d608baf`. Existing [demo authority and persistence](singleplayer-demo-contract.md) remain unchanged. No Unity/GPU/native process, build, frozen package or owner save was changed by this work.

## Actual gap and existing authority

The [R4 native aftermath screen](E:/HobbyDS9/artifacts/native-singleplayer-acceptance-20261019-r4/screenshots/14-aftermath-confirmed-fresh-orders.png) shows successful completion and fresh strategic orders beside an incorrect “No resolved-round report yet” prompt. Finalization deliberately clears current-batch battles/reports, so `Frame.Battle` becomes null. This is not a navigation reset; the existing report text reads only the cleared current-batch projection. The [client regression](../tests/HobbyDS9.TacticalReviewClient.Tests/ActualFleetAndLifecycleTests.cs) explicitly preserves this lack of a permanent archive.

Before finalization, the immutable actor-filtered completed observation includes every own participating ship, including destroyed ships, plus staged stocks. After accepted finalization, current owned ships/fleets provide actual retained stocks and location. The new presentation receipt uses only these existing facts. It does not preserve an enemy roster, enemy reserves, raw/private events, or infer an encounter winner from visible enemies.

## Implemented boundary

`ReviewCombatFlow` captures the completed observation before its existing finalize call. It publishes one `ReviewAftermathSummary` only after acceptance, the observed next-turn Planning boundary with no selected battle, the same controller/actor/generation and no required stale refresh. It also confirms destroyed owned participants are absent and surviving participants match post-finalization hull, four shield stocks, ammunition and encounter-sector location. These are display safeguards; Simulation remains the only consequence authority.

The record retains only the selected encounter's own lost IDs/names and confirmed surviving own snapshots. It excludes unrelated fleets and other encounters. The demo's existing three-panel view automatically opens **Battle summary** for a new receipt, showing the lawful location/turn, own losses, survivors and retained hull/four shields/torpedoes. Fresh strategic orders and terminal inspection retain their existing guards. Long content remains in the central detail scroller.

The record stays valid through same-turn navigation, resize, local or accepted fresh orders, save, failed save and failed load. It clears on a different controller/actor/generation, successful load, new demo, next turn/new battle or closure. Failed/stale finalization does not publish a falsely applied summary; repeated finalization does not replace a valid receipt. Multi-encounter batches explicitly show the **selected encounter only**, not invented whole-batch losses.

**Persistence limit:** this is one session-local display receipt, not a historical campaign archive or new save field. Completed-before-finalization saves still contain their existing current-batch reports. Post-finalization saves retain current condition, not destroyed identities or this receipt. After successful load/restart the report states that no summary is retained in this session and offers current ship inspection; it never asserts that no battle occurred or tells Planning to commit a nonexistent round. Persistent historical reports require a separate authoritative projection/save contract.

## Executed CPU evidence

- Full actual-source portable adapter suite: **134 passed, 0 failed/skipped**. Six new cases cover an actual paid three-ship battle with one own loss, two survivors and three retained torpedoes each; immutable receipt collections; unchanged complete saved bytes through display; navigation and accepted/local orders; failed save/load retention; successful load/advance/closure clearing; another real controller winning finalization first; and a real two-encounter later-stage rollback/retry with selected-only/actor isolation.
- Both source/boundary checks and **10 + 6 adversarial static tests passed**. Portable default/demo preparation passed without launching Unity.
- Eight pinned Unity 6000.6.5f1 split compiler outputs and metadata checks passed, including the receiving fixture. Review runtime Player/Editor and review Editor have zero warnings. Existing default deprecations and `DemoRuntimeAcceptanceTests.cs:69` nullable warning remain.

Receipts: `artifacts/demo-aftermath-summary/`, including `all-final.trx`, focused results, static logs and `api-compile/evidence.json` reference/source hashes. Initial fixture failures are retained: nullable annotations, then fault-injection reflection targeting the Application facade instead of its backend. The corrected final suite passes; no production authority workaround was introduced.

## Receiving/native gates

`DemoAftermathSummaryRenderTests.ConfirmedOwnAftermathAndSessionBoundaryAtThreeSizes` is authored and compiler-checked, **not executed**. It arranges a real paid three-ship campaign, resolves legal rounds, and confirms aftermath through the actual modal callback. Twelve proposed captures cover the confirmed summary, navigation retention, failed-load retention and replacement-session fallback at 1280 × 720, 1920 × 1080 and 3440 × 1440. Assertions cover auto-selected summary, actual own stock text, useful compact viewport, vertical scrolling and contained actions without horizontal overflow.

Direct UI events in this fixture will not establish OS keyboard/mouse acceptance. Independent exact-source review, coordinated graphics-enabled receiving, manual image inspection and native campaign/save/load verification remain required before the coordinator integrates this with the separate keyboard fix into a new candidate. R4 remains frozen.
