# Unity strategic vertical slice

Editor pin: **6000.6.5f1** (Unity 6.6), URP **17.6.0**. Preserve the existing Universal 3D scaffold. This client is a work in progress until the Unity test/build and desktop acceptance gates below pass; a .NET build alone does not verify Unity compilation or display behavior.

## Prepare portable libraries

From the repository root, using the SDK pinned by `global.json`:

```sh
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
```

Run this before opening Unity and after changing portable sources. It restores with locked mode, builds Application and its dependencies in Release/netstandard2.1, copies exactly Contracts/Application/Simulation DLLs to `Assets/Plugins/HobbyDS9`, and copies the exact scenario bytes to `Assets/HobbyDS9/Resources/ds9-slice.json`. Both DLLs and the copied scenario are ignored outputs. Their stable `.meta` identities are tracked; never regenerate or replace them during preparation. There is no manual DLL-copy step. Close the Editor while preparing to avoid platform file-lock/import races.

`HobbyDS9.UnityClient.asmdef` uses explicit precompiled references to **Application** and **Contracts** only, plus Unity's Newtonsoft JSON adapter. It has no Simulation reference. Simulation is distributed as Application's runtime dependency and is not auto-referenced by client scripts. Plugin `.meta` files set `isExplicitlyReferenced: 1`. All authoritative operations go through `ICampaignSession`, via the client `CampaignHost`. No portable source depends on Unity, Newtonsoft or UI state. No engine code is duplicated in Assets.

The pinned official `com.unity.nuget.newtonsoft-json` package is used because the DTOs have properties and Unity JsonUtility does not support that contract. Content hashing uses the exact resource bytes and SHA256; no save/content/rules versions change.

## Run the prototype

Open the existing SampleScene and press Play. `ClientEntry` bootstraps once at runtime and starts the real scenario through Application. It creates a UI Toolkit document with the default Unity theme, the tracked HobbyClient.uss flex layout, and placeholder colors/labels. It does not require the command-line host or modify the template scene.

- Drag empty map space with the left mouse button (or use the middle button) to pan; use the wheel to zoom around the pointer. Fit map restores the presentation camera.
- Select node labels or fleet markers. The right panel shows detached snapshot facts. Normal links and purple wormhole links show explicit traversal costs.
- End Turn passes the observed turn/revision to Application. New snapshots replace displayed facts; selection/pan/zoom never enter campaign state.
- AwaitingCombat disables End Turn. The panel shows the next canonical pending request, ships, tactics, target priorities and retreat options. Resolve with defaults or selected plans, repeat for remaining encounters, then explicitly finalize to complete the turn. Rejected retreats/results display their stable error without advancing the campaign.

Navigation node IDs are never parsed into system IDs. Fleet placement and labels use `AssociatedSystemId`; null associations produce transit-region nodes. Circular node placement is a deterministic placeholder view layout, not new navigation topology. Traversal costs are not movement budgets or turn estimates.

Major layout uses flex growth and a bounded sidebar, with a scrollable details/encounter panel. Panel scaling follows screen height rather than ultrawide aspect ratio. Individual map markers are positioned inside the map viewport. Required manual display checks remain 1920×1080, 2560×1440, 3440×1440 and 5120×1440; verify readable controls, visible fleets, pan/zoom, selection and combat scrolling at each size.

## Tests and Windows build

Use an activated **6000.6.5f1** Editor with **Windows Build Support (IL2CPP)** installed, plus the Visual Studio C++ desktop tools and Windows SDK required by Unity. Run from the repository root (quote the entire property argument if the Editor path contains spaces):

```sh
dotnet msbuild scripts/PrepareUnity.proj -t:EditMode "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
dotnet msbuild scripts/PrepareUnity.proj -t:Windows "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
```

The Windows target requires passing, nonempty EditMode results before building. Tests cover real scenario startup, no direct Simulation assembly reference, empty-space association handling, and canonical/reverse 25-turn client-host runs through combat. The pure `GalaxyProjection.cs` is also linked into the existing .NET test project for ordering, detachment, movement refresh and selection tests. Existing CI still runs locked restore, Release build, all .NET tests and both deterministic headless smokes on its three platforms. It also runs PrepareUnity.proj and `python scripts/check-unity-integration.py` to check metadata/GUIDs, explicit assembly references, copied DLLs and scenario bytes. These are static checks, not Unity compilation.

Editor logs, NUnit XML and the Windows player go under ignored `artifacts/unity`. `ClientBuild` enforces the Editor version, .NET Standard API compatibility and a Windows x64 IL2CPP build. The existing link.xml preserves portable DTOs and runtime dependencies; IL2CPP compilation and player execution still require local verification. Launch `artifacts/unity/Windows/HobbyDS9.exe` on Windows. Verify initial galaxy/fleets, all four display sizes, selection, End Turn and an AwaitingCombat → resolve → finalize → Planning cycle. The baseline 25-turn outcome remains turn 26/revision 228, 15 encounters, 8 ships/fleets, RNG `850d3ff45bd6b85e`.

Do not mark this milestone complete or merge to main until Editor compilation, EditMode tests, Windows build/launch, display acceptance and existing .NET gates are all green. The implementation branch is intentionally offered for local verification before merging. This cloud environment has no Unity Editor; use your local Windows installation for the remaining gates. No Unity test, visual acceptance or Windows build result is claimed here.

## Local acceptance and reporting

1. Pull `codex/unity-vertical-slice` into your local checkout (preserve any local changes first). Do not merge it into main yet.
2. Close Unity and run Prepare. Open `client/HobbyDS9.Unity` with Hub using 6000.6.5f1. Let the official Newtonsoft package resolve and all assets/scripts import. Require zero Console compilation errors.
3. Run the four cases under **Window → General → Test Runner → EditMode → Run All**, or close the Editor and run the EditMode target above. The XML must report Passed with a nonzero total.
4. In Play mode, check the four required Game view resolutions. On the initial map, Sol, Bajor and the other scenario systems must have real labels; each faction starts with one fleet. Select nodes/fleets, pan/zoom and use Fit map.
5. End turns until combat appears. Check participant/ship facts and plan controls; resolve defaults; resolve remaining requests; finalize explicitly; confirm the next Planning turn. Also exercise selected plans and a rejected invalid retreat without losing the checkpoint.
6. Close the Editor and run the Windows target above. It reruns EditMode tests before building. Launch the generated EXE and repeat the map/selection/End Turn/combat cycle; verify window resizing and all required aspect ratios.
7. Return `artifacts/unity/editmode.xml`, relevant Console errors or log excerpts from `editmode.log` / `windows-build.log`, and any runtime/display issues. These logs and builds are ignored artifacts, not source commits. If Unity normalizes tracked `.meta`/package files, retain the changes for review rather than regenerating GUIDs.

Deferred: visual polish/cinematic combat, saving UI, player economy/production/research command UI, fleet route editing UI, procedural geometry and new movement rules. The current milestone provides the requested selection, End Turn and combat-planning flow; Application retains the broader command/save surface for future UI increments.
