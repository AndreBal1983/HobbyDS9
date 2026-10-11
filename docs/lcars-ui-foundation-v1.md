# Responsive LCARS-inspired UI foundation v1

Branch `codex/lcars-ui-foundation-v1` starts exactly from combat/audio candidate
`795b485542b52bfb3c096db02643ca40fdf01b9e`. This is presentation only. Existing
acceptance branches and main are untouched. L1 adds tokens/components/policy;
L2 connects the shell/rail/header/production drawer; L3 restyles existing views,
adds integration/static coverage and completes the documentation. Each green
checkpoint is pushed separately. Local acceptance is required before any merge.

## Components and ownership

`LcarsShell` owns a persistent UI Toolkit tree, bounded header/readouts, segmented
navigation, vector structural elbow/bar, map frame, scrollable context frame,
production drawer/dock and message readout. `LcarsTopBar`, `LcarsNavigationRail`,
`LcarsNavSegment`, `LcarsPillButton`, `LcarsSectionHeader`, `LcarsPanelFrame` and
`LcarsReadout` are reusable presentation components without Application/Contracts
or Simulation dependencies. Future screens can use the same grammar without
introducing a UI framework. `ClientEntry` remains the adapter to existing detached
snapshots/actions. No DTOs, rules, resolver, commands or audio code changed.

The shell never recalculates strategic facts. Panels still issue the same stamped
Application commands. Human combat attention retains the original focus conditions;
AI-only resolution remains non-interrupting. Buffered and recent combat reports
remain available, and read-only AI fleet sections receive a distinct visual frame.
Navigation has exactly six section choices plus one independent production toggle.
A chevron and border identify the active section; production uses an explicit
plus/minus visibility label instead of creating a second active section.

`LcarsElbow` uses `generateVisualContent` and `Painter2D` with separate outer/inner
cubic curves derived from current `contentRect`. Thickness/radii clamp for small
rectangles, zero rectangles draw nothing. Layout/style repaint mechanisms redraw
it; no Update geometry loop, raster frame, proprietary font, logo or licensed art
is used. Decorative elements ignore picking; drawer bounds cover only their actual
visible panel. Ordinary bars/buttons/frames use USS, not custom meshes.

## Central tokens and typography

`Resources/LcarsTokens.uss` centralizes colors, type and spacing. `Lcars.uss` styles
the reusable shell/components; `HobbyClient.uss` contains strategic panel treatments.
`LcarsMetrics` centralizes pure responsive/geometry dimensions. The static checker
verifies the geometry bar matches the stylesheet bar token.

| Token | Logical reference value |
|---|---:|
| Structural bar | 30 |
| Button height | 44 |
| Navigation segment height | 50 |
| Standard / compact rail basis | 136 / 104 |
| Segment gap | 8 |
| Padding / inner gap | 16 / 12 |
| Normal / heading / diagnostic type | 18 / 24 / 16 |
| Pill radius | 22 |
| Elbow extent | 104 × 64 |
| Outer elbow radii | up to 88 × 56 |
| Inner elbow radii | outer radii minus thickness |
| Details standard / compact | 420 / 380 |
| Production | 360 |

No external font is added. Typography uses the existing Unity default-theme font:
uppercase short navigation/header furniture, mixed-case readable data/reports.
Condensed custom typography is deferred until an open-licensed font is selected
with provenance and license review. Color is complemented by text, chevrons,
plus/minus controls, border focus and explicit disabled states.

| Role | Color |
|---|---|
| Canvas / panel | `#05070A` / `#0B1018` |
| Structural amber / sand | `#D99552` / `#E2BC7C` |
| Muted peach | `#C98872` |
| Information lavender / blue | `#A58BC4` / `#6997B8` |
| Cool slate / alert coral | `#496979` / `#C7605B` |
| Ready accent | `#7FAF9B` |
| Primary / muted text | `#F1E8D8` / `#A9B0BA` |

Sand/amber structure the console, lavender identifies active/context/report areas,
blue marks controls, slate keeps disabled controls legible. Coral is reserved for
errors/destructive confirmation. Not every palette token is used on every screen.
There are no flashing, continuous decorative or panel-slide animations.

## Responsive policy and scaling

`ResponsiveLayoutPolicy` is pure C#, linked into ordinary .NET tests. Live layout
uses **logical panel bounds**, with Compact below width 1700 (or height 850),
Standard otherwise below width 2300, and Wide at width 2300 and above. All
breakpoints, including an extreme-narrow context drawer below width 1100, are
centralized. Furniture stays bounded; the map gets extra horizontal space.

Panel settings remain ScaleWithScreenSize, reference 1920×1080, MatchWidthOrHeight,
match=1. A small-window readability adjustment clamps the final pixel/logical scale
to **0.9 minimum**. The extra `panel.scale` multiplier is
`max(1, 1080 * 0.9 / viewportHeight)`. Normal displays at height >=972 retain exact
height matching. Thus text 18 is at least 16.2 physical pixels and controls 44 are
approximately 40 pixels at the minimum window, rather than 12/29 pixels at 720p.

Without this adjustment every 16:9 resolution has identical logical 1920×1080
bounds, making a distinct 720p mode impossible. The adjustment preserves comfortable
sizes on 1440p ultrawide, where multiplier=1 and the map expands horizontally.
The pure viewport helper mirrors this calculation for tests; actual layout decisions
continue to come from panel geometry, not raw hardware widths.

| Physical viewport | Logical bounds (approximately) | Mode |
|---|---|---|
| 1280×720 | 1422×800 | Compact |
| 1600×900 | 1778×1000 | Standard |
| 1920×1080 | 1920×1080 | Standard |
| 2560×1440 | 1920×1080 | Standard |
| 3440×1440 | 2580×1080 | Wide |
| 5120×1440 | 3840×1080 | Wide |

Compact reflows navigation horizontally, simplifies the decorative corner, wraps
header hierarchy gracefully and supplies a Details toggle. Its normal details width
is 380; extremely narrow windows use a right overlay that can collapse. Production
is always a drawer. Opening it temporarily hides compact details; closing production
restores details, and the Details control can return directly to context.
Standard uses a vertical rail, visible bounded context and production drawer by
default. Wide may auto-open a 360-unit production dock beside the map. An explicit
production visibility choice always overrides responsive defaults and persists
across mode changes/session replacement for the running shell. A manual open still
becomes a drawer on smaller layouts rather than squeezing the map permanently.

`GeometryChangedEvent` changes classes/bounds only when dimensions change; controls,
scroll views and existing view-model state are retained. A same-aspect physical
resize can leave logical bounds unchanged before the readability multiplier changes.
A UI Toolkit scheduled check samples physical height at 5Hz solely to reconcile
that multiplier **when height changes**, then normal geometry events update layout.
It does not rebuild/repaint geometry every frame. On disable it pauses/unregisters;
on enable it reuses UIDocument, clears prior root styles and avoids duplicate roots.
Bootstrap includes inactive existing ClientEntry objects when checking for duplicates.

## Panel integration and limits

Empire, selection, research, politics, environment/delivery data, production, combat,
saves and audio share readable headings/actions. Context and production are separate
vertical scroll views. Base-field labels stack above full-width inputs, long text
and foldouts wrap, and controls have visible hover/keyboard focus/disabled states.
Confirmation boxes and cancellation controls are visibly distinct. Revision remains
visible but subordinate to turn/phase/civilization/credits; research has its own row.
No screen adds authoritative facts or changes command stamps.

The map retains its existing selection, pan, zoom, fit, preview and markers. A drawer
can temporarily cover part of it; closing it reveals that space immediately. The
narrowest windows prioritize readable controls and toggles over simultaneous panels.
No claim of pixel-perfect layout/contrast is made without local rendering. The
Editor test window covers attached geometry event dispatch, not screenshot quality.

## Cloud verification

Expected **797 .NET tests**: all previous 771 plus responsive policy/geometry cases.
Locked restore, Release build with zero warnings/errors, full tests, Unity assembly
preparation/static integration and canonical/reverse/repeated headless smokes are
required. CI runs the existing three-OS matrix. Existing save/reload/reordered
acceptance and combat tests remain intact.

**45 authored Unity EditMode cases**: the previous 32 plus 13 LCARS cases covering
construction, active navigation, modes, production overrides/worlds/queue, scrollable
context, attached repeated geometry dispatch, elbow sizing, retained combat reports
and the audio singleton. These are **not executed in cloud**. Source/API inspection
and static checks are not Unity compilation or visual/runtime acceptance.

Save **12**, content **8**, rules `population-politics-geography-v1`, resolver
`combat-resolver.phased` **v1**, RNG unchanged. Required final save hash:

```text
10009857d0396dea4bd0a81001cadfb6b5ebfad2c8772223d8288bd70d3559e4
```

Expected 25-turn totals remain turn 26/revision 187, 17 encounters/12 checkpoints,
9 ships/fleets, allocator 97, RNG `d8919406d6e01cf1`, 98 shots/68 hits,
126 shield damage/116 hull damage, 10 destroyed ships/fleets, no retreats.
Audio assets, preferences, playlist and source lifetime are unchanged.

## Exact local commands

Close Unity before preparation/batch verification. From repository root in
PowerShell, with Unity **6000.6.5f1**, Windows Build Support **IL2CPP**, Visual Studio
Desktop development with C++ and the Windows SDK installed:

```powershell
git fetch origin
git switch codex/lcars-ui-foundation-v1
git pull --ff-only origin codex/lcars-ui-foundation-v1
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
python scripts/check-unity-integration.py
$unityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe'
dotnet msbuild scripts/PrepareUnity.proj -t:EditMode "-p:UnityEditor=$unityEditor"
dotnet msbuild scripts/PrepareUnity.proj -t:Windows "-p:UnityEditor=$unityEditor"
& './artifacts/unity/Windows/HobbyDS9.exe'
```

Windows target reruns EditMode before the x64 IL2CPP build. Require **45/45 passed**
in `artifacts/unity/editmode.xml`; inspect `editmode.log` and `windows-build.log`.
All generated outputs/caches are ignored. Return any compilation, USS import,
EditMode or player/layout errors for correction before merge.

## Full visual/functional local acceptance

- **1280×720 windowed:** readable normal text/buttons; compact strip/header visible,
  no overlaps, useful map area, Production drawer and Details toggle work; long
  politics/reports/input content scrolls and dropdown choices remain readable.
- **1600×900:** Standard rail/header transition is clean, no awkward/dead geometry;
  production defaults closed until chosen and map retains useful space.
- **1920×1080:** reference composition has clear LCARS identity, negative-space gaps,
  curves and hierarchies; map remains dominant, selected/hover/focus/disabled states
  are obvious without color alone. End Turn and Fit Map are bounded/easy to reach.
- **2560×1440:** proportional height scaling, readable furniture with no tiny type.
- **3440×1440:** same comfortable vertical scale, significantly larger map;
  production can auto-dock but manual visibility wins; details stay bounded.
- **5120×1440:** additional width primarily belongs to map; no giant pills/elbows,
  excessive decorative dead area or stretched raster shapes.
- **Live resize:** drag repeatedly between narrow/wide and same-aspect sizes;
  no duplicate/lost controls, disappearing map, feedback oscillation, clipped text
  or stale compact mode. Curves redraw, scroll/input state remains sensible and
  music continues without restarting or creating additional audio sources.
- **End Turn:** authoritative turn/phase/revision update; enabled/disabled appearance
  remains clear and data refreshes only from snapshots.
- **Research:** select/change a target, progress through turns, inspect unlocks.
- **Production:** owned worlds/queues/costs visible independently of map selection;
  enqueue and confirmed cancel use real commands; right panel has no duplicate
  full management section. Test toggling, auto-dock and manual resize overrides.
- **Politics/worlds:** inspect cohorts/policies/stability/environment/resources and
  deliveries; issue the existing policy command and check refreshed authoritative data.
- **Weighted geography:** preview then submit fleet route; verify costs/ETA/partial
  progress, selection, pan/zoom, wormhole/hazards and Fit Map remain operational.
- **Save/load:** create/overwrite/confirm/load a slot, reject bad input without
  replacing live state; restore pending, partial and fully buffered combat saves.
- **Audio:** Master/Music/Mute settings and soundtrack transitions work; no restart
  or duplicate source on tab changes, resize, combat or load.
- **AI-only combat:** continues without planner interruption; voluntarily inspect
  Recent Combat Reports afterward.
- **Human combat:** planner interrupts; only persisted Human fleets have controls,
  AI participants are read-only; resolve, review hull/loss/retreat/phase data,
  Continue across encounters, explicitly finalize into Planning; history persists.
- **Integrated stack:** all 45 EditMode cases pass, Windows x64 IL2CPP builds and
  the actual executable launches/loads the real DS9 slice. Do not merge until this
  full combat/audio/LCARS stacked acceptance is returned.
