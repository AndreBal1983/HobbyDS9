# Original asteroid belt v1

15 November 2026. **Art candidate; Blender file/export checks and visual preview only. Unity/native receiving pending QA.** Separate from Saturn rings. No authoritative generation, mining, collision, resource reward, movement obstruction, or save changes.

## Assets and provenance

- `Models/Belt_Rock_{0,1,2,3}_LOD0.fbx`: four original distorted icosahedra, 80 triangles each, unit bounding sphere. `LOD1` versions: 20 triangles each. Flat facets, warm-neutral vertex colors `RockTone`; no image texture.
- `Models/Belt_Example_LOD0.fbx`: combined 320-rock fixture, 25,600 triangles, one material. `LOD1`: first 120 placements using simplified prototypes, 2,400 triangles. These illustrate the recipe; never use the example's system/feature identity as campaign content.
- `Models/Belt_SoftAnnulus.fbx`: original 192-segment/384-triangle strip, inner radius 1, outer 1.24; UV U=angle turns, V=radial fraction. Analytic radial opacity fades both edges; sparse angular mottling. No narrow opaque bands, Cassini gap or Saturn material.
- `Shaders/BeltRock.shader`: original manual center-star lighting, opaque, one vertex-color material, no image maps. `_Tint=white`, `_NightFill=0.14`, `_DayStrength=1.1`, star center shared with the system. No shadow pass/collider.
- `Shaders/BeltSoftAnnulus.shader`: original URP unlit straight-alpha, two-sided, depth test on, depth write off; tint `(0.35,0.31,0.25,1)`, opacity 0.065 (art range 0.035–0.08). Skip or reduce haze on bright backgrounds. No raster texture or external dependency.
- `Sources/AsteroidBelt_v1.blend`: editable original mesh/material and illustrative scene. `layout-example.json` is the deterministic recipe witness; `manifest.json` has version, provenance and file hashes. `geometry-validation.json` and `validation-fbx.json` distinguish generated versus reimported checks.

Belt geometry, vertex colors, shaders and recipe are original HobbyDS9 project work; no external model or new bitmap is used. The **preview only** borrows unchanged Sun/Mars/Earth/Jupiter/Neptune maps from the already delivered `art/asset-production-01/system-v1/Textures`, Solar System Scope / INOVE, CC BY 4.0. Preserve existing attribution: https://www.solarsystemscope.com/textures/ and https://creativecommons.org/licenses/by/4.0/. The illustration is not Sol, a generated campaign snapshot, a body-count result or a native screenshot. Planet maps and studio lights are not belt runtime dependencies.

## Deterministic receiving recipe

Only instantiate from an already lawfully disclosed belt feature ID in the current actor snapshot. Backend owns actual bodies/features, the proposed typical 3–6-body generation rule, authored Sol's exact eight, ordering, disclosure and version/save policy. A missing feature draws no belt; never search trusted catalogues or infer deposits/hazards from this art. The new Sol cosmetic exception does not grant disclosure for random worlds or belts.

Use `belt-v1 + LF + exactSystemId + LF + exactFeatureId`, UTF-8, **no trailing LF**. FNV-1a unsigned 32-bit seed starts 2166136261, for each byte `s=((s XOR byte)*16777619) mod 2^32`; replace zero with 1. No runtime string hash, clock, process random, or campaign RNG. IDs need a canonical newline-free adapter contract; reject malformed IDs rather than concatenate ambiguously. The fixture text is `belt-v1\nsystem.example\nfeature.belt.example`, initial seed **342353800**.

Each draw: unsigned xorshift32 `x ^= x<<13; x ^= x>>17; x ^= x<<5` with 32-bit truncation after left shifts and at assignment; `u=(state>>8)/16777216.0`. For rock index `i`, take exactly eleven draws `a[0..10]` in this order. The generator is the executable reference:

```text
turns = frac(i * 0.6180339887498949 + 0.12 * (a0 - 0.5))
radial01 = (a1 + a2) / 2
heightOverMidRadius = 0.012 * (a3 - 0.5)
radiusOverMidRadius = 0.006 + 0.010 * a4 * a4
prototype = min(3, floor(a5 * 4))
rotationRadiansXYZ = TAU * (a6, a7, a8) [Blender intrinsic XYZ]
stretchXYZ = (0.8 + 0.5*a9, 0.7 + 0.4*a10, 0.75 + 0.35*a2)
tone = 0.68 + 0.28*a10
```

Sort features by ordinal stable ID. Use prefix subsets, not rerolls, at lower detail levels. Convert the full Blender rotation basis/quaternion into Unity's imported basis; Euler components are not portable by swapping two values. Positions: local plane `(cos(TAU*turns)*R, height, sin(TAU*turns)*R)`, where `R=lerp(displayInnerRadius,displayOuterRadius,radial01)` and `mid=(inner+outer)/2`. Stretch and sphere radius act on prototype vertices in that converted basis. Actual baked fixture is Blender XY/+Z; FBX uses `-Z forward / Y up`. Confirm imported world basis/normal and winding rather than guessing from object Euler values.

Display radii must come from the GUI's explicit schematic adapter for the disclosed feature/orbital slot, not AU conversion or art-invented gameplay distance. For the shown fixture `inner=10, outer=12.4`. Cap each rock's radius to `min(mid*radiusOverMidRadius, 0.15*(outer-inner))` if adapting to a narrower band. Maximum stretch is 1.3, so fit an outer envelope `outer + 0.0208*mid` and height ±`0.0268*mid`, conservatively including rock extent. Keep the ring in the same existing tilted orbital plane, parented at the existing star center; never billboard or rotate it with a planet. No per-frame orbit/spin/physics is required.

Do not uniformly scale the unit annulus if the requested inner/outer ratio differs: rewrite its two radial vertex rings to the adapter's two radii, retaining UV. Baked example meshes are inspection fixtures only. Runtime can combine transformed prototype vertices once per disclosed feature/version/LOD. Keep and reuse a deterministic cache; dispose only owned combined meshes/materials.

## Work and rendering budget

Suggested receiving budget, requiring native profiling: **at most 384 rocks total across the stage**, maximum 30,720 rock triangles at LOD0; a normal single belt uses 320. Split the fixed rock budget across disclosed features in ordinal order with stable residual allocation. Merge rocks sharing material into one stage mesh; merge soft strips separately. Target two belt draw calls total, no per-rock GameObjects, lights, colliders or Update calls; no textures, particles, compute shader, plugin or package install. Refresh only on lawful feature/version changes or a detail threshold crossing.

Suggested projected belt-diameter thresholds: above 220 render pixels use LOD0; 90–220 use first 120 LOD1 rocks per belt within the stage cap; below 90 use only the soft strip. Add 15% hysteresis. Fade between levels or switch after camera motion settles; no simultaneous full-detail duplicates. Keep total annulus budget ≤4096 triangles; lower the strip segment count deterministically across multiple features when necessary (minimum 16 segments per feature). If an unexpectedly large lawful feature list cannot fit this budget, keep the truthful existing feature list and report the receiving limit; do not perform unbounded mesh work or fabricate fewer authoritative features. Resource counts/CPU and actual shader support are QA gates, not measured claims here.

## Camera and appearance direction

Keep 24° elevation, smooth analytical body tracks and source ordering. For sparse systems, start with a **minimum horizontal schematic radius of 24 units**, independent of known body count. Fit `max(actual lawful body/belt envelope, minimum24)`; project the plane at 24°, add each object's own height/radius and 10–15% margins, then account for viewport aspect and label padding. Never zoom a one-body overview to fill its entire stage. Retain explicit selected-body focus and a return-to-overview action. At a narrow stage, expand vertical fit/reflow labels rather than clipping the Sun or outer belt.

Pair with the Sol radius/lighting recipe: central Sun radius 1.15, self-lit map, no orange-on-orange tint; comparable exaggerated planet radii 0.56–1.0, no hidden-class-dependent size changes. At 1440×650 the supplied illustration uses orthographic width 48 and a 24° view; about 30–40% headroom in the empty corners deliberately communicates space. Existing native stage aspect differs: recompute fit rather than copy Blender's orthographic width into Unity's half-height property. Keep 44-logical-pixel body hit targets, screen-constant readable labels and minimal orbit brightness. Belts do not intercept pointer selection. Preserve source focus/yaw/zoom/reduced-motion behavior.

The soft strip is a readability aid, not a real continuous opaque disc. Rock sizes/density, planetary positions and all distances here are illustrative/exaggerated. The wide preview uses existing solar maps solely to compare hue/scale; it must not become a random-world art lookup table. A Sol belt appears only if Backend supplies the lawful actual Sol belt feature; its eight planets remain exactly eight.

## Evidence and remaining gates

[Wide scene](Previews/overview-24deg.png) and [rock detail](Previews/belt-detail.png) were rendered with Blender 5.3.0 build `02f0899d78e3`, Cycles CPU, 16 samples, and visually inspected. Source counts: four 80-triangle and four 20-triangle prototypes, 25,600/2,400-triangle combined examples, 384-triangle soft strip. The illustration includes a broad studio readability light, so it is **not** a guarantee of native lighting parity; runtime's explicit shader is the starting recipe.

No Unity or desktop opened. QA must verify imports, positive transforms, normals/color/UVs, runtime shader compilation, stable layout/LOD/re-entry, lawful visibility, 1/2/3–6/eight-body overview fit and selection at 1280/1920/ultrawide, Sun visibility, Saturn-vs-belt distinction, mesh/material lifetime and CPU/render cost. Backend's versioned feature contract is still a dependency. No gameplay tests run because this isolated candidate changes no gameplay or receiver code.
