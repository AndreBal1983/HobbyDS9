# Combat UX orchestration and recent reports v1

Stacked candidate: `codex/combat-ux-report-fix-v1`, based exactly on audio candidate
`ec2ca216adec5b9e87fd702f0299e7a18aef0dfd`. Neither audio nor politics/geography nor
`main` is moved. The existing human-control/report correction was reused on this
new branch, then extended; the original branches remain separate acceptance targets.
The politics/geography branch already contained `f5a8293` when this task began
(`79009a3` is its production-dock ancestor); that existing work was preserved.

## Boundary and lifecycle

`Unity -> Application -> Simulation` is unchanged. `CombatOrchestrator` is a small
pure C# presentation controller, compiled in ordinary .NET tests as well as Unity.
An explicit `Pump()` after End Turn, successful load or Continue classifies frozen
pending encounters from the observed campaign's **persisted controller kinds**.
Any participating Human civilization makes the encounter human-involved; no name,
participant position or fixed single-player faction is assumed. Multiple persisted
Human controllers are supported locally (not multiplayer).

AI-only requests are processed in ordinal encounter-ID order with current observed
stamps. `StrategicClient.ResolveDetailed` resolves the authoritative pending request
by ID through Application, then calls `SubmitCombatResult`. Every AI-only encounter
in a mixed checkpoint is buffered, including those ordered after human encounters.
The next human planner pauses; after successful human resolution its report pauses
before the next planner. Continue acknowledges the report in presentation state.
When all results are buffered, human checkpoints expose explicit
**Finalize combat and complete turn**. Purely AI checkpoints call the same
`FinalizeCombat` automatically, remain on the player's strategic tab and display
“n AI battles resolved. View Combat Reports.”

The controller never reruns strategic phases, allocates encounters, changes seeds
or submits already-buffered requests. Automatic failure is surfaced and leaves the
checkpoint unfinished, with successful earlier submissions preserved. Rejections
never generate receipts/reports or trigger finalization. Opening/rendering the tab
has no campaign side effects. The player can still use Saves and other tabs during
a human checkpoint; automatic focus happens only for a new planner/report.

## Plans and authority

The existing default factory `DeterministicPhasedCombatResolver.CreateDefaultPlans`
is used both by default resolution and Application's composition:
FocusFire / Standard / Overcharge; LowestHullRatio; no retreat. No Unity copy of
combat doctrine exists. Resolver identity remains `combat-resolver.phased` v1.

`GetHumanCombatPlans` returns detached editable defaults for every persisted Human
fleet in the frozen pending request. `ResolveHumanCombat` rejects duplicate,
unknown and AI overrides, copies Human overrides, and fills all other fleets from
the canonical factory. Exactly one plan per participating fleet reaches the
existing resolver validation. The previous actor-scoped player APIs remain valid;
trusted pure resolver APIs remain available to headless hosts.

Different civilizations remain mutually hostile/free-for-all. The panel explicitly
says so and names every civilization in a multi-party heading. No alliances,
winners or implicit combat sides are invented. Future explicit hostility/sides can
replace that rule independently of client report rendering.

## Receipts and history

`CombatResolutionReceipt` contains the immutable submission result, immutable
`BattleReportSnapshot` only on successful buffering, and Human involvement. It
avoids exposing mutable request/result DTO aliases. Application projects validated
`GetResolvedCombats()` pairs into immutable reports; the client reconciles these on
capture/load without resolving them again.

`RecentCombatReport` stores the report and involvement flag in presentation memory.
History is newest turn first, then ordinal encounter ID; duplicate encounter IDs
are suppressed. Normally it retains at most 50 entries, **but always retains the
entire latest battle turn**, even if a single large checkpoint exceeds 50.
Finalization clears authoritative pending/resolved data, while history remains
renderable from frozen fleet/ship IDs and initial hull. It survives destruction of
authoritative ships/fleets. Campaign replacement clears old history and report
acknowledgments; an AwaitingCombat load reconstructs already-buffered reports.
Failed validation/load leaves the existing client session/history untouched.
History is deliberately absent from saves. Loading a Planning save starts with
empty history rather than displaying reports from another campaign.

The Combat tab shows **Recent Combat Reports** in Planning, including AI-only
reports. Summaries include location, turn, participants, held/destroyed/retreated
fleets per civilization, retreat destination, starting-to-remaining ship hull,
destroyed ships, shield/hull damage, shots/hits/misses and phases fought.
Expandable Long/Medium/Close sections show authoritative events in sequence order.
Unchanged hull and omitted fleet deltas mean held/surviving, not unknown. Damage
sums use BigInteger; no attacks or damage are recalculated. Labels come from detached
content/navigation snapshots with ID fallback for ship designs and locations.

## Verification and versions

Cloud: locked restore, Release build (zero warnings/errors), **771 .NET tests**,
portable Unity preparation/static checks and canonical/reverse/repeated 25-turn
smokes. The new controller acceptance runs all 25 turns against manual default
resolution, both continuously and with checkpoint reloads, comparing every turn's
save and the established final hash. Existing reordered/repeated/reloaded acceptance
and rollback tests remain unchanged. CI runs these tests and all three smoke paths
on Linux, Windows and macOS. Unity Editor/player/audio execution is unavailable in
cloud; no Unity runtime verification is claimed.

**32 authored Unity EditMode cases**: 21 strategic/politics/geography/production,
3 audio, 3 human-control/buffered-report and 5 orchestration/recent-history cases.

Save **12**, content **8**, rules **population-politics-geography-v1**, resolver v1,
RNG identity unchanged. Canonical/reverse/repeated final save SHA256:

```text
10009857d0396dea4bd0a81001cadfb6b5ebfad2c8772223d8288bd70d3559e4
```

Expected 25-turn totals: turn 26, revision 187, 17 encounters / 12 checkpoints,
9 ships / 9 fleets, allocation 97, RNG `d8919406d6e01cf1`; 98 shots, 68 hits,
126 shield damage, 116 hull damage, 10 destroyed ships/fleets, no retreats.
The soundtrack, import settings, preferences and AudioDirector are unchanged from
the audio candidate. No music restart/context hook was added to combat views.

## Exact local Windows acceptance

Close Unity before running preparation/batch tests. Use Unity **6000.6.5f1**, its
Windows Build Support **IL2CPP** module, Visual Studio Desktop development with C++
and the Windows SDK. From the repository root in PowerShell:

```powershell
git fetch origin
git switch codex/combat-ux-report-fix-v1
git pull --ff-only origin codex/combat-ux-report-fix-v1
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
python scripts/check-unity-integration.py
$unityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe'
dotnet msbuild scripts/PrepareUnity.proj -t:EditMode "-p:UnityEditor=$unityEditor"
dotnet msbuild scripts/PrepareUnity.proj -t:Windows "-p:UnityEditor=$unityEditor"
& './artifacts/unity/Windows/HobbyDS9.exe'
```

Windows target reruns EditMode before building x64 IL2CPP. Check
`artifacts/unity/editmode.xml` for **32/32 passed**, `editmode.log` and
`windows-build.log` for errors, and launch the generated player. These outputs and
Unity caches are ignored, never committed.

Manual checklist:

- Advance the actual DS9 slice: Dominion/Cardassian-only combat completes without
  opening the planner or asking for tactics. Turn/revision reach next Planning;
  non-blocking feedback appears and Combat voluntarily shows the AI battle report.
- For a Human/Federation battle, planner opens. All participants are shown, only
  Human fleets have tactic/priority/retreat controls; AI doctrine is read-only.
- Resolve with defaults and selected Human plans. Report shows location/turn,
  per-fleet/civilization outcomes, hull changes, losses/retreats, shots/hits/misses,
  shield/hull totals and ordered expandable phase events. No ambiguous winner label.
- Continue through multiple Human encounters; AI-only encounters do not interrupt.
  Finalize explicitly once every result is buffered. Reports remain on the Combat
  tab in Planning and can show ships/fleets that were destroyed.
- Save in pending/partial/fully buffered AwaitingCombat, load it, and confirm no
  repeated phases, duplicate submission/report, lost result or skipped finalization.
  A bad/incompatible load preserves the current campaign; a new Planning load
  clears old presentation history. Saves remain usable during the combat pause.
- Keep music running through AI auto-resolution, Human planning/report/Continue and
  finalization: no restart, duplicate director/source or preference changes.
- Recheck production dock, politics, weighted travel, selection, zoom/pan and saves
  at 1920×1080, 2560×1440, 3440×1440 and 5120×1440.
- Confirm 32/32 EditMode, Windows x64 IL2CPP build and real standalone launch.

Do not merge any acceptance branches or main until local acceptance is returned.
