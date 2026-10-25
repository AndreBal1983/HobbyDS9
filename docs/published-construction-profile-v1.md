# Explicit portable published-construction profile

Status: **proposed portable implementation for review**, not a shipping content or
Unity activation. The separate typed `CampaignPublishedConstructionApplication`
accepts an explicitly supplied base template and support catalogue. No menu,
host, slot store, JSON codec or default factory registers this profile. Existing
V1/V2 roots and their `UnsupportedSource` decisions remain unchanged.

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
These hashes establish consistency, **not authority**: a future caller must supply
the approved external template/support package. Saved metadata cannot approve
itself. No support ratings in the tests are adopted as live playtest balance.

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
its time. A strict outer codec/store and explicit host integration would require
their own reviewed increment before player activation.
The actor-scoped projection of current tactical support and capacity eligibility
is also deferred and must precede host/UI activation. Existing construction/design
snapshots do not yet advertise the new queue eligibility; command validation is
authoritative, but that alone is not a complete player-facing admission display.

Related contracts: [ship design](design/ship-design.md),
[architecture](architecture.md),
[journal reserve](construction-journal-reserve-v1.md),
[equipment source resolver](campaign-equipment-source-packet-v1.md).
