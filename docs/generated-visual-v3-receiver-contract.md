# Dedicated V3 receiver contract

24 October 2026. Source/CPU receiving candidate only. V1 renderer, saved geometry, routes, costs and simulation remain unchanged. Art source aa9b0f092838033ef402629c96d1533c5468249f and its original payload manifest are the asset authority; no regeneration or reference-image pixels. Host boundary c31084b supplies one validated actor-known GeneratedCampaignPresentation. No saved root, private feature tree, hidden omission count or seed enters rendering.

## Metric, orientation and optics

Isotropic coordinates X=(mapX-W/2)/W, Y=(mapY-H/2)/W, Z=zWorld/W. Camera is at current viewport-centre XY and D=4 map-width units. Let R32 be the clockwise matrix [[cos,-sin],[sin,cos]] in y-down screen convention. Authored disk maps to XY=.47*R32*(u,v*cos48-z*sin48), Z=.47*(v*sin48+z*cos48). Thus +v projects down the minor axis and toward the camera; authored normal +z projects oppositely along the minor axis and toward the camera. This explicitly chooses .47*W per authored macro-height unit, not the local-nebula .8 depth scale. Inclination, aspect and rotation apply once. PNG/upload v receives exactly one Y correction.

At perspective depth Z, inverse ray XY=Cxy+(S-Cxy)*(1-Z/4), where S is the screen point unprojected onto navigation z0. Apply the inverse rotation/inclination/scale to sample the atlas. Depth-off means q=1/orthographic XY=S while retaining physical inclination and optical kernels. Navigation markers, route anchors and picking stay on z0.

Named receiving approximation: g_h(z)=exp(-(z/h)^2)/(h*sqrt(pi)*erf(3)), truncated at ±3h. Twelve bins use the union edges [-.33,-.165,-.105,-.054,-.0525,-.027,0,.027,.0525,.054,.105,.165,.33]. Intersect actual disk support, order by ray distance and evaluate one midpoint atlas fetch per bin; weight each kernel by exact CDF differences divided by |dz/ds| once. No clipped-ray renormalisation. Constant columns integrate to1 face-on or1/cos48=1.49447655 obliquely. Kernel convention and bin approximation are receiver choices, not original asset metadata.

Here s is AUTHORED isotropic disk arclength, sDisk=sMapWidth/.47. Equivalently a world-arclength implementation uses W=CDFdifference/(.47*abs(dzDisk/dsMapWidth)). The central orthographic48degree column has the stated secant; off-axis perspective directions use their actual normalized inverse-ray disk direction. Numerical CDF evaluation uses the named error-function approximation (about1.5e-7 maximum erf error), not midpoint-density weights.

E=.12*atlasRGB already includes the core. Cosmetic partition wB=1-smoothstep(.08,.16,r), Eb=wB*E, Ed=(1-wB)*E preserves total emission; never add a second bulge. Per-bin J=Ed*W.035+Eb*W.11, tau=4*A*W.018. Accumulate L+=T*J*(1-exp(-tau))/tau, T*=exp(-tau), with stable ratio→1. No RGB*A, alpha premultiplication or fallback .65 dust multiplier in this optical path. Floor is applied once and exposure is separate.

## Local fields, mask and composition

Use the original128×96×64 linear independent emission/extinction data and its authored padded bounds, no additional2.4 padding. Local positions derive only from coarse proxy centre/reference radii and FeatureId+assetversion cosmetic variation. The actor-known pair has no Seed: this receiver explicitly chooses ID/version-only variants across campaigns. Map hash is identity/cache invalidation only. Local visual depth=.8*zLocal in the same map-width metric; particles share that camera/projection.

Local optical measure follows Art's clarified original recipe: projected local-Z, dzLocal=abs(deltaZworld)/.8, NOT Euclidean inverse-ray arclength under the anisotropic XY radii. tau=4*A*dzLocal and J=.30*RGB*dzLocal; direct local-z stepping2/N has no further.8/secant factor. Use correct centres1-(i+.5)*2/N. The original CPU proof accidentally reused64-sample endpoint offsets for48 nodes; retain its immutable images and disclose this small comparison difference. Depth-off retains local optical weights. Local accumulation stores integrated radiance L and opacity1-T (not asset extinction A); after composition mask M the final is macro*(1-M*opacity)+M*L. Particles are additive radiance within the masked local layer.

At most8 ray-box-culled lawful field instances, footprint mips, early mask,256 particles TOTAL. A soft splat's FULL quad diameter is at most32 local-target pixels (radius16); clipped viewport fragments are therefore bounded by256*32*32=262144. Its circular kernel is exp(-3*r²), truncated at radius1; brightness is original candidate brightness times.08 and footprint/fade gain. Candidate index then ordinal FeatureId shares the total cap fairly. Field residency ranks projected-box importance with a.85 retention factor and ordinal ties; replacement fades the old set out over.15s then the new set in over.15s, retaining at most8 fields. Only this finite transition schedules33ms refreshes; settled views submit nothing. Turning effects off immediately clears residency. No continuous reseeding, temporal noise or stacked cards. Only known proxies may instantiate fields; no hidden feature recovery or hidden total.

The dedicated non-pickable Image is the map's first child, aligned exactly to its content plane (CSS origin contentRect.xy minus parent border); existing lines and markers are later children. No v1 background code changes. Local volume plus particles are accumulated separately, then conservatively masked before adding unmasked decorative macro light. The final UI bilinear/subpixel footprint is part of the mask contract: a nonzero source texel's entire possible final-image support must lie inside the charted polygon UNION. Actual image-quad/independent lines-plane alignment and exterior enabled-minus-macro-only contribution ZERO are later receiving gates at three sizes, including borders/padding, corners and fractional pan/zoom.

CPU mask construction normalises exact integer collinear edges, cancels opposite interval overlaps to obtain the union boundary, scanline-fills interior and zeros a conservative boundary supercover enlarged by BOTH local-to-final-target and final-target-to-actual-image bilinear/final-pixel/subpixel support (at least3 local texels). Preserve holes, corner touches and partial shared edges; do not create seams at internal charted boundaries or infer rectangle coverage from corners alone. This consumes trusted validated non-overlapping charted convex cells, not arbitrary intersecting polygons: even-odd overlap can conservatively punch holes and is not a general polygon-union library. Input≤20,000 edges; boundary≤40,000 pieces; target≤960×540. One10-million primitive-operation budget covers extraction, sorting comparisons, row work and guard work. Process only edge Y bounds. Any malformed geometry, certificate or aggregate-budget failure zeros ALL local pixels and reports a bounded diagnostic; macro and navigation remain usable. CPU actual1000 full-known and fragmented fixtures must disclose fallback rates, not hide failures.

## Resources and staged acceptance

One linear2048 atlas with independent channel mips21.333MiB, one shared linear3D volume with guarded mips3.429MiB; two ARGBHalf targets at most960×540 together7.910MiB and R8 mask.494MiB total≈33.166MiB logical texture payload. Particle buffers remain below1MiB, giving<35MiB logical texture/buffer payload, excluding driver and hidden engine copies. Final macro resolution stays at its normal960×540 cap. ONLY the local accumulation and mask shrink so48*residentFields*localPixels≤40million conservative volume fetches per submission (one field960×540; eight fields about430×242). Actual boxes/mask reduce work further; this is not a GPU performance guarantee. Local volume fields jointly sum emission/extinction at each common local-Z sample before optical accumulation. The local layer stores integrated light/opacity and particles add RGB without changing opacity; the completed layer is conservatively masked during local accumulation and gated again after local-to-final bilinear upsampling, with the same point-mask coordinate, before macro composition. Both radiance and opacity are gated. The final gate adds one point mask fetch per final pixel, at most518400, and no additional render target.

Upload the original volume once with independent linear box-filtered mips; force every mip's six outer faces to zero. The coarsest mips consequently fade to zero rather than sample padded-edge emission. Release source TextAssets/staging arrays after upload. Atlas importer uses canonical Unity6000 serialization AND a V3-path-only AssetPostprocessor to require linearRGBA32, mips, non-readable, Clamp/Trilinear, uncompressed, alphaIsTransparency=false. No import has been executed at this source gate. Actual importer/graphicsFormat and numeric channel probe are mandatory engine gates; reference compilation cannot establish them. CPU staging/mask allocations are operation-bounded, but managed/driver overhead is not a measured VRAM claim.

Cache on actor/profile/host epoch/map/knowledge revision, view, settings and asset version. No idle GPU submissions. Revalidate the pair before publishing each refreshed frame; stale/profile failure clears local visuals. Dispose targets/materials/owned textures on detach/replacement/unsupported/lost context; flat usable fallback with no local reveal.

The existing receiver `effects` boolean now controls ONLY local nebulae/particles, presented as Nebulae on/off. Off clears local residency/transitions immediately and writes a zero local layer while keeping the same macro, camera, depth and floor. Thus the actual same-UI off frame is the macro-only control for exterior-mask subtraction, including local opacity shadows. Depth remains an independent control. V1 behavior is unchanged.

First CPU gates cover forward/inverse projection, z0 identity, depth-off, Gaussian energy/path normalization and union-mask support invariants/budgets. Player/Editor reference compilation follows exact source. Later separately leased receiving uses labelled Explore32 three-proxy versus separate combat zero-proxy fixtures,1/8-field workload, overview/regional/matched pan/depth-off, three-size alignment/picking<2px, actual composed exterior ZERO, cache/lifecycle/failure checks. GPU timings only if actually available; CPU submission is not FPS or GPU time. Menu V2 default/explicit V1 Continue and battle UI are separate later commits.

## Raw particle target convention

Raw clip-space particle meshes apply a conditional clip-Y flip under UNITY_UV_STARTS_AT_TOP to match Graphics.Blit target orientation. Their authored projected position, mask coordinate, optics and brightness are unchanged. The receiving asymmetric actual-pass probe must reject reflected placement independently of final masking; the full UI enabled-minus-off exterior gate remains exact zero. Raw component PNGs encode linear-data channels without display conversion and must not be judged as display previews. The actual UI transfer is unchanged.


## Inward explored-edge feather candidate (25 October 2026)

Source/CPU candidate, pending narrow actual-render acceptance. The unchanged
binary union/guard certificate is followed by an optional exact separable squared
Euclidean distance transform from its zero texel centres. Distances use logical
UI units independently on X and Y. Runtime requests8 logical pixels, with a
minimum transition width of3 times the larger UI-per-mask-texel spacing. A
smoothstep of that inward distance is floor-quantized to R8. Every old zero
remains exactlyzero and no output byte exceeds the certified input; default0
retains previous bytes and work. Existing shader gates compound this mask
through local volume, particles and final upsampling; the effective appearance
is not a single unmodified smoothstep. Optical coefficients/exposure/macro,
resident volume count and final-target resolution are unchanged.

No artificial viewport-edge seed is added: an all255 certificate stays255,
without a window-edge vignette. Narrow known islands may fade nearly away.
The existing conservative inset remains; this change smooths its contrast and
does not reclaim uncertain support. One double distance plane plus bounded line,
envelope and cut scratch is under4.1MiB CPU scratch at960x540, allocated only on
mask rebuild. All new scans/comparisons/envelope pushes/pops/evaluation/conversion
consume the SAME10M budget. Failure still clears the entire local mask. No
new GPU target, texture, shader fetch or idle submission is introduced.
