# Generated galaxy M1 / map presentation

Later owner correction: see the separately preserved [v1/v2 irregularity comparison](irregularity-correction.md). That revision improves geometric distribution only. Neither version implements the subsequently clarified stellar-mass/black-hole/nebula causality; final M1 design acceptance remains open.

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

## Actual sample exports and three-size evidence

The data now comes from Primary **771a3deadbf600078ee57eff9cd29d4660138c8c**, following the initial contract above. [Sample index](../../../art/generated-galaxy-m1/sample-index.json) pins exact input bytes and matched route-file hashes. Three samples were copied from the committed backend sample exports, without modification. Primary supplied seed43/40 as an additional actual export from the unchanged generator; its source note is retained. No old Sol/Bajor mockup or independent generator was reused.

| Seed / systems | Regions / empty | Canonical generated content hash |
| --- | --- | --- |
|42 /40 (default)|80 /40|`6e306b02c09344db583fd0871f806e42549e81a06697f53dd124fa1305660a8f`|
|43 /40 (same-size comparison)|80 /40|`8339df2ce3569c60407620a83a604bd73046bdd1da1c1c8749055247c3c9a46a`|
|12345 /30|60 /30|`85dac90f91180327f0fad3b915ca3145fdd8a1d1108d7b4f543c8c300d655884`|
|20261020 /50|100 /50|`c6d6dfcbee54c17607d5090f47ad403f5ba158a7d96efeadcc764da7a2d8b6cd`|

Open the [versioned sample design review](E:/HobbyDS9/artifacts/art/generated-galaxy-m1-20261020-v1/review.html), [interactive sample viewer](E:/HobbyDS9/artifacts/art/generated-galaxy-m1-20261020-v1/index.html), or [complete review ZIP](E:/HobbyDS9/artifacts/art/generated-galaxy-m1-20261020-v1/HobbyDS9-Generated-Galaxy-M1-Art.zip). The source renderer is [tools/art/generated-galaxy-m1](../../../tools/art/generated-galaxy-m1/index.html). It consumes samples only: no generation algorithm, Voronoi or pathfinding replacement is present. Its deliberate **View generated sample** button differs from native **Generate**, which Secondary binds to Primary. Native Open/reopen validation is intentionally not faked by the artifact's unavailable Open control.

The39 captures cover13 states at1280×720,1920×1080 and3440×1440: initial setup, default overview, optional graph links, selected system, empty region, candidate, route, mid/close zoom, unavailable sample retaining the valid map,30/50 overviews and same-size alternate seed. Final screenshots were visually inspected at narrow, standard and ultrawide sizes. Dense graph links are now **optional and initially off**, leaving the liked irregular region structure and actual empty space readable; selected route remains visible independently. Mid/close labels avoid the toolbar/legend. Region keyboard focus follows its actual contour rather than a misleading bounding box.

The default candidate route is `nav.generated.001` plus the ten returned Steps through actual graph nodes to `nav.generated.040`, total declared cost10. Its line includes empty navigation regions. The renderer checks every supplied route edge against actual Connections and sums only declared edge costs as a parity assertion; it never searches for routes or derives costs from geometry. In the native review, use FindRoute for any selected endpoints. In this art viewer, unmatched endpoints explain that only the exported candidate route is available; they do not invent a path or imply the real graph is unreachable.

## Receiving implementation details

Use the data extent for uniform Fit, leaving honest letterboxing instead of widening polygons. Star and bracket radii, stroke widths, labels and focus stay in screen pixels; centres/boundaries transform from the shared integer coordinate space. The source SVG is an art renderer, not an alternate authoritative mesh to import over DTO geometry. Candidate labels use the admitted civilisation catalogue's display name with the word **candidate**. Do not interpret its identity as owned territory.

Use a4px drag threshold before map pointer capture, including over region polygons. Suppress the click after a real pan so it does not accidentally select another region. Preserve keyboard focus when rerendering a selected marker. New map content identity clears selection/route/camera context even where node IDs repeat across seeds; generated IDs are map-scoped. An unavailable generation/open must retain the valid map rather than half-installing failed data. Secondary's native review owns unsaved-map confirmation and verified Open; no campaign/session object is replaced by ordinary inspection.

The complete browser checks passed three size-specific interaction groups with zero page errors: no automatic generation; actual system/region count parity; distinct star/empty/candidate targets; exact route sequence/cost; optional complete graph links; semantic zoom/Fit; keyboard and pointer pan; failed sample replacement isolation;30/50 and alternate-seed views. Independent rendered-data checks verified **all80 region polygons /446 vertices and all40 system centres** against the supplied default DTO after only the documented transform; two candidate markers; retained keyboard focus; exact exported JSON document parity; source/image hashes and loaded gallery examples. Reports are `review-checks.json` and `parity-checks.json` beside the gallery.

This is CPU/headless Chrome (`--disable-gpu`) **art/browser evidence only**. It does not rerun Primary's generator tests or prove portable Open, Unity widgets, physical input, performance or the final native package. No protected executable, existing save namespace, latest pointer, owner desktop, Blender scene or engine resource lease was touched. Backend source review/CI and Secondary's native receiving acceptance remain separately reported gates. Art stops here for the M1 owner review, with no sensors, exploration, AI, swirling3D or battle-asset work started.
