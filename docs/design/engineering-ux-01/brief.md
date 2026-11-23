# ENG-UX-01 — Engineering decision screen

13 November 2026. **R2: proposed design for the intended 20-project catalogue; owner review before implementation.** [Responsive 20-project mockup](mockup.html). [Earlier single-project drawing](locked-1280.svg) is retained as superseded design history, not the target. Artifacts are design demonstrations, not runtime captures.

## What is wrong, and what data actually exists

Reviewed PB `788190d3dcc2c371d23a694e21b13e08454034ee` and the supplied screenshot: one locked Advanced Starship Engineering project, no active target, 0/30 points, two missing Science requirements. The current card repeats its status, uses a huge empty progress bar, hides named prerequisites in a foldout and fills the page with empty project categories. Income and bank are separated in an aside. These are interface problems.

At PB788190d the content problem is that the campaign offers only **one application**, no refinements and no civil projects. Its authored “Unlocks Advanced Scout production” effect belongs to the old catalogue. The current Cycle14 UI correctly warns that Advanced Scout is unavailable here. Owner correction **ENG-CONTENT-01 requires the existing 20 goals in the next playable build**. The proposed screen therefore targets that actual catalogue, not a polished one-project fallback. Preserve the Scout qualification wherever it remains true after receiving.

PR85 at `e365ec1ecf3d686b114548f805f68f73f742e89b` supplies the 20 actual definitions. [catalogue.js](catalogue.js) copies its complete `EngineeringApplications` array without invented replacement projects. Backend is assigned integration onto PB788190d; the receiving receipt is a dependency, not a claim PR85 has merged. The migration's unexecuted writer proposal is not received content. No writer or content integration is performed by this design.

## Proposed hierarchy

1. Compact overview: **Engineering at current allocation**, **Banked points**, **Current project**. “No active project — points remain banked” is sufficient. R&D allocation opens the existing accepted/draft split controls; it never silently changes allocations.
2. Compact **20-project browser**: title, state word, retained progress and result qualifier. Search names/results; filter actual new-capability/refinement kinds. No empty civil section. State filters/counts bind real snapshot states after integration; the review mockup instead labels its state fixtures hypothetical. Selection is local; highlight never starts a project. Deterministic title/ID ordering; preserve selected ID across refresh and do not hide locked projects by default.
3. Selected details: result first, named prerequisites, actual cost and retained progress, then one clear action. Keep technical maturity/provenance in a disclosure. **Review Science requirements** opens the relevant discipline without changing shares or targets. No ETA or projected bank spending.
4. Expandable latest-turn receipt summary: separate points added to bank, project spending and completion. No receipts means “No Engineering activity recorded”; do not fabricate net changes.

Lead with the selected project's actual result and trade-offs: e.g. **Compact Reactor: space 18→15 at output60; credits6→8 / Industry9→12**, immutable new-design version access rather than automatic fleet upgrades. Sensor/lab preview ratings do not promise campaign detection or colony income. The original Advanced Scout project retains its construction warning if still excluded by the integrated catalogue; this is an outcome limitation, not a new legality rule. Selection changes the target; work occurs on End turn, never instantly.

## State and interaction

| State | Presentation | Action |
|---|---|---|
| Empty | “No Engineering projects in this campaign.” Keep overview and Science route. | No fake project picker. |
| Locked | **Locked — 2 Science requirements missing.** Show each named requirement and met/missing state. | Disabled **Start project**, enabled **Review Science requirements**. |
| Available | **Available**, retained progress/cost, actual result. | **Start project**, or **Resume project** if saved work exists. |
| Current | **In progress**; actual progress, even when 0%; show zero income honestly. | Non-action **Current project**; secondary **Pause project** submits the existing clear-target command. |
| Different available selection with a current target | Show both current and selected projects and their retained progress. | **Switch project**; both keep earned progress. Existing profile confirmation behavior applies. |
| Completed | **Completed — 100%** and qualified actual result. | No Develop action. No build/component link without a real supported destination. |
| Busy/combat/stale/draft conflict | Keep observed values and selection, explain the actual access reason. | Disable mutation; never retry, invent success or clear drafts. |

“Paused” is only a presentation qualifier for saved work on a noncurrent project. There is one active target, no new queue, portfolio, currency, pause order or refit rule. Target/clear retain progress; completion alone releases supported effects. Accepted command feedback names the new target and says work advances on End turn. Selection/navigation issue zero gameplay commands.

The R2 mockup uses the **20 actual PR85 definitions**, including their exact IDs, Science/project prerequisites, base workloads and effect text. It defaults to Compact Reactor. All state examples are **hypothetical fixtures**, not live eligibility or earned progress; current is illustrative12/base40 and switch retains another actual goal, Efficient Drive, at illustrative9/base45. Displayed workload is explicitly before campaign pace; final UI must use authoritative adjusted `Cost`. Income/bank remain explicit unavailable placeholders, not invented zero. The old one-project SVG/PNG is retained only as superseded R1 history.

## Binding contract

| Display | Existing source | Limit |
|---|---|---|
| Income, bank, accepted split | `ResearchOverviewSnapshot.EngineeringAllocation / ResearchUnits.Scale`, `EngineeringReserveConceptual`, `EngineeringPercent` | Label income “At current allocation”; not a guaranteed next-turn forecast. Unsent `ReviewResearchDraft` remains separate. |
| Target, cost, work, state | `Target`, `EngineeringCardSnapshot.IsTarget/Cost/Progress/MaturityPercent/Band` | Cost already includes pace. Convert internal units once (600 per point). Screenshot 30 and base-definition 60 are not interchangeable constants. |
| Eligibility | `CanTarget`, `UnavailableReason`, current editable/stamp checks | Access and project state are separate. No UI-derived legality. |
| Science blockers | `Definition.ScienceRequirements`, `ScienceSatisfied` | Discipline names exist; unique discovery names do not. Say **Construction · level 1→2 · discovery 1** and **Propulsion · level 1→2 · discovery 2**. Do not invent named technologies. Current Science cards provide current-interval markers, not arbitrary future schedules. |
| Engineering blockers | `Definition.Prerequisites`, `PrerequisitesSatisfied` | Resolve display names from catalogue; retain diagnostic ID if missing. Totals alone are insufficient. |
| Result | `Definition.EffectSummary` plus existing exact Cycle14 exception | Generic text is not a structured deployment projection. Broader comparisons/result links need a separately agreed backend/content seam. |
| Latest turn | `ResearchOverviewSnapshot.Receipts` | Categories/application IDs/amounts available. No ETA or completion forecast. |

Sources: [actual UI](../../../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewPlayerView.cs), [projections](../../../src/HobbyDS9.Application/ResearchOverview.cs), [playable catalogue](../../../scenarios/playable-fleet-v1/scenario.json), [current contract](../../science-engineering-v1.md), [unexecuted content variant](../../engineering-content-variant-source-20261112.md), [ship-design chapter](../ship-design.md), [LCARS foundation](../../lcars-ui-foundation-v1.md). The ship chapter's weighted portfolio is proposed roadmap; single-active-project mechanics govern this screen.

## Responsive and accessible behavior

At **1280×720**, compact overview and 300px browser beside fluid selected details; keep blockers and action in the first detail view. At **1920×1080**, use a 340px browser and bounded text measure. At **3440×1440**, centre a workspace up to 1640px; widen framing, not every control. At narrow widths or enlarged text, stack browser above details with one page scroll. Avoid nested scrolling traps; no tall summary pushing the only project off-screen.

Use existing amber structure, lavender context, blue controls and ready accent. Every state has a word/icon, not color alone. Body text at least 18 logical px, controls at least 44px, visible focus, reduced-motion compatible and text reflow at 200%. Keyboard-selectable project rows do not submit targets. Tab visits browser, Science link, action, optional Pause and disclosures. Enter/Space follows the existing single-activation/held-key guards. Disabled reasons remain readable without hover. Accepted/rejected feedback is announced without stealing focus. Refresh keeps selection/scroll and unrelated drafts.

## Review and later acceptance

Review the 20-project browser, exact Compact Reactor result and named requirements, then other actual project selections and labelled state fixtures at all three sizes. The mockup has no external services or game connection. After Backend delivers ENG-CONTENT-01, Unity must independently confirm **all20 definitions in the prepared runtime scenario and actual GUI binding**, locked choices visible, exact received IDs/prerequisites/version gates, qualified Scout and sensor/lab outcomes, and viable new-design access. Backend tests alone do not establish these. Full screen implementation still waits for owner design review; existing stamped commands, switch/clear retained work, zero-income state and draft/rejection behavior remain required. No consequential question blocks reviewing this direction.

## Receiving update
Backend delivered ENG-CONTENT-01fd0bb6c2d7ea74479e94f5e4045d8aa02f15e88a. The mockup now copies that actual20-definition array, preserving PR85 requirements/costs and the admitted utility namespace in effect/version IDs; this supersedes the original PR85-only array description above. Current receiving scenario SHA147af76fabf1903a195ab58d571b3ed7c6eb9bad5efd170b13d2656b223cbbb1. State fixtures remain hypothetical and workload remains base/beforepace, not live cost.
Unity independently prepared this content in TURN-POPUP-01-gui combined tree, ran the exact content/byte checker successfully, and authored/compiled EngineeringTwentyGuiTests against the actual prepared resource and actual ReviewPlayerView binding. It checks20 cards including locked Utility Hull goals, names/effects/disabled initial actions, exact resource hash and unchanged frame during rendering. It has not executed; QA must run --hobbyds9-engineering20-gui-tests and verify the actual PB executable/menu/profile at250systems. Source-only tests are not runtime acceptance. Compatible-version access is not a promise of Cycle construction mappings: Advanced Scout remains excluded and sensor/lab outputs remain previews. No full Engineering screen implementation before owner design review.
