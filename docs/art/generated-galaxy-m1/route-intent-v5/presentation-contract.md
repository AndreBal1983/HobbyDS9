# M1 route intent and natural layout / early art handoff

23 October 2026. **Owner-authorized bounded follow-up:** direct choices among equal-cost routes, transit without automatic star visits, ordered Shift+right-click waypoint drafts with undo/clear and costs, and natural star variation decoupled from the near2×quiet-area target. **Proposed:** the UX/presentation defaults below. **Published early contract:** `12ce1d5d188b22a196272a524ae49fb614b9d6c7`. **Route API implementation:** `c936130740c66c90efd235474de8f6152d754657`. **Pending:** committed route-plan fixtures, natural-generator implementation/samples, frontend implementation and acceptance. This document is not evidence of implemented behavior.

The new Art checkout starts at `bcd13ee01a763acdb8a89b55c6872b9e889d4471`. Preserve the [safe913checkpoint](E:/HobbyDS9/docs/scaled-galaxy-m1-checkpoint-2026-10-23.md), its executable/ZIP and saves, and the [delivered scale gallery](E:/HobbyDS9/artifacts/art/generated-galaxy-m1-20261023-scale-v4-1/review.html). Do not alter old renderer recipes or published evidence. [New presentation tokens](../../../../art/generated-galaxy-m1/route-intent-v5/tokens.json) are an early proposal, not a portable API.

## Draft controls and intent

Keep the map dominant and the route draft in the bounded inspector. Its header is **Route preview**, with a persistent Start row, an ordered list of user pins, per-leg costs, total cost and reachable **Remove last** / **Clear route** controls. Show a short instruction: “Right-click sets the destination. Shift + right-click adds a waypoint.” These remain map-review previews, not fleet commands or POI actions.

| Input | Proposed preview behavior | Visible consequence |
| --- | --- | --- |
| Right-click with a valid start | Replace destination/queued pins with the chosen destination | One destination row; recomputed first leg and total |
| Shift + right-click | Append a pin; the former final destination becomes a via pin | Ordered via rows and a new final destination |
| Remove last | Pop the last user pin; the previous via pin becomes destination | Recompute affected leg/total; disable when no pins |
| Clear route | Clear queued pins and preview lines, retaining the explicitly chosen start | Empty preview instructions; no campaign mutation |
| Click a draft row or pin | Inspect/highlight that pin and leg | Does not silently change start or reorder the queue |
| Use as route start | Explicitly change the start according to the frontend contract | Recompute against that start; no implicit rebase from ordinary inspection |

The published query limit is64ordered targets, including the final destination. Disclose that bound rather than silently dropping pins. Repeated nodes and zero-length legs are valid ordered targets; do not deduplicate them or treat zero cost as a failure. Keep the list scrollable, with at least the active leg and destination reachable at1280×720.

## Transit and pinned system locations

Only **user-chosen** start/via/destination positions receive route-intent markers. Intermediate graph nodes are corridor constraints, not numbered visit stops. A star remains an ordinary star glyph when the route passes through its region; do not add arrival rings, scan flashes, visited ticks or POI action labels.

Distinguish these concepts in data and text:

- A transit-region pin constrains passage through a named region. Display “Via Region…” with an optional “Contains System… · transit only” subline. Whether it pins a local position or only a corridor node must come from the exact contract.
- A user-pinned star position can legitimately constrain the line to that chosen location. Label it as a location/preview target; it still does not order a visit, survey or other action.
- The released route target name is **SystemLocation**, retaining enum value2 and requiring the exact associated SystemId. It is only a system-position draft target: label it “System location”, not “visit draft”, and pin the actual star position. No actual visit, scout, survey or arrival semantics are introduced. Input-to-intent mapping remains the frontend's explicit UX decision.

Retain the exact supplied start/end anchors and any explicit pin positions. Do not invent a physical ship position in a review that only supplies a node/site. Where the current API offers only a node anchor, describe that honestly; do not draw an arbitrary mouse position as authoritative.

## Leg state and cost

Show every user pin in order, with each leg's returned crossing count/cost and the returned or validated total. Crossing count and cost are separate concepts even when every current M1 edge costs1. Do not infer travel duration, fuel, danger, sensor knowledge or a nebula surcharge. Minimum lawful cost remains primary; directness is a deterministic tie-break, not a replacement travel objective.

A failed leg remains identifiable in the user's retained queue, but the published failed plan returns **no successful prefix legs, steps or aggregate costs**. Clear the current route line and show an unavailable total; do not present earlier rows as returned valid legs, bridge the failure, silently skip a pin or stitch stale legs into a new plan. Convert zero-based FailedLegIndex to a human leg number; a missing index indicates a query-level error. A previous complete preview may only be retained as separately labeled stale host state if its explicit contract permits it, never as this failed result. Distinguish pending recalculation from an actual invalid result.

Any blocked/unknown/access case in art evidence must be an explicit portable seam fixture, clearly separated from normal full-map output. This increment adds no campaign access, fog, exploration, diplomacy or AI behavior. The lawful-filter seam belongs before each search; styling cannot grant access or reveal unknown topology.

The map document remains authoritative generated geography. Do not insert a local waypoint queue into its canonical hash/export or campaign save. Review-draft persistence is pending an explicit host contract, including map identity and failed-load isolation. A different map identity clears/revalidates map-scoped origin/pins; reused ID strings are insufficient. Do not claim save/reload behavior before that contract and its actual tests exist.

## Contained transit drawing

For this initial binding, use the published **centreless ordered-midpoint chain only**. Ordinary transit must stop returning to each intermediate region centre/star. Preserve actual start/end and explicit anchors, then connect consecutive shared-edge midpoints through their intervening convex cells. RegionTransit is only a region constraint: absent a deliberate valid host-local pin, join incoming/outgoing corridor portions within that region without forcing its star/site. SystemLocation pins the exact associated star. This baseline is contained; it is not globally taut, geometrically shortest or minimum-bend. Any later string-pulling/rounding requires its own verified ordered-corridor contract, not mere containment somewhere in the polygon union.

Keep every accepted positive shared edge, including short crooked passages. Corner touching is not a new connection, and a drawn shortcut must not appear to omit an accepted crossing or visit a blocked/unknown region. Preserve exact raw corridor/portal diagnostics alongside the visible line. For multiple legs, validate each leg in its own order; repeated regions across different legs are not automatically errors.

Optional gentle rounding comes later and only with verified containment/crossing order. Do not require a random spline or smoothness at the expense of lawful route geometry. The visible path is presentation; it never changes the returned graph nodes, per-leg cost or total.

## Markers and legibility

Start uses a small hollow square, via pins use numbered hollow markers and the final destination a distinct double ring. These differ from lavender unoccupied-home brackets, ordinary stars and environmental feature markers. Emphasize the selected leg slightly; other legs of the same current successful plan may use lower contrast. A failed current plan supplies no valid-prefix lines or costs. Automatic transit nodes get no numbered pin glyphs.

Use the existing semantic zoom and nearest-received-pointer policy. When pins coincide or labels overlap, the ordered inspector remains the reliable way to select them. Do not move the actual pin/star positions to separate icons. Numbering is stable in queue order; changing zoom, selection or hover must not renumber the route. Preserve pan/drag suppression, Fit and keyboard focus. The known narrow-overview subpixel ambiguity is resolved by nearest-pointer policy and zoom, not by relaxed alignment tolerances.

## Two controlled route comparisons

1. **Drawing only:** hold the actual map, start/end, ordered node sequence, portals and costs fixed. Compare the previous centre-returning line with contained transit drawing. Report actual intermediate star positions no longer forced onto the line; do not claim new route choice from this comparison.
2. **Route choice:** hold map, endpoints, waypoint anchors and allowed-navigation context fixed. Compare the actual old ordinal tie choice and new direct tie choice from Primary's supplied snapshots. Confirm equal minimum primary cost before describing a tie improvement. Keep declared secondary metrics separate from travel cost; if filters or costs changed, it is not a controlled equal-cost comparison.

Use the owner's supplied screenshots only as visual examples; obtain exact map identity and endpoints for numerical assertions. Do not invent an unseen fifth screenshot or assert that a geometrically longer route is wrong when it saves a crossing. Art does not implement another pathfinder to manufacture a baseline.

## Repeated waypoints and per-leg geometry

The independently checked single-simple-corridor painter is not evidence for flattening a multi-leg queue. Paint every leg with its own actual start/end anchors, then retain the shared user waypoint anchor between the legs. A→B→A must visibly reach the deliberately chosen B anchor and return: the portal sequence P→B→P must not collapse to P→P just because the two legs cross the same shared edge.

Resolve a valid host anchor for each explicit user pin, including a RegionTransit waypoint, and keep it consistent between adjacent legs. A deliberate waypoint/site anchor can be fixed by the host; ordinary **automatic** intermediate region centres remain unpinned. Never discard an explicit leg boundary or waypoint through global vertex/path deduplication. Repeated target IDs and zero-length legs stay as separate ordered rows with their actual returned costs. A zero-crossing leg may still have distinct supported local anchors inside one cell; do not invent a travel-time consequence.

Bounded geometry checks must include A→B→A, A→B→B→C, a zero-length first/last leg and repeated shared portals. Verify each leg's ordered positive boundary crossings and anchors, with no line bridge or hidden cost loss after remove-last/replacement. Cross-leg containment in an unordered polygon union is insufficient.

## Exact query and version binding

Naming status: early `12ce1d5` used SystemVisit and InvalidVisitTarget. The route implementation at **`c936130740c66c90efd235474de8f6152d754657`** now uses **SystemLocation (value2)** and **InvalidSystemTarget (value7)**. Product labels describe locations only; no visit/scout semantics are introduced.

The c936route implementation adds FindRoutePlan to the existing Generated, Environment and Scaled review wrappers while retaining old FindRoute unchanged; the Natural wrapper is still a separate pending implementation. Queries must supply MapContentHash, PolicyVersion, OriginNodeId, ordered Targets and an explicit Filter with known permitted node/connection IDs. Missing filter lists reject; empty lists grant nothing. Full-map review deliberately supplies all map IDs. Persisted draft filters are never trusted permission; re-supply/revalidate on reload.

Policy `minimum-crossings-centre-span-v1` minimizes unit crossings, then summed floor-integer Euclidean **centre-hop spans**, then the ordinal whole path. GeographicLengthUnits is that deterministic proxy, not visible midpoint-line length, physical distance/time, minimum bends or shortest taut-path length. Do not label the result the straightest possible drawn route. Success contains ordered Legs, each Route excludes its origin, plus aggregate Steps/cost and map/policy stamps. Retain leg boundaries and repeated manual targets when using flattened steps. Atomic failure supplies no success prefix or total.

Natural format4 / `natural-environment-regions-v5` / `provisional-natural-star-field-v1` has Settings.Map and Tuning.Environment, with persisted StarGroups. Its new dedicated API remains planned at this early pin; no new samples are yet available. Do not apply the older format3 direct-Settings adapter or claim generation from type declarations. StarGroups are generation provenance, not geometric/graph connected components; an Isolated provenance label alone is not proof of nearest-neighbour isolation.

## Natural-layout evidence

Wait for a new explicit generator/version and actual seeded250exports before1000. Never jitter old triple markers, hide every third star, redistribute glyphs after topology or relabel a rendered layout as new generated data.

Compare old/new at the same seed, system count, extent, camera and rendering style, while prominently recording **different map identities/generator versions**. Reset selection/drafts when identities differ. This is not a same-feature causal fixture unless Primary supplies one; a new distribution can legitimately move stars, features and region identities.

Report actual quiet/disturbed area means/counts, route crossing statistics and candidate-start/traversal metrics from each version. Preserve the soft2×area target as a generator outcome; do not promise unchanged pacing or exact area ratios after revising distribution. Crooked junctions and genuinely sparse navigation space remain visible.

For arity/spacing variation use Primary's explicit generated group data if supplied. Otherwise disclose geometric nearest-neighbour/group proxies and their fixed measurement scale; do not invent membership or label inferred groups as authoritative pairs/triples. Show the actual point distribution at overview and matched close crops so apparent improvement does not come from different zoom or label suppression.

## Bounded acceptance and dependencies

First use actual250fixtures for a compact three-size set: one-leg preview, two/three ordered pins, selected leg with cost breakdown, remove-last, clear, transit past an unpinned star, close crooked/short-edge passage, and source-backed before/after comparisons. Reuse unchanged shell/feature evidence. Then use actual1000data for overview, dense local selection/waypoints and one corridor stress case. Add error/persistence views only when their real contract/fixtures exist; no invented failures to fill a gallery.

Check source/sample hashes, exact accepted node order, per-leg/total costs, explicit anchors, ordered boundary crossings, unchanged map JSON, undo/clear state, normal-versus-Shift right-click, post-pan selection, keyboard focus and three-size label reachability. Test current included-usage policy before chunks and at most five minutes apart; stop on90%, denial/unknown status or purchased-credit decrease.

Dependencies now published: exact64-target query/filter shape, typed intent, atomic result/error behavior, centre-span tie proxy, centreless midpoint baseline and natural format4 shape at12ce1d5. Remaining dependencies: committed actual route fixtures, natural implementation/maps/reports, host-local point/intent binding and draft persistence, plus their verified source pins. Root relays this handoff to its existing frontend agent. Art owns CPU/browser source and evidence only, with no Unity/native/GPU/paid-generation lease or new coordinator. Final comparisons wait for actual committed inputs. No broad4X integration or automatic M2 work.
