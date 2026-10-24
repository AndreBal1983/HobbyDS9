# Unpublished multi-turn construction journal reserve

25 October 2026. Source/CPU helper only. No new profile, format reservation,
admission, live metadata, source/production rule, save shape, Application, Unity or
player operation. Existing V1/V2 published construction remains UnsupportedSource.

`ConstructionJournalReserve.Evaluate` receives the **actual frozen/rebuilt**
playable package (not its pre-generation template), expected package hash, current
or staged physical candidate, retained static and published record lists, journal
usage, operation kind and technical record/work budgets. The expected hash is
consistency binding supplied by a future trusted caller; matching a caller string
does not authenticate content or confer authority. Saved support/reservation data
cannot authorize anything. No new generic encoder, persistent token or cache is used.

The immutable result binds exact package/content/rules identity, turn, revision,
outer phase, proposed operation and delta. It contains copied floors and scalar
facts only; it must be recalculated on every candidate. `Certified` means the
record and journal-capacity inequalities hold within the supported closed-rule
domain. It is **not** a transaction, full validation, authority or universal
simulation-liveness certificate.

## Closed current rules

The named internal calculation revision is
`construction-journal-current-ship-only-v1`, not a registered campaign profile.
Its premise is the currently reviewed fixed command/phase set: research/Engineering,
publication, static or future opt-in blueprint enqueue, cancellation, governance,
physical travel, cloak, nonempty fleet organisation, at most three tactical rounds
per encounter with one controlled human, advance and aftermath. No current command
changes installed infrastructure, Automation or colony ownership. Those exact
candidate fields and world identities are compared to the actual initial package.
Package/scenario/physical/core identities and actor controllers must agree.

Full candidate/source/catalogue/payment/provenance validation, nonempty and unique
ship/fleet membership, disjoint encounter participants, valid political/cohort/world
shapes, and no concurrent caller mutation remain trusted-caller prerequisites.
The helper additionally checks inexpensive premises, work ranges and retained/
pending identity uniqueness. It does not silently call old integrated admission
to reject a future published queue that the ordinary validator can represent.
Adding conquest, infrastructure changes, a new Industry modifier, extra controlled
humans, longer combat or multi-entry operations invalidates the domain until review.

## Floor, horizon and journal arithmetic

For each world with pending work, actual `WorldEconomy.Project` runs on a deep
copied world for every actual catalogue governance policy × every defined
ResistanceState × disruption 0/positive. Other state and frozen definitions are
read-only; Project does not write them. The minimum preserves every existing
integer division and power-shortage effect. No measured floor, 7,500/5,000 political
multiplier, projected current Industry or copied combat-stat formula is substituted.
A zero floor is Uncertifiable. The current catalogue may change a policy and must
be supplied as the approved immutable package; hash text alone is not approval.

For colony c with positive floor I_c, the FIFO horizon is the sum of
`max(1, ceil((RequiredIndustry - CompletedIndustry) / I_c))` over **each** item.
The global H is the maximum across all owners' colonies. No leftover Industry is
carried to a tail; work ranges and distinct queue identities must be valid. Queues
retain their actual paid/source revision facts: publishing R2 never changes R1's
work. The future source validator still proves those costs and frozen semantics.

Let S = all live ships and P = all pending queues, without actor/source exemptions.
Potential ships = S+P; nonempty fleets and disjoint encounters imply
`E <= floor((S+P)/2)`. A future batch reserves `B = 2 + 3E` journal slots (advance,
once-only aftermath, up to three human round entries per encounter). Splitting a
fleet does not inflate this ship-based bound. Planning reserves H*B; existing
AwaitingCombat adds exact unfinished human rounds and batch aftermath using the
accepted `PlayableAdvanceCapacity.RequiredSlots(candidate)-1`. No empty-batch
assumption or doubled proposed advance is used.

The inequality is `used + proposedDelta + currentDebt + H*B <= packageLimit`.
The closed operation enum supports an already-staged observation (delta 0), one
ordinary optional player operation (delta 1), and internal AI procurement (delta 0
in the player journal). The latter still enlarges P and must recalculate all
liability before payment/publication. The enum is a description from the trusted
caller, not a user-selected permission flag. Future multi-entry kinds reject.
Record capacity remains separately `retainedStatic + retainedPublished + P <= budget`;
destroyed retained ships still consume history and cancellation only removes P.

## Bounds and limits

65,536 is this helper's supported **technical journal domain**, matching the current
playable implementation domain; it is not a newly adopted campaign duration or
fleet limit. Work input must be 1..10,000,000 charged units. Array/count scans,
fixed-input comparisons and conservative per-projection copy/lookup/sort/cross-
product costs consume that budget before enumeration; exhaustion returns an
Uncertifiable result with work used. This is a bounded CPU calculation, not a
wall-clock or hardware-performance claim. DTO validation/ingress bounds still apply.

Horizon/reserve and turn/revision/allocation estimates use BigInteger and bounded
downcasts. Conservative headroom includes remaining player operations, automatic
AI orders/rounds, pending ship/fleet creation, encounter allocation and the existing
cohort ceiling. A direct food-stock upper-bound check catches provable near-overflow
states. Invalid work, identity, fixed inputs, zero floor, unsupported horizon or
near-exhausted counters produce Uncertifiable, never partial permission.

These are journal-capacity and directly provable numeric preconditions, **not a
general proof that every economy/research/technical operation cannot fail**.
Existing full advance validation/checked arithmetic and atomic rejection remain
in place for unrelated errors. The previously documented synthetic food-at-Int64Max
DTO is not claimed reachable through a trusted historical root. This packet does
not broaden into a generic arithmetic verification framework.

## Tests and future integration

CPU cases compare floor enumeration against actual Project over all disruption
values; cover zero policy/power/integer rounding, all-owner FIFO/no carry, source
R1/R2 publication, record and operation boundaries, immutable results, identity/
fixed-field changes, work/turn/revision/allocation/food exhaustion and internal AI
procurement. Actual existing Application operations exercise paid enqueue, research,
publication, travel and organisation; actual advancement, every human round and
aftermath show reserve conservation. A real fleet split leaves the encounter bound
unchanged. Component production staging is clearly separate from a newly admitted
campaign. Test-only funded/AI-affordability fixtures are not new live balance.

A future NEW opt-in profile must integrate candidate checks atomically before
**every** optional player operation and internal AI procurement, preserve earlier
paid liabilities, and recalculate through exact operation replay from trusted
package metadata. Mandatory progress consumes reserved space; optional AI procurement
can be skipped before payment if it cannot preserve space. Current AI is deliberately
unchanged by this uncalled helper. No saved reservation is trusted, no old root is
converted, and frozen map load never regenerates geometry.

Related: [next-boundary capacity](construction-boundary-capacity-v1.md),
[equipment sources](campaign-equipment-source-packet-v1.md),
[ship design](design/ship-design.md), [movement](design/time-movement-mobilisation.md).
