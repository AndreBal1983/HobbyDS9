# Engineering Development v1: writer source handoff

12 November 2026. **Source/data preparation only. Writer not imported or executed.** No new scenario or Unity resource capsule has been generated. No portable SDK, validator, compiler, engine, native player, desktop, census, lease or owner-state operation was performed. Root owns the Simulation rule port, runtime hook, composition, actual data generation and acceptance. The old packages, resources, production hashes and targets are unchanged.

## New files to review

- `scripts/build_engineering_content_variant.py`: independent, builtin-only Python writer source. It may write only an entirely absent `scenarios/engineering-development-v1`, an entirely absent `client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/EngineeringDevelopmentV1`, and that new Unity folder's metadata. It does not edit `PrepareUnity.proj`, old targets or runtime files.
- `scripts/engineering-content-variant-inputs.json`: full source input pins, file identities, 542 existing Unity metadata pins, mapping and independently calculated prospective hashes. This is source metadata, not a write permission or package-validity receipt.
- `E:/HobbyDS9/artifacts/engineering-development-source-20261112/writer-source-prep-result.json`: source/meta pins and explicitly unexecuted status. Proposed resource GUID/meta bytes are calculated as source metadata, not created in Assets.

## Exact lineage and retained facts

Actual `git show` source: commit `e365ec1ecf3d686b114548f805f68f73f742e89b`, path `scenarios/ds9-slice/scenario.json`, blob object `bcee2168529c90bbb41cec300b3f13fb08aeb231`, **150142 bytes**, SHA256 `A9D631C84AB8689718C9F5199877694FA4B027AC774E3EE0F1A6B9BB31FA692F`. This was read directly; the earlier abbreviated worker report was not treated as proof.

The new recipe starts with the current five-file `playable-fleet-v1` package. It retains current initial state, numeric values, rules, tactical equipment and RNG facts; it never substitutes the old scenario's initial state. Python integers preserve UInt64 values. Noninteger JSON inputs, duplicate JSON keys, changed pins/identities, missing parents, cycles and collisions fail closed. The four profile documents retain their exact raw bytes when no approved exact-ID reference remap is needed; ProfileId and FormatVersion are always preserved.

Imported Engineering content is the exact 20 applications, full 3-hull/30-component parent closure, and original version gates. The 19 derived versions are 17 component variants and 2 utility hull variants. All imported component VersionId values are mapped from `component.<suffix>` to `component.utility.<suffix>`; all imported component FamilyId values are mapped from `component-family.<suffix>` to `component-family.utility.<suffix>`. This produces **30 version + 13 family = 43** bidirectional pairs. Utility hull-family, role, numeric, cost, dependency, mode, mount and tag facts remain unchanged.

Structured references are remapped by exact ID, including parents, gates, initial/frozen/published/support references present in the new template. Exact bounded ID tokens inside `EffectSummary` are also updated, so human-readable output descriptions do not point to nonexistent original derived component IDs. No display-name heuristic is used. Inverse mapping must deep-equal the original imported catalogue, applications and gates; the new initial state must inverse-map to the current initial state. All other scenario fields must deep-equal the current base after excluding the four explicitly replaced definition collections.

Six original component IDs collide with modern ready content: `component.reactor.v1`, `component.life-support.v1`, `component.command.v1`, `component.propulsion.v1`, `component.escort-sensors.v1`, `component.energy-bank.v1`. Their definitions must not be blended or have compatibility widened. The namespace separates them in the NEW variant only.

The current activities and colonisation catalogues agree on shared rows. Their union preserves every modern ready object semantically unchanged; their raw source bytes are also retained as separate unchanged catalogue copies in the future new capsule. Modern IDs/families are not remapped. The final merged catalogue has 4 hull families, 6 hulls, 47 components and 6 roles. All 53 hull/component versions have exactly one access entry: the original mapped utility gates, plus empty gates for the 20 already-ready base versions. No new Engineering reward upgrades Galaxy/Defiant, broadens compatibility or rebinds art. This data recipe does not establish new runtime availability until the receiving validators and integration pass.

## Prospective hash, not actual package validation

A separate read-only in-memory source-data calculation produced:

- Prospective scenario bytes: **199082**.
- Scenario content SHA256: `ca48bdbe80a3ac56c53871228f33b561a0b89eb85bf1d18195007548d7f17f33`.
- Prospective playable PackageHash: `9b01863cc03265a875ee0ccd20028c7c21b7ade3a741bb6ef566bee176806320`.
- Remaining conflicting IDs or family/revision pairs after the approved map: **none in that source-data analysis**.

The writer source independently implements the pinned `ReviewExactPackageLoader.LoadPlayableFleet` chain. Let `H` be lowercase SHA256 of raw bytes. Every concatenation is ASCII and ends with a newline:

```text
core       = H("campaign-tactical-review-package-v1\n" + H(coreBytes) + "\n" + H(scenarioBytes) + "\n")
physical   = H("campaign-physical-review-package-v2\n" + H(physicalBytes) + "\n" + core + "\n")
integrated = H("campaign-integrated-playtest-package-v3\n" + H(packageBytes) + "\n" + physical + "\n")
playable   = H("campaign-playable-fleet-package-v6\n" + H(playableBytes) + "\n" + integrated + "\n")
```

Only the new scenario is serialized, using UTF-8, `ensure_ascii=False`, two-space indentation and one trailing newline; unchanged profile bytes are copied verbatim. The writer must reproduce the manifest's prospective hash before any output. No C# decoder, simulation validator, package constructor or native client has accepted these prospective bytes.

## Future write gate and output ownership

Do not invoke the writer now. Root and the existing frontend must independently review source and input closure, then supply distinct actual CLOSED/durable `ADMIT_NEW_ENGINEERING_DEVELOPMENT_V1_DATA_WRITE` records. Both bind exact writer/input-manifest/official-usage hashes and the two fixed relative output roots. Both additionally bind `authoritySha256` to the exact standing 1E27 and continuation A4EC policy pins from the input manifest. Their record/counterparty IDs must be reciprocal; frontend additionally pins the exact Root record bytes. `sourceOnly` must be false and `dataWriteOnlyNoCensusLeaseNativeAuthority` true. No such actual records were authored here.

The future authoritative usage wrapper must be a fresh actual `mcp__codex_app__get_usage_limits` result under decision `ACTUAL_OFFICIAL_ENGINEERING_VARIANT_USAGE`, owner `/root`, CLOSED/durable, with observed UTC and complete actual result. The writer checks ordinary included usage, every numeric window strictly below 80, absent/null optional secondary and no reached individual/spend/other denial; hard freshness is 0 <= age < 300 seconds with no future-timestamp allowance, and Root should aim 120. Window durations must be positive numeric values and reset timestamps must be valid integers within the declared future window. No credits, resets or model/account bypass are authorized. A4EC retains the owner's current included-usage exception and known balance-mismatch handling.

The writer rechecks the complete existing metadata universe and pins before generation, checks deterministic new GUIDs against all 542 pinned existing GUIDs, validates root parents and root absence **before inspecting any descendants**, and then creates only the two new roots and new files. Each file uses CreateNew, flush/fsync and exact byte roundtrip verification. New root file identities are checked throughout. Input bytes/identities are checked again after writing. A fixed original 30-second writer clock never restarts. Any error preserves partial NEW outputs and fails; there is no cleanup, overwrite, automatic retry or old-package mutation.

The future capsule includes five package files, merged `catalogue.json`, unchanged ready catalogue copies, `id-map.json`, provenance and manifest. Unity metadata uses a deterministic new UUIDv5 namespace, not borrowed GUIDs. The manifest hashes payload and metadata but excludes its own JSON bytes to avoid a circular pin. Source metadata pins are not a promotion or publication instruction.

## Remaining gates

Python AST and builtin JSON/pin/source checks only. Writer/helper functions have not been invoked. Actual output creation, byte roundtrips, deadline/failure behavior, source namespace lifetime under concurrent filesystem mutation, actual UUID/meta import and all package/availability/compiler tests remain unexecuted. Existing source metadata changing before invocation correctly invalidates the input manifest; do not silently re-pin it. Ordinary path ancestry checks are not an atomic filesystem transaction, so Root/frontend must qualify exclusive lifetime of these new source-output namespaces before writing. Runtime Engineering routing, source selection, package composition and native verification remain Root-owned. Desktop remains stopped after Escape.
