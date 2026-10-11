# Science & Engineering v1 local acceptance

Date: 2026-10-11 (Europe/Amsterdam).

Candidate: `8cc9dc8996fe5b31d59c2e35a64ae41247e5c72d`, branch `codex/science-engineering-foundation-v1`, plus the local test-harness and PrepareUnity changes described below. No gameplay or simulation source changed.

The existing checkout initially used `local/integrated-acceptance-handoff` at `34c2c6660d2901de2257d808e1b852504bd38b50`. The verified science branch was fetched and checked out without resetting, re-cloning, stashing or discarding local changes. Existing Unity settings changes and untracked audio/import files remained present during acceptance. No applicable AGENTS.md was found in the checkout or candidate.

## Results

- `dotnet msbuild scripts/PrepareUnity.proj -t:Windows` with UnityEditor pointing to `C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe`: exit code 0. This target runs Prepare, EditMode and the Windows build.
- EditMode: 61 total, 61 passed, 0 failed, 0 skipped. Evidence: `artifacts/unity/editmode.xml` and `artifacts/unity/editmode.log`.
- Windows build: `Build Finished, Result: Success`, followed by `Windows build completed: E:\HobbyDS9\artifacts\unity\Windows\HobbyDS9.exe`. Evidence: `artifacts/unity/windows-build.log`.
- Build implementation sets `BuildTarget.StandaloneWindows64` and `ScriptingImplementation.IL2CPP`. Output includes `HobbyDS9.exe` and a freshly generated `GameAssembly.dll` (42,322,432 bytes).
- `scripts/check-unity-integration.py`: passed. Scenario SHA256 remains `d2dc4ec92b535cdcb5b4eab79f46b99617b19e1b048272d46b63721d28438653`.
- `git diff --check`: passed.

## Narrow acceptance fix

The initial EditMode run passed 60/61 cases. `ResearchSplitPreviewIsLocalAndSubmissionUsesRealStampedBoundary` changed an unattached DropdownField and expected its value-change callback to enable Apply. Unity did not dispatch the event for that detached control.

The test now attaches the ResearchPanelView to a displayed editor window and closes it in a finally block. PrepareUnity's EditMode invocation retains batch mode but enables graphics because `-nographics` cannot initialize the test window. The Windows build invocation is unchanged. The test retains the preview-without-mutation, enabled Apply, stamped submission, stale rejection and save/load assertions.

Unity reported existing obsolete API warnings in AudioDirector, SfxDirector and AudioTests. This is not a claim of a warning-free Unity build. The portable Prepare assemblies built successfully; the previously reported cloud .NET suite and determinism gates were not rerun because no authoritative code changed.

## Windows player manual results

The built executable was launched and exercised through Windows Computer Use on 2026-10-11. Initial user-input guard rejections cleared after the user stopped interacting. No gameplay source changed.

| Check | Observed result |
| --- | --- |
| Initial R&D | Turn 1, revision 0: Science 70% (2.8), Engineering 30% (1.2), capacity 4. |
| Split preview / Apply | Selecting 50/50 retained the authoritative 70/30 summary and revision 0. Apply changed the summary to 2/2 and revision 1, with Order accepted. |
| Exact 50/50 spending | After End Turn, Turn 2 showed Engineering reserve 2. The UI-created save contained 2400 generated internal units, 1200 Science allocated/spent, 1200 Engineering allocated/reserved, and 200 progress/spent in each of six disciplines. No alternating split artifact. |
| Six Science cards | Biotech, Computing, Construction, Energy, Propulsion and Weapons were present and scrollable. Equal shares displayed 16⅔%; all showed 0.3% progress after the first turn. |
| Science preview / Apply | Biotech +5% preview became 21.67%, each other discipline 15.67%, while revision remained 6. Apply advanced it once to 7 and removed preview wording. The saved authoritative weights were 130,94,94,94,94,94, summing exactly to 600 (100%). |
| Equalize / persistence | Equalize changed Biotech back to 16⅔% preview at revision 7. Apply advanced revision to 8. Loading the previously saved Campaign 3 restored Turn 2/revision 7 and the authoritative 21.67% Biotech share. Campaign 3 was empty before this test; Campaign 1's incompatible old save was preserved. |
| Engineering presentation | Advanced Starship Engineering displayed New capability / Locked, 0% Development, conceptual cost 60, Advanced Scout unlock result, Construction interval 1→2 breakthrough 1 and Propulsion interval 1→2 breakthrough 2. It explicitly states one current project and retained progress on retargeting. Available/current transitions and retargeting were not ground out manually; the automated cases cover these. |
| Old Science focus | No single-discipline focus selector or focus wording was seen in the Research panel. |
| Production | Queued Scout at Mars: credits 57→47 and queue 0/20. Cancel + confirmation emptied the queue and refunded credits to 57. |
| Fleet / route / partial movement | Selected Federation Scout; Sol→Bajor preview cost 7, budget 3, ETA 3 without issuing an order. Confirm issued it once. Successive turns showed 3/7 and 6/7 in transit, then arrival at Bajor. |
| Human / AI combat | Onward Bajor→Bajoran Wormhole route led to a Turn 5 human Federation–Dominion encounter. One AI-only Gamma Exit battle was automatically buffered. Human defaults resolution produced a report: 6 shots, 4 hits, 2 misses, 3 phases, 7 shield damage, 9 hull damage. Finalization returned to Turn 6 Planning. |
| Audio preferences | Audio panel showed cue Deep Station. Music mute toggled while SFX mute remained off; SFX mute toggled while music mute remained off. Both were restored to unmuted; authoritative revision stayed 27. The user explicitly confirmed music/SFX playback and transitions, independent controls and save/load audio continuity passed. |
| Responsive display | Research was usable and scrollable at 3440×1440. Switching fullscreen/windowed and restoring to approximately 2362×1254 showed the production drawer collapse and the Research controls remain arranged without overlap. The capture helper subsequently returned only a tiny icon in the restored window. The user completed substantial resizing and explicitly confirmed Research remained usable and scrollable without overlap/clipping. |
| VSync | Source remains `vSyncCount = 1`, `targetFrameRate = -1`. The user explicitly confirmed substantially reduced GPU power/coil whine. This is a physical user observation, not an instrumented power measurement. |

The user answered `All these checks passed` to the explicit follow-up covering substantial resizing, music/SFX playback and transitions, independent controls, save/load audio continuity and reduced GPU power/coil whine. Manual acceptance passed for the exercised scope, with Engineering available/current/retarget transitions left to the green automated tests rather than manually grinding progression. The four acceptance/test-infrastructure changes are ready for commit and exact-SHA CI confirmation.

The player log contains URP postprocessing shader support/stripping warnings. Combat phase Foldout summaries can truncate at the right panel edge; the detailed report remains accessible. These observations are recorded without changing gameplay or claiming a warning-free player.

No merge to main occurred. The existing local main ref was `97d8d7b8c8609cc2a2dfa0f98271bf03a14b8932` before and after this run, differing from the handoff's stated `a0f750681438d26ded17fff38cb3df37c3da9eb0`. It was left unchanged.
