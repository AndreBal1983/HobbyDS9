# Freeform nebulae and a layered galaxy / revised art proof

23 October2026. **Owner requested:** remove the strict oval silhouette; use Hubble/Webb morphology as inspiration; assess a shared3D density/depth system for galactic structure, individual typed star markers and embedded nebulae. **Proposed:** presentation and future data boundaries below. **Not implemented:** portable stellar taxonomy, new galaxy generation, topology or production Unity rendering.

## Correction to the first study

`EnvironmentGalaxyFeatureDto` explicitly describes RadiusX/RadiusY as an axis-aligned ellipse of **generation influence, not physical extent or sensor coverage**. The first study incorrectly treated it as a hard physical art bound, then feathered that same bound. The old179-file gallery and its receipts remain immutable historical evidence, but the zero-alpha-outside-influence-ellipse criterion is superseded. Preserve the supplied ellipse for generation diagnostics; rendering gets separate padded bounds and a freeform density envelope. Filaments and glow may extend beyond the influence ellipse without implying hazard, visibility, sensors or movement effects.

## Inspected official references

- [Webb Cosmic Cliffs, NIRCam/MIRI](https://science.nasa.gov/asset/webb/cosmic-cliffs-in-the-carina-nebula-nircam-and-miri-composite-image/): an asymmetric wall, scalloped ridges, offshoots and a large open sky region, rather than a centred filled oval.
- [Hubble Veil remnant](https://science.nasa.gov/asset/hubble/veil-nebula-supernova-remnant/): a diagonal, broken, tangled filament sheet with transparent gaps and finer isolated threads.
- [Webb/Hubble NGC628](https://science.nasa.gov/asset/webb/webb-and-hubbles-views-of-spiral-galaxy-ngc-628/): branching dusty spiral structure, cavities and a diffuse central component across several scales.

The linked images were actually opened and inspected. Local inspection copies are outside the delivery; no reference pixels become our textures. Telescope composites assign colours to wavelength bands; these images do not establish literal star temperatures/classes from pixel colour. NASA's [star overview](https://science.nasa.gov/universe/stars/types/) also distinguishes brown dwarfs as substellar, rather than ordinary hydrogen-fusing stars. Fixture colours/glyphs will be labelled illustrative.

## Bounded proof plan

Reuse the original CPU coherent3D field and perspective-slice machinery in a new isolated branch/output. Generate one freeform nebula and one diffuse galactic field, each32×512×320depth slices, target<120seconds and<512MiB measured Python peak. Local nebula shape is a union of asymmetric lobes and warped curved filaments with subtractive cavities; it fades before a separately declared padded texture box. No radial/elliptical shape mask is applied by the renderer.

Deliver A in unchanged actual seed42/250 Natural v5 context, including supplied stars/regions/API route and pan parallax. Deliver B as an explicitly **illustrative galaxy fixture**, about600typed stellar/substellar markers plus local nebula instances, with overview and regional views. The fixture is not a current generator output. At most six CPU/headless browser captures, GPU disabled; no Unity imports/builds/native input, GPU lease, paid service or public asset release. Existing models/builds/galleries stay unchanged.

Pickable markers remain on the reference navigation plane. Decorative volume samples have independent depth and perspective parallax; typed markers share the coordinate projection without fake movement of their hit targets. A real3D navigation model remains a separate gameplay decision. The completed plan will separate this visual experiment from future versioned stellar data and macro-environment-first generation.

## Shared visual architecture

Yes: one density/depth framework can support a galactic background and local nebulae. Keep distinct fields and catalogue markers rather than turning every bright voxel into a star. Use a coarse3D disk/bulge/arm field for unresolved light and dust, smaller seeded freeform volumes for local features, and individual markers from a catalogue. All share one world-to-view transform and explicit depth order, while retaining different data meanings.

The galactic field here is a smooth mathematical structural illustration, not a finished Hubble-like galaxy. Branching arms, asymmetric spurs, patchy populations and richer dust hierarchy would be later art work. Four local clouds reuse one field with different transforms; they do not prove independent morphologies or physically correct interpenetrating-volume compositing. The fixture's star proportions are for demonstration, not an astrophysical population model.

For an actual feature, retain its supplied centre as anchor. Influence radii may provide an initial authoring scale, but never a physical clip. Store independent visual transform, depth and padded3D bounds in the art definition/cache. Use offset lobes, directed warped ribbons, ridges, dust channels, subtractive cavities and separated wisps. Rotate morphology independently of the axis-aligned influence diagnostic. Fade before all six padded bounds and use those bounds for culling; the box is a computation bound, not visible geometry. Enlarge it if a supported wisp reaches the guard band rather than clipping to an ellipse.

Use the [hybrid renderer proposal](nebula-volume-concept.md#recommended-unity-implementation) with this corrected bound: far coherent depth impostors,8–12slices regionally and a bounded24–48sample near raymarch. Seed morphology from galaxy seed + stable feature ID + visual version; quiet-area changes that preserve features should preserve art. Cache validation includes feature geometry/visual settings. Full map hash is provenance, not the morphology seed. This study uses fixed experimental authoring seeds, not an implemented production cache/seed contract.

Perspective gives relative pan displacement `q=D/(D-z)`; ordinary orthographic translation alone does not. Keep diagrammatic navigation atZ0, decorative fields at separate depths, and readable guides/markers above the fog colour pass. Picking intersects the same reference plane. The galaxy concept has visual volume thickness, not newly authoritative3D star positions or a3D route solver. True3D navigation needs a separate portable design and UX decision.

A full galactic field needs spatial LOD, not a galaxy-wide high-resolution texture. Begin with one coarse volume or a few clipped-map tiles, with nearby higher-resolution bricks. Coordinates/seeds must agree across seams. Preserve macro identity across LOD. Use scissored/tiled rendering, stable importance/residency caps and lawful-feature filtering before upload. Render typed markers in instanced/procedural batches rather than one GameObject/light per star. Cosmetic background sparkle must be visibly non-pickable.

The earlier Unity budgets remain proposed: roughly1.5ms steady /2.5ms heavy nebula GPU time at1080p on selected hardware, half-resolution shading, bounded near volumes and about96MiB total field/impostor/target residency. The macro field must share that allowance or justify a measured increase. A128³RG8 macro field with3D mipmaps is about4.57MiB; account for extra levels/bricks and marker buffers. Small-scene estimates are not whole-galaxy profiling. Measure overlap, fill, labels and memory on target hardware; no GPU/Unity benchmark was run here.

## Stellar classes: future portable work

Current Natural v5 supplies generic stars and generation-influence settings, not persisted spectral/evolutionary classes. Do not reinterpret influence bands as red/blue giants or brown dwarfs. A keeps every actual star generic. Only B uses illustrative classes with distinct fixture IDs and labels.

NASA describes [main-sequence spectral classes](https://science.nasa.gov/exoplanets/stars/) and [stellar/substellar categories](https://science.nasa.gov/universe/stars/types/) separately. A future versioned taxonomy should likewise separate object kind, temperature/spectral class and evolutionary stage, rather than combining incompatible axes:

| Data axis | Proposed future meaning | Presentation after lawful projection |
| --- | --- | --- |
| Object kind | Hydrogen-fusing star, substellar brown dwarf, compact remnant | Distinct glyph/label; brown dwarfs explicitly substellar. |
| Spectral/temperature class | O/B/A/F/G/K/M where applicable; separate substellar classes if supported | Restrained colour range, never inferred from telescope composite pixels. |
| Evolutionary/luminosity stage | Main sequence, giant/supergiant or supported remnant stage | Shape/halo/inspection model; colour alone does not define stage. |
| Physical descriptors | Versioned mass, radius, luminosity, temperature, age/metallicity with explicit units/ranges | Close inspection and plausible correlations; strategic glyph sizes stay exaggerated. |

These are proposals, not schema changes. Start with a small curated catalogue. Brown-dwarf markers need a readable symbolic display because they emit little visible light; the brown diamond is not naked-eye brightness. Giant glyph sizes are not physical radii. Unknown classes need neutral presentation, and Application must supply lawful information. No class automatically grants resources, colonization, visibility, hazards or movement effects.

## Macro-environment-first generation: separate proposal

If actual systems should follow galactic arms, a future portable generator must create macro environment first: bounded galaxy morphology/density, then population/age regions and star/nebula anchors/descriptors, then navigation sites/regions/adjacency. Use independent deterministic stage streams, explicit generation/tuning/taxonomy versions, bounded budgets and shared graph validation. Density may guide sampling; a renderer cannot decide stellar classes, connections or route costs.

That would deliberately change positions/hashes under a new version. Existing generic-star formats/maps/saves remain exact; define Open/backward compatibility and any migration policy first. Do not place spiral art behind old random anchors and claim those anchors were generated by arms. Future tests need fixed-seed regeneration, descriptor validity, lawful snapshots, connectivity, pacing/area budgets, old Open byte/hash preservation and human/AI validation parity. No such generator, taxonomy, migration or gameplay implementation is included here.

## Actual proof and limits

Output: `E:/HobbyDS9/artifacts/art/freeform-nebula-galaxy-v2-20261023-1/`. Open review.html for the gallery or index.html for the interactive study. A is unchanged actual seed42/250 map `4ea9b0d4581edb60460413e9fee0203257bd47e5b6cb43fc044ee5b2cda01976`; B is a synthetic600-marker fixture, seed8675309. Classes shown:214red dwarfs,119Sun-like,80red giants,71blue giants,63brown dwarfs,53white dwarfs. This distribution is illustrative, not physics or balance.

One CPU batch generated two32-plane512×320fields in13.7010seconds, measured peak85,598,208bytes (81.6MiB). It reused inspected v1 noise/memory functions and source-bound unchanged map context. Both fields fade to zero at the XY texture edges. The local field's first/last depth slices still have low peak alpha7/255 and6/255; a continuous six-face guard fade must be implemented before arbitrary orbit or production raymarching. The current fixed-direction slice proof does not establish that end-cap behavior. Actual freeform compositing measured37.7–38.0% of sampled alpha outside the generation ellipse, replacing the old physical-clip criterion.

Six GPU-disabled headless captures cover actual map centre/left/right, illustrative galaxy overview/regional and labelled influence/depth diagnostic. Checks preserve inputs, verify240px map-plane motion across ±120px pan, >40px near/far difference, flat-control zero differential, raw-star picking, Play/Pause and compact no-overflow. Overview shows all600fixture markers; regional view shows multiple classes. No extra density/capture polishing batch followed the approved pass.

This is a perspective depth-slice art proof: no arbitrary orbit, physically exact scattering, high-fidelity galactic dust hierarchy, production renderer, portable taxonomy, new topology or runtime performance acceptance. The galactic background remains a smooth structural sketch. Original v1 gallery/179-file manifest, ship pack, other map galleries and accepted builds remain unchanged.
