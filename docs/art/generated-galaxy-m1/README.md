# Generated galaxy M1 / map presentation

Current scope: [owner checkpoint](E:/HobbyDS9/docs/generated-galaxy-m1-2026-10-20.md). Art/docs/exports only, based on exact `cc8578c17e919dacaa2975b5cc757bee1bc8befc`. Preserve approved UI7419313, battle art e104143, current packages/saves, owner desktop and open Blender. Secondary owns Unity C#/USS, imports, build and native input; Primary owns all generation, geometry, graph, persistence and validation. Stop after M1 delivery for owner review; do not proceed to sensors, exploration progression, AI or swirling3D.

## Concrete contract inventory

Primary's initial committed contract is **0fee81edddd32768f5968da5ffccb80d81cf1a76**, in `GeneratedGalaxyContracts.cs` and `docs/generated-galaxy-backend-contract.md` on `codex/generated-galaxy-backend`. The eventual sample/export commit must be pinned separately; this initial contract alone is not a generated sample or implementation acceptance.

- `GeneratedGalaxyDocumentDto`: required version/profile/tuning/seed/settings/content hash; Width/Height; Regions, Systems, Connections and HomeCandidates. Seed is an unsigned64 canonical decimal **string**, never converted through JavaScript Number.
- Region: stable NodeId, DisplayName, optional SystemId, integer Center and ordered Boundary. Coordinates have upper-left origin, +X right, +Y down. Boundaries are clockwise with implicit last-to-first closure. Uniform fit/pan/zoom only; no client Voronoi reconstruction.
- Proposed review sizes30/40/50 systems have exactly60/80/100 regions, half genuinely empty. Width1,000,000 and height600,000/800,000/1,000,000. These presentation units are neither pixels nor physical travel distance.
- System: stable SystemId, DisplayName and NodeId. Its point is the associated region Centre. No bodies/resources are included in M1; the inspector must not make any up.
- Connection: actual EndpointA/EndpointB and explicit BaseTraversalCost, Normal kind. Adjacency is generator-owned. `FindRoute` returns `StrategicRouteSnapshot`; render origin plus returned Steps (which exclude origin). Show TotalCost, never a pixel-derived distance or an unsupported ETA.
- Two candidate homes use existing civilisation-definition identities and real region/system IDs. They are unoccupied candidates, not owned planets, enemy contacts or fleet locations.
- Generate/Open/Export belong to the isolated portable review. Full generated output is visible in this **full-map review**, explicitly separate from future actor-filtered campaign knowledge. Open must validate before replacing the current map. Export/reopen never writes campaign save slots.

## Proposed native visual/state contract

Reuse the approved amber/lavender/ice LCARS vocabulary through [tokens.json](../../../art/generated-galaxy-m1/tokens.json). No new raster or paid generation is needed. Dark map, restrained neutral region boundaries and subtle lanes preserve the liked irregular breakup. Star hierarchy comes from selection/candidate status, not invented star classes or ownership.

Secondary confirmed a **separate full-width review shell**, Back to menu and no campaign rail. Approximate inspector widths:280px at1280×720,320px at1920×1080,360px bounded at3440×1440. The map fits the source aspect with a margin rather than stretching polygons to fill the panel. Inline seed/size(default40, options30/50)/Generate/Export/Open stay reachable; initial state has no automatically generated map. Ordinary inspection has no modal. Replacing unsaved generated work requires a deliberate choice; failed generation/open retains the current valid map and its identity.

Star selection uses an amber ring; region selection uses an ice outline and very faint neutral fill. Candidate starts have separate open brackets with **Candidate** labels, never faction-coloured territory. Centre star and outer candidate bracket have distinct screen-constant hit targets. Hover and keyboard focus use ice feedback independently of selection. Empty region selection explicitly says no system, without implying survey, resources or political control.

Overview shows all actual star dots, candidate names and selected/route endpoints. Mid zoom adds system labels with stable-priority collision suppression. Close zoom can expose all readable system labels and the selected region identifier. Labels and hit targets stay in screen pixels while map geometry zooms. Do not drop actual stars to fake sparse data or add decorative stars to empty cells. Extra emptiness must come from Primary's actual generated regions; backdrop imagery never carries gameplay knowledge.

Route emphasis uses an amber path through exact returned node centres, including empty navigation regions. Start/target rings and inspector names distinguish endpoints. Right-click can prepare a route preview only in this review; it is not a fleet order or an interception capability. The native entry queries Primary for arbitrary endpoints. An art-only sample viewer may display only exported route fixtures and must identify unavailable fixture previews honestly rather than running its own replacement routing algorithm.

## Pending actual-sample evidence

Overview, mid and close examples at all three sizes, including real system/empty-region/candidate selection and a checked route, will use Primary's committed generated sample and route fixture. Until those arrive this file and tokens are a concrete style/contract handoff, **not screenshots or evidence of a real generated map**. No historical Sol/Bajor/region fixture will be relabelled as the new generator.
