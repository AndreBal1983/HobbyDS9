# Generated galaxy M1 — Unity review entry

20 October 2026. Implementation candidate for owner review. Generated geography only; sensors, exploration progression and responsive AI are later milestones. Do not promote this candidate to a default campaign or advance automatically to M2.

## Source and authority

The isolated `codex/generated-galaxy-m1-frontend` branch starts at protected fleet source `cc8578c17e919dacaa2975b5cc757bee1bc8befc`. It incorporates portable contract `0fee81e`, generator implementation `771a3de`, and Art tokens `a1a2fa2` with their histories. See the [portable contract](generated-galaxy-backend-contract.md), [real sample documents](samples/generated-galaxy-v1/README.md) and [presentation handoff](art/generated-galaxy-m1/README.md).

The menu opens a separate full-map review without replacing the active fleet campaign or its unsent edits. Generate uses `GeneratedGalaxyApplication`, the existing admitted civilization catalogue and the requested seed/system count. Candidate starts use the existing Federation and Cardassian definition identities; they are unoccupied candidates, not borders, colonies or contacts. Bodies/resources are not generated.

Unity draws the supplied centres and polygon vertices with a uniform presentation transform. It never generates substitute regions, derives movement cost from pixels or runs a second pathfinder. Route previews call `FindRoute` and include the origin plus the returned steps. Same-node queries are valid zero-cost previews. No fleet command is issued.

## Player path

1. Choose **Generated galaxy review** from Start / Continue.
2. Enter an unsigned whole-number seed, select **30**, **40** (default) or **50** systems, then **Generate**.
3. Click a star, region or candidate bracket. Inspect actual identities, empty regions and graph neighbours. The region picker provides keyboard access to every region.
4. Drag empty space or use arrow controls to pan; wheel or +/− to zoom; **Fit** restores the whole map. Labels remain screen-sized and become more detailed as zoom increases.
5. Choose **Use as route start**, then right-click another region/star, or select it and choose **Preview route here**. Inspect graph cost and route steps; this is not an ETA or movement order.
6. Choose map slot 1–3 and **Export**. **Open** validates and reopens that separate document. Back to menu retains the review locally and leaves the fleet campaign intact.

Generating/opening over unexported work requires confirmation. Failed generation, export or open retains the current map. Successful replacement resets selections and routes because node IDs are scoped to a particular map identity.

## Persistence boundary

The Unity host uses a separate `HobbyDS9GeneratedGalaxyReviewOnly-v1` directory with `generated-map-1/2/3.json` slots. No campaign save namespace, package or latest-validated pointer is changed. Strict JSON decoding enforces a 2 MiB limit, depth 16, required fields, bounded arrays, exact scalar types, and rejects duplicate/unknown properties. Portable `Open` verifies the full versioned result before replacement. Atomic writes preserve a previous export on failure.

Generated data remains an isolated future campaign input. Export does not publish a campaign, create fleets or activate AI. Visibility is intentionally the full generator output, not actor-filtered knowledge.

## Acceptance status

Initial receiving checks: **241 adapter tests passed**, including 20 new generated-map host/viewport/codec/binding cases. The three actual samples match Primary's content hashes and route data through the real Unity-compatible codec. Tests cover failed/corrupt/missing file replacement, overwrite protection, map-size preservation, seed precision, same-node routing and viewport bounds.

Unity reference compilation, actual Editor control/render acceptance, native input and strict Windows IL2CPP delivery must be reported separately when run. A source-level test or textual layout is not rendered acceptance. The dedicated `GeneratedGalaxyEngineTests` exercise displayed controls, three seed/size pairs, exact counts/selection/routes, candidate and empty-region inspection, export/reopen, failure retention and original campaign identity at 1280×720, 1920×1080 and 3440×1440.

Build target `GeneratedGalaxyBuild.BuildWindowsGeneratedGalaxy` writes a new isolated output under `artifacts/generated-galaxy/Windows`; it does not overwrite the protected fleet package. Final source review, exact CI, resource lease, input provenance and a new versioned deliverable remain required.

The previous fleet candidate's unfinished native aftermath/save-restart/audio checks remain unfinished; generated-map work does not imply their completion.
