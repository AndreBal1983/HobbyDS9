"""Finalize only additive receiving-v2 packs; preserves v1 source and runtime trees."""
import hashlib
import html
import json
import re
import struct
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
name=sys.argv[1]
assert name in ['system-receiving-witness-v2','hulls-runtime-v2']
pack=ROOT/'art/asset-production-01'/name
m=json.loads((pack/'manifest.json').read_text())
existing=set()
for path in (ROOT/'client/HobbyDS9.Unity/Assets').rglob('*.meta'):
    match=re.search(r'^guid: ([a-f0-9]{32})$',path.read_text(encoding='utf-8',errors='replace'),re.M)
    if match:existing.add(match[1])
rows=[]
for path in sorted(pack.rglob('*')):
    if path.is_file() and path.parent.name in ['Models','Textures']:
        relative=path.relative_to(pack).as_posix()
        guid=hashlib.sha256(('hobbyds9.art.production02/'+name+'/'+relative).encode()).hexdigest()[:32]
        assert guid not in existing
        row={'path':relative,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'bytes':path.stat().st_size,
            'proposed_guid':guid,'proposed_unity_path':'Assets/HobbyDS9/ArtProduction02/'+name+'/'+relative,
            'purpose':'QA witness only; no runtime admission' if name.startswith('system') else 'Proposed original hull runtime candidate'}
        if path.suffix=='.png':row['image_size']=list(struct.unpack_from('>II',path.read_bytes(),16))
        rows.append(row)
assert len({r['proposed_guid'] for r in rows})==len(rows)
receiving={'status':'not imported; QA/GUI own receiving','files':rows,'assets':m['assets'],
    'existing_project_guids_checked':len(existing),'guid_collisions':0,
    'runtime_policy':'Keep PR106 globe UVs unchanged; witness only' if name.startswith('system') else
        'New exact candidate route/material binder required; never overwrite or relabel unreceived approved external models'}
(pack/'receiving.json').write_bytes((json.dumps(receiving,indent=2)+'\n').encode())
cards=''.join('<figure><img src="Previews/'+html.escape(p.name)+'"><figcaption>'+html.escape(p.stem.replace('_',' '))+
              '</figcaption></figure>' for p in sorted((pack/'Previews').glob('*.png')))
page='''<!doctype html><meta charset="utf-8"><title>HobbyDS9 receiving v2</title><style>
body{margin:24px;background:#070d17;color:#dbe5ee;font:16px Segoe UI,sans-serif}h1{font-size:24px}
p{color:#aebfd2;max-width:1150px}main{display:grid;grid-template-columns:repeat(3,1fr);gap:18px}
figure{margin:0;border:1px solid #27384a;background:#03060a}img{width:100%;height:280px;object-fit:contain}
figcaption{font-size:13px;padding:10px}</style><h1>HobbyDS9 · '''+name+'''</h1>
<p>Actual asset proofs rendered in Blender CPU. Formula proxy images are not Unity captures.
Native axis conversion, URP appearance and owner acceptance remain open. System witness preserves v1 UVs;
its X-reflection views predict one receiver basis and do not establish a native defect.</p><main>'''+cards+'</main>'
(pack/'gallery.html').write_bytes(page.encode())
for path in pack.rglob('*'):
    if path.is_file() and path.suffix in ['.json','.txt','.html','.md']:
        path.write_bytes(path.read_text(encoding='utf-8-sig').encode())
m['files']=[{'path':p.relative_to(pack).as_posix(),'bytes':p.stat().st_size,
             'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
            for p in sorted(pack.rglob('*')) if p.is_file() and p.name!='manifest.json']
(pack/'manifest.json').write_bytes((json.dumps(m,indent=2)+'\n').encode())
print(json.dumps({'pack':name,'assets':len(m['assets']),'files':len(m['files']),
                  'bytes':sum(r['bytes'] for r in m['files']),'guid_collisions':0}))
