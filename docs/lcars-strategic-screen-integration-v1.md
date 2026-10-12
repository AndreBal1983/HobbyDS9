# LCARS strategic screen integration v1

The reviewed strategic prototype is preserved on `codex/lcars-strategic-screen-prototype-v1` at `b8fe3d512c3616103a70310fd8bd4da75c525f7d`. It was pushed and has not been rewritten.

The integration branch is `codex/lcars-strategic-screen-integration-v1`, created from exact accepted main `90edc7bff4fe9ecfd8a958417b5270a38ec2153a`. The prototype implementation and clipping fix were cherry-picked. Main is not merged or changed by this delivery.

## Reconciliation

- `ClientEntry.cs`: retained main's Engineering target/reserve and VSync initialization while adopting the LCARS resource header. Reserve presentation now uses conceptual research units rather than internal 600-unit values.
- `Lcars.uss`: kept the accepted Science allocation cards and progress-bar styles alongside the reviewed strategic shell styles.
- `HobbyDS9.Simulation.Tests.csproj`: retained both ScienceAllocationDraft and StrategicScreenPresentation linked sources.
- Prototype assumptions and tests were adapted to Engineering reserve and Engineering application requirements. Production uses `RequiredEngineeringApplicationDefinitionIds` from accepted contracts.
- Native End Turn exposed an existing delivery-history presentation error: navigation-node IDs were looked up as system IDs. The client now resolves navigation nodes correctly. A real post-delivery Mars regression checks rendering, collapsed optional history, eligible production, and unchanged saved campaign state.

Simulation, Application, Contracts, scenario content, ResearchPanelView, ScienceAllocationDraft and PrepareUnity.proj are unchanged from accepted main. Save 13, content 9, rules `science-engineering-v1`, six simultaneous Science disciplines, 600-unit arithmetic, 70/30 split, stamped Apply, local preview, Equalize Science, Engineering projects and research save/load behavior are preserved. All 61 accepted Unity cases remain, with three strategic presentation cases added.

## Final local verification — 12 October 2026

| Gate | Result |
| --- | --- |
| Locked restore | Passed |
| Release build | Passed; zero warnings/errors |
| Complete .NET tests | 945 passed; zero failures/skips |
| Canonical, reverse and repeated 25-turn smokes | Passed; matching accepted hash |
| PrepareUnity and static asset/assembly checks | Passed |
| Unity 6000.6.5f1 EditMode | 64 passed; zero failures |
| Windows x64 IL2CPP | Passed; final player launched and retested |
| CI | Final branch-tip run and platform results are recorded in the delivery response |

All three smokes retain turn 26/revision 185, 17 resolved encounters and RNG `d8919406d6e01cf1`. Accepted save SHA256 remains `83bdb9013db7ac7b2b33ef77c4b591a4bca065544a01912eb48c48161688f5c4`. Scenario SHA256 remains `d2dc4ec92b535cdcb5b4eab79f46b99617b19e1b048272d46b63721d28438653`.

Generated caches were moved to an acceptance copy on E: after the C: drive filled during an earlier IL2CPP compile. Final runtime/test source was copied from the integration worktree, and tests/build ran with PrepareUnity's UnityProject/PluginOutput/Artifacts overrides. This kept original user work and branch history intact. Generated settings, caches and build artifacts are excluded from the integration commit.

Evidence root: `E:\HobbyDS9\artifacts\lcars-integration-project\artifacts\unity`. Test evidence is `editmode.xml`/`editmode.log`; IL2CPP evidence is `windows-build.log`; player is `Windows\HobbyDS9.exe`.

## Visual acceptance

Actual Unity Game view captures, made with the committed CaptureBoth Editor helper in SampleScene Play mode on the final presentation source:

- `E:\HobbyDS9\artifacts\lcars-integration-project\artifacts\unity\lcars-strategic-prototype\mars-selected-1920x1080.png`
- `E:\HobbyDS9\artifacts\lcars-integration-project\artifacts\unity\lcars-strategic-prototype\mars-selected-3440x1440.png`

Both dimensions were validated by the capture helper. `visual-acceptance.log` records successful completion. These are real Game view screenshots, not recreated interfaces or the previous RenderTexture evidence. Both were inspected and compared with the preserved prototype images. The resource strip, connected rail, dominant map, compact right inspector and activity strip retain their reviewed geometry. Queue ship is visible and contained. Initial Mars correctly has zero local Dilithium and displays the existing resource explanation.

Long map marker names retain intentional ellipsis. At 1080p, the lower optional history foldout requires inspector scrolling; primary production controls remain visible. No new blocking visual issue was found.

## Native acceptance

Executed with Windows mouse/keyboard input in the IL2CPP player:

- Mars/Sol, Bajor and Federation fleet selections update the inspector.
- Empty-map left drag pans; mouse wheel zooms around the pointer; Fit Map resets the view. Passive decoration did not intercept these gestures.
- Sol-to-Bajor route preview shows cost 7, budget 3 and ETA 3 without changing campaign revision; confirmation accepts the queued route.
- Research shows Science 70% (2.8) and Engineering 30% (1.2), six allocation controls and Engineering requirements. Biotech +5 changes local preview without revision change; stamped Apply commits; Equalize previews equal allocations and Apply commits them. Inspector scrolling reaches Engineering controls.
- After End Turn delivers Dilithium, Mars Scout queues with the expected resource/credit deduction. The production dock opens, cancellation requires confirmation, and confirmation refunds resources. Closing the dock restores map width.
- End Turn advances turn, credits and activity text. Combat navigation opens a generated AI battle report with phase/shot/damage/survivor details. Human combat command behavior remains covered by the accepted automated harness; a new human battle was not manually played in this acceptance pass.
- Audio navigation exposes independent music/SFX controls. The user explicitly confirmed both music and SFX audible.
- Fullscreen/window transitions work. Final native border drag resized the player from 2362x1254 to 1942x1101; header, map, inspector, production action and activity strip reflowed correctly.
- Final post-delivery retest shows reserve 1.2, collapsed history and enabled Queue ship without the previous presentation exception. Player log contains no exception from that retest.
- VSync remains explicitly enabled with `QualitySettings.vSyncCount = 1`.

Scope remains the integrated strategic screen. No additional screens were redesigned and no merge to main was performed.
