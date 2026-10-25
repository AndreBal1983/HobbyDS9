# Three distinct activity hulls — local CPU asset handoff

Selected assets: **Constitution/** and **Defiant-v3/**. `Defiant/` and `Defiant-v2/` are preserved superseded candidates, not intake resources. Keep source blend files and proofs outside Unity Assets. No native Unity import or rendering was performed for this packet.

| Gameplay hull | Exact visual key | Primary resource | LOD0 / LOD1 triangles |
| --- | --- | --- | --- |
| hull.constitution-explorer.v1 | activity.constitution-explorer.v1 | Constitution/Models/ConstitutionExplorer_LOD0.fbx | 6,592 / 2,500 |
| hull.galaxy-cruiser.v1 | activity.galaxy-cruiser.v1 | Existing galaxy-unity-receiving-20261024-1 pack: classic Galaxy assembly | 82,860 / 20,617 |
| hull.defiant-combat.v1 | activity.defiant-combat.v1 | Defiant-v3/Models/Defiant_LOD0.fbx | 29,998 / 7,998 |

Class and ready blueprint IDs are recorded in delivery-contract.json. These are real distinct silhouettes, not aliases of hull.utility.v1. Artwork does not confer stats, compatible mounts, unlocks, research or activity capabilities.

## Geometry, import and presentation

All selected single-hull models have vertex geometry centred near the origin, +Z bow / +Y up in the explicit OBJ proof coordinate system; metre presentation dimensions. Constitution has exact 300m longitudinal span, Defiant approximately 170m. These art dimensions are not simulation distances/capacity rules. Defiant source -X was identified as its narrow bow after the provisional v2 CPU silhouette was reviewed; v3 corrects the direction.

Use FBX imported hierarchy unchanged beneath an identity presentation wrapper; preserve imported root scale/axis rotation. Do not reset imported FBX root TRS and do not apply Galaxy's separate source conversion to the new hulls. For normalized preview framing, scale an OUTER wrapper using assembled renderer bounds, not mesh identity. Validate imported world bounds and bow direction in Unity before acceptance.

Constitution: eight named scalar material roles in Constitution/asset-contract.json, no texture requirement. Preserve/exported normals. Defiant: one material, exactly three original 2048x2048 JPEG packed textures. `Defiant_Image_0.jpg` is sRGB base colour; `Defiant_Image_1.jpg` is linear glTF metallic/roughness (G roughness, B metallic); `Defiant_Image_2.jpg` is linear tangent-space normal. No separate emission map. Bind by explicit roles, not guessed importer names. A shader supporting only scalar roughness/metallic must disclose that it does not yet sample the metallic/roughness map.

FBX binary parsing independently confirmed finite unit normal arrays with ByPolygonVertex/IndexToDirect mappings in all four new selected FBXs. GLB meshes contain NORMAL and TEXCOORD_0 on every primitive. OBJ is a CPU silhouette proof/fallback geometry artifact, lacking vn normals; it is not the preferred runtime import.

## Validation and limits

cpu-validation.json checks exported OBJ geometry, GLB lengths/buffer bounds, triangle counts, normal/UV attributes, decodes embedded images, and independently decodes extracted JPEGs. All three embedded Defiant images match extracted packed source bytes by SHA256. fbx-normal-validation.json checks actual binary normal arrays. Proofs are CPU painter rasters with per-face shading and centroid texture samples: apparent facets do not establish actual runtime smooth shading quality. They demonstrate silhouette/orientation only. No native Unity materials, texture fidelity, lighting, draw-call or performance acceptance is claimed.

The Defiant Blender process exited 0, but the log includes a glTF sampler-choice warning and `Image "" not available. Keeping packed image` shutdown messages inherited from packed-image handling. These are retained openly. Independent GLB/image validation found no missing image payload or undecodable textures; this does not suppress the warnings or certify Unity import. Selected derivatives passed mesh.validate before export.

Expected native receiving: finite plausible bounds; full hull initially visible at each supported window size; three distinct silhouettes keyed by the actual new hull IDs; correct bow; smooth saucer / Defiant surfaces with imported normals; correct actual UV base colour and normal sampling; no utility-hull or Galaxy spoof fallback; orbit/zoom and reentry retain disposal/render-on-change invariants. Native checks remain outstanding.

## Rights and provenance

Constitution is new original procedural Constitution-style geometry using the project's owned fleet helper recipe; no third-party mesh or image pixels. It is a recognizable silhouette study, not an exact canonical model. Its source script, helper snapshot/hash and editable blend are retained.

Galaxy remains the existing Chris Kuhn / Kuhn Industries Big D derivative under CC BY-NC-SA 3.0. Preserve its original attribution, source license and derivative notices from the existing pack. Use classic nacelles for the fixed Galaxy Cruiser; Sovereign gallery study is not a new technology inference.

Defiant is an owner-provided Meshy-textured source, SHA256 15526c4097f98ddf147f9fa6c28c44f4ceecb54ae8843076a5d61df9f4008f82. Source task ID and license were not supplied. This packet is a locally authorized inspection/derivative for owner review, with no public redistribution clearance claimed. Preserve Sources/owner-defiant-provenance.json and source. Do not invent Meshy license terms or publish it as cleared stock artwork.

Owned CPU processes have terminal ledgers; no renderer was invoked and the protected owner Blender GUI PID18048 was untouched. Delivery manifest hashes exclude its own file and list payload explicitly. Earlier candidates/logs are retained as audit evidence.
