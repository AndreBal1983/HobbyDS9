# Route intent frontend: bounded receiving plan

Status: source/CPU work only, separate from accepted913a427 checkpoint. No campaign integration, engine acceptance or new executable is claimed.

Base:913a4277d6368d795512f067db0c68c3fe629258. Branch: `codex/galaxy-route-intent-frontend`.

## First geometry increment

Draw start centre, one exact positive shared-edge midpoint per accepted crossing, and final centre. Do not insert intermediate region centres or star positions. Each portal-to-portal segment belongs to its intervening convex region: exact integer half-plane tests on both endpoints independently prove containment of the whole segment. A midpoint lies strictly inside every positive-length edge, including a one-unit edge. Corner-only contact remains rejected. No edge-length cutoff, tolerance relaxation, curve, graph search or map mutation is introduced.

This stage removes intermediate POI detours; it does **not** claim a globally taut or shortest geometric line. A later corridor-shortening/curve stage needs its own proof, especially when an exact funnel touches a vertex. No epsilon nudge that silently changes the accepted corridor is allowed. Explicit waypoint legs join at their declared anchors; automatically traversed systems are not visits.

## Draft receiving contract

Normal right-click replaces the destination queue. Shift+right-click appends a target, retaining the former final target as a waypoint. Number targets in order, disclose each leg's crossings and the total, and provide remove-last/clear controls. Distinguish region transit from an explicit system/POI target without inventing a scout or campaign action. Map replacement clears map-specific drafts.

Primary supplies deterministic cost-first routes and ordered-leg validation; frontend does not duplicate portable search. Required result: ordered legs with origin/target IDs, accepted step IDs, cost and failure reason, linked to the map identity. Preserve failed middle legs visibly rather than rendering an apparent shortcut. Companion persistence, if included, must be versioned and separate from generated map hashes. Host integration awaits Primary's committed API.

## Verification boundaries

Keep old seed42/50's actual cost8 and accepted route as an unchanged search baseline for the presentation-only increment; prove exact corridor containment over full routes and exclude intermediate centres. Test zero-length/single-hop/reversed routes, short positive edges and corner rejection. Later portable tie-breaking may intentionally change old equal-cost region sequences: report those before/after results honestly and separately from new natural-generation samples.

Existing engine image assertions assume centre/portal alternation and need a reviewed adaptation before any engine run. No GPU/native/build lease is granted by this plan. Owner saves, frozen913 package/ZIP and old checkout remain untouched.
