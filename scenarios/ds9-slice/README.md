# DS9 strategic slice content package

[scenario.json](scenario.json) implements the `ds9-strategic-slice-v1` identity/graph/economy starting state: three civilizations, six nodes, four colonies and five undirected edges. Federation is Human; Cardassian Union and Dominion are AI. The Bajoran Wormhole–Gamma Exit edge is explicitly tagged Wormhole. All edges cost one traversal step.

Labels live in definitions, separate from state and simulation rules. Owned homeworld colonies and neutral Bajor use economy.baseline, start with population 1000, capacity 2000, food 20 and materials 10; civilizations start with 50 credits. Economy and civilization research are implemented; starting ships/fleets and production remain absent. research.advanced-engineering costs 9, has no prerequisites, and carries the content label Advanced Starship Engineering. All civilizations start with reserve 0, no target and an empty unlocked list. No ship design is unlocked yet. No legacy content, artwork or audio has been imported.

The host assigns ContentHash as lowercase SHA256 of the exact UTF-8 file bytes, including whitespace/labels; the JSON excludes its own hash. ContentFormatVersion is 3. Initial state is Planning and RulesVersion is phase-1-4b-v1. Save format 6 persists campaign Phase, civilization controllers/credits, system ownership, colony state and civilization research; reload requires the matching content catalog. Future rules-only canonical hashing will require deliberate versioning.

`dotnet run --project src/HobbyDS9.Headless -c Release --no-build` validates the bundled file and reports counts. A different file path can be passed after `--`. This does not run strategic turns. See [the architecture](../../docs/architecture.md#fixed-ds9-themed-scenario) for later content plans.
