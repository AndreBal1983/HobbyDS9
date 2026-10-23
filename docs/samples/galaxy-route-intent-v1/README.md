# Actual map-review route plans

`examples.json` is emitted by the real portable APIs on the retained250-system seed42format3map, hash006ab67d08f1cd3c3d9e73da2fa335fb5e4ba1cee1acd824d63dbd44135c1937. The unchanged map is [here](../scaled-galaxy-v4/seed-42-systems-250.scaled.json). This compares routing policies on one fixed topology, separately from frontend midpoint-chain painting and later natural generation.

Reproduce: `dotnet run --project src/HobbyDS9.Headless -c Release -- --galaxy-route-intent-examples`. Complete typed requests include map hash, policy, target intents and explicit full-map permitted sets. Results are real immutable API results, not an Art pathfinder. LegacyCentreSpanUnits is measured by restricting the new metric query to the exact legacy edge sequence; the host verifies that sequence and equal primary cost before emitting the comparison. It is not smoothed-corridor length.

Examples include a normal old/new equal-primary-cost comparison, three ordered mixed transit/system-location targets, A→B→A, A→B→B→C with a zero-length leg, and a blocked second leg that atomically returns no prefix/cost. Use each successful leg independently when painting; do not flatten a return leg into a zero-length portal segment. SystemLocation is draft endpoint intent, not an executed visit/scout order. Intermediate system regions remain ordinary transit.

No gameplay, saved campaign, actor access/knowledge, topology or old FindRoute behavior changes. Separate frontend draft persistence must revalidate map identity, policy and every leg/filter on load.
