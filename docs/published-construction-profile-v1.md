# Explicit portable published-construction profile

Status: **provisional playtest source implementation for review**, not a validated
Unity/player delivery. The separate typed `CampaignPublishedConstructionApplication`
accepts an explicitly supplied base template and support catalogue. A separate
opt-in menu, typed host, strict codec and distinct slot store now provide source
integration. Existing default campaigns, V1/V2 roots and their `UnsupportedSource`
decisions remain unchanged. No package has been rebuilt for this increment.

## Identity and replay

The new outer DTO uses Format 8 and
`generated-campaign-published-construction-v1`. Its policy is
`generated-static-centres-published-support-v1`; the embedded core has its own
`generated-campaign-owned-core-published-construction-v1` marker. The wrapper
identity binds the supplied template identity, exact explicitly encoded support
metadata hash, retained-record budget, actual replay-operation limit, closed journal
rules and compiler version. The actual limit is encoded explicitly even when the
supplied historical base-template hash is identical.
Each generated package domain additionally binds the frozen Natural map hash.
These hashes establish consistency, **not authority**: the composition root must supply
the approved external template/support package. Saved metadata cannot approve
itself. The explicitly reviewed provisional playtest table is authored in
[`scenarios/published-construction-v1`](../scenarios/published-construction-v1/README.md).
Its exact Unity resource copy uses the same bytes; no test helper is a runtime
content factory. The support compiler's historical `provisional-test-only` wire
label remains unchanged; this is reversible playtest tuning, not final balance.

Load validates the stored geometry and reconstructs this new domain from that
geometry; it never regenerates a map from its seed. It replays the complete
accepted-operation transcript and compares the nested state, discovery receipts
and typed published-built provenance. Partial export through inherited historical
roots is rejected. A marker change does not convert an old root.

## Publication, payment and equipment

Publication continues through the existing stamped command and immutable registry.
Queue admission uses the same actor, access, yard, payment and source checks as
ordinary construction, plus exact tactical-support eligibility. Later completion
derives R1 from its publication-frozen catalogue even if R2 or later unlocks exist.
There is no duplicate publication registry and no current-access recheck at
completion. The original source blueprint and paid queue remain authoritative.

Static and published completion records share identity/capacity checks. The typed
published record retains owner, queue, ship, original fleet, turn, blueprint
revision and exact source/support derivation identity even after destruction.
Actual pre/post-Production association validates each new record. Pools start at
the compiled source's exact values: unsupported shields/ammunition grant no stock;
published no-cloak is explicit. Authored source/cloak validation is unchanged.
Observation, tactical validation and playback resolve the same retained source;
playback reports the actual published blueprint identity rather than a cruiser
alias. No fixed authored tactical fit is cloned into the new class.

## Atomic operations and capacity

Only this profile stages each mutating operation in a private session fork. Fixed,
privately owned metadata may be shared; all mutable state, source containers,
receipts, organisation, history and transcript are detached. The existing command
and phase algorithms execute on the candidate. Full validation, reserve checks
and all potentially throwing copies finish before one gated publication of the
prepared fields. Failure leaves the previous complete save unchanged. Internal
fault probes cover command/provision and late transcript/cache-copy boundaries.

Every accepted human optional operation pays its actual journal entry before the
candidate's remaining mandatory debt is certified. AI uses the same command
validator and tests each detached procurement candidate before retaining payment;
the enclosing advance is independently checked before publication. The accepted
`ConstructionJournalReserve` calculator covers all owners' queues, exact pending
combat debt, FIFO completion horizons and mixed retained-record capacity. Its
65,536 operation and record domains are technical support bounds, not new fleet
or balance rules. The food check is only a per-queued-world direct check over that
world's own completion horizon; it is **not global-horizon economy safety**.
Ordinary unrelated arithmetic/phase failures still reject transactionally.

## Receiving evidence and limits

Portable tests exercise paid frozen-R1 completion through R2, mixed static and
published ships, wrong actor/stale rejection, forged marker/package/provenance,
caller mutation isolation, capacity-gated AI and human orders, and injected late
failure/retry. An actual Natural 250/42 campaign drives commands solely from
actor-known snapshots: built beam escort, exploration, return to its own colony,
lawful guard encounter, every-round reload, truthful playback, aftermath reload
and a new strategic order. Full roots are used only for audit/replay assertions.

This is CPU evidence, not Unity/AOT, visual, native interaction or responsiveness
acceptance. Full replay remains expensive; no validation was removed to reduce
its time. Actual Unity import/receiving, strict build and native validation are
still required before delivering this source increment to a player.

## Explicit player boundary

The construction-playtest foldout uses the existing seed/settings and a distinct
`HobbyDS9PublishedConstructionOnly-v1` namespace with
`unity-published-construction-v1-` filenames. The original New/Continue controls
retain their profiles. There is no fallback, conversion or outer-root adapter.
Strict Format8 carrier admission requires the embedded construction marker; all
host Load/Reopen paths additionally complete external-package validation and
authoritative replay before publishing a new host/frame/presentation.

The designer's optional construction foldout lists only this actor's published
revisions and own colonies. Its detached eligibility observation includes the
actual shared validator's reason/cost, tactical support and the result of running
the paid command on a detached canonical post-payment candidate. Capturing it
changes no money, IDs, transcript or RNG. UI admission binds the exact returned
object, controller and frame as well as actor/turn/revision; the command still
revalidates all current rules. Unsupported fits remain publishable with a clear
build refusal. This API does not expose foreign costs or support details.

The new concrete background save work captures only a detached export under the
live gate. Complete replay, codec copies and file I/O then run outside that gate;
render-current checks never wait on worker validation. Background load prepares
the entire new host/controller/known presentation before UI publication. Existing
token-owned busy state, captured slots, dirty-draft retention, cancellation and
stale-finally guards apply. Final store writes retain overwrite race checks and
atomic replacement. Once an atomic write begins, later UI cancellation may still
leave a valid point-in-time save, without publishing stale UI completion.

Related contracts: [ship design](design/ship-design.md),
[architecture](architecture.md),
[journal reserve](construction-journal-reserve-v1.md),
[equipment source resolver](campaign-equipment-source-packet-v1.md).
