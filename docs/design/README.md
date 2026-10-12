# Gameplay design decisions

Decision date: 12 October 2026.

Markdown in Git is the authoritative source for the migrated decisions below. [The Google gameplay design document](https://docs.google.com/document/d/1-Z8K30kPxawcEbZKhNvxrmswrq7h7soADP7plu95_WA) remains the legacy source for other gameplay chapters; this is a scoped migration, not a complete replacement of that document.

## Chapters

| Chapter | Scope | Status |
| --- | --- | --- |
| [Strategic AI](strategic-ai.md) | Persistent goals, coordinated plans, editable faction personalities, five fair difficulties, worked diplomacy plan | Direction agreed; detailed tuning proposed |
| [Ship design](ship-design.md) | Shared capacity and mounting limits, dependencies, power profiles, engineered versions, refits and AI design | Shared capacity/mounting and hull refinement agreed; detailed rules proposed |

| [Exploration and procedural missions](exploration-missions.md) | System/body surveys, first contact, mission director, shared consequences, expedition repair dilemmas | Direction agreed; infrastructure and tuning proposed |

## Decisions adopted

- Keep faction abilities, personality and difficulty independent; canonical factions are editable presets.
- Give AI persistent victory goals and milestones. Turn utility scoring executes the plan.
- Provide five difficulties through better planning/search rather than resources, rule exceptions or hidden information.
- Assemble classes from a shared internal capacity budget with explicit mounting and compatibility constraints.
- Spend existing Engineering capacity to develop and refine hull/component versions; preserve diminishing returns and scientific prerequisites.
- Integrate the ship designer with the strategic planner: build for mission, timing, affordability and logistics.

The power-profile rules, version/refit details, schemas, example statistics and implementation stages are proposals, not additional user-approved balance values.

Exploration/first-contact decisions and the proposed mission infrastructure are recorded in [exploration-missions.md](exploration-missions.md). Persistent missions, survey/repair integration, diplomacy adapters and consequence schemas require scoped implementation; documenting them does not establish runtime support.

## Design versus implementation

These chapters describe intended gameplay. [Architecture](../architecture.md) and milestone/acceptance documents describe current implementation. Do not relabel a proposal as implemented merely because it is documented.

The current Science & Engineering foundation deliberately has one Engineering target, maturity capped at 100%, fixed ship designs and simple AI. The proposed concurrent Engineering portfolio, overmaturity, modular classes and hierarchical AI require future scoped implementation and acceptance. Preserve current behavior until that work is explicitly undertaken.

For Science/Engineering concepts not migrated here, read the legacy document alongside [current implementation rules](../science-engineering-v1.md). Cross-references to Chapters 3–7 in the migrated text refer to that legacy document.

## Working rules

1. Read the relevant design chapter before implementation.
2. Preserve **agreed**, **proposed** and **open** distinctions.
3. Report conflicts between intended design and current contracts before changing those contracts.
4. Make narrow, deterministic implementation increments; update acceptance and versioning separately.
5. Keep design decisions in Git; avoid competing edits to migrated sections in the Google Doc.
