# Actor-safe own weapon and fallback explanations

Status: **additive authoritative observation increment**, stacked on PR #26 at `e268b2c490f424b96d21593e9c1427d39c7f5be7`. [B1 core](campaign-tactical-v2-stage-b-core.md), [physical contracts/gates](campaign-tactical-v2-physical-travel.md) and [stress acceptance](campaign-tactical-v2-travel-stress.md) remain unchanged. No balance, equipment, hostility, cloak, opportunity, activation or client/storage rule is added.

## Projection guarantees

Every actual Application-owned `CampaignTacticalOwnWeapon` now carries immutable `Availability`, with `Status`, flag-valued `OwnBlockers` and nullable tactical `Round`. `CampaignTacticalOwnShip.Unarmed` means no installed weapon packages, not simply no currently usable weapon. Own weapon lists are ordinally ordered.

| Status | Guarantee |
| --- | --- |
| `OutsideEncounter` | Strategic inspection has no current battle-round context. Own hardware/ammunition blockers can still be stated; initial package cadence is not interpreted as live readiness. |
| `OwnStateBlocked` | At the observed boundary, at least one actual own-state constraint prevents battery admission or active-round participation. |
| `TargetDependent` | No listed own blocker at this boundary. **No shot, target availability, offensive opportunity allocation, chance, damage or outcome is promised.** |

`OwnBlockers` flags are `NotEquipped`, `PowerIncomplete`, `NoDamage`, `NoAmmunition` (torpedoes only), `CadenceWait`, `ShipDestroyed`, `FormationInactive`, and `EncounterComplete`. All are derived from the owned ship/weapon, owned formation status and public encounter boundary. There is no enemy-state input to this projection. Authored review weapons are installed by compilation; `NotEquipped` remains the shared resolver's existing standalone constraint, not a new campaign unequip command. A zero-ammunition beam is not an ammunition blocker.

The resolver's existing weapon predicate is factored into portable `TacticalWeaponOwnConstraints.Blockers`: equipped, allocated power equals required power, positive damage, cadence reached, and torpedo ammunition positive. The same helper supplies weapon-local observation flags, avoiding a second readiness formula. Existing active formation/live hull filtering supplies the additional participation constraints. No RNG, geometry, targeting, opportunity consumption, report emission or state mutation changes.

Observations are round-boundary facts. Maneuver, target arc/range/visibility and reactive beam defense are resolved later; defense may consume shared opportunities before offensive fire. The projection does not distinguish visible target eligibility, infer hidden targets, count opponents, inspect enemy reserves or orders, or promise a discharge. `OpportunitiesPerRound` remains the existing nominal own-package statistic, not a forecast of remaining offensive shots. Own commitment acceptance does not imply firing; the existing parent observation exposes commitment status separately. Existing cloak rules are not replaced by an invented cloaked-weapon blocker.

The old public snapshot constructor signatures remain available; manually constructed snapshots without authority-supplied context default to `OutsideEncounter`. Production observations supply the computed context. No presenter calculation is required.

## Typed historical fallback cause

Filtered report entries expose nullable `FocusFallbackCause`. It is `PreferredFocusUnavailableForOpportunity` only for a validated actor-owned `FocusFallback` report. Other entries return null. Consumers use this typed property instead of parsing `Detail` or a private save. The existing safe text remains for compatibility.

This cause means that the recorded target selection used a lawful visible/eligible hostile alternative when preferred focus was unavailable for that opportunity. The report's already-filtered target is a historical visible fact; it may subsequently be destroyed. The cause does not assert a shot occurred, expose the unavailable preferred target's hidden existence, or classify the reason as destruction, hiding, range or arc. Standing focus remains the player's intent and is not cleared by fallback.

More specific fallback reasons are **gated**. A current boundary cannot reliably reconstruct earlier per-opportunity eligibility. A later alternative would capture a filtered typed historical reason at resolution, review the exact actor-visible evidence and persistence/replay contract, and version it if necessary. This increment does not invent that history or reveal hidden state.

## Compatibility and acceptance

These fields are computed return-model properties only. No saved DTO, rules/compiler/resolver identifier, package/save/content format or migration changes. Nested Save 14 / Content 11, B1 core save 1 and physical composite save 2 remain. All legacy/B1/physical vectors retain their prior values.

Eleven new cases cover actual power-zero, torpedo stock-zero, initial cadence and zero-damage blockers with absent resolver battery/discharge reports; unarmed, strategic inspection, current-round cadence and completed encounter; hidden/all-hidden observations without target/package leaks; culture/reload and rejected-command equality; and all 64 combinations of the old weapon predicate. An existing actual fallback regression additionally checks typed cause, retained focus, reload equality and destroyed/inactive owned-ship flags.

Local locked restore/Release build passes with zero warnings/errors; full suite **1,685 tests passes**. PrepareUnity/static checks and standalone/B1/physical walkthroughs pass. Portable Linux/Windows/macOS exact-head CI is required and recorded on the draft PR. No Unity Editor, native player, desktop, client adapter or durable-store acceptance is claimed. Owner policy approval and all prior production gates remain in place.
