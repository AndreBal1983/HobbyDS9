# Playtest feedback and delivery brief — 18 November 2026

Status: owner-requested work, delegated; not yet implemented or accepted. Source is the Product Desk playtest remarks and screenshots of build `af0273c`. Baseline: `codex/combined-20261115-next-qa`, exact `af0273c2ab89ff199e8e7dfb57d8a69dfbb896c8`. Preserve the reviewed CI fixes in this baseline.

## Delivery objective and controls

Deliver a new verified Windows playable candidate with visible improvements, truthful stats and working interactions. Keep Product Desk available; specialists work in isolated worktrees and publish exact handoffs to Integration & QA. Do not switch or stage the shared root checkout. Batch pushes; retain PR checks and the main-only push restriction. No broad main merge is authorized by this brief.

Shared account usage: check before sustained work and at handoff. Checkpoint by 72% consumed, hard stop at 75%, leaving 25% reserve. Do not use credits or reset credits to evade the restriction. Latest initial reading: 0% consumed in the current weekly window. Coordinate aggregate consumption across chats.

Read repository contributor instructions, [design index](design/README.md), [architecture](architecture.md), and relevant chapters before implementation. Intended and shipped behavior must remain distinct. In particular consult [ship design](design/ship-design.md) and [Engineering model](design/engineering-tree-model-01.md) for module work. Preserve deterministic saves, shared validation and portable simulation authority.

## Owner feedback and acceptance

| Area | Requested result | Primary owner |
| --- | --- | --- |
| System overview | Keep improved lighting and smooth orbit lines. Replace uniform spacing with varied, readable compressed spacing and minimum separation; maintain orbit ordering and truthful AU values. Real-scale spacing remains rejected. Restore smooth motion when selecting planets, respecting reduced-motion preference. | Unity & GUI |
| Asteroid belt | Sol must visibly show its asteroid belt. Investigate why the existing belt implementation is absent in this screenshot/build path; ensure authored system data, save/new-campaign behavior, rendering and art bindings agree. Do not label an unobserved belt accepted. No mining expansion. | Backend + GUI + Art |
| Production | Compact construction catalogue so substantially more choices fit on screen. Show useful comparison stats: Industry cost, exploration capability and combat capability where supported, plus essential material/queue information. Use concise icons and rows/cards. Unknown or unsupported ratings must not be invented. | GUI + Backend projections |
| Navigation | Remove duplicate Empire navigation/screen, keep Worlds and preserve access to any unique actions. Double-click planet names in Worlds to open details without the separate right-hand World details button. Mouse Back/Forward buttons navigate meaningful screen history without issuing orders or losing edits. | Unity & GUI |
| Engineering project popup | Replace prose with visual stats and concise labels. Show Target research (e.g. Reduce module hull space), Expected outcome (12 → 9), and current Hull space, Power, Output, Industry cost, Maturity. Capacity blocks for capacities, Industry numeric. Avoid internal IDs and repeated explanatory paragraphs. | GUI + Backend |
| Engineering outcome popup | Show Result: Success; improvement summary; Side effects: None or concise actual changes; new Hull space, Power, Output, Industry cost, Maturity. Use before/after blocks and numbers. Choices: Accept engineering outcome; Continue development to reduce downsides; Discard outcome. These must execute real, persisted behavior. | Backend + GUI |
| Fleet roster | Use a compact filterable list of ships, comparable to Worlds, with ship icons, supply and shield status icons, current mission, and clear fleet grouping/organization. Reuse existing approved designs; Art creates missing design references. Unknown supply/shield data must be shown honestly. Replace generic placeholder icons when matching approved assets exist. | GUI + Art + Backend projections |

## Engineering rules and implementation boundary

The owner explicitly requests the established targeted-refinement rules, not another hardcoded catalogue outcome. Every completed refinement improves its selected attribute; initial policy is 70% no detractor / 30% a downside in another supported attribute. Diminishing-return factor is 1 through maturity 100, `(150-M)/50` between 100 and 150, and 0 at 150; apply to improvement, maturity gain and downside magnitude, not the 70/30 probability. Fixed project workload initially. Preserve scientific generation gates, retained versions, branches and deterministic persisted outcomes. Numerical ranges remain tunable according to the design record.

Accept retains/releases the result as a usable version; Continue uses that result as the parent for another normal targeted project; Discard rejects the candidate without undoing spent work or changing earlier versions. These operational interpretations are the implementation proposal to reconcile against the agreed branching record. Never reroll by reloading or reopening the popup, auto-upgrade published ship designs, or erase a downside for free. Backend and Architecture must record any unresolved semantic conflict promptly; proceed with independent GUI work while resolving it. Do not fake maturity for old catalogue projects that lack it. Existing save compatibility requires explicit migration or a clearly named new-campaign edition, not silent replacement.

## Team sequence

1. Architecture & Design checks this brief against agreed rules and existing UI references; identify only real blocking ambiguities and deliver a concise contract to Backend/GUI.
2. Art & Audio locates the approved fleet/production designs, supplies missing compact LCARS references and existing ship/status icon bindings, and diagnoses belt visibility with GUI. Avoid redundant asset generation.
3. Backend implements the smallest complete supported targeted-refinement/outcome lifecycle and truthful UI projections, plus the belt data fix if needed. Hand off schemas, exact commits, compatibility and focused deterministic tests.
4. Unity & GUI owns all UI changes and integrates Backend/Art handoffs. Parallel internal work is allowed with nonoverlapping file ownership; do not have multiple writers edit the same UI files.
5. Integration & QA assembles exact delivered commits in a new isolated receiver, validates and builds Windows IL2CPP. It may first deliver a clearly scoped GUI candidate while Engineering mechanics continue, but must explicitly list omitted requests and never claim full completion. QA alone owns native desktop testing; coordinate with user mouse use.

## Build acceptance evidence

- Screenshots of Sol including varied spacing, visible belt and selection transition; compact Production and fleet roster; Worlds navigation; both Engineering popup states.
- Native tests: mouse history, double-click Worlds, filters, no accidental orders, allocation/end-turn regression, save/reload, and module accept/continue/discard with persistent outcomes.
- Applicable portable tests, CI and Unity checks; distinguish known baseline failures from new failures. Do not remove gates to achieve green.
- Exact source SHA, verified package launcher, sealed package receipt and new-vs-existing-save behavior. Exercise a campaign and at least one turn. Report the known window-X crash separately; test in-game Quit.
- Product Desk receives concise handoffs and a final implemented/pending/blocker list. A compiled build alone is not visual acceptance.
