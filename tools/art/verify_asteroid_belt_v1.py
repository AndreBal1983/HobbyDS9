"""Reimport exports in background Blender; record geometry evidence, not Unity acceptance."""
import hashlib
import json
import math
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'art/system-richness-01/belt-v1'
expected = json.loads((OUT/'geometry-validation.json').read_text())
checks = []
for spec in expected['assets']:
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(OUT/spec['path']))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    assert len(meshes) == 1, spec['path']
    obj = meshes[0]
    count = sum(len(p.vertices)-2 for p in obj.data.polygons)
    assert count == spec['triangles'], spec['path']
    assert len(obj.data.materials) == 1
    assert all(math.isfinite(x) for v in obj.data.vertices for x in v.co)
    assert all(v.normal.length > .99 for v in obj.data.vertices)
    world = [obj.matrix_world @ v.co for v in obj.data.vertices]
    center = [(min(v[i] for v in world)+max(v[i] for v in world))/2 for i in range(3)]
    row = {'path':spec['path'], 'meshes':1, 'triangles':count, 'materialSlots':1,
           'finiteVertices':True, 'unitNormals':True, 'worldBoundsCenter':center,
           'uvLayers':len(obj.data.uv_layers), 'colorLayers':len(obj.data.color_attributes)}
    if 'SoftAnnulus' in spec['path']:
        assert len(obj.data.uv_layers) == 1
        assert max(abs(v.z) for v in world) < .0001
        assert abs(max(v.length for v in world)-1.24) < .0001
        row['restoredBlenderXYPlane'] = True
    else:
        assert len(obj.data.color_attributes) >= 1
    checks.append(row)
(OUT/'validation-fbx.json').write_text(json.dumps({'schema':1,
    'status':'Blender FBX reimport only; Unity/native pending', 'assets':checks},indent=2)+'\n', encoding='utf-8', newline='\n')
manifest = json.loads((OUT/'manifest.json').read_text())
runtime = [p for p in sorted((OUT/'Models').glob('*.fbx')) if 'Example' not in p.name]
runtime += sorted((OUT/'Shaders').glob('*.shader'))
manifest['proposedRuntimeReceiving'] = {
    'status':'paths only; not imported; shared material remaps and native validation required',
    'meshImport':'scale1, preserve normals/vertex colors/UV; no auto materials, animation, lights or cameras; prototypes Read/Write on for bounded combine',
    'paths':[{'source':p.relative_to(OUT).as_posix(),
              'unityPath':'Assets/HobbyDS9/Review/Resources/SystemBodyArt/AsteroidBeltV1/'+p.name,
              'resourceKey':'SystemBodyArt/AsteroidBeltV1/'+p.stem,
              'bytes':p.stat().st_size, 'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
             for p in runtime],
    'sourceBytes':sum(p.stat().st_size for p in runtime),
    'bakedExamplesExcluded':True, 'newRasterTextures':0}
manifest['files'] = []
for p in sorted(OUT.rglob('*')):
    if p.is_file() and p.name != 'manifest.json':
        data = p.read_bytes()
        manifest['files'].append({'path':p.relative_to(OUT).as_posix(), 'bytes':len(data),
                                 'sha256':hashlib.sha256(data).hexdigest()})
manifest['sourceFiles'] = []
for name in ['build_asteroid_belt_v1.py','verify_asteroid_belt_v1.py']:
    p = ROOT/'tools/art'/name
    manifest['sourceFiles'].append({'path':p.relative_to(ROOT).as_posix(),
        'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n', encoding='utf-8', newline='\n')
print('VERIFIED',len(checks),'FBX files; exact triangles, finite geometry, normals and color/UV layers.')
