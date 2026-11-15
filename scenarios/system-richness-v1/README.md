# System richness v1

15 November 2026. SYSTEM-RICHNESS-01 is the owner's scoped world/System completeness exception to the gameplay feature hold. **Implemented source candidate; GUI/Art belt rendering and native acceptance remain open.** Survey/reactor implementation remains held. See [architecture](../../docs/architecture.md), [exploration/knowledge design](../../docs/design/exploration-missions.md), [static astronomy](../../docs/generated-static-astronomy-v2-contract.md) and [Sol08](../sol-eight-planets-v1/README.md).

## Generation and preservation

[policy.json](policy.json) is the closed provisional recipe `system-richness-v1`: uniform target counts **3, 4, 5, 6**, theoretical mean **4.5**, and a provisional **75% chance of one belt** in eligible generated systems. These are initial content choices, not established balance or an astronomical population model. No rare extremes are added. A target is not a hard cap: existing authored bodies are never removed. Source starter systems are preserved, including Sol's exact eight bodies and the existing Cardassian home. Historical fixed scenarios and profiles do not opt in.

The existing portable physical inventory remains the authority. It first creates its unchanged primaries, activity/minor bodies and four provisional near-frontier opportunities, then adds real definition/state/body/control/identity rows up to the target in non-starter systems. Additional IDs are `planet.colonisation.zz.richness.v1.<SystemId>.<ordinal>` (matching `planet-definition.colonisation.*`). The suffix sorts after existing generated bodies, preserving their names/identities. Authored Sol08, original body definitions, starting colonies/population/stocks, Science, Engineering20, fleets and campaign RNG remain unchanged. New planets are unclaimed and receive no colony, population or delivered stocks.

Count and belt decisions hash **generation version + map seed + stable SystemId + separate purpose domain**. Additional physical environments/deposits reuse the existing deterministic inventory recipe with versioned stable planet IDs: Temperate/Arid/Oceanic/Frozen/Vacuum, existing unit conventions, finite deposits and natural-capacity rules. They are provisional game environments, not NASA facts. Some worlds have zero natural capacity and no deposits. Habitability, survey, settlement and extraction eligibility still depend on actual existing attributes/validators; body count grants no yield or bonus. No new mining, resource reward, hazard, travel cost, AI rule or survey behavior is introduced. Queries/rendering consume no campaign RNG or allocation sequence and do not generate content.

## Persistence and admission

The complete outer carrier is **Format15**, profile `generated-colonisation-system-richness-v1`; package format2/profile `generated-colonisation-system-richness-package-v1`. `CampaignSystemRichnessPackageDto` carries `Generation`; `CampaignSystemRichnessSaveDto` carries `GenerationVersion` and `SystemBelts`, alongside the existing complete Cycle checkpoint. The classes are the one explicit closed extension of the original Cycle carriers. Unknown derived carrier types are rejected; JSON type-name dispatch is not enabled. Old Cycle14 retains its original properties and serialized shape, with no added null fields.

The frozen physical inventory has policy `planetary-inventory-richness-v1` and a new authoring/hash domain. Its bound is **6,040 planets**, with at most **1,000 belts**. The common ownership validator now supports that structural ceiling; old closed inventory and preflight admission still enforce their original **1,040** limit. Existing 2,000,000 validation-visit and 64MiB root limits remain; belt records receive explicit bounded outer accounting. No journal, AI planning or replay budget is relaxed.

Map topology, generator/stored geometry format, star nodes, lanes and route costs are unchanged. Loading consumes the stored map and retained physical genesis through the existing full ordered replay and exact comparison. It also compares every stored belt against the versioned deterministic start. Missing, mixed, truncated, duplicate, altered or earlier carriers reject. Earlier content is **not** regenerated or migrated into the new edition.

PB New campaign selects the new package using the prepared `PlayableFleetPackages/v1/system-richness-policy.json`; default historical builders/profiles keep their existing package. The PB save directory becomes `HobbyDS9CampaignColonisationCycleOnly-v1-engineering20-sol08-richness1`. Earlier directories and files remain intact. Existing cycle filenames are retained inside the separate directory; the complete carrier header identifies Format15. Start a new campaign; Continue is limited to the matching edition.

## Belts: exact GUI/Art contract

`CampaignSystemRichnessSaveDto.SystemBelts` stores these independent static astronomical features, not fake planets, study sites or ore grants:

| Field | Meaning |
| --- | --- |
| `FeatureId` | Stable `belt.richness.v1.<SystemId>` identity |
| `SystemId` | Actual generated system association |
| `InnerRadiusMilliAu` | Provisional inner circumstellar reference radius, integer 2200–4199 |
| `OuterRadiusMilliAu` | Inner radius plus deterministic thickness 300–999 |
| `VisualSeed` | Persisted 64-character hex hash in a separate visual domain |

The radial unit is **`milli-au-procedural-reference-v1`**: 1000 units represent one nominal AU for this authored procedural layout, not a measured orbit or ephemeris. Generated planet orbit positions remain presentation; no physical orbital-motion law is introduced. Do not label their schematic positions as measured distances. Sol's separate NASA mean-distance contract remains unchanged, and this generator adds no belt to a preserved starter system.

The existing `CaptureKnownAstronomy(actor)` snapshot now has immutable **`SystemBelts`** and nullable **`SystemBeltGeneration`** (`system-richness-v1` only for the new edition). Each `SystemAsteroidBeltSnapshot` copies the five fields above and exposes `RadiusUnit` plus `DisclosurePolicy=system-presence-retained-arrival-v1`. Nebula proxies and their original charted-centre policy stay unchanged. The original five-argument snapshot constructor is retained.

Belts use the **same authoritative disclosure helper as physical planet existence**: current own presence/colony coverage or retained actual arrival. A star-chart neighbour alone does not disclose its belt. Departure retains the known feature; survey/environment/control remain separate, so belt discovery reveals no hidden planet statistics or ore. Do not render the private root's full belt array. Render only the actor snapshot, using persisted identity/seed/radial extent; never regenerate existence or scatter with campaign RNG, frame order or wall time. Include the generation/actor/revision in any visual cache identity. Art may build the belt appearance around that stable visual seed; no mining/hazard affordance is supported.

## Receiving evidence and limits

Focused execution covers real body counts through 1000 systems, saved complete carriers and normal turns, missing/tampered/duplicate astronomy, old/new rejection, source reorder stability, campaign/map/stock/RNG counterexamples, unknown-before-arrival and retained-after-departure disclosure, closed recipe/types, typed host/store, Sol08 and Engineering20. Existing generated/astronomy, carrier and root-budget regressions are separate evidence. Exact pins, counters, metrics and logs live in `artifacts/coordination/SYSTEM-RICHNESS-01/backend.md` in the shared workspace.

This source does not draw belts. GUI must consume the actor-filtered static records, preserve planet selection and any Sol08 distance navigation, and label procedural scale honestly. QA must verify the new content/header/save namespace in the combined player, discover a belt lawfully, inspect 3–6-body systems and retained hidden statistics, save/cold Continue, and measure actual frame/input/command/load behavior. CPU replay measurements are not native frame times. Main-HUD fidelity and existing Editor/native/art findings remain open.
