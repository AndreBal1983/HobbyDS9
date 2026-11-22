# State repository implementation authorization — 22 November 2026

Owner instruction: “Let's go with Architecture's recommendations. Have the team start implementing the database and save system in phases, preserve existing saves, and keep the usage restriction in place.”

This approves the recommendations and phased implementation in [the reviewed ADR](state-repository-savegame-adr.md), exact review source `55a0c67b1a96d965c6a9c0214dc7f60d85743efe` / PR132. It supersedes the assignment's architecture-only hold for the following scoped implementation. It does not claim code is implemented, authorize multiplayer, or remove the ADR's evidence gates.

## Accepted direction and defaults

- Offline-first embedded per-campaign SQLite, immutable complete existing-root checkpoints first, branch/save references, reconstructible catalogue, and exact JSON compatibility/import/export with originals preserved.
- No account, database server, cloud requirement, direct SQL in the portable core, command-only replay shortcut, or automatic gameplay-format conversion.
- Ten rolling autosaves and three milestone points; 512 MiB soft automatic-storage budget. Manual saves protected, no arbitrary three-slot cap. Follow ADR behavior when the soft budget cannot accommodate protected/recoverable data; never silently delete manual saves.
- Completed-turn and valid combat-safe autosave boundaries with changed-state coalescing and bounded memory. Continue singleplayer after autosave failure with a persistent truthful unsaved warning; explicit manual Save/Save-and-Quit failure keeps the session and offers the ADR's recovery choices.
- Branch when resuming older history, retain independent futures and manual pins. Quick Save remains deferred.
- Full payloads initially; chunk dedup, engine delta/copy refactors and multiplayer remain separate later work requiring their own evidence/scope. Provider selection and measured performance budgets remain technical review gates, not invented accepted results.

## Execution and ownership

Architecture leads phase contracts, dependency order, go/no-go review and integration of this authorization into the ADR's agreed/proposed/open labels. Keep measured limitations and unresolved native/provider evidence explicit.

1. **A2 + B1:** Data & Persistence owns the isolated host repository/schema/transactions/operation-receipt and recovery prototype with tests. GUI and QA own a narrowly isolated native-provider packaging probe in the real Windows x64 Unity IL2CPP environment. Architecture sets one compatible provider recipe and disjoint source ownership before these converge. No production default switch.
2. **B2 and compatibility:** Data & Persistence owns validated immutable checkpoint restore, branch references, legacy import/export, backups and failure injection. Backend supplies version-specific carrier/restore validation and authoritative capture boundaries; keep Format17/18 refinement and belt sidecars complete. Never mutate owner saves to make fixtures.
3. **C1/C2:** GUI implements scalable save-manager and failure/recovery UI; Backend implements the host/session capture and coalescing seam while persistence stays in adapters. Start independent specifications/scaffolding early, activate only on tested dependencies. Protect session epochs, bounded queue memory, idempotent retries and unknown-commit resolution.
4. **D1:** QA integrates exact reviewed commits into an opt-in player, verifies offline new/save/load/recovery/quit/cold reopen and legacy-original preservation, with measured reliability and native packaging evidence. Review at 1920×1080; 720p is not required. Retain a verified JSON fallback and rollback route.
5. **D2:** Default production activation remains a separate rollout checkpoint after independent receiving review, as recommended by the approved ADR. Prepare a concrete accepted candidate and evidence before raising that final decision. Do not block authorized earlier implementation asking for approval again.

No main merge, rewrite of current sealed players, destructive cleanup, multiplayer implementation or broad engine mutation refactor is authorized here. Choose compatible source bases explicitly: the ADR is on newer main documentation, while the working player is Format18/package5 `2fc0dbca46073121379dd9491c859a02a6f22dcd`. Do not roll it back to main's core14 by accidental branch ancestry. Coordinate with the ongoing UX native acceptance; QA alone owns native desktop/Unity execution.

## Delivery and resource controls

Each phase yields a small reviewable PR/commit, dependency/ownership record, exact runtime/provider pins and licenses, focused tests and truthful limitations. Go forward autonomously when the agreed gates pass; pause dependent activation on real failed gates while independent work continues. A headless SQLite proof is not native IL2CPP acceptance; a successful DB commit is not proof of complete game restore.

Preserve all owner JSON files byte-for-byte by default. Imports are validated and idempotent, use separate destinations and recovery copies, reject unsupported versions explicitly, and never delete originals or silently upgrade core14 to18. New gameplay storage schema version is separate from game format/content identity.

Current account use:35% consumed. Shared checkpoint72%, hard stop75%, reserve25%; check before sustained work and at handoff. No reset or paid credits to bypass the cap. Batch pushes, preserve CI, use isolated worktrees, keep Product Desk responsive, and report milestones/failures rather than repeated unchanged snapshots.
