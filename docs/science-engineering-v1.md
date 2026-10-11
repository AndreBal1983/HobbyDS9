# Science & Engineering foundation v1

Base: `a0f750681438d26ded17fff38cb3df37c3da9eb0`; branch `codex/science-engineering-foundation-v1`. Main stays unchanged pending local Unity acceptance.

S1 introduces portable explicit research-state/receipt/command payload contracts and independently tested Science functions. E1 integrates these with authoritative sessions and replaces the legacy research path; the live schema switches atomically there to save 13/content 9/rules `science-engineering-v1`. The interim S1 checkpoint preserves the existing executable campaign until replacement is complete. No Engineering application gameplay is added in S1.

Science disciplines are explicitly numbered Biotech, Computing, Construction, Energy, Propulsion, Weapons. Levels 1–10; Standard interval costs: 100,130,170,220,290,380,500,650,850. Pace factors: Fast 5000, Quick 6700, Standard 10000, Epic 15000, Marathon 30000. Costs use integer ceiling with BigInteger intermediates and checked conversion to long.

Discovery seeds are mixed from initial campaign seed and canonical civilization ID using FNV-1a and isolated SplitMix64. A separate local stream derives interval count (3–5), Fisher–Yates ordinal order, signed 10–15% raw weight deviations and largest-remainder integer normalization (ties by threshold index). Chunks are positive and sum exactly to cost. No shared campaign RNG is queried or advanced. Schedules are derived, never persisted.

Science allocation is floor(R&D × allocationBps / 10000); Engineering receives the exact remainder. Reserves bank, Science consumes only the selected discipline, crossing every ordered threshold and carrying through multiple levels. At level 10 progress is zero and remaining reserve banks without redirection. Explicit statistics and ordered receipts support conservation verification.

S1/E1/I1 are implemented below. Cloud verifies portable integration; Unity/Windows player acceptance remains local. Combat resolver and RNG versions remain unchanged. No refinement, black-box technology, modular ships, diplomacy, espionage, graphics menu or unrelated gameplay changes are authorized.

## E1 authoritative integration

The session owns explicit Science/Engineering state. The closed envelope supports `SetResearchAllocation` (6), `SetScienceFocus` (7) and `SetEngineeringTarget` (8, nullable target to clear); old generic research command/state/definitions are removed. Ordinary rejections use stable codes and preserve state/RNG/allocation/revision; accepted commands change plans only. The Research phase allocates and spends on deep turn staging.

Engineering retains per-application progress when switching/clearing targets. Maturity is floor(progress × 100 / adjusted cost): Development 0–24%, Prototype 25–49%, Operational 50–99%, Mature 100%. Only Mature unlocks designs/prerequisites; completion clears the target and banks surplus. Science requirements use structural ordinals, not positional indices; higher levels satisfy earlier intervals. Validation rejects missing references, duplicate requirements, cycles and unrepresentable adjusted costs.

DS9 starts at Standard pace, 7000 Science BPS, Construction focus, six level-1 disciplines and independent persisted discovery seeds. Advanced Starship Engineering costs 60 and requires Construction interval 1/ordinal 1 and Propulsion interval 1/ordinal 2. Advanced Scout requires its Mature application. No designs or economy outputs were added.

Save 13/content 9/rules `science-engineering-v1` reject save 12/content 8; no automatic migration is provided for this replacement. Campaign RNG and combat resolver identities are unchanged. Detached research projections include nested facts and bounded canonical latest-turn receipts. Nonzero authored starting reserves are starting stock rather than generated R&D; allocation counters record generated points only. Actual scenario starting reserves are zero.

## I1 client integration

`ResearchOverviewSnapshot.For` projects current R&D output, exact per-turn floor allocation, reserves, six Science cards (cost/percentage/ordered structural breakthroughs), Engineering requirements/eligibility/progress/maturity and latest receipts. Nested collections are detached read-only copies. Availability is always derived from current monotonic Science/prerequisite state; there is no persisted availability cache to invalidate after breakthroughs. The same eligibility rules gate commands and design production. Content requirement order is normalized independently of input ordering.

Unity retains the existing Research navigation section and renders `ResearchPanelView` within its responsive details scroll. Split options use 5% steps (including an exact current custom BPS value if needed); Apply is explicit. Focus/target/clear controls call `StrategicClient` with the observed turn/revision and typed success/error SFX feedback. Cards show structural threshold ticks and adjusted costs; application progress survives retarget/load. Rendering and editing a dropdown do not mutate campaign state. No new audio, images, fonts, graphics menus or gameplay knobs were introduced.

Client bootstrap sets `QualitySettings.vSyncCount = 1` and `UnityEngine.Application.targetFrameRate = -1` before scene rendering, including before a pre-existing entry point is detected. These defaults are presentation-only; project QualitySettings and persistent preferences are unchanged.

## Deterministic 25-turn acceptance

Canonical, reverse combat order, repeated execution, reordered content/state and repeated saves/reloads (including pending, partially buffered and fully buffered encounters) converge on:

- save SHA256 `52487572e9ee41003afb0143dd08748b9ea9cf4590af54171d2e1651e9282fef`;
- content file SHA256 `7317beab247763b6a57e429bd8c8087ae71646a5b7ebf8411ce6dffc131b3e68`;
- completed 25, next turn 26, Planning, revision 185, AI commands 131;
- 17 encounters, 12 AwaitingCombat checkpoints, 43 range phases, 98 shots, 68 hits, shield damage 126, hull damage 116, 10 ships/fleets destroyed, 0 retreats;
- 9 ships/fleets remain, 2 queued items, 16 production completions, next entity sequence 97;
- campaign RNG `d8919406d6e01cf1` (unchanged SplitMix64 identity/version);
- births 501, deaths 50, migrations 31, refugees 0, food delivered 13, resources extracted/delivered/consumed 275/5/32;
- movement cost paid 189, edges completed 111, partial-traversal turns 26; political disruptions 0.

Each civilization generates 100 R&D, allocates 50 Science and 50 Engineering, spends 50 Science and 0 Engineering, ends with 0 Science reserve and 50 Engineering reserve. Starting reserves are explicitly 0/0. Construction remains level 1 at progress 50; the other five disciplines remain level 1 at progress 0. Breakthrough totals: Cardassian 1, Dominion 2, Federation 2. No levels complete or applications mature. The authored 70/30 allocation produces 2/2 from each turn's output of 4 because Science floors per turn; rounding is not deferred or accumulated. Aggregate conservation: 300 generated = 150 Science spent + 0 Engineering spent + 0 Science reserve delta + 150 Engineering reserve delta. No human orders are injected to force Advanced Scout eligibility.

This intentional rules/schema replacement changes the old research baseline hash. All combat/economy/geography totals above remain unchanged; two obsolete AI research-target commands disappear, lowering revision and command totals by two.

## Local Unity acceptance required

Cloud verifies .NET and static integration, not Unity compilation/rendering/audio or Windows IL2CPP execution. On Windows with Unity 6000.6.5f1, an active license, Windows Build Support (IL2CPP), Visual Studio Desktop Development with C++ and the Windows SDK:

```powershell
# Pull the candidate branch; close any open Editor before batch tests.
git switch codex/science-engineering-foundation-v1
git pull --ff-only
$unity = "C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet msbuild scripts/PrepareUnity.proj -t:EditMode "-p:UnityEditor=$unity"
dotnet msbuild scripts/PrepareUnity.proj -t:Windows "-p:UnityEditor=$unity"
.\artifacts\unity\Windows\HobbyDS9.exe
```

Check compilation and all authored EditMode cases, then the standalone player. In Research verify the 70/30 start, explicit split Apply, all six focus controls, progress/breakthrough markers and Engineering's initial Construction/Propulsion lock. Change focus/split, advance turns and cross both required structural breakthroughs; select Engineering, observe Development/Prototype/Operational/Mature, then Advanced Scout unlock. Save/load mid-Science, mid-Engineering, after target clear/retarget and during combat; malformed/old saves must preserve the live session. Check receipts, accepted/error SFX, existing production/movement/combat/report/audio flows and responsive layouts at 1920×1080, 2560×1440, 3440×1440 and 5120×1440. Confirm VSync defaults and absence of new graphics settings.

Deferred: refinement, black-box technologies, modular engineering, diplomacy/espionage, new navigation/economy/species rules, new ships/buildings, graphics menu and multiplayer. Main stays at the accepted base until this candidate passes local acceptance.

## Cloud verification and staged checkpoints

S1 `838d80ee33ebe230a0e0f1d3af07d66d1ef57083` passed 841 .NET tests. E1 `1998ef2ea69816cedca7e6eb29bd9272b6be20b0` passed 884 tests and [three-platform CI](https://github.com/AndreBal1983/HobbyDS9/actions/runs/38166228711). I1 passes 909 tests, locked restore, Release build with zero warnings/errors, portable assembly/scenario preparation and Unity asset/reference/static checks. Canonical and reverse headless runs match the baseline above; the full suite covers repeated/reordered state and content, ordinary/mid-progress reload and pending/partial/full combat checkpoint reload. Research command phase/stamp/control/deduplication guards, exact maturity boundaries, production unlock, counter overflow and protected-phase rollback are included.

There are 58 authored Unity EditMode cases (five added for Research), not a claim of cloud execution. Unity compilation, all 58 cases, responsive rendering/audio and Windows x64 IL2CPP/player acceptance remain the local gate.
