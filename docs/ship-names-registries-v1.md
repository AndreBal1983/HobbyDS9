# Ship names and registries v1

Implemented 12 November 2026 as a presentation catalogue on the current playable Fleet UI. This does not change construction rules, hull/class names, technical entity IDs, authoritative state, save/content versions, RNG or allocation order.

## Research and catalogue

The [catalogue](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/ShipNames/starfleet-v1.json) contains twelve researched Starfleet names, reference registries where verified, and per-entry sources: Enterprise, Voyager, Defiant, Discovery, Cerritos, Protostar, Stargazer, Reliant, Excelsior, Bonchune, John Glenn and Thunderchild. An unverified reference registry is left empty rather than guessed. These are names for game vessels, not a claim that the game reproduces those historical ships, eras or classes.

The official [ship introduction](https://www.startrek.com/get-to-know) demonstrates `U.S.S. Name`, `NCC-` followed by a number, `NX-` examples, and `NCC-1701-D`. The [registry discussion](https://www.startrek.com/news/1424) explicitly says NCC has no official expanded abbreviation. [Stargazer](https://www.startrek.com/news/celebrating-the-ships-of-the-line-uss-stargazer-ncc-2893), [Reliant](https://www.startrek.com/news/celebrating-the-ships-of-the-line-uss-reliant-ncc-1864), [Excelsior](https://www.startrek.com/news/new-official-star-trek-starships-collection-ships-unveiled) and the [ships index](https://www.startrek.com/topic/ships) provide further names and registries. [Thunderchild](https://www.startrek.com/news/attack-wing-wave-17-federation-u-s-s-thunderchild) supplies its name.

Inference: names are proper vessel names, distinct from ship class and fleet designation. The sampled numbers do not establish a universal class/year/production formula. NX examples include experimental/prototype vessels, but that does not justify assigning NX merely because a player publishes a new blueprint. Lettered legacy registries exist; they do not establish a general automatic replacement policy.

## Implemented game convention

- Federation identities are selected from the frozen catalogue using the immutable global ship allocation sequence already assigned by construction. The authored `ship.equipment-cruiser` occupies slot zero. No new random draw, counter, command or save field is introduced.
- Ordinary game registries are `NCC-` followed by `100000 + allocation sequence`, using invariant decimal text and overflow-safe arithmetic. They deliberately differ from the historical reference registries. This is a game convention, not a canonical numbering claim.
- Names cycle through the twelve entries; subsequent cycles receive a numeric name suffix, e.g. `U.S.S. Voyager 2`. This keeps names and registries distinct across all generated ships without relying on the current roster or reusing a destroyed vessel's identity. Numeric name suffixes are also a game convention, not claimed canonical lettered registry succession.
- Identity is independent of class/fit, current fleet, roster order, losses, current turn and viewing history. Reorganization and complete save/load retain the label because the technical ship ID is unchanged. Older saves gain the same deterministic labels without migration or byte changes.
- Fleet roster cards show vessel name and registry separately from class/fit. Map and roster fleet labels preserve player-chosen names. An unnamed one-ship fleet displays its vessel identity; an unnamed larger fleet displays its first ordinal member's name followed by `group`. That automatic group label may change when its leading member leaves; the individual vessel identity does not.
- Only actual actor-owned observations feed the UI. Enemy labels retain the existing visibility rules. Federation names are never assigned to other factions. Other factions and unknown authored ships retain neutral vessel/task-group descriptions pending separately sourced faction catalogues.
- Technical IDs remain unchanged for commands, validation and saves. Fleet rename, creation and transfer continue through the existing authoritative commands. This is a display identity catalogue, not a new authoritative commissioning/rename mechanic.

The v1 catalogue's ordering and length are frozen. Do not append, reorder or replace entries in place: doing so would rename ships in existing saves. A future expanded catalogue needs a deliberate identity-version policy. `ReferenceRegistry` is research metadata and never substituted for a game's unique registry.

## Validation

New portable tests cover exact selection, ten thousand unique names/registries, reverse iteration, non-Western culture, the full allocation range, unsupported identities, real construction, reorganization, player fleet names and complete save/load with byte-identical exports. The actual Unity runtime assembly is compiler-checked against the installed Unity 6000.6.5f1 API; that is not Editor, IL2CPP or native-player acceptance.

The inherited portable compile boundary is narrowly corrected by excluding the Unity-only Sol views from that adapter project. They remain included in the actual Unity runtime compiler check.

Broader acceptance is not claimed: the existing review-binding static checker also fails on the unchanged base `999246f` with a retired keyboard-origin assertion. The full adapter run reports older colonisation resource-pin failures: the composition root pins CRLF bytes while its test fixtures use the LF scenario copies. Expected hashes, fixtures and runtime acceptance behavior are left unchanged; this task does not repair that independent historical content contract.

Executed results: 29/29 focused name and playable-fleet binding cases passed, including all seven new naming cases. The complete portable adapter suite executed 725 cases: 605 passed; all 120 failures have the same `Different provisional colonisation content` error before name selection. PrepareUnity and default Unity static integration passed. The complete Unity review runtime compiled against the installed Editor's APIs with zero warnings/errors. No Unity process, new Windows player, desktop input, push or merge was performed for this task.
