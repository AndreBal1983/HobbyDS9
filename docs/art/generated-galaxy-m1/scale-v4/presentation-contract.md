# M1 topology and scale / early presentation handoff

23 October 2026. **Owner-authorized next M1 increment:** preserve crooked junctions, show accepted routes through their actual shared edges, investigate quiet-empty cells averaging near twice system-cell area with disturbed-space exceptions, review250 systems then validate1000 at a5:4 extent. **Proposed:** the visual policy and thresholds below. **Published contract:** Primary `24608de10ed8bc4e54552f9419dd39c67fa7a948`. **Actual portable sample source:** `aa434036462c08eceb323aa581e539d307ca65e0`. **Pending:** separate native Unity performance/input acceptance and owner review.

This handoff is design/source preparation only. It does not establish public support for larger maps, manufacture geometry, or claim runtime behavior. Preserve the delivered `e47fcc944283db0ee223ec3d23ad78cf87942ae6` art and all immutable review bundles. CPU/browser work only; no engine/GPU/native/desktop lease, paid generation or M2 work. Check the owner's included-usage policy before bounded work chunks and at most five minutes apart; stop on90% or any unavailable/denied/paid-credit-decrease condition.

Proposed reusable values: [scale presentation tokens](../../../../art/generated-galaxy-m1/scale-v4/tokens.json).

## Exact new contract dependency

Primary's `docs/scaled-galaxy-m1-contract.md` and `ScaledGalaxyContracts.cs` at `24608de10ed8bc4e54552f9419dd39c67fa7a948` define format3 / `environment-clustered-regions-v4` / `provisional-quiet-empty-area-v1`. Settings are now directly on `Settings`, without the older format2 `Settings.Map` wrapper; environment tuning is under `Tuning.Environment`. Layout/feature/influence row shapes are retained. A new read-only browser adapter must respect this version dispatch rather than reinterpreting old exports. The early handoff originally preceded implementation. Actual supported candidate Generate/Open exports and reports are now pinned in the [delivery handoff](handoff.md); this browser presentation does not establish native Unity acceptance.

The declared quiet-empty classification is per-site: no system, not a mandatory black-hole node, and no enabled black-hole or nebula contribution reaching `Tuning.QuietDisturbanceThreshold`. Stellar influence alone does not classify a cell as disturbed. Its entire polygon is not thereby asserted physically quiet. Actual component/area diagnostic samples are now hash-pinned with independently checked report parity.

## One connected geography

Render Primary's exact new positions, polygons, adjacency, footprint extents and explicit costs. Fit the complete5:4 extent with uniform scale and honest letterboxing; do not split it into a visible5×4tile grid. Interior subdivisions used for culling/picking are invisible implementation details, not navigation sectors or independent maps.

Keep every crooked/offset junction and every positive-length shared edge, including edges shorter than a screen pixel. Do not snap corners, merge small polygons, introduce corner-only adjacency, smooth the boundaries, filter short edges or displace stars for legibility. Fading or culling an off-screen stroke does not remove its graph connection.

## Routes through shared-edge portals

The current direct centre-to-centre chords can appear to cross regions that are absent from the accepted node sequence. Replace only their presentation with an exact portal chain. The frontend explicitly owns presentation-only portal extraction from the existing detached polygon DTOs, as confirmed by Root and Primary's contract. No new portable portal DTO is required. Derive the portal from the exact shared boundary endpoints of the two accepted adjacent polygons, with no epsilon, tolerance-based nearest-edge guess or short-edge filter. Store doubled midpoint coordinates to preserve halves.

For route `A, B, C`, the initial truthful construction is `centre(A), portal(A,B), centre(B), portal(B,C), centre(C)`. A portal midpoint lies on the actual common edge; its two adjoining segments stay within their respective convex cells. Keep half-coordinate midpoints exactly when endpoint sums are odd; do not round them to integer map units or snap them to a vertex. The exact portable coordinate representation remains Primary's decision.

Portal points are **drawing vertices, not extra route steps, movement costs or waypoints chosen by the player**. Keep the accepted node order, cost, endpoint association and inspector count unchanged. Distinguish route and selected region with the existing amber/ice hierarchy. Overview shows one quiet amber path and endpoints; close inspection can show a small hollow portal tick and the crossed shared edge. Do not add numbered labels at every portal across a large map. Selected/hovered passage details may show its two real neighbouring regions and declared cost.

No unconstrained spline, funnel, shortcut or minimum-edge policy in this increment. Any later smoothing must prove it remains inside the ordered corridor. The original eight-crossing route from the assessment remains useful historical evidence; it is not a license to fabricate an equivalent path in new samples.

## Semantic zoom and labels

Use screen-space crowding, not fixed multiples of Fit alone, to decide label density. A1000-system overview and a250-system overview have different projected cell spacing. A proposed indicator is the median projected region bounding-box diagonal among currently visible real polygons. It is presentation metadata only and never a distance or travel rule.

| Level | Initial proposed indicator | System labels | Ordinary boundaries |
| --- | --- | --- | --- |
| Overview | Typical visible region diagonal below40px | Up to12: selection, route endpoints and candidate starts first |0.75px,0.24 opacity |
| Regional |40–90px | Up to36, stable priorities then collision suppression |1px,0.28 opacity |
| Local | Above90px | Up to80, still collision-checked |1px,0.32 opacity |

These are starting art values, not approved performance budgets. Further cap labels by available viewport area; at1280×720 the cap may be lower. Selected/route-endpoint labels have first claim on the budget, but must use a safe callout placement rather than overlapping another priority label. Use a deterministic ordinal ID tie-break after semantic priority. Preserve a clean top-toolbar and bottom-legend safe area, bounded inspector and screen-sized text/strokes.

All actual visible stars remain point glyphs when ordinary names are hidden. Do not replace them with invented clusters or make label suppression look like missing systems. Feature footprints remain factual generation-influence ellipses clipped to the full map extent. Prefer selected-feature outlines over an all-star influence web at1000 systems; an explicit all-influences diagnostic toggle may be expensive and requires profiling. Do not silently convert a full overlay into a different visual metric.

Allow enough zoom range to reach a useful local view at1000 systems; the old6×Fit ceiling is not assumed sufficient. A proposed starting ceiling is32×Fit, bounded by actual usability. Zoom around pointer/selection and preserve a single content-box transform across painter, markers, pointer conversion, portal route, hit testing and culling. Returning to Fit restores the whole extent and resets map-scoped inspection only after a successful document replacement.

## Picking and viewport culling

Transparent per-star13px hit discs will overlap at large-map overview. DOM/control paint order must not decide which star wins. Use a presentation-only spatial index of real visible centres and nearest-distance picking inside the screen-sized hit radius, with ordinal ID only as an exact-distance tie-break. Star centres have priority over candidate brackets and feature strokes. Then consider black-hole/nebula centre targets and candidate stroke targets; regions remain independently inspectable. Provide the Environment index and candidate/feature inspector actions for genuinely coincident or occluded centres.

Cull off-screen markers, labels and polygon strokes using their exact projected bounds plus a small screen margin. Retain the complete document and graph; totals continue to describe the real full map. Do not create1000 persistent UI controls merely to hide most of them if measured performance warrants a single indexed interaction surface. Preserve keyboard access to selected/visible items and a route/feature list that does not depend on reaching thousands of tab stops. These are frontend implementation recommendations, not an art claim that Unity is optimized.

## Quiet space, disturbed exceptions and corridors

The near2:1area relationship is a generation target, never a painter scale factor. Only show quiet/disturbed labels and corridor highlights from Primary's declared classification and measurements. Nonzero influence alone must not be treated as the classification rule without a contract. Do not recolour cells to manufacture an apparent two-thirds empty area or relocate stars after topology.

Quiet space should read as breathing room: faint neutral boundaries, no decorative filler stars and no default faction fill. Disturbed empty regions may be small and retain the real black-hole/nebula cues. If actual connected quiet-space component IDs/links are provided, an optional corridor view can highlight those exact member regions and adjacency crossings. Do not invent a corridor spine, guarantee a passage or name a geographic corridor from an unvalidated visual gap. If only summary diagnostics are supplied, show the summary and allow ordinary region inspection instead of fabricating a spatial overlay.

Report area means/counts for system, quiet empty and disturbed empty separately, with the exact source definition and seed/version. Distinguish whole-map empty/system ratio from quiet-only ratios. Show corridor connectivity and route crossing statistics only when measured from the actual graph. A soft aggregate target may have per-map and disturbed-space exceptions; keep those visible rather than presenting every empty polygon as exactly twice a system polygon.

## Bounded evidence plan

First250-system review: up to12 states at1280×720,1920×1080 and3440×1440, for a maximum36 captures: overview, busy system selection, quiet-empty inspection, disturbed-empty inspection, black-hole details, nebula details, portal-route overview, short-edge/junction close-up, regional zoom, local zoom, Fit after pan, optional actual-corridor/diagnostic view. Substitute a clearly labeled summary if no corridor geometry exists; do not invent a view to fill the list.

Then1000-system stress review: up to6 states at the same three sizes, maximum18 captures: full overview, busy regional view, local selection, portal route, selected-feature influence, and resize/Fit recovery. Use actual new versioned samples; do not enlarge or cosmetically repartition the250map. At least one alternate seed should receive data/interaction checks; add screenshots only for a new failure or unresolved concern. Preserve earlier green evidence rather than rerunning it.

Check exact source/capture/sample hashes; supplied polygon/feature parity; nearest-centre collision cases; selection/route endpoint alignment at the existing native tolerance; keyboard/zoom/Fit; no inspector overflow; true node/edge/cost sequence. For every route hop assert a positive-length shared edge, portal membership and intra-region segment containment. Reproduce any original centre-chord misleading crossings as a before/after visual only when backed by its real pinned sample. No fake input, error suppression or relaxed alignment threshold.

Browser timings and screenshots are art evidence. Primary owns generator/public Open/compatibility/area-corridor diagnostics. The frontend implementer owns Unity/input/performance/native acceptance and the game package. Root coordinates their contracts and resource leases. Stop for M1 review, with no new default activation or subsequent milestone work.
