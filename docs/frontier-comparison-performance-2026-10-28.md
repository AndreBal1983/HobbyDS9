# Exact replay comparison investigation

28 October 2026. The comparison-local reflection metadata candidate is **parked**, not installed. Production `FrontierSame` remains unchanged. The original candidate and receipts are retained in the [source packet](E:/HobbyDS9/artifacts/frontier-same-local-metadata-source-20261028/CHECKPOINT.md); this is separate from the unavailable external R1 submission.

Thirteen new behavioral cases passed first on the unchanged helper, then on the candidate with identical test bytes. They cover shared-reference traversal, ordinal left/right getter order, actual overlapping comparisons, fresh values, list ordering/length, exception wrapping, short-circuit counts and existing enumerator acquisition/disposal behavior. Fifty existing Frontier/Growth/Repeat replay cases also passed on the candidate. These are focused checks, not a full-suite or Unity/AOT claim.

One fresh managed process per version loaded the same immutable official legacy Format 12 root through strict decoding and the complete normal loader. Both full exported roots matched all 2,644,379 input bytes, with content/input purity and actual dependency hashes verified. Only the Simulation assembly differed between the two processes; the runner and other dependencies were identical. The candidate's separately rebuilt Application assembly was deliberately not substituted. Both processes exited normally, without watchdog expiry.

| Observation | Original | Candidate |
| --- | ---: | ---: |
| Normal complete managed load | 10,328.5238 ms | 10,433.8211 ms |

This single ordered pair demonstrates no useful improvement. It does not establish statistical equivalence, a regression, allocation savings, a dominant hotspot or Unity/native Save/Continue performance. Concurrent owner workloads, JIT, GC and order effects were not controlled. No further benchmark loop followed. See the [actual comparison receipt](E:/HobbyDS9/artifacts/frontier-same-local-metadata-proof-20261028/comparison-result.json).

The production experiment was restored exactly to the original source; the behavioral regression tests remain. No replay, equality, rules, save vocabulary, resource policy or shared human/AI validation was removed. Mature Save/Continue latency remains open; the next optimisation needs evidence in the actual replay/validation path rather than an assumed benefit from metadata reuse.
