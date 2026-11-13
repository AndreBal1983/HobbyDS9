# SYS-VIS-01 — scoped GUI correction plan

13 November 2026. BasePB788190d3dcc2c371d23a694e21b13e08454034ee. Recovered references: Art receipt artifacts/coordination/SYS-VIS-01/art-direction.md, original PR60b3496b6b1da31634d9beb8a7357b2480f37a9a72 and owner revision PR6874193131bc6b22eda3fcc4e7da35876f8e38f227. Art's current-system-owner.png and system-reference-comparison.png were visually inspected. Their comparison is reproduced source concepts against the actual owner screenshot, not a new approved render.

## Comparison before implementation

Current: Earth label has a portrait of orbit arcs; two tiny globes occupy a small central cluster in a huge fixed-height viewer, while body cards sit below the tallest inspector. Recovered prior concepts: bounded image hero, connected body list and selected context dock, aligned readable facts, restrained amber/lavender console hierarchy. Their blue CSS planet and fictional data are not literal replacement artwork.

The latest standalone director renders/camera/lighting scenes remain missing per Art's scoped recovery. Matching that latest target is therefore HELD. This patch may correct the concrete selected-body failure and apply recovered hierarchy, but cannot claim artistic parity or owner acceptance. Do not regenerate planets, add undisclosed bodies or switch models to missing/unreceived packs.

## Concrete correction

1. Replace the overview-texture crop with a lazy, separate selected-body render target. Reuse the selected disclosed body's validated mesh and property block. Render one portrait globe only, with its own camera, fixed framing and presentation light; exclude star/orbits/other bodies. Do not depend on overview viewport-Y conventions, camera yaw/zoom or screen position. Portrait failure clears the image and shows explicit unavailable text, rather than displaying a stale globe.
2. Keep two render targets maximum: existing bounded overview plus512-square portrait. Allocate portrait once on demand; render only when selected identity changes/target is recreated. Borrow mesh/material/texture resources, own and dispose clone/camera/target. Stages remain inactive except synchronous render requests. No perpetual animation, gameplay state or hidden observation access.
3. Fit the schematic overview to actual disclosed orbital bounds/aspect with deliberate margin, and modestly enlarge all disclosed-body glyphs equally. Retain explicit schematic sizes/distances. Selection/picking, focus/open distinctions, reduce motion and camera reset retain existing boundaries.
4. Put body cards directly below the overview in its visual column, with a bounded selected-body inspector beside it. Increase portrait to220logicalpx, cap inspector width, wrap to stacked layout on narrow content geometry. Use existing LCARS framing and44px controls, not a new visual theme. No new content/data, physics, yields, queues or save changes.
5. Preserve map attribution, environment/disclosure guards and neutral unresolved appearance. No speculative atmosphere/corona/material redesign while latest target is missing.

## Verification and QA receiving

Source/compiler work only here; QA owns Unity/native. Add focused engine cases for actual portrait target identity, no crop, selected mesh/property-block binding, orbit/overview independence, invalid identity clearing and native-resource disposal. Do not claim authored tests executed. Compile modified runtime/test sources against installed9f1 reference closure where available.

QA receiving: frozen SHA, Earth/Mars disclosed fixture; actual overview/Earth/Mars/rotation+zoom extremes/reduced motion at1280x720,1920x1080,3440x1440; inspect named globe and absence of orbit fragments, fit/marker alignment, no hidden bodies, selection/focus/open events, state-preserving reentry/load, no idle redraw, bounded targets and disposal. Capture actual before/after. Until such frames exist, only before screenshot and source corrections are evidence. Latest-render quality admission remains blocked on original Art director delivery.
