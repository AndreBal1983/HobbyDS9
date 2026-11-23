# Generated-map route presentation through shared boundaries

23 October 2026. This increment changes the review renderer only. It preserves accepted graph paths, crossing costs, crooked junctions, all positive-length boundaries, geometry documents and old-format Open behavior.

The former centre-to-centre chords could pass through unrelated cells. For the environment-v3 seed42/50 map, the accepted039→052→026→025→005→013→084→021→044 route costs8, but some chords visually entered077,065,024and082.

The renderer now caches centre→shared-edge midpoint→next centre points for each accepted step. A portal matches the exact integer endpoints supplied by both polygons; it uses doubled integer coordinates so a half-unit midpoint is retained without rounding. There is no epsilon match, corner snapping or minimum edge length. Missing positive shared edges fail explicitly instead of falling back to a misleading chord. This helper consumes an accepted path and never performs navigation search.

Validated polygons are convex and contain their centres. Each half-segment therefore remains inside its intended polygon, and the midpoint lies strictly within the actual shared edge. The guarantee concerns the route centreline; ordinary screen-space line thickness is cosmetic. Graph-link debug overlays remain abstract centre-to-centre links.

CPU acceptance proves exact endpoint half-plane containment and checks interior ownership for every connection in the seed42/50 map and all seven retained v1/v2 samples. It includes a one-unit near-corner edge, half-unit midpoint, reverse direction, zero-step route and rejected corner-only contact. The reported route remains8crossings and byte-identical export. Existing engine pixel checks now inspect both portal half-segments and retain the marker tolerance; engine execution remains a separate receiving gate.

This increment does not implement expanded250/1000settings, new density tuning, campaign movement, sensors, AI or M2. Those contracts remain separate.
