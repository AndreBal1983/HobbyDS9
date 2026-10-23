# Generated campaign v1 — early portable contract

23 October 2026. Owner resumed the playable-demo goal. This scoped increment connects Natural v5 to the existing campaign, rather than extending the map-review harness. Base: `5cb498d54e8b496e31cdbcd58019038ca56bac94`. Implementation/acceptance pending; this document is the early receiving contract.

## Public boundary

`CampaignGeneratedApplication.StartGame(CampaignPlayableFleetPackageDto template, NaturalGalaxySettingsDto settings)` and `LoadSave(template, CampaignGeneratedSaveDto save)` return `ICampaignGeneratedSession : ICampaignPlayableFleetSession`. Add `CaptureGeneratedMap(actor)` and `ExportGeneratedSave()`. All strategy/production/research/fleet/travel/tactical panels retain that SAME session. No renderer receives the trusted full save/map/template or an independent review session as campaign authority.

`CaptureGeneratedMap` returns detached known-region polygons, known connections, known system anchors and only known node-bound features; it excludes the seed, generation settings, hidden regions, all home-candidate assignments, hidden features and enemy orders. This is a presentation DTO, not a full Natural document. Map dimensions/hash identify the campaign map. Existing `CaptureStrategy(actor)` and inherited travel/contact/tactical views remain the actor-safe gameplay source. Trusted save stores alone receive complete saves. Failed creation/load constructs a separate instance and cannot replace a live session.

`ObservationTurn` and `ObservedNodeIds` distinguish current local coverage from known but unobserved geography. Provisional coverage is only own colonies and stationary own fleet nodes; a partially departed fleet's retained origin does not provide coverage. Ordinary uncloaked hostile contacts require physical co-location with that coverage. Revisited no-detection removes an obsolete track; neither that nor an empty observed region disproves stealth. Snapshot freshness, Follow admission and AI contact selection use the same current-coverage boundary. Unbound nebula footprints are deliberately absent in first-v1 campaign geometry; no local nebula artwork or full sensor model is claimed.

New outer format **7**, profile `generated-campaign-v1`, policy `generated-neighbor-exploration-v1`, owns the full Natural-format4 document, exact trusted template package hash, generated package identity, complete playable-shaped owned root and discovery history. Its embedded core marker differs from format6 so truncation cannot be imported as6/3/2/1. Old wrappers1–6/loaders and current Save14/Content11/rules `science-engineering-v1`/compiler `ship-design-compiler-v1` remain unchanged; no migration. Load builds the scenario from the **stored frozen map**, not by running the current generator on its seed. It validates frozen map structure/geometry/hash and exact template identity, replays accepted operations and compares the complete root/history before returning. Only New Game invokes generation.

## Deterministic factory order and identity

1. Validate the trusted existing playable template and Natural settings, with no caller mutation. Host verifies the exact template source-byte hash through its existing package reader.
2. Generate Natural v5 with its existing independent streams, settings, units and map IDs. No art or distribution changes.
3. Copy template definitions/state. Replace only geographic scenario content: generated region graph and actual system anchors; preserve positive shared edges at cost1 and empty transit regions. Seeded map homes replace fixed lore positions; the whole map is the galaxy, with no fixed quadrant partition.
4. Resolve selected starter civilizations and home assignments in ordinal identity order; initialize owned starting colonies, resources, research state, existing-class ships and equipment through copied validated content. Keep research/production/time/speed rules unchanged. Any bounded starter-content restriction and provisional pacing numbers must be explicit in the implementation receipt.
5. Initialize identical actor knowledge rules at each own home: own home and adjacent nodes/edges, no remote enemy state or authored omniscient tracking grid. Arrival charts neighboring ordinary regions/edges; knowledge remains distinct from physical topology and survey.
6. Validate the generated scenario and all nested package contracts before exposing a session. Generated content/package hashes domain-separate exact template hash, frozen Natural map hash and explicit factory/policy version. Gameplay never reads a wall clock or host frame state.

First functional starter policy explicitly selects the two existing equipped template civilizations named in Natural home settings: one human and one AI. The idle third fixture civilization and unrelated neutral colonies are omitted from live entities/references, not presented as a functioning empire. Existing owned colonies, resources, classes, initial hull/equipment/stocks and research are copied at generated homes; template economic asymmetries remain provisional scenario content, not AI difficulty bonuses. No fixed lore geography or quadrant partition is retained. Existing replay budget is copied explicitly from the trusted template and therefore bound by its source hash. Generated campaign RNG starts at unsigned seed XOR `0x47454e43414d5031`, independent of map-generation streams; no map draw consumes campaign RNG.

Generated contact rows are structurally checked during private staged operations; complete format7 ingress additionally replays every accepted operation and compares the complete contact/discovery/root state. The fixed-grid physical contact-proof validator remains exact for all older profiles. No loader accepts a generated nested root on the strength of structural contact checks alone.

## Lawful ongoing play

Reuse the existing paid construction, research, physical travel, simultaneous tactical rounds, persistent stocks/aftermath and accepted-operation replay. New generated policy combines owned-resource AI procurement/research with known-map exploration and observed contacts. No fixed hidden destination or full-map enemy search. Human and AI travel use the same legality validator. Scoped local contact observation must disclose its provisional coverage rather than claim a complete sensor/diplomacy/strategic-planning roadmap. No new equipment/hull/weapon rules, refits, calendar rebalance or arbitrary technology grants.

## Ranked gaps and gates

1. Scenario factory: exact region/system/home mapping and valid deterministic content identity.
2. New complete wrapper and replay validation; old save/package compatibility and atomic failure.
3. Restricted map projection, growing knowledge, local contacts and lawful AI strategy/travel.
4. Actual frontend binding to this session, save/Continue and the existing tactical loop.

First prove a250-system campaign: real paid construction/research, route progress/discovery, AI decisions, human/AI combat/aftermath and save/reload/repeat/reorder equality. Then measure1000 with finite generator/document/replay budgets. Independent portable source/privacy review precedes frontend engine/build/native acceptance. CPU-only backend assignment grants no Unity/GPU/input lease; protected player/packages/evidence remain untouched. Included-usage stop policy remains mandatory.
