# Unity Ship Designer — Campaign Blueprint Integration

Status: **proposed milestone; implementation candidate on `codex/unity-designer-campaign-blueprints`, pending combined review and merge**. Local validation and Windows demonstration evidence are recorded in [campaign integration acceptance](unity-campaign-blueprint-integration-acceptance.md). This does not change the agreed/proposed/open labels in the design chapters.

Before this candidate, the accepted Ship Designer UX rebuild edited a local fixture-backed draft and requested portable Application previews. The campaign backend already supports publication and construction through [Blueprint Construction v1](blueprint-construction-v1.md). The candidate connects the live campaign provider, stamped publication and exact-revision construction. Offline designer previews retain disabled publication; live actions require authoritative gates. Earlier UX acceptance documents describe their historical local-only scope.

## Required integration

- Use the campaign-owned design catalogue and current authoritative campaign state through Application projections, rather than fixture availability.
- Reflect Engineering availability for hull and component versions; refresh after advancement, commands and load.
- Present published blueprint revisions and preserve immutable revision identity when selecting, editing or constructing a class.
- Submit `PublishShipBlueprint` with current observed stamps and a complete draft; present backend validation and rejection results without local legality or performance calculations.
- Present campaign construction options for the selected owned colony and exact published revision.
- Submit `EnqueueBlueprintConstruction` through the existing Application command boundary, using current authoritative eligibility, prices and resources.
- Refresh catalogue, publication and construction projections after accepted commands. Handle stale state, pending combat and rejected commands without losing the local draft or claiming success.

## Acceptance boundary

Campaign publication and construction must use shared human/AI Simulation validation. Unity presents detached snapshots and submits commands; it must not invent unlocked versions, compile authoritative performance, calculate authoritative costs or mutate campaign DTOs. Published revisions and queued/built ships retain the backend's frozen catalogue, allocation and provenance contracts.

Exercise unavailable Engineering versions, invalid drafts, stale stamps, foreign colonies/blueprints, insufficient resources, revision locking, cancellation, save/load and blueprint-derived movement/combat. Verify that failed publication preserves both campaign state and the user's draft, and that new campaigns and load replace availability and revision projections correctly.

Do not enable Publish or advertise campaign-connected designer construction before these gates pass. Refit, AI design planning, tactical subsystem targeting and the wider [ship-design roadmap](design/ship-design.md) remain separate scoped work.
