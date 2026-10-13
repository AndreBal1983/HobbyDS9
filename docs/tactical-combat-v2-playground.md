# Play tactical combat v2 in the Windows console

This is a **human-playable standalone sandbox**, built on [PR #9](https://github.com/AndreBal1983/HobbyDS9/pull/9), starting at `53c81b4eaebf9cd304f04d7a58d7348db0480dcb`. It uses the existing [one-round backend](tactical-combat-v2-backend.md) without a campaign adapter, Unity or graphics. Sample names describe authored fixtures, not faction bonuses or canon claims. Balance remains provisional. The branch `codex/tactical-combat-v2-playground` is delivered as a **draft**, without merge.

## Play now — no .NET installation needed

The complete self-contained Windows x64 folder has been published locally. Open PowerShell yourself and run this single command:

```powershell
& 'C:\Users\ambal\.codex\worktrees\tactical-combat-v2-playground\HobbyDS9\artifacts\tactical-playground\win-x64\HobbyDS9.TacticalPlayground.exe'
```

Choose scenario **1, 2 or 3**. Or start directly with the cruiser and a fixed seed:

```powershell
& 'C:\Users\ambal\.codex\worktrees\tactical-combat-v2-playground\HobbyDS9\artifacts\tactical-playground\win-x64\HobbyDS9.TacticalPlayground.exe' --scenario cruiser --seed 47
```

Keep the **whole `win-x64` folder** together when copying it elsewhere. It contains the executable, `HobbyDS9.TacticalPlayground.dll`, Contracts/Simulation DLLs, `.deps.json`, `.runtimeconfig.json`, and the bundled .NET runtime/native libraries. The initial publication contained 196 files, approximately 81.4 MB. This is a portable playable **folder**, not a single-file executable. No scenario data files are missing: these three explicit fixtures are authored in the host. A graphical desktop was never operated during validation; the console binary was exercised with redirected/piped text.

## Controls and walkthrough

The pre-round **OMNISCIENT SANDBOX VIEW** deliberately shows every authored formation/ship on both sides: hull, armor, four current/max shield sectors, recharge/power, weapon classes/arcs/opportunities/cadence, finite ammo, relative range/facings, standing orders and local objective progress. This is a debug playtest view, not campaign fog of war.

1. At a round boundary, press **Enter** to choose orders. **S** saves, **L** loads, **D** toggles detailed event logs, and **Q** quits. Save/load asks for a JSON path; blank cancels. Failed load preserves the current game.
2. Choose a maneuver for **each active human formation**. Type a number or name, or leave blank to retain its existing order:

   | Number | Order | What to try |
   | --- | --- | --- |
   | 1 | Maintain Distance / Hold & Fire | Stabilize at preferred range; reserve some beam capacity for defense |
   | 2 | Charge | Close and prioritize offense, risking enemy forward weapons |
   | 3 | Flank | Seek lateral enemy exposure and deny its forward arcs |
   | 4 | Screen | Protect an allied formation with beam opportunities; choose the protected ally |
   | 5 | Breakthrough | Close and attempt the fixture's local breakthrough goal |
   | 6 | Pursuit | Close on withdrawing opponents |
   | 7 | Fighting Withdrawal | Open range while broad/aft-covered weapons may continue firing |
   | 8 | Disengage | Attempt extraction; requires a successful distance/contest condition |

3. Hold/Flank/Screen also ask for preferred range: **1 Distant, 2 Engagement, 3 Close, 4 Intermixed**. Choose target priority **1 LowestId, 2 LowestHullRatio, 3 CommandSupport, 4 Screen** and an available enemy formation. Target **0** clears concentration; blank retains it. Screen can designate an available ally or **0** for self only. No control reallocates shields or individual weapon banks.
4. Review the proposed orders. **Enter/Y** commits and resolves **exactly one round**; **N** discards the preview so you can revise; **Q** quits. EOF never confirms an unfinished choice. The fixed opposing sequence is committed in the same backend call and never counter-picks newly selected human orders.
5. Read the report, then choose again at the next boundary. Completion is explicit, never later than round three. **R** starts the same fixture/seed again; the next completed run shows both human hull/survivor totals and order sequences for comparison. **N** returns to the scenario menu; **Q** quits.

Numbers and enum names are accepted; maneuver names ignore spaces/hyphens/case and `hold` aliases MaintainDistance. Blank retains previous choices. A retained target that has exited cannot force a shot; the backend acquires an eligible alternative. The program does not clear or control the screen.

## Three distinct engagements

| Scenario | Human force / goal | Fixed opponent sequence |
| --- | --- | --- |
| `cruiser` / 1 | Agile Federation-style cruiser with broad arrays and torpedoes vs two forward-heavy attackers; compare Flank/Charge | Charge, Charge, FightingWithdrawal |
| `screen` / 2 | Escort with four beam opportunities protecting an unarmed transport from five-opportunity torpedo salvos | MaintainDistance, concentrating on the transport, all rounds |
| `breakthrough` / 3 | Fast runner needs two successful Intermixed breakthrough rounds against a defending line | Screen, Pursuit, Pursuit |

The opposing force is a documented **script, not tactical AI**. The sandbox exposes this sequence in its debug briefing. Complete ship/mount/power data is in [PlaygroundScenarios.cs](../src/HobbyDS9.TacticalPlayground/PlaygroundScenarios.cs). No live blueprint or faction data is rewritten. The initial sector splits are fixed; attacks change exposure/depletion, not allocation.

## Example: orders change the result

Same `cruiser`, seed **47**, all other fixture inputs equal:

```text
Flank, round 1:
  Maneuver formation.attack-wing / formation.cruiser:
    Engagement -> Engagement; facings Fore/Fore -> Starboard/Fore
  The forward-only attack wing emits no offensive discharges.
  The cruiser strikes the attacker's Starboard sector.

Charge, round 1:
  Engagement -> Close; facings Fore/Fore -> Fore/Fore
  ship.cruiser: incoming landed 170; shields absorbed 70;
    armor removed 38; post-armor 62; hull lost 62; overkill discarded 0.
```

After three Flank rounds, cruiser hull is **220/220**, enemy hull total **132**; the sandbox checksum is `0de23f5fb76b8e3150a0593d69a760254c7a931a669c5258beb06d5b2618eee3`. Three Charge rounds leave cruiser hull **63/220**, enemy hull total **170**, checksum `f67cd3e66055f7db10880ddb37845459f6c81bf2d410db7870f2f557eb07052e`. Both end at the round limit, without inventing a winner.

In the screen fixture at seed 47, the first Screen round spends **four beam opportunities** and intercepts **three torpedoes**; the failed attempt also costs an opportunity. The protected transport takes no landed damage that round. Compare switching the escort to Charge. The breakthrough fixture completes after two Breakthrough rounds: runner **222/240 hull**, `BrokenThrough`, goal **2/2**, checksum `ac8cbcb951e0f75ccab886a4adaba63367941bedaa30e9dd992115b2c65c17a6`. Holding instead does not achieve the runner's crossing objective.

Default reports include geometry/facing changes, discharges/targets/hits/misses, attempted and landed yield, interception/offensive cost, per-sector absorbed damage/recharge, exact armor reduction, actual hull loss/overkill, destruction and mission progress. **Attempted yield** includes misses/intercepted launches; **landed yield** reaches shields. **Post-armor** damage can exceed remaining hull; only the bounded actual hull loss is applied, and the rest is discarded overkill. All values come from backend events/state; the host does not duplicate hit, defense, armor or maneuver formulas. `--details` / **D** appends every immutable authoritative event. Rendering/replay consumes no RNG.

## Save and resume

In interactive mode, use **S** after round 1 or 2 and give a path such as `C:\Users\ambal\Downloads\combat-playground-save.json`. Later launch with:

```powershell
& 'C:\Users\ambal\.codex\worktrees\tactical-combat-v2-playground\HobbyDS9\artifacts\tactical-playground\win-x64\HobbyDS9.TacticalPlayground.exe' --resume 'C:\Users\ambal\Downloads\combat-playground-save.json'
```

`--save path.json` enables atomic checkpoint writing after each resolved round. The standalone host save format **1** / fixture version **1** includes the entire v2 boundary/RNG plus submitted human orders and exact events. Loading reconstructs the authored starting fixture/seed, replays the recorded joint rounds, and rejects mismatched history or boundary state. This preserves fixed fits, scripted opponents and RNG, rather than trusting an edited save to grant shields/ammo. No campaign save/migration is accepted. Files have a 16 MiB input ceiling. Temporary writes use the destination directory, flush, then replace; a failed validation/load does not replace the live session or previous valid save.

## Scripted play and reproducing the build

With .NET SDK **10.0.401**, these commands work from the isolated checkout:

```powershell
Set-Location 'C:\Users\ambal\.codex\worktrees\tactical-combat-v2-playground\HobbyDS9'
dotnet run --project src/HobbyDS9.TacticalPlayground -c Release -- --help
dotnet run --project src/HobbyDS9.TacticalPlayground -c Release -- --scenario cruiser --seed 47 --script 'Flank;Flank;Flank'
dotnet run --project src/HobbyDS9.TacticalPlayground -c Release -- --scenario cruiser --seed 47 --script 'Charge;Charge;Charge'
dotnet run --project src/HobbyDS9.TacticalPlayground -c Release -- --scenario screen --seed 47 --script 'Screen;Charge;Screen' --details
dotnet run --project src/HobbyDS9.TacticalPlayground -c Release -- --scenario breakthrough --seed 47 --script 'Breakthrough;Breakthrough'
dotnet run --project src/HobbyDS9.TacticalPlayground -c Release -- --scenario cruiser --seed 47 --script 'Flank' --stop-after 1 --save artifacts/tactical-playground/save.json
dotnet run --project src/HobbyDS9.TacticalPlayground -c Release -- --resume artifacts/tactical-playground/save.json --script 'Flank;Flank'
dotnet publish src/HobbyDS9.TacticalPlayground/HobbyDS9.TacticalPlayground.csproj -c Release -r win-x64 --self-contained true -o artifacts/tactical-playground/win-x64
python scripts/check-tactical-playground.py
```

Inline `--script` tokens apply to the first active human formation at invocation. Other human formations retain their orders. Short scripts and blank tokens retain orders for remaining rounds. Numeric maneuvers work (`'3;3;3'`). On resume, script steps describe **remaining rounds**, while `--stop-after 1|2` is an **absolute** boundary and requires both `--save` and `--script`. If an explicitly ordered formation dies, subsequent commands for it reject rather than silently controlling another ship; blank remaining steps retain surviving orders. `--reverse-fixture` checks canonicalization without changing the result. Bad arguments/scripts exit **2**; success/user quit/EOF exits **0**.

For several formations, target/range/protection or mixed priorities, pass a JSON file instead of inline maneuvers:

```json
{
  "Rounds": [
    {
      "Orders": [
        { "FormationId": "formation.escort", "Maneuver": "Screen", "TargetPriority": "LowestHullRatio", "PreferredRange": "Engagement", "ProtectedFormationId": "formation.transport" },
        { "FormationId": "formation.transport", "Maneuver": "MaintainDistance" }
      ]
    },
    { "Orders": [] },
    { "Orders": [] }
  ]
}
```

`--script .\my-orders.json` runs that file. Enum fields use string names; unknown fields/values reject. An omitted formation retains orders; a supplied DTO replaces its order fields (omitted fields use contract defaults). No hidden attack selector or subsystem targeting exists.

## Acceptance and scoped fix

Local validation on 13 October 2026: locked restore; **zero-warning/error Release**; **1,306 passing tests, zero failed/skipped** (1,273 existing plus 31 playground cases and two shield-power regressions). New coverage includes actual TextReader/TextWriter console flows, confirmation/cancellation/EOF, same-seed interactive replay, scripted/JSON orders, argument validation, stale/foreign/duplicate submission, deep ownership, atomic saved boundaries 1/2, corrupt-file rejection, event/report accounting, deterministic repeated/reversed outputs, and genuine order differences. The published Windows executable ran all three scenarios, a piped one-confirmed-round interaction, and save/resume checks; output logs are local ignored artifacts. These are headless console checks, **not human visual acceptance**.

Focused v2 fix: PR #9's validator admitted `ShieldPowerRequired=0`, `ShieldPowerAllocated=0`, `ShieldRechargePerRound>0`, which then recharged under its allocation comparison. Positive recharge now **requires positive required power**; zero-power passive sectors with **zero recharge** remain legal. Underallocated positive-power shields still cannot recharge. No damage algorithm, rules identity, DTO field, campaign v1 policy or golden is changed. The existing v2 reference checksum remains `ccae7836c085936ed962b9ba001764ae4252f135fdf62fa1fbe2c0ea0bcef07c`; authored playground vectors are separate.

Legacy 25-turn canonical/reverse/repeated result remains save SHA256 `fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`, RNG `d8919406d6e01cf1`. Campaign Save **14**, Content **10**, rules **science-engineering-v1**, compiler **ship-design-compiler-v1** and default phased resolver v1 are unchanged. Portable preparation/static boundary checks pass. The final-commit Linux/Windows/macOS workflow also runs all three scripted engagements and save/resume; exact CI run links/status are recorded in the draft PR/final handoff.

Deferred: campaign cutover/projections/authorization, real tactical AI, strategic objective consequences, resupply/recovery, compiler v2 equipment outputs, production balance and Unity choreography. The omniscient fixture view is not a fog-of-war contract. Sides, ranges, four shield sectors and local objectives use the existing provisional backend. **Unity Editor, batch mode, GUI/player/native visual acceptance and Agent 2/PR #10 work: NOT RUN / NOT TOUCHED.** See [backend follow-up gates](tactical-combat-v2-backend.md#follow-up-gates-before-campaign-cutover). No automatic merge is performed.
