# Central combat viewer and aftermath

Prepared 14 October 2026. **Proposed presentation for owner review.** Central battle space and meaningful aftermath follow the relayed owner playtest direction; detailed labels, prominence, dimensions and visual arrangements below remain proposals. This increment changes documentation and independent wireframes only. No gameplay, Unity implementation, model import, merge or activation.

**Owner acceptance, 14 October 2026:** the visual design and layout are accepted for a first iteration. Use the accompanying wireframes as the visual implementation baseline. This supersedes the pending visual/layout review status above; it does not establish implemented/native acceptance, approve new gameplay or proposed balance policies, or authorize default activation. Further detailed presentation refinements remain iterative.

## Review artifacts

Open the [annotated interactive wireframes](wireframes/combat-interface.html). The review controls switch between eight illustrative states, three layout widths and faction accents. All battle buttons are inert; these are fictional display examples, not connected sessions. The width controls preview layout in the document, not native Unity resolution acceptance. Numbered annotations appear beside/below the frame.

## Sources and status reconciliation

Read [contributor instructions](../../AGENTS.md), [design index](../design/README.md), [tactical design](../design/tactical-combat.md), [architecture](../architecture.md), [local lifecycle milestone](E:/HobbyDS9/docs/campaign-tactical-v2-lifecycle-spec.md) and [faction interface direction](E:/HobbyDS9/docs/art/faction-interfaces.md).

The local checkout remains older/dirty. Newer sources were read with `git show` at the supplied consolidated checkpoint; no pull/reset changed local work. Use the pinned [physical travel review](https://github.com/AndreBal1983/HobbyDS9/blob/c5d2b6ac32e8a6c47b073e58f53e9ecb129c11e4/docs/campaign-tactical-v2-physical-travel.md), [playable UX specification](https://github.com/AndreBal1983/HobbyDS9/blob/c5d2b6ac32e8a6c47b073e58f53e9ecb129c11e4/docs/campaign-tactical-v2-playable-ux-spec.md), [Stage B core](https://github.com/AndreBal1983/HobbyDS9/blob/c5d2b6ac32e8a6c47b073e58f53e9ecb129c11e4/docs/campaign-tactical-v2-stage-b-core.md) and [newer architecture](https://github.com/AndreBal1983/HobbyDS9/blob/c5d2b6ac32e8a6c47b073e58f53e9ecb129c11e4/docs/architecture.md) for context. Pinned URLs avoid broken relative links to files absent locally.

**Conflict to preserve explicitly:** early lifecycle/UX documents say the lifecycle/physical contracts are absent. Later review code implements them inside gated composite sessions. Conversely, that implementation and consolidated merge do not establish production activation or owner acceptance of proposed mobility, timing, cloak-power or observation-precision policies. The legacy strategic screenshots are evidence of the old side-panel client, not successful tactical-v2 manual acceptance. This proposal grants no missing authority by drawing a button.

Inspected [observation/commitment contracts](https://github.com/AndreBal1983/HobbyDS9/blob/c5d2b6ac32e8a6c47b073e58f53e9ecb129c11e4/src/HobbyDS9.Contracts/CampaignTacticalReviewContracts.cs) expose own accepted commitment separately from standing intent, permitted enemies/relations and filtered reports. They exclude opponent readiness and new orders. [Physical travel contracts](https://github.com/AndreBal1983/HobbyDS9/blob/c5d2b6ac32e8a6c47b073e58f53e9ecb129c11e4/src/HobbyDS9.Contracts/CampaignTacticalTravelContracts.cs) distinguish Stay, StayEngage, Leave and Follow, with known-contact freshness and explicit routes. Quiet Stay exists; its prominence here is an interface proposal.

## Owner-supplied polish reference, 14 October 2026

The owner supplied [this interface image](E:/HobbyDS9/docs/art/concepts/combat-interface-2026-10-14/04-owner-polish-reference.png) as inspiration for interface design polish. It supplements the accepted first-iteration layout; it is a visual reference, not evidence of implemented controls or approval of every depicted value, ship identity or behaviour. Source/creator and distribution clearance are unrecorded; retain it as reference material rather than a runtime asset.

**Proposed application:** warm amber LCARS elbows and segmented perimeter rails; restrained lavender section headings; dark panel interiors; thin, consistent dividers; and a clear distinction between selected cards, editable drafts and the primary review action. Give ship imagery the central visual emphasis, with peripheral controls quieter. Keep header/footer aligned to the panel grid. Use soft highlights sparingly for selection and focus, avoiding continuous glow on ordinary borders. Condensed display lettering can support short headings, while readable text carries values and instructions.

The image's separate standing maneuver and unsent draft, same-fleet Screen explanation, explicit schematic staging and own-ship directional shield display fit the existing proposal. Preserve those distinctions when polishing. Formation spacing and model scale remain illustrative; a wireframe hostile must not imply a known class, fit or precise location beyond the permitted observation. The ship model, registry and inspector must refer to the same selected ship. Four shield stocks require numbers/labels as well as arcs, and empty ammunition remains explicit.

Apply the same hierarchy to the other faction consoles through their own frame shapes, typography and motifs, following [faction interface direction](E:/HobbyDS9/docs/art/faction-interfaces.md). This Federation image does not turn those themes into LCARS recolours. Responsive layouts, keyboard focus, text readability and authoritative action boundaries take precedence over copying the reference pixel for pixel. Exact material finish, font choice and animation remain proposed refinements; no interface implementation changes in this update.

## Proposed composition

Use a dedicated central combat surface within the established console. On standard/wide displays it covers most of the play area, with the strategic shell visible only as subdued context. Narrow displays use a full-screen sheet. Closing returns to the campaign; a persistent Resume battle affordance keeps the pending checkpoint discoverable. Closing, camera movement, inspection and resizing never commit, finalize or advance time.

1. **Header:** sector, battle status and round boundary. Show a small review-profile indicator when relevant, with unsupported features clearly gated. Do not present an encounter-wide fleet count that includes hidden ships.
2. **Fleet rail:** owned fleets and separately permitted visible hostile fleets. One strategic fleet remains one command unit. Select visible ships for inspection, never individual attack orders. Long lists scroll locally.
3. **Central viewer:** fleet silhouettes/model placeholders, selected pair's backend range/exposure and an explicit schematic staging label. The Galaxy prototype remains separate; no assets are imported here. No grid, ruler, drag-to-steer or apparent collision authority. A camera turn does not rotate the meaning of ship-relative shield sectors.
4. **Order dock:** selected own fleet, standing maneuver/focus, unsent edits and a round-wide review summary. Only fleet maneuver and enemy-fleet preference are player choices. Legal options come from authority. Same-fleet screening explanation lives beside the order, not as another allied-fleet selector.
5. **Round report:** a short readable outcome summary with subordinate ordered event scrolling. Ship/fleet filters use only permitted identities. Actual firing/defensive contributions and fallback reasons come from reports, not Unity calculations or class-label bonuses.
6. **Persistent footer:** one context-sensitive primary action, Back and inspection/save access if the active host supports those boundaries. Review precedes commitment; an edit field's Enter cannot commit a round.

Order review lists every controllable owned fleet, retained versus changed intent, and makes clear that focus is a preference among legal targets. Accepted receipts lock the submitted round. No automatic stale-command retry or silent replacement of orders; retain rejected drafts and display the authoritative reason. No opponent-order animation before round resolution.

## State and action matrix

| Wireframe | Main content | Footer/action boundary |
| --- | --- | --- |
| Battle planning | Selected fleet, standing/draft maneuver and lawful enemy preference, own inspector, schematic range | Review round orders; then explicit Commit round. No mid-battle exit/re-cloak/profile/refill controls |
| Orders accepted / waiting | Own receipt and committed order summary; inspection remains available | “Waiting for round resolution”; commit disabled. No opponent readiness count, check marks, progress percentage or private-order tooltip |
| Round result | Accepted outcome, retained own stocks, reveal and lawful fallback explanation; next-round boundary | Plan next round only if battle continues; viewing report never reruns resolution |
| Battle complete / aftermath pending | Staged losses/survivors, location and current own hull/sectors/ammunition | “Confirm aftermath” only if whole-batch readiness allows it. Otherwise “Waiting for other battles”; do not expose their hidden participants |
| Aftermath confirmed / survivors | Confirmed own losses and retained pools, current strategic Planning turn, stopped routes | Per surviving fleet: Stay, Stay / seek combat, Leave, Follow. Review a fresh order; submit separately from aftermath |
| Defeated / no owned survivors | Owned losses, no surviving fleet to order | If batch pending, same finalization gate as above; after confirmation Return to campaign. No active movement/follow action for destroyed fleets |
| Destination/contact unavailable | Confirmed aftermath plus retained local route/follow draft and precise lawful rejection | Submit disabled; Choose another destination/contact or Keep current accepted order. No invented route/contact fallback or hidden survival confirmation |
| No visible enemy | Empty hostile area and cautious message; own maneuver remains editable | Normal review/commit; never auto-select Screen or disclose hidden names/counts/positions |

The waiting state represents an unresolved joint round, not evidence that a particular opponent has or has not committed. The completed state concerns this encounter; only explicit public batch readiness enables finalization.

## Inspection and round-one privacy

Own ship inspection shows exact hull current/max, four ship-relative shield current/max stocks, per-package ammunition/readiness and backend reasons. Zero-capacity pools display 0/0 rather than a percentage. Preserve distinctions between unpowered, cadence-limited, no legal opportunity and empty ammunition.

Visible enemy inspection offers type, permitted hull condition and shield strength. Default mockups use qualitative enemy condition labels; exact numeric enemy precision is a review-policy choice and must be explicitly gated. No enemy ammunition, hidden fit/power, fresh orders or inferred reserve bars appear in inspection, report, focus picker or replay.

Hidden round-one ships do not appear as named silhouettes, outline placeholders, count badges or missing-index gaps. Mixed fleets show only visible members. Incoming ordnance may be reported without identifying its hidden launcher. End-of-round-one reveal appears only from the authoritative permitted boundary; no premature reveal on firing. Mutual full cloak remains ordinary maneuver-only round one, within the three-round limit. Approach cloak is read-only battle context, not a mid-battle toggle. Do not surface detailed operational cost/mobility claims unless the active reviewed profile supplies and permits them.

## Aftermath: confirmed facts versus next-turn drafts

The aftermath is a full-width report with a fleet selector, owned loss summary, survivor table and next-action dock. Keep Fore, Port, Starboard and Aft separately readable. Example stocks in wireframes are illustrative: hull 90/100, shields 3/40, 13/40, 40/40, 5/40 and torpedoes 0/3. They demonstrate depletion, not balance or a recorded battle.

**Before finalization:** label values “Battle result — pending campaign confirmation.” All subsequent travel controls stay unavailable. Confirm aftermath atomically once across the complete batch. While pending, retain the report on failure; do not optimistically show a new turn or pay/refill anything. After successful confirmation, remove the finalization action and read the new Planning boundary. A repeated click/load cannot finalize again. Long-term report/history retention needs host support; this proposal does not add a permanent battle archive.

**After finalization:** label the outcome “Aftermath confirmed” and the next-action panel “Draft order for current Planning turn.” Show the actual turn supplied by authority, not a UI-incremented date. All participating routes were stopped; never resume an old interrupted route automatically. Selecting an action drafts only one owned surviving fleet's strategic order; batch submission and cross-fleet redistribution are outside this increment.

| Choice | Player-facing explanation | Guardrail |
| --- | --- | --- |
| Stay | Remain in this sector without requesting combat | Neutral, low-emphasis alternative; does not promise immunity from lawful hostile engagement |
| Stay / seek combat | Remain and seek a lawful engagement during the next advance | No immediate battle or round four; actual same-sector/same-time contact and engagement rules still required |
| Leave | Choose a known destination and review an explicit known route | Next-turn strategic travel, not tactical retreat or guaranteed escape. Weighted costs/progress come from Application; no fabricated ETA |
| Follow | Choose a lawful known contact and an explicit route through known space | Show last observed sector/turn and freshness only. No hidden enemy route, destination, intention or guaranteed catch; lawful waiting requires tracking coverage |

Display accepted order separately from an unsent replacement. After submission, show the actual receipt; movement occurs on normal strategic advancement. Changing a card or inspecting the route does not submit or advance. A failed command preserves the draft and the last accepted order; present only the backend's lawful reason. If a contact becomes unavailable, do not explain it as destruction, cloaking or departure unless that fact is independently permitted.

No between-battle recovery/refill, instant pursuit result, chase probability, repair action, reallocation of ships or automatic second encounter is introduced. Tactical **Fighting withdrawal**, if offered by the active contract, changes battle maneuver and is not the aftermath **Leave** action or an exit order.

## Responsive and faction treatment

Layout dimensions are proposed logical review sizes, not modifications to existing responsive policy. Preserve the [LCARS foundation](../lcars-ui-foundation-v1.md) readability floor and common action semantics.

| Size | Arrangement | Scrolling/focus |
| --- | --- | --- |
| Narrow: 1280 × 720 review | Full-screen sheet; compact horizontal fleet strip; viewer beside Orders/Inspect tabs; report replaces viewer when opened | Stable header/footer. Owned lists, inspector and events scroll inside their panels; primary action stays visible. Tabs preserve draft/selection |
| Standard: 1920 × 1080 | Central surface with fleet rail, large viewer and bounded order/inspection dock; compact report strip below | No full-screen vertical combat list; long events/rosters scroll locally |
| Wide: 3440 × 1440 | Same controls bounded in width; extra space goes to staging and optional expanded report/inspector | Do not stretch text or detach actions from selected fleet; stable reading measure |

Use faction-specific colours, frame shapes and typography on the same hierarchy/placement. The wireframe accent switch illustrates tokens only; it is not finished Klingon/Cardassian/Romulan/Dominion UI artwork. Text, badges and borders convey state independently of colour. Keyboard focus enters the sheet, moves in reading order and returns to the launching control on close; Esc returns to campaign without submitting, with a draft-preservation/discard policy reviewed separately. Reduced-motion presentation must preserve every report/control.

## Review checklist and open questions

The wireframe must cover all eight states at narrow/standard/wide, with no horizontal document overflow, visible primary footer, readable own four-sector values, qualified enemy condition, clear pending/confirmed labels and gated unavailable actions. Artifact rendering demonstrates proposal readability only; no Unity/native playtest or gameplay acceptance is claimed.

### Executed artifact checks

Headless Chrome rendered the standalone document in 24 state/width combinations: eight states at 1280×720, 1920×1080 and 3440×1440 browser viewports, selecting the corresponding proposed layout. Checks passed for document/frame horizontal overflow, footer containment, five annotations per state, inert gameplay commands, hidden mock roster, own accepted receipt, confirmed/draft distinction and faction-accent persistence. No JavaScript page errors were reported. Narrow combat, standard aftermath and wide combat images were visually reviewed; narrow stage spacing was adjusted before the final successful pass.

The [render/check recipe](E:/HobbyDS9/tools/art/render_combat_wireframes.cjs) uses the installed bundled Node/Playwright and installed headless Chrome against a loopback static server. It tests this independent wireframe, not Unity or campaign rules. Local ignored evidence: [layout results](E:/HobbyDS9/artifacts/art/combat-interface/layout-checks.json), [annotated standard screen](E:/HobbyDS9/artifacts/art/combat-interface/annotated-standard.png), [narrow battle](E:/HobbyDS9/artifacts/art/combat-interface/narrow-plan.png), [standard aftermath](E:/HobbyDS9/artifacts/art/combat-interface/standard-survivors.png) and [wide battle](E:/HobbyDS9/artifacts/art/combat-interface/wide-plan.png). Seven PNGs are retained locally; these ignored evidence files do not travel with Git. The HTML and proposal remain the portable review artifacts. No gameplay tests were needed for this design-only change.

Open owner/art questions:

- Prefer a centred near-full-screen overlay or an entirely separate screen at standard/wide sizes? Proposal: overlay, full screen at narrow.
- Should quiet Stay be a separate fourth card or a secondary option beside Stay / seek combat? Proposal: visible, lower emphasis.
- Should the default centre favour a restrained schematic or eventually the 3D fleet stage? Proposal: schematic first, same authoritative labels for either.
- Exact report retention after finalization, permitted enemy condition precision and final review-profile policies require contract/product review. Do not resolve them through styling.

No game rules or automatic activation follow approval of the visual proposal. A future implementation should bind this design to the accepted host/facade, then exercise actual Unity controls and privacy/rejection paths independently.

Delivery note: this isolated branch copies the owner-accepted combat document and its interactive wireframes from the local art workspace; linked lifecycle/faction inputs remain explicitly local references. Art director source files are untouched. Wireframes are illustrative, not executable or native acceptance.
