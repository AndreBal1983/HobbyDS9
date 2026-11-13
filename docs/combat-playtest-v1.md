# Authored close-contact combat playtest

COMBAT-PLAYTEST-01, 13 November 2026. Explicit opt-in fixture based on delivered PB `788190d3dcc2c371d23a694e21b13e08454034ee`. This reuses the accepted [single-human Bajor military loop](singleplayer-demo-r7-acceptance.md) and its [existing contract](singleplayer-demo-contract.md). Its native acceptance is historical; the new fixture has portable receiving evidence only.

## Scenario and expected contact

The fixed seven-node military-loop map is deliberately small. The Federation starts at Sol and the Cardassian opponent at Cardassia, with their existing lawful finite contact knowledge, hostile relation, real fits, finite ammunition, directional shields, hull, costs, construction and R&D. All starting state/equipment/resource values are identical to `singleplayer-demo-v1`; the new scenario identity and two initial ordinary travel orders are the only semantic changes to the five-file package.

The human's authored Leave route is Sol → Bajor. The opponent's authored Leave route is Cardassia → Badlands → Bajor. Its existing public Bajor-defence AI uses shared travel validation and then holds/engages at the objective; it does not run away toward a remote generated home. No map node, ship or stock is teleported or granted during play. No global AI policy, combat or detection rule changes. Normal generated campaigns and far-separated homes are untouched.

Keeping these initial orders and pressing normal End turn reaches a genuine encounter within **three ordinary advances** in the shipped fixture and the two tested RNG variations. A player who cancels travel, changes posture or withdraws may change that outcome. This is a labelled authored combat-test fixture, not normal map balance, a guaranteed victory, or a generated250-system campaign.

## GUI entry contract

New source: `scenarios/combat-playtest-v1/{demo,package,physical,core,scenario}.json`. Prepare with `dotnet msbuild scripts/PrepareUnityCombatPlaytest.proj -t:Prepare`. The matching committed Unity assets are under `Assets/HobbyDS9/Review/Resources/CombatPlaytestV1/`. Existing preparation targets and default selection are unchanged.

`CombatPlaytestOptions` exposes `DisplayName`, `Description`, `ResourceRoot`, `InputNames()` and `CreatePackage(byte[][], ReviewNewtonsoftCodec)`. It loads the existing complete demo4 → integrated3 → physical2 → campaign path from exact raw bytes. It rejects the normal demo source instead of falling back. GUI should expose an explicit separate menu action with the supplied authored-fixture disclosure, not replace Cycle New Campaign or alter generated settings.

Read `TextAsset.bytes` for the five input names in the declared order and create the existing `ReviewUiModel(package, codec, store)`. Use `ReviewDemoSlotStore(Path.Combine(Application.persistentDataPath, CombatPlaytestOptions.SaveDirectory), codec)`. Start and Continue must always use this new package and this directory, not the normal `demoSource` field or normal demo store. The directory is `HobbyDS9CombatPlaytestOnly-v1`; a slot such as `combat-1` is suitable. Default activation stays false.

Use the existing campaign map, contact/travel, human combat planner, joint round, aftermath and save controls. No new resolver or UI-owned outcome is required. No portable Simulation/Contracts/Application code changes accompany this fixture. GUI owns the actual menu/control integration; Backend does not claim the new menu exists or native rendering ran.

## Play sequence and acceptance

1. Explicitly select **Close-contact combat test (authored fixture)** and start fresh. Inspect the human's initial Bajor route, lawful contact and exact own weapon stocks.
2. Keep the authored travel orders and advance normally three times. The real physical movement/contact path allocates an encounter at Bajor; the AI commitment exists before human selection. The human planner remains waiting, with no human round automatically committed.
3. Choose lawful fleet maneuvers/focus and commit one joint round at a time. The portable proof uses Charge and a lawful visible target. Other legal player choices remain available. Simultaneous opposing rounds use the actual weapons, finite magazines, directional shield stocks and hull.
4. Inspect reports, then explicitly finalize the complete aftermath. Surviving campaign hull/ammo/shield stocks must match final tactical state, rather than reset.
5. Save and cold Continue at pending planning, between rounds, at complete-but-unfinalized combat and after finalization. Compare complete wrapper4 roots and retain the same next decision; never save just a nested physical/campaign root.

Canonical portable run: encounter `encounter.spawn.0000000000000009` after3advances,3joint rounds,8ammunition units spent and298aggregate hull lost. Test-only RNG42/43variants also reach contact after3advances and spend8ammunition, with243hull lost. These are deterministic observations of fixed inputs, not a promised universal outcome or a player-visible disclosure of enemy ammunition. The test-only variants recompute source/package identity and never change shipped RNG.

## Persistence and verification

Existing format4 and all compiler/resolver/AI policy versions are retained. Changed raw scenario/physical bytes change the package and content identities. Normal demo and combat-test saves reject across profiles; separate directories preserve owner saves. No old save conversion or rebinding is supplied.

`prepare-combat-playtest-content.py` checks all five source/resource pairs against the preserved military-loop recipe; `--write` is only for regenerating these new task-owned files. It preserves the original three profile documents byte-for-byte, original state numerically, original hostilities/contacts/rules and two legal routes. The MSBuild target can prepare an alternate output directory for byte checks without importing Unity.

`CombatPlaytestTests` proves normal allocation, initial human planner wait, AI commitment, public joint rounds, finite ammo/directional shield/hull changes, explicit finalization and exact surviving stock transfer, repeated/interrupted replay parity, cold actual UI-model store roundtrip, and incompatible normal-demo rejection. Existing single-player UI and portable military-loop tests protect the reused stack. Execution counts, exact source and file/package hashes are in `artifacts/coordination/COMBAT-PLAYTEST-01/backend.md`. GUI/QA still own combined exact-source import, the opt-in menu, actual planner/native interaction, rendered evidence and the next player build. No main merge or release is authorized by this delivery.
