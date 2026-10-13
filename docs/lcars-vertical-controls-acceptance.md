# Vertical LCARS controls — acceptance record

Date: **2026-10-13, Europe/Amsterdam**. Status: **draft review; native acceptance partially complete**. The user asked to continue without mouse/keyboard control, so remaining desktop checks are deferred rather than asserted as passed. No main merge or force push.

Review: [draft PR #12](https://github.com/AndreBal1983/HobbyDS9/pull/12), stacked on [draft PR #10](https://github.com/AndreBal1983/HobbyDS9/pull/10). Base is `967f63723000b3391e2cc61823eb35594b905d52`; fetched main was `5beba2d652d283a2efb2f3ef44ddd5df9e38815d`. Built-code commit: `2bf8945b8f80287e8229f92adb6263049ac86789`. Later documentation-only review commits do not alter this built code.

## Implemented boundary

Real non-inverted vertical SliderInt tracks replace the horizontal controls, with zero at the bottom and full demand at the top. Twelve original segments fill bottom-up against detached backend full demand. Lower/middle/upper segments use blue/lavender/amber; dark inactive segments and centered thumbs improve separation. Requested/Allocated share a scale inside bounded, wrapping component cards. Profile budget totals remain detached backend readouts above the bank.

Exact delayed Int64 entry, single-unit keys, Home/End, release-only pointer commits, focused-track restoration, independent profiles and existing discard guards retain their existing APIs. Powered minimum/full demand come from Application; Unity supplies no new legality, performance, capacity or power-distribution rules. See the [research study and three candidate wireframes](lcars-vertical-controls-reference-study.md).

Only Unity control/view/USS, the existing capture helper, tests and documentation changed. Overview image SHA-256 remains `e67b76ba0a505dbd32b273a00bf4c3c09ced319cb723a063f2b56711f7f3f9d9`. Audio/SFX code, VSync, save/content/rules versions, campaign state, portable backend, AI and combat remain unchanged. Publication is disabled; [Unity Ship Designer — Campaign Blueprint Integration](unity-ship-designer-campaign-blueprint-integration.md) remains **proposed, not implemented**.

## Executed automated checks

- Locked restore and .NET Release build: passed, zero warnings/errors. **1,104 .NET tests passed, zero failed/skipped.**
- PrepareUnity and static asset/portable assembly-boundary checks: passed. Scenario SHA-256 remains `3b80eb970c5d832550fc947344e860777910d80208af0fc2f474a98ae04e0b59`.
- Unity **6000.6.5f1 EditMode: 96 passed, zero failed/skipped**. The five additional cases cover bottom-segment ordering, vertical orientation/real thumb motion, focus after refresh, native-style Up/Down events and wrapping at three logical widths. Existing pointer deferral/duplicate-navigation, exact invalid entry, profile/draft/reset and backend-budget tests continue to pass.
- Windows x64 **IL2CPP build passed** after native visual refinements. Evidence in this isolated worktree: `artifacts/unity/editmode.xml`, `editmode.log`, `windows-build.log`, `Windows/HobbyDS9.exe`.
- Canonical and reverse-order 25-turn headless smoke runs retained save SHA-256 `fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`, with 17 resolved combats. Full tests cover blueprint-derived campaign movement/combat and save/reload determinism. No golden changed.
- Built-code CI: [push run 38359024815](https://github.com/AndreBal1983/HobbyDS9/actions/runs/38359024815), [PR run 38359029958](https://github.com/AndreBal1983/HobbyDS9/actions/runs/38359029958), both successful on exact code SHA `2bf8945b8f80287e8229f92adb6263049ac86789`, across Windows/Linux/macOS. Final documentation-head CI is verified separately in the handoff/PR.

## Actual native observations

No scripted game-state injection was used for the native screenshots. Mouse dragging and keyboard/exact entry operated the shipped Windows player.

| Client size | States captured and inspected | Findings / limit |
|---|---|---|
| 1280 × 720 | Allocation 0, 8/16, 15/16, exact 100; separate invalid-message capture | Compact system picker and shorter rails; three cards fit across the bank. Scroll exposes Requested/Allocated labels, full rails and exact fields together; a further scroll exposes zero labels and error explanations. Cards, budget and all explanations do not fit simultaneously in the short viewport. Back/Reset remain outside the scroll panel. No overlapping essential controls observed |
| 1920 × 1080 | Allocation 0, 8/16, 15/16, exact 100; no-demand Cruise and Overview | Native geometry, dragging and exact entry passed with three cards across. **These captures precede the final segment-band adjustment** (five blue / four lavender / three amber). The final palette needs native recapture at this size; do not label these images as final-head native proof |
| 3440 × 1440 | Final-code allocation 0, 8/16, 15/16 and retained exact 100, plus native 0→1→2 keys and Requested zero | Five bounded cards across. Full first-row tracks, legends, exact inputs and budget are readable; longer component errors/results and later cards remain scrollable. Home/End and repeated one-unit Up/Down work after preview refresh. Both Requested and Allocated were operated directly |

At 15/16 with the other Escort allocations unchanged, the backend reports **63 allocated against 60 available**. The warm position band stays warm; a separate red border/text conveys the backend budget error. Full Command controls are warm while valid, demonstrating that high utilization alone is not an error. Exact 100 stays 100 in both readout and draft even though the visible thumb stops at 16. Below-minimum values 1 and 2 produce backend errors. Zero-power optional weapons retain backend warning semantics.

Native testing verified profile separation/return and exact invalid continuity before the final palette pass. Full automated tests also cover tab continuity, reset and discard guards. Final-code profile/tab/reset/navigation rechecks and current-player strategic map, Research, Production and report-access smoke remain **pending** after the desktop-control pause. The initial map rendered correctly, but this is not a substitute for those interaction smoke checks. No populated battle report/replay or new campaign-owned blueprint publication/construction was exercised here. Prior PR #10 results are historical evidence, not fresh native acceptance for this revision.

Two player sessions exited cleanly during testing; logs showed orderly Unity shutdown rather than a recorded crash. The user then requested work continue without mouse/keyboard. No further desktop input was sent. Mouse/keyboard acceptance should resume when requested; no cause is inferred for the exits. Physical touch and controller/D-pad input are untested.

## Runnable package and visible evidence

Complete Windows runtime: **`E:\HobbyDS9\artifacts\demos\Vertical-LCARS-Demo\HobbyDS9.exe`**. Keep the adjacent Data, D3D12 and DLL files. Launch the executable, select **Ship design → Template: Escort → Operating profiles → Combat**. Scroll the bank on smaller screens. Drag a rail upwards, or focus it and use Up/Down, Home/End; exact fields commit on Return/focus loss.

Actual native screenshots and three side-by-side comparisons are under `E:\HobbyDS9\artifacts\demos\Vertical-LCARS-Demo\screenshots`. The 1920 comparison is explicitly from before the final color-band pass. The original horizontal package remains separately available at `E:\HobbyDS9\artifacts\demos\LCARS-Slider-Demo`.

Native logs contain no Exception/NullReference/Error matches. Package hashes are checked against the final IL2CPP output. Research images remain outside Unity Assets and are not included in the runnable package. Deferred native checks prevent declaring the complete visual/E2E milestone accepted; the implementation and build are ready for draft review.
