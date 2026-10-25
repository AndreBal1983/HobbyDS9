# Activities campaign with streamed construction

Status: source/CPU candidate. Actual Unity, player-build and native acceptance are
pending. Provisional content remains reversible playtest tuning, not final balance.
The owner's [normal production agreement](design/time-movement-mobilisation.md#normal-production-and-material-supply--agreed-25-october-2026)
supersedes upfront construction payment for this new path.

## Explicit version and compatibility

Format10 uses `generated-campaign-activities-streamed-v1`, policy
`generated-activities-streamed-production-v1` and embedded marker
`generated-campaign-owned-core-activities-streamed-v1`. Its typed package wraps
the pinned three-class/activity content with explicit cumulative-consumption,
same-Economy freight and terminal-liability policy identities. Existing generated
V1/V2, Format8 and the separately sealed Format9 source prototype retain their
typed roots, hashes, stores and rules. There is no conversion or fallback.

The source menu makes the new campaign the first expanded New/Continue group.
Earlier generated campaigns retain their exact actions and distinct slots in the
collapsed earlier-campaign group. The superseded Format9 prototype retains only
clearly labelled Continue controls; its portable factory remains available.
No accepted executable is replaced by a source change.

## Normal queue and physical inputs

Ship and structure orders share one FIFO per colony. Queueing validates the
controlled actor, current stamp, owned world, actual yard/current source access,
frozen revision, supported equipment and technical capacity. It allocates the
order without charging credits or deducting materials. Empty local stock does not
forbid an order: the world displays **Awaiting materials** until work is possible.
Compiled credit values remain source facts, not normal construction prices.
Credits still fund the existing expedition and aid choices. Buyout is not added.

Streamed orders have their own required/consumed resource vectors and completion
receipts. They do not fabricate prepaid queue fields. For requirement M, work W
and completed work w, cumulative consumed material is ceil(M*w/W), with zero at
w=0. All positive requirements are unique and ordinal; every consumed vector
equals that formula exactly. Intermediate products use BigInteger. Production
chooses the maximum integral work affordable from actual local stock, up to actual
Industry and remaining work. First positive work therefore needs one unit of every
positive-cost material. Later work within that already consumed integral quantum
does not charge it again. No resource is consumed twice at completion.

Economy derives only the current head's next-work tranche and gives existing
physical logistics max(authored stock target, construction tranche). This input
is ephemeral; it never changes DesiredStockpiles. Extraction precedes logistics;
food priority, same-owner knowledge-filtered routes, physical donor surplus,
shared endpoint/edge capacity, stability and hostile interference remain real.
There is no Planning import or empire warehouse. The supported abstraction is
delivery within the same Economy pass, not freighters or multi-week cargo transit.

Production recomputes actual Industry after Economy and only spends stocks that
actually arrived. A blockade can stop future deliveries. Already local buffers
remain usable; when the next required quantum is missing, progress pauses and is
retained. Supply restoration resumes it. Cancellation loses completed work and
consumed materials. Unconsumed stock never left the world, so it needs no refund.
One head receives Industry, at most one item completes per colony/turn, no carry.
Completed structures affect output in the following eligible Economy cycle.

## Transactions, capacity and replay

Demand, delivery, queue progress, consumption statistics, completion effects,
equipment/pools and retained provenance, structures, activities, AI changes,
identities, RNG and journal publish atomically. Phase guards permit only the exact
streamed stock delta and receipted structure additions; unrelated strategic facts
remain protected. A late provisioning/copy failure discards the whole candidate.
Completed and destroyed ships retain frozen source/provenance records. R2
publication and later access changes never upgrade a queued R1.

Resource interruption may stall forever. This policy therefore makes **no finite
completion horizon guarantee** from Industry. It protects one terminal journal
entry for every pending cancellation/finding plus exact remaining combat debt.
Actual next advances stage the real phases and must leave the exact required
round/aftermath entries and terminal liabilities affordable. Optional human and
AI candidates cannot consume that reserve. Terminal cancellation/claim/abort does
not need a new future advance reserve; its action releases its own liability.
Revision headroom protects those actual increments separately. Trusted scenario
starts require revision zero; a defensive counter-edge test is explicitly fault
injected, not evidence of a reachable full-replay near-long-max campaign.

Technical journal exhaustion may stop further advances. Reserving slots for an
AI order does not grant the human control over it, and this increment does not
claim that every AI terminal slot can be manually drained after exhaustion.
Automatic AI procurement uses shared admission and post-candidate capacity;
there is no actor-switching escape, free completion or journal compaction.
Ordinary transactional arithmetic failures remain possible and reject atomically.

The strict carrier and separate `HobbyDS9CampaignStreamedActivitiesOnly-v1` store
are not domain authority. Full Load replay uses the approved external package and
saved frozen map, then compares the complete resulting typed root. Save/load
validation runs on detached worker state; publication replaces host/frame only
after full validation. The existing one-use operation ownership, overwrite race
checks, stale completion rules and draft retention remain in force.

## Player observations and acceptance

Snapshots contain only own orders, stocks, relevant own delivery receipts and
actor-observed sites/relations/findings. Work-with-current-stock is a current
advisory projection, not a delivery promise or command permission. The map warns
before a route ends an owned active or awaiting-claim expedition; the captured
route/stamp is submitted only on confirmation. Ordinary navigation without such
an expedition retains its existing interaction.

CPU evidence covers unpaid frozen-R1 completion, gradual integer consumption,
physical delivery/blockade/buffer/resumption and donor protection, partial cancel,
late rollback, exact full replay and legacy production; actual lawful combat
through rounds/aftermath with an unfinished order; actual 250-system actor-known
science and minor consent; strict carrier/store rejection; typed host/model
save/reopen, dirty drafts and nonblocking held-IO/stale-operation behavior.
The deferred popup test proves model/root behavior; actual pointer/button Cancel
and Confirm, readable three-size panels, imported hulls and a new player package
still require separate Unity/native receiving evidence.

The prior [Format9 contract](campaign-activities-v1.md) describes that preserved
prepaid source checkpoint and must not be read as this streamed policy.
