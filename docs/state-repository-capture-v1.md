# Complete campaign capture and restore boundary

22 November 2026. Authorized host-adapter implementation candidate on the tested player source `2fc0dbca46073121379dd9491c859a02a6f22dcd`. Read the [execution contract](https://github.com/AndreBal1983/HobbyDS9/blob/6844d0a9b46e10c336b2f68d08e4d0ef6eba2cdc/docs/design/state-repository-implementation-contract.md). Source, repository/provider and native acceptance remain separate. No default storage activation, gameplay upgrade, migration or multiplayer is provided by this boundary.

## Ownership and compatibility

The new `TacticalReviewClient` partial host and neutral types depend only on existing Application/Contracts and the .NET Standard2.1 reference surface. They acquire no persistence/provider, SQL, filesystem or Unity dependency. An opt-in composition adapter maps these fields to Data's provider-free `CheckpointImage`/`ICheckpointValidator`; storage transactions do not receive a mutable session.

The typed Cycle adapter accepts only its exact copied package and the existing strict codec/loader. Outer Cycle14 and peers17/18 retain their actual carrier types and complete histories. Existing peers15/16 follow the same exact loader; their acceptance must be recorded separately. **Foundational `SaveEnvelopeDto`14 is a distinct carrier and is unsupported by this adapter.** A number14 alone is not a dispatch key. No source is promoted to another gameplay edition.

## Exact image and descriptor

`CycleCampaignRepositoryAdapter.Validate` clones bounded input, strictly decodes it, loads through the matching complete Cycle loader and compares the entire decoded carrier's codec output with a complete reexport. It returns the exact original bytes, including harmless JSON whitespace, rather than silently normalizing imported bytes. Missing/duplicate/unknown/tampered data remains subject to the existing strict decoder and complete load/replay checks. Maximum image size remains64MiB.

`CampaignRepositoryImage` privately owns bytes and returns copies, exact SHA256, length, authoritative turn/revision/phase and an immutable structured edition descriptor. Its identity is `campaign-repository-edition-v1|` followed by fixed-order key/value pairs, each encoded as invariant decimal UTF16 code-unit length, colon, exact string. No separator can make two field lists ambiguous. The descriptor is bounded to16384characters; this is a host metadata encoding, not a new game-state canonical hash.

Fields include carrier/root/profile; outer package format/profile/hash; colony/template/support/frozen inventory; playable/integrated/physical/review carrier versions/profiles/hashes; inner save format/scenario/content/rules; resolver/version; RNG algorithm/version; map/generation; refinement/belt policy and codec version. Values are derived from validated authoritative content. The receiving repository must compare **all** indexed turn/revision/phase/descriptor/length/hash fields with the validated image. Indexed metadata alone never authorizes a load.

The trusted codec contract must use the actual strict edition wire decoder. The existing Newtonsoft Cycle implementation can be bound through `DelegatedCampaignRepositoryCodec`; a permissive DTO serializer that fills omitted members is not equivalent.

## Session and work ownership

`PrepareRepositoryCheckpoint` checks the host's existing exclusive interaction owners and exact observed frame, then captures one complete detached root under the host gate, after outer adoption. Its one-use worker owns only detached data and a copied package. It never polls a mutable live host while encoding/replaying. Cancellation releases the one-use closure; a valid older image can still belong to its original branch when current gameplay has advanced.

`CampaignRepositoryStamp.EpochIdentity` combines the private host identity and its existing load generation. New/Open creates another host identity; every existing load changes the generation. It is not fabricated from turn/revision and never consumes game RNG/IDs. State matching also includes phase and actor-scoped combat encounter/round/boundary identities. The current Cycle host's authoritative revisions and full root cover its private committed battle changes. This stamp is not a universal future foundational-buffered-combat or network protocol contract.

`IsRepositorySessionCurrent` fences late receipts across New/Load, while `IsRepositoryStateCurrent` additionally identifies unsaved progression within the same session. A durable receipt for an older state in the same epoch can describe that older durable point; it must not claim the current revision is saved. Storage operation IDs, expected heads/reference generations and receipt resolution remain Data/coordinator responsibilities.

`PrepareRepositoryRestore` owns a bounded payload copy. Worker validation constructs a detached replacement session without publishing it. `CompleteRepositoryRestore` accepts the exact one-use result only if its source epoch/state/battle boundaries remain current and no outer interaction owns the host. It prepares lawful replacement projections before swapping authority and increasing the epoch. Failure/staleness/cancellation/abandonment leave the live session unchanged. The caller must integrate the returned frame with its controller/UI; this optional host seam does not silently replace the current model or save-and-quit flow.

## Gates and limits

Focused evidence is recorded in the Backend phase receipt. Representative progressed refinement/belt and combat continuation, deterministic next effects, stale/foreign/cancelled work, exact-byte ownership and malformed carrier tests are required before full B2 acceptance. A compile or metadata-only fixture is not native acceptance.

The complete export and replay costs remain. This seam alone is **not** a bounded autosave coordinator: C2 must enforce one in-flight image plus one latest stamped autosave intent, admit manual requests separately, protect epoch/head/reference generations, resolve unknown writes and implement the approved warning/failure policy. Native A2/D1 and the separate D2 default rollout gate remain open until independently received.

## Opt-in save queue

The separate CampaignRepositoryHost library uses the provider-free Persistence API. It does not change the default TacticalReviewClient references or the JSON save route. It takes the current host frame, a confirmed branch and the strict Cycle codec. Call ObserveFrame and enqueue save intents on the owner thread. Call PrepareNext on that same thread, execute the returned one-use worker off-thread, then complete that exact worker on the owner thread. Autosave intents retain only the newest complete boundary and carry forward a due milestone. A waiting manual request whose frame has advanced requires fresh confirmation.

Use PreparePayloadRead for selected-row validation or Load. It reserves the same complete-image permit as a save, freezes the source frame before I/O and validates the indexed metadata against the actual decoded root. Finish validation with CompletePayloadValidation, or use CompletePayloadRestore to adopt the restored frame. Busy adoption keeps the permit until the outer interaction drains. Cancellation of that Load prevents later adoption. After a successful Load, bind the selected durable branch, or fork the earlier checkpoint, before resuming writes. Pass the resulting load proof to ConfirmRestoredCheckpoint to mark that actual adopted frame durable.

A known receipt and the current branch head are separate facts. Unknown writes retain their immutable request and operation ID for resolution. A known committed receipt survives a failed branch read. New writes wait for a confirmed head. Use Snapshot for last durable progress and warning state, and CanQuitAfterManualSave for Save-and-Quit. Maintenance is an independent worker and reports retention and budget outcomes separately from manual save completion. Pace follow-up reads after repeated storage outages so a blocked store does not repeatedly occupy the worker every frame.
