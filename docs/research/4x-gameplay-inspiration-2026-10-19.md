# 4X reference study and HobbyDS9 decision candidates

19 October 2026. **Research and proposals only. No gameplay decision, implementation, asset licence, or release acceptance is created by this document.**

The immediate recommendation is to improve the player's ability to understand and execute the existing loop before adding another large subsystem: retain map context, expose the next useful action, explain research-to-construction dependencies, and make battle consequences and subsequent strategic orders visible. The larger opportunity is an exploration-led campaign with meaningful destinations and alternative objectives. That direction already exists in HobbyDS9's design; the gap is chiefly implementation, pacing and presentation, rather than a missing statement of intent.

## Scope and evidence

“Past ten years” means games relevant during **19 October 2016–19 October 2026**, including enduring older releases. A separate release-window shortlist appears below. This avoids excluding Stellaris merely because its first release preceded the window by five months, or excluding Civilization V despite substantial current play.

There is no comparable public worldwide ten-year player/sales census for all these games. This is a **curated PC 4X reach ranking using lifetime Steam review volume**, not a universal sales ranking or a count of reviews written inside the decade. The population includes established empire-scale games explicitly presented as 4X and closely related space 4X/strategy hybrids. Publisher-described RTS titles such as Northgard are separated; this boundary matters to the top five. It is a research sample, not proof that every obscure or non-Steam title was considered.

Primary measurements were retrieved directly from Valve on 19 October 2026:

- [Steam review endpoint example](https://store.steampowered.com/appreviews/289070?json=1&language=all&purchase_type=all&num_per_page=0): `query_summary.total_reviews`, all languages and purchase types. Review **quantity**, not recommendation percentage, supplies the ordering. Multiple reviews, promotions, age and controversies bias this measure; owners and sales cannot be inferred from it.
- [Steam application details example](https://store.steampowered.com/api/appdetails?appids=289070&l=english): application identity and Steam-listed release date. These dates can differ from a worldwide advertised date, an earlier Epic release or an Early Access start.
- [Steam current-player endpoint example](https://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v1/?appid=281990): a momentary concurrent-player check, used to establish continuing relevance, **not** ten-year unique players or a second blended ranking.

Machine-readable receipts: [review/release snapshot](4x-steam-popularity-snapshot-2026-10-19.json) and [recent activity supplement](4x-current-activity-supplement-2026-10-19.json). The latter was sampled at approximately **18:08 UTC**. Counts will change. Store and measurement links remain inspectable; no opaque estimated owner counts were mixed in.

Footage method: a background YouTube browser played muted publisher videos or a developer demonstration; selected timestamps were paused and the rendered frames inspected. The observations below record the **actual displayed time**, which can be several seconds later than the requested seek. This is a sampled visual analysis, **not** five complete playthroughs, a hands-on game test, an audio review, an AI-quality test or a claim about every current patch. Ads, thumbnails, subtitles and descriptions were not counted as gameplay observations. Most source frames are older versions; the 2026 Civ VII tutorial is explicitly distinguished from its prerelease showcase. No third-party images or video were downloaded into the game or licensed for reuse.

## Ranked reference list

Steam-listed dates below are preserved as reported, rather than silently substituting another platform's date. “Legacy” means first Steam release falls outside the strict release window, not that it was irrelevant during the decade.

| Rank | Game / primary store source | Lifetime reviews | Steam-listed release | Reference use |
| --- | --- | ---: | --- | --- |
| 1 | [Civilization VI](https://store.steampowered.com/app/289070/) | 380,455 | 20 Oct 2016 | First-turn tasks, contextual production, research explanation |
| 2 | [Civilization V](https://store.steampowered.com/app/8930/) | 211,134 | 23 Sep 2010 — legacy | Map readability, compact selected-unit commands, diplomacy transition |
| 3 | [Stellaris](https://store.steampowered.com/app/281990/) | 197,133 | 9 May 2016 — legacy | Galaxy/system hierarchy, outliner, discovery decisions |
| 4 | [Civilization VII](https://store.steampowered.com/app/1295660/) | 62,773 | 10 Feb 2025 | Objective feedback, production/purchase distinction, setup hierarchy |
| 5 | [Humankind](https://store.steampowered.com/app/1124300/) | 28,906 | 17 Aug 2021 | Visible goals, selected-city context, battle geography |
| 6 | [Age of Wonders 4](https://store.steampowered.com/app/1669000/) | 23,292 | 2 May 2023 | Strategic/tactical transition and contextual battle action palette |
| 7 | [Endless Space 2](https://store.steampowered.com/app/392110/) | 23,144 | 18 May 2017 | Space-specific battle planning and optional cinematic presentation |
| 8 | [Endless Legend](https://store.steampowered.com/app/289130/) | 19,104 | 18 Sep 2014 — legacy | Asymmetric fantasy 4X reference; footage not inspected in this study |
| 9 | [Sins of a Solar Empire: Rebellion](https://store.steampowered.com/app/204880/) | 16,366 | 12 Jun 2012 — legacy | Real-time space 4X/RTS hybrid; not a turn-model template |
| 10 | [Dune: Spice Wars](https://store.steampowered.com/app/1605220/) | 13,622 | 14 Sep 2023 | Real-time 4X hybrid; political/economic pressure reference |
| 11 | [Age of Wonders III](https://store.steampowered.com/app/226840/) | 13,339 | 31 Mar 2014 — legacy | Older tactical 4X comparator |
| 12 | [Galactic Civilizations III](https://store.steampowered.com/app/226860/) | 10,406 | 14 May 2015 — legacy | Space empire and ship-design comparator |
| 13 | [Terra Invicta](https://store.steampowered.com/app/1176470/) | 9,993 | 5 Jan 2026 | Grand-strategy/space expansion hybrid; substantial complexity caution |
| 14 | [Sins of a Solar Empire II](https://store.steampowered.com/app/1575940/) | 8,852 | 15 Aug 2024 | Space 4X/RTS hybrid; Steam date, not original Epic launch |
| 15 | [Master of Orion](https://store.steampowered.com/app/298050/) | 8,208 | 25 Aug 2016 — legacy | Accessible space 4X comparator |
| 16 | [Old World](https://store.steampowered.com/app/597180/) | 6,989 | 18 May 2022 | Event/turn opportunity-cost comparator; Steam date, not Epic launch |
| 17 | [Age of Wonders: Planetfall](https://store.steampowered.com/app/718850/) | 6,674 | 6 Aug 2019 | Science-fiction strategic/tactical transition |
| 18 | [Endless Legend 2](https://store.steampowered.com/app/3407390/) | 4,297 | 17 Sep 2026 | Recent release has much shorter review accumulation time |
| 19 | [Millennia](https://store.steampowered.com/app/1268590/) | 3,792 | 26 Mar 2024 | Alternative historical-development reference |
| 20 | [Distant Worlds 2](https://store.steampowered.com/app/1531540/) | 3,584 | 10 Mar 2022 | Space automation/logistics comparator |
| 21 | [Galactic Civilizations IV](https://store.steampowered.com/app/1357210/) | 2,634 | 19 Oct 2023 | Space 4X comparator; Steam date, not original Epic launch |
| 22 | [Ara: History Untold](https://store.steampowered.com/app/2021880/) | 2,306 | 24 Sep 2024 | Production-chain and economic information reference |

[Northgard](https://store.steampowered.com/app/466560/) has **68,869** reviews, more than Civ VII, and is a relevant adjacent RTS/territory-management reference. Its publisher/store presents it as real-time strategy; treating every territory RTS as 4X would change the comparison population. It remains an explicit adjacent reference rather than disappearing from the data. Total War and Paradox historical grand-strategy games are also outside this focused core-4X sample; this is not a ranking of all strategy games.

At the activity check, **Civ VI 26,784; Civ V 15,176; Stellaris 11,447; Civ VII 9,178; Humankind 731** players were online on Steam. This supports legacy inclusion but does not establish lifetime retention, current quality, or which mechanics cause popularity. Three Civ games in the top five are a consequence of the disclosed proxy; they were not replaced to manufacture diversity.

If “past ten years” instead means **released inside the exact window**, the top five in this sample are **Civ VI, Civ VII, Humankind, Age of Wonders 4, Endless Space 2**. Both meanings are now available for the owner's choice. The Steam VI date is one day earlier than the commonly advertised 21 October launch; either falls within the window.

## Actual footage observations: top five

### 1. Civilization VI — teach a sequence of decisions

Publisher source: [How To Take Your First Turns](https://www.youtube.com/watch?v=UBh2YM-0xws), linked from [2K's new-player guide](https://store.2k.com/news/sid-meiers-civilization-vi-new-player-quick-guide) and its official tutorial playlist. Additional source: [How to Generate Science](https://www.youtube.com/watch?v=3NLoDMsqJkE).

| Viewed time | Observed frame / interaction context | Transferable lesson |
| --- | --- | --- |
| [0:25](https://www.youtube.com/watch?v=UBh2YM-0xws&t=25s) | Selected settler and partially revealed map; unit information/actions and prominent next-turn control occupy stable corners. | Start with one useful object and a next action; avoid making the player find an abstract department first. |
| [0:54](https://www.youtube.com/watch?v=UBh2YM-0xws&t=54s) | City remains visible while a contextual production list offers unit choices and turns. | Show what will be built, where and when beside the selected production context. |
| [1:17](https://www.youtube.com/watch?v=UBh2YM-0xws&t=77s) | The same city/map composition accompanies the transition to choosing the first technology. | Teach linked steps without discarding spatial context. |
| [Science 0:52](https://www.youtube.com/watch?v=3NLoDMsqJkE&t=52s) | A campus is visually isolated from the surrounding city to explain its science role. | Explain a source-to-output relationship; decoration alone does not explain research. |

For HobbyDS9, preserve six-discipline Science and earned Engineering. Borrow the explanatory chain **capacity → allocation → discovery → eligible Engineering → actually buildable class**, rather than importing a selectable Civ technology tree or its balance.

### 2. Civilization V — keep the strategic object readable

Source: [Firaxis developer gameplay demonstration, hosted by IGN](https://www.youtube.com/watch?v=vWrUCjct_bQ), 13:34 video, older prerelease material. The footage is a primary visual observation of that demonstration; it is not evidence of today's complete rules. [Official digital-manual access](https://support.civilization.com/hc/en-us/articles/37707647846803-Civilization-V-Digital-Manual) is a separate rules reference.

| Viewed time | Observed frame / interaction context | Transferable lesson |
| --- | --- | --- |
| [1:05](https://www.youtube.com/watch?v=vWrUCjct_bQ&t=65s) | Clear hex geography, distinct army markers, border lines and a compact minimap/Next Turn region. | Provide semantic map symbols and persistent orientation, not only an attractive surface. |
| [4:34](https://www.youtube.com/watch?v=vWrUCjct_bQ&t=274s) | Selected musketman's action stack and movement/strength panel remain separate from unexplored map space. | Keep selected-object commands near the object summary, and represent unknown space distinctly. |
| [5:25](https://www.youtube.com/watch?v=vWrUCjct_bQ&t=325s) | A dedicated leader scene presents a small labelled choice list: war, trade, demands, discussion and exit. | Large focused interactions can have a dedicated screen with a concise action set. |

Do not import one-unit-per-tile or a diplomacy system merely to match this screen. HobbyDS9's fleet is the command unit; future diplomatic actions need their own authoritative contracts.

### 3. Stellaris — galaxy context plus actionable detail

Publisher source: [Paradox Grand Strategy, Beginners #6: Open Play](https://www.youtube.com/watch?v=O4Vct1Qzf0M), 51:25. This is an older game build: its planet tile system is **not** a current Stellaris feature recommendation. The [current publisher overview](https://www.paradoxinteractive.com/games/stellaris/about) independently establishes exploration/procedural species as intended product pillars, not what every sampled frame proves.

| Viewed time | Observed frame / interaction context | Transferable lesson |
| --- | --- | --- |
| [2:06](https://www.youtube.com/watch?v=O4Vct1Qzf0M&t=126s) | Galaxy view with territorial colour, named systems and a persistent right outliner grouping owned objects. | Separate navigation/selection from the large central map, with consistent selected-object identity. |
| [8:03](https://www.youtube.com/watch?v=O4Vct1Qzf0M&t=483s) | Planet detail opens while the galaxy and outliner remain visible. | Detail need not destroy location context; close/back should return to the same selection. |
| [25:02](https://www.youtube.com/watch?v=O4Vct1Qzf0M&t=1502s) | A centred anomaly card includes a scientist/level context and leave, change-scientist, research options over the retained world. | A discovery becomes a decision through capability, commitment and an explicit way to defer. |
| [35:03](https://www.youtube.com/watch?v=O4Vct1Qzf0M&t=2103s) | Planet detail contains a blocker-clear operation and visible queued work while the empire map remains behind it. | Distinguish currently selected work, queued consequences and global context. |

HobbyDS9 already agrees on arrival/survey/study distinctions and equipment-dependent missions. Its current frontier backend only charts public navigation facts on completed arrivals. Do not put undiscovered planet data or proposed anomaly choices in a live UI simply because this reference displays them.

### 4. Civilization VII — explain cost, progress and setup

Publisher sources: [2026 Guide to Getting Started: Game Setup](https://www.youtube.com/watch?v=lA-Rqq73lj4), 7:21, and [2024 official gameplay showcase](https://www.youtube.com/watch?v=Tc3_EO6Bj2M), 25:21. The former was visibly dated four months before this research and describes the Test of Time update. The latter is prerelease, visibly marked work in progress, and largely removes UI for cinematic shots; it is **not** evidence that controls are discoverable.

| Viewed time | Observed frame / interaction context | Transferable lesson |
| --- | --- | --- |
| [2026 1:32](https://www.youtube.com/watch?v=lA-Rqq73lj4&t=92s) | A completed objective card overlays the map and identifies its reward. | Completion should explain what changed and why it matters, not just append an event code. |
| [2026 3:57](https://www.youtube.com/watch?v=lA-Rqq73lj4&t=237s) | Separate Production/Purchase tabs; building entries show effect icons and turn values; map remains alongside. | Do not conflate queueing, immediate spending, progress and completion. |
| [2026 4:55](https://www.youtube.com/watch?v=lA-Rqq73lj4&t=295s) | Map-type and size cards explain their choice; Advanced Settings is separate; Continue is prominent. | Offer a small comprehensible start configuration before detailed procedural parameters. |
| [2024 6:33 / 7:48 / 8:59](https://www.youtube.com/watch?v=Tc3_EO6Bj2M&t=393s) | Cinematic city, discovered waterfall/terrain edge, and units in varied terrain. | Use environmental identity and reveal transitions for atmosphere, while retaining a readable control mode. These frames did not prove commands or hidden simulation rules. |

An era-transition/reset system is not recommended by these observations. It would conflict with the owner's continuous DS9 campaign intent unless deliberately redesigned. Presentation clarity can be borrowed independently of those mechanics.

### 5. Humankind — goals and combat retain world meaning

Publisher source: [HUMANKIND Gameplay Overview](https://www.youtube.com/watch?v=Pw-yKOte5eA), 5:17, 2021. Its [publisher manual](https://cdn.cloudflare.steamstatic.com/steam/apps/1124300/manuals/HKD_STEAM_MAN_EN_V15.pdf) is supporting rules documentation, not a substitute for viewing.

| Viewed time | Observed frame / interaction context | Transferable lesson |
| --- | --- | --- |
| [0:48 / 1:08](https://www.youtube.com/watch?v=Pw-yKOte5eA&t=48s) | Appearance options are separated from AI-persona strengths and biases. | Separate cosmetic identity from behavioural/gameplay settings. |
| [1:45](https://www.youtube.com/watch?v=Pw-yKOte5eA&t=105s) | A grid of named achievements/objectives shows explicit Fame rewards. | Provide legible intermediate goals so progress is not only “wait until the end.” |
| [2:43](https://www.youtube.com/watch?v=Pw-yKOte5eA&t=163s) | Selected city, borders and surrounding resource geography remain visible beside construction choices. | Tie economic choices to the selected place and its actual inputs. |
| [3:39 / 3:54](https://www.youtube.com/watch?v=Pw-yKOte5eA&t=234s) | Army context changes into an encounter on the same geographic area, with unit positions and a delimited battle space. | Explain where battle came from and return survivors to that meaningful strategic context. |

Do not import terrain-hex micromanagement into HobbyDS9's abstract formation combat. Retain the contextual transition and outcome explanation; keep orders at fleet level.

## Supplementary references for the strict release-window interpretation

- **Age of Wonders 4:** [official tactical demonstration, 3:01](https://www.youtube.com/watch?v=cUuzGAh5L2M&t=181s) was actually viewed. A selected unit has a portrait, action palette and highlighted tactical hexes. Useful principle: selection, eligible targets and next action are visually connected. Its individual-unit hex orders are not appropriate defaults for the owner's simple fleet commands. This is an additional sample, not a full AoW4 analysis.
- **Endless Space 2:** additional actual game capture, [PenguinzPlay's Fleet Composition and Space Battles](https://www.youtube.com/watch?v=0ZqRv6gIj7o), is a community recording, not a developer explanation. At [7:13](https://www.youtube.com/watch?v=0ZqRv6gIj7o&t=433s), the tactics interface shows three schematic approach lanes and selectable tactic cards; historical opponent options are explicitly labelled as last-used options. At [8:50](https://www.youtube.com/watch?v=0ZqRv6gIj7o&t=530s), a close 3D battle view shows ship identity/condition rings, speed indicators and Quit Battle. The second frame was paused at video time530.839 seconds, verified from the page's video element after visual inspection. These observations show the transition from compact planning to spectacle with an information overlay; they do not establish its resolver's exact maths. The [official manual](https://steamcdn-a.akamaihd.net/steam/apps/392110/manuals/User%27s_Manual_-_Endless_Space_2.pdf) separately offers watching a battle in 3D or receiving its summary; the developer's [battle-design discussion](https://community.amplitude-studios.com/amplitude-studios/endless-space-2/forums/66-game-design/threads/20675-community-gdd-4-update-battle-overview) discusses equivalent auto/manual results. Borrow the optional presentation pattern, not its lane formulas, opponent knowledge or card bonuses.

## HobbyDS9 baseline used for the comparison

Sources were read separately because design intent and runtime evidence are different:

- Owner-working-tree [design index](../design/README.md), [strategic AI](../design/strategic-ai.md), [ship design](../design/ship-design.md), [exploration](../design/exploration-missions.md), [time/movement](../design/time-movement-mobilisation.md), [tactical combat](../design/tactical-combat.md), [architecture](../architecture.md), [product-manager memory](../product-manager-memory.md) and latest coordinator checkpoint at `E:/HobbyDS9/docs/autonomous-demo-checkpoint.md`. Historical paragraphs in memory/architecture were not treated as latest implementation facts.
- Current combined candidate checkout `C:/Users/ambal/.codex/worktrees/demo-keyboard-controls/HobbyDS9`, inspected at `ba152554a2307cf389a621398066ae1a1d608baf`: [demo contract](../singleplayer-demo-contract.md), [backend acceptance](../singleplayer-demo-backend-acceptance.md), [frontend acceptance](C:/Users/ambal/.codex/worktrees/demo-keyboard-controls/HobbyDS9/docs/singleplayer-demo-ui-acceptance.md), [Engineering/map context](C:/Users/ambal/.codex/worktrees/demo-keyboard-controls/HobbyDS9/docs/singleplayer-demo-engineering-map-context.md), [frontier contract](../frontier-reconnaissance-v1.md). The checkout can advance after this research; pin matters. Absolute links identify candidate-only local documents not yet present on this research branch's main base.
- Coordinator's actual R1–R3 native receipts distinguish an earned Engineering project, paid cruisers, human-only AI battles, finite stocks, atomic aftermath, Victory and real restart continuation from current R4 build/receiving work. This researcher did not rerun those tests or operate Unity/native controls. Existing acceptance documents contain earlier pending gates; later checkpoint evidence supersedes them only for the stated source/package.

At this checkpoint: the **opt-in twelve-turn military demo** implements a coherent limited loop, not the full intended 4X. It uses an authored small graph, one actual construction catalogue, Science/one-target Engineering, lawful bounded defensive AI, human fleet orders, simultaneous tactical rounds, finite stocks, explicit aftermath and terminal outcome. Live v2 publication from the modular v1 designer remains unsupported. Public-arrival charting exists in a **separate gated frontier profile**; it is not planetary surveying, generated galaxy play or activated in the demo. Full diplomacy, colony expansion, mission director, generalized long-term AI and complete procedural galaxy generation are not established by these contracts.

## Decision-ready gap matrix

Every row below is a **proposal for discussion**. “Already agreed” means the direction is in design; it does not approve the suggested implementation, formulas or balance. Presentation-only options still require lawful projection support and actual usability acceptance.

| Priority / observed inspiration | Player problem / benefit | HobbyDS9 status and actual gap | Bounded option to consider | Trade-off / dependencies / decision |
| --- | --- | --- | --- | --- |
| P0 — VI first turns; VII objective card | Player cannot tell what to do next or how departments connect. | Paid construction, research, movement, battle and outcome exist; discoverability is the current acceptance risk. | A dismissible four-step briefing with links to actual relevant panels; update completed steps from authority, never auto-submit orders. | Less freedom in teaching order; needs typed progress facts. Decide whether the first demo uses guided or completely free opening. |
| P0 — VI production beside city; Humankind selected place | Player loses track of which world/fleet a screen controls. | Map/Worlds/Production/Fleets exist; recent qualifier and map-scope fixes are being received. | Consistent selected-entity breadcrumb, visible location and “Back to map” restoring camera/selection; one contextual primary action. | Requires stable selection identity and generation guards, not new gameplay. Choose shared navigation grammar before reskinning each panel. |
| P0 — VI research explanation; VII cost hierarchy | Science progress looks disconnected from something useful to build. | Six disciplines, earned prerequisites and one Engineering target implemented; demo admits Cruiser only. Full modular portfolio is proposed. | Show “Current allocation”, “Discovered”, “Engineering eligible”, “Buildable here” as distinct states; show actual missing requirements and local yard costs. | More explanations can clutter 720p; keep detail progressive. No false Advanced Scout unlock or invented ETA. Choose terminology and retained detail depth. |
| P0 — VII Production/Purchase | Paying, queueing and completion are mistaken for the same event. | Real FIFO paid production exists. Buyout/expediting is agreed direction but detailed rules/open implementation. | Present queue cost, paid status, local Industry progress and actual completed-ship destination. Keep future Buyout absent or clearly outside current feature set. | Copy/layout first; do not implement Civ instant purchase without a separate economic decision. |
| P0 — Humankind battle geography | Combat feels detached from the strategic reason for fighting. | Central fleet-round combat, explicit aftermath and fresh strategic decisions implemented; 3D presentation not yet a full accepted resolver experience. | Battle entry names sector, participating own fleets and lawful visible opponents; end shows losses, remaining hull/shields/ammunition, location and next legal strategic action. | Enemy stocks/orders stay private; no unrelated allied screening. Decide the minimal summary versus expandable round history. |
| P0 — AoW4 selection/targets; ES2 documented watch/summary | Spectacle can hide how commands affected outcomes or delay repeated battles. | Agreed abstract authoritative formation resolution; Unity visualizes results. | Schematic/tactical overview plus optional 3D playback of already-resolved events; pause, speed, skip and return to orders between rounds. Clearly mark Draft → Commit → Resolved. | Playback must not run RNG, grant shots, reveal cloaked units or recalculate hits. Requires filtered event mapping and asset readiness. Decide preferred default camera and playback length. |
| P1 — Stellaris galaxy/outliner | Attractive 3D galaxy can make routes and objects harder to find. | Authored navigation and lawful route previews exist. Irregular regions/visible travel are agreed direction; exact generation rules are not implemented here. | Mock a 3D galaxy with a planar tactical overlay: names, lane legend, known/unknown distinction, route costs/ETA, selected fleet and home/fit controls; support flat overview. | Camera tilt cannot imply vertical route shortcuts or invented distance costs. Need authoritative seeded generator, connectivity/start fairness and knowledge projection before live generation. Choose graph/region rules separately from appearance. |
| P1 — VII map setup cards | Procedural settings overwhelm the player or produce incomparable runs. | No accepted generalized generator established in the demo. | Setup mockup with seed, size, shape, density and opponents; basic preset first, advanced panel second; identify proposed controls. | Every parameter needs rules/version/save binding and deterministic tests. Choosing a mockup is not approval of its geography formulas. |
| P1 — Stellaris anomaly decision | Revealing dots supplies little reason to explore. | Arrival/survey/study and mission consequences already agreed; frontier only publishes navigation facts. | After frontier UI acceptance, scope one equipment-checked finite survey site with a real duration, defer action and persistent result. | Needs body/site identities, lawful observations, shared consequence system and replay-safe rewards. Decide which first discovery tests the intended frontier feeling. |
| P1 — Humankind intermediate goals | A single military deadline offers limited replayability. | Demo's Bajor surviving-hull criterion is provisional; broader science/diplomacy/exploration victories already intended. | First add one optional scenario objective using existing facts, or a compact objective checklist; later compare genuinely different victory paths. | Changing win evaluation needs versioned policy and terminal/save tests. Do not turn every task into a reward farm or claim a complete alternative victory already exists. |
| P1 — VI/VII city context; strategic-map resources | Expansion lacks meaningful destinations and economic choice. | Colony economy exists; generalized colonization/ownership transfer not proven in this demo. Multiple inhabited groups/body identities are intended. | Design a small colony-expansion slice with survey, valid colony fit, real founding cost and upkeep before adding many randomly generated worlds. | Large simulation/content/AI/persistence dependency; no instant clicking an unknown planet to own it. Decide peaceful founding versus contact/integration as the first expansion slice. |
| P1 — Civ V dedicated diplomacy; Stellaris discovery | Every contact being a battle limits DS9-style play. | Full diplomacy/first-contact direction already recorded; bounded demo is explicitly military. | A lawful first-contact inspection and one reversible cooperation proposal in a separate slice, with response and refusal feedback. | Requires governments, relationships, ownership/visibility and fair AI validation. No universal bribe button or full treaty web implied. Decide whether first-contact depth precedes colony expansion. |
| P1 — Stellaris event choice / VI explanatory spotlight | More fleet roles could matter outside combat. | Hull capacity/mount/power/mission specialization is agreed; full modular v2 publication/refit remains gated. | Explain existing Cruiser fit honestly now; later demonstrate one scientific variant with a specific survey capability and actual sacrificed combat capacity. | Requires compiler/provenance/construction contracts, not UI role-name buffs. Decide one useful role trade-off before a large component catalogue. |
| P2 — Humankind separate persona controls | Cosmetic faction identity can be confused with cheating AI difficulty. | Abilities/personality/difficulty separation and five fair planning levels already agreed; demo uses bounded defence/procurement AI. | Mock separate setup fields and an explanation of supported demo AI; later introduce one persistent goal/milestone trace for developer review. | Never show private enemy intent to human players. Fairness is a rules invariant; tuning/search budgets are proposed, not proven strength. |
| P2 — recurring map/detail patterns | A growing empire becomes repetitive administration. | Standing orders/queues/alerts agreed direction; actual supported queues and fleet orders already exist. | Sort owned fleet/world lists by “needs attention”; completion/threat notifications link to lawful existing context; keep a compact turn summary. | No automatic order submissions or hidden discoveries; forecasting needs truthful data. Decide what blocks End Turn versus only warns. |
| P2 — Civ V symbols; VII cinematic environment | One visual treatment cannot serve both spectacle and dense decisions. | Owner accepts initial battle style; ArtDirector owns coherent all-screen mockups. | Shared type/colour/spacing/action rules; restrained semantic icons and decorative 3D backgrounds, with readable text retained above them. | Must test 1280/1920/3440, contrast, long names and keyboard focus. Avoid copying branded layouts/assets. Choose the information hierarchy before extra rendered ornament. |

## ArtDirector briefing: coherent screen and interaction coverage

This is input to the ArtDirector's separate assignment, not a competing new art system. Suggested mockup inventory should cover **states and transitions**, not just one attractive screen per tab:

1. **Launch/Continue:** objective promise, selected save identity, fresh start versus retained campaign; missing/incompatible save feedback.
2. **New procedural campaign:** basic preset/advanced setup, seed and generation preview; all unimplemented controls marked proposed.
3. **Galaxy overview:** lawful known map, unexplored space, map legend, owned-object overview, objective and needs-attention summary.
4. **Selected sector/system:** breadcrumb, known facts, own/visible objects, route destination context; unknown facts stay undisclosed.
5. **Fleet travel:** owned fleet composition/condition, route preview, costs/ETA/partial travel, review/confirm, invalid or stale route.
6. **Worlds/economy:** all-owned versus selected-world scope, stocks/income/local Industry and queued commitments.
7. **Science and Engineering:** allocation, discovered prerequisites, eligible/current/mature projects, actual construction payoff and unavailable states.
8. **Production:** selected yard, legal class, real prices/requirements, FIFO queue, completion and resulting fleet; proposed buyout shown separately if requested.
9. **Ship design:** current local v1 preview limitation versus proposed campaign-connected v2 workflow; capacity/power/compatibility and saved class identity.
10. **Battle entry and orders:** sector origin, distinct fleets, visible contacts, cloak-safe absence, round/maneuver/fleet focus, own stocks, draft/commit confirmation.
11. **3D round playback:** overview and close camera, selected ship detail, lawful event captions, pause/speed/skip, reduced-effects alternative; no physics-authority implication.
12. **Round result / next order:** what changed, losses/depletion, next round decision without inventing future enemy orders.
13. **Aftermath / strategic continuation:** atomic-finalization boundary, survivor summary, explicit fresh stay/re-engage or legal movement; no automatic intra-route pursuit resolver.
14. **Objective and terminal outcome:** progress, Victory/Defeat/Stalemate, read-only inspection, save and new campaign; no active-looking commands that reject terminal state.
15. **Discovery/survey/contact and diplomacy:** separately labelled future-feature mockups with capability, duration, cost, defer/refusal and consequences; not implied live functions.
16. **Save/Load, Audio, Display, pause and help:** consistent modal/focus behaviour, committed-versus-draft distinction, failure feedback, music/SFX states, resolution/accessibility controls.

For each, request an annotated **1280×720 layout first**, a 1920×1080 composition and a 3440×1440 adaptation. Include no selection, selection, long-name, empty/disabled, pending-confirmation, failed/stale-command, terminal and keyboard-focus examples where applicable. Use the same selected-entity labels, ordinary/wormhole legend, primary/secondary/danger action hierarchy and back/close grammar throughout.

Map storyboard: overview → select known system → choose owned fleet → preview legal route → review/confirm → intermediate travel → lawful contact → battle entry → orders → 3D resolved round → aftermath → returned map with survivors and depleted stocks. A second storyboard should show peaceful arrival → newly charted lanes, followed by a **proposed** survey decision. Do not use an omniscient map reveal as a shortcut.

## Recommended decision order and guardrails

1. Finish acceptance of the existing demo and coherent UI. Select a shared navigation/selection grammar and next-action presentation.
2. Decide the 3D playback contract and map interaction grammar using reversible mockups. Keep backend authority and privacy fixed.
3. Choose **one** next strategic slice: frontier/survey, colony expansion or first contact. Exploration/survey most directly addresses the agreed frontier identity, but this is a recommendation for the owner, not approval.
4. Only then specify generator parameters, costs, timing, AI use and save/version consequences. Test a normal action, constrained-resource case, exploit and save/interruption before implementation.

Reject feature parity as a goal. Do not copy Civilization's era resets, individual hex orders, Stellaris's obsolete planet tiles, arbitrary fleet command caps, hidden-information AI, free post-battle resource restoration or a real-time physics combat model. The useful references are the player decisions and how clearly the interface communicates them.

Documentation validation: review the numeric snapshots, primary store identities, timestamp URLs and local relative links; `git diff --check`. No gameplay tests are required because this change adds research only. Existing demo acceptance continues separately.
