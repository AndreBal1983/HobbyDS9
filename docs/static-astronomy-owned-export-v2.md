# Static astronomy v2 — owned slot-export validation

24 October 2026. This bounded change removes one redundant JSON copy during v2 **slot-save self-validation**. It does not change the authoritative loader, replay, frozen geometry, save/content versions, gameplay, RNG or AI. It does not address the dominant repeated Foundation-validation cost measured in late campaign replay.

## Optional capability and restricted call site

`IStaticAstronomyOwnedExportValidator.ValidateOwnedStaticAstronomyExport(template, ownedRoot)` is an optional capability, separate from the required codec interface. Its sole production implementation is the sealed `ReviewNewtonsoftCodec`. That implementation invokes the existing complete `CampaignStaticAstronomyApplication.LoadSave` and discards the detached candidate.

`PhysicalReviewHost.ExportGenerated` defaults to the original copy/load path. Only `SaveGeneratedToSlot` enables the owned-export option. Under the existing host gate, the host still exports a detached authoritative snapshot and performs its first full closed-codec `CopyStaticAstronomySave`. If the v2 codec advertises the optional capability, it then validates that private snapshot directly rather than invoking a helper that would serialize/copy it a second time. The slot writer still encodes, checks size and performs its atomic write. Any export/copy/validation failure still returns `SerializationFailed` before a store write.

In-memory save handles, v1 saves, Continue, Reopen, slot loads and handle loads retain their original copy chains. Codecs without the optional capability retain their existing second-copy calls and Nth-copy fault behavior. Advertising the capability means accepting its complete-validation obligation; it is not a marker allowing the host to skip validation or a trust token for arbitrary caller/file roots.

## Ownership and complete validation

Backend export copies map/settings/geometry/features/groups, physical state/pools/proofs, built ships, names, discoveries and transcript operations. The first sealed-codec round trip admits the closed shape and creates a detached DTO tree with scalar/string leaves. The host-local root is not published to a renderer or slot before self-validation.

The unchanged loader checks template/profile/policy/hash pairing, validates and copies frozen geometry, derives the generated package, and reconstructs the session through every accepted operation. Commands and retained operation payloads are copied; authority does not adopt the checkpoint's mutable state or lists. It checks both nested playable and full generated structural equality, including discoveries. History is reconstructed through successful finalization. A rejected candidate cannot replace the live session, epoch, draft or save-handle sequence. No validation result is cached across saves or files.

## Acceptance boundary

`StaticAstronomyOwnedExportTests` covers capability use only on v2 slot saves; preserved ingress/handle/v1/custom-codec chains; first-copy, real carrier and real size failures; optional-validator failure; corrupted profile/hash/map/transcript; no-write/frame/draft/epoch/handle-sequence atomicity; and mutation of captured exports/ingress/template without changing authority or other handles. Existing adapter fault and closed-codec cases also run.

External, test-owned evidence uses the exact sealed native B69 and fresh1000 saves without committing large new fixtures. Before/after slot saves preserve native bytes and actor-safe battle history. The comparison performs at most one warm save and one measured before/after save per root, using pinned source copies with namespace/timing wrappers. It is .NET CPU evidence with measurement noise, not a native-player performance claim. Unity/frontend integration and actual native receiving require the coordinator's separate gate.
