# Designer hull reference increment

Source candidate, 24 October 2026. No Unity/GPU/native receiving or final art acceptance is claimed here.

The overview retains a large central hull reference beside authoritative draft capacity, power, capabilities and costs. Its installed-package summary updates from the actual draft. No exterior mesh changes are invented for component edits, internal equipment or mount compatibility.

`ShipDesignerPreview.ReferenceKey` binds only exact `hull.utility.v1` to the original `fed.exploration-cruiser.v1` art-library entry. Its caption explicitly says **Illustrative hull reference · equipment shown schematically**. Other hull IDs use a generic group schematic. This presentation mapping does not create a civilization identity, ship design, physical socket or gameplay equivalence.

`ShipPreviewStage.ShowArtReference` is separate from `Show(ShipPreviewSelection)`. The existing owned/lawfully-visible ship resolver and its exact civilization/design binding remain unchanged. The reference uses the already tracked original fleet assets documented in [the art runtime contract](demo-art-runtime-contract.md); the high-poly owner-selected Galaxy source is not imported by this increment.

One 768×480 stage is created on attachment and released on detach or explicit view disposal. Same-hull Overview refresh updates only facts, installed list and issues/comparison while retaining the attached stage/image. Changing pages or hull recreates its reference; reattaching a retained view creates a fresh stage after releasing the previous image. Rendering is manual once per attachment, with no timers, automatic rotation or frame loop. Logical color/depth storage is approximately 2.46 MiB before driver overhead; this is not a measured GPU memory/performance guarantee.

Reference compilation must first build `prototypes/HobbyDS9.UnityReviewSplitCompile/ArtRuntime/ArtRuntime.csproj` against the pinned Unity/URP references, then compile DefaultRuntime using that fresh ArtDemo DLL. Historical ArtDemo DLLs do not contain the additive reference seam. Engine receiving must exercise attached preview, same-hull refresh, detach/reattach, explicit disposal, fallback, actual material/framing and three screen sizes. Added tests are source until executed by Unity; reference compilation alone does not establish their behavior.

Shared capacity, power profiles and mount family/size/location compatibility remain portable backend rules. A standardized visual anchor never makes every module fit every hull; a large forward deflector still requires its actual compatible forward mount.
