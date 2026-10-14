# Campaign tactical v2 UX specification review

Review date: 13 October 2026. Reviewed local UX specification commit `bb8982682f4440ef93aba3c164d28609ec893f02` at `C:/Users/ambal/.codex/worktrees/campaign-tactical-v2-ux-spec/HobbyDS9/docs/campaign-tactical-v2-playable-ux-spec.md` and combined acceptance report for [PR #18](https://github.com/AndreBal1983/HobbyDS9/pull/18).

This is a product/document review, not source-code or independent native acceptance. The UX specification remains proposed; its eight open choices are not approved by reviewing them. No implementation or merge is authorized by this document.

## Review outcome

The specification is a useful design basis for the eventual Unity slice. It separates real backend capability from proposed controls, restricts the scenario to the agreed initial scope, and gives explicit round/order/resource/save/finalization examples. Six textual wireframes are proposed layouts, not visual acceptance. Current campaign v2 remains disabled.

The combined candidate records 1,358 .NET tests, 116 Unity EditMode tests, Windows IL2CPP, real native publication/construction/save-load and unchanged determinism. GitHub API verification confirms PR #18 is draft/unmerged at `22096fb56d81d203f194a6c0407d7de76721d93c`, targeting main `703e395b793af41953d440d04d69e0744909caf5`, and [CI 38416668428](https://github.com/AndreBal1983/HobbyDS9/actions/runs/38416668428) succeeded on that exact head. Native evidence was read, not reproduced here. Fresh physical audio/GPU observations are outside the completed required acceptance, and no new audio/graphics changes are reported.

## Proposed revisions before UX implementation

1. **Saving should not require discarding unsent edits.** Wireframe E currently makes discard-and-save the only save option when the planner is dirty. Prefer “Save committed battle state”: save the authoritative checkpoint and keep the local order draft in the running session. Explain that unsent edits are not stored and will be lost on successful load/restart. Cancel and explicit discard remain available when appropriate. Failed save must retain both the valid prior save and local edits. This is a proposal for owner review, not an implemented save contract.
2. **Reconcile the specification against the accepted combined checkpoint before implementation.** Its documentation branch starts at the older local main. Preserve the distinction between recorded source inspections and current runtime capability. Replace machine-local planning links with repository-relative references when those documents are committed alongside the specification; do not claim that untracked plans are distributed project documentation.
3. **Explain tactical contacts without promising a new fog-of-war system.** Actor/controller ownership and withholding fresh opponent orders are mandatory. Exact visible enemy geometry/equipment remains D4: the first scenario needs an explicit lawful observation policy. Avoid silently expanding this slice into strategic fog-of-war or rendering an omniscient standalone event stream as player intelligence.
4. **Keep target legality separate from immediate fire opportunity.** The specification gets this distinction right; retain it in final contracts. A concentration preference is not a locked ship target, subsystem attack or guarantee of firing through screening. Do not duplicate acquisition rules in Unity.
5. **Keep maneuver exclusions enforceable in Simulation.** Basic Defeat objectives are insufficient alone because the standalone Disengage maneuver can extract under another objective. The scenario order validator must reject every excluded exit path. Menu visibility is presentation, not authority. Fighting Withdrawal must remain in-battle distance change.

## Decisions to resolve by stage

| Stage | Relevant open choices | Treatment |
| --- | --- | --- |
| Equipment foundation | Explicit per-ship capabilities, fit/power/stock definitions and versioned authored source | Ready for the prepared scope; no formation aggregation or campaign activation invented |
| Campaign lifecycle | D1 survivor geography/contact; D2 formation/geometry/mixed-source routing; D4 observation/baseline AI/joint commitments; D6 saves; D7 current-batch reports | Resolve before commissioning the lifecycle; supported identities follow actual contracts |
| Unity interaction specification | D3 retained lost targets/protection; D5 menu/Pursuit; D8 warning/report/confirmation details; revised unsent-edit save behavior | Confirm against accepted lifecycle projections, then finalize Unity prompt |

Suggested design-round order: formation/source mapping, then initial geometry and lawful observations, then nondecisive round-three aftermath/contact, then baseline orders and persistence/report details. Questions should use real authored equipment examples once available. Do not settle detailed rating formulas without those inputs.

Proposed defaults for later discussion: retained target/protection intent with lawful backend fallback, practical maneuver names first, exact stocks with zero-stock warnings, current-batch round reports retained across checkpoint saves, and no campaign-long report archive. These remain proposals; no response means no agreement.

## Next assignments

The immediate integration work is complete as a candidate. A later explicit merge instruction should finalize PR #18, preserve source histories and verify exact-main CI after merge. Do not rerun the entire already-passed acceptance without a changed executable input or unresolved failure.

After the combined checkpoint is accepted, Codex 1 can receive the [authored equipment foundation prompt](prompts/tactical-v2-authored-equipment-foundation.md). It must keep campaign activation disabled and v1 blueprints unchanged. Codex 2 can revise its documentation using this review and prepare examples against the new equipment contract when delivered. Neither task requires another visual redesign, tactical runtime UI or cinematic implementation yet.

The UX document should be retained as a separate reviewed documentation change; it is not automatically included in PR #18. No messages were sent to either chat by this review.
