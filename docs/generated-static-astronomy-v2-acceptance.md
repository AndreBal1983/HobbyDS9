# Generated static astronomy v2 — portable acceptance

24 October 2026. Bounded CPU-only implementation based on protected generated-campaign v1 commit `e6eb3392437a2896efe067d4e76d433759ee6842`. Source branch: `codex/generated-static-astronomy-v2`. Final source/tree pins and file hashes belong to the external immutable evidence manifest, avoiding a self-referential commit pin in this document.

## Results

- Complete portable .NET Release suite: **2,108 passed, 0 failed, 0 skipped**, including the prior 2,078 cases and **30 static astronomy cases**. Raw result: `static-astronomy-full-release.trx`.
- Strengthened focused run: **30 passed, 0 failed, 0 skipped**. Raw result: `static-astronomy-strengthened.trx`. Earlier implemented focused run: 25/25.
- Independent source review of the first runtime commit `c8e17ae014da3ff5440914037010529992b2cffe` cleared the separate capability wrapper, preserved v1 defaults/hash path, frozen index, containment ties, actor validation, immutable view and strict profile pairing. The final strengthening adds direct knowledge-level and late-index failure cases; final-source review remains the coordinator's gate.
- No frontend, Unity, PrepareUnity, GPU, native-player, push or CI dispatch was performed for this bounded backend increment. Their future acceptance remains separate.

## Compatibility and determinism

Existing v1 Start/Load/Export remains on the old factory/interface/hash path. The accepted actual AwaitingCombat fixture retains SHA-256 `02fdb4e0a3113d259128f46f1a513854ab95a11bc228b90ba8fb92cb720fa8a5`; aftermath retains `8f026214856d5195e7761945b2c03c9a4e8a5e38befc5081389368b97c6b1be3`. Both load and export byte-exact in the focused and full suites. Fixtures were copied from the sealed v1 evidence; that bundle was not rewritten.

V1/v2 generated map bytes and initial campaign state match. Twelve turns of paired v1/v2 campaign state, AI orders, contacts and RNG match exactly. Reordered civilization/fleet/home inputs and repeated v2 runs give identical exported roots. Repeated astronomy queries do not change authoritative saves. Existing canonical/reverse/repeated/save-load regression cases passed as part of the complete suite; the old headless CLI evidence was not rerun or relabelled as new evidence.

The new profile is `generated-campaign-v2`, policy `generated-neighbor-exploration-static-centres-v2`, embedded core marker `generated-campaign-owned-core-v2`, outer format 7. Observation is `charted-static-centres-v1`; visual reference is `cosmetic-influence-scale-v1`. Natural format 4/generator v5, Save 14, authored Content 11, legacy Content 10 and existing rules/compiler remain unchanged. Mixed headers, coordinated marker flips, altered hashes, damaged transcript and invalid geometry reject. There is no save upgrade or migration.

## Privacy, knowledge and failure boundaries

Closed-polygon centre containment retains every shared-edge/vertex owner. Any Charted or Surveyed containing region reveals a deduplicated nebula; Unknown does not. Influence overlap alone reveals nothing. Public snapshots contain detached immutable scalar proxies and actor/map/turn/revision/policy stamps, without private labels, density, node/system anchors, hidden counts or the full feature tree. Unknown and uncontrolled actors reject before querying the index.

Actual human exploration first charted nebulae at turn 27 and retained three through turn 32 and save/load. This is a deterministic test trajectory, not a gameplay timing promise. A hash-correct malformed frozen feature reaches the late index rejection; the existing live save and observation remain unchanged. Failed loads publish no replacement session.

## Bounded diagnostics

Strengthened focused-run observations on this machine (diagnostic only; no wall-clock acceptance assertion):

| Fixture | Start | Index | 100 captures | AABB checks | Cross products | Memberships |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 250 systems / 500 regions | 308 ms | 0 ms | 0 ms | 7,500 | 125 | 15 |
| 1,000 systems / 2,000 regions | 6,188 ms | 2 ms | 1 ms | 120,000 | 498 | 60 |

The adversarial **index component** case uses 1,250 nebulae at a common vertex and 2,000 closed fan polygons, 20,000 aggregate vertices. It is not a generated campaign fixture. It reaches exactly 2,500,000 AABB checks, 25,000,000 cross products and 2,500,000 memberships, with 1,250 unique visible proxies. Observed 117 ms and approximately 32,038,992 bytes managed-memory delta; allocation/timing are environment-dependent. Over-limit region/feature inputs reject. Production caches one private index per immutable session map and queries known inverse-index rows.

## Remaining boundaries

Observation semantics are provisional; cosmetic generation-influence radii are neither physical nebula extent nor sensor coverage. The implementation changes no routes, costs, AI inputs, contact freshness or known navigation nodes. Future host dispatch must use the explicit new profile pair and a separate storage namespace. No frontend integration is authorized by this report; coordinator final review and later engine/native gates remain required.
