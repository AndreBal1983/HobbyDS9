"""Parse generated payloads independently; this does not establish Unity import acceptance."""
from pathlib import Path
import hashlib
import json
import math
import struct

root=Path(__file__).resolve().parents[2]/'art/ships/cardassian-cruiser-v1'
manifest=json.loads((root/'manifest.json').read_text(encoding='utf-8'))
results=[]
for lod in manifest['lods']:
    raw=(root/lod['glb']).read_bytes()
    assert struct.unpack_from('<III',raw)==(0x46546c67,2,len(raw))
    size,kind=struct.unpack_from('<II',raw,12);assert kind==0x4e4f534a
    doc=json.loads(raw[20:20+size]);offset=20+size
    bin_size,bin_kind=struct.unpack_from('<II',raw,offset);assert bin_kind==0x004e4942
    payload=raw[offset+8:offset+8+bin_size]
    assert all('uri'not in b for b in doc['buffers']) and not doc.get('images')
    def values(index):
        a=doc['accessors'][index];view=doc['bufferViews'][a['bufferView']]
        width={'SCALAR':1,'VEC2':2,'VEC3':3}[a['type']]
        fmt='f'if a['componentType']==5126 else'I'
        flat=struct.unpack_from('<'+fmt*(a['count']*width),payload,view.get('byteOffset',0)+a.get('byteOffset',0))
        return [flat[i:i+width]for i in range(0,len(flat),width)]
    triangles=0;positions=[]
    for mesh in doc['meshes']:
        for p in mesh['primitives']:
            xyz=values(p['attributes']['POSITION']);normal=values(p['attributes']['NORMAL']);uv=values(p['attributes']['TEXCOORD_0']);indices=[v[0]for v in values(p['indices'])]
            assert len(xyz)==len(normal)==len(uv) and len(indices)%3==0
            assert all(0<=i<len(xyz)for i in indices)
            assert all(math.isfinite(v)for point in xyz for v in point)
            assert all(abs(sum(v*v for v in n)-1)<1e-5 for n in normal)
            for i in range(0,len(indices),3):
                a,b,c=[xyz[j]for j in indices[i:i+3]];ab=[b[j]-a[j]for j in range(3)];ac=[c[j]-a[j]for j in range(3)];cross=[ab[1]*ac[2]-ab[2]*ac[1],ab[2]*ac[0]-ab[0]*ac[2],ab[0]*ac[1]-ab[1]*ac[0]]
                assert sum(v*v for v in cross)>1e-10
                assert sum(cross[j]*normal[indices[i]][j]for j in range(3))>0
            triangles+=len(indices)//3;positions+=xyz
    assert triangles==lod['triangles']
    nodes={n['name']:n for n in doc['nodes']};assert nodes['Axis_Bow']['translation'][2]>0 and nodes['Axis_Up']['translation'][1]>0
    for anchor in manifest['anchors']:assert nodes[anchor['name']]['translation']==anchor['position']
    obj=(root/lod['obj']).read_text(encoding='utf-8').splitlines();vertices=[v for v in obj if v.startswith('v ')];faces=[f for f in obj if f.startswith('f ')]
    assert len(faces)==triangles and len(vertices)==triangles*3
    for face in faces:
        assert all(1<=int(i.split('/')[0])<=len(vertices)for i in face[2:].split())
    results.append({'lod':lod['level'],'passed':True,'triangles':triangles,'objFaces':len(faces),'glbMaterials':len(doc['materials']),'anchorCount':len(manifest['anchors']),'boundsMatch':all(abs(min(v[i]for v in positions)-lod['boundsXYZ'][0][i])<1e-4 and abs(max(v[i]for v in positions)-lod['boundsXYZ'][1][i])<1e-4 for i in range(3))})
    assert results[-1]['boundsMatch']
for file in manifest['files']:assert hashlib.sha256((root/file['file']).read_bytes()).hexdigest()==file['sha256']
report={'status':'CPU payload checks only; no Unity/Blender/GPU run','allPassed':True,'results':results}
(root/'validation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))
