# Belt bodies and finite mining — explicit campaign edition

19 November 2026. Implementation candidate for the owner-authorized UX/celestial-body brief. Read the [brief](ux-playtest-20261119.md), [display policy](display-policy.md), [refinement contract](engineering-refinement-v1.md) and [Architecture contract at85f3388](https://github.com/AndreBal1983/HobbyDS9/blob/85f3388de64d834dbc978ac6f5b860886dfc6c4d/docs/design/belt-bodies-extraction-20261119.md). Source/portable acceptance and native acceptance remain separate in the receiving receipt. This is a bounded mining slice, not the full economy/habitat roadmap.

## Bodies, knowledge and preservation

One aggregate body corresponds to each existing belt feature. `BodyId = "body." + FeatureId` is a one-to-one adapter; rendered individual rocks have no gameplay identity or deposits. Existing Sol and generated belt FeatureId/SystemId/radii/VisualSeed and all planet generation remain unchanged. Belts remain outside GameState.Planets, FrozenColonisationPlanets, surface settlement and population records.

A new pinned hash domain generates one finite Dilithium deposit per aggregate, without touching campaign RNG or allocation. The provisional amount is120/180/240/300/360 units. Sol always has a positive deposit. Selection, preview, drawing and survey grant no ore or Science reward.

Existing current coverage/retained arrival rules disclose existence; chart adjacency does not. Resource amounts and unassisted habitability are absent until a completed real survey. A living owned stationary ship with powered StudyWork>=1 participates for two weekly ticks. Belt, planet and active/awaiting-claim site-study tasks share exclusion. Cancellation/interruption reset unfinished work, consistent with the current planetary survey edition. This does not silently implement the separate proposed persistent-survey framework.

Surveyed habitability is0 basis points for unsupported unassisted biological habitation in vacuum. It is not a claim that all species have identical preferences. No natural settlement capacity or supported orbital habitat is created. Ordinary camp/planet settlement commands reject belt IDs. The mining rig is uncrewed and provides no housing, population, food or system sovereignty.

Current coverage projects live remaining ore using a detached observation; remote values retain their last observed amount/turn. Observation records refresh after Economy and Movement as well as the completed-week boundary, including a combat pause. Observation and resource authority remain distinct. Unknown is null; known exhausted is0.

## Paid construction and real extraction

The player's order fixes one surveyed belt and an owned populated Established same-system sponsoring colony. The first version admits one queued/built installation per aggregate globally; queued cancellation can release that reservation, and completed installations cannot be deleted/retargeted through Cancel. This avoids mixing ownership of the single local ore buffer. This exclusivity is versioned implementation policy, not a broad territorial claim system.

All numbers here are **proposed, pinned tuning**, not owner-approved numeric canon:

- Setup is60 Industry,0 credits and no material ingredient, following the existing frontier-mine work-cost precedent. `ColonisationProductionKind.BeltMining` is an explicit target-bearing recipe in the existing mixed FIFO, not a second Industry authority or fake planet infrastructure.
- The closed recipe supplies its own3power for3demand. It is uncrewed and does not draw distant colony power. Colony infrastructure/habitat is unchanged.
- Blocked deployment retains full paid work at the FIFO head until local freight access is clear. It produces no ore or next-item work cascade. Cancellation retains spent work and consumes no unspent resources.
- From the next Economy phase after actual deployment, extraction is `min(3, remaining deposit, 12 - local stock)`. Deplete the shared body deposit and credit the owned installation buffer exactly once. Full storage or depletion pauses extraction without destroying material.
- The standing mining order requests delivery of its actual ore to the frozen sponsor, up to3 units/week. It does not require an unrelated ship-production deficit. Ordinary food/material freight has priority; delivery uses the SAME residual sponsor endpoint dictionary after that pass.
- Same-system Path uses the existing node, blockage, interference, phenomena and stability rules. No extra inter-system edge or freight budget is created. Delivered amount is debited from mine stock, credited to exact colony stock and consumes actual residual endpoint capacity. Separate typed belt shipment receipts avoid invented planet/colony origins in LastDeliveries.
- Invalid, lost or unpopulated sponsors pause construction/extraction/delivery without retargeting, ownership transfer or refund. A blocked haul retains mined stock in the12-unit buffer. A depleted mine can still ship its remaining stock.

Conservation extends the existing complete invariant: initial planet/belt deposits and actual colony resources equal remaining deposits, mine/colony/cargo stocks, reservations/consumption and existing sinks. Setup work, extraction and delivered amounts have distinct history. ProductionCompleted counts a deployed mine only when its real completion receipt exists.

## API and transactional behavior

Optional `ICampaignBeltBodiesSession` exposes CaptureBeltBodies(actor), PreviewBeltCommand(command) and SubmitBeltCommand(command). Capture is immutable actor/turn/revision-bound data. Commands carry observed stamps, BodyId and explicit intent; they cannot specify desired ore, yield, work or completion.

Actions are StartSurvey, CancelSurvey, EnqueueMine and CancelMine. Eligibility uses shared human/AI controller-neutral checks; no new mining AI planner is supplied. Reject duplicate/stale/foreign/invalid-phase commands before mutation. Detached transaction copies preserve rollback of allocated identities, work, resources and journals.

Host SupportsBeltBodies reads frozen source identity, independently of capture/busy state. BeltBodyInteractionWork/Completion/ControllerUpdate use the same copied-command worker, exclusive lease, cancellation and exact-frame publication pattern as refinement. GUI owns its ReviewUiModel wrapper, body selection/picking/details and confirmation flow. Refinement support routing explicitly includes the new edition.

## Versioning, bounded replay and receiving

New peer package5/root18 (`generated-belt-bodies-v1`) preserves all Format17 refinement state and support-v5 behavior. Use BeltBodiesPlaytestOptions.CreatePackage(growth); new save suffix is `-belt-bodies-v1`. A new campaign is required. Old17 and earlier roots/slots remain closed; no migration or silent decorative-belt promotion occurs.

The root persists definitions, deposits/local stock, observations, survey records, installation/queue lineage, cumulative extraction/delivery, last-week shipments and stamped belt operations. At each ordinary journal offset, merge belt and refinement operations by unique ExpectedRevision; exact actor/controller/stamp validation then replays the whole state. Separate fixed replay blocks are forbidden. Complete-root comparison rejects tampered ore, observations, history or order.

Caps:1000 bodies/deposits,4000 observations,4096 survey records,2048 mine records,4096 belt commands,1000 last-week shipments; the existing64 refinement-project/1024 refinement-command caps remain. All fields share the existing2 million-visit/64MiB root budget; fixed visitors and strict typed JSON preflight run before copying/decoding. Tuning, limits and policy identities participate in the new package hash.

QA receives exact Backend source plus independent GUI integration, re-prepares current DLLs, builds/seals the exact combined SHA and performs native proof at1920×1080. Smaller windows are best-effort. Native gates include selection without mutation; unknown→two-week survey; real paid work; next-week extraction/stock delivery; shared resource conservation; Save/quit/cold Continue; unchanged Sol visuals and working Engineering Accept/Continue/Discard. Do not infer native acceptance from the portable tests or source compile.
