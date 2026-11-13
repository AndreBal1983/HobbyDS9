# Current workbench input-test receiving

13 November 2026. **Test-source alignment; actual Unity rerun pending.** No runtime, gameplay, validation, art, save, package or pinned Editor version change. Base is QA's tested HUD receiving `f5cdac08c4ba2ca9de491747bd8da0cc8a1357e7`.

QA's exact0a5 graphics ledger reported six failures. Two LCARS cases assert the older instrument-bank presentation instead of the current [art workbench](ship-designer-art-workbench-ui-20261024.md):

| Recorded failure | Current source evidence | Updated receiving assertion |
|---|---|---|
| PowerBankWrapsAtNarrowStandardAndWideWidthsWithReachableExactControls: expected120–180px, actual280 | ShipDesigner.uss explicitly overrides the historical156px tiles with full-width workbench rows, min-width0/max-widthnone; the workbench panel has a280px minimum | Renamed workbench case requires full bank-content width, containment/no row overlap, two independent nonoverlapping44px hit columns, exact track height/common scale and the two exact editors at700/1200/1900 widths |
| FullPositionIsNotErrorButBackendBudgetErrorsRemainVisible: obsolete BACKEND ERROR caption absent | Current view emits each actual backend explanation with HardError styling and a separate validation foldout | Preserve the healthy-full-state checks; require the actual AvailablePowerExceeded backend issue, matching source path/message/severity in the projection, and that actual message in an error-styled rendered label |

Neither change merely broadens the old limit or removes validation. The backend over-budget fixture and full/invalid state semantics are unchanged. The tests now catch missing budget evidence, lost error styling, missing/overlapping hit columns, clipped rows or track geometry instead of prescribing an obsolete tile/caption. Existing pointer/capture/key/exact-value/detach/profile continuity cases remain unchanged.

QA should rerun `HobbyDS9.UnityClient.Tests.LcarsPowerSliderTests` with graphics and an owned Editor panel under the existing exclusive receiving workflow. These source changes do not declare either case green before that run. The project-version assertion, two legacy designer-reference cases and archive-scan menu fixture remain separately open; no expectation was relaxed for those failures. The main-HUD native/fidelity/performance gate and current queued art comparison are unchanged. This batch requires no broad gameplay rerun.
