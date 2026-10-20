# Environment-first M1: real format2 samples

These maps seed stars/system anchors, two black holes and three nebula footprints **before** deriving region density and topology. They are actual portable exports, not environmental overlays on an unrelated random graph. Geometry and influences are normalized review abstractions, not physics or campaign hazard/sensor/AI rules.

Every `.environment.json` has a matching `.route.json` produced by the public route API. Layout is a geometry container, not a nested legacy document. The source contract is [environment-galaxy-m1-contract.md](../../environment-galaxy-m1-contract.md).

| Seed / systems | Mode | Content SHA256 |
| --- | --- | --- |
| 42 / 40 | all | `404d1e15057f42531a6f1da078efd78663d902dcff22e5484e6e3ba2cc8d68fe` |
| 43 / 40 | all | `2cbb3c7ec0a8a83868f4d6e79cf9f2c2f4d07ead42cfe9503e6903721ce547fe` |
| 12345 / 30 | all | `768d23805daf819671ec63a0705b85115aba66eb0a2a2aae98d648a5aaa020a7` |
| 20261020 / 50 | all | `7717edb8d52de98e20b14574aad33430333cde62397c2e531e4f4deb8d4f6a10` |
| 42 / 40 | none | `149b748f0c2be5edd42471575bf298acf751e26e7211b71704ffa61bb29d3a81` |
| 42 / 40 | no-stars | `e58781cdc4bf92714bb1127be55cc18f45f64d77dc0d0fc24348c1f80798a742` |
| 42 / 40 | no-black-holes | `b213ba0f15469bab0ff60f6940afae863b15fd5ba12ef7678021bf42056c12eb` |
| 42 / 40 | no-nebulae | `99267a670c668bee2d2f5d57c37b2ded4eff8cc52f683d505e8bed1fc1b8c15e` |

## Reproduction and causal comparison

`dotnet run --project src/HobbyDS9.Headless -c Release -- --environment-galaxy 42 40 all` emits a normal map. Replace the command with `--environment-galaxy-route` for its candidate-home route, or mode with none/no-stars/no-black-holes/no-nebulae for controlled generation comparisons. `--environment-galaxy-open <path>` validates full format2 content. Old `--generated-galaxy-open` still validates format1 v1/v2 without migration.

`--environment-galaxy-causality` reproduces [causal-comparisons.json](causal-comparisons.json): seeds0–15 plus42, same features/tuning and other inputs per on/off pair, measuring **extra sites** inside the union of the selected family's half-weight ellipse cores. Mandatory star/black-hole anchor sites are excluded. A site's core membership uses the same declared integer elliptical falloff, evaluated from the persisted feature parameters. The fixed total region budget means sites redistribute elsewhere; this is not a guarantee of an increase for every individual seed or an inverse-area formula.

| Family | Enabled / disabled local extra sites over17seeds | Seed42 alone |
| --- | --- | --- |
| Stars | 415 / 335 | 23 / 21 |
| Black holes | 106 / 49 | 6 / 2 |
| Nebulae | 179 / 108 | 8 / 5 |

In each comparison the entire Features collection, including positions, footprints, weights and explicit star/system/node associations, is unchanged. Switching off an influence does not delete its feature or its mandatory anchor. All-off maps retain only baseline pressure for extra-site selection. Frontend visibility toggles are separate and must not substitute these generation switches or alter authority.

## Presentation meaning

Render stars at Features.Position, associated with their explicit SystemId/NodeId. Every star remains its original mandatory region site; it was not relocated into an arbitrary later region. Feature footprints are axis-aligned **generation influence ellipses**, not physical sizes, sensors, damage zones or territory. Clip drawing to the map extent. Nebulae span cells; they have no single NodeId. Black-hole nodes are genuinely empty of star systems.

RegionInfluences reports pressure sampled at each site: BaseDensity plus sorted Contributions, capped at MaximumDensity. It supports truthful “these features increase local sampling pressure” explanations, not unique attribution of a complete polygon to one object. Regions/graph/costs come from Layout; all ordinary edge costs remain1 with no duplicate disturbance surcharge.

Format2 / environment-first-regions-v3 / provisional-environment-influence-v1. The new identity does not rewrite retained format1 samples or campaign saves. Review counts and influence parameters remain **provisional**. No M2 work.
