# Active task: one-use review modal permission

14 October 2026. New bounded investigation from immutable PR #31 `03ed7c3c01796e1c517787547bbf456257dd0ffe`, branch `codex/unity-review-modal-permission`. Do not modify the old candidate/worktree or confuse this with historical UX work.

Finding is initially a source hypothesis: existing dialog callbacks bypass confirmation presence, clear the modal and execute the action; same-frame operations need not change viewEpoch. Extract current callback semantics into a portable actual-source seam used by ReviewEntry, record cancel→queued-confirm / duplicate-confirm evidence before fixing, then verify one-use identity/cancellation/invalidation with the same production source. Do not claim real UIElements event execution.

Ownership: ReviewEntry, minimal new portable gate/meta, linked actual-source focused adapter tests, narrow docs/static/workflow changes if needed. Primary owns compiler-harness changes; do not edit UnityReviewApiCompile or new Primary compiler projects. No backend/default/mechanics changes. Keep dirty/overwrite/busy/privacy/epoch guards.

No Unity Editor/native/helper/desktop authorization; do not execute any. No cache cleanup retry. Safe checkpoint by 06:25 UTC, stop 06:30 UTC (08:30 Amsterdam). Finish stacked draft/exact-head checks and preserve old vectors; unchanged full suites need no local rerun.
