# Explicit Unity physical-review import package

14 October 2026. **Portable preparation/static evidence only; runtime binding incomplete.** Fresh isolated branch starts at reviewed [PR #29](https://github.com/AndreBal1983/HobbyDS9/pull/29), `372de4ca8d74f43dc723238461036e9aa6d7e605`. Gameplay, source schemas, default activation, old PrepareUnity/ci.yml and Secondary's controller/storage/shell/runtime paths are unchanged.

## Explicit preparation

`dotnet msbuild scripts/PrepareUnityReview.proj -t:Prepare` restores locked dependencies and builds only the portable review client and its Application/Contracts/Simulation dependencies. It copies those **four netstandard2.1 DLLs** to the existing explicit plugin directory. DLLs remain ignored reproducible outputs; their metadata is committed. No wildcard import, Editor/native target, net10 storage/shell/probe, xUnit or System.Text.Json dependency enters Unity.

The generated committed resources are exact-byte copies of the existing faster/slower profiles. Preparation overwrites only these named generated outputs, never reserializes content or changes identity:

```text
Assets/HobbyDS9/Review/Resources/ReviewPackages/
  faster/{package,core,scenario}.json
  slower/{package,core,scenario}.json
```

Unity TextAsset logical paths are **`ReviewPackages/{faster,slower}/{package,core,scenario}`**, with no extension. They match the coordinated loader contract. Load their raw bytes; preserve the existing source/core/outer hash algorithm and complete physical format-2 host save. Resource availability does not activate a session or certify its Newtonsoft codec.

Shared parent metadata, declared before editing and reserved for reuse by Secondary:

| Asset path | Stable GUID |
| --- | --- |
| `Assets/HobbyDS9/Review.meta` | `640c39c3756b55ee9d1fb5fdca5aec25` |
| `Assets/HobbyDS9/Review/Resources.meta` | `3202193758b95cc6b1df7b52321f9a7e` |

The package/client-DLL metadata uses deterministic UUIDv5 under namespace `2439c88e-c2c9-4fb3-b2cb-5bd9e9fb06bc` keyed by repository-relative asset path; the [checker](../scripts/check-unity-review-integration.py) asserts exact GUIDs and global uniqueness. Reuse these two shared files on integration; do not generate competing parent GUIDs.

## Executed checks and what they establish

[Prepare target](../scripts/PrepareUnityReview.proj), [checker](../scripts/check-unity-review-integration.py), [host-only PE probe](../scripts/UnityReviewImportProbe/Program.cs), [13 package tests](../tests/UnityReviewImport.Tests/test_unity_review_import.py), and [dedicated workflow](../.github/workflows/unity-review-import.yml) are independently owned by this import increment.

Local default preparation, explicit review preparation, repeated preparation, default static checks, review checker and **13 positive/adversarial package tests passed**. Repeat preserves plugin/resource/metadata bytes. The PE probe inspects actual assembly metadata without loading/executing plugins: framework attribute, assembly names/references, public API and referenced types. Exact reference sets are:

| DLL | References beyond netstandard |
| --- | --- |
| Contracts | None |
| Simulation | Contracts |
| Application | Contracts, Simulation |
| TacticalReviewClient | Contracts, Application |

Checks also verify each imported DLL equals its fresh portable output, required physical/explanation APIs exist, forbidden host references/concrete file/process APIs are absent, explicit importer flags/GUIDs remain stable, and all six LF resource files match their sources. Composite hashes remain faster `49e54967f76f8d84822731f8a31b715775ca673ea034d9643079979098735541`, slower `3b64263436ef37aa847d27d184e4cb973c69e21def201c8a483e7e4674344180`.

Negative cases cover stale DLLs, injected host-only DLL, changed target/reference/type/API inventories, drift/duplicate GUIDs, auto-reference flags, corrupt bytes and activation changes in both source/destination. They test package validation, not combat. The net10 PE probe is a build-machine inspection tool and is not imported.

The dedicated Linux/Windows/macOS workflow performs default and explicit preparations, the checker/tests, default static regression and repeat/recheck. Exact-head results belong on the draft PR. No gameplay suite is rerun locally for unchanged gameplay; existing workflows independently retain their established regression gates.

## Remaining integration dependencies

Secondary owns Review runtime/editor/tests/scenes/asmdefs, explicit ClientEntry selector and linker preservation. Those seams are absent in this isolated tree and are reported as dependencies by the checker, not claimed validated through regex. Existing default asmdefs, bootstrap, linker and build scene are untouched.

After combining reviewed heads, validate the new asmdef/client reference and linker entries, explicit disabled-by-default launch, raw-byte loader, Unity Newtonsoft complete-root codec and isolated atomic slot adapter. Then execute Editor compile/EditMode, stripped Windows x64 IL2CPP, new-process save/reopen and authorized input/render/responsive/audio/regression acceptance. **None is executed or certified here.** Preparation cannot prove runtime/AOT compatibility, rendered output, policy acceptance or permission to merge/activate.
