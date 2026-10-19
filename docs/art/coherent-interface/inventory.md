# Coherent interface: early scope and interaction inventory

19 October2026. Owner requested complete GUI concepts including procedural galaxy interactions and cinematic3D battle resolution. **All delivered layouts/data are design concepts; no production implementation or new gameplay approval.** Worktree based on main7dab0407; newer demo/R4 capability evidence comes from coordinator checkpoint, not a claim that main contains that executable. This inventory precedes visual production.

## Status reconciliation

Implemented means a scoped profile/source capability, not final native acceptance. The fixed single-player demo, legacy v1 designer/publication, opt-in tactical-v2 physical travel and wrapper5 frontier are separate capabilities; do not make them one fictional supported campaign. Procedural/Voronoi generation, full sensors, mission surveys/study, refits and treaty UI remain future/proposed.16October colonisation/conquest supersedes older multiple-government-per-planet wording: one controlling government per planet, different planets may differ. Stage-B/physical contracts supersede old tactical chapter implementation-gap paragraphs for cloak/reveal/private rounds. Frontier only charts public lanes on actual arrivals; it is not planetary survey or general sensing.

## Interaction flows

- Start→Continue(profile-compatible slots) or setup→mode/faction/personality/difficulty→procedural seed/density preview→validation→explicit start. A full generation preview is pre-campaign; it never reveals hidden later-campaign empires. Generator controls are proposed.
- Map hover/focus→inspect/pin; pan/zoom/filter alter presentation only. Select owned fleet→known destination→route cost/ETA review→explicit submit→accepted order; invalid/unknown destinations retain accepted route. Partial-edge progress cannot be erased by a replacement. Sensor overlays separate current/stale/unobserved; no report means no guarantee of emptiness.
- World→economy/politics/yard; class library→local design/components/profiles→validation→release only in a supported profile→exact-revision construction. Current demo admits authored v2 Cruiser only; no v1 conversion. Research prerequisite→eligible Engineering→explicit develop→maturity does not promise unsupported construction.
- Frontier arrival receipt→body survey proposal→study/mission choice. Surveyed uninhabited planet→settler source/equipment/supplies→review expedition→actual deployment establishes claim→multi-week establishment. Rival same-turn deployment pauses dispute; orbital victory is separate from ground control/integration. All settlement/conquest concepts remain future design.
- Strategic fleets enter battle as separate command units→Distant opening assembly inspection→standing versus unsent fleet maneuver/enemy-fleet preference→round review→explicit commit→own receipt only→authoritative filtered result/reveal→next round. Cinematic3D playback/camera never submits positions, orders, damage or time. No tactical sector-exit, manual steering or individual firing. Cloak protection consumes normal resources; all surviving cloaked ships reveal after round1, mutual cloak yields maneuver-only first round, no fourth round.
- Battle complete→whole-batch readiness→atomic aftermath confirmation once→retained survivors/stocks/stopped routes→fresh strategic order review/submit. Terminal outcomes permit inspection/save only. Save/load confirms consequences; failed load retains host/drafts. Settings/help never advance time.

## Screen/state matrix

Every row receives distinct high-fidelity composition/state, annotated feature scope and1280/1920/3440 captures. Component-family tabs share one designer template but have scoped category content. Text/controls stay native; owned art decorates bounded viewers. Layout and all numerical fixture values are illustrative.

| ID | Screen/state | Capability status | Boundary |
|---|---|---|---|
|start|Welcome / Start and Continue|IMPLEMENTED (scoped profile)|Single-player demo implemented; new composition proposed|
|continue|Continue / compatible campaign slots|IMPLEMENTED (scoped profile)|Wrapper4 demo saves; wrapper5 frontier remains separate|
|setup|Campaign setup / mode and difficulty|PROPOSED|Full campaign setup proposed; five fair difficulty principles agreed|
|setup-galaxy|Procedural galaxy / seed and density preview|PROPOSED|Generator parameters/topology proposed; no runtime generator|
|setup-faction|Civilisation / personality / difficulty|AGREED DESIGN (future integration)|Independent faction personality and difficulty agreed; presets proposed|
|setup-review|Review campaign / generation validation|PROPOSED|Generation validation and launch transaction proposed|
|turn-summary|Weekly activity / decisions|PROPOSED|Weekly clock agreed; consolidated activity layout proposed|
|victory|Victory / inspect or save completed campaign|IMPLEMENTED (scoped profile)|Demo terminal outcome implemented; no fresh orders|
|defeat|Defeat / no new campaign mutations|IMPLEMENTED (scoped profile)|Demo terminal outcome implemented|
|map-overview|Procedural galaxy / known space|PROPOSED|Owner requested procedural map; generated fixture only|
|map-dense|Dense galaxy / semantic zoom|PROPOSED|Density and level-of-detail interface proposed|
|map-sector|Selected navigation region / system context|IMPLEMENTED (scoped profile)|Known weighted graph inspection implemented; polygon geometry proposed|
|map-hover|Map hover / keyboard focus / pinned selection|PROPOSED|Equivalent pointer and keyboard inspection proposed|
|map-route|Fleet route / preview and explicit review|IMPLEMENTED (scoped profile)|Known route and stamped submission implemented|
|map-unavailable|Unknown destination / retained accepted route|IMPLEMENTED (scoped profile)|Unknown route rejection and retained state implemented|
|map-travel|Fleet in transit / intermediate progress|IMPLEMENTED (scoped profile)|Weighted partial-edge progress implemented; sector animation proposed|
|map-sensors|Coverage / current stale unknown|AGREED DESIGN (future integration)|Sensor warning principles agreed; sensing projection/rules open|
|map-filters|Map filters / independent layers|PROPOSED|Filters are presentation proposals using lawful data only|
|empire|Empire overview / exceptions|IMPLEMENTED (scoped profile)|Owned economy/world/fleet summaries implemented in existing profiles|
|worlds|World list / owned and permitted|IMPLEMENTED (scoped profile)|Existing owned colony inspection; list composition proposed|
|system|System / bodies and separate planet owners|AGREED DESIGN (future integration)|Body survey and one government per planet agreed future design|
|world|Planet / known facts and actions|IMPLEMENTED (scoped profile)|Existing owned environment/economy inspection; future actions gated|
|colony|Colony economy / production and needs|IMPLEMENTED (scoped profile)|Economy population outputs implemented|
|politics|Population / policies and ordered causes|IMPLEMENTED (scoped profile)|Population-politics existing profile; conquest integration future|
|logistics|Infrastructure / freight and support|IMPLEMENTED (scoped profile)|Existing physical freight; expedition support accounting open|
|production|Empire production / yards and queues|IMPLEMENTED (scoped profile)|Authored admitted v2 Cruiser construction implemented in demo|
|queue|World production / exact revision / eligibility|IMPLEMENTED (scoped profile)|Queue payments completion cancellation implemented; custom-v2 conversion unsupported|
|production-cancel|Cancel order / proportional refund review|IMPLEMENTED (scoped profile)|Existing profile cancellation; authority supplies actual refund|
|buyout|Expedite construction / eligible remaining work|AGREED DESIGN (future integration)|Emergency buyout direction agreed; costs/timing/eligibility open|
|fleet|Fleet roster / ships and retained pools|IMPLEMENTED (scoped profile)|Actor-owned fleets and finite stocks implemented|
|fleet-compose|Fleet composition / create transfer detach|PROPOSED|Strategic formation operations require scoped admission; no tactical split|
|fleet-orders|Strategic orders / remain seek move follow|IMPLEMENTED (scoped profile)|Physical travel intentions implemented; follow needs current lawful contact|
|fleet-cloak|Approach cloak / capable members only|IMPLEMENTED (scoped profile)|Scoped per-fleet approach setting and per-ship capability implemented|
|ship|Own ship / fit power and finite magazines|IMPLEMENTED (scoped profile)|Exact owned inspection implemented|
|contact|Visible contact / hull and shields only|IMPLEMENTED (scoped profile)|Visible type hull shields; no enemy ammo fit or new orders|
|science|Science / six-discipline allocation draft|IMPLEMENTED (scoped profile)|Science split six disciplines and explicit apply implemented|
|engineering|Engineering / eligible target and maturity|IMPLEMENTED (scoped profile)|One target reserve cost and capped maturity; no assumed Scout construction|
|research-report|Research receipts / prerequisites and deltas|IMPLEMENTED (scoped profile)|Existing projected latest-turn records; presentation proposed|
|library|Class library / revision provenance|IMPLEMENTED (scoped profile)|Legacy blueprint registry implemented; current demo authored-v2 only|
|designer|Ship designer / overview and capability|IMPLEMENTED (scoped profile)|Local v1 designer exists; not v2 construction authorisation|
|designer-hull|Hull versions / fixed silhouette / capacity|IMPLEMENTED (scoped profile)|Hull catalogue local preview; Engineering version/refit policy separate|
|designer-components|Components / dependencies mounts quantity|IMPLEMENTED (scoped profile)|Local catalogue installation editing exists; category tabs included|
|designer-power|Cruise Combat Survey / allocation profiles|IMPLEMENTED (scoped profile)|Local v1 profile preview exists; no per-shot power switches|
|designer-validation|Validation / compare / draft errors|IMPLEMENTED (scoped profile)|Local compiler checks implemented; versioned data authoritative|
|designer-publish|Release revision / unsupported profile boundary|IMPLEMENTED (scoped profile)|Legacy publication exists; demo v1-to-v2 publication unavailable|
|refit|Refit / current versus target installed fit|AGREED DESIGN (future integration)|Compatible refits agreed direction; compatibility and lifecycle open|
|frontier|Arrival charting / new public lanes|IMPLEMENTED (scoped profile)|Implemented opt-in wrapper5 frontier; inactive in demo; not survey|
|survey|Body survey / equipment / finite study queue|AGREED DESIGN (future integration)|Arrival Survey Study distinctions agreed; mission engine not implemented|
|mission|Mission decision / repair and interference|AGREED DESIGN (future integration)|Persistent choices consequences agreed; template timing and outcomes proposed|
|first-contact|First contact / permitted approaches|AGREED DESIGN (future integration)|Detection/action-driven contact agreed; exact response rules open|
|colonise|Founding expedition / survey settlers supplies|AGREED DESIGN (future integration)|16Oct colonisation framework agreed; not implemented|
|colony-establish|Founding colony / support / evacuation|AGREED DESIGN (future integration)|Ownership on deployment and finite reserves agreed; timings open|
|colony-dispute|Rival deployment / paused dispute|AGREED DESIGN (future integration)|No processing-order winner; dispute lifecycle open|
|blockade|Planet-specific orbital operations|AGREED DESIGN (future integration)|Targeted blockade/surrender/landing framework agreed; numbers open|
|invasion|Ground campaign / strategic policy and supply|AGREED DESIGN (future integration)|Abstract weekly campaign agreed; no ground tile battle|
|occupation|Occupation / policy integration liberation|AGREED DESIGN (future integration)|Control integration and historical population identity separate|
|diplomacy|Diplomatic relations / contact and rights|AGREED DESIGN (future integration)|Diplomatic expansion direction agreed; treaty model proposed|
|treaty|Treaty offer / transit supply settlement rights|PROPOSED|Distinct rights agreed; negotiations terms and acceptance open|
|battle-lobby|Encounter selection / participating fleets|IMPLEMENTED (scoped profile)|Owned encounters and batch readiness implemented; no hidden participants|
|battle-orders|Battle deployment / round fleet orders|IMPLEMENTED (scoped profile)|One strategic fleet one command unit; Distant initial pair; no drag steering|
|battle-inspect|Battle / own ship condition and visible contact|IMPLEMENTED (scoped profile)|Selected inspected ship independent of targeting preference|
|battle-review|Review / explicit round commitment|IMPLEMENTED (scoped profile)|All owned fleet commitments; no per-ship firing orders|
|battle-waiting|Own orders accepted / waiting|IMPLEMENTED (scoped profile)|Private own receipt; no enemy readiness display|
|battle-replay|3D resolver projection / replay controls|PROPOSED|Authoritative abstract resolver implemented; cinematic3D playback proposed|
|battle-result|Round resolved / filtered event report|IMPLEMENTED (scoped profile)|Joint round and retained current stocks implemented|
|battle-hidden|No visible enemy / cautious order review|IMPLEMENTED (scoped profile)|No hidden count type location intent; standing maneuver retained|
|battle-reveal|End round1 reveal / round2 target access|IMPLEMENTED (scoped profile)|Surviving cloaked ships reveal after round1; no re-cloak|
|battle-mutual|Round1 no legal targets / maneuver only|IMPLEMENTED (scoped profile)|Mutual-cloak handling implemented; no nonexistent-shot ammo costs|
|battle-aftermath|Batch aftermath / atomic confirm|IMPLEMENTED (scoped profile)|No refill; commit once only when whole batch ready|
|battle-fresh|Confirmed aftermath / fresh strategic draft|IMPLEMENTED (scoped profile)|Stopped routes do not auto resume; new order submitted separately|
|battle-no-survivors|Owned losses / no survivor orders|IMPLEMENTED (scoped profile)|No action for destroyed fleets; retain report/save where allowed|
|saves|Save load / committed state and local drafts|IMPLEMENTED (scoped profile)|Profile-separated atomic slots implemented|
|save-confirm|Overwrite / named slot and consequence|IMPLEMENTED (scoped profile)|Explicit overwrite confirmation and safe cancel|
|load-confirm|Load / discard-local-draft confirmation|IMPLEMENTED (scoped profile)|Only successful load replaces host; generation invalidation|
|load-error|Failed load / profile mismatch / retry|IMPLEMENTED (scoped profile)|Failed load retains session and local drafts|
|audio|Audio / master music effects / cue context|IMPLEMENTED (scoped profile)|Existing Deep Station cues and UI effects; no music generation|
|audio-unavailable|Unavailable audio / recoverable settings|IMPLEMENTED (scoped profile)|Service availability handled without blocking play|
|display|Display / size mode / accessibility proposal|IMPLEMENTED (scoped profile)|Three app sizes/modes implemented; text-scale option proposed|
|help|Help / input / strategy and battle boundaries|PROPOSED|Consolidated help proposed; actual supported controls documented|
|notifications|Notifications / actionable lawful receipts|PROPOSED|Unified queue proposed; no hidden intel aggregation|
|states|Components / focus disabled error busy empty|PROPOSED|Shared state visual grammar proposed|
|diagnostics|Developer review / explicitly separate|IMPLEMENTED (scoped profile)|Existing diagnostic utilities; not player actor switching|

Sources: [design index](../../design/README.md), [architecture](../../architecture.md), [tactical core](../../campaign-tactical-v2-stage-b-core.md), [physical travel](../../campaign-tactical-v2-physical-travel.md), [frontier](../../frontier-reconnaissance-v1.md), [demo](../../singleplayer-demo-contract.md), [colonisation](../../design/colonisation-and-conquest.md), [designer](../../ship-designer-prototype-v1.md), [blueprints](../../blueprint-construction-v1.md). Newer R4/source and native evidence are recorded in the coordinator-owned primary-checkout checkpoint. Historical art inventories/concepts are references, not current native proof.
