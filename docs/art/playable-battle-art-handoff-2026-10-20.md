# Playable battle art handoff /20 October2026

Scope: the owner's [playable frontend assignment](E:/HobbyDS9/docs/playable-frontend-milestone-2026-10-20.md), using existing admitted classes. Art owns source/export files and this storyboard; Secondary owns Unity imports, prefabs, scenes, camera/effects runtime and native acceptance. Primary owns actor-safe event and gameplay contracts. No runtime, scenario, save, catalogue admission, balance or R8 executable changed here. All work used CPU geometry/export/inspection; no Blender, Unity, GPU, paid generation or resource lease was used.

The updated interface source is **74193131bc6b22eda3fcc4e7da35876f8e38f227**, PR68, separate branch `codex/map-selection-prototype`. Current [combined design review](E:/HobbyDS9/artifacts/art/owner-interface-revision-20261020-v2-1/map-interaction-review.html) includes the owner's whole-percent Science correction. Its JS/CSS is design guidance, not a Unity runtime/data source. Keep the accepted three-column battle hierarchy and explicit per-round review/commit boundary.

## Actual classes and assets

The [machine-readable mapping](../../art/ships/playable-class-visuals-v1.json) uses **exact civilisation-definition + ship-design-definition pairs**, never actor-ID/name/role/statistics guesses.

| Existing scenario pair | Visual | Status |
| --- | --- | --- |
| `civilization.federation` / `ship-design.source-cruiser` | Existing original exploration cruiser; three tracked FBX LODs and prefab | Existing exact `ShipVisualCatalog.Resolve` binding at e3e7621; reuse |
| `civilization.cardassian` / `ship-design.travel-target` | New original Cardassian-style broad-shoulder, long-spine cruiser prototype | Proposed receiving binding; source/export complete, native import and final art acceptance still open |

The scenario initially instantiates `ship.equipment-cruiser` and `ship.cardassian-scout`, both with220 hull. The latter's entity name is historical; its **design is travel-target**, not permission to bind the generic Scout. The existing Scout/Advanced Scout catalogue entries do not establish supported v2 construction; Engineering maturity does not change that. Primary must supply any additional admitted class contract before art bindings expand. Patrol frigate and compact escort remain reusable unbound art. The textured Galaxy candidate remains separate and unbound.

Federation assets already live under `client/HobbyDS9.Unity/Assets/HobbyDS9/ArtDemo/Resources/ArtDemo/Federation/` at the milestone base. No import or duplicate asset tree is needed merely to reuse them. The original editable Blender sources and GLBs remain in [the v1 delivery](E:/HobbyDS9/artifacts/art/federation-fleet-v1). Existing LOD counts are7,700 /3,542 /2,004 triangles. Their initial Unity import tests are historical; the new battle renderer still needs its own material, orientation, framing, scale, batching and frame acceptance.

There was **no matching Cardassian model** in that catalogue. This delivery fills the geometry gap with fresh original CPU-authored prototype meshes, not a recoloured Federation ship or an approved canon Galor replica:

- [Cardassian export directory](../../art/ships/cardassian-cruiser-v1/manifest.json): three OBJ+MTL and three self-contained GLB LODs, six material definitions and eight named visual anchors.
- [Three-quarter preview](../../art/ships/cardassian-cruiser-v1/three-quarter.png) and [top preview](../../art/ships/cardassian-cruiser-v1/top.png), rasterised on CPU from the actual mesh. These are not Unity screenshots.
- [Reproducible geometry source](../../tools/art/build_cardassian_cruiser.py) and [independent payload validator](../../tools/art/validate_cardassian_exports.py).

Owner-supplied Cardassian images informed ochre materials, broad forward shoulders, layered forward structures and a long central spine. No downloaded mesh, reference-image pixels, third-party texture, logo or deck plan was used. Geometry is intentionally a simple battle-distance prototype, without final unique texture atlases, markings, intricate greebles or a production-approved class silhouette. This closes the missing-faction-placeholder problem for review; it does not establish final artistic approval.

| Cardassian LOD | Triangles | Bounds XYZ metres |
| --- | ---: | --- |
| 0 | 3,320 |360 ×75 ×480.95 |
| 1 | 1,496 |360 ×75 ×480.95 |
| 2 | 512 |360 ×72.5 ×480.95 |

## Import and material guidance

Authoring axes are **+Y up / +Z forward**, metre-scale positions, identity root. Art dimensions are proposed presentation choices, not scenario dimensions or combat distance. OBJ is a receiving candidate for Unity's normal model import, GLB is a portable alternative requiring an appropriate importer. **No native import claim is made.** Unity may apply format-specific axis/handedness conversion: compare asymmetric forward aperture, forked aft end and Axis_Bow/Axis_Up before binding. Do not compensate by changing gameplay coordinates. OBJ groups/materials are present, but OBJ has no transform nodes: create effect anchors from manifest data after confirming the receiving root. GLB contains the eight named nodes already. Avoid shipping the authoring rig or adding collision-based combat resolution.

Map `CARD_Ochre`, `CARD_Plate_Gold`, `CARD_Inset`, `CARD_RedBrown` to appropriate receiving lit materials. The manifest contains base colour/metallic/roughness. `CARD_Drive_Amber` and `CARD_Windows` carry restrained emissive hints. MTL is a fallback colour carrier; it is not a guarantee of URP shader mapping. Federation's existing pearl/slate/bronze/red/blue palette remains distinct. Do not use a single faction tint to replace these material families. Controlled bloom must preserve hull and shield legibility; emission is decoration, not proof a gameplay system is powered.

Proposed LOD thresholds may start from the existing Federation pack, then be measured against actual battle framing. Six material slots can create multiple draws; do not claim a fleet performance budget from triangle counts alone. The validator checks data, not GPU memory or frame time. Secondary should verify correct silhouettes, non-mirrored facing, inward/outward normals, flat-shaded authoring appearance, all LOD transitions, material mappings and anchors in the actual receiving build.

## Effect anchors and authority

The mapping records measured Federation GLB positions. `Effect_Torpedo` is a candidate ordnance source when an actor-safe matching event permits it. `Effect_Deflector` is **not** a weapon origin simply because it glows. Nacelle pivots are attachment points, not measured exhaust tips. Recheck imported FBX transforms; do not assume authoring -Y/+Z coordinates are already Unity coordinates.

The Cardassian manifest supplies `Effect_Beam_Fore`, `Effect_Ordnance_Fore`, port/starboard drive points, impact/focus points and axis markers. These are **presentation anchor proposals**, not gameplay mount groups or guarantees of a fitted weapon. Place effects only after an authoritative event identifies a visible source/target/weapon class, and respect actual facing/sector information where supplied. If no exact emitter mapping exists, use a labelled generic visual origin consistent with the legal event; do not invent an equipment version. For a shield/hull hit with no physical surface coordinate, a soft sector pulse or generic near-surface hit is truthful; an exact breach or subsystem failure would not be.

If a lawful incoming-ordnance event has no disclosed launcher, animate from the viewer edge without spawning or naming its hidden source. Hidden contacts get no mesh, silhouette, formation slot, focus target, count-bearing debris or camera framing hint. Cloaked owned ships may remain inspectable to their own actor, but visibility must be decided before creating scene objects from an actor-filtered projection. No private save/root inspection or catalogue lookup by hidden IDs.

## Reference and original cinematic storyboard

Owner reference: [Endless Space battle video](https://www.youtube.com/watch?v=U3-yppHztUM). Direct fetching was throttled; **I could not view that specific video and do not claim shot-by-shot analysis or timestamps**. As primary supporting material, the [official Endless Space2 manual, printed pages36–37](https://cdn.cloudflare.steamstatic.com/steam/apps/392110/manuals/ES2_PC_OM_JAN_2018_210x297_UK_HR.PDF) describes cinematic versus overview cameras and an advanced battle report. The proposal below is original, using that high-level presentation pattern and the owner's reference direction. It imports no cards, fixed range phases, retreat penalty, faction art or mechanics from Endless Space.

Use readable fleet silhouettes, slow three-quarter passes and brief weapon/impact emphasis while keeping the existing command UI and overview option. Combat remains our abstract simultaneous round resolver, **maximum three rounds with early completion**, not a pre-resolved full-battle movie. Do not manufacture a three-round sequence when the battle actually ends sooner.

| Beat | Framing and choreography | Authoritative gate / truthful UI |
| --- | --- | --- |
| Round1 planning | Wide paused own-fleet establishing shot, selected fleet highlighted; only disclosed opposing ships visible | Distant opening abstract state; no drag-to-place formation, opponent commitment or undisclosed count |
| Commit current round | Hold framing with accepted own-order receipt; do not show an opponent readiness animation | Resolve only the committed joint round; never compute rounds2/3 before new human choices |
| Resolved maneuver | Brief three-quarter dolly showing the resulting relative maneuver, not a physical route simulation | Drive choreography from actual resolved state/events, not merely the player's requested choice. Screen protects members of its own fleet only |
| Fire / defence / impact | Frame visible emitter and target where lawful; beam/ordnance, defensive interception, shield-sector pulse and restrained hull hit | Only typed actor-safe events. Miss does not get a hit flash; hidden incoming source stays hidden. No extra shots or ammo use invented to fill time |
| Round checkpoint | Settle camera, show actual current/max hull, four shield pools, finite ammo and round deltas | Pause for next orders. Hold/maintain is one option; include Charge, Flank, Screen and Fighting withdrawal when legal. Fresh enemy orders remain private |
| Round2, optional round3 | Reuse the same readable stage, with shot emphasis varied by the committed result | New review/commit each round. Ordinary cloak/reveal rules stay authoritative; no re-cloak or camera-based shield rotation |
| End / early defeat | Surviving ships settle; only recorded destroyed ships receive destruction effects | Meaningful own loss/survivor/stock summary precedes spectacle. Whole-batch readiness controls once-only aftermath |
| Return to campaign | Report, explicit aftermath, map with stopped routes and fresh intent choice | Current pools persist. Terminal outcome permits inspect/save only. Loaded campaign has current stocks, not an invented permanent battle movie/report archive |

Place fleets in deterministic presentation slots keyed by the **visible fleet and ship IDs**, retaining separate formations even when fleets are colocated strategically or share a route. Normalise stage spacing/camera against the largest visible hull length; it is not range in metres. Starting camera can favour own bow at a shallow three-quarter elevation; an overview/reset remains available. Camera orbit does not redefine fore/port/starboard/aft. A fighting-withdrawal pose may drift back within the frame, but it must not imply successful sector escape. A requested Charge that failed to close range must not be animated as a successful arrival at close range.

Typed-event batches establish simultaneity; use stable event ordering only to make an understandable animation. Playback timing, cosmetic noise and lights must not consume campaign RNG. Pause/speed/skip change presentation only; skip seeks to the already-resolved result, never calls Resolve/Finalize again. Re-entering or loading must cancel old animation callbacks/resources, bind the new actor/session epoch and avoid replaying damage or destruction as mutations. If a required shot/impact event is absent from the receiving contract, display the available aggregate result honestly and request that narrow Primary projection; do not fabricate events from final hull alone.

## Executed checks and remaining gates

CPU payload checks passed all three GLBs and OBJs: actual buffer/accessor/index parsing, finite positions, unit normals, nondegenerate triangles, winding/normal agreement, UV presence, matching triangle counts and bounds, eight named anchor positions, no external texture/buffer dependencies, file hashes and decreasing LOD counts. OBJ index ranges and triangle totals match. The actual top/three-quarter mesh previews were visually inspected. See [validation.json](../../art/ships/cardassian-cruiser-v1/validation.json).

Open receiving gates: Secondary's Unity model import/orientation/material/anchor/LOD verification; actual live scene shots and effects matched to Primary's final safe events; framing at all three sizes; performance/resource disposal; native per-round choice/skip/early-defeat/result/aftermath/save-reload acceptance. Final artistic approval, canon-class naming and production texture/detail targets remain open. No blocker is hidden by calling the prototype a validated final Cardassian asset.
