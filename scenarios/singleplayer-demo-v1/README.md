# Bounded single-player Bajor defence demo

Provisional opt-in content, not global balance. One human Federation and lawful Cardassian AI. See [contract](../../docs/singleplayer-demo-contract.md) for APIs/save4 and the explicit deadline objective. Existing integrated3/physical2/v1 fixtures are preserved separately.

The five exact raw inputs are `demo.json`, `package.json`, `physical.json`, `core.json`, `scenario.json`. First assemble the integrated3 package from its four inputs; then bind demo4 metadata/hash. Do not normalize bytes after hashing or save only a nested root. Human factories/views reject the AI actor.

Compared with integrated-playtest-v1: dedicated scenario identity; only Federation human, other civilizations AI (Dominion inert); Federation/Cardassian 300 initial credits; shared source-cruiser price120/Industry16; shared institute Research50 and fast existing ResearchPaceFactor5000. Fits/weapons/hull/movement/shield stocks/ammunition and finite20-turn contact freshness unchanged. Both sides pay shared prices/physical stocks.

Run `dotnet run --project src/HobbyDS9.Headless -c Release -- --singleplayer-demo` for actual authority: two paid human builds, changed R&D, earned Engineering, real construction/route/AI/joint rounds/explicit aftermath, retained state and deadline objective; repeated/reversed/reload checks. UI/native acceptance is not performed by this command. Seed/order fixture variants in tests are headless adversarial evidence, not trusted replacement campaign saves.
