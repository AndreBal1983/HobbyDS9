# Portable frontier host/controller seam

19 October 2026. Bounded headless client increment from main `187929ee`, using the independently reviewed [frontier backend](frontier-reconnaissance-v1.md). No gameplay, package/save schema, Unity/frontend, JSON codec, disk store or default-demo activation change. Frontier policy remains provisional and explicitly opt-in.

## Binding contract

- `IFrontierReviewHostCodec : IIntegratedReviewHostCodec` adds typed `CopyPackage(CampaignFrontierPackageDto)` and `CopySave(CampaignFrontierSaveDto)` overloads. Lower-profile copies are not valid complete frontier saves.
- `IFrontierReviewCheckpointStore.Write(slot, CampaignFrontierSaveDto privateRoot, overwrite)` / `Read(slot)` carry only complete wrapper5, including transcript, actor discoveries and integrated3/physical2/core state. These are trusted host persistence carriers, never renderer or AI inputs.
- `PhysicalReviewHost.StartFrontier(package, codec, store = null)` uses actual `CampaignFrontierApplication`. `OpenFrontierFromSlot(package, codec, store, slot, actor, encounter = null)` returns the existing `PhysicalHostOpenResult` convention.
- `ReviewFrame.Frontier` is nullable immutable `CampaignDiscoveryOverview`: only the selected controlled actor's known broad node identities/kinds, lawful lanes and discovery receipts. It contains no transcript, full world proof, private save or other actor's observations. Null means a non-frontier host. Existing actor-filtered `Strategy.Navigation` and `Travel` remain available.
- Opaque SaveCommitted/LoadCommitted, slot save/load and controller Reopen dispatch frontier5 before demo4, integrated3 or physical2. No nested-root export fallback. Handles remain opaque and host-bound.

Before publishing an opaque or slot save, the host passes the codec's copied wrapper5 back through the backend loader. A copier that silently drops the transcript or other required authority fails before overwriting a valid checkpoint. This uses existing bounded backend replay validation; the host adds no gameplay or alternate history validator. The concrete future store remains responsible for atomic filesystem replacement.

Use the existing `ReviewController` travel/battle work and `ExecutePending`/`Receive`, observed host strategic commands and `AdvancePlanning`. Physical partial edges, arrival charting at next Planning and pending combat/atomic aftermath remain backend rules. Preview/read/refresh has no gameplay effect.

Failed copy/read/validation/write must preserve current host/session/generation and unsent controller drafts. Successful replacement publishes candidate and lawful frame together, increments generation and rejects obsolete offers/work. Reopen creates a different host identity: a caller must replace cached host/controller together through `OpenFrontierFromSlot`, or use same-host load; after controller-only Reopen dispatch pending work through its current host via `ExecutePending`. Existing physical/integrated/demo behavior remains unchanged.

## Acceptance status

Starting baseline: locked restore, zero-warning/error portable build and **63/63 existing physical/demo host cases passed**. Local implementation verification passed all **26 new frontier cases**, and the preserved 63 also passed after the host/frame dispatch change (88-case run before the final copier-truncation regression). Preserved real client **51/51**, durable storage **47/47** and mock presenter **115/115** passed. Final physical-host case inventory is **89 = 63 retained + 26 frontier**; both CI exact-count guards were updated accordingly, retaining total/passed equality and zero failures/skips.

Executed frontier coverage: actual branching arrivals and raw unknown routes; partial movement; complete5 opaque/slot/reopen copies; rejected copier truncation before checkpoint overwrite; read/copy/validation/write failures retaining frame/generation/travel/tactical drafts; stale/foreign frame/offer/controller work and old receipts; reopened-host dispatch; controlled-actor privacy and detached views; pending human/AI commitments; explicit once-only aftermath and discovery publication. Independent persistence/integration review and exact-head CI remain delivery gates. No unrelated full Simulation rerun, Editor/native/GPU execution or frontend asset/source changes were performed.

Later Unity integration requires its own explicitly typed wrapper5 JSON codec/store/namespace/resources and separate authorization. This increment supplies none of those callsites and does not switch the current native demo package.
