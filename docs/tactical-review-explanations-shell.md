# Authoritative own explanations and runnable trusted review shell

14 October 2026. **Headless review tool, opt-in, policies proposed, production activation disabled.** No Unity playability, multiplayer authentication or secrecy from the trusted host is asserted.

## Preserved provenance

This fresh codex/tactical-review-explanations-shell branch starts at combined [PR #28](https://github.com/AndreBal1983/HobbyDS9/pull/28), **2a3b2fdedfdcbb62619b316d91fd56936d0e8a50**, then ordinarily merges delivered [PR #27](https://github.com/AndreBal1983/HobbyDS9/pull/27), **d52247605feaed9ff54ee3547a358af7466e7498**.

Local merge **597a1c9d32fcc9391749680b1a9093fde557d649** has those exact first/second parents. Their common physical checkpoint is #26 **e268b2c490f424b96d21593e9c1427d39c7f5be7**. Both histories, including #28's #23/#25/#24 ancestry, remain intact. No main/source-PR merge, reset, force push or rebase is performed.

The only imported backend changes are the exact seven-file delivered #27 increment. Backend production and backend tests are byte-identical to #27, beyond this branch's new client/shell/tests/docs/workflow. Read AGENTS, [architecture](architecture.md), [explanation guarantees](campaign-tactical-v2-weapon-explanations.md), [complete-root client/storage proof](tactical-travel-client-integration.md) and [durable atomicity/privacy limits](tactical-review-durable-checkpoints.md). Main, production Unity, source scenarios and ci.yml remain unchanged.

## Explanation binding

[Renderer](../prototypes/HobbyDS9.TacticalReviewClient/ReviewTextRenderer.cs) uses actual immutable Availability / Unarmed / FocusFallbackCause. It neither derives flags from power/ammo/cadence nor queries opponents to determine readiness.

| Supplied status/fact | Display meaning |
| --- | --- |
| OutsideEncounter | No current battle-round context or live firing readiness; show supplied own flags and outside-context round |
| OwnStateBlocked | Supplied own constraints block participation at the observed boundary; list actual own blocker flags |
| TargetDependent | No listed own blocker; **no shot promised**. No target availability, remaining shots, chance, damage or outcome forecast |
| Unarmed | Backend boolean means no installed weapon packages; a stock-empty or unpowered ship remains armed |
| EncounterComplete blocker | Complete encounter has no further round/fire; supplied completion status is shown |
| PreferredFocusUnavailableForOpportunity | A lawful alternative was used for that recorded opportunity. No shot implied; no invented destruction/hiding/range/arc reason |

Own-state explanations now exist. The retained phrase “Readiness explanation unavailable for a specific target” refers only to the unavailable target-specific decision/forecast, preserving the distinction and prior tests. Enemy inspection still contains only lawful visible condition, never own-style availability, equipment, stocks or flags. Detail strings/private save data are not parsed for fallback explanations.

No rules/compiler/content/save/resolver identities change. B1 save 1, physical complete save 2 and nested legacy Save 14 remain. More specific historical fallback reasons and target firing predictions remain gated.

## Runnable entry point

[TrustedReviewShell](../prototypes/HobbyDS9.TacticalReviewShell/TrustedReviewShell.cs) is a small .NET 10 host entry point around the **existing** PhysicalReviewHost, ReviewController, typed codec and disk store. It does not provide an alternate save path, simulation, AI/opponent policy, invented legal menus or source provisioning.

From this delivery checkout root, run this exact scripted faster demo in an **empty explicit review-only directory**:

    dotnet run --project prototypes/HobbyDS9.TacticalReviewShell/HobbyDS9.TacticalReviewShell.csproj -c Release -- "scenarios/tactical-v2-physical-review/faster" "E:/HobbyDS9/artifacts/review-shell-faster-v1" "civ.federation" --demo

For slower, change faster to slower and the directory to E:/HobbyDS9/artifacts/review-shell-slower-v1. The source directory contains exact package.json, core.json and scenario.json; hashes come from those bytes. The supplied directory is the only storage location. Demo slots **demo-waiting** and **demo-final** are explicit review files. Occupied slots reject; there is no automatic overwrite or user-save enumeration. For Linux/macOS use an explicit absolute review-only path appropriate to that host.

Omit --demo to read commands from stdin interactively or through a caller-provided pipe. No Codex terminal/desktop automation is required. The command prints help then accepts:

| Command | Purpose |
| --- | --- |
| show / refresh / inspect ship-id / report [round] | Current lawful facts, own availability or filtered historical reports; no mutation |
| actor id [discard] / encounter id [discard] | Deliberate lawful scope selection; dirty draft replacement requires explicit discard |
| route own-fleet known-destination | Actual backend known-route result, costs and stamps, not an ETA/pursuit forecast |
| travel fleet intent [destination or -] [contact or -] | Local draft using Stay/StayEngage/Leave/Follow and actual observed data |
| send-travel command-id | Explicit reviewed stamped travel submission |
| fleet fleet maneuver visible-focus-or-- / round command-id | Local fleet order change then explicit simultaneous human commitment |
| cloak fleet true-or-false command-id | Actual owned Planning setting |
| advance / finalize | Explicit authoritative strategic advance / ready atomic batch aftermath |
| save slot [overwrite] / load slot [discard] / open slot [discard] | Existing complete-root durable operations; open validates a new trusted host |
| reset / exit | Clear local edit without a command / end host input |

Every reply labels **ACTOR VIEW** and the local trusted hotseat context. A successful actor switch constructs a valid scoped controller before replacement, clears inspection/local edits and labels the new view. Failed unknown-actor selection or missing discard keeps the previous view/draft. The host can deliberately select each controlled human's own view; simultaneous human choices are **not claimed secret from this trusted tool**. Selected actor views still hide opponents' fresh orders/reserves.

Commands return actual typed backend/checkpoint errors when submitted. Menu/phase/argument failures return fixed safe messages. Raw exception/JSON/path diagnostics are never rendered. Advancing with an unsent draft is blocked. Command IDs are caller-supplied; stale/duplicate results are not retried. Disk/file safety and dirty-load behavior reuse the #25/#28 path.

## Concise scripted review sequence

[Demo](../prototypes/HobbyDS9.TacticalReviewShell/ScriptedReviewDemo.cs) labels every step SCRIPTED HUMAN CHOICE. It uses the exact same shell commands/controllers as user input:

1. View Federation's own OutsideEncounter facts; set authored cloak; query the known Bajor route; explicitly submit Follow.
2. Select Cardassian view; submit its own explicit Leave route toward the wormhole. The script is controlled human input, not inferred hidden routing or AI.
3. Select Federation; advance once. Faster source reports Bajor stop **3/4**, Distant encounter, TargetDependent with **no shot promised**.
4. Commit actor A's Hold order. Save **demo-waiting**, then open it through validated complete-root host replacement.
5. Switch to B, commit its human order. Later rounds deliberately change to Flank then Screen. Inspect actual cadence/blockers; report typed historical events.
6. Finalize once. Next-turn orders are Stay; submit fresh StayEngage from both views and advance to a new battle with torpedoes **3**, not refilled.
7. Finish the second three-round battle and save **demo-final**, with own torpedoes **0**. No same-turn rematch or extra calendar advance.

The slower script stops after its actual miss, shows own weighted progress **2/3** and last-seen contact semantics, then saves demo-final. It does not estimate the other fleet's current destination from contact history.

Representative lines:

    ACTOR VIEW: civ.federation | LOCAL TRUSTED HOTSEAT REVIEW
    Own availability OutsideEncounter | blockers None | round outside context
    Target dependent: no listed own-state blocker; no shot promised.
    Own availability OwnStateBlocked | blockers CadenceWait | round 2
    Encounter complete; no further round.
    Authoritative result: None
    Checkpoint result: InvalidCheckpoint

Exact surrounding lines depend on the selected real boundary. These are text-host examples, not rendered/native visual acceptance.

## Separate acceptance evidence

Local SDK 10.0.401, locked restore/Release **0 warnings/errors**:

| Suite | Result |
| --- | --- |
| New authoritative explanation/shell | **23 passed; 0 failed/skipped** |
| Preserved actual physical/root-process tests | **47 passed; 0 failed/skipped** |
| Preserved B1 durable failure/privacy tests | **47 passed; 0 failed/skipped** |
| Preserved real B1 presenter | **51 passed; 0 failed/skipped** |
| Preserved test-owned mocks | **115 passed; 0 failed/skipped** |
| Combined backend including exact #27 | **1,685 passed; 0 failed/skipped** |

[New tests](../tests/HobbyDS9.TacticalReviewShell.Tests/) use actual Application instances for power/ammo/cadence/damage blockers, outside/complete/unarmed, hidden-target TargetDependent and typed generic fallback. Shell tests cover actor/draft/inspection switching, corrupt/explicit slots, typed rejection without retry, CLI stdin/process execution and selected-view privacy. No own availability or fallback cause is manufactured in fixtures. Prior test sources/case counts remain unchanged.

The actual CLI runs faster/slower demos in separate processes and reproduces complete physical root hashes:

- Faster: **a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177**
- Slower: **77073cc154b78dbca2e5f6fa1a618cae490d11410f51f4cb2bb9e640a33210c4**

Preserved #28 process/root/stock/report/privacy gates and all old legacy/B1/physical vectors remain required. [Dedicated workflow](../.github/workflows/tactical-review-shell.yml) verifies separate 23/47/47/51/115 counts on all three platforms. Existing regression workflow verifies 1,685 and static/headless vectors unchanged. Exact delivered head/CI are in the draft PR/handover.

## Remaining scope

Owner policy/activation decisions, dynamic sensors, mixed-hostility owner admission, source provisioning, full map UI, multiplayer authorization, Unity/native/three-resolution acceptance and platform/storage limitations remain as previously documented. Specific historical fallback classification, target-ready/shot forecasts and new combat/AI rules are excluded.

This is a concretely runnable **trusted headless review candidate**, not production Unity/default activation. No main/source PR merge, native player, desktop control or other-chat message is part of delivery.
