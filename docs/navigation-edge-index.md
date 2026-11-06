# Owned navigation endpoint lookup

WeightedNavigationGraph indexes both directions of validated copied edges with ordinal nested keys, after existing topology copies and public snapshots. Edge retains null/unknown/self/case semantics and copies every result. No caller DTO or dynamic cost is cached. Existing validation, snapshots, route order, knowledge and live effects remain unchanged.

Focused old-scan all-pairs tests include reverse/null/unknown/case, caller and copy-out mutation, live effect mutation/expiry and exact nonadjacent error. Constructor work and memory increase by O(E); no speed or native benefit is claimed.

The 6 November source increment also uses that existing endpoint index for `Neighbors`. It enumerates the selected node's adjacency keys instead of scanning every connection, then retains the same ordinal sort and newly allocated read-only list. Null, empty, unknown, differently cased and isolated queries retain empty results; caller input changes cannot alter the copied topology. No additional index, mutable state, dynamic cost, knowledge or validation result is cached.

The new neighbor behavior cases run first against unchanged production and then unchanged against the candidate, alongside focused existing edge, owned-topology and weighted route/movement checks. This is a source/CPU validation boundary. It establishes no mature Save/Continue latency, allocation, Unity or Windows-player improvement; the earlier reflection-metadata experiment remains [parked](frontier-comparison-performance-2026-10-28.md). All ordinary validation, ordered replay, final root equality, route tie ordering, current knowledge and live effects remain required.
