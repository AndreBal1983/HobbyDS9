# Native review control contrast patch

14 October 2026. PM observed pale inherited gold text on light-gray controls in the actual 1280×720 Windows review player built from `280930db107204f23a4fe0821710bbbe75c1fc10`. Primary reports real EditMode 121/121 and isolated Windows IL2CPP success on that pin; these executions were not performed here.

This separate branch, `codex/unity-review-control-contrast`, descends from immutable [modal PR #32](https://github.com/AndreBal1983/HobbyDS9/pull/32) `280930d`. It preserves that source/parent and changes presentation only; no gameplay, flow, modal permission or default-v1 style change. Primary owns rebuild and PM GUI; no independent Editor/player/helper/desktop or cleanup retry runs here.

## Four changed paths

- [ReviewEntry](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewEntry.cs) adds a review root class, explicit action/input classes and loads the new stylesheet last.
- [ReviewControls.uss](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/ReviewControls.uss) and its builtin stylesheet-importer `.meta` set foreground/background for those controls, disabled states, hover/active/focus, text caret/selection, collapsed dropdown text and arrow. Matching dropdown menu rows use dark-on-light defaults and white-on-blue focus/selection. Popups outside this scoped root retain the existing readable theme; their actual structure/selection remains part of PM's native recheck.
- This report.

Labels/readouts retain the existing light-on-dark root palette. New styles are scoped to the review tree and explicit control classes, so default-v1 styles/assets, scrollbar glyphs, parent metadata and the six package resources remain untouched. The one-use modal gate and all commands/callbacks remain byte-identical to the parent apart from assigning a CSS class to buttons.

## Source checks and remaining visual gate

Declared solid foreground/background ratios: enabled button **10.07:1**, hover/active button **12.89:1**, enabled input/selected dropdown **15.20:1**, disabled control **6.89:1**, menu default **17.31:1**, focused/selected menu **6.96:1**. These calculations check declared palette pairs only, not actual style resolution, parent opacity, fonts, rendered selection or accessibility acceptance. Local source/palette check is retained at `artifacts/unity-review-control-contrast/palette-check.py`.

Zero-warning compiler-only Unity-reference build passed; binding checker and seven static cases passed; the 21 focused actual-source modal regressions passed. No unchanged backend suite was rerun locally. Compiler projects are unmodified. USS parsing/import/render is **not executed here**; meaningful native findings belong to the coordinated rebuild/recheck of the exact delivered pin, especially enabled/disabled button captions, Actor/slot/input values, selected contact, focus/caret and menu highlighting at the three required resolutions.

No merge/default activation. Earlier 121/121 EditMode and IL2CPP results apply to parent `280930d`, not automatically to this stylesheet patch. Final stable head is supplied in the draft PR/handover without a report self-hash.
