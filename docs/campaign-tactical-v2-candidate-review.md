# Tactical v2 candidate mechanics review

Prepared during the overnight trial on 13 October 2026. This is a review of delivered Stage A proposals, not approval of formulas, independent execution evidence or acceptance of Stage B.

Source: draft PR #20 at `95a43685806f8b4fd724e6d645f854ac78f98571`, its executable contract report, `FleetCandidates.cs` and `TravelCandidates.cs`. Agreed behavior is recorded in [tactical combat](design/tactical-combat.md); runtime acceptance follows [the lifecycle milestone](campaign-tactical-v2-lifecycle-spec.md).

## Recommended candidate choices and limits

| Topic | Recommended review candidate | Why and what still needs proof |
| --- | --- | --- |
| Shared travel time | Exact remaining edge cost divided by actual fleet travel budget | Relates movement to the same elapsed week; avoids same-cost-step timing for unequal speeds. Production must handle real traversal modifiers and persisted weighted progress |
| Intermediate boundary | Batch arrivals, contact, then departures at the same instant | Prevents ordinal first-mover advantage and passing through an occupied hostile sector without a lawful meeting. Equal-time rule is explicit, not a chase roll |
| Shared start after battle | Departures before renewed contact at turn start | Supports the owner's fresh leave/follow choice after an inconclusive battle. Staying fleets with mutual engagement still meet. Both policies remain provisional until reviewed |
| Mobility | Begin with individual contributions, omit extra coordination bonus | Keeps fast ships useful alongside capital ships without adopting a new fleetwide commander modifier. It does not approve the current threshold formula as final balance |
| Target ranking | Use observed condition with stable ties | Supports understandable automatic targeting. Every input must be declared visible under the scan contract; private fit data cannot silently improve AI knowledge |
| Cloak power | Explicit concurrent authored reservation | No free operating power, extra volley or resource recovery. Later release at reveal must not invent an automatic unreviewed reallocation |
| Baseline AI | Lawful Hold and stable visible focus | A reproducible baseline for transaction/observation proof, not strategic tactical intelligence. Advanced policy remains separate scope |
| Save/commit | Private commitments, actor-own receipts, distinct boundary/global stamps, current-batch reports | Supports simultaneous orders and reload without exposing fresh opponent choices. Final contract must reconcile client chronology rather than copy prototype stamps |

These recommendations were sent as provisional candidate policies for gated work. They are not newly owner-approved tuning and do not authorize activation or merging.

## Specific mobility concern

The prototype gates an entire ship's existing defensive opportunities on `floor((speed + agility) / 2) >= threatMobility`; its optional coordination term can move the whole opportunity set across that threshold. In the documented fixture, the cruiser at 65 has no screening opportunities against threat 70, while adding a support value of 6 gives it both. This is a deliberately sharp discontinuity.

Before making that a production screening rule, demonstrate marginal changes around the threshold, a stationary but well-equipped defensive ship, agile unarmed escorts, saturated fire and different incoming weapon types. The agreed behavior requires actual equipment/power/opportunity costs and useful mobility, not mobility creating free defense or removing every powered defensive reaction without explanation. Candidate local lateral aspect is also not yet integrated into full arc, volley and shield-facing rules. Keep any such formula provisional and separately versioned.

## Observation and target ranking

The Stage A visible-ship model exposes current hull but not maximum hull, while the lowest-hull-ratio candidate takes maximum hull from its private ship fixture. The owner's agreed scan term is hull condition, so a lawful published condition ratio may be an appropriate production projection. That needs an explicit contract; do not claim this private-fixture comparison alone proves AI uses only lawful observations. Automatic resolution must also respect visible fleet focus, legal fallback, ship arcs/ranges and cloak state.

## Remaining contact edge cases

Require worked production evidence for equal-time arrivals/departures, a stationary participant after a first meeting, later arrivals into a pending encounter, disconnected contact components, opposite-direction edge crossings, weighted transit and following from last-known information. The prototype intentionally leaves several of these as policy gates. A fleet may not teleport into contact or take a second full movement budget after combat.

## Review boundary

No gameplay code or numerical policy was changed by this review. Existing source fixtures and reported tests are evidence of the candidate, not proof of the complete actual campaign lifecycle. Final review must use the delivered Stage B commit and its real-session acceptance, filtered projections and old/new deterministic vectors.
