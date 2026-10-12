# HobbyDS9 contributor instructions

Before gameplay implementation, read [the design index](docs/design/README.md) and the relevant design chapters. AI work must consult [strategic-ai.md](docs/design/strategic-ai.md); ship, hull, component, power or refit work must consult [ship-design.md](docs/design/ship-design.md). Exploration, survey, first-contact, mission/event consequences and autonomous-repair work must consult [exploration-missions.md](docs/design/exploration-missions.md). Time/calendar, movement, production buyout/expediting and sensor-warning work must consult [time-movement-mobilisation.md](docs/design/time-movement-mobilisation.md).

Read [architecture.md](docs/architecture.md) and the applicable milestone/acceptance documents for current contracts and implemented behavior. Intended design and implemented behavior have separate status. Flag conflicts; do not silently expand a scoped task to implement the complete roadmap.

Preserve agreed/proposed/open labels. Keep strategic rules and validation in the portable simulation; Unity presents snapshots and submits commands. Preserve deterministic ordering, save/version compatibility and shared human/AI validation.

For documentation-only work, check links, consistency and the diff; gameplay tests are unnecessary unless behavior changes.
