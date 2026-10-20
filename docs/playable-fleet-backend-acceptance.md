# Playable fleet backend acceptance

20 October 2026. Bounded existing-class milestone, based on reviewed `e3e76216c5d1033ae51614e364ccb1717506bb99`. This is a headless candidate for independent review and frontend integration, not main acceptance or a native-player claim. The [contract](playable-fleet-backend-contract.md) was committed before implementation.

## Implemented path

The new `ICampaignPlayableFleetSession` extends the existing integrated session. Two paid source Cruisers complete through ordinary production, retain authored equipment and finite stocks, assemble into a named allocated fleet, issue lawful Follow from current contact knowledge, resolve the actual committed human/AI round, expose actor-safe typed playback, finalize atomically and reload with identical observed history. No later human round is simulated to produce a movie.

Create/transfer/rename require owned colocated stationary fleets in Planning, matching turn/revision and a new command ID. Accepted routes and non-Stay intentions require explicit clearing first. Empty source fleet entities/settings/orders are removed; last-known contacts and historical receipts remain historical. Original construction fleet records stay immutable. Failed allocation/late validation leaves authority, names, IDs, replay and RNG unchanged. Full accepted-operation replay proves current membership and rejects forged roots, names and truncated saves.

The scenario contains byte-identical copies of `singleplayer-demo-v1` scenario/core/physical/package JSON. Its separate `playable.json` selects existing lawful Cardassian defense AI at Bajor, retains the source's dormant Dominion, declares the 2048-operation technical review budget and has no Demo4 deadline/victory wrapper. No new class, balance, designer publication or refit rules.

## Versions and deterministic vectors

| Contract | Value |
| --- | --- |
| New opt-in wrapper | 6 / `campaign-tactical-v2-playable-fleet-1` |
| Embedded owned-core marker | `campaign-tactical-v2-playable-fleet-owned-core-1` |
| Existing wrappers | 1/2/3/4/5 unchanged; no migration or fallback |
| Legacy save | 14 |
| Foundation content / authored equipment content | 10 / 11, unchanged |
| Rules / ship compiler | `science-engineering-v1` / `ship-design-compiler-v1`, unchanged |
| Package SHA256 | `87e57e759ad9ddbe0eebcecef86fb04a9c55c025ea28031205ac323c2c9c41c7` |
| Walkthrough save SHA256 | `f63317e1aa708b6e6c5bcb278c0d9aecfa17ca264f94290013a1b4f6a9d9cec5` |

The walkthrough compares canonical, reverse selected-ship/order enumeration, repeated and save/reload runs. Tests additionally reorder source civilization/fleet/design/contact collections. Existing Demo4 and other legacy golden assertions passed unchanged.

## Reproduction and evidence

```text
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --playable-fleet
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
python scripts/check-unity-integration.py
```

Release build: zero warnings/errors. The full suite passed 1854 cases with zero failures/skips, including 31 new playable-fleet cases and the capacity correction. The outer JSON follows the repository LF policy so exact package bytes match across platforms. All six separately built portable client projects passed: adapter200, UX115, client51, shell23, storage47, travel89 (525 total). PrepareUnity and static integration passed; no Editor or native player was launched.

`PlayableFleetTests` covers real construction and later construction into a newly created fleet; create/transfer/rename, source retirement, ownership/stamps, explicit route clearing, partial travel/combat exclusion, allocation exhaustion/late rollback, root forgery, old-loader rejection, waiting-round reload, actual event/condition reconciliation, history rollback/once-only aftermath, hidden-launcher projection, large damage totals, explicit replay budget exhaustion and deterministic walkthrough variants. Hidden-launcher projection uses a clearly isolated raw-event fixture, never imported campaign authority.

Capacity regression cases cover exact-fit and one-slot-short admission, a paid construction completion rolled back with the rejected turn, multiple independent encounters resolved in reverse order, AI-only completed batches, reload before each remaining round/finalization, all combat-phase nonprogress APIs, invalid/stale/duplicate input and Planning rename consumption.

The supplied 2048-rename transcript measured about 967–973 ms to reload and 530693 serialized characters locally. This is diagnostic, not a portable performance assertion or worst-case replay guarantee.

## Frontend handoff and open gates

Bind `CampaignPlayableFleetApplication.StartPackage/LoadSave` and the complete new6 codec. The host raw-resource hash assembly is in `src/HobbyDS9.Headless/PlayableFleetPackageReader.cs`; preserve exact resource bytes and the complete wrapper. Use a distinct profile/resource/save namespace. Keep the original Demo4 and Frontier5 entry points separate. Never render private save DTOs.

Playback event keys are encounter/round/sequence. Before/after relations contain only visible pairs. Condition events identify the affected target; they do not invent an attacker. Incoming hidden torpedoes have null source ship/fleet and range. Damage totals are received damage; source shot/hit/miss counts are observed only. Own final condition includes actual remaining equipment/stocks. Skip/speed/camera controls must only change presentation.

Independent persistence/code review, exact-head cross-platform CI and combined frontend validation remain delivery gates. Secondary owns new6 codec/resources, Unity import/build/native lease, actual paid production/assembly/right-click combat journey, visual model bindings, audio, responsive and load/modal negatives. The prior R8 native gaps remain open until exercised in that combined player. No main merge or validated-pointer promotion is authorized by this milestone.

## Independent-review capacity correction

Candidate 68ad1db is retained as the review baseline. Review found that the technical transcript cap could strand a pending battle. The follow-up only permits a staged advance when the remaining bound accommodates that advance, every remaining human round across the complete actual batch, and once-only finalization. AI-only completed encounters still reserve finalization. A failed admission publishes none of the staged economy/AI/production/movement/IDs/RNG or history. The budget is not raised or exposed as a gameplay statistic. Accepted combat commands must progress a round; all other combat-phase mutations reject without spending capacity. Unsafe pending saves from unreviewed 68ad1db now fail replay validation explicitly; no migration is provided or claimed. Normal walkthrough vectors and older profiles remain unchanged.
