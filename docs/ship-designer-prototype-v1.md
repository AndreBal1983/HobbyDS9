# LCARS Ship Designer prototype v1

Presentation candidate on `codex/lcars-ship-designer-prototype-v1`, from exact accepted main `36ac2513b319a94ebe1ea05465f595f009604577`. Worktree: `/workspace/HobbyDS9-ship-designer`. The original checkout is preserved. No merge to main.

Read against [ship design](design/ship-design.md), [design index](design/README.md), [architecture](architecture.md), [Unity workflow](../client/HobbyDS9.Unity/README.md) and [accepted LCARS integration](lcars-strategic-screen-integration-v1.md). Proposed balance and gameplay rules remain proposed; this UI does not implement them.

## Open and use

In SampleScene Play mode, choose **Ship design** in the connected strategic navigation. The existing LCARS shell presents a full workspace while retaining its campaign resource header, navigation and activity strip.

- **Left:** ship-class name, hull/version, desired role and starting template; compare start versus draft. All edits are local to the running session and are not in campaign saves.
- **Centre:** a shared vector hull schematic, explanatory Core/Mission/Defence groups, installation quantities, used/total/available capacity where a preview exists, and system-specific issues. Placement is an explanation, never a packing puzzle.
- **Right:** nine-component catalogue, readable descriptions and unit facts/costs, and pinned Add package / Replace selected controls above an independently scrollable installed list. Select an installed package to replace its full quantity; quantities merge if the replacement is already installed. +/− adjusts quantities and Remove deletes a package.
- **Bottom:** Cruise/Combat/Survey inspection, available/requested/actual power, effective capabilities, costs and upkeep. Profile selection is inspection, not an edit to operating priorities.
- **Persistent toolbar:** Compare start, Reset edits and Return to map. Reset, changing templates, and explicit strategic navigation require confirmation if the fit/name/role was modified. Cancel retains edits and restores the displayed template selection. Confirm discards exactly once. Changing a quantity back to its starting value clears the modified flag if no other edits remain.

Comparison shows installation differences, name/role, capacity, profile status, power, effective capability and cost values at the **same selected profile**. Unknown results display “Not evaluated”, never zero.

The inactive **Construction unavailable** control is explicit. There is no functional Build, publish, refit or construction submission. End Turn and Fit Map are disabled inside this local workspace and restored to their ordinary campaign availability on exit. Production navigation passes through the discard guard before opening its existing dock. Map objects, selection and camera remain in their original tree; research, production commands, combat orchestration, save/load, audio controls and VSync are not rewritten. Mandatory combat focus may interrupt the workspace without deleting the retained local draft.

## Demo catalogue and preview honesty

`FixtureShipDesignPreviewProvider` is the one initial adapter behind `IShipDesignPreviewProvider`. `ShipDesignDraft` owns editing/lifecycle; `ShipDesignerView` only presents it. All types are **Unity-client-local**, not new shared portable contracts.

The surveyor **Pathfinder** and escort **Sentinel** use one `demo.pathfinder-hull-v1`, with illustrative capacity 100, two energy mounts, one launcher mount and power 60. Catalogue: reactor, drive, crew support, command computer, sensors, labs, shields, energy bank and launcher. These are not live DS9 content or Engineering unlocks. The status banner, validation captions, component unit facts and cost heading all label fixture/demo provenance.

Preview is **exact-fit lookup of authored examples**, not local validation, power allocation, arithmetic cost aggregation or performance compilation. Name changes preserve the same fit example. Other role/installation edits can deliberately become unsupported:

| Interaction | Authored example |
| --- | --- |
| Surveyor in Cruise/Survey | Demo valid; unarmed is legal in this example |
| Surveyor in Combat | Warning: unarmed, avoid hostile engagements |
| Escort in Combat | Warning: 66 requested / 60 allocated; weapons receive 18/24, shown as 75% requested output |
| Escort in Survey | Warning: basic observation, no dedicated labs |
| Remove one common core system from either unchanged template | Hard error beside the missing system; restore it via catalogue |
| Surveyor: labs quantity 2 | Hard error beside labs; 104/100 capacity, 4 over |
| Escort: banks quantity 3 | Hard error beside banks; 3/2 energy mounts and 106/100 capacity |
| Other edited fits | **Not evaluated**: capacity, power, effective outputs, total costs and legality are not fabricated |

Green valid, amber warning, red hard-error and muted not-evaluated states also have explicit text labels. Invalid example results suppress unavailable performance/cost facts. Per-component unit facts remain catalogue descriptions, not a claim about an unsupported full fit. Reset restores the authored preview. A failing provider keeps the local draft and displays preview unavailable.

## Backend coordination and binding handoff

Inspected early backend handoff on `codex/ship-design-infrastructure-v1` at `1e8726c429fa688b9b47c0438607f27b40c39f6b`, [contract document](https://github.com/AndreBal1983/HobbyDS9/blob/1e8726c429fa688b9b47c0438607f27b40c39f6b/docs/ship-design-infrastructure-v1.md). At that inspected commit it contains documentation only; no compilable API is available to bind. Its shared types remain backend-owned and are not copied into this branch.

The planned entry is `HobbyDS9.Application.ShipDesignApplication.Create(ShipDesignCatalogueDto)`, with immutable catalogue and `ShipDesigner.Preview(ShipClassDraftDto, ShipDesignAccessDto)`. The draft carries unique installation IDs, immutable component-version IDs/quantities, and explicit requested/allocated values for **all three profiles**. Invalid drafts return structured issues instead of ordinary-validation exceptions. No automatic allocation/shedding is promised.

Binding steps once the backend API and fixture catalogue are published:

1. Replace only the provider construction in `ClientEntry` with an Application-backed implementation of `IShipDesignPreviewProvider`; retain the session editor/view. Do not call backend Publish or campaign commands.
2. Use canonical backend hull/component/template IDs and detached backend draft/profile inputs. The current demonstration IDs are intentionally prefixed `demo.` and are not authoritative IDs. Extend local shaping for actual backend installation/profile inputs as needed; do not invent an authoritative schema.
3. Source **all** capacity/free space, crew/mount usage, reactor/distribution budgets, requested/allocated/full power, effective performance, costs and eligibility from preview results. Default explicit profile inputs must come from authored backend templates; do not infer allocation rules in Unity.
4. Map Error/Warning severity and source paths to affected installation/group/profile labels and the global validation section. `IsValid` with warnings maps to warning, without warnings to valid; errors map to hard error. Missing/failed integration maps to not evaluated. Keep unsupported survey/range outputs labelled as integration gaps.
5. Set provider `FixtureResults=false` and an accurate status label only after genuine backend binding. Do not remove demo labels merely because the contract document exists. Backend availability/access requirements and validation remain backend-owned.

This front-end shapes one installed stack per selected component version. A backend with distinct mount/coverage stacks can expand that presentation without changing shared contracts here. This milestone inspects profiles; allocation/priorities editing can be added after the backend handoff defines those inputs. These are integration gaps, not permission to duplicate the compiler.

## Responsive implementation

Unity **6000.6.5f1** and UI Toolkit/VisualElement/USS are preserved. Frames, bars, rail, buttons, data rows, meters and compact readouts reuse accepted LCARS components/tokens and the existing font. `ShipDesigner.uss` is scoped to the new view.

At 1920×1080 and 3440×1440, panel scaling remains height-based and the designer uses left/centre/right columns, with extra width going to the schematic. At narrower logical workspace widths (<1450), the class/schematic pair sits above a full-width catalogue. A scrollable content body and independent inner lists keep text/controls readable instead of shrinking fonts; the primary Reset/Return toolbar stays outside that scroll area. The smaller acceptance window is **1280×720**, including a continuous resize from either larger target. These rules have authored control-tree checks, **not executed pixel verification in cloud**.

## Executed evidence and pending Unity gates

| Gate | Status |
| --- | --- |
| Pinned SDK / locked restore / Release build | Passed; zero warnings/errors |
| Complete .NET tests | 959 passed, zero failures/skips; 14 new local-editor/fixture interaction cases |
| PrepareUnity / static asset, GUID, stylesheet and assembly boundary checks | Passed; static only |
| C# 9 syntax parse of 38 Unity source files | Passed; zero syntax errors; not Unity API compilation |
| Canonical and reverse 25-turn smokes | Passed; turn 26/revision 185, 17 encounters, same accepted save hash/RNG |
| New Unity EditMode cases | 5 authored; **not executed** (64 existing + 5 expected = 69) |
| Unity-dependent C# compilation / USS import / IL2CPP | **Not executed**; no activated Editor installed in this cloud environment |
| Actual Unity screenshots / native mouse-keyboard interaction / clipping review | **Not executed**; local Unity required |

Save SHA256 stays `83bdb9013db7ac7b2b33ef77c4b591a4bca065544a01912eb48c48161688f5c4`; RNG `d8919406d6e01cf1`. Live scenario bytes retain SHA256 `d2dc4ec92b535cdcb5b4eab79f46b99617b19e1b048272d46b63721d28438653`. No Simulation, Application, Contracts, scenario, version, package or project-settings file is edited. The .NET build compiles the pure local draft/provider through linked test sources; it does **not** compile Unity-dependent view/editor code. Static checks are not Unity execution.

## Local acceptance and actual screenshots

Preserve unrelated changes and use a dedicated checkout of this branch; do not overwrite another agent's work. With the Editor closed, from repository root:

```sh
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
python scripts/check-unity-integration.py
dotnet msbuild scripts/PrepareUnity.proj -t:EditMode "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
```

Keep graphics enabled for EditMode, as in the accepted main script. Require zero Console compilation/style-import errors and 69 passing, nonempty cases. For Windows IL2CPP use the existing Windows target with its documented modules/toolchain; execute the resulting player before claiming native acceptance.

Open SampleScene, enter Play mode and navigate to Ship design. With a clean draft, use **HobbyDS9 → LCARS → Capture ship designer preview fixtures**. It reuses the accepted real Game view capture engine, captures Surveyor/Survey and Escort/Combat at all three fixed sizes, validates PNG dimensions and primary-action bounds, and restores the starting template/profile after success. It refuses to discard a modified draft. The original Mars capture menu/validation is retained. Capture helper changes are also **unexecuted here**.

Expected files under `artifacts/unity/ship-designer-prototype/` (not created in cloud):

- `surveyor-1920x1080.png`, `surveyor-3440x1440.png`, `surveyor-1280x720.png`
- `escort-1920x1080.png`, `escort-3440x1440.png`, `escort-1280x720.png`

Inspect actual images for clipping, unintended wrapping, readable descriptions/costs, visible primary actions and balanced frame geometry. No synthetic/browser image is substituted for Unity evidence.

Native interaction checklist:

1. Rename, select role and template; compare the starting design at each profile. Cancel template replacement and confirm that selection and edits remain; then explicitly discard and switch.
2. Select a catalogue package, add it, select an installed target, replace it, increase/decrease quantity and remove it. Verify local modified status and no campaign turn/revision/resource change.
3. Exercise the authored valid/warning/hard-error/not-evaluated examples above. Verify issues beside the affected systems and reduced escort Combat weapons. Unsupported fits must not claim valid construction or display template totals as current facts.
4. Reset: cancel first, then confirm. Return/navigation with modified drafts: stay first, then discard. Test Production, Research and Audio navigation through the guard. Construction remains disabled.
5. At both required resolutions and the smaller resizable window, reach all sections by scrolling; confirm Return and Reset stay accessible and input focus/quantity controls work. Capture both clean fixture templates only after resetting edits.
6. After returning to the strategic screen, exercise map selection/pan/zoom/Fit Map, production, Science/Engineering, save/load, combat and audio. VSync remains 1 and targetFrameRate -1. Record Editor/player warnings and do not describe pre-existing acceptance as this candidate's runtime verification.

Review this prototype and the real screenshots before binding backend or broadening the designer. Main remains unmerged.
