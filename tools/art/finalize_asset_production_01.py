"""Write additive receiving metadata/contact gallery and refresh exact-byte manifest."""
import hashlib
import html
import json
import re
import shutil
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
pack = ROOT / 'art/asset-production-01' / (sys.argv[1] + '-v1')
m = json.loads((pack / 'manifest.json').read_text())
if sys.argv[1] == 'system':
    (pack/'Shaders').mkdir(exist_ok=True)
    shutil.copyfile(ROOT/'tools/art/shaders/PlanetLimb01.shader', pack/'Shaders/PlanetLimb01.shader')
    (pack/'Textures/ATTRIBUTION.txt').write_text(
        'Solar body JPEG maps: Solar System Scope / INOVE.\n'
        'Source: https://www.solarsystemscope.com/textures/\n'
        'License: CC BY 4.0, https://creativecommons.org/licenses/by/4.0/\n'
        'License page checked 2026-11-13. JPEGs copied unchanged from the local\n'
        'repository art handoff. Source paths and SHA-256 are in manifest.json.\n'
        'No new remote texture download or remote-byte equivalence is claimed.\n'
        'OriginalCorona.png and DecorativeStars.png are original procedural art\n'
        'created for HobbyDS9, not Solar System Scope maps.\n', encoding='utf-8')
material_recipes = {}
for asset in m['assets']:
    raw = (pack/asset['glb']).read_bytes()
    length = struct.unpack_from('<I', raw, 12)[0]
    gltf = json.loads(raw[20:20+length])
    for material in gltf.get('materials', []):
        name = material['name']
        pbr = material.get('pbrMetallicRoughness', {})
        recipe = {'shader': 'Universal Render Pipeline/Lit',
                  'base_color_linear_factor': pbr.get('baseColorFactor', [1,1,1,1]),
                  'metallic': pbr.get('metallicFactor', 1),
                  'smoothness': 1-pbr.get('roughnessFactor', 1),
                  'emission_color_linear': material.get('emissiveFactor', [0,0,0]),
                  'emission_strength': material.get('extensions', {}).get('KHR_materials_emissive_strength', {}).get('emissiveStrength', 1),
                  'alpha_mode': material.get('alphaMode', 'OPAQUE'),
                  'double_sided': material.get('doubleSided', False)}
        if 'baseColorTexture' in pbr:
            index = pbr['baseColorTexture']['index']
            image = gltf['images'][gltf['textures'][index]['source']]
            recipe['texture_image_name'] = image.get('name', 'see GLB embedded image')
            recipe['texture_color_space'] = 'sRGB'
        if name == 'Atmosphere_Placeholder':
            recipe = {'shader_source': 'Shaders/PlanetLimb01.shader', 'enabled_by_default': False,
                      '_BaseColor': [.035,.24,.65,.25], '_LimbPower': 5,
                      'note': 'Replace exported solid placeholder before enabling. Native shader compilation unexecuted.'}
        if name == 'Saturn_Rings':
            recipe['double_sided'] = True
        if name == 'Corona_Alpha_Emission':
            recipe.update({'alpha_mode': 'BLEND', 'z_write': False, 'cast_shadows': False,
                           'camera_facing': True, 'double_sided': True})
        material_recipes[name] = recipe
(pack/'material-recipes.json').write_text(json.dumps(material_recipes, indent=2)+'\n', encoding='utf-8')

def canonicalize_text():
    # Byte-pinned generated text must not depend on Windows newline translation.
    for p in pack.rglob('*'):
        if p.is_file() and p.suffix in ['.json', '.html', '.txt', '.shader', '.md']:
            p.write_bytes(p.read_text(encoding='utf-8-sig').encode('utf-8'))

canonicalize_text()
existing = set()
for p in (ROOT / 'client/HobbyDS9.Unity/Assets').rglob('*.meta'):
    match = re.search(r'^guid: ([a-f0-9]{32})$', p.read_text(encoding='utf-8', errors='replace'), re.M)
    if match:
        existing.add(match.group(1))
inventory = []

def dimensions(path):
    data = path.read_bytes()
    if data[:8] == b'\x89PNG\r\n\x1a\n':
        return struct.unpack_from('>II', data, 16)
    if data[:2] == b'\xff\xd8':
        offset = 2
        while offset < len(data):
            if data[offset] != 255:
                offset += 1
                continue
            while data[offset] == 255:
                offset += 1
            marker = data[offset]
            offset += 1
            if marker in [0xD8, 0xD9]:
                continue
            length = struct.unpack_from('>H', data, offset)[0]
            if marker in [0xC0, 0xC1, 0xC2]:
                h, w = struct.unpack_from('>HH', data, offset+3)
                return w, h
            offset += length
    raise ValueError('Unknown image dimensions: '+str(path))

def texture_memory(w, h):
    bc7, rgba = 0, 0
    while True:
        bc7 += ((w+3)//4)*((h+3)//4)*16
        rgba += w*h*4
        if w == h == 1:
            return {'full_mips_BC7_estimated_bytes': bc7, 'full_mips_RGBA8_estimated_bytes': rgba}
        w, h = max(1, w//2), max(1, h//2)

for p in sorted(pack.rglob('*')):
    if p.is_file() and p.parent.name in ['Models', 'Textures', 'Shaders']:
        relative = p.relative_to(pack).as_posix()
        proposed = 'Assets/HobbyDS9/ArtProduction01/' + pack.name + '/' + relative
        guid = hashlib.sha256(('hobbyds9.art.production01/' + pack.name + '/' + relative).encode()).hexdigest()[:32]
        assert guid not in existing, 'Proposed GUID already present: ' + proposed
        item = {'path': relative, 'proposed_unity_path': proposed,
                          'proposed_guid': guid, 'sha256': hashlib.sha256(p.read_bytes()).hexdigest(),
                          'bytes': p.stat().st_size}
        if p.parent.name == 'Textures' and p.suffix.lower() in ['.png', '.jpg']:
            w, h = dimensions(p)
            item.update({'width': w, 'height': h, 'sRGB': True,
                         'memory_estimate_at_source_resolution': texture_memory(w, h)})
        inventory.append(item)
assert len(set(i['proposed_guid'] for i in inventory)) == len(inventory)
receiving = {'status': 'proposed paths/GUIDs only; not imported', 'namespace': 'hobbyds9.art.production01',
             'existing_project_guids_checked': len(existing), 'collisions': 0,
             'route': 'Use FBX plus explicit URP material remaps; GLB is alternate interchange, not duplicate runtime import',
             'files': inventory, 'exact_bindings': [
                 {'asset_id': a['asset_id'], 'candidate_binding': a['candidate_binding'], 'lod': a['lod'], 'fbx': a['fbx']}
                 for a in m['assets']]}
(pack / 'receiving.json').write_text(json.dumps(receiving, indent=2)+'\n', encoding='utf-8')
previews = sorted((pack / 'Previews').glob('*.png'))
cards = ''.join('<figure><img src="Previews/'+html.escape(p.name)+'"><figcaption>'+
                html.escape(p.stem.replace('_', ' '))+'</figcaption></figure>' for p in previews)
page = '''<!doctype html><html><meta charset="utf-8"><title>HobbyDS9 asset production 01</title>
<style>*{box-sizing:border-box}body{margin:0;padding:28px;background:#070d17;color:#dbe5ee;font:16px Segoe UI,sans-serif}
h1{font-size:26px;margin:0 0 8px}p{color:#9cacc1;margin:8px 0 22px;max-width:1050px}
main{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:18px}figure{margin:0;border:1px solid #27384a;background:#03060a}
img{width:100%;height:280px;object-fit:contain;display:block}figcaption{padding:10px;color:#aebfd2;font-size:13px}
</style><h1>HobbyDS9 · '''+pack.name+'''</h1><p>Actual new asset renders · Blender Cycles CPU / AgX.
Proposed artwork; Unity import, URP appearance and owner visual acceptance remain open.
Illustrative compositions are not campaign snapshots. Direct-light/top/side views expose surface and shape.</p><main>'''+cards+'</main></html>'
(pack / 'gallery.html').write_text(page, encoding='utf-8')
canonicalize_text()
m['phase'] = sys.argv[1]
m['files'] = [{'path': p.relative_to(pack).as_posix(), 'bytes': p.stat().st_size,
               'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}
              for p in sorted(pack.rglob('*')) if p.is_file() and p.name != 'manifest.json']
(pack / 'manifest.json').write_bytes((json.dumps(m, indent=2)+'\n').encode('utf-8'))
print(json.dumps({'pack': pack.name, 'assets': len(m['assets']), 'files': len(m['files']),
                  'bytes': sum(x['bytes'] for x in m['files']), 'guid_collisions': 0}))
