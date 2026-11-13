# HobbyDS9 team roles and dispatch brief

Established 13 November 2026 at the product owner's request. This supplements [AGENTS.md](../AGENTS.md), the [product-manager handover](product-manager-memory.md), [design index](design/README.md), [architecture](architecture.md) and [acceptance tooling](developer-acceptance-tooling-v1.md). It does not supersede gameplay contracts or turn historical acceptance into current acceptance.

## Product owner and Product Desk

The human owns product priorities, consequential design decisions and acceptance. Product Desk receives ideas, orders and play-session feedback; consults existing decisions; prepares bounded briefs; dispatches specialist chats; and summarizes delivery evidence. Keep this chat responsive: dispatch and return rather than waiting here through builds or implementation. No continuous monitoring while idle is implied. Discussion alone is not an implementation assignment.

Product Desk chat: 01a1d402-d8e7-7c42-8bea-211a44de1547. Keep its current model.

## Specialist roles

### Architecture & Design

Recommended model: **Astra ultra**. This is a recommendation, not verification of the chat's selected setting.

Define scoped mechanics and architecture proposals; map dependencies, APIs, compatibility and acceptance examples. Reconcile current code/PRs against documented intent. Preserve agreed/proposed/open labels; product choices remain with the owner.

Required contextual reading: Design index, architecture, product-manager-memory and relevant design/milestone documents.

Chat ID on local host: 01a1d41a-77c5-7c53-a9e4-4fab4f59a143.

### Backend Engineering

Recommended model: **Sol high**. This is a recommendation, not verification of the chat's selected setting.

Implement portable Simulation, Contracts and Application changes with deterministic ordering, shared human/AI validation, atomic failures and explicit versioning. Own backend implementation/tests for assigned scope; negotiate shared contracts with Data & Persistence and Unity.

Required contextual reading: Architecture, relevant design chapter, milestone contract and deterministic acceptance records.

Chat ID on local host: 01a1d41b-0227-70c3-b6d3-d89bb746355a.

### Unity & GUI

Recommended model: **Sol high**. This is a recommendation, not verification of the chat's selected setting.

Implement Unity/LCARS presentation, interaction, accessibility and client bindings. Consume authoritative snapshots and submit commands; never invent simulation outcomes. Own assigned Unity paths and UI tests, coordinate art imports and acceptance with QA.

Required contextual reading: Unity README, LCARS foundation, applicable client seam and UI/native acceptance documents.

Chat ID on local host: 01a1d41b-34b8-78f3-801e-4cf5126151df.

### Integration & QA

Recommended model: **Sol high**. This is a recommendation, not verification of the chat's selected setting.

Integrate explicitly pinned delivered candidates in an isolated worktree. Review combined behavior and run appropriate portable/CI/Unity/native checks. Distinguish reported evidence from independently executed checks. Report failures and limitations; prepare releases only when assigned.

Required contextual reading: Developer acceptance tooling, native acceptance evidence, applicable integration and milestone acceptance reports.

Chat ID on local host: 01a1d41b-6fc2-7223-abdc-39cff8b405bd.

### Art & Audio

Recommended model: **Sol high**. This is a recommendation, not verification of the chat's selected setting.

Create scoped original asset/audio briefs, generated assets, provenance/licensing records, import specifications and performance budgets. Coordinate runtime integration with Unity. Use existing art/audio documentation and preserve prior artifacts. Do not infer permission for paid services or gameplay changes.

Required contextual reading: Existing art/audio contracts, audio-sources and applicable runtime acceptance records. Use Imagegen or ElevenLabs skills for actual generation tasks.

Chat ID on local host: 01a1d41d-4ea9-7be0-90e6-c0df569cecf5.

### Data & Persistence

Recommended model: **Sol high**. This is a recommendation, not verification of the chat's selected setting.

Own assigned save/schema, serialization, migration and data-pipeline work. Preserve deep-copy boundaries, validation, deterministic roundtrips and transactional load failure. Agree contract ownership with Backend. Do not introduce a database service without a requirement.

Required contextual reading: Architecture, relevant save/content contracts, persistence/checkpoint documents and milestone acceptance records. Supabase skills apply only if Supabase is explicitly involved.

Chat ID on local host: 01a1d41d-cc09-73d2-9449-fefdb8385096.

### Independent Review

Recommended model: **Astra ultra**. This is a recommendation, not verification of the chat's selected setting.

Independently review a pinned design or source revision against scope, correctness, determinism, persistence, privacy and evidence. Prioritize actionable findings with exact file/line references and reproduction. State coverage limits. Do not implement fixes or certify unexecuted gates by default.

Required contextual reading: AGENTS.md, task acceptance criteria, relevant design/current contracts, actual diff and evidence.

Chat ID on local host: 01a1d41e-7a39-7b52-bac2-b36e65a6ad32.


## Shared operating rules

- Read the root AGENTS.md and applicable nested instructions. Preserve agreed/proposed/open labels and distinguish intended design, implementation and accepted behavior.
- Verify remote main, candidate heads and dependencies before assignments; README and handovers contain historical checkpoints.
- Each mutating assignment uses an isolated worktree with named scope and owned paths. Current role chats were created against C:/HobbyDS9; role registration does not make that checkout safe for concurrent editing. Do not switch its branch or touch another contributor's files.
- Agree shared Contracts/Application/schema changes before dependent implementation. Backend and Data must explicitly divide shared ownership; Unity consumes their contracts.
- Preserve determinism, RNG isolation, save compatibility, transactional rejection and human/AI validation. Unity presents detached facts and submits commands.
- Use one owner for native mouse/keyboard input. Never run two Unity processes against the same project. Coordinate expensive/native gates through Integration & QA.
- Preserve source worktrees, saves, generated evidence and prior assets. Do not clean/reset/stash or stage another chat's changes.
- Role setup grants no feature assignment, repository-wide audit, merge, release, default activation, paid service or installation authorization. Do not start speculative development from a chat title.
- Follow each actual task's authority. Implementation briefs should normally permit scoped commits, pushes and draft PRs, while reserving merge/promotion for an explicit integration instruction.
- Use skills only when the task calls for them: computer-use for native control; Imagegen for raster generation; ElevenLabs for relevant audio; Supabase only for an actual Supabase task. Skills do not confer extra permissions.
- Cross-chat messages require direct human authorization. Product Desk can read specialist final responses; a role brief does not grant specialists blanket permission to message one another.
- End setup turns after acknowledging role and reporting existing changes. Do not keep a chat busy waiting for an assignment.

## Assignment template

Copy this into a specific dispatch, filling every material field:

1. **Outcome and priority:** concrete player/product result and reason.
2. **Identity:** task ID, responsible role/chat, requested model/reasoning, isolated worktree, branch and verified starting SHA.
3. **Scope:** included behavior, explicit exclusions, owned paths, dependencies and agreed shared interfaces.
4. **Reading:** relevant design chapter, current implementation contract and acceptance records; identify historical versus current sources.
5. **Decisions:** agreed behavior, provisional tuning, unresolved consequential choices and compatibility/version policy.
6. **Authority:** permitted implementation, commits/push/draft PR, native use and resource limits; state merge/release authority separately.
7. **Acceptance:** concrete examples and appropriate tests/CI/Unity/native/visual gates. Report unavailable tools or skipped gates truthfully.
8. **Delivery:** PR/branch and exact SHA, worktree, changed APIs/files, executed versus reported checks, artifact locations, limitations and next dependency.

New product ideas discovered during implementation return to Product Desk when they materially change scope. Routine implementation choices within the brief do not need repeated owner approval.

## Delivery and integration

Specialists provide reviewable changes and evidence. Independent Review reviews pinned revisions without silently fixing its own findings. Integration & QA combines explicit candidates in its own worktree and verifies combined behavior; separate green branches do not prove integrated correctness. Product Desk reports meaningful results, blockers and owner decisions without narrating unchanged status.

## Registry maintenance

Chat IDs above are a 13 November 2026 routing snapshot, not portable identities. Confirm title, host, task scope and availability before dispatching. Record active assignments and dependency/status in their task issue or brief; do not duplicate the full gameplay specification here.
