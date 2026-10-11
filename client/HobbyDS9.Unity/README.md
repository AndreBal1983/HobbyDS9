# Unity strategic vertical slice

Editor pin: **6000.6.5f1** (Unity 6.6), URP **17.6.0**. Preserve the existing Universal 3D scaffold. Historical vertical-slice acceptance on this Editor version passed: compilation, real scenario/map/fleets, selection/pan/zoom, End Turn/AI movement, combat planning/buffering/finalization with persistent damage, 4/4 EditMode tests, and a launched Windows x64 IL2CPP player. These Unity results were reported by the project owner; cloud CI verifies the .NET and static integration gates.

## Prepare portable libraries

From the repository root, using the SDK pinned by `global.json`:

```sh
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
```

Run this before opening Unity and after changing portable sources. It restores with locked mode, builds Application and its dependencies in Release/netstandard2.1, copies exactly Contracts/Application/Simulation DLLs to `Assets/Plugins/HobbyDS9`, and copies the exact scenario bytes to `Assets/HobbyDS9/Resources/ds9-slice.json`. Both DLLs and the copied scenario are ignored outputs. Their stable `.meta` identities are tracked; never regenerate or replace them during preparation. There is no manual DLL-copy step. Close the Editor while preparing to avoid platform file-lock/import races.

`HobbyDS9.UnityClient.asmdef` uses explicit precompiled references to **Application** and **Contracts** only, plus Unity's Newtonsoft JSON adapter. It has no Simulation reference. Simulation is distributed as Application's runtime dependency and is not auto-referenced by client scripts. Plugin `.meta` files set `isExplicitlyReferenced: 1`. All authoritative operations go through `ICampaignSession`, via the client `CampaignHost`. No portable source depends on Unity, Newtonsoft or UI state. No engine code is duplicated in Assets.

The pinned official `com.unity.nuget.newtonsoft-json` package is used because the DTOs have properties and Unity JsonUtility does not support that contract. Content hashing uses the exact resource bytes and SHA256; the current Science/Engineering candidate uses save 13/content 9/rules `science-engineering-v1`, rejecting older packages/saves.

## Run the prototype

Open the existing SampleScene and press Play. `ClientEntry` bootstraps once at runtime and starts the real scenario through Application. It creates a UI Toolkit document with the default Unity theme, the tracked HobbyClient.uss flex layout, and placeholder colors/labels. It does not require the command-line host or modify the template scene.

- Drag empty map space with the left mouse button (or use the middle button) to pan; use the wheel to zoom around the pointer. Fit map restores the presentation camera.
- Select node labels or fleet markers. The right panel shows detached snapshot facts. Normal links and purple wormhole links show explicit traversal costs.
- End Turn passes the observed turn/revision to Application. New snapshots replace displayed facts; selection/pan/zoom never enter campaign state.
- AwaitingCombat disables End Turn. The panel shows the next canonical pending request, ships, tactics, target priorities and retreat options. Resolve with defaults or selected plans, repeat for remaining encounters, then explicitly finalize to complete the turn. Rejected retreats/results display their stable error without advancing the campaign.

Navigation node IDs are never parsed into system IDs. Fleet placement uses `LocationNodeId`; system labels use explicit `AssociatedSystemId`; null associations produce transit-region nodes. Circular node placement is a deterministic placeholder view layout, not new navigation topology. Traversal costs and movement budgets are distinct. The current candidate obtains ETA from Application using authoritative traversal rules.

Major layout uses flex growth and a bounded sidebar, with a scrollable details/encounter panel. Combat controls stack their labels above full-width inputs so tactic and priority values remain readable. Panel scaling follows screen height rather than ultrawide aspect ratio. Individual map markers are positioned inside the map viewport. Required manual display checks remain 1920×1080, 2560×1440, 3440×1440 and 5120×1440; verify readable controls, visible fleets, pan/zoom, selection and combat scrolling at each size.

## Tests and Windows build

Use an activated **6000.6.5f1** Editor with **Windows Build Support (IL2CPP)** installed, plus the Visual Studio C++ desktop tools and Windows SDK required by Unity. Run from the repository root (quote the entire property argument if the Editor path contains spaces):

```sh
dotnet msbuild scripts/PrepareUnity.proj -t:EditMode "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
dotnet msbuild scripts/PrepareUnity.proj -t:Windows "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
```

The Windows target requires passing, nonempty EditMode results before building. Tests cover real scenario startup, no direct Simulation assembly reference, empty-space association handling, and canonical/reverse 25-turn client-host runs through combat. The pure `GalaxyProjection.cs` is also linked into the existing .NET test project for ordering, detachment, movement refresh and selection tests. Existing CI still runs locked restore, Release build, all .NET tests and both deterministic headless smokes on its three platforms. It also runs PrepareUnity.proj and `python scripts/check-unity-integration.py` to check metadata/GUIDs, explicit assembly references, copied DLLs and scenario bytes. These are static checks, not Unity compilation.

Editor logs, NUnit XML and the Windows player go under ignored `artifacts/unity`. `ClientBuild` enforces the Editor version, .NET Standard API compatibility and a Windows x64 IL2CPP build. The existing link.xml preserves portable DTOs and runtime dependencies. Windows x64 IL2CPP compilation and player startup passed local acceptance; repeat these checks after engine/client changes. Launch `artifacts/unity/Windows/HobbyDS9.exe` on Windows. Verify initial galaxy/fleets, all four display sizes, selection, End Turn and an AwaitingCombat → resolve → finalize → Planning cycle. The current candidate baseline is turn 26/revision 187, 17 encounters, 9 ships/fleets, RNG `d8919406d6e01cf1`.

Unity acceptance is performed locally because the cloud environment has no Unity Editor. Keep local compilation, EditMode tests and Windows build/launch green alongside .NET CI. Verify the four display sizes when making layout changes; the final control-sizing/importer cleanup received static checks in the cloud rather than another Unity run.

## Local acceptance and reporting

1. Pull the current `main` into your local checkout, preserving any local changes first.
2. Close Unity and run Prepare. Open `client/HobbyDS9.Unity` with Hub using 6000.6.5f1. Let the official Newtonsoft package resolve and all assets/scripts import. Require zero Console compilation errors.
3. Run the four cases under **Window → General → Test Runner → EditMode → Run All**, or close the Editor and run the EditMode target above. The XML must report Passed with a nonzero total.
4. In Play mode, check the four required Game view resolutions. On the initial map, Sol, Bajor and the other scenario systems must have real labels; each faction starts with one fleet. Select nodes/fleets, pan/zoom and use Fit map.
5. End turns until combat appears. Check participant/ship facts and plan controls; resolve defaults; resolve remaining requests; finalize explicitly; confirm the next Planning turn. Also exercise selected plans and a rejected invalid retreat without losing the checkpoint.
6. Close the Editor and run the Windows target above. It reruns EditMode tests before building. Launch the generated EXE and repeat the map/selection/End Turn/combat cycle; verify window resizing and all required aspect ratios.
7. Return `artifacts/unity/editmode.xml`, relevant Console errors or log excerpts from `editmode.log` / `windows-build.log`, and any runtime/display issues. These logs and builds are ignored artifacts, not source commits. If Unity normalizes tracked `.meta`/package files, retain the changes for review rather than regenerating GUIDs.

The original vertical-slice acceptance above is retained as historical context. The Strategic Gameplay UI v1 work and its separate acceptance gates are described below. Visual polish, cinematic combat, procedural geometry and new gameplay remain deferred.

## Strategic Gameplay UI v1 (awaiting local acceptance)

The new interface keeps one tabbed sidebar beside the galaxy map. The persistent top bar shows the human civilization, credits, research target/reserve, turn, phase and revision. Tabs remain selected during ordinary snapshot refreshes; entering a combat checkpoint opens Combat automatically. The fixed DS9 scenario has one human civilization. The client selects the first ordinal human actor in other content; multi-human hot-seat selection is deferred.

- **Empire** lists owned colonies and fleets with links to their contextual controls.
- **Research** lists every definition, cost, prerequisites and available/current/completed/locked state. Select an available target to submit the existing command. Reserve is a civilization-wide pool, not separate progress on each technology.
- **Selection / colony** shows population/capacity, food, named local resources, owner credits, ordered production queue and ship design costs. Queue an available design; cancelling requires inline confirmation and uses the existing proportional refund of unused costs.
- **Selection / fleet** shows ownership, hull/design facts and queued route. Choose Set / replace route, click a map node, review the gold route and ordered steps, then Confirm route. Same-node routes clear queued orders. The displayed traversal cost is not an arrival-time estimate. Cancelling a preview changes no campaign state.
- **Save / Load** offers three fixed slots under `UnityEngine.Application.persistentDataPath/CampaignSaves` (normally `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/CampaignSaves` on Windows). An occupied slot requires overwrite confirmation; loading confirms replacement of unsaved campaign progress. The host writes UTF-8 and flushes it to disk into a same-directory temporary file, then atomically replaces the existing slot or moves a new slot into place. It never deletes a prior valid save as a fallback. Failed replacement leaves that save intact; failed JSON parsing/validation leaves the live campaign intact. No save data is written into the project.
- **Combat** retains independent resolution, buffering and explicit finalization. Draft combat plans survive tab switches in the running view; they are intentionally not campaign-save data. Saves preserve pending requests and already-buffered results. Loading clears draft plans, route preview, object selection and camera, then shows the loaded phase.

`StrategicClient.cs` is a Unity-independent presentation controller compiled into the client assembly and linked into ordinary .NET tests. It owns an Application session reference and detached snapshots, creates stamped commands, previews routes with `FindRoute`, and replaces the session only after successful load/capture. `CampaignHost` owns Newtonsoft serialization and content hashing; `SaveSlotStore` owns file I/O. `StrategicPanels.cs` holds only UI composition and ephemeral view state. `StrategicOptions` in Application adds immutable advisory research/production availability projections so the UI can explain disabled choices without implementing gameplay validation. Simulation still performs every authoritative validation.

Player IDs use `player-ui.<counter>` and skip `AcceptedPlayerCommandIds`, including after a restart/load. They consume neither campaign allocation nor RNG, and are not replay-deterministic across different UI sessions (only gameplay outcomes for the same commands are deterministic). Each attempted UI action uses a fresh ID. Actions retain the observed turn/revision, route previews retain the original snapshot, and stale rejection never triggers an automatic retry. The refreshed UI shows a readable reason plus the stable error code.

### Verify this milestone locally before merging

The previous milestone's successful local Unity acceptance does **not** cover these new controls. This branch adds five EditMode cases, bringing the expected total to **9**. Cloud verification executes the pure client tests, .NET suite, both baseline smokes and static integration checks; it cannot execute Unity or Windows IL2CPP.

From PowerShell at the repository root, with the Editor closed and local changes preserved:

```powershell
git fetch origin
git switch codex/strategic-gameplay-ui-v1
git pull --ff-only origin codex/strategic-gameplay-ui-v1
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
python scripts/check-unity-integration.py
dotnet msbuild scripts/PrepareUnity.proj -t:EditMode "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
dotnet msbuild scripts/PrepareUnity.proj -t:Windows "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
& ./artifacts/unity/Windows/HobbyDS9.exe
```

Windows requires the existing IL2CPP module, Visual Studio C++ desktop tools and Windows SDK. Require zero Editor compilation errors and 9/9 EditMode passes. The new tests exercise real host JSON round-trip, malformed/incompatible load preservation, route preview/map projection, slot replacement and combat checkpoint serialization. Windows target reruns EditMode tests before building.

Manual acceptance in the player:

1. At all four required resolutions (1920×1080, 2560×1440, 3440×1440, 5120×1440), verify status, readable buttons/dropdowns, scrollable panels and a map that occupies most of the screen. Confirm tab persistence and pan/zoom/Fit map.
2. From Empire, manage Sol. Queue Scout, confirm credits/Dilithium and queue update, cancel once with the keep-current-state button, then confirm cancellation and check the refund. Inspect a foreign colony and verify player orders are disabled.
3. Select Advanced Starship Engineering in Research, verify Current status/reserve, advance turns and verify Completed state and Advanced Scout availability. Check disabled explanations for insufficient resources or missing research.
4. Select the Federation fleet. Preview a route, cancel it and confirm the queued route is unchanged. Preview/confirm a new route, replace it, and advance a turn to observe authoritative movement. Inspect the explicit wormhole cost. Same-node confirmation clears the route.
5. Pass through AwaitingCombat with selected plans/defaults, verify research/production/routes are disabled, buffer results, then finalize. Switch tabs while editing plans and confirm draft selections persist.
6. Save to an empty slot; test overwrite confirmation. Save while awaiting combat and again after buffering. Load each checkpoint and finish it without rerunning strategic phases or buffered encounters.
7. Save a campaign, quit the player, launch again, load it and verify exact turn/phase, resources, queue, research, route and damage. Test a malformed copy in a spare slot: it must show an error/disabled Load and keep the current campaign. Restore that test slot afterward.
8. Return the EditMode XML, Windows build/launch result, and any Console/runtime/display errors. Keep generated logs/builds/saves out of Git. Merge to main only after this acceptance passes.

Limitations: three fixed slots, no rename/delete UI or autosave; no camera/UI preference persistence; no multi-human selector; placeholder map geometry and art. No gameplay, save/content/rules/resolver version or balance changes.


## Population/economy v1 local acceptance

Historical economy milestone instructions (the integrated acceptance is recorded below): this candidate had **14 Unity EditMode cases** (the previous 9 plus mixed-world/resource/round-trip and strict incomplete/obsolete-save tests). They require local execution; cloud checks do not establish Unity compilation or IL2CPP acceptance.

Close Unity, pull `codex/economy-population-foundation-v1`, then from the repository root run:

```powershell
git switch codex/economy-population-foundation-v1
git pull --ff-only
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
dotnet msbuild scripts/PrepareUnity.proj -t:EditMode "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
dotnet msbuild scripts/PrepareUnity.proj -t:Windows "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
```

Use Unity 6000.6.5f1 with Windows Build Support (IL2CPP), Visual Studio Desktop development with C++, and a compatible Windows SDK. Launch `artifacts/unity/Windows/HobbyDS9.exe`.

1. Select Sol and use the **World** dropdown to switch Earth/Mars. Earth must show Human/Vulcan/Andorian cohorts; Mars has lower population and higher installed industry. Empire Manage buttons select the corresponding world.
2. Inspect habitat shares, natural/effective capacity, automatic allocations, cohort legal/needs ratings, stability/target, food, credit/R&D contribution, infrastructure and local Dilithium. Bajor has a Bajoran majority and Cardassian resident minority.
3. Queue/cancel Scout on Earth; verify local Dilithium and credits reserve/refund. Mars begins without Dilithium and becomes eligible after delivery. Costs must show named resources.
4. Advance turns and inspect last-turn origin/path/destination receipts, receiving stability and integrity. Nearby hostile fleets halve integrity; direct occupation blocks imports. Mars is intentionally import-dependent and can experience shortages in the idle-player 25-turn run. Population changes and migration totals follow the deterministic rules; no manual population assignment exists.
5. Exercise research, production completion, routes, AwaitingCombat, plans, buffering and explicit finalization. Save before/after turns and combat; loading must restore planet/cohort/stability/resource facts. Old save-format slots are rejected and leave the active session intact.
6. Check scrollable panels at 1920×1080, 2560×1440, 3440×1440 and 5120×1440. The map remains the main workspace.

Refugees are exercised through deterministic .NET fixtures, including rejection/no-destination, retained origin/legal status/memories and reload; no invasion trigger or policy UI was invented. See [the new baseline and scope](../../docs/economy-population-v1.md). Return compilation/test/build results and manual acceptance before this branch merges to main.

## Population politics and strategic geography v1 acceptance

The politics/geography integration added four EditMode cases, bringing that earlier candidate to **18** cases, including governance/stale snapshots, weighted preview/partial-edge JSON round-trip, an independent empty hazard region and hidden-route filtering. These tests are authored, not executed in the cloud. Previous 14/14 local acceptance covers the earlier economy milestone only.

With Unity closed, pull `codex/population-politics-strategic-geography-v1` and run from the repository root on Windows:

```powershell
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet msbuild scripts/PrepareUnity.proj -t:EditMode "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
dotnet msbuild scripts/PrepareUnity.proj -t:Windows "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
```

For the current combat-control/reporting candidate, require zero Editor compilation errors, 24/24 EditMode passes, Windows x64 IL2CPP build success, and a launched `artifacts/unity/Windows/HobbyDS9.exe`. Windows Build Support (IL2CPP), Visual Studio Desktop development with C++ and a compatible Windows SDK remain required. See [the exact manual acceptance checklist](../../docs/population-politics-geography-v1.md#local-unity-acceptance) for governance, partial travel, combat and all four display sizes. Main remains unchanged until these new checks are accepted.


## Production overview layout (current candidate)

The strategic workspace is `Production | Galaxy map | Selection/details`. The
**Production** header control toggles a separate, scrollable left dock. At
height-scaled workspace widths below 2300 UI units it starts collapsed (including
1920×1080 and 2560×1440); at ultrawide widths it starts open. An explicit toggle
persists across selection changes, turns and resizing for the current client
session. Both docks have bounded widths; the map takes the remaining space.

Production lists every human-owned world in ordinal colony-ID order, independently
of the selected system/fleet. Industry, queue progress, paid amounts, design costs
and unavailable reasons come from detached Application snapshots/options. Queue
and confirmed Cancel use the same StrategicClient stamped Application commands
as before. Rendering issues no commands. Politics, population, environment,
infrastructure, resources and delivery reports stay in the right details panel;
its former full production-management block is replaced by an Open Production
shortcut. No save/content/rules versions or simulation balance changed.

This adds three Unity EditMode cases, bringing the expected current total to
**21**. Run the EditMode and Windows targets above with Unity 6000.6.5f1; neither
Unity compilation nor IL2CPP execution is available in the cloud.

For local acceptance at 1920×1080, 2560×1440, 3440×1440 and 5120×1440:

- Toggle Production; confirm the map remains usable with both docks open.
- Confirm Earth and Mars appear, with local Industry, queue progress/paid amounts,
  design costs and disabled design reasons; enemy worlds are absent.
- Select a fleet, Bajor and an empty navigation region: production remains usable
  and its world foldouts retain their open/closed state across refreshes.
- Queue a legal design from the left dock; confirm credits/resources and revision
  refresh from authoritative state. Cancel through the inline confirmation; check
  the refund and queue refresh. Opening/collapsing/rendering alone changes no turn
  or revision. Inspect right-side Politics/population/environment/deliveries and
  confirm there is no duplicate full production block.
- End Turn, pass through AwaitingCombat and explicitly finalize; production remains
  visible but commands are disabled during the checkpoint and resume in Planning.

## Audio Foundation v1 stacked candidate

`codex/audio-foundation-v1` is based exactly on `79009a3` and leaves the separate
politics/geography/combat candidate unchanged. It includes both original Deep
Station MP3s, a referenced Resources catalog, persistent two-source AudioDirector,
six-second scheduled cue crossfades and an Audio tab with PlayerPrefs Master/Music
volume and Mute. Music is entirely presentation state and continues across panels,
turns, save/load and combat. Sources stream, preserve stereo/sample rate and never
hard-loop these full cues. There is no local asset-copy step.

This branch expects **747 .NET tests** and **24 Unity EditMode cases** (21 base + 3
audio). Gameplay versions and the deterministic hash are unchanged. Unity/audio
runtime and Windows IL2CPP acceptance remain local. See the [complete audio guide](../../docs/audio-foundation-v1.md)
for original-byte hashes, import settings, architecture, exact Windows commands and
the listening/duplicate-source/save-isolation checklist.

## Combat UX/report stacked candidate

`codex/combat-ux-report-fix-v1` stacks on the audio candidate. AI-only encounters
resolve/buffer/finalize through Application without opening the planner. Human
encounters expose controls only for persisted Human fleets, pause for reports and
complete through explicit finalization. Recent reports remain accessible in
Planning and clear on campaign replacement. Combat remains free-for-all between
different civilizations; no balance/save/content/rules/resolver changes.

Current expected verification: **771 .NET tests**, **32 authored Unity EditMode
cases**. Unity runtime and Windows x64 IL2CPP acceptance remain local. See
[combat-ux-report-fix-v1.md](../../docs/combat-ux-report-fix-v1.md) for architecture,
exact commands, versions/baseline and the complete local acceptance checklist.

## Responsive LCARS-inspired UI stacked candidate

`codex/lcars-ui-foundation-v1` starts exactly at `795b485`. It introduces an original
console palette, vector elbow, reusable controls/frames and a live responsive shell
without gameplay/audio/save changes. Expected **797 .NET tests**, **45 authored
Unity EditMode cases**. Existing acceptance branches/main stay unchanged. See
[lcars-ui-foundation-v1.md](../../docs/lcars-ui-foundation-v1.md) for tokens, logical
breakpoints/readability scaling, exact local IL2CPP commands and the full six-size
visual/functional acceptance matrix. Unity compilation/rendering remains unverified
in cloud.

### Integrated LCARS audio acceptance

The user-supplied Federation WAVs and six populated catalog references are now retained from accepted handoff `34c2c6660d2901de2257d808e1b852504bd38b50`. [Provenance](../../docs/audio-sources.md) records exact filenames, SHA256 and accepted importer settings. Cloud source downloads had returned HTTP 403; no substitutes were used.

User-reported acceptance: Unity 6000.6.5f1 EditMode 50/50 passed, Windows x64 IL2CPP build succeeded, music and soundtrack crossfade sounded correct, LCARS SFX and independent audio controls passed, responsive Windows-player UI and AI-only/human combat/report flows were exercised. Consolidation restores three accidentally removed headless-safe elbow cases, giving 53 authored cases; no cloud Unity execution is claimed. [Verification guide](../../docs/lcars-sfx-foundation.md) records exact commands and distinguishes local reports from cloud gates. This describes the accepted base; the new research candidate enables client-only VSync defaults, while graphics options remain deferred.

## Science & Engineering candidate revision

`codex/science-engineering-foundation-v1` builds on accepted main `a0f7506`, revising the unaccepted `242247d` candidate. Research has exact 70/30 generation, six simultaneously progressing disciplines and equal starting shares shown as 16⅔%. Per-card +/− previews transfer 5 percentage points evenly among other disciplines, with enum-order residual units. Equalize Science restores a local 100-each draft; Apply Science allocation submits all six shares using the observed turn/revision. Editing/rendering does not mutate the campaign. Top-level split Apply remains separate. Maxed allocations warn that their share banks.

Engineering displays New capability / Refinement / Civil application, Locked / Available / Current / Completed, progress, prerequisites and authored result summary. Only Advanced Starship Engineering is live DS9 content; completed projects unlock capabilities. Costs/reserves/receipts display conceptual values or percentages, never raw internal units as primary UI. Save 13/content 9/rules `science-engineering-v1` and combat resolver/RNG identities remain unchanged. Save 12/content 8 and incomplete intermediate candidate-13 saves are rejected, with no migration for the unaccepted candidate.

Bootstrap retains VSync count 1 and target frame rate -1. The revised suite has 61 authored EditMode cases, covering six shares, preview/confirmation, equalization/reload, conceptual receipts, project kind, maxed-bank warning, Engineering progress/completion and combat guards alongside existing client/audio/LCARS tests. Cloud checks do not execute Unity. Local Unity 6000.6.5f1 and Windows x64 IL2CPP require fresh acceptance. Windows needs Windows Build Support (IL2CPP), Visual Studio Desktop Development with C++ and Windows SDK. See [exact commands/checklist and deterministic baseline](../../docs/science-engineering-v1.md). Main remains unchanged.
