"""Relocate FBX + textures and reimport independently through Blender; no Unity.

blender -b --python tools/art/verify_asset_production_01_fbx.py -- system
Copies exports to a fresh explicitly scoped artifacts folder to verify relative dependencies.
"""
import json
import shutil
import sys
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
phase = sys.argv[sys.argv.index('--')+1]
assert phase in ['system', 'hulls', 'system-receiving-witness-v2', 'hulls-runtime-v2']
pack_name = phase+'-v1' if phase in ['system','hulls'] else phase
pack = ROOT / 'art/asset-production-01' / pack_name
scratch = ROOT / 'artifacts/asset-production-01-fbx-relocation' / pack_name
assert not scratch.exists(), 'Refusing to overwrite prior relocation evidence'
scratch.mkdir(parents=True)
for name in ['Models', 'Textures']:
    shutil.copytree(pack/name, scratch/name)
m = json.loads((pack/'manifest.json').read_text())
bpy.ops.wm.read_factory_settings(use_empty=True)
results, failures = [], []
for asset in m['assets']:
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for image in list(bpy.data.images):
        bpy.data.images.remove(image)
    bpy.ops.import_scene.fbx(filepath=str(scratch/asset['fbx']), use_image_search=False)
    objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    count = 0
    points = []
    for obj in objects:
        obj.data.calc_loop_triangles()
        count += len(obj.data.loop_triangles)
        if not obj.data.uv_layers:
            failures.append('No imported UV: '+asset['name'])
        points.extend([obj.matrix_world@v.co for v in obj.data.vertices])
    actual = [[min(v[a] for v in points) for a in range(3)],
              [max(v[a] for v in points) for a in range(3)]]
    expected = [[min(v['bounds_blender'][0][a] for v in asset['meshes']) for a in range(3)],
                [max(v['bounds_blender'][1][a] for v in asset['meshes']) for a in range(3)]]
    error = max(abs(actual[i][a]-expected[i][a]) for i in range(2) for a in range(3))
    if count != sum(o['triangles'] for o in asset['meshes']):
        failures.append('Triangle count: '+asset['name'])
    if error > .002:
        failures.append('Bounds/metre scale: '+asset['name']+' '+str(error))
    dependencies = []
    for image in bpy.data.images:
        if image.source != 'FILE':
            continue
        path = Path(bpy.path.abspath(image.filepath)).resolve()
        dependencies.append(str(path))
        if not path.is_relative_to(scratch.resolve()) or not path.is_file():
            failures.append('Nonrelocatable texture: '+asset['name']+' '+str(path))
    results.append({'asset': asset['name'], 'triangles': count, 'world_bounds_metres': actual,
                    'max_bounds_error_metres': error, 'relocated_texture_paths': dependencies})
report = {'pack': pack.name, 'method': 'FBX reimport from relocated Models/Textures; no image search',
          'passed': not failures, 'failures': failures, 'assets': results,
          'limitation': 'Blender FBX reimport is not Unity importer or URP verification'}
report_path = ROOT/'artifacts'/'asset-production-01-fbx-relocation'/(phase+'-report.json')
report_path.write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
print('FBX_VERIFY', report['passed'], len(results), json.dumps(failures), flush=True)
if failures:
    raise RuntimeError('FBX verification failed')
