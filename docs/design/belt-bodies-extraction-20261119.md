# Belt bodies and extraction — minimal delivery contract

19 November 2026. **Owner-requested capability; proposed implementation policy and balance; no runtime/native acceptance claimed here.** Source: [new UX brief](../ux-playtest-20261119.md), `a92368fdb173fade4bf17514461d2b6abdba81b7`, over verified playable `fec332ee8af4e4c28054239c73b347dd0778fa4c`. This request supersedes the previous no-mining boundary for this batch. It does not authorize a wider economy, habitat, conquest or resource-market rewrite. Independent portrait, world-pane and dense-construction work proceeds immediately.

Read with [architecture](../architecture.md), [exploration](exploration-missions.md), [colonisation](colonisation-and-conquest.md), [normal construction](time-movement-mobilisation.md#normal-production-and-material-supply--agreed-25-october-2026), [current resource/logistics foundation](../economy-population-v1.md) and [refinement edition](../engineering-refinement-v1.md). Historical acceptance/version numbers in those documents do not override the pinned playable source.

## Agreed request and current gaps

**Agreed:** a belt is a selectable/discoverable celestial body with finite mineable resources and normally low habitability; survey and resource disclosure are lawful; extraction changes real resources after real work; old saves, Sol visuals and working Engineering remain intact. Deposit sizes, yields, construction recipe, scores, delivery limits and habitat capabilities are **proposed/versioned tuning**, not numerical canon.

At the pin, [SystemAsteroidBeltDto](../../src/HobbyDS9.Contracts/CampaignSystemRichnessContracts.cs) contains only aggregate identity/system/radii/VisualSeed. [Planet survey](../../src/HobbyDS9.Simulation/CampaignTacticalReviewSession.ColonisationCommands.cs) targets actual planets. [Economy extraction](../../src/HobbyDS9.Simulation/EconomyPhase.cs) drains each colony's own planet into that colony's stock. The [streamed queue](../../src/HobbyDS9.Simulation/CampaignTacticalReviewSession.ColonisationProduction.cs) has published ships, local infrastructure and settlement kits, not targeted belt installations. These require narrow typed additions; a fake planet/colony or mining button cannot bridge them.

## Body identity, generation and habitability

- One canonical body per **belt aggregate**. Reuse existing `FeatureId` as belt `BodyId`, validating global uniqueness, or retain a validated one-to-one adapter if the final API needs another ID. Generated identity remains `belt.richness.v1.<SystemId>`; Sol remains `belt.authored.sol.asteroid.v1`. Rendered rock instances have no gameplay IDs, deposits or ownership.
- Add an explicitly typed `AsteroidBelt` definition/state, not `PlanetStateDto` with a misleading name. Frozen facts include system/name/body kind, policy, habitat inputs and initial deposit amounts by existing resource ID. Mutable facts include remaining deposits, operations and owned local stock. Retain the existing astronomy record and its Sol 2200–3200 milli-AU extent/VisualSeed unchanged.
- Generate resources/habitat inputs once from a new pinned policy/hash domain, map seed and stable belt identity. Do not change existing belt existence, planet generation, campaign RNG or visual density to encode ore. Loading, surveying and drawing never regenerate deposits. Start with the real `resource.dilithium` consumer; new resource types are unnecessary. Deposit quantities are Backend-owned bounded tuning; the new Sol belt must have a real positive deposit for receiving.
- Expose a **surveyed, clearly based unassisted habitability score**, using existing 0–10000 basis-point conventions. Recommended first policy: 0 for unsupported unassisted biological habitation, with zero natural settlement capacity. A different low score is permissible if its species/environment/habitat basis is explicit. This is not a population-weighted colony score or a claim that all species have identical preferences.
- Mining provides no population, food, natural capacity or artificial habitat. Reject ordinary surface-colonisation/camp deployment to belt IDs. The current camp's artificial capacity is not permission to settle every astronomical object. Orbital habitat settlement can remain explicitly unavailable in this slice; a low score and resource discovery grant no settlement rights or ownership of the system.

## Lawful survey and observation

Reuse [current presence/retained-arrival disclosure](../../src/HobbyDS9.Simulation/CampaignTacticalReviewSession.StaticAstronomy.cs). Chart adjacency alone reveals no belt, belt count, ore or habitability. Discovery means known existence, not survey or operating permission.

Add a real belt-survey order/state, or a versioned discriminated body-survey target. Recommended first survey reuses current requirements: owned living stationary ship in the system, powered `StudyWork >= 1`, two actual weekly ticks, and interruption/cancellation behavior consistent with planet survey. Share ship-task exclusion across planet/belt survey and active/awaiting-claim study; selection does no work. An autonomous mining installation does not occupy that survey ship after deployment. Any later ship-based extraction must also share task exclusion.

On completion retain the observed habitat/resource facts and turn. Distinguish **unknown**, **current** and **last observed**. Immutable initial deposit evidence is not remaining ore. Live remaining quantities require explicit current observation/own operating telemetry; after coverage ends, retain the last lawful observation rather than reading private remaining deposits. Own extraction stock/work is operational information, not a blanket sensor into a foreign installation. Policy must define when observations refresh and persist those records.

Unknown values are absent, not zero/empty; known exhausted ore is zero. Current planet projections' zero/empty defaults must not leak into belt UI. Reasons, filter counts, markers and visual colors cannot reveal unsurveyed facts. Discovery/survey grants no resources or free Science reward.

## Proposed real extraction path

Use a **self-contained uncrewed belt mining installation**, sponsored by an owned, populated, Established colony in the same system. No synthetic settlement is required. Its closed recipe covers construction and local deployment; pin this operating assumption in the new edition rather than pretending a distant colony supplies the existing planetary mine's PowerDemand3. No new power-network mechanic or invented habitat capability is required.

| Step | Required behavior | Initial proposed policy |
| --- | --- | --- |
| Queue installation | Stamped shared command identifies exact belt and sponsor; require lawful completed survey, local operating access and supported resource. Freeze target/recipe. Add a real belt-targeted item to the sponsor's existing mixed FIFO. | One queued/built installation per actor/belt; normal queue position, no extra Industry budget. |
| Construct / deploy | Use actual allocated Industry and cumulative delivered-material accounting through `StreamedMaterialWork`. Cancellation follows this edition's sunk-work/consumed-material rule. Completion creates the belt installation, not colony infrastructure. If local deployment access is blocked, retain paid work and wait; never redirect to another body. | 60 Industry, zero upfront credits, no material ingredient matches the current frontier-mine cost precedent. The new recipe may differ if explicitly pinned. No immediate ore or next-item work cascade on completion. |
| Extract | From the next Economy phase after activation, debit the belt deposit and credit the mine's separately owned local stock atomically. | `min(3 units/week, remaining deposit, free storage)`; storage 12. Rate/storage/resource mix remain tuning. Full storage or exhausted deposit pauses without destroying ore. |
| Deliver | Transfer actual mine stock only to the frozen same-system sponsor, using the existing freight pass's remaining receiving capacity and route/integrity rules. Need is the normal desired stock/active construction demand. | Mine throughput cap 3/week, shared sponsor endpoint budget, existing stability/integrity scaling. No empire warehouse or instantaneous Planning import. |

The extraction formula is shared in meaning with existing finite-deposit accounting, but requires a typed belt source. Separate per-actor mine stock does not duplicate the belt's shared remaining deposit. Process competing installations in a pinned stable order and cap total installations/orders/records. Local operating policy must be explicit; no treaty, capture or territorial ownership mechanic is implied by mining. If existing access rules forbid a location, expose the lawful blocker rather than invent permission.

Extend [PopulationLogistics](../../src/HobbyDS9.Simulation/PopulationLogistics.cs) with typed belt sources or an equivalent shared-budget seam. Preserve ordinary freight priority; belt deliveries consume what remains, not a newly reconstructed full sponsor budget. Decrease source stock by exactly the amount delivered. Blocked/full/invalid destination leaves ore at the mine; lost or no-longer-owned sponsor pauses operation/delivery without automatic retargeting, transfer of ownership or refunds. Receipts identify a belt origin rather than inventing an `OriginPlanetId`.

The inherited freight path currently treats any other owner's stopped fleet as blockage/interference. Reuse that implemented versioned rule for this slice; do not silently broaden/narrow diplomacy or claim the canon's future planet-specific blockade model is complete. Avoid revealing the hidden cause of a generic unavailable route.

For each resource, extend the existing complete conservation invariant, including **planetary deposits, colony stocks, cargo, material reservations/consumption and existing sinks**, with **remaining belt deposits and owned mine stocks**. Extraction and delivery move quantities once; paid setup, extraction and delivered amounts have distinct receipts. Query, preview, survey completion, save and reload cannot mint ore. Shared validation, phase/ownership/stamp guards, deduplication and transaction rollback apply equally to human/AI calls. Adding a mining AI planner is unnecessary.

## Backend / GUI boundary

Final type/member names belong to Backend; the following semantic projection is additive, not an assertion that these types exist. Return one immutable actor/turn/revision/edition-bound capture, through the existing prepared-frame/worker ownership boundary.

| Projection | Required fields |
| --- | --- |
| Belt identity | Body/FeatureId, SystemId, display name, kind; unchanged visual record. |
| Knowledge / survey | Known/surveyed status, observation turn/freshness per fact; own active order and completed/required survey work; lawful eligible ship choices and stable reasons. |
| Environment/resources | Nullable habitability, scale and basis; settlement capability/reason; resource IDs and separately qualified initial/observed-remaining values. |
| Own mining | Exact sponsor, queue/order/installation identity, paid/required construction work, operating/blocked/exhausted status, mine stock/storage, actual extracted/delivered quantities and recipient. |
| Actions | Survey/start/cancel and queue/cancel mine only when real commands exist; actor/frame-bound eligibility/reasons. Client submits identities and intent, never desired deposits, yield or completion. |

In the actual [ReviewColonisationPanel](../../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewColonisationPanel.cs) route, union **already disclosed** Planet/Belt entries using a discriminated stable selection key. Preserve planet actions/details; add belt-specific details and one aggregate activation target. Handle a disclosed belt-only system without a planet-selected early return. Keep Sol's current renderer; do not create clickable gameplay rocks. A selected belt never submits an order. The existing `ReviewUiModel.PrepareColonisationObservation` and prepared-frame binding must not merge observations from different revisions.

## New edition, deterministic replay and receiving

Prefer an explicit new-campaign edition with distinct package/profile/root identities and save suffix; Backend supplies final numbers. Retain all Format17 refinement state, policy/support-v5 behavior, publications and Sol data. Keep old Format17 and older loaders/slots closed to new data; no silent promotion of decorative old belts into ore-bearing bodies, no old-save overwrite, and no hidden regeneration. Migration is unnecessary unless separately designed and tested.

Persist frozen belt definitions, remaining deposits, actor observations/survey work, installation/queue targets, mine stock, delivery accounting and command history. Extend detached copies, strict codecs, visitors, bounded validation, canonical hashes and complete replay. Refinement already interleaves commands at `BaseJournalOffset`; new belt operations need one unambiguous total accepted order relative to refinement and ordinary operations, including multiple commands at one offset. Separate unsynchronized journals are insufficient. Existing root/visit limits remain; publish explicit bounded counts rather than bypassing them.

Receiving must show: deterministic/reordered generation and unchanged Sol visuals; unknown/arrival/survey/current/stale disclosure without leaks; duplicate/stale/foreign/invalid-phase rejection; paid queued construction and blocked deployment; no yield before activation; finite extraction, full storage, depleted deposit, shared-capacity delivery/blockage, sponsor loss and conservation; whole-root save/reload/replay including interleaved Engineering Accept/Continue/Discard. Use a real owned Sol sponsor, survey-capable ship and positive belt deposit, not a fixture-only mining button.

QA owns the exact combined Windows build and native selection/survey/construction/turn/resource-change/save/quit/cold-continue proof. GUI ships the dense ship **and installation** rows in the actual generated-campaign route and the independent planet/world-pane changes. A GUI-only intermediate must list belt gameplay as pending. All numerical policy choices above remain proposed and must be recorded with the implemented source pin. No main merge is authorized.
