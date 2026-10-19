# Single-player demo Unity frontend — review candidate

19 October 2026. **Source implementation candidate; native acceptance incomplete.** This is the separate single-human demo frontend on the accepted portable demo backend/host and original ArtDemo package. Do not merge or publish a validated-demo pointer until actual keyboard interaction and the full native campaign loop pass. The later owner's autonomous-demo instruction authorizes this implementation; the historical tactical UX documentation branch is separate and complete. Work resumed after the 14 October Windows connection outage; unchanged results below are retained from that source, with the final selector regression and static checks rerun on 19 October.

## Implemented player flow

The dedicated Windows build opens Start Demo / Continue directly without command-line knowledge. A fresh campaign uses the actual demo backend; the public objective, turn, outcome and costs come from immutable projections. One Federation human faces the lawful AI; no enemy actor switch appears. Human round commitment resolves one jointly committed round; aftermath remains explicit. Construction and Engineering use actual availability and prices. Existing v1 designer remains a labelled local preview without unsupported v2 publication or conversion.

Menu, Map, Worlds, Research, Production, Fleets, Combat, Save/Load, Audio, Display and Ship Design share the existing live controls. The selected owned ship uses its already-filtered civilization definition and design identity for the ArtDemo preview. Only the exact Federation/source-cruiser binding renders the original cruiser; other identities get the provider's truthful generic fallback. Replacement/detachment clears the image and disposes the preview. No enemy roster/save inspection or gameplay inference is used for art.

Display offers the three accepted application resolutions and window modes; this changes the player window, not the Windows desktop. The dedicated builder temporarily selects first-launch 1280 × 720 Windowed/resizable and restores original project settings afterward. Existing default builds, VSync bootstrap and old saves remain supported.

Display choices currently apply to the running application; persistence of a preferred display mode across fresh launches is deferred. Existing audio preferences retain their supported device-local storage.

## Persistence and authority

The Newtonsoft adapter and atomic disk store accept only the complete format-4 wrapper. Five raw authored resources retain exact source bytes and the backend hash ladder. Slots demo1–demo3 live under the separate HobbyDS9SingleplayerDemoOnly-v4 directory/prefix; old format-3 slots are never overwritten or silently loaded. Missing/forged carriers, nested exports and wrong profiles fail closed.

Successful load replaces host, controller and presentation together and invalidates obsolete work. Failed save/load retains local edits and accepted state; failed overwrite retains the previous valid file. Save records committed state while local edits remain unsent. Starting a new campaign first creates a valid candidate and then replaces the old context; abandoning local work requires the existing confirmation. Terminal outcomes disable mutations while inspection/save remain available. Unity does not evaluate combat, AI decisions, objectives or performance values.

## Evidence and outstanding gates

| Check | Actual result |
| --- | --- |
| Complete actual-source portable adapter suite | 123 passed, zero failed/skipped; includes 13 new demo codec/store/model cases and every prior case |
| Locked portable preparation and Release solution build | Passed, zero warnings/errors after restoring this new worktree's solution assets |
| Runtime API compiler using pinned receiving Editor/ArtDemo references | Passed, zero warnings/errors |
| Binding and integrated adversarial static cases | 7 + 6 passed; forbidden host dependencies and dispatch remain rejected |
| Art runtime file integrity | All 49 file hashes/GUID dependencies passed |
| Receiving graphics-enabled full EditMode inventory | 162 discovered: 158 passed, zero failed, four dedicated bootstrap cases explicitly ignored in this general run |
| Dedicated demo menu/panel graphics case | Passed, 30 actual runtime RenderTexture captures across 1280 × 720, 1920 × 1080, 3440 × 1440; no actor switch or state mutation |
| Dedicated legacy player graphics/action cases | 2 passed on 19 October, zero failures/skips; 75 captures, paid construction, route-preview invalidation, three-round battles and save/load checks retained |
| Keyboard → Input System provider → UI case | **Failed**, three retained dedicated attempts; Enter did not start the actual menu, so Tab and exactly-once confirmation assertions were not reached |
| Strict Windows x64 IL2CPP | Next: build this source as a native review candidate |
| Native single-human campaign, input, save/restart, audio/performance | Pending coordinator operation of the new executable; prior two-human package evidence is not evidence for this build |

Render capture uses depth24 so UI Toolkit stencil masks retain rounded-control glyphs. These captures use direct UI events to arrange/navigate the screen and do not prove OS mouse/keyboard input. Compact navigation captions and persistent footer are readable; fleet condition and route lists scroll independently. At 720p the preview makes condition rows require scrolling. Larger layouts keep the selected Cruiser's hull/four sectors/finite magazine visible. The shorter Destination and Contact details captions avoid clipped foldout instructions. Exact layout findings must be reconfirmed against the final native executable.

The keyboard fixture uses queued real Keyboard device states and InputSystem updates; no direct NavigationSubmit event is used in that case. Hidden batch Editor routing remains a possible cause, not an established diagnosis. Earlier native automation also failed Return/Tab while mouse/Escape worked, so actual product input remains an unresolved gate. No production Enter shortcut, added EventSystem, weakened assertion or fake pass is included. Failed logs/XML stay under artifacts/singleplayer-demo-ui/demo-input-{1,2,3}.

The resumed legacy renderer initially stopped at an obsolete caption-based destination lookup. The destination Foldout now has the stable identity fleet-route-destination, and the fixture asserts its presence before exercising the same selection, preview, invalidation and command path. Previous failing runs remain preserved; no meaningful assertion was removed. The first invocation had also omitted the required integrated actor argument; its separate bootstrap failure is retained rather than treated as a product regression.

The unchanged accepted backend records 1,777 .NET cases and independent review; those are inherited source evidence, not a rerun by this frontend milestone. Campaign determinism/finite pools/AI rules are unchanged here; native retained stocks and pending-combat restart still require combined proof.

## Reproduce and review

Run dotnet msbuild scripts/PrepareUnity.proj -t:Prepare followed by scripts/PrepareUnityDemo.proj -t:Prepare. Run the complete tests/HobbyDS9.UnityReviewAdapter.Tests project in Release, the binding/integrated scripts and their static tests, and tools/art/verify_demo_art_assets.py. Hosted CI requires at least 123 actual-source cases, all executed/passed, and compares each prepared demo resource's SHA256 against its authored source.

Pinned graphics Editor is Unity 6000.6.5f1. General EditMode run uses the ordinary project; dedicated demo renderer uses test filter DemoRuntimeAcceptanceTests.ActualDemoMenuAndPanelsRenderWithoutActorSwitchOrMutation with --hobbyds9-demo-render-tests --hobbyds9-singleplayer-demo. The separate meaningful keyboard fixture remains failing and must be reported independently. Existing player renderer uses PlayerConsoleRenderTests with --hobbyds9-player-render-tests --hobbyds9-integrated-playtest --hobbyds9-integrated-actor civ.federation. Optional API compiler now takes UnityArtDemoPath; optional split compiler requires receiving ArtDemo and InputSystem DLL paths and records their hashes.

Build the dedicated source through HobbyDS9.UnityReview.Editor.SingleplayerDemoBuild.BuildWindowsDemo. Output is artifacts/unity-singleplayer-demo/Windows/HobbyDS9Demo.exe with Start-Demo.cmd. Copy the complete output to a new versioned review-candidate package, recording source SHA, executable/GameAssembly hashes, build log and evidence. Preserve old packages, owner saves and the latest-validated pointer. No native input is held by this frontend worker; the coordinator owns the serialized desktop handoff.
