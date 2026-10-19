"""Repair only zero-length diagnostic GLB tangents using its existing UV faces.

No source/UV/normal/image/FBX edits. Unity FBX tangent acceptance stays pending.
"""
from pathlib import Path
import hashlib
import json
import struct
import sys
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
PACK = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT/'artifacts/galaxy-v2-prepared'
file = PACK/'Validation/GalaxyMeshyV2_LOD0.glb'
receipt_file = PACK/'Validation/tangent-repair.json'
data = bytearray(file.read_bytes()); before = hashlib.sha256(data).hexdigest()
if receipt_file.exists() and json.loads(receipt_file.read_text())['afterSha256'] == before:
    print('Diagnostic tangent repair already verified.'); sys.exit(0)
n = struct.unpack_from('<I',data,12)[0]; g = json.loads(data[20:20+n]); binary_start = 28+n


def accessor(index):
    a=g['accessors'][index];v=g['bufferViews'][a['bufferView']]
    assert 'byteStride' not in v
    width={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
    offset=binary_start+v.get('byteOffset',0)+a.get('byteOffset',0)
    return np.frombuffer(data,dtype={5126:'<f4',5125:'<u4',5123:'<u2'}[a['componentType']],count=a['count']*width,offset=offset).reshape(-1,width)


p=g['meshes'][0]['primitives'][0];a=p['attributes']
positions=accessor(a['POSITION']);normals=accessor(a['NORMAL']);uv=accessor(a['TEXCOORD_0'])
tangents=accessor(a['TANGENT']);faces=accessor(p['indices']).reshape(-1,3)
lengths=np.linalg.norm(tangents[:,:3],axis=1); bad=np.flatnonzero(lengths<1e-6)
assert np.all((lengths<1e-6)|(np.abs(lengths-1)<.02)), 'Unexpected nonzero tangent corruption'
assert len(bad)<=16, 'Unexpected widespread tangent damage; inspect reduction instead'
changed=[]
for vertex in bad:
    repaired=False
    for face_index in np.flatnonzero(np.any(faces==vertex,axis=1)):
        face=faces[face_index];dp1=positions[face[1]]-positions[face[0]];dp2=positions[face[2]]-positions[face[0]]
        du1=uv[face[1]]-uv[face[0]];du2=uv[face[2]]-uv[face[0]]
        determinant=float(du1[0]*du2[1]-du1[1]*du2[0])
        if abs(determinant)<1e-12: continue
        t=(dp1*du2[1]-dp2*du1[1])/determinant;b=(dp2*du1[0]-dp1*du2[0])/determinant
        normal=normals[vertex];t=t-normal*np.dot(normal,t);length=float(np.linalg.norm(t))
        if length<1e-7: continue
        t/=length;sign=-1 if np.dot(np.cross(normal,t),b)<0 else 1
        tangents[vertex]=[*t,sign]
        changed.append({'vertex':int(vertex),'incidentTriangle':int(face_index),'basis':t.tolist(),'sign':sign});repaired=True;break
    assert repaired, ('No nondegenerate UV basis',int(vertex))
assert np.allclose(np.linalg.norm(tangents[:,:3],axis=1),1,atol=.02)
file.write_bytes(data)
receipt={'scope':'Diagnostic GLB only; does not prove Unity FBX tangent acceptance',
    'policy':'First ascending-index nondegenerate incident triangle UV gradient; Gram-Schmidt against existing normal; sign from its bitangent',
    'beforeSha256':before,'afterSha256':hashlib.sha256(data).hexdigest(),'repairedVertices':changed,
    'unchanged':'positions,indices,UVs,normals,images,materials,FBX/source files'}
receipt_file.write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(f'Repaired {len(changed)} zero diagnostic tangents; no source/UV/image/FBX change.')
