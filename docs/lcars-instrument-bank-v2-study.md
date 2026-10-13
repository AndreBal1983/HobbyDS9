# LCARS instrument bank v2 — reference study and design

Research date: 2026-10-13. Starting point: draft PR #12, `6b899c6faaf59432a8b34a6bd3f430db4b648d5a`, stacked on PR #10. This is Unity presentation only; publication stays disabled. No main/tactical changes or automatic merge.

## Inspected references before implementation

All four original images in the supplied v2 ZIP were extracted and inspected, including the 120×96 elbow crop at native resolution. These are the primary acceptance direction. The crop shows the shell frame joining the navigation rail, not the component card. The three full captures show excessive card width and identical normalized heights. They are baseline evidence, not v2 acceptance.

| Visual reference (direct source) | Provenance and decision informed | Rights |
| --- | --- | --- |
| [Voyager shuttle console](https://www.gtjlcars.de/images/LCARSbilder/initiations058.jpg), [gallery](https://www.gtjlcars.de/LCARSindex/Lcarsbilder.htm) | On-screen production capture hosted by a fan. Paired narrow instruments and large dark separators motivate paired touch areas without a floating knob. | Protected production imagery; inspect only |
| [TNG wall console](https://www.gtjlcars.de/images/LCARSbilder/PDVD_023.jpg) | On-screen capture hosted by a fan. Continuous elbows and fixed outer grouping motivate repairing the shell's actual joint geometry. | No reuse grant |
| [Picard production graphic 1](https://www.pushing-pixels.org/wp-content/uploads/2023/04/picard-season2-c4d1.jpg), [artist interview](https://www.pushing-pixels.org/2023/04/30/the-art-and-craft-of-screen-graphics-interview-with-noah-schloss.html) | Credited production artwork, newer era rather than DS9 canon. Instrument clusters with adjacent numbers motivate shared totals above the bank. Omit translucent ornament. | Protected; inspect only |
| [Picard production graphic 2](https://www.pushing-pixels.org/wp-content/uploads/2023/04/picard-season2-c4d2.jpg) | Credited production artwork. Clear compartment boundaries motivate a separate selected editor instead of repeated explanations in every tile. | Protected; inspect only |
| [Fan dashboard screenshot](https://raw.githubusercontent.com/drwjkirkpatrick-web/lcars-web-design/main/docs/screenshots/lcars-dashboard.png), [project](https://github.com/drwjkirkpatrick-web/lcars-web-design) | Fan interpretation, not proof of canon mechanics. Flat amber/violet/blue, concise headings and elbow negative space motivate existing game tokens and original geometry. | MIT code; no code/art imported |

Additional research: [collector's TNG Ops panel photographs and production account](https://wrathofdhanprops.blogspot.com/2018/08/okudagrams-part-5-up-close-and-personal.html), [fan Voyager theme](https://www.thelcars.com/themes/voyager.html), [Unity 6 pointer events](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-Pointer-Events.html). Search included TNG/DS9/Voyager production panels and engineering controls. The five images above were actually inspected. No borrowed asset enters Unity or the demo. Screen geometry inspires the presentation; snapping, exact entry and accessible labels are functional game decisions, not claims about television controls.

## Three candidate layouts

```text
A / compact paired bank (chosen)
PROFILE / COMBAT        backend totals         SCALE: 2 bars / power
 Command     Launcher      Shields      Sensors     Energy ×2
   R A         R A           R A           R A         R A
              ##            ##                       ##
              ##            ##                       ##
   ##         ##            ##            ##         ##
   ##         ##            ##            ##         ##
 ─────────────────── common zero baseline ───────────────────
 R4 A3        R8 A6          R14 A14       R6 A6       R16 A12
 MAX4         MAX8           MAX14        MAX6        MAX16
 [selected component / exact R / exact A / backend facts + issues]

B / single stack with ghost requested target
 Command     Launcher    ...      [tap R / tap A mode]
   #|          #|                [selected exact fields]
```

B packs more devices, but hides which of two independent values is being edited and creates target/fill ambiguity. Reject for this milestone.

```text
C / categories with separate scale legends
 ESSENTIALS: 2 bars/power     WEAPONS: 1 bar/4 power
   R A   R A   R A             R A   R A   R A
```

C preserves small instruments alongside huge equipment, but makes cross-category heights incomparable. Reserve explicit grouping for future usability evidence; v2 uses one bank-wide scale instead.

A gives a visible R/A pair, a common zero baseline and exact fields only in the selected region. The tested tiles use 156 logical units, a 28-unit light width inside a 44-unit pointer column, and a five-unit bar pitch (four in compact/short layouts). Tiles wrap and the bank scrolls; header/profile totals, Back and Reset remain fixed. The selected editor scrolls independently below the bank or follows it inside the bank's scroll content in compact/short mode. Severe backend issues also appear as text at the tile/bank level.

## Scale and interaction contract

This is rendering arithmetic, not gameplay calculation. All ranges/minima/values and performance/validation originate from existing backend projections. No inferred EXCESS numeric metric is added: the current projection supplies available/requested and actual allocation; excess is shown through backend budget issues rather than reparsing display strings into a new authoritative number.

Normal bank maxima that fit the available instrument height use two segments per integer unit. Either half of a pair selects that whole unit, rounding upwards to the pair's value. The zero strip below the stack selects zero; dragging below the stack also selects zero. Keyboard arrows always step one actual integer unit. Pointer capture maintains scrubbing outside the column; release or capture loss commits the changed local value once. Detaching cancels an unfinished preview. Prospective values and pending text update while dragging, without backend compilation until commit.

At most 48 segments per track (32 in compact/short banks). Compact uses a 128-unit stack budget; standard uses 240. Short panels reduce the common segment limit, not individual capacities. Larger maxima use a single positive integral power-per-bar value obtained with overflow-safe ceiling division. Partial final bars and capacities smaller than one coarse bar carry an explicit approximate/break annotation; exact values remain visible. Extreme Int64 inputs do not create unbounded elements or lose typed values. Zero demand has no segments and a compact disabled zero control.

The shell currently draws a 64-unit elbow inside a 48-unit frame; its 25.6-unit top strip also disagrees with the structural bar. The fix belongs to the vector shell elbow and its sizing/join, with the navigation rail width supplied to that geometry. No overlay or scrolling-frame workaround.

## Acceptance status

Implementation, tests, rendered evidence and exact-head CI are recorded in the [acceptance document](lcars-instrument-bank-v2-acceptance.md). Prior native results do not count as v2 results. The owner resumed desktop input on 13 October 2026, then stopped it to join a meeting; remaining native navigation checks are explicitly pending. Physical touch, controller input and screen-reader interoperability require separate device testing; labels/tooltips and keyboard focus are retained.
