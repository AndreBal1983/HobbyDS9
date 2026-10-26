# Owned navigation endpoint lookup

WeightedNavigationGraph indexes both directions of validated copied edges with ordinal nested keys, after existing topology copies and public snapshots. Edge retains null/unknown/self/case semantics and copies every result. No caller DTO or dynamic cost is cached. Existing validation, snapshots, route order, knowledge and live effects remain unchanged.

Focused old-scan all-pairs tests include reverse/null/unknown/case, caller and copy-out mutation, live effect mutation/expiry and exact nonadjacent error. Constructor work and memory increase by O(E); no speed or native benefit is claimed.
