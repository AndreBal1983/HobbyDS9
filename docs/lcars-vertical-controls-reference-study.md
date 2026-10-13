# Vertical LCARS controls — reference study

Status: presentation refinement of draft [PR #10](https://github.com/AndreBal1983/HobbyDS9/pull/10), starting head `967f63723000b3391e2cc61823eb35594b905d52`; fetched main `5beba2d652d283a2efb2f3ef44ddd5df9e38815d`. No gameplay or campaign publication change. Research completed before implementation on **2026-10-13, Europe/Amsterdam**.

## Visited sources

All entries below were opened on the access date above. Image inspection is distinguished from reading project documentation. These are observations and design inferences, not evidence that our new game controls are screen canon.

| Source / exact URL | Category / inspection | Detail used | Usability drawback | Licensing / reuse |
|---|---|---|---|---|
| [GTJ LCARS gallery](https://www.gtjlcars.de/LCARSindex/Lcarsbilder.htm), [Voyager shuttle detail](https://www.gtjlcars.de/images/LCARSbilder/initiations058.jpg), [TNG display](https://www.gtjlcars.de/images/LCARSbilder/PDVD_023.jpg) | On-screen captures, hosted by a fan; both images inspected | Tall paired instrument groups; violet/orange framing; adjacent numbers; separated black compartments | Tiny legends and decorative data unsuitable for precise game editing | Gallery acknowledges extracted and externally collected images; Paramount/CBS rights, no reuse grant. Observed only; no image shipped |
| [Noah Schloss production interview](https://www.pushing-pixels.org/2023/04/30/the-art-and-craft-of-screen-graphics-interview-with-noah-schloss.html), [credited Picard graphic](https://www.pushing-pixels.org/wp-content/uploads/2023/04/picard-season2-c4d1.jpg) | Genuine production graphics, designer interview; image inspected | Instrument clusters around content, thin boundaries and clear dark separation | Dense translucent scientific decoration would obscure an editable draft | Production artwork remains protected; observed only |
| [Official PADD launch](https://www.startrek.com/videos/star-trek-padd-app-available-today) | Official licensed recreation / authorship context; article read | Okudas involved in a functional LCARS application; task-based interaction matters | A licensed mobile database is not evidence for vertical power-control mechanics | CBS/Paramount; article only, no assets or quotations copied |
| [louh/lcars](https://github.com/louh/lcars) | Fan implementation; README and license read | Responsive framing and reduction of ornament on small screens | Web grid implementation cannot directly transfer to UI Toolkit | GPL-3.0; no code/assets imported |
| [LCARS web design](https://github.com/drwjkirkpatrick-web/lcars-web-design), [dashboard image](https://raw.githubusercontent.com/drwjkirkpatrick-web/lcars-web-design/main/docs/screenshots/lcars-dashboard.png) | Fan recreation; README/license and dashboard image inspected | Flat blue/violet/gold, condensed labels, rounded elbow, numeric facts next to graphs | Large decorative sidebar and tiny chart labels consume space | MIT code; branded material may have separate rights. No code/image imported; its canon claim is not independently endorsed |
| [mttaggart/lcars-css](https://github.com/mttaggart/lcars-css) | Fan implementation; README/license read | Explicit balance between visual style and efficient space | Archived project and web dependencies; not a Unity control | MIT code, LCARS attribution to CBS. Observed only |
| [joernweissenborn/lcars](https://github.com/joernweissenborn/lcars) | Fan implementation; README/license read | Consistent panel nesting and reusable geometry | Styling framework alone does not establish usable interactions | MIT code; no source imported |
| [TheLCARS theme gallery](https://www.thelcars.com/themes/), [Voyager theme](https://www.thelcars.com/themes/voyager.html) | Fan recreation; accessible pages/labels read | Repeated section bands and restrained grouped navigation | Extensive decorative number sequences; not power sliders | Site copyright and CBS trademarks; no reuse grant. No assets shipped |
| [Unity 6 SliderInt API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/UIElements.SliderInt.html) | Technical primary documentation | Real vertical integer slider; non-inverted vertical high end is at the top | SliderInt cannot retain an Int64 outside its range; stock appearance needs scoped styling | Documentation reference only. Compile/test against installed **6000.6.5f1**, not an assumed API version |

This exceeds eight substantive sources across six domains. Search covered TNG/DS9/Voyager panels, engineering distribution, segmented histograms, Okuda palettes, modern fan controls and Unity orientation. The LCARS.org.uk TNG/Voyager pages timed out; Unity's proposed docs.unity.com manual URL was inaccessible; ADG Picard PDFs were rejected by the web reader (one exceeded its content limit). These failed opens are **not counted**. `lcars.computer` returned only a JavaScript shell and is not counted as an inspected design. The in-app browser failed to initialise with “failed to write kernel assets”; relevant public images were instead inspected directly from curated URLs. No broad crawler or browser bypass was used.

## Supplied references and observed motifs

Both ZIP images were extracted into ignored research artifacts and inspected. Their provenance/reuse rights are unspecified: user-supplied visual direction, not permission to ship copies. No external reference image enters Unity Assets.

1. **Paired upright columns:** supplied phone panel and Voyager shuttle detail use narrow adjacent instrument groups; Requested/Allocated should share height and scale.
2. **Bottom-up fill with dark gaps:** supplied phone panel has distinct energized segments and dark unused segments. Its amber is at the bottom and green at the top; we deliberately adapt the palette and progression below rather than claiming an exact copy.
3. **Black separation:** both supplied images, classic screen captures and modern dashboard leave black channels between instruments. Use gaps rather than shadows or gradients.
4. **Rounded elbow and hard section edges:** supplied physical panel and Voyager capture group small facts inside larger curved framing. Use original asymmetric rounded bank borders, not two giant independent framed graphs.
5. **Numbers near instruments:** Voyager capture and fan dashboard put values and legends next to data. Exact inputs and powered minimum/full demand stay beside each rail, not in a distant inspector.
6. **Limited palette and flat fill:** classic captures and fan dashboard use violet, cool blue and amber. Keep existing game tokens; no imported font or neon rainbow.
7. **Hierarchy over decoration:** TNG display has broad grouping but extremely small legends. Preserve its grouping while increasing legend sizes and omitting arbitrary numbers.
8. **Separate summary from controls:** production graphic separates its main subject from surrounding instrumentation. Profile totals remain a distinct backend readout above the engineering bank.

## Three alternatives, before coding

These rough wireframes describe original layouts; vertical `#` marks indicate active bottom segments. Numbers and validation text remain independent of color.

### 1. Compact paired pillars — chosen, in a wrapping bank

```text
PROFILE TOTALS: available / requested / allocated [backend]
╭ Component A × 2 ───────────╮  ╭ Component B × 1 ───────────╮
│ REQUESTED     ALLOCATED   │  │ REQUESTED     ALLOCATED   │
│ .  full 16    .  full 16  │  │ .  full 4     .  full 4   │
│ #  min 8      .  min 8    │  │ #  min 2      #  min 2    │
│ #  exact[12]  #  exact[8] │  │ #  exact[3]   #  exact[2] │
│ #  0 units    #  0 units  │  │ #  0 units    #  0 units  │
│ backend output / errors  │  │ backend output / errors  │
╰──────────────────────────╯  ╰──────────────────────────╯
```

Equal-height controls make differences visible without subtraction. Cards wrap with available width and scroll as a bank. Narrow presentation shortens rails while keeping real vertical controls and adjacent exact fields. Many installations use multiple bounded columns rather than a single extremely long row list. Long backend errors expand a card; scrolling remains necessary on 720p.

### 2. Per-component meters with a numeric column

```text
component   requested allocated | exact R / A | backend facts
A           [#]       [#]       | [12] [8]    | min8/full16
B           [#]       [.]       | [3]  [0]    | min2/full4
C           [.]       [.]       | [0]  [0]    | errors...
```

Good alignment and predictable rows, but consumes vertical space for every installation; numeric columns separate from meters on narrow screens. This repeats the old long-list problem.

### 3. Grouped bank with a master budget rail

```text
[BUDGET] | propulsion [R][A] | weapons [R][A] | support [R][A]
  [##]   | exact fields     | exact fields  | exact fields
```

Strong console composition at ultrawide sizes, but category membership and a second master scale complicate variable component lists. A draggable master rail could imply new distribution rules. Keep the budget as explicit detached totals, not another adjustable control.

## Implementation decision

Choose **compact paired pillars inside a wrapping engineering bank**, with a separate profile totals panel. The supplied segmented columns motivate bottom-up discrete fill; Voyager's paired groups motivate equal scales with adjacent numbers; the fan dashboard's flat blue/violet/gold motivates existing game tokens and original rounded framing. Louh's responsive approach motivates reducing ornament/rail height on narrow screens while retaining exact entry.

Use cool blue in lower segments, lavender in middle segments, amber in upper segments. This adapts the supplied green/yellow direction to established DS9 styling, with warmer colors at higher utilization as requested; it deliberately differs from the supplied phone's palette ordering. **Position is not validity.** A backend error adds a red border and explicit text, while the meter keeps utilization colors. Negative/over-range exact Int64 entries remain visible and in the draft; only the visible thumb is bounded. Zero/full/powered-minimum labels are facts from existing Application projections; Unity neither derives legality nor distributes power.

Review must include actual native direction, repeated single-unit keys, focus after refresh, release-only commits, exact invalid values, profile retention, scroll reachability and all three resolutions. Mouse and keyboard are the supported acceptance inputs; controller and physical touch are outside the tested scope. No new publication, construction, combat or save behavior is included. See [campaign integration follow-up](unity-ship-designer-campaign-blueprint-integration.md).
