# Overnight deliverables integration

14 October 2026. The owner authorized background committing/consolidation while manually testing the existing native build. Branch `codex/overnight-deliverables-integration` combines delivered histories on accepted main `53d2e64973d9081a4d0090e771f5e777fc55cf40`. It does not activate a default campaign, approve proposed mechanics, or replace the owner's native test artifact.

## Preserved sources

- Native candidate [PR #33](https://github.com/AndreBal1983/HobbyDS9/pull/33), `bae723f182dbff07bcfbf83a47716c7143eed6b8`, already contains the equipment/lifecycle/travel/client/storage/import/binding/modal implementation histories from #19/#20/#22–#32.
- [Prototype CI #21](https://github.com/AndreBal1983/HobbyDS9/pull/21), `b1b10f8638d8f5ca2635885c917f7c96ce0819a4`: preserves its history and dedicated workflow. Existing mock test sources were already present through the real-client stack. Two add/add documentation conflicts were resolved to the delivered #21 wording explaining its dedicated CI, without changing tests or production code.
- PM design/handover branch through `cfa425a`: scoped agreed/proposed decisions, prompts and chronological evidence, including the owner's later native authorization and completed cutoff.
- `5966ddd6bf624f944bc716dceb092da3c4184ffb`: [activation packet](campaign-tactical-v2-activation-decisions.md) and [readiness audit](campaign-tactical-v2-unity-readiness.md).
- `cec1475b23cc5eb661c6a65d7f38eb153e1257ef`: [independent audit](unity-physical-review-independent-audit.md), actual split-assembly compiler harness and [coverage report](unity-review-split-compiler.md).
- `87e652967b1539c4f322306acaabd70b933800e4`: [runtime warning follow-up](unity-review-runtime-followup.md), documentation only.

Ordinary merge commits preserve history; no source branch is rewritten or deleted. Historical reports retain their dated scope and original authorization, rather than being rewritten to imply that every native check or policy was accepted at that earlier checkpoint. The [morning handover](overnight-handover-2026-10-14.md) supplies the subsequent native results and remaining gates. Original stacked PRs remain open drafts until an explicit final integration disposition; no PR is silently marked complete by this consolidation.

## Verification of the combined tree

The final tree's production `src`, scenario content, Unity `client`, existing test sources, and real client/storage/shell code match native candidate `bae723f` exactly. Differences in those areas are documentation only. Added executable code is confined to the optional reference compiler harness/PE metadata probe; the added workflow explicitly runs the preserved prototype cases.

Executed on the combined checkout: default and review portable preparation; default Unity static regression; review PE/dependency/resource-byte checks; binding/source checks; **7 binding adversarial and 13 import adversarial cases**, all passed. The split compiler compiled **eight actual separate Player/Editor/test assembly outputs**, including real default Bootstrap and review Launch, and verified metadata. Existing Unity deprecation warnings remain visible. This compiler check executes no Unity process, test runner or compiled Unity assembly.

An initial import-byte check overlapped a reference compiler rebuild and observed stale copied DLLs. After the rebuild finished, sequential preparation and the complete import/binding checks passed. No source fix was required. Generated exact-byte review resources are local preparation artifacts; no resource/content change is committed.

Fresh exact-head GitHub CI is tracked in the integration PR. Earlier native evidence is **121/121 actual Unity EditMode cases, successful Windows IL2CPP and bounded two-battle/restart/input/layout checks on bae723f**. No Editor/player was rerun during this background consolidation, and the owner is free to continue testing that preserved artifact. Unchanged full gameplay suites were not redundantly rerun locally; the combined PR's portable CI performs the final remote regression.

## Remaining disposition

Manual review feedback, keyboard navigation diagnosis, ICU/post-processing follow-up and four proposed policy choices remain open. General mixed-hostility sides and newly constructed v2 source provisioning remain gated. Default activation stays disabled. Merge to main, closing/rebasing original stacked PRs and enabling gameplay are distinct actions; this branch makes one coherent candidate available for review first.
