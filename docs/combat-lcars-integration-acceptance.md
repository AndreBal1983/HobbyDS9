# Combat v2 and LCARS integration acceptance

Executed 13 October 2026 for [Issue #14](https://github.com/AndreBal1983/HobbyDS9/issues/14), delivered through [integration PR #15](https://github.com/AndreBal1983/HobbyDS9/pull/15).

## History and scope

Fresh isolated worktree/branch `codex/combat-lcars-integration-2026-10` started at remote main `5beba2d652d283a2efb2f3ef44ddd5df9e38815d`. Two ordinary no-fast-forward merges produced implementation commit `2cf33e3d9c34d29f6284dc452f5b568150c2f0ff`. Its 38 changed files are byte-for-byte the union of both final branch diffs; no conflict repair or gameplay/visual redesign was necessary. This acceptance record is the only additional tracked file. All five source heads are reachable:

| PR | Preserved head |
| --- | --- |
| #9 | `53c81b4eaebf9cd304f04d7a58d7348db0480dcb` |
| #11 | `7ad7b89d2efa769be536d8a0f3c04ee05ff050b6` |
| #10 | `967f63723000b3391e2cc61823eb35594b905d52` |
| #12 | `6b899c6faaf59432a8b34a6bd3f430db4b648d5a` |
| #13 | `ab110b6e6d41b5ba7a2d490d68185eedda13de48` |

Existing worktrees and branches were left intact. Unity-generated settings were backed up locally and restored to the committed settings; no generated settings, binaries or captures are committed. Campaign phased combat remains the default, tactical v2 remains standalone, and Unity blueprint publication remains disabled.

## Executed automated gates

| Gate | Result |
| --- | --- |
| Locked .NET restore and full Release build | Passed; zero warnings/errors |
| Complete .NET suite | 1,307 passed; zero failed/skipped |
| Campaign 25-turn canonical/reverse/repeated | Exact matching save hash and RNG; 17 resolved battles, 26 partial-traversal turns |
| Tactical v2 reference and persistence/ordering tests | Passed in the complete suite; original v2 golden unchanged |
| Actual console subprocess acceptance | Four authored playthroughs (three scenarios), repeated/reversed transcript, save/resume boundaries 1 and 2 passed |
| Published self-contained Windows console executable | Same four playthroughs and both saved boundaries passed using the executable directly |
| PrepareUnity and static assembly/asset checks | Passed |
| Unity 6000.6.5f1 EditMode | 107 passed; zero failed/skipped |
| Windows x64 IL2CPP via PrepareUnity Windows target | Passed |
| Combined implementation CI | Windows/Linux/macOS passed on exact `2cf33e3d9c34d29f6284dc452f5b568150c2f0ff` |

Combined implementation [PR CI 38381459582](https://github.com/AndreBal1983/HobbyDS9/actions/runs/38381459582) and [push CI 38381423951](https://github.com/AndreBal1983/HobbyDS9/actions/runs/38381423951) succeeded. Each platform's logs were checked for all 1,307 tests, zero Release warnings/errors, three identical legacy hashes/RNG and the console/checkpoint smoke. Final documentation-head and post-merge main CI are recorded in PR #15 and the delivery report, and must be green before declaring integration complete.

## Combined native and visual acceptance

Fresh desktop permission was obtained for this integration. Tested the newly built combined IL2CPP player, not an earlier leaf demo. At 1280x720: Escort Combat Requested/Allocated direct segment input, zero, scrub from zero to 8/16, Home/End and one-unit Up, backend minimum and total-budget errors, scrolling to the delayed exact editor, invalid allocation 100, independent Requested zero, Cruise/Survey/Combat retention, and Back/Keep. At 3440x1440 fullscreen: retained edited draft and exact 100, fixed continuous shell, Weapons navigation, quantity 2→3→2, visible capacity/mount overflow, Remove to zero and Add to one. At 1920x1080 windowed: all thirteen system pages, Reset/Keep and Reset/Discard, section-navigation/Keep and section-navigation/Discard. Discard restored the template and navigation completed; no campaign command was sent by designer edits.

Strategic map, Research (70/30 and six-way Science), production catalogue and recent-combat-report access opened correctly. End Turn advanced the new disposable campaign to turn 2/revision 5. The fresh campaign had no populated battle report; populated combat/report behavior, movement, production, blueprint revisions, save/reload and deterministic checkpoints were exercised by the complete automated suite. This smoke does not claim campaign-connected designer publication or tactical-v2 campaign combat.

The existing Editor capture entry produced **36 real rendered Game-view PNGs**, twelve fixture states each at **1280x720, 1920x1080 and 3440x1440**. All dimensions were verified. Reviewed the complete set in four contact sheets and full-size representative paired/invalid views, alongside native observations. Shared baselines/proportional stack heights, zero states, retained exact values, textual errors, fixed frame and primary controls remained usable; compact details require scrolling. Command maximum 4 has eight bars, Launcher maximum 8 has sixteen, on the same scale. No overlap preventing input was found.

Evidence is retained in the isolated worktree's ignored `artifacts/unity/` logs and in `E:\HobbyDS9\artifacts\combat-lcars-integration-2026-10-native\`: complete Windows/console packages, native player log and `visual-captures/`. Initial package import/build attempts failed because C: was full; only this integration's generated caches/output were relocated to E:. After the owner expanded C:, the desktop helper recovered and native acceptance completed. Hidden Editor capture attempts did not produce frames; the visible Editor run completed all 36 captures. These environmental retries are not substituted for successful gates.

No new application exceptions or Unity script compilation errors were found in successful test/build/capture/player logs. Existing audio API deprecations, the native ICU minimal-segmentation warning, and transient licensing-token refresh diagnostics remain. Physical touch, controllers, screen readers, every DPI setting and subjective audio/GPU measurement were not newly certified in this integration.

## Unchanged compatibility and determinism

- Campaign Save **14**, Content **10**, rules **`science-engineering-v1`**, compiler **`ship-design-compiler-v1`**; no migration or golden rebaselining.
- Campaign resolver **`combat-resolver.phased` / 1**, RNG **`splitmix64-rejection-v1`**.
- Legacy save SHA256 **`fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`**, gameplay RNG **`d8919406d6e01cf1`**.
- Scenario SHA256 **`3b80eb970c5d832550fc947344e860777910d80208af0fc2f474a98ae04e0b59`**.
- Separate tactical-v2 reference SHA256 **`ccae7836c085936ed962b9ba001764ae4252f135fdf62fa1fbe2c0ea0bcef07c`**; standalone host save/fixture format 1 remains separate.

The underlying milestone documents retain their historical acceptance status. This document supplies the combined-build acceptance that was missing from those earlier PRs. Campaign adapters, tactical AI, designer publication, refits and broader roadmap work remain deferred.
