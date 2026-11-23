# TURN-POPUP-01 completion presentation

Status: source candidate, partial Production coverage. Unity/native acceptance is pending. This increment does not change simulation rules or saves.

## Boundary and delivery policy

The existing campaign dialog presents actual completion facts only after a successful live transition to the next player Planning week. Research comes from the published actor-filtered receipts: Science discoveries, Science levels and mature Engineering projects. Allocation, spending and intermediate maturity are excluded. An AwaitingCombat checkpoint waits for the real aftermath transition. A failed action, ordinary redraw, tab change or Planning save load does not collect events.

The presentation queue is session-local, scoped to the model object, controlled actor and generation. Each accepted boundary and notice is consumed once. Loading or replacing a session establishes a new baseline; old Planning receipts are not replayed. Unread notices are not persisted across quit/restart. An unread notice can reattach after a same-session redraw. A persisted unread inbox would require a separate contract and save/version decision.

Next/Close/Escape acknowledge one notice. Inspect uses the existing Research or owned-world navigation only when the destination still exists. Callbacks verify model, frame and notice identity. The existing one-use modal permission, disabled background, scrollable body, fixed footer, focus restoration and keyboard release machinery are retained; held Escape is guarded across sequential notices. No gameplay command is submitted by notification acknowledgment or inspection.

Engineering labels/effects come from the received twenty-project catalogue. Advanced Starship Engineering explicitly states that the Cycle construction catalogue excludes Scout. Version availability does not promise a compatible fit or construction option, and existing ships/published revisions remain unchanged.

## Remaining Production dependency

The current client can read actual completed structure receipts. It cannot lawfully recover complete ship and settlement-kit history, including a ship destroyed in the same resolved turn. The source therefore includes structures only. It does not infer completion from missing queue rows, living ships, stock changes or trusted full-save access.

Full TURN-POPUP-01 delivery is held for Backend's detached actor-filtered terminal-production projection: resolved turn, unique order ID, order owner/colony, kind and actual name/definition, frozen blueprint/revision and optional produced ship ID. Filtering must follow the completed order owner, even when current ownership has changed. No new saved event/acknowledgment ledger is requested. The receiving request and authoritative source map are recorded in the shared TURN-POPUP-01 coordination directory.

## Verification and receiving

Portable tests exercise one-use boundaries/acknowledgments, deterministic ordering/deduplication, actor/week filtering, no-completion silence, AwaitingCombat delay, failed/same/skipped boundaries, stale model/generation callbacks and the Scout qualification. The focused completion plus existing modal-permission suite passed 31/31 cases. Source compilation is against installed Unity 6000.6.9f1 reference assemblies; it is not Editor, player, rendering or input execution.

QA owns Unity/native execution. Required receiving cases are actual Science discovery/level and Engineering completion; structure, ship and kit completion after the remaining feed arrives; simultaneous notices; no-completion and failed turns; combat aftermath; redraw/tab/load isolation; Next/Close/Inspect; reentrant callbacks and held Return/Space/Escape; background exclusion; focus and long-body layout at 1280×720. Use the existing popup classes and recovered Art references. Do not claim latest-render parity or owner visual acceptance from source compilation.

See [current implementation](current-demo-implementation.md), [Science and Engineering foundation](science-engineering-v1.md), [twenty-project content](engineering-development-tree-v1.md), [System source increment](sys-vis-01-gui.md) and [Production source increment](prod-vis-01-gui.md).
