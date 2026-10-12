# Tactical combat v2 standalone backend

Status: **standalone backend implemented; provisional balance; campaign and Unity integration NOT ATTEMPTED**. This implements Phase A of [Issue #8](https://github.com/AndreBal1983/HobbyDS9/issues/8), using the [tactical design](design/tactical-combat.md), [ship design](design/ship-design.md), [architecture](architecture.md) and [blueprint construction](blueprint-construction-v1.md). It does not amend their agreed/proposed/open labels.

Starting `origin/main`: `5beba2d652d283a2efb2f3ef44ddd5df9e38815d`, fetched 12 October 2026. This already includes PR #5 and the subsequently merged PR #6. Work is isolated on `codex/tactical-combat-v2-backend`; neither main nor the UX branch/worktrees are edited. Delivery is a **draft PR**, without merge authorization.

## Compatibility and explicit boundary

`TacticalEncounterV2Dto` has its own format **1**, resolver **`combat-resolver.tactical` / 2**, rules **`tactical-combat-v2-provisional-1`**, and encounter-local **`splitmix64-rejection-v1`** stream. Every identity is checked on input. The complete provisional rules object is frozen in each encounter checkpoint; numerical choices below are tuning candidates, not approved universal game balance.

Campaign Save **14**, Content **10**, rules **`science-engineering-v1`**, compiler **`ship-design-compiler-v1`**, RNG version **1**, and default **`combat-resolver.phased` / 1** are unchanged. No existing DTO, enum, content byte, serializer or golden is changed. V2 is not an `ICombatResolver`: its round checkpoint cannot accidentally be submitted through the campaign's v1 result path. No inferred conversion of a published blueprint's aggregate attack/shield values supplies missing armor, weapon classes, magazines or sectors.

## API and owned state

- Contracts: [`TacticalCombatV2Contracts.cs`](../src/HobbyDS9.Contracts/TacticalCombatV2Contracts.cs).
- Resolver: [`DeterministicTacticalCombatV2Resolver`](../src/HobbyDS9.Simulation/DeterministicTacticalCombatV2Resolver.cs).
- Validation/canonical copies: [`TacticalCombatV2State`](../src/HobbyDS9.Simulation/TacticalCombatV2State.cs).
- Complete executable fixture construction: [`TacticalCombatV2Fixture`](../tests/HobbyDS9.Simulation.Tests/TacticalCombatV2Fixture.cs), with no dependency on campaign content or the ship designer.

The host supplies an explicit frozen v2 fixture: encounter identity and seed; at least two hostile sides; formations with standing orders, capability ratings and objective; ships with current/max hull, armor, power allocation, four current/max shields, mount groups, equipped weapons and magazines; and exactly one canonical geometry pair for every opposing formation pair. Same `SideId` means allied; distinct sides are hostile. Geometry pairs retain both formations' facing and shared range, including after a formation exits.

```csharp
// encounter is a host-owned explicit TacticalEncounterV2Dto fixture/checkpoint.
TacticalCombatV2State.Validate(encounter);
var resolver = new DeterministicTacticalCombatV2Resolver();
var round1 = resolver.ResolveRound(encounter); // Only round 1 is computed.
var savedBoundary = round1.NextState;          // Serialize in the host adapter.

// Commit both sides' lawful replacements in ONE call; omitted orders persist.
var orders = new TacticalOrderBatchV2Dto
{
    EncounterId = savedBoundary.EncounterId,
    Round = savedBoundary.NextRound,
    Replacements = new List<TacticalOrderV2Dto>
    {
        new TacticalOrderV2Dto
        {
            FormationId = savedBoundary.Formations[0].FormationId,
            Maneuver = TacticalManeuver.FightingWithdrawal,
            TargetPriority = TacticalTargetPriority.LowestHullRatio
        }
    }
};
var round2 = resolver.ResolveRound(savedBoundary, orders);
// Alternatively ResolveAutomatic(initial) iterates the SAME function with standing orders.
// Inspect Completion before requesting a subsequent round.
```

There is no partially committed order state or opponent-order reveal API. A trusted host gathers decisions independently from its lawful start-round observations, then supplies the joint batch. V2 does **not** implement fog-of-war projection, actor authorization, a hidden-information UI or an AI decision service. It validates ownership relationships within the fixture (enemy targets, allied protection), not provenance of observations from a campaign that is not connected.

`ResolveRound` validates before using a private deep copy. Stale/duplicate/unknown/inactive orders, bad IDs/enums/geometry, contradictory completion/hull/objective state, negative/overflowing profiles, invalid mounts/coverage, over-allocation of power and invalid cadence/ammunition reject with `ArgumentException`. Inputs must not be concurrently mutated. `TacticalRoundV2Result.NextState` exports a **new detached copy on every read**; events are a read-only list of immutable `TacticalEventV2` objects. The host owns serialized round state; this pure API is not a tamper-proof session/checkpoint submission service. Displaying or deserializing the event stream consumes no RNG. State and events have JSON round-trip tests using the host's `System.Text.Json`; the portable engine has no serialization dependency.

## Provisional mechanics

### Cadence, geometry and targeting

At most three rounds. `NextRound` starts at 1 and becomes 4 after round 3; non-`None` completion prevents another round. Dead ships and exited formations cannot fire in later rounds. A round commits orders, maneuvers, prepares target access, launches torpedoes, allocates interception, fires remaining energy opportunities, rolls hits, applies the simultaneous volley, evaluates objectives, and then recharges only if another round remains.

Formation ratings are bounded **0..100**. Default maneuver score is `4*Speed + 3*Agility + Sensors + Command + Cohesion`. Weights are fixture tuning (0..100 each); fleet size adds no damage penalty and imposes no cap. Geometry changes by at most one range category per round. Charge, Breakthrough and Pursuit request closure; FightingWithdrawal and Disengage request opening; Hold, Screen and Flank request movement toward `PreferredRange`. Matching requests move together. Otherwise the higher score wins beyond the provisional **10-point tie margin**. Within the margin opposing closure/opening holds range; a tie between movement and holding allows the movement request. A superior holder can deny closure. There are no coordinates, command hierarchy, officer bonuses or strategic interception calculations.

Facing is refreshed each round: default Fore; withdrawal exposes Aft. Winning Flank exposes the opponent's Port if canonical formation A wins, or Starboard if B wins; the winner preserves its own forward access. A lost flank does not grant lateral exposure. The constant side assignment is an abstract convention, not a camera orientation. Installed weapon coverage checks the attacker's own facing; shield impact uses the target's facing. Orders never redistribute any shield capacity or current points.

Target priorities are LowestId, LowestHullRatio (BigInteger cross-products), CommandSupport and Screen/Escort, with ordinal ship-ID ties. An optional enemy formation concentration is a first preference among **eligible** targets, not permission to bypass access. A Screen can block acquisition of its designated allied formation when screen/target ranges are at least Engagement and within one category. A successful flank can bypass this access block. There is no interception merely from strategic co-location.

### Mounted weapons, magazines and reactive defense

Only the three universal classes exist. Mounts declare compatible class, slots and coverage; equipped weapon coverage must be a subset. Total weapon and shield allocations cannot exceed `PowerBudget`; energy weapons fire only at their explicit full required power. Unpowered/unequipped modules never fire. Partial power allocation is legal but disables that module; this fixture model has no power-scaling compiler.

Beam reaches Engagement/Close/Intermixed; Cannon reaches Close/Intermixed; Torpedo reaches all four ranges. Damage, accuracy and tracking are explicit fixture statistics. There are no faction-name bonuses or shield penetration. Opportunities are 1..64 per package, a provisional work bound **not** a fleet limit. Cadence is 1..3 rounds for every package. Any offensive or defensive use sets `NextFireRound = currentRound + CadenceRounds`; unused/ineligible packages do not consume cadence.

One valid torpedo launch consumes one round from its finite magazine before interception or hit rolls. Yield stays constant at Distant; only hit chance loses the provisional **1,500 basis points**, representing warning/evasion opportunity. All offensive hit chances clamp `AccuracyBps - target.EvasionBps - distantPenalty` to 0..10,000. Each nonintercepted discharge consumes one encounter-local draw, including 0%/100% endpoints. No refill, repair or campaign resupply occurs.

Powered beams reserve a fraction of their opportunity budget for incoming threats: Screen **100%**, Withdrawal/Disengage **100%**, Hold/Flank **50%**, Charge/Breakthrough/Pursuit **0%**. Fractional opportunities floor (a one-opportunity Hold beam therefore reserves zero). Unused reservations return to offense. Screen defenses are attempted first, followed by canonical ship/weapon order. Each eligible defensive package attempts once per incoming torpedo, spends **one actual firing opportunity even on failure**, and may attempt later torpedoes if capacity remains. A successful intercept stops further attempts against that torpedo. Saturated batteries have no free residual defense. Ally coverage requires Screen, explicit designated protection or the same formation, nearby access as above, and an arc toward the attacker. Self-defense may intercept at Distant, where torpedoes have longer warning even though beams cannot reach the distant launching ship offensively.

Default interception chance clamps `3000 + TrackingBps + 20*Sensors + distantBonus` to basis-point endpoints; Distant bonus is **1,000**. Every attempt consumes one local draw. Launches, interception results, target/facing, hit/miss and actual losses are separately traced. The firing-opportunity event describes available powered package capacity before arc/target acquisition; a package with no legal target may emit no discharge.

### Shields, recharge and armor

Four sectors **Fore, Port, Starboard, Aft** retain separate fixed maxima/current values. Incoming successful damage aggregates per target/sector. Only that sector absorbs damage; overflow from all sectors is aggregated once per target's simultaneous volley before armor. There is no active-shield bleed-through, bypass, component damage, cross-sector transfer or hull regeneration. Ships alive before the volley can discharge even if simultaneously destroyed.

Recharge uses one ship-wide `ShieldRechargePerRound` budget, requires the explicit shield power allocation, and caps at unchanged sector maxima. Default priority is Fore → Port → Starboard → Aft, skipping any sector successfully hit this round (`SuppressRechargeOnHitSector`, provisional and fixture-tunable). It restores depleted points without taking them from intact facings. Recharge follows damage and objective resolution and happens only when completion is `None`; thus at most two transitions, never retroactive and never after round 3 or early completion. Existing stored shields remain protective; power gating applies to recharge in this fixture model. Post-encounter shield recovery is absent.

Armor is one nonablative ship-wide rating. Default exact arithmetic is `floor(D * 100 / (100 + 6*A))`, with **k=0.06 provisional**. The numerator/denominator are frozen tunable integers. D is all shield overflow to this target in the simultaneous volley, not individual-hit damage. D=100 yields hull-bound damage **100, 62, 45** for armor **0, 10, 20**. A ten-shot volley of 1 damage against A=20 equals one shot of 10: **4** hull damage. No minimum damage floor or reduction cap is invented: sufficiently high armor can floor tiny volleys to zero. BigInteger handles accumulation/multiplication/division; final actual shield/hull losses are clamped to valid Int64 pools, discarding overkill. Armor events retain arbitrary-size before/after values in invariant decimal detail strings.

### Local objectives and honest limits

Defeat completes through decisive remaining-side count (no timed objective counter). Hold and Delay count rounds with a surviving formation, without claiming territory ownership or strategic delay consequences. Breakthrough requires the Breakthrough intent, Intermixed access and a won score contest against every surviving hostile formation; Disengage requires the Disengage intent, Distant access and a won contest against every hostile formation. Their explicit objective goal is 1..3 successful rounds; progress survives boundaries. When reached they emit objective completion and exit. A Disengage maneuver under another objective may retreat immediately on successful extraction. A matching/stronger Pursuit can block opening/extraction. Objectives evaluate the same post-volley/pre-exit survivor set so exit order cannot alter another formation's opportunity.

An achieved local objective ends the encounter even with hostile survivors. Otherwise fewer than two active sides is decisive; the third-round boundary ends remaining encounters. Raid, Escort crossing goals, named strategic interception, territory control, retreat destinations and renewed campaign contact **are not implemented**. Screening protection is implemented, but is not a campaign Escort mission. No default campaign conversion, migration, tactical AI or Unity presentation is claimed.

## Validation evidence

Local headless acceptance completed 13 October 2026 using .NET SDK **10.0.401**, runtime **10.0.12**, Windows **10.0.26200**, AMD Ryzen 9 **9950X** (16 cores/32 logical processors). Starting main: locked restore, zero-warning/error Release, **1,104/1,104** tests; no baseline drift. Final implementation: **1,273/1,273** tests, including **169 new v2 cases**, zero failures/skips. Cases include every opposing maneuver pair at every range (64 parameterized cases), 100-seed mixed-weapon repeated/reversed properties, integer extremes, defensive saturation/opportunity cost, simultaneous lethality, legal later-round changes, deep ownership, atomic rejection, saved boundaries and immutable event JSON replay.

```text
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore --filter FullyQualifiedName~TacticalCombatV2 --logger "console;verbosity=detailed"
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25 --combat-order reverse
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
python scripts/check-unity-integration.py
```

All listed local gates passed. Prepare only builds/copies portable libraries/resources in the isolated worktree; static checks inspect files/assembly boundaries. **Unity Editor, batch mode, EditMode, PlayMode, native builds, desktop/player/manual acceptance and screenshots: NOT RUN**, by this assignment's headless-only constraint. CI executes the existing Linux/Windows/macOS workflow on the pushed final branch commit; exact-head run links/status are recorded in the draft PR and final handoff rather than inferred from historical runs.

Legacy canonical/reverse/repeated 25-turn result remains SHA256 **`fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`**, RNG **`d8919406d6e01cf1`**, turn 26/revision 185, 131 AI commands, 17 battles/12 checkpoints, 43 phases, 98 shots/68 hits and shield/hull damage 126/116. Scenario content SHA256 remains **`3b80eb970c5d832550fc947344e860777910d80208af0fc2f474a98ae04e0b59`**.

Independent v2 reference: default two-formation/one-ship fixture, seed 47, 10-damage 100%-accuracy beams, 50 shields per facing, no recharge. Three rounds, six hits, Fore=20 and other sectors=50, hull=1000 each, final local stream **`b54cda58fbbee8ad`**. SHA256 of the host JSON serialized automatic round results (each detached next state plus ordered events) is **`ccae7836c085936ed962b9ba001764ae4252f135fdf62fa1fbe2c0ea0bcef07c`**. The test pins this separate v2 vector and independently calculated RNG/hull/shield facts; no legacy vector was updated.

### Measured performance and scaling risk

The xUnit Release stress runner warms the API on a small fixture, then times three rounds including validation, owned copies and result construction. Managed allocation is bytes allocated on the resolver thread, **not peak RAM/RSS**. One measured run (not a throughput guarantee):

| Scenario | Formations / ships | Events | Elapsed | Managed bytes allocated |
| --- | --- | --- | --- | --- |
| Beams, one opportunity/ship | 2 / 1,000 | 9,019 | 39.464 ms | 35,599,760 |
| Beams, one opportunity/ship | 20 / 1,000 | 9,370 | 62.352 ms | 38,999,224 |
| Beams + two torpedo opportunities/ship, Screen | 10 / 1,000 | 28,885 | 298.275 ms | 87,450,968 |

Fixtures use 1,000 hull and four 50-point shields; focus fire naturally destroys some ships. Tests assert valid surviving state, unchanged input, three rounds and allocation below a practical 512 MiB test ceiling; no machine-sensitive timing assertion is used. Per-formation target ranking avoids per-discharge ship sorting and preserves exact events. Geometry storage/validation is O(formations²); acquisition considers formation candidates, and reactive defense may scan batteries per incoming torpedo. Huge numbers of small formations or salvos still need profiling/batching. There is no hard fleet-size restriction or blanket damage penalty; event volume, copies and working memory are real scaling costs. The benchmark covers fixtures, not unimplemented campaign/Unity performance.

## Follow-up gates before campaign cutover

1. Version the compiler/content contract to produce complete mount coverage, powered class weapons/cadence, armor, fixed sector maxima and recharge. Retain immutable old blueprint revisions and their original compiler/validation behavior; reject incomplete v2 conversions rather than inventing equipment.
2. Decide persistent shield/ammunition/recovery/replenishment semantics and deliberate Save/Content/Rules version identities, including explicit backward rejection or separately authorized migration. The current v1 ship state contains hull, not these new persistent pools.
3. Add a versioned encounter allocator and session-owned round checkpoints: lawful observations, simultaneous order gathering/actor validation, transactional round/result submission, batch canonicalization, save boundaries, retreat destinations and objective consequences. Validate results against original frozen fits; detached fixture DTOs alone are not an authorization boundary.
4. Integrate declared strategic detection/interception and objective inputs. Design Escort/Raid/Hold territory consequences and renewed-contact policy; do not simulate campaign success with fixture counters.
5. Add lawful tactical AI through the same validators, deterministic campaign regressions and intentional new versioned baselines, preserving v1 compatibility where supported.
6. Build Unity snapshots/choreography from immutable backend events in a separately scoped branch. Run Editor/AOT/native/manual acceptance there; this backend draft does not establish those results. Review and tune all provisional constants, particularly armor floor, warning/PD policy, shield suppression and maneuver ties, before making production balance claims.
