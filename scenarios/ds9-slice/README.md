# DS9 strategic slice content package

[scenario.json](scenario.json) implements the `ds9-strategic-slice-v1` identity/graph skeleton: three civilizations, six nodes, initial ownership and five undirected edges. Federation is Human; Cardassian Union and Dominion are AI. The Bajoran Wormhole–Gamma Exit edge is explicitly tagged Wormhole. All edges cost one traversal step.

Labels live in definitions, separate from state and simulation rules. No colonies, population, economy, research, starting ships/fleets or production data exist yet. No legacy content, artwork or audio has been imported.

The host assigns ContentHash as lowercase SHA256 of the exact UTF-8 file bytes, including whitespace/labels; the JSON excludes its own hash. ContentFormatVersion remains 1. Initial state is Planning and RulesVersion is phase-1-3b-v1. Save format 4 persists campaign Phase, civilization controllers and system ownership; reload requires the matching content catalog. Future rules-only canonical hashing will require deliberate versioning.

`dotnet run --project src/HobbyDS9.Headless -c Release --no-build` validates the bundled file and reports counts. A different file path can be passed after `--`. This does not run strategic turns. See [the architecture](../../docs/architecture.md#fixed-ds9-themed-scenario) for later content plans.
