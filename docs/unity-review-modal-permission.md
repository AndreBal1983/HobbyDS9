# One-use review modal permission

14 October 2026. **Reproduced portable callback semantics; narrow source fix. Actual UIElements event delivery remains unperformed.** This branch, `codex/unity-review-modal-permission`, stacks on immutable [PR #31](https://github.com/AndreBal1983/HobbyDS9/pull/31) `03ed7c3c01796e1c517787547bbf456257dd0ffe`. The old candidate/worktree and its ignored cache remain untouched. No mechanics, backend, default activation or main merge is included.

Read [the binding report](tactical-unity-review-binding.md), [architecture](architecture.md), and the [independent audit pinned at caef913](https://github.com/AndreBal1983/HobbyDS9/blob/caef913aa457de8867a85c39906dedbfbe1f4d31/docs/unity-physical-review-independent-audit.md). Primary separately owns compiler-harness split/ClientEntry coverage; those projects are not edited here.

## Finding and before-fix evidence

In #31, AddButton captures viewEpoch, active/enabled and busy/closed guards, but `dialog=true` bypasses confirmation presence. Confirm clears the overlay and invokes its action; Cancel merely clears it. If a retained dialog callback is invoked again while those outer guards remain valid, no separate modal identity/consumption check rejects it. Saving a committed slot and failed load leave the actual UI model's Frame reference unchanged, so those actions need not invalidate the view epoch.

An initial semantics-preserving extraction at **`3ac559ad4498abbe5cfc86a354f8483436b1acd6`** moved the existing clear-then-action callbacks into an actual portable source seam used by ReviewEntry. It deliberately added no permission checks. The focused tests execute that same linked Runtime source, not copied test-only callback logic or fake UI objects.

```powershell
# Run in an isolated checkout of extraction commit 3ac559ad4498abbe5cfc86a354f8483436b1acd6.
dotnet restore tests/HobbyDS9.UnityReviewAdapter.Tests/HobbyDS9.UnityReviewAdapter.Tests.csproj --locked-mode -warnaserror
dotnet test tests/HobbyDS9.UnityReviewAdapter.Tests/HobbyDS9.UnityReviewAdapter.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~ModalPermissionTests'
```

At the extraction commit, both future invariants failed:

| Direct retained-callback sequence | Required action count | Observed before fix |
| --- | --- | --- |
| Cancel → old Confirm | 0 | **1** |
| Confirm → old Confirm | 1 | **2** |

Local record: `artifacts/unity-review-modal-permission/before-fix.trx` (two failures, zero skipped). This establishes callback semantics under the stated valid outer guards. It **does not demonstrate that Unity delivers such a queued event**, that detached controls receive it, or that a native user can trigger it. The engine queue/focus behavior remains an independent later gate. The initially green #31 audit therefore remains correctly labelled as having no demonstrated native defect.

## Narrow fix

[ReviewModalPermission](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewModalPermission.cs) owns one opaque request by reference identity. TryOpen refuses to replace an active request. Confirm or Cancel must match that active request and consumes it **before** invoking external clear/action code. Repeated, canceled and replaced requests become no-ops, even when the Frame/view epoch does not change. External reentrancy or exceptions cannot reuse a consumed request. A callback may open a new modal; the prior request cannot clear it.

[ReviewEntry](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewEntry.cs) binds both terminal buttons to that request and requires an open permission before a dialog button can dispatch. Enable, disable, build/replacement, failed build and overlay clearing invalidate the permission. Existing epoch, active/enabled, dirty, overwrite, busy, closed and candidate-validation guards remain in place. The gate owns no campaign data or authoritative rules and is intended for serialized UI-thread callbacks, not a cross-thread lock, account permission or backend command deduplication mechanism.

Only eight paths change versus #31:

- `Review/Runtime/ReviewEntry.cs`.
- New `Review/Runtime/ReviewModalPermission.cs` and its stable `.meta`.
- `tests/HobbyDS9.UnityReviewAdapter.Tests/ModalPermissionTests.cs`.
- `scripts/check-unity-review-binding.py` and `tests/UnityReviewBinding.Static/test_binding.py` (reject actual button wiring that bypasses the portable permission).
- `.github/workflows/unity-review-adapter.yml` (require at least 64 actual-source cases).
- This report.

The existing adapter wildcard links the new portable Runtime file; no project/link duplication is introduced. Bootstrap, all asmdefs, scenarios, resources, codec/store/UI-model/backend files and all Primary compiler/import projects remain byte-identical to #31.

## Executed checks

- **21 focused actual-source cases passed**, including both original failures; duplicate cancel; old confirm/cancel against a replacement; invalidation; active-open refusal; reentrancy; exceptions; separate gates; real canceled slot overwrite and successful/failed overwrite; real missing/malformed/wrong-format load and successful complete-candidate replacement.
- Real successful and failed saves retain the same Frame reference; the probe records exactly one write attempt despite repeated Confirm. Canceled overwrite records zero attempts. Failed loads preserve Frame/controller/draft with one attempt. **Successful Load changes Frame**, as the existing model requires; it replaces authority once and a repeated retained callback cannot replace it again. No fake successful same-frame load behavior is invented.
- **64 complete adapter cases passed** (43 preserved + 21 new), zero failed/skipped. The existing faster `a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177` and slower `77073cc154b78dbca2e5f6fa1a618cae490d11410f51f4cb2bb9e640a33210c4` vectors remain unchanged.
- Locked restore and actual portable .NET Standard 2.1/C#9 source build passed with zero warnings/errors.
- Existing compiler-only API project, unmodified, compiled the changed ReviewEntry and added Runtime gate against installed Unity6000.6.5f1 references, pinned Newtonsoft and Unity-custom NUnit with zero warnings/errors. This remains broad single-assembly CLI reference compilation; no Editor import/split-asmdef/bootstrap claim. Primary owns that separate coverage increment.
- Binding source/configuration checker and **seven adversarial static cases passed**. Source wiring checks are distinct from runtime event acceptance.

After-fix local records: `artifacts/unity-review-modal-permission/after-fix-focused.trx` and `after-fix-complete-adapter.trx`. Exact final head and Linux/Windows/macOS CI results belong on the stacked draft PR; no report self-hash is fabricated. Unchanged full backend suites are not rerun locally for this lifetime-only change.

## Remaining gates

No Unity Editor, EditMode runner, native helper/build/player, desktop/input, screenshots or cleanup retry was executed. The new tests directly invoke the actual portable request callbacks and real adapter/model file operations under .NET. They do not prove real UIElements queuing, modal/focus/overlay visuals, stripping/AOT or native filesystem behavior. Later authorized engine acceptance should explicitly exercise cancel→retained confirm, duplicate activation, replacement modal, disable/re-enable and same-frame save/load failures alongside normal modal keyboard behavior. Existing runtime/visual/default-regression gates from #31 remain open. No merge or activation is authorized here.
