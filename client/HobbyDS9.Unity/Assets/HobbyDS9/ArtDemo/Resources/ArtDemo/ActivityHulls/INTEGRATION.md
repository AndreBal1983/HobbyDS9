# Activity hull source intake

Status: source/reference candidate; native Unity import, shader appearance and executable receiving remain separate gates.

ShipPreviewStage.ShowActivityHull(exactHullVersionId) accepts a caller-disclosed hull identity only. ActivityHullCatalog.Find admits exactly hull.constitution-explorer.v1, hull.galaxy-cruiser.v1 and hull.defiant-combat.v1. Unknown IDs clear the stage and retain an honest schematic caption. It does not infer hulls from class names, roles, civilization, component quantities or the old utility hull. The existing Show(selection) lawful resolver and illustrative utility/gallery methods are unchanged.

The fixed model is presentation of the disclosed hull, not a physical fitted-equipment layout. Component counts, power, capacity, eligibility and capabilities remain portable facts. OrbitReference/ZoomReference remain manual and bounded. Gallery study and cutaway setters do not modify a fixed activity hull. Same-identity refresh retains the model, texture and orbit; Clear/Dispose release the private preview materials and model normally.

Constitution: original procedural Constitution-style Explorer, eight explicit material slots, LOD0 6,592 and LOD1 2,500 triangles. Defiant: owner-provided local derivative, LOD0 29,998 and LOD1 7,998 triangles; source -X bow corrected before intake. New primary resources: four FBXs, three 2K JPEGs. Sources, full GLBs and Blender files remain in the external art packet.

Galaxy uses the already imported classic assembly: hull/core/blue deflector LOD0 and classic nacelle pair LOD1, 70,377 source triangles. This deliberately reuses the existing two classic nacelle resources; it does not import another Galaxy pack. Socket wrappers preserve imported FBX root TRS and apply metre socket anchors once. An outer wrapper normalizes presentation size.

FBX import requests authored normals and tangent generation, no cameras/lights/colliders/animation. New hull runtime materials bind by the received source submesh slot order rather than generated importer material names. Defiant samples sRGB base colour, linear tangent-space normal, and linear roughness G / metallic B. The shader retains controlled studio shading and a scalar/specular approximation; this is not physically matched Blender/glTF lighting. The new metallic/roughness branch defaults off, retaining old gallery scalar behavior.

CPU source checks: exact catalogue probe passed three admitted/five rejected IDs without Unity initialization; ArtRuntime Player and the new Editor-test source reference compilation passed zero warnings/errors. Selected binary FBXs contain finite exported normal arrays; GLB UV/normals and embedded/extracted JPEG hashes were verified in the source packet. Tests for actual meshes, lifecycle, orbit and camera clipping are supplied but are not executed by reference compilation. HLSL output and actual texture/native appearance remain unaccepted.

See CREDITS.txt and Provenance for rights limitations, source hashes, CPU evidence and the retained packed-image exporter warnings. Defiant license/task details are missing: local owner review only, no public redistribution clearance. Existing Galaxy CC BY-NC-SA 3.0 attribution/license remain in Designer/Galaxy.
