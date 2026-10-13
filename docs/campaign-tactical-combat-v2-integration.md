# Campaign Tactical Combat v2 integration — source inspection checkpoint

Status: **implemented authoritative source inspection; live campaign v2 activation blocked**.
This is the last safe checkpoint permitted by the supplied headless integration
specification. It does **not** implement a complete v2 fit compiler, campaign round
checkpoint, tactical AI, persistent ammunition/shields, aftermath transaction or a
campaign-owned v2 scenario. Those mandatory live-integration gates remain incomplete.
No Unity, desktop, native player or Editor was executed for this milestone.

Base inspected and tested: `703e395b793af41953d440d04d69e0744909caf5`.
Worktree/branch: `codex/campaign-tactical-v2-integration`; delivery is a **draft PR,
not a merge**. The [accepted combined milestone](combat-lcars-integration-acceptance.md)
remains the reference for the existing standalone resolver and designer. Read
[architecture](architecture.md), [backend](tactical-combat-v2-backend.md),
[playground](tactical-combat-v2-playground.md), [blueprints](blueprint-construction-v1.md),
[designer publication proposal](unity-ship-designer-campaign-blueprint-integration.md)
and the [design index](design/README.md) together. Design agreement does not supply
missing authored statistics or resolve proposed/open policies.

## Implemented boundary

`SimulationSession.InspectCampaignTacticalV2(actor)` captures campaign identity,
phase/stamps, the requested human actor's actual ships, exact frozen blueprint
revisions, and current catalogue/deployment access under one session lock. It
returns an immutable `CampaignTacticalV2EligibilitySnapshot`. Invalid, unknown and
AI actors receive typed errors and no ships, blueprints or catalogue. Different
human actors can inspect their own sources for local hot-seat use; this is not an
authentication service. Opponent equipment and AI-only fleet inventories are not
exposed by this capability.

Each ship includes its authoritative v1 resolved design and optional exact frozen
publication. Publications include hull/component versions, installations,
mount usage, all operating profiles, compiled power allocations and engineering
version provenance. Current deployment access is separate: losing access to a
component or changing the current catalogue cannot reinterpret an existing ship.
Each owned revision is restored once per inspection and reused immutably across
ships; no current-draft or latest-revision substitution occurs. Unbuilt owned
revisions are also inventoried. Static designs have **null** frozen fit data.

The additive Application capability is `ICampaignTacticalV2Inspection`; existing
`ICampaignSession` consumers need no changes. The only shared Application edit is
adding `partial` to `CampaignApplication`; its new implementation lives in a
separate file. `ShipDesignApplication`, `ICampaignSession` and every tracked Unity
file are untouched. Codex 2's designer publication work needs no integration edit
for this checkpoint. Future live activation requires joint review of the new
compiler/publication contracts, rather than changes to its UX in this branch.

This is a **versioned source projection**, not a complete v2 fit/stat projection.
Inspection format 1 / `campaign-tactical-v2-source-inspection` is a diagnostic
identity only. There is no executable tactical DTO, fixture attachment, caller
equipment override, activation setter, result import, RNG consumption or command
submission on this API. `CanActivate` is false for every currently supported
campaign, including one with no ships; `CanProjectTacticalV2` is false for both
static and frozen-v1 sources. Typed blocker codes enumerate fit and lifecycle
gaps. A caller-created or serialized report has no authority and cannot be loaded
as campaign state or submitted as a combat result.

## Authoritative source and version matrix

| Fact | Static campaign design | Exact frozen blueprint v1 | Safe v2 use now |
| --- | --- | --- | --- |
| Stable ship/owner/fleet identity, current hull | Live `ShipStateDto` | Same live state, exact blueprint ID/revision | Inspect unchanged |
| Maximum hull, strategic movement | Explicit definition | Frozen compiled design | Inspect unchanged; no tactical speed conversion |
| Scalar shields, attacks at three ranges, accuracy/evasion | Explicit v1 combat profile | Frozen v1 compiler output | Legacy resolver only |
| Hull and component versions, quantities | Not represented | Frozen hull, component catalogue and installations | Inventory, not inferred equipment |
| Mount category/size/count/coverage tag | Not represented | Frozen v1 mount limits, requirements and usage | No automatic tag-to-arc or bank-to-class mapping |
| Cruise/Combat/Survey power, mandatory support dependencies | Not represented | Frozen profile allocations and compiled preview | Inventory; not a v2 firing-power contract |
| Engineering access | Static construction prerequisites | Authored version gates + current mature applications; frozen built versions retained | Construction access stays authoritative |
| Beam versus Cannon/Pulse class | Absent | EnergyWeapon does not distinguish them | **Blocked** |
| Per-installation weapon damage/tracking/opportunities/cadence | Absent | Aggregate outputs only | **Blocked** |
| Magazine capacity, physical ammo stock and ammo variant | Absent | Torpedo aggregate attacks only | **Blocked** |
| Four shield maxima, current sector stock and round recharge | Absent | Scalar capacity only | **Blocked** |
| Armor | Absent | Absent | **Blocked** |
| Tactical formation speed/agility/sensors/command/cohesion/roles | Absent | Some preview ratings, no approved formation derivation | **Blocked** |
| Round checkpoint/controller-owned orders/aftermath/objectives | Phased-v1 lifecycle | Same lifecycle | **Blocked for v2** |

| Identity | Current supported value | Change in this checkpoint |
| --- | --- | --- |
| Campaign save | 14 | None |
| Scenario content | 10 | None |
| Ship-design catalogue | 1 | None |
| Campaign rules | `science-engineering-v1` | None |
| Frozen compiler | `ship-design-compiler-v1` | None |
| Campaign resolver | `combat-resolver.phased` / 1 | None |
| RNG | `splitmix64-rejection-v1` | None |
| Standalone tactical format/resolver | Format 1, `combat-resolver.tactical` / 2 | None |
| Standalone provisional rules | `tactical-combat-v2-provisional-1` | None |
| Inspection projection | Format 1, `campaign-tactical-v2-source-inspection` | New, not persisted |

No existing JSON schema, blueprint, compiler, scenario bytes or golden baseline was
rewritten. Save 14 remains supported unchanged; there is no silent migration or
automatic v2 conversion.

## Conversion and migration proposal — not implemented

The following is a **proposal**, not accepted numeric balance or runtime support.

1. Introduce an explicitly authored equipment catalogue/compiler contract, separate
   from immutable v1 publications. Every required numeric/enum field must distinguish
   omission from a valid zero. Pin catalogue, compiler, rules, content and fit identity
   in each publication. Reserve a new catalogue/compiler identity only when this
   contract is implemented and accepted; do not relabel v1 records as v2.
2. Author weapon class, categorical mount compatibility and exact installed mount
   binding, coverage, damage, accuracy/tracking, firing opportunities, cadence,
   power requirements and magazine/ammo definitions. Do not split range totals,
   guess from family/display names, treat a component quantity as a shot count or
   derive Torpedo damage from v1 attacks. An explicitly unarmed v2 fit may have an
   explicitly empty weapon list; absence is not an empty list.
3. Author armor and fixed Fore/Port/Starboard/Aft shield maxima plus finite ship-wide
   recharge. Validate every powered output against the full Combat profile, including
   power reserved for life support, command, propulsion and targeting. The existing
   standalone power model only sums shields/weapons; binding a complete ship profile
   needs an explicit reservation/scaling contract before campaign adoption. Fractional
   v1 output must not become a fully powered unscaled v2 weapon.
4. Define formation capability aggregation from actual installed capabilities and
   formation composition. Strategic movement is not tactical speed; a preview sensor
   total is not automatically a 0–100 sensor rating. Do not default five ratings to
   50 or manufacture command/role bonuses. Initial geometry, fleet-to-formation
   composition and supported objective assignment need authoritative rules too.
5. Publish new immutable revisions with complete compiled v2 fits. Exact old
   revisions remain v1 and either stay on the phased path or produce typed ineligible
   results. Mixed v1/v2 encounters require a deliberate routing policy, not implicit
   conversion. Existing ships cannot receive a new fit without a separately accepted
   refit/migration milestone; refits are excluded here.
6. Persist live per-ship ammunition and four current shield stocks, with caps bound to
   the exact installed revision. Construction must initialize only explicitly
   provisioned, paid physical ammunition; authored initial fleets need explicit
   initial stocks. Do not fill magazines at each encounter or load. Define a separate
   finite between-battle shield recovery rule and lawful physical resupply path;
   neither recharge nor ammunition provisioning rates are selected here. Hull repair
   remains the existing explicit campaign rule, not a tactical heal.
7. Persist opt-in routing plus frozen encounter fits/rules/seed, latest lawful orders,
   round state, ammo, sector stocks, objectives and canonical event trace. This requires
   new save/content/campaign rules contracts. A candidate next save/content pair is
   15/11 **subject to coordinated acceptance**; no numbers or identities are reserved
   or supported by this code. Existing loaders must explicitly reject unsupported
   new versions. Start with a new authored v2 scenario; an old-save migration tool is
   a separate, explicit, tested deliverable if subsequently requested.

## Proposed session round and aftermath contract — not implemented

Only session-owned pending encounter identity and frozen source data may create a
v2 battle. Clients would submit encounter ID, actor ID, observed turn/revision,
round and formation order replacements; never ship lists, enemy fits, RNG seeds,
geometry or results. Validate persisted controller/ownership, phase, stamps,
duplicates, active formations, legal enemy targets, allied protection and replay
before touching state or RNG. Simultaneously commit all sides' orders, preserve
standing orders and resolve **one** round. Host preview/resolver output cannot
become a caller-forged campaign result.

AI order generation needs a clearly named deterministic **baseline policy**, not
a claim of the intended tactical planner. It must receive lawful observations and
use the same order validator. Which legal standing/default maneuvers it chooses is
an explicit future policy decision; v1 FocusFire/Overcharge plans cannot simply be
renamed to v2 maneuvers. Save/reload after rounds one and two must reproduce the
next events, resource stocks, completion and encounter RNG exactly. Rejections must
leave the entire session, RNG and command ledger byte-identical.

Do not apply shield/ammo/hull aftermath piecemeal to strategic state. Stage each
completed encounter's session-owned result, then validate/apply the whole batch in
canonical encounter order once. Bind hull losses, surviving shield/ammo stocks and
destroyed ships to stable ship IDs and exact blueprint revisions. Clearing the
batch and opening Planning must be one validated transaction; no duplicate rewards,
entity creation, erased orders or second clock advance. Tests must include invalid
later encounters and failed finalization with full rollback.

**Retreat/objective blocker:** v2 Disengage and Breakthrough have no strategic
destination in their standalone state. Current v1 validation requires a known
adjacent nonhostile navigation destination outside other battle locations. A new
policy must choose/validate destinations from authoritative knowledge/topology,
respect weighted-edge progress and conflicts across the batch, and explicitly
define movement continuation/time cost. If no legal destination exists, reject
the objective/route before resolution rather than teleport or silently invent a
fallback. Hold/Delay/Breakthrough need declared strategic objectives and completion
consequences; there is no approved reward/objective store to credit. Formation-level
partial withdrawal and fleets split between outcomes also need a deliberate policy.
These questions prevent safe activation even if fit data were available.

## Real campaign walkthrough available now

```text
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25 --inspect-tactical-v2 civ.federation
```

This advances the **actual campaign through phased-v1 combat**, then inventories
its surviving owned ships. It prints inspection format 1, `can-activate=False`,
`campaign-resolver=combat-resolver.phased/1` and the eleven typed blockers. It does
not play a v2 campaign battle. Without `--turns`, it inspects the initial campaign.

The production test publishes `blueprint.escort` revision 1 through the real command,
enqueues its construction, runs Production, publishes revision 2 and inspects the
built ship. The ship still points to revision 1; frozen hull/components and Combat
power allocations are returned from that revision. Reload with a changed current
catalogue/access leaves this old source unchanged. A three-encounter test inspects
pending and buffered v1 battles across save/reload and finalizes them normally.
There is **no campaign R1→save→R2→R3 v2 walkthrough**; substituting a standalone
playground fixture would falsely claim live integration.

## Acceptance evidence

Fresh base before source edits: locked restore, zero-warning/error Release build,
**1,307/1,307** .NET tests, unchanged legacy 25-turn save hash/RNG. Final local
Release build: zero warnings/errors; **1,331/1,331** tests, including **24** new
inspection cases. Existing movement, production, combat, blueprint persistence,
standalone tactical ordering/round resume, overflow and validation cases all pass.

New coverage includes actor errors and hot-seat ownership, no foreign equipment
disclosure, repeat/cross-seed inspection (0, 1, 47, UInt64 max), empty-campaign
gate closure, refusal to derive equipment from display names/scalars, real
production/exact old revision, catalogue/access changes, culture/permutations,
immutable exports, Application/save-load compatibility, foreign-blueprint exclusion,
legacy content without a catalogue, forged-v2 result rejection, three pending/buffered
encounters and 1,000 constructed-source ships. Canonical, reverse and repeated
25-turn CLI runs remain identical in state/RNG. The new CI script compares the
entire strategic diagnostic transcript with and without inspection and requires
the explicit activation blockers.

Unchanged goldens:

- Legacy save: `fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`.
- Legacy RNG: `d8919406d6e01cf1`.
- Standalone tactical v2: `ccae7836c085936ed962b9ba001764ae4252f135fdf62fa1fbe2c0ea0bcef07c`.
- Scenario SHA256: `3b80eb970c5d832550fc947344e860777910d80208af0fc2f474a98ae04e0b59`.

PrepareUnity **Prepare only**, static assembly/assets check and all four playable
console scripts plus both round-boundary resumes passed. Prepare generates portable
copies; no tracked Unity edits were made. Cross-platform exact-head CI evidence is
recorded in the draft PR after the pushed commit completes its three matrix jobs.

Measured locally on Windows/.NET 10.0.12 (diagnostic, not universal performance
guarantees): 1,000 ships referencing two frozen revisions inspected in 1.760 ms in
the isolated test, 17.661 ms during the concurrent full suite; 1,805,136 thread
allocation bytes. The existing standalone 1,000-ship stress tests still pass:
two formations/three rounds/9,019 events ~87.9 ms, twenty formations/9,370 events
~109.1 ms, ten mixed formations/28,885 events ~755.7 ms. These measure **standalone
resolution**, not a live campaign-v2 transaction. Many unique blueprint revisions
cost additional compiler restores; this inspection is an explicit diagnostic,
not a per-frame client path.

## Outstanding gates and next milestones

**Blocked, not claimed passed:** complete authored v2 fit/compiler and formation
derivation; versioned opt-in campaign routing; controller-owned joint round orders
and baseline AI; persistent resource/provisioning/recovery policy; legal strategic
retreat/objectives; atomic v2 batch aftermath; new save/content compatibility;
campaign-owned v2 scenario and end-to-end deterministic round checkpoint acceptance.
No v2 campaign event hash exists at this checkpoint. Existing standalone hashes and
tests are regression evidence only.

Next milestones should settle the source/resource/version policy first, implement
the explicit compiler and newly authored opt-in content, then implement and test
session rounds and batch aftermath before client playback. Preserve agreed fixed
shield allocation, three-round cap, shield-first damage, armor only after shields,
finite ammunition and no special bypass/subsystem damage. Detailed balance, recovery,
resupply, formation and retreat choices above remain **proposed/open**. Refits, AI
ship design, tactical physics, Unity designer UX and extra economy work remain excluded.
