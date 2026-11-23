# Authored eight-planet Sol edition

15 November 2026. SOL-08 is the owner's narrow System completeness exception to the gameplay feature hold. Source candidate only; GUI distance rendering, navigation, native acceptance and main-HUD fidelity remain separate gates. Survey/reactor gameplay is unchanged.

`scenario.json` extends the [Engineering20 edition](../engineering-development-v1/scenario.json). It retains **Earth `planet.sol`** and **Mars `planet.mars`** exactly. Six distinct definitions/state identities add Mercury, Venus, Jupiter, Saturn, Uranus and Neptune (`planet.<lowercase-name>` / `planet-definition.<lowercase-name>`). Nothing renames Earth to `planet.solEarth`. The existing Earth/Mars colonies, population, deposits, ownership, designs and research are unchanged. No moons, dwarf planets, orbital motion, new settlements or narrative effects are added.

## Astronomy and presentation join

[astronomy.json](astronomy.json), version `sol-mean-orbits-v1`, records NASA NSSDCA's [Planetary Fact Sheet — Metric](https://nssdc.gsfc.nasa.gov/planetary/factsheet/), retrieved 15 November 2026, source marked updated 13 November 2026. Its [notes](https://nssdc.gsfc.nasa.gov/planetary/factsheet/planetfact_notes.html) identify the Sun-distance row as semi-major axis. Source precision is retained, converting the published millions of kilometres to integer kilometres:

| Body | Mean Sun distance (km) |
| --- | ---: |
| Mercury | 57,900,000 |
| Venus | 108,200,000 |
| Earth | 149,600,000 |
| Mars | 228,000,000 |
| Jupiter | 778,500,000 |
| Saturn | 1,432,000,000 |
| Uranus | 2,867,000,000 |
| Neptune | 4,515,000,000 |

These are static mean-radius references, not present positions, time-averaged radial integrals or an ephemeris for the campaign date. GUI should apply one uniform distance scale (or a camera/projection), preserving these radius ratios. A selectable diagram is allowed, but equal-spaced rings must not be described as real distances. Body mesh sizes and visual phases are independent presentation choices, not supplied scientific dimensions or authoritative movement.

Preparation copies the two files to `PlayableFleetPackages/v1/sol08-scenario.json` and `sol08-astronomy.json`. GUI may read `Version`, `DistanceUnit` and `Planets` (`PlanetId`, `DisplayName`, `Order`, `MeanSunDistanceKm`) from the astronomy resource. Join **only** to planets already supplied by the actor-filtered colonisation observation. Never construct additional displayed bodies from this table or use its eight rows to reveal an unknown system. Locate generated Sol through the observed `planet.sol` row's current `SystemId`, not the old `system.sol` string. Rendering/navigation/LCARS selectors belong to GUI; this backend change supplies no renderer or orbital simulator.

## Required gameplay fields are provisional

The six additions have empty deposits/remaining deposits, no colonies, no population and no empire/government ownership. Required environment fields reuse the existing Expansion frozen inventory's no-natural-capacity Vacuum defaults: gravity 3000, pressure 0, radiation 200, `atmosphere.vacuum`, nine habitat rows, Volcanic share 10000 and all natural capacities 0. `PlanetClass` is explicitly **Unclassified**. These are provisional game placeholders, not NASA-derived gravity, radiation, atmospheres, habitability or a statement that Venus and the giants are physically vacuum worlds. No extraction reward or viable natural settlement is invented. Changing these gameplay facts requires separate content design; the six start unsurveyed and their environment is not disclosed by the System observation.

## Generated mapping, knowledge and compatibility

`GeneratedCampaignFactory` preserves template planets associated with a selected actor's source home-colony system, including uncolonised bodies. The source-system mapping must have one selected owning actor. It then maps each body's unchanged identity to that actor's generated home system. It does not inject Sol into random systems or manufacture a colony to retain a body. Templates without these additions produce the same retained planet set as before.

Home visibility uses the existing explicit authored Earth/Mars ownership and initial home knowledge/presence: own colonies provide home-system coverage, which discloses physical body identities. This is the approved all-eight initial home disclosure within the current model; there is no reveal-all policy or fabricated survey receipt. Only owned Earth/Mars are initially surveyed under the existing rule. The other six reveal neither environment nor detailed control until ordinary survey rules allow it. The rival actor receives no Sol bodies without its own coverage/arrival. Existing arrival retention and other-system discovery remain unchanged.

The scenario bytes and scenario ID change, producing new template/package/content identities. Existing strict loaders reject earlier content in both directions; there is **no migration or reinterpretation**. PB Cycle now uses `HobbyDS9CampaignColonisationCycleOnly-v1-engineering20-sol08`, leaving earlier stores intact. The PB menu explicitly requires a new campaign. DTO, wrapper, rules and save-format versions are unchanged because the existing content-mismatch contract handles this new edition. The astronomy resource has its own presentation version and source pin; it is not new persisted simulation state. Other profiles and historical scenario/resource bytes remain unchanged.

Focused tests exercise actual Cycle composition at 30/250 systems, actor filtering, unclaimed genesis, no partial survey disclosure, exact save/replay roundtrip, a successful turn, old/new content rejection, preservation of all pre-existing authored facts and Engineering20 regression. QA must combine the source with GUI's distance-rendering/navigation work and verify the actual prepared player. Source tests alone do not accept real-distance rendering, small inner-orbit picking or native layout/performance.
