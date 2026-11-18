"""CPU-only derivative of the verified owner Meshy v2 source.

Run Blender --background --factory-startup --threads 2 --python this_file.py.
No render, provider call, active scene access, gameplay or Unity activation.
"""
import bpy
import bmesh
import hashlib
import json
import math
import struct
import sys
import time
from pathlib import Path
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'assets/art/galaxy-class-meshy-v2/galaxy-class-v2.glb'
OUT = Path(sys.argv[sys.argv.index('--') + 1]) if '--' in sys.argv else ROOT / 'artifacts/galaxy-v2-prepared'
LENGTH = 620.0
TARGETS = (60000, 20000, 7000)
SOURCE_SHA = '3464ee3faa5aa4e41d01baf08d7403ebb28075a950dceb311b68e467660a67aa'


def digest(data): return hashlib.sha256(data).hexdigest()
def log(message): print('GALAXY_V2 ' + message, flush=True)
def triangles(mesh): mesh.calc_loop_triangles(); return len(mesh.loop_triangles)


def extract_images():
    data = SOURCE.read_bytes()
    assert len(data) == 56507400 and digest(data) == SOURCE_SHA, 'Unexpected source bytes'
    n = struct.unpack_from('<I', data, 12)[0]
    gltf = json.loads(data[20:20+n]); binary = data[28+n:]
    roles = ('BaseColor', 'MetallicRoughness', 'Normal', 'Emission')
    records = []
    for role, image in zip(roles, gltf['images']):
        view = gltf['bufferViews'][image['bufferView']]
        payload = binary[view.get('byteOffset', 0):view.get('byteOffset', 0)+view['byteLength']]
        path = OUT / 'Textures' / (role + '.jpg')
        path.write_bytes(payload)
        records.append({'role': role, 'file': path.relative_to(OUT).as_posix(), 'bytes': len(payload), 'sha256': digest(payload)})
    return gltf, records


start = time.monotonic()
for name in ('Models', 'Textures', 'Sources', 'Validation'): (OUT/name).mkdir(parents=True, exist_ok=True)
gltf, textures = extract_images()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.unit_settings.system = 'METRIC'
bpy.context.scene.unit_settings.scale_length = 1
log('import source')
bpy.ops.import_scene.gltf(filepath=str(SOURCE), import_shading='NORMALS', merge_vertices=False)
objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
assert len(objects) == 1 and len(objects[0].data.materials) == 1
original = objects[0]
source_triangles = triangles(original.data)
assert source_triangles == 1067410
log(f'imported {source_triangles} triangles, {len(original.data.vertices)} vertices')

# Retain exact source JPEGs, not recompressed Blender image exports.
by_sha = {t['sha256']: t for t in textures}
image_records = []
for image in list(bpy.data.images):
    if image.packed_file:
        h = digest(bytes(image.packed_file.data))
        assert h in by_sha, ('Unexpected embedded image', image.name, h)
        record = by_sha[h]
        colour_space = image.colorspace_settings.name
        # Imported buffer images have blank original paths; unpacking those can
        # retain stale Image_0.jpg references. Remap to explicit owned files.
        loaded = bpy.data.images.load(str(OUT / record['file']), check_existing=False)
        loaded.name = record['role']; loaded.colorspace_settings.name = colour_space
        image.user_remap(loaded); bpy.data.images.remove(image)
        image_records.append({'role': record['role'], 'size': list(loaded.size), 'colourSpace': colour_space})
        log(f'image {record["role"]}: {tuple(loaded.size)} / {colour_space}')
assert len(image_records) == 4

# Exact-position welding removes glTF seam-duplicated geometric vertices while
# preserving per-loop UV coordinates/charts; it does not unwrap or bake maps.
original.data.transform(original.matrix_world)
original.matrix_world = Matrix.Identity(4)
bm = bmesh.new(); bm.from_mesh(original.data)
before = len(bm.verts)
bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-7)
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
bm.to_mesh(original.data); bm.free()
log(f'weld: {before} -> {len(original.data.vertices)} geometric vertices')

# glTF Y-up imports to Blender Z-up; raw bow -X becomes Blender -Y by +90Z.
original.data.transform(Matrix.Rotation(math.pi/2, 4, 'Z'))
bpy.context.view_layer.update()
points = [Vector(v.co) for v in original.data.vertices]
lo = Vector(tuple(min(v[i] for v in points) for i in range(3)))
hi = Vector(tuple(max(v[i] for v in points) for i in range(3)))
centre = (lo+hi)/2; scale = LENGTH/(hi.y-lo.y)
original.data.transform(Matrix.Scale(scale, 4) @ Matrix.Translation(-centre))
del points
original.name = 'SourceForReduction'

root = bpy.data.objects.new('GalaxyMeshyV2Candidate', None)
bpy.context.scene.collection.objects.link(root)
anchors = []
for name, point in [('Axis_Bow', (0, -LENGTH/2, 0)), ('Axis_Up', (0, 0, LENGTH*.15))]:
    marker = bpy.data.objects.new(name, None); bpy.context.scene.collection.objects.link(marker)
    marker.parent = root; marker.location = point; anchors.append(marker)

records = []
for level, target in enumerate(TARGETS):
    log(f'LOD{level} target {target}: start')
    obj = original.copy(); obj.data = original.data.copy(); bpy.context.scene.collection.objects.link(obj)
    obj.name = f'GalaxyMeshyV2_LOD{level}'; obj.parent = root
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active = obj
    modifier = obj.modifiers.new('UV-preserving collapse', 'DECIMATE')
    modifier.decimate_type = 'COLLAPSE'; modifier.ratio = target/source_triangles
    modifier.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    mesh = obj.data
    mesh.calc_loop_triangles()
    for face in mesh.polygons: face.use_smooth = True
    count = triangles(mesh)
    assert count <= target*1.02 and count > target*.75, (target, count)
    assert len(mesh.uv_layers) == 1 and len(mesh.materials) == 1
    uvs = [tuple(loop.uv) for loop in mesh.uv_layers.active.data]
    assert all(math.isfinite(x) and -.001 <= x <= 1.001 for uv in uvs for x in uv)
    bounds = [Vector(c) for c in obj.bound_box]
    dimensions = [max(v[i] for v in bounds)-min(v[i] for v in bounds) for i in range(3)]
    assert abs(dimensions[1]-LENGTH) < LENGTH*.015
    for part in [root, obj] + anchors: part.select_set(True)
    fbx = OUT/'Models'/f'GalaxyMeshyV2_LOD{level}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={'EMPTY','MESH'},
        add_leaf_bones=False, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
        global_scale=1, bake_anim=False, use_mesh_modifiers=True, mesh_smooth_type='FACE', path_mode='RELATIVE')
    records.append({'level': level, 'targetTriangles': target, 'triangles': count, 'vertices': len(mesh.vertices),
        'loops': len(mesh.loops), 'uvLayers': len(mesh.uv_layers), 'materialSlots': len(mesh.materials),
        'blenderSizeXYZMetres': dimensions, 'fbx': fbx.relative_to(OUT).as_posix(),
        'fbxBytes': fbx.stat().st_size, 'fbxSha256': digest(fbx.read_bytes())})
    log(f'LOD{level} exported: {count} triangles, {len(mesh.vertices)} vertices')
    if level == 0:
        glb = OUT/'Validation'/'GalaxyMeshyV2_LOD0.glb'
        bpy.ops.export_scene.gltf(filepath=str(glb), export_format='GLB', use_selection=True,
            export_yup=True, export_apply=True, export_cameras=False, export_lights=False, export_tangents=True)
    obj.hide_set(True)

bpy.data.objects.remove(original, do_unlink=True)
for obj in bpy.context.scene.objects:
    if obj.type == 'MESH':
        obj.hide_set(not obj.name.endswith('_LOD0'))
        obj.hide_render = not obj.name.endswith('_LOD0')
for image in bpy.data.images:
    if image.source == 'FILE': image.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Sources'/'GalaxyMeshyV2_Optimized.blend'), compress=True)
report = {'sourceCommit': '22a5ad955e3e45a01981b4bc7fedcdb256718b68', 'sourceGlbSha256': SOURCE_SHA,
    'sourceTriangles': source_triangles, 'sourceVertices': before, 'blenderVersion': bpy.app.version_string,
    'status': 'CPU-prepared candidate; Unity/textured-preview/mapping activation pending',
    'nominalArtLengthMetres': LENGTH, 'rawGlTfBow': '-X', 'rawGlTfUp': '+Y',
    'authoringBow': '-Y', 'authoringUp': '+Z', 'expectedEngineBow': '+Z', 'expectedEngineUp': '+Y',
    'pivot': 'source bounding-box centre', 'sourceToBlenderScale': scale,
    'textureExtraction': textures, 'blenderImages': image_records, 'lods': records,
    'elapsedSeconds': time.monotonic()-start, 'recipe': 'tools/art/prepare_galaxy_v2.py',
    'notes': ['Original source remains intact.', 'No mesh generation, GPU render or Unity invocation.',
        'Collapse simplification retains/interpolates per-loop UV charts; no re-unwrap/retexture.',
        'One material/double-sided source; no new equipment interfaces or gameplay statistics.',
        'FBX axes and texture fidelity still require receiving Unity graphics validation.']}
(OUT/'derivative-manifest.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
log(f'CPU_PREP_COMPLETE {report["elapsedSeconds"]:.1f}s')
