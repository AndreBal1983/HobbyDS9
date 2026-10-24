# Playable battle presentation v2 receiving

24 October 2026. Source preparation only, based on the accepted V3/menu source at376af8e. Integration is held until the current generated-map visual receiving checkpoint. This does not change the current V3 receiving candidate or its frame budget.

The playable battle opens on Battle view when a new encounter is first displayed. Later user tab choices survive ordinary redraws of that encounter; existing new-round playback, round-report and aftermath transitions retain their behavior. Actor/view replacement resets the local view preference. The active tab is marked visually.

Fleet details start collapsed on a new view, with an explicit button beside the tabs. The expansion preference persists across subsequent encounters for the same actor/view. The existing fleet/ship selectors and inspection remain accessible on expansion; hiding them does not submit, reset or discard any order. The order rail stays visible.

Only Battle view replaces the detail scroller with a flexible image host, allowing the scene to fill the available central area. Status, Report and Inspection retain scrolling. Playback controls use compact spacing; the scale explanation moves to a short caption and tooltip. The render target remains exactly1280x600 with ScaleToFit: no new GPU allocation policy, camera, models, simulation geometry, battle order, playback acknowledgment or resource authority is introduced. Letterboxing and actual painted size must be judged in receiving, not inferred from CSS.

Player and Editor reference compilation must pass. Source review checks the encounter/actor state, existing callback guards and unchanged command/draft flow; no cosmetic mirror tests are needed. No Unity, import, graphics or native activity belongs to this source increment.

Later scoped receiving must use a labelled lawful pending battle checkpoint and actual controls at1280x720,1920x1080 and3440x1440. Confirm initial Battle selection, highlighted tabs and collapsed details, then expand/select/inspect/collapse without changing committed state or a local order draft. Select Status or Report and redraw the same boundary to prove the choice is retained. Resolve existing rounds through the real UI, verify playback controls and report/aftermath remain reachable, and retain save/reload and historical-report distinctions.

Record image widget and actual ScaleToFit painted rectangle separately from the black surround. At1280x720 target at least220px painted scene height, materially larger than the prior140px widget; round actions, tabs, playback controls and the order rail must remain on screen without horizontal panning. At all three sizes, preserve screenshots and any clipping failure. If the layout cannot meet both image size and action accessibility, revise it before promotion. This target is proposed receiving tuning, not an already observed result or owner visual acceptance.

The [tactical design](design/tactical-combat.md) still distinguishes authoritative abstract tactical resolution from cinematic Unity presentation. The [architecture](architecture.md) and existing playable campaign contracts remain unchanged.

## Isolated material preparation, 24 October2026

The later material increment preserves finite authored emission, metallic and smoothness values when adapting existing ship materials to the isolated PreviewStudio shader. That shader consumes emission directly; source shader keywords are not copied. HDR emission remains above1 where authored. Missing or nonfinite properties retain black emission,0.3 metallic and0.4 smoothness, matching the existing shader defaults; finite surface scalars are clamped to0..1. Invalid base colours use the previous faction fallback.

Material caching includes base colour, emission, both surface values and the existing effect-versus-hull shader choice. Thus differently finished surfaces with the same base colour remain distinct. Original shared assets are read only, effects retain their shader/response, and disposing the stage clears its owned material cache. Cardassian named colour adaptation is retained; this does not add a manifest roughness conversion, new model aliases, geometry, hardpoints, glow-based capabilities or combat behavior.

This is source preparation in an isolated Root branch. The package undergoing native V2 tests is unchanged. Player and Editor reference compilation passed with0warnings/0errors. Actual image quality and material lifetime remain unaccepted until a separate receiving lease after the safe map checkpoint. Focus receiving on authored source properties versus clones, same-colour/different-response cache separation, missing/nonfinite fallback, unchanged source properties and effect markers, and repeated construction/disposal. Use the same labelled battle framing and existing three-round controls; no additional full campaign replay is needed solely for this material change.
