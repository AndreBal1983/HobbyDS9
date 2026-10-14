# Tactical v2 playtest merge acceptance

14 October 2026. The owner requested automated testing followed by a full merge so playtesting can proceed. This authorizes merging the explicit gated review stack after acceptance, not approving its proposed balance/timing policies or activating tactical v2 in default campaigns.

## Corrected review finding

The client formerly required every non-null enemy focus to be currently visible, including an already-standing focus. After that fleet's destruction in a multi-fleet encounter, the backend deliberately retains the focus and falls back to another lawful enemy. The client therefore blocked independent maneuver edits. `ReviewController.EditFleet` now allows the existing standing focus while continuing to reject arbitrary hidden/unknown/own targets.

The real-package regression `DestroyedFocusStaysAsIntentWhileTypedReportExplainsLawfulFallback` now changes Hold to Screen in round 2 after the focused opponent dies. It verifies invalid focus rejection, accepted Screen, retained focus, and backend fallback to the surviving opponent. The strengthened case **failed before the fix**, then the complete **51-case real-client suite passed** after it. No backend rule, content, save format or activation gate changed. Red/green TRX files are in ignored `artifacts/retained-focus-regression`.

## Final acceptance sequence

1. Commit/push the exact candidate, run all eight PR workflows on Linux/Windows/macOS, verify every job and preserved backend/determinism gates.
2. In this isolated PM checkout, prepare default/review portable assets, run actual pinned Unity6000.6.5f1 EditMode tests in batch mode and build a separate Windows x64 IL2CPP review player. Record XML, logs, source pin and automatic import/settings differences. No mouse/keyboard control or overwrite of the owner's earlier native build is required.
3. Recheck the pinned PR and current main, mark the draft ready, then merge with an expected-head check and ordinary history preservation. Preserve source branches and local user work.
4. Verify the resulting main revision and its portable CI. Supply a launcher/build receipt for the new playtest artifact. Do not silently replace an already-running player or ordinary campaign saves.

Final run IDs, exact SHAs/counts, failures if any, build hashes and merge outcome belong in the PR handover and ignored acceptance receipt. This source document records the code correction and required sequence; it is not itself evidence that pending checks ran or a merge occurred.

## Remaining manual and design limits

The build is the real opt-in review harness with diagnostic text and trusted actor switching, not a finished tactical game UI. Keyboard navigation, physical audio, full graphics/post-processing and international text remain separate manual/native acceptance gaps unless new evidence establishes otherwise. Existing ICU/URP warnings must be reported. The previous limited native two-battle/layout result applies to its old exact pin; the client fix requires a fresh automated Editor/build gate before merge.

Travel-boundary timing, mobility formulas, cloak power and enemy-condition precision remain proposed. Mixed-hostility sides and newly constructed v2 source provisioning remain gated. Ordinary v1 campaigns and save compatibility must retain their existing path. No default activation or speculative game mechanics are part of this merge.
