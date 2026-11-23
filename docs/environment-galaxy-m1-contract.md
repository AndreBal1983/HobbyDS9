# Environment-first generated galaxy M1

20 October 2026. Owner-authorized M1 correction: stars, black holes and nebulae shape topology before region construction. This is a map review, not physics, sensors, hazards, exploration progression, AI or campaign activation. All density/spacing/count settings below are **provisional review tuning**.

## Exact binding and compatibility

New `EnvironmentGalaxyApplication.Generate(EnvironmentGalaxySettingsDto, admittedCivilizations)` and `Open(EnvironmentGalaxyDocumentDto, admittedCivilizations)` return `EnvironmentGalaxyReview`, exposing detached `Export()` and graph-only `FindRoute(originNodeId,destinationNodeId)`. Existing GeneratedGalaxyApplication and format1 v1/v2 Open are unchanged. Hosts dispatch on outer FormatVersion; do not pass the new Layout through a legacy document loader.

Format2, profile `generated-galaxy-review-v1`, generator `environment-first-regions-v3`, tuning `provisional-environment-influence-v1`. The full canonical hash covers all settings, resolved features, tuning, layout and region influence observations. Open replays the exact declared supported algorithm and compares complete content. Unknown versions, mixed contracts and corrupted content reject without replacing current review; no migration or old-map re-layout.

Settings.Map is the existing seed/30–40–50/home-definition-ID DTO. Set StellarInfluenceEnabled, BlackHoleInfluenceEnabled and NebulaInfluenceEnabled explicitly; normal review generation uses all three true. These persisted switches generate controlled same-feature comparisons. They do not hide features, alter campaign sensors or confer knowledge. Separate frontend visibility toggles must not regenerate authority.

## Geometry and feature contract

Layout exposes Width/Height, Regions, Systems, Connections and HomeCandidates using existing row DTOs. Units and orientation remain integer presentation coordinates, upper-left origin, +X right/+Y down; uniform Fit, no distance-derived cost. Region polygons remain clockwise with implicit closure. IDs are scoped to content identity. Two times as many regions as systems retain genuine empty cells.

Features have FeatureId, Kind (Star1/BlackHole2/Nebula3), DisplayName, Position, RadiusX, RadiusY, DensityWeight and nullable SystemId/NodeId. Footprints are axis-aligned **generation influence ellipses**, not physical object sizes, sensor coverage, damage zones or territory. Stars have SystemId and a mandatory NodeId at their original Position. Black holes also have a mandatory empty-region NodeId. Nebulae span multiple cells and have no single NodeId/SystemId. No labels imply live ownership or hidden enemy facts.

Generate star anchors first, followed by black-hole anchors and nebula footprints, using separate private deterministic stage streams. Star positions are immutable inputs to topology; they are never assigned to arbitrary later cell centres. Retain each star as a mandatory Voronoi site. Additional empty-region sites are sampled globally using a capped integer density field and separation guard, followed by exact clipping/shared-boundary adjacency validation. No feature changes mid-attempt; retries advance only the site stream. Candidate homes are selected from the final connected system graph.

RegionInfluences records each site's BaseDensity, sorted nonzero feature Contributions (FeatureId/Amount), and capped TargetDensity. This is a sample of the input density field at that site. It explains placement pressure; it is **not** a claim that one feature uniquely caused a particular polygon, nor an exact inverse-area formula. Mandatory star/black-hole sites exist even when their influence is disabled.

## Initial provisional policy

- BaseDensity100; MaximumDensity1000; summed overlaps clamp to that cap.
- Star anchors separated by60,000 units; all navigation sites by30,000; outer margin15,000. Fixed rectangle extents are unchanged.
- Two black holes and three nebula footprints. Seeded star bands use weight/radius pairs120/70,000,200/95,000,280/120,000; these are normalized influence values, not inferred physical masses. Black holes use weight650/radius160,000. Nebulae use weight420 with seeded RadiusX180,000–240,000 and RadiusY100,000–160,000. Feature instances persist their exact parameters.
- Integer elliptical falloff: outside either axis radius contribution is zero; otherwise compute `q = floor(dx² * 1,000,000 / RadiusX²) + floor(dy² * 1,000,000 / RadiusY²)`. For q below1,000,000, contribution is `floor(weight * (1,000,000 - q) / 1,000,000)`, otherwise zero. Acceptance compares an integer draw below MaximumDensity with capped target density. Dense/disturbed areas therefore tend to gain more nearby sites and smaller cells; sparse areas tend to larger cells/fewer crossings.
- Each star/black-hole anchor has a bounded `SystemCount * 1024` candidate budget. Each complete extra-site placement attempt has a pooled `RegionCount * 1024` candidate budget and at most8attempts; geometry retries retain every feature unchanged. These constants persist in Tuning. Star, black-hole, nebula and extra-site streams are isolated from campaign RNG and each other. There is no randomized late system population stage: systems are the seeded star anchors. Toggling one influence leaves every feature's position/parameters and other feature inputs fixed, while redistributing the fixed total region budget.
- All ordinary edges still cost1. No distance surcharge or duplicate disturbance slowdown; existing campaign movement/hazard definitions remain untouched.

Star/black-hole/nebula identities here are explicit review feature kinds, not new ship/body/resource/gameplay definitions. No physical mass or general-relativity model is claimed.

## Acceptance and delivery

Check independent geometry/connectivity/non-overlap, exact star/system/node references and anchor preservation, field contributions/cap arithmetic, deterministic repeated/reordered inputs, bounded many-seed validity, full feature-layout roundtrip and failed replacement isolation. Controlled feature-on/off comparisons hold other inputs fixed and measure the declared local-density direction; report redistribution caused by the fixed budget. Retain old format1 v1/v2 exports exactly.

Supply actual map/feature/route samples and comparison fixtures for Art/Secondary. Render footprints from DTOs, preserve marker alignment/picking priority and distinguish feature visibility from generation switches. Build/CI/visual/native gates remain separate. Stop for M1 owner review; no M2 work.
