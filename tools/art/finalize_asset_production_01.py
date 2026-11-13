"""Write additive receiving metadata/contact gallery and refresh exact-byte manifest."""
import hashlib
import html
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
pack = ROOT / 'art/asset-production-01' / (sys.argv[1] + '-v1')
m = json.loads((pack / 'manifest.json').read_text())
existing = set()
for p in (ROOT / 'client/HobbyDS9.Unity/Assets').rglob('*.meta'):
    match = re.search(r'^guid: ([a-f0-9]{32})$', p.read_text(encoding='utf-8', errors='replace'), re.M)
    if match:
        existing.add(match.group(1))
inventory = []
for p in sorted(pack.rglob('*')):
    if p.is_file() and p.parent.name in ['Models', 'Textures']:
        relative = p.relative_to(pack).as_posix()
        proposed = 'Assets/HobbyDS9/ArtProduction01/' + pack.name + '/' + relative
        guid = hashlib.sha256(('hobbyds9.art.production01/' + pack.name + '/' + relative).encode()).hexdigest()[:32]
        assert guid not in existing, 'Proposed GUID already present: ' + proposed
        inventory.append({'path': relative, 'proposed_unity_path': proposed,
                          'proposed_guid': guid, 'sha256': hashlib.sha256(p.read_bytes()).hexdigest(),
                          'bytes': p.stat().st_size})
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
m['phase'] = sys.argv[1]
m['files'] = [{'path': p.relative_to(pack).as_posix(), 'bytes': p.stat().st_size,
               'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}
              for p in sorted(pack.rglob('*')) if p.is_file() and p.name != 'manifest.json']
(pack / 'manifest.json').write_text(json.dumps(m, indent=2)+'\n', encoding='utf-8')
print(json.dumps({'pack': pack.name, 'assets': len(m['assets']), 'files': len(m['files']),
                  'bytes': sum(x['bytes'] for x in m['files']), 'guid_collisions': 0}))
