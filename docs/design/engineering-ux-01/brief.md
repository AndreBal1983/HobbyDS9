# ENG-UX-01 — Engineering decision screen

13 November 2026. **Proposed design for owner review. No implementation.** [Responsive mockup](mockup.html); [compact layout drawing](locked-1280.svg). These are design artifacts, not runtime captures.

## What is wrong, and what data actually exists

Reviewed PB `788190d3dcc2c371d23a694e21b13e08454034ee` and the supplied screenshot: one locked Advanced Starship Engineering project, no active target, 0/30 points, two missing Science requirements. The current card repeats its status, uses a huge empty progress bar, hides named prerequisites in a foldout and fills the page with empty project categories. Income and bank are separated in an aside. These are interface problems.

The separate content problem is that this campaign offers only **one application**, no refinements and no civil projects. Its authored “Unlocks Advanced Scout production” effect belongs to the old catalogue. The current Cycle14 UI correctly warns that Advanced Scout is unavailable here. A new layout cannot create a useful reward. Promote that limitation into the result headline; do not replace it with a fictional hull, reactor or performance gain.

PR85 at `e365ec1ecf3d686b114548f805f68f73f742e89b` offers 20 projects in a separate main-based scenario, **not the PB catalogue**. The migration's variant writer is unexecuted source preparation, not received content. No writer is invoked or content integrated by this design.

## Proposed hierarchy

1. Compact overview: **Engineering at current allocation**, **Banked points**, **Current project**. “No active project — points remain banked” is sufficient. R&D allocation opens the existing accepted/draft split controls; it never silently changes allocations.
2. Compact project browser: title, state word, retained progress and result qualifier. Current PB shows one row and “1 application · no refinement or civil projects offered.” Hide empty sections. A larger admitted catalogue can add state/kind filters and counts. Selection is local; highlight never starts a project. Deterministic title/ID ordering; preserve selected ID across refresh.
3. Selected details: result first, named prerequisites, actual cost and retained progress, then one clear action. Keep technical maturity/provenance in a disclosure. **Review Science requirements** opens the relevant discipline without changing shares or targets. No ETA or projected bank spending.
4. Expandable latest-turn receipt summary: separate points added to bank, project spending and completion. No receipts means “No Engineering activity recorded”; do not fabricate net changes.

For the current project, lead with **“No new construction option in this campaign.”** Then: “The authored Advanced Scout effect belongs to the original catalogue. Advanced Scout is not offered here.” Keep authoritative eligibility: this warning is not a new legality rule. If the project becomes eligible, Start remains possible with the limitation adjacent. Selection changes the target; work occurs on End turn, never instantly.

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

The mockup's locked case uses only screenshot facts. Available, active (illustrative 12/30), completed, empty and switch examples are clearly **hypothetical state fixtures**, not earned progress. Cropped-reference income/bank are shown as unavailable, not invented zero; real UI binds supplied values including valid zero. No additional projects appear in the actual PB case.

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

Review the locked current-PB design, then the labelled hypothetical presets at the three target sizes. The self-contained browser mockup has no external assets or game connection. After owner approval, verify read-only selection, exact prerequisites and outcome warning, existing stamped commands, retained work on switch/clear, zero-income current state, completion and draft/rejection preservation. Larger catalogue admission remains a separate assignment. No consequential question blocks review of this direction.
