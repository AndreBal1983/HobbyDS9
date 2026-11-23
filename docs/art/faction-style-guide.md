# Faction frontend, ship and audio style guide v1

22–23 November 2026. **Consolidated source audit and proposed design; no default theme activation or new playable faction.** Work order `2584f27`; current-player evidence pin **`2fc0dbca46073121379dd9491c859a02a6f22dcd`**. The work-order branch has older source ancestry: missing newer art in that tree is not evidence that it was removed. Historical framing proposals are pinned separately at `b3496b6b1da31634d9beb8a7357b2480f37a9a72`.

This guide covers every recovered catalogue polity and explicitly documented console family. It does not expand the faction roster to every franchise species. Federation LCARS is **one family**. Shared readability, navigation and truthful state semantics remain consistent while the framing, materials and audio vocabulary differ.

## Coverage and status

| Group | Actual identity | Current status | This guide |
| --- | --- | --- | --- |
| Federation | `civilization.federation` | Runtime definition; current generated player | Source-backed LCARS/ship family; opt-in refinements proposed |
| Cardassian Union | `civilization.cardassian` | Runtime definition; current generated opponent | Existing original ship prototype/material evidence; full interface/audio proposal |
| Dominion | `civilization.dominion` | Runtime definition in authored scenarios; not current two-empire selection | Documented interlocked-rib interface proposal; missing reviewed ship/audio family |
| Klingon | No civilization definition recovered | Explicit documented console family; `species.klingon` is a species | Proposed stepped/angular interface, ship/livery/audio direction; no invented runtime ID |
| Romulan | No civilization definition recovered | Explicit documented console family | Proposed swept/open interface, ship/livery/audio direction; no invented runtime ID |
| Ilex Survey Commune | `minor.ilex.v1` | Actual independent minor-society definition, not an empire | Neutral-family variant; media proposal, no inferred survey/warp bonus |
| Namar Local Council | `minor.namar.v1` | Actual independent minor-society definition, not an empire | Neutral-family variant; generic lawful craft marker until actual bindings exist |
| Neutral / unknown / custom | No new faction | Presentation fallback | Plain readable family; explicit custom mapping required |

[Exact audit, source paths and hashes](../../art/faction-style-v1/catalogue-audit.json). Core species are Human, Vulcan, Andorian, Klingon, Cardassian, Bajoran, Ferengi and Founder; activities add Ilex/Namar. These records do not automatically create empire themes or playable choices. Borg-like actors in design examples are not a registered catalogue/theme family; Breen was not found as a catalogue/theme-selector entry. Bajor/Klingon music filenames are media evidence, not faction registration. The unreceived `E:/HobbyDS9/docs/art/faction-interfaces.md` and music-direction reference remain missing; their detailed original palettes/fonts/music cannot be called recovered approvals.

## Common contract, distinct families

Keep task/navigation order, selected-vs-draft-vs-accepted meaning, known/stale/unknown scope, inspector placement and explicit primary/destructive actions consistent. A themed border never changes a command, cost, eligibility, AI personality, authorisation, save format or database mode. Decoration must not hide action labels, shrink hit targets, change keyboard traversal or move a destructive action into the primary action's place.

Theme selection and identity disclosure are different. A local interface theme may be a user preference, including a reference preview. It does **not** change the player's civilization or recolor every ship. An observed ship's model/livery/icon still requires its own lawful revealed identity and exact asset mapping. Unknown contacts remain neutral/generic even if a trusted save/private actor ID would reveal them. No hidden faction tint, sound, music change, silhouette or registry hint. Internet actor-filtered replication must retain this same boundary.

Use the project sans font for all body text, numbers and commands; initial tokens use body18/secondary16/numeric22/title28, 24px line height. Header treatment can vary in weight/spacing, not readability. Alien/ornamental characters are nonessential decoration only, never the sole label or an unreceived font dependency. Use localised/tabular numeric formatting without changing the underlying values. Review at **1920×1080**; smaller windows are best-effort, not a new acceptance matrix.

### Shared states and accessibility

The [token contract](../../art/faction-style-v1/theme-tokens.json) keeps available/mint, active/amber, completed/lavender, warning/gold, rejected/coral, information/blue and unknown/unavailable/neutral consistent. Each also has a text label and owned shape; hue is never the only signal. State colors are separate from faction brand colors, even when an accent is similar. “Not reported” is not zero, damage or rejection. Unavailable retains a reason; accepted audio/visual feedback occurs only after an actual accepted operation, not on click.

Initial static targets are at least4.5:1 for ordinary text and3:1 for necessary non-text indicators against adjacent backgrounds, following [W3C text contrast](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html) and [non-text contrast](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html). Do not rely on [color alone](https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html). Project controls retain44px logical targets and a visible offset focus ring; this44px value is the project convention, not a blanket WCAG claim. [Measured sRGB token pairs](../../art/faction-style-v1/contrast-checks.json) cover all profiles/surfaces, shared states, focus/borders and dark text on brand fills. These measurements do not certify the whole native UI, thin-line rendering, overlays, images or disabled controls.

No compulsory flash, glow pulse, scan-line noise, shake or stereo/pitch surprise. Reduced motion removes nonessential animation/automatic rotation and transitions; focus/hover/passive AI updates remain quiet. Keep separate music/SFX gain and mute; meaningful alerts have text/visual equivalents. A future high-contrast override can reduce chrome while retaining identity labels; it is proposed, not an already implemented setting.

## Family direction

All newly chosen palette hexes, geometry refinements, ship studies and sound briefs below are **proposals**. Historical source-backed framing does not imply approval of a finished skin. [Full comparative matrix](../../art/faction-style-v1/comparison-matrix.md) and [structured art/audio directions](../../art/faction-style-v1/art-audio-directions.json) carry the same coverage.

| Family | Interface grammar | Palette / light |
| --- | --- | --- |
| Federation | Asymmetric elbow rail, rounded segmented section ends and calm central hierarchy | Amber `#EFB16B`, lavender `#B9ACE6`, dark blue-black; actual received materials retain separate pearl/slate/bronze/red/blue roles |
| Klingon | Stepped angular brackets, chamfered outer marks and restrained stacked accents; rectangular44px control hit areas remain | Warm red `#EAA294` and pale brass `#D6C291` over charcoal; no all-red warning screen |
| Cardassian | Layered curved junctions, nested lens-like edges and broad arced housing around a straight readable grid | Ochre/gold `#D6B16D`, pale muted green `#B7C7A8`, brown-black; source ship plate/inset roles stay distinct |
| Romulan | Swept/open junctions, paired thin spines and sparse long directional divisions | Jade `#A6D5B3`, pale gold `#D6C391`, deep green-black; no tiny green text or opaque sensor clutter |
| Dominion | Symmetric interlocked ribs and faceted section ends; repeated structural rhythm | Violet `#C7B0ED`, cool silver `#B4C6D9`, graphite-violet; purple brand is not a hostile/error signal |
| Ilex / neutral | Plain rectilinear civic/utility scaffold; sparse aqua identifiers | Aqua `#9CD3D7`, mineral pale `#CAD2B5`; no name-derived technology/rule claim |
| Namar / neutral | Same clear civic scaffold, restrained warm identifiers rather than an invented “primitive” UI | Mineral `#D1C49F`, slate-blue `#B6C9D6`; no inferred capability tier |
| Unknown / custom | Low-chrome rectilinear scaffold and explicit generic identity | Slate `#B8C8DB` and muted lavender `#C2BDCF`; no hidden faction reveal |

These are not LCARS recolors: only Federation uses its elbow/segmented-rail grammar. Other families alter outer framing/junctions, section motifs, marks and material/audio direction while keeping the same meaningful UI hierarchy. Decorative cuts/curves sit outside the control's accessible rectangle. Do not clip text or focus rings to imitate a bevel.

## Ships, materials, livery and lighting

**Federation:** preserve received exact classes. Saucer/secondary hull/nacelle separation is one family cue, not a rule forcing compact escorts into that silhouette. Current pearl/slate/silver/bronze/recess materials and local red/amber/blue/ice emission roles are source evidence. Keep registry/identification zones readable, dark recesses controlled and full hull bounds visible; do not tint every material amber.

**Cardassian:** the original cruiser prototype supplies broad shoulders/long spine, ochre hull, plate-gold accents, dark inset and red-brown secondary roles. It is not final owner art or a newly cleared canon model. Proposed new classes can keep layered curved armor and restrained amber drive/ice windows without copying a Federation layout.

**Klingon proposal:** broad swept planes, compact central mass and forward emphasis; gunmetal/olive panels, warm-red identification masks and subordinate wear. **Romulan proposal:** swept/open wings, central voids and layered planes; jade/graphite with localized pale-green/gold lighting. **Dominion proposal:** compact faceted central volumes, paired swept elements and repeated modular plates; graphite/violet/bronze with small identity masks. No reviewed runtime family binding was recovered for these three; the guide's vector shapes are original grammar studies, not delivered classes, equipment or hardpoints.

**Ilex/Namar:** no exact ship binding recovered. Generic lawful markers are correct until real craft are authored. Utility-volume studies may follow actual content needs; names, polity type and visual rusticity do not imply warp/study/combat ability. Unknown/custom contacts remain generic regardless of any private identity.

Keep a neutral calibration light/exposure for cross-family material review before any cinematic tint. Family UI colors are screen-space choices, **not** conversions of shader base-color tuples. For example, received `FED_Hull_Pearl` stores base(.51,.58,.65)/metallic.35/smoothness.64; `FED_Panel_Slate` stores(.29,.36,.44)/.45/.60; `FED_Phaser_Bronze` stores(.28,.20,.105)/.65/.66. Cardassian manifest tuples include ochre(.52,.34,.16), plate gold(.73,.55,.29) and inset(.16,.115,.065). Preserve source interpretation; this guide does not replace their shaders, textures, roughness or emission. New material tuning is a later measured study.

Render silhouettes at multiple useful inspection sizes, preserve negative space and full bounds, and keep livery readable without overbright bloom. Emission is decoration unless an actual supplied gameplay field drives a labelled effect; glow does not prove a powered reactor, weapon or shield. Weapon/class identification and hidden fleet information must not be inferred from art. Keep LOD/material/draw/cached-render-target budgets and borrowed-vs-owned resource lifetimes explicit. No new mesh/texture production tonight.

### Existing exact asset and provenance boundary

[Media inventory](../../art/faction-style-v1/media-inventory.json) pins current source paths, hashes, GUIDs and limits. The exact Federation source-cruiser pair maps through `ShipVisualCatalog`; current Constitution/Galaxy/Defiant inspection uses exact hull versions. Galaxy keeps its special resolver and **CC BY-NC-SA3.0** notices; Defiant's owner-provided source licence/task details remain unreceived and local-review limitations stay. Original Constitution-style geometry is project work, not an exact canon claim. Existing Cardassian original prototype maps only through the reviewed exact/permitted-visible paths in `ReviewBattleStage`. Do not add a generic faction-to-ship fallback.

No original media byte was changed or copied into this packet. Do not treat an art-library entry, theme choice, music filename or species record as a gameplay availability mapping. Missing `faction-interfaces.md`/music-direction files and any unreceived approved replacement palette are morning source-recovery items, not grounds to invent approval or block this independent guide.

## Sound, music and UI cues

Every family shares semantic roles: deliberate press, actual accepted result, actual rejection, real processing, deliberate navigation/search, lawful alert and music context. Keep the meaning, caption/reason, bounded repetition and mute behavior identical. Hover, passive refresh, AI activity and merely inspecting a hidden/unknown actor are silent. No fake processing delay. Do not emit “success” before authoritative acceptance, including later internet play. No sound-only warning or automatic reveal of faction/private events.

- Federation direction: soft rounded electronic attack, short resolving accepted pair, restrained orchestral/horn/string exploration bed.
- Klingon proposal: dry tactile/resonant cue; low brass and sparse measured percussion, without sampled war cries or copied themes.
- Cardassian proposal: low rounded pulse and compact rejection; low strings/bass clarinet/bassoon with space between phrases.
- Romulan proposal: clear damped/plucked tone; strings/woodwind and restrained texture, without whisper cues implying hidden activity.
- Dominion proposal: tightly grouped synthetic notes with a soft body; low strings/repetition that never becomes a continuous alarm.
- Ilex/Namar proposals: quiet two-tone/airy utility or dry warm civic cues, respectively; no dedicated existing set is claimed.
- Neutral/custom: existing runtime behavior or explicitly configured neutral cue/silence; never relabel a Federation recording as authentic delivered audio for another faction.

The exact current music catalogue has **nine** tracks: Deep Station, Deep Station track1, Quiet Frontier, Across the Stars, Secrets in the Courtyard, Bajor1/2, Cardassia1 and Klingon1. Categories are presentation contexts, not automatic faction assignments. Existing six-second crossfade settings and individual tail hints are recorded, not newly auditioned seamless-loop evidence. A Cardassian/Bajor/Klingon filename alone must not switch playback or disclose a hidden encounter.

Six existing Federation WAVs map buttonPress/success/error/processing/screenOpen/search. Their user-supplied franchise provenance has unverified direct retrieval details and unclear redistribution permission; they are prototype evidence, not a royalty-free universal library. Generated music records and owner-folder additions have different evidence: three ElevenLabs source records, two owner-supplied Suno-origin cues, four later owner-folder cues with unsupplied creator/rights detail. Preserve each record; do not make a new legal/public-release assurance.

New faction cue packs, composition, loudness/transition audition and any commissioning remain the [scoped backlog](../../art/faction-style-v1/backlog.json). No purchases, synthesis, cloned voices, media downloads, local-generator service reinstalls or automated Suno work tonight. The reference gallery is **silent** and changes no current audio catalogues.

## Reusable tokens and samples

[Schema v1](../../art/faction-style-v1/SCHEMA.md), [theme tokens](../../art/faction-style-v1/theme-tokens.json) and [silent sample gallery](../../art/faction-style-v1/samples.html). The five profile color roles are background, surface, raised, brandPrimary and brandSecondary. Civilizations and minor IDs have separate arrays; planned styles have empty civilization arrays. Unknown IDs map to neutral. No JSON package, new font, script/URL/media loader or default-theme switch is requested. GUI owns any add-only, provider-free binding/sample and validates data bounds; storage A2/C1 files remain separate.

The same fixture observations appear in every sample so reviewers compare family chrome, not different statistics or assumed racial advantages. Decorative mark/silhouette SVGs are original proposals, not copied franchise logos, production ships, map emblems or observed opponent identities. A local theme preference never changes those identities. Inspect full1920samples and the matrix; smaller-window captures are not new gates. Sample/native equivalence, UI Toolkit clipping/focus, rendered contrast, actual resource lifetime and audio listening remain unaccepted until measured in their own receiving scope.

## Scoped backlog and morning decisions

P0: exact lookup/privacy/state/accessibility review and source/rights receipts. P1: opt-in component sheets, small original missing-family silhouette/material studies, semantic cue briefs and bounded music audition. P2: minor/custom content validation and native family rollout. Each backlog row records owner and acceptance, not a gameplay roadmap or permission to bulk-generate assets.

Morning owner decisions are recorded, **not asked overnight**: any faction expansion beyond this audited union; recovery of missing detailed references; future original ship/cue commissioning and usable provenance; a later opt-in theme rollout. None blocks current database/UX phases. No main merge, database default activation, public hosting/ports or overnight purchases are implied. Architecture owns any index integration and multiplayer scope changes; this guide edits neither shared index nor architecture file.
