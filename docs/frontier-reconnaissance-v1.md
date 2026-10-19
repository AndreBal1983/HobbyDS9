# Frontier reconnaissance v1 — provisional opt-in contract

Implementation candidate based on main `c2621f82`, with the later documentation-only main `4de8d4f` merged normally. **Proposed demo policy; default activation disabled.** This is ordinary arrival-based charting, not planetary survey, equipped sensing, colonisation or a complete 4X game. Current twelve-turn Bajor demo remains separate.

## API and compatibility

`CampaignFrontierApplication.StartPackage` / `LoadSave` return `ICampaignFrontierSession`, extending the existing integrated strategy/travel/combat boundary. `CaptureDiscovery(actor)` returns detached immutable known node IDs/names/kinds, public lane endpoints/costs and that actor's discovery receipts. It exposes no system contents, ownership, governments, deposits, hazards, other actors' discoveries, future orders or private proof. Existing stamped travel submissions and shared AI validation remain authoritative; no scan command is introduced.

Complete host persistence uses **wrapper 5**, profile `campaign-tactical-v2-frontier-review-1`, policy `arrival-public-lanes-provisional-v1`. It nests integrated3/physical2/lifecycle1/campaign14/content11. Rules `science-engineering-v1`, compiler `ship-design-compiler-v1` and combat resolver identities remain unchanged. The nested lifecycle save carries the explicit `campaign-tactical-v2-frontier-owned-core-1` profile so older lifecycle/physical/integrated loaders cannot accept a truncated frontier checkpoint. Nested export APIs fail closed. No save migration or old-format reinterpretation is supplied.

## Provisional charting policy

The package explicitly lists ordinary public lanes. The bounded fixture has no hazards or hidden system contents in its navigation facts. Completing a physical arrival charts the adjacent public lanes and their endpoint identities for the arriving fleet's owner. It never promotes body/site knowledge or marks a node Surveyed. A route preview, starting/stationary presence, partial edge or unvisited route suffix reveals nothing. An actual arrived prefix still counts if a subsequent encounter stops the route. Publication occurs only on the next Planning boundary, together with successful strategic advance or atomic battle aftermath.

Each actor's first actual arrival at a node yields one receipt, including an empty delta at an already charted dead end. Receipt count is bounded by actors × nodes; repeat visits give no duplicate. Attribution uses canonical fleet/route-prefix order within a turn; it does not claim to be a sensor timestamp. Failed turns/finalization publish neither knowledge nor receipts. Provisional lane reach and timing are isolated policy choices, not approved general sensor or exploration balance.

The opt-in AI chooses reachable unvisited IDs from its own known-node list and submits through the same stamped travel validator as humans. It preserves existing routes, locked partial edges and StayEngage. It does not inspect hidden ownership or neighbors and does not run the legacy ownership-scoring AI, add procurement goals or change the accepted demo AI.

## Save provenance and bounded replay

The complete save retains accepted travel, strategic, cloak, round, AdvanceTurn and FinalizeBatch operations, with original IDs, stamps, payloads and order. Rejections do not append. Load starts the exact package, replays through real backend APIs, then compares the complete nested checkpoint, receipts and transcript. Historical deterministic AI execution is replay validation, not new decisions at the loaded boundary; the complete phase, commitments, RNG, stocks and state must match. Historical world snapshots are **not** accepted as discovery evidence. The only physical world proof retained is the existing nested physical2 current-turn proof.

This establishes a **legally replayable history consistent with the complete checkpoint**, not cryptographic authenticity of an offline user's past. An entirely different legal history with its exact resulting checkpoint is valid. An invented old arrival plus coordinated receipts/knowledge, omitted/reordered operations, or a matching receipt with mismatched current fleets/turn/stocks is rejected. No signing, secrets or second mutable world store is introduced.

This review profile has a technical limit of **4,096 accepted operations**. The 4,097th mutation rejects with Overflow before any state, ledger, RNG, stock or receipt change; the complete 4,096-operation save remains loadable. This bounds replay cost, is not a fleet limit, and does not apply to any other profile. It is a review-scenario limitation, not a general campaign retention policy.

## Acceptance

The actual [branching fixture](../scenarios/frontier-reconnaissance-v1/README.md) and Application walkthrough run to Planning turn 8, with 9 first-arrival receipts across actors and 13 accepted operations. Canonical, reversed authored collections, repeated execution and reload at each turn produce the exact same complete save. The first weighted lane costs 8 against the unchanged compiled cruiser budget 6, proving partial movement before arrival. The two subsequent known branches are actual route choices.

Focused tests cover actor isolation; unknown-route rejection; stationary/partial/combat-stopped travel; next-Planning publication; stamped rejection and actual shared AI legality; strategic payload ownership; historical forgery and coordinated checkpoint tampering; omitted/reordered operations; 4,096/4,097 boundaries; nested-loader rejection; repeat visits; all-phase and post-discovery rollback; pending human/AI commitments and atomic aftermath reload. Existing golden paths are preserved. Exact test/CI evidence is recorded with delivery; no Unity/native acceptance is claimed.

New LF-byte-pinned package: `8af5a37850685afd665131bec6cd12eb575cfc883b5f745b9507263790fcbb18`. New serialized final save: `332817bbf6383fc464e04445b49bc020be77a0abd3f8d71048ca0b295e99a588`. These are independent vectors, not replacements for previous campaign/demo goldens.

Local verification, 19 October 2026: locked restore; zero-warning/error full Release build; **1,822/1,822 .NET tests** (1,777 retained + 45 frontier); actual CLI canonical/reordered/repeated/reload comparison; `PrepareUnity.proj -t:Prepare` with `UnityProject` redirected to an artifacts-only preparation directory. No Unity source files, Editor, native player, desktop or GPU operation was used. Full standard Unity static integration and three-platform checks remain CI evidence to confirm on the delivered exact SHA; independent review is required before merge.
