# LCARS Instrument Bank v2 — acceptance and handoff

Presentation-only refinement stacked on PR #12 (`6b899c6faaf59432a8b34a6bd3f430db4b648d5a`). [Draft PR #13](https://github.com/AndreBal1983/HobbyDS9/pull/13) targets `codex/vertical-lcars-controls-v1`. No merge or changes to main, tactical worktrees, PR #9 or PR #11. The [reference study](lcars-instrument-bank-v2-study.md) records the supplied references, five external visual sources and three candidate layouts.

## Delivered controls

156-unit tiles show independently editable Requested/Allocated light columns. Each light stack is 28 units wide in a 44-unit hit column. There is no SliderInt, floating fader or thumb. Pointer-down and captured scrubbing show a prospective value with PENDING text; release/capture loss commits the changed draft once. Detaching cancels an unfinished gesture. The gesture freezes its panel-space origin before focus selects the exact editor. The zero strip has a separate style from the zero instrument state, preventing zero selection from repositioning an entire instrument.

Two bars represent one integer power unit when the bank maximum fits: Command maximum 4 = 8 bars, Launcher maximum 8 = 16; allocation 3 = 6 lit bars and 6 = 12. Either half selects the same whole unit. All stacks share a zero baseline and scale. Larger maxima use one common, explicitly annotated coarse scale, bounded to 48 bars (32 in compact/short layouts), with overflow-safe Int64 handling. Exact numbers remain visible; zero-demand equipment has no phantom stack.

Arrows step one power unit; Home selects zero, End maximum. Selecting a title or focusing a track exposes the same delayed Int64 fields in the selected editor. Invalid typed values remain unchanged and go through backend validation. Severe errors have textual tile/bank indications and full backend details in the expandable validation region. On standard/wide screens, selected details scroll separately; on short/narrow screens they follow the bank in the main scroll region. Cruise, Combat and Survey values remain independent.

The shell elbow now joins the navigation rail and 30-unit structural bar continuously. Its geometry and Label margin fix belong to the fixed shell; content scrolling does not move the outer frame. The supplied Overview artwork remains byte-identical.

All ranges, minimum demand, available power, legality and performance originate from existing portable backend projections. EXCESS is represented by backend issues rather than an invented numeric projection. No campaign command, simulation, compiler, content/save version or VSync change is included. Blueprint publication remains disabled pending **Unity Ship Designer — Campaign Blueprint Integration**: campaign-owned catalogue, engineering availability, published revisions, PublishShipBlueprint, EnqueueBlueprintConstruction, campaign construction options and current authoritative campaign state.

## Automated verification

- Locked restore; .NET Release build: zero warnings/errors; full .NET suite: **1,104 passed, none failed/skipped**.
- PrepareUnity and existing static checks passed.
- Unity EditMode: **107 passed, none failed**. Includes physical paired-segment hit tests, capture/scrub/release, no extra draft preview while dragging, bounds/invalid exact input, Int64 extremes, keyboard steps, shared scale, profile/draft continuity and responsive shell geometry. The real designer focus regression also checks zero instruments stay in the paired layout. Batch capture-loss tests dispatch the public pooled event explicitly because batch Editor lacks the native OS dispatch loop.
- Windows x64 IL2CPP build: passed with Unity 6000.6.5f1. Existing Unity deprecation warnings in unchanged audio code remain; the .NET build is warning-free.
- Canonical/reverse 25-turn, 17-combat deterministic verification: save checksum `fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`, scenario checksum `3b80eb970c5d832550fc947344e860777910d80208af0fc2f474a98ae04e0b59`.
- Overview artwork SHA256: `e67b76ba0a505dbd32b273a00bf4c3c09ced319cb723a063f2b56711f7f3f9d9`.

## Native and rendered evidence

Built code: `d2b20c89284c8d3799133a8ce571cb672fd4eef7`. [Push CI 38375054531](https://github.com/AndreBal1983/HobbyDS9/actions/runs/38375054531) and [PR CI 38375060545](https://github.com/AndreBal1983/HobbyDS9/actions/runs/38375060545) passed on that exact SHA. All three jobs (Windows, Linux, macOS) passed. CI runs the portable .NET suite, PrepareUnity/static boundary checks and repeated/reverse headless smoke; Unity Editor and IL2CPP results are local licensed runs. The final documentation revision and its separate CI are recorded in the demo manifest and PR.

The owner explicitly resumed desktop input on 13 October 2026, then stopped Computer Use with Escape to join a meeting. No further desktop input was sent. The implementation/build is ready for review, but **the complete native E2E milestone is not yet accepted**. Earlier screenshots that exposed the zero-state defect are retained separately and are not final acceptance proof.

| Client size | Final-code native observations | Findings / limits |
|---|---|---|
| 1280 × 720 | Initial map, Overview artwork, template/system dropdown navigation, Cruise, Combat; Energy allocation 0, 8/16, full 16, exact 100; Command 3/4, Launcher 6/8; selected exact editor and backend error text | Eight narrow tiles across; ninth wraps. Compact header/profile totals, Back and Reset remain fixed. Scrolling reaches readouts, MAX labels, lower rows and exact details. When error text expands the toolbar, names, full tallest tracks, numeric readouts and all explanations cannot fit simultaneously in the short viewport. Essential controls are reachable by scroll; no overlapping controls observed. The final Survey click was stopped before execution |
| 1920 × 1080 | Energy direct zero click, scrub 0→8, Home→0→Up1→Up2→Down1→End16, exact 100; Command 3/4 and Launcher 6/8; Cruise/Combat/Survey; Weapons and return to retained Combat 100; independent selected-detail scrolling | Eight tiles across, ninth wraps. 4-power and 8-power stacks are visibly 8 and 16 bars on the same baseline; partial 3 and 6 illuminate 6 and 12. Exact invalid input remains 100 while bars bound their display. Severe error text and full backend explanations remain readable. Fixed shell joins are smooth; content scrolling leaves the frame fixed |
| 3440 × 1440 | Native fullscreen resize from 1920 preserves the invalid Combat draft; Energy 0, scrub 8, End16; Requested zero independently; Command 3/4, Launcher 6/8; Cruise/Survey | Nine tiles across. Common scale, zero baselines, values and selected exact editor remain aligned. Backend budget and range errors remain textual. Elbow/rail/bar join stays continuous after resize |

At Energy 16 with the original other Combat allocations, backend preview reports 64 allocated against 60 available. Changing Command to 3 reports its backend minimum/dependency issues; high utilization itself does not invent an error. Exact Energy 100 stays 100 in the field and instrument readout after Return, profile changes and a system-tab round trip. Requested and Allocated were operated independently. Pending/release single-commit behavior is asserted by EditMode dispatch tests; native mouse gestures verify hit coordinates and resulting values without claiming an instrumented native callback count.

**Remaining native checks:** Reset with Keep/Discard; Back and section-navigation guards; fresh Research/Production/report-access smoke; final 720p Survey/return and all-system/quantity editing interaction sweep. Existing automated tests cover draft continuity, edits and guards, but do not substitute for these remaining native clicks. The test player was left at 1280×720, Escort / Combat with deliberately invalid Energy allocation 100 and Command 3. A later authorized native pass should reset/discard that test draft and leave a clean Escort / Combat screen for owner review. No real campaign data was edited.

The final-code Editor capture helper completed twelve actual rendered Game-view cases at each target resolution (36 images): Surveyor/Escort Overview, Weapons, Power, Combat, zero/full/invalid, Cruise/Survey, paired 3-of-4 / 6-of-8, and invalid selected details. These fixture captures are separate from native mouse/keyboard evidence. Native screenshots, focused shell-corner crops and side-by-side comparisons with PR #12 are included in the demo's screenshots folder. The 1920 PR #12 comparison is explicitly historical, from before its final palette adjustment. Supplied references and third-party research images are not runtime assets or packaged artwork.

Physical touch, controller/D-pad and screen-reader interoperability require device testing. Responsive tests include a logical 1600×900 DPI-equivalent layout; they do not prove every Windows display scaling setting. New populated combat/replay or campaign blueprint publication/construction is outside this visual milestone.

## Demo

Fresh separate package: `E:\HobbyDS9\artifacts\demos\LCARS-Instrument-Bank-v2\HobbyDS9.exe`, with README, final build manifest and screenshots. Previous Vertical-LCARS-Demo and LCARS-Slider-Demo packages are preserved. Select **Ship design → Escort → Operating profiles → Combat**. Keep the adjacent Data, D3D12 and DLL files. Build/test logs and fixture captures remain in ignored local artifacts.
