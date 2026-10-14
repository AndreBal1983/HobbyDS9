# Single-player demo contract

14 October 2026. **Implementation in progress, not native acceptance.** Additive, opt-in, provisional demo policy; old review/default loaders remain unchanged. Source base PR40 `8ae866556e21cef47a9e5b0efaea49e01d30f3f6`. Primary owns portable backend/scenario/tests, Secondary owns client binding. No Unity or desktop resource lease is held by this workstream.

## Early binding contract

`CampaignSingleplayerDemoApplication.StartPackage(CampaignSingleplayerDemoPackageDto)` / `LoadSave(package, CampaignSingleplayerDemoSaveDto)` return `ICampaignSingleplayerDemoSession : ICampaignIntegratedPlaytestSession`. Additive methods are `CaptureDemo()` (immutable objective/outcome/turn/phase/human identity only) and `ExportDemoSave()` (trusted persistence host only). Existing strategy, own ship, contact, travel, round and aftermath APIs remain in use for **HumanActorId only**. No enemy-actor switching is needed or permitted. Factory implementation and acceptance follow this early contract checkpoint; do not claim runtime support from DTO availability alone.

`AdvanceTurn(turn, revision)` stages AI economy/production/research and fleet orders inside the whole-turn transaction. It never advances automatically merely because a screen renders. Enemy round choices commit deterministically from lawful round-start observations before human orders, with the same fleet/target validation. Human `CommitRound` remains explicit and resolves only one joint round. `FinalizeBatch` stays a separate explicit human action so reports are not dismissed automatically. No automatic human orders, hidden counter-picking, extra resource bonuses or ammunition refill.

The complete new save is wrapper **4**, profile `campaign-tactical-v2-singleplayer-demo-1`, containing integrated3/physical2/lifecycle1/campaign14/content11. Bare legacy/physical/integrated saves are rejected by this loader; no migration. Package binds `HumanActorId`, `OpponentActorId`, `ObjectiveNodeId`, `LastTurn`, `AiPolicyId` and the exact integrated package. Persisted opponent plan contains policy/actor/goal/last-planned turn; accepted commands, fleets, stocks, queues and round commitments remain in the same nested authoritative root. Exporting only an inherited nested root must fail closed. Rules `science-engineering-v1` and compiler `ship-design-compiler-v1` remain unchanged.

## Provisional bounded scenario

One human Federation and a Cardassian AI contest **Bajor**. The AI has a persistent, public scenario goal to defend Bajor, buys affordable admitted cruisers and routes its own fleets there through known topology. It may use only owned resources/ships, public prices and lawful contacts; no future player route or private orders. Other authored civilizations are inert AI with no combat role in this bounded scenario.

Objective: retain a surviving human fleet at Bajor, without a surviving hostile fleet there, at the end of resolving turn12; destroying the opponent's military earlier also wins. Losing every human ship loses. If neither side achieves victory by the deadline, report stalemate. Evaluation waits for pending combat aftermath; it never bypasses a battle to declare success. This is not invasion, ownership transfer or diplomacy. Contact freshness remains finite20 turns, longer than the bounded demo, and current encounters produce lawful renewed observations. No infinite tracking is introduced.

Demo content adjusts both sides' initial capital, shared cruiser prices/Industry and shared research pace explicitly for decisions and reinforcement within a few turns. Existing fits and finite shield/ammunition stocks remain exact. Final numerical values and observed completion timing will be recorded with actual acceptance, not presented as approved global balance. General v1-blueprint-to-v2 construction/refits remain unsupported.

## Required evidence before integration

Actual single-human headless flow: research, paid production, completion, lawful routing, AI decisions, interactive joint rounds, retained aftermath, fresh orders and objective; ordinary and pending-combat reload; repeated/reordered/reload vector equality. Prove human commands are never commandeered, AI cannot read fresh human orders/enemy ammunition, deterministic budget/order, replay rejection, whole-turn rollback and terminal/stall bounds. Independent backend/persistence review and exact-head portable/static/CI validation precede merge. Native player usability is a separate combined release gate.
