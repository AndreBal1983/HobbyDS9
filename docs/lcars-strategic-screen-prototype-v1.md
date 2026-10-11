# LCARS strategic screen prototype v1

Branch: `codex/lcars-strategic-screen-prototype-v1`. Exact accepted-main base: `a0f750681438d26ded17fff38cb3df37c3da9eb0`. No merge to main. This is a presentation candidate awaiting Unity acceptance, not a completed screenshot milestone.

## Framework and boundaries

Unity **6000.6.5f1**, UI Toolkit / VisualElement / USS, the existing font/theme, `ClientEntry`, `LcarsShell`, and the existing command/session interfaces are preserved. No changes to Application, Contracts, Simulation, scenarios, research DTOs/logic, save/content/rules versions, Unity package manifests, or project settings. No competing framework or external art/font dependency was added.

The supplied attachment contained the written brief only; referenced screenshots/visual references were not present. No other screen is redesigned. Research, Combat, Saves and Audio retain their existing composition behind the shared shell.

## Presentation changes

- Top strip: faction/research/reserve/revision plus separate credits, turn and phase readouts and existing End Turn/Fit Map actions.
- Connected amber left rail: wider single-line section labels, lavender active selection, connected elbow/header and footer termination. Production retains its independent dock/drawer toggle.
- Central map: dominant at both requested aspect ratios, selected-marker outline, larger readable fleet markers, view-only elliptical placement using available width, passive navigation legend. Existing topology and pan/zoom/selection/route-command interfaces are unchanged. All map-adjacent decorations ignore picking; the legend is a sibling of the map, never a map hit target.
- Right inspector: Mars starts selected through its real Sol navigation node. Compact colony/capacity/stability/food/output rows precede production. Empire and fleet selections have compact fact rows. Detailed world/governance/delivery controls are preserved in a collapsed section; its expansion and the ship choice persist as ephemeral view preferences through refresh.
- Production: live local stock, industry, queue meters, ship selector, costs, availability explanation, enqueue and confirmed cancellation through the existing `ProductionOverview` adapter. Orders retain the observed snapshot stamps. The empire production dock starts closed at both resolutions, independently of its previous responsive default, because world controls already occupy the inspector.
- Bottom activity strip: short confirmations/input hints and translated player-facing command/turn/battle rejection reasons. Stable diagnostic codes remain in the existing command results and exception logs.

Reusable additions in `LcarsComponents`: `LcarsDataRow`, `LcarsMeter`, `LcarsInspectorBlock`, `LcarsCompactReadout`. Existing buttons, navigation, frames, top bar and elbow are extended. Shared palette, type, spacing, header/frame/activity sizing, radii, rail and overlay tokens remain in `LcarsTokens.uss`; geometry/responsive bounds remain in `LcarsMetrics`. The static integration check guards shared rail width and bar thickness against drift.

## Live facts and mock data

No mock gameplay facts or synthetic colony were needed. Initial Mars is the scenario's `planet.mars`, not a new authoritative entity. It initially has **zero local Dilithium**, so Scout production is visibly disabled with the existing advisory explanation directing the player to deliveries. This is expected gameplay, not a broken production UI. Earth and other eligible owned worlds can still issue ordinary production orders. Detailed world output values remain estimates from the existing Application projections.

## Verification status

| Gate | Result |
| --- | --- |
| Exact base / own branch | Verified |
| Locked restore / Release .NET build | Passed; zero warnings/errors |
| Existing and new .NET tests | Passed (839 total, zero failures/skips) |
| Portable DLL/scenario preparation | Passed |
| Unity asset/GUID/reference/token checks | Passed; static only |
| Canonical/reverse 25-turn headless runs | Passed; turn 26/revision 187, 17 encounters; matching save hash and RNG |
| 1920×1080 layout policy | Verified mathematically: standard, no context drawer; initial map >60% of width |
| 3440×1440 layout policy | Verified mathematically: wide, 2580×1080 logical panel; initial map >60% of width |
| New Unity control-tree tests | Authored, **not executed** |
| Unity C# compilation / USS import | **Not executed**; no activated Editor available in cloud |
| Pixel rendering / map pointer behavior | **Not verified**; requires Unity |
| Required Mars screenshots | **Not captured**; requires Unity |

The .NET build compiles portable assemblies and linked pure presentation sources, **not** Unity-dependent client/editor classes. Mathematical checks are not visual verification. No browser recreation, image generation or synthetic screenshot is offered as Unity evidence.

## Complete the Unity screenshot gate

In a checkout of this branch, with Unity closed, run from the repository root:

```sh
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
python scripts/check-unity-integration.py
```

Open the project at `client/HobbyDS9.Unity` with activated Unity **6000.6.5f1**. Require zero Console compile/style-import errors. Run all EditMode tests including the two new `StrategicPrototypeTests` cases. Existing `.NET` and headless checks do not replace them.

Open SampleScene and enter Play mode. Initial selection must be Mars, with the empire Production dock closed. Use **HobbyDS9 → LCARS → Capture Mars at both review resolutions**. The Editor-only helper creates/reuses fixed Game view sizes, waits for rendered frames, checks Mars/production visibility and map width, captures real Game view PNGs, and validates their dimensions. It uses reflection for Unity's nonpublic Game view size API; runtime UI has no dependency on it. This helper is also unexecuted in cloud. If the pinned Editor's internal API differs, set each Game view fixed resolution manually and capture the actual Game view.

Expected output locations (these files do **not** exist yet):

- `artifacts/unity/lcars-strategic-prototype/mars-selected-1920x1080.png`
- `artifacts/unity/lcars-strategic-prototype/mars-selected-3440x1440.png`

Review both captures for header spacing, rail readability, map dominance, inspector density, button sizing, intentional wrapping, balance, activity-strip fit and clipping. At each resolution also test empty-map left/middle drag, scroll around pointer, node/fleet selection, Fit Map, route preview/cancel/confirm, world switching and queue/cancel on an eligible owned world. Mars's initial disabled queue must show the local-resource explanation. Confirm the passive legend/elbow never absorbs clicks. Retain actual screenshots and Unity test results for review; generated artifacts remain ignored.

## Reconciliation and next steps

Review this strategic prototype and its two real Mars screenshots before extending to any other screen. Reconcile with Science & Engineering by keeping its research methods/contracts and porting only the compact header/shell presentation. Most new inspector logic is isolated in `StrategicPrototypePanels.cs`; the unavoidable shared integration points are `ClientEntry` and a small strategic-selection/fleet portion of `StrategicPanels`. No research method was edited. Do not merge or broaden scope until the Unity acceptance/screenshot gate is complete.

## Local continuation — 11 October 2026

Continued from candidate `5dae7f7b2cccb226628e1ace9d59e7a2a02d9b77` in an isolated managed worktree. The Science & Engineering checkout and main were preserved.

Unity 6000.6.5f1 imported and compiled the client. All **55 EditMode tests passed**, including both StrategicPrototypeTests. The portable suite passed again: **839 tests**. Updated the older production overview assertion to expect `CommandFeedback.Explain` rather than the raw diagnostic enum.

Real Unity rendering exposed a clipped Queue ship action at both review sizes. Compacted the selected-world system summary by retaining system/navigation in one row and the world selector while eliminating repeated system title/ownership. Colony ownership remains visible. The corrected captures pass the map-width and enqueue-button containment checks at both sizes.

Captured and visually reviewed:

- `artifacts/unity/lcars-strategic-prototype/mars-selected-1920x1080.png`
- `artifacts/unity/lcars-strategic-prototype/mars-selected-3440x1440.png`

**Capture provenance:** a Unity 6000.6.5f1 Mono development player rendered the live UIDocument into exact-size Unity RenderTextures. These are actual Unity UI renders, not Game view screenshots. Hidden Editor rendering stalled, and Windows fullscreen rendering retained the desktop resolution. The temporary harness and its build source are retained under ignored `artifacts/unity/capture-harness`; neither is shipped in Assets. Logs are `editmode.log`, `review-build.log`, and `player-capture.log` under `artifacts/unity`.

The Mars inspector, disabled enqueue explanation, resource strip, navigation, activity strip, and map dominance were visually inspected at both sizes. Longer map marker names use ellipsis. Native mouse interaction, route preview/cancel/confirm, world switching, and real queue/cancel remain pending interactive acceptance. Do not treat render-target evidence as completion of the original Game view/pointer gate. No gameplay contracts, research logic, versions, or other screens were changed. No merge to main.
