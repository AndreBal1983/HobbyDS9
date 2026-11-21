# Main HUD alignment 01

13 November 2026. **Presentation source candidate; native reference-fidelity acceptance remains OPEN and gates further gameplay expansion.** No survey, reactor, combat or Engineering development rules are changed. Account usage was checked before this bounded batch and at its source-review checkpoint:48% weekly used; preserve the owner's25% remaining floor with a buffer for concurrent chats. No purchased usage/reset is part of this work.

## Evidence and scoped result

Base is QA's actual built GUI source `0a5fa688890362d3b440cadf369f44fe22c90d7b`, after PR100 source `6724c1773f792dc5e3b711e71b84537c9db5bb8e`. Existing PR100 and asset-receiving PR106 remain separate and unchanged. Shared native evidence is recorded in `artifacts/coordination/GUI-REFRESH-01/qa-after.md`, outside this worktree;52 after captures are indexed by `after-manifest-0a5fa6888903.json` there.

The actual generated-map 1920 capture and combat-map 1280 capture were inspected alongside the recovered `references/map-overview.png` (concept source PR60/owner direction PR68, as indexed by Art's visual-target register) and supplementary `generated-map-actual-route.png`. The concept's illustrative data/routes are not campaign facts. The current generated native scene has only a few legally known stars; filling it with fictional systems is not an acceptable way to match the concept's density. Shader/art/map-morphology comparisons remain their own receiving gates.

| Observed shell difference | Bounded source change | Receiving gate |
|---|---|---|
| Native header combines turn/phase/credits in a small right-aligned paragraph; reference separates their labels and values | Three current-frame readouts for Campaign phase, Turn and Credits; widths/type compact at the existing narrow breakpoint; campaign facts hidden on the landing screen | Actual values, long `Combat pending`, large balances, clipping and responsive header fit |
| Native generated/colonisation campaign puts Save/End turn above content, while the reference uses a persistent bottom command bar | Active campaign actions consistently occupy the fixed existing footer; pre-campaign Back/menu/settings/archive exceptions retained | End turn/Save/Return, draft Apply/Discard, disabled/busy and combat-owned actions; no overlap or scroll burial |
| Native frame is a short floating amber strip; reference has a connected left/bottom curved structure | Existing decorative element becomes a connected border frame with explicit Ignore picking, no new image/texture/animation | Actual corners/insets at each size; no lost targets or intercepted pointer/keyboard actions |
| Native rail/footer buttons use global pill shapes and bold text; reference rail uses compact rectangular segments | Scope six-pixel corners and normal navigation label weight to the rail; footer corners follow it; other buttons keep their controls | Current route chevron/amber selected edge, visible keyboard focus,44logical-pixel navigation targets and scroll reachability |

Phase/turn/credits still come from the exact current actor frame. The existing callback/current-frame/held-key/modal logic, route list, profile restrictions, save separation and draft semantics are retained. The new status container participates in the existing explicit detached-root publication/rollback ownership. There are no new per-frame updates, render targets, images, models, shaders or gameplay queries; this does not establish a measured performance result.

## Validation and QA handoff

Run the existing `PlayerConsoleRenderTests` under its explicit `--hobbyds9-player-render-tests` graphics-capable fixture. Its existing multi-size action/modal/draft navigation cases remain; added assertions check header readouts/current values, decorative Ignore picking, fixed-footer containment and content ending above it. Actual native generated/colonisation and combat profiles must also be exercised: an older two-human fixture alone does not prove the profile-specific footer correction.

QA should compare the same main HUD/map state before/after at1280x720,1920x1080 and true3440x1440 where available. Record logical render target/window/captured pixels/DPI separately; current native3440 evidence is cropped by the available desktop and does not pass full-width acceptance. Inspect the header, rail, map/inspector bounds, footer and focus states as distinct regions. Exercise mouse/keyboard navigation, scroll to Help, open/close Save/Menu, return to map, unsent-edit disabled End turn, modal Cancel and one ordinary turn using the exact source. Check campaign-menu absence of stale status and combat's separate primary action ownership.

Record actual idle/interaction frame time and resource observations in QA's receipt; source's bounded/no-idle-loop construction is not an FPS claim. Remaining six inherited/current Editor failures in QA's0a5 receipt remain open, as do sparse-map visual fidelity, unreceived hull/module art, populated-state comparisons and any new layout failures. Source compilation cannot close these gates. Do not start additional gameplay implementation because this source batch exists.
