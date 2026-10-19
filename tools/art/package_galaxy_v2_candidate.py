"""Copy CPU-prepared candidate files outside Unity Assets, pin every payload.

Does not activate/import the candidate or alter the original owner package.
"""
from pathlib import Path
import hashlib
import json
import shutil
import sys

ROOT=Path(__file__).resolve().parents[2]
SOURCE=Path(sys.argv[1]) if len(sys.argv)>1 else ROOT/'artifacts/galaxy-v2-prepared'
TARGET=ROOT/'assets/art/galaxy-class-meshy-v2-runtime'
files=['derivative-manifest.json']
files += [f'Models/GalaxyMeshyV2_LOD{i}.fbx' for i in range(3)]
files += [f'Textures/{role}.{ext}' for role,ext in [('BaseColor','jpg'),('Normal','jpg'),('Emission','jpg'),('MetallicRoughness','jpg'),('MetallicSmoothness','png')]]
files += [f'Validation/{name}.json' for name in ['cpu-validation','fbx-cpu-validation','tangent-repair']]
records=[]
for name in files:
    source=SOURCE/name;target=TARGET/name
    target.parent.mkdir(parents=True,exist_ok=True)
    if source.suffix=='.json':
        target.write_bytes(('\n'.join(line.rstrip() for line in source.read_text(encoding='utf-8').splitlines())+'\n').encode('utf-8'))
    else: shutil.copyfile(source,target)
    records.append({'file':name,'bytes':target.stat().st_size,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()})
manifest={'assetId':'fed.galaxy-meshy-v2.standin.v1','status':'CPU-prepared candidate; not Unity activated',
    'ownerSourceCommit':'22a5ad955e3e45a01981b4bc7fedcdb256718b68','sourceGlbSha256':'3464ee3faa5aa4e41d01baf08d7403ebb28075a950dceb311b68e467660a67aa',
    'files':records,'scope':'Outside Unity Assets; no existing catalog, mapping, shaders or release assets changed',
    'runtimeTextureInputs':['BaseColor.jpg','Normal.jpg','Emission.jpg','MetallicSmoothness.png'],
    'sourceOnlyTexture':'MetallicRoughness.jpg retained for byte/channel provenance, not an added AO or runtime map',
    'pending':['Receiving pinned Unity import/metas/material/prefab/LODs','Native finite unit tangent frames with UV-gradient fallback if needed',
        'Texture-aware preview adapter and real textured graphics evidence','Explicit reversible appearance binding/activation by coordinator']}
(TARGET/'candidate-manifest.json').write_bytes((json.dumps(manifest,indent=2)+'\n').encode('utf-8'))
print(f'CPU candidate packaged: {len(records)} files, {sum(r["bytes"] for r in records):,} bytes. No Unity activation.')
