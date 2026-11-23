# Compact world and dense construction reference

19 November 2026. **Proposed reference, not live UI or native acceptance.** Exact base `fec332ee8af4e4c28054239c73b347dd0778fa4c`. No runtime/gameplay/resource/save/workflow edits. New owner brief governs; earlier no-mining scope is superseded for this batch. Backend/Architecture own real belt bodies, deposits, habitability, extraction, costs and save policy.

## Use immediately

- [Reference HTML](compact-reference.html): World pane, Dense construction and Central credits tabs; static controls, no commands. [World 1280](world-1280.png) / [1920](world-1920.png); [Construction 1280](production-1280.png) / [1920](production-1920.png); [Credits 1280](credits-1280.png) / [1920](credits-1920.png). Actual headless-browser renders, not Unity screenshots. Values/names/availability are owner samples or illustrative fixture data, not a source for live values.
- [Asset bindings](asset-bindings.json); [four new original world/body glyphs](world-icon-strokes.json). No new planet, belt, ship or installation texture/mesh. No runtime import or SVG package. Copy stroke polylines into GUI-owned Painter2D helpers; Art changes no live helper/UI source.
- [Central attribution text](central-credits.txt), based on existing `Review/Resources/SystemBodyArt/ATTRIBUTION.txt`. Preserve source records and links; remove only the portrait's local disclosure.
- [Unchanged owner screenshots/provenance](recovered/provenance.json). These are the rejected baseline layouts, not approval of their typography/cards.

## World pane: Engineering visual language

Use the existing Engineering cell treatment (`PlayerConsole.uss`): dark `#101b29`, lavender `#b9ace6` edge/labels, readable 20–24px values, 14–16px labels. Numeric Industry stays numeric. Do not invent capacity maxima, percent gauges or balances from absent facts. Existing capacity blocks are appropriate only when the actual quantity and meaningful limit are supplied.

Hierarchy: **identity/type/settlement chips → Population / Industry per week / Research per week → Food produced/consumed/net/storage → queue strip → Environment / Resources / Survey disclosures → unique actions**. The reference facts component is about400px high at1280, beside the existing planet portrait; no repeated Earth/temperate/settlement sentences. Manage colony and valid survey/local-ship actions remain reachable. Secondary condition text goes in details/tooltips, not another primary paragraph.

Scopes are separate: owned economy facts belong to the current supplied observation; control has its own observed week; initial deposits have **no observation date/current remaining stock** in this baseline. Preserve those distinctions. Do not stamp an entire pane “current” based on one field. A retained control/environment record does not prove current economy or settlement status. No owned colony row does not prove uncolonized. Unknown/null demand, balance, resources, survey or ownership shows Not reported/Unknown, never zero. Use lawful civilization display names or neutral control wording; no `civ.*` identifiers.

The lower retained-record strip and explanation are **reference-only scope examples**, not an extra panel/prose block to copy into every world screen. Survey wording “No completed record” mirrors source semantics; it does not assert a failed/incomplete survey. Existing in-progress survey with unknown total must not gain a percentage/ETA.

## Dense construction: actual generated-campaign route

**Target `ReviewColonisationPanel.Construction` at baseline lines218–285**, which still creates 48%-width cards and a long `ProductionComparisonPresentation.Summary`. GUI has since extracted this into its owned partial; apply there. Do not stop at a different/alternate `ReviewConstructionCatalogue` page.

Suggested **48px single rows**, 44×44px Inspect/`+` targets, no per-row paragraphs. One row per exact entry for **both ships and installations**, not a two-column card grid. Primary name16–17px, values14–16px; align numeric columns and headings. Wrap long names within two short lines or expose the full name through focus/tooltip. No horizontal scroll/clipped actions; optional columns move to Inspect if necessary.

Ships: icon/name+revision, Industry, Study work, Contact, Hull, Shields, materials, owned/queued, Inspect/`+`. All are named fields from the frozen `selected.Comparison`; they are not live ship condition or a composite exploration/combat rating. Armor, speed, movement and actual weapon packages/powered state stay in selected Inspect. Missing comparison is Not reported, not zero/Unarmed. Reference four-ship values follow the owner's baseline screenshot, not a new formula.

Installations: icon/name, numeric Industry, **base** named output, power demand, materials, built/queued, Inspect/`+`. Base benefits already exist through `observation.BuildingBenefits`; actual output still depends on power/politics/deposits. Conditions and extraction details belong in Inspect. The fixture uses only the three baseline buildable installations (80/60/40Industry, not their old credit fields), plus the actual settlement-kit recipe (60Industry/Dilithium6). It adds no mine option, reward, credit charge or unsupported choice. Example counts are illustrative; built/queued uniqueness disables the example Research laboratory addition.

Keep current four category sections, direct validated `+`, separate selected-world FIFO/queue and catalogue scrolling, cancel consequences, source-frame/world/dirty/busy guards and exact published revision. No new confirmation, buyout/reorder or pay-on-enqueue semantics. Reference has eight real catalogue-entry kinds (4ships/3installations/kit), no filler: DOM checks find7fully visible at1280×720 (all4ships/all3installations; kit scrolls) and8at1920×1080. [Browser density/geometry evidence](reference-metrics.json) is **not** a native same-viewport acceptance comparison; owner's Production image is a cropped1865×682 region rather than a full-window measurement. QA must compare the real generated route at identical viewport sizes.

## Reuse assets rather than regenerate

Ships deliberately have no proof thumbnail in the actual baseline route: old Constitution/Defiant pixels resemble exploded multi-view parts. Use the existing original `ship` vector from `art/playtest-20261118/icon-strokes.json` as a generic row cue, with the exact class name. Do not resurrect those pixels as a hero or infer an art family from a name/faction/role. Selected Inspect continues through `ActivityHullCatalog.Find(exactHullVersionId)` and `ShipPreviewStage.ShowActivityHull`; the specialized Galaxy resolver remains intact.

| Exact selected hull | Existing inspector route |
| --- | --- |
| `hull.constitution-explorer.v1` | `ArtDemo/ActivityHulls/Constitution/Models/ConstitutionExplorer_LOD0` |
| `hull.galaxy-cruiser.v1` | Existing classic Galaxy branch of `ActivityHullModel.Create`; catalog ResourcePath is intentionally null, not a missing generic Resources path |
| `hull.defiant-combat.v1` | `ArtDemo/ActivityHulls/Defiant/Models/Defiant_LOD0`; preserve owner-provided local-review restriction |

Existing 24–30px installation images: `infrastructure.fabrication-annex.v1` → `ConstructionCardArt/fusion`; `infrastructure.research-laboratory.v1` → `ConstructionCardArt/research`; `infrastructure.hydroponic-extension.v1` → `ConstructionCardArt/hydroponics`; supported kit → `ConstructionCardArt/kit`. Preserve GUID/bytes/importers: sRGB/max512/no mipmaps/CPU unreadable. Borrow textures; no duplicated imports or render target per row. Full selected ship inspector keeps its current180px exterior view.

New original30-unit population/food/queue/asteroid-body outlines supplement existing ship/survey and ReviewIcon Construction/Computing/Building/Energy strokes. JSON lists each separately stroked x,y polyline, width2, no fill; inherited color, labels/tooltips and pointer-ignored. SVGs are authoring sources only. The asteroid glyph depicts **one aggregate body**, not individually selectable renderer rocks. It supplies no habitability/deposit/ownership/eligibility fact.

## Portrait and central credits

Use the existing lit/textured 3D globe/RenderTexture. Rotate the actual globe, **not its Image element or a flat map**. Suggested art starting speed2°/s (one revolution3minutes), yaw drag0.3° per logical pixel and gentle pitch with a safe clamp; GUI may tune. Pause auto during captured drag, then resume without snapping after release; keep current orientation. Respect reduced motion (no auto-spin), app/panel focus/visibility, pointer cancel/lost capture and disposal. Limit manipulation to the portrait, suppress click-through/navigation/orders, avoid stealing parent scroll. No campaign RNG/turns/commands or resource reloads for rotation. Keyboard-accessible Pause/Reset is local view state.

The reference shows unchanged owner portrait pixels through a **CSS viewport crop solely to illustrate placement**. It is not a new asset, relighting, runtime bitmap rotation or proof of native interaction. Existing textures/lighting stay unchanged. The local credits Foldout at `ReviewPlanetPortrait.cs:24–25` goes away. Use existing `ReviewEntry.Help.DrawPlayerHelp` → **Help & credits / Visual & audio credits** instead.

Central source must cover **all9** licensed maps: Sun, Mercury, Venus Atmosphere, Earth Day Map, Mars, Jupiter, Saturn, Uranus, Neptune — **Solar System Scope / INOVE, CC BY4.0**, source and licence URLs, source-copy/import/schematic changes. Current central Earth/Mars-only wording is incomplete. Keep `SystemBodyArt/ATTRIBUTION.txt` unchanged, and preserve other received ship/local-review and music/audio credits. No new legal/distribution claim is made by this local reference.

## Belt-body integration and open gates

The new request supersedes previous no-mining scope. Backend/Architecture supply selectable aggregate identity, lawful discovery/survey/resource/habitability facts, extraction cost/work/depletion/stock transfer, settlement constraints and explicit save policy. **No art-defined habitability percentage, fake planet façade or cosmetic mining action.** Use the supplied body kind and units, no numerical balance canon here. Unknown resources/habitability remain unknown. Existing Sol belt meshes, shaders, static seeds/LOD/disposal and body order stay; decorative rock instances do not become campaign entities. No regeneration of existing planet/belt assets.

Source refs/JSON/SVG/links/assets/CI triggers and complete diff are checked; renders inspected at1280×720 and1920×1080. Headless local browser only, no Unity/native desktop. Gameplay tests unnecessary for this art-only package. Native rotation/reduced motion/capture/focus, compact actual-route world/ship/installation UI, readable controls/scroll and a same-viewport density comparison, central credits accessibility, belt gameplay/observation/extraction/save path, Engineering/Production/navigation/Quit regressions remain GUI/Backend/QA gates.
