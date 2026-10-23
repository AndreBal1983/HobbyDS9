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
