# Compact LCARS references and exact asset bindings

18 November 2026. **Proposed reference, not an implemented or accepted native screen.** Base `af0273c2ab89ff199e8e7dfb57d8a69dfbb896c8`. No gameplay, Unity/runtime/resource/importer/save/workflow edits. The new owner brief governs implementation; recovered concepts provide hierarchy, not mechanics or final approval.

Open [compact-reference.html](compact-reference.html); tabs select **static illustrative layouts**. Controls submit nothing. [Fleet 1280](fleet-1280.png), [Production 1280](production-1280.png), [Fleet 1920](fleet-1920.png), [Production 1920](production-1920.png) are actual inspected headless-browser renders, not Unity. All names, locations, costs, percentages, queues and missions are labelled illustrative; use only lawful current frame values in the receiver. GUI owns live UI sources; Art changes none.

## Recovered direction

- [Owner fleet concept](recovered/owner-fleet.png), source `74193131bc6b22eda3fcc4e7da35876f8e38f227`: owner-directed **proposed** filters, expandable fleet headers, indented compact ship rows, stable selected row and separate details. Not final/native approval. Do not copy obsolete Empire navigation, internal IDs, example facts or an invented power metric.
- [Production comparison](recovered/production-reference-comparison.png), original receiving `artifacts/coordination/PROD-VIS-01/production-reference.md`: compact catalogue/queue hierarchy. Later four-section/direct-`+` construction semantics supersede old paid-on-enqueue concepts; the new brief asks for greater density and real comparison fields.
- Both recovered PNGs are unchanged copies; [provenance/hashes](recovered/provenance.json). No new approval or external licence is claimed.

## Practical layout values

**Fleet:** search/location/status filters on one line; fleet headers 44 px; ship rows **58 px** starting value; 16 px member indentation. Columns: member selection, exact-hull image 56×40, ship/class, supply, shields, accepted mission, Inspect. Primary text 17–19 px; secondary 14–16 px. Keep ≥44×44 logical-pixel interaction targets, including the area containing a visually smaller checkbox. Selected ship details and the existing full-hull 3D inspector stay separate from compact rows. Filters/selection/inspection issue no order or transfer. Preserve organization review/validation and named fleet membership; co-located fleets remain separate.

**Production:** collapse context prose into one world/Industry/material line plus expandable context. Suggested **76 px rows**, thumbnail 56×40, Inspect and `+` 44 px, separate 260 px queue (310 px on larger layouts). Reference shows six choices plus category headings. Retain Ships / Planetary buildings / Special projects / System improvements, direct `+`, Inspect, separate catalogue/queue scrolling, mixed FIFO and cancellation. No extra enqueue confirmation, buyout, reorder, credit charge or unsupported item. Selected-world/frame guards remain mandatory.

Line one: exact name/revision. Line two: **Industry cost**, actual exploration/survey capability and named combat outputs where supported. No invented composite rating: use **Not reported** until projected. Omit ship-capability fields for buildings rather than implying zero. Line three: exact material requirements, owned/queued counts and a concise availability reason. Queue keeps actual Industry/material streaming and cancellation consequences, not the old pay-once concept. Selected full-hull preview retains its existing 180 px height when expanded; no render target per row.

At short/narrow viewports, scroll lists/details predictably rather than shrinking text or using horizontal scrolling. Reference captures are 1280×900 and 1920×1080; 1280×720/ultrawide native acceptance remains QA-owned. The broad reference shell illustrates hierarchy, not a navigation implementation mandate.

## Exact existing image bindings

Directory: `client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/ConstructionCardArt/`. [Exact paths, Resources keys, GUIDs and local byte hashes](bindings.json).

| Exact disclosed identity | Resources key | Existing file |
| --- | --- | --- |
| `hull.constitution-explorer.v1` | `ConstructionCardArt/constitution` | `constitution.png` |
| `hull.galaxy-cruiser.v1` | `ConstructionCardArt/galaxy` | `galaxy.jpg` |
| `hull.defiant-combat.v1` | `ConstructionCardArt/defiant` | `defiant.png` |
| `infrastructure.fabrication-annex.v1` | `ConstructionCardArt/fusion` | `fusion.png` |
| `infrastructure.research-laboratory.v1` | `ConstructionCardArt/research` | `research.png` |
| `infrastructure.hydroponic-extension.v1` | `ConstructionCardArt/hydroponics` | `hydroponics.png` |
| `infrastructure.frontier-mine.v1` | `ConstructionCardArt/mine` | `mine.png` |
| Existing supported settlement kit | `ConstructionCardArt/kit` | `kit.png` |

Reuse `ConstructionCardArt.Ship(exactHullVersionId)` or its same mapping. Lawful activity ship snapshots already carry `HullVersionId` (`CampaignActivitiesContracts.cs`); join exact ShipId+FleetId in the current frame. Never infer a hull from display name, faction, role or a substring of a design ID. Refined versions need an explicit validated art-family mapping if Backend provides one; unknown versions stay neutral/textual.

Borrow textures; preserve bytes/GUIDs/settings. Existing importers: sRGB, max512, no mipmaps, CPU unreadable, alpha-as-transparency, no NPOT scaling. Fit, do not crop nacelles. No duplicate imports or per-row live render targets; never destroy borrowed Resources. Constitution/Defiant proof thumbnails contain multiple views and are identity cues, **not newly accepted final standalone portraits**; retain selected 3D inspection.

Existing `ConstructionCardArt/ATTRIBUTION.txt`: Galaxy/fusion/research from Construction mockup; Constitution/Defiant from existing proof renders; **Defiant is owner-provided local-review art**. Preserve that restriction; no new distribution licence, recolor or palette/emission replacement is claimed. Images are referenced, not copied into new runtime resources.

## Original missing status glyphs

[icon-strokes.json](icon-strokes.json) and `icons/{ship,supply,shield,mission,survey}.svg`: five original 30×30 outline glyphs, 2-unit stroke, no fill, no font/emoji/bitmap. SVGs are authoring sources, not assumed runtime imports. JSON polylines are x,y float lists compatible with existing `ReviewIcon`'s `Line(params float[] xy)` / Painter2D pattern. Inherit resolved color, use `pickingMode=Ignore`, tooltip and adjacent real-state text. GUI copies the strokes into its owned icon source; no concurrent `ReviewIcon.cs` edit here and no SVG package/install.

| Key | Bind to | Unknown / safeguards |
| --- | --- | --- |
| `ship` | Generic vessel only when exact hull art unavailable | Label generic; never assert a class |
| `supply` | Actual Backend general supply field | Neutral outline + **Not reported**; ammunition is not general supply |
| `shield` | `frame.OwnShips` actual current/max shield sectors | **Not reported** if missing; do not substitute hull or imply zero/damage |
| `mission` | Accepted `frame.Travel.OwnOrders.Intent`, route or lawful active assignment | Existing accepted-state label; unsent drafts are not missions |
| `survey` | Actual reported exploration/survey capability | **Not reported** until known; no discovery/knowledge unlock |

Keep Fore/Port/Starboard/Aft in details. If summarizing a weakest sector, label it explicitly and require all four reported sectors. `Full · 4 sectors` only when all four known current values equal positive maxima. Missing/max-zero sectors are unavailable. No mission ETA/completion/reward inference from a glyph.

Reuse existing `ReviewIconKind.Construction` for Industry, `Weapons` for named combat outputs, `Energy` for Power, `Building` and discipline icons in Engineering. No new rating formula. LCARS amber/cyan/lavender accents do not replace status text. No animation required.

## Sol belt: no new art needed

At baseline `src/HobbyDS9.Simulation/SystemRichnessData.cs:29–30`, `Belts` excludes every `HomeCandidate`. Sol lacks an authoritative feature; previous `docs/system-belts-01-gui.md` explicitly expected absence in Sol. Backend must provide the authored Sol feature and explicit new/existing-save policy. GUI must not fabricate it. Preserve eight planets and no mining expansion.

All **11** current runtime assets already exist at `Resources/SystemBodyArt/AsteroidBeltV1/`: `Belt_Rock_{0..3}_LOD{0,1}.fbx`, `Belt_SoftAnnulus.fbx`, `BeltRock.shader`, `BeltSoftAnnulus.shader`. Current `ReviewSystemBelts` is wired. No new mesh/texture/import needed. Existing `BeltVisualSlot` maps `12 + 0.004*referenceMilliAu`; reconcile this with the new compressed Sol spacing. For an actual supplied main-belt feature, fit its full display envelope between Mars/Jupiter with minimum margins, keeping source/procedural readouts distinct from illustrative spacing.

Preserve lawful actor/turn/revision/system guards, feature/generation validation adapted only to Backend's explicit new contract, persisted seed/static scatter, 24° plane, no pointer interception, reduced motion, budgets/LOD/cache/disposal and resource fallback. No private-root or hidden-fact queries. Sol cosmetic permission does not itself reveal a belt or random-world facts. QA must observe actual default Sol visibility on the intended edition plus old-save behavior; source bindings do not close native acceptance.

## Evidence and open gates

Recovered images and all four new renders inspected. Isolated headless Chrome only; no Unity/native desktop/game player. JSON, SVG/XML, local links, resource/GUID/hash presence, script syntax, complete diff and retained main-only push/PR triggers checked. No gameplay tests needed for an art-only packet. Actual implementation, supported stat projections, keyboard/focus/hits/scrolling at 1280×720/1920×1080/ultrawide, hull appearance, controls/resource lifetime and Sol belt remain GUI/Backend/QA gates.
