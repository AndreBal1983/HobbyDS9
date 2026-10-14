# Opt-in physical-review Unity binding

14 October 2026. **Authored binding with executed portable/compiler/static evidence; Editor/AOT/native acceptance unperformed.** Branch `codex/tactical-unity-review-binding` starts at reviewed [PR #29](https://github.com/AndreBal1983/HobbyDS9/pull/29), `372de4ca8d74f43dc723238461036e9aa6d7e605`. The historical UX reconciliation at `455bf10` is a different completed task.

Source binding checkpoint: `1a30b4b316f894c5a7cd3a67d16156c7b91697ef`. Integration commit `1a3f325faed9f810e24e31718b3dd747d6ef4c0a` preserves that first parent and delivered [PR #30](https://github.com/AndreBal1983/HobbyDS9/pull/30) import `8e4594fffd3962d7c5e3d1cae45d45973800d224` as second parent. No PR/main merge, default activation, backend change or source-branch rewrite occurs. Final head/CI are recorded on the draft PR to avoid a self-referential report SHA.

Read [architecture](architecture.md), [physical contract](campaign-tactical-v2-physical-travel.md), [explanations and shell](tactical-review-explanations-shell.md), [import package](unity-physical-review-import.md), and [readiness audit pinned at 5966ddd](https://github.com/AndreBal1983/HobbyDS9/blob/5966ddd6bf624f944bc716dceb092da3c4184ffb/docs/campaign-tactical-v2-unity-readiness.md). Proposed review policies and production activation remain gated.

## Authored behavior

- [Runtime](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/) contains the actual C#9/netstandard-compatible Newtonsoft complete-root codec, exact-byte resource loader, disjoint slot adapter, portable UI host model and UIElements entry. The same six non-engine files are linked into [the portable adapter](../prototypes/HobbyDS9.UnityReviewAdapter/HobbyDS9.UnityReviewAdapter.csproj); no substitute serializer/model is tested.
- Existing ClientEntry adds only an explicit review selector after unchanged VSync 1 / targetFrameRate −1. Default v1 startup/asmdefs/builder remain unchanged. Review adds no second RuntimeInitialize callback. link.xml preserves review client/bridge alongside existing assemblies; this is not AOT proof.
- Labels identify proposed policies, disabled production activation and controlled actor view. This is a trusted hotseat host, not account security or invented opponent AI. Actor changes validate real ownership and require deliberate dirty-draft discard. Hidden orders/stocks/private roots, serialized saves, paths and raw exceptions do not enter player labels.
- Fleet maneuver and enemy-fleet preference, lawful ship inspection, own four shield stocks/hull/ammunition/availability, reports, known-route travel, approach cloak, advance and ready-batch finalization use real host/controller operations. No pursuit/weapon/geometry math is duplicated. Retain-focus is explicit; inability to edit an unavailable retained focus is explained, leaving intent intact until lawful selection/explicit clear. Ship selection is inspection, not firing orders.
- Save retains local drafts outside checkpoints. Load stages the complete candidate through actual physical Application validation before replacing the host. Failed load retains controller/draft/inspection. Overwrite, dirty actor/encounter changes, reset, load and exit are deliberate. Modal siblings are disabled; busy/pending/closed guards reject work. New frame/actor/host bindings increment a view epoch, rejecting queued old-view callbacks. Disable/re-enable retains the model and recreates UI, unregistering the old geometry callback.
- [Empty review scene](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Scenes/PhysicalReview.unity) and [explicit Editor builder](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Editor/ReviewBuild.cs) are authored only. Builder uses `artifacts/unity-review/Windows/HobbyDS9Review.exe`, separate from default output, and restores temporary API/backend settings. Neither scene import nor builder was executed.

## Complete-root, exact bytes and slots

The codec preserves complete physical format-2 package/save DTOs: physical phase, requests, contacts, orders, receipts/proof and full nested Core. No nested-core fallback, JsonUtility, net10 storage/shell import, floating-point/string integer intermediary, CLR type metadata or source reserialization hashing occurs.

Unknown/case-mismatched/duplicate members, wrong scalar/array tokens, missing non-nullable scalars, invalid UTF-8 and extra root data are rejected with fixed diagnostics. Nullable ingress and semantic enum/format/pool/proof requirements reach actual Application validators. Parser checks do not replace authority. DTO reflection is preserved, still requiring Unity/AOT execution.

Logical resources are `ReviewPackages/{faster,slower}/{package,core,scenario}`. TextAsset.bytes feed the existing scenario/core/physical composite algorithm using SHA256.Create/ComputeHash. Confirmed package hashes: faster `49e54967f76f8d84822731f8a31b715775ca673ea034d9643079979098735541`; slower `3b64263436ef37aa847d27d184e4cb973c69e21def201c8a483e7e4674344180`. Preparation restores exact LF source bytes after checkout; automatic Windows CRLF checkout is not a substitute for preparation.

Slots use only `Application.persistentDataPath/HobbyDS9PhysicalReviewOnly-v2/unity-physical-review-{slot}.json`. Names are 1–32 lowercase ASCII/digit-start characters, then lowercase/digits/underscore/hyphen. Directory is fully qualified/non-root. Lexical confinement/endpoint reparse checks cover this trusted configured location, not hostile ancestor substitution or concurrent filesystem attack.

Writes encode before I/O, CreateNew a uniquely named same-directory temporary, flush and File.Move new slots / File.Replace explicit overwrites. No delete-first/copy fallback or enumeration. Failure cleans only its own temporary where possible and preserves the prior file. Tests inject pre-write/partial-write/pre-replace failures. No power-loss/directory-fsync/network/concurrent-writer/native Mono durability claim.

## Executed evidence

| Check | Actual result and limits |
| --- | --- |
| Locked restore / portable adapter/tests Release | Passed, zero warnings/errors; netstandard2.1/C#9 actual codec/model/slots |
| [Actual-source portable tests](../tests/HobbyDS9.UnityReviewAdapter.Tests/) | **43 passed, zero failed/skipped** under .NET; real physical Application/host/controller, no Unity mocks |
| Same 43 cases with exact pinned Unity Newtonsoft DLL in test output | **43 passed** under .NET, not Editor/Mono/AOT; package 3.2.1 Runtime DLL SHA256 `7292d3eb508652d14726749dd27094f2d481aeccf2db6427b62f68a71460897e`, assembly 13.0.0.0; portable CI uses locked NuGet 13.0.2 |
| [Compiler-only Unity API project](../prototypes/HobbyDS9.UnityReviewApiCompile/HobbyDS9.UnityReviewApiCompile.csproj) | All seven runtime sources, Editor helper and five authored EditMode cases compile, zero warnings/errors, against installed Unity 6000.6.5f1 modules, pinned Newtonsoft and Unity-custom NUnit. No Unity process/import/test runner/native execution |
| Default PrepareUnity / explicit PrepareUnityReview | Passed; portable DLL/resource generation only |
| Default static checker / import PE/resource checker | Passed after preparation; import checker still prints its historical runtime-dependency notice and does not certify this binding |
| Import adversarial cases | **13 passed** on combined tree; Primary sources unchanged |
| [Binding source/configuration checker](../scripts/check-unity-review-binding.py) / [adversarial cases](../tests/UnityReviewBinding.Static/test_binding.py) | Passed / **6 passed**; asmdefs, linker, default selector/gate, no render dispatch, host exclusions, disjoint output; not runtime proof |

Actual portable cases cover Planning/null proof, pending physical AwaitingCombat with nested legacy Planning, waiting commitment, resolved reports, finalized Planning, complete-root reopen, changed Flank/Screen rounds and depleted second battle. Test-only canonical root serialization uses the existing headless serializer for vector comparison; Unity file bytes are not claimed identical to headless encoding.

Unchanged vectors match: faster two-battle `a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177`; slower miss `77073cc154b78dbca2e5f6fa1a618cae490d11410f51f4cb2bb9e640a33210c4`. Faster torpedoes retain 7 → 3 → 0; slower retains 2/3 progress and lawful last-seen knowledge without current enemy destination. Actual UI-model hotseat/save/reopen also produces the faster vector.

Other cases cover Int64/UInt64 limits, nullable zero/false/null, corrupt format/enum/pool/proof/root, numeric strings/fractions/duplicate/case/missing fields, exact-source hash drift, bounded/disjoint slots, overwrite failures, retained drafts/failed candidates, unknown actors, dirty exit, busy/pending replacement rejection and stale controller work.

Backend/default/client production sources and inherited tests remain identical to #29; default assets/artwork are unchanged. [Dedicated adapter CI](../.github/workflows/unity-review-adapter.yml) runs portable cases/static checks on Linux/Windows/macOS. Inherited workflows retain established suites/regression. Exact-head outcomes belong on the draft PR. Unchanged gameplay suites are not claimed rerun locally here.

## Unperformed gates

**Not executed:** Unity Editor compilation/import; five authored EditMode cases or existing Editor suite; IL2CPP/stripping; native serialization/files/new-process resume; player input/render/resizing/focus/modal behavior; physical audio/GPU observation or default-v1 native regression. No desktop tools/player/helper launched. CLI C# reference compilation is narrower than Editor acceptance.

Planned sizes: 1280×720 stacked scrollable controls/readouts; 1920×1080 side-by-side; 3440×1440 bounded control width/wider reports. Text wraps with proposed minimum font 16; actor/save/reset/exit toolbar persists. These are **unrendered policies**, not overlap/clipping/readability evidence. Actual focus/modal/file behavior and disable/reopen need authorized native checks.

Later runtime acceptance must prepare this exact candidate, execute Editor import/EditMode plus old tests, explicitly build the disjoint Windows IL2CPP player, and prove waiting/resolved complete-root new-process resume, two depleted battles, stale/dirty/modal guards, actor privacy and three real resolutions. Recheck default designer/production/routes/v1 reports/audio/VSync. Owner policy approval, activation and PR/main merge remain separate gates. This source delivery does not claim the first playable campaign slice is accepted or activated.
