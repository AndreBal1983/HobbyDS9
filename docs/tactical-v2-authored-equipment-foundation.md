# Authored Tactical v2 equipment foundation

13 October 2026. **Implemented equipment-source validation and detached inspection; campaign v2 activation remains disabled.** Numerical examples are **provisional test-scenario values**, not approved universal balance. Formation, geometry, lifecycle, ammunition/shield persistence and aftermath remain separate work. Design chapters retain their agreed/proposed/open labels.

Delivery: [draft PR #19](https://github.com/AndreBal1983/HobbyDS9/pull/19), branch `codex/tactical-v2-authored-equipment-foundation`, isolated worktree `C:/Users/ambal/.codex/worktrees/tactical-v2-authored-equipment-foundation/HobbyDS9`. This assignment does not authorize merging PR #19.

## Accepted checkpoint and source head

The owner authorized finalizing the reviewed combined acceptance before this assignment. [PR #18](https://github.com/AndreBal1983/HobbyDS9/pull/18) was merged normally into main **`53d2e64973d9081a4d0090e771f5e777fc55cf40`**, with a tree identical to accepted candidate `22096fb56d81d203f194a6c0407d7de76721d93c`. [Resulting-main CI passed](https://github.com/AndreBal1983/HobbyDS9/actions/runs/38423206886). GitHub also recognizes PRs #16/#17 as merged through their preserved history; their source branches/worktrees remain intact.

The fresh equipment baseline at that main commit passed locked restore, zero-warning/error Release and **1,358/1,358 tests**. Before implementation, the design index, ship/tactical chapters, architecture, blueprint contract, standalone backend/playground, inspection API/tests, actual content/save/resolver validation and accepted designer report were reviewed. No existing checkout or local planning document was changed.

Implemented and locally tested source head: **`b676372ee062f86261373b7d8660449ffff7ec4a`**. The subsequent documentation commit changes no executable input. Exact final delivery head and Linux/Windows/macOS CI links are recorded in PR #19 and `artifacts/equipment-foundation/final/delivery-record.json`, avoiding a self-referential commit hash in this file.

## Source and version matrix

| Contract/source | Identity | Status |
| --- | --- | --- |
| Legacy campaign package | Content **10**, existing exact JSON bytes | Supported unchanged; cannot carry authored-equipment data |
| New opt-in source package | Content **11** | Requires the complete typed equipment catalogue |
| Authored equipment schema | Format **1**, `authored-tactical-v2-equipment-v1` | New content-only contract |
| Equipment projection | Format **1**, `authored-tactical-v2-equipment-projection-v1` | Detached read-only source facts |
| Existing inspection envelope | Format **1**, `campaign-tactical-v2-source-inspection` | Additive optional equipment/source-kind facts; legacy observations retained |
| Campaign saves | **14** | No new mutable resource fields, no migrations |
| Campaign rules/RNG | `science-engineering-v1`; `splitmix64-rejection-v1` / 1 | Unchanged |
| V1 catalogue/compiler | Catalogue 1; `ship-design-compiler-v1` | Unchanged; exact immutable publications retained |
| Campaign resolver | `combat-resolver.phased` / 1 | Unchanged; never routes authored equipment into v2 |
| Standalone v2 | Format 1; `combat-resolver.tactical` / 2; `tactical-combat-v2-provisional-1` | Unchanged regression reference, never campaign authority |

Content 11 is a deliberate consequence of adding required, rules-relevant source definitions and per-ship initial provisions to the serialized definition catalogue. It is not a reservation for the later campaign lifecycle. `FoundationVersions.ContentFormatVersion` stays 10 for existing callers/packages; the new identity declares supported authored content 11. Validation explicitly accepts each supported shape, rejects equipment in format 10, requires the catalogue in format 11, and rejects unknown format/schema identities. Save 14 suffices because equipment/stocks are immutable content inputs at this milestone, not persisted runtime resources. A future lifecycle must review its actual save/rules contracts independently; Save 15 is not reserved here.

## Schema and authority

Contracts are [typed DTOs](../src/HobbyDS9.Contracts/AuthoredTacticalEquipmentDtos.cs) and [immutable snapshots](../src/HobbyDS9.Contracts/AuthoredTacticalEquipmentSnapshots.cs). Every required numeric/enum field is nullable at ingress; lists and strings have no fabricated defaults. Omission is distinguishable from zero, Fore sector, an empty energy-ammunition ID, or an explicitly empty weapon/mount/stock list.

`DefinitionCatalogDto.AuthoredTacticalEquipment` contains schema/provenance/balance identities, registered ammunition definitions, versioned static fits and exact initial ship provisions. Fit/munition definition IDs join the content definition registry and cannot collide with existing typed definitions. Fits include:

- Explicit maximum hull, armor, evasion, role and five per-ship tactical capabilities. No strategic-speed conversion, name parsing, formation bonuses or default rating 50.
- Reactor output, distribution limit and four explicit essential reservations: life support, command, propulsion, targeting.
- Four fixed sector maxima, ship-wide per-round recharge and its required/allocated power.
- Mount groups with explicit compatible weapon classes, categorical coverage and package-slot capacity.
- Installed package/version and mount IDs, slot consumption, Beam/Cannon/Torpedo class, coverage, damage, accuracy/tracking, opportunities, cadence, initial next-fire round, required/allocated power and finite magazine capacity/munition identity.

Initial provisions bind an authored stable ship ID, owner, v1 fallback static design, exact fit ID/revision, all four initial shield stocks and one explicit stock entry per torpedo package. Energy packages explicitly use zero magazine and an empty munition ID; no energy stock entries are invented. Stocks can be zero. A complete unarmed fit explicitly declares empty weapons/mounts/ammunition. Binding checks reject wrong ownership, missing initial ships, blueprint substitution, mismatched fit revisions and incompatible hull limits. Destroyed authored ships may be absent from a later save; inspection does not recreate them.

The [pure Simulation validator](../src/HobbyDS9.Simulation/AuthoredTacticalEquipment.cs) supplies typed code/path failures, including append-only `InvalidAuthoredTacticalEquipment`, `InvalidTacticalEquipmentPower` and `InvalidTacticalEquipmentBinding` codes. It rejects missing/duplicate/noncanonical data, unknown classes/arcs, incompatible mounts/coverage, slot overuse, invalid cadence/opportunities, out-of-range stocks and infeasible power. Accumulation uses BigInteger to reject overflow without wrapping. Valid scalar capacities remain finite Int64 values; no arbitrary aggregate hull/shield cap or balance formula is added.

The normal scenario/save factory invokes these validators. Simulation deep-copies/canonically orders every source list into its owned definitions. `ICampaignTacticalV2Inspection.InspectCampaignTacticalV2(actor)` still takes only an actor; under one lock it selects actual owned ships and their exact content bindings. Its optional `AuthoredEquipment` is a read-only detached snapshot and `SourceKind` distinguishes authored v2 equipment, legacy static and frozen v1 sources. Invalid/unknown/AI actors receive no source data. Application session and v1 designer/publication interfaces do not change. A caller-created snapshot, battle fixture or serialized report has no import/submission path and cannot authorize campaign results.

`InitialAmmunition` and `InitialCurrent` are explicitly **authored initial provisions**, not current campaign stocks. Repeated inspection merely reads those content facts: it performs no provisioning transaction, refill, healing, RNG draw, entity allocation or ledger mutation. No persisted ammunition/shield fields, round allocation or post-battle recovery were added. Current hull remains the ordinary authoritative ship fact. Exact saved content identity is checked by the existing host/package/save boundary; portable code does not independently hash JSON bytes.

## Proposed reservation semantics and worked example

These semantics are implemented for the new source schema and remain a **proposal for future campaign adoption**, not a redesign of v1 profiles or the standalone resolver:

1. Effective ship power is the lesser of reactor output and hull distribution limit.
2. All four essential categories must be explicit. Life support, command and mobile propulsion require positive fully met reservations. Targeting may explicitly be zero on an unarmed fit; an installed weapon fit requires positive targeting power. Required and allocated essential power must match.
3. Reserve essential power first. Equipment power budget is effective power minus those reservations. Weapon plus recharge allocations must fit that remainder.
4. Each installed weapon/recharge allocation is zero (disabled) or its full declared requirement. Partial allocation is rejected rather than granting unscaled damage/recharge or inventing a curve. Disabled packages still occupy their physical mounts and retain their declared statistics; their `Powered` flag is false.
5. Positive recharge requires positive required power. Shield stocks/maxima remain fixed content facts; this does not invent a new active-shield decay, passive-stock removal or between-battle recovery rule. Future recharge uses the existing v2 resolver only after lifecycle adoption.

Provisional cruiser: reactor **100**, distribution **80**; essential reservations **4 life support + 4 command + 12 propulsion + 6 targeting = 26**. Equipment budget is **54**, not 80 or 100. Beam **10**, cannon **12**, launcher **8** and recharge **6** allocate **36**, leaving **18** unused. Reducing distribution to **35** rejects: equipment 36 alone appears plausible against the reactor but full ship allocation **62** exceeds usable power. It never reaches a battle resolver with a fictitious full-reactor combat budget.

The general mount has three slots, allows Beam/Cannon and covers all sectors. Its beam consumes one slot and cannon two; a fourth consumed slot rejects even when the extra package is unpowered. The dedicated launcher accepts Torpedo, one slot, Fore/Aft coverage. The authored cruiser has hull **220**, armor **10**, sector maxima **40/25/25/20**, initial stocks **18/25/12/0**, recharge **4** and a **12-round** magazine provisioned with **7** rounds. No scalar v1 shield pool or aggregate attack is split to produce these values.

The unarmed surveyor has hull **100**, armor **0**, effective power **40**, essentials **18** (targeting explicitly zero), equipment budget **22**, recharge allocation **2** and rate **1**. It has no weapon/mount/ammunition rows. Its four ten-point sectors explicitly start **5/10/0/8**. These deliberately non-full stocks prove that inspection and reload do not silently fill capacity.

## Opt-in source package and CLI walkthrough

[The new scenario](../scenarios/tactical-v2-equipment-source/scenario.json), ID `tactical-v2-equipment-source-sandbox-v1`, owns its fits, ammunition definitions, binding identities and initial stocks. It reuses the accepted strategic skeleton/catalogue as a separately versioned package, adding two human-owned source ships and one foreign source binding. The existing human Scout remains intentionally legacy/ineligible. The newly authored equipment ships have explicitly **inert v1 combat fallback** designs; no weapon conversion or executable v2 encounter is attached. This is a source-validation sandbox, **not a playable v2 campaign**. Existing v1 static construction does not provision v2 equipment on subsequently produced ships, even for the same static design ID; construction/payment semantics are future work.

From the isolated repository root:

```powershell
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- scenarios/tactical-v2-equipment-source/scenario.json --inspect-tactical-v2 civ.federation
```

Observed source output includes `can-activate=False`, two `can-project-v2=True` authored ships and the legacy Scout's `False`. It prints exact fit/provenance, budgets, per-ship capabilities, package/mount/munition statistics and labelled initial stocks. Foreign source equipment is not printed. The mixed inventory retains legacy equipment blockers. With only complete owned sources, the equipment blockers disappear but **six lifecycle/derivation blockers remain**: formation capabilities, persistent resources/recovery policy, session rounds/order authority, strategic retreat/objective policy, atomic batch consequences and versioned activation/save contract.

For an invalid walkthrough, copy the new JSON to a temporary file and set the cruiser's `DistributionLimit` to **35**; run the same CLI with that file. Creation rejects with `InvalidTacticalEquipmentPower` at `$.Definitions.AuthoredTacticalEquipment.Fits[0].EssentialReservations`. The committed [headless acceptance script](../scripts/check-authored-tactical-equipment.py) performs both valid and invalid walkthroughs using the real scenario/session/Application path, repeated observations, entire strategic-diagnostic comparison and foreign-source checks. It never attaches a standalone fixture as campaign authority.

## Local and CI acceptance

| Gate | Result |
| --- | --- |
| Accepted-main locked restore / Release | Passed, zero warnings/errors |
| Accepted-main complete suite | 1,358 passed, zero failed/skipped |
| Final locked restore / full Release | Passed, zero warnings/errors |
| Final complete suite | **1,507 passed, zero failed/skipped** |
| New equipment cases | **149 passed**, including 68 missing-field cases |
| Actual source CLI valid/invalid/repeat/isolation smoke | Passed |
| PrepareUnity portable preparation / static boundary/assets | Passed |
| Legacy canonical/reverse/repeated 25-turn campaigns | Exact pinned hash and RNG in all three |
| Legacy optional-inspection transcript comparison | Exact state/RNG/transcript preserved; gate closed |
| Standalone v2 reference / scripted playground / R1/R2 resumes | Passed unchanged |
| Unity Editor, EditMode, native build/player, desktop | **Not run**: this assignment is strictly headless |

All 1,358 starting cases are retained, including the inspection, designer and combined acceptance cases. New tests cover absent-versus-zero fields and explicit unarmed fits; class/mount/coverage/slot validation; cadence/fire-round/stock/cap bounds; mandatory reservations, disabling and Int64 overflow/extremes; source/version/owner binding; malformed null rows; foreign disclosure; detached input/output immutability; canonical source reordering and en-US/nl-NL/tr-TR cultures; content/save round trips; accepted command ledger and stale rejection immutability; disappearance of destroyed ships; no source inheritance/provisioning through legacy construction; and exact v1 frozen-publication preservation.

Local logs: `artifacts/equipment-foundation/baseline/` and `artifacts/equipment-foundation/final/`; complete TRX: `tests/HobbyDS9.Simulation.Tests/TestResults/equipment-foundation-final.trx`. CI adds the real authored-source smoke to the existing Linux/Windows/macOS workflow. Exact final SHA/run evidence is recorded in [PR #19](https://github.com/AndreBal1983/HobbyDS9/pull/19); portable/static CI is not Unity runtime acceptance.

Reproduce:

```powershell
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
python scripts/check-unity-integration.py
python scripts/check-authored-tactical-equipment.py
python scripts/check-campaign-tactical-v2-inspection.py
python scripts/check-tactical-playground.py
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25 --combat-order reverse
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25
```

Unchanged-path evidence:

- Legacy package SHA256 **`3b80eb970c5d832550fc947344e860777910d80208af0fc2f474a98ae04e0b59`**; legacy save **`fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`**; RNG **`d8919406d6e01cf1`**.
- Legacy turn 26/state 185, 131 AI commands, 17 battles/12 checkpoints, nine ships/fleets, next entity 97.
- Separate standalone v2 reference **`ccae7836c085936ed962b9ba001764ae4252f135fdf62fa1fbe2c0ea0bcef07c`**.
- New source package SHA256 **`d826ca1ff1108eb680980d29969c27e624ed7fb487e04c2efd1eb8bf529c940e`**; its initial Save-14 diagnostic SHA256 **`9e8a73748bb94d7075a368b865c853d5efa5e0424ad1eb7d32561d0fafe7efb3`** is distinct from the legacy golden and is not a campaign-v2 event hash.

No existing golden, scenario byte, v1 compiler, standalone resolver, design chapter or tracked Unity asset was modified. Unavoidable shared edits are the optional definition field, explicit content-format validation, owned source copy, initial/live binding checks, additive source snapshot and existing inspection implementation. The host only prints projected facts and hashes package bytes; it contains no combat/power rule implementation.

## Remaining design round — proposals, not implementation

The explicit per-ship capability inventory is speed, agility, sensors, command, cohesion and combat role; tactical speed is independent from authored strategic movement. For the sample cruiser/surveyor these are **70/60/65/50/55** and **45/50/75/65/60**. Formation composition and initial relative geometry are still undefined.

Two possible worked aggregation approaches illustrate the unresolved choice:

- **Maintain-formation limiting model:** these two ships could use tactical speed **45**, agility **50**, cohesion **55**, with explicitly defined sensor/command leadership selecting **75/65**. This assumes bottlenecks and leadership selection; those assumptions require approval and do not imply a fleet-size cap or penalty.
- **Weighted capability model:** equal authored weights would yield speed **57**, agility **55**, sensors **70**, command **57**, cohesion **57**, using integer floor. Physical/class weights, command hierarchy and whether averages are appropriate for any rating remain open. These arithmetic examples are not code or approved formulas.

Next decisions must address composition/aggregation, formation IDs and lawful initial geometry; opt-in activation and mixed v1/v2 routing; persisted current ammunition/sectors and exact provisioning ownership; no automatic ammunition refill or between-battle shield recovery in the initial slice; simultaneous controller-owned round orders and baseline AI; destruction/basic three-round resolution and nondecisive round-three strategic outcomes; atomic multi-encounter aftermath. Strategic retreat and objective exits remain excluded from the first live slice until lawful consequences exist. No teleport, objective rewards, v2 blueprint compiler, refit, resupply economy, tactical AI expansion or Unity work was implemented or started by this milestone.
