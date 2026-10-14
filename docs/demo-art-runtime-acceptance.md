# Demo art runtime evidence

14 October 2026. Source branch `codex/demo-art-runtime`, based on `8ae866556e21cef47a9e5b0efaea49e01d30f3f6`. [Contract](demo-art-runtime-contract.md); [current checkpoint](demo-art-runtime-checkpoint.md).

## Completed evidence

- CPU-only portable Prepare and PrepareUnityIntegrated targets: successful, zero-warning library builds; no gameplay source changed.
- Runtime binary integrity: 49 tracked resource files with SHA-256/size pins and resolved local prefab GUID dependencies, nine FBXs, nine URP materials, three prefabs, preview shader. Total shipped resource payload approximately 1.35 MB. No runtime dependency on ignored Blender/artifact directories.
- Unity **6000.6.5f1**, graphics-enabled **Editor** EditMode filter `HobbyDS9.ArtDemo.Tests`: initial 13/15 pass, corrected run **15/15 pass**. Tests cover exact/hidden/unsupported mapping, all three prefab and nine LOD triangle counts/metre bounds/axes/normals/UV/materials, real cruiser/fallback pixels without error shader, external scene-light/object invariance, same-frame replacement/clear, simultaneous distinct previews, disposal and preservation of external state.
- Independent source review caught the initial unsupported `Camera.scene` assumption. It is removed, not suppressed in the test. First run also exposed ordinary `SceneManager.CreateScene` being unavailable in EditMode. No scene creation is now needed. Model bounds are derived from mesh bounds, including inactive instances.
- Lease released immediately after corrected run; no desktop input, native player launch or Blender workload. Original editable sources and previous packages remain preserved.

Actual Editor captures from the corrected run (not Blender renders or whole-screen concepts):

![Cruiser preview](demo-art-runtime-evidence/cruiser.png)

![Explicit generic fallback](demo-art-runtime-evidence/fallback.png)

## Final exact-pin recheck

**Passed at source `02f5992be2041ee52f08d917fa7b7777af0d3415`: 15/15**, zero failed, pinned Unity6000.6.5f1 graphics-enabled Editor EditMode. The final fixture confirms world emission retention after import and additionally counts inactive cameras, owned materials, fallback meshes and render textures after disposal. Actual refreshed PNGs were visually inspected; runtime manifest verification still passes after import. Unity exited and the lease was released immediately. No runtime source changed afterward.

Exact source/results/evidence hashes are in `artifacts/art-demo-runtime/final-acceptance.json` and `FINAL-ACCEPTANCE.md`; raw final report/log are `editmode-final.xml/.log`. These final local report updates and PR metadata supersede historical pending paragraphs in the frozen PR source docs. No additional source push was made after the final test, respecting the coordinator's freeze.

Raw reports/logs: `C:/Users/ambal/.codex/worktrees/demo-art-runtime/HobbyDS9/artifacts/art-demo-runtime/editmode.xml`, `editmode.log`, `editmode-2.xml`, `editmode-2.log`. Receipt and runtime manifest preserve exact tested/source hashes; Git/PR records delivery pins. Generated receiving-project/Unity settings deltas from preparation/import are excluded from this art branch's commits.

## Limits

CI follow-up: both default static workflows at02f failed because the Git-policy `.gitattributes` file was inside Assets and therefore lacked Unity metadata. The policy is moved to the repository root with identical ArtDemo-only patterns; metadata validation is unchanged. This fixes file placement, not runtime behaviour. Prior15/15 graphics evidence still applies to the unchanged runtime/asset payload, while exact new-head hosted static checks remain a separate gate.

Simple provisional hull art and fixed studio preview lighting; no final texture atlases, canon-class claim, new gameplay ships/equipment, physics/combat authority, native player interaction proof or profiled fleet performance budget. Only the exact Federation source-cruiser binding is enabled. Unsupported faction/design identities remain generic; hidden/non-visible selections clear. Frontend call-site binding and combined player validation are separate deliveries owned by Secondary/coordinator.
