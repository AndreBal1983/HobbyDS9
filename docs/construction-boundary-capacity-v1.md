# Construction boundary capacity facts — source/CPU packet

24 October 2026. Unpublished internal helper; no new profile, format reservation,
content, queue admission, save field, Application, host or Unity change.
Existing integrated/generated V1/V2 published construction remains UnsupportedSource.

## Existing journal decision

`PlayableAdvanceCapacity.RequiredSlots` factors the existing staged decision:
one advance, one aftermath when AwaitingCombat, and `4 - NextRound` for each
unresolved human encounter after AI-only resolution. Invalid round boundaries and
unresolved AI-only encounters reject exactly as before. Only this calculation is
called by the existing `CanPublishPlayableAdvance`; it does not execute another
Production phase or introduce new reservations in a live session.

## Unpublished next-boundary facts

`ConstructionBoundaryCapacity.Evaluate` receives actual post-Economy/post-Research,
pre-Production state, its actual post-Production state, the validated complete
staged travel boundary, before/after static and published retained associations,
controllers, expected turn/revision, and technical record/journal budgets.
The trusted future caller must supply that actual staging association. Today's
Planning state and projected Industry are not substitutes.

The helper executes the real `ProductionPhase` on a detached copy, canonicalizes
the result and uses the existing exact protected-phase guards to compare the
supplied post-Production state and movement-proof start. It checks the stamped
boundary, all-owner pending identities, retained ship/queue uniqueness and
append-only identity associations, completion counter delta, and each actual
removed queue head against its newly allocated ship and retained source kind,
owner, fleet, turn and exact static design or blueprint revision. The final
combat boundary may have destroyed that ship: retained production associations,
rather than final live ship count, establish that it completed.

This is **capacity evidence, not a transaction/provenance/authentication validator**.
Existing source/support validation, complete staged movement/combat validation,
shared paid enqueue and replay still belong to the future trusted profile.
These helpers cannot authenticate fabricated historical DTOs or grant authority.
They do not accept a caller-supplied completion certificate or permission flag.

The detached result separates:

- retained static + published records and all-owner pending reservations;
- whether retained + pending fits the supplied technical record budget;
- the exact actual staged advance/combat/aftermath journal slots;
- remaining queue count as an arithmetic one-cancel-per-queue escape reserve;
- whether the batch fits and that reserve also fits after the batch;
- whether **every** pending paid queue completed in this one actual production
  phase. Only that exact completion, with record and journal room, yields
  `AllPaidQueuesCompleteNextStagedAdvance`; otherwise it is `Uncertifiable`.

Cancellation slots do not assert that cancellation is currently legal during
combat, that a live reservation has been protected, or that construction will
eventually complete. No horizon is inferred from Industry. Production completes
at most one queue head per colony; even a second affordable tail remains
uncertifiable. Human and AI queues enter the same calculation without actor
exemptions. Cancellation releases only an actual removed pending reservation;
destroying a built ship does not release retained history.

The 32,768 combined retained/pending-entry helper bound is a detached technical
workload bound, **not a fleet cap or newly adopted campaign capacity**. The earlier
4,096-record validator batch bound likewise remains a technical helper limit.
No live budget value or permanent ship limit is introduced. Future admission
must still settle the general finite-journal continuation problem; these
next-boundary facts do not solve it or enable paid published orders.

## CPU acceptance

Focused tests use ordinary paid construction handlers and the real Production
phase with explicitly labelled component staging fixtures. They cover mixed
human published / AI static simultaneous completion, all-owner reservations,
retained destroyed records, cancel release, incomplete heads and FIFO tails,
stale/forged/incomplete associations, independent exact round costs, journal
edges, Int64 budget edges and detached late production overflow. They do not
claim a new integrated profile, player loop or graphics execution.

Existing playable capacity/rollback/AI remainder and integrated/source regressions
retain the old outcomes. Existing authored initial/built/observed and generated
V1/V2 JSON golden hashes remain exact. See the external packet for actual TRX
counts and source pins. No physical receiving or owner-state operations occurred.

Related: [equipment source packet](campaign-equipment-source-packet-v1.md),
[ship design](design/ship-design.md),
[movement and mobilisation](design/time-movement-mobilisation.md).
