# Provisional ship design fixtures

`fixture.json` contains a standalone format-1 `Catalogue`, explicit version `Access`, and two editable `Drafts`. Hosts/tests decode it outside the portable libraries. It is not part of `ds9-slice/scenario.json`, live production or campaign content hashing.

Surveyor and Escort share `hull.utility.v1`: 100 usable capacity, hull 30, distribution 60, two size-2 forward energy mounts and one launcher mount. Both fit exactly 100 capacity. Surveyor is unarmed; its Survey profile uses 52 power and produces Science rating 100. Escort's Combat profile requests 66, allocates 60, and produces shield 20, attacks 12/15/18, accuracy 7500 and evasion 1000. Cruise budget is 4 for both. Escort's reduced weapon output is intentional and does not add power or change resolver doctrine.

Costs: Surveyor credits/Industry/maintenance 30/50/10; Escort 34/56/12, each with one Dilithium unit. Crew, load, sizes, power, performance and costs are abstract illustrative units, **not canon or accepted balance**. Operational-range/Science/sensor ratings have no campaign consumers yet. See [implementation/API contract](../../docs/ship-design-infrastructure-v1.md).
