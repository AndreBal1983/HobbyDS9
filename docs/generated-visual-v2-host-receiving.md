# Generated visual v2 host receiving boundary

24 October 2026. Source increment only; no graphics, engine, build or native acceptance.

Base protected playable checkpoint f52c3c8db4b34d1b9cd0a483c60e4e5c3ad463d0. Portable source received exactly from 4a542554335681a1866b4c80241cc72b687eb640 via its three commits; local receiving commits 93b1831, 8b219cd, 5064f28. Rules and geometry remain portable. No shader receives a saved root, Natural document, full feature tree or private index.

`IStaticAstronomyReviewHostCodec` and `IStaticAstronomyReviewCheckpointStore` are explicit v2 capabilities. Planned public host Start/Open methods invoke only CampaignStaticAstronomyApplication; v1 Start/Open methods invoke only CampaignGeneratedApplication. Internal save/reopen dispatch is fixed by the host's construction profile. No loader fallback, field flip, conversion or seed regeneration.

V2 codec admits only the complete generated-campaign-v2 / generated-neighbor-exploration-static-centres-v2 / generated-campaign-owned-core-v2 combination using the existing closed format-7 shape. Separate directory HobbyDS9GeneratedStaticAstronomyOnly-v2 and file prefix unity-generated-static-astronomy-v2-. Existing v1 directory, prefix and codec methods stay unchanged. A carrier alone is insufficient: authoritative LoadSave must complete before publication.

`GeneratedCampaignPresentation` bundles the current ReviewFrame, detached actor-known geometry and immutable astronomy, validating actor/map hash/turn/revision/observation policy. The host constructs it under the same lock and validates host ID/generation before consumption. A stale/mixed pair must fail closed with no local visual publication. KnownMap returns fresh detached copies; cached geometry cannot be mutated by the renderer. Later rendering uses only this pair, never reacquires unfiltered data.

Candidate-first publication applies to Continue, slot Load and committed checkpoint reopen. Late validation failure retains the old host, frame, drafts and files. V2 slots never reuse v1 slots. CPU receiving will check explicit profile separation, closed/malformed data, failure atomicity, copied views, stale epochs and both-direction direct Continue compatibility. Fixture A Explore32 (three lawful proxies) and fixture B combat (zero proxies) remain separate labelled journeys.

Implementation and focused CPU evidence follow this early typed contract; no current UI changes or graphics claims.
