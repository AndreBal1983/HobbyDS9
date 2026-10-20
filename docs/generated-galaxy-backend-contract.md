# Generated galaxy M1: portable contract

Status: implementation contract for owner review, 20 October 2026. Generation tuning is **provisional**, not approved campaign balance. Base cc8578c17e919dacaa2975b5cc757bee1bc8befc. This separate review entry generates real topology; it does not start a campaign or implement sensors, exploration progress, economics or AI.

## Binding surface

`GeneratedGalaxyApplication.Generate(settings, admittedCivilizations)` and `Open(document, admittedCivilizations)` return an isolated `GeneratedGalaxyReview`. `Export()` returns a detached `GeneratedGalaxyDocumentDto`. `FindRoute(originNodeId, destinationNodeId)` returns the existing `StrategicRouteSnapshot`: destination steps exclude origin; same-node succeeds with zero cost. Queries never issue fleet commands.

`GeneratedGalaxySettingsDto`: canonical unsigned-64 decimal `Seed` string (including "0"), `SystemCount` 30/40/50 (review default40), exactly two distinct `HomeCivilizationDefinitionIds`. The caller supplies existing admitted civilization definitions; IDs must match that catalog. Catalog/home-list enumeration order does not affect generation. Candidate assignment is not sovereignty or live enemy knowledge.

The DTO in Contracts defines exact property names. Regions have NodeId, DisplayName, optional SystemId, Center, Boundary. Systems have SystemId, DisplayName, NodeId. HomeCandidates have CivilizationDefinitionId, SystemId, NodeId. Connections reuse NavigationConnectionDto with explicit Kind=Normal and BaseTraversalCost=1. No new ship, faction, body or resource definitions are invented. Generic system/region labels identify generated geography only.

## Geometry

Portable integer presentation units: origin upper left, X right, Y down. Width1,000,000; height600,000/800,000/1,000,000 for30/40/50 systems. Units have no light-year, screen-pixel or movement meaning. Fit/pan/zoom transform these coordinates uniformly. Generate exactly twice as many regions as systems, with real empty regions.

Sites are independently jittered within bounded cells. Exact rational Voronoi clipping produces convex irregular region polygons; vertices round once to shared integer points. Boundary lists are clockwise in screen coordinates and omit a repeated closing vertex. Center is the generating site and the system's map position if present. The generator owns sites, polygon boundaries, associations and adjacency; presentation must not reconstruct alternative regions. Edges connect regions sharing a nonzero boundary segment. Travel costs are declared independently of site distance/region area; these provisional unit costs do not alter old weighted-navigation content.

Candidate homes use two system-bearing regions with maximal graph distance, with deterministic ID tie breaking. Minimum separation is verified. They are unoccupied candidates, not colonies, borders, fleets or contacts. Half the cells have no star; do not fill them with decorative systems.

## Persistence and validation

Separate document identity: format1, profile `generated-galaxy-review-v1`, generator `irregular-regions-v1`, tuning `provisional-review-30-40-50-v1`. Seed/settings and the entire generated result persist with a canonical SHA256 digest. Open bounds and validates the input, regenerates the exact supported version and compares all persisted content. Unknown versions or corrupt/mismatching data fail explicitly. Retain old algorithm versions or reject them; never silently regenerate an old map using a changed algorithm.

Portable code accepts typed DTOs and performs no filesystem/JSON I/O. Hosts serialize all DTO fields, bound input file size, and call Open before replacing their current map. Use a distinct generated-map namespace/extension and atomic host writes; campaign save slots and existing profile dispatch stay untouched. A failed Open returns no replacement and cannot mutate an existing review. Detached exports cannot mutate its stored authority.

Save14, campaign wrapper profiles1–6, Content10/11, science-engineering-v1 and ship-design-compiler-v1 remain unchanged. No migration, default activation, main merge, or old package replacement.

## Acceptance

Validate30/40/50 counts, genuine empty regions, connected unique adjacency, stable identities and associations, convex non-overlapping layout/shared edges, separated candidates, bounded invalid/extreme settings, repeated/reordered inputs, seed variation, export/open identity and failure isolation. Exercise representative many-seed fixtures and retain exact sample hashes for cross-platform comparison. Existing campaign tests/goldens must pass unchanged. Unity/native acceptance belongs to Secondary.
