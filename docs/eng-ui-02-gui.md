# ENG-UI-02 GUI candidate

Superseded presentation details: the [15 November owner polish](eng-ui-02-polish-20261115.md) uses side-by-side columns, hides unavailable projects and replaces native menus. The earlier preview below records the previous all-project/full-width proposal, not the current eligibility or layout contract.

15 November 2026. Source candidate; native acceptance pending. The approved two-section layout supersedes the earlier catalogue/detail split. [Reviewable layout preview](mockups/engineering-two-sections.html) uses the 20 authored project names and proposed presentation categories; its commands are disabled and campaign metrics are labelled placeholders. It is not a gameplay screenshot or native rendering evidence.

Exactly **New engineering projects** and **Technology refinement**, each with independent search/category state. Current content maps 8 NewCapability and 12 Refinement entries; CivilApplication uses the first section, and unknown category IDs remain visible under Other technologies. Categories are presentation suggestions, not approved simulation taxonomy. Tabs remain the navigation between Science and Engineering. The shared allocation control stays in Science's R&D direction panel; the Engineering-only allocation page and redundant crosslinks are removed.

Project cards open the existing LCARS one-use campaign dialog, with background exclusion, frame/actor guard, Escape and focus return. It shows current eligibility, earned progress/cost, named Science and project prerequisites, authored expected capability, and conditional completion estimates. Eligible noncurrent work is estimated **if selected now with accepted income/allocation unchanged**. Bank covers remaining work: next Research phase. Otherwise ceil((remaining-bank)/acceptedIncome), or no finite estimate at zero income. All quantities use internal units before display conversion. This is presentation arithmetic; the simulation still spends work and validates commands.

| Feature | Candidate support |
| --- | --- |
| Authored start/resume/switch/pause | Existing stamped SetEngineeringTarget, one active project, retained progress. Current Colonisation direct policy and other profiles' confirmation preserved. |
| Completion, cost, prerequisites, income, reserve | Actual immutable Research snapshot. `MaturityPercent` is explicitly project completion. |
| Expected output | Actual authored EffectSummary, with current profile construction exclusions. The Research projection has no typed output/version/parent relation; the popup labels that gap instead of parsing prose or inventing links. |
| Equipment maturity / scientific generation | Unavailable. VersionNumber is not maturity and project completion is not equipment maturity. |
| Attribute selection / parent selection / 100–150% repeatable refinement | Unavailable explanation. Fixed authored project actions remain the only executable work. |
| Deployment | No automatic refit, blueprint publication or construction. Current catalogue and explicit publication remain separate. |

## Proposed Backend seam

Add an immutable actor/frame-bound `CaptureEngineeringWorkspace(actorId)` projection carrying accepted income/reserve, turn/revision/content identity, current target, stable project IDs, eligibility, adjusted cost/progress, named requirements and support flags. Typed `ExpectedOutputs` should reverse-join authoritative version access gates to component/hull catalogue snapshots and join static ship-design Engineering prerequisites separately. Each output needs kind, version/design ID, display name, actual capabilities and genuine ancestry where present. No UI title parsing or new save fields are needed for that read-only increment.

Future refinement requires real parent/version state, independent equipment maturity, Science-permitted goal IDs and action-specific reasons. Proposed command: `BeginRefinement(parentVersionId, goalId, contextId, expectedStamp, commandId)`. The simulation validates ownership, retained parent, Science, branch maturity, bounds and paid work. Client supplies no outcome statistics, maturity, RNG seed or refund. This proposal does not implement or approve those rules. See the [design index](design/README.md), [ship design](design/ship-design.md) and [current foundation](science-engineering-v1.md).

## Validation and receiving

Nine portable focused cases cover all 20 source IDs, 8/12 section counts, unknown-category fallback, session-local filter retention/reset, bank/income estimates, zero income and large integer boundaries. Source compiler checks all Review runtime/tests against the pinned Unity assemblies; this does not execute Unity. The existing native render fixture now inspects both sections and the standard popup, including honest refinement limitations. QA still owns actual narrow/wide render, scroll/filter, keyboard/pointer, stale-modal, action/rejection, draft survival and native session checks.
