# Strategic AI

Status: **Agreed direction**; detailed tuning and implementation sequence **proposed**. Implementation has not been changed by this documentation update.

Decision date: 12 October 2026.

Design decision, 12 October 2026: computer-controlled civilizations must form persistent long-term strategies and coordinate their economy, Science, Engineering, diplomacy, exploration and fleets to execute them. Five difficulty levels increase planning quality without resource bonuses, privileged information or exceptions to player rules. This specifies intended design, not a claim that the current implementation already supports it.

## Faction abilities, personality and difficulty

Keep three independent data profiles. Civilization abilities describe actual bonuses and constraints. Personality describes preferred goals and behaviour. Difficulty describes the planner's search and evaluation budget. Canonical factions provide editable presets; custom factions can change abilities and behaviour independently under the normal game-setup restrictions. Strategy follows the configured profile and situation, never a hard-coded faction name.

Personality includes victory preferences, diplomacy and integration, scientific ambition, applied Engineering and refinement, expansion, aggression, risk tolerance, treaty reliability, occupation policy, espionage, preferred fleet roles and combat doctrine. Preferences bias choices but do not force an infeasible strategy. A diplomacy-oriented Federation can pursue Science or defensive war when circumstances justify it.

## Planning architecture

1. Knowledge model: expose only the civilization's lawful observations, known routes, own resources, diplomatic information and remembered intelligence. Store last-seen dates and uncertainty; distinguish observations from estimates. Opponent intent is a hypothesis. Unknown planets, orders, fleets, discoveries and random outcomes remain hidden.

2. Strategic planner: compare enabled victory paths using faction advantages, personality, current progress, reachable opportunities, costs, threats and estimated rival progress. Select a primary route and supporting objectives. Evaluate future investment value rather than only this turn's yield.

3. Operational planner: turn the strategy into persistent milestones with prerequisites, success criteria, targets, assigned fleets or colonies, resource reservations, estimated completion and fallback options. Plans cover research prerequisites, production, physical logistics, treaties and movement together.

4. Turn executor: generate legal candidate actions, score their contribution to milestones and survival, then select a compatible portfolio. Reserve credits, local Industry, required named resources, fleet time and R&D allocation once; do not double-spend shared capacity. Execute through the same command validation as human players.

5. Outcome review: after resolution, compare actual results with expectations, update knowledge and milestone progress, and revise the remaining plan. Persist strategy, reservations, commitments, knowledge and deterministic planner state in save games.

## Evaluation and commitment

Evaluate plans through victory progress, sustainable economic capacity, Science and Engineering readiness, diplomatic durability, strategic access, survival and opportunity cost. Express benefits over multiple turns; a prerequisite may be valuable because it unlocks a later milestone. Maintain minimum solvency, supply and credible defence constraints before discretionary spending. Avoid rewarding the same benefit twice.

Use bounded search over meaningful plan alternatives and approximate forecasts, not exhaustive enumeration of every possible command sequence. Longer forecasts use coarse milestones and conservative uncertainty. Simulate plausible opponent responses and discovery outcomes from lawful knowledge; never inspect the real hidden state or future campaign RNG. More computation helps only when forecasts and evaluation are sound.

Keep the current strategy unless a replacement exceeds a configurable switching margin after accounting for remaining costs, disruption and committed obligations. Reassess urgently after invasion, loss of an essential route or partner, a major discovery, or evidence of an imminent rival victory. A temporary defence operation need not replace the primary victory goal. Past spending alone must not justify continuing a failed plan.

## Five difficulty levels without cheating

Provisional tuning profile: Very Easy compares 1–2 plan variants with a 2-turn detailed forecast; Easy compares up to 3 with 4 turns; Normal up to 6 with 8 turns; Hard up to 12 with 12 turns; Expert up to 24 with 20 turns. These are initial caps for playtesting, not measured strength or performance guarantees. All levels retain a long-term victory goal and milestones beyond the detailed forecast.

Very Easy uses simple estimates and a single expected response. Easy checks obvious obstacles. Normal coordinates departments and checks one fallback. Hard compares several opponent responses and alternative resource allocations. Expert evaluates multiple contingencies, uncertainty and rival victory prevention. All levels react to visible emergencies; easier levels remain coherent rather than deliberately wasting turns.

Difficulty changes search breadth, forecast detail, response modelling and optimisation effort. It does not change income, costs, production, combat outcomes, diplomatic acceptance, fog of war or RNG access. Use fixed deterministic work-unit budgets, stable candidate ordering and state-owned seeds for repeatable choices. A wall-clock timeout may protect responsiveness, but must not make campaign decisions depend on machine speed.

## Worked plan: Federation diplomatic expansion

Primary goal: Diplomatic Hegemony where enabled and feasible. Membership is a means of increasing capacity and political unity, not the only requirement for this victory. Preserve the legacy gameplay design document's Chapter 7 provisional conditions: after the Minimum Victory Turn, Opinion 70+ and Trust 60+ with at least 70% of extant civilizations, formal alignment with at least 60%, inclusion of at least half the remaining major powers, and five consecutive qualifying turns. Partners at war with one another cannot both count unless their conflict is resolved. Use the authoritative victory evaluator for exact counting.

Milestone A — establish reach. Survey lawful routes and make first contact with reachable minors and majors. Select scouting routes by expected contact value, cost and risk. Build exploration capability only where local Industry and supply support it. Success means a useful contact network and viable routes, not knowledge of the hidden map.

Milestone B — select a sustainable partner portfolio. Rank known candidates by expected relationship progress, strategic access, population and institutions, cost, competition, independence preference and defensive obligations. Reserve capacity for major-power relationships as well as minors. Treat membership acceptance as uncertain even when formal thresholds are met.

Milestone C — build cooperation and trust. Negotiate mutually useful trade and scientific cooperation; fulfil promises and maintain influence. Offer aid, missions or protection only when supported by existing content and affordable obligations. Do not assume repeated gifts guarantee acceptance. Independence-oriented minors may remain valuable permanent affiliates.

Milestone D — align development. Allocate the single R&D Capacity between Science and Engineering according to the plan. Fund scientific disciplines that support actual known opportunities; develop relevant exploration, infrastructure, logistics and defensive applications. Forecast undiscovered breakthroughs as uncertain opportunities, not guaranteed selectable technologies. Respect local Industry and physical transport of resources such as Dilithium.

Milestone E — affiliation and membership. Use the existing relationship ladder: Independent → Cooperation → Affiliation → Membership. Affiliation normally needs Opinion 50+, Trust 40+ and sufficient influence. Membership normally needs Opinion 70+, Trust 60+ and strongest influence. Submit a legal proposal only when conditions are met and acceptance is plausible; a refusal updates the plan. Preserve the member's culture and institutions under existing rules.

Milestone F — sustain the coalition. Integrate member economies, defend exposed supply links, maintain relationships as influence decays, and secure enough major-power alignment. Avoid promises whose military or financial cost cannot be met. Monitor the legal victory checklist and maintain the qualifying network for the required five turns.

## Illustrative turn decisions and contingencies

Example scenario, not a prescribed campaign timeline: three known minors exist. A has Opinion 55, Trust 45 and a valuable science institution; B is nearer but strongly independence-oriented; C is valuable but lies beyond a threatened supply route. A is the leading affiliation candidate, B is a cooperation partner, and C remains a deferred objective. A major-power relationship receives its own milestone so minor recruitment cannot crowd out the victory requirement.

First planning turn: offer A an affordable cooperation or affiliation package if all real conditions are satisfied, scout toward an unknown contact region, preserve the defensive fleet near the threatened route, and fund the known development opportunities supporting the plan. Log why a new warship or unrelated weapon refinement lost priority.

Following turns: apply actual treaty responses, exploration and research results. If A accepts affiliation, maintain trust and influence before considering membership; if A refuses, improve the relationship or retain affiliation without proposal spam. If a useful breakthrough appears, schedule its Engineering application rather than assuming it was available earlier.

If an invasion threatens the core, suspend lower-value diplomatic spending, mobilise affordable defence and applied Engineering, and seek feasible defensive agreements while retaining the diplomatic goal. If A becomes unreachable or permanently unwilling, redirect investment to another partner. If major-power alignment becomes unachievable, compare enabled alternatives such as Scientific Ascendancy; switch only when the revised plan is materially better.

## Other long-term victory plans

Scientific Ascendancy: build institutions and cooperative knowledge access, develop the required spread of Science disciplines and Grand Breakthroughs, prepare the Science, Engineering and infrastructure needed for The Fabric of the Universe, and protect completion. Follow the legacy gameplay design document's Chapter 7 existing prerequisites instead of rewarding an isolated research score.

Domination: develop a sustainable industrial and supply base, field appropriate fleets and invasion capacity, prioritise inhabited objectives, manage occupation stability, and hold the qualifying population share for the required duration. Peaceful membership can also contribute under existing rules; control of empty territory is not the objective.

Exploration: develop survey capability and scientific study, acquire Grand Expedition discoveries, engineer and build a supplied expedition, then complete To Andromeda and Back by successful return. Revealing the map or launching alone does not satisfy the goal. Scenario objectives override generic victory planning when configured.

## Implementation sequence and acceptance

Phase 1: establish the restricted knowledge interface, editable profile schema, persistent strategic plan state, legal command adapters and decision trace. Phase 2: implement the Federation diplomacy example end to end with a small scenario; use explicit mocks only for unavailable mechanics. Phase 3: add coordinated resource allocation, bounded forecasts and replanning. Phase 4: add the remaining victory routes and five difficulty profiles. Phase 5: calibrate through reproducible automated campaigns.

Acceptance checks: no privileged information or state mutation; no double reservations; legal R&D totals and physical logistics; objectives survive save/reload; identical state, profile and planner seed produce identical commands; strategy responds to invasion without oscillation; membership refusal is respected; diplomatic victory includes the required major powers; scientific and exploration capstones are planned through completion.

Evaluate difficulty using paired campaigns across varied seeds, starts, opponents and custom profiles, exchanging starting positions. Measure victory progress, win rates, insolvency, unfulfilled commitments, unnecessary strategy switches and decision cost. Stronger settings should improve statistically across a scenario suite, not be assumed superior because they search further. Include cases where diplomacy is infeasible and where bonuses conflict with personality.

Each major decision records the primary goal, milestone, observed facts and uncertain estimates, considered alternatives, expected benefit, committed resources, selected legal commands and replanning reason. These are developer diagnostics, not automatic disclosure of AI intentions to opposing players. Keep all planning in the portable simulation layer; Unity presents state, orders and outcomes.
