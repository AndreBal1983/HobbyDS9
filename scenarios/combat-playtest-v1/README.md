# Close-contact combat test

Explicit authored fixture, not a balanced normal generated campaign. It reuses the accepted singleplayer Bajor military-loop package with a separate identity and ordinary initial travel orders to Bajor. Keep those orders and use End turn three times to reach real combat. Human planner, one joint round at a time, explicit aftermath and complete-save APIs remain unchanged.

See [entry, rules, steps and evidence](../../docs/combat-playtest-v1.md). Prepare only this profile with `dotnet msbuild scripts/PrepareUnityCombatPlaytest.proj -t:Prepare`. Runtime resource root: `CombatPlaytestV1/`. Separate saves: `HobbyDS9CombatPlaytestOnly-v1`. Existing campaign saves do not migrate. GUI must supply an explicitly labelled opt-in entry; Backend has not launched Unity or built a player.
