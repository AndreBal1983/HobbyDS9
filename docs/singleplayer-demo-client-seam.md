# Portable demo host/controller seam

14 October 2026. Implementation candidate on ordinary merge of frontend13f633734b787397ee7559d175f3cf7ffbba1450 and backend7dd7bb0c22c4ec321a38af4fad01d3ed787c19c7. This work owns only portable host/frame/controller/tests. Secondary owns Unity loader/parser/codec/atomic disk store/resources/UI; no Editor/native/GPU work here.

Concrete API:

- `ISingleplayerDemoHostCodec : IIntegratedReviewHostCodec` adds typed overloads `CopyPackage(CampaignSingleplayerDemoPackageDto)` and `CopySave(CampaignSingleplayerDemoSaveDto)`.
- `ISingleplayerDemoCheckpointStore.Write(string slot, CampaignSingleplayerDemoSaveDto privateRoot, bool overwrite)` / `Read(string slot)` accepts only complete wrapper4. It is a trusted persistence seam, never a view.
- `PhysicalReviewHost.StartDemo(package, codec, store = null)` creates the real single-human session; `OpenDemoFromSlot(package, codec, store, slot, actor, encounter = null)` returns existing `PhysicalHostOpenResult`.
- Existing host/controller SaveCommitted, LoadCommitted, slot Save/Load and Reopen dispatch demo4 first. No inherited physical/integrated export fallback. In-memory handles remain opaque and bound to their host.
- `ReviewFrame.Demo` is nullable immutable `CampaignDemoSnapshot`; null means actual legacy/review/integrated host. Nonnull provides human/objective/deadline/phase/outcome/text only. No AI plan/root/commitments or private enemy state enters the frame.

Use the existing same-session ReviewController for tactical/travel work, host stamped strategic submission for paid research/production and explicit AdvancePlanning. Actor must be the demo's HumanActorId; backend rejects AI switching/commands. Human CommitRound resolves one joint round against existing AI choices; FinalizeBatch remains explicit. Terminal frame outcomes should disable plan/advance UI; backend rejects terminal mutation authoritatively.

Failure reads/copies/validation/write leave the current host/session/generation intact; controller drafts remain unless a replacement succeeds with explicit discard. Successful replacement publishes candidate+frame atomically, increments generation and invalidates obsolete work/offers/callbacks. Reopen creates a new host identity. Preserve existing physical/integrated store/codec interfaces and slots. Unity must add a separate full4 codec/store/namespace/launch path; do not pass nested3 or alias old saves.

Focused actual controller/store acceptance and final source pin follow; this early contract is not a native demo/readiness claim. [Backend contract](singleplayer-demo-contract.md) and [acceptance](singleplayer-demo-backend-acceptance.md) govern authority/pacing/profile identity.
