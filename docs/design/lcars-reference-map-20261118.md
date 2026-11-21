# Fleet and Production LCARS reference map — 18 November 2026

Status: source/reference handoff for the owner-requested playtest improvements.
Baseline inspected: `af0273c2ab89ff199e8e7dfb57d8a69dfbb896c8`.
This document adds no runtime behavior or visual/native acceptance.

## Authority and reading order

The current owner direction requests a compact, filterable ship list with fleet
grouping, mission and honest supply/shield status, plus a denser construction
catalogue with useful supported comparison facts. That direction authorizes the
new work; it does not turn every older concept detail into an approved contract.

Read the [design index](README.md), [architecture](../architecture.md),
[current catalogue contract](../construction-catalogue-v1.md),
[Production receiving note](../prod-vis-01-gui.md) and
[GUI refresh record](../gui-refresh-01.md) alongside these references.

## Best fleet references

- [Owner-directed roster image](C:/HobbyDS9/artifacts/coordination/GUI-REFRESH-01/references/supplementary/owner-fleet.png): expandable fleets, nested ship rows, search, location/hull filters, sortable facts and accepted-order context.
- [Fleet composition](C:/HobbyDS9/artifacts/coordination/GUI-REFRESH-01/references/fleet-compose.png) and [orders](C:/HobbyDS9/artifacts/coordination/GUI-REFRESH-01/references/fleet-orders.png): supplementary organization/action hierarchy, not new command authority.
- [Local editable roster source](C:/HobbyDS9/artifacts/worktrees/SYS-VIS-01-art/artifacts/SYS-VIS-01/recovered-owner/fleet-roster.js); [durable PR68 source](https://github.com/AndreBal1983/HobbyDS9/blob/74193131bc6b22eda3fcc4e7da35876f8e38f227/tools/interface-mockups/fleet-roster.js).
- [Image manifest](C:/HobbyDS9/artifacts/coordination/GUI-REFRESH-01/references/supplementary/manifest.json) pins the roster reproduction to `74193131bc6b22eda3fcc4e7da35876f8e38f227` and labels it **proposed layouts, not native approval**.

The owner requested the roster direction; exact layout and fictional values are
not blanket final approval. The inspected image is a browser-source reproduction,
not a Unity screenshot. Its power-rating column explicitly says unavailable.
Use the compact grouping/filtering grammar without copying obsolete navigation,
unsupported scores or fictional ships/orders.

## Best Production references

- [Comparison board](C:/HobbyDS9/artifacts/coordination/PROD-VIS-01/production-reference-comparison.png), [Production](C:/HobbyDS9/artifacts/coordination/PROD-VIS-01/owner-production-1920.png), [selected-yard queue](C:/HobbyDS9/artifacts/coordination/PROD-VIS-01/coherent-queue-1920.png) and [world ledger](C:/HobbyDS9/artifacts/coordination/PROD-VIS-01/owner-empire-1920.png).
- [Recovered-reference receipt](C:/HobbyDS9/artifacts/coordination/PROD-VIS-01/production-reference.md) records provenance, later-contract precedence and the missing newer standalone Construction approval/render gap.
- [Original PR60 production source](https://github.com/AndreBal1983/HobbyDS9/blob/b3496b6b1da31634d9beb8a7357b2480f37a9a72/tools/interface-mockups/app.js), pin `b3496b6b1da31634d9beb8a7357b2480f37a9a72`.
- [Owner-revised PR68 ledger source](https://github.com/AndreBal1983/HobbyDS9/blob/74193131bc6b22eda3fcc4e7da35876f8e38f227/tools/interface-mockups/empire-overview.js), pin `74193131bc6b22eda3fcc4e7da35876f8e38f227`.

These are fictional browser concepts useful for aligned rows, bounded context,
amber active edges, lavender headings and ice numeric facts. They are not an
approved final compact catalogue. Later owner direction governs four catalogue
sections, direct one-item `+`, exact selected-world FIFO and independent scrolling.
Retain streamed Industry/local-material rules; do not restore old upfront credit
gates, refund/ETA claims, buyout or fictional construction options from the images.
The prior source refinement uses compact rows and one selected full-hull inspector;
the new brief asks for greater useful density, not removal of identity or blockers.

## Existing assets and bindings

- [ConstructionCardArt](../../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ConstructionCardArt.cs) maps exact hull IDs to `constitution`, `galaxy` and `defiant`, and exact infrastructure/kit identities to their images.
- [Construction image folder](../../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/ConstructionCardArt) includes `constitution.png`, `galaxy.jpg`, `defiant.png`, `fusion.png`, `research.png`, `hydroponics.png`, `mine.png` and `kit.png`.
- [Attribution](../../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/ConstructionCardArt/ATTRIBUTION.txt) identifies mockup/proof imagery; the owner-provided Defiant is local-review only. Existence is not final artistic approval.
- [ReviewIcon](../../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewIcon.cs) provides owned vector Construction, Building, Engineering, Science and lock symbols. No dedicated supply/shield icon set was recovered in this bounded search.
- [ActivityHullCatalog](../../client/HobbyDS9.Unity/Assets/HobbyDS9/ArtDemo/Runtime/ActivityHullPresentation.cs) routes disclosed exact hull identities to full-hull presentation; [ShipVisualCatalog](../../client/HobbyDS9.Unity/Assets/HobbyDS9/ArtDemo/Runtime/ShipVisualCatalog.cs) retains its separate exact provisional mappings.
- [Fleet roster](../../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewFleetRoster.cs) currently binds actual hull, four shield sectors, torpedoes and accepted travel/activity context, with unavailable states kept explicit.

The later Production correction deliberately suppressed misleading old ship-study
thumbnails. Do not reintroduce them as approved final icons or enlarge them into
hero art. Art should confirm a matching identity/source before reuse; missing
symbols may use concise labelled vector proposals. Preserve original files and
attribution. Keep one selected full-hull preview, not one live stage per list row.

## Truthful implementation and receiving

Filters, sorting and inspection are local UI actions; preserve exact selection,
fleet membership, drafts and existing guarded organization/order submission.
Supply, shields, mission, exploration and combat facts must come from supported
actor-safe projections. Do not infer strength from hull size/art, substitute zero
for unknown, or invent a composite rating. Icons supplement text and actual values.

QA should compare actual candidate captures at 1280x720, 1920x1080 and 3440x1440,
including long/empty/unknown/damaged states, filtering, grouping, selected identity,
independent scrolling, exact queue effects and no accidental orders. Source/image
inspection and compilation are separate from Unity rendering/native acceptance.
This handoff inspected the two primary reference images and source bindings only;
it generated no assets, launched no Unity/player and ran no gameplay tests.
