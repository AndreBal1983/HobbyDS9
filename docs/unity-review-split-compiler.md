# Split Unity reference compiler coverage

14 October 2026. **Compiler-only local gate**, based on reviewed #31 `03ed7c3c01796e1c517787547bbf456257dd0ffe` through [audit](unity-physical-review-independent-audit.md) `caef913aa457de8867a85c39906dedbfbe1f4d31`. No actual Unity source, old gameplay/ci.yml, ReviewEntry or portable-adapter tests change. No Editor process, helper, builder, test runner, player, native code or desktop is invoked. No activation or merge.

## New coverage

The [six source-link projects](../prototypes/HobbyDS9.UnityReviewSplitCompile/) preserve actual Unity assembly names and compile the real source corpus, not mock engine APIs. Default runtime now includes **ClientEntry and its actual Bootstrap body**. Default/review runtime are separate projects with no direct cross-reference; Editor/test projects reference their corresponding runtime rather than flattening all code into one assembly.

| Project / output assembly | Actual linked files | Variants executed | Result |
| --- | ---: | --- | --- |
| DefaultRuntime / HobbyDS9.UnityClient | 33 | Player; Editor | Compiled; two existing runtime deprecation diagnostics per rebuild |
| ReviewRuntime / HobbyDS9.UnityReview | 7 | Player; Editor | Compiled, no errors/warnings |
| DefaultEditor / HobbyDS9.UnityClient.Editor | 4 | Editor only | Compiled; referenced default-runtime diagnostics retained |
| ReviewEditor / HobbyDS9.UnityReview.Editor | 1 | Editor only | Compiled, no errors/warnings |
| DefaultTests / HobbyDS9.UnityClient.Tests | 8 | Editor + test symbols | Compiled; runtime deprecations plus two existing AudioTests deprecation diagnostics |
| ReviewTests / HobbyDS9.UnityReview.Tests | 1 | Editor + test symbols | Compiled, no errors/warnings; authored cases **not executed** |

All eight outputs target **netstandard2.1 / C#9**. Player and Editor outputs/intermediates are separate. Explicit baseline symbols: UNITY_6000_6_OR_NEWER, UNITY_6000_0_OR_NEWER, UNITY_2023_1_OR_NEWER, UNITY_STANDALONE, UNITY_STANDALONE_WIN. Editor adds UNITY_EDITOR/UNITY_EDITOR_WIN; test projects also add UNITY_INCLUDE_TESTS. This is a declared subset, not Unity's complete generated define environment.

Runtime references include installed **UnityEngine*.dll only**, excluding UnityEditor modules. Editor/test projects add actual UnityEditor*.dll; tests explicitly require installed Unity-custom NUnit plus UnityEngine.TestRunner/UnityEditor.TestRunner references. No net10 host/storage/shell is referenced by the eight outputs. .NET SDK project-reference propagation is not identical to Unity asmdef resolution; actual metadata is recorded rather than claiming exact import parity.

PE metadata inspection (without loading/executing the DLLs) confirms assembly names/targets and:

- Both default-runtime variants contain private static ClientEntry.Bootstrap with **148 IL bytes**; neither references HobbyDS9.UnityReview directly.
- Both review-runtime variants contain public static ReviewLaunch.Launch with **78 IL bytes**; neither references HobbyDS9.UnityClient.
- Neither runtime references UnityEditor or the forbidden net10 host dependencies. The qualified reflection target and separate assembly identity remain source/metadata-consistent; actual reflected execution is not proven.

## Reproduction and evidence

Run [the explicit local script](../scripts/check-unity-split-compile.py):

```powershell
python scripts/check-unity-split-compile.py --unity-managed-dir "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Data\Managed" --newtonsoft "E:\HobbyDS9\client\HobbyDS9.Unity\Library\PackageCache\com.unity.nuget.newtonsoft-json@74deb55db2a0\Runtime\Newtonsoft.Json.dll" --nunit "E:\HobbyDS9\client\HobbyDS9.Unity\Library\PackageCache\com.unity.ext.nunit@0198eae3b53e\net472\unity-custom\nunit.framework.dll" --test-runner-dir "E:\HobbyDS9\client\HobbyDS9.Unity\Library\ScriptAssemblies"
```

Installed references were available; no installation occurs. The script verifies the pinned Runtime Newtonsoft SHA256 `7292d3eb508652d14726749dd27094f2d481aeccf2db6427b62f68a71460897e`, explicitly rebuilds each project, then runs a **host-only net10 PE reader**. The reader is not imported into Unity and executes no compiled Unity assembly. Logs, reference paths/hashes, source-file hashes, symbols, warnings, output hashes and metadata are in ignored `artifacts/split-compiler/evidence.json` and per-project logs. The optional prior single-assembly harness remains unchanged.

The initial harness inherited portable nullable/warnings-as-errors policy and promoted existing Unity diagnostics to errors. Matching Unity's default nullable context (source directives still apply) and retaining visible warnings resolved that harness mismatch. Existing AudioDirector/SfxDirector FindFirstObjectByType calls warn CS0618; AudioTests' FindObjectsSortMode/overload also warn. These are **not silently suppressed or labelled zero-warning**, and no unrelated audio/designer rewrite is made. Initial missing UnityTest attributes were resolved by adding the actual test-runner references to this harness, not altering tests.

## Limits and next gate

This closes the prior default-bootstrap source-compilation blind spot and distinguishes runtime/editor/test assemblies. It does **not** import asmdefs/resources, execute initializers/reflection, discover/run NUnit cases, emulate Mono, link/strip/AOT compile, or render/dispatch UI input. Wide installed module references and SDK transitive references can still differ from Unity's real compiler graph. Warnings remain a maintenance observation, not a manufactured source failure.

Next safe step remains authorized pinned-candidate Editor import/compile/EditMode, then stripped Windows native and live acceptance. No unchanged gameplay suite or CI is rerun for this local reference gate. Policy acceptance, activation and merge stay separate; the checkpoint is complete before the overnight cutoff.
