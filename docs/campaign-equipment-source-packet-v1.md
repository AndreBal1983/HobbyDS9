# Campaign equipment source packet 1

24 October 2026. Source/CPU candidate from protected `767bc966`. This packet factors existing authored/static equipment reads and adds detached consistency checks for a possible future published-built source. **No live profile, construction admission, save field, Application/host/codec/UI/content or balance change.** Existing V1/V2 `UnsupportedSource` behavior remains, including for a successfully published, standalone-compiler-supported class.

## Existing source consumers

`CampaignEquipmentSourceResolver.ResolveAuthored` resolves exactly one initial or retained static-built source, its exact fit revision, explicit authored cloak policy and truthful design/owner. Unknown, duplicate or initial-vs-built ambiguous sources reject. Missing/ambiguous authored cloak metadata still rejects; there is no implicit no-cloak fallback for authored equipment.

The detached internal source returns copied fit/provision/initial stocks and an independently copied initial pool. Fit, pool validation, condition names, own-ship projection, encounter cloak, generated contact cloak and playback identity now share this source path. `ResolveProvision`, `KnownSource`, construction templates/admission and all serialized roots retain their old contracts. No cache or new source registry is installed in the session.

## Detached published-built checks

`CampaignPublishedBuiltShipDto` is a NEW standalone DTO, referenced by no historical save DTO. It retains ship, queue, exact blueprint revision, owner, construction fleet/turn and exact source/support/compiler/fit fingerprints. `PublishedBuiltShipValidation` is internal and uncalled by live sessions.

`ValidateRetained` resolves the exact immutable publication from the supplied state, validates its frozen catalogue and recompiles it through the existing `CampaignBlueprints.FrozenAccess` archival convention. FrozenAccess verifies recorded source facts; it is **not current Engineering availability or permission**. It compares recorded compiled outputs/cost, invokes the accepted support compiler against this frozen source, and verifies every provenance identity. It accepts retained destroyed-ship records while checking any surviving ship's exact blueprint/owner/hull. Duplicate ship/queue identities reject. The 4,096-record detached batch bound is a technical helper limit, not a gameplay fleet/ship cap or new-profile admission.

`ValidateCompletion` additionally takes explicit actual before/after production states. It requires the named pre-production owned queue head, exact blueprint, expected paid credits/resources/Industry, unchanged source record, removed completed queue and new allocated ship in the recorded construction fleet/turn. It returns a deeply detached source plus copied provenance; it changes neither state.

These checks do NOT prove historical payment from a hash or fabricated DTO. The future trusted profile must call shared stamped enqueue/current-access validation, supply the actual staged ProductionPhase association, validate the entire candidate transaction and compare complete retained history through operation replay (including completeness/cross-source uniqueness). Retained validation alone cannot authenticate an arbitrary historical ledger. No current-access recheck is added at completion, and no public source token authorizes construction.

Published derivation uses the accepted explicit no-cloak policy, zero shields/recharge, empty ammo and only mapped equipment. It never invents an authored SourceProvisionShipId, static design ID or cruiser capabilities. Display name comes from the exact publication; playback identifies the actual blueprint rather than impersonating a known static art identity. All support numerics remain explicitly test-only. No live ratings, metadata package, schema/profile identity or content admission is adopted.

## Acceptance

New tests exercise authored/static exact facts and copy isolation; unknown/ambiguous/missing-cloak rejection; real ordinary paid queue and ProductionPhase association with detached tactical derivation; forged receipt/source/cost/power/owner/support/identity rejection; retained destroyed records without current access; no-cloak/zero stocks; copy isolation and reordered metadata; and old generated/integrated admission rejection.

Before rebuilding the Debug simulation, an external probe captured the pre-change assemblies and produced deterministic integrated initial/built/observed and generated V1/V2 initial JSON. Five SHA256 constants in the new tests match those bytes exactly after the refactor. Existing integrated production/combat/load/rollback/privacy, authored equipment, tactical review, standalone support and selected generated reorder/rollback regressions are run separately. This is CPU acceptance; no new integrated published-construction player loop or Unity/IL2CPP/native execution is claimed.

The next policy/content/atomic construction packet requires independent review and fresh authorization. Protected art/designer packages and owner saves remain untouched.